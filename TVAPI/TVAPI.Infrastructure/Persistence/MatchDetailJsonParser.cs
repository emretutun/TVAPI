using TVAPI.Domain.Matches;
using TVAPI.Domain.Teams;

namespace TVAPI.Infrastructure.Persistence;

/// <summary>
/// soccer_lineups, soccer_events ve soccer_stats tablolarındaki JSON'ları Domain modeline çevirir.
/// Veritabanına bağlı değildir; bu sayede veritabanı olmadan sınanabilir.
/// </summary>
internal static class MatchDetailJsonParser
{
    /// <summary>Kadro açıklanmadıysa (kaynak null yazdıysa ya da takımlardan biri eksikse) null döner.</summary>
    public static MatchLineup? ParseLineup(string json, string sourceDescription)
    {
        var parsed = BroadageJson.Deserialize<LineupJson>(json, "Maç kadrosu", sourceDescription);

        return parsed?.HomeTeam is null || parsed.AwayTeam is null
            ? null
            : new MatchLineup(parsed.HomeTeam.ToDomain(), parsed.AwayTeam.ToDomain());
    }

    /// <summary>Olaylar kaynaktaki sırayla döner; olay yoksa boş liste.</summary>
    public static IReadOnlyList<MatchEvent> ParseEvents(string json, string sourceDescription)
    {
        var parsed = BroadageJson.Deserialize<List<EventJson?>>(json, "Maç olayları", sourceDescription);

        return (parsed ?? [])
            .OfType<EventJson>()
            .Select(matchEvent => matchEvent.ToDomain())
            .ToList();
    }

    /// <summary>İstatistik yoksa (kaynak null yazdıysa ya da takımlardan biri eksikse) null döner.</summary>
    public static MatchStatistics? ParseStatistics(string json, string sourceDescription)
    {
        var parsed = BroadageJson.Deserialize<StatisticsJson>(json, "Maç istatistikleri", sourceDescription);

        return parsed?.HomeTeam is null || parsed.AwayTeam is null
            ? null
            : new MatchStatistics(parsed.HomeTeam.ToDomain(), parsed.AwayTeam.ToDomain());
    }

    // Aşağıdaki sınıflar kaynaktaki JSON'un karşılığıdır ve yalnızca burada kullanılır.
    // Kaynakta maçın tarihi, turnuvası, haftası da tekrar edilir; onlar maçın kendisinden okunduğu için alınmaz.

    private sealed class LineupJson
    {
        public LineupTeamJson? HomeTeam { get; set; }
        public LineupTeamJson? AwayTeam { get; set; }
    }

    private sealed class LineupTeamJson : BroadageTeamJson
    {
        public BroadageNamedJson? Formation { get; set; }
        public ManagerJson? Manager { get; set; }
        public List<BroadagePlayerJson?>? Starting { get; set; }
        public List<BroadagePlayerJson?>? Bench { get; set; }

        public TeamLineup ToDomain() => new(
            ToTeamSummary(),
            NullIfBlank(Formation?.Name),
            Manager?.DisplayName(),
            ToPlayers(Starting),
            ToPlayers(Bench));

        private static IReadOnlyList<Player> ToPlayers(List<BroadagePlayerJson?>? players) =>
            (players ?? []).OfType<BroadagePlayerJson>().Select(player => player.ToDomain()).ToList();
    }

    private sealed class ManagerJson
    {
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? KnownName { get; set; }

        // Bilinen ad varsa o, yoksa ad ve soyad birleştirilir.
        public string? DisplayName() =>
            NullIfBlank(KnownName) ?? NullIfBlank($"{Name} {Surname}".Trim());
    }

    private sealed class EventJson
    {
        public int Id { get; set; }
        public BroadageNamedJson? Type { get; set; }
        public int Minute { get; set; }
        public int OrderNo { get; set; }
        public int? TeamId { get; set; }
        public BroadagePlayerJson? Player { get; set; }
        public int HomeTeamScore { get; set; }
        public int AwayTeamScore { get; set; }
        public string? CombineId { get; set; }

        public MatchEvent ToDomain() => new(
            Id,
            new MatchEventType(Type?.Id ?? 0, Type?.Name ?? string.Empty, Type?.ShortName ?? string.Empty),
            Minute,
            OrderNo,
            TeamId is > 0 ? TeamId : null,
            Player?.ToDomain(),
            HomeTeamScore,
            AwayTeamScore,
            NullIfBlank(CombineId));
    }

    private sealed class StatisticsJson
    {
        public StatisticsTeamJson? HomeTeam { get; set; }
        public StatisticsTeamJson? AwayTeam { get; set; }
    }

    private sealed class StatisticsTeamJson : BroadageTeamJson
    {
        public StatisticsValuesJson? Statistics { get; set; }

        public TeamStatistics ToDomain()
        {
            var values = Statistics ?? new StatisticsValuesJson();

            return new TeamStatistics(
                ToTeamSummary(),
                values.BallPossession,
                values.ShotsOnTarget,
                values.ShotsOffTarget,
                values.Fouls,
                values.Corners,
                values.Offsides,
                values.Saves,
                values.YellowCards,
                values.SecondYellowCards,
                values.RedCards);
        }
    }

    private sealed class StatisticsValuesJson
    {
        public int BallPossession { get; set; }
        public int ShotsOnTarget { get; set; }
        public int ShotsOffTarget { get; set; }
        public int Fouls { get; set; }
        public int Corners { get; set; }
        public int Offsides { get; set; }
        public int Saves { get; set; }
        public int YellowCards { get; set; }
        public int SecondYellowCards { get; set; }
        public int RedCards { get; set; }
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
