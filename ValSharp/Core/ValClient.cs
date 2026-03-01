using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ValSharp.DTOs;
using ValSharp.Endpoints;
using ValSharp.Interceptors;

namespace ValSharp.Core
{
    public delegate void GameStateChangedEventHandler(GameState newState, GameState oldState);

    public class ValClient
    {
        public event GameStateChangedEventHandler GameStateChanged;
        public string? InterceptedClientConfigUrl => Interceptor is null ? null : $"http://{Interceptor.Host}:{Interceptor.HttpPort}";
        public string RCResourceId { get; private set; }
        public string XmppDomain { get; private set; }

        private readonly ILogger Logger;
        public readonly ValAuth Auth;
        public readonly MatchEndpoints Match;
        public readonly PartyEndpoints Party;
        public readonly PlayerEndpoints Player;
        public readonly ChatEndpoints Chat;
        public readonly ConfigEndpoints Config;
        public readonly IGContentEndpoints InGameContent;
        public readonly StorefrontEndpoints Store;

        private readonly ValorantInterceptor Interceptor;

        public GameState GameState
        {
            get => _gameState;
            private set
            {
                lock(_gameStateLock)
                {
                    if (_gameState != value)
                        this.GameStateChanged?.Invoke(value, _gameState);

                    _gameState = value;
                }
            }
        }

        private object _gameStateLock = new();
        private GameState _gameState;
        

        public ValClient(ValAuth auth, ILoggerFactory loggerFactory)
        {
            HttpUtils._logger = loggerFactory.CreateLogger(typeof(HttpUtils));
            Logger = loggerFactory.CreateLogger<ValClient>();

            Auth = auth;
            Interceptor = new ValorantInterceptor(logger: loggerFactory.CreateLogger<ValorantInterceptor>());

            Match = new MatchEndpoints(Logger, Auth);
            Party = new PartyEndpoints(Logger, Auth);
            Player = new PlayerEndpoints(Logger, Auth, Interceptor);
            Config = new ConfigEndpoints(Logger, Auth);
            InGameContent = new IGContentEndpoints(Logger, Auth);
            Store = new StorefrontEndpoints(Logger, Auth);
            Chat = new ChatEndpoints(Logger, Auth, Interceptor, this);

            _gameState = GameState.Unknown;

            Interceptor.PresenceReceived += OnPresence;
            GameStateChanged += OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState newState, GameState oldState)
        {
            Logger?.LogInformation($"Game state change from {oldState} to {newState}");
        }

        public void StartInterceptor()
        {
            if (Interceptor is null || Interceptor.IsStarted)
                return;

            Interceptor.Start();
        }

        private Task OnPresence(PresenceEventArgs e)
        {
            if (Auth.Subject is null)
                return Task.CompletedTask;

            if (!e.From.StartsWith(Auth.Subject))
                return Task.CompletedTask;

            var resourceId = e.From.Split('/')[1];
            var xmppDomain = e.From.Split('@')[1].Split('/')[0];

            if (RCResourceId != resourceId)
            {
                RCResourceId = resourceId;
                Logger?.LogDebug("RiotClient Resource Id: {ResId}", resourceId);
            }

            if (XmppDomain != xmppDomain)
            {
                XmppDomain = xmppDomain;
                Logger?.LogDebug("Xmpp Domain: {Domain}", xmppDomain);
            }

            if (e.P is null)
                return Task.CompletedTask;

            GameState = e.P?.SessionLoopState switch
            {
                "MENUS" => GameState.InMenu,
                "INGAME" => GameState.InGame,
                "PREGAME" => GameState.InPreGame,
                _ => GameState.Unknown
            };

            return Task.CompletedTask;
        }
    }

    public enum GameState
    {
        Unknown,
        InMenu,
        InPreGame,
        InGame,
    }

    public class InterceptorConfiguration
    {
        public string? Host { get; set; }
        public int? Port { get; set; }
    }
}
