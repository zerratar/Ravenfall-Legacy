using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;

namespace Assets.Scripts
{
    public class GameCache
    {
        private static GameCache instance;
        public static GameCache Instance => instance ?? (instance = new GameCache());
        private readonly ConcurrentQueue<GameCacheState> stateCache = new ConcurrentQueue<GameCacheState>();
        private readonly object mutex = new object();

        private List<GameCachePlayerItem> playerCache;
        public bool IsAwaitingGameRestore => stateCache.Count > 0;
        internal void SetPlayersState(IReadOnlyList<PlayerController> players)
        {
            lock (mutex)
            {
                playerCache = new List<GameCachePlayerItem>();
                foreach (var player in players)
                {
                    var item = new GameCachePlayerItem();
                    item.Definition = player.Definition;
                    item.Definition.Resources = player.Resources;
                    item.Definition.Statistics = player.Statistics;
                    item.Definition.Skills = player.Stats;
                    item.Definition.InventoryItems = player.Inventory.GetInventoryItems();

                    item.Health = player.Stats.Health.CurrentValue;
                    item.Position = player.Transform.position;
                    item.TrainingTask = player.GetTask();
                    item.TrainingTaskArg = player.GetTaskArguments();

                    item.ResetPosition = player.Dungeon.InDungeon || player.Raid.InRaid || player.Ferry.OnFerry || player.StreamRaid.InWar;
                    item.Island = player.Island?.Identifier;

                    playerCache.Add(item);
                }
            }
        }

        internal void BuildState()
        {
            var state = new GameCacheState();
            state.Players = playerCache;
            stateCache.Enqueue(state);
        }

        internal GameCacheState GetReloadState()
        {
            if (stateCache.TryDequeue(out var res))
                return res;
            return null;
        }
    }

    public class GameCacheState
    {
        public List<GameCachePlayerItem> Players { get; set; }
    }

    public class GameCachePlayerItem
    {
        public Player StreamUser { get; set; }
        public RavenNest.Models.Player Definition { get; set; }
        public TaskType TrainingTask { get; set; }
        public string[] TrainingTaskArg { get; set; }
        public UnityEngine.Vector3 Position { get; set; }
        public int Health { get; set; }

        public bool ResetPosition { get; set; }
        public string Island { get; set; }
    }
}
