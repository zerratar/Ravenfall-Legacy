using System;
using UnityEngine;

/// <summary>
/// Gradient noise that repeats on the unit square: its lattice wraps at the period, so every octave tiles and so
/// does their sum. A cellular (Worley) distance on the same wrapping grid, for pebbles, leaves and bubbles.
/// Deterministic from the seed. For textures drawn at runtime (the sea's ripples and foam, ground surfaces).
/// </summary>
/// <remarks>Ported from FF3Unity's ground textures.</remarks>
public sealed class TileNoise
{
    private readonly int[] perm = new int[512];

    public TileNoise(int seed)
    {
        var p = new int[256];
        for (var i = 0; i < 256; i++) p[i] = i;
        var rng = new System.Random(seed * 7919 + 17);
        for (var i = 255; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (p[i], p[j]) = (p[j], p[i]);
        }
        for (var i = 0; i < 512; i++) perm[i] = p[i & 255];
    }

    private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static readonly float[] Gx = { 1, -1, 1, -1, 1, -1, 0, 0 }, Gy = { 1, 1, -1, -1, 0, 0, 1, -1 };
    private static float Grad(int hash, float x, float y) { var h = hash & 7; return Gx[h] * x + Gy[h] * y; }
    private int Hash(int x, int y) => perm[perm[x & 255] + (y & 255)];

    /// <summary>Perlin noise in 0..1 over u, v in 0..1, with <paramref name="period"/> lattice cells across the square.</summary>
    public float Value(float u, float v, int period)
    {
        float x = u * period, y = v * period;
        int X = (int)Math.Floor(x), Y = (int)Math.Floor(y);
        x -= X; y -= Y;
        X = ((X % period) + period) % period; Y = ((Y % period) + period) % period;
        int X1 = (X + 1) % period, Y1 = (Y + 1) % period;
        float fu = Fade(x), fv = Fade(y);
        float n00 = Grad(Hash(X, Y), x, y), n10 = Grad(Hash(X1, Y), x - 1, y), n01 = Grad(Hash(X, Y1), x, y - 1), n11 = Grad(Hash(X1, Y1), x - 1, y - 1);
        var n = Mathf.Lerp(Mathf.Lerp(n00, n10, fu), Mathf.Lerp(n01, n11, fu), fv);
        return Mathf.Clamp01(n * 0.71f + 0.5f);
    }

    /// <summary>Fractal sum in 0..1; <paramref name="frequency"/> is the coarsest octave's cells across the square (rounded so it tiles).</summary>
    public float Fbm(float u, float v, float frequency, int octaves, float gain = 0.5f)
    {
        var period = Mathf.Max(1, Mathf.RoundToInt(frequency));
        float sum = 0f, amp = 1f, norm = 0f;
        for (var i = 0; i < octaves; i++)
        {
            sum += (Value(u, v, period) - 0.5f) * amp; norm += amp;
            amp *= gain; period *= 2;
        }
        // the sum of octaves rarely reaches its bounds; stretched so the range is used
        return Mathf.Clamp01(sum / norm * 0.5f / 0.35f + 0.5f);
    }

    /// <summary>Distance (in cells) to the nearest of one jittered point per cell on a wrapping grid, and that cell's own 0..1 id.</summary>
    public (float distance, float id) Cellular(float u, float v, int cells)
    {
        float x = u * cells, y = v * cells;
        int X = (int)Math.Floor(x), Y = (int)Math.Floor(y);
        float best = 9f, bestId = 0f;
        for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                int cx = (((X + dx) % cells) + cells) % cells, cy = (((Y + dy) % cells) + cells) % cells;
                var h = Hash(cx, cy);
                float px = X + dx + (h & 15) / 15f, py = Y + dy + ((h >> 4) & 15) / 15f;
                float ddx = px - x, ddy = py - y;
                var d = ddx * ddx + ddy * ddy;
                if (d < best) { best = d; bestId = perm[h + 1] / 255f; }
            }
        return (Mathf.Sqrt(best), bestId);
    }

    /// <summary>Distances (in cells) to the nearest two jittered points on a wrapping grid: Worley noise that tiles.</summary>
    public (float first, float second) Nearest(float u, float v, int cells)
    {
        float x = (u - Mathf.Floor(u)) * cells, y = (v - Mathf.Floor(v)) * cells;
        int X = (int)Math.Floor(x), Y = (int)Math.Floor(y);
        float best = 9f, next = 9f;
        for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                int cx = (((X + dx) % cells) + cells) % cells, cy = (((Y + dy) % cells) + cells) % cells;
                var h = (uint)Hash(cx, cy) * 2654435761u;
                h ^= h >> 15;
                float px = X + dx + (h & 1023) / 1023f, py = Y + dy + ((h >> 10) & 1023) / 1023f;
                float ddx = px - x, ddy = py - y;
                var d = ddx * ddx + ddy * ddy;
                if (d < best) { next = best; best = d; }
                else if (d < next) next = d;
            }
        return (Mathf.Sqrt(best), Mathf.Sqrt(next));
    }
}
