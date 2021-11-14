using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace RavenNest.SDK.Endpoints
{
    public class WebSocketEndpoint : IWebSocketEndpoint
    {
        private ConcurrentDictionary<string, CharacterStateUpdate> lastSavedState
            = new ConcurrentDictionary<string, CharacterStateUpdate>();

        private ConcurrentDictionary<string, CharacterSkillUpdate> lastSavedSkills
            = new ConcurrentDictionary<string, CharacterSkillUpdate>();

        private ConcurrentDictionary<Guid, CharacterExpUpdate> lastSavedExp
            = new ConcurrentDictionary<Guid, CharacterExpUpdate>();

        private ConcurrentDictionary<string, DateTime> lastSavedStateTime
            = new ConcurrentDictionary<string, DateTime>();
        private ConcurrentDictionary<string, DateTime> lastSavedSkillsTime
            = new ConcurrentDictionary<string, DateTime>();

        private const double ForceSaveInterval = 5d;

        private readonly IGameServerConnection connection;
        private readonly IRavenNestClient client;
        private readonly GameManager gameManager;
        public bool ForceReconnecting { get; private set; }
        public bool IsReady => connection?.IsReady ?? false;
        public WebSocketEndpoint(
            IRavenNestClient client,
            GameManager gameManager,
            ILogger logger,
            IAppSettings settings,
            ITokenProvider tokenProvider,
            IGamePacketSerializer packetSerializer)
        {
            connection = new WSGameServerConnection(
                logger,
                settings,
                tokenProvider,
                packetSerializer,
                gameManager);

            connection.Register("game_event", new GameEventPacketHandler(gameManager));
            connection.OnReconnected += OnReconnected;
            this.client = client;
            this.gameManager = gameManager;
        }

        private void OnReconnected(object sender, EventArgs e)
        {
            ForceReconnecting = false;

            try
            {
                connection.SendNoAwait("sync_client", new ClientSyncUpdate { ClientVersion = Application.version });
            }
            catch
            {
                // ignore
            }
        }

        public async Task<bool> UpdateAsync()
        {
            if (client.Desynchronized)
            {
                if (connection.IsReady)
                {
                    connection.Close();
                }

                return false;
            }
            if (connection.IsReady)
            {
                return true;
            }

            if (connection.ReconnectRequired)
            {
                await Task.Delay(2000);
            }

            return await connection.CreateAsync();
        }

        public Task UpdatePlayerEventStatsAsync(EventTriggerSystem.SysEventStats e)
        {
            if (client.Desynchronized) return Task.CompletedTask;
            try
            {
                var player = this.gameManager.Players.GetPlayerByUserId(e.Source);
                if (!player) return Task.CompletedTask;
                var avgSecPerTrigger = e.TotalTriggerTime.TotalSeconds / e.TotalTriggerCount;
                var maxResponse = e.TriggerRangeMax.Select(x => x.Value).OrderByDescending(x => x).FirstOrDefault();
                var minResponse = e.TriggerRangeMin.Select(x => x.Value).OrderBy(x => x).FirstOrDefault();
                var data = new PlayerSessionActivity
                {
                    UserId = e.Source,
                    CharacterId = player.Id,
                    AvgResponseTime = TimeSpan.FromSeconds(avgSecPerTrigger),
                    MaxResponseTime = TimeSpan.FromSeconds(maxResponse),
                    MinResponseTime = TimeSpan.FromSeconds(minResponse),
                    MaxResponseStreak = (int)e.HighestTriggerStreak,
                    ResponseStreak = (int)e.TriggerStreak,
                    TotalInputCount = (int)e.InputCount.Sum(x => x.Value),
                    TotalTriggerCount = (int)e.TriggerCount.Sum(x => x.Value),
                    TripCount = (int)e.InspectCount,
                    Tripped = e.Flagged,
                    UserName = player.TwitchUser.Username
                };
                connection.SendNoAwait("update_user_session_stats", data);
                return Task.CompletedTask;
            }
            catch
            {
                return Task.CompletedTask;
            }
        }

        public async Task<bool> SaveActiveSkillAsync(PlayerController player)
        {
            if (player.IsBot && player.UserId.StartsWith("#"))
            {
                return true;
            }

            return await Task.Run(() =>
            {
                try
                {

                    // TODO(zerratar): since this will be invoked much more frequently than the
                    //                 save all stats; plus this one blocks that from being called
                    //                 there is a slight risk you will never sync "all" stats for this player
                    //  Option 1. We save ALL skills that has been modified here.
                    //  Option 2. We change how and when these functions are called by enqueueing them instead.
                    //      They will then be dequeued every 0.5s or perhaps even more frequently. Then Every
                    //      10th-tick or enqueued item should be "Save All" and every enqueued item other than that one
                    //      is for saving the active one.

                    var activeSkill = player.GetActiveSkillStat();
                    if (activeSkill == null)
                    {
                        return true;
                    }

                    var characterUpdate = new CharacterExpUpdate
                    {
                        SkillIndex = Skills.IndexOf(player.Stats, activeSkill),
                        Level = activeSkill.Level,
                        Experience = activeSkill.Experience,
                        CharacterId = player.Id
                    };

                    if (lastSavedExp.TryGetValue(player.Id, out var lastUpdate))
                    {
                        if (!RequiresUpdate(lastUpdate, characterUpdate))
                        {
                            return true; // return true so we dont get a red name in the player list just because the exp hasnt changed.
                        }
                    }

                    connection.SendNoAwait("update_character_exp", characterUpdate);
                    lastSavedExp[player.Id] = characterUpdate;
                    return true;
                }
                catch (Exception exc)
                {
                    Shinobytes.Debug.LogError("Saving " + player?.Name + " failed. " + exc.Message);
                    return false;
                }
            });

        }
        public async Task<bool> SavePlayerSkillsAsync(PlayerController player)
        {
            if (player.IsBot && player.UserId.StartsWith("#"))
            {
                return true;
            }

            var state = player.BuildPlayerState();
            if (state == null || string.IsNullOrEmpty(state.UserId))
            {
                return false;
            }

            return await Task.Run(() =>
            {
                try
                {
                    //if (client.Desynchronized) return false;
                    var characterUpdate = new CharacterSkillUpdate
                    {
                        Level = state.Level,
                        Experience = state.Experience,
                        UserId = state.UserId,
                        CharacterId = state.CharacterId
                    };

                    if (lastSavedSkills.TryGetValue(player.UserId, out var lastUpdate))
                    {
                        if (!RequiresUpdate(lastUpdate, characterUpdate))
                        {
                            //Shinobytes.Debug.LogWarning("Saving " + player?.Name + " skipped. No changes to stats.");
                            return true; // return true so we dont get a red name in the player list just because the exp hasnt changed.
                        }
                    }

                    connection.SendNoAwait("update_character_skills", characterUpdate);
                    //if (response != null && response.TryGetValue<bool>(out var result) && result)
                    //{
                    //    lastSavedSkills[player.UserId] = characterUpdate;
                    //    lastSavedSkillsTime[player.UserId] = DateTime.UtcNow;
                    //    return true;
                    //}
                    lastSavedSkills[player.UserId] = characterUpdate;
                    lastSavedSkillsTime[player.UserId] = DateTime.UtcNow;
                    return true;
                }
                catch (Exception exc)
                {
                    Shinobytes.Debug.LogError("Saving " + player?.Name + " failed. " + exc.Message);
                    return false;
                }
            });
        }

        public void SendPlayerLoyaltyData(TwitchCheer d)
        {
            if (client.Desynchronized) return;
            var data = new UserLoyaltyUpdate
            {
                IsModerator = d.IsModerator,
                IsSubscriber = d.IsSubscriber,
                NewCheeredBits = d.Bits,
                UserName = d.UserName,
                IsVip = d.IsVip,
                NewGiftedSubs = 0,
                UserId = d.UserId
            };

            connection.SendNoAwait("update_user_loyalty", data);
        }

        public void SendPlayerLoyaltyData(TwitchSubscription d)
        {
            if (client.Desynchronized) return;
            var data = new UserLoyaltyUpdate
            {
                IsModerator = d.IsModerator,
                IsSubscriber = d.IsSubscriber,
                NewCheeredBits = 0,
                UserName = d.UserName,
                NewGiftedSubs = d.ReceiverUserId == null || d.ReceiverUserId == d.UserId ? 0 : 1,
                UserId = d.UserId
            };

            connection.SendNoAwait("update_user_loyalty", data);
        }

        public void SendPlayerLoyaltyData(PlayerController player)
        {
            if (client.Desynchronized) return;
            if (player == null || !player)
            {
                return;
            }

            if (player.IsBot && player.UserId.StartsWith("#"))
            {
                return;
            }

            var data = new UserLoyaltyUpdate
            {
                CharacterId = player.Id,
                IsModerator = player.IsModerator,
                IsSubscriber = player.IsSubscriber,
                IsVip = player.IsVip,
                NewCheeredBits = player.BitsCheered,
                NewGiftedSubs = player.GiftedSubs,
                UserId = player.UserId
            };

            player.GiftedSubs = 0;
            player.BitsCheered = 0;

            connection.SendNoAwait("update_user_loyalty", data);
        }

        public void SyncTimeAsync(TimeSpan delta, DateTime time, DateTime serverTime)
        {
            connection.SendNoAwait("sync_time", new TimeSyncUpdate { Delta = delta, LocalTime = time, ServerTime = serverTime });
        }

        public async Task<bool> SavePlayerStateAsync(PlayerController player)
        {
            if (client.Desynchronized) return false;
            if (player == null || !player || string.IsNullOrEmpty(player.UserId))
                return false;

            if (player.IsBot && player.UserId.StartsWith("#"))
            {
                return true;
            }

            var pos = player.transform.position;
            var island = player.Island?.Identifier ?? "";

            if (player.Raid.InRaid)
            {
                pos = player.Raid.PreviousPosition;
                if (player.Raid.PreviousIsland != null)
                {
                    island = player.Raid.PreviousIsland?.Identifier ?? "";
                }
            }

            if (player.Dungeon.InDungeon)
            {
                pos = player.Dungeon.PreviousPosition;
                if (player.Dungeon.PreviousIsland != null)
                {
                    island = player.Dungeon.PreviousIsland?.Identifier ?? "";
                }
            }

            return await Task.Run(() =>
            {
                var characterUpdate = new CharacterStateUpdate(
                    player.UserId,
                    player.Id,
                    player.Stats.Health.CurrentValue,
                    island,
                    player.Duel.InDuel ? player.Duel.Opponent?.UserId ?? "" : "",
                    player.Raid.InRaid,
                    player.Arena.InArena,
                    player.Dungeon.InDungeon,
                    player.Onsen.InOnsen,
                    player.GetTask().ToString(),
                    string.Join(",", player.GetTaskArguments()),
                    pos.x,
                    pos.y,
                    pos.z);

                if (lastSavedState.TryGetValue(player.UserId, out var lastUpdate))
                {
                    if (!RequiresUpdate(lastUpdate, characterUpdate))
                    {
                        return false;
                    }
                }

                connection.SendNoAwait("update_character_state", characterUpdate);
                lastSavedState[player.UserId] = characterUpdate;
                lastSavedStateTime[player.UserId] = DateTime.UtcNow;
                return true;
            });
        }

        private bool RequiresUpdate(CharacterExpUpdate a, CharacterExpUpdate b)
        {
            return b.Level > a.Level || b.Experience > a.Experience;
        }

        private bool RequiresUpdate(CharacterStateUpdate oldState, CharacterStateUpdate newState)
        {
            if (!lastSavedStateTime.TryGetValue(oldState.UserId, out var date) ||
                DateTime.UtcNow - date > TimeSpan.FromSeconds(ForceSaveInterval))
                return true;

            if (oldState.Health != newState.Health) return true;
            if (oldState.InArena != newState.InArena) return true;
            if (oldState.InRaid != newState.InRaid) return true;
            if (oldState.Island != newState.Island) return true;
            if (oldState.Task != newState.Task) return true;
            if (oldState.TaskArgument != newState.TaskArgument) return true;
            return oldState.DuelOpponent != newState.DuelOpponent;
        }

        private bool RequiresUpdate(CharacterSkillUpdate oldState, CharacterSkillUpdate newState)
        {
            if (!lastSavedSkillsTime.TryGetValue(oldState.UserId, out var date))
                return true;

            if (DateTime.UtcNow - date < TimeSpan.FromSeconds(ForceSaveInterval))
                return false; // don't save yet or we will be saving on each update.

            for (var i = 0; i < oldState.Experience.Length; ++i)
            {
                var oldExp = oldState.Experience[i];
                var newExp = newState.Experience[i];
                if (oldExp != newExp) return true;
            }

            return false;
        }

        public void Close()
        {
            connection.Close();
        }

        public void Reconnect()
        {
            if (client.Desynchronized) return;
            ForceReconnecting = true;
            connection.Reconnect();
        }
        public class Position
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }
    }
}