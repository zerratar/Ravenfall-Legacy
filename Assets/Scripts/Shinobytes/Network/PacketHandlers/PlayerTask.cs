using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class PlayerTask : PacketHandler
{
    public PlayerTask(
        GameManager game,
        RavenBotConnection server,
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
                packet.Client.SendMessage(task.Player.Username, Localization.MSG_NOT_PLAYING);
                return;
            }

            player.SetTask(task.Task, task.Arguments);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError(exc);
        }
    }
}