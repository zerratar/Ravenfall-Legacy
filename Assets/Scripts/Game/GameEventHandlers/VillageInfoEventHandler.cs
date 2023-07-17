using RavenNest.Models;

public class VillageInfoEventHandler : GameEventHandler<VillageInfo>
{
    public override void Handle(GameManager gameManager, VillageInfo data)
    {
        if (gameManager.Village && gameManager.Village != null)
        {
            gameManager.Village.TownHall.SetResources(data.Coins, data.Ore, data.Wood, data.Fish, data.Wheat);
            gameManager.Village.SetTierByLevel(data.Level);
            if (!gameManager.Village.HousesAreUpdating)
            {
                gameManager.Village.SetHouses(data.Houses);
            }
            gameManager.Village.TownHall.SetExp(data.Experience);
            return;
        }

        // We are in another scene, ignore any errors.
    }
}
