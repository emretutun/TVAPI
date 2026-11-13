using TVAPI.Domain.Competitions;

namespace TVAPI.Application.Competitions;

/// <summary>Puan durumu cevabı: hangi turnuva, aşama ve haftaya ait olduğu ve üç tablo.</summary>
public sealed record StandingsDto(
    TournamentDto Tournament,
    StageDto Stage,
    int RoundId,
    IReadOnlyList<StandingRowDto> Overall,
    IReadOnlyList<StandingRowDto> Home,
    IReadOnlyList<StandingRowDto> Away)
{
    public static StandingsDto From(CurrentCompetition competition, int roundId, Standings standings) => new(
        new TournamentDto(competition.Tournament.Id, competition.Tournament.Name),
        new StageDto(competition.Stage.Id, competition.Stage.Name, competition.Stage.ShortName),
        roundId,
        standings.Overall.Select(StandingRowDto.From).ToList(),
        standings.Home.Select(StandingRowDto.From).ToList(),
        standings.Away.Select(StandingRowDto.From).ToList());
}

public sealed record StandingRowDto(
    int Position,
    TeamDto Team,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points,
    int PositionChange)
{
    public static StandingRowDto From(StandingRow row) => new(
        row.Position,
        new TeamDto(row.Team.Id, row.Team.Name, row.Team.ShortName, row.Team.MediumName),
        row.Played,
        row.Won,
        row.Drawn,
        row.Lost,
        row.GoalsFor,
        row.GoalsAgainst,
        row.GoalDifference,
        row.Points,
        row.PositionChange);
}

public sealed record TeamDto(int Id, string Name, string ShortName, string MediumName);
