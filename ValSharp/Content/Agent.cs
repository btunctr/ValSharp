using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValSharp.Content;

public class Agent
{
    [JsonProperty("uuid")]
    public string Uuid { get; set; }

    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("developerName")]
    public string DeveloperName { get; set; }

    [JsonProperty("releaseDate")]
    public DateTime ReleaseDate { get; set; }

    [JsonProperty("characterTags")]
    public string[] CharacterTags { get; set; }

    [JsonProperty("displayIcon")]
    public string DisplayIcon { get; set; }

    [JsonProperty("displayIconSmall")]
    public string DisplayIconSmall { get; set; }

    [JsonProperty("bustPortrait")]
    public string BustPortrait { get; set; }

    [JsonProperty("fullPortrait")]
    public string FullPortrait { get; set; }

    [JsonProperty("fullPortraitV2")]
    public string FullPortraitV2 { get; set; }

    [JsonProperty("killfeedPortrait")]
    public string KillfeedPortrait { get; set; }

    [JsonProperty("background")]
    public string Background { get; set; }

    [JsonProperty("backgroundGradientColors")]
    public string[] BackgroundGradientColors { get; set; }

    [JsonProperty("assetPath")]
    public string AssetPath { get; set; }

    [JsonProperty("isFullPortraitRightFacing")]
    public bool IsFullPortraitRightFacing { get; set; }

    [JsonProperty("isPlayableCharacter")]
    public bool IsPlayableCharacter { get; set; }

    [JsonProperty("isAvailableForTest")]
    public bool IsAvailableForTest { get; set; }

    [JsonProperty("isBaseContent")]
    public bool IsBaseContent { get; set; }

    [JsonProperty("role")]
    public Role Role { get; set; }

    [JsonProperty("recruitmentData")]
    public RecruitmentData RecruitmentData { get; set; }

    [JsonProperty("abilities")]
    public Ability[] Abilities { get; set; }

    [JsonProperty("voiceLine")]
    public VoiceLine VoiceLine { get; set; }
}

public class Role
{
    [JsonProperty("uuid")]
    public string Uuid { get; set; }

    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("displayIcon")]
    public string DisplayIcon { get; set; }

    [JsonProperty("assetPath")]
    public string AssetPath { get; set; }
}

public class RecruitmentData
{
    [JsonProperty("counterId")]
    public string CounterId { get; set; }

    [JsonProperty("milestoneId")]
    public string MilestoneId { get; set; }

    [JsonProperty("milestoneThreshold")]
    public int MilestoneThreshold { get; set; }

    [JsonProperty("useLevelVpCostOverride")]
    public bool UseLevelVpCostOverride { get; set; }

    [JsonProperty("levelVpCostOverride")]
    public int LevelVpCostOverride { get; set; }

    [JsonProperty("startDate")]
    public DateTime StartDate { get; set; }

    [JsonProperty("endDate")]
    public DateTime EndDate { get; set; }
}

public class Ability
{
    [JsonProperty("slot")]
    public string Slot { get; set; }

    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("displayIcon")]
    public string DisplayIcon { get; set; }
}

public class VoiceLine
{
    [JsonProperty("minDuration")]
    public float MinDuration { get; set; }

    [JsonProperty("maxDuration")]
    public float MaxDuration { get; set; }

    [JsonProperty("mediaList")]
    public Media[] MediaList { get; set; }
}

public class Media
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("wwise")]
    public string Wwise { get; set; }

    [JsonProperty("wave")]
    public string Wave { get; set; }
}