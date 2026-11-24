using System;
using RavenNest.Combat;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
///     The tower as it stands in the main world (docs/world-tower.md, docs/tower-worlds-proposal.md section 6): a
///     colossal landmark on its own islet in the sea, a stack of sections that each look like a floor's world, lit by
///     the stream's progress, with the entrance plaza at its foot. It lives in its own small scene
///     (Assets/Scenes/Tower/WorldTower.unity, made by tools/tower/world/world-tower-build.cs), which
///     <see cref="WorldTowerScene"/> loads beside the main world, so MainWorld itself holds nothing of it.
///     <para>The light: a floor the stream has cleared glows steadily, the floor it is on glows and breathes, a floor
///     not reached is dark. It reads what the server last said (<see cref="TowerManager.State"/>) and nothing else:
///     before the server has answered, every floor is dark.</para>
///     <para>Seen from far: the game's view distance ends long before the tower, so its shader (Ravenfall/WorldTower)
///     draws it squeezed in front of the far plane and this gives its renderers bounds that are never culled. Past
///     <see cref="FarDistance"/> the low detail models show.</para>
///     <para>Potato mode: the low detail models at every distance, no breathing light, no drifting cloud, bobbing
///     rocks, lightning or gulls, no lamps; steady light stays. It needs no setting of its own: the cost while it is
///     in view is its triangles (docs/world-tower.md has the numbers).</para>
/// </summary>
public sealed class WorldTower : MonoBehaviour
{
    [Serializable]
    public sealed class Section
    {
        [Tooltip("The floor it shows (1, 2, 3...); 0 for the cloud cap and the foot.")]
        public int Floor;
        [Tooltip("What draws up close, and from far away or in potato mode.")]
        public Renderer[] Near = new Renderer[0];
        public Renderer[] Far = new Renderer[0];
        [Tooltip("Always lit, whatever the stream's progress: the gate and the plaza.")]
        public bool AlwaysLit;

        [NonSerialized] public Material Material;
        [NonSerialized] public float Glow;
        [NonSerialized] public float Flash;
    }

    public static WorldTower Instance { get; private set; }

    /// <summary>
    ///     For looking at the light without a server (the editor's captures): the step the stream would be on, or 0 for
    ///     what the server says. Never set in a build.
    /// </summary>
    public static int PreviewStep;

    [Tooltip("The material every piece is drawn with (Ravenfall/WorldTower); each section gets its own copy at the start.")]
    public Material Shared;
    public Section[] Sections = new Section[0];

    [Tooltip("Past this many metres from the camera the low detail models show.")]
    public float FarDistance = 950f;
    [Tooltip("Within this many metres the tower casts shadows.")]
    public float ShadowDistance = 260f;
    [Tooltip("Past this the tower is not drawn at all: the tower's own floors and towns lie far out at sea.")]
    public float HideDistance = 3600f;

    [Tooltip("Turned slowly round the tower: the storm's cloud ring, the cloud cap. Degrees a second each.")]
    public Transform[] Turning = new Transform[0];
    public float[] TurnSpeeds = new float[0];
    [Tooltip("Rocks adrift: they rise and sink a little.")]
    public Transform[] Bobbing = new Transform[0];
    [Tooltip("Gulls: each pivot is turned round the tower.")]
    public Transform[] Flocks = new Transform[0];
    [Tooltip("Lamps and what else costs frames without saying anything: off in potato mode.")]
    public GameObject Extras;
    [Tooltip("The easter egg by the gate (docs/easter-eggs.md): shown with the Silly surprises setting.")]
    public WorldGag Egg;

    [Tooltip("The leaderboard stone's words: where the stream is, and the floor's name.")]
    public TMPro.TextMeshPro SlabPlace;
    public TMPro.TextMeshPro SlabName;

    private static readonly int GlowId = Shader.PropertyToID("_Glow");
    private static readonly int PulseId = Shader.PropertyToID("_Pulse");
    private static readonly int AnimateId = Shader.PropertyToID("_WorldTowerAnimate");
    private static readonly int SeaId = Shader.PropertyToID("_WorldTowerSea");
    private static readonly int HazeId = Shader.PropertyToID("_WorldTowerHaze");
    private static readonly int HazeColorId = Shader.PropertyToID("_WorldTowerHazeColor");

    /// <summary>How much the floor the stream is on dims as it breathes.</summary>
    private const float Pulse = 0.6f;

    private Camera view;
    private bool? far;
    private bool? shadows;
    private bool? shown;
    private bool? extras;
    private bool? egg;
    private bool animate = true;
    private int step = -1;
    private int cleared = -1;
    private float nextLook;
    private Vector3[] bobFrom;

    private void Awake()
    {
        Instance = this;
        // never culled: the camera's far plane is nearer than the tower from most of the world
        var always = new Bounds(Vector3.zero, Vector3.one * 20000f);
        foreach (var section in Sections)
        {
            if (Shared)
            {
                section.Material = new Material(Shared) { name = Shared.name + " (floor " + section.Floor + ")", hideFlags = HideFlags.DontSave };
                section.Glow = section.AlwaysLit ? 1f : 0f;
                section.Material.SetFloat(GlowId, section.Glow);
                section.Material.SetFloat(PulseId, 0f);
            }
            foreach (var r in section.Near) Prepare(r, section.Material, always);
            foreach (var r in section.Far) Prepare(r, section.Material, always);
        }
        bobFrom = new Vector3[Bobbing.Length];
        for (var i = 0; i < Bobbing.Length; ++i) if (Bobbing[i]) bobFrom[i] = Bobbing[i].localPosition;
        Shader.SetGlobalFloat(SeaId, SeaChart.Sea);
        Shader.SetGlobalFloat(AnimateId, 1f);
        Words();
        Detail(true);
    }

    private static void Prepare(Renderer r, Material material, Bounds always)
    {
        if (!r) return;
        if (material) r.sharedMaterial = material;
        r.localBounds = always;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        foreach (var section in Sections) if (section.Material) Destroy(section.Material);
    }

    private void LateUpdate()
    {
        var now = Time.unscaledTime;
        if (now >= nextLook)
        {
            nextLook = now + 0.5f;
            Look();
        }
        Shine(Time.deltaTime);
        if (!animate || shown == false) return;
        var dt = Time.deltaTime;
        for (var i = 0; i < Turning.Length; ++i)
            if (Turning[i]) Turning[i].Rotate(0f, (i < TurnSpeeds.Length ? TurnSpeeds[i] : 1f) * dt, 0f, Space.Self);
        for (var i = 0; i < Bobbing.Length; ++i)
            if (Bobbing[i]) Bobbing[i].localPosition = bobFrom[i] + Vector3.up * (Mathf.Sin(Time.time * 0.35f + i * 1.7f) * 2.2f);
        for (var i = 0; i < Flocks.Length; ++i)
            if (Flocks[i]) Flocks[i].Rotate(0f, (5.5f + 1.3f * i) * (i % 2 == 0 ? 1f : -1f) * dt, 0f, Space.Self);
    }

    /// <summary>Twice a second: the stream's progress, the camera's distance, potato mode, the haze.</summary>
    private void Look()
    {
        var game = GameManager.Instance;
        var potato = game && game.PotatoMode;
        if (animate == potato)
        {
            animate = !potato;
            Shader.SetGlobalFloat(AnimateId, animate ? 1f : 0f);
        }

        if (!view || !view.isActiveAndEnabled) view = Camera.main;
        var eye = view ? view.transform.position : transform.position;
        var flat = eye - transform.position;
        flat.y = 0f;
        var distance = flat.magnitude;
        // not from the tower's own floors and towns, far out at sea, nor from down in a dungeon
        var show = distance < HideDistance && eye.y > -30f && GraphicsToggler.GraphicsEnabled;
        if (shown != show)
        {
            shown = show;
            for (var i = 0; i < transform.childCount; ++i) transform.GetChild(i).gameObject.SetActive(show);
            far = null;
            extras = null;
            egg = null;
        }
        if (!show) return;

        // the low detail models from far away (a band of 60 m so it does not flip back and forth) and in potato mode
        var wantFar = potato || distance > FarDistance + (far == true ? -30f : 30f);
        if (far != wantFar) Detail(!wantFar);
        var wantShadows = distance < ShadowDistance;
        if (shadows != wantShadows)
        {
            shadows = wantShadows;
            foreach (var section in Sections)
            {
                foreach (var r in section.Near) if (r) r.shadowCastingMode = wantShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                foreach (var r in section.Far) if (r) r.shadowCastingMode = wantShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
        }
        if (Extras && extras != !potato)
        {
            extras = !potato;
            Extras.SetActive(!potato);
        }
        if (Egg && egg != SettingsApplier.SillySurprises)
        {
            egg = SettingsApplier.SillySurprises;
            Egg.Show(egg.Value);
        }

        // the game's haze reads the squeezed depth; the shader adds the rest of the air with the same numbers
        var haze = Haze.Enabled && !Haze.Suppressed && view && view.cameraType == CameraType.Game;
        Shader.SetGlobalVector(HazeId, new Vector4(haze ? Haze.Density * Weather.HazeAmount : 0f, 1f / Mathf.Max(Haze.HeightScale, 1f), Haze.BaseHeight, Haze.StartDistance));
        var tri = RenderSettings.ambientMode == AmbientMode.Trilight;
        var sky = tri ? RenderSettings.ambientSkyColor.linear : RenderSettings.ambientLight.linear;
        var equator = tri ? RenderSettings.ambientEquatorColor.linear : RenderSettings.ambientLight.linear;
        Shader.SetGlobalVector(HazeColorId, Color.Lerp(sky, equator, 0.55f) * 0.95f);

        Progress();
    }

    private void Detail(bool near)
    {
        far = !near;
        foreach (var section in Sections)
        {
            foreach (var r in section.Near) if (r) r.enabled = near;
            foreach (var r in section.Far) if (r) r.enabled = !near;
        }
        foreach (var flock in Flocks) if (flock) flock.gameObject.SetActive(near);
    }

    /// <summary>What the server last said of the stream's climb: which floors are lit, and the stone's words.</summary>
    private void Progress()
    {
        var now = 0;
        if (Application.isEditor && PreviewStep > 0) now = PreviewStep;
        else
        {
            var tower = TowerManager.Instance;
            var state = tower ? tower.State : null;
            if (state != null) now = Mathf.Max(1, state.Floor);
        }
        if (now == step) return;
        var before = cleared;
        step = now;
        // nothing known yet: every floor dark
        cleared = now > 0 ? Tower.FloorsCleared(now - 1) : -1;
        // a floor that fell while the game was running flares up before it settles
        if (before >= 0 && cleared > before)
            foreach (var section in Sections)
                if (section.Floor > before && section.Floor <= cleared) section.Flash = 1f;
        Words();
    }

    /// <summary>The floor the stream is on, 0 when it has cleared them all or nothing is known.</summary>
    private int CurrentFloor => step > 0 && step <= Tower.TopFloor ? Tower.FloorOf(step) : 0;

    private void Shine(float dt)
    {
        var current = CurrentFloor;
        foreach (var section in Sections)
        {
            if (!section.Material || section.AlwaysLit) continue;
            var lit = section.Floor > 0 && (section.Floor <= cleared || section.Floor == current);
            var target = lit ? 1f : 0f;
            section.Glow = Mathf.MoveTowards(section.Glow, target, dt * 0.45f);
            section.Flash = Mathf.MoveTowards(section.Flash, 0f, dt * 0.3f);
            section.Material.SetFloat(GlowId, section.Glow * (1f + 1.6f * section.Flash));
            section.Material.SetFloat(PulseId, section.Floor == current ? Pulse : 0f);
        }
    }

    private void Words()
    {
        // "FLOOR 2" over "PART 4": two short lines can be twice as tall as one long one
        if (SlabPlace) SlabPlace.text = step > 0 ? TowerWords.Place(step).ToUpperInvariant().Replace(", ", "\n") : "";
        if (SlabName) SlabName.text = step > 0 ? TowerWords.FloorName(step) : "";
    }
}

/// <summary>
///     Loads the world tower's scene beside the main world when the game is up, in the editor from its path and in a
///     build by its name (it has to be in the build's scene list, as the tower's floors are). A build without it plays
///     as before, with no tower on the horizon.
/// </summary>
public static class WorldTowerScene
{
    public const string Name = "WorldTower";
    public const string Path = "Assets/Scenes/Tower/WorldTower.unity";

    private static bool loading;
    private static bool missingSaid;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        loading = false;
        missingSaid = false;
        SceneManager.sceneLoaded -= Loaded;
        SceneManager.sceneLoaded += Loaded;
        PlaySession.Ending += () => SceneManager.sceneLoaded -= Loaded;
        Ensure();
    }

    private static void Loaded(Scene scene, LoadSceneMode mode) => Ensure();

    private static void Ensure()
    {
        if (!Application.isPlaying || loading || WorldTower.Instance) return;
        // only beside the main world
        if (!UnityEngine.Object.FindAnyObjectByType<GameManager>()) return;
        if (SceneManager.GetSceneByName(Name).IsValid()) return;
        AsyncOperation op = null;
#if UNITY_EDITOR
        if (System.IO.File.Exists(Path))
            op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Path, new LoadSceneParameters(LoadSceneMode.Additive));
#else
        if (Application.CanStreamedLevelBeLoaded(Name)) op = SceneManager.LoadSceneAsync(Name, LoadSceneMode.Additive);
#endif
        if (op == null)
        {
            if (!missingSaid) Shinobytes.Debug.Log("WorldTowerScene: " + Name + " is not in this build's scene list; no tower on the horizon.");
            missingSaid = true;
            return;
        }
        loading = true;
        op.completed += _ => loading = false;
    }
}
