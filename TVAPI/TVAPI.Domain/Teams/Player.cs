namespace TVAPI.Domain.Teams;

/// <param name="Position">Genel mevki: kaleci, defans, orta saha, forvet.</param>
/// <param name="TacticalPosition">
/// Oyuncunun o maçtaki sahadaki yeri (sol bek, ön libero...). Yalnızca maç kadrosunda bulunur;
/// takım kadrosunda ve maç olaylarında null'dır.
/// </param>
public sealed record Player(
    int Id,
    string Name,
    string MediumName,
    string ShortName,
    int? ShirtNumber,
    PlayerPosition? Position,
    Country? Country,
    PlayerPosition? TacticalPosition = null);

public sealed record PlayerPosition(int Id, string Name, string ShortName);

/// <summary>Takımın bir maçta kadroya yazdığı oyuncular (ilk 11 ve yedekler birlikte).</summary>
public sealed record RecentLineup(int MatchId, DateTime MatchDate, IReadOnlyList<Player> Players);

/// <summary>Bir takımın kadrosu.</summary>
public sealed record Squad(IReadOnlyList<Player> Players)
{
    /// <summary>Kadro çıkarılırken en fazla kaç maça bakılacağı.</summary>
    public const int MaxRecentMatches = 15;

    // Futbol sezonu temmuzda başlar; kadro çıkarılırken önce bu sezonun maçlarına bakılır.
    private const int SeasonStartMonth = 7;

    // Mevkisi ya da forma numarası bilinmeyen oyuncular kendi grubunun sonuna düşer.
    private const int UnknownPositionOrder = int.MaxValue;
    private const int UnknownShirtNumberOrder = int.MaxValue;

    public bool IsEmpty => Players.Count == 0;

    /// <summary>
    /// Kadroyu takımın son maçlarında kadroya yazılan oyunculardan çıkarır. Kayıtlı kadro listesi
    /// transferlerden sonra geç güncellenebildiği için maç kadroları daha günceldir.
    /// Kural eski NTR HTSPOR'dakinin aynısıdır: bu sezonun maçı varsa yalnızca onlar, yoksa eldeki maçlar;
    /// en yeni <see cref="MaxRecentMatches"/> maç; bir oyuncunun en yeni maçtaki bilgisi geçerlidir.
    /// </summary>
    /// <param name="lineups">Takımın maç kadroları, en yeni maç başta.</param>
    /// <param name="today">Sezonun hesaplanacağı gün.</param>
    public static Squad FromRecentLineups(IReadOnlyList<RecentLineup> lineups, DateOnly today)
    {
        var seasonStart = new DateTime(today.Month >= SeasonStartMonth ? today.Year : today.Year - 1, SeasonStartMonth, 1);
        var thisSeason = lineups.Where(lineup => lineup.MatchDate >= seasonStart).ToList();

        var players = (thisSeason.Count > 0 ? thisSeason : lineups)
            .Take(MaxRecentMatches)
            .SelectMany(lineup => lineup.Players)
            .DistinctBy(player => player.Id)
            .ToList();

        return new Squad(players);
    }

    /// <summary>
    /// Kadroyu yayında kullanılan sıraya dizer: önce mevki (kaleci, defans, orta saha, forvet),
    /// aynı mevkide forma numarası. Eski NTR HTSPOR'daki sıralamanın aynısıdır.
    /// </summary>
    public Squad Ordered() => new(
        Players
            .OrderBy(player => player.Position is { Id: > 0 } position ? position.Id : UnknownPositionOrder)
            .ThenBy(player => player.ShirtNumber ?? UnknownShirtNumberOrder)
            .ThenBy(player => player.Id)
            .ToList());
}
