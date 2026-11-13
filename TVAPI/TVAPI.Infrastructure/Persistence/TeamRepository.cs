using Microsoft.Extensions.Logging;
using TVAPI.Application.Common;
using TVAPI.Application.Teams;
using TVAPI.Domain.Teams;

namespace TVAPI.Infrastructure.Persistence;

internal sealed class TeamRepository : ITeamRepository
{
    private const string TeamColumns = """
            t.team_id            AS TeamId,
            t.team_name          AS Name,
            t.team_short_name    AS ShortName,
            t.team_medium_name   AS MediumName,
            t.teknik_direktor    AS CoachName,
            t.country_id         AS CountryId,
            t.country_name       AS CountryName,
            t.country_short_name AS CountryShortName,
            t.stadium_id         AS StadiumId,
            t.stadium_name       AS StadiumName,
            t.stadium_short_name AS StadiumShortName,
            t.stadium_city_name  AS StadiumCityName
        """;

    // soccer_teams'te (tournament_id, stage_id, team_id) index'i benzersiz değildir: aynı takım için
    // birden çok satır olabilir. NOT EXISTS, her takımın yalnızca en güncel satırını bırakır.
    // Sorgu bu üç sütunlu index'i kullanır.
    private const string TeamsByStageSql = $"""
        SELECT
        {TeamColumns}
        FROM soccer_teams t
        WHERE t.tournament_id = @TournamentId
          AND t.stage_id = @StageId
          AND t.team_id <> 0
          AND NOT EXISTS (
                SELECT 1
                FROM soccer_teams newer
                WHERE newer.tournament_id = t.tournament_id
                  AND newer.stage_id = t.stage_id
                  AND newer.team_id = t.team_id
                  AND (IFNULL(newer.updated_at, '1970-01-01') > IFNULL(t.updated_at, '1970-01-01')
                       OR (IFNULL(newer.updated_at, '1970-01-01') = IFNULL(t.updated_at, '1970-01-01')
                           AND newer.id > t.id)))
        ORDER BY t.team_name, t.team_id
        """;

    // Takım birden çok turnuva ve sezonda kayıtlıdır; en son güncellenen satır geçerlidir.
    // DİKKAT: team_id tek başına index'li değildir, bu sorgu tabloyu baştan sona tarar (yerelde 18 bin satır).
    // Önbellek sayesinde seyrek çalışır; tablo büyürse team_id için index gerekir.
    private const string TeamByIdSql = $"""
        SELECT
        {TeamColumns}
        FROM soccer_teams t
        WHERE t.team_id = @TeamId
        ORDER BY t.updated_at DESC, t.id DESC
        LIMIT 1
        """;

    private const string SquadSql = """
        SELECT team_data
        FROM soccer_players
        WHERE team_id = @TeamId
        ORDER BY updated_at DESC, id DESC
        LIMIT 1
        """;

    // Takımın, kadrosu açıklanmış maçları; en yeni maç başta. Aynı maçın birden çok satırı olabildiği için
    // istenen maç sayısının iki katı satır okunur, tekrarlar kodda ayıklanır (eski NTR HTSPOR'daki yöntem).
    // DİKKAT: takım id'leri index'in başında olmadığı için sorgu soccer_matches'i baştan sona tarar.
    private const string RecentLineupsSql = """
        SELECT l.match_id AS MatchId, m.date AS MatchDate, l.lineups AS Json
        FROM soccer_lineups l
        INNER JOIN soccer_matches m ON m.match_id = l.match_id
        WHERE (m.home_team_id = @TeamId OR m.away_team_id = @TeamId)
          AND l.lineups <> 'null'
        ORDER BY m.date DESC, l.updated_at DESC, l.id DESC
        LIMIT @MaxRows
        """;

    private readonly BroadageDatabase _database;
    private readonly ILogger<TeamRepository> _logger;

    public TeamRepository(BroadageDatabase database, ILogger<TeamRepository> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Team>> GetByStageAsync(int tournamentId, int stageId, CancellationToken cancellationToken)
    {
        var rows = await _database.QueryAsync<TeamRow>(
            TeamsByStageSql,
            new { TournamentId = tournamentId, StageId = stageId },
            cancellationToken);

        return rows.Select(row => row.ToDomain()).ToList();
    }

    public async Task<Team?> GetByIdAsync(int teamId, CancellationToken cancellationToken)
    {
        var row = await _database.QuerySingleOrDefaultAsync<TeamRow>(
            TeamByIdSql,
            new { TeamId = teamId },
            cancellationToken);

        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<RecentLineup>> GetRecentLineupsAsync(
        int teamId,
        int maxMatches,
        CancellationToken cancellationToken)
    {
        var rows = await _database.QueryAsync<LineupRow>(
            RecentLineupsSql,
            new { TeamId = teamId, MaxRows = maxMatches * 2 },
            cancellationToken);

        var lineups = new List<RecentLineup>();
        foreach (var row in rows.DistinctBy(row => row.MatchId))
        {
            var players = ReadTeamPlayers(row, teamId);
            if (players is not null)
            {
                lineups.Add(new RecentLineup(row.MatchId, row.MatchDate, players));
            }
        }

        return lineups.Take(maxMatches).ToList();
    }

    // Kadro birçok maçtan derlenir; tek bir maçın bozuk kaydı bütün kadroyu düşürmemeli.
    // Bozuk kayıt atlanır ama sessizce değil: hangi maç olduğu loga yazılır.
    private IReadOnlyList<Player>? ReadTeamPlayers(LineupRow row, int teamId)
    {
        try
        {
            var lineup = MatchDetailJsonParser.ParseLineup(row.Json, $"maç {row.MatchId}");
            var side = lineup?.Home.Team.Id == teamId ? lineup.Home
                : lineup?.Away.Team.Id == teamId ? lineup.Away
                : null;

            if (side is null)
            {
                return null;
            }

            // Sahadaki yer (sol bek, ön libero...) o maça özgüdür; takım kadrosunda anlamı olmadığı için taşınmaz.
            return side.Starting
                .Concat(side.Bench)
                .Select(player => player with { TacticalPosition = null })
                .ToList();
        }
        catch (InvalidSourceDataException exception)
        {
            _logger.LogWarning(exception, "Takım {TeamId} kadrosu derlenirken maç {MatchId} atlandı.", teamId, row.MatchId);
            return null;
        }
    }

    public async Task<Squad> GetRegisteredSquadAsync(int teamId, CancellationToken cancellationToken)
    {
        var json = await _database.QuerySingleOrDefaultAsync<string>(
            SquadSql,
            new { TeamId = teamId },
            cancellationToken);

        return string.IsNullOrWhiteSpace(json)
            ? new Squad([])
            : SquadJsonParser.Parse(json, $"takım {teamId}");
    }

    private sealed class LineupRow
    {
        public int MatchId { get; set; }
        public DateTime MatchDate { get; set; }
        public string Json { get; set; } = string.Empty;
    }

    /// <summary>Sorgu sonucunun bire bir karşılığı. Yalnızca bu sınıfın içinde kullanılır.</summary>
    private sealed class TeamRow
    {
        public int TeamId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string MediumName { get; set; } = string.Empty;
        public string? CoachName { get; set; }
        public int CountryId { get; set; }
        public string? CountryName { get; set; }
        public string? CountryShortName { get; set; }
        public int StadiumId { get; set; }
        public string? StadiumName { get; set; }
        public string? StadiumShortName { get; set; }
        public string? StadiumCityName { get; set; }

        // Kaynak sütunlar NOT NULL olduğu için bilinmeyen değerler boş metin ya da 0 olarak gelir;
        // bunları dışarıya "yok" (null) olarak veririz.
        public Team ToDomain() => new(
            TeamId,
            Name,
            ShortName,
            MediumName,
            NullIfBlank(CoachName),
            string.IsNullOrWhiteSpace(CountryName)
                ? null
                : new Country(CountryId, CountryName, CountryShortName ?? string.Empty),
            string.IsNullOrWhiteSpace(StadiumName)
                ? null
                : new Stadium(StadiumId, StadiumName, StadiumShortName ?? string.Empty, NullIfBlank(StadiumCityName)));

        private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
