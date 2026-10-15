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

    [Tooltip("Where smoke rises from the chimney, in the house's own space (VillageLife); none when zero.")]
    public Vector3 Chimney;

    [Tooltip("The colour of the chimney's smoke.")]
    public Color ChimneySmoke = new Color(0.62f, 0.62f, 0.64f, 1f);
}
