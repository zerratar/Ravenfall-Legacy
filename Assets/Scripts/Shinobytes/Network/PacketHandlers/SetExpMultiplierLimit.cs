public class SetExpMultiplierLimit : ChatBotCommandHandler<SetExpMultiplierRequest>
{
    public SetExpMultiplierLimit(
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

        Game.Twitch.SetExpMultiplierLimit(player.Name, data.ExpMultiplier);
    }
}
