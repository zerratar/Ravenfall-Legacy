public class VillageInfoEventHandler : GameEventHandler<VillageInfo>
{
    protected override void Handle(GameManager gameManager, VillageInfo data)
    {
        if (gameManager.Village && gameManager.Village != null)
        {
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
