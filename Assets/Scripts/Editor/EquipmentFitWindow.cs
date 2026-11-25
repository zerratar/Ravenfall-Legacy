using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
///     Ravenfall > Equipment Fit: a character to try held and worn item models on (weapons, shields, bows, staffs,
///     tools, head pieces, necklaces), posed in any of the player's animations (paused, stepped or playing), with the
///     item moved, turned and scaled by hand and saved into its prefab: for this item, for every item with the same
///     model, or for every item of its kind.
///     <para>An item model sits in the game as the game puts it there: an ItemController under the slot's bone
///     (Hand_R, Hand_L, Head_Attachment or Neck) at no offset and a world scale of 1, with the prefab as its child, so
///     the prefab ROOT's local position, rotation and scale is the fit (SyntyPlayerAppearance.Equip,
///     ItemController.Create). The rig here is built the same way, so what is saved is what every character and the
///     world tower's statue show.</para>
///     <para>Saving writes the prefab and records the fit in tools/items/equipment-fits.json, which is in git:
///     Assets/Resources is not, and some prefabs are made again by scripts (the Dark Fortress ones by
///     tools/dark-fortress/dark-fortress-prefabs.py). "Apply saved fits" puts every recorded fit back.</para>
///     <para>The rig lives in a scene of its own opened beside the open scenes, far above the sea, and is closed
///     without saving when the rig is removed, the window closes or play mode starts. No other scene is changed or even
///     marked as changed. Edit mode only.</para>
/// </summary>
public sealed class EquipmentFitWindow : EditorWindow
{
    private const string RigName = "Equipment Fit Rig (not saved)";
    internal const string FitsFile = "tools/items/equipment-fits.json";
    private static readonly Vector3 RigSpot = new Vector3(0f, 150f, 0f);

    internal enum Slot { MainHand, OffHand, Head, Neck }

    internal static readonly string[] SlotLabels = { "Main hand", "Off hand", "Head", "Neck" };

    private static readonly string[] SlotBones =
    {
        "Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_R/Shoulder_R/Elbow_R/Hand_R",
        "Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_L/Shoulder_L/Elbow_L/Hand_L",
        "Root/Hips/Spine_01/Spine_02/Spine_03/Neck/Head/Head_Attachment",
        "Root/Hips/Spine_01/Spine_02/Spine_03/Neck",
    };

    /// <summary>A kind of item: where its prefabs are, which slot it hangs on, and which animations suit it.</summary>
    internal sealed class Kind
    {
        public string Label;
        public Slot Slot;
        public string[] Clips;
        public readonly List<string> Prefabs = new List<string>();
    }

    private sealed class Held
    {
        public string Path;
        public Transform Holder;
        public GameObject Instance;
    }

    [Serializable] private sealed class Fit { public string prefab; public Vector3 position; public Vector3 rotation; public Vector3 scale; public string saved; }
    [Serializable] private sealed class FitList { public List<Fit> fits = new List<Fit>(); }

    internal readonly List<Kind> kinds = new List<Kind>();
    private readonly Held[] held = new Held[4];
    // what the window remembers through a script reload or play mode (the rig itself is made again)
    [SerializeField] private string[] chosen = new string[4];
    [SerializeField] private Slot editing = Slot.OffHand;
    [SerializeField] private bool wasUp;

    private GameObject rig;
    private Scene rigScene;
    private GameObject character;
    private Animator animator;
    private ItemManager rigItems;
    private readonly List<Material> copies = new List<Material>();
    [SerializeField] private bool male = true;
    private AnimationClip[] clips = new AnimationClip[0];
    [SerializeField] private int clipIndex;
    [SerializeField] private float clipTime = 0.25f;
    private bool playing;
    [SerializeField] private float speed = 1f;
    private double lastTick;
    [SerializeField] private bool showForearm = true;
    private (Vector3 p, Quaternion r, Vector3 s)? copied;

    [MenuItem("Ravenfall/Equipment Fit")]
    public static void Open()
    {
        var w = GetWindow<EquipmentFitWindow>("Equipment Fit");
        w.minSize = new Vector2(420, 560);
        w.Show();
    }

    private void OnEnable()
    {
        if (chosen == null || chosen.Length != 4) chosen = new string[4];
        FindKinds();
        CloseLeftoverRigScenes();
        // after a script reload the rig is gone: it comes back as it was
        EditorApplication.update += Tick;
        SceneView.duringSceneGui += OnSceneGui;
        EditorApplication.playModeStateChanged += OnPlayMode;
        AssemblyReloadEvents.beforeAssemblyReload += RemoveRig;
    }

    private void OnDestroy() => wasUp = false;

    private void OnDisable()
    {
        EditorApplication.update -= Tick;
        SceneView.duringSceneGui -= OnSceneGui;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        AssemblyReloadEvents.beforeAssemblyReload -= RemoveRig;
        RemoveRig();
    }

    private void OnPlayMode(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingEditMode) RemoveRig();
        if (change == PlayModeStateChange.EnteredEditMode) RefreshUi();
    }

    // ---- the kinds of items ---------------------------------------------------------------------------------------

    private void FindKinds()
    {
        kinds.Clear();
        Kind Add(string label, Slot slot, string clipWords)
        {
            var k = new Kind { Label = label, Slot = slot, Clips = clipWords.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray() };
            kinds.Add(k);
            return k;
        }
        var shields = Add("Shields", Slot.OffHand, "Shield,Armed,Sword-Attack,Block");
        var bows = Add("Bows", Slot.OffHand, "Bow,Archer,Aim");
        var swords = Add("Swords", Slot.MainHand, "Armed,Sword,2Hand,Attack");
        var axes = Add("Axes", Slot.MainHand, "Armed,2Hand,Attack,Chop");
        var spears = Add("Spears", Slot.MainHand, "Spear,2Hand,Armed,Attack");
        var staffs = Add("Staffs", Slot.MainHand, "Staff,Magic,Spell,Cast,2Hand");
        var maces = Add("Maces and others", Slot.MainHand, "Armed,Attack,2Hand");
        var tools = Add("Tools", Slot.MainHand, "Mine,Chop,Craft,Fish,Armed");
        var heads = Add("Head pieces", Slot.Head, "Idle,Cheer,Run");
        var necks = Add("Necklaces", Slot.Neck, "Idle,Cheer,Run");

        void Folder(Kind k, string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!k.Prefabs.Contains(path)) k.Prefabs.Add(path);
            }
        }
        const string W = "Assets/Resources/Character/Weapons/";
        Folder(shields, W + "Shields");
        Folder(bows, W + "Bows");
        Folder(swords, W + "Swords");
        Folder(axes, W + "Axes");
        Folder(spears, W + "Spears");
        Folder(staffs, W + "Staffs");
        Folder(tools, W + "Tools");
        Folder(heads, "Assets/Resources/Character/Appearance/Head");
        Folder(necks, "Assets/Resources/Character/Appearance/Neck");
        Folder(necks, "Assets/Resources/Character/Male/Accessories/Necklaces");
        Folder(necks, "Assets/Resources/Character/Female/Accessories/Necklaces");
        // the Dark Fortress weapons are in one folder: each goes with its kind by its name
        if (AssetDatabase.IsValidFolder(W + "DarkFortress"))
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { W + "DarkFortress" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path);
                var k = name.Contains("Shield") ? shields : name.Contains("Sword") ? swords : name.Contains("Axe") ? axes
                    : name.Contains("Staff") || name.Contains("Scythe") ? staffs : name.Contains("Bow") ? bows : maces;
                k.Prefabs.Add(path);
            }
        }
        foreach (var k in kinds) k.Prefabs.Sort((a, b) => string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));
        kinds.RemoveAll(k => k.Prefabs.Count == 0);
    }

    internal Kind KindOf(string path) => path == null ? null : kinds.FirstOrDefault(k => k.Prefabs.Contains(path));

    internal static string NiceName(string path) => path == null ? "" : Path.GetFileNameWithoutExtension(path);

    internal static string Where(string path)
    {
        if (path == null) return "";
        if (path.Contains("/DarkFortress/")) return "Dark Fortress";
        if (path.Contains("/Other/")) return "Other";
        return "";
    }

    // ---- the rig --------------------------------------------------------------------------------------------------

    /// <summary>
    ///     Runs <paramref name="make"/> with the rig's scene active, so what it makes lands there, and keeps every other
    ///     scene as it was: a scene that was not marked as changed before is not marked after.
    /// </summary>
    private void Guarded(Action make)
    {
        var before = new List<(Scene s, bool dirty)>();
        for (var i = 0; i < SceneManager.sceneCount; i++) { var s = SceneManager.GetSceneAt(i); before.Add((s, s.isDirty)); }
        var active = SceneManager.GetActiveScene();
        var switched = rigScene.IsValid() && rigScene.isLoaded && active != rigScene;
        if (switched) SceneManager.SetActiveScene(rigScene);
        try { make(); }
        finally
        {
            if (switched && active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            foreach (var (s, dirty) in before) if (s.IsValid() && s.isLoaded && !dirty && s.isDirty) ClearDirty(s);
            ClearDirty(rigScene);
        }
    }

    private static readonly MethodInfo ClearSceneDirtiness = typeof(EditorSceneManager).GetMethod("ClearSceneDirtiness", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

    private static void ClearDirty(Scene s)
    {
        if (ClearSceneDirtiness != null && s.IsValid() && s.isLoaded && s.isDirty) ClearSceneDirtiness.Invoke(null, new object[] { s });
    }

    private double nextRebuildTry;

    /// <summary>Called every editor tick: the rig was up and is gone (a script reload, play mode): made again once Unity is idle.</summary>
    private void RebuildIfWanted()
    {
        if (!wasUp || rig || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        var now = EditorApplication.timeSinceStartup;
        if (now < nextRebuildTry) return;
        nextRebuildTry = now + 5.0;
        BuildRig();
    }

    /// <summary>A rig scene left open by an editor that went away without closing it (it is never saved, so it is untitled).</summary>
    private static void CloseLeftoverRigScenes()
    {
        for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            var s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded || !string.IsNullOrEmpty(s.path) || SceneManager.sceneCount < 2) continue;
            if (!s.GetRootGameObjects().Any(g => g.name == RigName)) continue;
            var others = new List<(Scene s, bool dirty)>();
            for (var j = 0; j < SceneManager.sceneCount; j++) { var o = SceneManager.GetSceneAt(j); if (o != s) others.Add((o, o.isDirty)); }
            if (SceneManager.GetActiveScene() == s) foreach (var (o, _) in others) if (o.isLoaded) { SceneManager.SetActiveScene(o); break; }
            EditorSceneManager.CloseScene(s, true);
            foreach (var (o, dirty) in others) if (o.IsValid() && o.isLoaded && !dirty && o.isDirty) ClearDirty(o);
        }
    }

    private void BuildRig()
    {
        RemoveRig();
        wasUp = true;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        if (!prefab) { ShowNotification(new GUIContent("No Assets/Prefabs/Player.prefab")); return; }

        var before = new List<(Scene s, bool dirty)>();
        for (var i = 0; i < SceneManager.sceneCount; i++) { var s = SceneManager.GetSceneAt(i); before.Add((s, s.isDirty)); }
        var active = SceneManager.GetActiveScene();
        rigScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
        foreach (var (s, dirty) in before) if (s.IsValid() && s.isLoaded && !dirty && s.isDirty) ClearDirty(s);

        Guarded(() =>
        {
            rig = new GameObject(RigName) { hideFlags = HideFlags.DontSave };
            rig.transform.position = RigSpot;
            rigItems = MakeItemManager(rig.transform);
            character = (GameObject)UnityEngine.Object.Instantiate(prefab, rig.transform);
            character.name = "Character";
            foreach (var t in character.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.DontSave;
            character.transform.localPosition = Vector3.zero;
            character.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Dress();
            animator = character.GetComponentInChildren<Animator>(true);
        });
        clips = animator && animator.runtimeAnimatorController
            ? animator.runtimeAnimatorController.animationClips.Where(c => c).GroupBy(c => c.name).Select(g => g.First()).OrderBy(c => c.name).ToArray()
            : new AnimationClip[0];
        var start = Array.FindIndex(clips, c => c.name == "Shield-Idle");
        if (clipIndex <= 0 || clipIndex >= clips.Length) clipIndex = start >= 0 ? start : 0;
        for (var s = 0; s < chosen.Length; s++) if (!string.IsNullOrEmpty(chosen[s])) Hold((Slot)s, chosen[s]);
        Pose();
        FrameSelected();
        RefreshUi();
    }

    /// <summary>
    ///     An item manager of the rig's own (so the character's look never touches the scene's): the metal tiers'
    ///     materials as tools/items/class-gear-preview.cs loads them, and no items.
    /// </summary>
    private static ItemManager MakeItemManager(Transform parent)
    {
        var go = new GameObject("Items") { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(parent, false);
        var items = go.AddComponent<ItemManager>();
        var bf = BindingFlags.NonPublic | BindingFlags.Instance;
        var names = Enum.GetNames(typeof(RavenNest.Models.ItemMaterial));
        var count = Mathf.Max(0, (int)RavenNest.Models.ItemMaterial.Apprentice - 1);
        var metal = new Material[count];
        for (var i = 0; i < metal.Length && i + 1 < names.Length; i++)
        {
            var name = names[i + 1];
            if (name == "Abraxas") name = "Ultima";
            if (name == "ElderAdamantite") name = "ElderAdamantitite";
            metal[i] = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + name + ".mat");
        }
        typeof(ItemManager).GetField("itemMaterials", bf)?.SetValue(items, metal);
        return items;
    }

    /// <summary>The player's own look, as the game sets it, with only the character's parts to be seen.</summary>
    private void Dress()
    {
        var bf = BindingFlags.NonPublic | BindingFlags.Instance;
        var app = character.GetComponentInChildren<SyntyPlayerAppearance>(true);
        if (!app) return;
        app.useMeshCombiner = false; // the combiner runs in coroutines: play mode only
        typeof(SyntyPlayerAppearance).GetField("itemManager", bf)?.SetValue(app, rigItems);
        var partObjects = new HashSet<GameObject>();
        foreach (var f in typeof(SyntyPlayerAppearance).GetFields(bf))
            if (f.FieldType == typeof(GameObject[]) && f.GetValue(app) is GameObject[] arr)
                foreach (var p in arr) if (p) partObjects.Add(p);
        foreach (var r in character.GetComponentsInChildren<Renderer>(true)) if (!partObjects.Contains(r.gameObject)) r.enabled = false;
        foreach (var c in character.GetComponentsInChildren<Light>(true)) c.enabled = false;
        foreach (var c in character.GetComponentsInChildren<Canvas>(true)) c.enabled = false;
        foreach (var c in character.GetComponentsInChildren<Camera>(true)) c.enabled = false;
        var look = new RavenNest.Models.SyntyAppearance
        {
            Gender = male ? RavenNest.Models.Gender.Male : RavenNest.Models.Gender.Female,
            Hair = 2, Head = 0, Eyebrows = 0, FacialHair = male ? 0 : -1, Cape = -1,
            SkinColor = "#C99A7A", HairColor = "#4A3524", BeardColor = "#4A3524", EyeColor = "#2B2622",
            StubbleColor = "#B98C70", ScarColor = "#BD8F72", WarPaintColor = "#C99A7A", HelmetVisible = true,
        };
        // the game's look code asks renderer.material, which in edit mode copies the material and makes Unity log an
        // "Instantiating material" error for each copy (about 14 for a character): expected here, and the copies are
        // destroyed with the rig
        try { app.SetAppearance(look, null, false); }
        catch (Exception e) { Debug.LogWarning("Equipment Fit: the character's look could not be set: " + e.Message); }
    }

    private void RemoveRig()
    {
        playing = false;
        var before = new List<(Scene s, bool dirty)>();
        for (var i = 0; i < SceneManager.sceneCount; i++) { var s = SceneManager.GetSceneAt(i); if (s != rigScene) before.Add((s, s.isDirty)); }
        if (rig)
        {
            // material copies made while dressing (ours, and any the game's code made anyway)
            foreach (var r in rig.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials) if (m && !EditorUtility.IsPersistent(m) && !copies.Contains(m)) copies.Add(m);
            DestroyImmediate(rig);
        }
        foreach (var m in copies) if (m) DestroyImmediate(m);
        copies.Clear();
        if (rigScene.IsValid() && rigScene.isLoaded)
        {
            if (SceneManager.GetActiveScene() == rigScene)
                foreach (var (s, _) in before) if (s.IsValid() && s.isLoaded) { SceneManager.SetActiveScene(s); break; }
            if (SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(rigScene, true);
        }
        foreach (var (s, dirty) in before) if (s.IsValid() && s.isLoaded && !dirty && s.isDirty) ClearDirty(s);
        rigScene = default;
        rig = null; character = null; animator = null; rigItems = null;
        for (var i = 0; i < held.Length; i++) held[i] = null;
    }

    private Transform Bone(Slot slot) => character ? character.transform.Find(SlotBones[(int)slot]) ?? FindDeep(character.transform, Path.GetFileName(SlotBones[(int)slot])) : null;

    private static Transform FindDeep(Transform t, string name)
    {
        foreach (var c in t.GetComponentsInChildren<Transform>(true)) if (c.name == name) return c;
        return null;
    }

    /// <summary>The item on its bone as the game puts it: a holder at no offset and a world scale of 1, the prefab under it.</summary>
    private void Hold(Slot slot, string path)
    {
        chosen[(int)slot] = path;
        Guarded(() =>
        {
            var h = held[(int)slot];
            if (h != null && h.Holder) DestroyImmediate(h.Holder.gameObject);
            held[(int)slot] = null;
            if (!character || string.IsNullOrEmpty(path)) return;
            var bone = Bone(slot);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!bone || !prefab) return;
            var holder = new GameObject(SlotLabels[(int)slot] + " (holder)") { hideFlags = HideFlags.DontSave };
            holder.transform.SetParent(bone, false);
            holder.transform.localPosition = Vector3.zero;
            holder.transform.localRotation = Quaternion.identity;
            var ls = bone.lossyScale;
            holder.transform.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
            var instance = (GameObject)UnityEngine.Object.Instantiate(prefab, holder.transform);
            instance.name = prefab.name;
            foreach (var t in instance.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.DontSave;
            instance.transform.localPosition = prefab.transform.localPosition;
            instance.transform.localRotation = prefab.transform.localRotation;
            instance.transform.localScale = prefab.transform.localScale;
            held[(int)slot] = new Held { Path = path, Holder = holder.transform, Instance = instance };
        });
    }

    private void Unhold(Slot slot)
    {
        chosen[(int)slot] = null;
        Guarded(() =>
        {
            var h = held[(int)slot];
            if (h != null && h.Holder) DestroyImmediate(h.Holder.gameObject);
            held[(int)slot] = null;
        });
    }

    private void Pose()
    {
        if (!animator || clips.Length == 0) return;
        var clip = clips[Mathf.Clamp(clipIndex, 0, clips.Length - 1)];
        Guarded(() => clip.SampleAnimation(animator.gameObject, clip.length * Mathf.Repeat(clipTime, 1f)));
        SceneView.RepaintAll();
    }

    private float pollTimer;

    private void Tick()
    {
        RebuildIfWanted();
        var now = EditorApplication.timeSinceStartup;
        var dt = Mathf.Min((float)(now - lastTick), 0.1f);
        lastTick = now;
        if (rigScene.IsValid()) ClearDirty(rigScene);
        if (playing && animator && clips.Length > 0)
        {
            var clip = clips[Mathf.Clamp(clipIndex, 0, clips.Length - 1)];
            clipTime = Mathf.Repeat(clipTime + dt * speed / Mathf.Max(0.01f, clip.length), 1f);
            Pose();
            timeSlider?.SetValueWithoutNotify(clipTime);
        }
        // the item moved by hand in the scene view: the fields follow
        pollTimer += dt;
        if (pollTimer > 0.1f) { pollTimer = 0f; SyncFitFields(); }
    }

    private void FrameSelected()
    {
        var sv = SceneView.lastActiveSceneView;
        var h = held[(int)editing];
        if (!sv || !character) return;
        var target = h != null && h.Instance ? h.Instance.transform : Bone(editing);
        if (!target) return;
        Selection.activeGameObject = target.gameObject;
        var b = new Bounds(target.position, Vector3.one * 0.6f);
        foreach (var r in target.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
        sv.Frame(b, false);
    }

    // ---- choosing items -------------------------------------------------------------------------------------------

    internal void Pick(Slot slot, string path)
    {
        if (!ConfirmLeave()) return;
        editing = slot;
        Hold(slot, path);
        Pose();
        FrameSelected();
        RefreshUi();
    }

    /// <summary>The next or previous item of the slot's kind, with its own saved fit.</summary>
    private void Step(Slot slot, int by)
    {
        var path = chosen[(int)slot];
        var kind = KindOf(path) ?? kinds.FirstOrDefault(k => k.Slot == slot);
        if (kind == null) return;
        var i = path == null ? -1 : kind.Prefabs.IndexOf(path);
        var next = kind.Prefabs[(int)Mathf.Repeat(i + by, kind.Prefabs.Count)];
        Pick(slot, next);
    }

    /// <summary>The item being fitted was moved and not saved: asks what to do first. False keeps it.</summary>
    private bool ConfirmLeave()
    {
        var h = held[(int)editing];
        if (h == null || !h.Instance || !Unsaved(h)) return true;
        var answer = EditorUtility.DisplayDialogComplex("Unsaved fit",
            NiceName(h.Path) + " was moved and not saved.", "Save this item", "Keep editing", "Discard");
        if (answer == 1) return false;
        if (answer == 0) Save(new List<string> { h.Path }, h.Instance.transform, false);
        return true;
    }

    private static bool Unsaved(Held h)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(h.Path);
        if (!prefab || !h.Instance) return false;
        var a = h.Instance.transform; var b = prefab.transform;
        return (a.localPosition - b.localPosition).sqrMagnitude > 1e-10f || Quaternion.Angle(a.localRotation, b.localRotation) > 0.01f || (a.localScale - b.localScale).sqrMagnitude > 1e-10f;
    }

    // ---- saving ---------------------------------------------------------------------------------------------------

    /// <summary>The prefabs of the same kind that show the same model (the same meshes).</summary>
    private List<string> SameModel(string path)
    {
        var result = new List<string> { path };
        var key = MeshKey(path);
        var kind = KindOf(path);
        if (kind == null || key.Length == 0) return result;
        foreach (var other in kind.Prefabs) if (other != path && MeshKey(other) == key) result.Add(other);
        return result;
    }

    private readonly Dictionary<string, string> meshKeys = new Dictionary<string, string>();

    private string MeshKey(string path)
    {
        if (meshKeys.TryGetValue(path, out var k)) return k;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var names = new List<string>();
        if (prefab)
        {
            foreach (var f in prefab.GetComponentsInChildren<MeshFilter>(true)) if (f.sharedMesh) names.Add(f.sharedMesh.name);
            foreach (var s in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (s.sharedMesh) names.Add(s.sharedMesh.name);
        }
        names.Sort();
        return meshKeys[path] = string.Join("|", names);
    }

    private void Save(List<string> paths, Transform fit, bool ask = true)
    {
        if (ask)
        {
            var list = string.Join("\n", paths.Take(25).Select(NiceName)) + (paths.Count > 25 ? "\n... and " + (paths.Count - 25) + " more" : "");
            if (!EditorUtility.DisplayDialog("Save the fit", "Write this position, rotation and scale into " + paths.Count + " prefab(s)?\n\n" + list, "Save", "Cancel")) return;
        }
        var p = fit.localPosition; var r = fit.localRotation; var s = fit.localScale;
        WritePrefabs(paths, p, r, s);
        ShowNotification(new GUIContent("Saved " + paths.Count + " prefab(s)"));
        // other items held that were saved too take their new fit from their prefabs
        for (var i = 0; i < held.Length; i++)
        {
            var h = held[i];
            if (h != null && h.Instance && paths.Contains(h.Path) && h.Instance.transform != fit) Hold((Slot)i, h.Path);
        }
        Pose();
        RefreshUi();
    }

    /// <summary>Writes the fit into the prefabs and records it in <see cref="FitsFile"/>.</summary>
    internal static void WritePrefabs(List<string> paths, Vector3 p, Quaternion r, Vector3 s)
    {
        var fits = LoadFits();
        foreach (var path in paths)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.transform.localPosition = p;
                root.transform.localRotation = r;
                root.transform.localScale = s;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            fits.fits.RemoveAll(f => f.prefab == path);
            fits.fits.Add(new Fit { prefab = path, position = p, rotation = r.eulerAngles, scale = s, saved = DateTime.Now.ToString("yyyy-MM-dd HH:mm") });
        }
        fits.fits.Sort((a, b) => string.CompareOrdinal(a.prefab, b.prefab));
        SaveFits(fits);
        AssetDatabase.SaveAssets();
    }

    private void ApplySavedFits()
    {
        var fits = LoadFits();
        if (fits.fits.Count == 0) return;
        if (!EditorUtility.DisplayDialog("Apply saved fits", "Write every recorded fit (" + fits.fits.Count + ") into its prefab again?\n\nFor after a script made prefabs again, or on a machine where Assets/Resources was copied without them.", "Apply", "Cancel")) return;
        var done = 0;
        foreach (var f in fits.fits)
        {
            if (!File.Exists(f.prefab)) continue;
            var root = PrefabUtility.LoadPrefabContents(f.prefab);
            try
            {
                root.transform.localPosition = f.position;
                root.transform.localEulerAngles = f.rotation;
                root.transform.localScale = f.scale;
                PrefabUtility.SaveAsPrefabAsset(root, f.prefab);
                done++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        for (var i = 0; i < held.Length; i++) if (held[i] != null) Hold((Slot)i, held[i].Path);
        Pose();
        ShowNotification(new GUIContent("Applied " + done + " fit(s)"));
        RefreshUi();
    }

    private static FitList LoadFits()
    {
        try { return File.Exists(FitsFile) ? JsonUtility.FromJson<FitList>(File.ReadAllText(FitsFile)) ?? new FitList() : new FitList(); }
        catch { return new FitList(); }
    }

    private static void SaveFits(FitList fits)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FitsFile));
        File.WriteAllText(FitsFile, JsonUtility.ToJson(fits, true));
    }

    // ---- the window (UI Toolkit) ----------------------------------------------------------------------------------

    private static Color Dim => EditorGUIUtility.isProSkin ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.35f, 0.35f, 0.35f);
    private static Color Card => EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.84f, 0.84f, 0.84f);
    private static Color CardOn => EditorGUIUtility.isProSkin ? new Color(0.17f, 0.27f, 0.38f) : new Color(0.72f, 0.82f, 0.95f);
    private static readonly Color Accent = new Color(0.95f, 0.7f, 0.15f);

    private VisualElement body, startPanel;
    private Button buildButton, removeButton;
    private ToolbarToggle maleToggle, femaleToggle;
    private readonly VisualElement[] slotCards = new VisualElement[4];
    private readonly Image[] slotIcons = new Image[4];
    private readonly Label[] slotNames = new Label[4];
    private readonly Label[] slotKinds = new Label[4];
    private PopupField<int> clipField;
    private Slider timeSlider;
    private Button playButton;
    private Toggle suitsToggle;
    private Label fitTitle, fitState, dfNote;
    private Vector3Field posField, rotField, scaleField;
    private Button saveOne, saveSame, saveKind;
    private VisualElement fitPanel, fitEmpty;
    private Label savedCount;

    private static Label Header(string text)
    {
        var l = new Label(text);
        l.style.unityFontStyleAndWeight = FontStyle.Bold;
        l.style.fontSize = 13;
        l.style.marginTop = 10; l.style.marginBottom = 4;
        return l;
    }

    private static VisualElement Row()
    {
        var r = new VisualElement();
        r.style.flexDirection = FlexDirection.Row;
        r.style.alignItems = Align.Center;
        return r;
    }

    private static Button Btn(string text, Action click, float width = -1, string tooltip = null)
    {
        var b = new Button(click) { text = text, tooltip = tooltip };
        if (width > 0) b.style.width = width;
        b.style.height = 24;
        return b;
    }

    public void CreateGUI()
    {
        var root = rootVisualElement;
        root.style.paddingLeft = 8; root.style.paddingRight = 8; root.style.paddingTop = 6; root.style.paddingBottom = 6;
        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        root.Add(scroll);

        // the character
        var top = Row();
        buildButton = Btn("Show character", BuildRig);
        buildButton.style.flexGrow = 1; buildButton.style.height = 28;
        buildButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        removeButton = Btn("Remove", RemoveRigAndRefresh, 80);
        removeButton.style.height = 28;
        var genders = new Toolbar();
        genders.style.marginLeft = 6; genders.style.height = 28;
        maleToggle = new ToolbarToggle { text = "Male", value = true };
        femaleToggle = new ToolbarToggle { text = "Female" };
        maleToggle.RegisterValueChangedCallback(e => SetGender(e.newValue));
        femaleToggle.RegisterValueChangedCallback(e => SetGender(!e.newValue));
        genders.Add(maleToggle); genders.Add(femaleToggle);
        top.Add(buildButton); top.Add(removeButton); top.Add(genders);
        scroll.Add(top);

        startPanel = new Label("Show the character to start. It stands high above the sea in a scene of its own; the open scenes are not changed. Shortcuts in this window: [ and ] for the previous and next item, Space to play or pause.\n\nBuilding the character makes Unity log about 14 red \"Instantiating material\" lines in the console. They are expected and harmless: the game's look code copies materials, which Unity only complains about outside play mode.");
        startPanel.style.whiteSpace = WhiteSpace.Normal;
        startPanel.style.color = Dim;
        startPanel.style.marginTop = 8;
        scroll.Add(startPanel);

        body = new VisualElement();
        scroll.Add(body);

        // the items
        body.Add(Header("Items"));
        for (var s = 0; s < 4; s++) body.Add(MakeSlotCard((Slot)s));

        // the animation
        body.Add(Header("Animation"));
        var clipRow = Row();
        clipField = new PopupField<int>(new List<int> { 0 }, 0, i => ClipName(i), i => ClipName(i));
        clipField.style.flexGrow = 1;
        clipField.RegisterValueChangedCallback(e => { clipIndex = e.newValue; Pose(); });
        suitsToggle = new Toggle("Suits the item") { value = true };
        suitsToggle.labelElement.style.minWidth = 0;
        suitsToggle.style.marginLeft = 6;
        suitsToggle.RegisterValueChangedCallback(_ => RefreshClips());
        clipRow.Add(clipField); clipRow.Add(suitsToggle);
        body.Add(clipRow);
        var playRow = Row();
        playButton = Btn("Play", TogglePlay, 64);
        playRow.Add(playButton);
        playRow.Add(Btn("<", () => StepFrame(-1), 28, "One frame back"));
        playRow.Add(Btn(">", () => StepFrame(1), 28, "One frame on"));
        timeSlider = new Slider(0f, 1f) { value = clipTime };
        timeSlider.style.flexGrow = 1; timeSlider.style.marginLeft = 6;
        timeSlider.RegisterValueChangedCallback(e => { clipTime = e.newValue; playing = false; UpdatePlayButton(); Pose(); });
        playRow.Add(timeSlider);
        body.Add(playRow);
        var speedRow = Row();
        var speedSlider = new Slider("Speed", 0.05f, 2f) { value = speed, showInputField = true };
        speedSlider.style.flexGrow = 1;
        speedSlider.RegisterValueChangedCallback(e => speed = e.newValue);
        var forearm = new Toggle("Forearm line") { value = showForearm };
        forearm.style.marginLeft = 8;
        forearm.RegisterValueChangedCallback(e => { showForearm = e.newValue; SceneView.RepaintAll(); });
        speedRow.Add(speedSlider); speedRow.Add(forearm);
        body.Add(speedRow);

        // the fit
        body.Add(Header("Fit"));
        fitEmpty = new Label("Pick an item in a slot to fit it.");
        fitEmpty.style.color = Dim;
        body.Add(fitEmpty);
        fitPanel = new VisualElement();
        body.Add(fitPanel);
        var titleRow = Row();
        fitTitle = new Label();
        fitTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        fitTitle.style.flexGrow = 1;
        fitState = new Label();
        titleRow.Add(fitTitle); titleRow.Add(fitState);
        fitPanel.Add(titleRow);
        var hint = new Label("Move it with the scene view's tools (W, E, R with the item selected) or the fields here. Values are relative to the bone, as the game places the item.");
        hint.style.whiteSpace = WhiteSpace.Normal; hint.style.color = Dim; hint.style.marginBottom = 4;
        fitPanel.Add(hint);
        posField = new Vector3Field("Position");
        rotField = new Vector3Field("Rotation");
        scaleField = new Vector3Field("Scale");
        posField.RegisterValueChangedCallback(e => ApplyField(t => t.localPosition = e.newValue));
        rotField.RegisterValueChangedCallback(e => ApplyField(t => t.localEulerAngles = e.newValue));
        scaleField.RegisterValueChangedCallback(e => ApplyField(t => t.localScale = e.newValue));
        fitPanel.Add(posField); fitPanel.Add(rotField); fitPanel.Add(scaleField);
        var turnRow = Row();
        turnRow.style.marginTop = 4;
        var turnLabel = new Label("Turn 90 degrees");
        turnLabel.style.width = 110;
        turnRow.Add(turnLabel);
        foreach (var (label, axis) in new[] { ("X", Vector3.right), ("Y", Vector3.up), ("Z", Vector3.forward) })
        {
            turnRow.Add(Btn("-" + label, () => Turn(axis, -90f), 38, "Turn about the item's own " + label + " axis"));
            turnRow.Add(Btn("+" + label, () => Turn(axis, 90f), 38, "Turn about the item's own " + label + " axis"));
        }
        fitPanel.Add(turnRow);
        var toolRow = Row();
        toolRow.Add(Btn("Select", FrameSelected, -1, "Select the item and frame it in the scene view"));
        toolRow.Add(Btn("Copy fit", () => { var t = Current; if (t) copied = (t.localPosition, t.localRotation, t.localScale); RefreshUi(); }));
        var paste = Btn("Paste fit", () => { if (copied != null) ApplyField(t => { t.localPosition = copied.Value.p; t.localRotation = copied.Value.r; t.localScale = copied.Value.s; }); });
        paste.name = "paste";
        toolRow.Add(paste);
        toolRow.Add(Btn("Back to saved", BackToSaved));
        foreach (var c in toolRow.Children()) c.style.flexGrow = 1;
        fitPanel.Add(toolRow);
        dfNote = new Label("This prefab is made by tools/dark-fortress/dark-fortress-prefabs.py. The fit is recorded, so after a rerun of that script press Apply saved fits.");
        dfNote.style.whiteSpace = WhiteSpace.Normal; dfNote.style.color = Accent; dfNote.style.marginTop = 4;
        fitPanel.Add(dfNote);
        saveOne = Btn("Save this item", () => { var h = held[(int)editing]; if (h != null && h.Instance) Save(new List<string> { h.Path }, h.Instance.transform); });
        saveOne.style.height = 30; saveOne.style.marginTop = 8;
        saveOne.style.unityFontStyleAndWeight = FontStyle.Bold;
        fitPanel.Add(saveOne);
        var saveRow = Row();
        saveSame = Btn("Same model", () => { var h = held[(int)editing]; if (h != null && h.Instance) Save(SameModel(h.Path), h.Instance.transform); });
        saveKind = Btn("All of kind", () => { var h = held[(int)editing]; var k = KindOf(h?.Path); if (k != null && h.Instance) Save(k.Prefabs, h.Instance.transform); });
        saveSame.style.flexGrow = 1; saveKind.style.flexGrow = 1;
        saveRow.Add(saveSame); saveRow.Add(saveKind);
        fitPanel.Add(saveRow);

        // the record
        scroll.Add(Header("Saved fits"));
        var footRow = Row();
        savedCount = new Label();
        savedCount.style.flexGrow = 1; savedCount.style.color = Dim;
        footRow.Add(savedCount);
        footRow.Add(Btn("Apply saved fits", ApplySavedFits, -1, "Write every recorded fit into its prefab again"));
        scroll.Add(footRow);

        // shortcuts while the window has focus
        root.focusable = true;
        root.RegisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown);
        root.schedule.Execute(RefreshIcons).Every(400);
        RefreshUi();
    }

    private string ClipName(int i) => i >= 0 && i < clips.Length ? clips[i].name : "-";

    private VisualElement MakeSlotCard(Slot slot)
    {
        var s = (int)slot;
        var card = Row();
        card.style.marginBottom = 4;
        card.style.paddingLeft = 6; card.style.paddingRight = 6; card.style.paddingTop = 4; card.style.paddingBottom = 4;
        card.style.borderTopLeftRadius = 4; card.style.borderTopRightRadius = 4; card.style.borderBottomLeftRadius = 4; card.style.borderBottomRightRadius = 4;
        card.style.borderLeftWidth = 3;
        card.RegisterCallback<MouseDownEvent>(e =>
        {
            if (e.target is Button) return;
            editing = slot; FrameSelected(); RefreshUi();
        });
        var icon = new Image { scaleMode = ScaleMode.ScaleToFit };
        icon.style.width = 48; icon.style.height = 48; icon.style.marginRight = 8;
        icon.style.backgroundColor = new Color(0, 0, 0, 0.15f);
        var text = new VisualElement();
        text.style.flexGrow = 1;
        var slotLabel = new Label(SlotLabels[s]);
        slotLabel.style.color = Dim; slotLabel.style.fontSize = 10;
        var name = new Label("-");
        name.style.unityFontStyleAndWeight = FontStyle.Bold;
        name.style.fontSize = 13;
        var kind = new Label();
        kind.style.color = Dim; kind.style.fontSize = 10;
        text.Add(slotLabel); text.Add(name); text.Add(kind);
        card.Add(icon); card.Add(text);
        card.Add(Btn("<", () => Step(slot, -1), 28, "Previous item  ( [ )"));
        card.Add(Btn("Pick...", () => EquipmentPickerWindow.Show(this, slot, chosen[s]), 60, "Pick an item from a list with pictures"));
        card.Add(Btn(">", () => Step(slot, 1), 28, "Next item  ( ] )"));
        card.Add(Btn("x", () => { Unhold(slot); RefreshUi(); }, 24, "Take it off"));
        slotCards[s] = card; slotIcons[s] = icon; slotNames[s] = name; slotKinds[s] = kind;
        return card;
    }

    private void OnKey(KeyDownEvent e)
    {
        var el = e.target as VisualElement;
        if (el is TextField || el?.GetFirstAncestorOfType<TextField>() != null) return;
        if (el is FloatField || el?.GetFirstAncestorOfType<FloatField>() != null) return;
        if (e.keyCode == KeyCode.RightBracket) { Step(editing, 1); e.StopPropagation(); }
        else if (e.keyCode == KeyCode.LeftBracket) { Step(editing, -1); e.StopPropagation(); }
        else if (e.keyCode == KeyCode.Space) { TogglePlay(); e.StopPropagation(); }
    }

    private void RemoveRigAndRefresh() { wasUp = false; RemoveRig(); RefreshUi(); }

    private void SetGender(bool isMale)
    {
        maleToggle?.SetValueWithoutNotify(isMale);
        femaleToggle?.SetValueWithoutNotify(!isMale);
        if (male == isMale) return;
        male = isMale;
        if (rig) BuildRig();
    }

    private void TogglePlay()
    {
        playing = !playing;
        lastTick = EditorApplication.timeSinceStartup;
        UpdatePlayButton();
    }

    private void UpdatePlayButton() { if (playButton != null) playButton.text = playing ? "Pause" : "Play"; }

    private void StepFrame(int frames)
    {
        if (clips.Length == 0) return;
        playing = false;
        var clip = clips[Mathf.Clamp(clipIndex, 0, clips.Length - 1)];
        var rate = clip.frameRate > 0 ? clip.frameRate : 30f;
        clipTime = Mathf.Repeat(clipTime + frames / rate / Mathf.Max(0.01f, clip.length), 1f);
        timeSlider?.SetValueWithoutNotify(clipTime);
        UpdatePlayButton();
        Pose();
    }

    private Transform Current
    {
        get { var h = held[(int)editing]; return h != null && h.Instance ? h.Instance.transform : null; }
    }

    private void ApplyField(Action<Transform> change)
    {
        var t = Current;
        if (!t) return;
        Guarded(() => { Undo.RecordObject(t, "Equipment fit"); change(t); });
        SceneView.RepaintAll();
        SyncFitFields();
    }

    private void Turn(Vector3 axis, float degrees) => ApplyField(t => t.localRotation = t.localRotation * Quaternion.AngleAxis(degrees, axis));

    private void BackToSaved()
    {
        var h = held[(int)editing];
        if (h == null || !h.Instance) return;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(h.Path);
        ApplyField(t => { t.localPosition = prefab.transform.localPosition; t.localRotation = prefab.transform.localRotation; t.localScale = prefab.transform.localScale; });
    }

    private void SyncFitFields()
    {
        if (posField == null || fitState == null) return;
        var t = Current;
        if (!t) return;
        var focused = posField.focusController?.focusedElement as VisualElement;
        var editingFields = focused != null && (posField.Contains(focused) || rotField.Contains(focused) || scaleField.Contains(focused));
        if (!editingFields)
        {
            posField.SetValueWithoutNotify(t.localPosition);
            rotField.SetValueWithoutNotify(t.localEulerAngles);
            scaleField.SetValueWithoutNotify(t.localScale);
        }
        var h = held[(int)editing];
        var unsaved = h != null && Unsaved(h);
        fitState.text = unsaved ? "moved, not saved" : "as saved";
        fitState.style.color = unsaved ? Accent : Dim;
    }

    private void RefreshClips()
    {
        if (clipField == null) return;
        var h = held[(int)editing];
        var kind = KindOf(h?.Path);
        var list = new List<int>();
        var suits = suitsToggle != null && suitsToggle.value;
        for (var i = 0; i < clips.Length; i++)
            if (!suits || kind == null || kind.Clips.Any(w => clips[i].name.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)) list.Add(i);
        if (clips.Length > 0 && !list.Contains(clipIndex)) list.Insert(0, clipIndex);
        if (list.Count == 0) list.Add(0);
        clipField.choices = list;
        clipField.SetValueWithoutNotify(Mathf.Clamp(clipIndex, 0, Mathf.Max(0, clips.Length - 1)));
        if (suitsToggle != null) suitsToggle.text = kind != null ? "Suits " + kind.Label.ToLowerInvariant() : "Suits the item";
    }

    private void RefreshIcons()
    {
        for (var s = 0; s < 4; s++)
        {
            if (slotIcons[s] == null) continue;
            var path = chosen[s];
            slotIcons[s].image = path == null ? null : Icon(path);
        }
    }

    internal static Texture2D Icon(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!prefab) return null;
        return AssetPreview.GetAssetPreview(prefab) ?? AssetPreview.GetMiniThumbnail(prefab);
    }

    private void RefreshUi()
    {
        if (body == null) return;
        var on = rig != null && !EditorApplication.isPlayingOrWillChangePlaymode;
        body.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        startPanel.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
        buildButton.text = on ? "Rebuild character" : "Show character";
        removeButton.SetEnabled(on);
        maleToggle.SetValueWithoutNotify(male);
        femaleToggle.SetValueWithoutNotify(!male);
        for (var s = 0; s < 4; s++)
        {
            var path = chosen[s];
            var kind = KindOf(path);
            slotNames[s].text = path == null ? "-" : NiceName(path);
            var where = Where(path);
            slotKinds[s].text = path == null ? "nothing held" : (kind?.Label ?? "") + (where.Length > 0 ? "  -  " + where : "");
            slotCards[s].style.backgroundColor = editing == (Slot)s ? CardOn : Card;
            slotCards[s].style.borderLeftColor = editing == (Slot)s ? Accent : Card;
        }
        RefreshIcons();
        RefreshClips();
        timeSlider?.SetValueWithoutNotify(clipTime);
        UpdatePlayButton();
        var h = held[(int)editing];
        var has = h != null && h.Instance;
        fitPanel.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
        fitEmpty.style.display = has ? DisplayStyle.None : DisplayStyle.Flex;
        if (has)
        {
            var kind = KindOf(h.Path);
            fitTitle.text = SlotLabels[(int)editing] + ": " + NiceName(h.Path);
            dfNote.style.display = h.Path.Contains("/DarkFortress/") ? DisplayStyle.Flex : DisplayStyle.None;
            var same = SameModel(h.Path);
            saveSame.text = "Save all with the same model (" + same.Count + ")";
            saveSame.SetEnabled(same.Count > 1);
            saveKind.text = kind != null ? "Save all " + kind.Label.ToLowerInvariant() + " (" + kind.Prefabs.Count + ")" : "Save all of kind";
            saveKind.SetEnabled(kind != null);
            fitPanel.Q<Button>("paste")?.SetEnabled(copied != null);
            SyncFitFields();
        }
        savedCount.text = LoadFits().fits.Count + " fit(s) recorded in " + FitsFile;
    }

    // ---- the scene view -------------------------------------------------------------------------------------------

    private void OnSceneGui(SceneView sv)
    {
        if (!showForearm || !character) return;
        if (editing != Slot.MainHand && editing != Slot.OffHand) return;
        var hand = Bone(editing);
        if (!hand || !hand.parent) return;
        Handles.color = new Color(1f, 0.8f, 0.1f, 1f);
        Handles.DrawLine(hand.parent.position, hand.position, 3f);
        Handles.SphereHandleCap(0, hand.parent.position, Quaternion.identity, 0.02f, EventType.Repaint);
        Handles.SphereHandleCap(0, hand.position, Quaternion.identity, 0.02f, EventType.Repaint);
    }
}

/// <summary>The item picker of <see cref="EquipmentFitWindow"/>: every item of the slot's kinds with its picture, filtered by name.</summary>
public sealed class EquipmentPickerWindow : EditorWindow
{
    private EquipmentFitWindow owner;
    private EquipmentFitWindow.Slot slot;
    private string current;
    private readonly List<string> paths = new List<string>();
    private readonly List<VisualElement> tiles = new List<VisualElement>();
    private ToolbarSearchField search;
    private ScrollView scroll;
    private VisualElement grid;
    private string kindFilter;
    private int selected = -1;

    internal static void Show(EquipmentFitWindow owner, EquipmentFitWindow.Slot slot, string current)
    {
        var w = CreateInstance<EquipmentPickerWindow>();
        w.owner = owner; w.slot = slot; w.current = current;
        w.titleContent = new GUIContent("Pick: " + EquipmentFitWindow.SlotLabels[(int)slot]);
        var at = owner.position;
        w.position = new Rect(at.x + 20, at.y + 60, 640, 560);
        w.ShowAuxWindow();
        w.Build();
    }

    private void Build()
    {
        var root = rootVisualElement;
        root.Clear();
        root.style.paddingLeft = 8; root.style.paddingRight = 8; root.style.paddingTop = 8; root.style.paddingBottom = 8;
        search = new ToolbarSearchField();
        search.style.width = StyleKeyword.Auto;
        search.RegisterValueChangedCallback(_ => Fill());
        root.Add(search);
        var chips = new VisualElement();
        chips.style.flexDirection = FlexDirection.Row;
        chips.style.flexWrap = Wrap.Wrap;
        chips.style.marginTop = 4; chips.style.marginBottom = 6;
        var kinds = owner.kinds.Where(k => k.Slot == slot).ToList();
        var currentKind = owner.KindOf(current);
        kindFilter = currentKind?.Label;
        void Chip(string label, string value)
        {
            var b = new ToolbarToggle { text = label, value = kindFilter == value };
            b.RegisterValueChangedCallback(e =>
            {
                if (!e.newValue) { b.SetValueWithoutNotify(true); return; }
                kindFilter = value;
                foreach (var c in chips.Children()) if (c is ToolbarToggle t && t != b) t.SetValueWithoutNotify(false);
                Fill();
            });
            chips.Add(b);
        }
        if (kinds.Count > 1) Chip("All", null);
        foreach (var k in kinds) Chip(k.Label + " (" + k.Prefabs.Count + ")", k.Label);
        root.Add(chips);
        scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.flexWrap = Wrap.Wrap;
        scroll.Add(grid);
        root.Add(scroll);
        var help = new Label("Type to filter. Arrow keys to move, Enter to pick, Esc to close. Double-click a picture to pick it.");
        help.style.color = new Color(0.6f, 0.6f, 0.6f);
        help.style.fontSize = 10;
        help.style.marginTop = 4;
        root.Add(help);
        root.RegisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown);
        root.schedule.Execute(RefreshIcons).Every(300);
        Fill();
        search.Focus();
    }

    private void Fill()
    {
        paths.Clear(); tiles.Clear(); grid.Clear();
        var text = search.value ?? "";
        foreach (var k in owner.kinds)
        {
            if (k.Slot != slot) continue;
            if (kindFilter != null && k.Label != kindFilter) continue;
            foreach (var p in k.Prefabs)
                if (text.Length == 0 || EquipmentFitWindow.NiceName(p).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 || EquipmentFitWindow.Where(p).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    paths.Add(p);
        }
        for (var i = 0; i < paths.Count; i++)
        {
            var index = i;
            var path = paths[i];
            var tile = new VisualElement();
            tile.style.width = 96; tile.style.height = 118;
            tile.style.marginRight = 4; tile.style.marginBottom = 4;
            tile.style.paddingLeft = 4; tile.style.paddingRight = 4; tile.style.paddingTop = 4;
            tile.style.borderTopLeftRadius = 4; tile.style.borderTopRightRadius = 4; tile.style.borderBottomLeftRadius = 4; tile.style.borderBottomRightRadius = 4;
            tile.style.borderTopWidth = 2; tile.style.borderBottomWidth = 2; tile.style.borderLeftWidth = 2; tile.style.borderRightWidth = 2;
            tile.tooltip = path;
            var img = new Image { scaleMode = ScaleMode.ScaleToFit, image = EquipmentFitWindow.Icon(path) };
            img.style.width = 84; img.style.height = 74;
            var name = new Label(EquipmentFitWindow.NiceName(path));
            name.style.whiteSpace = WhiteSpace.Normal;
            name.style.fontSize = 10;
            name.style.unityTextAlign = TextAnchor.UpperCenter;
            var where = EquipmentFitWindow.Where(path);
            tile.Add(img); tile.Add(name);
            if (where.Length > 0)
            {
                var w = new Label(where);
                w.style.fontSize = 9; w.style.color = new Color(0.6f, 0.6f, 0.6f); w.style.unityTextAlign = TextAnchor.UpperCenter;
                tile.Add(w);
            }
            tile.RegisterCallback<MouseDownEvent>(e =>
            {
                Select(index);
                if (e.clickCount >= 2) PickSelected();
            });
            grid.Add(tile);
            tiles.Add(tile);
        }
        selected = current != null ? paths.IndexOf(current) : -1;
        if (selected < 0 && paths.Count > 0) selected = 0;
        Highlight();
    }

    private void Select(int i)
    {
        if (paths.Count == 0) return;
        selected = Mathf.Clamp(i, 0, paths.Count - 1);
        Highlight();
        scroll?.ScrollTo(tiles[selected]);
    }

    private void Highlight()
    {
        var on = new Color(0.95f, 0.7f, 0.15f);
        var cur = new Color(0.3f, 0.55f, 0.85f);
        for (var i = 0; i < tiles.Count; i++)
        {
            var c = i == selected ? on : paths[i] == current ? cur : new Color(0, 0, 0, 0);
            tiles[i].style.borderTopColor = c; tiles[i].style.borderBottomColor = c; tiles[i].style.borderLeftColor = c; tiles[i].style.borderRightColor = c;
            tiles[i].style.backgroundColor = i == selected ? new Color(0.95f, 0.7f, 0.15f, 0.12f) : new Color(0, 0, 0, 0.12f);
        }
    }

    private void OnKey(KeyDownEvent e)
    {
        var perRow = Mathf.Max(1, (int)((grid.layout.width + 4) / 100f));
        switch (e.keyCode)
        {
            case KeyCode.Escape: Close(); e.StopPropagation(); break;
            case KeyCode.Return: case KeyCode.KeypadEnter: PickSelected(); e.StopPropagation(); break;
            case KeyCode.RightArrow: Select(selected + 1); e.StopPropagation(); break;
            case KeyCode.LeftArrow: Select(selected - 1); e.StopPropagation(); break;
            case KeyCode.DownArrow: Select(selected + perRow); e.StopPropagation(); break;
            case KeyCode.UpArrow: Select(selected - perRow); e.StopPropagation(); break;
        }
    }

    private void PickSelected()
    {
        if (selected < 0 || selected >= paths.Count || !owner) return;
        var path = paths[selected];
        Close();
        owner.Pick(slot, path);
    }

    private void RefreshIcons()
    {
        for (var i = 0; i < tiles.Count; i++)
        {
            var img = tiles[i].Q<Image>();
            if (img == null) continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            var preview = prefab ? AssetPreview.GetAssetPreview(prefab) : null;
            if (preview && img.image != preview) img.image = preview;
        }
    }
}
