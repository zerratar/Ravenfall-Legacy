public class ChannelStateChanged : ChatBotCommandHandler<Arguments>
{
    public ChannelStateChanged(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(Arguments data, GameMessage gm, GameClient client)
    {
        if (data.Count < 4)
        {
            return;
        }

        var channelName = data.GetArg<string>(1);
        var inChannel = data.GetArg<bool>(2);

        Game.RavenBotController.HasJoinedChannel = inChannel;

        //if (!inChannel)
        //{
        //    Shinobytes.Debug.LogError("Bot left channel (" + channelName + ")");
        //}
        //else
        //{
        //    Shinobytes.Debug.Log("Bot joined channel (" + channelName + ")");
        //}
    }
}
