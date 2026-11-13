using Microsoft.Extensions.Configuration;

namespace TVAPI.Infrastructure.Caching;

/// <summary>Haber veritabanından okunan verilerin önbellek süreleri.</summary>
internal sealed record NewsCacheOptions(
    TimeSpan RundownsFreshFor,
    TimeSpan StoriesFreshFor,
    TimeSpan StoryScenesFreshFor)
{
    private const string Section = "News:Cache:";

    public static NewsCacheOptions From(IConfiguration configuration) => new(
        // Günün yayın akışları gün içinde seyrek değişir.
        RundownsFreshFor: TimeSpan.FromSeconds(Read(configuration, "RundownsSeconds", 30)),
        // Akıştaki haberler yayına kadar eklenip sıralanır.
        StoriesFreshFor: TimeSpan.FromSeconds(Read(configuration, "StoriesSeconds", 10)),
        // Habere bağlı sahneler yayın sırasında kaydedilir; operatör kaydettiğini çabuk görmelidir.
        StoryScenesFreshFor: TimeSpan.FromSeconds(Read(configuration, "StoryScenesSeconds", 5)));

    private static int Read(IConfiguration configuration, string key, int defaultValue) =>
        int.TryParse(configuration[Section + key], out var value) && value > 0 ? value : defaultValue;
}
