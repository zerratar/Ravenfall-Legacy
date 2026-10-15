using UnityEngine;

/// <summary>
/// The weather's sounds, made rather than recorded, so there is nothing to import: thunder, near (a crack, then the
/// roll) or far (a low rumble swelling in), and a loop of rain. Each is noise, filtered and shaped.
/// </summary>
/// <remarks>
/// Thunder is brown noise (white noise summed, so it is mostly low) through a low-pass, under an envelope of a few
/// swells at random, the way a roll of thunder comes back off the clouds and the hills; near thunder starts with a
/// crack of bright noise and crackle. The rain is hiss with drops ticking in it, and its end is faded into its start
/// so it loops without a seam.
/// </remarks>
public static class WeatherSounds
{
    private const int Rate = 22050;

    /// <summary>A roll of thunder, near or far; a different one for each seed.</summary>
    public static AudioClip Thunder(bool near, int seed)
    {
        var random = new System.Random(seed);
        var seconds = near ? 7f : 8f;
        var count = (int)(seconds * Rate);
        var samples = new float[count];

        // the roll's swells: when, how wide, how loud
        var swells = near ? 7 : 6;
        var at = new float[swells];
        var width = new float[swells];
        var height = new float[swells];
        var t0 = near ? 0.05f : 0.6f;
        for (var i = 0; i < swells; ++i)
        {
            // the first at once, the rest spread over the roll, more of them early
            at[i] = i == 0 ? t0 : t0 + Mathf.Pow((float)random.NextDouble(), 1.5f) * (seconds * 0.6f);
            width[i] = near ? Mathf.Lerp(0.35f, 1.1f, (float)random.NextDouble()) : Mathf.Lerp(0.6f, 1.6f, (float)random.NextDouble());
            height[i] = Mathf.Lerp(0.45f, 1f, (float)random.NextDouble()) * (i == 0 ? 1f : 0.85f);
        }

        var cutoff = near ? 260f : 140f;
        var lowpass = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate);
        var crackPass = 1f - Mathf.Exp(-2f * Mathf.PI * 1800f / Rate);
        float brown = 0f, low = 0f, low2 = 0f, crackLow = 0f, peak = 1e-4f;
        for (var n = 0; n < count; ++n)
        {
            var t = n / (float)Rate;
            var white = (float)(random.NextDouble() * 2.0 - 1.0);
            brown = brown * 0.996f + white * 0.06f;
            low += lowpass * (brown - low);
            low2 += lowpass * (low - low2);

            var envelope = 0f;
            for (var i = 0; i < swells; ++i)
            {
                var d = (t - at[i]) / width[i];
                envelope += height[i] * (d < 0f ? Mathf.Exp(-d * d * 6f) : Mathf.Exp(-d * 1.6f));
            }
            envelope *= Mathf.Exp(-t * (near ? 0.22f : 0.18f));
            var s = low2 * envelope;

            if (near && t < 0.6f)
            {
                // the crack: bright noise (white less its low part), and crackle, gone in a few tenths of a second
                crackLow += crackPass * (white - crackLow);
                var crack = (white - crackLow) * Mathf.Exp(-t * 14f) * 0.55f;
                if (random.NextDouble() < 0.004 * Mathf.Exp(-t * 6f)) crack += (float)(random.NextDouble() * 2.0 - 1.0) * 0.8f;
                s += crack;
            }
            // a little fade in, so it never clicks on; far thunder swells in slowly
            s *= Mathf.Clamp01(t / (near ? 0.01f : 0.8f));
            samples[n] = s;
            peak = Mathf.Max(peak, Mathf.Abs(s));
        }
        var gain = 0.9f / peak;
        for (var n = 0; n < count; ++n) samples[n] *= gain;
        // and out at the end
        var tail = Rate / 4;
        for (var n = 0; n < tail; ++n) samples[count - 1 - n] *= n / (float)tail;

        var clip = AudioClip.Create(near ? "Thunder (near)" : "Thunder (far)", count, 1, Rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>A few seconds of rain to loop: a soft hiss, drops ticking in it.</summary>
    public static AudioClip Rain(int seed)
    {
        var random = new System.Random(seed);
        var length = 4 * Rate;
        var fade = Rate / 4;
        var made = new float[length + fade];
        var hissPass = 1f - Mathf.Exp(-2f * Mathf.PI * 3200f / Rate);
        var bodyPass = 1f - Mathf.Exp(-2f * Mathf.PI * 600f / Rate);
        float hiss = 0f, body = 0f, drop = 0f, dropDecay = 0f, peak = 1e-4f;
        for (var n = 0; n < made.Length; ++n)
        {
            var white = (float)(random.NextDouble() * 2.0 - 1.0);
            hiss += hissPass * (white - hiss);
            body += bodyPass * (white - body);
            // a drop now and then: a short click, dying fast
            if (random.NextDouble() < 260.0 / Rate)
            {
                drop = (float)(random.NextDouble() * 0.8 + 0.2) * (random.NextDouble() < 0.5 ? -1f : 1f);
                dropDecay = Mathf.Lerp(0.82f, 0.95f, (float)random.NextDouble());
            }
            drop *= dropDecay;
            var s = hiss * 0.55f + body * 0.5f + drop * 0.25f;
            made[n] = s;
            peak = Mathf.Max(peak, Mathf.Abs(s));
        }

        var samples = new float[length];
        var gain = 0.7f / peak;
        for (var n = 0; n < length; ++n)
        {
            var s = made[n];
            // the start mixed with what runs on past the end, so the loop meets itself
            if (n < fade) s = Mathf.Lerp(made[length + n], s, n / (float)fade);
            samples[n] = s * gain;
        }
        var clip = AudioClip.Create("Rain", length, 1, Rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
