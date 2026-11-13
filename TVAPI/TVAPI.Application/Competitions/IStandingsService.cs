namespace TVAPI.Application.Competitions;

/// <summary>Puan durumu kullanım senaryoları.</summary>
public interface IStandingsService
{
    /// <summary>
    /// Turnuvanın aktif aşamasındaki puan durumunu getirir.
    /// <paramref name="roundId"/> verilmezse aktif hafta kullanılır.
    /// Turnuva, hafta ya da puan durumu bulunamazsa null döner.
    /// </summary>
    Task<StandingsDto?> GetAsync(int tournamentId, int? roundId, CancellationToken cancellationToken);
}
