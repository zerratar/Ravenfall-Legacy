using UnityEngine;

[CreateAssetMenu(fileName = "New Town House", menuName = "Game/Town House")]
public class TownHouse : ScriptableObject
{
    public int Type;
    public string Name;
    public string Description;

    public int BuildWoodCost;
    public int BuildStoneCost;

    public GameObject Prefab;
}
