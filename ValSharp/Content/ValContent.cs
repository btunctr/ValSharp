using Newtonsoft.Json.Linq;
using ValSharp.Content;

namespace ValSharp.Content
{
    public static class ValContent
    {
        public static readonly List<string> BeforeAscendantSeasons = new List<string>
        {
            "0df5adb9-4dcb-6899-1306-3e9860661dd3",
            "3f61c772-4560-cd3f-5d3f-a7ab5abda6b3",
            "0530b9c4-4980-f2ee-df5d-09864cd00542",
            "46ea6166-4573-1128-9cea-60a15640059b",
            "fcf2c8f4-4324-e50b-2e23-718e4a3ab046",
            "97b6e739-44cc-ffa7-49ad-398ba502ceb0",
            "ab57ef51-4e59-da91-cc8d-51a5a2b9b8ff",
            "52e9749a-429b-7060-99fe-4595426a0cf7",
            "71c81c67-4fae-ceb1-844c-aab2bb8710fa",
            "2a27e5d2-4d30-c9e2-b15a-93b8909a442c",
            "4cb622e1-4244-6da3-7276-8daaf1c01be2",
            "a16955a5-4ad0-f761-5e9e-389df1c892fb",
            "97b39124-46ce-8b55-8fd1-7cbf7ffe173f",
            "573f53ac-41a5-3a7d-d9ce-d6a6298e5704",
            "d929bc38-4ab6-7da4-94f0-ee84f8ac141e",
            "3e47230a-463c-a301-eb7d-67bb60357d4f",
             "808202d6-4f2b-a8ff-1feb-b3a0590ad79f",
        };

        public static readonly Dictionary<SocketType, string> Sockets = new Dictionary<SocketType, string>
        {
            { SocketType.Skin, "bcef87d6-209b-46c6-8b19-fbe40bd95abc" },
            { SocketType.SkinLevel, "e7c63390-eda7-46e0-bb7a-a6abdacd2433" },
            { SocketType.SkinChroma, "3ad1b2b2-acdb-4524-852f-954a76ddae0a" },
            { SocketType.SkinBuddy, "77258665-71d1-4623-bc72-44db9bd5b3b3" },
            { SocketType.SkinBuddyLevel, "dd3bf334-87f3-40bd-b043-682a57a8dc3a" }
        };

        public static readonly Dictionary<string, int> ContentTiers = new Dictionary<string, int>()
        {
            {"12683d76-48d7-84a3-4e09-6985794f0445", 0 },
            {"0cebb8be-46d7-c12a-d306-e9907bfc5a25", 1 },
            {"60bca009-4182-7998-dee7-b8a2558dc369", 2 },
            {"e046854e-406c-37f4-6607-19a9ba8426fc", 3 },
            {"411e4a55-4e59-7757-41f0-86a53f101bb5", 4 },
        };

        public static readonly Dictionary<string, string> Gamemodes = new Dictionary<string, string>
        {
            { "newmap", "New Map" },
            { "competitive", "Competitive" },
            { "unrated", "Unrated" },
            { "swiftplay", "Swiftplay" },
            { "spikerush", "Spike Rush" },
            { "deathmatch", "Deathmatch" },
            { "ggteam", "Escalation" },
            { "onefa", "Replication" },
            { "custom", "Custom" },
            { "snowball", "Snowball Fight" },
            { "", "Custom" }
        };

        public static Weapon[]? GetWeapons(string language = "en-US")
        {
            var token = HttpUtils.SendRequest<JToken>($"https://valorant-api.com/v1/weapons?language={language}");
            return token?["data"]?.ToObject<Weapon[]>();
        }

        public static Skin[]? GetSkins(string language = "en-US")
        {
            var token = HttpUtils.SendRequest<JToken>($"https://valorant-api.com/v1/weapons/skins?language={language}");
            return token?["data"]?.ToObject<Skin[]>();
        }

        public static Chroma[]? GetSkinChromas(string language = "en-US")
        {
            var token = HttpUtils.SendRequest<JToken>($"https://valorant-api.com/v1/weapons/skinchromas?language={language}");
            return token?["data"]?.ToObject<Chroma[]>();
        }

        public static SkinLevel[]? GetSkinLevels(string language = "en-US")
        {
            var token = HttpUtils.SendRequest<JToken>($"https://valorant-api.com/v1/weapons/skinlevels?language={language}");
            return token?["data"]?.ToObject<SkinLevel[]>();
        }

        public static Weapon[]? GetWeaponFromId(string id, string language = "en-US")
        {
            var token = HttpUtils.SendRequest<JToken>($"https://valorant-api.com/v1/weapons/{id}?language={language}");
            return token?["data"]?.ToObject<Weapon[]>();
        }

        public static Skin[]? GetSkinFromId(string id, string language = "en-US")
        {
            var token = HttpUtils.SendRequest<JToken>($"https://valorant-api.com/v1/weapons/skins/{id}?language={language}");
            return token?["data"]?.ToObject<Skin[]>();
        }

        public static Chroma[]? GetSkinChromaFromId(string id, string language = "en-US")
        {
            var token = HttpUtils.SendRequest<JToken>($"https://valorant-api.com/v1/weapons/skinchromas/{id}?language={language}");
            return token?["data"]?.ToObject<Chroma[]>();
        }

        public static SkinLevel[]? GetSkinLevelForId(string id, string language = "en-US")
        {
            var token = HttpUtils.SendRequest<JToken>($"https://valorant-api.com/v1/weapons/skinlevels/{id}?language={language}");
            return token?["data"]?.ToObject<SkinLevel[]>();
        }

        public static Agent[]? GetAgents(bool isPlayableCharacter = true, string language = "en-US")
        {
            var token = HttpUtils.SendRequest<JToken>($"https://valorant-api.com/v1/agents?isPlayableCharacter={isPlayableCharacter}&language={language}");
            return token?["data"]?.ToObject<Agent[]>();
        }

        public static CompetitiveTier[]? GetCompetitiveTiers(string language = "en-US")
        {
            var token = HttpUtils.SendRequest<JToken>($"https://valorant-api.com/v1/competitivetiers?language={language}");
            return token?["data"]?.ToObject<CompetitiveTier[]>();
        }

        public static Map[]? GetMaps(string language = "en-US")
        {
            var token = HttpUtils.SendRequest<JToken>($"https://valorant-api.com/v1/maps?language={language}");
            return token?["data"]?.ToObject<Map[]>();
        }

        public static GameMode[]? GetGamemodes(string language = "en-US")
        {
            var token = HttpUtils.SendRequest<JToken>($"https://valorant-api.com/v1/gamemodes?language={language}");
            return token?["data"]?.ToObject<GameMode[]>();
        }

        public static ContentTier[]? GetContentTiers(string language = "en-US")
        {
            var token = HttpUtils.SendRequest<JToken>($"https://valorant-api.com/v1/contenttiers?language={language}");
            return token?["data"]?.ToObject<ContentTier[]>();
        }


        public enum SocketType
        {
            Skin,
            SkinLevel,
            SkinChroma,
            SkinBuddy,
            SkinBuddyLevel
        };
    }
}
