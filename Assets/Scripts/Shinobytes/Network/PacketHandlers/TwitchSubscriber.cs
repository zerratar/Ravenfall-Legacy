
public class TwitchSubscriber : ChatBotCommandHandler<TwitchSubscription>
{
    public TwitchSubscriber(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchSubscription data, GameClient client)
    {
        Game.RavenNest.EnqueueLoyaltyUpdate(data);
        Game.Twitch.OnSubscribe(data);
    }
}