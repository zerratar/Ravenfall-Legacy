using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NavMeshHelper
{
    private static readonly Dictionary<string, Vector3> samplePositions = new Dictionary<string, Vector3>();

    // islands with a navmesh of their own (the mangrove island): its own area, which agents there keep to and every
    // other agent stays off. The world's navmesh still has old scraps on such an island, walkable but joined to
    // nothing; an agent placed on one would be stuck.
    private static readonly List<(Vector2 centre, float radius, int area)> ownAreas = new List<(Vector2, float, int)>();
    private static int ownAreasMask;

    public static void AddOwnArea(Vector3 centre, float radius, int area)
    {
        ownAreas.RemoveAll(x => x.area == area); // statics outlive play sessions without a domain reload
        ownAreas.Add((new Vector2(centre.x, centre.z), radius, area));
        ownAreasMask |= 1 << area;
        samplePositions.Clear();
    }

    /// <summary>The navmesh areas an agent at a position walks on.</summary>
    public static int AreaMaskAt(Vector3 position)
    {
        for (var i = 0; i < ownAreas.Count; i++)
        {
            var a = ownAreas[i];
            if ((new Vector2(position.x, position.z) - a.centre).sqrMagnitude <= a.radius * a.radius)
                return 1 << a.area;
        }
        return NavMesh.AllAreas & ~ownAreasMask;
    }

    /// <summary>Gives an agent the areas of where it is, placing it again when they change.</summary>
    public static void UseAreasAt(NavMeshAgent agent, Vector3 position)
    {
        var mask = AreaMaskAt(position);
        if (agent.areaMask == mask) return;
        agent.areaMask = mask;
        if (agent.enabled && agent.isOnNavMesh) agent.Warp(position);
    }

    public static bool SamplePosition(Vector3 position, float maxDistance, out Vector3 sample)
    {
        var x = Math.Round(position.x, 4);
        var y = Math.Round(position.y, 4);
        var z = Math.Round(position.z, 4);
        var key = $"{x}_{y}_{z}";
        if (samplePositions.TryGetValue(key, out sample))
        {
            return true;
        }

        if (NavMesh.SamplePosition(position, out var hit, maxDistance, AreaMaskAt(position)))
        {
            sample = hit.position;

            samplePositions[key] = sample;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Like SamplePosition, but only accepts navmesh within maxHeightDifference of the position.
    /// NavMesh.SamplePosition measures mostly sideways, so above water it can return the seabed
    /// straight below instead of the dock a few meters away. When the closest hit is too far up
    /// or down, rings of probes around the position look for navmesh at the right height.
    /// </summary>
    public static bool SampleNearHeight(Vector3 position, float maxDistance, float maxHeightDifference, out Vector3 sample)
    {
        var areas = AreaMaskAt(position);
        if (NavMesh.SamplePosition(position, out var hit, maxDistance, areas)
            && Mathf.Abs(hit.position.y - position.y) <= maxHeightDifference)
        {
            sample = hit.position;
            return true;
        }

        for (var radius = 1f; radius <= 16f; radius *= 2f)
        {
            var found = false;
            var bestDistance = float.MaxValue;
            sample = position;
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI / 4f;
                var probe = position + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (!NavMesh.SamplePosition(probe, out hit, 1f, areas)) continue;
                if (Mathf.Abs(hit.position.y - position.y) > maxHeightDifference) continue;
                var distance = (hit.position - position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    sample = hit.position;
                    found = true;
                }
            }

            if (found) return true;
        }

        sample = position;
        return false;
    }
}
