
public class TwitchSubscriber : ChatBotCommandHandler<UserSubscriptionEvent>
{
    public TwitchSubscriber(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(UserSubscriptionEvent data, GameMessage gm, GameClient client)
    {
        //Game.RavenNest.EnqueueLoyaltyUpdate(data);
        Game.Twitch.OnSubscribe(data);
    }
}