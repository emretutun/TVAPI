using TVAPI.Domain.Competitions;

namespace TVAPI.Domain.Matches;

public sealed record MatchStatistics(TeamStatistics Home, TeamStatistics Away);

/// <param name="BallPossession">Topa sahip olma yüzdesi (0-100).</param>
public sealed record TeamStatistics(
    TeamSummary Team,
    int BallPossession,
    int ShotsOnTarget,
    int ShotsOffTarget,
    int Fouls,
    int Corners,
    int Offsides,
    int Saves,
    int YellowCards,
    int SecondYellowCards,
    int RedCards);
