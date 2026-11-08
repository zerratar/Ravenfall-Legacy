using System.Collections.Generic;
using RavenNest.Combat;
using UnityEngine;

/// <summary>
///     Makes a battle pet's model (docs/battle-pets-design.md, section 9): a tamed creature uses the model of the enemy
///     it was tamed from ("enemy:Red Metalon", found among the world's enemies and copied without its scripts), scaled
///     to the pet's size at its stage; a woken pet item uses the item's own prefab ("Pets/Raven"), grown a little per
///     stage. The copy keeps only transforms, renderers and the animator, so nothing in it thinks it is an enemy.
/// </summary>
public static class PetModels
{
    private static readonly Dictionary<string, GameObject> enemyTemplates = new Dictionary<string, GameObject>();
    private static Transform holder;

    public sealed class Model
    {
        public GameObject Root;
        public Animator Animator;
        public AnimationClip[] Clips;
        /// <summary>The model's height after scaling, for the reach, the bolt's start and the name above it.</summary>
        public float Height;
    }

    /// <summary>The model for a pet; null when its model cannot be found (it then fights unseen? no: it falls back to a raven).</summary>
    public static Model Create(PetSpecies species, int stage, string itemPrefab)
    {
        if (species == null) return null;
        GameObject source = null;
        var companion = species.Source == PetSource.Companion;
        var key = companion ? itemPrefab : species.ModelAt(stage);
        if (!string.IsNullOrEmpty(key))
        {
            if (key.StartsWith("enemy:")) source = FindEnemy(key.Substring(6));
            else source = Resources.Load<GameObject>(key.StartsWith("pet:") ? key.Substring(4) : key);
        }
        if (!source) source = Resources.Load<GameObject>("Pets/Raven");
        if (!source) return null;

        if (!holder)
        {
            var go = new GameObject("Battle pet models (inactive)");
            go.SetActive(false);
            Object.DontDestroyOnLoad(go);
            holder = go.transform;
        }

        // copied under an inactive parent, so no script on it wakes up before it is stripped
        var copy = Object.Instantiate(source, holder);
        copy.name = "Battle pet " + species.Id;
        copy.transform.localPosition = Vector3.zero;
        copy.transform.localRotation = Quaternion.identity;
        var animator = copy.GetComponentInChildren<Animator>(true);
        var clips = animator && animator.runtimeAnimatorController ? animator.runtimeAnimatorController.animationClips : new AnimationClip[0];
        Strip(copy);

        // a pet item keeps its prefab's own scale (the HD animals are scaled down at their root); an enemy's model is
        // measured at scale one and scaled to the pet's height
        var original = source.transform.localScale;
        copy.transform.SetParent(null, false);
        copy.transform.localScale = companion ? original : Vector3.one;
        // the enemy copied may be hidden at the moment (defeated, or culled while far from the camera): then every
        // part is shown; otherwise what it shows (a few enemies hide spare gear)
        var renderers = copy.GetComponentsInChildren<Renderer>(true);
        var anyOn = false;
        foreach (var r in renderers) if (r.enabled && !(r is ParticleSystemRenderer)) anyOn = true;
        foreach (var r in renderers)
        {
            if (!anyOn && !(r is ParticleSystemRenderer)) r.enabled = true;
            if (r is SkinnedMeshRenderer s) s.updateWhenOffscreen = true;
        }
        copy.SetActive(true);

        var size = Size(copy, out var height);
        float scale;
        if (companion) scale = (float)BattlePets.CompanionGrowth[Mathf.Clamp(stage, 0, BattlePets.CompanionGrowth.Length - 1)];
        else scale = size > 0.01f ? (float)species.HeightAt(stage) / size : 1f;
        copy.transform.localScale = (companion ? original : Vector3.one) * scale;
        foreach (var r in copy.GetComponentsInChildren<Renderer>(true)) if (r is SkinnedMeshRenderer s) s.updateWhenOffscreen = false;

        return new Model { Root = copy, Animator = animator, Clips = clips, Height = Mathf.Max(0.3f, height * scale) };
    }

    /// <summary>The model's size for scaling: its height, or most of its length for long ones (dragons, metalons).</summary>
    private static float Size(GameObject go, out float height)
    {
        var any = false;
        var bounds = new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
        }
        height = any ? bounds.size.y : 1f;
        return any ? Mathf.Max(bounds.size.y, Mathf.Max(bounds.size.x, bounds.size.z) * 0.6f) : 1f;
    }

    private static void Strip(GameObject go)
    {
        // scripts first (they may need the components below), then physics and navigation
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) if (mb) Object.DestroyImmediate(mb);
        foreach (var c in go.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true)) Object.DestroyImmediate(c);
        foreach (var c in go.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>(true)) Object.DestroyImmediate(c);
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        foreach (var c in go.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(c);
        foreach (var c in go.GetComponentsInChildren<AudioSource>(true)) Object.DestroyImmediate(c);
        foreach (var c in go.GetComponentsInChildren<Light>(true)) c.enabled = false;
        // health bars, name tags and other UI on enemies
        foreach (var c in go.GetComponentsInChildren<Canvas>(true)) if (c) { if (c.gameObject == go) Object.DestroyImmediate(c); else Object.DestroyImmediate(c.gameObject); }
        foreach (var c in go.GetComponentsInChildren<ParticleSystem>(true)) if (c) { if (c.gameObject == go) Object.DestroyImmediate(c); else Object.DestroyImmediate(c.gameObject); }
    }

    /// <summary>An enemy in the world by its kind's name or definition id, as the template for its tamed pets.</summary>
    private static GameObject FindEnemy(string name)
    {
        if (enemyTemplates.TryGetValue(name, out var cached) && cached) return cached;
        GameObject found = null;
        foreach (var e in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
        {
            if (!e) continue;
            var d = e.CombatDefinition;
            if (d != null && (string.Equals(d.Name, name, System.StringComparison.OrdinalIgnoreCase) || string.Equals(d.Id, name, System.StringComparison.OrdinalIgnoreCase)))
            {
                found = e.gameObject;
                // an unnamed one is the plain look (named enemies sometimes wear more)
                if (KindName(e.name) == name) break;
                continue;
            }
            if (string.Equals(KindName(e.name), name, System.StringComparison.OrdinalIgnoreCase)) { found = e.gameObject; break; }
        }
        if (found) enemyTemplates[name] = found;
        return found;
    }

    /// <summary>"Red Metalon (3)" is "Red Metalon".</summary>
    public static string KindName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        var paren = name.IndexOf(" (");
        return (paren > 0 ? name.Substring(0, paren) : name).Trim();
    }
}
