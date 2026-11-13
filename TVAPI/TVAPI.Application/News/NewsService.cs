using TVAPI.Application.Scenes;
using TVAPI.Domain.News;

namespace TVAPI.Application.News;

public interface INewsService
{
    /// <summary>Bir günün yayın akışlarını getirir; tarih verilmezse bugün.</summary>
    Task<IReadOnlyList<RundownDto>> GetRundownsAsync(DateOnly? date, CancellationToken cancellationToken);

    /// <summary>Bir yayın akışındaki haberleri getirir.</summary>
    Task<IReadOnlyList<StoryDto>> GetStoriesAsync(
        DateOnly date,
        int directoryId,
        int timeSlotId,
        CancellationToken cancellationToken);

    /// <summary>Habere bağlı kayıtlı sahneleri ve kullandıkları şablonları getirir; sahne yoksa listeler boş gelir.</summary>
    Task<StoryScenesDto> GetStoryScenesAsync(long storyId, CancellationToken cancellationToken);
}

internal sealed class NewsService : INewsService
{
    private readonly INewsRepository _newsRepository;
    private readonly ISceneTemplateRepository _templateRepository;
    private readonly TimeProvider _timeProvider;

    public NewsService(
        INewsRepository newsRepository,
        ISceneTemplateRepository templateRepository,
        TimeProvider timeProvider)
    {
        _newsRepository = newsRepository;
        _templateRepository = templateRepository;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<RundownDto>> GetRundownsAsync(DateOnly? date, CancellationToken cancellationToken)
    {
        var day = date ?? DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var rundowns = await _newsRepository.GetRundownsAsync(day, cancellationToken);

        return rundowns.Select(RundownDto.From).ToList();
    }

    public async Task<IReadOnlyList<StoryDto>> GetStoriesAsync(
        DateOnly date,
        int directoryId,
        int timeSlotId,
        CancellationToken cancellationToken)
    {
        var stories = await _newsRepository.GetStoriesAsync(date, directoryId, timeSlotId, cancellationToken);

        return stories.Select(StoryDto.From).ToList();
    }

    public async Task<StoryScenesDto> GetStoryScenesAsync(long storyId, CancellationToken cancellationToken)
    {
        var scenes = await _newsRepository.GetStoryScenesAsync(storyId, cancellationToken);

        // Sahneler haber veritabanında, şablonları kanal veritabanındadır; şablonlar tek seferde okunur.
        var templateIds = scenes.Scenes.Select(scene => scene.TemplateId).Distinct().Order().ToList();
        var templates = templateIds.Count == 0
            ? []
            : await _templateRepository.GetByIdsAsync(templateIds, cancellationToken);

        return new StoryScenesDto(
            storyId,
            scenes.Source == NewsSceneSource.NewsSavedScenes ? "newsSavedScenes" : "vizSavedScenes",
            scenes.Scenes.Select(NewsSceneDto.From).ToList(),
            templates.Select(SceneTemplateDto.From).ToList());
    }
}
