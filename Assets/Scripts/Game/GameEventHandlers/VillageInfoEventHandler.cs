using System;

public class VillageInfoEventHandler : GameEventHandler<VillageInfo>
{
    protected override void Handle(GameManager gameManager, VillageInfo data)
    {
        gameManager.Village.SetTierByLevel(data.Level);
        gameManager.Village.SetHouses(data.Houses);

        UnityEngine.Debug.Log("Village Info Received! " +
            "Name: " + data.Name +
            ", Level: " + data.Level +
            ", House Slots: " + data.Houses.Count);
    }
}
