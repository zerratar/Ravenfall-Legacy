using System.Linq;
using System.Runtime.Remoting.Messaging;

public class AppearanceChange : PacketHandler<PlayerAppearanceRequest>
{
    public AppearanceChange(
        GameManager game,
        GameServer server,
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
                client.SendCommand(data.Player.Username, "change_appearance_failed", "You are not playing, use !join to to start playing first.");
                return;
            }

            if (string.IsNullOrEmpty(data.Appearance))
            {
                client.SendCommand(data.Player.Username, "change_appearance_failed", "Invalid appearance data");
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
            client.SendCommand(data.Player.Username, "change_appearance_failed", "Game is not ready yet.");
        }
    }
}