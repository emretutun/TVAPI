using System.Globalization;
using System.Text.Json;
using TVAPI.Application.Common;
using TVAPI.Domain.Competitions;
using TVAPI.Domain.Teams;

namespace TVAPI.Infrastructure.Persistence;

/// <summary>
/// Broadage'in JSON sütunlarını okumak için ortak yardımcı ve birden çok sütunda
/// aynı biçimde geçen parçalar (oyuncu, takım, ad/kısa ad çifti).
/// </summary>
internal static class BroadageJson
{
    // Kaynaktaki JSON alan adları camelCase; Web ayarları büyük/küçük harf farkını da tolere eder.
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// JSON'u çözer. Bu sütunlar veritabanında düz metindir ve MySQL içeriği doğrulamaz;
    /// bozuk içerik sessizce yutulmaz, hangi kaydın bozuk olduğunu söyleyen bir hataya çevrilir.
    /// Kaynak "veri yok" anlamında null yazdıysa sonuç null olur.
    /// </summary>
    public static T? Deserialize<T>(string json, string dataName, string sourceDescription)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (JsonException exception)
        {
            throw new InvalidSourceDataException($"{dataName} JSON'u çözülemedi ({sourceDescription}).", exception);
        }
    }

    public static string FirstNonBlank(string? first, string? second) =>
        !string.IsNullOrWhiteSpace(first) ? first : second ?? string.Empty;
}

/// <summary>Kaynakta id, name, shortName üçlüsüyle gelen her şey: ülke, mevki, olay tipi, diziliş.</summary>
internal class BroadageNamedJson
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? ShortName { get; set; }
}

/// <summary>Oyuncunun mevkisi. Maç kadrosunda içinde sahadaki yeri (tactical) de bulunur.</summary>
internal sealed class BroadagePositionJson : BroadageNamedJson
{
    public BroadageNamedJson? Tactical { get; set; }
}

/// <summary>Kadro, maç kadrosu ve maç olaylarında aynı biçimde geçen oyuncu.</summary>
internal sealed class BroadagePlayerJson
{
    public int Id { get; set; }
    public string? KnownName { get; set; }
    public string? KnownNameMedium { get; set; }
    public string? KnownNameShort { get; set; }

    // Forma numarası kaynakta tutarsızdır: maç kadrosunda sayı (31), olaylarda metin ("10"),
    // bazen de boş gelir. Bu yüzden ham haliyle okunup burada yorumlanır.
    public JsonElement ShirtNumber { get; set; }

    public BroadagePositionJson? Position { get; set; }
    public BroadageNamedJson? Country { get; set; }

    public Player ToDomain()
    {
        // Kısa adlar boşsa uzun ada düşülür (eski NTR HTSPOR'daki kural).
        var mediumName = BroadageJson.FirstNonBlank(KnownNameMedium, KnownName);

        return new Player(
            Id,
            BroadageJson.FirstNonBlank(KnownName, mediumName),
            mediumName,
            BroadageJson.FirstNonBlank(KnownNameShort, mediumName),
            ReadShirtNumber(),
            ToPosition(Position),
            Country is null
                ? null
                : new Country(Country.Id, Country.Name ?? string.Empty, Country.ShortName ?? string.Empty),
            ToPosition(Position?.Tactical));
    }

    private static PlayerPosition? ToPosition(BroadageNamedJson? position) =>
        position is null
            ? null
            : new PlayerPosition(position.Id, position.Name ?? string.Empty, position.ShortName ?? string.Empty);

    private int? ReadShirtNumber()
    {
        var number = 0;
        var isNumber = ShirtNumber.ValueKind switch
        {
            JsonValueKind.Number => ShirtNumber.TryGetInt32(out number),
            JsonValueKind.String => int.TryParse(
                ShirtNumber.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out number),
            _ => false,
        };

        return isNumber && number > 0 ? number : null;
    }
}

/// <summary>Maç kadrosu ve istatistiklerde takımın ortak alanları. Ek alanlar türeyen sınıflardadır.</summary>
internal abstract class BroadageTeamJson
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? ShortName { get; set; }
    public string? MediumName { get; set; }

    public TeamSummary ToTeamSummary() => new(
        Id,
        Name ?? string.Empty,
        ShortName ?? string.Empty,
        MediumName ?? string.Empty);
}
