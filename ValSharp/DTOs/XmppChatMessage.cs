using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValSharp.DTOs
{
    public class IncomingChatMessage
    {
        public ChatChannelType ChannelType => OutgoingChatMessage.ParseChannel(Type);
        public string Type { get; }
        public string Id { get; }
        public string Stamp { get; }
        public string From { get; }
        public string To { get; }
        public string Body { get; }
        public string Subject { get; }

        internal IncomingChatMessage(string type, string id, string stamp, string from, string to, string body, string subject)
        {
            Type = type;
            Id = id;
            Stamp = stamp;
            From = from;
            To = to;
            Body = body;
            Subject = subject;
        }

        private IncomingChatMessage() { }
    }

    public class OutgoingChatMessage
    {
        public string Id { get; }
        public string To { get; }
        public string From { get; }
        public string Body { get; private set; }
        public ChatChannelType ChannelType { get; }
        public object? Data { get; set; }
        public string Type => ParseChannel(ChannelType);
        public string Xml => $@"<message id=""{Id}"" to=""{To}"" type=""{Type}""><body>{Body}</body></message>";

        public OutgoingChatMessage(ChatChannelType channelType, string id, string to, string body, string from)
        {
            ChannelType = channelType;
            Id = id;
            To = to;
            Body = body;
            From = from;
        }

        public void SetBody(string newBody) => this.Body = newBody;

        public static ChatChannelType ParseChannel(string channel) => channel.ToLower() switch
        {
            "groupchat" => ChatChannelType.GroupChat,
            "system" => ChatChannelType.System,
            "chat" => ChatChannelType.Chat,
            _ => ChatChannelType.Unknown,
        };

        public static string ParseChannel(ChatChannelType type) => type switch
        {
            ChatChannelType.Chat => "chat",
            ChatChannelType.GroupChat => "groupchat",
            ChatChannelType.System => "system",
            _ => "Unknown",
        };
    }
}
