using TVAPI.Application.Scenes;
using TVAPI.Domain.Scenes;
using static TVAPI.Tests.Scenes.SceneTemplateServiceTests;

namespace TVAPI.Tests.Scenes;

public sealed class PlaylistServiceTests
{
    private const int TemplateId = 4821; // SceneTemplateServiceTests.Kelebek

    private static readonly Playlist Isimler = new(
        Id: 16,
        Name: "İSİMLER",
        CategoryId: 3,
        UserId: 0,
        FixedDirectoryId: 157,
        FixedTemplateId: null,
        CreatedAt: new DateTime(2024, 1, 5, 9, 0, 0));

    [Fact]
    public async Task Playlists_are_read_for_normalized_categories()
    {
        var playlists = new FakePlaylistRepository { Playlists = [Isimler] };
        var service = new PlaylistService(playlists, new FakeSceneTemplateRepository());

        var result = await service.GetByCategoriesAsync([3, 2, 3], CancellationToken.None);

        Assert.Equal([2, 3], playlists.RequestedCategoryIds);
        var playlist = Assert.Single(result);
        Assert.Equal("İSİMLER", playlist.Name);
        Assert.Equal(157, playlist.FixedDirectoryId);
        Assert.Null(playlist.FixedTemplateId);
    }

    [Fact]
    public async Task Playlists_without_any_category_are_rejected_before_reading()
    {
        var playlists = new FakePlaylistRepository();
        var service = new PlaylistService(playlists, new FakeSceneTemplateRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByCategoriesAsync([], CancellationToken.None));

        Assert.Null(playlists.RequestedCategoryIds);
    }

    [Fact]
    public async Task Unknown_playlist_returns_nothing_without_reading_scenes()
    {
        var playlists = new FakePlaylistRepository();
        var service = new PlaylistService(playlists, new FakeSceneTemplateRepository());

        Assert.Null(await service.GetContentAsync(404, CancellationToken.None));
        Assert.False(playlists.ScenesRequested);
    }

    // "Playlist yok" null'dır (404). "Playlist var ama boş" ise boş listelerle dolu bir cevaptır (200).
    [Fact]
    public async Task Empty_playlist_returns_empty_lists_and_reads_no_templates()
    {
        var templates = new FakeSceneTemplateRepository { Templates = [Kelebek] };
        var service = new PlaylistService(new FakePlaylistRepository { Playlists = [Isimler] }, templates);

        var content = await service.GetContentAsync(Isimler.Id, CancellationToken.None);

        Assert.NotNull(content);
        Assert.Equal(Isimler.Id, content.Playlist.Id);
        Assert.Empty(content.Scenes);
        Assert.Empty(content.Templates);
        Assert.Null(templates.RequestedIds);
    }

    // Aynı şablondan üç sahne olsa da şablon bir kez okunur ve cevapta bir kez yer alır.
    [Fact]
    public async Task Templates_used_by_the_scenes_are_read_once_and_returned_once()
    {
        var templates = new FakeSceneTemplateRepository { Templates = [Kelebek] };
        var playlists = new FakePlaylistRepository
        {
            Playlists = [Isimler],
            Scenes = [NewScene(1, order: 1), NewScene(2, order: 2), NewScene(3, order: 3)],
        };
        var service = new PlaylistService(playlists, templates);

        var content = await service.GetContentAsync(Isimler.Id, CancellationToken.None);

        Assert.Equal([1L, 2L, 3L], content!.Scenes.Select(scene => scene.Id));
        Assert.All(content.Scenes, scene => Assert.Equal(TemplateId, scene.TemplateId));
        Assert.Equal([TemplateId], templates.RequestedIds);
        Assert.Equal(TemplateId, Assert.Single(content.Templates).Id);
    }

    [Fact]
    public async Task Scene_fields_are_mapped()
    {
        var playlists = new FakePlaylistRepository { Playlists = [Isimler], Scenes = [NewScene(7, order: 4)] };
        var service = new PlaylistService(playlists, new FakeSceneTemplateRepository { Templates = [Kelebek] });

        var detail = await service.GetSceneAsync(7, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal(7, detail.Scene.Id);
        Assert.Equal(Isimler.Id, detail.Scene.PlaylistId);
        Assert.Equal(4, detail.Scene.Order);
        Assert.Equal("Ali Veli", detail.Scene.Description);
        Assert.Equal("txt_baslik=ALİ VELİ", detail.Scene.Parameters);
        Assert.Null(detail.Scene.Metadata);
        Assert.Equal("KELEBEK_TEK", detail.Template.Name);
    }

    [Fact]
    public async Task Unknown_scene_returns_nothing()
    {
        var service = new PlaylistService(new FakePlaylistRepository(), new FakeSceneTemplateRepository());

        Assert.Null(await service.GetSceneAsync(404, CancellationToken.None));
    }

    // Sahne ve şablonu iki ayrı okumadır; şablon arada silinmişse sahne oynatılamaz, bu yüzden dönmez.
    [Fact]
    public async Task Scene_whose_template_is_gone_returns_nothing()
    {
        var playlists = new FakePlaylistRepository { Playlists = [Isimler], Scenes = [NewScene(7, order: 1)] };
        var service = new PlaylistService(playlists, new FakeSceneTemplateRepository());

        Assert.Null(await service.GetSceneAsync(7, CancellationToken.None));
    }

    private static SavedScene NewScene(long id, int order) => new(
        id,
        TemplateId,
        Isimler.Id,
        order,
        Description: "Ali Veli",
        Parameters: "txt_baslik=ALİ VELİ",
        Metadata: null,
        ModifiedAt: new DateTime(2026, 11, 13, 14, 0, 0),
        CreatedAt: new DateTime(2026, 11, 13, 13, 0, 0));

    private sealed class FakePlaylistRepository : IPlaylistRepository
    {
        public IReadOnlyList<Playlist> Playlists { get; init; } = [];
        public IReadOnlyList<SavedScene> Scenes { get; init; } = [];

        public IReadOnlyCollection<int>? RequestedCategoryIds { get; private set; }
        public bool ScenesRequested { get; private set; }

        public Task<IReadOnlyList<Playlist>> GetByCategoriesAsync(
            IReadOnlyCollection<int> categoryIds,
            CancellationToken cancellationToken)
        {
            RequestedCategoryIds = categoryIds;
            return Task.FromResult(Playlists);
        }

        public Task<Playlist?> GetByIdAsync(int playlistId, CancellationToken cancellationToken) =>
            Task.FromResult(Playlists.FirstOrDefault(playlist => playlist.Id == playlistId));

        public Task<IReadOnlyList<SavedScene>> GetScenesAsync(int playlistId, CancellationToken cancellationToken)
        {
            ScenesRequested = true;
            return Task.FromResult(Scenes);
        }

        public Task<SavedScene?> GetSceneByIdAsync(long savedSceneId, CancellationToken cancellationToken) =>
            Task.FromResult(Scenes.FirstOrDefault(scene => scene.Id == savedSceneId));
    }
}
