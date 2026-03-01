## What is ValSharp?
ValSharp is a proof of concept wrapper library for interacting with Valorant. It exposes a API in C# to get game data. It also offers a interceptor to intercept chat messages between your client and Riot servers

---
#### Note that this is a old project so the code in this project may not be the best. There are some bugs and there are some unfinished features. I started this project for fun and Im still doing it for fun in my free times. Also Riot changed their chat api to XMPP and this broke many old chat code. I didnt looked much into it so old chat code is still there. If you want to test it yourself you can look at the demo project provided.

---

## Project Structure
* **📁 ValSharp**
  * 📄 `ExtensionMethods.cs`
  * 📄 `HttpUtils.cs`
  * **📁 Content** — *External Valorant API*
    * 📄 `ValContent.cs`
  * **📁 Core** — *Authentication and Client*
    * 📄 `ValAuth.cs`
    * 📄 `ValClient.cs`
  * **📁 DTOs**
  * **📁 Endpoints**
  * **📁 Interceptors** — *In-Game Chat Interceptors. Used to intercept XMPP communication*
    * 📄 `RiotXMPPInterceptor.cs`
    * 📄 `ValorantInterceptor.cs`

* **📁 ValSharp-Demo** — *Example implementation project*
  * 📄 `AIConstants.cs` — *AI Prompts*
  * 📄 `AIFunctions.cs` — *Function that AI can call (for ask command)*
  * 📄 `Attributes.cs`
  * 📄 `CommandContainer.cs`
  * 📄 `FuzzyMatcher.cs` — *Search logic*
  * 📄 `InGameCache.cs`
  * 📄 `MiddlewareContainer.cs`
  * 📄 `Program.cs`

## XMPP Interception
Riot uses XMPP for in game chat, ValSharp hosts a Tcp server for local client to connect and then routes the xml traffic to the riot servers. This way we can read, send, suspress messages. With this we can listen to incoming/outgoing presence updates & messages.

For XMPP Interception to work you need to create a certificate. Create a **📁 certs** folder at the execution directory of the software. Use a tool like **OpenSSL** to create a 📄 `server.cert` and 📄 `server.key` and put them in **📁 certs** folder.

Note: I know code is bad readability wise but making it work was a pain.

### Auth Phase
To work with Riot and local game api's you need both basic and bearer authentication. 📄`ValAuth` class does this authentication. When this game is launched a `lockfile` is created. This file contains the necessary information for basic authentication and then bearer jwt authentication. There is no need for logging in with username and password.


### ValClient
ValClient combines all of the necessary logic into a single class. It handles XMPP interception and exposes events listed below;

- `GameStateChanged` - Triggers when game state is changed (ex: InGame -> InMenu)
- `Chat.MessageReceived` - Incoming Message
- `Chat.OutgoingMessage` - Outgoing Message (message body can be changed or message can be dropped)
- `Player.PresenceReceived`

You can use endpoint classes to call local/remote game apis to get data or to do things.


## Chat
To work with chat you can use `Chat` endpoint class in your `ValClient` instance.
Apis in Chat endpoint class are not fully functional (some of them were deprecated apis riot switched to xmpp and they no longer work) but you can still send and receive messages with XMPP interceptor. You can do so by subscribing to the message events.

### Working with outgoing messages
`Chat.OutgoingMessage` triggers when you send a message from your client (game). Before your message is sent to Riot servers this event is triggered. You can change the body of this message by modifying `Body` property inside `OutgoingMessageEventArgs` or you can drop the message by setting `Drop` to `true`.

### Sending a new message
Unlike modifying a message sending is another story. You need to send the message with correct parameters so that Riot's server processes it. You can send 2 types of messages to the client or to the riot server. When you send to client that message will be faked so no one but you will see that message. Faked messages will not be sent to Riot servers.

#### To send a normal message you need following parameters;
- Channel Type (Type of the channel that you are sending message into. Look at `enum ChatChannelType`)
- Message (Body of the message)
- To (Destination of the message)

Destination needs to be in following format
```
[match/room id]-[team]@[service].[shard/region].[XMPP Domain]
```
Example:
```
00000000-0000-0000-0000-000000000000-blue@ares-coregame.eu2.pvp.net
```

You can look at `Reply` and `ReplySilently` in `ValSharp-Demo.Program` to see a example construction.

#### To send a fake message you need following parameters;
- Channel Type (Type of the channel that you are sending message into. Look at `enum ChatChannelType`)
- Message (Body of the message)
- To (Destination of the message)
- From (Sender of the message)
- Jid

Destination needs to be in following format;
```
[puuid]@[xmpp-domain]/[Riot Client Resource Id]
```
Example;
```
00000000-0000-0000-0000-000000000000@eu1.pvp.net/RC-0000000000
```
You can find your own Riot Client Resource Id in `ValClient.RCResourceId` and XmppDomain in `ValClient.XmppDomain`
these values gets stored once you receive a presence. (On presence updates RCResourceId can change too)

From (Sender) should look like this;
```
00000000-0000-0000-0000-000000000000-blue@ares-coregame.eu2.pvp.net/00000000-0000-0000-0000-000000000000
```

First part is same as destination formate for normal messages. After `/` is the puuid of the player that sent the message. In our case because we want to send message to ourselves puuid should be our own. You can get your own puuid from `ValClient.Auth.Subject`

##  ValSharp-Demo
ValSharp-Demo is a functional demo app. It has a Command & Middleware architecture. It has commands that you can call from the game chat and also it has AI support (DeepSeek) for messages. Beware that this is not a finished software this is just a demo for a proof of concept wrapper. To use it you need to download the source and compile yourself. You can put your DeepSeek API key in api-key.txt file at your execution directory. If you want to use another model or add more functionality you need to code those yourself.

### Features
- AI Message Editing (You write your message and send to chat AI changes it to the format that you want and send it)
  - !ai - Enable/Disable AI edit

- AI Reply (by typing !r or !r [playername] after a players message you can let AI reply to them in your own fashion)

- Skin Finder (Find who owns a skin in game)
  - !kimin [Skin Name] [Weapon Name]

- Player Skin
  - !s [Player or Agent Name] [Weapon Name]

- Player Rank Details
  - !rank [Player or Agent Name]

- Player Party Details
  - !party [Player or Agent Name]

- AI Ask
  - !sor [Prompt]
  - !sorw [Player or Agent Name] - Add/Remove player to whitelist  for using AI ask command (To save tokens by default only you can use AI ask command)
  - !soriptal - Stops AI Ask command

- Chat Art 
  - !art - Lists all avaliable chat art short names
  - !art [short name] - Sends chat art to chat

### NOTE: If you put '!!' insted of '!' your command will be on silent mode. Your command will be dropped as message but still handled and the response from that command will sent to you silently.

### AI Ask
You can use !sor command to ask AI things about the game. Functions that can supply In game information can be called by AI and thuss it can generate a response. Current prompt for this command is not realy good. Vast majority of times AI dont know what to do and cant provide an answer where it should be able to.

### Middleware System
Middlewares are methods in `MiddlewareContainer` class that gets called when a incoming/outgoing message is received they can drop/change outgoing messages or block the event to reach to command handlers. In demo app there are 2 middlewares;
- `LastPlayerMessageMiddleware` - Saves last messages of players that are not commands
- `AIResponseMiddleware` - If AI edit is enabled it generates the AI response from users original message  and sends it

Middleware methods should be `public` they can return `MiddlewareAction` or `Task<MiddlewareAction>` so they can be async. They should have a single parameter with type `InGameMessage`.
You can put `IncomingAttribute` or `OutgoingAttribute` this attributes can tell the executor to when to invoke middleware. If you dont put any direction attribute middleware will get invoked for both directions.

If you return `MiddlewareAction.Continue` then execution flow will continue. Other middlewares will run and then command handlers will be run. But if you return `MiddlewareAction.Cancel` then execution flow will be stopped so other middlewares will not run and command handler will not work.

Middleware Example
```cs
[Outgoing]
public async Task<MiddlewareAction> TestMiddleware(InGameMessage message)
{
    switch (message.Body)
    {
        case "Change Me":
            message.Modify("Hello, World!");
            return MiddlewareAction.Cancel;
        case "Drop me":
            message.Drop();
            return MiddlewareAction.Cancel;
        default:
            return MiddlewareAction.Continue;
    }
}
```

### Command System
Commands are methods that can be called from game chat with putting `!` at the begining of the message. Just like middlewares you need to put command methods in `CommandContainer`. Your command method should have a `CommandAttribute` with a command name (Dont add command trigger to this name). Also like middlewares Incoming and Outgoing attributes can be put on commands to serve the same purpose. Command methods can have `void` or `Task` as their return types. They should be `public` and they should have a `InGameMessage` parameter as its first parameter. If you want to get arguments you can add `string[]` as your second parameter.

!art command from source;
```cs
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
```

Replies from commands will be sent after a short while after command execution is completed. So they are not sent directly.

### Launching Game
For XMPP Interceptor to work both game and RiotClient should be closed and for local auth to work game needs to be open, how will this work? In the entry point `Main` method its checked whether game or client is open or not. If so It'll ask the user to kill it. When user approves game is automaticly launched with correct configuration to make XMPP Interceptor to work. (Manualy launching will not work unless you give the right starting parameters) After game is launched app will wait for the game to start and will try to get authenticated. After authentication it'll be ready to use.

### Notes
- Im aware that I should not call reflection methods every call but this is just a fun demo project so I'm not planning on doing much optimizations on this.

- Code is a bit dirty. As I mentioned at the start its an old project and I combined old code with new code. I did not refactord old code.

- Not all endpoint are implemented or working some of them are partialy implemented.

- If you happen to close the app while game is running your game chat and friends list will probably be broken. To fix it you'll need to reopen the game with or without software. To debug or apply changes while game is runing you can use Visual Studio as your IDE and Hot Reload feature within that. You can also put breakpoints or logs to debug your code.

- Presence acquired from API and from XMPP are diffrent classes and they are a bit weird

#### Known Bugs
- !rank seems to return self rank for every player requested


### Attribution & Sources
- https://valorant-api.com/ 
- https://github.com/techchrism/valorant-api-docs
- https://github.com/techchrism/valorant-xmpp-logger
- https://github.com/giorgi-o/CrossPlatformPlaying/wiki/Riot-Games
