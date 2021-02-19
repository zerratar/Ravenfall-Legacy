public class VillageLevelUpEventHandler : GameEventHandler<VillageLevelUp>
{
    protected override void Handle(GameManager gameManager, VillageLevelUp data)
    {
        if (data.Experience > 0)
            gameManager.Village.SetExp(data.Experience);

        if (data.LevelDelta > 0)
        {
            UnityEngine.Debug.Log($"Congratulations! Village gained {data.LevelDelta} level(s)!");
            gameManager.Village.SetTierByLevel(data.Level);
            gameManager.Village.SetSlotCount(data.HouseSlots);
        }
    }
}
