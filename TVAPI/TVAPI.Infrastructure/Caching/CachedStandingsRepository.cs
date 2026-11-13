using TVAPI.Application.Competitions;
using TVAPI.Domain.Competitions;
using TVAPI.Infrastructure.Persistence;

namespace TVAPI.Infrastructure.Caching;

/// <summary><see cref="StandingsRepository"/> üzerine önbellek ekler (decorator).</summary>
internal sealed class CachedStandingsRepository : IStandingsRepository
{
    private readonly StandingsRepository _inner;
    private readonly ResilientCache _cache;
    private readonly BroadageCacheOptions _options;

    public CachedStandingsRepository(StandingsRepository inner, ResilientCache cache, BroadageCacheOptions options)
    {
        _inner = inner;
        _cache = cache;
        _options = options;
    }

    public Task<Standings?> GetAsync(int tournamentId, int stageId, int roundId, CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"standings:{tournamentId}:{stageId}:{roundId}",
            _options.StandingsFreshFor,
            token => _inner.GetAsync(tournamentId, stageId, roundId, token),
            cancellationToken);
}
