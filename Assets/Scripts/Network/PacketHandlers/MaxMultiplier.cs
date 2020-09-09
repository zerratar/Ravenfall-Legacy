public class MaxMultiplier : PacketHandler<Player>
{
    public MaxMultiplier(
      GameManager game,
      GameServer server,
      PlayerManager playerManager)
  : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        if (Game.Twitch.CurrentBoost.Active)
        {
            client.SendCommand(data.Username, "message", $"Current Exp Multiplier is at {Game.Twitch.CurrentBoost.Multiplier}. Max is {Game.Permissions.ExpMultiplierLimit}. Currently {Game.Twitch.CurrentBoost.CheerPot} bits.");
        }
        else
        {
            client.SendCommand(data.Username, "message", $"Max exp multiplier is {Game.Permissions.ExpMultiplierLimit}. Currently {Game.Twitch.CurrentBoost.CheerPot} bits.");
        }
    }
}
