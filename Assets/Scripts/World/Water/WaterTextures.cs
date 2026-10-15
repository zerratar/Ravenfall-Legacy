using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The two pages the ocean shader reads, drawn tileable once a session: the ripple normals (a heightfield of soft
/// swell with sharper small crests laid over it, turned into a normal map) and the noise page - R a fractal for the
/// foam's ragged edge, G a web of bright lines along cell borders for the caustics, B a second fractal at another
/// grain, A cell interiors for the bubbles in a whitecap. No assets; about a tenth of a second to draw, on threads.
/// </summary>
/// <remarks>Ported from FF3Unity.</remarks>
public static class WaterTextures
{
    public const int Size = 512;

    private static Texture2D normals, noise;

    /// <summary>The ripple normal map, tileable, linear, mipmapped.</summary>
    public static Texture2D Normals
    {
        get
        {
            if (normals) return normals;
            var px = DrawNormals();
            normals = new Texture2D(Size, Size, TextureFormat.RGBA32, true, true) { name = "Ocean ripples", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            normals.SetPixels32(px);
            normals.Apply(true, true);
            return normals;
        }
    }

    /// <summary>The foam and caustic noise page, tileable, linear, mipmapped.</summary>
    public static Texture2D Noise
    {
        get
        {
            if (noise) return noise;
            var px = DrawNoise();
            noise = new Texture2D(Size, Size, TextureFormat.RGBA32, true, true) { name = "Ocean foam and caustics", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            noise.SetPixels32(px);
            noise.Apply(true, true);
            return noise;
        }
    }

    private static Color32[] DrawNormals()
    {
        var n = new TileNoise(311);
        var h = new float[Size * Size];
        Parallel.For(0, Size, y =>
        {
            var v = y / (float)Size;
            for (var x = 0; x < Size; x++)
            {
                var u = x / (float)Size;
                var soft = n.Fbm(u, v, 5, 5, 0.55f);
                var ridged = 1f - Mathf.Abs(2f * n.Fbm(u + 0.31f, v + 0.77f, 13, 3, 0.5f) - 1f);
                h[y * Size + x] = soft * 0.72f + ridged * ridged * 0.28f;
            }
        });
        var px = new Color32[Size * Size];
        const float strength = 5.5f; // slope per texel of height, so the map has real relief at the scale the shader reads it
        Parallel.For(0, Size, y =>
        {
            int y0 = (y - 1 + Size) % Size, y1 = (y + 1) % Size;
            for (var x = 0; x < Size; x++)
            {
                int x0 = (x - 1 + Size) % Size, x1 = (x + 1) % Size;
                var dx = (h[y * Size + x1] - h[y * Size + x0]) * 0.5f * strength;
                var dy = (h[y1 * Size + x] - h[y0 * Size + x]) * 0.5f * strength;
                var nrm = new Vector3(-dx, -dy, 1f).normalized;
                px[y * Size + x] = new Color32(B(nrm.x * 0.5f + 0.5f), B(nrm.y * 0.5f + 0.5f), B(nrm.z * 0.5f + 0.5f), 255);
            }
        });
        return px;
    }

    private static Color32[] DrawNoise()
    {
        var n = new TileNoise(977);
        var px = new Color32[Size * Size];
        Parallel.For(0, Size, y =>
        {
            var v = y / (float)Size;
            for (var x = 0; x < Size; x++)
            {
                var u = x / (float)Size;
                // R: the foam's ragged fractal
                var foam = n.Fbm(u, v, 4, 5, 0.5f);
                // G: caustics - cell borders as bright thin lines, the cells warped a little so the web is not a grid
                float wu = u + (n.Fbm(u, v, 3, 2) - 0.5f) * 0.06f, wv = v + (n.Fbm(u + 0.5f, v + 0.5f, 3, 2) - 0.5f) * 0.06f;
                var (f1, f2) = n.Nearest(wu, wv, 7);
                var web = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((f2 - f1) / 0.22f));
                var caustic = Mathf.Pow(web, 1.6f) * 0.85f + n.Fbm(u + 0.2f, v + 0.9f, 9, 2) * 0.15f;
                // B: a second fractal at another grain, for the rolling line to break against (and the wave groups)
                var second = n.Fbm(u + 0.41f, v + 0.13f, 7, 4, 0.5f);
                // A: bubbles - cell interiors, rounded, over a fractal so they do not pack into a honeycomb
                var (b1, _) = n.Nearest(u + 0.27f, v + 0.61f, 9);
                var blob = 1f - Mathf.Clamp01(b1 * 1.3f);
                var bubbles = blob * blob * 0.6f + n.Fbm(u + 0.66f, v + 0.21f, 11, 3, 0.55f) * 0.4f;
                px[y * Size + x] = new Color32(B(foam), B(caustic), B(second), B(bubbles));
            }
        });
        return px;
    }

    private static byte B(float v) => (byte)Mathf.RoundToInt(Mathf.Clamp01(v) * 255f);
}
