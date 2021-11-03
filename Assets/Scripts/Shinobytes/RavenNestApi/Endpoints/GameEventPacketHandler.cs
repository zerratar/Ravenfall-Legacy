using RavenNest.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    public class GameEventPacketHandler : GamePacketHandler
    {
        public GameEventPacketHandler(GameManager gameManager)
            : base(gameManager)
        {
        }

        public override Task HandleAsync(GamePacket packet)
        {
            //UnityEngine.Debug.Log("Got game event packet with id: " + packet.Id);
            if (packet.TryGetValue<EventList>(out var gameEvents))
            {
                //UnityEngine.Debug.Log("==== " + gameEvents.Events.Count + " events received");
                GameManager.HandleGameEvents(gameEvents);
            }
            return Task.CompletedTask;
        }
    }
}