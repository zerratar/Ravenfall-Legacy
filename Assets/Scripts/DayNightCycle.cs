using Sirenix.Serialization.Utilities;
using System;
using UnityEngine;
using UnityEngine.Rendering;

public class DayNightCycle : MonoBehaviour
{
    private static readonly int atmosphereThicknessID = Shader.PropertyToID("_AtmosphereThickness");


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
        // under a grey sky its light goes grey
        RenderSettings.ambientSkyColor = Greyed(SkyColor.Evaluate(skylightLerpValue), Weather.Grey);
        RenderSettings.ambientEquatorColor = Greyed(EquatorColor.Evaluate(skylightLerpValue), Weather.Grey * 0.7f);
        RenderSettings.ambientGroundColor = GroundColor.Evaluate(skylightLerpValue);

        skyLight.intensity = Mathf.Lerp(NightLight.intensity, DayLight.intensity, skylightLerpValue) * Weather.Sunlight;
        if (shadowStrength < 0f) shadowStrength = skyLight.shadowStrength;
        skyLight.shadowStrength = shadowStrength * Weather.Shadows;
        skyLight.colorTemperature = Mathf.Lerp(NightLight.colorTemperature, DayLight.colorTemperature, skylightLerpValue);
        skyLight.color = Color.Lerp(NightLight.color, DayLight.color, skylightLerpValue);
        skyLight.transform.rotation = Quaternion.Lerp(NightLight.transform.rotation, DayLight.transform.rotation, skylightLerpValue);

        skyboxMaterial.SetFloat(atmosphereThicknessID, Mathf.Lerp(NightAtmosphere, DayAtmosphere, skylightLerpValue));

        if (RenderSettings.fog)
        {
            RenderSettings.fogColor = Color.Lerp(NightFogColor, DayFogColor, skylightLerpValue);
        }
    }

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