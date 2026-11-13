using TVAPI.Application.Competitions;

namespace TVAPI.Application.Matches;

/// <summary>
/// Tek bir maçla ilgili kullanım senaryoları. Bütün metotlarda maç yoksa null döner (404);
/// maç var ama veri henüz oluşmadıysa içi boş bir cevap döner.
/// </summary>
public interface IMatchDetailService
{
    /// <summary>Maçın güncel hali: skor, dakika, durum.</summary>
    Task<MatchDto?> GetByIdAsync(int matchId, CancellationToken cancellationToken);

    Task<MatchLineupDto?> GetLineupAsync(int matchId, CancellationToken cancellationToken);

    Task<MatchEventsDto?> GetEventsAsync(int matchId, CancellationToken cancellationToken);

    Task<MatchStatisticsDto?> GetStatisticsAsync(int matchId, CancellationToken cancellationToken);
}

internal sealed class MatchDetailService : IMatchDetailService
{
    private readonly IMatchRepository _matchRepository;
    private readonly IMatchDetailRepository _detailRepository;

    public MatchDetailService(IMatchRepository matchRepository, IMatchDetailRepository detailRepository)
    {
        _matchRepository = matchRepository;
        _detailRepository = detailRepository;
    }

    public async Task<MatchDto?> GetByIdAsync(int matchId, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetByIdAsync(matchId, cancellationToken);

        return match is null ? null : MatchDto.From(match);
    }

    public async Task<MatchLineupDto?> GetLineupAsync(int matchId, CancellationToken cancellationToken)
    {
        if (!await MatchExistsAsync(matchId, cancellationToken))
        {
            return null;
        }

        var lineup = await _detailRepository.GetLineupAsync(matchId, cancellationToken);

        return MatchLineupDto.From(matchId, lineup);
    }

    public async Task<MatchEventsDto?> GetEventsAsync(int matchId, CancellationToken cancellationToken)
    {
        if (!await MatchExistsAsync(matchId, cancellationToken))
        {
            return null;
        }

        var events = await _detailRepository.GetEventsAsync(matchId, cancellationToken);

        return MatchEventsDto.From(matchId, events);
    }

    public async Task<MatchStatisticsDto?> GetStatisticsAsync(int matchId, CancellationToken cancellationToken)
    {
        if (!await MatchExistsAsync(matchId, cancellationToken))
        {
            return null;
        }

        var statistics = await _detailRepository.GetStatisticsAsync(matchId, cancellationToken);

        return MatchStatisticsDto.From(matchId, statistics);
    }

    // "Böyle bir maç yok" (404) ile "maç var ama kadro/olay/istatistik henüz yok" (boş cevap)
    // istemci için farklı durumlardır; bu yüzden önce maçın varlığına bakılır.
    private async Task<bool> MatchExistsAsync(int matchId, CancellationToken cancellationToken) =>
        await _matchRepository.GetByIdAsync(matchId, cancellationToken) is not null;
}
