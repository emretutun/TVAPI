using TVAPI.Application.Competitions;
using TVAPI.Application.Teams;
using TVAPI.Domain.Competitions;
using TVAPI.Domain.Matches;

namespace TVAPI.Application.Matches;

/// <summary>
/// Maç kadroları. Kadro henüz açıklanmadıysa <see cref="Home"/> ve <see cref="Away"/> null gelir.
/// </summary>
public sealed record MatchLineupDto(int MatchId, TeamLineupDto? Home, TeamLineupDto? Away)
{
    public static MatchLineupDto From(int matchId, MatchLineup? lineup) => new(
        matchId,
        lineup is null ? null : TeamLineupDto.From(lineup.Home),
        lineup is null ? null : TeamLineupDto.From(lineup.Away));
}

public sealed record TeamLineupDto(
    TeamDto Team,
    string? Formation,
    string? ManagerName,
    IReadOnlyList<PlayerDto> Starting,
    IReadOnlyList<PlayerDto> Bench)
{
    public static TeamLineupDto From(TeamLineup lineup) => new(
        TeamDtoMapper.From(lineup.Team),
        lineup.Formation,
        lineup.ManagerName,
        lineup.Starting.Select(PlayerDto.From).ToList(),
        lineup.Bench.Select(PlayerDto.From).ToList());
}

/// <summary>Maç olayları, oluş sırasıyla. Maç başlamadıysa liste boştur.</summary>
public sealed record MatchEventsDto(int MatchId, IReadOnlyList<MatchEventDto> Events)
{
    public static MatchEventsDto From(int matchId, IReadOnlyList<MatchEvent> events) => new(
        matchId,
        events.Select(MatchEventDto.From).ToList());
}

public sealed record MatchEventDto(
    int Id,
    MatchEventTypeDto Type,
    int Minute,
    int OrderNo,
    int? TeamId,
    PlayerDto? Player,
    int HomeScore,
    int AwayScore,
    string? CombineId)
{
    public static MatchEventDto From(MatchEvent matchEvent) => new(
        matchEvent.Id,
        new MatchEventTypeDto(matchEvent.Type.Id, matchEvent.Type.Name, matchEvent.Type.ShortName),
        matchEvent.Minute,
        matchEvent.OrderNo,
        matchEvent.TeamId,
        matchEvent.Player is null ? null : PlayerDto.From(matchEvent.Player),
        matchEvent.HomeScore,
        matchEvent.AwayScore,
        matchEvent.CombineId);
}

public sealed record MatchEventTypeDto(int Id, string Name, string ShortName);

/// <summary>
/// Maç istatistikleri. İstatistik henüz yoksa <see cref="Home"/> ve <see cref="Away"/> null gelir.
/// </summary>
public sealed record MatchStatisticsDto(int MatchId, TeamStatisticsDto? Home, TeamStatisticsDto? Away)
{
    public static MatchStatisticsDto From(int matchId, MatchStatistics? statistics) => new(
        matchId,
        statistics is null ? null : TeamStatisticsDto.From(statistics.Home),
        statistics is null ? null : TeamStatisticsDto.From(statistics.Away));
}

public sealed record TeamStatisticsDto(
    TeamDto Team,
    int BallPossession,
    int ShotsOnTarget,
    int ShotsOffTarget,
    int Fouls,
    int Corners,
    int Offsides,
    int Saves,
    int YellowCards,
    int SecondYellowCards,
    int RedCards)
{
    public static TeamStatisticsDto From(TeamStatistics statistics) => new(
        TeamDtoMapper.From(statistics.Team),
        statistics.BallPossession,
        statistics.ShotsOnTarget,
        statistics.ShotsOffTarget,
        statistics.Fouls,
        statistics.Corners,
        statistics.Offsides,
        statistics.Saves,
        statistics.YellowCards,
        statistics.SecondYellowCards,
        statistics.RedCards);
}

internal static class TeamDtoMapper
{
    public static TeamDto From(TeamSummary team) => new(team.Id, team.Name, team.ShortName, team.MediumName);
}
