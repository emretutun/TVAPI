namespace TVAPI.Domain.Scenes;

/// <summary>Kayıtlı sahnelerin toplandığı liste (tbl_playlist_group).</summary>
/// <param name="CategoryId">Playlist kategorisi (plc_id); reji programları listeleri kategoriye göre çeker.</param>
/// <param name="FixedDirectoryId">Dolu ise listeye yalnızca bu klasördeki şablonlardan sahne eklenebilir; kısıt yoksa null.</param>
/// <param name="FixedTemplateId">Dolu ise listeye yalnızca bu şablondan sahne eklenebilir; kısıt yoksa null.</param>
public sealed record Playlist(
    int Id,
    string Name,
    int CategoryId,
    int UserId,
    int? FixedDirectoryId,
    int? FixedTemplateId,
    DateTime CreatedAt);

/// <summary>
/// Operatörün doldurup kaydettiği sahne (tbl_saved_scenes): bir şablon ve o şablonun alanlarına yazılan değerler.
/// </summary>
/// <param name="TemplateId">Sahnenin şablonu (tbl_gui.gui_id).</param>
/// <param name="Order">Liste içindeki sıra (sira_no).</param>
/// <param name="Parameters">Alan değerleri; reji programının yazdığı biçimde, olduğu gibi.</param>
/// <param name="Metadata">Programa özel ek bilgi; olduğu gibi.</param>
public sealed record SavedScene(
    long Id,
    int TemplateId,
    int PlaylistId,
    int Order,
    string Description,
    string? Parameters,
    string? Metadata,
    DateTime ModifiedAt,
    DateTime? CreatedAt);
