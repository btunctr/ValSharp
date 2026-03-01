using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValSharp.DTOs
{
    public class RiotClientConfig
    {
        [JsonProperty("chat.affinities")]
        public Dictionary<string, string> ChatAffinities { get; set; }

        [JsonProperty("chat.affinity_domains")]
        public Dictionary<string, string> ChatAffinityDomains { get; set; }

        [JsonProperty("chat.port")]
        public int ChatPort { get; set; }
    }
}
