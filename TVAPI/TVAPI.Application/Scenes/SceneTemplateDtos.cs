using TVAPI.Domain.Scenes;

namespace TVAPI.Application.Scenes;

/// <summary>Sahne şablonu cevabı: şablonun kendisi, doldurulabilir alanları ve animasyonları.</summary>
public sealed record SceneTemplateDto(
    int Id,
    string Name,
    string Description,
    int DirectoryId,
    string ScenePath,
    string InDirectorName,
    string OutDirectorName,
    string IntraDirectorName,
    decimal IntraDirectorDelay,
    bool SceneResetEnabled,
    string ConflictingTemplateIds,
    int TargetLayer,
    DateTime CreatedAt,
    DateTime ModifiedAt,
    IReadOnlyList<SceneElementDto> Elements,
    IReadOnlyList<SceneDirectorDto> Directors)
{
    public static SceneTemplateDto From(SceneTemplateDetail detail) => new(
        detail.Template.Id,
        detail.Template.Name,
        detail.Template.Description,
        detail.Template.DirectoryId,
        detail.Template.ScenePath,
        detail.Template.InDirectorName,
        detail.Template.OutDirectorName,
        detail.Template.IntraDirectorName,
        detail.Template.IntraDirectorDelay,
        detail.Template.SceneResetEnabled,
        detail.Template.ConflictingTemplateIds,
        detail.Template.TargetLayer,
        detail.Template.CreatedAt,
        detail.Template.ModifiedAt,
        detail.Elements.Select(SceneElementDto.From).ToList(),
        detail.Directors.Select(SceneDirectorDto.From).ToList());
}

public sealed record SceneElementDto(
    int Id,
    string Name,
    int Type,
    string Title,
    string DefaultValue,
    int MaxSize,
    int Order,
    bool UseDefault)
{
    public static SceneElementDto From(SceneElement element) => new(
        element.Id,
        element.Name,
        element.Type,
        element.Title,
        element.DefaultValue,
        element.MaxSize,
        element.Order,
        element.UseDefault);
}

public sealed record SceneDirectorDto(int Id, string Name, string Title, decimal? Length)
{
    public static SceneDirectorDto From(SceneDirector director) =>
        new(director.Id, director.Name, director.Title, director.Length);
}
