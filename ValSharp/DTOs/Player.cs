using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValSharp.DTOs
{
    public class SeasonalBadgeInfo
    {
        public string SeasonID { get; set; }
        public int NumberOfWins { get; set; }
        public object WinsByTier { get; set; }
        public int Rank { get; set; }
        public int LeaderboardRank { get; set; }
    }
    public class PlayerName
    {
        public string DisplayName { get; set; }
        public string Subject { get; set; }
        public string GameName { get; set; }
        public string TagLine { get; set; }
        public int StatusCode { get; set; }
    }

    public class PlayerIdentity
    {
        public string Subject { get; set; }
        public string PlayerCardID { get; set; }
        public string PlayerTitleID { get; set; }
        public int AccountLevel { get; set; }
        public string PreferredLevelBorderID { get; set; }
        public bool Incognito { get; set; }
        public bool HideAccountLevel { get; set; }
    }
}
