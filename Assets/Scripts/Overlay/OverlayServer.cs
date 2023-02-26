using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Assets.Scripts.Overlay
{
    public class OverlayServer : OverlayCommunication, IDisposable
    {
        private const double StartRetryTimeSeconds = 3;

        private TcpListener server;
        private bool disposed;
        private bool started;
        private bool isStarting;
        private readonly GameManager gameManager;
        private readonly IOverlayPacketManager packetManager;

        private DateTime lastStartTry;

        private readonly object clientMutex = new object();
        private List<OverlayClient> clients = new List<OverlayClient>();

        public event EventHandler<OverlayClient> ClientConnected;
        private OverlayClient lastConnectedClient;
        private int failureTries;
        private DateTime lastFailureTime;
        private int absoluteFailures;

        /// <summary>
        /// Checks whether or not any clients are connected. This requires locking so don't use it frequently.
        /// </summary>
        public bool HasConnections
        {
            get
            {
                lock (clientMutex)
                {
                    return clients.Count > 0;
                }
            }
        }

        public OverlayServer(GameManager gameManager, IOverlayPacketManager packetManager)
        {
            this.server = new TcpListener(System.Net.IPAddress.Any, CommunicationPort);
            this.gameManager = gameManager;
            this.packetManager = packetManager;
        }


        public void Update()
        {
#if UNITY_STANDALONE_LINUX
            return;
#endif

            if (!started)
            {
                if (DateTime.UtcNow - lastStartTry >= TimeSpan.FromSeconds(StartRetryTimeSeconds))
                {
                    TryStart();
                }
                return;
            }

            if (this.writePackets.TryPeek(out var packet))
            {
                lock (clientMutex)
                {
                    if (clients.Count > 0)
                    {
                        foreach (var client in clients)
                        {
                            client.Send(packet);
                        }

                        writePackets.TryDequeue(out _);
                    }
                }
            }
        }

        private bool TryStart()
        {
            try
            {
                var elapsedSinceLastFailure = (DateTime.UtcNow - lastFailureTime);
                // only once every 3s, or if we have failed 5 times. We will wait a minute
                // before we try again.

                if (absoluteFailures >= 3)
                {
                    // we give up. Only way we can fix this is if the streamer restarts
                    // the game.
                    return false;
                }

                if (elapsedSinceLastFailure.TotalSeconds <= 3 || failureTries >= 5)
                {
                    if (elapsedSinceLastFailure.TotalSeconds >= 60)
                    {
                        failureTries = 0; // lets retry again.
                        absoluteFailures++;
                    }
                    return false;
                }

                if (isStarting)
                    return false;

                isStarting = true;
                lastStartTry = DateTime.UtcNow;
                this.server.Start(0x1000);
                this.AcceptClients();
                this.started = true;
                failureTries = 0;
                return true;
            }
            catch (Exception exc)
            {
#if DEBUG
                if (failureTries == 0)
                {
                    Shinobytes.Debug.LogError("Failed to start overlay server: " + exc);
                }
#endif
                failureTries++;
                lastFailureTime = DateTime.UtcNow;
            }
            finally
            {
                isStarting = false;
            }
            return false;
        }

        private void AcceptClients()
        {
            try
            {
                this.server.BeginAcceptTcpClient(OnClientAccepted, this.server);
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("Unable to accept new clients: " + exc);
                this.Dispose();
            }
        }

        private void OnClientAccepted(IAsyncResult ar)
        {
            try
            {
                if (disposed || server == null)
                    return;

                var client = server.EndAcceptTcpClient(ar);
                if (client != null)
                {
                    lock (clientMutex)
                    {
                        var overlayClient = new OverlayClient(this, packetManager, client);
                        this.lastConnectedClient = overlayClient;
                        this.clients.Add(overlayClient);
                        if (this.ClientConnected != null)
                        {
                            ClientConnected.Invoke(this, overlayClient);
                        }
                    }
                    Shinobytes.Debug.Log("Overlay client connected!");
                }
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("Unable to accept client: " + exc);
                this.Dispose();
                return;
            }
            AcceptClients();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            if (this.server != null)
            {
                this.server.Stop();
                this.server = null;
            }
        }

        internal void OnClientDisconnected(OverlayClient overlayClient)
        {
            lock (clientMutex) this.clients.Remove(overlayClient);
            Shinobytes.Debug.Log("Overlay client disconnected!");
        }
    }
}
