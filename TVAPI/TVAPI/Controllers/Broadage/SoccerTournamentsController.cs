using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TVAPI.Application.Competitions;
using TVAPI.Application.Teams;

namespace TVAPI.Controllers.Broadage
{
    // TURNUVA UÇLARI (kaynak: webnews_broadage)
    //   GET /api/v1/soccer/tournaments                       turnuva listesi
    //   GET /api/v1/soccer/tournaments/{id}/current          aktif sezon ve hafta
    //   GET /api/v1/soccer/tournaments/{id}/standings?round= puan durumu
    //   GET /api/v1/soccer/tournaments/{id}/matches?round=   haftanın maçları
    //   GET /api/v1/soccer/tournaments/{id}/teams            sezonun takımları
    //
    // Controller ince kalır: isteği alır, servisi çağırır, HTTP cevabına çevirir. SQL ya da iş kuralı içermez.
    [ApiController]
    [Route("api/v1/soccer/tournaments")]
    public class SoccerTournamentsController : ControllerBase
    {
        private readonly ICompetitionService _competitionService;
        private readonly IStandingsService _standingsService;
        private readonly IMatchService _matchService;
        private readonly ITeamService _teamService;

        public SoccerTournamentsController(
            ICompetitionService competitionService,
            IStandingsService standingsService,
            IMatchService matchService,
            ITeamService teamService)
        {
            _competitionService = competitionService;
            _standingsService = standingsService;
            _matchService = matchService;
            _teamService = teamService;
        }

        /// <summary>Kaynakta kayıtlı bütün turnuvaları döner.</summary>
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<TournamentDto>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<TournamentDto>>> GetAll(CancellationToken cancellationToken)
        {
            return Ok(await _competitionService.GetTournamentsAsync(cancellationToken));
        }

        /// <summary>
        /// Turnuvanın aktif aşamasını (sezon) ve aktif haftasını döner.
        /// Haftası olmayan turnuvalarda (hazırlık maçları gibi) <c>round</c> null gelir.
        /// </summary>
        [HttpGet("{tournamentId:int:min(1)}/current")]
        [ProducesResponseType<CurrentCompetitionDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CurrentCompetitionDto>> GetCurrent(int tournamentId, CancellationToken cancellationToken)
        {
            var current = await _competitionService.GetCurrentAsync(tournamentId, cancellationToken);

            return current is null ? NotFound() : Ok(current);
        }

        /// <summary>
        /// Turnuvanın aktif aşamasındaki puan durumunu döner (genel, iç saha, deplasman).
        /// <c>round</c> verilmezse aktif hafta kullanılır.
        /// </summary>
        [HttpGet("{tournamentId:int:min(1)}/standings")]
        [ProducesResponseType<StandingsDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StandingsDto>> GetStandings(
            int tournamentId,
            [FromQuery, Range(1, int.MaxValue)] int? round,
            CancellationToken cancellationToken)
        {
            var standings = await _standingsService.GetAsync(tournamentId, round, cancellationToken);

            return standings is null ? NotFound() : Ok(standings);
        }

        /// <summary>
        /// Turnuvanın aktif aşamasındaki bir haftanın maçlarını tarih sırasıyla döner.
        /// <c>round</c> verilmezse aktif hafta kullanılır.
        /// </summary>
        [HttpGet("{tournamentId:int:min(1)}/matches")]
        [ProducesResponseType<MatchListDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MatchListDto>> GetMatches(
            int tournamentId,
            [FromQuery, Range(1, int.MaxValue)] int? round,
            CancellationToken cancellationToken)
        {
            var matches = await _matchService.GetByRoundAsync(tournamentId, round, cancellationToken);

            return matches is null ? NotFound() : Ok(matches);
        }

        /// <summary>Turnuvanın aktif aşamasında (sezonunda) yer alan takımları ada göre sıralı döner.</summary>
        [HttpGet("{tournamentId:int:min(1)}/teams")]
        [ProducesResponseType<TeamListDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TeamListDto>> GetTeams(int tournamentId, CancellationToken cancellationToken)
        {
            var teams = await _teamService.GetByTournamentAsync(tournamentId, cancellationToken);

            return teams is null ? NotFound() : Ok(teams);
        }
    }
}
