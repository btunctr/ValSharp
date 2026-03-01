using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ValSharp.Content
{
    public class Map
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("narrativeDescription")]
        public string NarrativeDescription { get; set; }

        [JsonProperty("tacticalDescription")]
        public string TacticalDescription { get; set; }

        [JsonProperty("coordinates")]
        public string Coordinates { get; set; }

        [JsonProperty("displayIcon")]
        public string DisplayIcon { get; set; }

        [JsonProperty("listViewIcon")]
        public string ListViewIcon { get; set; }

        [JsonProperty("listViewIconTall")]
        public string ListViewIconTall { get; set; }

        [JsonProperty("splash")]
        public string Splash { get; set; }

        [JsonProperty("stylizedBackgroundImage")]
        public string StylizedBackgroundImage { get; set; }

        [JsonProperty("premierBackgroundImage")]
        public string PremierBackgroundImage { get; set; }

        [JsonProperty("assetPath")]
        public string AssetPath { get; set; }

        [JsonProperty("mapUrl")]
        public string MapUrl { get; set; }

        [JsonProperty("xMultiplier")]
        public double XMultiplier { get; set; }

        [JsonProperty("yMultiplier")]
        public double YMultiplier { get; set; }

        [JsonProperty("xScalarToAdd")]
        public double XScalarToAdd { get; set; }

        [JsonProperty("yScalarToAdd")]
        public double YScalarToAdd { get; set; }

        [JsonProperty("callouts")]
        public JArray Callouts { get; set; }
    }
}
