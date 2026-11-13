using TVAPI.Application.Scenes;
using TVAPI.Domain.Scenes;

namespace TVAPI.Infrastructure.Persistence;

/// <summary>
/// Sahne şablonlarını kanal veritabanından okur: tbl_gui, tbl_gui_elements, tbl_gui_director.
/// Üç tabloda da status = 1 etkin kayıt demektir; silinen kayıtlar status = 0 olarak tabloda kalır.
/// </summary>
internal sealed class SceneTemplateRepository : ISceneTemplateRepository
{
    // tinyint(1) unsigned sütunlar (scene_reset_enabled, use_default) sürücüye göre bool ya da sayı gelebilir;
    // CAST ile her zaman sayı olarak okunur, bool'a çevirme kodda yapılır.
    private const string TemplateColumns = """
            g.gui_id                                  AS Id,
            g.gui_name                                AS Name,
            g.gui_description                         AS Description,
            g.dir_id                                  AS DirectoryId,
            g.scene_full_path                         AS ScenePath,
            g.in_director_name                        AS InDirectorName,
            g.out_director_name                       AS OutDirectorName,
            g.intra_director_name                     AS IntraDirectorName,
            g.intra_director_delay                    AS IntraDirectorDelay,
            CAST(g.scene_reset_enabled AS SIGNED)     AS SceneResetEnabled,
            g.conflicting_gui_id                      AS ConflictingTemplateIds,
            g.target_layer                            AS TargetLayer,
            g.creation_time                           AS CreatedAt,
            g.modification_time                       AS ModifiedAt
        """;

    // Klasör süzgeci dir_id index'ini kullanır. Süzgeçlerden hangisinin verildiği parametreyle bildirilir;
    // böylece tek ve sabit bir SQL metni yeter, metin birleştirerek sorgu kurulmaz.
    private const string SearchSql = $"""
        SELECT
        {TemplateColumns}
        FROM tbl_gui g
        WHERE g.status = 1
          AND (@FilterByDirectory = 0 OR g.dir_id IN @DirectoryIds)
          AND (@Name IS NULL OR g.gui_name = @Name)
        ORDER BY g.gui_name, g.gui_id
        """;

    private const string TemplatesByIdsSql = $"""
        SELECT
        {TemplateColumns}
        FROM tbl_gui g
        WHERE g.status = 1
          AND g.gui_id IN @TemplateIds
        ORDER BY g.gui_id
        """;

    private const string TemplateByIdSql = $"""
        SELECT
        {TemplateColumns}
        FROM tbl_gui g
        WHERE g.status = 1
          AND g.gui_id = @TemplateId
        """;

    // Alanlar ve animasyonlar bütün şablonlar için tek sorguda okunur (gui_id index'i).
    // Eski kod her şablon için ayrı sorgu atıyordu; 100 şablonluk klasörde 100 sorgu yerine 1 sorgu.
    // Sıra eski koddaki gibidir: kayıt sırası (birincil anahtar).
    private const string ElementsSql = """
        SELECT
            e.gui_id                          AS TemplateId,
            e.gui_element_id                  AS Id,
            e.element_name                    AS Name,
            e.element_type                    AS Type,
            e.element_gui_title               AS Title,
            e.element_default_value           AS DefaultValue,
            e.max_size                        AS MaxSize,
            e.sira_no                         AS SortOrder,
            CAST(e.use_default AS SIGNED)     AS UseDefault
        FROM tbl_gui_elements e
        WHERE e.status = 1
          AND e.gui_id IN @TemplateIds
        ORDER BY e.gui_id, e.gui_element_id
        """;

    private const string DirectorsSql = """
        SELECT
            d.gui_id          AS TemplateId,
            d.director_id     AS Id,
            d.director_name   AS Name,
            d.director_title  AS Title,
            d.director_length AS Length
        FROM tbl_gui_director d
        WHERE d.status = 1
          AND d.gui_id IN @TemplateIds
        ORDER BY d.gui_id, d.director_id
        """;

    private readonly ChannelDatabase _database;

    public SceneTemplateRepository(ChannelDatabase database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<SceneTemplateDetail>> SearchAsync(
        IReadOnlyCollection<int> directoryIds,
        string? name,
        CancellationToken cancellationToken)
    {
        var templates = await _database.QueryAsync<TemplateRow>(
            SearchSql,
            new { FilterByDirectory = directoryIds.Count > 0 ? 1 : 0, DirectoryIds = directoryIds, Name = name },
            cancellationToken);

        return await WithChildrenAsync(templates, cancellationToken);
    }

    public async Task<IReadOnlyList<SceneTemplateDetail>> GetByIdsAsync(
        IReadOnlyCollection<int> templateIds,
        CancellationToken cancellationToken)
    {
        if (templateIds.Count == 0)
        {
            return [];
        }

        var templates = await _database.QueryAsync<TemplateRow>(
            TemplatesByIdsSql,
            new { TemplateIds = templateIds },
            cancellationToken);

        return await WithChildrenAsync(templates, cancellationToken);
    }

    public async Task<SceneTemplateDetail?> GetByIdAsync(int templateId, CancellationToken cancellationToken)
    {
        var template = await _database.QuerySingleOrDefaultAsync<TemplateRow>(
            TemplateByIdSql,
            new { TemplateId = templateId },
            cancellationToken);

        if (template is null)
        {
            return null;
        }

        var details = await WithChildrenAsync([template], cancellationToken);

        return details[0];
    }

    private async Task<IReadOnlyList<SceneTemplateDetail>> WithChildrenAsync(
        IReadOnlyList<TemplateRow> templates,
        CancellationToken cancellationToken)
    {
        if (templates.Count == 0)
        {
            return [];
        }

        var parameters = new { TemplateIds = templates.Select(template => template.Id).ToList() };
        var elements = await _database.QueryAsync<ElementRow>(ElementsSql, parameters, cancellationToken);
        var directors = await _database.QueryAsync<DirectorRow>(DirectorsSql, parameters, cancellationToken);

        var elementsByTemplate = elements.ToLookup(element => element.TemplateId);
        var directorsByTemplate = directors.ToLookup(director => director.TemplateId);

        return templates
            .Select(template => new SceneTemplateDetail(
                template.ToDomain(),
                elementsByTemplate[template.Id].Select(element => element.ToDomain()).ToList(),
                directorsByTemplate[template.Id].Select(director => director.ToDomain()).ToList()))
            .ToList();
    }

    // Aşağıdaki sınıflar sorgu sonuçlarının bire bir karşılığıdır ve yalnızca burada kullanılır.

    private sealed class TemplateRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DirectoryId { get; set; }
        public string ScenePath { get; set; } = string.Empty;
        public string InDirectorName { get; set; } = string.Empty;
        public string OutDirectorName { get; set; } = string.Empty;
        public string IntraDirectorName { get; set; } = string.Empty;
        public decimal IntraDirectorDelay { get; set; }
        public int SceneResetEnabled { get; set; }
        public string ConflictingTemplateIds { get; set; } = string.Empty;
        public int TargetLayer { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

        public SceneTemplate ToDomain() => new(
            Id,
            Name,
            Description,
            DirectoryId,
            ScenePath,
            InDirectorName,
            OutDirectorName,
            IntraDirectorName,
            IntraDirectorDelay,
            SceneResetEnabled != 0,
            ConflictingTemplateIds,
            TargetLayer,
            CreatedAt,
            ModifiedAt);
    }

    private sealed class ElementRow
    {
        public int TemplateId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
        public int MaxSize { get; set; }
        public int SortOrder { get; set; }
        public int UseDefault { get; set; }

        public SceneElement ToDomain() => new(Id, Name, Type, Title, DefaultValue, MaxSize, SortOrder, UseDefault != 0);
    }

    private sealed class DirectorRow
    {
        public int TemplateId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal? Length { get; set; }

        public SceneDirector ToDomain() => new(Id, Name, Title, Length);
    }
}
