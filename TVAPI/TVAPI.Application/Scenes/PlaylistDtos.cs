using TVAPI.Domain.Scenes;

namespace TVAPI.Application.Scenes;

public sealed record PlaylistDto(
    int Id,
    string Name,
    int CategoryId,
    int UserId,
    int? FixedDirectoryId,
    int? FixedTemplateId,
    DateTime CreatedAt)
{
    public static PlaylistDto From(Playlist playlist) => new(
        playlist.Id,
        playlist.Name,
        playlist.CategoryId,
        playlist.UserId,
        playlist.FixedDirectoryId,
        playlist.FixedTemplateId,
        playlist.CreatedAt);
}

public sealed record SavedSceneDto(
    long Id,
    int TemplateId,
    int PlaylistId,
    int Order,
    string Description,
    string? Parameters,
    string? Metadata,
    DateTime ModifiedAt,
    DateTime? CreatedAt)
{
    public static SavedSceneDto From(SavedScene scene) => new(
        scene.Id,
        scene.TemplateId,
        scene.PlaylistId,
        scene.Order,
        scene.Description,
        scene.Parameters,
        scene.Metadata,
        scene.ModifiedAt,
        scene.CreatedAt);
}

/// <summary>
/// Bir playlist'in içeriği. Sahneler şablonlarına <c>templateId</c> ile bağlanır; şablonların kendisi
/// <see cref="Templates"/> listesinde bir kez yer alır. Aynı şablondan yüz sahne olsa da şablon bir kez gönderilir.
/// </summary>
public sealed record PlaylistContentDto(
    PlaylistDto Playlist,
    IReadOnlyList<SavedSceneDto> Scenes,
    IReadOnlyList<SceneTemplateDto> Templates);

/// <summary>Tek bir kayıtlı sahne, şablonuyla birlikte.</summary>
public sealed record SavedSceneDetailDto(SavedSceneDto Scene, SceneTemplateDto Template);
