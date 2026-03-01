using Newtonsoft.Json;

namespace ValSharp.Content;

public class Weapon
{
    [JsonProperty("uuid")]
    public string Uuid { get; set; }

    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("defaultSkinUuid")]
    public string DefaultSkinUuid { get; set; }

    [JsonProperty("displayIcon")]
    public string DisplayIcon { get; set; }

    [JsonProperty("killStreamIcon")]
    public string KillStreamIcon { get; set; }

    [JsonProperty("assetPath")]
    public string AssetPath { get; set; }

    [JsonProperty("weaponStats")]
    public WeaponStats WeaponStats { get; set; }

    [JsonProperty("shopData")]
    public ShopData ShopData { get; set; }

    [JsonProperty("skins")]
    public Skin[] Skins { get; set; }
}

public class WeaponStats
{
    [JsonProperty("fireRate")]
    public float FireRate { get; set; }

    [JsonProperty("magazineSize")]
    public int MagazineSize { get; set; }

    [JsonProperty("runSpeedMultiplier")]
    public float RunSpeedMultiplier { get; set; }

    [JsonProperty("equipTimeSeconds")]
    public float EquipTimeSeconds { get; set; }

    [JsonProperty("reloadTimeSeconds")]
    public float ReloadTimeSeconds { get; set; }

    [JsonProperty("firstBulletAccuracy")]
    public float FirstBulletAccuracy { get; set; }

    [JsonProperty("shotgunPelletCount")]
    public int ShotgunPelletCount { get; set; }

    [JsonProperty("wallPenetration")]
    public string WallPenetration { get; set; }

    [JsonProperty("feature")]
    public string Feature { get; set; }

    [JsonProperty("fireMode")]
    public string FireMode { get; set; }

    [JsonProperty("altFireType")]
    public string AltFireType { get; set; }

    [JsonProperty("adsStats")]
    public AdsStats AdsStats { get; set; }

    [JsonProperty("altShotgunStats")]
    public AltShotgunStats AltShotgunStats { get; set; }

    [JsonProperty("airBurstStats")]
    public AirBurstStats AirBurstStats { get; set; }

    [JsonProperty("damageRanges")]
    public DamageRange[] DamageRanges { get; set; }
}

public class AdsStats
{
    [JsonProperty("zoomMultiplier")]
    public float ZoomMultiplier { get; set; }

    [JsonProperty("fireRate")]
    public float FireRate { get; set; }

    [JsonProperty("runSpeedMultiplier")]
    public float RunSpeedMultiplier { get; set; }

    [JsonProperty("burstCount")]
    public int BurstCount { get; set; }

    [JsonProperty("firstBulletAccuracy")]
    public float FirstBulletAccuracy { get; set; }
}
public class AltShotgunStats
{
    [JsonProperty("shotgunPelletCount")]
    public int ShotgunPelletCount { get; set; }

    [JsonProperty("burstRate")]
    public float BurstRate { get; set; }
}
public class AirBurstStats
{
    [JsonProperty("shotgunPelletCount")]
    public int ShotgunPelletCount { get; set; }

    [JsonProperty("burstDistance")]
    public float BurstDistance { get; set; }
}
public class DamageRange
{
    [JsonProperty("rangeStartMeters")]
    public float RangeStartMeters { get; set; }

    [JsonProperty("rangeEndMeters")]
    public float RangeEndMeters { get; set; }

    [JsonProperty("headDamage")]
    public float HeadDamage { get; set; }

    [JsonProperty("bodyDamage")]
    public float BodyDamage { get; set; }

    [JsonProperty("legDamage")]
    public float LegDamage { get; set; }
}
public class ShopData
{
    [JsonProperty("cost")]
    public int Cost { get; set; }

    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("shopOrderPriority")]
    public int ShopOrderPriority { get; set; }

    [JsonProperty("categoryText")]
    public string CategoryText { get; set; }

    [JsonProperty("gridPosition")]
    public GridPosition GridPosition { get; set; }

    [JsonProperty("canBeTrashed")]
    public bool CanBeTrashed { get; set; }

    [JsonProperty("image")]
    public string Image { get; set; }

    [JsonProperty("newImage")]
    public string NewImage { get; set; }

    [JsonProperty("newImage2")]
    public string NewImage2 { get; set; }

    [JsonProperty("assetPath")]
    public string AssetPath { get; set; }
}
public class GridPosition
{
    [JsonProperty("row")]
    public int Row { get; set; }

    [JsonProperty("column")]
    public int Column { get; set; }
}
public class Skin
{
    [JsonProperty("uuid")]
    public string Uuid { get; set; }

    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty("themeUuid")]
    public string ThemeUuid { get; set; }

    [JsonProperty("contentTierUuid")]
    public string ContentTierUuid { get; set; }

    [JsonProperty("displayIcon")]
    public string DisplayIcon { get; set; }

    [JsonProperty("wallpaper")]
    public string Wallpaper { get; set; }

    [JsonProperty("assetPath")]
    public string AssetPath { get; set; }

    [JsonProperty("chromas")]
    public Chroma[] Chromas { get; set; }

    [JsonProperty("levels")]
    public SkinLevel[] Levels { get; set; }
}
public class Chroma
{
    [JsonProperty("uuid")]
    public string Uuid { get; set; }

    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty("displayIcon")]
    public string DisplayIcon { get; set; }

    [JsonProperty("fullRender")]
    public string FullRender { get; set; }

    [JsonProperty("swatch")]
    public string Swatch { get; set; }

    [JsonProperty("streamedVideo")]
    public string StreamedVideo { get; set; }

    [JsonProperty("assetPath")]
    public string AssetPath { get; set; }
}

public class SkinLevel
{
    [JsonProperty("uuid")]
    public string Uuid { get; set; }

    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty("levelItem")]
    public string LevelItem { get; set; }

    [JsonProperty("displayIcon")]
    public string DisplayIcon { get; set; }

    [JsonProperty("streamedVideo")]
    public string StreamedVideo { get; set; }

    [JsonProperty("assetPath")]
    public string AssetPath { get; set; }
}