using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValSharp.Core
{
    public class ValAuth
    {
        public const string XRiotClientPlatform = "ew0KCSJwbGF0Zm9ybVR5cGUiOiAiUEMiLA0KCSJwbGF0Zm9ybU9TIjogIldpbmRvd3MiLA0KCSJwbGF0Zm9ybU9TVmVyc2lvbiI6ICIxMC4wLjE5MDQyLjEuMjU2LjY0Yml0IiwNCgkicGxhdGZvcm1DaGlwc2V0IjogIlVua25vd24iDQp9";
        public string BearerAuth => string.Concat("Bearer ", this.AccessToken);
        public string PASToken { get; set; }
        public string EntitlementToken { get; set; }
        public string AccessToken { get; set; }
        public string BasicAuth { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("region")]
        public Region Region { get; set; }
        public string[] LockFile { get; set; }
        public string LockFilePath { get; set; }

        public async Task<bool> TryLocalAuthAsync(string lockFilePath)
        {
            if (!File.Exists(lockFilePath))
                return false;

            string lockfile = string.Empty;

            try
            {
                using (var fs = new FileStream(lockFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default))
                    lockfile = sr.ReadToEnd();
            }
            catch
            {}

            if (lockfile == string.Empty)
                return false;

            var response = HttpUtils.SendRequest("https://valorant-api.com/v1/version", RequestMethod.GET);

            if (response == null || !response.IsSuccessful)
                return false;

            var versionJsonObj = JObject.Parse(response.Content)["data"]!;
            string[] lockFileSplit = lockfile.Split(':');

            string valBasicAuth = $"Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"riot:{lockFileSplit[3]}"))}";

            var entResponse = HttpUtils.SendRequest($"https://127.0.0.1:{lockFileSplit[2]}/entitlements/v1/token",
                                headers: new NameValueCollection()
                                {
                                    ["Authorization"] = valBasicAuth,
                                });

            if (entResponse == null || !entResponse.IsSuccessful)
                return false;

            JObject entObj = JObject.Parse(entResponse.Content);


            LockFile = lockFileSplit;
            Version = versionJsonObj["riotClientVersion"]!.Value<string>()!;
            AccessToken = (string)entObj["accessToken"]!;
            EntitlementToken = (string)entObj["token"]!;
            Subject = (string)entObj["subject"]!;
            BasicAuth = valBasicAuth;
            LockFilePath = lockFilePath;

            var regionResponse = HttpUtils.SendRequest($"https://127.0.0.1:{lockFileSplit[2]}/player-affinity/product/v1/token", RequestMethod.POST, headers: new NameValueCollection()
            {
                ["Authorization"] = valBasicAuth
            }, body: new { product = "valorant" });


            if (regionResponse == null || !regionResponse.IsSuccessful)
                return false;

            JObject regObj = JObject.Parse(regionResponse.Content);
            string reg = (string)regObj["affinities"]!["live"]!.ToString().ToUpper();

            if (reg == "NA") Region = Region.NA;
            else if (reg == "AP") Region = Region.AP;
            else if (reg == "EU") Region = Region.EU;
            else if (reg == "KO") Region = Region.KO;

            var pasResponse = HttpUtils.SendRequest("https://riot-geo.pas.si.riotgames.com/pas/v1/service/chat", RequestMethod.GET,
                                                             headers: new NameValueCollection().AddDefaultHeaders(this, EXTMAuthMethod.Bearer));


            if (pasResponse != null && pasResponse.IsSuccessful)
                PASToken = pasResponse.Content;

            return true;
        }
    }

    public enum Region
    {
        NA,
        EU,
        AP,
        KO
    }
}
