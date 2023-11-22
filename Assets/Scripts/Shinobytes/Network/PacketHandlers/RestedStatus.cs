using System;

public class RestedStatus : ChatBotCommandHandler
{
    public RestedStatus(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }
    public override void Handle(GameMessage gm, GameClient client)
    {
        try
        {
            var player = PlayerManager.GetPlayer(gm.Sender);
            if (player == null)
            {
                client.SendReply(gm, Localization.MSG_NOT_PLAYING);
                return;
            }

            if (player.onsenHandler.InOnsen)
            {
                SendOnsenStatusMessage(gm, player, client);
                return;
            }

            SendRestedStatusMessage(gm, player, client);
        }
        catch { }
    }
    private void SendRestedStatusMessage(GameMessage gm, PlayerController player, GameClient client)
    {
        if (player.Rested.RestedTime > 0)
        {
            var hours = player.Rested.RestedTime / 60f / 60f;
            var restedTime = Utility.FormatTime(hours);
            client.SendReply(gm, Localization.MSG_RESTED, (player.Rested.ExpBoost).ToString(), restedTime);
        }
        else
        {
            client.SendReply(gm, Localization.MSG_NOT_RESTED);
        }
    }

    private void SendOnsenStatusMessage(GameMessage gm, PlayerController player, GameClient client)
    {
        var hours = player.Rested.RestedTime / 60f / 60f;
        var restedTime = Utility.FormatTime(hours);
        client.SendReply(gm, Localization.MSG_RESTING, System.Math.Max(2, player.Rested.ExpBoost).ToString(), restedTime);
    }
}