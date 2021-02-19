using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class RavenBotConnection : IDisposable
{
    public const int ServerPort = 4040;
    public readonly int RemoteBotPort = 4041;

    private readonly GameManager game;
    private readonly TcpListener server;
    private readonly List<GameClient> connectedClients = new List<GameClient>();
    private readonly ConcurrentQueue<Packet> availablePackets = new ConcurrentQueue<Packet>();
    private readonly ConcurrentDictionary<string, Type> packetHandlers = new ConcurrentDictionary<string, Type>();
    private SessionOwnerMessage queuedSessionOwnerMessage;
    private GameClient remoteClient;

    public event EventHandler<GameClient> LocalConnected;
    public event EventHandler<GameClient> RemoteConnected;
    public event EventHandler<GameClient> LocalDisconnected;
    public event EventHandler<GameClient> RemoteDisconnected;

    public RavenBotConnection(GameManager game, string remoteBotServer)
    {
        this.game = game;
        server = new TcpListener(new IPEndPoint(IPAddress.Any, ServerPort));

        if (string.IsNullOrEmpty(remoteBotServer))
            remoteBotServer = "127.0.0.1";

        RemoteBotHost = remoteBotServer;
    }

    public bool IsBound => server.Server.IsBound;


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

    public Packet ReadPacket()
    {
        if (availablePackets.TryDequeue(out var packet))
        {
            //game.Log("Read packet: " + packet.JsonDataType);
            return packet;
        }
        return null;
    }

    public void HandleNextPacket(params object[] args)
    {
        var packet = ReadPacket();
        if (string.IsNullOrEmpty(packet?.JsonDataType))
        {
            return;
        }

        HandlePacket(packet, args);
    }

    public void Stop()
    {
        try
        {
            foreach (var client in connectedClients)
            {
                client.Dispose();
            }
        }
        catch { }

        connectedClients.Clear();

        if (server.Server.IsBound)
        {
            server.Stop();
        }

        if (!server.Server.IsBound)
        {
            game.Log("Bot Server stopped.");
        }
    }

    public void Dispose()
    {
        Stop();
    }

    public void DataReceived(GameClient gameClient, string rawCommand)
    {
        var index = rawCommand.IndexOf(':');
        var jsonDataType = rawCommand.Remove(index);
        var jsonData = rawCommand.Substring(index + 1);
        availablePackets.Enqueue(new Packet(gameClient, jsonDataType, jsonData));
    }

    public void Register<T>(string packetCommand)
    {
        packetHandlers[packetCommand.ToLower()] = typeof(T);
    }

    public void SendCommand(string receiver, string identifier, string message, params string[] args)
    {
        var client = ActiveClient;
        if (client == null) return;
        client.SendCommand(receiver, identifier, message, args);
    }

    public void SendMessage(string receiver, string format, params string[] args)
    {
        var client = ActiveClient;
        if (client == null) return;
        client.SendMessage(receiver, format, args);
    }
    public void Send(string receiver, string format, params object[] args)
    {
        var client = ActiveClient;
        if (client == null) return;
        var a = args == null ? new string[0] : args.Select(x => x.ToString()).ToArray();
        client.SendMessage(receiver, format, a);
    }
    public void Broadcast(string format, params object[] args)
    {
        var client = ActiveClient;
        if (client == null) return;
        var a = args == null ? new string[0] : args.Select(x => x.ToString()).ToArray();
        client.SendMessage(string.Empty, format, a);
    }

    private void HandlePacket(Packet packet, params object[] packetHandlerArgs)
    {
        try
        {
            var type = packet.JsonDataType;
            if (!packetHandlers.TryGetValue(type.ToLower(), out var handlerType))
            {
                game.LogError($"'{type}' is not a known command. :(");
                return;
            }

            HandlePacket(handlerType, packetHandlerArgs, packet);
        }
        catch (Exception exc)
        {
            game.LogError(exc.ToString());
        }
    }

    private void HandlePacket(
        Type packetHandlerType,
        object[] packetHandlerArgs,
        Packet packet)
    {
        var packetHandler = InstantiateHandler(packetHandlerType, packetHandlerArgs);
        if (packetHandler == null)
        {
            throw new Exception(
                $"Nooo! Packet handler for {packetHandlerType.FullName} could not be instantiated. Plz fix!");
        }

        packetHandler.Handle(packet);
    }

    internal void Connect(BotConnectionType type)
    {
        if (type == BotConnectionType.Local)
        {
            if (server == null || !server.Server.IsBound)
            {
                ListenForLocalBot();
            }
        }
        else if (!IsConnectedToRemote)
        {
            game.Log("Connecting to remote bot...");
            remoteClient = new GameClient(this, OnClientConnected, OnRemoteConnectionFailed);
        }
    }

    private async void OnRemoteConnectionFailed()
    {
        game.Log("Failed to connect to remote bot. Retrying");

        await Task.Delay(1000);

        if (IsConnectedToLocal)
        {
            game.Log("Connected to local bot, remote reconnection tries cancelled.");
            return;
        }

        Connect(BotConnectionType.Remote);
    }

    internal void Disconnect(BotConnectionType type)
    {
        if (type == BotConnectionType.Local)
        {
            // disconnecting local means stopping the server.
            Stop();
        }
        else if (remoteClient != null)
        {
            //remoteClient.SendCommand("", "leave", game.RavenNest.TwitchUserId + "$" + game.RavenNest.TwitchUserName);
            remoteClient.Dispose();
            remoteClient = null;
        }
    }

    private void ListenForLocalBot()
    {
        server.Start(0x1000);
        server.BeginAcceptTcpClient(OnAcceptTcpClient, null);
        game.Log("Bot Server started");
    }

    private PacketHandler InstantiateHandler(Type packetHandlerType, params object[] args)
    {
        var ctors = packetHandlerType.GetConstructors(
            BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        var ctor = ctors.FirstOrDefault();
        if (ctor == null)
        {
            game.LogError($"InstantiateHandler: No public constructor found!");
            return null;
        }

        var parameters = ctor.GetParameters();
        if (parameters.Length != args.Length)
        {
            game.LogError($"InstantiateHandler: Unexpected amount of parameters for ctor: {parameters.Length}, expected: {args.Length}");
            return null;
        }

        return (PacketHandler)ctor.Invoke(args);
    }

    private void OnAcceptTcpClient(IAsyncResult ar)
    {
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
        game.Log("Connected to remote bot");
        RemoteConnected?.Invoke(this, client);

        if (queuedSessionOwnerMessage != null)
        {
            var msg = queuedSessionOwnerMessage;
            SendSessionOwner(msg.TwitchUserId, msg.TwitchUserName, msg.SessionId);
        }
    }

    public void OnClientConnected(TcpClient client)
    {
        game.Log("Bot connected");
        var gameClient = new GameClient(this, client);
        connectedClients.Add(gameClient);
        LocalConnected?.Invoke(this, gameClient);

        if (queuedSessionOwnerMessage != null)
        {
            var msg = queuedSessionOwnerMessage;
            SendSessionOwner(msg.TwitchUserId, msg.TwitchUserName, msg.SessionId);
        }
    }

    public void OnClientDisconnected(GameClient gameClient)
    {
        if (connectedClients.Remove(gameClient))
        {
            game.Log("Bot disconnected");
            connectedClients.Remove(gameClient);
            LocalDisconnected?.Invoke(this, gameClient);
        }
        else
        {
            game.LogError("Disconnected from remote bot");
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

    public void LogError(string message) => game.LogError(message);

    public void Log(string message) => game.Log(message);

    public void Announce(string message, params string[] args)
    {
        if (!IsConnected) return;
        ActiveClient.SendMessage("", message, args);
    }

    internal void SendSessionOwner(string twitchUserId, string twitchUserName, Guid sessionId)
    {
        if (!IsConnected)
        {
            this.queuedSessionOwnerMessage = new SessionOwnerMessage
            {
                TwitchUserId = twitchUserId,
                TwitchUserName = twitchUserName,
                SessionId = sessionId
            };
            return;
        }

        ActiveClient.SendSessionOwner(twitchUserId, twitchUserName, sessionId);
        if (ActiveClient.IsRemote)
        {
            game.Log("Sent session info to server");
        }
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