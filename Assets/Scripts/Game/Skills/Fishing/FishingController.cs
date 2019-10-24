using UnityEngine;

public class FishingController : MonoBehaviour
{
    [SerializeField] private Transform rotationTarget;

    public int Level = 1;
    public decimal Experience => GameMath.GetFishingExperience(Level);
    public decimal Resource => 1;
    public Transform LookTransform => !!rotationTarget ? rotationTarget : transform;

    public bool Fish(PlayerController player)
    {
        var proc = player.Stats.Fishing.CurrentValue / Level;
        return proc * UnityEngine.Random.value >= 0.5f;
    }
}
