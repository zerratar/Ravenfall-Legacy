using UnityEngine;

/// <summary>
///     The Fortress Hand Cannon's shot (docs/dark-fortress-design.md): a ranged weapon that fires a cannonball instead
///     of an arrow. A flash and a puff of smoke at the muzzle, the cannon kicks back in the hand, a boom, the ball flies
///     in a low arc and bursts in a puff of dust where it lands, with a thud. Only the look and the sound: the hit and its
///     damage are the bow's, as they always were (the arrow was only a picture too).
///     The flash, the smoke and the dust follow the Weapon effects setting and potato mode, as the weapons' element
///     effects do; with them off the ball still flies and the cannon still booms.
/// </summary>
public sealed class CannonShot : MonoBehaviour
{
    /// <summary>The weapon models that are cannons (Resources paths of their prefabs).</summary>
    private static readonly string[] CannonPrefabs = { "Character/Weapons/DarkFortress/DF Cannon 01" };

    private const float BallSpeed = 26f;     // metres a second along the ground
    private const float BallRadius = 0.11f;
    private const float RecoilDistance = 0.12f;
    private const float RecoilSeconds = 0.18f;

    private static Material additive;
    private static Material smokeMaterial;
    private static Texture2D dot;
    private static Mesh sphere;

    private GameObject ball;
    private Vector3 from, to;
    private float flight, flown;
    private ParticleSystem flash, smoke, dust;
    private Transform recoilModel;
    private Vector3 recoilRest;
    private float recoilLeft;

    /// <summary>The ranged weapon in the hand is a cannon.</summary>
    public static bool Is(ItemController weapon)
    {
        if (!weapon) return false;
        var path = weapon.GenericPrefabPath;
        if (string.IsNullOrEmpty(path)) return false;
        foreach (var cannon in CannonPrefabs) if (path == cannon) return true;
        return false;
    }

    /// <summary>Fires the player's cannon at the target. False when the player holds no cannon (an arrow flies then).</summary>
    public static bool TryFire(PlayerController player, Transform target)
    {
        if (!player || !target || player.Equipment == null) return false;
        var cannon = player.Equipment.BowObject;
        if (!Is(cannon)) return false;
        var shot = player.GetComponent<CannonShot>();
        if (!shot) shot = player.gameObject.AddComponent<CannonShot>();
        shot.Fire(player, cannon, target);
        return true;
    }

    private void Fire(PlayerController player, ItemController cannon, Transform target)
    {
        var aim = target.position + Vector3.up * 1.0f;
        var muzzle = Muzzle(cannon, aim);
        var direction = aim - muzzle;
        if (direction.sqrMagnitude < 0.01f) direction = player.transform.forward;
        var effects = WeaponElementFx.Enabled;

        // the ball, of the cannon's own dark iron
        if (!ball)
        {
            ball = new GameObject("Cannonball");
            ball.AddComponent<MeshFilter>().sharedMesh = Sphere;
            var renderer = ball.AddComponent<MeshRenderer>();
            var cannonRenderer = cannon.GetComponentInChildren<Renderer>();
            if (cannonRenderer) renderer.sharedMaterial = cannonRenderer.sharedMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ball.transform.localScale = Vector3.one * BallRadius * 2f;
        }
        from = muzzle;
        to = target.position + Vector3.up * 0.9f;
        flight = Mathf.Max(0.12f, Vector3.Distance(from, to) / BallSpeed);
        flown = 0f;
        ball.transform.position = from;
        ball.SetActive(true);

        if (effects)
        {
            var look = Quaternion.LookRotation(direction.normalized);
            Burst(ref flash, "Cannon flash", muzzle, look, Flash, 24);
            Burst(ref smoke, "Cannon smoke", muzzle, look, Smoke, 22);
        }

        // the kick: the model jumps back along the barrel and settles
        var model = cannon.transform.childCount > 0 ? cannon.transform.GetChild(0) : null;
        if (model)
        {
            if (recoilModel != model || recoilLeft <= 0f) { recoilModel = model; recoilRest = model.localPosition; }
            var back = model.parent.InverseTransformDirection(-direction.normalized) * RecoilDistance;
            recoilModel.localPosition = recoilRest + back;
            recoilLeft = RecoilSeconds;
        }

        GameSounds.Play(SoundKind.CannonShot, muzzle, 0.7f, 0f, player);
    }

    private void Update()
    {
        if (recoilLeft > 0f && recoilModel)
        {
            recoilLeft -= Time.deltaTime;
            var t = Mathf.Clamp01(1f - recoilLeft / RecoilSeconds);
            recoilModel.localPosition = Vector3.Lerp(recoilModel.localPosition, recoilRest, t);
            if (recoilLeft <= 0f) recoilModel.localPosition = recoilRest;
        }

        if (!ball || !ball.activeSelf) return;
        flown += Time.deltaTime;
        var k = Mathf.Clamp01(flown / flight);
        // a low arc: up a little and down onto the target
        var arc = Mathf.Sin(k * Mathf.PI) * Mathf.Min(1.2f, Vector3.Distance(from, to) * 0.08f);
        ball.transform.position = Vector3.Lerp(from, to, k) + Vector3.up * arc;
        if (k < 1f) return;

        ball.SetActive(false);
        if (WeaponElementFx.Enabled) Burst(ref dust, "Cannon impact", to, Quaternion.identity, Dust, 22);
        GameSounds.Play(SoundKind.RockImpact, to, 0.5f);
    }

    private void OnDestroy()
    {
        if (ball) Destroy(ball);
        if (flash) Destroy(flash.gameObject);
        if (smoke) Destroy(smoke.gameObject);
        if (dust) Destroy(dust.gameObject);
        if (recoilModel) recoilModel.localPosition = recoilRest;
    }

    /// <summary>
    ///     The end of the barrel: the model's longest axis (the barrel) runs through its mesh bounds, and of its two ends
    ///     the one more toward the target is the muzzle.
    /// </summary>
    private static Vector3 Muzzle(ItemController cannon, Vector3 aim)
    {
        var filter = cannon.GetComponentInChildren<MeshFilter>();
        if (!filter || !filter.sharedMesh) return cannon.transform.position;
        var b = filter.sharedMesh.bounds;
        var axis = b.size.z >= b.size.x && b.size.z >= b.size.y ? new Vector3(0f, 0f, b.extents.z) : b.size.x >= b.size.y ? new Vector3(b.extents.x, 0f, 0f) : new Vector3(0f, b.extents.y, 0f);
        var t = filter.transform;
        var a = t.TransformPoint(b.center + axis);
        var c = t.TransformPoint(b.center - axis);
        return (aim - a).sqrMagnitude < (aim - c).sqrMagnitude ? a : c;
    }

    // ---- the particles: made once per player, emitted in bursts ----------------------------------------------------

    private static void Burst(ref ParticleSystem system, string name, Vector3 at, Quaternion rotation, System.Action<ParticleSystem> setup, int count)
    {
        if (!system)
        {
            var go = new GameObject(name);
            system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.enabled = false;
            setup(system);
        }
        system.transform.SetPositionAndRotation(at, rotation);
        system.Emit(count);
    }

    private static void Flash(ParticleSystem ps)
    {
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.75f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.92f, 0.55f), new Color(1f, 0.45f, 0.1f));
        main.maxParticles = 40;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.04f;
        var sizes = ps.sizeOverLifetime;
        sizes.enabled = true;
        sizes.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = Additive;
    }

    private static void Smoke(ParticleSystem ps)
    {
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.73f, 0.7f, 0.8f), new Color(0.55f, 0.54f, 0.53f, 0.7f));
        main.gravityModifier = -0.05f;
        main.maxParticles = 60;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.06f;
        var sizes = ps.sizeOverLifetime;
        sizes.enabled = true;
        sizes.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 2.2f));
        var colors = ps.colorOverLifetime;
        colors.enabled = true;
        colors.color = FadeOut();
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = SmokeMaterial;
    }

    private static void Dust(ParticleSystem ps)
    {
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.57f, 0.5f, 0.8f), new Color(0.45f, 0.42f, 0.38f, 0.7f));
        main.gravityModifier = 0.2f;
        main.maxParticles = 60;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.2f;
        var sizes = ps.sizeOverLifetime;
        sizes.enabled = true;
        sizes.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.7f, 1f, 1.8f));
        var colors = ps.colorOverLifetime;
        colors.enabled = true;
        colors.color = FadeOut();
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = SmokeMaterial;
    }

    private static ParticleSystem.MinMaxGradient FadeOut()
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0f, 1f) });
        return new ParticleSystem.MinMaxGradient(g);
    }

    // hero mode's additive particle material with a soft dot, as the weapons' element effects use
    private static Material Additive
    {
        get
        {
            if (additive) return additive;
            var template = Resources.Load<Material>("HeroTrail");
            additive = template ? new Material(template) : new Material(Shader.Find("Sprites/Default"));
            additive.name = "Cannon flash";
            SetDot(additive);
            return additive;
        }
    }

    // the same particle shader, blended by alpha instead of added: grey smoke that darkens what is behind it
    private static Material SmokeMaterial
    {
        get
        {
            if (smokeMaterial) return smokeMaterial;
            var template = Resources.Load<Material>("HeroTrail");
            smokeMaterial = template ? new Material(template) : new Material(Shader.Find("Sprites/Default"));
            smokeMaterial.name = "Cannon smoke";
            if (smokeMaterial.HasProperty("_Blend")) smokeMaterial.SetFloat("_Blend", 0f); // alpha
            if (smokeMaterial.HasProperty("_SrcBlend")) smokeMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (smokeMaterial.HasProperty("_DstBlend")) smokeMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (smokeMaterial.HasProperty("_SrcBlendAlpha")) smokeMaterial.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            if (smokeMaterial.HasProperty("_DstBlendAlpha")) smokeMaterial.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            smokeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            smokeMaterial.DisableKeyword("_ALPHAMODULATE_ON");
            SetDot(smokeMaterial);
            return smokeMaterial;
        }
    }

    private static void SetDot(Material material)
    {
        material.mainTexture = Dot;
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", Dot);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
    }

    private static Texture2D Dot
    {
        get
        {
            if (dot) return dot;
            const int Size = 32;
            dot = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = "Cannon puff", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; ++y)
            {
                for (var x = 0; x < Size; ++x)
                {
                    var dx = (x + 0.5f) / Size * 2f - 1f;
                    var dy = (y + 0.5f) / Size * 2f - 1f;
                    var a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    a = a * a * (3f - 2f * a);
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            dot.SetPixels32(pixels);
            dot.Apply();
            return dot;
        }
    }

    private static Mesh Sphere
    {
        get
        {
            if (sphere) return sphere;
            var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere = temp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temp);
            return sphere;
        }
    }
}
