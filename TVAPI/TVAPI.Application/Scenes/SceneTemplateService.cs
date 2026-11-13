namespace TVAPI.Application.Scenes;

public interface ISceneTemplateService
{
    /// <summary>
    /// Klasöre ve/veya ada göre şablonları getirir. En az bir süzgeç verilmelidir;
    /// ikisi de boşsa <see cref="ArgumentException"/> fırlatır.
    /// </summary>
    Task<IReadOnlyList<SceneTemplateDto>> SearchAsync(
        IReadOnlyCollection<int> directoryIds,
        string? name,
        CancellationToken cancellationToken);

    /// <summary>Tek şablon; yoksa null.</summary>
    Task<SceneTemplateDto?> GetByIdAsync(int templateId, CancellationToken cancellationToken);
}

internal sealed class SceneTemplateService : ISceneTemplateService
{
    private readonly ISceneTemplateRepository _repository;

    public SceneTemplateService(ISceneTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<SceneTemplateDto>> SearchAsync(
        IReadOnlyCollection<int> directoryIds,
        string? name,
        CancellationToken cancellationToken)
    {
        // Aynı sorgu hep aynı biçimde gitsin diye süzgeçler düzenlenir (önbellek anahtarı buna bağlıdır).
        var orderedDirectoryIds = directoryIds.Distinct().Order().ToList();
        var trimmedName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();

        // Süzgeçsiz istek bütün şablonları alanlarıyla birlikte döndürürdü (binlerce kayıt); buna izin verilmez.
        if (orderedDirectoryIds.Count == 0 && trimmedName is null)
        {
            throw new ArgumentException("En az bir süzgeç verilmelidir: klasör ya da ad.");
        }

        var templates = await _repository.SearchAsync(orderedDirectoryIds, trimmedName, cancellationToken);

        return templates.Select(SceneTemplateDto.From).ToList();
    }

    public async Task<SceneTemplateDto?> GetByIdAsync(int templateId, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(templateId, cancellationToken);

        return template is null ? null : SceneTemplateDto.From(template);
    }
}
