using System.Collections;
using System.Collections.Generic;
using RavenNest.Combat;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
///     What a Substitution looks like (docs/substitution-escape-idea.md), one look per source so viewers can tell what was
///     found. The smoke step (the magical attribute): the player bursts into a puff of smoke, a small log thuds down where
///     they stood (a ninja's substitution, as a joke) and they reappear in a second puff just outside the ring. The shadow
///     phase (a Shadowstone): the player turns into a dark figure of themselves with a bright violet rim, which glides over
///     the floor (never into it: the owner, 2026-11-28) to the ring's edge on a trail of shadow, then turns back into the
///     player. Both follow the settings' "Boss attack effects"; with it off or in potato mode a
///     single short flash at each end says it instead of the smoke and shadow.
/// </summary>
public static class SubstitutionFx
{
    private static Material dust;
    private static GameObject logPrefab;
    private static bool logLooked;

    /// <summary>
    ///     Smoke's colours: a mid grey and a light one, thick, so a puff reads at a stream camera's distance on dark
    ///     floors and on bright snow alike (a pale smoke vanished on the snow).
    /// </summary>
    public static readonly Color Smoke = new Color(0.80f, 0.80f, 0.83f, 1f);
    public static readonly Color SmokeDark = new Color(0.47f, 0.47f, 0.52f, 1f);
    /// <summary>The shadow's: near black with a violet edge, so it is not mistaken for a hole in the floor.</summary>
    public static readonly Color Shadow = new Color(0.08f, 0.03f, 0.15f, 0.95f);
    public static readonly Color ShadowEdge = new Color(0.62f, 0.38f, 1f, 0.9f);

    /// <summary>Seconds the shadow takes to slide out: longer for a longer way, so it is seen to go.</summary>
    public static float ShadowSlide(float distance) => Mathf.Clamp(distance / 20f, 0.35f, 0.7f);

    public static void Play(SubstitutionLook look, PlayerController player, Vector3 from, Vector3 to)
    {
        var host = GameManager.Instance;
        if (!host || !player) return;
        var ground = Ground(from);
        var landing = Ground(to);
        GameSounds.Play(SoundKind.Dodge, landing, 0.9f);
        if (!SettingsApplier.BossEffects)
        {
            // the cheap look: one flash where they stood and one where they are
            Flash(ground, look);
            Flash(landing, look);
            return;
        }

        if (look == SubstitutionLook.ShadowPhase) host.StartCoroutine(ShadowRoutine(player, ground, landing));
        else SmokeStep(player, ground, landing);
    }

    // ---- the smoke step -------------------------------------------------------------------------------------------

    private static void SmokeStep(PlayerController player, Vector3 from, Vector3 to)
    {
        Puff(from, 1f);
        Log(from, player.transform.rotation);
        Puff(to, 0.85f);
    }

    /// <summary>A puff of smoke about a person's size: a thick burst that billows out and up, then thins away.</summary>
    private static void Puff(Vector3 at, float scale)
    {
        var ps = Burst("Substitution Puff", at + Vector3.up * 1f, Dust(), 2.2f);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f * scale, 4.5f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(1.1f * scale, 2.1f * scale);
        main.startColor = new ParticleSystem.MinMaxGradient(SmokeDark, Smoke);
        main.gravityModifier = -0.06f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f * scale;
        // they slow fast, so the puff holds together round the body instead of flying apart
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 0.6f;
        limit.dampen = 0.25f;
        Grow(ps, 1.8f);
        FadeOut(ps);
        Emit(ps, Mathf.RoundToInt(42 * scale));
        // a ring of low smoke rolling out over the ground
        var ring = Burst("Substitution Puff Ring", at + Vector3.up * 0.2f, Dust(), 1.8f);
        var rm = ring.main;
        rm.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.9f);
        rm.startSpeed = new ParticleSystem.MinMaxCurve(2.6f * scale, 4f * scale);
        rm.startSize = new ParticleSystem.MinMaxCurve(0.8f * scale, 1.4f * scale);
        rm.startColor = new ParticleSystem.MinMaxGradient(SmokeDark, Smoke);
        var rs = ring.shape;
        rs.shapeType = ParticleSystemShapeType.Circle;
        rs.radius = 0.25f;
        rs.radiusThickness = 0f;
        rs.rotation = new Vector3(90f, 0f, 0f);
        Grow(ring, 1.6f);
        FadeOut(ring);
        Emit(ring, Mathf.RoundToInt(24 * scale));
    }

    /// <summary>The joke: a log thuds down where the player stood, rocks once, lies a moment and sinks away.</summary>
    private static void Log(Vector3 at, Quaternion facing)
    {
        var host = GameManager.Instance;
        if (!host) return;
        var log = MakeLog();
        if (!log) return;
        host.StartCoroutine(LogRoutine(log, at, facing));
    }

    private static GameObject MakeLog()
    {
        if (!logLooked)
        {
            logLooked = true;
            logPrefab = Resources.Load<GameObject>("SubstitutionLog");
        }
        GameObject log;
        if (logPrefab)
        {
            log = Object.Instantiate(logPrefab);
        }
        else
        {
            // no prop: a plain log of two cylinders (bark and a paler cut end)
            log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(log.GetComponent<Collider>());
            log.transform.localScale = new Vector3(0.32f, 0.42f, 0.32f);
            var r = log.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader) r.material = new Material(shader) { color = new Color(0.36f, 0.24f, 0.14f) };
        }
        log.name = "Substitution Log";
        log.transform.localScale *= 1.2f;
        foreach (var c in log.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        foreach (var r in log.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        return log;
    }

    private static IEnumerator LogRoutine(GameObject log, Vector3 at, Quaternion facing)
    {
        // lying across where the player faced, dropped from about chest height
        var lying = facing * Quaternion.Euler(0f, 90f, 0f);
        var t = log.transform;
        var drop = 0.32f;
        var height = 1.1f;
        for (var e = 0f; e < drop && log; e += Time.deltaTime)
        {
            var k = e / drop;
            t.SetPositionAndRotation(at + Vector3.up * (height * (1f - k * k)), lying * Quaternion.Euler(0f, 0f, 35f * (1f - k)));
            yield return null;
        }
        if (!log) yield break;
        GameSounds.Play(SoundKind.WoodChop, at, 0.7f);
        // a little bounce and a rock from side to side
        for (var e = 0f; e < 0.45f && log; e += Time.deltaTime)
        {
            var k = e / 0.45f;
            var hop = Mathf.Sin(k * Mathf.PI) * 0.12f * (1f - k);
            var rock = Mathf.Sin(k * Mathf.PI * 3f) * 9f * (1f - k);
            t.SetPositionAndRotation(at + Vector3.up * hop, lying * Quaternion.Euler(rock, 0f, 0f));
            yield return null;
        }
        if (!log) yield break;
        t.SetPositionAndRotation(at, lying);
        // a moment for viewers to see it, then into the ground
        yield return new WaitForSeconds(1.6f);
        for (var e = 0f; e < 0.6f && log; e += Time.deltaTime)
        {
            t.position = at + Vector3.down * (0.5f * e / 0.6f);
            yield return null;
        }
        if (log) Object.Destroy(log);
    }

    // ---- the shadow phase -----------------------------------------------------------------------------------------

    private static IEnumerator ShadowRoutine(PlayerController player, Vector3 from, Vector3 to)
    {
        // the body goes dark: a figure of it with a violet rim (readable on a dark floor by its rim, on snow by its dark
        // body) stands a moment where the player was, then glides over the floor to the ring's edge
        var figure = MakeFigure(player);
        var facing = player.transform.rotation;
        var hidden = Hide(player);
        Sink(from);
        var host = GameManager.Instance;
        if (figure && host)
        {
            // a moment standing as a shadow first, so it is seen for what it is
            yield return FigureMove(figure, from, facing, 0f, 0f, 0f, FigureHold + 0.1f);
        }
        var away = to - from;
        away.y = 0f;
        var travel = away.sqrMagnitude > 0.01f ? Quaternion.LookRotation(away.normalized) : facing;

        // the shadow slides along the ground, a dark streak with violet wisps at its edge
        var slide = ShadowSlide(Vector3.Distance(from, to));
        var slider = new GameObject("Substitution Shadow");
        slider.transform.position = from + Vector3.up * 0.06f;
        var blot = Burst("Substitution Shadow Blot", from, Dust(), slide + 1.5f, slider.transform);
        var bm = blot.main;
        bm.duration = slide + 0.1f;
        bm.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.65f);
        bm.startSpeed = 0f;
        bm.startSize = new ParticleSystem.MinMaxCurve(1.8f, 2.4f);
        bm.startColor = Shadow;
        var blotRenderer = blot.GetComponent<ParticleSystemRenderer>();
        blotRenderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        var be = blot.emission;
        be.rateOverDistance = 7f;
        FadeOut(blot);
        blot.Play();
        blot.Emit(3);
        var wisps = Burst("Substitution Shadow Wisps", from, Dust(), slide + 1.5f, slider.transform);
        var wm = wisps.main;
        wm.duration = slide + 0.1f;
        wm.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        wm.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        wm.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
        wm.startColor = new ParticleSystem.MinMaxGradient(Shadow, Color.Lerp(Shadow, ShadowEdge, 0.75f));
        wm.gravityModifier = -0.3f;
        var ws = wisps.shape;
        ws.shapeType = ParticleSystemShapeType.Circle;
        ws.radius = 0.7f;
        ws.rotation = new Vector3(90f, 0f, 0f);
        var we = wisps.emission;
        we.rateOverDistance = 18f;
        FadeOut(wisps);
        wisps.Play();

        for (var e = 0f; e < slide && slider; e += Time.deltaTime)
        {
            var k = e / slide;
            k = 1f - (1f - k) * (1f - k);
            var at = Vector3.Lerp(from, to, k);
            slider.transform.position = at + Vector3.up * 0.06f;
            if (figure)
            {
                // on the floor the whole way, turned to where it goes
                figure.transform.SetPositionAndRotation(at, Quaternion.Slerp(facing, travel, Mathf.Clamp01(e / 0.1f)));
                SetFigure(figure, at.y, 1f);
            }
            yield return null;
        }
        if (slider)
        {
            slider.transform.position = to + Vector3.up * 0.06f;
            foreach (var ps in slider.GetComponentsInChildren<ParticleSystem>())
            {
                ps.transform.SetParent(null, true);
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            Object.Destroy(slider);
        }

        // it turns back into the player: the figure stops at the edge, faces the fight again, then the player is there
        Rise(to);
        if (figure && host)
        {
            for (var e = 0f; e < FigureRise * 0.5f && figure; e += Time.deltaTime)
            {
                figure.transform.SetPositionAndRotation(to, Quaternion.Slerp(travel, facing, e / (FigureRise * 0.5f)));
                SetFigure(figure, to.y, 1f);
                yield return null;
            }
            if (figure) figure.transform.SetPositionAndRotation(to, facing);
            yield return new WaitForSeconds(0.08f);
            Show(hidden);
            yield return FigureFade(figure, 0.18f);
            Object.Destroy(figure);
        }
        else
        {
            yield return new WaitForSeconds(0.12f);
            Show(hidden);
        }
    }

    // ---- the shadow figure ----------------------------------------------------------------------------------------

    private const float FigureHold = 0.15f;
    private const float FigureRise = 0.42f;
    private static Material figureMaterial;
    private static bool figureLooked;
    private static readonly int FloorYId = Shader.PropertyToID("_FloorY");
    private static readonly int FadeId = Shader.PropertyToID("_Fade");

    /// <summary>
    ///     The player's shape as it stands this moment (their meshes baked), drawn with the shadow figure's material
    ///     (Ravenfall/Substitution Shadow Figure: a dark body, a violet rim, cut off at the floor). Hidden until it is
    ///     placed. Null without the material.
    /// </summary>
    private static GameObject MakeFigure(PlayerController player)
    {
        if (!figureLooked)
        {
            figureLooked = true;
            figureMaterial = Resources.Load<Material>("SubstitutionShadow");
        }
        if (!figureMaterial || !player) return null;

        var root = new GameObject("Substitution Shadow Figure");
        var inverse = Matrix4x4.TRS(player.transform.position, player.transform.rotation, Vector3.one).inverse;
        foreach (var r in player.GetComponentsInChildren<Renderer>())
        {
            if (!r || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer || r is SpriteRenderer) continue;
            // name tags and other text: not the body
            if (r.sharedMaterial && r.sharedMaterial.shader && r.sharedMaterial.shader.name.Contains("TextMeshPro")) continue;

            Mesh mesh;
            Matrix4x4 local;
            if (r is SkinnedMeshRenderer skinned)
            {
                if (!skinned.sharedMesh) continue;
                mesh = new Mesh();
                skinned.BakeMesh(mesh, true);
                // baked in the renderer's space with its scale: placed at its position and rotation
                local = inverse * Matrix4x4.TRS(r.transform.position, r.transform.rotation, Vector3.one);
            }
            else
            {
                var filter = r.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh) continue;
                mesh = filter.sharedMesh;
                local = inverse * r.transform.localToWorldMatrix;
            }

            var part = new GameObject("Part");
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = local.GetColumn(3);
            part.transform.localRotation = local.rotation;
            part.transform.localScale = local.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = part.AddComponent<MeshRenderer>();
            var materials = new Material[Mathf.Max(1, mesh.subMeshCount)];
            for (var i = 0; i < materials.Length; i++) materials[i] = figureMaterial;
            mr.sharedMaterials = materials;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            if (r is SkinnedMeshRenderer) part.AddComponent<BakedMeshOwner>().Mesh = mesh;
        }
        if (root.transform.childCount == 0)
        {
            Object.Destroy(root);
            return null;
        }
        root.SetActive(false);
        // never left behind (a coroutine stopped with its host)
        Object.Destroy(root, 5f);
        return root;
    }

    /// <summary>The figure stands at <paramref name="at"/> and moves from one height under it to another.</summary>
    private static IEnumerator FigureMove(GameObject figure, Vector3 at, Quaternion facing, float fromY, float toY, float seconds, float hold)
    {
        if (!figure) yield break;
        figure.transform.SetPositionAndRotation(at + Vector3.up * fromY, facing);
        SetFigure(figure, at.y, 1f);
        figure.SetActive(true);
        if (hold > 0f) yield return new WaitForSeconds(hold);
        for (var e = 0f; e < seconds && figure; e += Time.deltaTime)
        {
            var k = e / seconds;
            k = k * k * (3f - 2f * k);
            figure.transform.position = at + Vector3.up * Mathf.Lerp(fromY, toY, k);
            yield return null;
        }
        if (figure) figure.transform.position = at + Vector3.up * toY;
    }

    private static IEnumerator FigureFade(GameObject figure, float seconds)
    {
        var floor = figure ? figure.transform.position.y : 0f;
        for (var e = 0f; e < seconds && figure; e += Time.deltaTime)
        {
            SetFigure(figure, floor, 1f - e / seconds);
            yield return null;
        }
    }

    private static readonly MaterialPropertyBlock figureBlock = new MaterialPropertyBlock();

    private static void SetFigure(GameObject figure, float floorY, float fade)
    {
        figureBlock.Clear();
        figureBlock.SetFloat(FloorYId, floorY);
        figureBlock.SetFloat(FadeId, fade);
        foreach (var r in figure.GetComponentsInChildren<MeshRenderer>(true)) r.SetPropertyBlock(figureBlock);
    }

    /// <summary>Frees a baked mesh with the figure that drew it.</summary>
    private sealed class BakedMeshOwner : MonoBehaviour
    {
        public Mesh Mesh;
        private void OnDestroy() { if (Mesh) Object.Destroy(Mesh); }
    }

    /// <summary>Dark smoke drawn down into the floor where the player stood.</summary>
    private static void Sink(Vector3 at)
    {
        var ps = Burst("Substitution Shadow Sink", at + Vector3.up * 1.1f, Dust(), 1.4f);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(Shadow, ShadowEdge);
        main.gravityModifier = 1.4f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.6f, 1.6f, 0.6f);
        FadeOut(ps);
        Emit(ps, figureMaterial ? 14 : 30); // fewer over the figure, which says it better
    }

    /// <summary>A column of dark wisps rising where the shadow stops, the player taking shape inside it.</summary>
    private static void Rise(Vector3 at)
    {
        var ps = Burst("Substitution Shadow Rise", at + Vector3.up * 0.1f, Dust(), 1.6f);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 3.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(Shadow, ShadowEdge);
        main.gravityModifier = -0.1f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 0.6f;
        limit.dampen = 0.15f;
        FadeOut(ps);
        // a thin column: the figure rising inside it is what is to be seen
        Emit(ps, figureMaterial ? 14 : 36);
        // and a faint violet spark of it at the feet
        var spark = Burst("Substitution Shadow Spark", at + Vector3.up * 0.6f, Dust(), 1f);
        var sm = spark.main;
        sm.startLifetime = 0.35f;
        sm.startSpeed = 0f;
        sm.startSize = 3.2f;
        sm.startColor = new Color(ShadowEdge.r, ShadowEdge.g, ShadowEdge.b, 0.55f);
        Grow(spark, 1.4f);
        FadeOut(spark);
        Emit(spark, 1);
    }

    // the player's visible body (not its particles), switched off for the slide; given back as it was
    private static List<Renderer> Hide(PlayerController player)
    {
        var list = new List<Renderer>();
        if (!player) return list;
        foreach (var r in player.GetComponentsInChildren<Renderer>())
        {
            if (!r || !r.enabled || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            r.enabled = false;
            list.Add(r);
        }
        return list;
    }

    private static void Show(List<Renderer> hidden)
    {
        foreach (var r in hidden) if (r) r.enabled = true;
    }

    // ---- the cheap look -------------------------------------------------------------------------------------------

    /// <summary>One short flash of the look's colour (effects off, potato mode).</summary>
    private static void Flash(Vector3 at, SubstitutionLook look)
    {
        var ps = Burst("Substitution Flash", at + Vector3.up * 1f, Dust(), 0.6f);
        var main = ps.main;
        main.startLifetime = 0.22f;
        main.startSpeed = 0f;
        main.startSize = 2.4f;
        main.startColor = look == SubstitutionLook.ShadowPhase ? new Color(0.65f, 0.45f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.9f);
        Grow(ps, 1.5f);
        FadeOut(ps);
        Emit(ps, 1);
    }

    // ---- pieces -------------------------------------------------------------------------------------------------

    private static Vector3 Ground(Vector3 at)
    {
        if (NavMesh.SamplePosition(at, out var hit, 2.5f, NavMesh.AllAreas)) return hit.position;
        return at;
    }

    private static Material Dust()
    {
        if (!dust) dust = Resources.Load<Material>("TowerBossDust");
        return dust;
    }

    private static ParticleSystem Burst(string name, Vector3 at, Material material, float lifetime, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent) go.transform.SetParent(parent, false);
        else go.transform.position = at;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.2f;
        main.maxParticles = 120;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        Object.Destroy(go, lifetime + (parent ? 2f : 0f));
        return ps;
    }

    private static void Emit(ParticleSystem ps, int count)
    {
        ps.Play();
        ps.Emit(count);
    }

    private static void Grow(ParticleSystem ps, float to)
    {
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f / to, 1f, 1f));
    }

    private static void FadeOut(ParticleSystem ps)
    {
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.06f), new GradientAlphaKey(0.85f, 0.55f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
    }
}
