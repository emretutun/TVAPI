using TVAPI.Application.Weather;
using TVAPI.Domain.Weather;

namespace TVAPI.Infrastructure.Persistence;

/// <summary>Hava durumunu kanal veritabanından okur: tbl_hava_merkez, tbl_hava_tahmin, def_hava_durum.</summary>
internal sealed class WeatherRepository : IWeatherRepository
{
    private const string CitiesSql = """
        SELECT
            m.merkez_id       AS Id,
            m.merkez_tanim    AS Name,
            m.merkez_tanim_en AS NameEn
        FROM tbl_hava_merkez m
        WHERE m.merkez_id IN @CityIds
        ORDER BY m.merkez_id
        """;

    // (merkez_id, tarih) benzersiz index'ini kullanır: her yer için yalnızca istenen günler okunur.
    // Eski sorgudaki DAYOFWEEK hesabı burada yok; haftanın günü tarihten kodda bulunur.
    private const string ForecastsSql = """
        SELECT
            t.merkez_id        AS CityId,
            t.tarih            AS Date,
            t.en_yuksek_derece AS High,
            t.en_dusuk_derece  AS Low,
            t.ruzgar_yon       AS WindDirection,
            t.ruzgar_hiz       AS WindSpeed,
            t.nem_orani        AS Humidity,
            d.hd_id            AS ConditionId,
            d.hd_kisa          AS ConditionCode,
            d.hd_adi           AS ConditionName,
            d.hd_video         AS ConditionVideo
        FROM tbl_hava_tahmin t
        INNER JOIN def_hava_durum d ON d.hd_id = t.hd_id
        WHERE t.merkez_id IN @CityIds
          AND t.tarih >= @From
          AND t.tarih <= @To
        ORDER BY t.merkez_id, t.tarih
        """;

    private readonly ChannelDatabase _database;

    public WeatherRepository(ChannelDatabase database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<WeatherCity>> GetCitiesAsync(
        IReadOnlyCollection<int> cityIds,
        CancellationToken cancellationToken)
    {
        if (cityIds.Count == 0)
        {
            return [];
        }

        var rows = await _database.QueryAsync<CityRow>(CitiesSql, new { CityIds = cityIds }, cancellationToken);

        return rows.Select(row => new WeatherCity(row.Id, row.Name, row.NameEn)).ToList();
    }

    public async Task<IReadOnlyList<DailyForecast>> GetForecastsAsync(
        IReadOnlyCollection<int> cityIds,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        if (cityIds.Count == 0)
        {
            return [];
        }

        // Tarihler sorguya DateTime olarak verilir; sütun "date" türünde olduğu için saat kısmı 00:00'dır.
        var rows = await _database.QueryAsync<ForecastRow>(
            ForecastsSql,
            new
            {
                CityIds = cityIds,
                From = from.ToDateTime(TimeOnly.MinValue),
                To = to.ToDateTime(TimeOnly.MinValue),
            },
            cancellationToken);

        return rows.Select(row => row.ToDomain()).ToList();
    }

    // Aşağıdaki sınıflar sorgu sonuçlarının bire bir karşılığıdır ve yalnızca burada kullanılır.

    private sealed class CityRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
    }

    private sealed class ForecastRow
    {
        public int CityId { get; set; }
        public DateTime Date { get; set; }
        public int High { get; set; }
        public int Low { get; set; }
        public string? WindDirection { get; set; }
        public int? WindSpeed { get; set; }
        public int? Humidity { get; set; }
        public int ConditionId { get; set; }
        public string ConditionCode { get; set; } = string.Empty;
        public string ConditionName { get; set; } = string.Empty;
        public string ConditionVideo { get; set; } = string.Empty;

        public DailyForecast ToDomain() => new(
            CityId,
            DateOnly.FromDateTime(Date),
            High,
            Low,
            new WeatherCondition(ConditionId, ConditionCode, ConditionName, ConditionVideo),
            // Kaynak bilinmeyen rüzgar yönünü boş metin olarak tutar.
            string.IsNullOrWhiteSpace(WindDirection) ? null : WindDirection,
            WindSpeed,
            Humidity);
    }
}
