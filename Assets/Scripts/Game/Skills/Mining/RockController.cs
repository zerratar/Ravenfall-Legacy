using UnityEngine;

public class RockController : MonoBehaviour
{
    public int Level;
    public decimal Experience => GameMath.GetWoodcuttingExperience(Level);
    public decimal Resource => 1;

    public bool Mine(PlayerController player)
    {
        return true;
    }
}