using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Assets.Scripts
{
    public class GameCache
    {
        private const string PlayerStateCacheFileName = "state-data.json";
        private const string TempPlayerStateCacheFileName = "tmp-state-data.json";
        //private static GameCache instance;
        //public static GameCache Instance => instance ?? (instance = new GameCache());
        //private readonly ConcurrentQueue<GameCacheState> stateCache = new ConcurrentQueue<GameCacheState>();

        private static readonly object mutex = new object();
        private static GameCacheState? stateCache;
        private static List<GameCachePlayerItem> playerCache;
        public static bool IsAwaitingGameRestore;
        public static readonly TwitchUserStore TwitchUserStore = new TwitchUserStore();

        internal static void SavePlayersState(IReadOnlyList<PlayerController> players)
        {
            SetPlayersState(players);
            BuildState();
            SaveState();
        }


        internal static void SetState(GameCacheState? cacheState)
        {
            stateCache = cacheState;
            if (stateCache != null)
            {
                stateCache = new GameCacheState
                {
                    Created = System.DateTime.UtcNow,
                    Players = cacheState.Value.Players
                };
            }
        }

        internal static void SetPlayersState(IReadOnlyList<PlayerController> players)
        {
            Shinobytes.Debug.Log("Updating Player State.");
            lock (mutex)
            {
                playerCache = new List<GameCachePlayerItem>();
                foreach (var player in players)
                {
                    try
                    {
                        if (player.IsBot)
                        {
                            Shinobytes.Debug.LogWarning(player.Name + " is a bot and was not added to player state cache.");
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
                        Shinobytes.Debug.LogError("Failed to add " + player?.Name + " to the player state cache. " + exc.Message);
                    }
                }
            }
        }

        internal static GameCacheState BuildState()
        {
            var state = new GameCacheState();
            state.Created = System.DateTime.UtcNow;
            state.Players = playerCache;
            //stateCache.Enqueue(state);
            stateCache = state;
            return state;
        }

        public enum LoadStateResult
        {
            Success,
            Error,
            Expired
        }

        internal static LoadStateResult LoadState(bool forceReload = false)
        {
            if (Shinobytes.IO.File.Exists(PlayerStateCacheFileName))
            {
                try
                {
                    var expiryTime = SettingsMenuView.GetPlayerCacheExpiryTime();
                    if (expiryTime == TimeSpan.Zero) return LoadStateResult.Success;
#if DEBUG
                    Shinobytes.Debug.Log("Loading state file: " + Shinobytes.IO.Path.GetFilePath(PlayerStateCacheFileName));
#endif
                    var state = Newtonsoft.Json.JsonConvert.DeserializeObject<GameCacheState>(Shinobytes.IO.File.ReadAllText(PlayerStateCacheFileName));

                    if (!forceReload && (System.DateTime.UtcNow - state.Created) > expiryTime)
                    {
                        Shinobytes.Debug.LogWarning("State Cache File has expired and will not be loaded.");
                        return LoadStateResult.Expired;
                    }

                    stateCache = state;

                    IsAwaitingGameRestore = true;

                    Shinobytes.Debug.Log("Loading Player State file...");
                }
                catch (System.Exception exc)
                {
                    Shinobytes.Debug.LogError("Failed to load player state: " + exc.Message);
                    return LoadStateResult.Error;
                }
            }

            return LoadStateResult.Success;
        }

        internal static void SaveState()
        {
            if (stateCache != null)
            {
                try
                {

                    Shinobytes.Debug.Log("Saving Player State file... (" + (stateCache?.Players?.Count ?? 0) + " players)");
                    var stateData = Newtonsoft.Json.JsonConvert.SerializeObject(stateCache);
                    // To ensure we dont accidently overwrite the state-data with half written data.
                    // in case the game crashes and saving in progress.
                    Shinobytes.IO.File.WriteAllText(TempPlayerStateCacheFileName, stateData);
                    Shinobytes.IO.File.Copy(TempPlayerStateCacheFileName, PlayerStateCacheFileName, true);
                    Shinobytes.IO.File.Delete(TempPlayerStateCacheFileName);
                }
                catch (System.Exception exc)
                {
                    Shinobytes.Debug.LogError("Failed to save player state: " + exc.Message);
                }
            }
        }

        internal static GameCacheState? GetReloadState()
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
