public class OnsenLeave : ChatBotCommandHandler<TwitchPlayerInfo>
{
    public OnsenLeave(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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

            if (!player.Onsen.InOnsen)
            {
                return;
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

            Game.Onsen.Leave(player);

            //Game.RavenBot.SendMessage(player.PlayerName, Localization.MSG_ONSEN_LEFT);
        }
        catch { }
    }
}
