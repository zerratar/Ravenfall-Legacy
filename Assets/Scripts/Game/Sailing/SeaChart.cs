using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Where own boats may sail (docs/own-boats.md, phase 2): the water grid and the islands' dock points baked by
/// tools/sailing/bake-sea-chart.cs into StreamingAssets/Sailing/sea-chart.json. A route is the shortest way over the
/// grid, kept a little off the coasts, then pulled straight wherever open water allows, so a boat sails a few long
/// legs instead of a staircase.
/// </summary>
public static class SeaChart
{
    public struct Dock
    {
        public string Island;
        public Vector3 Position;
        /// <summary>The way a boat faces when it comes alongside.</summary>
        public float Heading;
    }

    private static bool loaded;
    private static float cell, originX, originZ;
    private static int width, height;
    private static bool[] water;
    private static byte[] coast; // cells next to land cost more, so routes keep off the beaches
    private static readonly List<Dock> docks = new List<Dock>();

    public static float Sea { get; private set; } = -3.31f;
    public static bool Ready => Load();

    private static bool Load()
    {
        if (loaded) return water != null;
        loaded = true;
        try
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Sailing", "sea-chart.json");
            var json = JObject.Parse(File.ReadAllText(path));
            cell = (float)json["cell"];
            originX = (float)json["originX"];
            originZ = (float)json["originZ"];
            width = (int)json["width"];
            height = (int)json["height"];
            Sea = (float)json["sea"];
            var bits = Convert.FromBase64String((string)json["water"]);
            water = new bool[width * height];
            for (var i = 0; i < water.Length; i++) water[i] = (bits[i >> 3] & (1 << (i & 7))) != 0;
            coast = new byte[water.Length];
            for (var z = 0; z < height; z++)
                for (var x = 0; x < width; x++)
                {
                    if (!water[z * width + x]) continue;
                    for (var dz = -1; dz <= 1 && coast[z * width + x] == 0; dz++)
                        for (var dx = -1; dx <= 1; dx++)
                            if (!Water(x + dx, z + dz)) { coast[z * width + x] = 1; break; }
                }
            foreach (var d in json["docks"])
                docks.Add(new Dock { Island = (string)d["island"], Position = new Vector3((float)d["x"], Sea, (float)d["z"]), Heading = (float)d["heading"] });
            return true;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("The sea chart could not be read, own boats cannot sail: " + exc.Message);
            water = null;
            return false;
        }
    }

    public static bool TryGetDock(string island, out Dock dock)
    {
        dock = default;
        if (!Load() || string.IsNullOrEmpty(island)) return false;
        foreach (var d in docks)
        {
            if (!d.Island.Equals(island, StringComparison.OrdinalIgnoreCase)) continue;
            dock = d;
            return true;
        }
        return false;
    }

    /// <summary>The dock point nearest a place at sea, of the islands that may be sailed to.</summary>
    public static bool TryGetNearestDock(Vector3 from, Func<string, bool> allowed, out Dock dock)
    {
        dock = default;
        if (!Load()) return false;
        var best = float.MaxValue;
        foreach (var d in docks)
        {
            if (allowed != null && !allowed(d.Island)) continue;
            var distance = Flat(d.Position - from).sqrMagnitude;
            if (distance >= best) continue;
            best = distance;
            dock = d;
        }
        return best < float.MaxValue;
    }

    public static bool IsWater(Vector3 at) => Load() && Water(CellX(at.x), CellZ(at.z));

    /// <summary>
    /// The way from one place to another over water, as the points to steer by: the start, the turns, the end. Null
    /// when there is none (no chart, or the end is cut off from the start).
    /// </summary>
    public static List<Vector3> Route(Vector3 from, Vector3 to)
    {
        if (!Load()) return null;
        if (!Nearest(CellX(from.x), CellZ(from.z), out var start) || !Nearest(CellX(to.x), CellZ(to.z), out var goal)) return null;

        var cells = AStar(start, goal);
        if (cells == null) return null;

        // pulled straight: from each kept point, on to the farthest cell still in open sight over water
        var route = new List<Vector3> { Flat(from, Sea) };
        var anchor = Centre(cells[0]);
        var i = 0;
        while (i < cells.Count - 1)
        {
            var next = i + 1;
            for (var j = cells.Count - 1; j > i + 1; j--)
            {
                if (!Clear(anchor, Centre(cells[j]))) continue;
                next = j;
                break;
            }
            anchor = Centre(cells[next]);
            route.Add(anchor);
            i = next;
        }
        route.Add(Flat(to, Sea));
        return route;
    }

    /// <summary>Somewhere to wander to: open water a few hundred metres off, that can be reached.</summary>
    public static List<Vector3> Wander(Vector3 from, System.Random random)
    {
        if (!Load()) return null;
        for (var attempt = 0; attempt < 24; attempt++)
        {
            var angle = (float)(random.NextDouble() * Math.PI * 2);
            var distance = 150f + (float)random.NextDouble() * 300f;
            var target = from + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * distance;
            var x = CellX(target.x); var z = CellZ(target.z);
            if (!Water(x, z) || coast[z * width + x] != 0) continue;
            var route = Route(from, target);
            if (route != null) return route;
        }
        return null;
    }

    // ---- the grid --------------------------------------------------------------------------------------

    private static int CellX(float x) => Mathf.FloorToInt((x - originX) / cell);
    private static int CellZ(float z) => Mathf.FloorToInt((z - originZ) / cell);
    private static bool Water(int x, int z) => x >= 0 && z >= 0 && x < width && z < height && water[z * width + x];
    private static Vector3 Centre(int index) => new Vector3(originX + (index % width + 0.5f) * cell, Sea, originZ + (index / width + 0.5f) * cell);
    private static Vector3 Flat(Vector3 v, float y = 0f) => new Vector3(v.x, y, v.z);

    /// <summary>The water cell nearest a cell (itself if it is water), within a few hundred metres.</summary>
    private static bool Nearest(int x, int z, out int index)
    {
        for (var r = 0; r < 40; r++)
        {
            var best = -1; var bestDistance = int.MaxValue;
            for (var dz = -r; dz <= r; dz++)
                for (var dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dz)) != r || !Water(x + dx, z + dz)) continue;
                    var d = dx * dx + dz * dz;
                    if (d < bestDistance) { bestDistance = d; best = (z + dz) * width + x + dx; }
                }
            if (best >= 0) { index = best; return true; }
        }
        index = -1;
        return false;
    }

    /// <summary>Whether a straight line stays over water (and off the coast cells), checked every half cell.</summary>
    private static bool Clear(Vector3 a, Vector3 b)
    {
        var steps = Mathf.CeilToInt(Flat(b - a).magnitude / (cell * 0.5f));
        for (var s = 1; s < steps; s++)
        {
            var p = Vector3.Lerp(a, b, s / (float)steps);
            var x = CellX(p.x); var z = CellZ(p.z);
            if (!Water(x, z) || coast[z * width + x] != 0) return false;
        }
        return true;
    }

    private static readonly int[] StepX = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] StepZ = { 0, 0, 1, -1, 1, -1, 1, -1 };

    private static List<int> AStar(int start, int goal)
    {
        var cost = new Dictionary<int, float> { [start] = 0f };
        var from = new Dictionary<int, int>();
        var open = new SortedSet<(float, int)> { (0f, start) };
        int gx = goal % width, gz = goal / width;
        while (open.Count > 0)
        {
            var current = open.Min;
            open.Remove(current);
            var index = current.Item2;
            if (index == goal)
            {
                var path = new List<int> { goal };
                while (from.TryGetValue(path[path.Count - 1], out var previous)) path.Add(previous);
                path.Reverse();
                return path;
            }
            int x = index % width, z = index / width;
            for (var k = 0; k < 8; k++)
            {
                int nx = x + StepX[k], nz = z + StepZ[k];
                if (!Water(nx, nz)) continue;
                if (k >= 4 && (!Water(x + StepX[k], z) || !Water(x, z + StepZ[k]))) continue; // no corner cutting
                var next = nz * width + nx;
                var step = (k >= 4 ? 1.414f : 1f) * (coast[next] != 0 ? 3f : 1f);
                var total = cost[index] + step;
                if (cost.TryGetValue(next, out var known) && known <= total) continue;
                if (cost.ContainsKey(next)) open.Remove((known + Heuristic(nx, nz, gx, gz), next));
                cost[next] = total;
                from[next] = index;
                open.Add((total + Heuristic(nx, nz, gx, gz), next));
            }
        }
        return null;
    }

    private static float Heuristic(int x, int z, int gx, int gz)
    {
        int dx = Math.Abs(x - gx), dz = Math.Abs(z - gz);
        return Math.Max(dx, dz) + 0.414f * Math.Min(dx, dz);
    }
}
