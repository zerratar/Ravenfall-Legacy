using UnityEngine;

/// <summary>
/// The weather over the islands, changing on its own: clear spells, fair weather with a few clouds, cloudy and
/// overcast skies, rain, heavy rain and thunderstorms, each lasting a while and drifting into the next over a few
/// minutes. It sets how much of the sky the volumetric clouds cover (<see cref="SkyClouds.Coverage"/>) and how dark
/// they are, how much sunlight gets through, how hard the shadows are, how hazy the air is, how hard it rains and how
/// wet the ground is; <see cref="WeatherEffects"/> draws the rain and the lightning. With the changing weather
/// setting; held at fair weather while that is off, in potato mode (<see cref="Suppressed"/>) and while the
/// volumetric clouds are off, as there would be no clouds to see.
/// </summary>
/// <remarks>
/// Ticked by <see cref="DayNightCycle"/>, which puts the light on the sun. A day is seven minutes, so a spell of
/// weather lasts a few days. Each kind tends to turn into a neighbour, so a clear sky clouds over by way of fair
/// weather, rain comes out of an overcast sky and a storm clears by way of rain. A thunderstorm takes longer to come
/// in than the rest: the clouds darken as it nears, and its lightning flashes far off before it strikes close by.
/// </remarks>
public static class Weather
{
    public enum Kind { Clear, Fair, Cloudy, Overcast, Rain, HeavyRain, Thunderstorm }

    private static readonly int OvercastId = Shader.PropertyToID("_RavenfallOvercast");
    private static readonly int WetnessId = Shader.PropertyToID("_RavenfallWetness");

    private readonly struct Look
    {
        public readonly float Coverage, Sunlight, Shadows, Haze, Grey, Dark, Rain, Storm;
        public readonly float MinMinutes, MaxMinutes, ComeInSeconds;

        public Look(float coverage, float sunlight, float shadows, float haze, float grey, float dark, float rain, float storm,
            float minMinutes, float maxMinutes, float comeInSeconds)
        {
            Coverage = coverage; Sunlight = sunlight; Shadows = shadows; Haze = haze; Grey = grey; Dark = dark; Rain = rain; Storm = storm;
            MinMinutes = minMinutes; MaxMinutes = maxMinutes; ComeInSeconds = comeInSeconds;
        }
    }

    // how much of the sky is cloud, the sunlight let through, the shadows' strength, the haze, how grey the sky's
    // light goes, how dark the clouds are, how hard it rains, how stormy it is (lightning), how long it lasts
    // (minutes) and how long it takes to come in (seconds)
    private static readonly Look[] Looks =
    {
        new Look(0.03f, 1.04f, 1f, 0.85f, 0f, 0f, 0f, 0f, 8f, 20f, 180f), // clear
        new Look(0.3f, 1f, 1f, 1f, 0f, 0f, 0f, 0f, 12f, 30f, 180f), // fair
        new Look(0.5f, 0.86f, 0.85f, 1.15f, 0.2f, 0.05f, 0f, 0f, 8f, 20f, 180f), // cloudy
        new Look(0.82f, 0.55f, 0.5f, 1.5f, 0.55f, 0.15f, 0f, 0f, 5f, 12f, 180f), // overcast
        new Look(0.88f, 0.45f, 0.4f, 1.5f, 0.65f, 0.35f, 0.45f, 0.1f, 4f, 10f, 120f), // rain
        new Look(0.93f, 0.34f, 0.3f, 1.8f, 0.72f, 0.55f, 1f, 0.35f, 3f, 7f, 120f), // heavy rain
        new Look(0.95f, 0.26f, 0.25f, 1.7f, 0.78f, 0.8f, 0.85f, 1f, 3f, 7f, 300f), // thunderstorm
    };

    // what tends to follow what: a row for each kind, the chances of each kind after it, in the order of Kind
    private static readonly float[,] Follows =
    {
        { 0f, 0.75f, 0.25f, 0f, 0f, 0f, 0f },
        { 0.4f, 0f, 0.5f, 0.1f, 0f, 0f, 0f },
        { 0.1f, 0.45f, 0f, 0.3f, 0.15f, 0f, 0f },
        { 0.05f, 0.4f, 0f, 0f, 0.4f, 0f, 0.15f },
        { 0f, 0f, 0.25f, 0.35f, 0f, 0.25f, 0.15f },
        { 0f, 0f, 0f, 0.15f, 0.6f, 0f, 0.25f },
        { 0f, 0f, 0f, 0.15f, 0.45f, 0.4f, 0f },
    };

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

    /// <summary>How dark the clouds are, 0 (white) to 1 (a thunderhead).</summary>
    public static float Dark { get; private set; }

    /// <summary>How hard it rains, 0 to 1.</summary>
    public static float Rain { get; private set; }

    /// <summary>How stormy it is, 0 to 1: how near the lightning is, and how often it comes.</summary>
    public static float Storm { get; private set; }

    /// <summary>How wet the ground is, 0 to 1: it wets in a minute or so of rain and dries over a few.</summary>
    public static float Wetness { get; private set; }

    /// <summary>The light of a lightning flash on the land just now, 0 for none (set by <see cref="WeatherEffects"/>).</summary>
    public static float Flash { get; set; }

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
        Grey = Dark = Rain = Storm = Wetness = Flash = 0f;
        SkyClouds.Coverage = Looks[(int)Kind.Fair].Coverage;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        // the sea and the ground in the editor should not stay grey and wet after play
        PlaySession.Ending += () =>
        {
            Shader.SetGlobalFloat(OvercastId, 0f);
            Shader.SetGlobalFloat(WetnessId, 0f);
        };
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
            Put(Looks[(int)Kind.Fair], Looks[(int)Kind.Fair], 0f, deltaTime);
            return;
        }

        if (Coming != Current)
        {
            change += deltaTime / Looks[(int)Coming].ComeInSeconds;
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

        Put(Looks[(int)Current], Looks[(int)Coming], change, deltaTime);
    }

    private static void Begin()
    {
        random = new System.Random();
        // a session never opens in rain or under an overcast sky
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

    /// <summary>
    /// The weather partway from one kind to the next. The new weather comes in as a front from upwind, across the sky
    /// (<see cref="SkyClouds.FrontAt"/>): far off at first, so the change is seen coming, overhead halfway, and gone
    /// far downwind by the end, past where the clouds are drawn. The light, the rain and the haze change as the front
    /// goes over; the lightning comes before it, far off.
    /// </summary>
    private static void Put(Look from, Look to, float change, float deltaTime)
    {
        // how far upwind the front is: fast far off, slow overhead, about 5.7 km at either end
        var x = 1f - 2f * change;
        var reach = 1667f * x + 4000f * x * x * x;
        var upwind = -SkyClouds.Wind.normalized;
        var eye = Ambience.HasFocus ? Ambience.FocusPoint : Camera.main ? Camera.main.transform.position : Vector3.zero;
        SkyClouds.FrontAt = new Vector2(eye.x, eye.z) + upwind * reach;
        // how far it has come over the focus, 0 to 1
        var t = Mathf.Clamp01(-reach / SkyClouds.FrontWidth + 0.5f);
        t = t * t * (3f - 2f * t);

        // the clouds come and go a little within a spell too
        var drift = Changing ? (Mathf.PerlinNoise(Time.time * 0.004f, 0.37f) - 0.5f) * 0.08f : 0f;
        SkyClouds.Coverage = Mathf.Clamp01(from.Coverage + drift);
        SkyClouds.FrontCoverage = Mathf.Clamp01(to.Coverage + drift);
        SkyClouds.FrontDarker = (1f - 0.6f * to.Dark) / (1f - 0.6f * from.Dark);
        Sunlight = Mathf.Lerp(from.Sunlight, to.Sunlight, t);
        Shadows = Mathf.Lerp(from.Shadows, to.Shadows, t);
        HazeAmount = Mathf.Lerp(from.Haze, to.Haze, t);
        Grey = Mathf.Lerp(from.Grey, to.Grey, t);
        // the clouds' own darkness is this side's; beyond the front they are FrontDarker
        Dark = from.Dark;
        // the rain starts late in the change and stops early, so it falls from a sky already dark
        Rain = Mathf.Lerp(from.Rain, to.Rain, to.Rain > from.Rain ? t * t : 1f - (1f - t) * (1f - t));
        // a storm's lightning is seen and heard long before it is overhead
        var early = change * change * (3f - 2f * change);
        Storm = Mathf.Lerp(from.Storm, to.Storm, early);

        var wet = Mathf.Min(1f, Rain * 1.5f);
        Wetness = Mathf.MoveTowards(Wetness, wet, deltaTime / (wet > Wetness ? 60f : 240f));

        Shader.SetGlobalFloat(OvercastId, Mathf.Clamp01(Grey / Looks[(int)Kind.Overcast].Grey));
        Shader.SetGlobalFloat(WetnessId, Wetness);
    }
}
