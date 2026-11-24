using UnityEngine;

/// <summary>
///     Sets the tower's look (<see cref="TowerManager.ApplyLook"/>) after every camera has moved this frame: the game's
///     cameras move in LateUpdate, and the look goes by where the camera is. Set before the camera moved, the frame of
///     a cut between an island and a tower place was drawn in the other one's light (2026-11-24). On the tower
///     manager's own object.
/// </summary>
[DefaultExecutionOrder(10000)]
public sealed class TowerLook : MonoBehaviour
{
    private TowerManager tower;

    private void Awake() => tower = GetComponent<TowerManager>();

    private void LateUpdate()
    {
        if (tower) tower.ApplyLook();
    }
}
