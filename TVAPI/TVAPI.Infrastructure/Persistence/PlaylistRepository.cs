using TVAPI.Application.Scenes;
using TVAPI.Domain.Scenes;

namespace TVAPI.Infrastructure.Persistence;

/// <summary>
/// Playlist'leri ve kayıtlı sahneleri kanal veritabanından okur: tbl_playlist_group, tbl_saved_scenes.
/// status = 1 etkin kayıt demektir.
/// </summary>
internal sealed class PlaylistRepository : IPlaylistRepository
{
    private const string PlaylistColumns = """
            p.group_id         AS Id,
            p.name             AS Name,
            p.plc_id           AS CategoryId,
            p.user_id          AS UserId,
            p.fixed_gui_dir_id AS FixedDirectoryId,
            p.fixed_gui_id     AS FixedTemplateId,
            p.create_date      AS CreatedAt
        """;

    // plc_id index'ini kullanır.
    private const string PlaylistsByCategoriesSql = $"""
        SELECT
        {PlaylistColumns}
        FROM tbl_playlist_group p
        WHERE p.status = 1
          AND p.plc_id IN @CategoryIds
        ORDER BY p.name, p.group_id
        """;

    private const string PlaylistByIdSql = $"""
        SELECT
        {PlaylistColumns}
        FROM tbl_playlist_group p
        WHERE p.status = 1
          AND p.group_id = @PlaylistId
        """;

    private const string SceneColumns = """
            s.saved_scene_id    AS Id,
            s.scene_gui_id      AS TemplateId,
            s.group_id          AS PlaylistId,
            s.sira_no           AS SortOrder,
            s.description       AS Description,
            s.scene_parameters  AS Parameters,
            s.scene_metadata    AS Metadata,
            s.modification_time AS ModifiedAt,
            s.create_date       AS CreatedAt
        """;

    // Canlıdaki sp_GetPlaylistSavedScenes prosedürünün içindeki sorgunun aynısıdır: şablonu silinmiş sahne dönmez,
    // sıra sira_no ve kayıt numarasıdır. Prosedür çağrılmaz; böylece API'nin veritabanında prosedür çalıştırma
    // yetkisine ihtiyacı olmaz, yalnızca okuma yetkisi yeter. (group_id, sira_no) index'ini kullanır.
    private const string ScenesByPlaylistSql = $"""
        SELECT
        {SceneColumns}
        FROM tbl_saved_scenes s
        INNER JOIN tbl_gui g ON g.gui_id = s.scene_gui_id
        WHERE s.status = 1
          AND g.status = 1
          AND s.group_id = @PlaylistId
        ORDER BY s.sira_no, s.saved_scene_id
        """;

    private const string SceneByIdSql = $"""
        SELECT
        {SceneColumns}
        FROM tbl_saved_scenes s
        INNER JOIN tbl_gui g ON g.gui_id = s.scene_gui_id
        WHERE s.status = 1
          AND g.status = 1
          AND s.saved_scene_id = @SavedSceneId
        """;

    private readonly ChannelDatabase _database;

    public PlaylistRepository(ChannelDatabase database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<Playlist>> GetByCategoriesAsync(
        IReadOnlyCollection<int> categoryIds,
        CancellationToken cancellationToken)
    {
        var rows = await _database.QueryAsync<PlaylistRow>(
            PlaylistsByCategoriesSql,
            new { CategoryIds = categoryIds },
            cancellationToken);

        return rows.Select(row => row.ToDomain()).ToList();
    }

    public async Task<Playlist?> GetByIdAsync(int playlistId, CancellationToken cancellationToken)
    {
        var row = await _database.QuerySingleOrDefaultAsync<PlaylistRow>(
            PlaylistByIdSql,
            new { PlaylistId = playlistId },
            cancellationToken);

        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<SavedScene>> GetScenesAsync(int playlistId, CancellationToken cancellationToken)
    {
        var rows = await _database.QueryAsync<SceneRow>(
            ScenesByPlaylistSql,
            new { PlaylistId = playlistId },
            cancellationToken);

        return rows.Select(row => row.ToDomain()).ToList();
    }

    public async Task<SavedScene?> GetSceneByIdAsync(long savedSceneId, CancellationToken cancellationToken)
    {
        var row = await _database.QuerySingleOrDefaultAsync<SceneRow>(
            SceneByIdSql,
            new { SavedSceneId = savedSceneId },
            cancellationToken);

        return row?.ToDomain();
    }

    // Aşağıdaki sınıflar sorgu sonuçlarının bire bir karşılığıdır ve yalnızca burada kullanılır.

    private sealed class PlaylistRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public int UserId { get; set; }
        public int FixedDirectoryId { get; set; }
        public int FixedTemplateId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Kaynak "kısıt yok" bilgisini 0 ile tutar; dışarıya null olarak verilir.
        public Playlist ToDomain() => new(
            Id,
            Name,
            CategoryId,
            UserId,
            FixedDirectoryId > 0 ? FixedDirectoryId : null,
            FixedTemplateId > 0 ? FixedTemplateId : null,
            CreatedAt);
    }

    private sealed class SceneRow
    {
        public long Id { get; set; }
        public int TemplateId { get; set; }
        public int PlaylistId { get; set; }
        public int SortOrder { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Parameters { get; set; }
        public string? Metadata { get; set; }
        public DateTime ModifiedAt { get; set; }
        public DateTime? CreatedAt { get; set; }

        public SavedScene ToDomain() =>
            new(Id, TemplateId, PlaylistId, SortOrder, Description, Parameters, Metadata, ModifiedAt, CreatedAt);
    }
}
