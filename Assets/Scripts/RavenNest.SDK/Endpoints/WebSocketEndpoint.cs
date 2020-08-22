using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RavenNest.SDK.Endpoints
{
    public class WebSocketEndpoint : IWebSocketEndpoint
    {
        private ConcurrentDictionary<string, CharacterStateUpdate> lastSavedState
            = new ConcurrentDictionary<string, CharacterStateUpdate>();
        private ConcurrentDictionary<string, DateTime> lastSavedTime
            = new ConcurrentDictionary<string, DateTime>();

        private const double ForceSaveInterval = 5d;

        private readonly IGameServerConnection connection;
        private readonly IGameManager gameManager;

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
            this.gameManager = gameManager;
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
            if (IntegrityCheck.IsCompromised)
            {
                return false;
            }

            var state = player.BuildPlayerState();

            var characterUpdate = new CharacterSkillUpdate
            {
                Experience = state.Experience,
                UserId = state.UserId
            };

            var response = await connection.SendAsync("update_character_skills", characterUpdate);
            if (response != null && response.TryGetValue<bool>(out var result))
            {
                return result;
            }

            return false;
        }


        public async Task<bool> SavePlayerStateAsync(PlayerController player)
        {
            if (IntegrityCheck.IsCompromised)
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

            var response = await connection.SendAsync("update_character_state", characterUpdate);
            if (response != null && response.TryGetValue<bool>(out var result))
            {
                if (result)
                {
                    lastSavedState[player.UserId] = characterUpdate;
                    lastSavedTime[player.UserId] = DateTime.UtcNow;
                    return true;
                }
            }

            return false;
        }

        private bool RequiresUpdate(CharacterStateUpdate oldState, CharacterStateUpdate newState)
        {
            if (!lastSavedTime.TryGetValue(oldState.UserId, out var date) || DateTime.UtcNow - date > TimeSpan.FromSeconds(ForceSaveInterval))
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

        public void Close()
        {
            connection.Close();
        }


        public class Position
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }
    }
}