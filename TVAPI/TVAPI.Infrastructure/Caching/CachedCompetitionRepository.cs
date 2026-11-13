using TVAPI.Application.Competitions;
using TVAPI.Domain.Competitions;
using TVAPI.Infrastructure.Persistence;

namespace TVAPI.Infrastructure.Caching;

/// <summary>
/// <see cref="CompetitionRepository"/> üzerine önbellek ekler (decorator).
/// Aynı arayüzü uygular; servisler önbellek olduğunu bilmez, asıl repository de bilmez.
/// </summary>
internal sealed class CachedCompetitionRepository : ICompetitionRepository
{
    private readonly CompetitionRepository _inner;
    private readonly ResilientCache _cache;
    private readonly BroadageCacheOptions _options;

    public CachedCompetitionRepository(CompetitionRepository inner, ResilientCache cache, BroadageCacheOptions options)
    {
        _inner = inner;
        _cache = cache;
        _options = options;
    }

    public Task<CurrentCompetition?> GetCurrentAsync(int tournamentId, CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"competition:current:{tournamentId}",
            _options.CompetitionFreshFor,
            token => _inner.GetCurrentAsync(tournamentId, token),
            cancellationToken);

    // Turnuva listesi sezonda bir iki kez değişir; takım künyesiyle aynı uzun süreyi kullanır.
    public Task<IReadOnlyList<Tournament>> GetTournamentsAsync(CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            "competition:tournaments",
            _options.TeamsFreshFor,
            _inner.GetTournamentsAsync,
            cancellationToken);
}
