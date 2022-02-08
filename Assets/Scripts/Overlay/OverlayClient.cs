using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.Scripts.Overlay
{
    public class OverlayClient : OverlayCommunication, IDisposable
    {
        private TcpClient client;
        private bool disposed;
        private bool isConnecting;
        private bool streamsInitialized;
        private BinaryReader readStream;
        private BinaryWriter writeStream;

        private Thread readThread;
        private Thread writeThread;

        private DateTime lastConnectTry;

        private const double ReconnectTimeSeconds = 3;
        private readonly OverlayServer server;
        private readonly IOverlayPacketManager packetManager;

        public bool HasReceivedItems;
        public bool IsDisposed => disposed;
        public bool IsConnected => !disposed && client != null && client.Connected;
        public OverlayClient(IOverlayPacketManager packetManager)
        {
            this.client = new TcpClient();
            this.packetManager = packetManager;
        }

        public OverlayClient(OverlayServer server, IOverlayPacketManager packetManager, TcpClient client)
        {
            this.client = client;
            this.server = server;
            this.packetManager = packetManager;
            if (this.client != null && this.client.Connected)
            {
                this.SetupStreams();
            }
        }

        public async void UpdateAsync()
        {
            if (this.client != null && !this.client.Connected && !isConnecting)
            {
                if (DateTime.UtcNow - lastConnectTry >= TimeSpan.FromSeconds(ReconnectTimeSeconds))
                {
                    await TryConnectAsync();
                }
                return;
            }

            if (readPackets.TryDequeue(out var packet))
            {
                if (!packetManager.TryHandle(packet))
                {
                    Shinobytes.Debug.LogError("Unhandled packet received, name: " + packet.Name);
                    return;
                }
            }
        }

        private async Task<bool> TryConnectAsync()
        {
            if (this.client.Connected)
            {
                this.SetupStreams();
                return true;
            }

            lastConnectTry = System.DateTime.UtcNow;

            try
            {
                isConnecting = true;
                await this.client.ConnectAsync(System.Net.IPAddress.Loopback, CommunicationPort);
                this.SetupStreams();
                Shinobytes.Debug.Log("Overlay connected to game!");
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogWarning("Unable to connect to game: " + exc + ", retrying in " + ReconnectTimeSeconds + " seconds.");
                return false;
            }
            finally
            {
                isConnecting = false;
            }

            return this.client != null && this.client.Connected;
        }

        internal static async Task<bool> TestServerAvailabilityAsync()
        {
            // this will create a dummy client, connect to the server and then disconnnect.
            // if the connection was OK. then we return True.
            try
            {
                using (var client = new TcpClient())
                {
                    client.ReceiveTimeout = 2000;
                    client.SendTimeout = 2000;
                    await client.ConnectAsync(System.Net.IPAddress.Loopback, CommunicationPort);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void SetupStreams()
        {
            if (this.streamsInitialized)
            {
                return;
            }

            var networkStream = this.client.GetStream();
            this.readStream = new System.IO.BinaryReader(networkStream);
            this.writeStream = new System.IO.BinaryWriter(networkStream);

            (this.readThread = new System.Threading.Thread(ReadProcess)).Start();
            (this.writeThread = new System.Threading.Thread(WriteProcess)).Start();

            this.streamsInitialized = true;
        }

        private void WriteProcess(object obj)
        {
            while (!disposed)
            {
                if (writePackets.TryDequeue(out var packet))
                {
                    this.writeStream.Write(packet.Name);
                    this.writeStream.Write(packet.Data ?? "{}");
                    this.writeStream.Flush();
                }
                else
                {
                    System.Threading.Thread.Sleep(10);
                }
            }
        }

        private void ReadProcess(object obj)
        {
            while (!disposed)
            {
                try
                {
                    var name = this.readStream.ReadString();
                    var data = this.readStream.ReadString();
                    this.readPackets.Enqueue(OverlayPacket.FromJson(name, data));
                }
                catch
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            if (server != null)
            {
                server.OnClientDisconnected(this);
            }

            if (this.readThread != null)
            {
                this.readThread = null;
            }

            if (this.writeThread != null)
            {
                this.writeThread = null;
            }

            if (this.readStream != null)
            {
                this.readStream.Dispose();
            }

            if (this.writeStream != null)
            {
                this.writeStream.Dispose();
            }

            if (this.client != null)
            {
                this.client.Close();
                this.client = null;
            }
        }
    }
}
