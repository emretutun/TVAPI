namespace TVAPI.Application.Competitions;

/// <summary>Maç kullanım senaryoları.</summary>
public interface IMatchService
{
    /// <summary>
    /// Turnuvanın aktif aşamasındaki bir haftanın maçlarını getirir.
    /// <paramref name="roundId"/> verilmezse aktif hafta kullanılır.
    /// Turnuva ya da hafta bulunamazsa null döner; hafta var ama maç yoksa boş liste döner.
    /// </summary>
    Task<MatchListDto?> GetByRoundAsync(int tournamentId, int? roundId, CancellationToken cancellationToken);

    /// <summary>
    /// Verilen turnuvaların, tarihi [<paramref name="from"/>, <paramref name="to"/>) aralığına düşen maçlarını getirir.
    /// Haftası olmayan turnuvalar (hazırlık maçları gibi) ve birden çok turnuvayı kapsayan skor bantları içindir.
    /// </summary>
    Task<IReadOnlyList<MatchDto>> GetByDateRangeAsync(
        IReadOnlyCollection<int> tournamentIds,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);
}
