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
        var msg = new ReplyMessageBuilder();

        if (!string.IsNullOrEmpty(ferryStats.Destination))
        {
            msg.Add(Localization.MSG_FERRY_INFO_DESTINATION, ferryStats.Destination, ferryStats.PlayersCount);
        }
        else
        {
            msg.Add(Localization.MSG_FERRY_INFO, ferryStats.PlayersCount);
        }

        var ferryCaptain = Game.Ferry.Captain;
        if (ferryCaptain)
        {
            msg.Add(Localization.MSG_FERRY_INFO_CAPTAIN, ferryCaptain.Name, ferryCaptain.Stats.Sailing.MaxLevel);
        }

        if (Game.Ferry.IsFerryBoostActive)
        {
            msg.Add(Localization.MSG_FERRY_INFO_BOOST, Game.Ferry.GetRemainingBoostTime());
        }
        client.SendReply(gm, msg.ToString(), msg.Arguments);
    }
}