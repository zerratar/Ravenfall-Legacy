using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    public abstract class GamePacketHandler
    {
        protected readonly GameManager GameManager;

        protected GamePacketHandler(GameManager gameManager)
        {
            GameManager = gameManager;
        }
        public abstract Task HandleAsync(GamePacket packet);
    }
}