using Shinobytes.Linq;
using System.Text;

namespace Assets.Scripts.Game
{
    public static class GameUtilities
    {
        public static bool IsValid(PlayerController player)
        {
            return string.IsNullOrEmpty(Validate(player));
        }

        public static string Validate(PlayerController player)
        {
            var sb = new StringBuilder();
            if (player == null || !player)
            {
                sb.AppendLine("Player is null or destroyed.");
            }
            else if (player.isDestroyed)
            {
                sb.AppendLine("Player is destroyed / removed.");
            }
            else
            {
                if (string.IsNullOrEmpty(player.PlayerName))
                {
                    sb.AppendLine(nameof(player.PlayerName) + " is null.");
                }

                if (!player.Animations || player.Animations == null)
                {
                    sb.AppendLine(nameof(player.Animations) + " is null.");
                }

                if (!player.ferryHandler || player.ferryHandler == null)
                {
                    sb.AppendLine(nameof(player.ferryHandler) + " is null.");
                }

                if (!player.onsenHandler || player.onsenHandler == null)
                {
                    sb.AppendLine(nameof(player.onsenHandler) + " is null.");
                }

                if (!player.Appearance || player.Appearance == null)
                {
                    sb.AppendLine(nameof(player.Appearance) + " is null.");
                }

                if (!player.Island || player.Island == null)
                {
                    if (!player.dungeonHandler.InDungeon && player.ferryHandler && !player.ferryHandler.OnFerry && !player.streamRaidHandler.InWar)
                    {
                        sb.AppendLine(nameof(player.Island) + " is null.");
                    }
                }

                if (!player.IsBot)
                {
                    if (string.IsNullOrEmpty(player.PlatformId))
                    {
                        sb.AppendLine(nameof(player.PlatformId) + " is null.");
                    }

                    if (player.User == null)
                    {
                        sb.AppendLine(nameof(player.User) + " is null.");
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(player.User.PlatformId))
                        {
                            sb.AppendLine(nameof(player.User.PlatformId) + " is null.");
                        }
                        if (string.IsNullOrEmpty(player.User.Username))
                        {
                            sb.AppendLine(nameof(player.User.Username) + " is null.");
                        }
                    }
                }
            }

            return sb.ToString();
        }

        public static string Validate(GameManager gameManager)
        {
            var sb = new StringBuilder();

            if (gameManager == null || !gameManager)
            {
                sb.AppendLine(nameof(GameManager) + " is null or destroyed.");
            }
            else
            {
                if (gameManager.RavenBot == null)
                {
                    sb.AppendLine(nameof(gameManager.RavenBot) + " is null");
                }
                else if (!gameManager.RavenBot.IsConnected)
                {
                    sb.AppendLine(nameof(gameManager.RavenBot) + " is not connected.");
                }

            }

            return sb.ToString();
        }

        public static string Validate(PlayerController player, GameManager gameManager)
        {
            return Validate(player) + Validate(gameManager);
        }
    }
}
