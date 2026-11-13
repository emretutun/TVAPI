namespace TVAPI.Application.Weather;

public interface IWeatherService
{
    /// <summary>Verilen yerleri, isteğin sırasıyla getirir; bulunmayan id'ler atlanır.</summary>
    Task<IReadOnlyList<WeatherCityDto>> GetCitiesAsync(IReadOnlyList<int> cityIds, CancellationToken cancellationToken);

    /// <summary>
    /// Verilen yerlerin tahminlerini, isteğin sırasıyla getirir; bulunmayan id'ler atlanır.
    /// </summary>
    /// <param name="from">İlk gün; verilmezse bugün.</param>
    /// <param name="days">Kaç gün (ilk gün dahil).</param>
    Task<IReadOnlyList<CityForecastDto>> GetForecastsAsync(
        IReadOnlyList<int> cityIds,
        DateOnly? from,
        int days,
        CancellationToken cancellationToken);
}

internal sealed class WeatherService : IWeatherService
{
    private readonly IWeatherRepository _repository;
    private readonly TimeProvider _timeProvider;

    public WeatherService(IWeatherRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<WeatherCityDto>> GetCitiesAsync(
        IReadOnlyList<int> cityIds,
        CancellationToken cancellationToken)
    {
        var requestedIds = cityIds.Distinct().ToList();
        var cities = await _repository.GetCitiesAsync(Ordered(requestedIds), cancellationToken);
        var citiesById = cities.ToDictionary(city => city.Id);

        // Ekrandaki kutuların sırası isteğin sırasıdır (1. kutu = ilk id); cevap o sırayı korur.
        return requestedIds
            .Where(citiesById.ContainsKey)
            .Select(id => WeatherCityDto.From(citiesById[id]))
            .ToList();
    }

    public async Task<IReadOnlyList<CityForecastDto>> GetForecastsAsync(
        IReadOnlyList<int> cityIds,
        DateOnly? from,
        int days,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(days, 1);

        var firstDay = from ?? DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var lastDay = firstDay.AddDays(days - 1);

        var requestedIds = cityIds.Distinct().ToList();
        var orderedIds = Ordered(requestedIds);

        var cities = await _repository.GetCitiesAsync(orderedIds, cancellationToken);
        var forecasts = await _repository.GetForecastsAsync(orderedIds, firstDay, lastDay, cancellationToken);

        var citiesById = cities.ToDictionary(city => city.Id);
        var forecastsByCity = forecasts.ToLookup(forecast => forecast.CityId);

        return requestedIds
            .Where(citiesById.ContainsKey)
            .Select(id => new CityForecastDto(
                WeatherCityDto.From(citiesById[id]),
                forecastsByCity[id].OrderBy(forecast => forecast.Date).Select(DailyForecastDto.From).ToList()))
            .ToList();
    }

    // Veritabanına giden sorgu hep aynı biçimde olsun diye id'ler sıralanır (önbellek anahtarı buna bağlıdır);
    // isteğin kendi sırası cevabı kurarken ayrıca uygulanır.
    private static List<int> Ordered(IEnumerable<int> ids) => ids.Order().ToList();
}
