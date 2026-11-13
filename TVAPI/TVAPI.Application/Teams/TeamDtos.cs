using TVAPI.Application.Competitions;
using TVAPI.Domain.Competitions;
using TVAPI.Domain.Teams;

namespace TVAPI.Application.Teams;

/// <summary>Bir turnuvanın aktif aşamasındaki takımlar.</summary>
public sealed record TeamListDto(TournamentDto Tournament, StageDto Stage, IReadOnlyList<TeamDetailDto> Teams)
{
    public static TeamListDto From(CurrentCompetition competition, IReadOnlyList<Team> teams) => new(
        new TournamentDto(competition.Tournament.Id, competition.Tournament.Name),
        new StageDto(competition.Stage.Id, competition.Stage.Name, competition.Stage.ShortName),
        teams.Select(TeamDetailDto.From).ToList());
}

/// <summary>Takım künyesi. (Maç ve puan durumundaki kısa takım bilgisi <see cref="TeamDto"/>'dur.)</summary>
public sealed record TeamDetailDto(
    int Id,
    string Name,
    string ShortName,
    string MediumName,
    string? CoachName,
    CountryDto? Country,
    StadiumDto? Stadium)
{
    public static TeamDetailDto From(Team team) => new(
        team.Id,
        team.Name,
        team.ShortName,
        team.MediumName,
        team.CoachName,
        CountryDto.From(team.Country),
        team.Stadium is null
            ? null
            : new StadiumDto(team.Stadium.Id, team.Stadium.Name, team.Stadium.ShortName, team.Stadium.CityName));
}

public sealed record CountryDto(int Id, string Name, string ShortName)
{
    public static CountryDto? From(Country? country) =>
        country is null ? null : new CountryDto(country.Id, country.Name, country.ShortName);
}

public sealed record StadiumDto(int Id, string Name, string ShortName, string? CityName);

/// <summary>Bir takımın kadrosu, yayın sırasına dizilmiş.</summary>
/// <param name="Source">Kadronun nereden çıkarıldığı: <c>matchLineups</c> ya da <c>registeredSquad</c>.</param>
public sealed record SquadDto(int TeamId, string Source, IReadOnlyList<PlayerDto> Players)
{
    public static SquadDto From(int teamId, SquadSource source, Squad squad) => new(
        teamId,
        source == SquadSource.MatchLineups ? "matchLineups" : "registeredSquad",
        squad.Players.Select(PlayerDto.From).ToList());
}

public sealed record PlayerDto(
    int Id,
    string Name,
    string MediumName,
    string ShortName,
    int? ShirtNumber,
    PlayerPositionDto? Position,
    CountryDto? Country,
    PlayerPositionDto? TacticalPosition)
{
    public static PlayerDto From(Player player) => new(
        player.Id,
        player.Name,
        player.MediumName,
        player.ShortName,
        player.ShirtNumber,
        PlayerPositionDto.From(player.Position),
        CountryDto.From(player.Country),
        PlayerPositionDto.From(player.TacticalPosition));
}

public sealed record PlayerPositionDto(int Id, string Name, string ShortName)
{
    public static PlayerPositionDto? From(PlayerPosition? position) =>
        position is null ? null : new PlayerPositionDto(position.Id, position.Name, position.ShortName);
}

public enum SquadSource
{
    /// <summary>Takımın son maçlarında kadroya yazılan oyuncular.</summary>
    MatchLineups,

    /// <summary>Kaynaktaki kayıtlı kadro listesi (maç kadrosu bulunamadığında).</summary>
    RegisteredSquad,
}
