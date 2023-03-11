
public class SetExpMultiplier : ChatBotCommandHandler<int>
{
    public SetExpMultiplier(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }

    public override void Handle(int data, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (player == null || !player || !player.IsGameAdmin)
        {
            return;
        }

        //Game.Twitch.SetExpMultiplier(player.Name, data.ExpMultiplier);
    }
}
