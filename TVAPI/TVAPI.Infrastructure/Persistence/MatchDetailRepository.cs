using TVAPI.Application.Matches;
using TVAPI.Domain.Matches;

namespace TVAPI.Infrastructure.Persistence;

internal sealed class MatchDetailRepository : IMatchDetailRepository
{
    // Üç tabloda da match_id index'lidir ama benzersiz değildir: aynı maç için birden çok satır olabilir.
    // En son güncellenen satır geçerlidir (eski NTR HTSPOR kodundaki kural).

    // Kadro açıklanmadan önce kaynak sütuna 'null' metnini yazar; daha eski dolu bir satır varsa o kullanılır.
    private const string LineupSql = """
        SELECT lineups
        FROM soccer_lineups
        WHERE match_id = @MatchId
          AND lineups <> 'null'
        ORDER BY updated_at DESC, id DESC
        LIMIT 1
        """;

    private const string EventsSql = """
        SELECT events
        FROM soccer_events
        WHERE match_id = @MatchId
        ORDER BY updated_at DESC, id DESC
        LIMIT 1
        """;

    private const string StatisticsSql = """
        SELECT stats
        FROM soccer_stats
        WHERE match_id = @MatchId
        ORDER BY updated_at DESC, id DESC
        LIMIT 1
        """;

    private readonly BroadageDatabase _database;

    public MatchDetailRepository(BroadageDatabase database)
    {
        _database = database;
    }

    public async Task<MatchLineup?> GetLineupAsync(int matchId, CancellationToken cancellationToken)
    {
        var json = await ReadJsonAsync(LineupSql, matchId, cancellationToken);

        return json is null ? null : MatchDetailJsonParser.ParseLineup(json, Describe(matchId));
    }

    public async Task<IReadOnlyList<MatchEvent>> GetEventsAsync(int matchId, CancellationToken cancellationToken)
    {
        var json = await ReadJsonAsync(EventsSql, matchId, cancellationToken);

        return json is null ? [] : MatchDetailJsonParser.ParseEvents(json, Describe(matchId));
    }

    public async Task<MatchStatistics?> GetStatisticsAsync(int matchId, CancellationToken cancellationToken)
    {
        var json = await ReadJsonAsync(StatisticsSql, matchId, cancellationToken);

        return json is null ? null : MatchDetailJsonParser.ParseStatistics(json, Describe(matchId));
    }

    private async Task<string?> ReadJsonAsync(string sql, int matchId, CancellationToken cancellationToken)
    {
        var json = await _database.QuerySingleOrDefaultAsync<string>(sql, new { MatchId = matchId }, cancellationToken);

        return string.IsNullOrWhiteSpace(json) ? null : json;
    }

    private static string Describe(int matchId) => $"maç {matchId}";
}
