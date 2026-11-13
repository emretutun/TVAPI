using TVAPI.Application.Common;
using TVAPI.Infrastructure.Persistence;

namespace TVAPI.Tests.Persistence;

/// <summary>Örnek JSON'lar yerel veritabanındaki gerçek kayıtların kısaltılmış halidir.</summary>
public sealed class MatchDetailJsonParserTests
{
    private const string Source = "maç 2058099";

    private const string LineupJson = """
        {
          "id": 2058099,
          "date": "11/09/2026 17:00:00",
          "homeTeam": {
            "id": 3, "name": "Beşiktaş", "shortName": "BJK", "mediumName": "Beşiktaş",
            "formation": { "id": 7, "name": "4-2-3-1" },
            "manager": { "id": 11, "name": "Ole Gunnar", "surname": "Solskjaer", "knownName": "" },
            "starting": [
              { "id": 1, "knownName": "Ersin Destanoğlu", "knownNameMedium": "Ersin", "knownNameShort": "Ersin",
                "shirtNumber": 30,
                "position": { "id": 1, "name": "Kaleci", "shortName": "K",
                              "tactical": { "id": 1, "name": "Kaleci", "shortName": "GK" } },
                "country": { "id": 1, "name": "Türkiye", "shortName": "TR" } },
              { "id": 2, "knownName": "Orkun Kökçü", "shirtNumber": 10,
                "position": { "id": 3, "name": "Ortasaha", "shortName": "O" } }
            ],
            "bench": [
              { "id": 3, "knownName": "Emirhan Topçu", "shirtNumber": 53 }
            ]
          },
          "awayTeam": {
            "id": 15659, "name": "Erzurumspor FK", "shortName": "EFK", "mediumName": "Erzurumspor FK",
            "manager": { "knownName": "Serkan Özbalta", "name": "Serkan", "surname": "Özbalta" },
            "starting": [],
            "bench": null
          }
        }
        """;

    [Fact]
    public void Lineup_is_mapped_for_both_teams_in_source_order()
    {
        var lineup = MatchDetailJsonParser.ParseLineup(LineupJson, Source);

        Assert.NotNull(lineup);
        Assert.Equal("BJK", lineup.Home.Team.ShortName);
        Assert.Equal("4-2-3-1", lineup.Home.Formation);
        Assert.Equal("Ole Gunnar Solskjaer", lineup.Home.ManagerName); // bilinen ad boş: ad + soyad
        Assert.Equal([1, 2], lineup.Home.Starting.Select(player => player.Id));
        Assert.Equal(30, lineup.Home.Starting[0].ShirtNumber);
        Assert.Equal("Kaleci", lineup.Home.Starting[0].Position?.Name);
        Assert.Equal("GK", lineup.Home.Starting[0].TacticalPosition?.ShortName);
        Assert.Null(lineup.Home.Starting[1].TacticalPosition); // kaynakta yoksa null
        Assert.Equal(53, Assert.Single(lineup.Home.Bench).ShirtNumber);

        Assert.Equal(15659, lineup.Away.Team.Id);
        Assert.Null(lineup.Away.Formation);
        Assert.Equal("Serkan Özbalta", lineup.Away.ManagerName);
        Assert.Empty(lineup.Away.Starting);
        Assert.Empty(lineup.Away.Bench);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("""{ "homeTeam": { "id": 3 } }""")] // takımlardan biri eksik
    public void Lineup_that_is_not_announced_gives_nothing(string json)
    {
        Assert.Null(MatchDetailJsonParser.ParseLineup(json, Source));
    }

    [Fact]
    public void Events_are_mapped_in_source_order()
    {
        // Olaylardaki forma numarası metindir ("10"); maç kadrosunda ise sayıdır.
        const string json = """
            [
              { "id": 23707927, "type": { "id": 39, "name": "Maç Başladı", "shortName": "" },
                "minute": 1, "orderNo": 0, "awayTeamScore": 0, "homeTeamScore": 0 },
              { "id": 23708250, "type": { "id": 23, "name": "Asist", "shortName": "" }, "minute": 21,
                "player": { "id": 216873, "knownName": "Orkun Kökçü", "shirtNumber": "10", "knownNameShort": "Orkun" },
                "teamId": 3, "orderNo": 11, "combineId": "288133918842", "awayTeamScore": 0, "homeTeamScore": 0 },
              { "id": 23708243, "type": { "id": 6, "name": "Gol", "shortName": "G" }, "minute": 21,
                "player": { "id": 206660, "knownName": "Emirhan Topçu", "shirtNumber": "53" },
                "teamId": 3, "orderNo": 11, "combineId": "288133918842", "awayTeamScore": 0, "homeTeamScore": 1 }
            ]
            """;

        var events = MatchDetailJsonParser.ParseEvents(json, Source);

        Assert.Equal([23707927, 23708250, 23708243], events.Select(matchEvent => matchEvent.Id));

        var kickOff = events[0];
        Assert.Equal("Maç Başladı", kickOff.Type.Name);
        Assert.Null(kickOff.TeamId);
        Assert.Null(kickOff.Player);
        Assert.Null(kickOff.CombineId);

        var goal = events[2];
        Assert.Equal(6, goal.Type.Id);
        Assert.Equal("G", goal.Type.ShortName);
        Assert.Equal(21, goal.Minute);
        Assert.Equal(11, goal.OrderNo);
        Assert.Equal(3, goal.TeamId);
        Assert.Equal("Emirhan Topçu", goal.Player?.Name);
        Assert.Equal(53, goal.Player?.ShirtNumber);
        Assert.Equal(1, goal.HomeScore);
        Assert.Equal(0, goal.AwayScore);
        Assert.Equal(events[1].CombineId, goal.CombineId); // gol ile asist aynı değeri taşır
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[ null ]")]
    public void Match_without_events_gives_an_empty_list(string json)
    {
        Assert.Empty(MatchDetailJsonParser.ParseEvents(json, Source));
    }

    [Fact]
    public void Statistics_are_mapped_for_both_teams()
    {
        const string json = """
            {
              "id": 2058099,
              "awayTeam": { "id": 15659, "name": "Erzurumspor FK", "shortName": "EFK", "mediumName": "Erzurumspor FK",
                "statistics": { "fouls": 11, "saves": 4, "corners": 1, "offsides": 4, "redCards": 0, "yellowCards": 0,
                                "shotsOnTarget": 0, "ballPossession": 26, "shotsOffTarget": 8, "secondYellowCards": 0 } },
              "homeTeam": { "id": 3, "name": "Beşiktaş", "shortName": "BJK", "mediumName": "Beşiktaş",
                "statistics": { "fouls": 10, "saves": 0, "corners": 10, "offsides": 2, "redCards": 1, "yellowCards": 3,
                                "shotsOnTarget": 6, "ballPossession": 74, "shotsOffTarget": 15, "secondYellowCards": 1 } }
            }
            """;

        var statistics = MatchDetailJsonParser.ParseStatistics(json, Source);

        Assert.NotNull(statistics);
        Assert.Equal("Beşiktaş", statistics.Home.Team.Name);
        Assert.Equal(74, statistics.Home.BallPossession);
        Assert.Equal(6, statistics.Home.ShotsOnTarget);
        Assert.Equal(15, statistics.Home.ShotsOffTarget);
        Assert.Equal(10, statistics.Home.Fouls);
        Assert.Equal(10, statistics.Home.Corners);
        Assert.Equal(2, statistics.Home.Offsides);
        Assert.Equal(0, statistics.Home.Saves);
        Assert.Equal(3, statistics.Home.YellowCards);
        Assert.Equal(1, statistics.Home.SecondYellowCards);
        Assert.Equal(1, statistics.Home.RedCards);
        Assert.Equal(26, statistics.Away.BallPossession);
        Assert.Equal(4, statistics.Away.Saves);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    public void Missing_statistics_give_nothing(string json)
    {
        Assert.Null(MatchDetailJsonParser.ParseStatistics(json, Source));
    }

    [Fact]
    public void Corrupt_json_is_reported_with_the_kind_of_data_and_the_match()
    {
        var lineup = Assert.Throws<InvalidSourceDataException>(() => MatchDetailJsonParser.ParseLineup("{ yarım", Source));
        var events = Assert.Throws<InvalidSourceDataException>(() => MatchDetailJsonParser.ParseEvents("{}", Source));
        var statistics = Assert.Throws<InvalidSourceDataException>(() => MatchDetailJsonParser.ParseStatistics("[]", Source));

        Assert.Contains("Maç kadrosu", lineup.Message);
        Assert.Contains("Maç olayları", events.Message);
        Assert.Contains("Maç istatistikleri", statistics.Message);
        Assert.All([lineup, events, statistics], exception => Assert.Contains(Source, exception.Message));
    }
}
