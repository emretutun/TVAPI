using TVAPI.Application.Competitions;
using TVAPI.Domain.Competitions;
using TVAPI.Infrastructure.Persistence;

namespace TVAPI.Infrastructure.Caching;

/// <summary><see cref="MatchRepository"/> üzerine önbellek ekler (decorator).</summary>
internal sealed class CachedMatchRepository : IMatchRepository
{
    private readonly MatchRepository _inner;
    private readonly ResilientCache _cache;
    private readonly BroadageCacheOptions _options;

    public CachedMatchRepository(MatchRepository inner, ResilientCache cache, BroadageCacheOptions options)
    {
        _inner = inner;
        _cache = cache;
        _options = options;
    }

    public Task<IReadOnlyList<Match>> GetByRoundAsync(
        int tournamentId,
        int stageId,
        int roundId,
        CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"matches:{tournamentId}:{stageId}:{roundId}",
            _options.MatchesFreshFor,
            token => _inner.GetByRoundAsync(tournamentId, stageId, roundId, token),
            cancellationToken);

    public Task<Match?> GetByIdAsync(int matchId, CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"matches:id:{matchId}",
            _options.MatchesFreshFor,
            token => _inner.GetByIdAsync(matchId, token),
            cancellationToken);

    public Task<IReadOnlyList<Match>> GetByDateRangeAsync(
        IReadOnlyCollection<int> tournamentIds,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"matches:range:{string.Join(',', tournamentIds)}:{from:yyyyMMddHHmm}:{to:yyyyMMddHHmm}",
            _options.MatchesFreshFor,
            token => _inner.GetByDateRangeAsync(tournamentIds, from, to, token),
            cancellationToken);
}
