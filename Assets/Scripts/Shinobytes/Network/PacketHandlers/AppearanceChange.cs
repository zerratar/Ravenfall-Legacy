using System.Linq;
using System.Runtime.Remoting.Messaging;

public class AppearanceChange : PacketHandler<PlayerAppearanceRequest>
{
    public AppearanceChange(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(PlayerAppearanceRequest data, GameClient client)
    {
        if (Game.RavenNest.SessionStarted)
        {
            var player = PlayerManager.GetPlayer(data.Player);
            if (!player)
            {
                client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
                return;
            }

            if (string.IsNullOrEmpty(data.Appearance))
            {
                client.SendMessage(data.Player.Username, Localization.MSG_APPEARANCE_INVALID);
                return;
            }

            if (player.Appearance.TryUpdate(
                data.Appearance
                    .Split(',')
                    .Select(x => int.TryParse(x, out var value) ? value : 0).ToArray()))
            {
                await Game.RavenNest.Players.UpdateAppearanceAsync(player.UserId, player.Appearance.ToAppearanceData());
            }
        }
        else
        {
            client.SendMessage(data.Player.Username, Localization.GAME_NOT_READY);
        }
    }
}