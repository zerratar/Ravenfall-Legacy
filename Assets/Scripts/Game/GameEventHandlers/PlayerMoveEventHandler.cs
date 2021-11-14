using RavenNest.Models;
using UnityEngine;

public class PlayerMoveEventHandler : GameEventHandler<PlayerMove>
{
    protected override void Handle(GameManager gameManager, PlayerMove data)
    {
        Shinobytes.Debug.Log($"PlayerMoveEventHandler " + data.UserId);
    }
}

