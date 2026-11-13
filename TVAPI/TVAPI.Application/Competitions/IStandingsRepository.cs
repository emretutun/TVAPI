using TVAPI.Domain.Competitions;

namespace TVAPI.Application.Competitions;

/// <summary>Puan durumu verisine erişim sözleşmesi.</summary>
public interface IStandingsRepository
{
    /// <summary>Verilen turnuva, aşama ve haftanın puan durumunu getirir; kayıt yoksa null döner.</summary>
    Task<Standings?> GetAsync(int tournamentId, int stageId, int roundId, CancellationToken cancellationToken);
}
