using TVAPI.Application.Teams;
using TVAPI.Domain.Teams;
using TVAPI.Infrastructure.Persistence;

namespace TVAPI.Infrastructure.Caching;

/// <summary><see cref="TeamRepository"/> üzerine önbellek ekler (decorator).</summary>
internal sealed class CachedTeamRepository : ITeamRepository
{
    private readonly TeamRepository _inner;
    private readonly ResilientCache _cache;
    private readonly BroadageCacheOptions _options;

    public CachedTeamRepository(TeamRepository inner, ResilientCache cache, BroadageCacheOptions options)
    {
        _inner = inner;
        _cache = cache;
        _options = options;
    }

    public Task<IReadOnlyList<Team>> GetByStageAsync(int tournamentId, int stageId, CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"teams:stage:{tournamentId}:{stageId}",
            _options.TeamsFreshFor,
            token => _inner.GetByStageAsync(tournamentId, stageId, token),
            cancellationToken);

    public Task<Team?> GetByIdAsync(int teamId, CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"teams:id:{teamId}",
            _options.TeamsFreshFor,
            token => _inner.GetByIdAsync(teamId, token),
            cancellationToken);

    public Task<Squad> GetRegisteredSquadAsync(int teamId, CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"teams:squad:{teamId}",
            _options.TeamsFreshFor,
            token => _inner.GetRegisteredSquadAsync(teamId, token),
            cancellationToken);

    public Task<IReadOnlyList<RecentLineup>> GetRecentLineupsAsync(
        int teamId,
        int maxMatches,
        CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"teams:lineups:{teamId}:{maxMatches}",
            _options.TeamsFreshFor,
            token => _inner.GetRecentLineupsAsync(teamId, maxMatches, token),
            cancellationToken);
}
