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

        if (Game.Ferry.IsFerryBoostActive)
        {
            client.SendReply(gm, $"The ferry is currently sailing to {ferryStats.Destination} with {ferryStats.PlayersCount} players on board. Ferry boost will end in {Game.Ferry.GetRemainingBoostTime()}");
            return;
        }
        client.SendReply(gm, $"The ferry is currently sailing to {ferryStats.Destination} with {ferryStats.PlayersCount} players on board.");
    }
}