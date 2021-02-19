using UnityEngine;

public class RaidStreamer : PacketHandler<StreamerRaid>
{
    public RaidStreamer(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(StreamerRaid data, GameClient client)
    {
        client.SendMessage("", Localization.MSG_REQ_RAID, data.War ? "war raid" : "raid", data.Player.Username);
        if (await Game.RavenNest.EndSessionAndRaidAsync(data.Player.Username, data.War))
        {
            client.SendMessage("", Localization.MSG_REQ_RAID_SOON);
            Game.BeginStreamerRaid(data.Player.Username, data.War);
        }
        else
        {
            client.SendMessage("", Localization.MSG_REQ_RAID_FAILED);
        }
    }
}
