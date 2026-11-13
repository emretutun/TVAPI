using TVAPI.Domain.Scenes;

namespace TVAPI.Application.Scenes;

/// <summary>Sahne şablonlarına erişim sözleşmesi. Yalnızca etkin (silinmemiş) kayıtlar döner.</summary>
public interface ISceneTemplateRepository
{
    /// <summary>
    /// Süzgeçlere uyan şablonlar, ada göre sıralı; her biri alanları ve animasyonlarıyla.
    /// </summary>
    /// <param name="directoryIds">Boş değilse yalnızca bu klasörlerdeki şablonlar.</param>
    /// <param name="name">Verildiyse yalnızca adı tam olarak bu olan şablonlar.</param>
    Task<IReadOnlyList<SceneTemplateDetail>> SearchAsync(
        IReadOnlyCollection<int> directoryIds,
        string? name,
        CancellationToken cancellationToken);

    /// <summary>Verilen id'lerdeki şablonlar; silinmiş ya da bulunmayanlar listede yer almaz.</summary>
    Task<IReadOnlyList<SceneTemplateDetail>> GetByIdsAsync(IReadOnlyCollection<int> templateIds, CancellationToken cancellationToken);

    /// <summary>Tek şablon; yoksa ya da silinmişse null.</summary>
    Task<SceneTemplateDetail?> GetByIdAsync(int templateId, CancellationToken cancellationToken);
}
