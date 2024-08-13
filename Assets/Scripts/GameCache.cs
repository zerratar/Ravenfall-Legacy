using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Assets.Scripts
{
    public class GameCache
    {
        private static readonly string[] PlayerStateCacheFileNames = {
            "state-data.json",
            "player-state.json",
            "players.json",
        };

        private static string DefaultPlayerStateCacheFileName = PlayerStateCacheFileNames[0];

        private const string TempPlayerStateCacheFileName = "tmp-state-data.json";
        //private static GameCache instance;
        //public static GameCache Instance => instance ?? (instance = new GameCache());
        //private readonly ConcurrentQueue<GameCacheState> stateCache = new ConcurrentQueue<GameCacheState>();

        private static readonly object mutex = new object();
        private static RestorableGameState? stateCache;
        private static List<RestorablePlayer> playerCache;
        public static bool IsAwaitingGameRestore;
        private static string usedPlayerStateCacheFileName;
        public static readonly TwitchUserStore TwitchUserStore = new TwitchUserStore();

        internal static void SavePlayersState(IReadOnlyList<PlayerController> players)
        {
            SetPlayersState(players);
            BuildState();
            SaveState();
        }

        internal static void SetState(RestorableGameState? cacheState)
        {
            stateCache = cacheState;
            if (stateCache != null)
            {
                stateCache = new RestorableGameState
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
                playerCache = new List<RestorablePlayer>();
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

                        var item = new RestorablePlayer();
                        item.NameTagHexColor = player.PlayerNameHexColor;
                        item.User = player.User;
                        item.CharacterId = player.Id;
                        item.CharacterIndex = player.CharacterIndex;
                        item.LastActivityUtc = player.LastChatCommandUtc;
                        if (item.User == null)
                        {
                            item.User = new User(
                                    player.UserId,
                                    player.Id,
                                    def.UserName,
                                    def.Name,
                                    player.PlayerNameHexColor,
                                    player.Platform,
                                    player.PlatformId,
                                    player.IsBroadcaster,
                                    player.IsModerator,
                                    player.IsSubscriber,
                                    player.IsVip,
                                    player.IsGameAdmin,
                                    player.IsGameModerator,
                                    def.Identifier
                                );
                        }
                        else
                        {
                            item.User.IsVip = player.IsVip;
                            item.User.IsModerator = player.IsModerator;
                            item.User.IsBroadcaster = player.IsBroadcaster;
                            item.User.IsSubscriber = player.IsSubscriber;
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

        internal static RestorableGameState BuildState()
        {
            var state = new RestorableGameState();
            state.Created = System.DateTime.UtcNow;
            state.Players = playerCache;
            //stateCache.Enqueue(state);
            stateCache = state;
            return state;
        }

        public enum LoadStateResult
        {
            NoPlayersRestored,
            PlayersRestored,
            Error,
            Expired
        }

        private static RestorableGameState GetLatestCache()
        {
            var state = new RestorableGameState();
            foreach (var file in PlayerStateCacheFileNames)
            {
                if (!Shinobytes.IO.File.Exists(file))
                {
                    continue;
                }

                var stateContent = Shinobytes.IO.File.ReadAllText(file);
                var readState = Newtonsoft.Json.JsonConvert.DeserializeObject<RestorableGameState>(stateContent);
                if (readState.Created > state.Created)
                {
                    state = readState;
                    usedPlayerStateCacheFileName = file;
                }
            }
            stateCache = state;
            return state;
        }



        internal static LoadStateResult LoadState(bool forceReload = false)
        {
            try
            {
                var expiryTime = SettingsMenuView.GetPlayerCacheExpiryTime();
                if (expiryTime == TimeSpan.Zero)
                {
                    return LoadStateResult.NoPlayersRestored;
                }

                var state = GetLatestCache();
                if (state.Players == null || state.Players.Count == 0)
                {
                    Shinobytes.Debug.Log("No player state file found or state file did not contain any players.");
                    return LoadStateResult.NoPlayersRestored;
                }

                if (!forceReload && (System.DateTime.UtcNow - state.Created) > expiryTime)
                {
                    Shinobytes.Debug.LogWarning("State Cache File has expired and will not be loaded.");
                    return LoadStateResult.Expired;
                }

                if (!forceReload && (System.DateTime.UtcNow - state.Created) > expiryTime)
                {
                    Shinobytes.Debug.LogWarning("State Cache File has expired and will not be loaded.");
                    return LoadStateResult.Expired;
                }
            }
            catch (System.Exception exc)
            {
                Shinobytes.Debug.LogError("Failed to load player state: " + exc.Message);
                return LoadStateResult.Error;
            }

            return LoadStateResult.PlayersRestored;
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
                    Shinobytes.IO.File.Copy(TempPlayerStateCacheFileName, usedPlayerStateCacheFileName, true);
                    Shinobytes.IO.File.Delete(TempPlayerStateCacheFileName);
                }
                catch (System.Exception exc)
                {
                    Shinobytes.Debug.LogError("Failed to save player state: " + exc.Message);
                }
            }
        }

        internal static RestorableGameState? GetReloadState()
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

    public struct RestorableGameState
    {
        public System.DateTime Created { get; set; }
        public List<RestorablePlayer> Players { get; set; }
    }

    public class RestorablePlayer
    {
        public User User { get; set; }
        public System.Guid CharacterId { get; set; }
        public string NameTagHexColor { get; set; }
        public int CharacterIndex { get; set; }
        public DateTime LastActivityUtc { get; set; }

        // potentially we can add skill experience and level here if we encrypt it with a session key that the server gives uniquely every login
        // then upon "restore", send the blob to the server to decrypt and apply changes to the characters before returning them back here.
        // on the server side it should only update characters that has not been updated within a reasonable time. 

        // or: what if we get a key with every session ping, only the latest key will be accepted.

    }
}
