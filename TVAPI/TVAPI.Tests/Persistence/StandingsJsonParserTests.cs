using TVAPI.Application.Common;
using TVAPI.Infrastructure.Persistence;

namespace TVAPI.Tests.Persistence;

public sealed class StandingsJsonParserTests
{
    private const string Source = "turnuva 1, aşama 62999, hafta 6";

    // Alan adları ve biçim, yerel veritabanındaki gerçek kayıtla aynıdır.
    private const string ValidJson = """
        {
          "overall": [
            {
              "position": 1,
              "team": { "id": 3, "name": "Galatasaray", "shortName": "GS", "mediumName": "Galatasaray" },
              "played": 6, "won": 5, "draw": 1, "lost": 0,
              "scored": 14, "against": 4, "average": 10, "points": 16, "delta": -1
            },
            {
              "position": 2,
              "team": { "id": 5, "name": "Fenerbahçe", "shortName": "FB", "mediumName": "Fenerbahçe" },
              "played": 6, "won": 4, "draw": 1, "lost": 1,
              "scored": 11, "against": 6, "average": 5, "points": 13, "delta": 1
            }
          ],
          "home": [],
          "away": []
        }
        """;

    [Fact]
    public void Source_fields_are_mapped_to_domain_names()
    {
        var standings = StandingsJsonParser.Parse(ValidJson, Source);

        Assert.Equal(2, standings.Overall.Count);
        Assert.Empty(standings.Home);
        Assert.Empty(standings.Away);

        var leader = standings.Overall[0];
        Assert.Equal(1, leader.Position);
        Assert.Equal(3, leader.Team.Id);
        Assert.Equal("Galatasaray", leader.Team.Name);
        Assert.Equal("GS", leader.Team.ShortName);
        Assert.Equal(6, leader.Played);
        Assert.Equal(5, leader.Won);
        Assert.Equal(1, leader.Drawn);
        Assert.Equal(0, leader.Lost);
        Assert.Equal(14, leader.GoalsFor);        // kaynak: scored
        Assert.Equal(4, leader.GoalsAgainst);     // kaynak: against
        Assert.Equal(10, leader.GoalDifference);  // kaynak: average
        Assert.Equal(16, leader.Points);
        Assert.Equal(-1, leader.PositionChange);  // kaynak: delta
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("""{ "overall": null }""")]
    public void Missing_tables_become_empty_lists(string json)
    {
        var standings = StandingsJsonParser.Parse(json, Source);

        Assert.Empty(standings.Overall);
        Assert.Empty(standings.Home);
        Assert.Empty(standings.Away);
    }

    [Fact]
    public void Row_without_team_does_not_break_the_table()
    {
        var standings = StandingsJsonParser.Parse("""{ "overall": [ { "position": 1, "points": 3 } ] }""", Source);

        var row = Assert.Single(standings.Overall);
        Assert.Equal(0, row.Team.Id);
        Assert.Equal(string.Empty, row.Team.Name);
        Assert.Equal(3, row.Points);
    }

    [Theory]
    [InlineData("bu json değil")]
    [InlineData("""{ "overall": [ { "position": 1, """)]      // yarıda kesilmiş kayıt
    [InlineData("""{ "overall": "tablo yok" }""")]            // liste yerine metin
    [InlineData("""{ "overall": [ { "points": "on altı" } ] }""")] // sayı yerine metin
    public void Corrupt_json_is_reported_as_invalid_source_data(string json)
    {
        var exception = Assert.Throws<InvalidSourceDataException>(() => StandingsJsonParser.Parse(json, Source));

        // Hata mesajı hangi kaydın bozuk olduğunu söylemelidir; logda aranacak bilgi budur.
        Assert.Contains(Source, exception.Message);
    }
}
