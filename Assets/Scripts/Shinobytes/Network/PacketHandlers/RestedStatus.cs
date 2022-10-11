using System;

public class RestedStatus : ChatBotCommandHandler<TwitchPlayerInfo>
{
    public RestedStatus(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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

            if (player.Onsen.InOnsen)
            {
                SendOnsenStatusMessage(player, client);
                return;
            }

            SendRestedStatusMessage(player, client);
        }
        catch { }
    }
    private void SendRestedStatusMessage(PlayerController player, GameClient client)
    {
        if (player.Rested.RestedTime > 0)
        {
            var hours = player.Rested.RestedTime / 60f / 60f;
            var restedTime = Utility.FormatTime(hours);
            client.SendMessage(player.PlayerName, Localization.MSG_RESTED, (player.Rested.ExpBoost).ToString(), restedTime);
        }
        else
        {
            client.SendMessage(player.PlayerName, Localization.MSG_NOT_RESTED);
        }
    }

    private void SendOnsenStatusMessage(PlayerController player, GameClient client)
    {
        var hours = player.Rested.RestedTime / 60f / 60f;
        var restedTime = Utility.FormatTime(hours);
        client.SendMessage(player.PlayerName, Localization.MSG_RESTING, System.Math.Max(2, player.Rested.ExpBoost).ToString(), restedTime);
    }
}