using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace RavenNest.SDK.Endpoints
{
    public class Update<T>
    {
        public T Data;
        public DateTime Updated;
        public Update(T data)
        {
            Data = data;
            Updated = DateTime.UtcNow;
        }
    }

    public class Update<T, T2>
    {
        public T UpdateData;
        public T2 KnowledgeBase;
        public DateTime Updated;
        public Update(T data, T2 knowledgeBase)
        {
            UpdateData = data;

            // We have to make a copy of the knowledge base
            // otherwise we cannot ensure its integrity
            // I really don't like this but its a sacrifice I'm willing to make.

            KnowledgeBase = JsonConvert.DeserializeObject<T2>(JsonConvert.SerializeObject(knowledgeBase));
            Updated = DateTime.UtcNow;
        }
    }

    public class WebSocketApi
    {
        private Dictionary<Guid, Update<CharacterStateUpdate>> lastSavedState = new Dictionary<Guid, Update<CharacterStateUpdate>>();
        private Dictionary<Guid, Update<CharacterSkillUpdate>> lastSavedSkills = new Dictionary<Guid, Update<CharacterSkillUpdate>>();
        private Dictionary<Guid, Update<CharacterExpUpdate>> lastSavedExp = new Dictionary<Guid, Update<CharacterExpUpdate>>();

        private readonly IGameServerConnection connection;
        private readonly RavenNestClient client;
        private readonly GameManager gameManager;
        public bool ForceReconnecting { get; private set; }
        public bool IsReady => connection?.IsReady ?? false;
        public WebSocketApi(
            RavenNestClient client,
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
                connection.SendNoAwait(Guid.Empty, "sync_client", new ClientSyncUpdate { ClientVersion = Ravenfall.Version }, nameof(ClientSyncUpdate));
            }
            catch
            {
                // ignore
            }
        }

        public async Task<bool> UpdateAsync()
        {
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
            try
            {
                if (e.TotalTriggerCount <= 1)
                {
                    return Task.CompletedTask;
                }

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
                connection.SendNoAwait(player.Id, "update_user_session_stats", data, nameof(PlayerSessionActivity));
                return Task.CompletedTask;
            }
            catch
            {
                return Task.CompletedTask;
            }
        }

        public bool SaveActiveSkill(PlayerController player)
        {
            if (player.IsBot || player.UserId.StartsWith("#"))
            {
                return true;
            }

            try
            {
                var activeSkill = player.GetActiveSkillStat();
                if (activeSkill == null)
                {
                    return true;
                }

                var characterUpdate = new Update<CharacterExpUpdate>(new CharacterExpUpdate
                {
                    SkillIndex = Skills.IndexOf(player.Stats, activeSkill), // (int)activeSkill.Type
                    Level = activeSkill.Level,
                    Experience = activeSkill.Experience,
                    CharacterId = player.Id
                });

                if (lastSavedExp.TryGetValue(player.Id, out var lastUpdate))
                {
                    if (!RequiresUpdate(lastUpdate, characterUpdate))
                    {
                        return true;
                    }
                }

                connection.SendNoAwait(player.Id, "update_character_exp", characterUpdate.Data, nameof(CharacterExpUpdate));
                lastSavedExp[player.Id] = characterUpdate;
                return true;
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("Saving " + player?.Name + " failed. " + exc.Message);
                return false;
            }
        }
        public bool SavePlayerSkills(PlayerController player)
        {
            if (player == null || string.IsNullOrEmpty(player.UserId))
            {
                return false;
            }

            if (player.IsBot || player.UserId.StartsWith("#"))
            {
                return true;
            }

            var state = player.BuildPlayerState();
            if (state == null || string.IsNullOrEmpty(state.UserId))
            {
                return false;
            }
            try
            {
                //if (client.Desynchronized) return false;
                var characterUpdate = new Update<CharacterSkillUpdate>(new CharacterSkillUpdate
                {
                    Level = state.Level,
                    Experience = state.Experience,
                    UserId = state.UserId,
                    CharacterId = state.CharacterId
                });

                if (lastSavedSkills.TryGetValue(player.Id, out var lastUpdate))
                {
                    if (!RequiresUpdate(lastUpdate, characterUpdate))
                    {
                        return true;
                    }
                }

                connection.SendNoAwait(player.Id, "update_character_skills", characterUpdate.Data, nameof(CharacterSkillUpdate));
                lastSavedSkills[player.Id] = characterUpdate;
                return true;
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("Saving " + player?.Name + " failed. " + exc.Message);
                return false;
            }
        }

        public void SyncTimeAsync(TimeSpan delta, DateTime time, DateTime serverTime)
        {
            connection.SendNoAwait(Guid.Empty, "sync_time", new TimeSyncUpdate { Delta = delta, LocalTime = time, ServerTime = serverTime }, nameof(TimeSyncUpdate));
        }

        public bool SavePlayerState(PlayerController player)
        {
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

            if (player.Ferry.OnFerry)
            {
                island = null;
            }

            var characterUpdate = new Update<CharacterStateUpdate>(
               new CharacterStateUpdate(
                           player.UserId,
                           player.Id,
                           player.Stats.Health.CurrentValue,
                           island,
                           player.Duel.InDuel ? player.Duel.Opponent?.UserId ?? "" : "",
                           player.Raid.InRaid,
                           player.Arena.InArena,
                           player.Dungeon.InDungeon,
                           player.Onsen.InOnsen && !player.InCombat && !player.Ferry.OnFerry,
                           player.CurrentTaskName,
                           player.taskArgument,//string.Join(",", player.GetTaskArguments()),
                           pos.x,
                           pos.y,
                           pos.z)
            );

            if (lastSavedState.TryGetValue(player.Id, out var lastUpdate))
            {
                if (!RequiresUpdate(lastUpdate, characterUpdate))
                {
                    return true;
                }
            }

            connection.SendNoAwait(player.Id, "update_character_state", characterUpdate.Data, nameof(CharacterStateUpdate));
            lastSavedState[player.Id] = characterUpdate;
            return true;
        }

        private bool RequiresUpdate(Update<CharacterExpUpdate> v0, Update<CharacterExpUpdate> v1)
        {
            var a = v0.Data;
            var b = v1.Data;

            return (v1.Updated - v0.Updated) >= TimeSpan.FromSeconds(5) || b.Level > a.Level || b.Experience > a.Experience || a.SkillIndex != b.SkillIndex;
        }

        private bool RequiresUpdate(Update<CharacterStateUpdate> v0, Update<CharacterStateUpdate> v1)
        {
            var oldState = v0.Data;
            var newState = v1.Data;
            if (oldState.Health != newState.Health) return true;
            if (oldState.InArena != newState.InArena) return true;
            if (oldState.InRaid != newState.InRaid) return true;
            if (oldState.Island != newState.Island) return true;
            if (oldState.InOnsen != newState.InOnsen) return true;
            if (NotEquals(oldState.Task, newState.Task)) return true;
            if (NotEquals(oldState.TaskArgument, newState.TaskArgument)) return true;
            if (NotEquals(oldState.DuelOpponent, newState.DuelOpponent)) return true;
            if (v1.Updated - v0.Updated >= TimeSpan.FromSeconds(5)) return true;
            return false;
            //return !lastSavedStateTime.TryGetValue(oldState.CharacterId, out var date) || DateTime.UtcNow - date >= TimeSpan.FromSeconds(ForceSaveInterval);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equals(string a, string b)
        {
            return string.CompareOrdinal(a, b) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NotEquals(string a, string b)
        {
            return string.CompareOrdinal(a, b) != 0;
        }
        private bool RequiresUpdate(Update<CharacterSkillUpdate> v0, Update<CharacterSkillUpdate> v1)
        {
            var oldState = v0.Data;
            var newState = v1.Data;
            var osExp = oldState.Experience;
            var nsExp = newState.Experience;
            for (var i = 0; i < osExp.Length; ++i)
            {
                if (osExp[i] != nsExp[i]) return true;
            }

            //return !lastSavedSkillsTime.TryGetValue(oldState.CharacterId, out var date) || DateTime.UtcNow - date >= TimeSpan.FromSeconds(ForceSaveInterval);
            return true;
        }

        public void Close()
        {
            connection.Close();
        }

        public void Reconnect()
        {
            ForceReconnecting = true;
            connection.Reconnect();
        }
    }
}