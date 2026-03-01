using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValSharp.Core;
using ValSharp.DTOs;

namespace ValSharp.Endpoints
{
    public class MatchEndpoints
    {
        private readonly ILogger _logger;
        private readonly ValAuth _auth;

        internal MatchEndpoints(ILogger logger, ValAuth auth)
        {
            _logger = logger;
            _auth = auth;
        }

        private MatchEndpoints() { }

        #region Current Game
        public ActiveMatch? GetActiveMatchById(string matchId)
        {
            return HttpUtils.SendRequest<ActiveMatch>($"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/core-game/v1/matches/{matchId}", RequestMethod.GET,
                                                              headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public CurrentGamePlayer? GetPlayerInActiveMatch(string puuid)
        {
            return HttpUtils.SendRequest<CurrentGamePlayer>($"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/core-game/v1/players/{puuid}",
                 RequestMethod.GET, headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public PlayerMatchLoadout[]? GetActiveMatchLoadouts(string matchId)
        {
            var token = HttpUtils.SendRequest<JToken>($"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/core-game/v1/matches/{matchId}/loadouts", RequestMethod.GET,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));

            var loadouts = token?["Loadouts"]?.ToObject<JArray>()?.Select(t =>
            {
                var characterId = t["CharacterID"]!.ToString()!;
                var pml = t["Loadout"]!.ToObject<PlayerMatchLoadout>()!;
                pml.CharacterID = characterId!;
                return pml;
            });

            return loadouts!.ToArray();
        }
        #endregion
        #region Pre Game
        public PreGameMatch? GetPreGameMatchById(string preGameMatchId)
        {
            return HttpUtils.SendRequest<PreGameMatch>($"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/pregame/v1/matches/{preGameMatchId}", RequestMethod.GET,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public CurrentGamePlayer? GetPlayerInPreGame(string puuid)
        {
            return HttpUtils.SendRequest<CurrentGamePlayer>($"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/pregame/v1/players/{puuid}", RequestMethod.GET,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public void LockAgentInPreGame(string preGameMatchId, string agentId)
        {
            _ = HttpUtils.SendRequest($"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/pregame/v1/matches/{preGameMatchId}/lock/{agentId}", RequestMethod.POST,
                 headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public void QuitePreGameMatch(string preGameMatchId)
        {
            _ = HttpUtils.SendRequest($"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/pregame/v1/matches/{preGameMatchId}/quit", RequestMethod.POST,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public void SelectAgentInPreGame(string preGameMatchId, string agentId)
        {
            _ = HttpUtils.SendRequest($"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/pregame/v1/matches/{preGameMatchId}/select/{agentId}", RequestMethod.POST,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }
        #endregion
        #region Finished Game
        public dynamic? GetPlayerMatchHistory(string puuid, int startIndex = 0, int endIndex = 20, string? queue = null)
        {
            string query = $"https://pd.{_auth.Region}.a.pvp.net/match-history/v1/history/{puuid}?startIndex={startIndex}&endIndex={endIndex}{(queue != null ? $"&queue={queue}" : string.Empty)}";
            return HttpUtils.SendRequest<JToken>(query, RequestMethod.GET, headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        /// <summary>
        /// Finished match
        /// </summary>
        public dynamic? GetMatchDetailsById(string matchId)
        {
            return HttpUtils.SendRequest($"https://pd.{_auth.Region}.a.pvp.net/match-details/v1/matches/{matchId}", RequestMethod.GET,
                                                         headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }
        #endregion
    }
}
