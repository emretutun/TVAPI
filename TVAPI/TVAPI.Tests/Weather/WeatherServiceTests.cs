using TVAPI.Application.Weather;
using TVAPI.Domain.Weather;

namespace TVAPI.Tests.Weather;

/// <summary>"Bugün" 14 Kasım 2026 Cumartesi.</summary>
public sealed class WeatherServiceTests
{
    private static readonly DateOnly Today = new(2026, 11, 14);

    private static readonly WeatherCity Istanbul = new(34, "İstanbul", "Istanbul");
    private static readonly WeatherCity Ankara = new(6, "Ankara", "Ankara");
    private static readonly WeatherCondition Cloudy = new(6, "6", "Bulutlu", "3");

    // Ekrandaki kutuların sırası isteğin sırasıdır; cevap id sırasına göre değil, isteğe göre dizilir.
    [Fact]
    public async Task Cities_are_returned_in_the_requested_order()
    {
        var service = NewService(new FakeWeatherRepository { Cities = [Ankara, Istanbul] });

        var cities = await service.GetCitiesAsync([34, 6], CancellationToken.None);

        Assert.Equal([34, 6], cities.Select(city => city.Id));
        Assert.Equal("İstanbul", cities[0].Name);
    }

    [Fact]
    public async Task Unknown_and_repeated_cities_are_skipped()
    {
        var service = NewService(new FakeWeatherRepository { Cities = [Ankara] });

        var cities = await service.GetCitiesAsync([999, 6, 6], CancellationToken.None);

        Assert.Equal(6, Assert.Single(cities).Id);
    }

    [Fact]
    public async Task Forecast_starts_today_when_no_date_is_given()
    {
        var repository = new FakeWeatherRepository { Cities = [Istanbul] };
        var service = NewService(repository);

        await service.GetForecastsAsync([34], from: null, days: 3, CancellationToken.None);

        // Üç gün: bugün ve sonraki iki gün (ikisi de dahil).
        Assert.Equal((Today, Today.AddDays(2)), repository.RequestedRange);
    }

    [Fact]
    public async Task Forecast_starts_at_the_given_date()
    {
        var repository = new FakeWeatherRepository { Cities = [Istanbul] };
        var service = NewService(repository);

        await service.GetForecastsAsync([34], from: new DateOnly(2026, 12, 30), days: 5, CancellationToken.None);

        // Yıl sonunu da doğru geçmeli.
        Assert.Equal((new DateOnly(2026, 12, 30), new DateOnly(2027, 1, 3)), repository.RequestedRange);
    }

    [Fact]
    public async Task Cities_are_sorted_for_reading_but_answered_in_the_requested_order()
    {
        var repository = new FakeWeatherRepository
        {
            Cities = [Ankara, Istanbul],
            Forecasts =
            [
                NewForecast(6, Today, high: 9),
                NewForecast(34, Today.AddDays(1), high: 15),
                NewForecast(34, Today, high: 14),
            ],
        };
        var service = NewService(repository);

        var result = await service.GetForecastsAsync([34, 6], from: null, days: 3, CancellationToken.None);

        Assert.Equal([6, 34], repository.RequestedCityIds); // sorgu hep aynı biçimde gider
        Assert.Equal([34, 6], result.Select(item => item.City.Id));
        Assert.Equal([14, 15], result[0].Days.Select(day => day.High)); // günler tarih sırasıyla
        Assert.Equal(9, Assert.Single(result[1].Days).High);
    }

    [Fact]
    public async Task City_without_forecast_is_returned_with_no_days()
    {
        var service = NewService(new FakeWeatherRepository { Cities = [Istanbul] });

        var result = await service.GetForecastsAsync([34], from: null, days: 3, CancellationToken.None);

        Assert.Empty(Assert.Single(result).Days);
    }

    [Fact]
    public async Task Forecast_fields_are_mapped()
    {
        var service = NewService(new FakeWeatherRepository
        {
            Cities = [Istanbul],
            Forecasts = [new DailyForecast(34, Today, 14, 8, Cloudy, WindDirection: null, WindSpeed: 12, Humidity: null)],
        });

        var day = Assert.Single((await service.GetForecastsAsync([34], null, 3, CancellationToken.None))[0].Days);

        Assert.Equal(Today, day.Date);
        Assert.Equal(6, day.DayOfWeek); // Cumartesi; eski sorgudaki DAYOFWEEK-1 ile aynı sayı
        Assert.Equal(14, day.High);
        Assert.Equal(8, day.Low);
        Assert.Equal("Bulutlu", day.Condition.Name);
        Assert.Equal("3", day.Condition.Video);
        Assert.Null(day.WindDirection);
        Assert.Equal(12, day.WindSpeed);
        Assert.Null(day.Humidity);
    }

    [Fact]
    public async Task Sunday_is_day_zero()
    {
        var sunday = new DateOnly(2026, 11, 15);
        var service = NewService(new FakeWeatherRepository { Cities = [Istanbul], Forecasts = [NewForecast(34, sunday, 10)] });

        var day = Assert.Single((await service.GetForecastsAsync([34], sunday, 1, CancellationToken.None))[0].Days);

        Assert.Equal(0, day.DayOfWeek);
    }

    private static DailyForecast NewForecast(int cityId, DateOnly date, int high) =>
        new(cityId, date, high, Low: high - 6, Cloudy, WindDirection: null, WindSpeed: null, Humidity: null);

    private static WeatherService NewService(FakeWeatherRepository repository) =>
        new(repository, new FixedTimeProvider(new DateTimeOffset(Today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero)));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class FakeWeatherRepository : IWeatherRepository
    {
        public IReadOnlyList<WeatherCity> Cities { get; init; } = [];
        public IReadOnlyList<DailyForecast> Forecasts { get; init; } = [];

        public IReadOnlyCollection<int>? RequestedCityIds { get; private set; }
        public (DateOnly From, DateOnly To)? RequestedRange { get; private set; }

        public Task<IReadOnlyList<WeatherCity>> GetCitiesAsync(
            IReadOnlyCollection<int> cityIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<WeatherCity>>(Cities.Where(city => cityIds.Contains(city.Id)).ToList());

        public Task<IReadOnlyList<DailyForecast>> GetForecastsAsync(
            IReadOnlyCollection<int> cityIds,
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken)
        {
            RequestedCityIds = cityIds;
            RequestedRange = (from, to);
            return Task.FromResult(Forecasts);
        }
    }
}
