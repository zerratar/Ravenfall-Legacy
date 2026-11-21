/// <summary>
/// !toggle cape: hides the player's cape, their own or their clan's, or shows it again. While it is hidden the
/// armor's own back piece (a cloak, a pack, a pelt) shows, if the chest has one. The server keeps the choice.
/// </summary>
public class ToggleCapeVisibility : ChatBotCommandHandler
{
    public ToggleCapeVisibility(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager) : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var targetPlayer = PlayerManager.GetPlayer(gm.Sender);
        if (!targetPlayer)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        Game.RavenNest.Players.ToggleCapeAsync(targetPlayer.Id);

        var appearance = targetPlayer.Appearance;
        appearance.ToggleCapeVisibility();

        // a player without a cape has nothing to see change; the choice is kept for a cape they get later
        if (!appearance.HasCape)
        {
            client.SendReply(gm, appearance.CapeVisible ? Localization.MSG_CAPE_NONE_SHOWN : Localization.MSG_CAPE_NONE_HIDDEN);
            return;
        }

        if (appearance.CapeVisible)
        {
            client.SendReply(gm, Localization.MSG_CAPE_SHOWN);
            return;
        }

        client.SendReply(gm, appearance.BackPieceShows ? Localization.MSG_CAPE_HIDDEN_BACK_PIECE : Localization.MSG_CAPE_HIDDEN);
    }
}