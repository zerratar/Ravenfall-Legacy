using UnityEngine;

public class PlayerRemoveEventHandler : GameEventHandler<PlayerRemove>
{
    public override void Handle(GameManager gameManager, PlayerRemove data)
    {
        var player = gameManager.Players.GetPlayerById(data.CharacterId);
        if (!player)
        {
            Shinobytes.Debug.LogWarning($"Received Player Remove ({data.UserId}) but the player is not in this game. Reason: " + data.Reason);
            return;
        }

        gameManager.QueueRemovePlayer(player);
        Shinobytes.Debug.LogWarning($"{player.PlayerName} removed from the game. Reason: " + data.Reason);
    }
}