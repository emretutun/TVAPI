namespace TVAPI.Domain.Weather;

/// <summary>Hava tahmini yapılan yer (tbl_hava_merkez).</summary>
public sealed record WeatherCity(int Id, string Name, string NameEn);

/// <summary>Hava durumu tanımı (def_hava_durum).</summary>
/// <param name="Code">Kaynağın kısa kodu (hd_kisa).</param>
/// <param name="Name">Ekranda gösterilen ad: "Parçalı Bulutlu".</param>
/// <param name="Video">Bu durum için kullanılacak ikon/video numarası; kaynaktaki metin haliyle.</param>
public sealed record WeatherCondition(int Id, string Code, string Name, string Video);

/// <summary>Bir yerin bir güne ait tahmini (tbl_hava_tahmin).</summary>
public sealed record DailyForecast(
    int CityId,
    DateOnly Date,
    int High,
    int Low,
    WeatherCondition Condition,
    string? WindDirection,
    int? WindSpeed,
    int? Humidity);
