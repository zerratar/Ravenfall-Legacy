using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

public class GameServer : IDisposable
{
    public const int ServerPort = 4040;

    private readonly GameManager game;

    private readonly TcpListener server;
    
    private readonly List<GameClient> connectedClients
        = new List<GameClient>();

    private readonly ConcurrentQueue<Packet> availablePackets
        = new ConcurrentQueue<Packet>();

    private readonly ConcurrentDictionary<string, Type> packetHandlers
        = new ConcurrentDictionary<string, Type>();

    public GameServer(GameManager game)
    {
        this.game = game;
        var ipEndPoint = new IPEndPoint(IPAddress.Any, ServerPort);
        server = new TcpListener(ipEndPoint);
    }

    public bool IsBound => server.Server.IsBound;

    public void Start()
    {
        server.Start(0x1000);
        server.BeginAcceptTcpClient(OnAcceptTcpClient, null);
        game.Log("Server started");
    }

    public GameClient Client => connectedClients.Count > 0
            ? connectedClients.FirstOrDefault(x => x.Connected)
            : null;

    public Packet ReadPacket()
    {
        if (availablePackets.TryDequeue(out var packet))
        {
            game.Log("Read packet: " + packet.JsonDataType);
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
        if (server.Server.IsBound)
        {
            server.Stop();
        }
    }

    public void Dispose()
    {
        Stop();
    }

    public void DataReceived(GameClient gameClient, string rawCommand)
    {
        //game.Log("Raw data: " + rawCommand);
        var index = rawCommand.IndexOf(':');
        var jsonDataType = rawCommand.Remove(index);
        var jsonData = rawCommand.Substring(index + 1);
        availablePackets.Enqueue(new Packet(gameClient, jsonDataType, jsonData));
    }

    public void Register<T>(string packetCommand)
    {
        packetHandlers[packetCommand.ToLower()] = typeof(T);
    }

    public void SendObject<T>(T obj)
    {
        Client?.Write(JsonConvert.SerializeObject(obj));
    }

    public void Send(string correlationId, string playerName, string rawCommand)
    {
        Client?.Write(correlationId + "|" + playerName + ":" + rawCommand);
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

    public void OnClientConnected(TcpClient client)
    {
        game.Log("Client connected");
        var gameClient = new GameClient(this, client);
        connectedClients.Add(gameClient);
    }

    public void OnClientDisconnected(GameClient gameClient)
    {
        game.Log("Client disconnected");
        connectedClients.Remove(gameClient);
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