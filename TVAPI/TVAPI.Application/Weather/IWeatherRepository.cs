using TVAPI.Domain.Weather;

namespace TVAPI.Application.Weather;

public interface IWeatherRepository
{
    /// <summary>Verilen id'lerdeki yerler; bulunmayanlar listede yer almaz. Sıra garanti edilmez.</summary>
    Task<IReadOnlyList<WeatherCity>> GetCitiesAsync(IReadOnlyCollection<int> cityIds, CancellationToken cancellationToken);

    /// <summary>
    /// Verilen yerlerin <paramref name="from"/> ile <paramref name="to"/> arasındaki (ikisi de dahil) tahminleri;
    /// yere ve tarihe göre sıralı.
    /// </summary>
    Task<IReadOnlyList<DailyForecast>> GetForecastsAsync(
        IReadOnlyCollection<int> cityIds,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);
}
