using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    internal interface IGameServerConnection
    {
        Task<GamePacket> SendAsync(GamePacket packet);
        Task<GamePacket> SendAsync(string id, object model);

        void Register<TPacketHandler>(string packetId, TPacketHandler packetHandler)
            where TPacketHandler : GamePacketHandler;

        bool IsReady { get; }
        bool ReconnectRequired { get; }

        Task<bool> CreateAsync();
        void Close();
    }
}