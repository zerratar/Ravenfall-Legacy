using Sirenix.OdinInspector;
using UnityEngine;
public class FarmController : MonoBehaviour
{
    public int Level = 1;
    //public double Experience => GameMath.GetFishingExperience(Level);
    public int Resource => 1;

    public bool IsInvalid { get; internal set; }

    public float MaxActionDistance = 5;

    public IslandController Island;
    void Start()
    {
        this.Island = GetComponentInParent<IslandController>();
        var collider = GetComponent<SphereCollider>();
        if (collider)
        {
            MaxActionDistance = collider.radius;
        }
    }
    public bool Farm(PlayerController player)
    {
        return true;
    }

    [Button("Adjust Placement")]
    public void AdjustPlacement()
    {
        PlacementUtility.PlaceOnGround(this.gameObject);
    }
}
