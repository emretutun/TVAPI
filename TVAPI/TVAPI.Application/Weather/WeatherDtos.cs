using TVAPI.Domain.Weather;

namespace TVAPI.Application.Weather;

public sealed record WeatherCityDto(int Id, string Name, string NameEn)
{
    public static WeatherCityDto From(WeatherCity city) => new(city.Id, city.Name, city.NameEn);
}

public sealed record WeatherConditionDto(int Id, string Code, string Name, string Video);

/// <param name="DayOfWeek">Haftanın günü: 0 Pazar, 1 Pazartesi ... 6 Cumartesi (eski sorgudaki "gun" ile aynı).</param>
public sealed record DailyForecastDto(
    DateOnly Date,
    int DayOfWeek,
    int High,
    int Low,
    WeatherConditionDto Condition,
    string? WindDirection,
    int? WindSpeed,
    int? Humidity)
{
    public static DailyForecastDto From(DailyForecast forecast) => new(
        forecast.Date,
        (int)forecast.Date.DayOfWeek,
        forecast.High,
        forecast.Low,
        new WeatherConditionDto(
            forecast.Condition.Id,
            forecast.Condition.Code,
            forecast.Condition.Name,
            forecast.Condition.Video),
        forecast.WindDirection,
        forecast.WindSpeed,
        forecast.Humidity);
}

/// <summary>Bir yer ve o yerin istenen günlere ait tahminleri, tarih sırasıyla. Tahmini olmayan gün listede yer almaz.</summary>
public sealed record CityForecastDto(WeatherCityDto City, IReadOnlyList<DailyForecastDto> Days);
