public class SetTimeOfDay : ChatBotCommandHandler<SetTimeOfDayRequest>
{
    public SetTimeOfDay(
      GameManager game,
      RavenBotConnection server,
      PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }

    public override void Handle(SetTimeOfDayRequest data, GameMessage gm, GameClient client)
    {
        var user = gm.Sender;
        if (!user.IsModerator && !user.IsBroadcaster)
        {
            var player = PlayerManager.GetPlayer(gm.Sender);
            if (!player)
            {
                return;
            }
            if (!player.IsGameAdmin && !player.IsGameModerator)
            {
                return;
            }
        }

        Game.SetTimeOfDay(data.TotalTime, data.FreezeTime);
    }
}