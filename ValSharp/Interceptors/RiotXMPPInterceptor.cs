using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace ValSharp.Interceptors;

internal class RiotXMPPIntercepter
{
    public readonly int HttpPort;
    public readonly string Host;
    public bool IsStarted => _isStarted;

    private readonly Func<X509Certificate2> certificateFactory;
    private readonly ConcurrentDictionary<int, string> PortToRiotHostMap;
    private readonly HttpClient riotHttpClient;
    private readonly ILogger? logger;


    private bool _isStarted = false;
    private int _nextProxyPort = 36000;

    private sealed record XmppConnection(SslStream ClientStream, SslStream RiotStream);
    private volatile XmppConnection? _activeConnection;

    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private static readonly HashSet<string> KnownStanzas = new(StringComparer.OrdinalIgnoreCase)
    {
        "message", "presence", "iq", "stream:features", "stream:error", "proceed", "auth", "response", "success", "failure", "challenge"
    };

    public RiotXMPPIntercepter(string httpHost = "127.0.0.1", int httpPort = 35479, ILogger? logger = null, Func<X509Certificate2>? certificateFactory = null)
    {
        this.Host = httpHost;
        this.HttpPort = httpPort;
        this.logger = logger;
        this.PortToRiotHostMap = new ConcurrentDictionary<int, string>();

        this.riotHttpClient = new(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All
        });

        this.certificateFactory = certificateFactory ?? new Func<X509Certificate2>(() =>
        {
            const string certFile = "./certs/server.cert", keyFile = "./certs/server.key";

            if (!File.Exists(certFile) || !File.Exists(keyFile))
                throw new FileNotFoundException("Certificate files not found.");

            using var ephemeralCert = X509Certificate2.CreateFromPemFile(certFile, keyFile);
            byte[] pfxData = ephemeralCert.Export(X509ContentType.Pfx, "");

            return X509CertificateLoader.LoadPkcs12(
                pfxData,
                password: "",
                keyStorageFlags: X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable
            );

            //using var cert = X509Certificate2.CreateFromPemFile("./certs/server.cert", "./certs/server.key");
            //return new X509Certificate2(cert.Export(X509ContentType.Pfx));
        });
    }

    /// <summary>
    /// Called when a complete XMPP stanza is received FROM Riot (incoming to client).
    /// The message is read-only — mutations here have no effect on what is forwarded.
    /// </summary>
    protected virtual Task HookOnIncomingMessage(string message) => Task.CompletedTask;

    /// <summary>
    /// Called when a complete XMPP stanza is sent BY the client (outgoing to Riot).
    /// Mutate <paramref name="message"/> to change what is forwarded.
    /// Set <paramref name="message"/> to <see cref="string.Empty"/> to suppress/drop the packet entirely.
    /// </summary>
    protected virtual Task HookOnOutgoingMessage(StringBuilder message) => Task.CompletedTask;

    public void Start()
    {
        if (_isStarted)
            throw new Exception("Interceptor already started!");

        _isStarted = true;
        _ = Task.Run(StartHttpConfigBridge);
    }
    public async Task SendToServer(string xml)
    {
        var conn = _activeConnection;
        if (conn is null)
        {
            logger?.LogWarning("[XMPP] SendToServer called but no active connection");
            return;
        }
        await _sendLock.WaitAsync();
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(xml);
            await conn.RiotStream.WriteAsync(data);
            await conn.RiotStream.FlushAsync();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "[XMPP] Failed to send to Riot");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task SendToClient(string xml)
    {
        var conn = _activeConnection;
        if (conn is null)
        {
            logger?.LogWarning("[XMPP] SendToClient called but no active connection");
            return;
        }
        await _sendLock.WaitAsync();
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(xml);
            await conn.ClientStream.WriteAsync(data);
            await conn.ClientStream.FlushAsync();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "[XMPP] Failed to send to client");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task StartHttpConfigBridge()
    {
        try
        {
            logger?.LogDebug("HTTP Config Proxy starting");

            var listener = new HttpListener();
            listener.Prefixes.Add($"http://{Host}:{HttpPort}/");
            listener.Start();

            logger?.LogDebug("HTTP Config Proxy listening on {Host}:{Port}", Host, HttpPort);

            while (true)
            {
                var context = await listener.GetContextAsync();

                _ = Task.Run(async () =>
                {
                    var req = context.Request;
                    var res = context.Response;
                    string url = req.Url?.PathAndQuery ?? "unknown";

                    try
                    {
                        // logger?.LogDebug("[HTTP] {Method} {Url}", req.HttpMethod, url);

                        var riotUrl = $"https://clientconfig.rpg.riotgames.com{url}";
                        var riotReq = new HttpRequestMessage(new HttpMethod(req.HttpMethod), riotUrl);

                        foreach (string h in req.Headers)
                            if (!h.Equals("Host", StringComparison.OrdinalIgnoreCase))
                                riotReq.Headers.TryAddWithoutValidation(h, req.Headers[h]);

                        var riotRes = await riotHttpClient.SendAsync(riotReq);
                        byte[] responseData = await riotRes.Content.ReadAsByteArrayAsync();

                        // logger?.LogDebug("[HTTP] Received {Status} from Riot ({Bytes} bytes)", riotRes.StatusCode, responseData.Length);

                        if (url.Contains("/config/player"))
                        {
                            // logger?.LogDebug("[HTTP] Patching player config...");

                            var json = JObject.Parse(Encoding.UTF8.GetString(responseData));

                            if (json["chat.affinities"] is JObject affinities)
                            {
                                foreach (var property in affinities.Properties().ToList())
                                {
                                    int localPort = _nextProxyPort++;
                                    string realIp = property.Value.ToString();
                                    PortToRiotHostMap[localPort] = realIp;
                                    affinities[property.Name] = Host;
                                    _ = StartXmppProxy(localPort);
                                    // logger?.LogDebug("[HTTP] Proxy port {LocalPort} -> {RealIp}", localPort, realIp);
                                }

                                json["chat.port"] = 36000;
                                json["chat.host"] = Host;
                                json["chat.allow_bad_cert.enabled"] = true;

                                responseData = Encoding.UTF8.GetBytes(json.ToString(Newtonsoft.Json.Formatting.None));
                                logger?.LogDebug("Http patch completed");
                            }
                        }

                        res.StatusCode = (int)riotRes.StatusCode;
                        res.SendChunked = false;

                        CopyHeaders(riotRes, res);

                        res.ContentLength64 = responseData.Length;

                        await res.OutputStream.WriteAsync(responseData);
                        await res.OutputStream.FlushAsync();
                    }
                    catch (Exception e)
                    {
                        logger?.LogCritical(e, "[HTTP] Critical error handling {Url}", url);
                    }
                    finally
                    {
                        try { res.Close(); } catch { }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "HttpBridge error");
        }
    }

    private void CopyHeaders(HttpResponseMessage source, HttpListenerResponse target)
    {
        var restricted = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Transfer-Encoding", "Keep-Alive", "Connection", "Content-Length",
            "Content-Encoding", "Host", "Server", "WWW-Authenticate", "Access-Control-Allow-Origin"
        };

        foreach (var h in source.Headers) SafeAddHeader(target, h.Key, string.Join(", ", h.Value), restricted);
        foreach (var h in source.Content.Headers) SafeAddHeader(target, h.Key, string.Join(", ", h.Value), restricted);
    }

    private void SafeAddHeader(HttpListenerResponse res, string name, string value, HashSet<string> restricted)
    {
        if (restricted.Contains(name)) return;
        try { res.Headers.Add(name, value); }
        catch (Exception ex) { logger?.LogError(ex, "[HTTP] Failed to add header {Name}={Value}", name, value); }
    }

    private async Task StartXmppProxy(int localPort)
    {
        try
        {
            var cert = certificateFactory();
            var listener = new TcpListener(IPAddress.Loopback, localPort);
            listener.Start();

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var clientStream = new SslStream(client.GetStream(), false);
                        await clientStream.AuthenticateAsServerAsync(cert, false, SslProtocols.Tls12, false);

                        byte[] buffer = new byte[16384];
                        int read = await clientStream.ReadAsync(buffer);
                        string initialPacket = Encoding.UTF8.GetString(buffer, 0, read);

                        string targetDomain = "eu1.pvp.net";
                        var match = Regex.Match(initialPacket, "to=\"(.+?)\"");
                        if (match.Success) targetDomain = match.Groups[1].Value;

                        string riotServerAddress = targetDomain;
                        if (targetDomain.Contains("pvp.net"))
                        {
                            string region = targetDomain.Split('.')[0];
                            switch (region)
                            {
                                case "eu1": region = "euw1"; break;
                                case "na1": region = "na2"; break;
                                case "jp1": region = "jp1"; break;
                                case "ap1": region = "ap1"; break;
                                case "kr1": region = "kr1"; break;
                                case "br1": region = "br1"; break;
                                case "la1": region = "la1"; break;
                                case "la2": region = "la2"; break;
                            }
                            riotServerAddress = $"{region}.chat.si.riotgames.com";
                        }

                        // logger?.LogDebug("[XMPP] Routing {Domain} -> {Server}", targetDomain, riotServerAddress);

                        using var riotClient = new TcpClient(riotServerAddress, 5223);
                        using var riotStream = new SslStream(riotClient.GetStream(), false, (s, c, ch, e) => true);
                        await riotStream.AuthenticateAsClientAsync(riotServerAddress);
                        var connection = new XmppConnection(clientStream, riotStream);
                        _activeConnection = connection;

                        logger?.LogDebug("Routing {Domain} --> {Server}", targetDomain, riotServerAddress);
                        await riotStream.WriteAsync(buffer, 0, read);

                        try
                        {
                            await Task.WhenAll(
                                Relay(clientStream, riotStream, PacketDirection.Client),
                                Relay(riotStream, clientStream, PacketDirection.Riot)
                            );
                        }
                        finally
                        {
                            Interlocked.CompareExchange(ref _activeConnection, null, connection);
                            logger?.LogDebug("XMPP connection closed for {Domain}", targetDomain);
                        }
                    }
                    catch (Exception e)
                    {
                        logger?.LogError(e, "[XMPP] Proxy connection error");
                    }
                });
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger?.LogError(ex, "XMPP Proxy error");
        }
    }

    private async Task Relay(Stream from, Stream to, PacketDirection direction)
    {
        var rawBuffer = new byte[16384];
        var accumulator = new StringBuilder();

        try
        {
            while (true)
            {
                int read = await from.ReadAsync(rawBuffer);
                if (read == 0) break;

                string chunk = Encoding.UTF8.GetString(rawBuffer, 0, read);
                accumulator.Append(chunk);

                while (TryExtractNextStanza(accumulator, out string stanza))
                {
                    string toForward = await ProcessStanzaAsync(stanza, direction);

                    if (string.IsNullOrEmpty(toForward))
                    {
                        // logger?.LogDebug("[XMPP] {Direction} packet suppressed by hook", direction);
                        continue;
                    }

                    byte[] outBytes = Encoding.UTF8.GetBytes(toForward);
                    await to.WriteAsync(outBytes);
                    await to.FlushAsync();
                }
            }
        }
        catch (IOException) { }
        catch (Exception e)
        {
            logger?.LogError(e, "[XMPP] Relay error on {Direction} stream", direction);
        }
    }

    private bool TryExtractNextStanza(StringBuilder accumulator, out string stanza)
    {
        stanza = string.Empty;
        string current = accumulator.ToString();

        if (string.IsNullOrWhiteSpace(current))
            return false;

        string trimmed = current.TrimStart();

        if (trimmed.StartsWith("<?xml") || trimmed.StartsWith("<stream:stream"))
        {
            int closeAngle = trimmed.IndexOf('>');
            if (closeAngle < 0) return false;

            stanza = trimmed[..(closeAngle + 1)];
            int removeUpTo = current.IndexOf(stanza, StringComparison.Ordinal) + stanza.Length;
            accumulator.Remove(0, removeUpTo);
            return true;
        }

        var selfClose = Regex.Match(trimmed, @"^<(\w[\w:]*)[^>]*/\s*>", RegexOptions.Singleline);
        if (selfClose.Success && KnownStanzas.Contains(selfClose.Groups[1].Value))
        {
            stanza = selfClose.Value;
            int removeUpTo = current.IndexOf(stanza, StringComparison.Ordinal) + stanza.Length;
            accumulator.Remove(0, removeUpTo);
            return true;
        }

        var openTag = Regex.Match(trimmed, @"^<([\w:]+)[\s>]", RegexOptions.Singleline);
        if (!openTag.Success) return false;

        string tagName = openTag.Groups[1].Value;
        if (!KnownStanzas.Contains(tagName)) return false;

        string closeTagPattern = $"</{Regex.Escape(tagName)}>";
        int depth = 0;

        var allTags = Regex.Matches(trimmed,
            $@"<{Regex.Escape(tagName)}[\s>/]|</{Regex.Escape(tagName)}>",
            RegexOptions.Singleline);

        int endIndex = -1;
        foreach (Match t in allTags)
        {
            if (t.Value.StartsWith("</"))
            {
                depth--;
                if (depth == 0)
                {
                    endIndex = t.Index + t.Length;
                    break;
                }
            }
            else
            {
                depth++;
            }
        }

        if (endIndex < 0) return false;

        stanza = trimmed[..endIndex];
        int removeCount = current.IndexOf(stanza, StringComparison.Ordinal) + stanza.Length;
        accumulator.Remove(0, removeCount);
        return true;
    }

    private async Task<string> ProcessStanzaAsync(string stanza, PacketDirection direction)
    {
        try
        {
            if (direction == PacketDirection.Riot)
            {
                await HookOnIncomingMessage(stanza);
                return stanza;
            }
            else
            {
                var sb = new StringBuilder(stanza);
                await HookOnOutgoingMessage(sb);

                return sb.ToString();
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "[XMPP] Exception in {Direction} hook — forwarding original packet", direction);
            return stanza;
        }
    }

    protected enum PacketDirection { Client, Riot }
}