using TVAPI.Application.Competitions;

namespace TVAPI.Tests.Competitions;

public sealed class StandingsServiceTests
{
    [Fact]
    public async Task Active_round_is_used_when_no_round_is_requested()
    {
        var standingsRepository = new FakeStandingsRepository(TestData.EmptyStandings());
        var service = new StandingsService(
            new FakeCompetitionRepository(TestData.LeagueWithActiveRound()),
            standingsRepository);

        var result = await service.GetAsync(TestData.TournamentId, roundId: null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(TestData.ActiveRoundId, result.RoundId);
        Assert.Equal((TestData.TournamentId, TestData.StageId, TestData.ActiveRoundId), standingsRepository.Requested);
    }

    [Fact]
    public async Task Requested_round_overrides_active_round()
    {
        var standingsRepository = new FakeStandingsRepository(TestData.EmptyStandings());
        var service = new StandingsService(
            new FakeCompetitionRepository(TestData.LeagueWithActiveRound()),
            standingsRepository);

        var result = await service.GetAsync(TestData.TournamentId, roundId: 3, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result.RoundId);
        Assert.Equal((TestData.TournamentId, TestData.StageId, 3), standingsRepository.Requested);
    }

    [Fact]
    public async Task Unknown_tournament_returns_nothing_without_reading_standings()
    {
        var standingsRepository = new FakeStandingsRepository(TestData.EmptyStandings());
        var service = new StandingsService(new FakeCompetitionRepository(null), standingsRepository);

        var result = await service.GetAsync(999, roundId: null, CancellationToken.None);

        Assert.Null(result);
        Assert.Null(standingsRepository.Requested);
    }

    [Fact]
    public async Task Tournament_without_rounds_returns_nothing_when_no_round_is_requested()
    {
        var standingsRepository = new FakeStandingsRepository(TestData.EmptyStandings());
        var service = new StandingsService(
            new FakeCompetitionRepository(TestData.CupWithoutRounds()),
            standingsRepository);

        var result = await service.GetAsync(TestData.TournamentId, roundId: null, CancellationToken.None);

        Assert.Null(result);
        Assert.Null(standingsRepository.Requested);
    }

    [Fact]
    public async Task Round_without_standings_returns_nothing()
    {
        var service = new StandingsService(
            new FakeCompetitionRepository(TestData.LeagueWithActiveRound()),
            new FakeStandingsRepository(standings: null));

        var result = await service.GetAsync(TestData.TournamentId, roundId: 34, CancellationToken.None);

        Assert.Null(result);
    }
}
