using UnityEngine;

public class RockController : MonoBehaviour
{
    public int Level;
    public double Experience => GameMath.GetWoodcuttingExperience(Level);
    public double Resource => 1;

    public IslandController Island { get; private set; }

    public float MaxActionDistance = 5;
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