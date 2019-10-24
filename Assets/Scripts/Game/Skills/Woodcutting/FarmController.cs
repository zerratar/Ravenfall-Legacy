using UnityEngine;

public class FarmController : MonoBehaviour
{
    public int Level = 1;
    public decimal Experience => GameMath.GetFishingExperience(Level);
    public int Resource => 1;

    public bool Farm(PlayerController player)
    {
        return true;
    }
}