using TVAPI.Application.Common;
using TVAPI.Infrastructure.Persistence;

namespace TVAPI.Tests.Persistence;

public sealed class SquadJsonParserTests
{
    private const string Source = "takım 53";

    [Fact]
    public void Player_fields_are_mapped()
    {
        const string json = """
            [
              {
                "player": {
                  "id": 264550,
                  "knownName": "Bartu Kaya",
                  "knownNameMedium": "Bartu",
                  "knownNameShort": "B. Kaya",
                  "shirtNumber": 22,
                  "position": { "id": 1, "name": "Kaleci", "shortName": "K" },
                  "country": { "id": 1, "name": "Türkiye", "shortName": "TR" }
                },
                "tournaments": [ { "tournamentInf": { "id": 1 } } ]
              }
            ]
            """;

        var player = Assert.Single(SquadJsonParser.Parse(json, Source).Players);

        Assert.Equal(264550, player.Id);
        Assert.Equal("Bartu Kaya", player.Name);
        Assert.Equal("Bartu", player.MediumName);
        Assert.Equal("B. Kaya", player.ShortName);
        Assert.Equal(22, player.ShirtNumber);
        Assert.Equal("Kaleci", player.Position?.Name);
        Assert.Equal("TR", player.Country?.ShortName);
    }

    // Forma numarası kaynakta sayı da metin de olabilir; tanınmayan her değer "numara yok" sayılır.
    [Theory]
    [InlineData("7", 7)]
    [InlineData("\"7\"", 7)]
    [InlineData("\"\"", null)]
    [InlineData("null", null)]
    [InlineData("0", null)]
    [InlineData("\"yok\"", null)]
    public void Shirt_number_is_read_leniently(string rawValue, int? expected)
    {
        var json = $$"""[ { "player": { "id": 1, "knownName": "Oyuncu", "shirtNumber": {{rawValue}} } } ]""";

        var player = Assert.Single(SquadJsonParser.Parse(json, Source).Players);

        Assert.Equal(expected, player.ShirtNumber);
    }

    [Fact]
    public void Missing_short_names_fall_back_to_longer_name()
    {
        const string json = """[ { "player": { "id": 1, "knownName": "Hasan Ali Kaldırım" } } ]""";

        var player = Assert.Single(SquadJsonParser.Parse(json, Source).Players);

        Assert.Equal("Hasan Ali Kaldırım", player.MediumName);
        Assert.Equal("Hasan Ali Kaldırım", player.ShortName);
        Assert.Null(player.ShirtNumber);
        Assert.Null(player.Position);
        Assert.Null(player.Country);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("[ null, { } ]")] // oyuncusu olmayan kayıtlar atlanır
    public void Empty_or_playerless_data_gives_an_empty_squad(string json)
    {
        Assert.Empty(SquadJsonParser.Parse(json, Source).Players);
    }

    [Theory]
    [InlineData("bu json değil")]
    [InlineData("""{ "player": { "id": 1 } }""")]        // liste yerine tek nesne
    [InlineData("""[ { "player": { "id": "bir" } } ]""")] // sayı yerine metin
    public void Corrupt_json_is_reported_as_invalid_source_data(string json)
    {
        var exception = Assert.Throws<InvalidSourceDataException>(() => SquadJsonParser.Parse(json, Source));

        Assert.Contains(Source, exception.Message);
    }
}
