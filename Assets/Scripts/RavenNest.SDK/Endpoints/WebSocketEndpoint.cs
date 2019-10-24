using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

namespace RavenNest.SDK.Endpoints
{
    public class WebSocketEndpoint : IWebSocketEndpoint
    {
        private ConcurrentDictionary<string, CharacterStateUpdate> lastSavedState
            = new ConcurrentDictionary<string, CharacterStateUpdate>();

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
                packetSerializer);

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

        public async Task<bool> SavePlayerAsync(PlayerController player)
        {
            if (IntegrityCheck.IsCompromised)
            {
                return false;
            }

            var characterUpdate = new CharacterStateUpdate(
                player.UserId,
                player.Stats.Health.CurrentValue,
                player.Island?.Identifier,
                player.Duel.Opponent?.UserId,
                player.Raid.InRaid,
                player.Arena.InArena,
                player.GetTask().ToString(),
                string.Join(",", player.GetTaskArguments()),
                new Position
                {
                    X = player.transform.position.x,
                    Y = player.transform.position.y,
                    Z = player.transform.position.z
                });

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
                    return true;
                }
            }

            return false;
        }

        private bool RequiresUpdate(CharacterStateUpdate oldState, CharacterStateUpdate newState)
        {
            if (oldState.Health != newState.Health) return true;
            if (oldState.InArena != newState.InArena) return true;
            if (oldState.InRaid != newState.InRaid) return true;
            if (oldState.Island != newState.Island) return true;
            var op = oldState.Position;
            var np = newState.Position;
            if (op.X != np.X || op.Y != np.Y || op.Z != np.Z) return true;
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