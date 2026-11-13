using TVAPI.Application.Competitions;
using TVAPI.Domain.Competitions;

namespace TVAPI.Infrastructure.Persistence;

internal sealed class CompetitionRepository : ICompetitionRepository
{
    // stages ve rounds tablolarında voleybol kayıtları da var; futbol dışındakileri eliyoruz.
    private const string SportType = "soccer";

    // Bir turnuvanın birden çok aktif aşaması olabilir. Sıralama, eski NTR HTSPOR kodundaki kuralın aynısı:
    // en yeni sezon, tercihen aktif haftası olan aşama, en son güncellenen kayıt.
    private const string CurrentCompetitionSql = """
        SELECT
            t.tournament_id AS TournamentId,
            t.name          AS TournamentName,
            s.stage_id      AS StageId,
            s.name          AS StageName,
            s.short_name    AS StageShortName,
            r.round_id      AS RoundId,
            r.name          AS RoundName,
            r.short_name    AS RoundShortName
        FROM stages s
        INNER JOIN soccer_tournaments t
                ON t.tournament_id = s.tournament_id
        LEFT JOIN rounds r
               ON r.stage_id = s.stage_id
              AND r.active = 1
              AND r.sport_type = @SportType
        WHERE s.tournament_id = @TournamentId
          AND s.sport_type = @SportType
          AND s.active = 1
        ORDER BY s.season_id DESC, (r.round_id IS NULL), s.updated_at DESC, s.id DESC
        LIMIT 1
        """;

    private const string TournamentsSql = """
        SELECT tournament_id AS Id, name AS Name
        FROM soccer_tournaments
        ORDER BY tournament_id
        """;

    private readonly BroadageDatabase _database;

    public CompetitionRepository(BroadageDatabase database)
    {
        _database = database;
    }

    public async Task<CurrentCompetition?> GetCurrentAsync(int tournamentId, CancellationToken cancellationToken)
    {
        var row = await _database.QuerySingleOrDefaultAsync<CurrentCompetitionRow>(
            CurrentCompetitionSql,
            new { TournamentId = tournamentId, SportType },
            cancellationToken);

        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<Tournament>> GetTournamentsAsync(CancellationToken cancellationToken)
    {
        var rows = await _database.QueryAsync<TournamentRow>(TournamentsSql, new { }, cancellationToken);

        return rows.Select(row => new Tournament(row.Id, row.Name)).ToList();
    }

    private sealed class TournamentRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Sorgu sonucunun bire bir karşılığı. Yalnızca bu sınıfın içinde kullanılır, dışarı Domain nesnesi çıkar.</summary>
    private sealed class CurrentCompetitionRow
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public int StageId { get; set; }
        public string StageName { get; set; } = string.Empty;
        public string StageShortName { get; set; } = string.Empty;
        public int? RoundId { get; set; }
        public string? RoundName { get; set; }
        public string? RoundShortName { get; set; }

        public CurrentCompetition ToDomain() => new(
            new Tournament(TournamentId, TournamentName),
            new Stage(StageId, StageName, StageShortName),
            RoundId is null
                ? null
                : new Round(RoundId.Value, RoundName ?? string.Empty, RoundShortName ?? string.Empty));
    }
}
