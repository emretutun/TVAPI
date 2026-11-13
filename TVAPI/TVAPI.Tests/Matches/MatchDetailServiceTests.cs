using TVAPI.Application.Matches;
using TVAPI.Domain.Competitions;
using TVAPI.Domain.Matches;
using TVAPI.Tests.Competitions;

namespace TVAPI.Tests.Matches;

public sealed class MatchDetailServiceTests
{
    private const int MatchId = 2058099;
    private const int UnknownMatchId = 404;

    private static readonly TeamSummary HomeTeam = new(3, "Beşiktaş", "BJK", "Beşiktaş");
    private static readonly TeamSummary AwayTeam = new(15659, "Erzurumspor FK", "EFK", "Erzurumspor FK");

    private static readonly Match ExistingMatch = new(
        MatchId,
        new DateTime(2026, 9, 11, 20, 0, 0),
        new MatchStatus(5, "Maç Sonu", "MS"),
        CurrentMinute: 90,
        Stoppage: null,
        new MatchSide(HomeTeam, new MatchScore(4, 4, 2, null, null)),
        new MatchSide(AwayTeam, new MatchScore(0, 0, 0, null, null)),
        StadiumName: "Tüpraş Stadyumu",
        RefereeName: null,
        TournamentId: 1,
        Stage: new Stage(62999, "2026/2027 Sezonu", "26/27"),
        Round: new Round(5, "5. Hafta", "5.H"),
        StadiumId: 5574,
        RefereeId: 8531);

    [Fact]
    public async Task Match_is_returned_with_its_current_score()
    {
        var service = NewService(new FakeMatchDetailRepository());

        var match = await service.GetByIdAsync(MatchId, CancellationToken.None);

        Assert.NotNull(match);
        Assert.Equal(4, match.Home.Score.Current);
        Assert.Equal("MS", match.Status.ShortName);
    }

    [Fact]
    public async Task Unknown_match_returns_nothing_and_details_are_not_read()
    {
        var details = new FakeMatchDetailRepository();
        var service = NewService(details);

        Assert.Null(await service.GetByIdAsync(UnknownMatchId, CancellationToken.None));
        Assert.Null(await service.GetLineupAsync(UnknownMatchId, CancellationToken.None));
        Assert.Null(await service.GetEventsAsync(UnknownMatchId, CancellationToken.None));
        Assert.Null(await service.GetStatisticsAsync(UnknownMatchId, CancellationToken.None));
        Assert.Equal(0, details.Reads);
    }

    // "Maç yok" null'dır (404). "Maç var ama veri henüz yok" ise içi boş bir cevaptır (200).
    [Fact]
    public async Task Existing_match_without_details_returns_empty_answers()
    {
        var service = NewService(new FakeMatchDetailRepository());

        var lineup = await service.GetLineupAsync(MatchId, CancellationToken.None);
        var events = await service.GetEventsAsync(MatchId, CancellationToken.None);
        var statistics = await service.GetStatisticsAsync(MatchId, CancellationToken.None);

        Assert.NotNull(lineup);
        Assert.Equal(MatchId, lineup.MatchId);
        Assert.Null(lineup.Home);
        Assert.Null(lineup.Away);

        Assert.NotNull(events);
        Assert.Empty(events.Events);

        Assert.NotNull(statistics);
        Assert.Null(statistics.Home);
        Assert.Null(statistics.Away);
    }

    [Fact]
    public async Task Details_are_mapped_to_the_answer()
    {
        var details = new FakeMatchDetailRepository
        {
            Lineup = new MatchLineup(
                new TeamLineup(HomeTeam, "4-2-3-1", "Ole Gunnar Solskjaer", [], []),
                new TeamLineup(AwayTeam, null, null, [], [])),
            Events = [new MatchEvent(1, new MatchEventType(6, "Gol", "G"), 21, 11, HomeTeam.Id, null, 1, 0, null)],
            Statistics = new MatchStatistics(
                new TeamStatistics(HomeTeam, 74, 6, 15, 10, 10, 2, 0, 3, 0, 0),
                new TeamStatistics(AwayTeam, 26, 0, 8, 11, 1, 4, 4, 0, 0, 0)),
        };
        var service = NewService(details);

        var lineup = await service.GetLineupAsync(MatchId, CancellationToken.None);
        var events = await service.GetEventsAsync(MatchId, CancellationToken.None);
        var statistics = await service.GetStatisticsAsync(MatchId, CancellationToken.None);

        Assert.Equal("4-2-3-1", lineup!.Home!.Formation);
        Assert.Equal("EFK", lineup.Away!.Team.ShortName);
        Assert.Equal("Gol", Assert.Single(events!.Events).Type.Name);
        Assert.Equal(74, statistics!.Home!.BallPossession);
        Assert.Equal(4, statistics.Away!.Saves);
    }

    private static MatchDetailService NewService(FakeMatchDetailRepository details) =>
        new(new FakeMatchRepository([ExistingMatch]), details);

    private sealed class FakeMatchDetailRepository : IMatchDetailRepository
    {
        public MatchLineup? Lineup { get; init; }
        public IReadOnlyList<MatchEvent> Events { get; init; } = [];
        public MatchStatistics? Statistics { get; init; }

        /// <summary>Servisin ayrıntı verisini kaç kez okuduğu.</summary>
        public int Reads { get; private set; }

        public Task<MatchLineup?> GetLineupAsync(int matchId, CancellationToken cancellationToken)
        {
            Reads++;
            return Task.FromResult(Lineup);
        }

        public Task<IReadOnlyList<MatchEvent>> GetEventsAsync(int matchId, CancellationToken cancellationToken)
        {
            Reads++;
            return Task.FromResult(Events);
        }

        public Task<MatchStatistics?> GetStatisticsAsync(int matchId, CancellationToken cancellationToken)
        {
            Reads++;
            return Task.FromResult(Statistics);
        }
    }
}
