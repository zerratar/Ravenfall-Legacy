﻿using RavenNest.Models;

public class PlayerAppearanceEventHandler : GameEventHandler<SyntyAppearanceUpdate>
{
    public override void Handle(GameManager gameManager, SyntyAppearanceUpdate data)
    {
        var player = gameManager.Players.GetPlayerByUserId(data.UserId);
        if (player)
        {
            player.Appearance.SetAppearance(data.Value, () => player.Inventory.EquipAll(), true);
        }
    }
}
