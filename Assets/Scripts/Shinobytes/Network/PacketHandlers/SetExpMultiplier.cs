
public class SetExpMultiplier : ChatBotCommandHandler<SetExpMultiplierRequest>
{
    public SetExpMultiplier(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }

    public override void Handle(SetExpMultiplierRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (player == null || !player || !player.IsGameAdmin)
        {
            return;
        }

        Game.Twitch.SetExpMultiplier(player.Name, data.ExpMultiplier);
    }
}
