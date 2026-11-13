namespace TVAPI.Domain.Competitions;

/// <summary>
/// Bir turnuvanın şu anki durumu: aktif aşama ve aktif hafta.
/// Haftasız turnuvalarda (örnek: bazı kupalar) aktif hafta olmayabilir; o durumda <see cref="Round"/> null olur.
/// </summary>
public sealed record CurrentCompetition(Tournament Tournament, Stage Stage, Round? Round)
{
    public bool HasRound => Round is not null;

    /// <summary>
    /// İş kuralı: istemci hafta belirtmediyse aktif hafta kullanılır.
    /// İkisi de yoksa (haftasız turnuva) null döner.
    /// </summary>
    public int? ResolveRoundId(int? requestedRoundId) => requestedRoundId ?? Round?.Id;
}
