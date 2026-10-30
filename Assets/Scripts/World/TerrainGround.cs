using UnityEngine;

/// <summary>
/// The terrain under a point. Where terrains overlap (Ravebridge's island terrain lies over the sea floor of the two
/// big terrains round it), the one whose ground is highest there: the others are under it.
/// </summary>
public static class TerrainGround
{
    /// <summary>The terrain under a point in x and z, or null where there is none.</summary>
    public static Terrain At(Vector3 point)
    {
        Terrain best = null;
        var bestHeight = float.NegativeInfinity;
        foreach (var terrain in Terrain.activeTerrains)
        {
            if (!terrain || !terrain.terrainData) continue;
            var origin = terrain.transform.position;
            var size = terrain.terrainData.size;
            if (point.x < origin.x || point.z < origin.z || point.x > origin.x + size.x || point.z > origin.z + size.z) continue;
            var height = terrain.SampleHeight(point) + origin.y;
            if (height > bestHeight) { best = terrain; bestHeight = height; }
        }
        return best;
    }

    /// <summary>The ground's height under a point, from the terrain there; false where there is no terrain.</summary>
    public static bool TryHeight(Vector3 point, out float height)
    {
        var terrain = At(point);
        height = terrain ? terrain.SampleHeight(point) + terrain.transform.position.y : 0f;
        return terrain;
    }
}
