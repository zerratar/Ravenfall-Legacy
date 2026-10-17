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
    private readonly System.Threading.SemaphoreSlim writeSignal = new System.Threading.SemaphoreSlim(0);
    private readonly object threadStart = new object();

    private bool disposed;
    private bool canReconnect;

    private DateTime lastPongSent;
    private float lastPongSentTime;

    /// <summary>When the bot last sent anything, for noticing a connection that died without closing.</summary>
    public DateTime LastReceivedUtc { get; private set; } = DateTime.UtcNow;

    /// <summary>When this connection was opened.</summary>
    public DateTime ConnectedUtc => created;

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
        lock (threadStart)
        {
            if (receiveActive)
            {
                return;
            }

            receiveActive = true;
        }

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
                        if (msg == null)
                        {
                            break; // the bot closed the connection
                        }

                        LastReceivedUtc = DateTime.UtcNow;
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
        lock (threadStart)
        {
            if (writeActive)
            {
                return;
            }

            writeActive = true;
        }

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
                            var json = JsonConvert.SerializeObject(cmd);
                            writer.WriteLine(json);
                            writer.Flush();
                            onDataSent?.Invoke(json);
                            continue;
                        }

                        // wait for the next message instead of spinning a core
                        writeSignal.Wait(1000);
                    }
                    catch (Exception exc) when (exc is IOException || exc is ObjectDisposedException)
                    {
                        // the socket is gone: hand what is left to the next connection and close this one
                        server.LogError("GameClient.BeginWrite: the connection broke: " + exc.Message);
                        if (!disposed) Disconnect();
                        return;
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
            Languages.ArrangeArguments(format, args), sourceMessage.CorrelationId));
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
            Languages.ArrangeArguments(format, args), tags, category, string.Empty));
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
            Languages.ArrangeArguments(format, args), string.Empty));
    }

    public void SendReply(GameMessage sourceMessage, string format, params object[] args)
    {
        Write(
            GameMessageResponse.CreateReply("message",
            GameMessageRecipent.Create(sourceMessage.Sender),
            format.Replace("  ", " "), // a friendly trim.
            Languages.ArrangeArguments(format, args),
            sourceMessage.CorrelationId));
    }



    public void SendSessionOwner(Guid sessionId, Guid userId, Dictionary<string, object> userSettings)
    {
        if (sessionId == Guid.Empty || userId == Guid.Empty)
            return;

        // ISO 8601, so the bot reads it the same whatever the culture on either side
        Write(GameMessageResponse.CreateArgs("session", sessionId.ToString(), userId.ToString(), created.ToString("o"), userSettings));
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
        if (disposed)
        {
            // a reply to a command that came in on a connection that has since been replaced: it goes to the live
            // connection (or waits for the next one) instead of being lost with this one
            server.SendOnLiveConnection(cmd, this);
            return;
        }

        toWrite.Enqueue(cmd);
        try { writeSignal.Release(); } catch (ObjectDisposedException) { }
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
            try { writeSignal.Release(); } catch { }
        }
        catch { }

        // whatever was not written yet goes to the next connection
        while (toWrite.TryDequeue(out var pending))
        {
            server.SendOnLiveConnection(pending, this);
        }
    }
}