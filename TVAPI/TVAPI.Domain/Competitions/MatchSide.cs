namespace TVAPI.Domain.Competitions;

/// <summary>Maçın bir tarafı: takım ve o takımın skorları.</summary>
public sealed record MatchSide(TeamSummary Team, MatchScore Score);
