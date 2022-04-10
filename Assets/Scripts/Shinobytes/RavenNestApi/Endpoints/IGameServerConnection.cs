using System;
using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    internal interface IGameServerConnection
    {
        event EventHandler OnReconnected;
        Task<GamePacket> SendAsync(PlayerGamePacketRef packet);
        Task<GamePacket> SendAsync(Guid sender, string id, object model);
        void SendNoAwait(Guid sender, string id, object model, string type);
        void SendNoAwait(PlayerGamePacketRef packet);

        void Register<TPacketHandler>(string packetId, TPacketHandler packetHandler)
            where TPacketHandler : GamePacketHandler;

        bool IsReady { get; }
        bool ReconnectRequired { get; }

        Task<bool> CreateAsync();
        void Close();
        void Reconnect();
    }
}