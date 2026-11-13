using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TVAPI.Application.Common;

namespace TVAPI.Infrastructure.Caching;

/// <summary>
/// "En son başarılı veri" davranışını sağlayan önbellek.
/// 1) Veri tazeyse kaynağa gitmeden döner.
/// 2) Taze değilse kaynaktan yeniden okur ve saklar.
/// 3) Kaynak cevap vermezse ya da bozuk veri verirse, elde daha önce alınmış veri varsa onu döner
///    ve bu cevabın eski veriyle üretildiğini <see cref="IDataFreshness"/> üzerinden bildirir.
/// Veri bellekte tutulur; uygulama yeniden başlarsa önbellek boşalır.
/// </summary>
internal sealed class ResilientCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly TimeProvider _timeProvider;
    private readonly IDataFreshness _dataFreshness;
    private readonly BroadageCacheOptions _options;
    private readonly ILogger<ResilientCache> _logger;

    public ResilientCache(
        IMemoryCache memoryCache,
        TimeProvider timeProvider,
        IDataFreshness dataFreshness,
        BroadageCacheOptions options,
        ILogger<ResilientCache> logger)
    {
        _memoryCache = memoryCache;
        _timeProvider = timeProvider;
        _dataFreshness = dataFreshness;
        _options = options;
        _logger = logger;
    }

    public async Task<T> GetOrLoadAsync<T>(
        string key,
        TimeSpan freshFor,
        Func<CancellationToken, Task<T>> load,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var cached = _memoryCache.Get<Entry<T>>(key);

        if (cached is not null && now - cached.FetchedAt < freshFor)
        {
            _dataFreshness.Record(cached.FetchedAt, isStale: false);
            return cached.Value;
        }

        try
        {
            var value = await load(cancellationToken);

            _memoryCache.Set(key, new Entry<T>(value, now), _options.MaxStale);
            _dataFreshness.Record(now, isStale: false);

            return value;
        }
        catch (Exception exception) when (cached is not null && IsSourceFailure(exception))
        {
            _logger.LogWarning(
                exception,
                "Veri kaynağı hatası; {Key} için {FetchedAt} tarihli son başarılı veri kullanılıyor.",
                key,
                cached.FetchedAt);

            _dataFreshness.Record(cached.FetchedAt, isStale: true);

            return cached.Value;
        }
    }

    private static bool IsSourceFailure(Exception exception) =>
        exception is DataSourceUnavailableException or InvalidSourceDataException;

    private sealed record Entry<T>(T Value, DateTimeOffset FetchedAt);
}
