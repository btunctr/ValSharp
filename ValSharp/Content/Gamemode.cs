using Newtonsoft.Json;

namespace ValSharp.Content
{
    public class GameMode
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("duration")]
        public string Duration { get; set; }

        [JsonProperty("economyType")]
        public string EconomyType { get; set; }

        [JsonProperty("allowsMatchTimeouts")]
        public bool AllowsMatchTimeouts { get; set; }

        [JsonProperty("allowsCustomGameReplays")]
        public bool AllowsCustomGameReplays { get; set; }

        [JsonProperty("isTeamVoiceAllowed")]
        public bool IsTeamVoiceAllowed { get; set; }

        [JsonProperty("isMinimapHidden")]
        public bool IsMinimapHidden { get; set; }

        [JsonProperty("orbCount")]
        public int OrbCount { get; set; }

        [JsonProperty("roundsPerHalf")]
        public int RoundsPerHalf { get; set; }

        [JsonProperty("teamRoles")]
        public List<string> TeamRoles { get; set; }

        [JsonProperty("gameFeatureOverrides")]
        public List<FeatureOverride> GameFeatureOverrides { get; set; }

        [JsonProperty("gameRuleBoolOverrides")]
        public List<RuleOverride> GameRuleBoolOverrides { get; set; }

        [JsonProperty("displayIcon")]
        public string DisplayIcon { get; set; }

        [JsonProperty("listViewIconTall")]
        public string ListViewIconTall { get; set; }

        [JsonProperty("assetPath")]
        public string AssetPath { get; set; }

        public class FeatureOverride
        {
            [JsonProperty("featureName")]
            public string FeatureName { get; set; }

            [JsonProperty("state")]
            public bool State { get; set; }
        }

        public class RuleOverride
        {
            [JsonProperty("ruleName")]
            public string RuleName { get; set; }

            [JsonProperty("state")]
            public bool State { get; set; }
        }
    }
}
