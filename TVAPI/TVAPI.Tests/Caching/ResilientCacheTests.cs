using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using TVAPI.Application.Common;
using TVAPI.Infrastructure.Caching;

namespace TVAPI.Tests.Caching;

/// <summary>
/// Önbelleğin üç davranışı: taze veriyi kaynağa gitmeden vermek, süresi dolunca yenilemek,
/// kaynak çökünce son başarılı veriyi "eski" işaretiyle vermek.
/// Veritabanı yoktur; "kaynak" testin verdiği bir fonksiyondur, saat de testin elindedir.
/// </summary>
public sealed class ResilientCacheTests : IDisposable
{
    private const string Key = "matches:1:62999:6";
    private static readonly TimeSpan FreshFor = TimeSpan.FromSeconds(5);
    private static readonly DateTimeOffset Start = new(2026, 11, 13, 20, 0, 0, TimeSpan.Zero);

    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly ManualTimeProvider _clock = new(Start);

    public void Dispose() => _memoryCache.Dispose();

    [Fact]
    public async Task First_request_loads_from_source_and_reports_fresh_data()
    {
        var request = NewRequest();

        var value = await request.Cache.GetOrLoadAsync(Key, FreshFor, _ => Task.FromResult("1-0"), CancellationToken.None);

        Assert.Equal("1-0", value);
        Assert.Equal(Start, request.Freshness.OldestFetchedAt);
        Assert.False(request.Freshness.IsStale);
    }

    [Fact]
    public async Task Request_within_fresh_period_does_not_go_to_source()
    {
        await NewRequest().Cache.GetOrLoadAsync(Key, FreshFor, _ => Task.FromResult("1-0"), CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(4));

        var request = NewRequest();
        var sourceCalls = 0;
        var value = await request.Cache.GetOrLoadAsync(
            Key,
            FreshFor,
            _ =>
            {
                sourceCalls++;
                return Task.FromResult("2-0");
            },
            CancellationToken.None);

        Assert.Equal("1-0", value);
        Assert.Equal(0, sourceCalls);
        Assert.Equal(Start, request.Freshness.OldestFetchedAt);
        Assert.False(request.Freshness.IsStale);
    }

    [Fact]
    public async Task Request_after_fresh_period_reloads_from_source()
    {
        await NewRequest().Cache.GetOrLoadAsync(Key, FreshFor, _ => Task.FromResult("1-0"), CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(6));

        var request = NewRequest();
        var value = await request.Cache.GetOrLoadAsync(Key, FreshFor, _ => Task.FromResult("2-0"), CancellationToken.None);

        Assert.Equal("2-0", value);
        Assert.Equal(Start.AddSeconds(6), request.Freshness.OldestFetchedAt);
        Assert.False(request.Freshness.IsStale);
    }

    [Fact]
    public async Task When_source_is_unavailable_last_good_data_is_returned_and_marked_stale()
    {
        await NewRequest().Cache.GetOrLoadAsync(Key, FreshFor, _ => Task.FromResult("1-0"), CancellationToken.None);
        _clock.Advance(TimeSpan.FromMinutes(10));

        var request = NewRequest();
        var value = await request.Cache.GetOrLoadAsync<string>(Key, FreshFor, _ => throw SourceUnavailable(), CancellationToken.None);

        Assert.Equal("1-0", value);
        Assert.Equal(Start, request.Freshness.OldestFetchedAt);
        Assert.True(request.Freshness.IsStale);
    }

    [Fact]
    public async Task When_source_returns_corrupt_data_last_good_data_is_returned_and_marked_stale()
    {
        await NewRequest().Cache.GetOrLoadAsync(Key, FreshFor, _ => Task.FromResult("1-0"), CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(6));

        var request = NewRequest();
        var value = await request.Cache.GetOrLoadAsync<string>(Key, FreshFor, _ => throw CorruptData(), CancellationToken.None);

        Assert.Equal("1-0", value);
        Assert.True(request.Freshness.IsStale);
    }

    [Fact]
    public async Task When_source_recovers_data_is_fresh_again()
    {
        await NewRequest().Cache.GetOrLoadAsync(Key, FreshFor, _ => Task.FromResult("1-0"), CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(6));
        await NewRequest().Cache.GetOrLoadAsync<string>(Key, FreshFor, _ => throw SourceUnavailable(), CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(1));

        var request = NewRequest();
        var value = await request.Cache.GetOrLoadAsync(Key, FreshFor, _ => Task.FromResult("2-0"), CancellationToken.None);

        Assert.Equal("2-0", value);
        Assert.Equal(Start.AddSeconds(7), request.Freshness.OldestFetchedAt);
        Assert.False(request.Freshness.IsStale);
    }

    [Fact]
    public async Task When_source_is_unavailable_and_nothing_is_cached_the_error_reaches_the_caller()
    {
        var request = NewRequest();

        await Assert.ThrowsAsync<DataSourceUnavailableException>(() =>
            request.Cache.GetOrLoadAsync<string>(Key, FreshFor, _ => throw SourceUnavailable(), CancellationToken.None));

        Assert.Null(request.Freshness.OldestFetchedAt);
    }

    // Eski veri yalnızca kaynak hatalarında devreye girer. Kendi kodumuzdaki bir hata eski veriyle
    // örtülürse yayında fark edilmez; bu yüzden olduğu gibi dışarı çıkmalıdır.
    [Fact]
    public async Task Unexpected_errors_are_not_hidden_behind_cached_data()
    {
        await NewRequest().Cache.GetOrLoadAsync(Key, FreshFor, _ => Task.FromResult("1-0"), CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(6));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            NewRequest().Cache.GetOrLoadAsync<string>(
                Key,
                FreshFor,
                _ => throw new InvalidOperationException("programlama hatası"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Different_keys_do_not_share_data()
    {
        await NewRequest().Cache.GetOrLoadAsync("matches:1:62999:5", FreshFor, _ => Task.FromResult("hafta 5"), CancellationToken.None);

        var value = await NewRequest().Cache.GetOrLoadAsync(
            "matches:1:62999:6",
            FreshFor,
            _ => Task.FromResult("hafta 6"),
            CancellationToken.None);

        Assert.Equal("hafta 6", value);
    }

    // Uygulamada ResilientCache ve DataFreshness her HTTP isteği için yeniden oluşturulur,
    // bellekteki veri ve saat ise ortaktır. Testte de aynı düzen kurulur.
    private (ResilientCache Cache, DataFreshness Freshness) NewRequest()
    {
        var freshness = new DataFreshness();
        var options = new BroadageCacheOptions(
            CompetitionFreshFor: TimeSpan.FromSeconds(30),
            StandingsFreshFor: TimeSpan.FromSeconds(30),
            MatchesFreshFor: FreshFor,
            LineupsFreshFor: TimeSpan.FromSeconds(30),
            TeamsFreshFor: TimeSpan.FromMinutes(5),
            MaxStale: TimeSpan.FromHours(12));

        var cache = new ResilientCache(_memoryCache, _clock, freshness, options, NullLogger<ResilientCache>.Instance);

        return (cache, freshness);
    }

    private static DataSourceUnavailableException SourceUnavailable() =>
        new("Broadage", new TimeoutException("bağlantı kurulamadı"));

    private static InvalidSourceDataException CorruptData() =>
        new("JSON çözülemedi", new FormatException());

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
