namespace TVAPI.Domain.Competitions;

/// <summary>Bir aşamanın haftası (tur).</summary>
public sealed record Round(int Id, string Name, string ShortName);
