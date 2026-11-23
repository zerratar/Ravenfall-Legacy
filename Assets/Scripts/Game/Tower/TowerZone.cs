using UnityEngine;

/// <summary>
///     One room or area of a floor's route (<see cref="TowerRoute"/>, docs/tower-design.md section 22): where the group
///     arrives and gets back up while it is the room they are in, the spots its monsters stand on, and the gate that
///     opens to the next room once they have all fallen. The last room of a route is the boss's: no monsters, no gate.
/// </summary>
public sealed class TowerZone : MonoBehaviour
{
    /// <summary>What the builder calls it ("Entry Hall"); for the logs and the tests, not shown to players.</summary>
    public string Title = "";

    /// <summary>Where players arrive in this room (a newcomer while the group is here, someone back up after a fall).</summary>
    public Transform Entry;

    /// <summary>Where the room's monsters stand: every child is one spot.</summary>
    public Transform Spawns;

    /// <summary>The way on to the next room, opened when this one is cleared; none in the last room.</summary>
    public TowerGate Exit;

    public int SpotCount => Spawns ? Spawns.childCount : 0;

    public Vector3 Arrival => Entry ? Entry.position : transform.position;
}
