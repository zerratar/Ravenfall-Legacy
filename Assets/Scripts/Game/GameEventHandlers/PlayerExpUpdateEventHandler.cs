using RavenNest.Models;
using UnityEngine;

public class PlayerExpUpdateEventHandler : GameEventHandler<PlayerExpUpdate>
{
    protected override void Handle(GameManager gameManager, PlayerExpUpdate data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.UserId);
        if (player == null)
            return;

        var skill = player.GetSkill(data.Skill);
        if (skill != null)
        {
            skill.SetExp(data.Experience);
        }

        Shinobytes.Debug.Log($"PlayerExpUpdateEventHandler " + data.UserId);
    }
}

