using System.Text.Json;
using TVAPI.Application.Common;
using TVAPI.Domain.Competitions;

namespace TVAPI.Infrastructure.Persistence;

/// <summary>
/// Kaynaktaki puan durumu JSON'unu Domain modeline çevirir.
/// Veritabanına bağlı değildir; bu sayede veritabanı olmadan sınanabilir.
/// </summary>
internal static class StandingsJsonParser
{
    // Kaynaktaki JSON alan adları camelCase; Web ayarları büyük/küçük harf farkını da tolere eder.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Puan durumu veritabanında düz metin (longtext) olarak saklanır; MySQL içeriği doğrulamaz.
    // Bozuk içerik gelirse sessizce boş tablo dönmek yerine, hangi kaydın bozuk olduğunu söyleyen bir hata fırlatırız.
    public static Standings Parse(string json, string sourceDescription)
    {
        StandingsJson? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<StandingsJson>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidSourceDataException($"Puan durumu JSON'u çözülemedi ({sourceDescription}).", exception);
        }

        return new Standings(
            ToRows(parsed?.Overall),
            ToRows(parsed?.Home),
            ToRows(parsed?.Away));
    }

    private static IReadOnlyList<StandingRow> ToRows(List<StandingRowJson>? rows) =>
        rows is null ? [] : rows.Select(row => row.ToDomain()).ToList();

    // Aşağıdaki sınıflar kaynaktaki JSON'un bire bir karşılığıdır ve yalnızca burada kullanılır.
    private sealed class StandingsJson
    {
        public List<StandingRowJson>? Overall { get; set; }
        public List<StandingRowJson>? Home { get; set; }
        public List<StandingRowJson>? Away { get; set; }
    }

    private sealed class StandingRowJson
    {
        public int Position { get; set; }
        public TeamJson? Team { get; set; }
        public int Played { get; set; }
        public int Won { get; set; }
        public int Draw { get; set; }
        public int Lost { get; set; }
        public int Scored { get; set; }
        public int Against { get; set; }
        public int Average { get; set; }
        public int Points { get; set; }
        public int Delta { get; set; }

        public StandingRow ToDomain() => new(
            Position,
            new TeamSummary(
                Team?.Id ?? 0,
                Team?.Name ?? string.Empty,
                Team?.ShortName ?? string.Empty,
                Team?.MediumName ?? string.Empty),
            Played,
            Won,
            Draw,
            Lost,
            GoalsFor: Scored,
            GoalsAgainst: Against,
            GoalDifference: Average,
            Points,
            PositionChange: Delta);
    }

    private sealed class TeamJson
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ShortName { get; set; }
        public string? MediumName { get; set; }
    }
}
