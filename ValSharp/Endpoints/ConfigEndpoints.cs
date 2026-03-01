using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValSharp.Core;
using Microsoft.Extensions.Logging;
using ValSharp.DTOs;

namespace ValSharp.Endpoints
{
    public class ConfigEndpoints
    {
        private readonly ILogger _logger;
        private readonly ValAuth _auth;

        internal ConfigEndpoints(ILogger logger, ValAuth auth)
        {
            _logger = logger;
            _auth = auth;
        }

        private ConfigEndpoints() { }

        public RiotClientConfig? GetClientConfi()
        {
            return HttpUtils.SendRequest<RiotClientConfig>(
                    "https://clientconfig.rpg.riotgames.com/api/v1/config/player?app=Riot%20Client", RequestMethod.GET,
                    headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }
    }
}
