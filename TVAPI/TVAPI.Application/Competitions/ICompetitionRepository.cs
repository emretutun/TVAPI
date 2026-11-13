using TVAPI.Domain.Competitions;

namespace TVAPI.Application.Competitions;

/// <summary>
/// Turnuva verisine erişim sözleşmesi.
/// Arayüz Application katmanında durur, gerçek hali (MySQL) Infrastructure katmanındadır.
/// Böylece iş kuralları veritabanını tanımaz.
/// </summary>
public interface ICompetitionRepository
{
    /// <summary>Turnuvanın aktif aşamasını ve haftasını getirir; turnuva ya da aktif aşama yoksa null döner.</summary>
    Task<CurrentCompetition?> GetCurrentAsync(int tournamentId, CancellationToken cancellationToken);

    /// <summary>Kaynakta kayıtlı bütün futbol turnuvaları, id sırasıyla.</summary>
    Task<IReadOnlyList<Tournament>> GetTournamentsAsync(CancellationToken cancellationToken);
}
