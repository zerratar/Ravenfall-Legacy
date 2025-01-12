public class TwitchCheerer : ChatBotCommandHandler<CheerBitsEvent>
{
    public TwitchCheerer(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }

    public override void Handle(CheerBitsEvent data, GameMessage gm, GameClient client)
    {
        //Game.RavenNest.EnqueueLoyaltyUpdate(data);
        Game.Twitch.OnCheer(data);
    }
}
