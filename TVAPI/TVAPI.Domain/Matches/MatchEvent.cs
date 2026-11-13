using TVAPI.Domain.Teams;

namespace TVAPI.Domain.Matches;

/// <summary>
/// Maç içinde olan tek bir olay: gol, kart, oyuncu değişikliği, devre başı/sonu...
/// </summary>
/// <param name="OrderNo">Kaynağın olaya verdiği sıra numarası (aynı dakikadaki olaylar için).</param>
/// <param name="TeamId">Olayın ait olduğu takım; maçın geneline ait olaylarda (maç başladı gibi) null.</param>
/// <param name="Player">Olaydaki oyuncu; oyuncusuz olaylarda null.</param>
/// <param name="HomeScore">Olaydan sonraki ev sahibi skoru.</param>
/// <param name="AwayScore">Olaydan sonraki deplasman skoru.</param>
/// <param name="CombineId">Birbirine bağlı olayları eşler (örnek: gol ile asisti aynı değeri taşır).</param>
public sealed record MatchEvent(
    int Id,
    MatchEventType Type,
    int Minute,
    int OrderNo,
    int? TeamId,
    Player? Player,
    int HomeScore,
    int AwayScore,
    string? CombineId);

public sealed record MatchEventType(int Id, string Name, string ShortName);
