namespace TVAPI.Application.Competitions;

internal sealed class MatchService : IMatchService
{
    private readonly ICompetitionRepository _competitionRepository;
    private readonly IMatchRepository _matchRepository;

    public MatchService(ICompetitionRepository competitionRepository, IMatchRepository matchRepository)
    {
        _competitionRepository = competitionRepository;
        _matchRepository = matchRepository;
    }

    public async Task<MatchListDto?> GetByRoundAsync(int tournamentId, int? roundId, CancellationToken cancellationToken)
    {
        var competition = await _competitionRepository.GetCurrentAsync(tournamentId, cancellationToken);
        if (competition is null)
        {
            return null;
        }

        var effectiveRoundId = competition.ResolveRoundId(roundId);
        if (effectiveRoundId is null)
        {
            return null;
        }

        var matches = await _matchRepository.GetByRoundAsync(
            competition.Tournament.Id,
            competition.Stage.Id,
            effectiveRoundId.Value,
            cancellationToken);

        return MatchListDto.From(competition, effectiveRoundId.Value, matches);
    }

    public async Task<IReadOnlyList<MatchDto>> GetByDateRangeAsync(
        IReadOnlyCollection<int> tournamentIds,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        // Aynı sorgu her zaman aynı biçimde gitsin diye turnuvalar tekilleştirilip sıralanır, saniyeler atılır.
        // Böylece "son 3 saat" gibi her istekte biraz kayan aralıklar önbelleği gereksiz kayıtla doldurmaz.
        var orderedIds = tournamentIds.Distinct().Order().ToList();

        var matches = await _matchRepository.GetByDateRangeAsync(
            orderedIds,
            TruncateToMinute(from),
            TruncateToMinute(to),
            cancellationToken);

        return matches.Select(MatchDto.From).ToList();
    }

    private static DateTime TruncateToMinute(DateTime value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);
}
