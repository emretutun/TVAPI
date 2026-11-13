namespace TVAPI.Application.Common;

/// <summary>
/// Veri kaynağı cevap verdi ama içerik beklenen biçimde değil (örnek: bozuk JSON).
/// Mesaj, hangi kaydın bozuk olduğunu loglarda bulabilecek kadar ayrıntı içermelidir.
/// </summary>
public sealed class InvalidSourceDataException : Exception
{
    public InvalidSourceDataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
