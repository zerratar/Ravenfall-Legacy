using UnityEngine;

/// <summary>
/// The weather over the islands, changing on its own: clear spells, fair weather with a few clouds, cloudy skies and
/// overcast ones, each lasting a while and drifting into the next over a few minutes. It sets how much of the sky the
/// volumetric clouds cover (<see cref="SkyClouds.Coverage"/>), how much sunlight gets through, how hard the shadows are
/// and how hazy the air is. With the changing weather setting; held at fair weather while that is off, in potato mode
/// (<see cref="Suppressed"/>) and while the volumetric clouds are off, as there would be no clouds to see.
/// </summary>
/// <remarks>
/// Ticked by <see cref="DayNightCycle"/>, which puts the light on the sun. A day is seven minutes, so a spell of
/// weather lasts a few days. Each kind tends to turn into its neighbour, so a clear sky clouds over by way of fair
/// weather and an overcast one clears by way of cloudy.
/// </remarks>
public static class Weather
{
    public enum Kind { Clear, Fair, Cloudy, Overcast }

    private static readonly int OvercastId = Shader.PropertyToID("_RavenfallOvercast");

    private readonly struct Look
    {
        public readonly float Coverage, Sunlight, Shadows, Haze, Grey;
        public readonly float MinMinutes, MaxMinutes;

        public Look(float coverage, float sunlight, float shadows, float haze, float grey, float minMinutes, float maxMinutes)
        {
            Coverage = coverage; Sunlight = sunlight; Shadows = shadows; Haze = haze; Grey = grey;
            MinMinutes = minMinutes; MaxMinutes = maxMinutes;
        }
    }

    // how much of the sky is cloud, the sunlight let through, the shadows' strength, the haze, how grey the sky's
    // light goes, and how long it lasts (minutes)
    private static readonly Look[] Looks =
    {
        new Look(0.03f, 1.04f, 1f, 0.85f, 0f, 8f, 20f), // clear
        new Look(0.3f, 1f, 1f, 1f, 0f, 12f, 30f), // fair
        new Look(0.5f, 0.86f, 0.85f, 1.15f, 0.2f, 8f, 20f), // cloudy
        new Look(0.82f, 0.55f, 0.5f, 1.5f, 0.55f, 5f, 12f), // overcast
    };

    // what tends to follow what: a row for each kind, the chances of clear, fair, cloudy, overcast after it
    private static readonly float[,] Follows =
    {
        { 0f, 0.75f, 0.25f, 0f },
        { 0.4f, 0f, 0.5f, 0.1f },
        { 0.1f, 0.5f, 0f, 0.4f },
        { 0f, 0.2f, 0.8f, 0f },
    };

    /// <summary>How long one kind of weather takes to turn into the next, seconds.</summary>
    private const float ChangeSeconds = 180f;

    /// <summary>The setting: the weather changes, or it stays fair.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>Held at fair for now (potato mode), whatever the setting.</summary>
    public static bool Suppressed { get; set; }

    /// <summary>The weather is changing on its own: the setting is on, and the volumetric clouds are there to show it.</summary>
    public static bool Changing => Enabled && !Suppressed && SkyClouds.Active;

    /// <summary>The weather now, or the one it is turning from.</summary>
    public static Kind Current { get; private set; } = Kind.Fair;

    /// <summary>The weather it is turning into; the same as <see cref="Current"/> while it holds.</summary>
    public static Kind Coming { get; private set; } = Kind.Fair;

    /// <summary>The share of the sunlight that gets through, about 1 on a fair day.</summary>
    public static float Sunlight { get; private set; } = 1f;

    /// <summary>How strong the shadows are against a fair day's, 0 to 1.</summary>
    public static float Shadows { get; private set; } = 1f;

    /// <summary>How hazy the air is against a fair day's.</summary>
    public static float HazeAmount { get; private set; } = 1f;

    /// <summary>How grey the sky's light goes under the cloud, 0 to 1.</summary>
    public static float Grey { get; private set; }

    private static float holdLeft = -1f;
    private static float change;
    private static System.Random random;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Current = Coming = Kind.Fair;
        holdLeft = -1f;
        change = 0f;
        random = null;
        Sunlight = Shadows = HazeAmount = 1f;
        Grey = 0f;
        SkyClouds.Coverage = Looks[(int)Kind.Fair].Coverage;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        // the sea in the editor should not stay grey after play
        PlaySession.Ending += () => Shader.SetGlobalFloat(OvercastId, 0f);
    }

    /// <summary>Turns the weather to the given kind over the usual few minutes, then lets it carry on from there.</summary>
    public static void Bring(Kind kind)
    {
        if (holdLeft < 0f) Begin();
        if (Coming != Current) Current = Coming;
        Coming = kind;
        change = 0f;
    }

    /// <summary>Moves the weather on; called each frame by the day and night cycle.</summary>
    public static void Tick(float deltaTime)
    {
        if (holdLeft < 0f) Begin();
        if (!Changing)
        {
            Put(Looks[(int)Kind.Fair], Looks[(int)Kind.Fair], 0f);
            return;
        }

        if (Coming != Current)
        {
            change += deltaTime / ChangeSeconds;
            if (change >= 1f)
            {
                Current = Coming;
                change = 0f;
                holdLeft = Lasting(Current);
            }
        }
        else
        {
            holdLeft -= deltaTime;
            if (holdLeft <= 0f) Coming = Pick(Current);
        }

        var t = change * change * (3f - 2f * change);
        Put(Looks[(int)Current], Looks[(int)Coming], t);
    }

    private static void Begin()
    {
        random = new System.Random();
        // a session never opens under an overcast sky
        var roll = random.NextDouble();
        Current = Coming = roll < 0.3 ? Kind.Clear : roll < 0.75 ? Kind.Fair : Kind.Cloudy;
        holdLeft = Lasting(Current);
        change = 0f;
    }

    private static float Lasting(Kind kind)
    {
        var look = Looks[(int)kind];
        return Mathf.Lerp(look.MinMinutes, look.MaxMinutes, (float)random.NextDouble()) * 60f;
    }

    private static Kind Pick(Kind after)
    {
        var roll = (float)random.NextDouble();
        for (var i = 0; i < Looks.Length; ++i)
        {
            roll -= Follows[(int)after, i];
            if (roll <= 0f) return (Kind)i;
        }
        return Kind.Fair;
    }

    private static void Put(Look from, Look to, float t)
    {
        // the clouds come and go a little within a spell too
        var drift = Changing ? (Mathf.PerlinNoise(Time.time * 0.004f, 0.37f) - 0.5f) * 0.08f : 0f;
        SkyClouds.Coverage = Mathf.Clamp01(Mathf.Lerp(from.Coverage, to.Coverage, t) + drift);
        Sunlight = Mathf.Lerp(from.Sunlight, to.Sunlight, t);
        Shadows = Mathf.Lerp(from.Shadows, to.Shadows, t);
        HazeAmount = Mathf.Lerp(from.Haze, to.Haze, t);
        Grey = Mathf.Lerp(from.Grey, to.Grey, t);
        Shader.SetGlobalFloat(OvercastId, Grey / Looks[(int)Kind.Overcast].Grey);
    }
}
