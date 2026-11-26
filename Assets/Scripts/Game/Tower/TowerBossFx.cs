using System.Collections;
using RavenNest.Combat;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
///     What a tower boss's area attacks look like (the owner, 2026-11-26: "a slam is only a red ring, a spit only a green
///     one, nothing played"). The warning is hero mode's telegraph on the ground (a faint fill growing to its rim as the
///     blow nears, WaveRun.Moves). A slam lands with a shockwave of dust out to the ring's edge, a small camera shake and
///     a little knockback for the players inside; a spit is a glob arcing from the boss to the marked spot in its
///     element's colour, splashing where it lands. The particles (dust, glob, splash) follow the settings' "Boss attack
///     effects" and the shake its own "Boss slams shake the camera"; potato mode turns both off, leaving the telegraphs.
/// </summary>
public static class TowerBossFx
{
    private static Material telegraphTemplate, dustMaterial, glowMaterial, globMaterial;
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int LineId = Shader.PropertyToID("_Line");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    /// <summary>The red of a slam's warning.</summary>
    public static readonly Color SlamColor = new Color(1f, 0.3f, 0.12f, 1f);

    /// <summary>
    ///     The dust a slam throws up: earth brown, or a blue grey of snow powder on an ice floor (a light dust vanished on
    ///     the snow); dark enough to read on a bright floor.
    /// </summary>
    public static Color DustColor(string biome) => biome == "ice"
        ? new Color(0.56f, 0.64f, 0.74f, 0.85f)
        : new Color(0.5f, 0.45f, 0.38f, 0.85f);

    /// <summary>A spit's colour by the boss's element: poison green, ice blue, lightning yellow, fire orange; muck else.</summary>
    public static Color ElementColor(Element element)
    {
        switch (element)
        {
            case Element.Poison: return new Color(0.45f, 0.95f, 0.2f, 1f);
            case Element.Ice: return new Color(0.55f, 0.85f, 1f, 1f);
            case Element.Lightning: return new Color(1f, 0.92f, 0.4f, 1f);
            case Element.Fire: return new Color(1f, 0.5f, 0.12f, 1f);
            default: return new Color(0.66f, 0.72f, 0.35f, 1f);
        }
    }

    // ---- The warning ------------------------------------------------------------------------------------------

    /// <summary>
    ///     A circle on the ground at <paramref name="centre"/>, <paramref name="radius"/> wide, that fills up toward its
    ///     rim until the blow lands (<see cref="Fill"/>); hero mode's telegraph shader (Resources/HeroTelegraph).
    /// </summary>
    public static GameObject Telegraph(Vector3 centre, float radius, Color color, out Material material)
    {
        material = null;
        if (!telegraphTemplate) telegraphTemplate = Resources.Load<Material>("HeroTelegraph");
        if (!telegraphTemplate) return null;
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Tower Boss Telegraph";
        Object.Destroy(go.GetComponent<Collider>());
        var renderer = go.GetComponent<MeshRenderer>();
        material = new Material(telegraphTemplate) { hideFlags = HideFlags.DontSave };
        material.SetFloat(LineId, 0f);
        material.SetFloat(ProgressId, 0f);
        material.SetColor(BaseColorId, color);
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        go.transform.SetPositionAndRotation(Ground(centre) + Vector3.up * 0.07f, Quaternion.Euler(90f, 0f, 0f));
        go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        return go;
    }

    /// <summary>How far the warning has filled (0 to 1).</summary>
    public static void Fill(Material material, float progress)
    {
        if (material) material.SetFloat(ProgressId, Mathf.Clamp01(progress));
    }

    /// <summary>
    ///     The blow landed: the telegraph becomes a shockwave, a bright front racing from the middle out to its edge in
    ///     <paramref name="seconds"/>, fading as it gets there, then goes. One quad: it shows in potato mode too.
    /// </summary>
    public static void Land(GameObject telegraph, Material material, float seconds = 0.3f)
    {
        if (!telegraph) return;
        var host = TowerManager.Instance;
        if (!host || !material) { Remove(telegraph, material); return; }
        host.StartCoroutine(WaveRoutine(telegraph, material, seconds));
    }

    public static void Remove(GameObject telegraph, Material material)
    {
        if (telegraph) Object.Destroy(telegraph);
        if (material) Object.Destroy(material);
    }

    private static IEnumerator WaveRoutine(GameObject telegraph, Material material, float seconds)
    {
        var color = material.GetColor(BaseColorId);
        var bright = Color.Lerp(color, Color.white, 0.35f);
        var fade = seconds * 0.6f;
        for (var t = 0f; t < seconds + fade && telegraph; t += Time.deltaTime)
        {
            var k = Mathf.Clamp01(t / seconds);
            material.SetFloat(ProgressId, 1f - (1f - k) * (1f - k));
            var a = t <= seconds ? 1f : 1f - (t - seconds) / fade;
            material.SetColor(BaseColorId, new Color(bright.r, bright.g, bright.b, color.a * a));
            yield return null;
        }
        Remove(telegraph, material);
    }

    // ---- The slam ---------------------------------------------------------------------------------------------

    /// <summary>
    ///     A slam lands at <paramref name="centre"/>: dust rolls out to the ring's edge and a puff rises in the middle
    ///     (when effects are on), the ground thuds and the camera shakes a little (its own setting) when it is near.
    /// </summary>
    public static void SlamImpact(Vector3 centre, float radius, Color dust)
    {
        var ground = Ground(centre);
        GameSounds.Play(SoundKind.RockImpact, ground, 1f);
        ShakeCamera(ground, 0.16f, 0.35f);
        if (!SettingsApplier.BossEffects) return;
        GameSounds.Play(SoundKind.RockDebris, ground, 0.6f, 0.05f);
        DustRing(ground, radius, dust, Mathf.Clamp(Mathf.RoundToInt(radius * 12f), 48, 120), 0.7f, 1f);
        // the middle: a heavier puff that rises and spreads
        var puff = Burst("Tower Slam Puff", ground + Vector3.up * 0.2f, Dust(), 2.2f);
        var main = puff.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(radius * 0.22f, radius * 0.4f);
        main.startColor = dust;
        main.gravityModifier = -0.08f;
        var shape = puff.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = radius * 0.18f;
        Grow(puff, 1.8f);
        FadeOut(puff);
        Emit(puff, Mathf.Clamp(Mathf.RoundToInt(radius * 2f), 10, 24));
    }

    // a ring of puffs from near the middle out to the edge, fading as they get there
    private static void DustRing(Vector3 ground, float radius, Color color, int count, float lifeMin, float lifeMax)
    {
        var ring = Burst("Tower Slam Dust", ground + Vector3.up * 0.35f, Dust(), lifeMax + 0.3f);
        var main = ring.main;
        var life = (lifeMin + lifeMax) * 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        // at the ring's edge about as they fade (they start at an eighth of it)
        main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 0.8f / life, radius * 0.95f / life);
        main.startSize = new ParticleSystem.MinMaxCurve(Mathf.Max(0.7f, radius * 0.18f), Mathf.Max(1.1f, radius * 0.32f));
        main.startColor = color;
        main.gravityModifier = -0.03f;
        var shape = ring.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius * 0.12f;
        shape.radiusThickness = 0f;
        shape.rotation = new Vector3(90f, 0f, 0f);
        Grow(ring, 1.6f);
        FadeOut(ring);
        Emit(ring, count);
    }

    // ---- The spit ---------------------------------------------------------------------------------------------

    /// <summary>
    ///     A glob from <paramref name="from"/> (the boss's mouth) arcing to <paramref name="to"/> over
    ///     <paramref name="seconds"/>, in <paramref name="color"/>, with a trail of drops; it splashes where it lands.
    ///     Nothing flies when effects are off: the telegraph says it all.
    /// </summary>
    public static void Spit(Vector3 from, Vector3 to, float seconds, Color color, float size)
    {
        if (!SettingsApplier.BossEffects) return;
        var host = TowerManager.Instance;
        if (!host) return;
        host.StartCoroutine(SpitRoutine(from, Ground(to), seconds, color, size));
    }

    private static IEnumerator SpitRoutine(Vector3 from, Vector3 to, float seconds, Color color, float size)
    {
        var glob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glob.name = "Tower Boss Spit";
        Object.Destroy(glob.GetComponent<Collider>());
        var renderer = glob.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = Glob();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var block = new MaterialPropertyBlock();
        block.SetColor(BaseColorId, Color.Lerp(color, Color.white, 0.15f));
        renderer.SetPropertyBlock(block);
        glob.transform.localScale = Vector3.one * size;
        // the glow round it and the drops it leaves behind
        var trail = Burst("Tower Spit Trail", from, Glow(), seconds + 1f, glob.transform);
        var main = trail.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size * 0.95f);
        main.startColor = new Color(color.r, color.g, color.b, 0.8f);
        main.gravityModifier = 0.6f;
        var shape = trail.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = size * 0.3f;
        var emission = trail.emission;
        emission.rateOverDistance = 9f;
        FadeOut(trail);
        trail.Play();

        var distance = Vector3.Distance(from, to);
        var height = Mathf.Clamp(distance * 0.35f, 1.5f, 7f);
        for (var t = 0f; t < seconds && glob; t += Time.deltaTime)
        {
            var k = t / seconds;
            glob.transform.position = Vector3.Lerp(from, to, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * height);
            // a wobble: it is wet
            var s = 1f + 0.12f * Mathf.Sin(t * 30f);
            glob.transform.localScale = new Vector3(size * s, size / s, size * s);
            yield return null;
        }
        if (trail)
        {
            trail.transform.SetParent(null, true);
            trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if (glob) Object.Destroy(glob);
        Splash(to, TowerManager.SpitRadius, color);
    }

    /// <summary>A spit lands: drops thrown up and out, and a low wash of its colour over the marked circle.</summary>
    public static void Splash(Vector3 at, float radius, Color color)
    {
        var ground = Ground(at);
        GameSounds.Play(SoundKind.WaterSplash, ground, 0.8f);
        if (!SettingsApplier.BossEffects) return;
        var drops = Burst("Tower Spit Splash", ground + Vector3.up * 0.2f, Glow(), 1.4f);
        var main = drops.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.5f);
        main.startColor = new Color(color.r, color.g, color.b, 0.9f);
        main.gravityModifier = 1.6f;
        var shape = drops.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 38f;
        shape.radius = 0.4f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        FadeOut(drops);
        Emit(drops, 34);
        DustRing(ground, radius, new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f, 0.5f), 30, 0.45f, 0.7f);
    }

    // ---- Knockback ----------------------------------------------------------------------------------------------

    /// <summary>A player inside a slam is pushed a little away from its middle, never through a wall or off the ground.</summary>
    public static void Knock(PlayerController player, Vector3 from, float distance)
    {
        var host = TowerManager.Instance;
        if (!host || !player || player.isDestroyed) return;
        var away = player.Position - from;
        away.y = 0f;
        away = away.sqrMagnitude > 0.01f ? away.normalized : player.transform.forward;
        host.StartCoroutine(KnockRoutine(player, away, distance));
    }

    private static IEnumerator KnockRoutine(PlayerController player, Vector3 away, float distance)
    {
        const float Seconds = 0.22f;
        var moved = 0f;
        for (var t = 0f; t < Seconds; )
        {
            t = Mathf.Min(Seconds, t + Time.deltaTime);
            if (!player || player.isDestroyed || player.Stats.IsDead) yield break;
            var k = 1f - (1f - t / Seconds) * (1f - t / Seconds);
            var step = distance * k - moved;
            moved = distance * k;
            var at = player.transform.position;
            var to = at + away * step;
            var agent = player.GetComponent<NavMeshAgent>();
            var mask = agent ? agent.areaMask : NavMesh.AllAreas;
            // stops at a wall or the edge of the walkable ground
            if (NavMesh.Raycast(at, to, out var hit, mask)) { to = hit.position; t = Seconds; }
            if (agent && agent.enabled && agent.isOnNavMesh) agent.Move(to - at);
            else player.transform.position = to;
            yield return null;
        }
    }

    // ---- Camera shake -----------------------------------------------------------------------------------------

    /// <summary>The camera shakes a little when it is near <paramref name="at"/> (the settings' "Boss slams shake the camera").</summary>
    public static void ShakeCamera(Vector3 at, float strength, float seconds)
    {
        if (!SettingsApplier.BossShake) return;
        var game = GameManager.Instance;
        var cam = game && game.Camera ? game.Camera.transform : null;
        if (!cam || (cam.position - at).sqrMagnitude > 60f * 60f) return;
        CameraShake.Shake(cam.gameObject, strength, seconds);
    }

    // ---- Pieces -------------------------------------------------------------------------------------------------

    // where the ground is under a point (the floor of a room, the snow), else the point
    private static Vector3 Ground(Vector3 at)
    {
        if (NavMesh.SamplePosition(at, out var hit, 2.5f, NavMesh.AllAreas)) return hit.position;
        return at;
    }

    private static Material Dust()
    {
        if (!dustMaterial) dustMaterial = Resources.Load<Material>("TowerBossDust");
        return dustMaterial;
    }

    private static Material Glow()
    {
        if (!glowMaterial) glowMaterial = Resources.Load<Material>("HeroTrail");
        return glowMaterial ? glowMaterial : Dust();
    }

    private static Material Glob()
    {
        if (globMaterial) return globMaterial;
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        globMaterial = new Material(shader) { name = "Tower Boss Spit", hideFlags = HideFlags.DontSave };
        return globMaterial;
    }

    // a one-shot particle system that goes after a while (stopped, so the burst is emitted by hand)
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
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (!parent) Object.Destroy(go, lifetime);
        else Object.Destroy(go, lifetime + 2f);
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
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0.85f, 0.6f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
    }
}

/// <summary>
///     A short shake of the camera it sits on, on top of wherever the camera's own scripts put it this frame (it runs
///     after them); switched off when the shake is over, so it costs nothing between shakes.
/// </summary>
[DefaultExecutionOrder(32000)]
public sealed class CameraShake : MonoBehaviour
{
    private float until, length, strength, seed;
    private Vector3 applied, shakenAt;
    private bool shaken;

    public static void Shake(GameObject camera, float strength, float seconds)
    {
        if (!camera) return;
        var shake = camera.GetComponent<CameraShake>();
        if (!shake) shake = camera.AddComponent<CameraShake>();
        // a stronger shake under way is not cut short by a weaker one
        if (shake.enabled && Time.unscaledTime < shake.until && strength < shake.strength) return;
        shake.strength = strength;
        shake.length = Mathf.Max(0.05f, seconds);
        shake.until = Time.unscaledTime + shake.length;
        shake.seed = Random.value * 100f;
        shake.enabled = true;
    }

    private void LateUpdate()
    {
        var t = transform;
        // nobody placed the camera this frame (a still camera): last frame's shake comes off first
        if (shaken && t.position == shakenAt) t.position -= applied;
        shaken = false;
        var left = until - Time.unscaledTime;
        if (left <= 0f)
        {
            enabled = false;
            return;
        }
        var k = left / length;
        var amount = strength * k * k;
        var time = Time.unscaledTime * 38f;
        applied = new Vector3(Mathf.PerlinNoise(seed, time) - 0.5f, Mathf.PerlinNoise(seed + 7f, time) - 0.5f, Mathf.PerlinNoise(seed + 13f, time) - 0.5f) * (2f * amount);
        t.position += applied;
        shakenAt = t.position;
        shaken = true;
    }

    private void OnDisable()
    {
        if (shaken && transform.position == shakenAt) transform.position -= applied;
        shaken = false;
    }
}
