using Newtonsoft.Json.Linq;
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
    public class IGContentEndpoints
    {
        private readonly ILogger _logger;
        private readonly ValAuth _auth;

        internal IGContentEndpoints(ILogger logger, ValAuth auth)
        {
            _logger = logger;
            _auth = auth;
        }

        private IGContentEndpoints() { }

        public IGEvent[]? GetEvents()
        {
            var response = HttpUtils.SendRequest<JObject>(
                $"https://shared.{_auth.Region.ToString().ToLower()}.a.pvp.net/content-service/v3/content",
                RequestMethod.GET,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));

            if (response == null)
                return null;

            return response?["Events"]?.ToObject<IGEvent[]>();
        }

        public IGSeason[]? GetSeasons()
        {
            var response = HttpUtils.SendRequest<JObject>(
                $"https://shared.{_auth.Region.ToString().ToLower()}.a.pvp.net/content-service/v3/content",
                RequestMethod.GET,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));

            if (response == null)
                return null;

            return response?["Seasons"]?.ToObject<IGSeason[]>();
        }
    }
}
