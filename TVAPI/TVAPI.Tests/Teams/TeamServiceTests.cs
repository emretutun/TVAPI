using TVAPI.Application.Teams;
using TVAPI.Domain.Teams;
using TVAPI.Tests.Competitions;

namespace TVAPI.Tests.Teams;

public sealed class TeamServiceTests
{
    private static readonly Team Amed = new(
        Id: 53,
        Name: "Amed Sportif Faaliyetler",
        ShortName: "ASF",
        MediumName: "Amed Sportif",
        CoachName: null,
        Country: new Country(1, "Türkiye", "TR"),
        Stadium: new Stadium(6531, "Diyarbakır Stadyumu", "Diyarbakır", "Diyarbakır"));

    [Fact]
    public async Task Teams_are_read_for_the_active_stage_of_the_tournament()
    {
        var teamRepository = new FakeTeamRepository { Teams = [Amed] };
        var service = NewService(new FakeCompetitionRepository(TestData.LeagueWithActiveRound()), teamRepository);

        var result = await service.GetByTournamentAsync(TestData.TournamentId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal((TestData.TournamentId, TestData.StageId), teamRepository.RequestedStage);
        Assert.Equal(TestData.StageId, result.Stage.Id);
        var team = Assert.Single(result.Teams);
        Assert.Equal("ASF", team.ShortName);
        Assert.Null(team.CoachName);
        Assert.Equal("Diyarbakır", team.Stadium?.CityName);
    }

    // Takım listesi haftaya bağlı değildir; haftasız turnuvada (kupa) da çalışır.
    [Fact]
    public async Task Teams_are_available_for_tournaments_without_rounds()
    {
        var service = NewService(
            new FakeCompetitionRepository(TestData.CupWithoutRounds()),
            new FakeTeamRepository { Teams = [Amed] });

        var result = await service.GetByTournamentAsync(TestData.TournamentId, CancellationToken.None);

        Assert.Single(result!.Teams);
    }

    [Fact]
    public async Task Unknown_tournament_returns_nothing_without_reading_teams()
    {
        var teamRepository = new FakeTeamRepository();
        var service = NewService(new FakeCompetitionRepository(null), teamRepository);

        var result = await service.GetByTournamentAsync(999, CancellationToken.None);

        Assert.Null(result);
        Assert.Null(teamRepository.RequestedStage);
    }

    [Fact]
    public async Task Unknown_team_returns_nothing()
    {
        var service = NewService(new FakeCompetitionRepository(null), new FakeTeamRepository());

        Assert.Null(await service.GetByIdAsync(404, CancellationToken.None));
    }

    [Fact]
    public async Task Squad_of_unknown_team_returns_nothing_without_reading_the_squad()
    {
        var teamRepository = new FakeTeamRepository();
        var service = NewService(new FakeCompetitionRepository(null), teamRepository);

        var result = await service.GetSquadAsync(404, CancellationToken.None);

        Assert.Null(result);
        Assert.False(teamRepository.SquadRequested);
    }

    // "Takım yok" ile "takım var, kadrosu girilmemiş" ayrı durumlardır: ilki null (404), ikincisi boş liste.
    [Fact]
    public async Task Known_team_without_squad_returns_an_empty_list()
    {
        var service = NewService(
            new FakeCompetitionRepository(null),
            new FakeTeamRepository { Team = Amed });

        var result = await service.GetSquadAsync(Amed.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(Amed.Id, result.TeamId);
        Assert.Empty(result.Players);
    }

    [Fact]
    public async Task Squad_comes_from_recent_match_lineups_when_there_are_any()
    {
        var teamRepository = new FakeTeamRepository
        {
            Team = Amed,
            RecentLineups = [new RecentLineup(1, new DateTime(2026, 11, 8), [NewPlayer(7, positionId: 3, shirtNumber: 10)])],
            Squad = new Squad([NewPlayer(99, positionId: 1, shirtNumber: 1)]),
        };
        var service = NewService(new FakeCompetitionRepository(null), teamRepository);

        var result = await service.GetSquadAsync(Amed.Id, CancellationToken.None);

        Assert.Equal("matchLineups", result!.Source);
        Assert.Equal(7, Assert.Single(result.Players).Id);
        Assert.False(teamRepository.SquadRequested); // kayıtlı kadro listesine hiç gidilmedi
    }

    [Fact]
    public async Task Squad_falls_back_to_registered_squad_when_there_are_no_match_lineups()
    {
        var service = NewService(
            new FakeCompetitionRepository(null),
            new FakeTeamRepository { Team = Amed, Squad = new Squad([NewPlayer(99, positionId: 1, shirtNumber: 1)]) });

        var result = await service.GetSquadAsync(Amed.Id, CancellationToken.None);

        Assert.Equal("registeredSquad", result!.Source);
        Assert.Equal(99, Assert.Single(result.Players).Id);
    }

    [Fact]
    public async Task Squad_is_returned_in_broadcast_order()
    {
        var service = NewService(
            new FakeCompetitionRepository(null),
            new FakeTeamRepository
            {
                Team = Amed,
                Squad = new Squad([NewPlayer(1, positionId: 4, shirtNumber: 9), NewPlayer(2, positionId: 1, shirtNumber: 22)]),
            });

        var result = await service.GetSquadAsync(Amed.Id, CancellationToken.None);

        Assert.Equal([2, 1], result!.Players.Select(player => player.Id));
    }

    // Servis "bugün"ü saatten okur; testte saat sabittir (14 Kasım 2026, sezon 1 Temmuz 2026'da başladı).
    private static TeamService NewService(FakeCompetitionRepository competitions, FakeTeamRepository teams) =>
        new(competitions, teams, new FixedTimeProvider(new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero)));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    internal static Player NewPlayer(int id, int? positionId, int? shirtNumber) => new(
        id,
        Name: $"Oyuncu {id}",
        MediumName: $"Oyuncu {id}",
        ShortName: $"O{id}",
        shirtNumber,
        positionId is null ? null : new PlayerPosition(positionId.Value, "Mevki", "M"),
        Country: null);

    private sealed class FakeTeamRepository : ITeamRepository
    {
        public IReadOnlyList<Team> Teams { get; init; } = [];
        public Team? Team { get; init; }
        public Squad Squad { get; init; } = new([]);
        public IReadOnlyList<RecentLineup> RecentLineups { get; init; } = [];

        public (int TournamentId, int StageId)? RequestedStage { get; private set; }
        public bool SquadRequested { get; private set; }

        public Task<IReadOnlyList<Team>> GetByStageAsync(int tournamentId, int stageId, CancellationToken cancellationToken)
        {
            RequestedStage = (tournamentId, stageId);
            return Task.FromResult(Teams);
        }

        public Task<Team?> GetByIdAsync(int teamId, CancellationToken cancellationToken) => Task.FromResult(Team);

        public Task<Squad> GetRegisteredSquadAsync(int teamId, CancellationToken cancellationToken)
        {
            SquadRequested = true;
            return Task.FromResult(Squad);
        }

        public Task<IReadOnlyList<RecentLineup>> GetRecentLineupsAsync(
            int teamId,
            int maxMatches,
            CancellationToken cancellationToken) =>
            Task.FromResult(RecentLineups);
    }
}
