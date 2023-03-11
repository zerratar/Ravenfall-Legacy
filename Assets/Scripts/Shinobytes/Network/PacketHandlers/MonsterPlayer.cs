public class MonsterPlayer : ChatBotCommandHandler
{
    public MonsterPlayer(
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

        if (!player.TurnIntoMonster(5f * 60f))
        {
            client.SendReply(gm, "Player could not be turned into a monster right now :(");
        }
    }
}
