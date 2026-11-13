using TVAPI.Domain.Teams;

namespace TVAPI.Infrastructure.Persistence;

/// <summary>
/// soccer_players.team_data sütunundaki kadro JSON'unu Domain modeline çevirir.
/// Veritabanına bağlı değildir; bu sayede veritabanı olmadan sınanabilir.
/// </summary>
internal static class SquadJsonParser
{
    public static Squad Parse(string json, string sourceDescription)
    {
        var entries = BroadageJson.Deserialize<List<SquadEntryJson?>>(json, "Kadro", sourceDescription);

        var players = (entries ?? [])
            .Select(entry => entry?.Player)
            .OfType<BroadagePlayerJson>()
            .Select(player => player.ToDomain())
            .ToList();

        return new Squad(players);
    }

    // Kaynakta her oyuncunun yanında oynadığı turnuvalar da var; kullanılmadığı için okunmuyor.
    private sealed class SquadEntryJson
    {
        public BroadagePlayerJson? Player { get; set; }
    }
}
