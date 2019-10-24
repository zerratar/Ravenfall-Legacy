using UnityEngine;

public class RaidStreamer : PacketHandler<StreamerRaid>
{
    public RaidStreamer(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(StreamerRaid data, GameClient client)
    {
        client.SendCommand("", "message", $"Requesting {(data.War ? "war raid" : "raid")} on {data.Player.Username}s stream!");
        if (await Game.RavenNest.EndSessionAndRaidAsync(data.Player.Username, data.War))
        {
            client.SendCommand("", "message", $"The raid will start any moment now! PogChamp");            
            Game.BeginStreamerRaid(data.Player.Username, data.War);
        }
        else
        {
            client.SendCommand("", "message", $"Request failed, streamer is no longer playing or has disabled raids. :(");
        }
    }
}
