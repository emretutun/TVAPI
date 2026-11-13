using TVAPI.Domain.Matches;

namespace TVAPI.Application.Matches;

/// <summary>Tek bir maçın ayrıntı verisine (kadro, olaylar, istatistik) erişim sözleşmesi.</summary>
public interface IMatchDetailRepository
{
    /// <summary>Maçın kadroları; henüz açıklanmadıysa null.</summary>
    Task<MatchLineup?> GetLineupAsync(int matchId, CancellationToken cancellationToken);

    /// <summary>Maçın olayları, kaynaktaki (oluş) sırasıyla; olay yoksa boş liste.</summary>
    Task<IReadOnlyList<MatchEvent>> GetEventsAsync(int matchId, CancellationToken cancellationToken);

    /// <summary>Maçın istatistikleri; henüz yoksa null.</summary>
    Task<MatchStatistics?> GetStatisticsAsync(int matchId, CancellationToken cancellationToken);
}
