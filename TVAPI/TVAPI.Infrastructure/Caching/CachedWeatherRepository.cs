using TVAPI.Application.Weather;
using TVAPI.Domain.Weather;
using TVAPI.Infrastructure.Persistence;

namespace TVAPI.Infrastructure.Caching;

/// <summary><see cref="WeatherRepository"/> üzerine önbellek ekler (decorator).</summary>
internal sealed class CachedWeatherRepository : IWeatherRepository
{
    private readonly WeatherRepository _inner;
    private readonly ResilientCache _cache;
    private readonly ChannelCacheOptions _options;

    public CachedWeatherRepository(WeatherRepository inner, ResilientCache cache, ChannelCacheOptions options)
    {
        _inner = inner;
        _cache = cache;
        _options = options;
    }

    public Task<IReadOnlyList<WeatherCity>> GetCitiesAsync(
        IReadOnlyCollection<int> cityIds,
        CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"weather:cities:{string.Join(',', cityIds)}",
            _options.WeatherFreshFor,
            token => _inner.GetCitiesAsync(cityIds, token),
            cancellationToken);

    public Task<IReadOnlyList<DailyForecast>> GetForecastsAsync(
        IReadOnlyCollection<int> cityIds,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        _cache.GetOrLoadAsync(
            $"weather:forecasts:{string.Join(',', cityIds)}:{from:yyyyMMdd}:{to:yyyyMMdd}",
            _options.WeatherFreshFor,
            token => _inner.GetForecastsAsync(cityIds, from, to, token),
            cancellationToken);
}
