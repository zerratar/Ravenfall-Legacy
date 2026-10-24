using Sirenix.Serialization.Utilities;
using System;
using UnityEngine;
using UnityEngine.Rendering;

public class DayNightCycle : MonoBehaviour
{
    private static readonly int atmosphereThicknessID = Shader.PropertyToID("_AtmosphereThickness");
    // how much it is night, 0 by day to 1 in the middle of the night, for shaders (the ocean darkens with it)
    private static readonly int nightID = Shader.PropertyToID("_RavenfallNight");

    /// <summary>
    /// The setting: how bright the nights are, 0 for the old pitch dark (the scene's night light and ambient) to 1
    /// for a full moon. The night light in the scene sits below the horizon and is very dim, so by itself nothing
    /// but the torches could be seen at night, players included. The moon rides high and cool, and the night's
    /// ambient gets a floor that only ever lifts it: the day is unchanged.
    /// </summary>
    public static float NightBrightness { get; set; } = 0.75f;

    private static readonly Quaternion MoonRotation = Quaternion.Euler(55f, 200f, 0f);
    private const float MoonIntensity = 0.5f;
    private const float MoonTemperature = 9000f;
    private static readonly Color MoonColor = new Color(0.62f, 0.72f, 0.95f);
    private static readonly Color MoonSky = new Color(0.15f, 0.20f, 0.36f);
    private static readonly Color MoonEquator = new Color(0.20f, 0.24f, 0.34f);
    private static readonly Color MoonGround = new Color(0.15f, 0.15f, 0.16f);
    // how much of the moonlight a cloudy sky can take: under a storm the sun's 0.26 left the nights black even at full
    // brightness, so by night the weather only takes part of it
    private const float WeatherAtNight = 0.45f;


    [SerializeField] public GameManager gameManager;

    [SerializeField] public Light DayLight;
    [SerializeField] public Light NightLight;

    [SerializeField, GradientUsage(true)] public Gradient SkyColor;
    [SerializeField, GradientUsage(true)] public Gradient EquatorColor;
    [SerializeField, GradientUsage(true)] public Gradient GroundColor;

    [SerializeField] public AnimationCurve DayNight;

    [SerializeField] public AnimationCurve RealDayNightSpring;
    [SerializeField] public AnimationCurve RealDayNightSummer;
    [SerializeField] public AnimationCurve RealDayNightAutum;
    [SerializeField] public AnimationCurve RealDayNightWinter;

    [SerializeField] public float DayAtmosphere = 0.9f;
    [SerializeField] public float NightAtmosphere = 0.2f;

    [SerializeField] public Color NightFogColor;
    private Color DayFogColor;

    public const float TimeOfDay_Night = 270;
    public float CycleLength = 420f;

    public float TotalTime;
    public float Cycle;
    public float CycleProgress;

    private float freezeTimer;

    private Light skyLight;
    private float shadowStrength = -1f;
    private Material skyboxMaterial;
    //private float daytimeHoursNormalized = 1f / 24f;

    public bool IsNight
    {
        get
        {
            return CycleProgress >= 0.5f;
        }
    }

    /// <summary>How much it is day, 0 at night to 1 at noon, whatever the weather does to the sun.</summary>
    public float Daylight { get; private set; }

    public bool UseRealTime { get; set; }
    public bool IsEnabled { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        skyLight = RenderSettings.sun;
        skyboxMaterial = RenderSettings.skybox;
        if (RenderSettings.fog)
        {
            DayFogColor = RenderSettings.fogColor;
        }
    }

    internal void SetTimeOfDay(float totalTime, float freezeTime)
    {
        this.freezeTimer = freezeTime;
        this.TotalTime = totalTime;
    }

    // Update is called once per frame
    void Update()
    {
        Weather.Tick(Time.deltaTime);
        var skylightLerpValue = 0f;

        if (IsEnabled)
        {
            var cycle = 0f;
            var cycleProgress = 0f;

            if (freezeTimer > 0)
            {
                freezeTimer -= GameTime.deltaTime;
            }
            else if (gameManager.PotatoMode)
            {
                TotalTime = 0;
                skyLight.shadows = LightShadows.None;
            }
            else
            {
                skyLight.shadows = LightShadows.Soft;
                TotalTime += GameTime.deltaTime;
            }

            if (UseRealTime && freezeTimer <= 0)
            {
                const float secondsPerDay = 86400f;
                cycle = (float)(DateTime.Now - DateTime.Now.Date).TotalSeconds / secondsPerDay;
                cycleProgress = cycle - Mathf.Floor(cycle);
                skylightLerpValue = GetSeasonalCurve().Evaluate(cycleProgress);
            }
            else
            {
                cycle = TotalTime / CycleLength;
                cycleProgress = cycle - Mathf.Floor(cycle);
                skylightLerpValue = DayNight.Evaluate(cycleProgress);
            }

            Cycle = cycle;
            CycleProgress = cycleProgress;
        }
        else
        {
            skylightLerpValue = DayNight.Evaluate(CycleProgress);
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        Daylight = skylightLerpValue;
        Shader.SetGlobalFloat(nightID, 1f - skylightLerpValue);

        // the night end, as bright as the setting has it; eased, so most of the slider is a usable night and only the
        // far left end is the old pitch dark
        var moon = Mathf.Sqrt(Mathf.Clamp01(NightBrightness));
        var skyFloor = Color.Lerp(SkyColor.Evaluate(0f), MoonSky, moon);
        var equatorFloor = Color.Lerp(EquatorColor.Evaluate(0f), MoonEquator, moon);
        var groundFloor = Color.Lerp(GroundColor.Evaluate(0f), MoonGround, moon);

        // under a grey sky its light goes grey
        RenderSettings.ambientSkyColor = Greyed(Max(SkyColor.Evaluate(skylightLerpValue), skyFloor), Weather.Grey);
        RenderSettings.ambientEquatorColor = Greyed(Max(EquatorColor.Evaluate(skylightLerpValue), equatorFloor), Weather.Grey * 0.7f);
        RenderSettings.ambientGroundColor = Max(GroundColor.Evaluate(skylightLerpValue), groundFloor);

        var nightIntensity = Mathf.Lerp(NightLight.intensity, MoonIntensity, moon);
        var nightTemperature = Mathf.Lerp(NightLight.colorTemperature, MoonTemperature, moon);
        var nightColor = Color.Lerp(NightLight.color, MoonColor, moon);
        var nightRotation = Quaternion.Slerp(NightLight.transform.rotation, MoonRotation, moon);

        var weatherLight = Mathf.Lerp(Mathf.Lerp(1f, Weather.Sunlight, WeatherAtNight), Weather.Sunlight, skylightLerpValue);
        skyLight.intensity = Mathf.Lerp(nightIntensity, DayLight.intensity, skylightLerpValue) * weatherLight;
        if (shadowStrength < 0f) shadowStrength = skyLight.shadowStrength;
        skyLight.shadowStrength = shadowStrength * Weather.Shadows;
        skyLight.colorTemperature = Mathf.Lerp(nightTemperature, DayLight.colorTemperature, skylightLerpValue);
        skyLight.color = Color.Lerp(nightColor, DayLight.color, skylightLerpValue);
        skyLight.transform.rotation = Quaternion.Lerp(nightRotation, DayLight.transform.rotation, skylightLerpValue);

        skyboxMaterial.SetFloat(atmosphereThicknessID, Mathf.Lerp(NightAtmosphere, DayAtmosphere, skylightLerpValue));

        if (RenderSettings.fog)
        {
            RenderSettings.fogColor = Color.Lerp(NightFogColor, DayFogColor, skylightLerpValue);
        }
    }

    private static Color Max(Color a, Color b) => new Color(Mathf.Max(a.r, b.r), Mathf.Max(a.g, b.g), Mathf.Max(a.b, b.b), a.a);

    private static Color Greyed(Color colour, float amount)
    {
        if (amount <= 0f) return colour;
        var grey = colour.r * 0.3f + colour.g * 0.55f + colour.b * 0.15f;
        return Color.Lerp(colour, new Color(grey, grey, grey * 1.04f, colour.a), amount);
    }

    private AnimationCurve GetSeasonalCurve()
    {
        var now = DateTime.Now;
        if (now.Month >= 11 || now.Month <= 2)
            return RealDayNightWinter;
        if (now.Month >= 3 && now.Month <= 4)
            return RealDayNightSpring;
        if (now.Month >= 5 && now.Month <= 8)
            return RealDayNightSummer;
        return RealDayNightAutum;
    }
}