using TVAPI.Application.Competitions;
using TVAPI.Domain.Competitions;

namespace TVAPI.Infrastructure.Persistence;

internal sealed class MatchRepository : IMatchRepository
{
    private const string MatchColumns = """
            m.match_id                  AS Id,
            m.tournament_id             AS TournamentId,
            m.round_id                  AS RoundId,
            m.round_name                AS RoundName,
            m.round_short_name          AS RoundShortName,
            m.stage_id                  AS StageId,
            m.stage_name                AS StageName,
            m.stage_short_name          AS StageShortName,
            m.stadium_id                AS StadiumId,
            m.referee_id                AS RefereeId,
            m.date                      AS Date,
            m.status_id                 AS StatusId,
            m.status_name               AS StatusName,
            m.status_short_name         AS StatusShortName,
            m.current_minute            AS CurrentMinute,
            m.stoppage                  AS Stoppage,
            m.stadium_name              AS StadiumName,
            m.referee_name              AS RefereeName,
            m.home_team_id              AS HomeTeamId,
            m.home_team_name            AS HomeTeamName,
            m.home_team_short_name      AS HomeTeamShortName,
            m.home_team_medium_name     AS HomeTeamMediumName,
            m.home_team_score_current   AS HomeScoreCurrent,
            m.home_team_score_regular   AS HomeScoreRegular,
            m.home_team_score_halftime  AS HomeScoreHalfTime,
            m.home_team_score_extratime AS HomeScoreExtraTime,
            m.home_team_score_penalties AS HomeScorePenalties,
            m.away_team_id              AS AwayTeamId,
            m.away_team_name            AS AwayTeamName,
            m.away_team_short_name      AS AwayTeamShortName,
            m.away_team_medium_name     AS AwayTeamMediumName,
            m.away_team_score_current   AS AwayScoreCurrent,
            m.away_team_score_regular   AS AwayScoreRegular,
            m.away_team_score_halftime  AS AwayScoreHalfTime,
            m.away_team_score_extratime AS AwayScoreExtraTime,
            m.away_team_score_penalties AS AwayScorePenalties
        """;

    // WHERE koşulu (tournament_id, stage_id, round_id, match_id) benzersiz index'inin ilk üç sütununu kullanır.
    //
    // NOT EXISTS kısmı ertelenen maçlar içindir: aynı maç başka bir haftaya taşındığında eski haftadaki
    // satır tabloda kalabilir. Aynı aşamada daha güncel bir kaydı olan maç bu haftanın listesine girmez.
    // Kural eski NTR HTSPOR kodundakiyle aynıdır; fark, alt sorgunun aynı turnuva ve aşamayla
    // sınırlandırılmasıdır. Böylece alt sorgu da index kullanır, tüm tabloyu taramaz.
    private const string MatchesByRoundSql = $"""
        SELECT
        {MatchColumns}
        FROM soccer_matches m
        WHERE m.tournament_id = @TournamentId
          AND m.stage_id = @StageId
          AND m.round_id = @RoundId
          AND NOT EXISTS (
              SELECT 1
              FROM soccer_matches newer
              WHERE newer.tournament_id = m.tournament_id
                AND newer.stage_id = m.stage_id
                AND newer.match_id = m.match_id
                AND (
                    IFNULL(newer.updated_at, '1970-01-01') > IFNULL(m.updated_at, '1970-01-01')
                    OR (IFNULL(newer.updated_at, '1970-01-01') = IFNULL(m.updated_at, '1970-01-01') AND newer.id > m.id)
                )
          )
        ORDER BY m.date, m.match_id
        """;

    // Aynı maçın birden çok satırı olabilir (ertelenip başka haftaya taşınmışsa); en son güncellenen geçerlidir.
    // DİKKAT: soccer_matches'te match_id tek başına index'li değildir, bu sorgu tabloyu baştan sona tarar.
    // Önbellek sayesinde aynı maç için birkaç saniyede bir çalışır; tablo büyürse match_id için index gerekir.
    private const string MatchByIdSql = $"""
        SELECT
        {MatchColumns}
        FROM soccer_matches m
        WHERE m.match_id = @MatchId
        ORDER BY m.updated_at DESC, m.id DESC
        LIMIT 1
        """;

    // Tarih sütununda index yoktur; sorgu tournament_id index'iyle turnuvanın maçlarına iner, tarihi onların içinde süzer.
    // Bu yüzden en az bir turnuva verilmesi zorunludur.
    // NOT EXISTS: ertelenip başka haftaya taşınan maçın eski satırı listeye girmez (hafta sorgusundaki kuralın aynısı).
    private const string MatchesByDateRangeSql = $"""
        SELECT
        {MatchColumns}
        FROM soccer_matches m
        WHERE m.tournament_id IN @TournamentIds
          AND m.date >= @From
          AND m.date < @To
          AND NOT EXISTS (
              SELECT 1
              FROM soccer_matches newer
              WHERE newer.tournament_id = m.tournament_id
                AND newer.match_id = m.match_id
                AND (
                    IFNULL(newer.updated_at, '1970-01-01') > IFNULL(m.updated_at, '1970-01-01')
                    OR (IFNULL(newer.updated_at, '1970-01-01') = IFNULL(m.updated_at, '1970-01-01') AND newer.id > m.id)
                )
          )
        ORDER BY m.date, m.match_id
        """;

    private readonly BroadageDatabase _database;

    public MatchRepository(BroadageDatabase database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<Match>> GetByRoundAsync(
        int tournamentId,
        int stageId,
        int roundId,
        CancellationToken cancellationToken)
    {
        var rows = await _database.QueryAsync<MatchRow>(
            MatchesByRoundSql,
            new { TournamentId = tournamentId, StageId = stageId, RoundId = roundId },
            cancellationToken);

        return rows.Select(row => row.ToDomain()).ToList();
    }

    public async Task<Match?> GetByIdAsync(int matchId, CancellationToken cancellationToken)
    {
        var row = await _database.QuerySingleOrDefaultAsync<MatchRow>(
            MatchByIdSql,
            new { MatchId = matchId },
            cancellationToken);

        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<Match>> GetByDateRangeAsync(
        IReadOnlyCollection<int> tournamentIds,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        if (tournamentIds.Count == 0)
        {
            return [];
        }

        var rows = await _database.QueryAsync<MatchRow>(
            MatchesByDateRangeSql,
            new { TournamentIds = tournamentIds, From = from, To = to },
            cancellationToken);

        return rows.Select(row => row.ToDomain()).ToList();
    }

    /// <summary>Sorgu sonucunun bire bir karşılığı. Yalnızca bu sınıfın içinde kullanılır.</summary>
    private sealed class MatchRow
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public int? RoundId { get; set; }
        public string? RoundName { get; set; }
        public string? RoundShortName { get; set; }
        public int StageId { get; set; }
        public string? StageName { get; set; }
        public string? StageShortName { get; set; }
        public int? StadiumId { get; set; }
        public int? RefereeId { get; set; }
        public DateTime Date { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string StatusShortName { get; set; } = string.Empty;
        public int? CurrentMinute { get; set; }
        public int? Stoppage { get; set; }
        public string? StadiumName { get; set; }
        public string? RefereeName { get; set; }

        public int HomeTeamId { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string HomeTeamShortName { get; set; } = string.Empty;
        public string HomeTeamMediumName { get; set; } = string.Empty;
        public int? HomeScoreCurrent { get; set; }
        public int? HomeScoreRegular { get; set; }
        public int? HomeScoreHalfTime { get; set; }
        public int? HomeScoreExtraTime { get; set; }
        public int? HomeScorePenalties { get; set; }

        public int AwayTeamId { get; set; }
        public string AwayTeamName { get; set; } = string.Empty;
        public string AwayTeamShortName { get; set; } = string.Empty;
        public string AwayTeamMediumName { get; set; } = string.Empty;
        public int? AwayScoreCurrent { get; set; }
        public int? AwayScoreRegular { get; set; }
        public int? AwayScoreHalfTime { get; set; }
        public int? AwayScoreExtraTime { get; set; }
        public int? AwayScorePenalties { get; set; }

        public Match ToDomain() => new(
            Id,
            Date,
            new MatchStatus(StatusId, StatusName, StatusShortName),
            CurrentMinute,
            Stoppage,
            new MatchSide(
                new TeamSummary(HomeTeamId, HomeTeamName, HomeTeamShortName, HomeTeamMediumName),
                new MatchScore(HomeScoreCurrent, HomeScoreRegular, HomeScoreHalfTime, HomeScoreExtraTime, HomeScorePenalties)),
            new MatchSide(
                new TeamSummary(AwayTeamId, AwayTeamName, AwayTeamShortName, AwayTeamMediumName),
                new MatchScore(AwayScoreCurrent, AwayScoreRegular, AwayScoreHalfTime, AwayScoreExtraTime, AwayScorePenalties)),
            StadiumName,
            RefereeName,
            TournamentId,
            new Stage(StageId, StageName ?? string.Empty, StageShortName ?? string.Empty),
            // Kaynak hafta bilgisi olmayan maçta hafta yerine 0 yazabilir.
            RoundId is > 0
                ? new Round(RoundId.Value, RoundName ?? string.Empty, RoundShortName ?? string.Empty)
                : null,
            StadiumId is > 0 ? StadiumId : null,
            RefereeId is > 0 ? RefereeId : null);
    }
}
