namespace TVAPI.Domain.Competitions;

/// <summary>Bir turnuvanın aşaması. Lig için sezon anlamına gelir (örnek: 2026/2027).</summary>
public sealed record Stage(int Id, string Name, string ShortName);
