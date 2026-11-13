using Microsoft.Extensions.Configuration;

namespace TVAPI.Infrastructure.Caching;

/// <summary>Kanal veritabanından okunan verilerin önbellek süreleri.</summary>
internal sealed record ChannelCacheOptions(
    TimeSpan SceneTemplatesFreshFor,
    TimeSpan PlaylistsFreshFor,
    TimeSpan SavedScenesFreshFor,
    TimeSpan WeatherFreshFor)
{
    private const string Section = "Channel:Cache:";

    public static ChannelCacheOptions From(IConfiguration configuration) => new(
        // Sahne şablonları grafik ekibi yeni bir sahne tanımladığında değişir; dakikada bir yenilemek yeterlidir.
        SceneTemplatesFreshFor: TimeSpan.FromSeconds(Read(configuration, "SceneTemplatesSeconds", 60)),
        // Playlist'lerin kendisi (adı, kategorisi) seyrek değişir.
        PlaylistsFreshFor: TimeSpan.FromSeconds(Read(configuration, "PlaylistsSeconds", 30)),
        // Kayıtlı sahneler yayın sırasında eklenip değiştirilir; operatör kaydettiğini çabuk görmelidir.
        SavedScenesFreshFor: TimeSpan.FromSeconds(Read(configuration, "SavedScenesSeconds", 5)),
        // Hava tahmini günde birkaç kez güncellenir.
        WeatherFreshFor: TimeSpan.FromSeconds(Read(configuration, "WeatherSeconds", 300)));

    private static int Read(IConfiguration configuration, string key, int defaultValue) =>
        int.TryParse(configuration[Section + key], out var value) && value > 0 ? value : defaultValue;
}
