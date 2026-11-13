namespace TVAPI.Application.Common;

/// <summary>
/// Bir isteğin cevabında kullanılan verinin ne kadar taze olduğunu izler.
/// Veriyi getiren katman her okumada buraya kayıt düşer; sunum katmanı sonucu istemciye bildirir.
/// İstek başına bir tane oluşturulur.
/// </summary>
public interface IDataFreshness
{
    /// <summary>Bu istekte kullanılan en eski verinin kaynaktan alındığı an. Hiç veri okunmadıysa null.</summary>
    DateTimeOffset? OldestFetchedAt { get; }

    /// <summary>Kaynağa ulaşılamadığı için eski (son başarılı) veri kullanıldıysa true.</summary>
    bool IsStale { get; }

    void Record(DateTimeOffset fetchedAt, bool isStale);
}
