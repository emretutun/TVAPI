using TVAPI.Application.Competitions;
using TVAPI.Domain.Teams;

namespace TVAPI.Application.Teams;

public interface ITeamService
{
    /// <summary>Turnuvanın aktif aşamasındaki takımlar; turnuva ya da aktif aşama yoksa null.</summary>
    Task<TeamListDto?> GetByTournamentAsync(int tournamentId, CancellationToken cancellationToken);

    /// <summary>Takım künyesi; takım yoksa null.</summary>
    Task<TeamDetailDto?> GetByIdAsync(int teamId, CancellationToken cancellationToken);

    /// <summary>Takımın kadrosu; takım yoksa null, hiçbir kaynakta kadro yoksa boş liste.</summary>
    Task<SquadDto?> GetSquadAsync(int teamId, CancellationToken cancellationToken);
}

internal sealed class TeamService : ITeamService
{
    private readonly ICompetitionRepository _competitionRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly TimeProvider _timeProvider;

    public TeamService(
        ICompetitionRepository competitionRepository,
        ITeamRepository teamRepository,
        TimeProvider timeProvider)
    {
        _competitionRepository = competitionRepository;
        _teamRepository = teamRepository;
        _timeProvider = timeProvider;
    }

    public async Task<TeamListDto?> GetByTournamentAsync(int tournamentId, CancellationToken cancellationToken)
    {
        var competition = await _competitionRepository.GetCurrentAsync(tournamentId, cancellationToken);
        if (competition is null)
        {
            return null;
        }

        var teams = await _teamRepository.GetByStageAsync(
            competition.Tournament.Id,
            competition.Stage.Id,
            cancellationToken);

        return TeamListDto.From(competition, teams);
    }

    public async Task<TeamDetailDto?> GetByIdAsync(int teamId, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);

        return team is null ? null : TeamDetailDto.From(team);
    }

    public async Task<SquadDto?> GetSquadAsync(int teamId, CancellationToken cancellationToken)
    {
        // Önce takımın varlığına bakılır: "böyle bir takım yok" (404) ile
        // "takım var ama kadrosu yok" (boş liste) istemci için farklı durumlardır.
        var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
        if (team is null)
        {
            return null;
        }

        // Asıl kaynak takımın son maç kadrolarıdır; hiç maç kadrosu yoksa kayıtlı kadro listesine düşülür.
        var recentLineups = await _teamRepository.GetRecentLineupsAsync(teamId, Squad.MaxRecentMatches, cancellationToken);
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var squad = Squad.FromRecentLineups(recentLineups, today);
        var source = SquadSource.MatchLineups;

        if (squad.IsEmpty)
        {
            squad = await _teamRepository.GetRegisteredSquadAsync(teamId, cancellationToken);
            source = SquadSource.RegisteredSquad;
        }

        return SquadDto.From(teamId, source, squad.Ordered());
    }
}
