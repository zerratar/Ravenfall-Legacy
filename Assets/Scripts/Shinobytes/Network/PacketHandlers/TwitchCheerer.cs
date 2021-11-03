public class TwitchCheerer : PacketHandler<TwitchCheer>
{
    public TwitchCheerer(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }
    public override void Handle(TwitchCheer data, GameClient client)
    {
        Game.Twitch.OnCheer(data);
    }
}
