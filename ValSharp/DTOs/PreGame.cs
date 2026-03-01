using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValSharp.DTOs
{
    public class PreGameMatch
    {
        public string ID;
        public string Version;
        public PreGameTeam[] Teams;
    }

    public class PreGamePlayer
    {
        public string Subject { get; set; }

        public string CharacterID { get; set; }

        public string CharacterSelectionState { get; set; }

        public string PregamePlayerState { get; set; }

        public int CompetitiveTier { get; set; }

        public PlayerIdentity PlayerIdentity { get; set; }

        public SeasonalBadgeInfo SeasonalBadgeInfo { get; set; }

        public bool IsCaptain { get; set; }
    }

    public class PreGameTeam
    {
        public string TeamID { get; set; }
        public List<PreGamePlayer> Players { get; set; }
    }
}
