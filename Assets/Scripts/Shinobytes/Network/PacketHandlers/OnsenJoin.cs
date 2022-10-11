public class OnsenJoin : ChatBotCommandHandler<TwitchPlayerInfo>
{
    public OnsenJoin(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }
    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        try
        {
            var player = PlayerManager.GetPlayer(data);
            if (player == null)
            {
                client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
                return;
            }

            if (player.Ferry.Embarking)
            {
                player.Ferry.Cancel();

            }

            if (player.Duel.InDuel)
            {
                return;
            }

            if (player.Arena.InArena)
            {
                return;
            }

            if (player.Dungeon.InDungeon)
            {
                return;
            }

            if (player.Raid.InRaid)
            {
                return;
            }

            if (player.StreamRaid.InWar)
            {
                return;
            }

            if (player.Onsen.InOnsen)
            {
                // Send Rested Status?
                return;
            }

            if (Game.Onsen.Join(player))
            {
                //Game.RavenBot.SendMessage(player.PlayerName, Localization.MSG_ONSEN_ENTRY);
            }
        }
        catch { }
    }
}
