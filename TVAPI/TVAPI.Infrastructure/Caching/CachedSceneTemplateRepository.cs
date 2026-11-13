using TVAPI.Application.Scenes;
using TVAPI.Domain.Scenes;
using TVAPI.Infrastructure.Persistence;

namespace TVAPI.Infrastructure.Caching;

/// <summary><see cref="SceneTemplateRepository"/> üzerine önbellek ekler (decorator).</summary>
internal sealed class CachedSceneTemplateRepository : ISceneTemplateRepository
{
    private readonly SceneTemplateRepository _inner;
    private readonly ResilientCache _cache;
    private readonly ChannelCacheOptions _options;

    public CachedSceneTemplateRepository(SceneTemplateRepository inner, ResilientCache cache, ChannelCacheOptions options)
    {
        _inner = inner;
        _cache = cache;
        _options = options;
    }

    public Task<IReadOnlyList<SceneTemplateDetail>> SearchAsync(
        IReadOnlyCollection<int> directoryIds,
        string? name,
        CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"scenes:templates:dir={string.Join(',', directoryIds)}:name={name}",
            _options.SceneTemplatesFreshFor,
            token => _inner.SearchAsync(directoryIds, name, token),
            cancellationToken);

    public Task<IReadOnlyList<SceneTemplateDetail>> GetByIdsAsync(
        IReadOnlyCollection<int> templateIds,
        CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"scenes:templates:ids={string.Join(',', templateIds)}",
            _options.SceneTemplatesFreshFor,
            token => _inner.GetByIdsAsync(templateIds, token),
            cancellationToken);

    public Task<SceneTemplateDetail?> GetByIdAsync(int templateId, CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"scenes:template:{templateId}",
            _options.SceneTemplatesFreshFor,
            token => _inner.GetByIdAsync(templateId, token),
            cancellationToken);
}
