using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;

public class RockController : MonoBehaviour
{
    public int Level;
    public double Experience => GameMath.GetWoodcuttingExperience(Level);
    public double Resource => 1;

    public IslandController Island { get; private set; }

    [ReadOnly]
    public float MaxActionDistance = 5;

    [Button("Adjust Placement")]
    public void AdjustPlacement()
    {
        PlacementUtility.PlaceOnGround(this.gameObject);
    }

    void Start()
    {
        this.Island = GetComponentInParent<IslandController>();
        var collider = GetComponent<SphereCollider>();
        if (collider)
        {
            MaxActionDistance = collider.radius;
        }
    }
    public bool Mine(PlayerController player)
    {
        return true;
    }
}