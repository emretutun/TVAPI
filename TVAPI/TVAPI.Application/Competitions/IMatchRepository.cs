using TVAPI.Domain.Competitions;

namespace TVAPI.Application.Competitions;

/// <summary>Maç verisine erişim sözleşmesi.</summary>
public interface IMatchRepository
{
    /// <summary>
    /// Verilen haftanın maçlarını tarih sırasıyla getirir.
    /// Başka bir haftaya taşınmış (daha güncel kaydı başka haftada olan) maçlar listeye girmez.
    /// </summary>
    Task<IReadOnlyList<Match>> GetByRoundAsync(int tournamentId, int stageId, int roundId, CancellationToken cancellationToken);

    /// <summary>Maçın en güncel kaydı; maç yoksa null.</summary>
    Task<Match?> GetByIdAsync(int matchId, CancellationToken cancellationToken);

    /// <summary>
    /// Verilen turnuvaların, tarihi [<paramref name="from"/>, <paramref name="to"/>) aralığına düşen maçları; tarih sırasıyla.
    /// </summary>
    Task<IReadOnlyList<Match>> GetByDateRangeAsync(
        IReadOnlyCollection<int> tournamentIds,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);
}
