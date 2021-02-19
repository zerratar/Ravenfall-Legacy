
using System;
using static RavenNest.SDK.Endpoints.WebSocketEndpoint;

namespace RavenNest.SDK.Endpoints
{

    public class CharacterSkillUpdate
    {
        public Guid CharacterId { get; set; }
        public string UserId { get; set; }
        public decimal[] Experience { get; set; }
        public int[] Level { get; set; }
    }

    public class UserLoyaltyUpdate
    {
        public Guid CharacterId { get; set; }
        public string UserId { get; set; }
        public bool IsModerator { get; set; }
        public bool IsSubscriber { get; set; }
        public bool IsVip { get; set; }
        public int NewGiftedSubs { get; set; }
        public int NewCheeredBits { get; set; }
        public string UserName { get; set; }
    }

    public class TimeSyncUpdate
    {
        public TimeSpan Delta { get; set; }
        public DateTime LocalTime { get; set; }
        public DateTime ServerTime { get; set; }
    }

    public class CharacterStateUpdate
    {
        public CharacterStateUpdate(
            string userId,
            Guid characterId,
            int health,
            string island,
            string duelOpponent,
            bool inRaid,
            bool inArena,
            bool inDungeon,
            string task,
            string taskArgument,
            float x, float y, float z
            )
        {
            UserId = userId;
            CharacterId = characterId;
            Health = health;
            Island = island;
            DuelOpponent = duelOpponent;
            InRaid = inRaid;
            InArena = inArena;
            InDungeon = inDungeon;
            Task = task;
            TaskArgument = taskArgument;
            X = x;
            Y = y;
            Z = z;
        }
        public Guid CharacterId { get; set; }
        public string UserId { get; set; }
        public int Health { get; set; }
        public string Island { get; set; }
        public string DuelOpponent { get; set; }
        public bool InRaid { get; set; }
        public bool InArena { get; set; }
        public bool InDungeon { get; set; }
        public string Task { get; set; }
        public string TaskArgument { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}