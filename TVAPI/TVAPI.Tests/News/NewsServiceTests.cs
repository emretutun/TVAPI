using TVAPI.Application.News;
using TVAPI.Domain.News;
using static TVAPI.Tests.Scenes.SceneTemplateServiceTests;

namespace TVAPI.Tests.News;

/// <summary>"Bugün" 14 Kasım 2026.</summary>
public sealed class NewsServiceTests
{
    private static readonly DateOnly Today = new(2026, 11, 14);
    private const int TemplateId = 4821; // SceneTemplateServiceTests.Kelebek

    [Fact]
    public async Task Rundowns_are_read_for_today_when_no_date_is_given()
    {
        var news = new FakeNewsRepository
        {
            Rundowns = [new Rundown(101, Today, 350, "Ana Haber", 12, "Haber Bültenleri", 19, "19:00")],
        };
        var service = NewService(news);

        var rundown = Assert.Single(await service.GetRundownsAsync(date: null, CancellationToken.None));

        Assert.Equal(Today, news.RequestedDate);
        Assert.Equal(101, rundown.Id);
        Assert.Equal(350, rundown.DirectoryId);
        Assert.Equal("Ana Haber", rundown.DirectoryName);
        Assert.Equal(12, rundown.ParentDirectoryId);
        Assert.Equal(19, rundown.TimeSlotId);
        Assert.Equal("19:00", rundown.Time);
        Assert.False(rundown.IsArchive);
    }

    [Fact]
    public async Task Rundowns_are_read_for_the_given_date()
    {
        var news = new FakeNewsRepository();
        var service = NewService(news);

        await service.GetRundownsAsync(new DateOnly(2026, 11, 20), CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 11, 20), news.RequestedDate);
    }

    [Fact]
    public async Task Stories_are_returned_in_source_order()
    {
        var news = new FakeNewsRepository { Stories = [new Story(9002, "Derbi öncesi son durum"), new Story(9001, "Transfer gündemi")] };
        var service = NewService(news);

        var stories = await service.GetStoriesAsync(Today, 350, 19, CancellationToken.None);

        Assert.Equal([9002L, 9001L], stories.Select(story => story.Id));
        Assert.Equal("Derbi öncesi son durum", stories[0].Title);
    }

    // Aynı şablondan iki sahne olsa da şablon bir kez okunur ve cevapta bir kez yer alır.
    [Fact]
    public async Task Story_scenes_come_with_their_templates_once()
    {
        var templates = new FakeSceneTemplateRepository { Templates = [Kelebek] };
        var news = new FakeNewsRepository
        {
            Scenes = new NewsScenes(
                NewsSceneSource.NewsSavedScenes,
                [NewScene(1, order: 1, parameters: "txt_baslik=ALİ VELİ"), NewScene(2, order: 2, parameters: "txt_baslik=AYŞE")]),
        };
        var service = new NewsService(news, templates, Clock());

        var result = await service.GetStoryScenesAsync(9001, CancellationToken.None);

        Assert.Equal(9001, result.StoryId);
        Assert.Equal("newsSavedScenes", result.Source);
        Assert.Equal([1L, 2L], result.Scenes.Select(scene => scene.Id));
        Assert.Equal("txt_baslik=ALİ VELİ", result.Scenes[0].Parameters);
        Assert.Equal([TemplateId], templates.RequestedIds);
        Assert.Equal(TemplateId, Assert.Single(result.Templates).Id);
    }

    [Fact]
    public async Task Scenes_from_viz_records_are_marked_and_have_no_parameters()
    {
        var news = new FakeNewsRepository
        {
            Scenes = new NewsScenes(NewsSceneSource.VizSavedScenes, [NewScene(5, order: 1, parameters: null)]),
        };
        var service = new NewsService(news, new FakeSceneTemplateRepository { Templates = [Kelebek] }, Clock());

        var result = await service.GetStoryScenesAsync(9001, CancellationToken.None);

        Assert.Equal("vizSavedScenes", result.Source);
        Assert.Null(Assert.Single(result.Scenes).Parameters);
    }

    [Fact]
    public async Task Story_without_scenes_returns_empty_lists_and_reads_no_templates()
    {
        var templates = new FakeSceneTemplateRepository { Templates = [Kelebek] };
        var service = new NewsService(new FakeNewsRepository(), templates, Clock());

        var result = await service.GetStoryScenesAsync(9001, CancellationToken.None);

        Assert.Empty(result.Scenes);
        Assert.Empty(result.Templates);
        Assert.Null(templates.RequestedIds);
    }

    private static NewsScene NewScene(long id, int order, string? parameters) =>
        new(id, TemplateId, StoryId: 9001, order, "Açıklama", parameters, new DateTime(2026, 11, 14, 18, 30, 0));

    private static NewsService NewService(FakeNewsRepository news) =>
        new(news, new FakeSceneTemplateRepository(), Clock());

    private static TimeProvider Clock() =>
        new FixedTimeProvider(new DateTimeOffset(Today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class FakeNewsRepository : INewsRepository
    {
        public IReadOnlyList<Rundown> Rundowns { get; init; } = [];
        public IReadOnlyList<Story> Stories { get; init; } = [];
        public NewsScenes Scenes { get; init; } = new(NewsSceneSource.VizSavedScenes, []);

        public DateOnly? RequestedDate { get; private set; }

        public Task<IReadOnlyList<Rundown>> GetRundownsAsync(DateOnly date, CancellationToken cancellationToken)
        {
            RequestedDate = date;
            return Task.FromResult(Rundowns);
        }

        public Task<IReadOnlyList<Story>> GetStoriesAsync(
            DateOnly date,
            int directoryId,
            int timeSlotId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Stories);

        public Task<NewsScenes> GetStoryScenesAsync(long storyId, CancellationToken cancellationToken) =>
            Task.FromResult(Scenes);
    }
}
