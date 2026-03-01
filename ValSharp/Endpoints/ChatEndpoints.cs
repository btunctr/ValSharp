using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValSharp.Core;
using ValSharp.Interceptors;
using ValSharp.DTOs;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace ValSharp.Endpoints
{
    public class ChatEndpoints
    {
        public event Func<MessageEventArgs, Task>? MessageReceived
        {
            add => interceptor.MessageReceived += value;
            remove => interceptor.MessageReceived -= value;
        }

        public event Func<OutgoingMessageEventArgs, Task>? OutgoingMessage
        {
            add => interceptor.OutgoingMessage += value;
            remove => interceptor.OutgoingMessage -= value;
        }


        private readonly ILogger logger;
        private readonly ValAuth auth;
        private readonly ValorantInterceptor interceptor;
        private readonly ConcurrentDictionary<string, int> channelMessageCounts = new();

        internal ChatEndpoints(ILogger logger, ValAuth auth, ValorantInterceptor interceptor, ValClient valClient)
        {
            this.logger = logger;
            this.auth = auth;
            this.interceptor = interceptor;

            this.OutgoingMessage += Chat_OutgoingMessage;
            valClient.GameStateChanged += ValClient_GameStateChanged;
        }
        private ChatEndpoints() { }


        public GameChat[] GetAllChats()
        {
            return HttpUtils.SendRequest<GameChat[]>($"https://127.0.0.1:{auth.LockFile[2]}/chat/v6/conversations", RequestMethod.GET,
                    headers: new NameValueCollection().AddDefaultHeaders(auth, EXTMAuthMethod.Basic), innerObj: "conversations") ?? Array.Empty<GameChat>();
        }

        public ChatMessage[] GetChatHistory(string cid)
        {
            return HttpUtils.SendRequest<ChatMessage[]>(
                    $"https://127.0.0.1:{auth.LockFile[2]}/chat/v6/messages?cid={cid}",
                    RequestMethod.GET, headers: new NameValueCollection().AddDefaultHeaders(auth, EXTMAuthMethod.Basic), innerObj: "messages") ?? Array.Empty<ChatMessage>();
        }

        public ChatParticipant[] GetChatParticipants(string cid)
        {
            return HttpUtils.SendRequest<ChatParticipant[]>(
                    $"https://127.0.0.1:{auth.LockFile[2]}/chat/v5/participants",
                    RequestMethod.GET, headers: new NameValueCollection()
                                 .AddDefaultHeaders(auth, EXTMAuthMethod.Basic), innerObj: "participants") ?? Array.Empty<ChatParticipant>();
        }

        public GameChat[] GetGameChat()
        {
            return HttpUtils.SendRequest<GameChat[]>(
                    $"https://127.0.0.1:{auth.LockFile[2]}/chat/v6/conversations/ares-coregame",
                    RequestMethod.GET,
                    headers: new NameValueCollection()
                                 .AddDefaultHeaders(auth, EXTMAuthMethod.Basic), innerObj: "conversations") ?? Array.Empty<GameChat>();
        }

        public GameChat[] GetPartyChat()
        {
            return HttpUtils.SendRequest<GameChat[]>(
                     $"https://127.0.0.1:{auth.LockFile[2]}/chat/v6/conversations/ares-parties",
                     RequestMethod.GET,
                     headers: new NameValueCollection()
                                  .AddDefaultHeaders(auth, EXTMAuthMethod.Basic), innerObj: "conversations") ?? Array.Empty<GameChat>();
        }

        public GameChat[] GetPreGameChat()
        {
            return HttpUtils.SendRequest<GameChat[]>(
                    $"https://127.0.0.1:{auth.LockFile[2]}/chat/v6/conversations/ares-pregame",
                    RequestMethod.GET,
                    headers: new NameValueCollection()
                                 .AddDefaultHeaders(auth, EXTMAuthMethod.Basic), innerObj: "conversations") ?? Array.Empty<GameChat>();
        }


        private void ValClient_GameStateChanged(GameState newState, GameState oldState)
        {
            channelMessageCounts.Clear();
        }

        private Task Chat_OutgoingMessage(OutgoingMessageEventArgs arg)
        {
            logger?.LogDebug("New outgoing message: {Body}, Id: {Id}, To: {To}",
                arg.Body, arg.Id, arg.To);
            return Task.CompletedTask;
        }

        public async Task SendAsync(string to, ChatChannelType channelType, string body)
        {
            if (interceptor == null || !interceptor.IsStarted)
                return;

            string generatedId = Guid.NewGuid().ToString("N");

            var msg = new OutgoingChatMessage(channelType, generatedId, to, body, auth.Subject);

            await interceptor.SendToServer(msg.Xml);

            logger?.LogDebug("Sent new message. Message: {Message}, Id: {Id}, To: {To}",
                msg.Body, msg.Id, to);
        }

        public async Task SendToClientAsync(ChatChannelType type, string from, string to, string jid, string body)
        {
            if (interceptor == null || !interceptor.IsStarted)
                return;

            string generatedId = Guid.NewGuid().ToString();
            string xml = $"<message to='{to}' from='{from}' stamp='{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")}' id='{generatedId}' type='{OutgoingChatMessage.ParseChannel(type)}'>" +
                $"<x xmlns='http://jabber.org/protocol/muc#user'>" +
                $"<item jid='{jid}'/>" +
                $"</x><body>{body}</body>" +
                $"</message>";

            await interceptor.SendToClient(xml);
            logger?.LogDebug("Sent new message to client. Message: {Message}, Id: {Id}", body, generatedId);
        }
    }
}
