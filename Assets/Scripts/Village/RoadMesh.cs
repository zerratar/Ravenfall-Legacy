using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns the points a streamer placed into a road: a smooth curve through them, and a strip of the
/// road's width laid over the ground along it.
/// </summary>
/// <remarks>
/// The curve is a centripetal Catmull-Rom spline, which passes through every point and does not
/// loop or overshoot when points are close together or far apart. It is sampled every
/// <see cref="Step"/> metres; each sample gets three vertices across the road (both edges and the
/// middle) set on the terrain, so the road follows bumps rather than floating over or sinking into
/// them. The strip is wider than the road by <see cref="Shoulders"/>: the road shader fades the
/// surface out into the ground over the extra, so the solid part is as wide as the road. The first
/// texture coordinate is how far from the middle a vertex lies, 0 there and 1 at the edge, which
/// is what the shader blends by; the second runs along the road in road widths. Both ends are
/// rounded, so two roads that meet at a point overlap in a clean circle rather than in two square
/// corners.
/// </remarks>
public static class RoadMesh
{
    public const float Step = 0.5f;
    public const float Lift = 0.07f;
    /// <summary>How much wider than the road the strip is, for the edge to fade out over.</summary>
    public const float Shoulders = 1.45f;
    private const int CapSegments = 10;

    /// <summary>Points along the curve through <paramref name="points"/>, about <see cref="Step"/> apart.</summary>
    public static List<Vector2> Sample(IReadOnlyList<Vector2> points)
    {
        var samples = new List<Vector2>();
        if (points == null || points.Count == 0) return samples;
        if (points.Count == 1)
        {
            samples.Add(points[0]);
            return samples;
        }

        samples.Add(points[0]);
        for (var i = 0; i < points.Count - 1; ++i)
        {
            var p0 = i > 0 ? points[i - 1] : points[i] * 2f - points[i + 1];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = i + 2 < points.Count ? points[i + 2] : points[i + 1] * 2f - points[i];

            var steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(p1, p2) / Step));
            for (var s = 1; s <= steps; ++s)
            {
                samples.Add(CatmullRom(p0, p1, p2, p3, s / (float)steps));
            }
        }

        return samples;
    }

    /// <summary>The length of the curve, in metres.</summary>
    public static float Length(IReadOnlyList<Vector2> samples)
    {
        var length = 0f;
        for (var i = 1; i < samples.Count; ++i) length += Vector2.Distance(samples[i - 1], samples[i]);
        return length;
    }

    /// <summary>
    /// The road's strip along <paramref name="samples"/>, <paramref name="width"/> wide, with
    /// <paramref name="groundHeight"/> giving the terrain height under a point and
    /// <paramref name="lift"/> how far above it the road lies.
    /// </summary>
    public static void Build(Mesh mesh, IReadOnlyList<Vector2> samples, float width, Func<Vector3, float> groundHeight, float lift = Lift)
    {
        mesh.Clear();
        if (samples == null || samples.Count < 2) return;

        var count = samples.Count;
        var capVertices = CapSegments + 2;
        var vertices = new Vector3[count * 3 + capVertices * 2];
        var uvs = new Vector2[vertices.Length];
        var triangles = new int[(count - 1) * 12 + CapSegments * 3 * 2];
        var half = width * Shoulders / 2f;
        var along = 0f;

        for (var i = 0; i < count; ++i)
        {
            var previous = samples[Mathf.Max(0, i - 1)];
            var next = samples[Mathf.Min(count - 1, i + 1)];
            var tangent = (next - previous).normalized;
            var side = new Vector2(-tangent.y, tangent.x);
            if (i > 0) along += Vector2.Distance(samples[i - 1], samples[i]);

            for (var c = 0; c < 3; ++c)
            {
                var across = (c - 1) * half;
                var flat = samples[i] + side * across;
                var point = new Vector3(flat.x, 0f, flat.y);
                point.y = groundHeight(point) + lift;
                vertices[i * 3 + c] = point;
                uvs[i * 3 + c] = new Vector2(c == 1 ? 0f : 1f, along / width);
            }
        }

        var t = 0;
        for (var i = 0; i < count - 1; ++i)
        {
            for (var c = 0; c < 2; ++c)
            {
                var a = i * 3 + c;
                var b = a + 1;
                var d = a + 3;
                var e = d + 1;
                // clockwise seen from above, which Unity draws as the front
                triangles[t++] = a; triangles[t++] = b; triangles[t++] = d;
                triangles[t++] = b; triangles[t++] = e; triangles[t++] = d;
            }
        }

        // the round ends: a half disc of fan triangles past the first and the last sample
        void Cap(int at, float direction, float alongAt, int first)
        {
            var centre = samples[at];
            var other = samples[at == 0 ? 1 : count - 2];
            var outward = (centre - other).normalized;
            var side = new Vector2(outward.y, -outward.x) * direction;
            vertices[first] = Ground(centre);
            uvs[first] = new Vector2(0f, alongAt / width);
            for (var k = 0; k <= CapSegments; ++k)
            {
                var angle = Mathf.PI * k / CapSegments;
                var offset = (side * Mathf.Cos(angle) + outward * Mathf.Sin(angle)) * half;
                vertices[first + 1 + k] = Ground(centre + offset);
                // the rim is the road's edge all the way round, so the end fades out like the sides
                var forward = Vector2.Dot(offset, outward) * (at == 0 ? -1f : 1f);
                uvs[first + 1 + k] = new Vector2(1f, (alongAt + forward) / width);
            }

            for (var k = 0; k < CapSegments; ++k)
            {
                var a = first;
                var b = first + 1 + k;
                var d = first + 2 + k;
                // clockwise seen from above, as the strip is
                var ab = vertices[b] - vertices[a];
                var ad = vertices[d] - vertices[a];
                if (ab.z * ad.x - ab.x * ad.z < 0f) (b, d) = (d, b);
                triangles[t++] = a; triangles[t++] = b; triangles[t++] = d;
            }
        }

        Vector3 Ground(Vector2 flat)
        {
            var point = new Vector3(flat.x, 0f, flat.y);
            point.y = groundHeight(point) + lift;
            return point;
        }

        Cap(0, 1f, 0f, count * 3);
        Cap(count - 1, -1f, along, count * 3 + capVertices);

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    /// <summary>Distance from <paramref name="point"/> to the closest part of the curve.</summary>
    public static float DistanceTo(IReadOnlyList<Vector2> samples, Vector2 point, out int segment)
    {
        segment = -1;
        var best = float.MaxValue;
        for (var i = 1; i < samples.Count; ++i)
        {
            var d = DistanceToSegment(point, samples[i - 1], samples[i]);
            if (d < best)
            {
                best = d;
                segment = i - 1;
            }
        }
        return best;
    }

    public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
        return Vector2.Distance(p, a + ab * t);
    }

    // centripetal: alpha 0.5, through p1 and p2
    private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float Knot(float k, Vector2 a, Vector2 b) => k + Mathf.Max(0.0001f, Mathf.Sqrt(Vector2.Distance(a, b)));
        var t0 = 0f;
        var t1 = Knot(t0, p0, p1);
        var t2 = Knot(t1, p1, p2);
        var t3 = Knot(t2, p2, p3);
        var u = Mathf.Lerp(t1, t2, t);

        var a1 = (t1 - u) / (t1 - t0) * p0 + (u - t0) / (t1 - t0) * p1;
        var a2 = (t2 - u) / (t2 - t1) * p1 + (u - t1) / (t2 - t1) * p2;
        var a3 = (t3 - u) / (t3 - t2) * p2 + (u - t2) / (t3 - t2) * p3;
        var b1 = (t2 - u) / (t2 - t0) * a1 + (u - t0) / (t2 - t0) * a2;
        var b2 = (t3 - u) / (t3 - t1) * a2 + (u - t1) / (t3 - t1) * a3;
        return (t2 - u) / (t2 - t1) * b1 + (u - t1) / (t2 - t1) * b2;
    }

    // ---- Surfaces ----------------------------------------------------------------------------------
    // Only used when the road materials (Resources/Roads, on the island's terrain textures) are
    // missing: flat colours, so a road still shows.

    /// <summary>A tiling surface for road type <paramref name="type"/>: 0 dirt, 1 gravel, 2 cobbles.</summary>
    public static Texture2D MakeSurface(int type)
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            name = "Road surface " + type,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 4,
        };

        var pixels = new Color[size * size];
        var random = new System.Random(1337 + type);
        for (var y = 0; y < size; ++y)
        {
            for (var x = 0; x < size; ++x)
            {
                var u = x / (float)size;
                var v = y / (float)size;
                var colour = type == 2 ? Cobble(u, v) : type == 1 ? Gravel(u, v, random) : Dirt(u, v);
                // a darker band along both edges, worn and trodden, so the road has a border
                var fromEdge = Mathf.Min(u, 1f - u);
                var band = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fromEdge / 0.11f));
                colour *= Mathf.Lerp(type == 2 ? 0.62f : 0.74f, 1f, band);
                colour.a = 1f;
                pixels[y * size + x] = colour;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(true);
        return texture;
    }

    private static float Noise(float u, float v, float scale)
    {
        // tiles: the noise repeats every texture, as the road surface does
        var a = Mathf.PerlinNoise(u * scale, v * scale);
        var b = Mathf.PerlinNoise((u - 1f) * scale, v * scale);
        var c = Mathf.PerlinNoise(u * scale, (v - 1f) * scale);
        var d = Mathf.PerlinNoise((u - 1f) * scale, (v - 1f) * scale);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    private static Color Dirt(float u, float v)
    {
        var baseColor = new Color(0.55f, 0.42f, 0.29f);
        var n = Noise(u, v, 6f) * 0.6f + Noise(u, v, 18f) * 0.4f;
        var edge = Mathf.Abs(u - 0.5f) * 2f;
        // darker, worn ruts a third of the way in from each side
        var rut = Mathf.Exp(-Mathf.Pow((edge - 0.45f) * 8f, 2f)) * 0.08f;
        return baseColor * (0.88f + n * 0.22f - rut);
    }

    private static Color Gravel(float u, float v, System.Random random)
    {
        var baseColor = new Color(0.62f, 0.60f, 0.55f);
        var n = Noise(u, v, 10f);
        var speck = random.NextDouble() < 0.18 ? (float)(random.NextDouble() * 0.25 - 0.12) : 0f;
        return baseColor * (0.9f + n * 0.15f + speck);
    }

    private static Color Cobble(float u, float v)
    {
        // four rows of stones a tile, every other row shifted by half a stone
        const float rows = 4f;
        const float columns = 3f;
        var row = Mathf.Floor(v * rows);
        var shifted = u * columns + (row % 2f) * 0.5f;
        var cx = shifted - Mathf.Floor(shifted) - 0.5f;
        var cy = v * rows - row - 0.5f;
        var inside = Mathf.Max(Mathf.Abs(cx) * 1.15f, Mathf.Abs(cy));
        var mortar = inside > 0.43f;
        if (mortar) return new Color(0.30f, 0.28f, 0.26f);

        // each stone a slightly different grey, darker towards its edges
        var id = Mathf.Floor(shifted) * 7.3f + row * 3.1f;
        var tint = 0.85f + Mathf.Repeat(Mathf.Sin(id) * 43758.5f, 1f) * 0.25f;
        var shade = 1f - inside * 0.35f;
        return new Color(0.58f, 0.56f, 0.53f) * tint * shade;
    }
}
