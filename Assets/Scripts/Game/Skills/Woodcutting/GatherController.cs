using Sirenix.OdinInspector;
using UnityEngine;

public class GatherController : MonoBehaviour
{
    public int Level = 1;
    //public double Experience => GameMath.GetFishingExperience(Level);
    public int Resource => 1;

    public float MaxActionDistance = 5;

    public bool IsDepleted { get; set; }

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

    public bool Gather(PlayerController player)
    {
        return true;
    }

    [Button("Adjust Placement")]
    public void AdjustPlacement()
    {
        PlacementUtility.PlaceOnGround(this.gameObject);
    }
}