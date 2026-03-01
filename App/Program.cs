using DeepSeek.Core;
using DeepSeek.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ValSharp.Core;
using ValSharp.DTOs;
using ValSharp.Interceptors;

namespace ValSharp_Demo;

internal class Program
{
    public static InGameCache GameContent { get; private set; } = null!;

    static ValClient _valClient = null!;
    static ILoggerFactory _loggerFactory = null!;
    static ILogger _logger = null!;
    static ConcurrentDictionary<int, byte> _processedMessages = new();

    static async Task Main(string[] args)
    {
        using (_loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug)))
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string path = Path.Combine(localAppData, "Riot Games", "Riot Client", "Config", "lockfile");

            _logger = _loggerFactory.CreateLogger<Program>();
            _valClient = new ValClient(new ValAuth(), _loggerFactory);

            _valClient.Chat.OutgoingMessage += OnOutgoingMessage;
            _valClient.Chat.MessageReceived += OnIncomingMessage;
            _valClient.GameStateChanged += OnGameStateChange;

            GameContent = new InGameCache(_valClient, _loggerFactory);

        L1:
            bool isGameOpen = IsGameRunning();

            if (isGameOpen || IsRiotClientRunning())
            {

                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"Initilization whilst {(isGameOpen ? "game" : "riot client")} is running will not work.");

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Do you want to kill active processes? (y/n)");
                Console.Write("> ");
                Console.ResetColor();

                string answer = Console.ReadLine() ?? string.Empty;

                if (answer.ToUpper() == "Y")
                {
                    Console.WriteLine();

                    var procCollection = Process.GetProcesses().Where(p => p.ProcessName.ToLower().Contains("valorant") ||
                                                                           p.ProcessName.ToLower().Contains("riotclient"));
                    var waitList = new List<Task>();
                    foreach (var p in procCollection)
                    {
                        p.Kill();
                        waitList.Add(Task.Run(async () =>
                        {
                            await p.WaitForExitAsync();
                            _logger?.LogInformation($"Killed {p.ProcessName}");
                        }));
                    }

                    await Task.WhenAll(waitList);
                    await Task.Delay(1000);

                    goto L1;
                }

                return;
            }
            else
            {
                _logger.LogInformation("Launching game");

                try
                {
                    string riotClientPath = GetRiotClientPath();
                    string arguments = $"--client-config-url=\"{_valClient.InterceptedClientConfigUrl}\" --launch-product=valorant --launch-patchline=live";
                    Process.Start(riotClientPath, arguments);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error occured while launching the game");
                }
            }

            _valClient.StartInterceptor();
            await WaitForGame();

            if (!await _valClient.Auth.TryLocalAuthAsync(path))
            {
                _logger?.LogWarning("Not authenticated!");
                Console.Read();
                return;
            }

            GameContent.LoadContent();

            await Task.Delay(Timeout.Infinite);
        }
    }

    private static void OnGameStateChange(GameState newState, GameState oldState)
    {
        _processedMessages.Clear();
    }

    private static async Task WaitForGame()
    {
        _logger?.LogInformation("Waiting for game");
        bool isFirstTime = false;
        do
        {
            if (!isFirstTime)
                isFirstTime = true;
            else
                await Task.Delay(1000);
        } while (!Process.GetProcesses().Any(p => p.ProcessName.Contains("VALORANT")));
        _logger?.LogInformation("Game found");
    }
    private static bool IsRiotClientRunning()
    {
        return Process.GetProcessesByName("RiotClientServices").Length > 0;
    }
    private static bool IsGameRunning()
    {
        return Process.GetProcesses().Any(p => p.ProcessName.Contains("VALORANT"));
    }
    private static string GetRiotClientPath()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Riot Game valorant.live");
        return Regex.Match(key?.GetValue("UninstallString")?.ToString() ?? "", "^\"(.+?)\"").Groups[1].Value;
    }

    private static async Task OnOutgoingMessage(OutgoingMessageEventArgs e)
    {
        await HandleMessageAsync(new InGameMessage(_valClient, e));
    }
    private static async Task OnIncomingMessage(MessageEventArgs e)
    {
        await HandleMessageAsync(new InGameMessage(_valClient, e));
    }

    private static async Task HandleMessageAsync(InGameMessage message)
    {
        if (!await RunMiddlewaresAsync(message))
        {
            await message.SendRepliesAsync();
            return;
        }

        await ExecuteCommandsAsync(message);
        await message.SendRepliesAsync();
    }

    private static async Task<bool> RunMiddlewaresAsync(InGameMessage message)
    {
        var foundMethods = typeof(MiddlewareContainer).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                                     .Where(m =>
                                                     {
                                                         var parameters = m.GetParameters();
                                                         if (m.ReturnType != typeof(MiddlewareAction) && m.ReturnType != typeof(Task<MiddlewareAction>)) return false;
                                                         if (parameters.Length != 1) return false;
                                                         if (parameters[0].ParameterType != typeof(InGameMessage)) return false;
                                                         return true;
                                                     });

        if (_messageMiddlewaresContainer is null)
            _messageMiddlewaresContainer = new MiddlewareContainer(_valClient, _loggerFactory);

        foreach (MethodInfo method in foundMethods)
        {
            bool hasIncoming = method.GetCustomAttribute<IncomingAttribute>() is not null;
            bool hasOutgoing = method.GetCustomAttribute<OutgoingAttribute>() is not null;

            if (message.IsOutgoing && (hasIncoming && !hasOutgoing))
                continue;

            if (!message.IsOutgoing && (hasOutgoing && !hasIncoming))
                continue;

            try
            {
                object? ret = method.Invoke(_messageMiddlewaresContainer, [message]);
                MiddlewareAction action = MiddlewareAction.Cancel;

                if (method.ReturnType == typeof(Task<MiddlewareAction>))
                    action = await (Task<MiddlewareAction>)ret!;
                else
                    action = (MiddlewareAction)ret!;

                if (action == MiddlewareAction.Cancel)
                    return false;
            }
            catch (TargetInvocationException targetInvocException)
            {
                _logger?.LogWarning(targetInvocException.InnerException, "Middleware execution exception! Command: {MiddlewareName}", method.Name);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Middleware execution exception! Command: {MiddlewareName}", method.Name);
            }
        }

        return true;
    }

    private static async Task ExecuteCommandsAsync(InGameMessage message)
    {
        try
        {
            if (!message.Body.StartsWith("!"))
                return;

            bool isSilentReply = message.Body.StartsWith("!!");

            if (isSilentReply)
                message.SetSilentReply();

            //int msgHashCode = HashCode.Combine(message.Body);
            //if (message.IsOutgoing)
            //{
            //    _processedMessages.TryAdd(msgHashCode, 0);

            //    if (message.IsWaitingSilentReply)
            //    {
            //        _ = Task.Run(async () =>
            //        {
            //            await Task.Delay(TimeSpan.FromSeconds(3));
            //            _processedMessages.Remove(msgHashCode, out _);
            //        });
            //    }
            //} else if (message.FromSubject == _valClient.Auth.Subject)
            //{
            //    if (_processedMessages.Remove(msgHashCode, out _))
            //        return;
            //}

            var foundMethods = typeof(CommandContainer).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                                         .Where(m =>
                                                         {
                                                             if (m.GetCustomAttribute<CommandAttribute>() is null) return false;
                                                             if (m.ReturnType != typeof(void) && m.ReturnType != typeof(Task)) return false;
                                                             var parameters = m.GetParameters();
                                                             if (parameters.Length == 0 || parameters.Length > 2) return false;
                                                             if (parameters[0].ParameterType != typeof(InGameMessage)) return false;
                                                             if (parameters.Length == 2 && parameters[1].ParameterType != typeof(string[])) return false;
                                                             return true;
                                                         });
            if (_commandHandlersContainer is null)
                _commandHandlersContainer = new CommandContainer(_valClient, _loggerFactory);

            Stopwatch sw = new Stopwatch();

            foreach (MethodInfo method in foundMethods)
            {
                bool hasIncoming = method.GetCustomAttribute<IncomingAttribute>() is not null;
                bool hasOutgoing = method.GetCustomAttribute<OutgoingAttribute>() is not null;

                if (!hasIncoming && !hasOutgoing)
                    hasIncoming = hasOutgoing = true;

                if (message.IsOutgoing && (hasIncoming && !hasOutgoing))
                    continue;

                if (!message.IsOutgoing && (hasOutgoing && !hasIncoming))
                    continue;

                if (message.IsOutgoing && (hasIncoming && hasOutgoing) && !isSilentReply)
                    continue;

                var commandAttribute = method.GetCustomAttribute<CommandAttribute>()!;
                Regex regex = GenerateRegex(commandAttribute.Command);
                Match match = regex.Match(message.Body.TrimStart('!'));

                if (!match.Success)
                    continue;

                var allArgs = match.Groups["args"]?.Captures?.Select(c => c.Value)?.ToArray() ?? Array.Empty<string>();

                if (isSilentReply)
                    message.Drop();

                sw.Reset();
                sw.Start();

                try
                {
                    object? ret = method.Invoke(_commandHandlersContainer, method.GetParameters().Length == 1 ? [message] : [message, allArgs]);

                    if (method.ReturnType == typeof(Task))
                        await (Task)ret!;
                }
                catch (TargetInvocationException targetInvocException)
                {
                    _logger?.LogWarning(targetInvocException.InnerException, "Command execution exception! Command: {CommandName}", method.Name);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Command execution exception! Command: {CommandName}", method.Name);
                }
                finally
                {
                    sw.Stop();

                    if (isSilentReply)
                        message.Drop();

                    string elapsedTime = string.Empty;

                    if (sw.Elapsed.TotalMilliseconds > 1)
                        elapsedTime = $"{sw.Elapsed.TotalMilliseconds} ms";
                    else
                        elapsedTime = $"{sw.Elapsed.TotalMicroseconds} μs";

                    _logger?.LogDebug("Command '{CommandName}' took {ElapsedTime}", method.Name, elapsedTime);
                }

                return;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error");
        }
    }

    private static Regex GenerateRegex(string command)
    {
        string pattern = $@"^{Regex.Escape(command)}(?:\s+(?:""(?<args>[^""]+)""|(?<args>\S+)))*$";
        return new Regex(pattern, RegexOptions.IgnoreCase);
    }

    private static MiddlewareContainer? _messageMiddlewaresContainer;
    private static CommandContainer? _commandHandlersContainer;
}

internal class InGameMessage
{
    private readonly ValClient _valClient;
    private readonly (OutgoingMessageEventArgs? outgoingE, MessageEventArgs? incomingE) _eventArgs;

    public bool IsOutgoing => _eventArgs.outgoingE is not null;
    public string Body => IsOutgoing ? _eventArgs.outgoingE!.Body : _eventArgs.incomingE!.Body;
    public bool IsDropped => IsOutgoing ? _eventArgs.outgoingE!.Drop : false;
    public string FromSubject => IsOutgoing ? _valClient.Auth.Subject : _eventArgs.incomingE!.From.Split('/')[1];
    public bool IsWaitingSilentReply { get; private set; }

    public InGameMessage(ValClient client, OutgoingMessageEventArgs outgoingE)
    {
        _valClient = client;
        _eventArgs = (outgoingE, null);
    }

    public InGameMessage(ValClient client, MessageEventArgs incomingE)
    {
        _valClient = client;
        _eventArgs = (null, incomingE);
    }

    public void SetSilentReply()
    {
        IsWaitingSilentReply = true;
    }
    public void Drop()
    {
        if (!IsOutgoing) throw new Exception("Only outgoing messages can be dropped");
        _eventArgs.outgoingE!.Drop = true;
    }
    public void Undrop()
    {
        if (!IsOutgoing) throw new Exception("Only outgoing messages can be undropped");
        _eventArgs.outgoingE!.Drop = false;
    }
    public void Modify(string newBody)
    {
        if (!IsOutgoing) throw new Exception("Message bodies can only be changed for outgoing messages");
        _eventArgs.outgoingE!.Body = newBody;
    }
    public void Reply(string message)
    {
        if (IsWaitingSilentReply)
        {
            ReplySilently(message);
        }
        else
        {
            string sendTo = string.Empty;
            ChatChannelType channelType = ChatChannelType.Unknown;

            if (IsOutgoing)
            {
                sendTo = _eventArgs.outgoingE!.To;
                channelType = _eventArgs.outgoingE!.Type;
            }
            else
            {
                sendTo = _eventArgs.incomingE!.From.Split('/')[0];
                channelType = _eventArgs.incomingE!.Type;
            }

            _replyList.Add(new CReply()
            {
                To = sendTo,
                ChannelType = channelType,
                Message = message
            });
        }
    }
    public void ReplySilently(string message)
    {
        string from = string.Empty;
        string to = string.Empty;
        string jid = string.Empty;
        ChatChannelType channelType = ChatChannelType.Unknown;

        if (IsOutgoing)
        {
            channelType = _eventArgs.outgoingE!.Type;
            from = $"{_eventArgs.outgoingE!.To}/{_valClient.Auth.Subject}";
            jid = $"{_valClient.Auth.Subject}@{_valClient.XmppDomain}";
            to = $"{jid}/{_valClient.RCResourceId}";
        }
        else
        {
            channelType = _eventArgs.incomingE!.Type;
            from = _eventArgs.incomingE!.From;
            to = _eventArgs.incomingE!.To;
            jid = _eventArgs.incomingE!.To.Split('/')[0];
        }

        _silentReplyList.Add(new CSilentReply()
        {
            ChannelType = channelType,
            From = from,
            To = to,
            Jid = jid,
            Message = message
        });
    }

    public async Task SendRepliesAsync()
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(250);

            foreach (var silentReply in _silentReplyList)
            {
                await Task.Delay(50);
                await _valClient.Chat.SendToClientAsync(silentReply.ChannelType, silentReply.From, silentReply.To, silentReply.Jid, "[P] " + silentReply.Message);
            }

            foreach (var reply in _replyList)
            {
                await Task.Delay(100);
                await _valClient.Chat.SendAsync(reply.To, reply.ChannelType, reply.Message);
            }

            _silentReplyList.Clear();
            _replyList.Clear();
        });
    }

    private readonly List<CReply> _replyList = new();
    private readonly List<CSilentReply> _silentReplyList = new();

    class CReply
    {
        public required string To;
        public required string Message;
        public required ChatChannelType ChannelType;
    }

    class CSilentReply
    {
        public required string From;
        public required string To;
        public required string Jid;
        public required string Message;
        public required ChatChannelType ChannelType;
    }
}

internal enum MiddlewareAction
{
    Cancel,
    Continue,
}