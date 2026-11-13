using TVAPI.Domain.News;

namespace TVAPI.Application.News;

/// <summary>
/// Haber verisine erişim sözleşmesi. Yalnızca güncel tablolardan okur; arşiv şeması kapsam dışındadır.
/// </summary>
public interface INewsRepository
{
    /// <summary>Verilen günün yayın akışları, yayın saatine göre sıralı.</summary>
    Task<IReadOnlyList<Rundown>> GetRundownsAsync(DateOnly date, CancellationToken cancellationToken);

    /// <summary>Bir yayın akışındaki haberler, akış sırasıyla.</summary>
    Task<IReadOnlyList<Story>> GetStoriesAsync(
        DateOnly date,
        int directoryId,
        int timeSlotId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Habere bağlı kayıtlı sahneler, sırasıyla. Önce reji kayıtlarına, orada yoksa Viz kayıtlarına bakılır.
    /// </summary>
    Task<NewsScenes> GetStoryScenesAsync(long storyId, CancellationToken cancellationToken);
}
