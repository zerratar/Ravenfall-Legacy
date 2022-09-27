using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Text;
using MessagePack;
using RavenNest.Models.TcpApi;
using RavenNest.SDK.Endpoints;
using System.Threading;

namespace RavenNest.SDK
{
    public class TcpApi : IDisposable
    {
        public const int MaxMessageSize = 16 * 1024;
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
                // mostly for debugging, disabling the Tcp Api will allow the WebSocket Api to be used for saving players
                // so this is for testing websocket that it works as expected.
                if (!Enabled)
                {
                    if (Connected)
                    {
                        Disconnect();
                    }

                    System.Threading.Thread.Sleep(1000);
                    continue;
                }

                if (client != null)
                {
                    client.Tick(100);
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

                System.Threading.Thread.Sleep(16);
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
                    //if (Ravenfall.Version == "0.8.0.0a")
                    //{
                    //    // currently, we are only expecting PlayerRestedUpdate
                    //    // but we may change this in the future.
                    //    var data = MessagePackSerializer.Deserialize<Models.PlayerRestedUpdate>(obj, MessagePack.Resolvers.ContractlessStandardResolver.Options);
                    //    gameManager.Players.UpdateRestedState(data);
                    //    return;
                    //}

                    var eventList = MessagePackSerializer.Deserialize<Models.EventList>(obj, MessagePack.Resolvers.ContractlessStandardResolver.Options);
                    
                    gameManager.HandleGameEvents(eventList);
                }
                catch { }
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
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanBeUpdated(PlayerController player, out Update<CharacterUpdate, Skills> lastUpdate)
        {
            lastUpdate = null;
            var bad = player == null || !player || string.IsNullOrEmpty(player.UserId) || player.Id == Guid.Empty || player.IsBot || player.UserId.StartsWith("#");
            if (bad) return false;
            if (lastSaved.TryGetValue(player.Id, out lastUpdate) && DateTime.UtcNow - lastUpdate.Updated < TimeSpan.FromSeconds(MinDelayBetweenSaveSeconds))
            {
                return false;
            }
            return true;
        }

        public void UpdatePlayer(PlayerController player)
        {
            if (!CanBeUpdated(player, out var lastUpdated))
                return;

            (Island island, UnityEngine.Vector3 position) = GetPosition(player);
            (CharacterState state, string stateData) = GetState(player);

            var skills = GetSkillsToUpdate(player, lastUpdated);

            var update = new CharacterUpdate()
            {
                SessionToken = sessionToken,
                CharacterId = player.Id,
                Health = player.Stats.Health.CurrentValue,
                State = state,
                StateData = stateData,
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
            try
            {
                if (!RequiresUpdate(newUpdate, lastUpdated))
                {
                    return;
                }

                var packetData = MessagePackSerializer.Serialize(update, MessagePack.Resolvers.ContractlessStandardResolver.Options);
                if (packetData != null && packetData.Length > 0)
                {
                    client.Send(packetData);
                }
            }
            finally
            {
                lastSaved[player.Id] = newUpdate;
            }
        }

        private static bool RequiresUpdate(Update<CharacterUpdate, Skills> current, Update<CharacterUpdate, Skills> last)
        {
            if (last == null) return true; // we have not pushed an update before.
            var a = current.UpdateData;
            var b = last.UpdateData;

            if (a.State != b.State || a.StateData != b.StateData || a.Task != b.Task || a.Health != b.Health || a.TaskArgument != b.TaskArgument || a.Island != b.Island)
                return true;

            if (a.Skills.Count > 0 && a.Skills.Count != b.Skills.Count)
                return true;

            foreach (var sa in a.Skills)
            {
                if (!b.Skills.ContainsKey(sa.Key))
                    return true;

                var skillA = sa.Value;
                var skillB = b.Skills[sa.Key];
                if (skillA.Level != skillB.Level || skillA.Experience != skillB.Experience)
                    return true;
            }

            return false;
        }
        private static Dictionary<string, SkillUpdate> GetSkillsToUpdate(PlayerController player, Update<CharacterUpdate, Skills> lastUpdate)
        {
            var result = new Dictionary<string, SkillUpdate>();
            if (lastUpdate == null)
            {
                // get all. Since we don't have an earlier state to compare with.
                foreach (var s in player.Stats.SkillList)
                {
                    result[s.Name] = new SkillUpdate
                    {
                        Level = s.Level,
                        Experience = s.Experience
                    };
                }

                return result;
            }

            /*
                new System.Collections.Generic.Dictionary<string, SkillUpdate>
                {
                    ["Strength"] = new SkillUpdate { Level = 1, Experience = 0 },
                    ["Health"] = new SkillUpdate { Level = 10, Experience = 1000 },
                }             
             */


            // 1. we need to store the actual last exp state
            //    not just what we actually sent to the server. Otherwise we can't compare with a skill that was not sent to the server.

            foreach (var s in player.Stats.SkillList)
            {
                var oldSkill = lastUpdate.KnowledgeBase.GetSkill(s.Type);

                if (oldSkill.Level != s.Level || oldSkill.Experience != s.Experience)
                {
                    result[s.Name] = new SkillUpdate
                    {
                        Level = s.Level,
                        Experience = s.Experience
                    };
                }
            }

            return result;
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
                stateData = player.Duel.Opponent?.UserId;
            }
            else if (player.StreamRaid.InWar)
            {
                state = CharacterState.StreamRaidWar;
                stateData = player.GameManager.StreamRaid.Raider?.RaiderUserId;
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
}