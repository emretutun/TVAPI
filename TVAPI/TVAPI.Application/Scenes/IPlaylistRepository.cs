using TVAPI.Domain.Scenes;

namespace TVAPI.Application.Scenes;

/// <summary>Playlist ve kayıtlı sahnelere erişim sözleşmesi. Yalnızca etkin (silinmemiş) kayıtlar döner.</summary>
public interface IPlaylistRepository
{
    /// <summary>Verilen kategorilerdeki playlist'ler, ada göre sıralı.</summary>
    Task<IReadOnlyList<Playlist>> GetByCategoriesAsync(IReadOnlyCollection<int> categoryIds, CancellationToken cancellationToken);

    /// <summary>Tek playlist; yoksa ya da silinmişse null.</summary>
    Task<Playlist?> GetByIdAsync(int playlistId, CancellationToken cancellationToken);

    /// <summary>
    /// Playlist'teki kayıtlı sahneler, liste sırasıyla. Şablonu silinmiş sahneler dönmez
    /// (eski sp_GetPlaylistSavedScenes prosedüründeki kural).
    /// </summary>
    Task<IReadOnlyList<SavedScene>> GetScenesAsync(int playlistId, CancellationToken cancellationToken);

    /// <summary>Tek kayıtlı sahne; yoksa, silinmişse ya da şablonu silinmişse null.</summary>
    Task<SavedScene?> GetSceneByIdAsync(long savedSceneId, CancellationToken cancellationToken);
}
