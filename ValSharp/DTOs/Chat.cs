using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValSharp.DTOs
{
    public class ChatMessage
    {
        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("cid")]
        public string Cid { get; set; }

        [JsonProperty("game_name")]
        public string GameName { get; set; }

        [JsonProperty("game_tag")]
        public string GameTag { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("mid")]
        public string Mid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("pid")]
        public string Pid { get; set; }

        [JsonProperty("puuid")]
        public string Puuid { get; set; }

        [JsonProperty("read")]
        public bool Read { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
    public class ChatParticipant
    {
        [JsonProperty("cid")]
        public string Cid { get; set; }

        [JsonProperty("game_name")]
        public string GameName { get; set; }

        [JsonProperty("game_tag")]
        public string GameTag { get; set; }

        [JsonProperty("muted")]
        public bool Muted { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("pid")]
        public string Pid { get; set; }

        [JsonProperty("puuid")]
        public string Puuid { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }
    }
    public class GameChat
    {
        [JsonProperty("cid")]
        public string Cid { get; set; }

        [JsonProperty("direct_messages")]
        public bool DirectMessages { get; set; }

        [JsonProperty("global_readership")]
        public bool GlobalReadership { get; set; }

        [JsonProperty("message_history")]
        public bool MessageHistory { get; set; }

        [JsonProperty("mid")]
        public string Mid { get; set; }

        [JsonProperty("muted")]
        public bool Muted { get; set; }

        [JsonProperty("mutedRestriction")]
        public bool MutedRestriction { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public enum ChatChannelType
    {
        GroupChat,
        Chat,
        System,
        Unknown
    }
}
