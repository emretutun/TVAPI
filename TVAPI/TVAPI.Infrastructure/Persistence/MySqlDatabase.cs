using Dapper;
using MySqlConnector;
using TVAPI.Application.Common;

namespace TVAPI.Infrastructure.Persistence;

/// <summary>
/// Bir MySQL veritabanına giden bütün sorguların geçtiği ortak taban. YALNIZCA OKUMA yapar:
/// dışarıya açtığı iki metot da sorgu sonucu döndürür; veri değiştiren bir metodu bilerek yoktur.
/// Her sorgu kendi bağlantısını açıp kapatır; bağlantı havuzunu MySqlConnector yönetir.
/// Bağlantı ve zaman aşımı hataları burada tek yerde yakalanıp uygulamanın kendi hatasına çevrilir,
/// böylece repository sınıfları yalnızca SQL ve dönüşümle ilgilenir.
/// </summary>
internal abstract class MySqlDatabase
{
    private readonly string _dataSourceName;
    private readonly string? _connectionString;

    /// <param name="dataSourceName">Hata mesajlarında ve loglarda görünen ad.</param>
    /// <param name="connectionString">Bağlantı metni; tanımlı değilse null.</param>
    protected MySqlDatabase(string dataSourceName, string? connectionString)
    {
        _dataSourceName = dataSourceName;
        _connectionString = connectionString;
    }

    public Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object parameters, CancellationToken cancellationToken) =>
        ExecuteAsync(
            connection => connection.QuerySingleOrDefaultAsync<T>(
                new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)),
            cancellationToken);

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object parameters, CancellationToken cancellationToken)
    {
        var rows = await ExecuteAsync(
            connection => connection.QueryAsync<T>(
                new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)),
            cancellationToken);

        return rows.AsList();
    }

    private async Task<TResult> ExecuteAsync<TResult>(
        Func<MySqlConnection, Task<TResult>> query,
        CancellationToken cancellationToken)
    {
        // Bağlantı ayarı verilmemiş bir veritabanı, istemci açısından "ulaşılamayan kaynak"tır (503).
        // Uygulama yine de açılır; yalnızca bu veritabanını kullanan uçlar hata döner.
        if (_connectionString is null)
        {
            throw new DataSourceUnavailableException(
                _dataSourceName,
                new InvalidOperationException($"'ConnectionStrings:{_dataSourceName}' ayarı tanımlı değil."));
        }

        try
        {
            await using var connection = new MySqlConnection(_connectionString);

            return await query(connection);
        }
        // İstemci isteği iptal ettiyse bu bir veri kaynağı arızası değildir; o durumu çevirmeyiz.
        catch (MySqlException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DataSourceUnavailableException(_dataSourceName, exception);
        }
    }
}
