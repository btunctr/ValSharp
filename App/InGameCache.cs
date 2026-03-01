using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ValSharp.Content;
using ValSharp.Core;
using ValSharp.DTOs;

namespace ValSharp_Demo
{
    internal class InGameCache
    {
        const string language = "tr-TR";
        public static InGameCache Instance { get; private set; } = null!;

        private readonly ValClient _valClient;
        private readonly ILogger<InGameCache> _logger;

        public Dictionary<string, MMR>? PlayerMMR = new();
        public IGSeason[]? Seasons;
        public ActiveMatch? Match;
        public PlayerMatchLoadout[]? MatchLoadouts;
        public Dictionary<string, PlayerName>? PlayerNames;
        public ILookup<string, string>? PlayerPartyLookup;

        public Lazy<Map[]?> Maps = new(() => ValContent.GetMaps(language));
        public Lazy<GameMode[]?> GameModes = new(() => ValContent.GetGamemodes(language));
        public Lazy<CompetitiveTier[]?> CompetitiveTiers = new(() => ValContent.GetCompetitiveTiers(language));
        public Lazy<Dictionary<string, ContentTier>?> ContentTiers = new(() => ValContent.GetContentTiers(language)?.ToDictionary(x => x.Uuid));

        public Dictionary<string, Agent>? Agents;
        public Weapon[]? Weapons;

        public bool IsMatchDataLoaded => Match != null && MatchLoadouts != null && PlayerNames != null;
        public bool IsContentDataLoaded => Weapons != null && Agents != null;

        public InGameCache(ValClient client, ILoggerFactory loggerFactory)
        {
            _valClient = client;
            _logger = loggerFactory.CreateLogger<InGameCache>();
            Instance = this;
            _valClient.GameStateChanged += OnGameStateChange;
        }

        private void OnGameStateChange(GameState newState, GameState oldState)
        {
            if (newState == GameState.InPreGame || newState == GameState.InGame)
                LoadActiveMatch();
        }

        public void LoadContent()
        {
            Seasons = _valClient.InGameContent.GetSeasons();

            Weapons = ValContent.GetWeapons(language)!;
            Thread.Sleep(100);

            Agents = ValContent.GetAgents(true, language)!.ToDictionary(x => x.Uuid.ToUpper());
        }

        public void LoadActiveMatch()
        {
            var matchPlayer = _valClient.Match.GetPlayerInActiveMatch(_valClient.Auth.Subject);

            if (matchPlayer == null)
                return;

            Match = _valClient.Match.GetActiveMatchById(matchPlayer.MatchID)!;
            MatchLoadouts = _valClient.Match.GetActiveMatchLoadouts(matchPlayer.MatchID)!;
            PlayerNames = _valClient.Player.GetPlayerNames(Match.Players.Keys.ToArray())!.ToDictionary(x => x.Subject);

            var presences = _valClient.Player.GetPresence();

            if (presences is not null)
                PlayerPartyLookup = presences.Where(x => x.PrivateInfo?.PartyId is not null)?.ToLookup(x => x.PrivateInfo!.PartyId, x => x.Puuid);
        }

      
        public Weapon? FindWeapon(string name) =>
            FuzzyMatcher.GetBestMatch(Weapons, name, w => w.DisplayName);

        public Skin? FindSkin(Weapon weapon, string skinName) =>
            FuzzyMatcher.GetBestMatch(weapon.Skins, skinName, s => s.DisplayName);

        public Agent? FindAgent(string agentName) =>
            FuzzyMatcher.GetBestMatch(Agents?.Values, agentName, a => a.DisplayName);

        public bool TryFindPlayer(string playerQuery, out string? targetSubject, out bool hasMultipleAgents)
        {
            hasMultipleAgents = false;
            targetSubject = null;

            int teamIndex = -1;

            var match = Regex.Match(playerQuery, @"^(?<index>\s*[12])\.?(?<query>[a-zA-Z]+)$");
            if (match.Success)
            {
                try
                {
                    teamIndex = int.Parse(match.Groups["index"].Value);
                    playerQuery = match.Groups["query"].Value;
                }
                catch { teamIndex = -1; }
            }

            //todo teamIndex 

            var playerMatch = FuzzyMatcher.GetBestMatch(PlayerNames!, playerQuery, kvp => kvp.Value.DisplayName);

            if (playerMatch.Key != null)
            {
                targetSubject = playerMatch.Key;
            }
            else
            {
                var agent = FindAgent(playerQuery);
                if (agent != null)
                {
                    var playersWithAgent = Match!.Players.Values
                        .Where(p => p.CharacterID.ToLower().Equals(agent.Uuid.ToLower()))
                        .ToList();

                    if (playersWithAgent.Count > 1)
                    {
                        hasMultipleAgents = true;
                        return false;
                    }

                    targetSubject = playersWithAgent.FirstOrDefault()?.Subject;
                }
            }

            if (targetSubject == null)
                return false;

            return true;
        }

        public (int Current, int Avg, int Peak)? GetPlayerRankDetails(string puuid, string activeSeason)
        {
            try
            {
                MMR? playerMMR = null;

                if (PlayerMMR is null) return null;

                if (!PlayerMMR.TryGetValue(puuid, out playerMMR))
                    playerMMR = _valClient.Player.GetMMR(puuid);

                if (playerMMR is null)
                    return null;

                PlayerMMR.TryAdd(puuid, playerMMR);

                int max_rank = 0;
                int avg_rank = 0;

                if (playerMMR.QueueSkills != null && playerMMR.QueueSkills.ContainsKey("competitive"))
                {
                    var seasons = playerMMR.QueueSkills["competitive"]?.SeasonalInfoBySeasonID;
                    if (seasons != null)
                    {
                        foreach (var season in seasons)
                        {
                            if (season.Value == null)
                                continue;

                            int sRank = season.Value.Rank;

                            if (ValContent.BeforeAscendantSeasons.Any(x => x?.ToLower() == season.Value.SeasonID?.ToLower()))
                            {
                                if (sRank > 20)
                                    sRank += 3;
                            }
                            if (sRank > max_rank)
                            {
                                max_rank = sRank;
                            }
                            avg_rank += sRank;
                        }

                        if (seasons.Count() < 1)
                            avg_rank = 0;
                        else
                            avg_rank /= seasons.Count();

                        var d = playerMMR.QueueSkills["competitive"].SeasonalInfoBySeasonID;
                        int current_rank = !d.ContainsKey(activeSeason) ? 0 : d[activeSeason]?.CompetitiveTier ?? 0;

                        return (current_rank, avg_rank, max_rank);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user ranks");
            }

            return null;
        }
    }
}
