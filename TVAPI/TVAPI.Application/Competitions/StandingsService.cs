namespace TVAPI.Application.Competitions;

internal sealed class StandingsService : IStandingsService
{
    private readonly ICompetitionRepository _competitionRepository;
    private readonly IStandingsRepository _standingsRepository;

    public StandingsService(ICompetitionRepository competitionRepository, IStandingsRepository standingsRepository)
    {
        _competitionRepository = competitionRepository;
        _standingsRepository = standingsRepository;
    }

    public async Task<StandingsDto?> GetAsync(int tournamentId, int? roundId, CancellationToken cancellationToken)
    {
        var competition = await _competitionRepository.GetCurrentAsync(tournamentId, cancellationToken);
        if (competition is null)
        {
            return null;
        }

        // Haftasız turnuvada hafta bulunamaz, puan durumu da olmaz.
        var effectiveRoundId = competition.ResolveRoundId(roundId);
        if (effectiveRoundId is null)
        {
            return null;
        }

        var standings = await _standingsRepository.GetAsync(
            competition.Tournament.Id,
            competition.Stage.Id,
            effectiveRoundId.Value,
            cancellationToken);

        return standings is null ? null : StandingsDto.From(competition, effectiveRoundId.Value, standings);
    }
}
