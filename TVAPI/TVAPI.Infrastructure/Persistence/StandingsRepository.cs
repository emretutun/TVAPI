using TVAPI.Application.Competitions;
using TVAPI.Domain.Competitions;

namespace TVAPI.Infrastructure.Persistence;

internal sealed class StandingsRepository : IStandingsRepository
{
    // Üç sütun birlikte benzersiz index'tir; sorgu en fazla tek satır döner ve index'i kullanır.
    private const string StandingsSql = """
        SELECT standings
        FROM soccer_standings
        WHERE tournament_id = @TournamentId
          AND stage_id = @StageId
          AND round_id = @RoundId
        """;

    private readonly BroadageDatabase _database;

    public StandingsRepository(BroadageDatabase database)
    {
        _database = database;
    }

    public async Task<Standings?> GetAsync(int tournamentId, int stageId, int roundId, CancellationToken cancellationToken)
    {
        var json = await _database.QuerySingleOrDefaultAsync<string>(
            StandingsSql,
            new { TournamentId = tournamentId, StageId = stageId, RoundId = roundId },
            cancellationToken);

        // Okuma burada, JSON'u çözme işi StandingsJsonParser'da.
        return string.IsNullOrWhiteSpace(json)
            ? null
            : StandingsJsonParser.Parse(json, $"turnuva {tournamentId}, aşama {stageId}, hafta {roundId}");
    }
}
