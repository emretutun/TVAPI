using Microsoft.Extensions.Configuration;

namespace TVAPI.Infrastructure.Caching;

/// <summary>
/// Spor verisi önbellek süreleri. Bir veri "taze" süresi boyunca veritabanına gidilmeden döner.
/// Süre dolduktan sonra veritabanına ulaşılamazsa, <see cref="MaxStale"/> süresince son başarılı veri kullanılır.
/// </summary>
internal sealed record BroadageCacheOptions(
    TimeSpan CompetitionFreshFor,
    TimeSpan StandingsFreshFor,
    TimeSpan MatchesFreshFor,
    TimeSpan LineupsFreshFor,
    TimeSpan TeamsFreshFor,
    TimeSpan MaxStale)
{
    private const string Section = "Broadage:Cache:";

    public static BroadageCacheOptions From(IConfiguration configuration) => new(
        CompetitionFreshFor: TimeSpan.FromSeconds(Read(configuration, "CompetitionSeconds", 30)),
        StandingsFreshFor: TimeSpan.FromSeconds(Read(configuration, "StandingsSeconds", 30)),
        // Canlı maçta skor ve dakika sık değişir; bu yüzden maç listesi çok kısa tutulur.
        MatchesFreshFor: TimeSpan.FromSeconds(Read(configuration, "MatchesSeconds", 5)),
        // Maç kadrosu açıklandıktan sonra değişmez; açıklandığı an çabuk görünsün diye yine de kısa tutulur.
        LineupsFreshFor: TimeSpan.FromSeconds(Read(configuration, "LineupsSeconds", 30)),
        // Takım künyesi ve kadro maç sırasında değişmez; uzun tutulur.
        TeamsFreshFor: TimeSpan.FromSeconds(Read(configuration, "TeamsSeconds", 300)),
        MaxStale: TimeSpan.FromMinutes(Read(configuration, "MaxStaleMinutes", 720)));

    private static int Read(IConfiguration configuration, string key, int defaultValue) =>
        int.TryParse(configuration[Section + key], out var value) && value > 0 ? value : defaultValue;
}
