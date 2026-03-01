using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValSharp.DTOs
{
    public class UserPresence
    {
        [JsonProperty("actor")]
        public string Actor { get; set; }

        [JsonProperty("championId")]
        public string ChampionId { get; set; }

        [JsonProperty("basic")]
        public string Basic { get; set; }

        [JsonProperty("details")]
        public string Details { get; set; }

        [JsonProperty("game_name")]
        public string GameName { get; set; }

        [JsonProperty("game_tag")]
        public string GameTag { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("patchline")]
        public object Patchline { get; set; }

        [JsonProperty("pid")]
        public string Pid { get; set; }

        [JsonProperty("platform")]
        public object Platform { get; set; }

        [JsonProperty("private")]
        public string Private { get; set; }

        [JsonProperty("privateJwt")]
        public object PrivateJwt { get; set; }

        [JsonProperty("product")]
        public string Product { get; set; }

        [JsonProperty("puuid")]
        public string Puuid { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("resource")]
        public string Resource { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("time")]
        public object Time { get; set; }

        [JsonProperty("privinfo")]
        public UserPresenceDetails PrivateInfo { get; set; }
    }

    public class UserPresenceDetails
    {
        [JsonProperty("isValid")]
        public bool IsValid { get; set; }

        [JsonProperty("sessionLoopState")]
        public string SessionLoopState { get; set; }

        [JsonProperty("partyOwnerSessionLoopState")]
        public string PartyOwnerSessionLoopState { get; set; }

        [JsonProperty("customGameName")]
        public string CustomGameName { get; set; }

        [JsonProperty("customGameTeam")]
        public string CustomGameTeam { get; set; }

        [JsonProperty("partyOwnerMatchMap")]
        public string PartyOwnerMatchMap { get; set; }

        [JsonProperty("partyOwnerMatchCurrentTeam")]
        public string PartyOwnerMatchCurrentTeam { get; set; }

        [JsonProperty("partyOwnerMatchScoreAllyTeam")]
        public int PartyOwnerMatchScoreAllyTeam { get; set; }

        [JsonProperty("partyOwnerMatchScoreEnemyTeam")]
        public int PartyOwnerMatchScoreEnemyTeam { get; set; }

        [JsonProperty("partyOwnerProvisioningFlow")]
        public string PartyOwnerProvisioningFlow { get; set; }

        [JsonProperty("provisioningFlow")]
        public string ProvisioningFlow { get; set; }

        [JsonProperty("matchMap")]
        public string MatchMap { get; set; }

        [JsonProperty("partyId")]
        public string PartyId { get; set; }

        [JsonProperty("isPartyOwner")]
        public bool IsPartyOwner { get; set; }

        [JsonProperty("partyName")]
        public string PartyName { get; set; }

        [JsonProperty("partyState")]
        public string PartyState { get; set; }

        [JsonProperty("partyAccessibility")]
        public string PartyAccessibility { get; set; }

        [JsonProperty("maxPartySize")]
        public int MaxPartySize { get; set; }

        [JsonProperty("queueId")]
        public string QueueId { get; set; }

        [JsonProperty("partyLFM")]
        public bool PartyLfm { get; set; }

        [JsonProperty("partyClientVersion")]
        public string PartyClientVersion { get; set; }

        [JsonProperty("partySize")]
        public int PartySize { get; set; }

        [JsonProperty("partyVersion")]
        public long PartyVersion { get; set; }

        [JsonProperty("queueEntryTime")]
        public string QueueEntryTime { get; set; }

        [JsonProperty("playerCardId")]
        public string PlayerCardId { get; set; }

        [JsonProperty("playerTitleId")]
        public string PlayerTitleId { get; set; }

        [JsonProperty("isIdle")]
        public bool IsIdle { get; set; }
    }
}
