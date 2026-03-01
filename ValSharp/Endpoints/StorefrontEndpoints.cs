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
    public class StorefrontEndpoints
    {
        private readonly ILogger _logger;
        private readonly ValAuth _auth;

        internal StorefrontEndpoints(ILogger logger, ValAuth auth)
        {
            _logger = logger;
            _auth = auth;
        }

        private StorefrontEndpoints() { }

        public StorefrontOffer[]? GetPrices()
        {
            var response = HttpUtils.SendRequest<JObject>(
                $"https://pd.{_auth.Region}.a.pvp.net/store/v1/offers/",
                RequestMethod.GET,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));

            return response?["Offers"]?.ToObject<StorefrontOffer[]>();
        }

        public Storefront? GetStorefront()
        {
            return HttpUtils.SendRequest<Storefront>(
                $"https://pd.{_auth.Region}.a.pvp.net/store/v2/storefront/{_auth.Subject}",
                RequestMethod.GET,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public Dictionary<string, int>? GetWallet()
        {
            var response = HttpUtils.SendRequest<JObject>(
                $"https://pd.{_auth.Region}.a.pvp.net/store/v1/wallet/{_auth.Subject}",
                RequestMethod.GET,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));

            return response?["Balances"]?.ToObject<Dictionary<string, int>>();
        }
    }
}
