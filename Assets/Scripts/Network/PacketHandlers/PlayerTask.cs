using System;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class PlayerTask : PacketHandler
{
    public PlayerTask(
        GameManager game,
        GameServer server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(Packet packet)
    {
        try
        {
            var task = JsonConvert.DeserializeObject<PlayerTaskRequest>(packet.JsonData);
            var player = PlayerManager.GetPlayer(task.Player);
            if (player == null || !player)
            {
                packet.Client.SendCommand(task.Player.Username, "message", "You are not playing, use !join to play.");
                return;
            }

            if (player.Ferry && player.Ferry.Active)
            {
                player.Ferry.Disembark();
            }

            if (Game.Arena && Game.Arena.HasJoined(player) && !Game.Arena.Leave(player))
            {
                Debug.Log(player.PlayerName + " task cannot be done as you're inside the arena.");
                return;
            }

            if (player.Duel.InDuel)
            {
                return;
            }

            var type = Enum
                .GetValues(typeof(TaskType))
                .Cast<TaskType>()
                .FirstOrDefault(x => x.ToString().Equals(task.Task, StringComparison.InvariantCultureIgnoreCase));

            Game.Raid.Leave(player);

            if (Game.Arena.HasJoined(player))
            {
                Game.Arena.Leave(player);
            }

            var taskArgs = task.Arguments == null || task.Arguments.Length == 0
                ? new[] { type.ToString() }
                : task.Arguments;

            player.SetTaskArguments(taskArgs);
            player.GotoClosest(type);
        }
        catch (Exception exc)
        {
            Game.LogError(exc.ToString());
        }
    }
}