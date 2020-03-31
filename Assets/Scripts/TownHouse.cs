using UnityEngine;

[CreateAssetMenu(fileName = "New Town House", menuName = "Game/Town House")]
public class TownHouse : ScriptableObject
{
    public TownHouseSlotType Type;
    public string Name;
    public string Description;

    public int BuildWoodCost;
    public int BuildStoneCost;

    public Vector3 CameraOffset;
    public float IconOffset;

    public GameObject Prefab;
}
