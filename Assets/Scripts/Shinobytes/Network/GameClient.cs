using Assets.Scripts;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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


    private readonly ConcurrentQueue<GameMessageResponse> toWrite = new ConcurrentQueue<GameMessageResponse>();

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
            await Task.Delay(1000);

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
            Shinobytes.Debug.LogError("GameClient.Update: " + exc);
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

                        server.LogError("GameClient.BeginReceive: " + exc.Message);
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
                            var json = JsonConvert.SerializeObject(cmd);
                            writer.WriteLine(json);
                            writer.Flush();
                            onDataSent?.Invoke(json);
                            continue;
                        }
                    }
                    catch (Exception exc)
                    {
                        System.Threading.Thread.Sleep(10);
                        server.LogError("GameClient.BeginWrite: " + exc.Message);
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

    public void SendReply(GameMessage sourceMessage, PlayerController player, string format, params object[] args)
    {
        var recipent = GameMessageRecipent.Create(player);
        Write(GameMessageResponse.CreateReply("message", recipent,
            format.Replace("  ", " "), // a friendly trim.
            args, sourceMessage.CorrelationId));
    }

    public void SendReplyUseMessageIfNotNull(GameMessage sourceMessage, User player, string format, params object[] args)
    {
        if (sourceMessage != null)
        {
            SendReply(sourceMessage, format, args);
            return;
        }

        SendReply(GameMessageRecipent.Create(player), format, args);
    }

    public void Announce(string format, object[] args, string category = null, params string[] tags)
    {
        Write(new GameMessageResponse(
            "message",
            GameMessageRecipent.System,
            format.Replace("  ", " "), // a friendly trim.
            args, tags, category, string.Empty));
    }

    public void SendReply(PlayerController player, string format, params object[] args)
    {
        var recipent = GameMessageRecipent.Create(player);
        SendReply(recipent, format, args);
    }

    public void SendReply(GameMessageRecipent recipent, string format, object[] args)
    {
        Write(GameMessageResponse.CreateReply("message", recipent,
            format.Replace("  ", " "), // a friendly trim.
            args, string.Empty));
    }

    public void SendReply(GameMessage sourceMessage, string format, params object[] args)
    {
        Write(
            GameMessageResponse.CreateReply("message",
            GameMessageRecipent.Create(sourceMessage.Sender),
            format.Replace("  ", " "), // a friendly trim.
            args,
            sourceMessage.CorrelationId));
    }



    public void SendSessionOwner(Guid sessionId, Guid userId, Dictionary<string, object> userSettings)
    {
        if (sessionId == Guid.Empty || userId == Guid.Empty)
            return;

        Write(GameMessageResponse.CreateArgs("session", sessionId.ToString(), userId.ToString(), created.ToString(), userSettings));
    }

    public void SendPubSubToken(string userId, string username, string token)
    {
        Write(GameMessageResponse.CreateArgs("pubsub_token", userId, username, token));
    }

    public void SendPong(string correlationId)
    {
        lastPongSent = DateTime.UtcNow;
        lastPongSentTime = UnityEngine.Time.realtimeSinceStartup;
        Write(GameMessageResponse.CreateEmptyReply("pong", correlationId));
    }

    public void Write(GameMessageResponse cmd)
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