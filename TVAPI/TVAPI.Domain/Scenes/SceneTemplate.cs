namespace TVAPI.Domain.Scenes;

/// <summary>
/// Bir sahne şablonu: reji programının bir Viz sahnesini nasıl yükleyip oynatacağının tanımı (tbl_gui).
/// </summary>
/// <param name="DirectoryId">Şablonun bulunduğu klasör; reji programları şablonları klasöre göre listeler.</param>
/// <param name="ScenePath">Viz Engine'deki sahnenin tam yolu.</param>
/// <param name="InDirectorName">Giriş animasyonunun director adı.</param>
/// <param name="OutDirectorName">Çıkış animasyonunun director adı.</param>
/// <param name="IntraDirectorName">Ara animasyonun director adı.</param>
/// <param name="IntraDirectorDelay">Ara animasyondan önceki bekleme, saniye.</param>
/// <param name="ConflictingTemplateIds">Bu şablonla aynı anda yayında olamayan şablonlar; kaynaktaki metin haliyle.</param>
public sealed record SceneTemplate(
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
    DateTime ModifiedAt);

/// <summary>Şablonun doldurulabilir bir alanı (tbl_gui_elements).</summary>
/// <param name="Name">Viz sahnesindeki nesnenin adı.</param>
/// <param name="Type">Alanın türü; kaynaktaki sayı haliyle (1: metin alanı).</param>
/// <param name="Title">Reji programında operatöre gösterilen başlık.</param>
/// <param name="MaxSize">En fazla karakter sayısı; sınır yoksa 0.</param>
/// <param name="Order">Kaynaktaki sıra numarası (sira_no).</param>
/// <param name="UseDefault">true ise alan her zaman varsayılan değeri kullanır, operatör değiştiremez.</param>
public sealed record SceneElement(
    int Id,
    string Name,
    int Type,
    string Title,
    string DefaultValue,
    int MaxSize,
    int Order,
    bool UseDefault);

/// <summary>Şablonun bir animasyonu (tbl_gui_director).</summary>
/// <param name="Length">Animasyonun süresi, saniye; bilinmiyorsa null.</param>
public sealed record SceneDirector(int Id, string Name, string Title, decimal? Length);

/// <summary>Bir şablon, alanları ve animasyonlarıyla birlikte.</summary>
public sealed record SceneTemplateDetail(
    SceneTemplate Template,
    IReadOnlyList<SceneElement> Elements,
    IReadOnlyList<SceneDirector> Directors);
