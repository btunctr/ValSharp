using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValSharp.DTOs
{
    public class PartyPlayer
    {
        public string Subject { get; set; }
        public long Version { get; set; }
        public string CurrentPartyID { get; set; }
        public object Invites { get; set; }
        public dynamic Requests { get; set; }
        public int StatusCode { get; set; }
    }

    public class Party
    {
        public string ID { get; set; }
        public string MUCName { get; set; }
        public string VoiceRoomID { get; set; }
        public int Version { get; set; }
        public string ClientVersion { get; set; }
        public PartyMember[] Members { get; set; }
        public string State { get; set; }
        public string PreviousState { get; set; }
        public string StateTransitionReason { get; set; }
        public string Accessibility { get; set; }
        public MatchmakingData MatchmakingData { get; set; }
        public object Invites { get; set; }
        public object[] Requests { get; set; }
        public string QueueEntryTime { get; set; }
        public int RestrictedSeconds { get; set; }
        public string[] EligibleQueues { get; set; }
        public string[] QueueIneligibilities { get; set; }
    }

    public class PartyMember
    {
        public string Subject { get; set; }
        public int CompetitiveTier { get; set; }
        public PlayerIdentity PlayerIdentity { get; set; }
        public object SeasonalBadgeInfo { get; set; }
        public bool? IsOwner { get; set; }
        public int QueueEligibleRemainingAccountLevels { get; set; }
        public bool IsReady { get; set; }
        public bool IsModerator { get; set; }
        public bool UseBroadcastHUD { get; set; }
        public string PlatformType { get; set; }
    }
}
