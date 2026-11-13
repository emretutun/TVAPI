using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TVAPI.Application.Competitions;
using TVAPI.Application.Matches;

namespace TVAPI.Controllers.Broadage
{
    // MAÇ UÇLARI (kaynak: webnews_broadage)
    //   GET /api/v1/soccer/matches?tournamentId=&from=&to=  tarihe göre maçlar
    //   GET /api/v1/soccer/matches/{id}                     tek maç: skor, dakika, durum
    //   GET /api/v1/soccer/matches/{id}/lineups             maç kadroları
    //   GET /api/v1/soccer/matches/{id}/events              maç olayları
    //   GET /api/v1/soccer/matches/{id}/statistics          maç istatistikleri
    //
    // Tek bir maçla ilgili her şey maçın kendi adresinin altındadır: /soccer/matches/{id}/...
    // Bütün uçlarda maç yoksa 404 döner; maç var ama veri henüz oluşmadıysa 200 ve içi boş cevap döner.
    [ApiController]
    [Route("api/v1/soccer/matches")]
    public class SoccerMatchesController : ControllerBase
    {
        // Tarih sütunu index'li olmadığı için çok geniş aralıklar ve çok uzun turnuva listeleri kabul edilmez.
        private const int MaxRangeDays = 31;
        private const int MaxTournaments = 20;

        private readonly IMatchService _matchService;
        private readonly IMatchDetailService _matchDetailService;

        public SoccerMatchesController(IMatchService matchService, IMatchDetailService matchDetailService)
        {
            _matchService = matchService;
            _matchDetailService = matchDetailService;
        }

        /// <summary>
        /// Verilen turnuvaların, tarihi <c>from</c> (dahil) ile <c>to</c> (hariç) arasındaki maçlarını tarih sırasıyla döner.
        /// Haftası olmayan turnuvalar ve birden çok turnuvayı kapsayan skor bantları içindir.
        /// Tarihler veritabanındaki gibi yerel saattir. Örnek: <c>?tournamentId=1&amp;tournamentId=4&amp;from=2026-11-13&amp;to=2026-11-17</c>
        /// </summary>
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<MatchDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<MatchDto>>> GetByDateRange(
            [FromQuery, Required, MinLength(1), MaxLength(MaxTournaments)] int[] tournamentId,
            [FromQuery, Required] DateTime? from,
            [FromQuery, Required] DateTime? to,
            CancellationToken cancellationToken)
        {
            if (tournamentId.Any(id => id < 1))
            {
                ModelState.AddModelError(nameof(tournamentId), "Turnuva id'leri 1 ya da daha büyük olmalıdır.");
            }

            if (to <= from)
            {
                ModelState.AddModelError(nameof(to), "'to' tarihi 'from' tarihinden sonra olmalıdır.");
            }
            else if (to - from > TimeSpan.FromDays(MaxRangeDays))
            {
                ModelState.AddModelError(nameof(to), $"Tarih aralığı en fazla {MaxRangeDays} gün olabilir.");
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            return Ok(await _matchService.GetByDateRangeAsync(tournamentId, from!.Value, to!.Value, cancellationToken));
        }

        /// <summary>Maçın güncel halini döner: skor, dakika, durum.</summary>
        [HttpGet("{matchId:int:min(1)}")]
        [ProducesResponseType<MatchDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MatchDto>> GetById(int matchId, CancellationToken cancellationToken)
        {
            var match = await _matchDetailService.GetByIdAsync(matchId, cancellationToken);

            return match is null ? NotFound() : Ok(match);
        }

        /// <summary>Maç kadrolarını döner (ilk 11, yedekler, diziliş). Kadro açıklanmadıysa home ve away null gelir.</summary>
        [HttpGet("{matchId:int:min(1)}/lineups")]
        [ProducesResponseType<MatchLineupDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MatchLineupDto>> GetLineups(int matchId, CancellationToken cancellationToken)
        {
            var lineup = await _matchDetailService.GetLineupAsync(matchId, cancellationToken);

            return lineup is null ? NotFound() : Ok(lineup);
        }

        /// <summary>Maç olaylarını oluş sırasıyla döner (gol, kart, değişiklik...). Maç başlamadıysa liste boştur.</summary>
        [HttpGet("{matchId:int:min(1)}/events")]
        [ProducesResponseType<MatchEventsDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MatchEventsDto>> GetEvents(int matchId, CancellationToken cancellationToken)
        {
            var events = await _matchDetailService.GetEventsAsync(matchId, cancellationToken);

            return events is null ? NotFound() : Ok(events);
        }

        /// <summary>Maç istatistiklerini döner. İstatistik henüz yoksa home ve away null gelir.</summary>
        [HttpGet("{matchId:int:min(1)}/statistics")]
        [ProducesResponseType<MatchStatisticsDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MatchStatisticsDto>> GetStatistics(int matchId, CancellationToken cancellationToken)
        {
            var statistics = await _matchDetailService.GetStatisticsAsync(matchId, cancellationToken);

            return statistics is null ? NotFound() : Ok(statistics);
        }
    }
}
