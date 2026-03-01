using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValSharp.Core;
using ValSharp.DTOs;

namespace ValSharp
{
    public static class ExtensionMethods
    {
        internal static NameValueCollection AddDefaultHeaders(this NameValueCollection collection, ValAuth auth, EXTMAuthMethod authMethod)
        {
            collection.Add("X-Riot-ClientVersion",auth.Version);
            collection.Add("X-Riot-ClientPlatform", ValAuth.XRiotClientPlatform);
            collection.Add("X-Riot-Entitlements-JWT", auth.EntitlementToken);

            if (authMethod != EXTMAuthMethod.None)
                collection.Add("Authorization", authMethod == EXTMAuthMethod.Basic ? auth.BasicAuth : auth.BearerAuth);

            return collection;
        }

        public static void WriteString(this Stream stream, string str)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(str);
            stream.Write(buffer, 0, buffer.Length);
        }

        public static byte[] ReadAsBytes(this Stream input)
        {
            var buffer = new byte[16 * 1024];
            var ms = new MemoryStream();
            int read;

            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                ms.Write(buffer, 0, read);

            return ms.ToArray();
        }

        public static bool CompareCaseInsensitive(this string str, string str2) => str.ToUpper().Equals(str2.ToUpper());

        public static ChatChannelType ParseChannel(this string channel) => channel.ToLower() switch
        {
            "groupchat" => ChatChannelType.GroupChat,
            "system" => ChatChannelType.System,
            "chat" => ChatChannelType.Chat,
            _ => ChatChannelType.Unknown,
        };

        public static string ParseChannel(this ChatChannelType type) => type switch
        {
            ChatChannelType.Chat => "chat",
            ChatChannelType.GroupChat => "groupchat",
            ChatChannelType.System => "system",
            _ => "Unknown",
        };
    }

    internal enum EXTMAuthMethod
    {
        None,
        Basic,
        Bearer
    }
}
