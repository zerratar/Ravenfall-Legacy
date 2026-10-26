using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A drop on the ground (RavebridgeLoot): coins, a pouch or a potion tossed out of a fallen enemy, hovering and turning,
/// with a beam of its rarity's colour from uncommon up and its name over it (LootLabels). Walking over it picks it up
/// for the hero who made the kill; coins come to the hero from a few metres. A drop nobody takes goes after two
/// minutes; with too many on the ground the oldest goes straight into its bag, so a big wave never loses its loot.
/// </summary>
public sealed class LootDrop : MonoBehaviour
{
    private const float TossSeconds = 0.45f;
    private const float PickRadius = 1.4f;
    private const float CoinPull = 3.5f;
    private const float FlySeconds = 0.18f;
    private const float Lifetime = 120f;
    private const int MaxOnGround = 80;

    private static readonly List<LootDrop> all = new List<LootDrop>();
    public static IReadOnlyList<LootDrop> All => all;

    public PlayerController Owner { get; private set; }
    public RavebridgeLoot.Item Item { get; private set; }
    public long Amount { get; private set; }
    public float Born { get; private set; }

    /// <summary>The colour its label and beam are drawn in.</summary>
    public Color Color => Item != null ? RavebridgeLoot.ColorOf(Item.Grade) : RavebridgeLoot.CoinColor;

    /// <summary>What its label says.</summary>
    public string Label => Item != null
        ? (Amount > 1 ? Item.Name + " x" + Amount : Item.Name)
        : string.Format(Localization.UI_LOOT_COINS_ON_GROUND, Amount);

    /// <summary>Where its label goes.</summary>
    public Vector3 LabelPoint => transform.position + Vector3.up * 0.75f;

    private Transform model;
    private Vector3 from;
    private Vector3 rest;
    private float flyStarted = -1f;
    private Vector3 flyFrom;
    private bool taken;

    /// <summary>Tosses a drop out from a place: coins when <paramref name="item"/> is null.</summary>
    public static LootDrop Create(PlayerController owner, RavebridgeLoot.Item item, long amount, Vector3 at)
    {
        LootManager.Ensure();
        while (all.Count >= MaxOnGround) all[0].Take();

        var go = new GameObject(item != null ? "Loot " + item.Key : "Loot coins");
        go.transform.SetParent(LootManager.Root, true);
        var drop = go.AddComponent<LootDrop>();
        drop.Owner = owner;
        drop.Item = item;
        drop.Amount = amount;
        drop.Born = Time.time;
        drop.from = at + Vector3.up * 1f;
        var offset = Random.insideUnitCircle * Random.Range(0.6f, 1.6f);
        var target = at + new Vector3(offset.x, 0f, offset.y);
        drop.rest = NavMesh.SamplePosition(target, out var hit, 2f, NavMesh.AllAreas) ? hit.position : at;
        go.transform.position = drop.from;
        drop.Build();
        all.Add(drop);
        if (item == null) GameSounds.Play(SoundKind.CoinDrop, at, 0.45f, 0.2f, owner);
        return drop;
    }

    private void Build()
    {
        var fx = HeroEffects.Get;
        var prefab = fx ? (Item == null ? fx.LootCoin : Item.IsPotion ? fx.LootPotion : fx.LootPouch) : null;
        var size = Item == null ? 0.42f : Item.Grade >= RavebridgeLoot.Rarity.Epic ? 0.8f : 0.65f;
        if (prefab)
        {
            var count = Item == null ? Mathf.Clamp((int)(Amount / 8) + 1, 1, 4) : 1;
            var holder = new GameObject("Model").transform;
            holder.SetParent(transform, false);
            for (var i = 0; i < count; i++)
            {
                var piece = Instantiate(prefab, holder);
                foreach (var c in piece.GetComponentsInChildren<Collider>()) Destroy(c);
                piece.transform.localPosition = count > 1 ? new Vector3(Mathf.Cos(i * 2.1f), 0f, Mathf.Sin(i * 2.1f)) * 0.12f + Vector3.up * i * 0.03f : Vector3.zero;
                piece.transform.localRotation = Quaternion.Euler(Item == null ? 70f : 0f, i * 47f, 0f);
            }
            // every prop to about the same size, whatever its pack made it
            var bounds = new Bounds(transform.position, Vector3.zero);
            var first = true;
            foreach (var r in holder.GetComponentsInChildren<Renderer>())
            {
                if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var biggest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (biggest > 0.001f) holder.localScale = Vector3.one * (size / biggest);
            model = holder;
        }

        // the beam: from uncommon up, taller for the rarer
        if (Item != null && Item.Grade >= RavebridgeLoot.Rarity.Uncommon)
        {
            var material = Resources.Load<Material>("HeroTrail");
            if (material)
            {
                var beam = new GameObject("Beam").AddComponent<LineRenderer>();
                beam.transform.SetParent(transform, false);
                beam.useWorldSpace = false;
                beam.sharedMaterial = material;
                beam.positionCount = 2;
                var height = 1.2f + (int)Item.Grade * 0.6f;
                beam.SetPosition(0, Vector3.down * 0.3f);
                beam.SetPosition(1, Vector3.up * height);
                beam.widthMultiplier = 0.1f + (int)Item.Grade * 0.025f;
                var c = Color;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                    new[] { new GradientAlphaKey(0.75f, 0f), new GradientAlphaKey(0f, 1f) });
                beam.colorGradient = gradient;
                beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }

    private void Update()
    {
        if (taken) return;
        var now = Time.time;
        var age = now - Born;

        if (flyStarted >= 0f)
        {
            // on its way into the hero's hands
            var k = Mathf.Clamp01((now - flyStarted) / FlySeconds);
            var to = Owner ? Owner.Position + Vector3.up * 1.1f : flyFrom;
            transform.position = Vector3.Lerp(flyFrom, to, k * k);
            if (k >= 1f) Take();
            return;
        }

        if (age < TossSeconds)
        {
            var k = age / TossSeconds;
            // an arc out of the enemy, coming to rest hovering
            transform.position = Vector3.Lerp(from, rest, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 1.2f + 0.25f * k);
        }
        else
        {
            transform.position = rest + Vector3.up * (0.25f + Mathf.Sin((now + Born) * 2.2f) * 0.06f);
        }
        if (model) model.localRotation = Quaternion.Euler(0f, now * 70f, 0f);

        if (age > Lifetime || (Owner == null && age > 5f)) { Destroy(gameObject); return; }

        if (age > TossSeconds * 0.8f && Owner && !Owner.Stats.IsDead && Owner.InRavebridge)
        {
            var flat = Owner.Position - transform.position;
            flat.y = 0f;
            var reach = Item == null ? CoinPull : PickRadius;
            if (flat.sqrMagnitude < reach * reach)
            {
                flyStarted = now;
                flyFrom = transform.position;
            }
        }
    }

    /// <summary>Into the owner's bag, and gone from the ground.</summary>
    public void Take()
    {
        if (taken) return;
        taken = true;
        all.Remove(this);
        if (Owner) RavebridgeLoot.Pick(Owner, Item, Amount);
        Destroy(gameObject);
    }

    private void OnDestroy() => all.Remove(this);

    /// <summary>The drop nearest the cursor on screen within a few dozen pixels, for a click to walk to it.</summary>
    public static LootDrop UnderCursor(Camera cam, float pixels)
    {
        LootDrop best = null;
        var bestPixels = pixels;
        var mouse = (Vector2)Input.mousePosition;
        foreach (var d in all)
        {
            if (!d || d.taken) continue;
            var screen = cam.WorldToScreenPoint(d.transform.position);
            if (screen.z <= 0f) continue;
            var label = cam.WorldToScreenPoint(d.LabelPoint);
            var p = Mathf.Min(Vector2.Distance(mouse, screen), Vector2.Distance(mouse, label));
            if (p < bestPixels) { bestPixels = p; best = d; }
        }
        return best;
    }

    public Vector3 Rest => rest;
}

/// <summary>Keeps the drops together and the bags saved (RavebridgeLoot.Tick), and saves them when the game closes.</summary>
public sealed class LootManager : MonoBehaviour
{
    private static LootManager instance;

    public static Transform Root
    {
        get
        {
            Ensure();
            return instance.transform;
        }
    }

    public static void Ensure()
    {
        if (instance) return;
        var go = new GameObject("Ravebridge Loot");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<LootManager>();
    }

    private void Update() => RavebridgeLoot.Tick();

    private void OnApplicationQuit() => RavebridgeLoot.SaveNow();
}
