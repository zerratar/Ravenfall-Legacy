using UnityEditor;
using UnityEngine;

/// <summary>
/// Where trees and gather spots stand: the terrain, or a rock, path or other ground mesh. Old trees, players,
/// buildings, water and roads are not ground, so painting under or next to them lands on what is beneath.
/// </summary>
public static class ResourceGround
{
    private static int mask;
    private static bool maskMade;

    private static int Mask
    {
        get
        {
            if (!maskMade)
            {
                mask = ~LayerMask.GetMask("Ignore Raycast", "Water", "UI", "Player", "Tree", "Ferry", "Torch", "Building", "Enemy", "Volume", "TavernDisplay");
                maskMade = true;
            }
            return mask;
        }
    }

    /// <summary>The ground under the mouse in the Scene view.</summary>
    public static bool UnderMouse(Vector2 mouse, out RaycastHit ground)
    {
        var hits = Physics.RaycastAll(HandleUtility.GUIPointToWorldRay(mouse), 10000f, Mask, QueryTriggerInteraction.Ignore);
        ground = default;
        var best = float.MaxValue;
        foreach (var hit in hits)
        {
            if (hit.distance >= best || IsNotGround(hit.collider)) continue;
            best = hit.distance;
            ground = hit;
        }
        return best < float.MaxValue;
    }

    /// <summary>The ground at a point's x and z, looking from a little above it: the highest ground in reach.</summary>
    public static bool Below(Vector3 near, out RaycastHit ground)
    {
        var hits = Physics.RaycastAll(new Ray(near + Vector3.up * 30f, Vector3.down), 90f, Mask, QueryTriggerInteraction.Ignore);
        ground = default;
        var best = float.NegativeInfinity;
        foreach (var hit in hits)
        {
            if (hit.point.y <= best || IsNotGround(hit.collider)) continue;
            best = hit.point.y;
            ground = hit;
        }
        return !float.IsNegativeInfinity(best);
    }

    /// <summary>A point moved onto the ground, or left as it is when there is none in reach.</summary>
    public static Vector3 Snap(Vector3 point) => Below(point, out var ground) ? ground.point : point;

    private static bool IsNotGround(Collider collider) =>
        collider.GetComponentInParent<WorldRoad>() || collider.GetComponentInParent<TreeController>() || collider.GetComponentInParent<GatherController>();
}
