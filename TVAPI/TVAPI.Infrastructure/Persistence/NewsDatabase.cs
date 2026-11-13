namespace TVAPI.Infrastructure.Persistence;

/// <summary>
/// Kanalın haber veritabanı (örnek: htsportv_news): yayın akışları, haberler, habere bağlı kayıtlı sahneler.
/// Bağlantı ayarı zorunlu değildir: verilmemişse yalnızca haber uçları 503 döner.
/// </summary>
internal sealed class NewsDatabase : MySqlDatabase
{
    public const string ConnectionName = "News";

    public NewsDatabase(string? connectionString)
        : base(ConnectionName, connectionString)
    {
    }
}
