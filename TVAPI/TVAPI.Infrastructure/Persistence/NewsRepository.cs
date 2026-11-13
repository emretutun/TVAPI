using TVAPI.Application.News;
using TVAPI.Domain.News;

namespace TVAPI.Infrastructure.Persistence;

/// <summary>
/// Haber verisini haber veritabanından okur. Sorgular, canlıdaki üç prosedürün (sp_GetRundownsAtDate,
/// sp_GetRundownStories, sp_GetNewsSavedScenes) "güncel veri" kollarının aynısıdır. Prosedürler çağrılmaz;
/// böylece API'nin prosedür çalıştırma yetkisine ihtiyacı olmaz, yalnızca okuma yetkisi yeter.
/// Prosedürlerin arşiv kolları (haberturktv_archive şeması) kapsam dışındadır.
/// </summary>
internal sealed class NewsRepository : INewsRepository
{
    // id sütunları kaynakta işaretsiz (unsigned) sayıdır; CAST ile işaretli okunur, böylece .NET tarafında
    // long/int olarak sorunsuz eşlenir.

    // sp_GetRundownsAtDate. (yayin_tarihi, saat_id) benzersiz index'ini kullanır.
    private const string RundownsSql = """
        SELECT
            ys.id                    AS Id,
            ys.yayin_tarihi          AS Date,
            ys.dir_id                AS DirectoryId,
            d2.name                  AS DirectoryName,
            CAST(d1.id AS SIGNED)    AS ParentDirectoryId,
            d1.name                  AS ParentDirectoryName,
            ys.saat_id               AS TimeSlotId,
            dys.saat                 AS Time
        FROM tbl_yayin_saatleri ys
        INNER JOIN def_yayin_saatleri dys ON ys.saat_id = dys.id
        INNER JOIN tbl_dir_structure d2 ON ys.dir_id = d2.id
        INNER JOIN tbl_dir_structure d1 ON d2.pid = d1.id
        WHERE ys.status = 1
          AND ys.yayin_tarihi = @Date
        ORDER BY dys.saat
        """;

    // sp_GetRundownStories. (status, yayin_tarihi, dir_id, saat) index'ini kullanır.
    // "yalanci_haber" akışta yer tutan ama gerçek haber olmayan kayıtlardır; listeye girmez.
    private const string StoriesSql = """
        SELECT
            CAST(r.auto_id AS SIGNED) AS Id,
            r.baslik                  AS Title
        FROM tbl_records r
        WHERE r.akis_haber_id IS NOT NULL
          AND r.yayin_tarihi = @Date
          AND r.dir_id = @DirectoryId
          AND r.saat = @TimeSlotId
          AND r.status = 1
          AND r.yalanci_haber = 0
        ORDER BY r.sira, r.auto_id
        """;

    // sp_GetNewsSavedScenes: önce reji kayıtları... (scene_haber_id, sira_no) index'ini kullanır.
    private const string NewsSavedScenesSql = """
        SELECT
            s.saved_scene_id                 AS Id,
            s.scene_gui_id                   AS TemplateId,
            CAST(s.scene_haber_id AS SIGNED) AS StoryId,
            s.sira_no                        AS SortOrder,
            s.description                    AS Description,
            s.scene_parameters               AS Parameters,
            s.modification_time              AS ModifiedAt
        FROM tbl_news_saved_scene s
        WHERE s.status = 1
          AND s.scene_haber_id = @StoryId
        ORDER BY s.sira_no, s.saved_scene_id
        """;

    // ...habere ait reji kaydı yoksa Viz kayıtları. Bu tabloda alan değerleri (scene_parameters) yoktur.
    private const string VizSavedScenesSql = """
        SELECT
            s.saved_scene_id                 AS Id,
            s.scene_gui_id                   AS TemplateId,
            CAST(s.scene_haber_id AS SIGNED) AS StoryId,
            s.sira_no                        AS SortOrder,
            s.description                    AS Description,
            NULL                             AS Parameters,
            s.modification_time              AS ModifiedAt
        FROM viz_saved_scenes s
        WHERE s.status = 1
          AND s.scene_haber_id = @StoryId
        ORDER BY s.sira_no, s.saved_scene_id
        """;

    private readonly NewsDatabase _database;

    public NewsRepository(NewsDatabase database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<Rundown>> GetRundownsAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var rows = await _database.QueryAsync<RundownRow>(
            RundownsSql,
            new { Date = date.ToDateTime(TimeOnly.MinValue) },
            cancellationToken);

        return rows.Select(row => row.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<Story>> GetStoriesAsync(
        DateOnly date,
        int directoryId,
        int timeSlotId,
        CancellationToken cancellationToken)
    {
        var rows = await _database.QueryAsync<StoryRow>(
            StoriesSql,
            new { Date = date.ToDateTime(TimeOnly.MinValue), DirectoryId = directoryId, TimeSlotId = timeSlotId },
            cancellationToken);

        return rows.Select(row => new Story(row.Id, row.Title)).ToList();
    }

    public async Task<NewsScenes> GetStoryScenesAsync(long storyId, CancellationToken cancellationToken)
    {
        var parameters = new { StoryId = storyId };

        var newsScenes = await _database.QueryAsync<SceneRow>(NewsSavedScenesSql, parameters, cancellationToken);
        if (newsScenes.Count > 0)
        {
            return new NewsScenes(NewsSceneSource.NewsSavedScenes, ToDomain(newsScenes));
        }

        var vizScenes = await _database.QueryAsync<SceneRow>(VizSavedScenesSql, parameters, cancellationToken);

        return new NewsScenes(NewsSceneSource.VizSavedScenes, ToDomain(vizScenes));
    }

    private static IReadOnlyList<NewsScene> ToDomain(IReadOnlyList<SceneRow> rows) =>
        rows.Select(row => row.ToDomain()).ToList();

    // Aşağıdaki sınıflar sorgu sonuçlarının bire bir karşılığıdır ve yalnızca burada kullanılır.

    private sealed class RundownRow
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int DirectoryId { get; set; }
        public string DirectoryName { get; set; } = string.Empty;
        public long ParentDirectoryId { get; set; }
        public string ParentDirectoryName { get; set; } = string.Empty;
        public int TimeSlotId { get; set; }
        public string Time { get; set; } = string.Empty;

        public Rundown ToDomain() => new(
            Id,
            DateOnly.FromDateTime(Date),
            DirectoryId,
            DirectoryName,
            ParentDirectoryId,
            ParentDirectoryName,
            TimeSlotId,
            Time);
    }

    private sealed class StoryRow
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    private sealed class SceneRow
    {
        public long Id { get; set; }
        public int TemplateId { get; set; }
        public long StoryId { get; set; }
        public int SortOrder { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Parameters { get; set; }
        public DateTime ModifiedAt { get; set; }

        public NewsScene ToDomain() => new(Id, TemplateId, StoryId, SortOrder, Description, Parameters, ModifiedAt);
    }
}
