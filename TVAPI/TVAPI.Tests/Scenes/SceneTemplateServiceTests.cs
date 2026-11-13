using TVAPI.Application.Scenes;
using TVAPI.Domain.Scenes;

namespace TVAPI.Tests.Scenes;

public sealed class SceneTemplateServiceTests
{
    internal static readonly SceneTemplateDetail Kelebek = new(
        new SceneTemplate(
            Id: 4821,
            Name: "KELEBEK_TEK",
            Description: "Tek satır kelebek",
            DirectoryId: 157,
            ScenePath: "HTSPOR/KJ/KELEBEK_TEK",
            InDirectorName: "IN",
            OutDirectorName: "OUT",
            IntraDirectorName: "",
            IntraDirectorDelay: 0.5m,
            SceneResetEnabled: true,
            ConflictingTemplateIds: "12,48",
            TargetLayer: 1,
            CreatedAt: new DateTime(2024, 3, 1, 10, 0, 0),
            ModifiedAt: new DateTime(2026, 9, 12, 18, 30, 0)),
        Elements:
        [
            new SceneElement(1, "txt_baslik", Type: 1, "Başlık", DefaultValue: "", MaxSize: 40, Order: 1, UseDefault: false),
            new SceneElement(2, "img_logo", Type: 2, "Logo", DefaultValue: "logo.png", MaxSize: 0, Order: 2, UseDefault: true),
        ],
        Directors: [new SceneDirector(9, "IN", "Giriş", Length: 1.25m)]);

    [Fact]
    public async Task Template_is_mapped_with_its_elements_and_directors()
    {
        var service = new SceneTemplateService(new FakeSceneTemplateRepository { Templates = [Kelebek] });

        var template = await service.GetByIdAsync(4821, CancellationToken.None);

        Assert.NotNull(template);
        Assert.Equal("KELEBEK_TEK", template.Name);
        Assert.Equal(157, template.DirectoryId);
        Assert.Equal("HTSPOR/KJ/KELEBEK_TEK", template.ScenePath);
        Assert.Equal(0.5m, template.IntraDirectorDelay);
        Assert.True(template.SceneResetEnabled);
        Assert.Equal("12,48", template.ConflictingTemplateIds);

        Assert.Equal(["txt_baslik", "img_logo"], template.Elements.Select(element => element.Name));
        Assert.Equal(40, template.Elements[0].MaxSize);
        Assert.True(template.Elements[1].UseDefault);

        var director = Assert.Single(template.Directors);
        Assert.Equal("Giriş", director.Title);
        Assert.Equal(1.25m, director.Length);
    }

    [Fact]
    public async Task Unknown_template_returns_nothing()
    {
        var service = new SceneTemplateService(new FakeSceneTemplateRepository());

        Assert.Null(await service.GetByIdAsync(404, CancellationToken.None));
    }

    // Aynı sorgu hep aynı biçimde gitmelidir; yoksa her istek önbellekte ayrı bir kayıt açar.
    [Fact]
    public async Task Filters_are_normalized_before_reading()
    {
        var repository = new FakeSceneTemplateRepository { Templates = [Kelebek] };
        var service = new SceneTemplateService(repository);

        await service.SearchAsync([175, 157, 175], "  KELEBEK_TEK ", CancellationToken.None);

        Assert.Equal([157, 175], repository.RequestedDirectoryIds);
        Assert.Equal("KELEBEK_TEK", repository.RequestedName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_name_means_no_name_filter(string? name)
    {
        var repository = new FakeSceneTemplateRepository();
        var service = new SceneTemplateService(repository);

        await service.SearchAsync([157], name, CancellationToken.None);

        Assert.Null(repository.RequestedName);
    }

    // Süzgeçsiz istek bütün şablonları döndürürdü; servis buna veritabanına gitmeden karşı çıkar.
    [Fact]
    public async Task Search_without_any_filter_is_rejected_before_reading()
    {
        var repository = new FakeSceneTemplateRepository();
        var service = new SceneTemplateService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() => service.SearchAsync([], "  ", CancellationToken.None));

        Assert.Null(repository.RequestedDirectoryIds);
    }

    internal sealed class FakeSceneTemplateRepository : ISceneTemplateRepository
    {
        /// <summary>Servisin toplu şablon okumasında istediği id'ler; hiç istemediyse null.</summary>
        public IReadOnlyCollection<int>? RequestedIds { get; private set; }

        public Task<IReadOnlyList<SceneTemplateDetail>> GetByIdsAsync(
            IReadOnlyCollection<int> templateIds,
            CancellationToken cancellationToken)
        {
            RequestedIds = templateIds;
            return Task.FromResult<IReadOnlyList<SceneTemplateDetail>>(
                Templates.Where(template => templateIds.Contains(template.Template.Id)).ToList());
        }

        public IReadOnlyList<SceneTemplateDetail> Templates { get; init; } = [];

        public IReadOnlyCollection<int>? RequestedDirectoryIds { get; private set; }
        public string? RequestedName { get; private set; }

        public Task<IReadOnlyList<SceneTemplateDetail>> SearchAsync(
            IReadOnlyCollection<int> directoryIds,
            string? name,
            CancellationToken cancellationToken)
        {
            RequestedDirectoryIds = directoryIds;
            RequestedName = name;
            return Task.FromResult(Templates);
        }

        public Task<SceneTemplateDetail?> GetByIdAsync(int templateId, CancellationToken cancellationToken) =>
            Task.FromResult(Templates.FirstOrDefault(template => template.Template.Id == templateId));
    }
}
