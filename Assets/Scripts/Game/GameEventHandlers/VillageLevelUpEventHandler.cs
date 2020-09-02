public class VillageLevelUpEventHandler : GameEventHandler<VillageLevelUp>
{
    protected override void Handle(GameManager gameManager, VillageLevelUp data)
    {
        if (data.LevelDelta > 0)
        {
            UnityEngine.Debug.Log($"Congratulations! Village gained {data.LevelDelta} level(s)!");

            gameManager.Village.SetExp(data.Experience);
            gameManager.Village.SetTierByLevel(data.Level);
            gameManager.Village.SetSlotCount(data.HouseSlots);
        }
    }
}
