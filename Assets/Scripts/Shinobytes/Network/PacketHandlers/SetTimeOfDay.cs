public class SetTimeOfDay : ChatBotCommandHandler<SetTimeOfDayRequest>
{
    public SetTimeOfDay(
      GameManager game,
      RavenBotConnection server,
      PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }

    public override void Handle(SetTimeOfDayRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (player == null || !player || !player.IsBroadcaster)
        {
            return;
        }

        Game.SetTimeOfDay(data.TotalTime, data.FreezeTime);
    }
}