using TVAPI.Application.Common;

namespace TVAPI.Tests.Common;

public sealed class DataFreshnessTests
{
    private static readonly DateTimeOffset Older = new(2026, 11, 13, 20, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Newer = Older.AddSeconds(25);

    [Fact]
    public void Nothing_is_reported_when_no_data_was_read()
    {
        var freshness = new DataFreshness();

        Assert.Null(freshness.OldestFetchedAt);
        Assert.False(freshness.IsStale);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Oldest_fetch_time_is_reported_regardless_of_read_order(bool olderFirst)
    {
        var freshness = new DataFreshness();

        freshness.Record(olderFirst ? Older : Newer, isStale: false);
        freshness.Record(olderFirst ? Newer : Older, isStale: false);

        Assert.Equal(Older, freshness.OldestFetchedAt);
    }

    [Fact]
    public void Response_is_stale_when_any_of_its_reads_is_stale()
    {
        var freshness = new DataFreshness();

        freshness.Record(Older, isStale: true);
        freshness.Record(Newer, isStale: false);

        Assert.True(freshness.IsStale);
    }
}
