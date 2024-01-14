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
    public const int ServerPort = 4040;
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
    public RavenBotConnection(RavenNest.SDK.ILogger logger, RavenNestClient ravenNest, GameManager game, RavenBot ravenbot)
    {
        this.logger = logger;
        this.ravenNest = ravenNest;
        this.game = game;
        this.ravenbot = ravenbot;
        this.localBotServerEnabled = !(PlayerSettings.Instance.LocalBotServerDisabled ?? false);

        if (localBotServerEnabled)
        {
            server = new TcpListener(new IPEndPoint(IPAddress.Any, ServerPort));
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

    public GameClient Local => connectedClients.Count > 0
            ? connectedClients.FirstOrDefault(x => x.Connected)
            : null;

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

    public void HandleNextPacket(params object[] args)
    {
        var packet = ReadPacket();
        if (packet == null)
        {
            // no new packet received.
            return;
        }

        if (packet.Message == null || string.IsNullOrEmpty(packet.Message.Identifier))
        {
            logger.WriteError("Received message from bot that was not properly deserialized. Message is null. ");
            return;
        }

        HandlePacket(packet, args);
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
            foreach (var client in connectedClients)
            {
                client.Disconnect();
                client.Dispose();
            }
        }
        catch { }

        connectedClients.Clear();

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

        var msg = JsonConvert.DeserializeObject<GameMessage>(rawCommand);
        var cmd = new BotMessage(gameClient, msg);

#if DEBUG
        logger.WriteMessage(rawCommand);
#endif


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

        packetHandler.Handle(packet);
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
        else if (!IsConnectedToRemote && UseRemoteBot)
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
                logger.WriteError("Error Connecting to Remote Bot: " + exc.Message);
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
    }

    public void OnClientConnected(TcpClient client)
    {
        logger.WriteMessage("Bot connected");
        var gameClient = new GameClient(this, client, str =>
        {
            DataSent?.Invoke(this, str);
        });

        connectedClients.Add(gameClient);
        LocalConnected?.Invoke(this, gameClient);
        if (ravenbot.State == BotState.Disconnected)
        {
            ravenbot.State = BotState.Connected;
        }
    }

    public void OnClientDisconnected(GameClient gameClient)
    {
        if (connectedClients.Remove(gameClient))
        {
            logger.WriteMessage("Bot disconnected");
            connectedClients.Remove(gameClient);
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