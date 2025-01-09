using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;

public class RockController : MonoBehaviour
{
    public IslandController Island;
    public Vector3 Position;

    [ReadOnly]
    public float MaxActionDistance = 5;

    [Button("Adjust Placement")]
    public void AdjustPlacement()
    {
        PlacementUtility.PlaceOnGround(this.gameObject);
    }

    void Start()
    {
        this.Position = transform.position;
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