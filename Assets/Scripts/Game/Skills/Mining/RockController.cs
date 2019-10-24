using UnityEngine;

public class RockController : MonoBehaviour
{
    public RockType Type;

    public decimal Experience => GameMath.GetMiningExperienceFromType(Type);
    public decimal Resource => 1;

    public bool Mine(PlayerController player)
    {
        return true;
    }
}