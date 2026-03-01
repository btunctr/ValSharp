using DeepSeek.Core;
using DeepSeek.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ValSharp.Content;
using ValSharp.Core;
using ValSharp.Interceptors;
using YamlDotNet.Core.Tokens;

namespace ValSharp_Demo
{
    internal class CommandContainer
    {
        private readonly ValClient valClient;
        private readonly ILogger<CommandContainer> logger;
        private readonly DeepSeekClient deepSeekClient = new DeepSeekClient(AIConstants.DEEPSEEK_API_KEY);
        private readonly HashSet<string> commandAskWhitelist = new();
        private CancellationTokenSource? cmdAskCts;
        
        public CommandContainer(ValClient client, ILoggerFactory loggerFactory)
        {
            valClient = client;
            logger = loggerFactory.CreateLogger<CommandContainer>();
        }

        [Command("ai")]
        [Outgoing]
        public async Task CmdAISwitch(InGameMessage message)
        {
            MiddlewareContainer.IsAIResponseEnabled = !MiddlewareContainer.IsAIResponseEnabled;
            string msg = $"AI {(MiddlewareContainer.IsAIResponseEnabled ? "Enabled" : "Disabled")}";

            message.Drop();
            message.ReplySilently(msg);
            logger?.LogInformation(msg);
        }

        [Command("r")]
        [Outgoing]
        public async Task CmdAIReply(InGameMessage message, string[] args)
        {
            message.Drop();

            string? puuid = MiddlewareContainer.LastMessagePlayerPUUID;

            if (args.Length >= 1)
            {
                if (!InGameCache.Instance.TryFindPlayer(args[0], out puuid, out bool hasMultipleAgents))
                {
                    string replyMessage = hasMultipleAgents ? "Birden fazla oyuncu bulundu! Karakter adının başına aynı takım için 1. karşı takım için 2. koyarak tekrar deneyin." : "Oyuncu bulunamadı!";
                    message.ReplySilently(replyMessage);
                    return;
                }
            }

            if (puuid is null || !MiddlewareContainer.LastPlayerMessages.TryGetValue(puuid, out string? lastMessage))
                return;

            var chatRequest = AIConstants.YAPISTIR();
            chatRequest.Messages.Add(Message.NewUserMessage(lastMessage!));

            var chatResponse = await deepSeekClient.ChatAsync(chatRequest, new CancellationToken());
            string? textResponse = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrEmpty(textResponse))
                return;

            message.Undrop();
            message.Modify(textResponse!);
            logger.LogDebug("AI Response: " + textResponse!);
        }

        [Command("kimin")]
        public async Task CmdWhose(InGameMessage message, string[] args)
        {
            (Weapon? weapon, Skin? skin) ResolveWeaponAndSkin(string arg1, string arg2)
            {
                var weaponA = InGameCache.Instance.FindWeapon(arg1);
                var skinA = weaponA != null ? InGameCache.Instance.FindSkin(weaponA, arg2) : null;

                if (weaponA != null && skinA != null) return (weaponA, skinA);

                var weaponB = InGameCache.Instance.FindWeapon(arg2);
                var skinB = weaponB != null ? InGameCache.Instance.FindSkin(weaponB, arg1) : null;

                if (weaponB != null && skinB != null) return (weaponB, skinB);

                return weaponA != null ? (weaponA, skinA) : (weaponB, skinB);
            }

            if (args.Length != 2 || !InGameCache.Instance.IsMatchDataLoaded || !InGameCache.Instance.IsContentDataLoaded)
                return;

            string arg1 = args[0];
            string arg2 = args[1];

            if (arg1.Length < 2 || arg2.Length < 2)
            {
                message.Reply("Hatalı kullanım!");
                return;
            }

            var (weapon, skin) = ResolveWeaponAndSkin(arg1, arg2);

            if (weapon == null || skin == null)
            {
                message.Reply("Bulunamadı :(");
                return;
            }

            var skinSocketId = ValContent.Sockets[ValContent.SocketType.Skin];
            var matchingLoadouts = InGameCache.Instance.MatchLoadouts!
                .Where(ml => ml.Items.TryGetValue(weapon.Uuid, out var item) &&
                             item.Sockets[skinSocketId].Item.ID == skin.Uuid)
                .ToList();

            if (!matchingLoadouts.Any())
            {
                message.Reply($"{skin.DisplayName} {weapon.DisplayName} kimsede yok.");
                return;
            }

            var playerRequester = InGameCache.Instance.Match!.Players[message.FromSubject];
            var resultList = matchingLoadouts.Select(loadout =>
            {
                var owner = InGameCache.Instance.Match!.Players[loadout.Subject];
                var agent = InGameCache.Instance.Agents![owner.CharacterID.ToUpper()];

                bool isEnemy = !owner.TeamID.ToLower().Equals(playerRequester.TeamID.ToLower());
                bool hasDuplicateAgent = InGameCache.Instance.Match.Players.Values
                    .Count(p => p.CharacterID.ToLower().Equals(owner.CharacterID.ToLower())) > 1;

                return hasDuplicateAgent ? $"{(isEnemy ? "2" : "1")}.{agent.DisplayName}" : agent.DisplayName;
            });

            message.Reply(string.Join(", ", resultList));
        }

        [Command("s")]
        public async Task CmdSkin(InGameMessage message, string[] args)
        {
            if (args.Length != 2 || !InGameCache.Instance.IsMatchDataLoaded || !InGameCache.Instance.IsContentDataLoaded)
                return;

            string playerQuery = args[0];
            string weaponQuery = args[1];

            string? targetSubject = null;

            if (!InGameCache.Instance.TryFindPlayer(playerQuery, out targetSubject, out bool hasMultipleAgents))
            {
                string replyMessage = hasMultipleAgents ? "Birden fazla oyuncu bulundu! Karakter adının başına aynı takım için 1. karşı takım için 2. koyarak tekrar deneyin." : "Oyuncu bulunamadı!";
                message.Reply(replyMessage);
                return;
            }

            var weapon = InGameCache.Instance.FindWeapon(weaponQuery);
            if (weapon == null)
            {
                message.Reply("Silah bulunamadı!");
                return;
            }

            var playerLoadout = InGameCache.Instance.MatchLoadouts!.FirstOrDefault(ml => ml.Subject == targetSubject);
            var skinSocketId = ValContent.Sockets[ValContent.SocketType.Skin];

            if (playerLoadout != null && playerLoadout.Items.TryGetValue(weapon.Uuid, out var weaponItem))
            {
                var skinUuid = weaponItem.Sockets[skinSocketId].Item.ID;
                var skin = weapon.Skins.FirstOrDefault(s => s.Uuid == skinUuid);

                string response = skin != null ? skin.DisplayName : "Standart";
                message.Reply(response);
            }
            else
            {
                message.Reply("Silah verisi bulunamadı!");
            }
        }

        [Command("rank")]
        public async Task CmdRank(InGameMessage message, string[] args)
        {
            if (args.Length != 1 || !InGameCache.Instance.IsMatchDataLoaded || !InGameCache.Instance.IsContentDataLoaded)
                return;

            string playerQuery = args[0];

            string? targetSubject = null;

            if (!InGameCache.Instance.TryFindPlayer(playerQuery, out targetSubject, out bool hasMultipleAgents) || targetSubject is null)
            {
                string replyMessage = hasMultipleAgents ? "Birden fazla oyuncu bulundu! Karakter adının başına aynı takım için 1. karşı takım için 2. koyarak tekrar deneyin." : "Oyuncu bulunamadı!";
                message.Reply(replyMessage);
                return;
            }

            var activeSeason = InGameCache.Instance.Seasons!.FirstOrDefault(s => s.IsActive);
            var targetPlayer = InGameCache.Instance.Match!.Players[targetSubject!];
            var rankDetails = InGameCache.Instance.GetPlayerRankDetails(targetSubject!, activeSeason!.ID);

            if (rankDetails is null)
            {
                message.Reply("Oyuncunun rank detayları alınamadı!");
                return;
            }

            string currentRankName = InGameCache.Instance.CompetitiveTiers.Value!.Last().Tiers[rankDetails.Value.Current].TierName;
            string peakRankName = InGameCache.Instance.CompetitiveTiers.Value!.Last().Tiers[rankDetails.Value.Peak].TierName;
            string avgRankName = InGameCache.Instance.CompetitiveTiers.Value!.Last().Tiers[rankDetails.Value.Avg].TierName;

            message.Reply($"{(!targetPlayer.PlayerIdentity.HideAccountLevel ? $"Seviye: {targetPlayer.PlayerIdentity.AccountLevel}, " : string.Empty)}Mevcut: {currentRankName}, Ortalama: {avgRankName}, Max: {peakRankName}");
        }

        [Command("party")]
        public async Task CmdParty(InGameMessage message, string[] args)
        {
            if (args.Length != 1 || !InGameCache.Instance.IsMatchDataLoaded || !InGameCache.Instance.IsContentDataLoaded)
                return;

            string playerQuery = args[0];

            string? targetSubject = null;

            if (!InGameCache.Instance.TryFindPlayer(playerQuery, out targetSubject, out bool hasMultipleAgents))
            {
                string replyMsg = hasMultipleAgents ? "Birden fazla oyuncu bulundu! Karakter adının başına aynı takım için 1. karşı takım için 2. koyarak tekrar deneyin." : "Oyuncu bulunamadı!";
                message.Reply(replyMsg);
                return;
            }

            string? commonPartyId = InGameCache.Instance!.PlayerPartyLookup?.FirstOrDefault(x => x.Contains(targetSubject!))?.Key;

            if (commonPartyId is null)
            {
                message.Reply("Grup bilgisi alınamadı!");
                return;
            }

            var puuidCollection = InGameCache.Instance!.PlayerPartyLookup![commonPartyId!];

            var playerRequester = InGameCache.Instance.Match!.Players[message.FromSubject];
            var resultList = puuidCollection.Select(puuid =>
            {
                var owner = InGameCache.Instance.Match!.Players[puuid];
                var agent = InGameCache.Instance.Agents![owner.CharacterID.ToUpper()];

                bool isEnemy = !owner.TeamID.ToLower().Equals(playerRequester.TeamID.ToLower());
                bool hasDuplicateAgent = InGameCache.Instance.Match.Players.Values
                    .Count(p => p.CharacterID.ToLower().Equals(owner.CharacterID.ToLower())) > 1;

                return hasDuplicateAgent ? $"{(isEnemy ? "2" : "1")}.{agent.DisplayName}" : agent.DisplayName;
            });

            message.Reply(string.Join(", ", resultList));
        }

        [Command("sor")]
        public async Task CmdAsk(InGameMessage message, string[] args)
        {
            if (args.Length == 0 || !InGameCache.Instance.IsMatchDataLoaded || !InGameCache.Instance.IsContentDataLoaded || valClient.GameState != GameState.InGame)
                return;

            if (message.FromSubject != valClient.Auth.Subject && !commandAskWhitelist.Contains(message.FromSubject))
            {
                message.Reply("Bu komutu kullanma yetkiniz yok!");
                return;
            }

            string question = string.Join(' ', args);
            if (question.Length > 400) return;

            cmdAskCts?.Dispose();
            cmdAskCts = new CancellationTokenSource();
            cmdAskCts.CancelAfter(TimeSpan.FromSeconds(45));

            int totalPromptTokens = 0;
            int totalCompletionTokens = 0;
            int currentTurn = 0;
            var calledFunctions = new List<string>();
            var sw = Stopwatch.StartNew();

            try
            {
                var fnContainer = new AIFunctions(valClient, message.FromSubject);
                var toolMethodMap = typeof(AIFunctions).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                                       .ToDictionary(m => m.Name);

                var chatRequest = AIConstants.SOR();
                chatRequest.Messages.Add(Message.NewSystemMessage("Crnt Game Details;\n" + fnContainer.CurrentGame()));
                chatRequest.Messages.Add(Message.NewUserMessage(question));

                bool requiresAction = true;
                bool hasReplied = false;
                const int maxTurns = 3;

                while (requiresAction && currentTurn < maxTurns)
                {
                    cmdAskCts.Token.ThrowIfCancellationRequested();

                    var response = await deepSeekClient.ChatAsync(chatRequest, cmdAskCts.Token);
                    if (response?.Choices == null || response.Choices.Count == 0) break;

                    if (response.Usage != null)
                    {
                        totalPromptTokens += response.Usage.PromptTokens;
                        totalCompletionTokens += response.Usage.CompletionTokens;
                    }

                    var aiMessage = response.Choices[0].Message;
                    if (aiMessage is null) break;

                    chatRequest.Messages.Add(aiMessage);

                    if (aiMessage.ToolCalls != null && aiMessage.ToolCalls.Count > 0)
                    {
                        currentTurn++;

                        foreach (var toolCalled in aiMessage.ToolCalls)
                        {
                            if (toolCalled?.Function == null) continue;
                            calledFunctions.Add(toolCalled.Function.Name);

                            if (!toolMethodMap.TryGetValue(toolCalled.Function.Name, out MethodInfo? method)) continue;

                            var parameters = method.GetParameters();
                            object? arg1 = null;

                            if (parameters.Length == 1 && toolCalled.Function.Arguments != null)
                            {
                                arg1 = JsonSerializer.Deserialize(
                                    toolCalled.Function.Arguments.ToString(),
                                    parameters[0].ParameterType,
                                    AIConstants.options);
                            }

                            var toolResult = (string?)method.Invoke(fnContainer, parameters.Length == 0 ? null : [arg1]);
                            chatRequest.Messages.Add(Message.NewToolMessage(toolResult ?? "(null)", toolCalled.Id));
                        }
                    }
                    else
                    {
                        requiresAction = false;
                        if (!string.IsNullOrWhiteSpace(aiMessage.Content))
                        {
                            message.Reply("[AI] " + aiMessage.Content);
                            hasReplied = true;
                        }
                    }
                }

                if (!hasReplied)
                {
                    message.Reply(currentTurn >= maxTurns
                        ? ":/ İşlem iptal edildi!"
                        : ":/ Yanıt oluşturulamadı!");
                }
            }
            catch (OperationCanceledException)
            {
                message.Reply("Yanıt iptal edildi veya zaman aşımına uğradı!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI Command Error");
            }
            finally
            {
                sw.Stop();
                string functionLog = calledFunctions.Count > 0 ? string.Join(", ", calledFunctions) : "None";

                logger.LogInformation(
                    "AI Finished in {Ms}ms. Turns: {Turns}. Functions: [{Functions}] | Tokens: {Total}",
                    sw.ElapsedMilliseconds,
                    currentTurn,
                    functionLog,
                    totalPromptTokens + totalCompletionTokens
                );
            }
        }

        [Command("sorw")]
        [Outgoing]
        public async Task CmdWhitelistAsk(InGameMessage message, string[] args)
        {
            message.Drop();

            if (args.Length != 1)
                return;

            string playerQuery = args[0];
            string? targetSubject = null;

            if (!InGameCache.Instance.TryFindPlayer(playerQuery, out targetSubject, out bool hasMultipleAgents))
            {
                string replyMessage = hasMultipleAgents ? "Birden fazla oyuncu bulundu! Karakter adının başına aynı takım için 1. karşı takım için 2. koyarak tekrar deneyin." : "Oyuncu bulunamadı!";
                message.ReplySilently(replyMessage);
                return;
            }

            if (commandAskWhitelist.Contains(targetSubject!))
            {
                commandAskWhitelist.Remove(targetSubject!);
                message.ReplySilently("Player removed from whitelist");
            }
            else
            {
                commandAskWhitelist.Add(targetSubject!);
                message.ReplySilently("Player added to whitelist");
            }
        }

        [Command("soriptal")]
        [Outgoing]
        public async Task CmdCancelAsk(InGameMessage message)
        {
            message.Drop();

            if (cmdAskCts is not null)
                cmdAskCts.Cancel();

            message.ReplySilently("Command canceled!");
        }

        [Command("art")]
        [Outgoing]
        public void CmdChatArt(InGameMessage message, string[] args)
        {
            message.Drop();

            if (args.Length > 1)
                return;

            if (args.Length == 0)
            {
                message.ReplySilently(string.Join(", ", ArtDictionary.Keys));
            }
            else
            {
                if (ArtDictionary.TryGetValue(args[0], out var msg))
                {
                    message.Undrop();
                    message.Modify(msg);
                }
            }
        }

        private readonly Dictionary<string, string> ArtDictionary = new()
        {
            ["crab"] = "░░░░▄█▀▀▀░░░░░░░░▀▀▀█▄░░░░ ░░▄███▄▄░░▀▄██▄▀░░▄▄███▄░░ ░░▀██▄▄▄▄████████▄▄▄▄██▀░░ ░░░░▄▄▄▄██████████▄▄▄▄░░░░ ░░░▐▐▀▐▀░▀██████▀░▀▌▀░░░░░",
            ["love"] = "▒▒▒▒▒▒▒▒███▒▒▒███▒▒▒▒▒▒▒▒▒ ▒▒▒▒▒▒▒█▒▒▒█▒█▒▒▒█▒▒▒▒▒▒▒▒ ▒▒▒▒▒▒█▒▒▒▒▒█▒▒▒▒▒█▒▒▒▒▒▒▒ ▒▒▒▒▒█▒▒▒▒▒▒▒▒▒▒▒▒▒█▒▒▒▒▒▒ ▒▒▒▒▒█▒▒▒▒▒▒▒▒▒▒▒▒▒█▒▒▒▒▒▒ ▒▒▒▒▒█▒▒▒▒▒▒▒▒▒▒▒▒▒█▒▒▒▒▒▒ ▒▒▒▒▒▒█▒▒▒▒▒▒▒▒▒▒▒█▒▒▒▒▒▒▒ ▒▒▒▒▒▒▒█▒▒▒▒▒▒▒▒▒█▒▒▒▒▒▒▒▒ ▒▒▒▒▒▒▒▒█▒▒▒▒▒▒▒█▒▒▒▒▒▒▒▒▒ ▒▒▒▒▒▒▒▒▒█▒▒▒▒▒█▒▒▒▒▒▒▒▒▒▒ ▒▒▒▒▒▒▒▒▒▒█▒▒▒█▒▒▒▒▒▒▒▒▒▒▒ ▒▒▒▒▒▒▒▒▒▒▒█▒█▒▒▒▒▒▒▒▒▒▒▒▒ ▒▒▒▒▒▒▒▒▒▒▒▒█▒▒▒▒▒▒▒▒▒▒▒▒▒",
            ["sus"] = "░░░░░░░░░███████░░░░░░░░░░ ░░░░░░░░█░░░░░░░█░░░░░░░░░ ░░░░░░░█░░░░░░░░░█░░░░░░░░ ░░░░░░░█░░░███████░░░░░░░░ ░░░░░░░█░░█░░░███░█░░░░░░░ ░░░░░███░░█░░░░██░█░░░░░░░ ░░░░█░░█░░█░░░░░░░█░░░░░░░ ░░░░█░░█░░█░░░░░░░█░░░░░░░ ░░░░█░░█░░░███████░░░░░░░░ ░░░░█░░█░░░░░░░░░█░░░░░░░░ ░░░░█░░█░░░░░░░░░█░░░░░░░░ ░░░░█░░█░░░░░░░░░█░░░░░░░░ ░░░░█░░█░░░░░░░░░█░░░░░░░░",
            ["cat"] = "───────█▒▒▒█───█▒▒▒█────── ───────█▒▒▒█───█▒▒▒█────── ───────█▒▒▒█████▒▒▒█────── ──────█▒▒▒▒▒▒▒▒▒▒▒▒▒█───── ─────█▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒█──── ─────█▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒█──── ─────█▒▒▒█▒▒▒▒▒▒▒█▒▒▒█──── ─────█▒▒▒█▒▒▒▒▒▒▒█▒▒▒█──── ─────█▒▒▒░▒▒▒▒▒▒▒░▒▒▒█──── ──────██▒░▒▒▒█▒▒▒░▒██───── ─────█▒██▒▒▒▒▒▒▒▒▒██▒█──── █████▒▒▒███████████▒▒▒████ ░░░░░███░░░░░░░░░░░███░░░░",
            ["ez"] = "▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓ ▓╔═══════╗▓╔═══════╗▓▓▓▓▓▓ ▓║★★★★★★★║▓║★★★★★★★║▓▓▓▓▓▓ ▓║★╔═════╝▓║★╔══╗★★║▓▓▓▓▓▓ ▓║★║▓▓▓▓▓▓▓╚═╝▓╔╝★╔╝▓▓▓▓▓▓ ▓║★╚═════╗▓▓▓▓╔╝★╔╝▓▓▓▓▓▓▓ ▓║★╔═════╝▓▓▓╔╝★╔╝▓▓▓▓▓▓▓▓ ▓║★║▓▓▓▓▓▓▓▓╔╝★╔╝╔═╗▓▓▓▓▓▓ ▓║★╚═════╗▓╔╝★★╚═╝★║▓▓▓▓▓▓ ▓║★★★★★★★║▓║★★★★★★★║▓▓▓▓▓▓ ▓╚═══════╝▓╚═══════╝▓▓▓▓▓▓ ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓",
            ["nerd"] = "────────────████████────── ───────────██████████───── ──────────█▄▄▄████▄▄▄█──── ─────▄───█░░░░░██░░░░░█─── ─────█──░░░▓■▓░░░░▓■▓░░░── ─────█──▐█░▓▓▓░██░▓▓▓░█▌── ─────█──▐██░░░████░░░██▌── ─────█──▐█▛██████████▛█▌── ──▌▌▌▄▄──██▚████████▞██─── ──▌▌▌▀█──████▀████▀████─── ──▄▄▄██───████▄■■▄████──── ──▜███▛────██████████───── ───███──────████████──────",
            ["ggez"] = "░░░░░░░░░░░░░░░░░░░░░░░░░░ ░░░█▀▀▀░█▀▀▀░░█▀▀░▀▀█░░█░░ ░░░█░▀█░█░▀█░░█▀▀░▄▀░░░▀░░ ░░░▀▀▀▀░▀▀▀▀░░▀▀▀░▀▀▀░░▀░░ ░░░░░░░░░░░░░░░░░░░░░░░░░░",
            ["ggwp"] = "░░░░░░░░░░░░░░░░░░░░░░░░░░ ░░░░█▀▀▀░█▀▀▀░█▐▌█░█▀█░░░░ ░░░░█░▀█░█░▀█░█▐▌█░█▀▀░░░░ ░░░░▀▀▀▀░▀▀▀▀░░▀▀░░▀░░░░░░ ░░░░░░░░░░░░░░░░░░░░░░░░░░",
            ["afk"] = "░░█████╗░███████╗██╗░░██╗░ ░██╔══██╗██╔════╝██║░██╔╝░ ░███████║█████╗░░█████═╝░░ ░██╔══██║██╔══╝░░██╔═██╗░░ ░██║░░██║██║░░░░░██║░╚██╗░ ░╚═╝░░╚═╝╚═╝░░░░░╚═╝░░╚═╝░",
            ["awp"] = "╔════════════════════════╗ ║─────────▜████■██───────║ ▄▄▄▄▄▄▄▄▄▄▄▛▀▙██▄███▄▄███▜ ║─────────▗▛▀▀▀▀▀▀▀▀▀▀▀█▀▐ ║──■─■─■──────▝███▄█▌──▀█▐ ║──█─█─█───────▝▀▀─▝█────▀ ╚══▀═▀═▀════════════▀▘═══╝",
            ["1tap"] = "╔════════════════════════╗ ║░░█░░███░░███░░███░░█░█░║ ║░██░░░█░░░█░█░░█░█░█░█░█║ ║░░█░░░█░░░███░░███░░█░█░║ ║░███░░█░░░█░█░░█░░░░░█░░║ ╚════════════════════════╝",
            ["ff"] = "────────────────────────── ──────────██████────────── ──▐──────██▀██▀██──────▌── ─▐▐▐────█▄▄████▄▄█────▌▌▌─ ▐▐▐▐───▐█░■░██░■░█▌───▌▌▌▌ ▐███─▄▌▐█░░░██░░░█▌▐▄─███▌ ▐████▀─▐██████████▌─▀████▌ ▐███───▐██████████▌───███▌ ─███───▐████──████▌───███─ ────────████──████──────── ─────────████████───────── ──────────▀▀▀▀▀▀────────── ──────────────────────────",
            ["nice"] = "░░░░░░░░░░░░░░░░░░░░░░░░░░ ░░░░█▄░█░▀█▀░█▀▀░█▀▀░█░░░░ ░░░░█░▀█░░█░░█░░░█▀▀░▀░░░░ ░░░░▀░░▀░▀▀▀░▀▀▀░▀▀▀░▀░░░░ ░░░░░░░░░░░░░░░░░░░░░░░░░░",
            ["ban"] = "░░░░░░░░░░░░░░░░░░░░░░░░░░ ▄████▄░░░░░░░░░░░░░░░░░░░░ ██████▄░░░░░░▄▄▄░░░░░░░░░░ ░███▀▀▀▄▄▄▀▀▀░░░░░░░░░░░░░ ░░░▄▀▀▀▄░░░█▀▀▄░▄▀▀▄░█▄░█░ ░░░▄▄████░░█▀▀▄░█▄▄█░█▀▄█░ ░░░░██████░█▄▄▀░█░░█░█░▀█░ ░░░░░▀▀▀▀░░░░░░░░░░░░░░░░░",
            ["hmm"] = "▒▒▒▒▒▒▒▒▒▄▄▄▄▄▄▄▄▒▒▒▒▒▒▒▒▒ ▒▒▒▒▒▒▄█▀▀░░░░░░▀▀█▄▒▒▒▒▒▒ ▒▒▒▒▄█▀▄██▄░░░░░░░░▀█▄▒▒▒▒ ▒▒▒█▀░▀░░▄▀░░░░▄▀▀▀▀░▀█▒▒▒ ▒▒█▀░░░░███░░░░▄█▄░░░░▀█▒▒ ▒▒█░░░░░░▀░░░░░▀█▀░░░░░█▒▒ ▒▒█░░░░░░░░░░░░░░░░░░░░█▒▒ ▒▒█░░██▄░░▀▀▀▀▄▄░░░░░░░█▒▒ ▒▒▀█░█░█░░░▄▄▄▄▄░░░░░░█▀▒▒ ▒▒▒▀█▀░▀▀▀▀░▄▄▄▀░░░░▄█▀▒▒▒ ▒▒▒▒█░░░░░░▀█░░░░░▄█▀▒▒▒▒▒ ▒▒▒▒█▄░░░░░▀█▄▄▄█▀▀▒▒▒▒▒▒▒ ▒▒▒▒▒▀▀▀▀▀▀▀▒▒▒▒▒▒▒▒▒▒▒▒▒▒",
            ["ok"] = "░░░░░░░░░░░░░░░░██████░░░░ ░███░░░░░░░░░░██▒▒▒▒▒▒███░ █▒▒▒█░░░░░░░██▒▒▒▒▒▒▒▒▒▒▒█ ░█▒▒▒█░░░░██▒▒▒▒▒▒▒▒▒▒▒▒▒█ ░░█▒▒▒█░░██▒▒██▒▒▒▒██▒▒▒▒▒ ░░░█▒▒▒█░█▒▒▒████▒▒████▒▒▒ ░█████████▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒ █▒▒▒▒▒▒▒▒▒█▒▒▒▒█▒▒▒▒▒▒▒▒▒▒ ░█▒▒▒█████▒▒█▒▒▒▒▒▒▒▒▒██▒▒ █▒▒▒▒▒▒▒▒▒█▒███████████▒▒▒ ░█▒▒▒█████▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒ ░░█▒▒▒▒▒▒▒██▒▒▒▒▒▒▒▒▒▒▒▒██ ░░░███████░░█▒▒▒▒▒▒▒▒▒███░",
        };
    }
}
