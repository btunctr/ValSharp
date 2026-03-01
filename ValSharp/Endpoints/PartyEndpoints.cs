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
    public class PartyEndpoints
    {
        private readonly ILogger _logger;
        private readonly ValAuth _auth;

        internal PartyEndpoints(ILogger logger, ValAuth auth)
        {
            _logger = logger;
            _auth = auth;
        }

        private PartyEndpoints() { }

        public Party? ChangeQueue(string partyId, string queueId)
        {
            return HttpUtils.SendRequest<Party>(
                $"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/parties/v1/parties/{partyId}/queue",
                RequestMethod.POST,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public Party? EnterMatchmakingQueue(string partyId, string queueId)
        {
            return HttpUtils.SendRequest<Party>(
                $"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/parties/v1/parties/{partyId}/matchmaking/join",
                RequestMethod.POST,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer),
                body: new { queueId });
        }

        public CommunicationToken? GetChatToken(string partyId)
        {
            return HttpUtils.SendRequest<CommunicationToken>(
                $"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/parties/v1/parties/{partyId}/muctoken",
                RequestMethod.GET,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public Party? GetParty(string partyId)
        {
            return HttpUtils.SendRequest<Party>(
                $"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/parties/v1/parties/{partyId}",
                RequestMethod.GET,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public PartyPlayer? GetPartyPlayer(string partyId, string puuid)
        {
            return HttpUtils.SendRequest<PartyPlayer>(
                $"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/parties/v1/players/{puuid}",
                RequestMethod.GET,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public CommunicationToken? GetVoiceToken(string partyId)
        {
            return HttpUtils.SendRequest<CommunicationToken>(
                $"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/parties/v1/parties/{partyId}/voicetoken",
                RequestMethod.GET,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public Party? Invite(string partyId, string playerName, string playerTag)
        {
            return HttpUtils.SendRequest<Party>(
                $"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/parties/v1/parties/{partyId}/invites/name/{playerName}/tag/{playerTag}",
                RequestMethod.POST,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public Party? LeaveMatchmakingQueue(string partyId)
        {
            return HttpUtils.SendRequest<Party>(
                $"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/parties/v1/parties/{partyId}/matchmaking/leave",
                RequestMethod.POST,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public void RemovePlayer(string puuid)
        {
            HttpUtils.SendRequest<object>(
                $"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/parties/v1/players/{puuid}",
                RequestMethod.DELETE,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public void SendJoinRequest(string partyId)
        {
            HttpUtils.SendRequest<object>(
                $"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/parties/v1/parties/{partyId}/request",
                RequestMethod.POST,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }

        public Party? SetReady(string partyId, string puuid)
        {
            return HttpUtils.SendRequest<Party>(
                $"https://glz-{_auth.Region}-1.{_auth.Region}.a.pvp.net/parties/v1/parties/{partyId}/members/{puuid}/setReady",
                RequestMethod.POST,
                headers: new NameValueCollection().AddDefaultHeaders(_auth, EXTMAuthMethod.Bearer));
        }
    }
}
