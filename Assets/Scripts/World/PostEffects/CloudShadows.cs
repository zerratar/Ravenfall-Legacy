using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Shadows of clouds drifting over the islands, as the sun's cookie. With the volumetric clouds on, they are the
/// clouds' own shadows (<see cref="SkyClouds.DrawShadows"/>); without them, a soft, tiling noise texture moved along
/// with the wind. On unless the setting turns it off; potato mode keeps it off (<see cref="Suppressed"/>).
/// </summary>
/// <remarks>
/// The cookie only dims the sun's light where a cloud passes; the ambient light stays, so a cloud's shadow looks like
/// any other shadow, only larger and softer. The cost is one texture read wherever the sun lights a pixel. The texture
/// is made here when first needed (256 x 256, one channel), so there is no asset, and the scene's sun is left as it
/// was when the effect is off.
/// </remarks>
public static class CloudShadows
{
    private const int Size = 256;

    /// <summary>One tile of the clouds, in metres.</summary>
    private const float TileMetres = 360f;

    /// <summary>How fast and where the clouds drift, in metres a second.</summary>
    private static readonly Vector2 Wind = new Vector2(1.6f, 0.7f);

    private static Texture2D cookie;
    private static Light sun;
    private static UniversalAdditionalLightData sunData;
    private static float strength = 0.7f;

    /// <summary>The setting: cloud shadows on or off.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>Off for now (potato mode), whatever the setting.</summary>
    public static bool Suppressed { get; set; }

    /// <summary>How much of the sun a cloud takes away at its thickest, 0 to 1.</summary>
    public static float Strength
    {
        get => strength;
        set
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(value, strength)) return;
            strength = value;
            if (cookie) Fill(cookie);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        RenderPipelineManager.beginContextRendering -= BeforeFrame;
        RenderPipelineManager.beginContextRendering += BeforeFrame;
    }

    private static void BeforeFrame(ScriptableRenderContext context, List<Camera> cameras)
    {
        var light = RenderSettings.sun;
        if (light != sun)
        {
            Release();
            sun = light;
            sunData = sun ? sun.GetUniversalAdditionalLightData() : null;
        }
        if (!sun) return;

        if (!Enabled || Suppressed)
        {
            Release();
            return;
        }

        // with the volumetric clouds on, the shadows are theirs, drawn from the clouds overhead
        if (SkyClouds.DrawShadows(context, sun, sunData, strength)) return;

        if (!cookie) cookie = Make();
        if (sun.cookie != cookie) sun.cookie = cookie;

        // from the clock rather than added up each frame, as this runs once per render context; the offset wraps
        // at a tile so it stays small
        var t = Time.time;
        sunData.lightCookieSize = new Vector2(TileMetres, TileMetres);
        sunData.lightCookieOffset = new Vector2(Mathf.Repeat(Wind.x * t, TileMetres), Mathf.Repeat(Wind.y * t, TileMetres));
    }

    /// <summary>Takes the cookie off the sun, if it is ours or the clouds'.</summary>
    private static void Release()
    {
        if (!sun || !sun.cookie) return;
        if ((cookie && sun.cookie == cookie) || (SkyClouds.ShadowMap && sun.cookie == SkyClouds.ShadowMap)) sun.cookie = null;
    }

    private static Texture2D Make()
    {
        var texture = new Texture2D(Size, Size, GraphicsFormat.R8_UNorm, TextureCreationFlags.MipChain)
        {
            name = "Cloud Shadows",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 2,
            hideFlags = HideFlags.HideAndDontSave
        };
        Fill(texture);
        return texture;
    }

    /// <summary>
    /// Clouds from tiling value noise, four octaves on lattices that wrap around, so it tiles; the noise is
    /// then cut into patches with soft edges, so there is clear sky between the clouds.
    /// </summary>
    private static void Fill(Texture2D texture)
    {
        var pixels = new byte[Size * Size];
        for (var y = 0; y < Size; ++y)
        {
            for (var x = 0; x < Size; ++x)
            {
                var n = 0f;
                var weight = 0.5f;
                var total = 0f;
                for (var cells = 3; cells <= 24; cells *= 2)
                {
                    n += weight * Noise(x * cells / (float)Size, y * cells / (float)Size, cells);
                    total += weight;
                    weight *= 0.5f;
                }
                n /= total;
                var cloud = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 0.7f, n));
                pixels[y * Size + x] = (byte)Mathf.RoundToInt(255f * (1f - strength * cloud));
            }
        }
        texture.SetPixelData(pixels, 0);
        texture.Apply(true, false);
    }

    /// <summary>Smooth value noise on a lattice of <paramref name="cells"/> cells that wraps around.</summary>
    private static float Noise(float x, float y, int cells)
    {
        var x0 = Mathf.FloorToInt(x);
        var y0 = Mathf.FloorToInt(y);
        var fx = x - x0;
        var fy = y - y0;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        var a = Hash(x0, y0, cells);
        var b = Hash(x0 + 1, y0, cells);
        var c = Hash(x0, y0 + 1, cells);
        var d = Hash(x0 + 1, y0 + 1, cells);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    private static float Hash(int x, int y, int cells)
    {
        x = ((x % cells) + cells) % cells;
        y = ((y % cells) + cells) % cells;
        var h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)cells * 2246822519u;
        h = (h ^ (h >> 13)) * 1274126177u;
        h ^= h >> 16;
        return (h & 0xffff) / 65535f;
    }
}
