using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValSharp.Core;
using ValSharp.Interceptors;
using ValSharp.DTOs;

namespace ValSharp.Endpoints
{
    public class PlayerEndpoints
    {
        public event Func<PresenceEventArgs, Task>? PresenceReceived
        {
            add => interceptor.PresenceReceived += value;
            remove => interceptor.PresenceReceived -= value;
        }

        private readonly ILogger _logger;
        private readonly ValAuth _auth;
        private readonly ValorantInterceptor interceptor;

        internal PlayerEndpoints(ILogger logger, ValAuth auth, ValorantInterceptor interceptor)
        {
            _logger = logger;
            _auth = auth;
            this.interceptor = interceptor;
        }

        private PlayerEndpoints() { }

        public PlayerName[]? GetPlayerNames(params string[] idArray)
        {
            return HttpUtils.SendRequest<PlayerName[]>($"https://pd.{_auth.Region}.a.pvp.net/name-service/v2/players", RequestMethod.PUT,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer), idArray);
        }

        public PlayerName? GetSelfUsername() => GetPlayerNames(new string[] { _auth.Subject })?.FirstOrDefault();

        public Friend[]? GetFriends()
        {
            return HttpUtils.SendRequest<Friend[]>($"https://127.0.0.1:{_auth.LockFile[2]}/chat/v4/friends",
                RequestMethod.GET,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Basic));
        }

        public MMR? GetMMR(string? playerId = null)
        {
            var response = GetMMRResponse(playerId);

            if (response == null)
                return null;

            return JsonConvert.DeserializeObject<MMR>(response.Content);
        }

        public dynamic? GetMMRDynamic(string? playerId = null)
        {
            var response = GetMMRResponse(playerId);

            if (response == null)
                return null;

            return JObject.Parse(response.Content);
        }

        private IRestResponse? GetMMRResponse(string? playerId)
        {
            return HttpUtils.SendRequest($"https://pd.{_auth.Region}.a.pvp.net/mmr/v1/players/{playerId ?? _auth.Subject}", RequestMethod.GET,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public UserPresence[]? GetPresence()
        {
            var response = HttpUtils.SendRequest($"https://127.0.0.1:{_auth.LockFile[2]}/chat/v4/presences", RequestMethod.GET,
                                                 headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Basic));

            if (response == null || !response.IsSuccessful)
                return null;

            UserPresence[] presences = JsonConvert.DeserializeObject<UserPresence[]>(JObject.Parse(response.Content)["presences"]!.ToString())!;

            for (int i = 0; i < presences.Length; i++)
            {
                if (presences[i].Private == null)
                    continue;

                try
                {
                    presences[i].PrivateInfo = JsonConvert.DeserializeObject<UserPresenceDetails>(Encoding.UTF8.GetString(Convert.FromBase64String(presences[i].Private)))!;
                }
                catch
                {
                    continue;
                }
            }

            return presences;
        }
    }
}
