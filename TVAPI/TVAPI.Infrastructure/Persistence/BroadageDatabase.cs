namespace TVAPI.Infrastructure.Persistence;

/// <summary>Broadage (spor verisi) veritabanı: webnews_broadage.</summary>
internal sealed class BroadageDatabase : MySqlDatabase
{
    public const string ConnectionName = "Broadage";

    public BroadageDatabase(string connectionString)
        : base(ConnectionName, connectionString)
    {
    }
}
