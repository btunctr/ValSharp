using DeepSeek.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Text;
using ValSharp.Content;
using ValSharp.Core;
using ValSharp.DTOs;
using ValSharp.Interceptors;

namespace ValSharp_Demo
{
    internal class AIFunctions
    {
        private readonly ValClient _valClient;
        private readonly string AskedBy;
        private Lazy<ActiveMatchPlayer> askedByPlayer;

        public AIFunctions(ValClient valClient, string askedBy)
        {
            _valClient = valClient;
            AskedBy = askedBy;
            askedByPlayer = new(() => InGameCache.Instance.Match!.Players[AskedBy]);
        }

        public string CurrentGame()
        {
            var ig = InGameCache.Instance;
            var sb = new StringBuilder();

            Map? currentMap = ig.Maps.Value?.FirstOrDefault(map => map.Uuid == ig.Match!.MapID);
            GameMode? gameMode = ig.GameModes.Value?.FirstOrDefault(gm => gm.Uuid == ig.Match!.ModeID);

            if (currentMap is not null)
                sb.AppendLine($"Map:{currentMap.DisplayName}");

            if (gameMode is not null)
                sb.AppendLine($"Mode:{gameMode.DisplayName} Dur:{gameMode.Duration} RPH:{gameMode.RoundsPerHalf}");

            foreach (var kvp in ig.Match!.Players)
                sb.AppendLine(GetPlayerDetails(kvp.Value));

            return sb.ToString();
        }
        private string GetPlayerDetails(ActiveMatchPlayer? player)
        {
            var puuid = player!.Subject;

            if (!InGameCache.Instance.Agents!.TryGetValue(player.CharacterID, out Agent? agent))
                return string.Empty;

            PlayerName? pname = null;
            bool hasUsername = !player.PlayerIdentity.Incognito && (InGameCache.Instance.PlayerNames?.TryGetValue(puuid, out pname) ?? false);
            string name = hasUsername ? pname!.DisplayName : "?";
            string level = player.PlayerIdentity.HideAccountLevel ? "?" : player.PlayerIdentity.AccountLevel.ToString();
            string team = player.TeamID.ToLower().Equals(askedByPlayer.Value.TeamID.ToLower()) ? "1" : "2";
            string prefix = AskedBy == puuid ? "(Q)" : string.Empty;

            return $"{prefix}Agt:{agent.DisplayName} Name:{name} Lvl:{level} T:{team}";
        }

        [Description("Oyun içinde bir kostümü ve sahibini bulur. Opsiyonel silah adı verilirse sadece o silaha ait kostümler aranır")]
        public string FindSkin(FindSkinQueryDTO queryDTO)
        {
            string skinQuery = queryDTO.SkinQuery;
            Weapon? weapon = queryDTO.WeaponQuery is null ? null : InGameCache.Instance.FindWeapon(queryDTO.WeaponQuery);
            var skinSocketId = ValContent.Sockets[ValContent.SocketType.Skin];
            int maxTier = InGameCache.Instance.ContentTiers.Value!.Values.Max(x => x.Rank);
            var skinMap = InGameCache.Instance.Weapons!
                .SelectMany(w => w.Skins)
                .Where(s => s.Uuid != null)
                .ToDictionary(s => s.Uuid);

            var results = InGameCache.Instance.MatchLoadouts!
                .SelectMany(ml =>
                {
                    var matchPlayer = InGameCache.Instance.Match!.Players.GetValueOrDefault(ml.Subject);
                    if (matchPlayer == null) return Enumerable.Empty<(Skin skin, string subject)>();

                    var items = weapon != null
                        ? ml.Items.Where(i => i.Key == weapon.Uuid).Select(i => i.Value)
                        : ml.Items.Values;

                    return items
                        .Select(l =>
                        {
                            var socketItem = l.Sockets.GetValueOrDefault(skinSocketId)?.Item;
                            if (socketItem?.ID == null || !skinMap.TryGetValue(socketItem.ID, out Skin? skin))
                                return (skin: (Skin?)null, subject: ml.Subject);

                            return (skin: (Skin?)skin, subject: ml.Subject);
                        })
                        .Where(x => x.skin != null)
                        .Select(x => (skin: x.skin!, subject: x.subject));
                });

            var matched = FuzzyMatcher.GetMatches(results, skinQuery, x => x.skin.DisplayName);

            if (!matched.Any())
                return "Bulunamadı";

            return string.Join("|", matched.Select(x =>
            {
                var player = InGameCache.Instance.Match!.Players[x.subject];
                var details = GetPlayerDetails(player);
                ContentTier? contentTier = null;
                bool hasTier = x.skin.ContentTierUuid != null &&
                               InGameCache.Instance.ContentTiers.Value!.TryGetValue(x.skin.ContentTierUuid, out contentTier);
                string tierStr = !hasTier ? "-" : $"{contentTier!.Rank}/{maxTier} {contentTier.DisplayName}";

                return $"[{tierStr}]{x.skin.DisplayName} -> {details}";
            }));
        }

        [Description("Oyuncunun tüm silah kostümlerini getirir.")]
        public string PlayerSkin(PlayerSkinQuery queryDto)
        {
            string playerQuery = queryDto.PlayerQuery;
            string? skinQuery = queryDto.SkinQuery;

            string? targetSubject = null;

            if (!InGameCache.Instance.TryFindPlayer(playerQuery, out targetSubject, out bool hasMultipleAgents))
                return hasMultipleAgents ? "Çok oyuncu" : "Bulunamadı";

            var skinSocketId = ValContent.Sockets[ValContent.SocketType.Skin];
            int maxTier = InGameCache.Instance.ContentTiers.Value!.Values.Max(x => x.Rank);
            var skinMap = InGameCache.Instance.Weapons!
            .SelectMany(w => w.Skins)
            .Where(s => s.Uuid != null)
            .ToDictionary(s => s.Uuid);

            var playerSkins = InGameCache.Instance.MatchLoadouts!
                    .FirstOrDefault(ml => ml.Subject == targetSubject)
                    ?.Items?.Values?
                    .Select(l =>
                    {
                        var socketItem = l.Sockets.GetValueOrDefault(skinSocketId)?.Item;
                        if (socketItem?.ID == null || !skinMap.TryGetValue(socketItem.ID, out Skin? skin))
                            return new { displayName = "Standart", tier = "-" };

                        ContentTier? contentTier = null;
                        bool hasTier = skin.ContentTierUuid != null &&
                                       InGameCache.Instance.ContentTiers.Value!.TryGetValue(skin.ContentTierUuid, out contentTier);
                        string tierStr = !hasTier ? "-" : $"{contentTier!.Rank}/{maxTier} {contentTier.DisplayName}";

                        return new { displayName = skin.DisplayName, tier = tierStr };
                    });

            if (skinQuery is not null)
                playerSkins = FuzzyMatcher.GetMatches(playerSkins, skinQuery, s => s.displayName);

            if (playerSkins is null)
                return "Bulunamadı";

            return string.Join("|", playerSkins.Select(x => $"[{x.tier}]{x.displayName}"));
        }

        [Description("Oyuncunun belirli bir silah için kostümünü getirir.")]
        public string PlayerWeaponSkin(PlayerWeaponSkinQueryDTO queryDto)
        {
            string playerQuery = queryDto.PlayerQuery;
            string weaponQuery = queryDto.WeaponQuery;

            string? targetSubject = null;

            if (!InGameCache.Instance.TryFindPlayer(playerQuery, out targetSubject, out bool hasMultipleAgents))
                return hasMultipleAgents ? "Çok oyuncu" : "Bulunamadı";

            var weapon = InGameCache.Instance.FindWeapon(weaponQuery);

            if (weapon == null)
                return "Silah yok";

            var playerLoadout = InGameCache.Instance.MatchLoadouts!.FirstOrDefault(ml => ml.Subject == targetSubject);
            var skinSocketId = ValContent.Sockets[ValContent.SocketType.Skin];

            if (playerLoadout != null && playerLoadout.Items.TryGetValue(weapon.Uuid, out var weaponItem))
            {
                var skinUuid = weaponItem.Sockets[skinSocketId].Item.ID;
                var skin = weapon.Skins.FirstOrDefault(s => s.Uuid == skinUuid);
                return skin != null ? skin.DisplayName : "Standart";
            }

            return "Veri yok";
        }

        [Description("Oyuncunun grup arkadaşlarını getirir. Takım 1=soruyu soranın takımı, Takım 2=rakibi.")]
        public string PlayerParty(PlayerQueryDTO queryDto)
        {
            string playerQuery = queryDto.PlayerQuery;
            string? targetSubject = null;

            if (!InGameCache.Instance.TryFindPlayer(playerQuery, out targetSubject, out bool hasMultipleAgents))
                return hasMultipleAgents ? "Çok oyuncu" : "Bulunamadı";

            string? commonPartyId = InGameCache.Instance!.PlayerPartyLookup?.FirstOrDefault(x => x.Contains(targetSubject!))?.Key;

            if (commonPartyId is null)
                return "Grup yok";

            var puuidCollection = InGameCache.Instance!.PlayerPartyLookup![commonPartyId!];
            var playerRequester = InGameCache.Instance.Match!.Players[AskedBy];

            var resultList = puuidCollection
                .Where(puuid => puuid != targetSubject)
                .Select(puuid =>
                {
                    var owner = InGameCache.Instance.Match!.Players[puuid];
                    var agent = InGameCache.Instance.Agents![owner.CharacterID.ToUpper()];
                    bool isEnemy = !owner.TeamID.ToLower().Equals(playerRequester.TeamID.ToLower());
                    bool hasDuplicateAgent = InGameCache.Instance.Match.Players.Values
                        .Count(p => p.CharacterID.ToLower().Equals(owner.CharacterID.ToLower())) > 1;
                    return hasDuplicateAgent ? $"{(isEnemy ? "2" : "1")}.{agent.DisplayName}" : agent.DisplayName;
                });

            return "Grup:" + string.Join(",", resultList);
        }

        [Description("Oyuncunun rütbe bilgilerini getirir.")]
        public string PlayerRank(PlayerQueryDTO queryDto)
        {
            string playerQuery = queryDto.PlayerQuery;
            string? targetSubject = null;

            if (!InGameCache.Instance.TryFindPlayer(playerQuery, out targetSubject, out bool hasMultipleAgents))
                return hasMultipleAgents ? "Çok oyuncu" : "Bulunamadı";

            var activeSeason = InGameCache.Instance.Seasons!.FirstOrDefault(s => s.IsActive);
            var rankDetails = InGameCache.Instance.GetPlayerRankDetails(targetSubject!, activeSeason!.ID);

            if (rankDetails is null)
                return "Rütbe yok";

            var tiers = InGameCache.Instance.CompetitiveTiers.Value!.Last().Tiers;
            return $"Peak:{tiers[rankDetails.Value.Peak].TierName} Avg:{tiers[rankDetails.Value.Avg].TierName} Cur:{tiers[rankDetails.Value.Current].TierName}";
        }

        public class PlayerQueryDTO
        {
            [Description("Oyuncunun kullanıcı adı ve ya ajan adı. " +
    "Kullanıcı adı mevcut değilse ve oyunda aynı ajandan birden fazla varsa (biri kendi takımınızda, biri rakip takımda), " +
    "ajan adının başına '1.' veya '2.' ön ekini koyun. 1. kendi takımınızda, 2. rakip takımda olduğu anlamına gelir.")]
            public required string PlayerQuery { get; set; }
        }

        public class PlayerSkinQuery : PlayerQueryDTO
        {
            [Description("Oyun içi kostümün adı. Boş bırakılırsa (null) oyuncunun tüm kostümleri döndürülür. " +
    "Oyuncunun kostümleri arasında arama yapmak için kısmi isimler verebilirsiniz. " +
    "Örn: 'Yağmacı', oyuncunun sahip olduğu tüm Yağmacı kostümlerini getirecektir.")]
            public required string? SkinQuery { get; set; }
        }

        public class PlayerWeaponSkinQueryDTO : PlayerQueryDTO
        {
            [Description("Oyun içi silahın adı.")]
            public required string WeaponQuery { get; set; }
        }

        public class FindSkinQueryDTO
        {
            [Description("Oyun içi silah kostümünün adı.")]
            public required string SkinQuery { get; set; }

            [Description("İsteğe bağlı silah adı. Belirtilirse, oyun içinde belirtilen silah için kostüm araması yapılacaktır.")]
            public required string? WeaponQuery { get; set; }
        }
    }
}