namespace TVAPI.Domain.Competitions;

/// <summary>Bir takımın kimliği ve adları (uzun, orta, kısa).</summary>
public sealed record TeamSummary(int Id, string Name, string ShortName, string MediumName);
