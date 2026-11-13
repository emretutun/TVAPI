using TVAPI.Domain.Competitions;

namespace TVAPI.Application.Competitions;

/// <summary>
/// İstemciye dönen cevap modeli. Domain sınıflarını doğrudan dışarı vermeyiz;
/// böylece içerideki model değişse bile istemcinin gördüğü sözleşme sabit kalır.
/// </summary>
public sealed record CurrentCompetitionDto(TournamentDto Tournament, StageDto Stage, RoundDto? Round)
{
    public static CurrentCompetitionDto From(CurrentCompetition competition) => new(
        new TournamentDto(competition.Tournament.Id, competition.Tournament.Name),
        new StageDto(competition.Stage.Id, competition.Stage.Name, competition.Stage.ShortName),
        competition.Round is null
            ? null
            : new RoundDto(competition.Round.Id, competition.Round.Name, competition.Round.ShortName));
}

public sealed record TournamentDto(int Id, string Name);

public sealed record StageDto(int Id, string Name, string ShortName);

public sealed record RoundDto(int Id, string Name, string ShortName);
