using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;

public class RockController : MonoBehaviour
{
    public IslandController Island { get; private set; }
    public bool IsInvalid { get; internal set; }

    [ReadOnly]
    public float MaxActionDistance = 5;

    [Button("Adjust Placement")]
    public void AdjustPlacement()
    {
        PlacementUtility.PlaceOnGround(this.gameObject);
    }
    private float invalidTimer;
    private void Update()
    {
        if (IsInvalid)
        {
            invalidTimer += GameTime.deltaTime;
            if (invalidTimer >= 20)
            {
                invalidTimer = 0f;
                IsInvalid = false;
            }
        }
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