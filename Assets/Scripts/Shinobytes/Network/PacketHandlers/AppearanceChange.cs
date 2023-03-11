using System.Linq;
using System.Runtime.Remoting.Messaging;

public class AppearanceChange : ChatBotCommandHandler<PlayerAppearanceRequest>
{
    public AppearanceChange(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(PlayerAppearanceRequest data, GameMessage gm, GameClient client)
    {
        if (Game.RavenNest.SessionStarted)
        {
            var player = PlayerManager.GetPlayer(gm.Sender);
            if (!player)
            {
                client.SendReply(gm, Localization.MSG_NOT_PLAYING);
                return;
            }

            if (string.IsNullOrEmpty(data.Appearance))
            {
                client.SendReply(gm, Localization.MSG_APPEARANCE_INVALID);
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
            client.SendReply(gm, Localization.GAME_NOT_READY);
        }
    }
}