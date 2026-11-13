using TVAPI.Application.Competitions;
using TVAPI.Domain.Competitions;

namespace TVAPI.Tests.Competitions;

/// <summary>Servis testlerinde kullanılan hazır veriler ve veritabanı yerine geçen sahte repository'ler.</summary>
internal static class TestData
{
    public const int TournamentId = 1;
    public const int StageId = 62999;
    public const int ActiveRoundId = 6;

    public static CurrentCompetition LeagueWithActiveRound() => new(
        new Tournament(TournamentId, "Trendyol Süper Lig"),
        new Stage(StageId, "2026/2027 Sezonu", "26/27"),
        new Round(ActiveRoundId, "6. Hafta", "6"));

    public static CurrentCompetition CupWithoutRounds() => new(
        new Tournament(TournamentId, "Türkiye Kupası"),
        new Stage(StageId, "2026/2027 Sezonu", "26/27"),
        Round: null);

    public static Standings EmptyStandings() => new([], [], []);
}

internal sealed class FakeCompetitionRepository(CurrentCompetition? competition) : ICompetitionRepository
{
    public Task<CurrentCompetition?> GetCurrentAsync(int tournamentId, CancellationToken cancellationToken) =>
        Task.FromResult(competition);

    public Task<IReadOnlyList<Tournament>> GetTournamentsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Tournament>>(competition is null ? [] : [competition.Tournament]);
}

internal sealed class FakeStandingsRepository(Standings? standings) : IStandingsRepository
{
    /// <summary>Servisin repository'yi hangi değerlerle çağırdığı; hiç çağırmadıysa null.</summary>
    public (int TournamentId, int StageId, int RoundId)? Requested { get; private set; }

    public Task<Standings?> GetAsync(int tournamentId, int stageId, int roundId, CancellationToken cancellationToken)
    {
        Requested = (tournamentId, stageId, roundId);
        return Task.FromResult(standings);
    }
}

internal sealed class FakeMatchRepository(IReadOnlyList<Match> matches) : IMatchRepository
{
    public Task<Match?> GetByIdAsync(int matchId, CancellationToken cancellationToken) =>
        Task.FromResult(matches.FirstOrDefault(match => match.Id == matchId));

    public (int TournamentId, int StageId, int RoundId)? Requested { get; private set; }

    /// <summary>Servisin tarih aralığı sorgusunu hangi değerlerle çağırdığı.</summary>
    public (IReadOnlyCollection<int> TournamentIds, DateTime From, DateTime To)? RequestedRange { get; private set; }

    public Task<IReadOnlyList<Match>> GetByDateRangeAsync(
        IReadOnlyCollection<int> tournamentIds,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        RequestedRange = (tournamentIds, from, to);
        return Task.FromResult(matches);
    }

    public Task<IReadOnlyList<Match>> GetByRoundAsync(
        int tournamentId,
        int stageId,
        int roundId,
        CancellationToken cancellationToken)
    {
        Requested = (tournamentId, stageId, roundId);
        return Task.FromResult(matches);
    }
}
