// TVAPI cevaplarının reji programı (C# 7.3, .NET Framework 4.7.2) tarafındaki karşılıkları.
// Bu dosya ÖRNEKTİR: NTR HTSPOR projesine kopyalanmak üzere yazıldı, burada derlenmiyor.
// Projede daha önce eklediğin CSporApiTypes.cs yerine bunu kullanabilirsin; öncekilerin hepsini içerir.
// Alan adları API'nin JSON'uyla aynıdır (Newtonsoft büyük/küçük harfe bakmaz).
using System;
using System.Collections.Generic;

namespace NTR
{
    // ---- Ortak parçalar -------------------------------------------------------------------------

    /// <summary>id + name + shortName: turnuva, sezon, hafta, durum, ülke, mevki, olay tipi.</summary>
    public class ApiNamed { public int id; public string name; public string shortName; }

    /// <summary>Maç ve puan durumundaki kısa takım bilgisi.</summary>
    public class ApiTeam { public int id; public string name; public string shortName; public string mediumName; }

    public class ApiPlayer
    {
        public int id;
        public string name;         // tam ad (eski knownName)
        public string mediumName;   // eski knownNameMedium
        public string shortName;    // eski knownNameShort
        public int? shirtNumber;    // numara yoksa null
        public ApiNamed position;           // kaleci / defans / orta saha / forvet; yoksa null
        public ApiNamed country;            // yoksa null
        public ApiNamed tacticalPosition;   // yalnızca maç kadrosunda dolu
    }

    // ---- GET /api/v1/soccer/tournaments ------------------------------------------------------------
    // Cevap: List<ApiTournament>
    public class ApiTournament { public int id; public string name; }

    // ---- GET /api/v1/soccer/tournaments/{id}/current -----------------------------------------------
    public class ApiCurrent { public ApiNamed tournament; public ApiNamed stage; public ApiNamed round; }

    // ---- GET /api/v1/soccer/tournaments/{id}/standings?round= --------------------------------------
    public class ApiStandings
    {
        public ApiNamed tournament; public ApiNamed stage; public int roundId;
        public List<ApiStandingRow> overall; public List<ApiStandingRow> home; public List<ApiStandingRow> away;
    }
    public class ApiStandingRow
    {
        public int position; public ApiTeam team;
        public int played; public int won; public int drawn; public int lost;
        public int goalsFor; public int goalsAgainst; public int goalDifference;
        public int points; public int positionChange;
    }

    // ---- Maçlar -----------------------------------------------------------------------------------
    // GET /api/v1/soccer/tournaments/{id}/matches?round=          -> ApiMatchList
    // GET /api/v1/soccer/matches?tournamentId=..&from=..&to=..    -> List<ApiMatch>
    // GET /api/v1/soccer/matches/{id}                             -> ApiMatch
    public class ApiScore { public int? current; public int? regular; public int? halfTime; public int? extraTime; public int? penalties; }
    public class ApiSide { public ApiTeam team; public ApiScore score; }
    public class ApiMatch
    {
        public int id; public DateTime date; public ApiNamed status;
        public int? currentMinute; public int? stoppage;
        public ApiSide home; public ApiSide away;
        public string stadiumName; public string refereeName;
        public int tournamentId; public int? roundId;
        public ApiNamed stage;      // sezon
        public ApiNamed round;      // hafta; yoksa null
        public int? stadiumId; public int? refereeId;
    }
    public class ApiMatchList { public ApiNamed tournament; public ApiNamed stage; public int roundId; public List<ApiMatch> matches; }

    // ---- Takımlar ---------------------------------------------------------------------------------
    // GET /api/v1/soccer/tournaments/{id}/teams   -> ApiTeamList
    // GET /api/v1/soccer/teams/{id}               -> ApiTeamDetail
    // GET /api/v1/soccer/teams/{id}/players       -> ApiSquad
    public class ApiStadium { public int id; public string name; public string shortName; public string cityName; }
    public class ApiTeamDetail
    {
        public int id; public string name; public string shortName; public string mediumName;
        public string coachName;        // teknik direktör; yoksa null
        public ApiNamed country;        // yoksa null
        public ApiStadium stadium;      // yoksa null
    }
    public class ApiTeamList { public ApiNamed tournament; public ApiNamed stage; public List<ApiTeamDetail> teams; }
    public class ApiSquad
    {
        public int teamId;
        public string source;           // "matchLineups" ya da "registeredSquad"
        public List<ApiPlayer> players; // mevki ve forma numarası sırasıyla
    }

    // ---- Maç detayı -------------------------------------------------------------------------------
    // GET /api/v1/soccer/matches/{id}/lineups     -> ApiMatchLineup   (kadro açıklanmadıysa home ve away null)
    // GET /api/v1/soccer/matches/{id}/events      -> ApiMatchEvents   (maç başlamadıysa events boş)
    // GET /api/v1/soccer/matches/{id}/statistics  -> ApiMatchStatistics (istatistik yoksa home ve away null)
    public class ApiTeamLineup
    {
        public ApiTeam team; public string formation; public string managerName;
        public List<ApiPlayer> starting; public List<ApiPlayer> bench;
    }
    public class ApiMatchLineup { public int matchId; public ApiTeamLineup home; public ApiTeamLineup away; }

    public class ApiMatchEvent
    {
        public int id; public ApiNamed type; public int minute; public int orderNo;
        public int? teamId;         // maçın geneline ait olaylarda null
        public ApiPlayer player;    // oyuncusuz olaylarda null
        public int homeScore; public int awayScore;
        public string combineId;    // gol-asist ve çıkan-giren oyuncu aynı değeri taşır
    }
    public class ApiMatchEvents { public int matchId; public List<ApiMatchEvent> events; }

    public class ApiTeamStatistics
    {
        public ApiTeam team;
        public int ballPossession; public int shotsOnTarget; public int shotsOffTarget;
        public int fouls; public int corners; public int offsides; public int saves;
        public int yellowCards; public int secondYellowCards; public int redCards;
    }
    public class ApiMatchStatistics { public int matchId; public ApiTeamStatistics home; public ApiTeamStatistics away; }
}
