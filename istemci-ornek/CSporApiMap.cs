// TVAPI cevaplarını NTR HTSPOR'un mevcut sınıflarına çeviren yardımcılar.
// Bu dosya ÖRNEKTİR: NTR HTSPOR projesine kopyalanmak üzere yazıldı, burada derlenmiyor.
// Eski sınıfların adlarını oturumun başında okuduğum koda göre yazdım; derlerken ad uyuşmazlığı çıkarsa
// ilgili satırı kendi sınıfına göre düzelt.
//
// Büyük harf: eski kod takım ve stat adlarını ToUpper(tr-TR) ile çeviriyordu, kadro/olay/istatistik
// JSON'larına ise dokunmuyordu. Burada da aynı davranış korunuyor.
using System.Collections.Generic;
using System.Globalization;

namespace NTR
{
    public static class CSporApiMap
    {
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");

        private static string Upper(string value) { return (value ?? "").ToUpper(Tr); }

        // ---- Puan durumu: ApiStandings -> TeamsType ------------------------------------------------
        public static TeamsType ToTeamsType(ApiStandings api)
        {
            if (api == null) return null;
            return new TeamsType
            {
                Overall = ToTeamStats(api.overall),
                Home = ToTeamStats(api.home),
                Away = ToTeamStats(api.away)
            };
        }

        private static List<TeamStats> ToTeamStats(List<ApiStandingRow> rows)
        {
            List<TeamStats> result = new List<TeamStats>();
            if (rows == null) return result;
            foreach (ApiStandingRow row in rows)
            {
                result.Add(new TeamStats
                {
                    Team = new Team_JsonType
                    {
                        Id = row.team.id,
                        name = row.team.name,
                        shortName = row.team.shortName,
                        mediumName = row.team.mediumName
                    },
                    Position = row.position,
                    Played = row.played,
                    Won = row.won,
                    Draw = row.drawn,
                    Lost = row.lost,
                    Scored = row.goalsFor,
                    Against = row.goalsAgainst,
                    Average = row.goalDifference,
                    Points = row.points,
                    Delta = row.positionChange
                });
            }
            return result;
        }

        // ---- Takım: ApiTeamDetail -> TTeam ---------------------------------------------------------
        // PlayerLoader'ı çağıran yer atar:  team.PlayerLoader = GetPlayerList;
        public static TTeam ToTTeam(ApiTeamDetail api, int tournamentId, int stageId)
        {
            if (api == null) return null;
            TTeam team = new TTeam();
            team.tournament_id = tournamentId;
            team.stage_id = stageId;
            team.team_id = api.id;
            team.team_name = Upper(api.name);
            team.team_short_name = Upper(api.shortName);
            team.team_medium_name = Upper(api.mediumName);
            team.teknik_direktor = Upper(api.coachName);
            if (api.country != null)
            {
                team.country_id = api.country.id;
                team.country_name = Upper(api.country.name);
                team.country_short_name = Upper(api.country.shortName);
            }
            if (api.stadium != null)
            {
                team.stadium_id = api.stadium.id;
                team.stadium_name = Upper(api.stadium.name);
                team.stadium_short_name = Upper(api.stadium.shortName);
                team.stadium_city_name = Upper(api.stadium.cityName);
            }
            return team;
        }

        // ---- Kadro: ApiSquad -> List<PlayerData> ---------------------------------------------------
        public static List<PlayerData> ToPlayerDataList(ApiSquad api)
        {
            List<PlayerData> result = new List<PlayerData>();
            if (api == null || api.players == null) return result;
            foreach (ApiPlayer p in api.players)
                result.Add(new PlayerData { Player = ToPlayer(p) });
            return result;
        }

        // NTR.Player: forma numarası metindir, numara yoksa boş metin.
        private static NTR.Player ToPlayer(ApiPlayer p)
        {
            if (p == null) return null;
            return new NTR.Player
            {
                Id = p.id,
                KnownName = p.name,
                KnownNameMedium = p.mediumName,
                KnownNameShort = p.shortName,
                ShirtNumber = p.shirtNumber.HasValue ? p.shirtNumber.Value.ToString() : "",
                Position = p.position == null ? null : new NTR.Position { Id = p.position.id, Name = p.position.name, ShortName = p.position.shortName },
                Country = p.country == null ? null : new NTR.Country { Id = p.country.id, Name = p.country.name, ShortName = p.country.shortName }
            };
        }

        // ---- Maç olayları: ApiMatchEvents -> List<MatchEvent> --------------------------------------
        // Eski kod olay yoksa null dönüyordu; aynı davranış için boş listede null döner.
        public static List<MatchEvent> ToMatchEvents(ApiMatchEvents api)
        {
            if (api == null || api.events == null || api.events.Count == 0) return null;
            List<MatchEvent> result = new List<MatchEvent>();
            foreach (ApiMatchEvent e in api.events)
            {
                result.Add(new MatchEvent
                {
                    Id = e.id,
                    Type = new EventType { Id = e.type.id, Name = e.type.name, ShortName = e.type.shortName },
                    Minute = e.minute,
                    OrderNo = e.orderNo,
                    HomeTeamScore = e.homeScore,
                    AwayTeamScore = e.awayScore,
                    Player = ToPlayer(e.player),
                    TeamId = e.teamId ?? 0,
                    CombineId = e.combineId
                });
            }
            return result;
        }

        // ---- Maç istatistikleri: ApiMatchStatistics -> MatchStatistics -----------------------------
        // Eski kod istatistik yoksa null dönüyordu.
        public static MatchStatistics ToMatchStatistics(ApiMatchStatistics api)
        {
            if (api == null || api.home == null || api.away == null) return null;
            return new MatchStatistics
            {
                id = api.matchId,
                homeTeam = ToTeamStatistics(api.home),
                awayTeam = ToTeamStatistics(api.away)
            };
        }

        private static TeamStatistics ToTeamStatistics(ApiTeamStatistics s)
        {
            return new TeamStatistics
            {
                id = s.team.id,
                name = s.team.name,
                shortName = s.team.shortName,
                mediumName = s.team.mediumName,
                statistics = new Statistics
                {
                    ballPossession = s.ballPossession,
                    shotsOnTarget = s.shotsOnTarget,
                    shotsOffTarget = s.shotsOffTarget,
                    fouls = s.fouls,
                    corners = s.corners,
                    offsides = s.offsides,
                    saves = s.saves,
                    yellowCards = s.yellowCards,
                    secondYellowCards = s.secondYellowCards,
                    redCards = s.redCards
                }
            };
        }

        // ---- Maç kadrosu: ApiMatchLineup -> CSceneDataController.Root ------------------------------
        // Eski kod kadro açıklanmadıysa null dönüyordu.
        // API'de olmayanlar: diziliş id'si, teknik direktörün id/ad/soyad ayrımı (tam ad knownName'e yazılır),
        // Root.date / tournament / stage / round (maçın kendisinden okunabilir).
        public static CSceneDataController.Root ToLineupRoot(ApiMatchLineup api)
        {
            if (api == null || api.home == null || api.away == null) return null;
            return new CSceneDataController.Root
            {
                id = api.matchId,
                homeTeam = ToLineupTeam(api.home),
                awayTeam = ToLineupTeam(api.away)
            };
        }

        private static CSceneDataController.Team ToLineupTeam(ApiTeamLineup t)
        {
            return new CSceneDataController.Team
            {
                id = t.team.id,
                name = t.team.name,
                shortName = t.team.shortName,
                mediumName = t.team.mediumName,
                formation = t.formation == null ? null : new CSceneDataController.Formation { name = t.formation },
                manager = t.managerName == null ? null : new CSceneDataController.Manager { knownName = t.managerName },
                starting = ToLineupPlayers(t.starting),
                bench = ToLineupPlayers(t.bench)
            };
        }

        private static List<CSceneDataController.Player> ToLineupPlayers(List<ApiPlayer> players)
        {
            List<CSceneDataController.Player> result = new List<CSceneDataController.Player>();
            if (players == null) return result;
            foreach (ApiPlayer p in players)
            {
                result.Add(new CSceneDataController.Player
                {
                    id = p.id,
                    knownName = p.name,
                    knownNameMedium = p.mediumName,
                    knownNameShort = p.shortName,
                    shirtNumber = p.shirtNumber ?? 0,
                    country = p.country == null ? null : new CSceneDataController.Country { id = p.country.id, name = p.country.name, shortName = p.country.shortName },
                    position = p.position == null ? null : new CSceneDataController.Position
                    {
                        id = p.position.id,
                        name = p.position.name,
                        shortName = p.position.shortName,
                        tactical = p.tacticalPosition == null ? null : new CSceneDataController.Tactical
                        {
                            id = p.tacticalPosition.id,
                            name = p.tacticalPosition.name,
                            shortName = p.tacticalPosition.shortName
                        }
                    }
                });
            }
            return result;
        }
    }
}
