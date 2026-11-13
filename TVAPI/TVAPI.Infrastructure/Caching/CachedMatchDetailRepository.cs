using TVAPI.Application.Matches;
using TVAPI.Domain.Matches;
using TVAPI.Infrastructure.Persistence;

namespace TVAPI.Infrastructure.Caching;

/// <summary><see cref="MatchDetailRepository"/> üzerine önbellek ekler (decorator).</summary>
internal sealed class CachedMatchDetailRepository : IMatchDetailRepository
{
    private readonly MatchDetailRepository _inner;
    private readonly ResilientCache _cache;
    private readonly BroadageCacheOptions _options;

    public CachedMatchDetailRepository(MatchDetailRepository inner, ResilientCache cache, BroadageCacheOptions options)
    {
        _inner = inner;
        _cache = cache;
        _options = options;
    }

    public Task<MatchLineup?> GetLineupAsync(int matchId, CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"matches:lineup:{matchId}",
            _options.LineupsFreshFor,
            token => _inner.GetLineupAsync(matchId, token),
            cancellationToken);

    // Olaylar ve istatistikler canlı maçta skorla birlikte değişir; maç listesiyle aynı kısa süreyi kullanır.
    public Task<IReadOnlyList<MatchEvent>> GetEventsAsync(int matchId, CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"matches:events:{matchId}",
            _options.MatchesFreshFor,
            token => _inner.GetEventsAsync(matchId, token),
            cancellationToken);

    public Task<MatchStatistics?> GetStatisticsAsync(int matchId, CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"matches:statistics:{matchId}",
            _options.MatchesFreshFor,
            token => _inner.GetStatisticsAsync(matchId, token),
            cancellationToken);
}
