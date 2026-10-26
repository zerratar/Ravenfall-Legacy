using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// The looks of hero mode's skills (Resources/HeroEffects, made by tools/hero/hero-effects.cs): projectiles from the
/// AAA Projectiles pack (their own hit and flash come along) and bursts. The hero moves its projectiles itself, so
/// the pack's mover and physics are switched off on each copy.
/// </summary>
[CreateAssetMenu(menuName = "Ravenfall/Hero Effects")]
public class HeroEffects : ScriptableObject
{
    [Header("Projectiles")]
    public GameObject Arrow;
    public GameObject HolyBolt;
    public GameObject Fireball;
    public GameObject Lightning;
    public GameObject Meteor;
    public GameObject Smite;

    [Header("Bursts")]
    public GameObject ArrowRain;
    public GameObject Snare;
    public GameObject FrostNova;
    public GameObject MeteorBlast;
    public GameObject Mend;
    public GameObject Sanctuary;
    public GameObject Angel;
    public GameObject Cheer;
    public GameObject Arrival;

    // loot on the ground (LootDrop)
    public GameObject LootCoin;
    public GameObject LootPouch;
    public GameObject LootPotion;

    private static HeroEffects instance;
    private static GameObject container;

    public static HeroEffects Get
    {
        get
        {
            if (!instance) instance = Resources.Load<HeroEffects>("HeroEffects");
            return instance;
        }
    }

    /// <summary>A burst at a place, gone after a while.</summary>
    public static GameObject Burst(GameObject prefab, Vector3 position, float scale = 1f, float seconds = 2.5f)
    {
        if (!prefab) return null;
        var go = Instantiate(prefab, position, Quaternion.identity, Container());
        if (scale != 1f) go.transform.localScale *= scale;
        Tame(go);
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>()) ps.Play();
        Destroy(go, seconds);
        return go;
    }

    /// <summary>
    /// A projectile from one place to another (where the target is now, every frame), then its hit: arrived is called
    /// with where it landed. A height arcs it (arrows), a fall drops it from the sky (meteors, arrow rain).
    /// </summary>
    public static IEnumerator Fly(GameObject prefab, Vector3 from, Func<Vector3> to, float speed, float height, Action<Vector3> arrived, float hitScale = 1f)
    {
        if (!prefab)
        {
            arrived?.Invoke(to());
            yield break;
        }
        var go = Instantiate(prefab, from, Quaternion.LookRotation(Direction(from, to())), Container());
        var mover = go.GetComponent<ProjectileMover>();
        var hit = mover ? mover.hit : null;
        if (mover && mover.flash) Burst(mover.flash, from, 1f, 1.5f);
        Tame(go);
        // the pack's projectiles emit for a second, enough at its own speed; ours keep going until they land
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            main.loop = true;
            if (!ps.isPlaying) ps.Play();
        }

        var start = from;
        var total = Mathf.Max(0.1f, Vector3.Distance(from, to()));
        var travelled = 0f;
        var last = from;
        while (go && travelled < total)
        {
            var target = to();
            total = Mathf.Max(0.1f, Vector3.Distance(start, target));
            travelled += speed * Time.deltaTime;
            var k = Mathf.Clamp01(travelled / total);
            var position = Vector3.Lerp(start, target, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * height;
            if ((position - last).sqrMagnitude > 0.0001f) go.transform.rotation = Quaternion.LookRotation(position - last);
            last = position;
            go.transform.position = position;
            yield return null;
        }
        var end = go ? go.transform.position : to();
        if (go) Fade(go);
        if (hit) Burst(hit, end, hitScale, 2f);
        arrived?.Invoke(end);
    }

    private static Vector3 Direction(Vector3 from, Vector3 to)
    {
        var d = to - from;
        return d.sqrMagnitude > 0.0001f ? d : Vector3.forward;
    }

    /// <summary>The pack's own mover and physics off: the hero moves it.</summary>
    private static void Tame(GameObject go)
    {
        foreach (var m in go.GetComponentsInChildren<ProjectileMover>()) m.enabled = false;
        foreach (var r in go.GetComponentsInChildren<Rigidbody>()) { r.isKinematic = true; r.detectCollisions = false; }
        foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var c in go.GetComponentsInChildren<MonoBehaviour>())
        {
            // the pack's particle collisions spawn their own hits, and its auto destroy fights ours
            var name = c.GetType().Name;
            if (name == "ParticleCollisionInstance" || name == "AutoDestroyPS") c.enabled = false;
        }
    }

    /// <summary>A projectile stops emitting and goes once its trail has faded.</summary>
    private static void Fade(GameObject go)
    {
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>()) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.enabled = false;
        foreach (var l in go.GetComponentsInChildren<Light>()) l.enabled = false;
        Destroy(go, 1f);
    }

    private static Transform Container()
    {
        if (!container) container = GameObject.Find("Effects and Projectiles");
        if (!container) container = new GameObject("Effects and Projectiles");
        return container.transform;
    }
}
