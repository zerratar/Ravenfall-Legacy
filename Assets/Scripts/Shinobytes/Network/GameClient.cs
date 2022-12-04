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
    private readonly DateTime created;
    private readonly RavenBotConnection server;
    private readonly TcpClient client;
    private readonly Action<GameClient> onConnect;
    private readonly Action onConnectFailed;
    private readonly Action<string> onDataSent;
    private StreamReader reader;
    private StreamWriter writer;
    private bool receiveActive = false;
    private bool writeActive = false;


    private readonly ConcurrentQueue<GamePacket> toWrite = new ConcurrentQueue<GamePacket>();

    private bool disposed;
    private bool canReconnect;

    private DateTime lastPongSent;
    private float lastPongSentTime;

    public GameClient(RavenBotConnection server, TcpClient client, Action<string> onDataSent = null)
    {
        created = DateTime.UtcNow;
        this.onDataSent = onDataSent;
        this.server = server;
        this.client = client;
        this.IsRemote = false;
        reader = new StreamReader(this.client.GetStream());
        writer = new StreamWriter(this.client.GetStream());

        BeginReceive();
        BeginWrite();
    }

    public GameClient(RavenBotConnection server, Action<GameClient> onConnect, Action onConnectFailed, Action<string> onDataSent = null)
    {
        created = DateTime.UtcNow;
        this.server = server;
        this.IsRemote = true;
        this.client = new TcpClient();
        this.onConnect = onConnect;
        this.onConnectFailed = onConnectFailed;
        this.onDataSent = onDataSent;
        Connect(server.RemoteBotHost, server.RemoteBotPort);
    }

    private async void Connect(string host, int port)
    {
        try
        {
            await this.client.ConnectAsync(host, port);
            reader = new StreamReader(this.client.GetStream());
            writer = new StreamWriter(this.client.GetStream());
            BeginReceive();
            BeginWrite();

            await Task.Delay(500);

            if (onConnect != null)
            {
                onConnect.Invoke(this);
            }
            //this.client.BeginConnect(, new AsyncCallback(OnConnect), null);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError(exc);
            onConnectFailed?.Invoke();
        }
    }

    public bool IsRemote { get; }
    public bool IsLocal => !IsRemote;

    public void Update()
    {
        try
        {

            if (this.client.Connected)
            {
                BeginReceive();
                BeginWrite();

                //var pingPongDelta = UnityEngine.Time.realtimeSinceStartup - lastPongSentTime;
                //if (lastPongSentTime > 0 && pingPongDelta > PingPongTimeoutSeconds && server.IsConnectedToRemote)
                //{
                //    //Shinobytes.Debug.LogError("No Ping Pong from Remote Bot in the past " + pingPongDelta + " seconds! This may mean that we cannot get any data from the remote bot. Trying to force reconnect.");
                //    //server.Disconnect(BotConnectionType.Remote);
                //    lastPongSentTime = UnityEngine.Time.realtimeSinceStartup;
                //}
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError(exc);
        }
    }

    private void BeginReceive()
    {
        if (receiveActive)
        {
            return;
        }

        receiveActive = true;

        new System.Threading.Thread(() =>
        {
            try
            {
                if (client == null || !client.Connected)
                {
                    return;
                }

                while (client.Connected)
                {
                    if (disposed || (GameCache.IsAwaitingGameRestore && server.Game.RavenNest.Authenticated))
                        return;

                    try
                    {
                        var msg = reader.ReadLine();
                        HandlePacket(msg);
                    }
                    catch (Exception exc)
                    {
                        if (exc is System.ObjectDisposedException || exc is System.IO.IOException)
                        {
                            return;
                        }

                        server.LogError(exc.Message);
                        break;
                    }
                }

                if (disposed)
                    return;

                Disconnect();
            }
            catch
            {
                if (disposed)
                    return;

                Disconnect();
            }
            finally
            {
                receiveActive = false;
            }
        }).Start();


    }

    public void Disconnect()
    {
        try { Dispose(); } catch { }
        try { server.OnClientDisconnected(this); } catch { }
    }

    public bool Connected
    {
        get
        {
            try
            {
                if (disposed) return false;
                return client?.Connected ?? client?.Client?.Connected ?? false;
            }
            catch { return false; }
        }
    }

    private void BeginWrite()
    {
        if (writeActive)
        {
            return;
        }

        writeActive = true;

        new System.Threading.Thread(() =>
        {
            try
            {
                if (client == null || !client.Connected)
                {
                    return;
                }

                while (client.Connected)
                {
                    if (writer == null || disposed || GameCache.IsAwaitingGameRestore)
                        return;

                    try
                    {
                        if (toWrite.TryDequeue(out var cmd))
                        {
                            //server.Log(cmd);
                            if (cmd != null)
                            {
                                var json = JsonConvert.SerializeObject(cmd);
                                writer.WriteLine(json);
                                writer.Flush();

                                onDataSent?.Invoke(json);
                                continue;
                            }
                        }

                        //await Task.Delay(10);
                        System.Threading.Thread.Sleep(10);
                    }
                    catch (Exception exc)
                    {
                        server.LogError(exc.Message);
                    }
                }
            }
            catch (Exception exc)
            {
                server.LogError("BeginWrite exit early. exception: " + exc);
            }
            finally
            {
                writeActive = false;
            }
        }).Start();
    }

    public void SendMessage(PlayerController player, string format, params string[] args)
    {
        SendCommand(player.PlayerName, "message", format, args);
    }

    public void SendMessage(TwitchPlayerInfo player, string format, params string[] args)
    {
        SendCommand(player.Username, "message", format, args);
    }

    public void SendMessage(string playerName, string format, params string[] args)
    {
        SendCommand(playerName, "message", format, args);
    }

    //public void SendSessionOwner(string userId, string username)
    //{
    //    SendCommand("", "session_owner", "", userId, username);
    //}

    public void SendSessionOwner(string userId, string username, Guid sessionId)
    {
        if (string.IsNullOrEmpty(userId) || sessionId == Guid.Empty || string.IsNullOrEmpty(username))
        {
            //Shinobytes.Debug.Log("Oh my! We tried to send session details to the bot without having anything. A bit hasty arnt we?");
            return;
        }

        SendCommand("", "session_owner", "", userId, username, sessionId.ToString(), created.ToString());
    }

    public void SendPubSubToken(string userId, string username, string token)
    {
        SendCommand("", "pubsub_token", "", userId, username, token);
    }

    public void SendPong(int correlationId)
    {
        lastPongSent = DateTime.UtcNow;
        lastPongSentTime = UnityEngine.Time.realtimeSinceStartup;
        SendCommand("", "pong", "", correlationId.ToString());
    }

    public void SendFormat(string receiver, string format, params object[] args)
    {
        var a = args == null ? new string[0] : args.Select(x => x.ToString()).ToArray();
        SendMessage(receiver, format, a);
    }

    public void SendCommand(string playerName, string identifier, string format, params string[] args)
    {
        if (AdminControlData.NoChatBotMessages && identifier == "message")
            return;

        Write(new GamePacket(playerName, identifier, format, args));
    }

    public void Write(GamePacket cmd)
    {
        toWrite.Enqueue(cmd);
        Update();
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