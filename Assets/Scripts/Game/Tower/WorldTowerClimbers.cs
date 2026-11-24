using System;
using System.Collections.Generic;
using RavenNest.Models;
using UnityEngine;

/// <summary>
///     One of the stream's best climbers as the server gives them (api/tower/climbers, RavenNest's TowerClimberInfo):
///     a name and a look, and the character's part in this stream's climb.
/// </summary>
[Serializable]
public sealed class WorldTowerClimber
{
    public Guid CharacterId;
    public string Name;
    /// <summary>How many of the stream's clears the character fought in: what the list is ordered by.</summary>
    public int Clears;
    /// <summary>The highest part among them, as a step.</summary>
    public int Highest;
    public int LastHits;
    public SyntyAppearance Appearance;
}

/// <summary>
///     The stream's best climbers at the foot of the world tower (docs/world-tower.md): their names on the leaderboard
///     stone, and the best of them as the statue, in stone. The list is the server's (asked for at the start, after
///     every clear and now and then); while there is none, the stone keeps its carved runes and the pedestal the
///     nameless hero.
///     <para>The statue is the climber's own look: if they are in the game it is cast from their character as it
///     stands (what they wear and carry too); if they are not, from their appearance on a bare copy of the player
///     model. If neither can be had, the nameless hero stays.</para>
/// </summary>
public sealed partial class WorldTower
{
    [Header("The stream's best climbers")]
    [Tooltip("The stone's rows for the best climbers, top first.")]
    public TMPro.TextMeshPro[] ClimberRows = new TMPro.TextMeshPro[0];
    [Tooltip("The line over the rows that says what they are; empty while there are no names.")]
    public TMPro.TextMeshPro ClimbersTitle;
    [Tooltip("The carved runes that stand in the rows' place while there are no names.")]
    public GameObject SlabRunes;
    [Tooltip("Where the statue stands (the top of the pedestal), facing its forward.")]
    public Transform StatueSpot;
    [Tooltip("The nameless hero that stands there while the stream has no best climber.")]
    public GameObject GenericStatue;
    [Tooltip("What a statue is made of.")]
    public Material Stone;
    [Tooltip("How tall the best climber stands in stone, metres.")]
    public float StatueHeight = 13f;

    /// <summary>The list as the server last gave it; empty before it has, and for a stream nobody has climbed with.</summary>
    public IReadOnlyList<WorldTowerClimber> BestClimbers => climbers;

    private WorldTowerClimber[] climbers = new WorldTowerClimber[0];
    private float climbersDue = 5f;
    private bool climbersAsked;
    private Guid statueOf;
    private bool statueFromGame;
    private GameObject statue;
    private readonly List<Mesh> statueMeshes = new List<Mesh>();
    // how wide and how tall the figure last cast is, in its own metres
    private float castWidth, castHeight;
    // the meshes baked for a statue that is being made, until it stands
    private readonly List<Mesh> castMeshes = new List<Mesh>();

    /// <summary>Twice a second (Look): asks the server when it is time, and recasts the statue when its subject walks in.</summary>
    private void Climbers()
    {
        var game = GameManager.Instance;
        if (!game || game.RavenNest == null || !game.RavenNest.SessionStarted) return;
        if (Time.time >= climbersDue && !climbersAsked) AskClimbers(game);
        // the best climber joined the game after their statue was made from the bare model: cast it from them now
        if (climbers.Length > 0 && statue && !statueFromGame && statueOf == climbers[0].CharacterId)
        {
            var player = game.Players.GetPlayerById(statueOf);
            if (player && Ready(player)) Cast(climbers[0]);
        }
    }

    private async void AskClimbers(GameManager game)
    {
        climbersAsked = true;
        climbersDue = Time.time + 600f;
        try
        {
            var list = await game.RavenNest.Tower.ClimbersAsync();
            if (this) ShowClimbers(list ?? new WorldTowerClimber[0]);
        }
        catch (Exception exc)
        {
            // an older server has no such list: the stone keeps its runes
            Shinobytes.Debug.Log("WorldTower: no list of climbers from the server (" + exc.Message + ")");
        }
        finally
        {
            climbersAsked = false;
        }
    }

    /// <summary>The names onto the stone, and the first of them onto the pedestal.</summary>
    public void ShowClimbers(WorldTowerClimber[] list)
    {
        climbers = list ?? new WorldTowerClimber[0];
        var shown = 0;
        var size = float.MaxValue;
        for (var i = 0; i < ClimberRows.Length; ++i)
        {
            var row = ClimberRows[i];
            if (!row) continue;
            if (i < climbers.Length && !string.IsNullOrEmpty(climbers[i].Name))
            {
                // each row is fitted to its own width first, then all take the smallest size, so the list is even
                row.enableAutoSizing = true;
                row.text = string.Format(Localization.UI_TOWER_STONE_ROW, i + 1, climbers[i].Name, climbers[i].Clears);
                row.ForceMeshUpdate();
                size = Mathf.Min(size, row.fontSize);
                shown++;
            }
            else row.text = "";
        }
        if (shown > 0)
            foreach (var row in ClimberRows)
            {
                if (!row) continue;
                row.enableAutoSizing = false;
                row.fontSize = size;
            }
        if (SlabRunes) SlabRunes.SetActive(shown == 0);
        if (ClimbersTitle) ClimbersTitle.text = shown > 0 ? Localization.UI_TOWER_STONE_TITLE : "";

        if (climbers.Length == 0) { ClearStatue(); return; }
        if (statue && statueOf == climbers[0].CharacterId) return;
        Cast(climbers[0]);
    }

    private static bool Ready(PlayerController player) => player && !player.isDestroyed && player.gameObject.activeInHierarchy;

    private void Cast(WorldTowerClimber best)
    {
        if (!StatueSpot || !Stone) return;
        var game = GameManager.Instance;
        GameObject made = null;
        var fromGame = false;
        try
        {
            var player = game ? game.Players.GetPlayerById(best.CharacterId) : null;
            if (Ready(player))
            {
                made = CastFrom(player.transform);
                fromGame = made;
            }
            if (!made && best.Appearance != null) made = CastFromAppearance(game, best.Appearance);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("WorldTower: the climber's statue could not be made (" + exc.Message + "); the nameless hero stays.");
            made = null;
        }
        if (!made)
        {
            foreach (var mesh in castMeshes) if (mesh) Destroy(mesh);
            castMeshes.Clear();
            if (!statue) ClearStatue();
            return;
        }
        // the old statue goes with its meshes; the new one's are kept from here (clearing first took the new statue's
        // own meshes with it: an empty figure with a sword in the air)
        ClearStatue();
        statueMeshes.AddRange(castMeshes);
        castMeshes.Clear();
        statue = made;
        statueOf = best.CharacterId;
        statueFromGame = fromGame;
        if (GenericStatue) GenericStatue.SetActive(false);
    }

    private void ClearStatue()
    {
        if (statue) Destroy(statue);
        statue = null;
        statueOf = Guid.Empty;
        foreach (var mesh in statueMeshes) if (mesh) Destroy(mesh);
        statueMeshes.Clear();
        if (GenericStatue) GenericStatue.SetActive(true);
    }

    /// <summary>
    ///     A statue of whatever stands under <paramref name="source"/>: every mesh it shows, as it is posed this
    ///     moment, frozen and in stone, stood on the pedestal at the statue's height.
    /// </summary>
    private GameObject CastFrom(Transform source)
    {
        var root = new GameObject("Statue of the best climber");
        var made = new List<Mesh>();
        var bounds = new Bounds();
        var any = false;
        // in the source's own space, so the statue faces the way the pedestal says whatever way the source faced
        var toLocal = source.worldToLocalMatrix;
        void Add(Mesh mesh, Matrix4x4 world, bool ours)
        {
            if (!mesh || mesh.vertexCount == 0) return;
            var part = new GameObject(mesh.name);
            part.transform.SetParent(root.transform, false);
            var local = toLocal * world;
            part.transform.localPosition = local.GetColumn(3);
            part.transform.localRotation = local.rotation;
            part.transform.localScale = local.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            var materials = new Material[Mathf.Max(1, mesh.subMeshCount)];
            for (var i = 0; i < materials.Length; ++i) materials[i] = Stone;
            renderer.sharedMaterials = materials;
            if (ours) made.Add(mesh);
            var b = mesh.bounds;
            for (var i = 0; i < 8; ++i)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = local.MultiplyPoint3x4(corner);
                if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                else bounds.Encapsulate(p);
            }
        }

        foreach (var skinned in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (!skinned.enabled || !skinned.sharedMesh || !ShownUnder(skinned.transform, source)) continue;
            if (Carried(skinned.transform, source)) continue;
            var mesh = new Mesh { name = skinned.sharedMesh.name };
            skinned.BakeMesh(mesh, true);
            // a baked mesh keeps no bounds of its own: without them the figure is culled and the statue is sized by
            // whatever else it holds (the first cast was a sword twelve metres long on an empty pedestal)
            mesh.RecalculateBounds();
            Add(mesh, Matrix4x4.TRS(skinned.transform.position, skinned.transform.rotation, Vector3.one), true);
        }
        foreach (var renderer in source.GetComponentsInChildren<MeshRenderer>(true))
        {
            // what is held and worn; not name tags, marks or effects
            if (!renderer.enabled || !ShownUnder(renderer.transform, source) || renderer.GetComponent<TMPro.TMP_Text>()) continue;
            if (Carried(renderer.transform, source)) continue;
            var filter = renderer.GetComponent<MeshFilter>();
            if (!filter || !filter.sharedMesh || filter.sharedMesh.vertexCount < 12) continue;
            var material = renderer.sharedMaterial;
            if (!material || material.renderQueue >= 2450) continue;
            Add(filter.sharedMesh, renderer.transform.localToWorldMatrix, false);
        }
        if (!any || bounds.size.y < 0.2f)
        {
            Destroy(root);
            foreach (var mesh in made) Destroy(mesh);
            return null;
        }
        castMeshes.AddRange(made);
        castWidth = Mathf.Max(bounds.size.x, bounds.size.z);
        castHeight = bounds.size.y;
        root.transform.SetParent(StatueSpot, false);
        var scale = StatueHeight / bounds.size.y;
        root.transform.localScale = Vector3.one * scale;
        root.transform.localRotation = Quaternion.identity;
        // feet on the pedestal, the middle of the figure over its middle
        root.transform.localPosition = new Vector3(-bounds.center.x * scale, -bounds.min.y * scale, -bounds.center.z * scale);
        foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        }
        return root;
    }

    // a pet that rides along is not part of the figure (its holder is named for it: "Disco Raven Pet")
    private static bool Carried(Transform part, Transform source)
    {
        for (var t = part; t && t != source; t = t.parent)
            if (t.name.EndsWith(" Pet", StringComparison.Ordinal) || t.name.StartsWith("Pet ", StringComparison.Ordinal)) return true;
        return false;
    }

    // the first frame of the model's idle (or of whatever its animator has first), put on its bones
    private static void Pose(GameObject model)
    {
        var animator = model.GetComponentInChildren<Animator>(true);
        var controller = animator ? animator.runtimeAnimatorController : null;
        if (!controller) return;
        AnimationClip idle = null;
        foreach (var clip in controller.animationClips)
        {
            if (!clip) continue;
            if (!idle) idle = clip;
            if (clip.name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0) { idle = clip; break; }
        }
        if (idle) idle.SampleAnimation(animator.gameObject, 0f);
    }

    // active and enabled all the way up to the source (the source itself may sit under something inactive)
    private static bool ShownUnder(Transform part, Transform source)
    {
        for (var t = part; t && t != source; t = t.parent)
            if (!t.gameObject.activeSelf) return false;
        return true;
    }

    /// <summary>
    ///     For a climber who is not in the game: their look put on a copy of the player model that never wakes up (it is
    ///     made under an inactive object, so none of its scripts run), cast, and thrown away.
    /// </summary>
    private GameObject CastFromAppearance(GameManager game, SyntyAppearance appearance)
    {
        if (!game || !game.Players) return null;
        var field = typeof(PlayerManager).GetField("playerControllerPrefab", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var prefab = field != null ? field.GetValue(game.Players) as GameObject : null;
        if (!prefab) return null;
        var holder = new GameObject("Statue model (asleep)");
        holder.SetActive(false);
        try
        {
            var copy = Instantiate(prefab, holder.transform);
            copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var look = copy.GetComponentInChildren<SyntyPlayerAppearance>(true);
            if (!look) return null;
            look.SetAppearance(appearance, null, false);
            // a model that never woke up stands with its arms out: it is stood at ease first, from its own idle
            Pose(copy);
            var made = CastFrom(copy.transform);
            // still arms out (as wide as it is tall): no statue is better than that one, the nameless hero stays
            if (made && castWidth > castHeight * 0.7f)
            {
                Destroy(made);
                foreach (var mesh in castMeshes) if (mesh) Destroy(mesh);
                castMeshes.Clear();
                return null;
            }
            return made;
        }
        finally
        {
            Destroy(holder);
        }
    }
}
