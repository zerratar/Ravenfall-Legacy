using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Text;
using MessagePack;
using RavenNest.Models.TcpApi;
using RavenNest.SDK.Endpoints;
using System.Threading;
using Shinobytes.Linq;
using System.Numerics;

namespace RavenNest.SDK
{
    public class TcpApi : IDisposable
    {
        public const int MaxMessageSize = 1_048_576; // 1024 * 1024
        public const int ServerPort = 3920;
        public const int MinDelayBetweenSaveSeconds = 2;
        private readonly GameManager gameManager;
        private readonly string server;
        private readonly ITokenProvider tokenProvider;
        private readonly Thread thread;
        private DateTime lastConnectionTry;
        private Telepathy.Client client;
        private bool connecting;
        private string sessionToken;
        private bool disposed = false;
        private Dictionary<Guid, Update<CharacterUpdate, Skills>> lastSaved
            = new Dictionary<Guid, Update<CharacterUpdate, Skills>>();

        public bool Enabled = true;

        public bool Connected => client?.Connected ?? false;
        public bool IsReady => Connected && Enabled;

        public TcpApi(
            GameManager gameManager,
            string tcpApiEndpoint,
            ITokenProvider tokenProvider)
        {
            this.gameManager = gameManager;
            this.server = tcpApiEndpoint ?? "127.0.0.1";
            this.tokenProvider = tokenProvider;
            this.thread = new System.Threading.Thread(Update);
            this.thread.Start();
        }

        public void Connect()
        {
            if (connecting)
                return;

            lastConnectionTry = DateTime.UtcNow;
            client = new Telepathy.Client(MaxMessageSize);
            client.OnConnected = OnClientConnected;
            client.OnDisconnected = OnClientDisconnected;
            client.OnData = (data) => OnData(data);
            client.Connect(server, ServerPort);
        }

        public void Dispose()
        {
            Disconnect();
            client = null;
            disposed = true;
        }

        public void Disconnect()
        {
            try
            {
                if (client != null && client.Connected)
                {
                    client.Disconnect();
                }
            }
            catch { }
        }

        private void Update()
        {
            while (!disposed)
            {
                try
                {
                    var leftToProcess = 0;
                    // mostly for debugging, disabling the Tcp Api will allow the WebSocket Api to be used for saving players
                    // so this is for testing websocket that it works as expected.
                    if (!Enabled)
                    {
                        if (Connected)
                        {
                            Disconnect();
                        }

                        Thread.Sleep(1000);
                        continue;
                    }

                    if (client != null && Connected)
                    {
                        leftToProcess = client.Tick(1000);
                    }

                    // no need to even try to connect if we don't have a session token yet.

                    if (tokenProvider.HasSessionToken && !Connected && (DateTime.UtcNow - lastConnectionTry) >= TimeSpan.FromSeconds(5))
                    {
                        Connect();
                    }

                    if (!Connected)
                    {
                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }

                    if (leftToProcess == 0)
                        System.Threading.Thread.Sleep(16);
                }
                catch
                {
                    // ignored
                }
            }
        }

        public bool Send(object data)
        {
            var packetData = MessagePackSerializer.Serialize(data, MessagePack.Resolvers.ContractlessStandardResolver.Options);
            return client.Send(packetData);
        }

        private void OnData(ReadOnlyMemory<byte> obj)
        {
            if (!obj.IsEmpty)
            {
                connecting = false;

                try
                {
                    var eventList = MessagePackSerializer.Deserialize<Models.EventList>(obj, MessagePack.Resolvers.ContractlessStandardResolver.Options);
                    gameManager.HandleGameEvents(eventList);
                }
                catch (Exception exc)
                {
                    Shinobytes.Debug.LogError("Failed to deserialize and handle event list: " + exc.ToString());
                }
            }
        }

        private void OnClientDisconnected()
        {
            connecting = false;
        }

        private void OnClientConnected()
        {
            connecting = false;

            // as soon as we are connected. We push our session token to the server
            // or the server will disconnect us if we try to send any other data.
            // auth requests will be obsolete, as we will start having to send session token with every request to ensure its not lost.
            this.sessionToken = Base64Encode(Newtonsoft.Json.JsonConvert.SerializeObject(tokenProvider.GetSessionToken()));

            var packetData = MessagePackSerializer.Serialize(new AuthenticationRequest()
            {
                SessionToken = sessionToken
            }, MessagePack.Resolvers.ContractlessStandardResolver.Options);

            client.Send(packetData);

            // clear previously sent states to ensure we send it again in case it was a server restart.
            lastSaved.Clear();
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static bool IsValidPlayer(PlayerController player)
        {
            return player != null && player && player.UserId != Guid.Empty && !string.IsNullOrEmpty(player.PlatformId) && player.Id != Guid.Empty && !player.IsBot && !player.PlatformId.StartsWith("#");
        }

        public void SavePlayerExperience(IReadOnlyList<PlayerController> players, bool saveAllSkills = true)
        {
            var saveRequest = new SaveExperienceRequest();
            saveRequest.SessionToken = this.sessionToken;
            saveRequest.ExpUpdates = new ExperienceUpdate[players.Count];

            for (var i = 0; i < players.Count; i++)
            {
                var update = saveRequest.ExpUpdates[i] = new Models.TcpApi.ExperienceUpdate();
                var player = players[i];
                update.CharacterId = player.Id;
                if (saveAllSkills)
                {
                    update.Skills = GetSkillUpdate(player);
                }
                else
                {
                    update.Skills = GetActiveTrainingSkillUpdate(player);
                }
            }

            var packetData = MessagePackSerializer.Serialize(saveRequest, MessagePack.Resolvers.ContractlessStandardResolver.Options);
            if (packetData != null && packetData.Length > 0)
            {
                client.Send(packetData);
            }
            else
            {
                Shinobytes.Debug.LogError("Could not save experience, serialized packet data returned 0 in size.");
            }
        }


        public void SavePlayerState(IReadOnlyList<PlayerController> players)
        {
            var saveRequest = new SaveStateRequest();
            var states = new List<Models.TcpApi.CharacterStateUpdate>();
            saveRequest.SessionToken = this.sessionToken;
            //saveRequest.StateUpdates = new Models.TcpApi.CharacterStateUpdate[players.Count];

            for (var i = 0; i < players.Count; i++)
            {
                var update = new Models.TcpApi.CharacterStateUpdate();
                var player = players[i];

                (Island island, UnityEngine.Vector3 position) = GetPosition(player);
                (CharacterState state, string stateData) = GetState(player);

                update.CharacterId = player.Id;
                update.TrainingSkillIndex = GetTrainingSkillIndex(player);
                update.Health = (short)player.Stats.Health.CurrentValue;
                update.Island = island;
                update.State = state;
                update.X = (short)position.x;
                update.Y = (short)position.y;
                update.Z = (short)position.z;

                if (player.LastSavedState == null || RequiresUpdate(update, player.LastSavedState))
                {
                    states.Add(update);
                    player.LastSavedState = update;
                }
            }

            if (states.Count == 0)
            {
                return;
            }

            saveRequest.StateUpdates = states.ToArray();
            var packetData = MessagePackSerializer.Serialize(saveRequest, MessagePack.Resolvers.ContractlessStandardResolver.Options);
            if (packetData != null && packetData.Length > 0)
            {
                client.Send(packetData);
            }
            else
            {
                Shinobytes.Debug.LogError("Could not save states, serialized packet data returned 0 in size.");
            }
        }

        private bool RequiresUpdate(Models.TcpApi.CharacterStateUpdate a, Models.TcpApi.CharacterStateUpdate b)
        {
            if (a == null || b == null) return true;
            if (a.State != b.State
                || a.TrainingSkillIndex != b.TrainingSkillIndex
                || a.Health != b.Health
                || a.Island != b.Island
                || Distance(a.X, a.Y, a.Z, b.X, b.Y, b.Z) >= 3f)
                return true;

            return false;
        }

        public static float Distance(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            if (Vector.IsHardwareAccelerated)
            {
                var x = x1 - x2;
                var y = y1 - y2;
                var z = z1 - z2;
                float num = Dot(x, y, z, x, y, z);
                return (float)Math.Sqrt(num);
            }

            float num2 = x1 - x2;
            float num3 = y1 - y2;
            float num4 = z1 - z2;
            float num5 = num2 * num2 + num3 * num3 + num4 * num4;
            return (float)Math.Sqrt(num5);
        }

        public static float Dot(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            return x1 * x2 + y1 * y2 + z1 * z2;
        }

        private IReadOnlyList<SkillUpdate> GetActiveTrainingSkillUpdate(PlayerController player)
        {
            // check if we need to add Slayer and/or Sailing
            var updates = new List<SkillUpdate>();
            var sailing = GetSkillUpdate(player, Skill.Sailing);

            if (player.LastSailingSaved == null || player.LastSailingSaved.Experience != sailing.Experience || player.LastSailingSaved.Level != sailing.Level)
            {
                updates.Add(sailing);
                player.LastSailingSaved = sailing;
            }

            var slayer = GetSkillUpdate(player, Skill.Slayer);
            if (player.LastSlayerSaved == null || player.LastSlayerSaved.Experience != slayer.Experience || player.LastSlayerSaved.Level != slayer.Level)
            {
                updates.Add(slayer);
                player.LastSlayerSaved = slayer;
            }

            var activeSkill = player.GetActiveSkillStat();
            if (activeSkill == null)
            {
                return new SkillUpdate[0]; // none
            }

            if (activeSkill.Type == Skill.Health)
            {
                updates.Add(GetSkillUpdate(player, Skill.Attack));
                updates.Add(GetSkillUpdate(player, Skill.Defense));
                updates.Add(GetSkillUpdate(player, Skill.Strength));
                updates.Add(GetSkillUpdate(player, Skill.Health));
            }
            else
            {
                updates.Add(GetSkillUpdate(player, activeSkill.Type));
                if (activeSkill.Type == Skill.Ranged || activeSkill.Type == Skill.Magic || activeSkill.Type == Skill.Healing)
                {
                    updates.Add(GetSkillUpdate(player, Skill.Health));
                }
            }

            return updates;
        }
        private SkillUpdate GetSkillUpdate(PlayerController player, Skill targetSkill)
        {
            var skill = player.GetSkill(targetSkill);
            if (skill == null)
            {
                return null;
            }

            return new SkillUpdate
            {
                Experience = skill.Experience,
                Index = (byte)skill.Index,
                Level = (short)skill.Level,
            };
        }

        private IReadOnlyList<SkillUpdate> GetSkillUpdate(PlayerController player)
        {
            var skills = player.Stats.SkillList;
            var result = new SkillUpdate[skills.Length];
            for (var i = 0; i < skills.Length; ++i)
            {
                var skill = skills[i];
                result[i] = new SkillUpdate
                {
                    Experience = skill.Experience,
                    Index = (byte)skill.Index,
                    Level = (short)skill.Level,
                };
            }
            return result;
        }

        private int GetTrainingSkillIndex(PlayerController player)
        {
            var skill = player.GetActiveSkillStat();
            if (skill == null)
            {
                return -1;
            }

            return skill.Index;
        }

        public void UpdatePlayer(PlayerController player, PlayerUpdateType updateType = PlayerUpdateType.Modified)
        {
            Update<CharacterUpdate, Skills> lastUpdated = null;
            if (updateType != PlayerUpdateType.Force)
            {
                if (lastSaved.TryGetValue(player.Id, out lastUpdated))
                {
                    // Check if its too early to push a save for this character as we don't want to do it too frequently.
                    if (DateTime.UtcNow - lastUpdated.Updated < TimeSpan.FromSeconds(MinDelayBetweenSaveSeconds))
                    {
                        return;
                    }
                }
            }

            (Island island, UnityEngine.Vector3 position) = GetPosition(player);
            (CharacterState state, string stateData) = GetState(player);
            var skills = GetSkillsToUpdate(player, updateType == PlayerUpdateType.Modified ? lastUpdated : null);

            var update = new CharacterUpdate()
            {
                CharacterId = player.Id,
                Health = (short)player.Stats.Health.CurrentValue,
                State = state,
                Task = player.CurrentTaskName,
                TaskArgument = player.taskArgument,
                Island = island,
                X = position.x,
                Y = position.y,
                Z = position.z,
                Skills = skills
            };

            // finally, check if the data we save actually needs to be pushed.
            // this is to avoid sending data if we already sent something very similar before.
            var newUpdate = new Update<CharacterUpdate, Skills>(update, player.Stats);
            if (updateType == PlayerUpdateType.Modified && !RequiresUpdate(newUpdate, lastUpdated))
            {
                return;
            }

            var packetData = MessagePackSerializer.Serialize(update, MessagePack.Resolvers.ContractlessStandardResolver.Options);
            if (packetData != null && packetData.Length > 0)
            {
                client.Send(packetData);
                lastSaved[player.Id] = newUpdate;
            }
        }

        private static bool RequiresUpdate(Update<CharacterUpdate, Skills> current, Update<CharacterUpdate, Skills> last)
        {
            if (last == null) return true; // we have not pushed an update before.
            var a = current.UpdateData;
            var b = last.UpdateData;

            if (a.State != b.State || a.Task != b.Task || a.Health != b.Health || a.TaskArgument != b.TaskArgument || a.Island != b.Island)
                return true;

            if (a.Skills.Length > 0 && a.Skills.Length != b.Skills.Length)
                return true;

            foreach (var sa in a.Skills)
            {
                if (!b.Skills.Any(x => x.Index == sa.Index))
                    return true;

                var skillA = sa;
                var skillB = b.Skills.FirstOrDefault(x => x.Index == sa.Index);
                if (skillB == null)
                    return true;
                if (skillA.Level != skillB.Level || skillA.Experience != skillB.Experience)
                    return true;
            }

            return false;
        }
        private static SkillUpdate[] GetSkillsToUpdate(PlayerController player, Update<CharacterUpdate, Skills> lastUpdate)
        {
            if (lastUpdate == null)
            {
                var su = new SkillUpdate[player.Stats.SkillList.Length];
                // get all. Since we don't have an earlier state to compare with.
                foreach (var s in player.Stats.SkillList)
                {
                    su[s.Index] = new SkillUpdate
                    {
                        Index = (byte)s.Index,
                        Level = (short)s.Level,
                        Experience = s.Experience
                    };
                }

                return su;
            }

            var result = new List<SkillUpdate>();

            // 1. we need to store the actual last exp state
            //    not just what we actually sent to the server. Otherwise we can't compare with a skill that was not sent to the server.
            // YUCK! This is expensive as F trying to the knowledge base, why compare to this?
            foreach (var s in player.Stats.SkillList)
            {
                var oldSkill = lastUpdate.KnowledgeBase.GetSkill(s.Type);

                if (oldSkill.Level != s.Level || oldSkill.Experience != s.Experience)
                {
                    result.Add(new SkillUpdate
                    {
                        Index = (byte)s.Index,
                        Level = (short)s.Level,
                        Experience = s.Experience
                    });
                }
            }

            return result.ToArray();
        }

        private static (RavenNest.Models.TcpApi.Island, UnityEngine.Vector3) GetPosition(PlayerController player)
        {
            //var islandValue = Island.Ferry;

            var island = player.Island?.Island ?? Island.Ferry;
            var pos = player.transform.position;

            if (player.Ferry.OnFerry)
            {
                island = Island.Ferry;
                return (island, pos);
            }

            if (player.Raid.InRaid)
            {
                pos = player.Raid.PreviousPosition;
                if (player.Raid.PreviousIsland != null)
                {
                    island = player.Raid.PreviousIsland?.Island ?? Island.Ferry;
                }
            }

            if (player.Dungeon.InDungeon)
            {
                pos = player.Dungeon.PreviousPosition;
                if (player.Dungeon.PreviousIsland != null)
                {
                    island = player.Dungeon.PreviousIsland?.Island ?? Island.Ferry;
                }
            }

            return (island, pos);
        }

        private static (RavenNest.Models.TcpApi.CharacterState, string) GetState(PlayerController player)
        {
            string stateData = null;
            var state = CharacterState.None;

            if (player.Ferry.OnFerry)
            {
                return (CharacterState.None, null);
            }

            if (player.Raid.InRaid)
            {
                state = CharacterState.Raid;
            }
            else if (player.Dungeon.InDungeon)
            {
                state = CharacterState.Dungeon;
                stateData = player.GameManager.Dungeons.Dungeon.Name;
            }
            else if (player.Duel.InDuel)
            {
                state = CharacterState.Duel;
                stateData = player.Duel.Opponent?.Id.ToString();
            }
            else if (player.StreamRaid.InWar)
            {
                state = CharacterState.StreamRaidWar;
                stateData = player.GameManager.StreamRaid.Raider?.RaiderUserId.ToString();
            }
            else if (player.Arena.InArena)
            {
                state = CharacterState.Arena;
            }
            else if (player.Onsen.InOnsen && !player.InCombat)
            {
                state = CharacterState.Onsen;
            }

            return (state, stateData);
        }
    }

    public enum PlayerUpdateType
    {
        Modified,
        Everything,
        Force
    }
}