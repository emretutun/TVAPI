namespace TVAPI.Domain.Competitions;

/// <summary>Tek bir maç.</summary>
/// <param name="Round">Maçın haftası; kaynakta hafta bilgisi yoksa null.</param>
/// <param name="Date">Kaynakta saat dilimi bilgisi olmadan saklanan maç tarihi ve saati.</param>
public sealed record Match(
    int Id,
    DateTime Date,
    MatchStatus Status,
    int? CurrentMinute,
    int? Stoppage,
    MatchSide Home,
    MatchSide Away,
    string? StadiumName,
    string? RefereeName,
    int TournamentId,
    Stage Stage,
    Round? Round,
    int? StadiumId,
    int? RefereeId);
