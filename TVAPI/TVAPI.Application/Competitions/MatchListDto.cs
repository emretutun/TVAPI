using TVAPI.Domain.Competitions;

namespace TVAPI.Application.Competitions;

/// <summary>Bir haftanın maç listesi cevabı.</summary>
public sealed record MatchListDto(
    TournamentDto Tournament,
    StageDto Stage,
    int RoundId,
    IReadOnlyList<MatchDto> Matches)
{
    public static MatchListDto From(CurrentCompetition competition, int roundId, IReadOnlyList<Match> matches) => new(
        new TournamentDto(competition.Tournament.Id, competition.Tournament.Name),
        new StageDto(competition.Stage.Id, competition.Stage.Name, competition.Stage.ShortName),
        roundId,
        matches.Select(MatchDto.From).ToList());
}

public sealed record MatchDto(
    int Id,
    DateTime Date,
    MatchStatusDto Status,
    int? CurrentMinute,
    int? Stoppage,
    MatchSideDto Home,
    MatchSideDto Away,
    string? StadiumName,
    string? RefereeName,
    int TournamentId,
    int? RoundId,
    StageDto Stage,
    RoundDto? Round,
    int? StadiumId,
    int? RefereeId)
{
    public static MatchDto From(Match match) => new(
        match.Id,
        match.Date,
        new MatchStatusDto(match.Status.Id, match.Status.Name, match.Status.ShortName),
        match.CurrentMinute,
        match.Stoppage,
        MatchSideDto.From(match.Home),
        MatchSideDto.From(match.Away),
        match.StadiumName,
        match.RefereeName,
        match.TournamentId,
        match.Round?.Id,
        new StageDto(match.Stage.Id, match.Stage.Name, match.Stage.ShortName),
        match.Round is null ? null : new RoundDto(match.Round.Id, match.Round.Name, match.Round.ShortName),
        match.StadiumId,
        match.RefereeId);
}

public sealed record MatchStatusDto(int Id, string Name, string ShortName);

public sealed record MatchSideDto(TeamDto Team, MatchScoreDto Score)
{
    public static MatchSideDto From(MatchSide side) => new(
        new TeamDto(side.Team.Id, side.Team.Name, side.Team.ShortName, side.Team.MediumName),
        new MatchScoreDto(
            side.Score.Current,
            side.Score.Regular,
            side.Score.HalfTime,
            side.Score.ExtraTime,
            side.Score.Penalties));
}

public sealed record MatchScoreDto(int? Current, int? Regular, int? HalfTime, int? ExtraTime, int? Penalties);
