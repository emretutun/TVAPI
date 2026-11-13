using TVAPI.Domain.Competitions;
using TVAPI.Domain.Teams;

namespace TVAPI.Domain.Matches;

/// <summary>Bir maçın açıklanmış kadroları.</summary>
public sealed record MatchLineup(TeamLineup Home, TeamLineup Away);

/// <summary>Bir takımın maç kadrosu: ilk 11 ve yedekler, kaynaktaki sırayla.</summary>
public sealed record TeamLineup(
    TeamSummary Team,
    string? Formation,
    string? ManagerName,
    IReadOnlyList<Player> Starting,
    IReadOnlyList<Player> Bench);
