using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NavMeshHelper
{
    private static readonly Dictionary<string, Vector3> samplePositions = new Dictionary<string, Vector3>();

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

        if (NavMesh.SamplePosition(position, out var hit, maxDistance, NavMesh.AllAreas))
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
        if (NavMesh.SamplePosition(position, out var hit, maxDistance, NavMesh.AllAreas)
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
                if (!NavMesh.SamplePosition(probe, out hit, 1f, NavMesh.AllAreas)) continue;
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
