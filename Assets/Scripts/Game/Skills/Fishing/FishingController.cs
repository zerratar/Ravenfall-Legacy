using UnityEngine;

public class FishingController : MonoBehaviour
{
    [SerializeField] private Transform rotationTarget;

    public int Level = 1;
    public double Experience => GameMath.GetFishingExperience(Level);
    public double Resource => 1;
    public Transform LookTransform => !!rotationTarget ? rotationTarget : transform;

    public IslandController Island { get; private set; }

    public float MaxActionDistance = 5;
    void Start()
    {
        this.Island = GetComponentInParent<IslandController>();
        var collider = GetComponent<CapsuleCollider>();
        if (collider)
        {
            MaxActionDistance = collider.radius;
        }
    }
    public bool Fish(PlayerController player)
    {
        var proc = player.Stats.Fishing.CurrentValue / Level;
        return proc * UnityEngine.Random.value >= 0.5f;
    }
}
