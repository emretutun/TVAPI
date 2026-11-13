using TVAPI.Application.Competitions;
using TVAPI.Domain.Competitions;

namespace TVAPI.Tests.Competitions;

public sealed class MatchServiceTests
{
    [Fact]
    public async Task Active_round_is_used_when_no_round_is_requested()
    {
        var matchRepository = new FakeMatchRepository([]);
        var service = new MatchService(new FakeCompetitionRepository(TestData.LeagueWithActiveRound()), matchRepository);

        var result = await service.GetByRoundAsync(TestData.TournamentId, roundId: null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(TestData.ActiveRoundId, result.RoundId);
        Assert.Equal((TestData.TournamentId, TestData.StageId, TestData.ActiveRoundId), matchRepository.Requested);
    }

    [Fact]
    public async Task Requested_round_overrides_active_round()
    {
        var matchRepository = new FakeMatchRepository([]);
        var service = new MatchService(new FakeCompetitionRepository(TestData.LeagueWithActiveRound()), matchRepository);

        var result = await service.GetByRoundAsync(TestData.TournamentId, roundId: 20, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(20, result.RoundId);
        Assert.Equal((TestData.TournamentId, TestData.StageId, 20), matchRepository.Requested);
    }

    [Fact]
    public async Task Unknown_tournament_returns_nothing_without_reading_matches()
    {
        var matchRepository = new FakeMatchRepository([]);
        var service = new MatchService(new FakeCompetitionRepository(null), matchRepository);

        var result = await service.GetByRoundAsync(999, roundId: null, CancellationToken.None);

        Assert.Null(result);
        Assert.Null(matchRepository.Requested);
    }

    // Maçı olmayan hafta "bulunamadı" değildir: boş liste döner, istemci bunu 404'ten ayırt edebilir.
    [Fact]
    public async Task Round_without_matches_returns_an_empty_list()
    {
        var service = new MatchService(
            new FakeCompetitionRepository(TestData.LeagueWithActiveRound()),
            new FakeMatchRepository([]));

        var result = await service.GetByRoundAsync(TestData.TournamentId, roundId: null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Matches);
    }

    // Aynı sorgu hep aynı biçimde gitmelidir; yoksa her istek önbellekte ayrı bir kayıt açar.
    [Fact]
    public async Task Date_range_query_is_normalized_before_reading()
    {
        var matchRepository = new FakeMatchRepository([]);
        var service = new MatchService(new FakeCompetitionRepository(null), matchRepository);

        await service.GetByDateRangeAsync(
            [4, 1, 4],
            new DateTime(2026, 11, 13, 17, 5, 42),
            new DateTime(2026, 11, 13, 23, 59, 59),
            CancellationToken.None);

        var requested = matchRepository.RequestedRange!.Value;
        Assert.Equal([1, 4], requested.TournamentIds);
        Assert.Equal(new DateTime(2026, 11, 13, 17, 5, 0), requested.From);
        Assert.Equal(new DateTime(2026, 11, 13, 23, 59, 0), requested.To);
    }

    [Fact]
    public async Task Match_is_mapped_to_response_without_losing_fields()
    {
        var match = new Match(
            Id: 4821,
            Date: new DateTime(2026, 11, 14, 20, 0, 0),
            Status: new MatchStatus(5, "Maç Sonu", "MS"),
            CurrentMinute: 90,
            Stoppage: 4,
            Home: new MatchSide(new TeamSummary(3, "Galatasaray", "GS", "Galatasaray"), new MatchScore(2, 2, 1, null, null)),
            Away: new MatchSide(new TeamSummary(5, "Fenerbahçe", "FB", "Fenerbahçe"), new MatchScore(1, 1, 0, null, null)),
            StadiumName: "RAMS Park",
            RefereeName: null,
            TournamentId: TestData.TournamentId,
            Stage: new Stage(TestData.StageId, "2026/2027 Sezonu", "26/27"),
            Round: new Round(TestData.ActiveRoundId, "6. Hafta", "6.H"),
            StadiumId: 5574,
            RefereeId: null);
        var service = new MatchService(
            new FakeCompetitionRepository(TestData.LeagueWithActiveRound()),
            new FakeMatchRepository([match]));

        var result = await service.GetByRoundAsync(TestData.TournamentId, roundId: null, CancellationToken.None);

        var dto = Assert.Single(result!.Matches);
        Assert.Equal(4821, dto.Id);
        Assert.Equal(match.Date, dto.Date);
        Assert.Equal("MS", dto.Status.ShortName);
        Assert.Equal(90, dto.CurrentMinute);
        Assert.Equal(4, dto.Stoppage);
        Assert.Equal("Galatasaray", dto.Home.Team.Name);
        Assert.Equal(2, dto.Home.Score.Current);
        Assert.Equal(1, dto.Home.Score.HalfTime);
        Assert.Null(dto.Home.Score.Penalties);
        Assert.Equal("FB", dto.Away.Team.ShortName);
        Assert.Equal(1, dto.Away.Score.Current);
        Assert.Equal("RAMS Park", dto.StadiumName);
        Assert.Null(dto.RefereeName);
        Assert.Equal(TestData.TournamentId, dto.TournamentId);
        Assert.Equal(TestData.ActiveRoundId, dto.RoundId);
        Assert.Equal("26/27", dto.Stage.ShortName);
        Assert.Equal("6. Hafta", dto.Round?.Name);
        Assert.Equal(5574, dto.StadiumId);
        Assert.Null(dto.RefereeId);
    }
}
