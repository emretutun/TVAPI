using TVAPI.Application.News;
using TVAPI.Domain.News;
using TVAPI.Infrastructure.Persistence;

namespace TVAPI.Infrastructure.Caching;

/// <summary><see cref="NewsRepository"/> üzerine önbellek ekler (decorator).</summary>
internal sealed class CachedNewsRepository : INewsRepository
{
    private readonly NewsRepository _inner;
    private readonly ResilientCache _cache;
    private readonly NewsCacheOptions _options;

    public CachedNewsRepository(NewsRepository inner, ResilientCache cache, NewsCacheOptions options)
    {
        _inner = inner;
        _cache = cache;
        _options = options;
    }

    public Task<IReadOnlyList<Rundown>> GetRundownsAsync(DateOnly date, CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"news:rundowns:{date:yyyyMMdd}",
            _options.RundownsFreshFor,
            token => _inner.GetRundownsAsync(date, token),
            cancellationToken);

    public Task<IReadOnlyList<Story>> GetStoriesAsync(
        DateOnly date,
        int directoryId,
        int timeSlotId,
        CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"news:stories:{date:yyyyMMdd}:{directoryId}:{timeSlotId}",
            _options.StoriesFreshFor,
            token => _inner.GetStoriesAsync(date, directoryId, timeSlotId, token),
            cancellationToken);

    public Task<NewsScenes> GetStoryScenesAsync(long storyId, CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"news:story:{storyId}:scenes",
            _options.StoryScenesFreshFor,
            token => _inner.GetStoryScenesAsync(storyId, token),
            cancellationToken);
}
