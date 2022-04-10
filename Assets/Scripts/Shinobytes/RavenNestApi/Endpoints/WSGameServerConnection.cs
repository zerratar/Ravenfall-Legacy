using Assets.Scripts;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RavenNest.SDK.Endpoints
{
    public class WSGameServerConnection : IGameServerConnection, IDisposable
    {
        private readonly ILogger logger;
        private readonly IAppSettings settings;
        private readonly ITokenProvider tokenProvider;
        private readonly IGamePacketSerializer packetSerializer;
        private readonly GameManager gameManager;
        private readonly Dictionary<string, GamePacketHandler> packetHandlers = new Dictionary<string, GamePacketHandler>();
        private readonly Queue<PlayerGamePacketRef> sendQueue = new Queue<PlayerGamePacketRef>();
        private readonly Dictionary<Guid, TaskCompletionSource<GamePacket>> awaitedReplies = new Dictionary<Guid, TaskCompletionSource<GamePacket>>();

        private Thread sendProcessThread;
        private Thread readProcessThread;

        private PartialGamePacket unfinishedPacket;
        private ClientWebSocket webSocket;
        private bool disposed;
        private bool connected;
        private int connectionCounter;
        private bool reconnecting;
        private volatile bool connecting;

        public event EventHandler OnReconnected;

        public int SendAsyncTimeout { get; set; } = 5000;

        public WSGameServerConnection(
            ILogger logger,
            IAppSettings settings,
            ITokenProvider tokenProvider,
            IGamePacketSerializer packetSerializer,
            GameManager gameManager)
        {
            this.logger = logger;
            this.settings = settings;
            this.tokenProvider = tokenProvider;
            this.packetSerializer = packetSerializer;
            this.gameManager = gameManager;
        }

        public bool IsReady
        {
            get
            {
                if (webSocket == null)
                {
                    return false;
                }

                if (webSocket.State == WebSocketState.Open)
                {
                    return true;
                }

                if (webSocket.CloseStatus != null)
                {
                    return false;
                }

                if (!connected)
                {
                    return false;
                }

                return false;
            }
        }
        public bool ReconnectRequired => !IsReady && Volatile.Read(ref connectionCounter) > 0;

        public void Reconnect()
        {
            reconnecting = true;
            Close();
        }

        public async Task<bool> CreateAsync()
        {
            if (connecting)
            {
                return true;
            }
            if (connected)
            {
                return false;
            }

            connecting = true;
            try
            {
                //logger.Debug("Connecting to the server...");

                var sessionToken = tokenProvider.GetSessionToken();
                if (sessionToken == null)
                    return true;

                var token = JsonConvert.SerializeObject(sessionToken);
                var sessionTokenData = token.Base64Encode();

                webSocket = new ClientWebSocket();

                webSocket.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36");
                webSocket.Options.SetRequestHeader("session-token", sessionTokenData);
                await webSocket.ConnectAsync(new Uri(settings.WebSocketEndpoint), CancellationToken.None);

                connected = true;
                connecting = false;

                Interlocked.Increment(ref connectionCounter);

                //logger.Debug("Connected to the server");

                if (readProcessThread == null)
                {
                    (readProcessThread = new Thread(ProcessRead)).Start();
                }

                if (sendProcessThread == null)
                {
                    (sendProcessThread = new Thread(ProcessSend)).Start();
                }

                if (reconnecting)
                {
                    reconnecting = false;
                    OnReconnected?.Invoke(this, EventArgs.Empty);
                }

                return true;
            }
            catch (Exception exc)
            {
                logger.Error("Connection failed: " + exc.Message);
                connected = false;
            }
            finally
            {
                connecting = false;
            }
            return false;
        }

        private async void ProcessRead()
        {
            while (!disposed)
            {
                if (!await ReceiveDataAsync().ConfigureAwait(false))
                {
                    await Task.Delay(10);
                    //System.Threading.Thread.Sleep(10);
                }
            }
        }

        private async void ProcessSend()
        {
            while (!disposed)
            {
                if (!await SendDataAsync().ConfigureAwait(false))
                {
                    await Task.Delay(10);
                    //System.Threading.Thread.Sleep(10);
                }
            }
        }

        private async Task<bool> SendDataAsync()
        {
            if (!IsReady)
            {
                sendQueue.Clear();
                return false;
            }

            if (GameCache.Instance.IsAwaitingGameRestore) return false;
            if (sendQueue.Count == 0) return false;

            var packetsToSend = new List<GamePacket>();
            var packetMerge = new Dictionary<string, PlayerGamePacketRef>();
            var packetQueueCount = sendQueue.Count;

            while (sendQueue.TryDequeue(out var packet))
            {
                if (packet.Sender == Guid.Empty)
                {
                    packetsToSend.Add(packet.Packet);
                }
                else
                {
                    var key = packet.Sender + packet.Key;
                    if (!packetMerge.TryGetValue(key, out var prev))
                    {
                        prev = packet;
                        packetMerge[key] = prev;
                        prev.SendIndex = packetsToSend.Count;
                        packetsToSend.Add(prev.Packet);
                    }

                    if (prev.Created < packet.Created)
                    {
                        packetsToSend[prev.SendIndex] = packet.Packet;
                    }
                }
            }

            try
            {
                // if it fails, we can skip it since it does not include important information that cannot be sent again later with a new update.
                byte[] data = packetSerializer.SerializeMany(packetsToSend);
                await webSocket.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
                return true;
            }
            catch (Exception exc)
            {
                logger.Error("Error sending data to server: " + exc.ToString());
                Disconnect();
            }

            return false;
        }

        private async Task<bool> ReceiveDataAsync()
        {
            if (GameCache.Instance.IsAwaitingGameRestore) return false;
            if (!IsReady)
            {
                //if (!await CreateAsync())
                return false;
            }

            try
            {
                var buffer = new byte[4096];
                var segment = new ArraySegment<byte>(buffer);
                var result = await webSocket.ReceiveAsync(segment, CancellationToken.None);

                if (result.CloseStatus != null || !string.IsNullOrEmpty(result.CloseStatusDescription))
                {
                    Disconnect();
                    return false;
                }

                if (!result.EndOfMessage)
                {
                    if (unfinishedPacket == null)
                    {
                        unfinishedPacket =
                            new PartialGamePacket(
                                packetSerializer,
                                segment.Array,
                                result.Count);
                    }
                    else
                    {
                        unfinishedPacket.Append(segment.Array, result.Count);
                    }
                }
                else
                {
                    try
                    {
                        GamePacket packet = null;
                        if (unfinishedPacket != null)
                        {
                            unfinishedPacket.Append(segment.Array, result.Count);
                            packet = unfinishedPacket.Build();
                            unfinishedPacket = null;
                        }
                        else
                        {
                            packet = packetSerializer.Deserialize(segment.Array, result.Count);
                        }

                        if (awaitedReplies.TryGetValue(packet.CorrelationId, out var task))
                        {
                            if (task.TrySetResult(packet))
                            {
                                return true;
                            }
                        }

                        await HandlePacketAsync(packet);
                    }
                    catch (Exception exc)
                    {
                        Shinobytes.Debug.LogError("Error deserializing packet: " + exc.Message);
                    }
                }
                return true;
            }
            //catch (System.IO.IOException exc)
            //{
            //    logger.Error(exc.ToString());
            //    this.Disconnect();
            //    return false;
            //}
            catch (WebSocketException socketExc)
            {
                if (socketExc.WebSocketErrorCode != WebSocketError.ConnectionClosedPrematurely)
                {
                    logger.Error(socketExc.Message);
                }

                //gameManager.ForceGameSessionUpdate();

                Disconnect();
                return false;
            }
            catch (Exception exc)
            {
                logger.Error(exc.Message);
                Disconnect();
                return false;
            }
        }

        private async Task HandlePacketAsync(GamePacket packet)
        {
            if (packetHandlers.TryGetValue(packet.Id, out var handler))
            {
                await handler.HandleAsync(packet);
            }
        }

        public void Dispose()
        {
            try
            {
                if (webSocket != null)
                {
                    if (IsReady)
                    {
                        Disconnect();
                    }

                    webSocket.Dispose();
                }
                //readProcessThread.Join();
                //sendProcessThread.Join();
            }
            catch { }
        }

        private void Disconnect()
        {
            logger.Debug("Disconnected from server");

            connecting = false;
            connected = false;

            if (webSocket != null)
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    webSocket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
                }
                try
                {
                    webSocket.Dispose();
                    webSocket = null;
                }
                catch { }
            }
        }

        public void Register<TPacketHandler>(
            string packetId,
            TPacketHandler packetHandler) where TPacketHandler : GamePacketHandler
        {
            packetHandlers[packetId] = packetHandler;
        }

        public async Task<GamePacket> SendAsync(PlayerGamePacketRef packet)
        {
            var completionSource = new TaskCompletionSource<GamePacket>();

            awaitedReplies[packet.Packet.CorrelationId] = completionSource;

            sendQueue.Enqueue(packet);

            await Task.WhenAny(completionSource.Task, Task.Delay(SendAsyncTimeout));

            if (completionSource.Task.IsCompleted)
            {
                return completionSource.Task.Result;
            }

            return null;
        }
        public void SendNoAwait(PlayerGamePacketRef packet)
        {
            sendQueue.Enqueue(packet);
        }

        public Task<GamePacket> SendAsync(Guid sender, string id, object model)
        {
            return SendAsync(
                new PlayerGamePacketRef()
                {
                    Key = id,
                    Sender = sender,
                    Created = DateTime.UtcNow,

                    Packet = new GamePacket()
                    {
                        CorrelationId = Guid.NewGuid(),
                        Data = model,
                        Id = id,
                        Type = model.GetType().Name
                    }
                });
        }

        public void SendNoAwait(Guid sender, string id, object model, string type)
        {
            SendNoAwait(new PlayerGamePacketRef()
            {
                Key = id,
                Sender = sender,
                Created = DateTime.UtcNow,
                Packet = new GamePacket()
                {
                    CorrelationId = Guid.NewGuid(),
                    Data = model,
                    Id = id,
                    Type = type ?? model.GetType().Name
                }
            });
        }

        public void Close()
        {
            Disconnect();
            connected = false;
            connecting = false;
            Dispose();
        }
    }
}
