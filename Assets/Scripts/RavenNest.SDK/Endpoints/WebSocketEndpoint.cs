using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    public class WebSocketEndpoint : IWebSocketEndpoint
    {
        private ConcurrentDictionary<string, CharacterStateUpdate> lastSavedState
            = new ConcurrentDictionary<string, CharacterStateUpdate>();
        private ConcurrentDictionary<string, CharacterSkillUpdate> lastSavedSkills
            = new ConcurrentDictionary<string, CharacterSkillUpdate>();

        private ConcurrentDictionary<string, DateTime> lastSavedStateTime
            = new ConcurrentDictionary<string, DateTime>();
        private ConcurrentDictionary<string, DateTime> lastSavedSkillsTime
            = new ConcurrentDictionary<string, DateTime>();

        private const double ForceSaveInterval = 5d;

        private readonly IGameServerConnection connection;
        private readonly IGameManager gameManager;

        public bool ForceReconnecting { get; private set; }
        public bool IsReady => connection?.IsReady ?? false;

        public WebSocketEndpoint(
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
            this.gameManager = gameManager;
        }

        private void OnReconnected(object sender, EventArgs e)
        {
            ForceReconnecting = false;
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

        public async Task<bool> SavePlayerSkillsAsync(PlayerController player)
        {
            var state = player.BuildPlayerState();

            var characterUpdate = new CharacterSkillUpdate
            {
                Experience = state.Experience,
                UserId = state.UserId
            };

            if (lastSavedSkills.TryGetValue(player.UserId, out var lastUpdate))
            {
                if (!RequiresUpdate(lastUpdate, characterUpdate))
                {
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


        public async Task<bool> SavePlayerStateAsync(PlayerController player)
        {
            if (player == null || !player)
            {
                return false;
            }

            var characterUpdate = new CharacterStateUpdate(
                player.UserId,
                player.Stats.Health.CurrentValue,
                player.Island?.Identifier ?? "",
                player.Duel.InDuel ? player.Duel.Opponent?.UserId ?? "" : "",
                player.Raid.InRaid,
                player.Arena.InArena,
                player.GetTask().ToString(),
                string.Join(",", player.GetTaskArguments()),
                    player.transform.position.x,
                   player.transform.position.y,
                    player.transform.position.z
                );

            if (lastSavedState.TryGetValue(player.UserId, out var lastUpdate))
            {
                if (!RequiresUpdate(lastUpdate, characterUpdate))
                {
                    return false;
                }
            }

            connection.SendNoAwait("update_character_state", characterUpdate);

            //var response = await connection.SendAsync("update_character_state", characterUpdate);
            //if (response != null && response.TryGetValue<bool>(out var result))
            //{
            //    if (result)
            //    {
            //        lastSavedState[player.UserId] = characterUpdate;
            //        lastSavedStateTime[player.UserId] = DateTime.UtcNow;
            //        return true;
            //    }
            //}

            lastSavedState[player.UserId] = characterUpdate;
            lastSavedStateTime[player.UserId] = DateTime.UtcNow;
            return true;
        }

        private bool RequiresUpdate(CharacterStateUpdate oldState, CharacterStateUpdate newState)
        {
            if (!lastSavedStateTime.TryGetValue(oldState.UserId, out var date) || DateTime.UtcNow - date > TimeSpan.FromSeconds(ForceSaveInterval))
                return true;

            if (oldState.Health != newState.Health) return true;
            if (oldState.InArena != newState.InArena) return true;
            if (oldState.InRaid != newState.InRaid) return true;
            if (oldState.Island != newState.Island) return true;
#warning disabled update player state on position change
            //var op = oldState.Position;
            //var np = newState.Position;
            //if (op.X != np.X || op.Y != np.Y || op.Z != np.Z) return true;
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