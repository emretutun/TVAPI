namespace TVAPI.Application.Common;

/// <summary>
/// Veri kaynağına ulaşılamadığında ya da kaynak zamanında cevap vermediğinde fırlatılır.
/// Infrastructure, kütüphaneye özgü hataları (örnek: MySqlException) bu hataya çevirir;
/// böylece üst katmanlar hangi veritabanının kullanıldığını bilmek zorunda kalmaz.
/// </summary>
public sealed class DataSourceUnavailableException : Exception
{
    public DataSourceUnavailableException(string dataSourceName, Exception innerException)
        : base($"'{dataSourceName}' veri kaynağına ulaşılamadı.", innerException)
    {
        DataSourceName = dataSourceName;
    }

    public string DataSourceName { get; }
}
