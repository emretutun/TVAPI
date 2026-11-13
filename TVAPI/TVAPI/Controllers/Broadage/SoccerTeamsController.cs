using Microsoft.AspNetCore.Mvc;
using TVAPI.Application.Teams;

namespace TVAPI.Controllers.Broadage
{
    // TAKIM UÇLARI (kaynak: webnews_broadage)
    //   GET /api/v1/soccer/teams/{id}          takım künyesi
    //   GET /api/v1/soccer/teams/{id}/players  takım kadrosu
    //
    // Takım, turnuvadan bağımsız bir kaynaktır (aynı takım ligde de kupada da oynar);
    // bu yüzden kendi adresi vardır: /soccer/teams/{id}.
    [ApiController]
    [Route("api/v1/soccer/teams")]
    public class SoccerTeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;

        public SoccerTeamsController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        /// <summary>Takımın künyesini döner (adlar, teknik direktör, ülke, stat).</summary>
        [HttpGet("{teamId:int:min(1)}")]
        [ProducesResponseType<TeamDetailDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TeamDetailDto>> GetById(int teamId, CancellationToken cancellationToken)
        {
            var team = await _teamService.GetByIdAsync(teamId, cancellationToken);

            return team is null ? NotFound() : Ok(team);
        }

        /// <summary>
        /// Takımın kayıtlı kadrosunu mevki ve forma numarası sırasıyla döner.
        /// Takım var ama kadrosu girilmemişse boş liste döner.
        /// </summary>
        [HttpGet("{teamId:int:min(1)}/players")]
        [ProducesResponseType<SquadDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SquadDto>> GetPlayers(int teamId, CancellationToken cancellationToken)
        {
            var squad = await _teamService.GetSquadAsync(teamId, cancellationToken);

            return squad is null ? NotFound() : Ok(squad);
        }
    }
}
