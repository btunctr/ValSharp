using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValSharp.DTOs
{
    public class Entitlement
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; }

        [JsonProperty("issuer")]
        public string Issuer { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("entitlements")]
        public object[] Entitlements { get; set; }
    }
}
