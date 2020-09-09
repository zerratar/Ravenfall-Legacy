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

    [SerializeField] public float DayAtmosphere = 0.9f;
    [SerializeField] public float NightAtmosphere = 0.2f;

    public float CycleLength = 420f;

    public float TotalTime;
    public float Cycle;
    public float CycleProgress;

    private Light skyLight;
    private Material skyboxMaterial;

    public bool IsNight
    {
        get
        {
            return CycleProgress >= 0.5f;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        skyLight = RenderSettings.sun;
        skyboxMaterial = RenderSettings.skybox;
    }

    // Update is called once per frame
    void Update()
    {
        TotalTime += Time.deltaTime;

        var cycle = TotalTime / CycleLength;
        var cycleProgress = cycle - Mathf.Floor(cycle);

        Cycle = cycle;
        CycleProgress = cycleProgress;
        var skylightLerpValue = DayNight.Evaluate(cycleProgress);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = SkyColor.Evaluate(skylightLerpValue);
        RenderSettings.ambientEquatorColor = EquatorColor.Evaluate(skylightLerpValue);
        RenderSettings.ambientGroundColor = GroundColor.Evaluate(skylightLerpValue);

        skyLight.intensity = Mathf.Lerp(NightLight.intensity, DayLight.intensity, skylightLerpValue);
        skyLight.colorTemperature = Mathf.Lerp(NightLight.colorTemperature, DayLight.colorTemperature, skylightLerpValue);
        skyLight.color = Color.Lerp(NightLight.color, DayLight.color, skylightLerpValue);
        skyLight.transform.rotation = Quaternion.Lerp(NightLight.transform.rotation, DayLight.transform.rotation, skylightLerpValue);

        skyboxMaterial.SetFloat(atmosphereThicknessID, Mathf.Lerp(NightAtmosphere, DayAtmosphere, skylightLerpValue));

        if (gameManager.PotatoMode)
        {
            TotalTime = 0;
            skyLight.shadows = LightShadows.None;
        }
        else
        {
            skyLight.shadows = LightShadows.Soft;
        }
    }
}