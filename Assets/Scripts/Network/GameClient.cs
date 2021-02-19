using Assets.Scripts;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

public class GameClient : IDisposable
{
    private readonly RavenBotConnection server;
    private readonly TcpClient client;
    private readonly Action<GameClient> onConnect;
    private readonly Action onConnectFailed;
    private StreamReader reader;
    private StreamWriter writer;

    private readonly ConcurrentQueue<GamePacket> toWrite
        = new ConcurrentQueue<GamePacket>();
    private bool disposed;

    public GameClient(RavenBotConnection server, TcpClient client)
    {
        this.server = server;
        this.client = client;
        this.IsRemote = false;
        reader = new StreamReader(this.client.GetStream());
        writer = new StreamWriter(this.client.GetStream());

        BeginReceive();
        BeginWrite();
    }

    public GameClient(RavenBotConnection server, Action<GameClient> onConnect, Action onConnectFailed)
    {
        this.server = server;
        this.IsRemote = true;
        this.client = new TcpClient();
        this.onConnect = onConnect;
        this.onConnectFailed = onConnectFailed;

        try
        {
            this.client.BeginConnect(server.RemoteBotHost, server.RemoteBotPort, new AsyncCallback(OnConnect), null);
        }
        catch (Exception exc)
        {
            UnityEngine.Debug.LogError(exc);
            onConnectFailed?.Invoke();
        }
    }

    public bool IsRemote { get; }
    public bool IsLocal => !IsRemote;

    private void OnConnect(IAsyncResult ar)
    {
        try
        {
            this.client.EndConnect(ar);
            reader = new StreamReader(this.client.GetStream());
            writer = new StreamWriter(this.client.GetStream());
            BeginReceive();
            BeginWrite();

            if (onConnect != null)
            {
                onConnect.Invoke(this);
            }
        }
        catch
        {
            server.LogError("Unable to connect to remote bot.");
            if (onConnectFailed != null)
            {
                onConnectFailed.Invoke();
            }
        }
    }

    private async void BeginReceive()
    {
        while (client.Connected)
        {
            if (disposed || GameCache.Instance.IsAwaitingGameRestore)
                return;

            try
            {
                var msg = await reader.ReadLineAsync();
                HandlePacket(msg);
            }
            catch (Exception exc)
            {
                server.LogError(exc.ToString());
                Disconnect();
                return;
            }
        }

        Disconnect();
    }

    private void Disconnect()
    {
        try
        {
            Dispose();
        }
        catch
        {
        }

        server.OnClientDisconnected(this);
    }

    public bool Connected => client.Connected;

    private async void BeginWrite()
    {
        while (client.Connected)
        {
            if (disposed || GameCache.Instance.IsAwaitingGameRestore)
                return;

            try
            {
                if (toWrite.TryDequeue(out var cmd))
                {
                    //server.Log(cmd);
                    await writer.WriteLineAsync(JsonConvert.SerializeObject(cmd));
                    await writer.FlushAsync();
                }
                else
                {
                    await Task.Delay(10);
                }
            }
            catch (Exception exc)
            {
                server.LogError(exc.ToString());
            }
        }
    }

    public void SendMessage(PlayerController player, string format, params string[] args)
    {
        SendCommand(player.PlayerName, "message", format, args);
    }

    public void SendMessage(Player player, string format, params string[] args)
    {
        SendCommand(player.Username, "message", format, args);
    }

    public void SendMessage(string playerName, string format, params string[] args)
    {
        SendCommand(playerName, "message", format, args);
    }

    public void SendSessionOwner(string userId, string username)
    {
        SendCommand("", "session_owner", "", userId, username);
    }

    public void SendSessionOwner(string userId, string username, Guid sessionId)
    {
        SendCommand("", "session_owner", "", userId, username, sessionId.ToString());
    }

    public void SendFormat(string receiver, string format, params object[] args)
    {
        var a = args == null ? new string[0] : args.Select(x => x.ToString()).ToArray();
        SendMessage(receiver, format, a);
    }

    public void SendCommand(string playerName, string identifier, string format, params string[] args)
    {
        Write(new GamePacket(playerName, identifier, format, args));
    }

    public void Write(GamePacket cmd)
    {
        toWrite.Enqueue(cmd);
    }

    private void HandlePacket(string cmd)
    {
        server.DataReceived(this, cmd);
    }

    public void Dispose()
    {
        if (disposed) return;
        try
        {
            disposed = true;
            client?.Dispose();
            reader?.Dispose();
            writer?.Dispose();
        }
        catch { }
    }
}