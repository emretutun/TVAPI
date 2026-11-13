using TVAPI.Application.Scenes;
using TVAPI.Domain.News;

namespace TVAPI.Application.News;

/// <param name="IsArchive">
/// Akışın arşivden gelip gelmediği. API şimdilik yalnızca güncel tabloları okuduğu için her zaman false'tur;
/// alan, eski prosedürün cevabıyla uyumlu kalsın diye vardır.
/// </param>
public sealed record RundownDto(
    int Id,
    DateOnly Date,
    int DirectoryId,
    string DirectoryName,
    long ParentDirectoryId,
    string ParentDirectoryName,
    int TimeSlotId,
    string Time,
    bool IsArchive)
{
    public static RundownDto From(Rundown rundown) => new(
        rundown.Id,
        rundown.Date,
        rundown.DirectoryId,
        rundown.DirectoryName,
        rundown.ParentDirectoryId,
        rundown.ParentDirectoryName,
        rundown.TimeSlotId,
        rundown.Time,
        IsArchive: false);
}

public sealed record StoryDto(long Id, string Title)
{
    public static StoryDto From(Story story) => new(story.Id, story.Title);
}

public sealed record NewsSceneDto(
    long Id,
    int TemplateId,
    long StoryId,
    int Order,
    string Description,
    string? Parameters,
    DateTime ModifiedAt)
{
    public static NewsSceneDto From(NewsScene scene) => new(
        scene.Id,
        scene.TemplateId,
        scene.StoryId,
        scene.Order,
        scene.Description,
        scene.Parameters,
        scene.ModifiedAt);
}

/// <summary>
/// Bir habere bağlı kayıtlı sahneler ve kullandıkları şablonlar. Sahneler şablonlarına <c>templateId</c> ile bağlanır;
/// her şablon <see cref="Templates"/> içinde bir kez yer alır.
/// </summary>
/// <param name="Source">Sahnelerin kaynağı: <c>newsSavedScenes</c> ya da <c>vizSavedScenes</c>.</param>
public sealed record StoryScenesDto(
    long StoryId,
    string Source,
    IReadOnlyList<NewsSceneDto> Scenes,
    IReadOnlyList<SceneTemplateDto> Templates);
