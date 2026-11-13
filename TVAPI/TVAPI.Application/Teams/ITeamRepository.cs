using TVAPI.Domain.Teams;

namespace TVAPI.Application.Teams;

public interface ITeamRepository
{
    /// <summary>Bir turnuva aşamasında (sezonda) yer alan takımlar, ada göre sıralı.</summary>
    Task<IReadOnlyList<Team>> GetByStageAsync(int tournamentId, int stageId, CancellationToken cancellationToken);

    /// <summary>Takımın en güncel künyesi; takım yoksa null.</summary>
    Task<Team?> GetByIdAsync(int teamId, CancellationToken cancellationToken);

    /// <summary>Takımın kayıtlı kadro listesi; kayıt yoksa boş kadro.</summary>
    Task<Squad> GetRegisteredSquadAsync(int teamId, CancellationToken cancellationToken);

    /// <summary>
    /// Takımın kadrosu açıklanmış son maçları, en yeni maç başta; en fazla <paramref name="maxMatches"/> maç.
    /// </summary>
    Task<IReadOnlyList<RecentLineup>> GetRecentLineupsAsync(int teamId, int maxMatches, CancellationToken cancellationToken);
}
