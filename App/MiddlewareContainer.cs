using DeepSeek.Core;
using DeepSeek.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using ValSharp.Core;

namespace ValSharp_Demo
{
    internal class MiddlewareContainer
    {
        public static Dictionary<string, string> LastPlayerMessages = new();
        public static string? LastMessagePlayerPUUID = null;
        public static bool IsAIResponseEnabled = false;

        private readonly ValClient valClient;
        private readonly ILogger<MiddlewareContainer> logger;
        private readonly DeepSeekClient deepSeekClient = new DeepSeekClient(AIConstants.DEEPSEEK_API_KEY);

        public MiddlewareContainer(ValClient client, ILoggerFactory loggerFactory)
        {
            valClient = client;
            logger = loggerFactory.CreateLogger<MiddlewareContainer>();

            valClient.GameStateChanged += (@new, @old) =>
            {
                LastPlayerMessages.Clear();
            };
        }

        [Incoming]
        public MiddlewareAction LastPlayerMessageMiddleware(InGameMessage message)
        {
            if (message.Body.StartsWith("!"))
                return MiddlewareAction.Continue;

            LastPlayerMessages[message.FromSubject] = message.Body;
            LastMessagePlayerPUUID = message.FromSubject;

            return MiddlewareAction.Continue;
        }

        [Outgoing]
        public async Task<MiddlewareAction> AIResponseMiddleware(InGameMessage message)
        {
            if (!IsAIResponseEnabled || message.Body.StartsWith("!"))
                return MiddlewareAction.Continue;

            var chatRequest = AIConstants.OSMANLI();
            chatRequest.Messages.Add(Message.NewUserMessage(message.Body));

            var chatResponse = await deepSeekClient.ChatAsync(chatRequest, CancellationToken.None);
            string? textResponse = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrEmpty(textResponse))
            {
                message.Drop();
                return MiddlewareAction.Cancel;
            }

            message.Modify(textResponse!);
            logger.LogDebug("AI Response: " + textResponse!);

            return MiddlewareAction.Cancel;
        }
    }
}
