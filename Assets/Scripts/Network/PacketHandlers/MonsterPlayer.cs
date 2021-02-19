public class MonsterPlayer : PacketHandler<Player>
{
    public MonsterPlayer(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendFormat(data.DisplayName, "Player is not currently in the game.");
            return;
        }

        if (!player.TurnIntoMonster(5f * 60f))
        {
            client.SendFormat(data.DisplayName, "Player could not be turned into a monster right now :(");
        }
    }
}
