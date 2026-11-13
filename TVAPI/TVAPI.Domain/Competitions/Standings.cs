namespace TVAPI.Domain.Competitions;

/// <summary>
/// Bir haftanın puan durumu: genel, iç saha ve deplasman tabloları.
/// </summary>
public sealed record Standings(
    IReadOnlyList<StandingRow> Overall,
    IReadOnlyList<StandingRow> Home,
    IReadOnlyList<StandingRow> Away);
