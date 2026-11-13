using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TVAPI.Application.Weather;

namespace TVAPI.Controllers.Weather
{
    // HAVA DURUMU UÇLARI (kaynak: kanal veritabanı; tbl_hava_merkez, tbl_hava_tahmin, def_hava_durum)
    //   GET /api/v1/weather/cities?id=                       yerler (id sırasıyla değil, isteğin sırasıyla)
    //   GET /api/v1/weather/forecasts?cityId=&from=&days=    yerlerin günlük tahminleri
    [ApiController]
    [Route("api/v1/weather")]
    public class WeatherController : ControllerBase
    {
        private const int MaxCities = 50;
        private const int DefaultDays = 3; // eski reji ekranı bugün ve sonraki iki günü gösterir
        private const int MaxDays = 10;

        private readonly IWeatherService _weatherService;

        public WeatherController(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        /// <summary>
        /// Verilen yerleri isteğin sırasıyla döner; bulunmayan id'ler atlanır.
        /// <c>id</c> birden çok kez yazılabilir: <c>?id=34&amp;id=6&amp;id=35</c>.
        /// </summary>
        [HttpGet("cities")]
        [ProducesResponseType<IReadOnlyList<WeatherCityDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<WeatherCityDto>>> GetCities(
            [FromQuery, Required, MinLength(1), MaxLength(MaxCities)] int[] id,
            CancellationToken cancellationToken)
        {
            return Ok(await _weatherService.GetCitiesAsync(id, cancellationToken));
        }

        /// <summary>
        /// Verilen yerlerin günlük tahminlerini döner; yerler isteğin sırasıyla, günler tarih sırasıyla gelir.
        /// <c>from</c> verilmezse bugün, <c>days</c> verilmezse 3 gün kullanılır. Tahmini olmayan gün listede yer almaz.
        /// Örnek: <c>?cityId=34&amp;cityId=6&amp;from=2026-11-14&amp;days=3</c>
        /// </summary>
        [HttpGet("forecasts")]
        [ProducesResponseType<IReadOnlyList<CityForecastDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<CityForecastDto>>> GetForecasts(
            [FromQuery, Required, MinLength(1), MaxLength(MaxCities)] int[] cityId,
            [FromQuery] DateOnly? from,
            [FromQuery, Range(1, MaxDays)] int days = DefaultDays,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _weatherService.GetForecastsAsync(cityId, from, days, cancellationToken));
        }
    }
}
