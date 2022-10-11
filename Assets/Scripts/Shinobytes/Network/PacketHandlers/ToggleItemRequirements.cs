public class ToggleItemRequirements : ChatBotCommandHandler<TwitchPlayerInfo>
{
    public ToggleItemRequirements(
      GameManager game,
      RavenBotConnection server,
      PlayerManager playerManager)
      : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        //var player = PlayerManager.GetPlayer(data);
        //if (player == null || !player || !player.IsGameAdmin)
        //{
        //    return;
        //}

        //Game.NoItemRequirements = !Game.NoItemRequirements;

        //client.SendMessage(data, Game.NoItemRequirements
        //    ? "All item requirements have been turned on."
        //    : "All item requirements have been turned off. Players may now equip any item.");
    }
}
