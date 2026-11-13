namespace TVAPI.Application.Competitions;

internal sealed class CompetitionService : ICompetitionService
{
    private readonly ICompetitionRepository _repository;

    public CompetitionService(ICompetitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<CurrentCompetitionDto?> GetCurrentAsync(int tournamentId, CancellationToken cancellationToken)
    {
        var competition = await _repository.GetCurrentAsync(tournamentId, cancellationToken);

        return competition is null ? null : CurrentCompetitionDto.From(competition);
    }

    public async Task<IReadOnlyList<TournamentDto>> GetTournamentsAsync(CancellationToken cancellationToken)
    {
        var tournaments = await _repository.GetTournamentsAsync(cancellationToken);

        return tournaments.Select(tournament => new TournamentDto(tournament.Id, tournament.Name)).ToList();
    }
}
