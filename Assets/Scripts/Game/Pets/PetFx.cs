using System;
using RavenNest.Combat;
using UnityEngine;

/// <summary>
///     The battle pets' little effects, made in code so any model can use them (docs/battle-pets-design.md, section 9):
///     a bolt of the pet's element for pets that stay back, a ring for heals and buffs, and a burst for a tame or an
///     evolution. All of them are skipped when graphics are off.
/// </summary>
public static class PetFx
{
    private static Material material;
    private static readonly System.Collections.Generic.Dictionary<Color, Material> tinted = new System.Collections.Generic.Dictionary<Color, Material>();

    public static Color ElementColor(Element element)
    {
        switch (element)
        {
            case Element.Fire: return new Color(1f, 0.45f, 0.1f);
            case Element.Ice: return new Color(0.55f, 0.85f, 1f);
            case Element.Lightning: return new Color(0.85f, 0.7f, 1f);
            case Element.Poison: return new Color(0.45f, 1f, 0.3f);
            default: return new Color(1f, 0.95f, 0.75f);
        }
    }

    public static Color HealColor => new Color(0.5f, 1f, 0.55f);

    private static Material Material
    {
        get
        {
            if (material) return material;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader) { name = "Battle pet fx" };
            return material;
        }
    }

    private static bool On => GraphicsToggler.GraphicsEnabled && BattlePetController.Shown;

    /// <summary>A material of one colour (a property block does not tint the unlit shader under the SRP batcher).</summary>
    private static Material Tinted(Color color)
    {
        if (tinted.TryGetValue(color, out var m) && m) return m;
        m = new Material(Material) { name = "Battle pet fx " + color, color = color };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        tinted[color] = m;
        return m;
    }

    /// <summary>A bolt from <paramref name="from"/> to a moving target; <paramref name="arrived"/> runs when it gets there (also with graphics off).</summary>
    public static void Bolt(Vector3 from, Transform target, Vector3 targetOffset, Color color, float size, Action arrived)
    {
        if (!On || !target)
        {
            arrived?.Invoke();
            return;
        }
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Pet bolt";
        UnityEngine.Object.Destroy(go.GetComponent<Collider>());
        go.transform.position = from;
        go.transform.localScale = Vector3.one * size;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = Tinted(color);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var trail = go.AddComponent<TrailRenderer>();
        trail.sharedMaterial = Material;
        trail.time = 0.25f;
        trail.startWidth = size * 0.8f;
        trail.endWidth = 0f;
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<PetBoltMover>().Launch(target, targetOffset, arrived);
    }

    /// <summary>A ring that grows and fades on the ground around <paramref name="center"/>.</summary>
    public static void Ring(Vector3 center, float radius, Color color, float seconds = 0.6f)
    {
        if (!On) return;
        var go = new GameObject("Pet ring");
        go.transform.position = center + Vector3.up * 0.08f;
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = Material;
        line.loop = true;
        line.useWorldSpace = false;
        line.positionCount = 40;
        line.widthMultiplier = 0.08f;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<PetRingFader>().Begin(line, radius, color, seconds);
    }

    /// <summary>A burst of rings and a flash: a tame, an evolution, a level up.</summary>
    public static void Burst(Vector3 center, float size, Color color)
    {
        if (!On) return;
        Ring(center, size * 1.6f, color, 0.8f);
        Ring(center + Vector3.up * size * 0.5f, size * 1.1f, Color.white, 0.6f);
        Ring(center + Vector3.up * size, size * 0.7f, color, 0.5f);
    }
}

/// <summary>Flies a pet's bolt to its target and runs the hit when it arrives.</summary>
public sealed class PetBoltMover : MonoBehaviour
{
    private const float Speed = 14f;
    private Transform target;
    private Vector3 offset;
    private Action arrived;
    private float started;

    public void Launch(Transform target, Vector3 offset, Action arrived)
    {
        this.target = target;
        this.offset = offset;
        this.arrived = arrived;
        started = Time.time;
    }

    private void Update()
    {
        if (!target || Time.time - started > 2f)
        {
            Finish();
            return;
        }
        var goal = target.position + offset;
        var step = Speed * Time.deltaTime;
        var to = goal - transform.position;
        if (to.magnitude <= step) { transform.position = goal; Finish(); return; }
        transform.position += to.normalized * step;
    }

    private void Finish()
    {
        var a = arrived;
        arrived = null;
        try { a?.Invoke(); } catch (Exception exc) { Shinobytes.Debug.LogError("Pet bolt: " + exc); }
        Destroy(gameObject, 0.05f);
        enabled = false;
    }
}

/// <summary>Grows and fades a ring, then removes it.</summary>
public sealed class PetRingFader : MonoBehaviour
{
    private LineRenderer line;
    private float radius, seconds, started;
    private Color color;

    public void Begin(LineRenderer line, float radius, Color color, float seconds)
    {
        this.line = line;
        this.radius = radius;
        this.color = color;
        this.seconds = Mathf.Max(0.1f, seconds);
        started = Time.time;
        Draw(0f);
    }

    private void Update()
    {
        var t = (Time.time - started) / seconds;
        if (t >= 1f) { Destroy(gameObject); return; }
        Draw(t);
    }

    private void Draw(float t)
    {
        var r = Mathf.Lerp(radius * 0.25f, radius, 1f - (1f - t) * (1f - t));
        for (var i = 0; i < line.positionCount; ++i)
        {
            var a = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
        }
        var c = new Color(color.r, color.g, color.b, 1f - t);
        line.startColor = c;
        line.endColor = c;
    }
}
