using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RavenNest.SDK;
using UnityEngine;

public class RavenBotConnection : IDisposable
{
    public const int DefaultLocalBotServerPort = 4040;
    public readonly int RemoteBotPort = 4041;

    private readonly bool localBotServerEnabled;
    private readonly RavenNest.SDK.ILogger logger;
    private readonly RavenNestClient ravenNest;
    private readonly GameManager game;
    private readonly RavenBot ravenbot;
    private readonly TcpListener server;
    private readonly List<GameClient> connectedClients = new List<GameClient>();
    private readonly ConcurrentQueue<BotMessage> availablePackets = new ConcurrentQueue<BotMessage>();
    private readonly ConcurrentDictionary<string, Type> packetHandlers = new ConcurrentDictionary<string, Type>();

    /// <summary>Replies written to a connection that was replaced, waiting for the next one (with when they were made).</summary>
    private readonly ConcurrentQueue<(GameMessageResponse Message, DateTime Created)> orphanedReplies = new ConcurrentQueue<(GameMessageResponse, DateTime)>();

    /// <summary>Chat commands already handled, by identifier and chat message id, so one sent twice runs once.</summary>
    private readonly Dictionary<string, DateTime> handledCommands = new Dictionary<string, DateTime>();
    private DateTime nextHandledCommandsCleanup;

    /// <summary>Time per frame for bot commands. It was one command a frame, so a busy chat built a long queue.</summary>
    private const double CommandBudgetMs = 4;
    private const int MaxCommandsPerFrame = 50;

    /// <summary>The remote bot pings every few seconds; this long without a word means the connection is dead.</summary>
    private static readonly TimeSpan RemoteSilenceLimit = TimeSpan.FromSeconds(30);
    private readonly System.Diagnostics.Stopwatch commandBudget = new System.Diagnostics.Stopwatch();
    private GameClient remoteClient;
    private int reconnectionTimer = 1500;
    private bool allowAutomaticReconnect = true;
    private bool connectionInProgress;
    private float connectionAttemptStart;

    public event EventHandler<string> DataSent;
    public event EventHandler<GameClient> LocalConnected;
    public event EventHandler<GameClient> RemoteConnected;
    public event EventHandler<GameClient> LocalDisconnected;
    public event EventHandler<GameClient> RemoteDisconnected;

    public GameManager Game => game;
    public RavenBotConnection(
        RavenNest.SDK.ILogger logger,
        RavenNestClient ravenNest,
        GameManager game,
        RavenBot ravenbot)
    {
        this.logger = logger;
        this.ravenNest = ravenNest;
        this.game = game;
        this.ravenbot = ravenbot;
        this.localBotServerEnabled = !(PlayerSettings.Instance.LocalBotServerDisabled ?? false);

        if (localBotServerEnabled)
        {
            var port = PlayerSettings.Instance.LocalBotPort ?? DefaultLocalBotServerPort;
            server = new TcpListener(new IPEndPoint(IPAddress.Any, port));
        }

        string remoteBotServer = PlayerSettings.Instance.RavenBotServer;

        if (Application.isEditor || string.IsNullOrEmpty(PlayerSettings.Instance.RavenBotServer))
        {
            remoteBotServer = ravenNest.Settings.RavenbotEndpoint;
        }

        if (string.IsNullOrEmpty(remoteBotServer))
        {
            remoteBotServer = "127.0.0.1";
        }

        if (remoteBotServer.Contains(":"))
        {
            var parts = remoteBotServer.Split(":");
            if (int.TryParse(parts[^1], out var newRemoteBotPort))
                RemoteBotPort = newRemoteBotPort;
            remoteBotServer = parts[0];
        }

        RemoteBotHost = remoteBotServer;
    }

    public GameClient Local
    {
        get
        {
            lock (connectedClients)
            {
                return connectedClients.FirstOrDefault(x => x.Connected);
            }
        }
    }

    public GameClient Remote => remoteClient;
    public GameClient ActiveClient
    {
        get
        {
            if (IsConnectedToRemote)
                return remoteClient;
            if (IsConnectedToLocal)
                return Local;
            return null;
        }
    }

    public bool IsConnectedToRemote => Remote?.Connected ?? false;
    public bool IsConnectedToLocal => Local?.Connected ?? false;
    public bool IsConnected => IsConnectedToRemote || IsConnectedToLocal;

    public string RemoteBotHost { get; internal set; }
    public bool UseRemoteBot { get; internal set; } = true;

    public BotMessage ReadPacket()
    {
        availablePackets.TryDequeue(out var packet);
        return packet;
    }

    /// <summary>
    /// Handles the waiting bot commands, as many as fit in a few milliseconds this frame. Also notices a remote
    /// connection that has gone silent.
    /// </summary>
    public void HandleNextPacket(params object[] args)
    {
        CheckRemoteHeartbeat();

        commandBudget.Restart();
        var handled = 0;
        while (handled < MaxCommandsPerFrame && commandBudget.Elapsed.TotalMilliseconds < CommandBudgetMs)
        {
            var packet = ReadPacket();
            if (packet == null)
            {
                return;
            }

            handled++;

            // anything from the bot shows it works. Only the remote bot pings, so a streamer on the local bot was
            // told "Connecting to the bot" for as long as it ran, while their chat commands worked
            if (ravenbot != null && ravenbot.State != BotState.Ready)
            {
                ravenbot.State = BotState.Ready;
            }

            if (packet.Message == null || string.IsNullOrEmpty(packet.Message.Identifier))
            {
                logger.WriteError("Received message from bot that was not properly deserialized. Message is null. ");
                continue;
            }

            if (IsRepeat(packet.Message))
            {
                continue;
            }

            HandlePacket(packet, args);
        }
    }

    /// <summary>
    /// True for a chat command already handled: the same identifier and chat message id within ten minutes. A bot
    /// that resends after a reconnect can then not make anything happen twice.
    /// </summary>
    private bool IsRepeat(GameMessage message)
    {
        var id = message.CorrelationId;
        if (string.IsNullOrEmpty(id) || message.Identifier.Equals("ping", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var now = DateTime.UtcNow;
        if (now >= nextHandledCommandsCleanup)
        {
            nextHandledCommandsCleanup = now.AddMinutes(1);
            var old = handledCommands.Where(x => now - x.Value > TimeSpan.FromMinutes(10)).Select(x => x.Key).ToList();
            foreach (var key in old) handledCommands.Remove(key);
        }

        var commandKey = message.Identifier.ToLowerInvariant() + "|" + id;
        if (handledCommands.ContainsKey(commandKey))
        {
            logger.WriteDebug("Ignored a repeat of '" + message.Identifier + "' (" + id + ").");
            return true;
        }

        handledCommands[commandKey] = now;
        return false;
    }

    private void CheckRemoteHeartbeat()
    {
        var remote = remoteClient;
        if (remote == null || !remote.Connected)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (now - remote.LastReceivedUtc > RemoteSilenceLimit && now - remote.ConnectedUtc > RemoteSilenceLimit)
        {
            logger.WriteWarning("Nothing heard from the remote bot for " + (int)RemoteSilenceLimit.TotalSeconds + " seconds; reconnecting.");
            remote.Disconnect();
        }
    }

    /// <summary>
    /// Sends a reply on whichever connection is live now. Used by a connection that was closed or replaced, so
    /// replies to commands it brought in are not lost; with no connection they wait (up to two minutes) for the next.
    /// </summary>
    internal void SendOnLiveConnection(GameMessageResponse message, GameClient from)
    {
        var live = ActiveClient;
        if (live != null && live != from && live.Connected)
        {
            live.Write(message);
            return;
        }

        orphanedReplies.Enqueue((message, DateTime.UtcNow));
    }

    private void SendOrphanedReplies(GameClient client)
    {
        var now = DateTime.UtcNow;
        while (orphanedReplies.TryDequeue(out var item))
        {
            if (now - item.Created < TimeSpan.FromMinutes(2))
            {
                client.Write(item.Message);
            }
        }
    }

    public void Stop(bool allowReconnect)
    {
        try
        {
            this.allowAutomaticReconnect = allowReconnect;
            if (ActiveClient.Connected)
            {
                ActiveClient.Disconnect();
            }

            if (Remote != null)
            {
                Remote.Dispose();
            }

            if (Local != null)
            {
                Local.Dispose();
            }

            remoteClient = null;
        }
        catch { }

        try
        {
            GameClient[] clients;
            lock (connectedClients)
            {
                clients = connectedClients.ToArray();
                connectedClients.Clear();
            }

            foreach (var client in clients)
            {
                client.Disconnect();
                client.Dispose();
            }
        }
        catch { }

        if (localBotServerEnabled)
        {
            if (server.Server.IsBound)
            {
                server.Stop();
            }

            if (!server.Server.IsBound)
            {
                logger.WriteDebug("Bot Server stopped.");
            }
        }
    }

    public void Dispose()
    {
        Stop(false);
    }

    public void DataReceived(GameClient gameClient, string rawCommand)
    {
        if (string.IsNullOrEmpty(rawCommand))
        {
            return;
        }

        GameMessage msg;
        try
        {
            msg = JsonConvert.DeserializeObject<GameMessage>(rawCommand);
        }
        catch (Exception exc)
        {
            // one bad line no longer closes the connection
            logger.WriteError("Could not read a message from the bot: " + exc.Message);
            return;
        }

        var cmd = new BotMessage(gameClient, msg);

        //#if DEBUG
        //        logger.WriteMessage(rawCommand);
        //#endif


        availablePackets.Enqueue(cmd);
    }

    public void Register<T>(string packetCommand)
    {
        packetHandlers[packetCommand.ToLower()] = typeof(T);
    }

    public void SendReply(PlayerController receiver, string format, params object[] args)
    {
        var client = ActiveClient;
        if (client == null) return;
        client.SendReply(receiver, format, args);
    }

    public void Announce(string format, params object[] args)
    {
        var client = ActiveClient;
        if (client == null) return;
        client.Announce(format, args);
    }

    private void HandlePacket(BotMessage msg, params object[] packetHandlerArgs)
    {
        try
        {
            var type = msg.Message.Identifier;
            if (!packetHandlers.TryGetValue(type.ToLower(), out var handlerType))
            {
                logger.WriteError($"'{type}' is not a known command. :(");
                return;
            }

            HandlePacket(handlerType, packetHandlerArgs, msg);
        }
        catch (Exception exc)
        {
            logger.WriteError(exc.ToString());
        }
    }

    private void HandlePacket(
        Type packetHandlerType,
        object[] packetHandlerArgs,
        BotMessage packet)
    {
        var packetHandler = InstantiateHandler(packetHandlerType, packetHandlerArgs);
        if (packetHandler == null)
        {
            throw new Exception(
                $"Nooo! Packet handler for {packetHandlerType.FullName} could not be instantiated. Plz fix!");
        }

        // if this packet was from a ingame player, then set last activity to now
        try
        {
            if (game?.Players != null && packet?.Message?.Sender != null)
            {
                var targetPlayer = game.Players.GetPlayer(packet.Message.Sender);
                if (targetPlayer != null)
                {
                    // this will help determining afk players.
                    targetPlayer.LastChatCommandUtc = DateTime.UtcNow;
                }
            }
        }
        catch
        {
            // ignored.
        }

        // server commands sent by this handler get request ids made from the chat message id
        RavenNest.SDK.V2.ChatCommandContext.Begin(packet.Message.Identifier, packet.Message.CorrelationId);
        try
        {
            packetHandler.Handle(packet);
        }
        finally
        {
            RavenNest.SDK.V2.ChatCommandContext.End();
        }
    }

    internal void ConnectInternal(BotConnectionType type)
    {
        if (!allowAutomaticReconnect)
        {
            return;
        }

        Connect(type);
    }

    public void Connect(BotConnectionType type)
    {
        if (type == BotConnectionType.Local)
        {
            if (localBotServerEnabled && (server == null || !server.Server.IsBound))
            {
                ListenForLocalBot();
            }
        }
        else if (!IsConnectedToRemote && UseRemoteBot && allowAutomaticReconnect)
        {
            var sinceLastConnectionAttempt = UnityEngine.Time.time - connectionAttemptStart;
            if (connectionInProgress && sinceLastConnectionAttempt < 2)
            {
                return;
            }

            connectionInProgress = true;
            connectionAttemptStart = UnityEngine.Time.time;
            try
            {

                logger.WriteMessage("Connecting to remote bot...");
                remoteClient?.Dispose();
                remoteClient = new GameClient(this, OnClientConnected, OnRemoteConnectionFailed, str =>
                {
                    DataSent?.Invoke(this, str);
                });
            }
            catch (Exception exc)
            {
                connectionInProgress = false;
                logger.WriteError("Error Connecting to Remote Bot: " + exc);
            }
            finally
            {
            }
        }
    }

    private async void OnRemoteConnectionFailed()
    {
        connectionInProgress = false;
        ravenbot.State = BotState.Disconnected;

        if (!allowAutomaticReconnect)
        {
            return;
        }

        logger.WriteWarning("Failed to connect to remote bot. Retrying");

        await Task.Delay(reconnectionTimer);

        if (IsConnectedToLocal)
        {
            //reconnectionTimer = 1000;
            logger.WriteMessage("Connected to local bot, remote reconnection tries cancelled.");
            return;
        }

        //reconnectionTimer += 1000;
        //reconnectionTimer = Math.Min(3000, reconnectionTimer);
        //reconnectionTimer = 1000;

        Connect(BotConnectionType.Remote);
    }

    internal void Disconnect(BotConnectionType type)
    {
        ravenbot.State = BotState.Disconnected;
        if (type == BotConnectionType.Local)
        {
            // disconnecting local means stopping the server.
            Stop(false);
        }
        else if (remoteClient != null)
        {
            remoteClient.Disconnect();
            remoteClient.Dispose();
            remoteClient = null;
        }
    }

    private void ListenForLocalBot()
    {
        if (!Overlay.IsGame)
        {
            return;
        }

        if (!localBotServerEnabled)
        {
            return;
        }

        try
        {
            server.Start(0x1000);
            server.BeginAcceptTcpClient(OnAcceptTcpClient, null);
            logger.WriteMessage("Bot Server started");
        }
        catch (Exception exc)
        {
            logger.WriteError("Unable to start bot server. If using centralized bot, this can be ignored. " + exc.Message);
        }
    }

    private ChatBotCommandHandlerBase InstantiateHandler(Type packetHandlerType, params object[] args)
    {
        var ctors = packetHandlerType.GetConstructors(
            BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        var ctor = ctors.FirstOrDefault();
        if (ctor == null)
        {
            logger.WriteError($"InstantiateHandler: No public constructor found!");
            return null;
        }

        var parameters = ctor.GetParameters();
        if (parameters.Length != args.Length)
        {
            logger.WriteError($"InstantiateHandler: Unexpected amount of parameters for ctor: {parameters.Length}, expected: {args.Length}");
            return null;
        }

        return (ChatBotCommandHandlerBase)ctor.Invoke(args);
    }

    private void OnAcceptTcpClient(IAsyncResult ar)
    {
        if (!localBotServerEnabled)
        {
            return;
        }

        try
        {
            var tcpClient = server.EndAcceptTcpClient(ar);
            OnClientConnected(tcpClient);
        }
        catch { }
        try
        {
            server.BeginAcceptTcpClient(OnAcceptTcpClient, null);
        }
        catch { }
    }


    public void OnClientConnected(GameClient client)
    {
        connectionInProgress = false;

        logger.WriteMessage("Connected to remote bot");
        RemoteConnected?.Invoke(this, client);
        reconnectionTimer = 1000;
        if (ravenbot.State == BotState.Disconnected)
        {
            ravenbot.State = BotState.Connected;
        }
        if (game.RavenNest == null)
        {
            return;
        }

        if (game.RavenNest.Authenticated && game.RavenNest.SessionStarted)
        {
            SendSessionOwner();
        }

        SendOrphanedReplies(client);
    }

    public void OnClientConnected(TcpClient client)
    {
        logger.WriteMessage("Bot connected");
        var gameClient = new GameClient(this, client, str =>
        {
            DataSent?.Invoke(this, str);
        });

        lock (connectedClients)
        {
            connectedClients.Add(gameClient);
        }
        LocalConnected?.Invoke(this, gameClient);
        SendOrphanedReplies(gameClient);
        // the local bot is up once it has connected (it never pings); whether it is in the channel it says itself
        ravenbot.State = BotState.Ready;
    }

    public void OnClientDisconnected(GameClient gameClient)
    {
        bool wasLocal;
        lock (connectedClients)
        {
            wasLocal = connectedClients.Remove(gameClient);
        }

        if (wasLocal)
        {
            logger.WriteMessage("Bot disconnected");
            LocalDisconnected?.Invoke(this, gameClient);
        }
        else if (gameClient.IsRemote)
        {
            logger.WriteMessage("Disconnected from remote bot");
            RemoteDisconnected?.Invoke(this, gameClient);
        }
    }

    private bool TryParseHexColor(string hexColorString, out Color color)
    {
        color = Color.white;
        if (string.IsNullOrEmpty(hexColorString))
        {
            color = TwitchColors.All[Mathf.FloorToInt(TwitchColors.All.Length * UnityEngine.Random.value)];
            return false;
        }

        if (hexColorString.StartsWith("#"))
        {
            hexColorString = hexColorString.Substring(1);
        }

        var rgb = new float[3];
        for (var i = 0; i < 3; ++i)
        {
            var hex = hexColorString.Substring(i * 2, 2);
            rgb[i] = (float)int.Parse(hex, System.Globalization.NumberStyles.HexNumber) / 255f;
        }

        color = new Color(rgb[0], rgb[1], rgb[2], 1f);
        return true;
    }

    public void LogError(string message) => Shinobytes.Debug.LogError(message);

    public void Log(string message) => Shinobytes.Debug.Log(message);

    internal bool SendSessionOwner()
    {
        if (!IsConnected)
        {
            return false;
        }

        ActiveClient.SendSessionOwner(game.RavenNest.SessionId, game.RavenNest.UserId, game.RavenNest.UserSettings);

        return true;
    }

}

public enum BotConnectionType
{
    Local,
    Remote
}

public class SessionOwnerMessage
{
    public string TwitchUserId { get; set; }
    public string TwitchUserName { get; set; }
    public Guid SessionId { get; set; }
}

public static class TwitchColors
{
    private static Color FromHex(int i)
    {
        float r = (i >> 16) & 255;
        float g = (i >> 8) & 255;
        float b = i & 255;
        return new Color(r / 255f, g / 255f, b / 255f, 1);
    }

    public static Color Blue = new Color(0.2216981f, 0.2216981f, 1f, 1f);
    public static Color Coral = FromHex(0xFF7F50);
    public static Color DodgerBlue = FromHex(0x1E90FF);
    public static Color SpringGreen = FromHex(0x00FA9A);
    public static Color YellowGreen = FromHex(0x9ACD32);
    public static Color Green = FromHex(0x00FF00);
    public static Color OrangeRed = FromHex(0xFF4500);
    public static Color Red = FromHex(0xFF0000);
    public static Color GoldenRod = FromHex(0xDAA520);
    public static Color HotPink = FromHex(0xFF69B4);
    public static Color CadetBlue = FromHex(0x5F9EA0);
    public static Color SeaGreen = FromHex(0x2E8B57);
    public static Color Chocolate = FromHex(0xD2691E);
    public static Color BlueViolet = FromHex(0x8A2BE2);
    public static Color Firebrick = FromHex(0xB22222);
    public static Color[] All = {
        Blue,
        Coral,
        DodgerBlue,
        SpringGreen,
        YellowGreen,
        Green,
        OrangeRed,
        Red,
        GoldenRod,
        HotPink,
        CadetBlue,
        SeaGreen,
        Chocolate,
        BlueViolet,
        Firebrick
    };
}