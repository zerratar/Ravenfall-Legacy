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
}