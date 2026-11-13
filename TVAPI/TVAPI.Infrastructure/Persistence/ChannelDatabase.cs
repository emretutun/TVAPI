namespace TVAPI.Infrastructure.Persistence;

/// <summary>
/// Kanalın kendi veritabanı (örnek: htsportv): sahne şablonları, kayıtlı sahneler, playlist, hava durumu.
/// Her kanalın veritabanı ayrıdır; API'nin her kurulumu tek bir kanalın veritabanına bağlanır.
/// Bağlantı ayarı zorunlu değildir: verilmemişse yalnızca bu veritabanını kullanan uçlar 503 döner.
/// </summary>
internal sealed class ChannelDatabase : MySqlDatabase
{
    public const string ConnectionName = "Channel";

    public ChannelDatabase(string? connectionString)
        : base(ConnectionName, connectionString)
    {
    }
}
