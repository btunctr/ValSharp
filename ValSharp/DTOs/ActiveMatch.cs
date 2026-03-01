using System.Text.Json.Serialization;

namespace ValSharp.DTOs
{
    public class CurrentGamePlayer
    {
        public string Subject { get; set; }
        public string MatchID { get; set; }
        public string Version { get; set; }
    }
    public class ActiveMatch
    {
        public string MatchID { get; set; }
        public string Version { get; set; }
        public string State { get; set; }
        public string MapID { get; set; }
        public string ModeID { get; set; }
        public string ProvisioningFlow { get; set; }
        public string GamePodID { get; set; }
        public string AllMUCName { get; set; }
        public string TeamMUCName { get; set; }
        public string TeamVoiceID { get; set; }
        public bool IsReconnectable { get; set; }
        public Dictionary<string, ActiveMatchPlayer> Players { get; set; }

        [JsonConstructor]
        public ActiveMatch(string matchID, string version, string state, string mapID, string modeID, string provisioningFlow, string gamePodID, string allMUCName, string teamMUCName, string teamVoiceID, bool isReconnectable, ActiveMatchPlayer[] players)
        {
            MatchID = matchID;
            Version = version;
            State = state;
            MapID = mapID;
            ModeID = modeID;
            ProvisioningFlow = provisioningFlow;
            GamePodID = gamePodID;
            AllMUCName = allMUCName;
            TeamMUCName = teamMUCName;
            TeamVoiceID = teamVoiceID;
            IsReconnectable = isReconnectable;
            Players = players.ToDictionary(p => p.Subject);
        }
    }
    public class ActiveMatchPlayer
    {
        public string Subject { get; set; }
        public string TeamID { get; set; }
        public string CharacterID { get; set; }
        public PlayerIdentity PlayerIdentity { get; set; }
        public SeasonalBadgeInfo SeasonalBadgeInfo { get; set; }
    }

    public class PlayerMatchLoadout
    {
        public string CharacterID { get; set; }
        public string Subject { get; set; }
        public Dictionary<string, LoadoutItem> Items { get; set; }

        public class LoadoutItem
        {
            /// <summary>
            /// Item id
            /// </summary>
            public string ID;

            /// <summary>
            /// Item type id
            /// </summary>
            public string TypeID;

            public Dictionary<string, LoadoutItemSocket> Sockets;
        }

        public class SocketItem
        {
            public string ID { get; set; }
            public string TypeID { get; set; }
        }

        public class LoadoutItemSocket
        {
            public string ID { get; set; }
            public SocketItem Item { get; set; }
        }
    }
}
