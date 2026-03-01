using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValSharp.DTOs
{
    public class Friend
    {
        [JsonProperty("activePlatform")]
        public string ActivePlatform { get; set; }

        [JsonProperty("displayGroup")]
        public string DisplayGroup { get; set; }

        [JsonProperty("game_name")]
        public string GameName { get; set; }

        [JsonProperty("game_tag")]
        public string GameTag { get; set; }

        [JsonProperty("group")]
        public string Group { get; set; }

        [JsonProperty("last_online_ts")]
        public long? LastOnlineTimestamp { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }

        [JsonProperty("pid")]
        public string Pid { get; set; }

        [JsonProperty("puuid")]
        public string Puuid { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }
    }
}
