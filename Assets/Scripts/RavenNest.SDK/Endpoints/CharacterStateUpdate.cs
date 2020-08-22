
using static RavenNest.SDK.Endpoints.WebSocketEndpoint;

namespace RavenNest.SDK.Endpoints
{

    public class CharacterSkillUpdate
    {
        public string UserId { get; set; }
        public decimal[] Experience { get; set; }
    }

    public class CharacterStateUpdate
    {
        public CharacterStateUpdate(
            string userId,
            int health,
            string island,
            string duelOpponent,
            bool inRaid,
            bool inArena,
            string task,
            string taskArgument,
            float x, float y, float z
            )
        {
            UserId = userId;
            Health = health;
            Island = island;
            DuelOpponent = duelOpponent;
            InRaid = inRaid;
            InArena = inArena;
            Task = task;
            TaskArgument = taskArgument;
            X = x;
            Y = y;
            Z = z;
        }
        public string UserId { get; set; }
        public int Health { get; set; }
        public string Island { get; set; }
        public string DuelOpponent { get; set; }
        public bool InRaid { get; set; }
        public bool InArena { get; set; }
        public string Task { get; set; }
        public string TaskArgument { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}