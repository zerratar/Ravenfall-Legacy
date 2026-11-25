using System;
using System.Collections;
using System.Collections.Generic;
using RavenNest.Models;
using UnityEngine;

/// <summary>
///     One of the stream's best climbers as the server gives them (api/tower/climbers, RavenNest's TowerClimberInfo):
///     a name, a look and what they wear, and the character's part in this stream's climb.
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
    /// <summary>What they wear, as the items each piece looks like (no pets, no boats): for the statue.</summary>
    public Guid[] Gear;
}

/// <summary>
///     The stream's best climbers at the foot of the world tower (docs/world-tower.md): their names on the leaderboard
///     stone, and the best of them as the statue, in stone, with a plaque that says who it is. The list is the
///     server's (asked for at the start, after every clear and now and then); while there is none, the stone keeps its
///     carved runes and the pedestal the nameless hero.
///     <para><b>The statue</b> is made the same way whether the climber is in the game or not: their look and what
///     they wear (from their character when it is here, else from the server's answer) are put on a copy of the
///     player model that never wakes up, through the game's own appearance and equipment code, so every slot shows:
///     helmet, armor, cape, shield and the weapon. Of several weapons worn at once the strongest is held. The copy is
///     stood in a pose picked for the kind of weapon, a frame of one of the game's own animation clips
///     (<see cref="Poses"/>), then every mesh on it is frozen as it stands, given the one stone material and stood on
///     the pedestal; the copy is thrown away. Nothing of the statue moves or is skinned afterwards. The work is
///     spread over frames (the copy, the look, a few items a frame, a few meshes a frame), and the old statue stands
///     until the new one is whole.</para>
///     <para>It is made again when the best climber changes, when what they wear changes, and when they walk into the
///     game with gear the server did not know of. If it cannot be made, the nameless hero stays.</para>
/// </summary>
public sealed partial class WorldTower
{
    [Header("The stream's best climbers")]
    [Tooltip("The stone's rows for the best climbers, top first.")]
    public TMPro.TextMeshPro[] ClimberRows = new TMPro.TextMeshPro[0];
    [Tooltip("The line over the rows that says what they are; empty while there are no names.")]
    public TMPro.TextMeshPro ClimbersTitle;
    [Tooltip("The carved runes on each row's plaque, top first: shown while that row has no name.")]
    public GameObject[] RowRunes = new GameObject[0];
    [Tooltip("Where the statue stands (the top of the pedestal), facing its forward.")]
    public Transform StatueSpot;
    [Tooltip("The nameless hero that stands there while the stream has no best climber.")]
    public GameObject GenericStatue;
    [Tooltip("What a statue is made of.")]
    public Material Stone;
    [Tooltip("How tall the nameless hero stands, metres: a climber is cast at this over 2.2 metres, so the figure itself is about four fifths of it and what they hold up reaches higher.")]
    public float StatueHeight = 13f;
    [Tooltip("The plaque on the pedestal and the name on it; hidden while the nameless hero stands there.")]
    public GameObject StatuePlaque;
    public TMPro.TextMeshPro StatueName;

    /// <summary>The list as the server last gave it; empty before it has, and for a stream nobody has climbed with.</summary>
    public IReadOnlyList<WorldTowerClimber> BestClimbers => climbers;

    /// <summary>The kinds of weapon a statue is posed for.</summary>
    public enum StatueWeapon { None, OneHanded, SwordAndShield, TwoHanded, Spear, Staff, Bow }

    /// <summary>
    ///     The pose for each kind of weapon: a clip of the player's animator and how far into it (0 to 1). Picked by
    ///     looking at them in stone on the pedestal (docs/world-tower.md).
    /// </summary>
    public static readonly Dictionary<StatueWeapon, (string Clip, float At)> Poses = new Dictionary<StatueWeapon, (string, float)>
    {
        // a fist raised to the sky (a fist to the chest, Cheer2, read as a shrug from the plaza)
        { StatueWeapon.None, ("Unarmed-Boost1", 0.55f) },
        // the blade raised to the sky in both hands, the top of a great swing (the game's "boost" pose raises the
        // fist and lets the blade hang down the arm: a raised fist and no sword to see)
        { StatueWeapon.OneHanded, ("2Hand-Sword-Attack5", 0.2f) },
        { StatueWeapon.TwoHanded, ("2Hand-Sword-Attack5", 0.2f) },
        // on guard behind the shield, the blade out at the side
        { StatueWeapon.SwordAndShield, ("Shield-Idle", 0f) },
        // (a staff or a spear "planted" beside them hung past the pedestal's edge and below its top: the clips know
        // nothing of how long the thing is; the spear held ready across the body hid behind the figure) the spear
        // lifted over the head in both hands, the staff raised to the sky in one fist (it is held at its middle, so
        // it stands over the fist where a blade hangs under it)
        { StatueWeapon.Spear, ("2Hand-Spear-Unsheath-Back-Relax", 0.6f) },
        { StatueWeapon.Staff, ("Armed-Boost1", 0.5f) },
        // the bow drawn
        { StatueWeapon.Bow, ("2Hand-Bow-Attack1", 0.3f) },
    };

#if UNITY_EDITOR
    /// <summary>For trying poses in the editor: "clip name@0.4" is used for every statue made while it is set.</summary>
    public static string TryPose;
#endif

    /// <summary>What the statue on the pedestal is of and how it was made, for the tests: "" while the nameless hero stands.</summary>
    public string StatueNote { get; private set; } = "";

    private const float CastEverySeconds = 20f;
    private const int ItemsAFrame = 1, MeshesAFrame = 5;

    private WorldTowerClimber[] climbers = new WorldTowerClimber[0];
    private float climbersDue = 5f;
    private bool climbersAsked;
    private GameObject statue;
    private string statueKey = "";
    private string failedKey = "";
    private float nextCast;
    private Coroutine casting;
    private GameObject castHolder;
    private readonly List<Mesh> statueMeshes = new List<Mesh>();
    // the meshes of a statue that is being made, until it stands
    private readonly List<Mesh> castMeshes = new List<Mesh>();

    /// <summary>Twice a second (Look): asks the server when it is time, and sees to it that the statue is of the best climber as they are now.</summary>
    private void Climbers()
    {
        var game = GameManager.Instance;
        if (!game || game.RavenNest == null || !game.RavenNest.SessionStarted) return;
        if (Time.time >= climbersDue && !climbersAsked) AskClimbers(game);
        Statue(game);
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

    /// <summary>The names onto the stone; the first of them onto the pedestal follows (<see cref="Statue"/>).</summary>
    public void ShowClimbers(WorldTowerClimber[] list)
    {
        climbers = list ?? new WorldTowerClimber[0];
        var shown = 0;
        // each row is fitted to its plaque first. The rows under the first then share one size, so the list is even:
        // the smallest of theirs, but never under seven tenths of the largest, so one long name does not shrink every
        // row out of reading from the plaza (that row alone keeps its own smaller size). The first, on its larger
        // plaque and in gold, stands a quarter over them.
        var fit = new float[ClimberRows.Length];
        float smallest = float.MaxValue, largest = 0f;
        for (var i = 0; i < ClimberRows.Length; ++i)
        {
            var row = ClimberRows[i];
            if (!row) continue;
            var named = i < climbers.Length && !string.IsNullOrEmpty(climbers[i].Name);
            row.enableAutoSizing = true;
            row.text = named ? StoneRow(i + 1, climbers[i].Name, climbers[i].Clears) : "";
            // a row with no name keeps its carved runes
            if (i < RowRunes.Length && RowRunes[i]) RowRunes[i].SetActive(!named);
            if (!named) continue;
            shown++;
            row.ForceMeshUpdate();
            fit[i] = row.fontSize;
            if (i > 0) { smallest = Mathf.Min(smallest, fit[i]); largest = Mathf.Max(largest, fit[i]); }
        }
        var shared = largest > 0f ? Mathf.Max(smallest, largest * 0.7f) : 0f;
        for (var i = 0; i < ClimberRows.Length; ++i)
        {
            var row = ClimberRows[i];
            if (!row || fit[i] <= 0f) continue;
            row.enableAutoSizing = false;
            row.fontSize = i == 0 ? (shared > 0f ? Mathf.Min(fit[i], shared * 1.25f) : fit[i]) : Mathf.Min(fit[i], shared);
        }
        if (ClimbersTitle) ClimbersTitle.text = shown > 0 ? Localization.UI_TOWER_STONE_TITLE : "";
        failedKey = "";
        nextCast = 0f;
    }

    private static bool Ready(PlayerController player) => player && !player.isDestroyed && player.gameObject.activeInHierarchy && player.Inventory != null && player.Appearance;

    // the item a worn piece looks like
    private static Item LookOf(GameInventoryItem worn) => worn == null ? null : worn.SkinItem ?? worn.Item;

    /// <summary>The best climber's look and what they wear, as they are now: from their character when it is in the game, else as the server told.</summary>
    private static bool Source(GameManager game, WorldTowerClimber best, out SyntyAppearance look, out List<Item> gear, out bool live)
    {
        look = null;
        gear = new List<Item>();
        live = false;
        var player = game.Players.GetPlayerById(best.CharacterId);
        if (Ready(player))
        {
            live = true;
            look = player.Appearance.ToSyntyAppearanceData();
            foreach (var worn in player.Inventory.GetEquippedItems().ToArray())
            {
                var item = LookOf(worn);
                if (item != null) gear.Add(item);
            }
            return look != null;
        }
        look = best.Appearance;
        if (best.Gear != null && game.Items)
            foreach (var id in best.Gear)
            {
                var item = game.Items.Get(id);
                if (item != null) gear.Add(item);
            }
        return look != null;
    }

    // who it is and what they wear: the statue is made again when this changes
    private static string KeyOf(WorldTowerClimber best, SyntyAppearance look, List<Item> gear, bool live)
    {
        var key = new System.Text.StringBuilder(best.CharacterId.ToString("N"));
        key.Append(look != null ? "|" + look.Gender + "," + look.Hair + "," + look.Head + "," + look.Cape + "," + look.CapeHidden : "|-");
        var ids = new List<string>();
        foreach (var item in gear) ids.Add(item.Id.ToString("N").Substring(0, 8));
        ids.Sort(string.CompareOrdinal);
        key.Append('|').Append(string.Join(",", ids));
#if UNITY_EDITOR
        key.Append('|').Append(TryPose);
#endif
        return key.ToString();
    }

    /// <summary>The statue follows the list: nobody, the nameless hero; else the best climber as they are now.</summary>
    private void Statue(GameManager game)
    {
        if (!StatueSpot || !Stone) return;
        if (climbers.Length == 0)
        {
            if (statue || casting != null) ClearStatue();
            return;
        }
        if (casting != null || Time.time < nextCast || !game.Items || !game.Items.Loaded) return;
        var best = climbers[0];
        if (!Source(game, best, out var look, out var gear, out var live)) return;
        var key = KeyOf(best, look, gear, live);
        if (key == statueKey || key == failedKey) return;
        nextCast = Time.time + CastEverySeconds;
        casting = StartCoroutine(Cast(game, best, look, gear, live, key));
    }

    /// <summary>For the tests: the statue is made again now, whatever stands there.</summary>
    public void RecastStatue()
    {
        statueKey = "";
        failedKey = "";
        nextCast = 0f;
    }

    private void ClearStatue()
    {
        if (casting != null) StopCoroutine(casting);
        casting = null;
        if (castHolder) Destroy(castHolder);
        castHolder = null;
        // the statue that stands, and one that was being made
        if (StatueSpot) foreach (Transform child in StatueSpot) Destroy(child.gameObject);
        foreach (var mesh in castMeshes) if (mesh) Destroy(mesh);
        castMeshes.Clear();
        statue = null;
        statueKey = "";
        StatueNote = "";
        foreach (var mesh in statueMeshes) if (mesh) Destroy(mesh);
        statueMeshes.Clear();
        if (GenericStatue) GenericStatue.SetActive(true);
        if (StatuePlaque) StatuePlaque.SetActive(false);
    }

    /// <summary>The kind of weapon among what is worn that the statue holds, and the pieces that go on the model: every
    /// armor piece, that weapon, a shield with a one-handed weapon; no other weapons, no pets, no boats, no tools.</summary>
    private static StatueWeapon Pick(List<Item> gear, List<Item> worn)
    {
        Item weapon = null, shield = null;
        var best = -1;
        foreach (var item in gear)
        {
            if (item == null || BoatItems.Is(item) || item.Type == ItemType.Pet || item.Category == ItemCategory.Pet) continue;
            if (item.Type == ItemType.Shield) { shield = item; continue; }
            if (item.Category == ItemCategory.Weapon)
            {
                var score = item.WeaponPower + item.MagicPower + item.RangedPower + item.Level;
                if (score > best) { best = score; weapon = item; }
                continue;
            }
            switch (item.Type)
            {
                case ItemType.Helmet: case ItemType.Chest: case ItemType.Gloves: case ItemType.Boots: case ItemType.Leggings:
                case ItemType.LeftShoulder: case ItemType.RightShoulder: case ItemType.Amulet:
                case ItemType.Hat: case ItemType.Mask: case ItemType.HeadCovering:
                    worn.Add(item);
                    break;
            }
        }
        var kind = StatueWeapon.None;
        if (weapon != null)
        {
            worn.Add(weapon);
            switch (weapon.Type)
            {
                case ItemType.TwoHandedSword: case ItemType.TwoHandedAxe: kind = StatueWeapon.TwoHanded; break;
                case ItemType.TwoHandedSpear: kind = StatueWeapon.Spear; break;
                case ItemType.TwoHandedStaff: kind = StatueWeapon.Staff; break;
                case ItemType.TwoHandedBow: kind = StatueWeapon.Bow; break;
                default: kind = StatueWeapon.OneHanded; break;
            }
        }
        if (shield != null && (kind == StatueWeapon.OneHanded || kind == StatueWeapon.None))
        {
            worn.Add(shield);
            if (kind == StatueWeapon.OneHanded) kind = StatueWeapon.SwordAndShield;
        }
        return kind;
    }

    /// <summary>
    ///     Makes the statue over a number of frames, and swaps it in when it is whole. The model it is cast from is a
    ///     copy of the player prefab under an inactive object: none of its scripts ever run, it is never drawn, and
    ///     it is gone again at the end.
    /// </summary>
    private IEnumerator Cast(GameManager game, WorldTowerClimber best, SyntyAppearance look, List<Item> gear, bool live, string key)
    {
        GameObject made = null;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var worst = 0.0;
        void Lap() { worst = Math.Max(worst, watch.Elapsed.TotalMilliseconds); watch.Restart(); }
        var meshes = castMeshes;
        meshes.Clear();
        var note = "";
        var ok = false;
        castHolder = new GameObject("Statue model (asleep)");
        castHolder.SetActive(false);
        try
        {
            var field = typeof(PlayerManager).GetField("playerControllerPrefab", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var prefab = field != null ? field.GetValue(game.Players) as GameObject : null;
            if (!prefab) { note = "no player model"; yield break; }
            var copy = Instantiate(prefab, castHolder.transform);
            copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Lap();
            yield return null;
            watch.Restart();

            var model = copy.GetComponentInChildren<SyntyPlayerAppearance>(true);
            if (!model) { note = "no appearance on the player model"; yield break; }
            // asleep, it has not found these itself; and its parts stay apart (the combined mesh is made by a coroutine)
            model.gameManager = game;
            var items = typeof(SyntyPlayerAppearance).GetField("itemManager", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (items != null) items.SetValue(model, game.Items);
            model.useMeshCombiner = false;
            if (!Try(() => model.SetAppearance(look, null, false), ref note)) yield break;
            Lap();
            yield return null;
            watch.Restart();

            // what they wear, through the game's own way of putting an item on a character
            var worn = new List<Item>();
            var kind = Pick(gear, worn);
            var male = model.Gender == Gender.Male;
            var put = 0;
            foreach (var item in worn)
            {
                var piece = item;
                Try(() =>
                {
                    var controller = game.Items.Create(piece, male);
                    if (!controller) return;
                    // under the sleeping copy at once, so it is asleep too and goes with it
                    controller.transform.SetParent(castHolder.transform, false);
                    model.Equip(controller);
                    // on a bone now (a weapon, a shield, a hat): an item sets itself straight there when it starts,
                    // which a sleeping one never does
                    if (controller.transform.parent != castHolder.transform)
                        controller.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                }, ref note);
                if (++put % ItemsAFrame == 0) { Lap(); yield return null; watch.Restart(); }
            }
            Lap();
            yield return null;
            watch.Restart();

            var pose = Pose(copy, kind);
            foreach (var step in Freeze(copy.transform, meshes, root => made = root)) { Lap(); yield return step; watch.Restart(); }
            Lap();
            if (!made) { note = "nothing to cast"; yield break; }
            note = (live ? "from the game" : "from the server's answer") + ", " + worn.Count + " pieces, " + kind + ", " + pose + ", longest step " + worst.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " ms";
            ok = true;
        }
        finally
        {
            if (castHolder) Destroy(castHolder);
            castHolder = null;
            casting = null;
            if (ok && made && this)
            {
                // the old statue goes with its meshes; the new one stands
                if (statue) Destroy(statue);
                foreach (var mesh in statueMeshes) if (mesh) Destroy(mesh);
                statueMeshes.Clear();
                statueMeshes.AddRange(meshes);
                meshes.Clear();
                statue = made;
                made.SetActive(true);
                statueKey = key;
                StatueNote = best.Name + ": " + note;
                if (GenericStatue) GenericStatue.SetActive(false);
                if (StatueName) StatueName.text = best.Name ?? "";
                if (StatuePlaque) StatuePlaque.SetActive(!string.IsNullOrEmpty(best.Name));
            }
            else
            {
                if (made) Destroy(made);
                foreach (var mesh in meshes) if (mesh) Destroy(mesh);
                meshes.Clear();
                // not again for the same climber in the same gear; the nameless hero (or the statue that stood) stays
                failedKey = key;
                Shinobytes.Debug.LogWarning("WorldTower: the climber's statue could not be made (" + note + ").");
            }
        }
    }

    private static bool Try(Action step, ref string note)
    {
        try
        {
            step();
            return true;
        }
        catch (Exception exc)
        {
            note = exc.GetType().Name + ": " + exc.Message;
            return false;
        }
    }

    /// <summary>Stands the model in the pose for its weapon: a frame of one of its animator's own clips, put on its bones.</summary>
    private static string Pose(GameObject model, StatueWeapon kind)
    {
        var animator = model.GetComponentInChildren<Animator>(true);
        var controller = animator ? animator.runtimeAnimatorController : null;
        if (!controller) return "no animator";
        if (!Poses.TryGetValue(kind, out var pose)) pose = Poses[StatueWeapon.None];
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(TryPose))
        {
            var at = TryPose.LastIndexOf('@');
            if (at > 0 && float.TryParse(TryPose.Substring(at + 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var t))
                pose = (TryPose.Substring(0, at), t);
        }
#endif
        AnimationClip clip = null, fallback = null;
        foreach (var c in controller.animationClips)
        {
            if (!c) continue;
            if (c.name == pose.Clip) { clip = c; break; }
            if (!fallback && c.name == Poses[StatueWeapon.None].Clip) fallback = c;
        }
        if (!clip) clip = fallback;
        if (!clip) return "no clip " + pose.Clip;
        clip.SampleAnimation(animator.gameObject, clip.length * Mathf.Clamp01(pose.At));
        return clip.name + " at " + pose.At.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     A statue of whatever stands under <paramref name="source"/>: every mesh it shows, as it is posed this
    ///     moment, frozen and in the one stone, stood on the pedestal with its feet where the model's feet are. A few
    ///     meshes a frame; <paramref name="done"/> is given the statue (inactive until it is swapped in), or null.
    /// </summary>
    private IEnumerable Freeze(Transform source, List<Mesh> made, Action<GameObject> done)
    {
        var root = new GameObject("Statue of the best climber");
        root.SetActive(false);
        root.transform.SetParent(StatueSpot, false);
        var any = false;
        var count = 0;
        // where the figure touches the ground: its lowest point, and the middle of what is near it (the feet), so it
        // stands on the pedestal's middle and not with a boot over the edge
        var lowest = float.MaxValue;
        Vector2 feetMin = new Vector2(float.MaxValue, float.MaxValue), feetMax = new Vector2(float.MinValue, float.MinValue);
        var points = new List<Vector3>();
        // in the source's own space, so the statue faces the way the pedestal says
        var toLocal = source.worldToLocalMatrix;
        void Add(Mesh mesh, Matrix4x4 world)
        {
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
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            any = true;
        }

        foreach (var skinned in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (!skinned || !skinned.enabled || !skinned.sharedMesh || !ShownUnder(skinned.transform, source) || Carried(skinned.transform, source)) continue;
            var mesh = new Mesh { name = skinned.sharedMesh.name };
            skinned.BakeMesh(mesh, true);
            // a baked mesh keeps no bounds of its own: without them the figure is culled
            mesh.RecalculateBounds();
            made.Add(mesh);
            // baked with the renderer's own scale left out of the vertices, so it is put back by the part's place
            // (the model's parts are a hundredth of their mesh: cast without it, the figure was a hundred times
            // too big and nowhere near the pedestal)
            Add(mesh, skinned.transform.localToWorldMatrix);
            var place = toLocal * skinned.transform.localToWorldMatrix;
            mesh.GetVertices(points);
            foreach (var v in points)
            {
                var p = place.MultiplyPoint3x4(v);
                if (p.y < lowest) lowest = p.y;
                if (p.y > 0.3f) continue;
                feetMin = Vector2.Min(feetMin, new Vector2(p.x, p.z));
                feetMax = Vector2.Max(feetMax, new Vector2(p.x, p.z));
            }
            if (++count % MeshesAFrame == 0) yield return null;
        }
        foreach (var renderer in source.GetComponentsInChildren<MeshRenderer>(true))
        {
            // what is held and worn; not name tags, marks or effects (a glow is no stone)
            if (!renderer || !renderer.enabled || !ShownUnder(renderer.transform, source) || Carried(renderer.transform, source) || renderer.GetComponent<TMPro.TMP_Text>()) continue;
            var filter = renderer.GetComponent<MeshFilter>();
            if (!filter || !filter.sharedMesh || filter.sharedMesh.vertexCount < 12) continue;
            var material = renderer.sharedMaterial;
            // cut-out materials (queue 2450: the Dark Fortress weapons) are solid things; see-through ones are not
            if (!material || material.renderQueue > 2500) continue;
            Add(filter.sharedMesh, renderer.transform.localToWorldMatrix);
        }
        if (!any)
        {
            Destroy(root);
            done(null);
            yield break;
        }
        // a figure of 2.2 metres would stand as tall as the nameless hero; what it holds up reaches over that
        var scale = StatueHeight / 2.2f;
        root.transform.localScale = Vector3.one * scale;
        root.transform.localRotation = Quaternion.identity;
        var feet = feetMin.x <= feetMax.x ? (feetMin + feetMax) * 0.5f : Vector2.zero;
        if (lowest > 0.5f || lowest < -0.5f) lowest = 0f;
        root.transform.localPosition = new Vector3(-feet.x * scale, -lowest * scale, -feet.y * scale);
        done(root);
    }

    // a pet that rides along is not part of the figure (its holder is named for it: "Disco Raven Pet")
    private static bool Carried(Transform part, Transform source)
    {
        for (var t = part; t && t != source; t = t.parent)
            if (t.name.EndsWith(" Pet", StringComparison.Ordinal) || t.name.StartsWith("Pet ", StringComparison.Ordinal)) return true;
        return false;
    }

    // active all the way up to the source (the source itself sits under something inactive)
    private static bool ShownUnder(Transform part, Transform source)
    {
        for (var t = part; t && t != source; t = t.parent)
            if (!t.gameObject.activeSelf) return false;
        return true;
    }
}
