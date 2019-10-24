
public class TwitchSubscriber : PacketHandler<TwitchSubscription>
{
    public TwitchSubscriber(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchSubscription data, GameClient client)
    {
        Game.Twitch.OnSubscribe(data);
    }
}