namespace TVAPI.Domain.Competitions;

/// <summary>Puan durumunda tek bir takımın satırı.</summary>
public sealed record StandingRow(
    int Position,
    TeamSummary Team,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points,
    int PositionChange);
