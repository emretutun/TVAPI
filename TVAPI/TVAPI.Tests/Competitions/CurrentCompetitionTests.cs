namespace TVAPI.Tests.Competitions;

public sealed class CurrentCompetitionTests
{
    [Fact]
    public void Requested_round_wins_over_active_round()
    {
        Assert.Equal(3, TestData.LeagueWithActiveRound().ResolveRoundId(3));
    }

    [Fact]
    public void Active_round_is_used_when_nothing_is_requested()
    {
        Assert.Equal(TestData.ActiveRoundId, TestData.LeagueWithActiveRound().ResolveRoundId(null));
    }

    [Fact]
    public void Tournament_without_rounds_has_no_round_to_fall_back_to()
    {
        var cup = TestData.CupWithoutRounds();

        Assert.False(cup.HasRound);
        Assert.Null(cup.ResolveRoundId(null));
    }
}
