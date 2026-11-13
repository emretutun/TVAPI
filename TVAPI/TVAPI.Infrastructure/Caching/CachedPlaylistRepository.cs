using TVAPI.Application.Scenes;
using TVAPI.Domain.Scenes;
using TVAPI.Infrastructure.Persistence;

namespace TVAPI.Infrastructure.Caching;

/// <summary><see cref="PlaylistRepository"/> üzerine önbellek ekler (decorator).</summary>
internal sealed class CachedPlaylistRepository : IPlaylistRepository
{
    private readonly PlaylistRepository _inner;
    private readonly ResilientCache _cache;
    private readonly ChannelCacheOptions _options;

    public CachedPlaylistRepository(PlaylistRepository inner, ResilientCache cache, ChannelCacheOptions options)
    {
        _inner = inner;
        _cache = cache;
        _options = options;
    }

    public Task<IReadOnlyList<Playlist>> GetByCategoriesAsync(
        IReadOnlyCollection<int> categoryIds,
        CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"scenes:playlists:categories={string.Join(',', categoryIds)}",
            _options.PlaylistsFreshFor,
            token => _inner.GetByCategoriesAsync(categoryIds, token),
            cancellationToken);

    public Task<Playlist?> GetByIdAsync(int playlistId, CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"scenes:playlist:{playlistId}",
            _options.PlaylistsFreshFor,
            token => _inner.GetByIdAsync(playlistId, token),
            cancellationToken);

    // Kayıtlı sahneler yayın sırasında operatörler tarafından eklenip değiştirilir; kısa süre tutulur.
    public Task<IReadOnlyList<SavedScene>> GetScenesAsync(int playlistId, CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"scenes:playlist:{playlistId}:scenes",
            _options.SavedScenesFreshFor,
            token => _inner.GetScenesAsync(playlistId, token),
            cancellationToken);

    public Task<SavedScene?> GetSceneByIdAsync(long savedSceneId, CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"scenes:saved:{savedSceneId}",
            _options.SavedScenesFreshFor,
            token => _inner.GetSceneByIdAsync(savedSceneId, token),
            cancellationToken);
}
