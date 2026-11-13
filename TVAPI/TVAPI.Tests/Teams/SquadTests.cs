using TVAPI.Domain.Teams;

namespace TVAPI.Tests.Teams;

public sealed class SquadTests
{
    [Fact]
    public void Players_are_ordered_by_position_then_shirt_number()
    {
        var squad = new Squad(
        [
            TeamServiceTests.NewPlayer(1, positionId: 4, shirtNumber: 9),   // forvet
            TeamServiceTests.NewPlayer(2, positionId: 2, shirtNumber: 15),  // defans
            TeamServiceTests.NewPlayer(3, positionId: 1, shirtNumber: 31),  // kaleci
            TeamServiceTests.NewPlayer(4, positionId: 2, shirtNumber: 3),   // defans
            TeamServiceTests.NewPlayer(5, positionId: 1, shirtNumber: 1),   // kaleci
        ]);

        Assert.Equal([5, 3, 4, 2, 1], squad.Ordered().Players.Select(player => player.Id));
    }

    [Fact]
    public void Players_with_unknown_position_or_number_go_last()
    {
        var squad = new Squad(
        [
            TeamServiceTests.NewPlayer(1, positionId: null, shirtNumber: 5),
            TeamServiceTests.NewPlayer(2, positionId: 0, shirtNumber: 4),    // mevki id'si 0: bilinmiyor
            TeamServiceTests.NewPlayer(3, positionId: 1, shirtNumber: null),
            TeamServiceTests.NewPlayer(4, positionId: 1, shirtNumber: 12),
        ]);

        Assert.Equal([4, 3, 2, 1], squad.Ordered().Players.Select(player => player.Id));
    }

    [Fact]
    public void Ordering_does_not_change_the_original_squad()
    {
        var squad = new Squad(
        [
            TeamServiceTests.NewPlayer(1, positionId: 4, shirtNumber: 9),
            TeamServiceTests.NewPlayer(2, positionId: 1, shirtNumber: 1),
        ]);

        squad.Ordered();

        Assert.Equal([1, 2], squad.Players.Select(player => player.Id));
    }
}
