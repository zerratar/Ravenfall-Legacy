using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using RavenNest.Protocol;

namespace RavenNest.SDK.V2
{
    public enum GameConnectionState
    {
        /// <summary>Not started, or stopped.</summary>
        Off,
        Connecting,
        /// <summary>Welcomed: saves, commands and events flow.</summary>
        Ready,
        /// <summary>The server said no and not to come back (see <see cref="GameConnectionV2.LastRejection"/>).</summary>
        Rejected
    }

    /// <summary>
    /// The game's side of the v2 connection (docs/network-v2.md): one WebSocket to RavenNest, kept open on a
    /// background thread and reopened after a drop with the same resume id, so the server replays what was missed.
    /// It only moves messages; <see cref="NetworkV2"/> decides what they mean, on the main thread.
    /// </summary>
    public sealed class GameConnectionV2 : IDisposable
    {
        private static readonly int[] RetryDelaysMs = { 1000, 2000, 5000, 10000 };

        private readonly Uri uri;
        private readonly string clientVersion;
        private readonly BlockingCollection<IMessage> outbox = new BlockingCollection<IMessage>(new ConcurrentQueue<IMessage>());
        private CancellationTokenSource stop;
        private Task loop;
        private string token;
        private ClientWebSocket socket;
        private DateTime lastReceived;
        private DateTime lastSent;

        public GameConnectionV2(string webApiEndpoint, string clientVersion)
        {
            uri = ToWebSocketUri(webApiEndpoint);
            this.clientVersion = clientVersion;
        }

        /// <summary>Kept for the life of this game run; the server resumes a session for the same id.</summary>
        public Guid ResumeId { get; } = Guid.NewGuid();

        public GameConnectionState State { get; private set; }

        public bool IsReady => State == GameConnectionState.Ready;

        public Rejected LastRejection { get; private set; }

        /// <summary>The newest event the main thread applied; sent in Hello so a resumed session replays the rest.</summary>
        public long LastAppliedEventSequence;

        /// <summary>Messages from the server, for the main thread. A <see cref="Welcome"/> marks each new connection.</summary>
        public ConcurrentQueue<IMessage> Incoming { get; } = new ConcurrentQueue<IMessage>();

        public long MessagesIn, MessagesOut, BytesIn, BytesOut, Reconnects;

        /// <summary>The round trip of the last ping, in milliseconds.</summary>
        public double PingMs { get; private set; }

        public Uri Uri => uri;

        public static Uri ToWebSocketUri(string webApiEndpoint)
        {
            var api = new Uri(webApiEndpoint);
            if (api.IsLoopback)
            {
                // The local server's certificate is self-signed, and the editor's TLS (UnityTLS) refuses it whatever
                // validation callback is set, so the local server is reached over plain ws on port 80, as
                // LocalDevServerEndpoint.WebSocketEndpoint already does. Production has a real certificate.
                return new UriBuilder("ws", api.Host, 80, ProtocolInfo.Path).Uri;
            }

            var scheme = api.Scheme == "https" ? "wss" : "ws";
            return new UriBuilder(scheme, api.Host, api.Port, ProtocolInfo.Path).Uri;
        }

        /// <summary>Starts connecting (again) with the token from api/game/connection-token.</summary>
        public void Start(string connectionToken)
        {
            token = connectionToken;
            if (loop != null && !loop.IsCompleted)
            {
                // already running: reconnect now with the new token
                try { socket?.Abort(); } catch { }
                return;
            }

            LastRejection = null;
            stop = new CancellationTokenSource();
            State = GameConnectionState.Connecting;
            var ct = stop.Token;
            loop = Task.Run(() => RunAsync(ct));
        }

        /// <summary>Queues a message; it goes out as soon as the connection is ready.</summary>
        public void Send(IMessage message)
        {
            if (!outbox.IsAddingCompleted)
            {
                outbox.Add(message);
            }
        }

        public void Stop()
        {
            try { stop?.Cancel(); } catch { }
            State = GameConnectionState.Off;
        }

        public void Dispose()
        {
            Stop();
            try { socket?.Abort(); } catch { }
        }

        private async Task RunAsync(CancellationToken ct)
        {
            var attempt = 0;
            while (!ct.IsCancellationRequested)
            {
                var welcomed = false;
                try
                {
                    welcomed = await ConnectOnceAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exc)
                {
                    Shinobytes.Debug.LogWarning("Server connection (v2): " + exc.Message);
                }
                finally
                {
                    try { socket?.Abort(); } catch { }
                    socket = null;
                }

                if (State == GameConnectionState.Rejected || ct.IsCancellationRequested)
                {
                    break;
                }

                State = GameConnectionState.Connecting;
                Interlocked.Increment(ref Reconnects);
                attempt = welcomed ? 0 : attempt + 1;
                var delay = RetryDelaysMs[Math.Min(attempt, RetryDelaysMs.Length - 1)];
                try { await Task.Delay(delay, ct); } catch (OperationCanceledException) { break; }
            }

            if (State != GameConnectionState.Rejected)
            {
                State = GameConnectionState.Off;
            }
        }

        /// <summary>One connection, from Hello to its end. True when it got as far as a Welcome.</summary>
        private async Task<bool> ConnectOnceAsync(CancellationToken ct)
        {
            socket = new ClientWebSocket();
            socket.Options.KeepAliveInterval = TimeSpan.Zero; // our own pings, which also notice a dead server
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                timeout.CancelAfter(10_000);
                await socket.ConnectAsync(uri, timeout.Token);
            }

            lastReceived = lastSent = DateTime.UtcNow;
            await WriteAsync(new Hello
            {
                ClientVersion = clientVersion,
                Token = token,
                ResumeId = ResumeId,
                LastEventSequence = Interlocked.Read(ref LastAppliedEventSequence)
            }, ct);

            // nothing but the Hello goes out before the Welcome
            var first = await ReadAsync(ct, TimeSpan.FromSeconds(10));
            if (first is Rejected rejected)
            {
                OnRejected(rejected);
                return false;
            }

            if (!(first is Welcome welcome))
            {
                throw new IOException("the server did not answer Hello");
            }

            State = GameConnectionState.Ready;
            Incoming.Enqueue(welcome);

            using (var connection = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                var writer = Task.Run(() => WriteLoopAsync(connection.Token));
                try
                {
                    while (!connection.IsCancellationRequested)
                    {
                        var message = await ReadAsync(connection.Token, null);
                        if (message == null)
                        {
                            break; // closed
                        }

                        switch (message)
                        {
                            case Ping ping:
                                Send(new Pong { Timestamp = ping.Timestamp });
                                break;
                            case Pong pong:
                                PingMs = (DateTime.UtcNow.Ticks - pong.Timestamp) / (double)TimeSpan.TicksPerMillisecond;
                                break;
                            case Rejected r:
                                OnRejected(r);
                                return true;
                            default:
                                Incoming.Enqueue(message);
                                break;
                        }
                    }
                }
                finally
                {
                    connection.Cancel();
                    try { await writer; } catch { }
                }
            }
            return true;
        }

        private void OnRejected(Rejected rejected)
        {
            LastRejection = rejected;
            Shinobytes.Debug.LogWarning("Server connection (v2) refused: " + rejected.Reason + " " + rejected.Message);
            if (!rejected.Reconnect)
            {
                State = GameConnectionState.Rejected;
                Incoming.Enqueue(rejected);
            }
        }

        private async Task WriteLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (outbox.TryTake(out var message, 250))
                    {
                        await WriteAsync(message, ct);
                        continue;
                    }

                    var now = DateTime.UtcNow;
                    if ((now - lastReceived).TotalMilliseconds > ProtocolInfo.DeadAfterMs)
                    {
                        Shinobytes.Debug.LogWarning("Server connection (v2): nothing heard for " + ProtocolInfo.DeadAfterMs / 1000 + " s, reconnecting.");
                        socket.Abort();
                        return;
                    }

                    if ((now - lastSent).TotalMilliseconds > ProtocolInfo.PingIntervalMs)
                    {
                        await WriteAsync(new Ping { Timestamp = now.Ticks }, ct);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogWarning("Server connection (v2) could not send: " + exc.Message);
                try { socket.Abort(); } catch { }
            }
        }

        private async Task WriteAsync(IMessage message, CancellationToken ct)
        {
            var data = ProtocolSerializer.Serialize(message);
            await socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, ct);
            lastSent = DateTime.UtcNow;
            Interlocked.Increment(ref MessagesOut);
            Interlocked.Add(ref BytesOut, data.Length);
        }

        private readonly byte[] receiveBuffer = new byte[64 * 1024];

        /// <summary>The next message we understand, or null when the socket closed.</summary>
        private async Task<IMessage> ReadAsync(CancellationToken ct, TimeSpan? timeout)
        {
            using (var limit = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                if (timeout.HasValue) limit.CancelAfter(timeout.Value);
                while (true)
                {
                    var message = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), limit.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            return null;
                        }
                        message.Write(receiveBuffer, 0, result.Count);
                        // every part counts: a big message (a restore of hundreds of players) can take longer to
                        // arrive than DeadAfterMs, and was taken for a dead connection halfway through
                        lastReceived = DateTime.UtcNow;
                        if (message.Length > ProtocolInfo.MaxMessageBytes)
                        {
                            throw new IOException("message too large");
                        }
                    } while (!result.EndOfMessage);

                    lastReceived = DateTime.UtcNow;
                    Interlocked.Increment(ref MessagesIn);
                    Interlocked.Add(ref BytesIn, message.Length);

                    if (result.MessageType != WebSocketMessageType.Binary)
                    {
                        continue;
                    }

                    try
                    {
                        if (ProtocolSerializer.TryDeserialize(new ReadOnlyMemory<byte>(message.GetBuffer(), 0, (int)message.Length), out var msg))
                        {
                            return msg;
                        }
                    }
                    catch (Exception exc)
                    {
                        Shinobytes.Debug.LogWarning("Server connection (v2): could not read a message: " + exc.Message);
                    }
                }
            }
        }
    }
}
