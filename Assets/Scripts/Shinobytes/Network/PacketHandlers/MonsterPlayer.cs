public class MonsterPlayer : PacketHandler<TwitchPlayerInfo>
{
    public MonsterPlayer(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendMessage(data.DisplayName, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (!player.TurnIntoMonster(5f * 60f))
        {
            client.SendFormat(data.DisplayName, "Player could not be turned into a monster right now :(");
        }
    }
}
