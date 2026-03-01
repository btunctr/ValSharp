using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using ValSharp.DTOs;

namespace ValSharp.Interceptors;

internal class ValorantInterceptor : RiotXMPPIntercepter
{
    public event Func<PresenceEventArgs, Task>? PresenceReceived;
    public event Func<MessageEventArgs, Task>? MessageReceived;
    public event Func<OutgoingMessageEventArgs, Task>? OutgoingMessage;

    protected ILogger? Logger;
    private volatile bool isReady = false;

    public ValorantInterceptor(string httpHost = "127.0.0.1", int httpPort = 35479, ILogger? logger = null)
    : base(httpHost, httpPort, logger)
    {
        Logger = logger;
    }

    protected override async Task HookOnIncomingMessage(string stanza)
    {
        if (!TryParseXml(stanza, out var xml)) return;

        switch (xml!.Name.LocalName.ToLowerInvariant())
        {
            case "presence":
                {
                    var ePres = PresenceEventArgs.Parse(xml, stanza);

                    if (PresenceReceived is null)
                        return;

                    Delegate[] invocationList = PresenceReceived.GetInvocationList();
                    Task[] handlerTasks = new Task[invocationList.Length];

                    for (int i = 0; i < invocationList.Length; i++)
                    {
                        handlerTasks[i] = ((Func<PresenceEventArgs, Task>)invocationList[i])(ePres);
                    }

                    await Task.WhenAll(handlerTasks);


                    if (!isReady)
                    {
                        isReady = true;
                        Logger?.LogInformation("Interceptor ready");
                    }
                }
                break;

            case "message":
                {
                    var eMsg = MessageEventArgs.Parse(xml, stanza);

                    if (eMsg.Jid is not null && eMsg.SenderPuuid is not null)
                    {
                        if (MessageReceived is null)
                            return;

                        Delegate[] invocationList = MessageReceived.GetInvocationList();
                        Task[] handlerTasks = new Task[invocationList.Length];

                        for (int i = 0; i < invocationList.Length; i++)
                        {
                            handlerTasks[i] = ((Func<MessageEventArgs, Task>)invocationList[i])(eMsg);
                        }

                        await Task.WhenAll(handlerTasks);
                    }
                }
                break;
        }
    }

    protected override async Task HookOnOutgoingMessage(StringBuilder message)
    {
        string raw = message.ToString();
        if (!TryParseXml(raw, out var xml)) return;

        if (xml!.Name.LocalName.Equals("message", StringComparison.OrdinalIgnoreCase))
        {
            var args = OutgoingMessageEventArgs.Parse(xml, raw);

            if (OutgoingMessage is null)
                return;

            Delegate[] invocationList = OutgoingMessage.GetInvocationList();
            Task[] handlerTasks = new Task[invocationList.Length];

            for (int i = 0; i < invocationList.Length; i++)
            {
                handlerTasks[i] = ((Func<OutgoingMessageEventArgs, Task>)invocationList[i])(args);
            }

            await Task.WhenAll(handlerTasks);

            if (args.Drop)
            {
                Logger?.LogDebug("Outgoing message suppressed. Message: {Message}, Id: {Id}", args.Body,args.Id);
                message.Clear();
                return;
            }

            if (args.Body != (xml.Element("body")?.Value ?? string.Empty))
            {
                var bodyEl = xml.Element("body");

                if (bodyEl != null)
                    bodyEl.Value = args.Body;
                else
                    xml.Add(new XElement("body", args.Body));

                message.Clear();
                message.Append(xml.ToString(SaveOptions.DisableFormatting));
            }
        }
    }

    private bool TryParseXml(string raw, out XElement? element)
    {
        try
        {
            element = XElement.Parse(raw);
            return true;
        }
        catch (Exception)
        {
            element = null;
            return false;
        }
    }
}

public enum PresenceShow
{
    Available,
    Away,
    DoNotDisturb,
    ExtendedAway,
    Offline,
    Unknown
}

public class PresenceEventArgs : EventArgs
{
    /// <summary>The JID this presence is from, e.g. "abc@eu1.pvp.net/RC-xxx".</summary>
    public string From { get; init; } = string.Empty;

    /// <summary>The JID this presence is addressed to (usually your own JID).</summary>
    public string To { get; init; } = string.Empty;

    public PresenceShow Show { get; init; }

    /// <summary>The raw &lt;status&gt; text, which in Valorant is a JSON blob.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Parsed Valorant player state decoded from &lt;status&gt;.
    /// Null if the status payload was absent or not valid JSON.
    /// </summary>
    public XmppPresence? P { get; init; }

    /// <summary>The full original XML stanza.</summary>
    public string RawXml { get; init; } = string.Empty;

    internal static PresenceEventArgs Parse(XElement xml, string raw)
    {
        string typeAttr = xml.Attribute("type")?.Value ?? string.Empty;
        string showText = xml.Element("show")?.Value ?? string.Empty;

        var show = typeAttr.Equals("unavailable", StringComparison.OrdinalIgnoreCase)
            ? PresenceShow.Offline
            : showText.ToLowerInvariant() switch
            {
                "away" => PresenceShow.Away,
                "dnd" => PresenceShow.DoNotDisturb,
                "xa" => PresenceShow.ExtendedAway,
                "" => PresenceShow.Available,
                _ => PresenceShow.Unknown
            };

        string statusRaw = xml.Element("status")?.Value ?? string.Empty;
        string privateEncodedJson = xml.Element("games")?.Element("valorant")?.Element("p")?.Value ?? string.Empty;

        XmppPresence? playerState = null;

        if (!string.IsNullOrEmpty(privateEncodedJson))
        {
            if (!XmppPresence.TryParseFromBase64(privateEncodedJson, out playerState))
                XmppPresence.TryParse(privateEncodedJson, out playerState);
        }

        return new PresenceEventArgs
        {
            From = xml.Attribute("from")?.Value ?? string.Empty,
            To = xml.Attribute("to")?.Value ?? string.Empty,
            Show = show,
            Status = statusRaw,
            P = playerState,
            RawXml = raw
        };
    }
}

public class MessageEventArgs : EventArgs
{
    public string SenderPuuid { get; private set; } = null!;
    public string Jid { get; private set; } = null!;
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string Stamp { get; init; } = string.Empty;
    public ChatChannelType Type { get; init; }
    public string Body { get; init; } = string.Empty;
    public string RawXml { get; init; } = string.Empty;

    internal static MessageEventArgs Parse(XElement xml, string raw)
    {
        var x = new MessageEventArgs()
        {
            From = xml.Attribute("from")?.Value ?? string.Empty,
            To = xml.Attribute("to")?.Value ?? string.Empty,
            Id = xml.Attribute("id")?.Value ?? string.Empty,
            Stamp = xml.Attribute("stamp")?.Value ?? string.Empty,
            Type = OutgoingChatMessage.ParseChannel((xml.Attribute("type")?.Value ?? string.Empty).ToLowerInvariant()),
            Body = xml.Element("body")?.Value?.Trim() ?? string.Empty,
            RawXml = raw,
        };

        var splitTo = x.To.Split('/');
        var splitFrom = x.From.Split('/');

        if (splitTo.Length > 0)
            x.Jid = splitTo[0];

        if (splitFrom.Length > 1)
            x.SenderPuuid = splitFrom[1];

        return x;
    }
}
public class OutgoingMessageEventArgs : EventArgs
{
    public string To { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public ChatChannelType Type { get; init; }

    public string Body { get; set; } = string.Empty;
    public bool Drop { get; set; }
    public string RawXml { get; init; } = string.Empty;

    internal static OutgoingMessageEventArgs Parse(XElement xml, string raw) => new()
    {
        To = xml.Attribute("to")?.Value ?? string.Empty,
        Id = xml.Attribute("id")?.Value ?? string.Empty,
        Type = OutgoingChatMessage.ParseChannel((xml.Attribute("type")?.Value ?? string.Empty).ToLowerInvariant()),
        Body = xml.Element("body")?.Value?.Trim() ?? string.Empty,
        RawXml = raw
    };
}
public class XmppPresence
{
    // Identity
    public string? Puuid { get; init; }
    public string? GameName { get; init; }
    public string? TagLine { get; init; }

    // Rank (from playerPresenceData)
    public int? CompetitiveTier { get; init; }
    public int? LeaderboardPosition { get; init; }

    // Party (from partyPresenceData)
    public string? PartyId { get; init; }
    public string? PartyState { get; init; }
    public bool? IsPartyOwner { get; init; }
    public int? PartySize { get; init; }
    public int? MaxPartySize { get; init; }
    public string? PartyAccessibility { get; init; }
    public bool? IsPartyLFM { get; init; }
    public bool? IsCrossPlayEnabled { get; init; }
    public string? PartyClientVersion { get; init; }
    public string? PartyOwnerSessionLoopState { get; init; }
    public string? PartyOwnerMatchMap { get; init; }
    public string? PartyOwnerProvisioningFlow { get; init; }
    public int? PartyOwnerScoreAlly { get; init; }
    public int? PartyOwnerScoreEnemy { get; init; }

    // Session / Game (from matchPresenceData)
    public string? SessionLoopState { get; init; }
    public string? ProvisioningFlow { get; init; }
    public string? MatchMap { get; init; }
    public string? QueueId { get; init; }

    // Root-level flags
    public bool? IsValid { get; init; }
    public bool? IsIdle { get; init; }

    // Player preferences (from playerPresenceData)
    public string? PlayerCardId { get; init; }
    public string? PlayerTitleId { get; init; }
    public int? AccountLevel { get; init; }

    // Premier (from premierPresenceData)
    public PremierPresence? Premier { get; init; }

    /// <summary>
    /// Decodes the base64-encoded status payload from the XMPP &lt;status&gt; element
    /// and parses it into an <see cref="XmppPresence"/> instance.
    /// </summary>
    public static bool TryParseFromBase64(string base64, out XmppPresence? state)
    {
        state = null;
        try
        {
            byte[] bytes = Convert.FromBase64String(base64);
            string json = Encoding.UTF8.GetString(bytes);
            return TryParse(json, out state);
        }
        catch
        {
            return false;
        }
    }

    public static bool TryParse(string json, out XmppPresence? state)
    {
        state = null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;

            // Grab the nested objects — all optional since they may be absent
            r.TryGetProperty("matchPresenceData", out var match);
            r.TryGetProperty("partyPresenceData", out var party);
            r.TryGetProperty("playerPresenceData", out var player);
            r.TryGetProperty("premierPresenceData", out var premier);

            state = new XmppPresence
            {
                // Root
                Puuid = GetStr(r, "puuid"),
                GameName = GetStr(r, "game_name"),
                TagLine = GetStr(r, "game_tag"),
                IsValid = GetBool(r, "isValid"),
                IsIdle = GetBool(r, "isIdle"),

                // matchPresenceData
                SessionLoopState = GetStr(match, "sessionLoopState"),
                ProvisioningFlow = GetStr(match, "provisioningFlow"),
                MatchMap = GetStr(match, "matchMap"),
                QueueId = GetStr(match, "queueId"),

                // partyPresenceData
                PartyId = GetStr(party, "partyId"),
                PartyState = GetStr(party, "partyState"),
                IsPartyOwner = GetBool(party, "isPartyOwner"),
                PartySize = GetInt(party, "partySize"),
                MaxPartySize = GetInt(party, "maxPartySize"),
                PartyAccessibility = GetStr(party, "partyAccessibility"),
                IsPartyLFM = GetBool(party, "partyLFM"),
                IsCrossPlayEnabled = GetBool(party, "isPartyCrossPlayEnabled"),
                PartyClientVersion = GetStr(party, "partyClientVersion"),
                PartyOwnerSessionLoopState = GetStr(party, "partyOwnerSessionLoopState"),
                PartyOwnerMatchMap = GetStr(party, "partyOwnerMatchMap"),
                PartyOwnerProvisioningFlow = GetStr(party, "partyOwnerProvisioningFlow"),
                PartyOwnerScoreAlly = GetInt(party, "partyOwnerMatchScoreAllyTeam"),
                PartyOwnerScoreEnemy = GetInt(party, "partyOwnerMatchScoreEnemyTeam"),

                // playerPresenceData
                PlayerCardId = GetStr(player, "playerCardId"),
                PlayerTitleId = GetStr(player, "playerTitleId"),
                AccountLevel = GetInt(player, "accountLevel"),
                CompetitiveTier = GetInt(player, "competitiveTier"),
                LeaderboardPosition = GetInt(player, "leaderboardPosition"),

                // premierPresenceData
                Premier = premier.ValueKind == JsonValueKind.Object
                    ? PremierPresence.Parse(premier)
                    : null,
            };

            return true;
        }
        catch
        {
            return false;
        }
    }

    // Helpers — accept default JsonElement so missing sub-objects are handled gracefully
    private static string? GetStr(JsonElement el, string key) =>
        el.ValueKind == JsonValueKind.Object &&
        el.TryGetProperty(key, out var p) &&
        p.ValueKind == JsonValueKind.String
            ? p.GetString() : null;

    private static int? GetInt(JsonElement el, string key) =>
        el.ValueKind == JsonValueKind.Object &&
        el.TryGetProperty(key, out var p) &&
        p.ValueKind == JsonValueKind.Number
            ? p.GetInt32() : null;

    private static bool? GetBool(JsonElement el, string key) =>
        el.ValueKind == JsonValueKind.Object &&
        el.TryGetProperty(key, out var p) &&
        p.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? p.GetBoolean() : null;
}

public class PremierPresence
{
    public string? RosterId { get; init; }
    public string? RosterName { get; init; }
    public string? RosterTag { get; init; }
    public string? RosterType { get; init; }
    public int? Division { get; init; }
    public int? Score { get; init; }
    public bool? ShowAura { get; init; }
    public bool? ShowTag { get; init; }

    internal static PremierPresence Parse(JsonElement el) => new()
    {
        RosterId = GetStr(el, "rosterId"),
        RosterName = GetStr(el, "rosterName"),
        RosterTag = GetStr(el, "rosterTag"),
        RosterType = GetStr(el, "rosterType"),
        Division = GetInt(el, "division"),
        Score = GetInt(el, "score"),
        ShowAura = GetBool(el, "showAura"),
        ShowTag = GetBool(el, "showTag"),
    };

    private static string? GetStr(JsonElement el, string key) =>
        el.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static int? GetInt(JsonElement el, string key) =>
        el.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt32() : null;

    private static bool? GetBool(JsonElement el, string key) =>
        el.TryGetProperty(key, out var p) && p.ValueKind is JsonValueKind.True or JsonValueKind.False ? p.GetBoolean() : null;
}
