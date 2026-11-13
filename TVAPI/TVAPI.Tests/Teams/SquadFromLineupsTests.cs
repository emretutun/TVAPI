using TVAPI.Domain.Teams;

namespace TVAPI.Tests.Teams;

/// <summary>Kadronun son maç kadrolarından çıkarılması. "Bugün" 14 Kasım 2026; sezon 1 Temmuz 2026'da başladı.</summary>
public sealed class SquadFromLineupsTests
{
    private static readonly DateOnly Today = new(2026, 11, 14);

    [Fact]
    public void Players_of_recent_matches_are_merged_without_duplicates()
    {
        var squad = Squad.FromRecentLineups(
            [
                Lineup(matchId: 2, new DateTime(2026, 11, 8), players: [1, 2, 3]),
                Lineup(matchId: 1, new DateTime(2026, 11, 1), players: [2, 3, 4]),
            ],
            Today);

        Assert.Equal([1, 2, 3, 4], squad.Players.Select(player => player.Id));
    }

    // Aynı oyuncunun forma numarası sezon içinde değişebilir; en yeni maçtaki bilgi geçerlidir.
    [Fact]
    public void Newest_match_wins_when_a_player_appears_more_than_once()
    {
        var squad = Squad.FromRecentLineups(
            [
                new RecentLineup(2, new DateTime(2026, 11, 8), [TeamServiceTests.NewPlayer(7, positionId: 3, shirtNumber: 10)]),
                new RecentLineup(1, new DateTime(2026, 11, 1), [TeamServiceTests.NewPlayer(7, positionId: 3, shirtNumber: 99)]),
            ],
            Today);

        Assert.Equal(10, Assert.Single(squad.Players).ShirtNumber);
    }

    // Geçen sezon oynayıp ayrılan oyuncular kadroya girmemelidir.
    [Fact]
    public void Only_this_season_is_used_when_it_has_matches()
    {
        var squad = Squad.FromRecentLineups(
            [
                Lineup(matchId: 2, new DateTime(2026, 8, 15), players: [1, 2]),
                Lineup(matchId: 1, new DateTime(2026, 5, 20), players: [2, 99]), // geçen sezon
            ],
            Today);

        Assert.Equal([1, 2], squad.Players.Select(player => player.Id));
    }

    [Fact]
    public void Older_matches_are_used_when_this_season_has_none()
    {
        var squad = Squad.FromRecentLineups(
            [Lineup(matchId: 1, new DateTime(2026, 5, 20), players: [2, 99])],
            Today);

        Assert.Equal([2, 99], squad.Players.Select(player => player.Id));
    }

    // Ocak ayında sezon bir önceki yılın temmuzunda başlamıştır.
    [Fact]
    public void Season_started_last_year_when_today_is_before_july()
    {
        var squad = Squad.FromRecentLineups(
            [
                Lineup(matchId: 2, new DateTime(2026, 12, 20), players: [1]),
                Lineup(matchId: 1, new DateTime(2026, 6, 1), players: [99]), // 2025/26 sezonu
            ],
            today: new DateOnly(2027, 1, 10));

        Assert.Equal([1], squad.Players.Select(player => player.Id));
    }

    [Fact]
    public void At_most_fifteen_matches_are_used()
    {
        // 16 maç: her maçta yalnızca o maça özgü bir oyuncu var; en eski maçın oyuncusu dışarıda kalmalı.
        var lineups = Enumerable.Range(1, 16)
            .Select(index => Lineup(matchId: index, new DateTime(2026, 11, 14).AddDays(-index), players: [index]))
            .ToList();

        var squad = Squad.FromRecentLineups(lineups, Today);

        Assert.Equal(Squad.MaxRecentMatches, squad.Players.Count);
        Assert.DoesNotContain(squad.Players, player => player.Id == 16);
    }

    [Fact]
    public void No_lineups_give_an_empty_squad()
    {
        Assert.True(Squad.FromRecentLineups([], Today).IsEmpty);
    }

    private static RecentLineup Lineup(int matchId, DateTime date, int[] players) => new(
        matchId,
        date,
        players.Select(id => TeamServiceTests.NewPlayer(id, positionId: 3, shirtNumber: id)).ToList());
}
