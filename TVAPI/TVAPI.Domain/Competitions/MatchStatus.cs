namespace TVAPI.Domain.Competitions;

/// <summary>Maçın durumu (örnek: başlamadı, ilk yarı, bitti). Kimlik ve adlar kaynaktan olduğu gibi gelir.</summary>
public sealed record MatchStatus(int Id, string Name, string ShortName);
