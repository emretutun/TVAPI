namespace TVAPI.Application.Common;

internal sealed class DataFreshness : IDataFreshness
{
    public DateTimeOffset? OldestFetchedAt { get; private set; }

    public bool IsStale { get; private set; }

    // Bir cevap birden çok okumadan oluşabilir (örnek: aktif hafta + maç listesi).
    // İstemciye en kötü durumu bildiririz: en eski veri zamanı ve herhangi biri eskiyse "eski".
    public void Record(DateTimeOffset fetchedAt, bool isStale)
    {
        if (OldestFetchedAt is null || fetchedAt < OldestFetchedAt)
        {
            OldestFetchedAt = fetchedAt;
        }

        IsStale |= isStale;
    }
}
