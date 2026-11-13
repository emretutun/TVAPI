namespace TVAPI.Domain.News;

/// <summary>
/// Bir günün bir saatindeki yayın akışı (tbl_yayin_saatleri): hangi haber havuzunun hangi saatte yayınlanacağı.
/// </summary>
/// <param name="DirectoryId">Akışın haber havuzu (klasörü).</param>
/// <param name="ParentDirectoryId">Havuzun üst klasörü.</param>
/// <param name="TimeSlotId">Yayın saatinin tanım numarası (saat_id); haberleri çekerken kullanılır.</param>
/// <param name="Time">Yayın saati, kaynaktaki metin haliyle (örnek: "19:00").</param>
public sealed record Rundown(
    int Id,
    DateOnly Date,
    int DirectoryId,
    string DirectoryName,
    long ParentDirectoryId,
    string ParentDirectoryName,
    int TimeSlotId,
    string Time);

/// <summary>Yayın akışındaki bir haber (tbl_records); yalnızca listelemek için gereken bilgiler.</summary>
public sealed record Story(long Id, string Title);

/// <summary>Bir habere bağlı kayıtlı sahne.</summary>
/// <param name="TemplateId">Sahnenin şablonu (kanal veritabanındaki tbl_gui.gui_id).</param>
/// <param name="Parameters">Alan değerleri, olduğu gibi; kaynağı Viz kayıtları olan sahnelerde yoktur.</param>
public sealed record NewsScene(
    long Id,
    int TemplateId,
    long StoryId,
    int Order,
    string Description,
    string? Parameters,
    DateTime ModifiedAt);

/// <summary>Habere bağlı sahnelerin okunduğu yer.</summary>
public enum NewsSceneSource
{
    /// <summary>tbl_news_saved_scene: reji programının kaydettiği, alan değerleri dolu sahneler.</summary>
    NewsSavedScenes,

    /// <summary>viz_saved_scenes: habere reji kaydı yoksa bakılan, alan değeri taşımayan kayıtlar.</summary>
    VizSavedScenes,
}

public sealed record NewsScenes(NewsSceneSource Source, IReadOnlyList<NewsScene> Scenes);
