using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Assets.Scripts
{
    public class GameCache
    {
        private const string PlayerStateCacheFileName = "state-data.json";
        private const string TempPlayerStateCacheFileName = "tmp-state-data.json";
        private static GameCache instance;
        public static GameCache Instance => instance ?? (instance = new GameCache());
        //private readonly ConcurrentQueue<GameCacheState> stateCache = new ConcurrentQueue<GameCacheState>();

        private readonly object mutex = new object();

        private GameCacheState? stateCache;

        private List<GameCachePlayerItem> playerCache;
        public bool IsAwaitingGameRestore { get; set; }
        public TwitchUserStore TwitchUserStore { get; } = new TwitchUserStore();

        internal void SavePlayersState(IReadOnlyList<PlayerController> players)
        {
            SetPlayersState(players);
            BuildState();
            SaveState();
        }
        internal void SetPlayersState(IReadOnlyList<PlayerController> players)
        {
            GameManager.Log("Updating Player State.");
            lock (mutex)
            {
                playerCache = new List<GameCachePlayerItem>();
                foreach (var player in players)
                {
                    try
                    {
                        if (player.IsBot)
                        {
                            GameManager.LogWarning(player.Name + " is a bot and was not added to player state cache.");
                            continue;
                        }

                        var def = player.Definition;

                        var item = new GameCachePlayerItem();
                        item.NameTagHexColor = player.PlayerNameHexColor;
                        item.TwitchUser = player.TwitchUser;
                        item.CharacterId = player.Id;
                        item.CharacterIndex = player.CharacterIndex;
                        
                        if (item.TwitchUser == null)
                        {
                            item.TwitchUser = new TwitchPlayerInfo(
                                    def.UserId,
                                    def.UserName,
                                    def.Name,
                                    player.PlayerNameHexColor,
                                    player.IsBroadcaster,
                                    player.IsModerator,
                                    player.IsSubscriber,
                                    player.IsVip,
                                    def.Identifier
                                );
                        }
                        else
                        {
                            item.TwitchUser.IsVip = player.IsVip;
                            item.TwitchUser.IsModerator = player.IsModerator;
                            item.TwitchUser.IsBroadcaster = player.IsBroadcaster;
                            item.TwitchUser.IsSubscriber = player.IsSubscriber;
                        }

                        //item.Definition = player.Definition;
                        //item.Definition.Resources = player.Resources;
                        //item.Definition.Statistics = player.Statistics;
                        //item.Definition.Skills = player.Stats;
                        //item.Definition.InventoryItems = player.Inventory.GetInventoryItems();
                        playerCache.Add(item);
                    }
                    catch (System.Exception exc)
                    {
                        GameManager.LogError("Failed to add " + player?.Name + " to the player state cache. " + exc);
                    }
                }
            }
        }

        internal GameCacheState BuildState()
        {
            var state = new GameCacheState();
            state.Created = System.DateTime.UtcNow;
            state.Players = playerCache;
            //stateCache.Enqueue(state);
            stateCache = state;
            return state;
        }

        internal void LoadState()
        {
            if (System.IO.File.Exists(PlayerStateCacheFileName))
            {
                try
                {
                    var state = Newtonsoft.Json.JsonConvert.DeserializeObject<GameCacheState>(System.IO.File.ReadAllText(PlayerStateCacheFileName));

                    if ((System.DateTime.UtcNow - state.Created) > SettingsMenuView.GetPlayerCacheExpiryTime())
                    {
                        return;
                    }

                    stateCache = state;

                    IsAwaitingGameRestore = true;
                }
                catch (System.Exception exc)
                {
                    GameManager.LogError("Failed to load player state: " + exc);
                }
            }
        }

        internal void SaveState()
        {
            if (stateCache != null)
            {
                try
                {
                    var stateData = Newtonsoft.Json.JsonConvert.SerializeObject(stateCache);
                    // To ensure we dont accidently overwrite the state-data with half written data.
                    // in case the game crashes and saving in progress.
                    System.IO.File.WriteAllText(TempPlayerStateCacheFileName, stateData);
                    System.IO.File.Copy(TempPlayerStateCacheFileName, PlayerStateCacheFileName, true);
                    System.IO.File.Delete(TempPlayerStateCacheFileName);
                }
                catch (System.Exception exc)
                {
                    GameManager.LogError("Failed to save player state: " + exc);
                }
            }
        }

        internal GameCacheState? GetReloadState()
        {
            try
            {
                return stateCache;
            }
            finally
            {
                stateCache = null;
            }
        }
    }

    public struct GameCacheState
    {
        public System.DateTime Created { get; set; }
        public List<GameCachePlayerItem> Players { get; set; }
    }

    public class GameCachePlayerItem
    {
        public TwitchPlayerInfo TwitchUser { get; set; }
        public System.Guid CharacterId { get; set; }
        public string NameTagHexColor { get; set; }
        public int CharacterIndex { get; set; }
    }
}
