namespace TVAPI.Domain.Competitions;

/// <summary>
/// Bir takımın maçtaki skorları. Değer yoksa null kalır:
/// "henüz oynanmadı" ile "0 gol" aynı şey değildir, bu yüzden boş değeri sıfıra çevirmeyiz.
/// </summary>
public sealed record MatchScore(int? Current, int? Regular, int? HalfTime, int? ExtraTime, int? Penalties);
