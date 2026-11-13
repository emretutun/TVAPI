namespace TVAPI.Application.Scenes;

public interface IPlaylistService
{
    /// <summary>Verilen kategorilerdeki playlist'leri getirir. Kategori verilmezse <see cref="ArgumentException"/> fırlatır.</summary>
    Task<IReadOnlyList<PlaylistDto>> GetByCategoriesAsync(IReadOnlyCollection<int> categoryIds, CancellationToken cancellationToken);

    /// <summary>Playlist'in sahnelerini ve bu sahnelerin kullandığı şablonları getirir; playlist yoksa null.</summary>
    Task<PlaylistContentDto?> GetContentAsync(int playlistId, CancellationToken cancellationToken);

    /// <summary>Tek kayıtlı sahneyi şablonuyla getirir; yoksa null.</summary>
    Task<SavedSceneDetailDto?> GetSceneAsync(long savedSceneId, CancellationToken cancellationToken);
}

internal sealed class PlaylistService : IPlaylistService
{
    private readonly IPlaylistRepository _playlistRepository;
    private readonly ISceneTemplateRepository _templateRepository;

    public PlaylistService(IPlaylistRepository playlistRepository, ISceneTemplateRepository templateRepository)
    {
        _playlistRepository = playlistRepository;
        _templateRepository = templateRepository;
    }

    public async Task<IReadOnlyList<PlaylistDto>> GetByCategoriesAsync(
        IReadOnlyCollection<int> categoryIds,
        CancellationToken cancellationToken)
    {
        // Aynı sorgu hep aynı biçimde gitsin diye kategoriler düzenlenir (önbellek anahtarı buna bağlıdır).
        var orderedCategoryIds = categoryIds.Distinct().Order().ToList();
        if (orderedCategoryIds.Count == 0)
        {
            throw new ArgumentException("En az bir kategori verilmelidir.");
        }

        var playlists = await _playlistRepository.GetByCategoriesAsync(orderedCategoryIds, cancellationToken);

        return playlists.Select(PlaylistDto.From).ToList();
    }

    public async Task<PlaylistContentDto?> GetContentAsync(int playlistId, CancellationToken cancellationToken)
    {
        // "Böyle bir playlist yok" (404) ile "playlist var ama boş" (boş liste) ayrı durumlardır.
        var playlist = await _playlistRepository.GetByIdAsync(playlistId, cancellationToken);
        if (playlist is null)
        {
            return null;
        }

        var scenes = await _playlistRepository.GetScenesAsync(playlistId, cancellationToken);

        // Sahnelerin kullandığı şablonlar tek seferde okunur. Eski kod her sahne için üç ayrı sorgu atıyordu.
        var templateIds = scenes.Select(scene => scene.TemplateId).Distinct().Order().ToList();
        var templates = templateIds.Count == 0
            ? []
            : await _templateRepository.GetByIdsAsync(templateIds, cancellationToken);

        return new PlaylistContentDto(
            PlaylistDto.From(playlist),
            scenes.Select(SavedSceneDto.From).ToList(),
            templates.Select(SceneTemplateDto.From).ToList());
    }

    public async Task<SavedSceneDetailDto?> GetSceneAsync(long savedSceneId, CancellationToken cancellationToken)
    {
        var scene = await _playlistRepository.GetSceneByIdAsync(savedSceneId, cancellationToken);
        if (scene is null)
        {
            return null;
        }

        var template = await _templateRepository.GetByIdAsync(scene.TemplateId, cancellationToken);

        // Sahne ile şablonu iki ayrı okumadır; arada şablon silinmiş olabilir. Şablonsuz sahne oynatılamaz.
        return template is null
            ? null
            : new SavedSceneDetailDto(SavedSceneDto.From(scene), SceneTemplateDto.From(template));
    }
}
