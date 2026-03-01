using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ValSharp.Content
{
    public class ContentTier
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("devName")]
        public string DevName { get; set; }

        [JsonProperty("rank")]
        public int Rank { get; set; }

        [JsonProperty("juiceValue")]
        public int JuiceValue { get; set; }

        [JsonProperty("juiceCost")]
        public int JuiceCost { get; set; }

        [JsonProperty("highlightColor")]
        public string HighlightColor { get; set; }

        [JsonProperty("displayIcon")]
        public string DisplayIcon { get; set; }

        [JsonProperty("assetPath")]
        public string AssetPath { get; set; }
    }
}
