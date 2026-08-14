using System.Text;

public class FerryInfo : ChatBotCommandHandler
{
    public FerryInfo(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var ferryStats = Game.GetFerryStats();
        var msg = new StringBuilder();

        if (!string.IsNullOrEmpty(ferryStats.Destination))
        {
            msg.Append($"The ferry is currently sailing towards {ferryStats.Destination} with {ferryStats.PlayersCount} players on board.");
        }
        else
        {
            msg.Append($"The ferry is currently sailing with {ferryStats.PlayersCount} players on board.");
        }

        var ferryCaptain = Game.Ferry.Captain;
        if (ferryCaptain)
        {
            msg.Append(" " + ferryCaptain.Name + " is the captain with " + ferryCaptain.Stats.Sailing.MaxLevel + " sailing.");
        }

        if (Game.Ferry.IsFerryBoostActive)
        {
            msg.Append(" There is an active ferry boost giving 5x the speed, boost ends in " + Game.Ferry.GetRemainingBoostTime() + ".");
        }
        client.SendReply(gm, msg.ToString());
    }
}