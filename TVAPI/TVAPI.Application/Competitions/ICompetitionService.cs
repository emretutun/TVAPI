namespace TVAPI.Application.Competitions;

/// <summary>Turnuva kullanım senaryoları. Controller yalnızca bu arayüzü tanır.</summary>
public interface ICompetitionService
{
    /// <summary>Turnuvanın aktif aşamasını ve haftasını getirir; bulunamazsa null döner.</summary>
    Task<CurrentCompetitionDto?> GetCurrentAsync(int tournamentId, CancellationToken cancellationToken);

    /// <summary>Kaynakta kayıtlı bütün turnuvaları getirir.</summary>
    Task<IReadOnlyList<TournamentDto>> GetTournamentsAsync(CancellationToken cancellationToken);
}
