// Run with: unity command eval_file --file tools/easter-eggs/world-gags.cs (MainWorld open; edit mode for the final one)
//
// Makes Assets/Resources/EasterEggs/World Gags.prefab (local, as everything under Resources): the silly scenes of
// docs/easter-eggs.md, which SillySurprises puts in the world when the game starts. MainWorld is only read (ground
// heights, where the anvil and the fountain chicken are): it is never changed or saved. The pieces are built in a
// preview scene and saved as the prefab. Every scene is a WorldGag holding its pieces; signs get their words from
// Localization when they show (UI_GAG_SIGN_...). Rerun to rebuild; positions and sizes are in the list below.
//
// - Ironhill: a sheep standing on the forge-master's anvil, and a sign asking people to stop (bottle note 18)
// - Home: a seagull guarding a stolen sandwich on a DO NOT FEED THE SEAGULLS sign by the fishing spot (notes 9, 10)
// - Away: WANTED: ONE LEFT BOOT by the pirates' market (bottle note 16, the Old Boot odd find)
// - Heim: a skeleton who waited too long by his rod on the pier, BITING ANY MINUTE NOW; a fish in a scarf (note 32)
// - Kyo: Quackers, a little yellow duckling, bobbing in the sea by the hot spring (bottle note 19, the Quackers find)
// - Ravebridge: THERE IS NO COW LEVEL by the cow field gate (under the island, so it hides with it)
// - Home, Friday the 13th only: a black cat on the town hall square, BAD LUCK TODAY. PET THE CAT ANYWAY
// - Home, April 1st only: King Clucksworth, a hen in a big crown before the town hall, and ALL HAIL KING CLUCKSWORTH
// - Templates (hidden): the fish for the fish slap, the party hat and the cone for April 1st
var world = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (world.path != "Assets/Scenes/MainWorld.unity") return "open MainWorld first";
// works in play mode too (for trying placements: the game puts it in the world again with SillySurprises reloaded)

const string Animals = "Assets/Low Poly Animated Animals/- Prefabs/Animals/";
const string Board = "Assets/Polygonal Fantasy Pack/Environment Prefabs/Misc/Text Signboard 01.prefab";
const string Out = "Assets/Resources/EasterEggs/World Gags.prefab";
const float SeaLevel = -3.31f;
var missing = new System.Collections.Generic.List<string>();
var log = new System.Text.StringBuilder();
var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var root = new UnityEngine.GameObject("World Gags");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/FOT-NewRodin Pro B SDF.asset");
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources/EasterEggs")) UnityEditor.AssetDatabase.CreateFolder("Assets/Resources", "EasterEggs");
// the sign board's face: its pack's own material has a shader this project does not have (it draws pink)
const string BoardFacePath = "Assets/Resources/EasterEggs/Sign Board Face.mat";
var boardFace = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(BoardFacePath);
if (!boardFace)
{
    boardFace = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));
    UnityEditor.AssetDatabase.CreateAsset(boardFace, BoardFacePath);
}
boardFace.SetColor("_BaseColor", new UnityEngine.Color(0.17f, 0.11f, 0.07f));
boardFace.SetFloat("_Smoothness", 0.1f);
UnityEditor.EditorUtility.SetDirty(boardFace);

// what is under a point in MainWorld, looked for from a little above the height it should be at (roofs and trees
// above are skipped that way)
UnityEngine.Vector3 Ground(float x, float y, float z)
{
    var ps = world.GetPhysicsScene();
    if (ps.Raycast(new UnityEngine.Vector3(x, y + 1.0f, z), UnityEngine.Vector3.down, out var hit, 5f, ~0, UnityEngine.QueryTriggerInteraction.Ignore)) return hit.point;
    missing.Add("no ground at " + x + ", " + y + ", " + z);
    return new UnityEngine.Vector3(x, y, z);
}
// "Parent/Name": a scene object by its name and its parent's
UnityEngine.Transform FindInWorld(string path)
{
    var parts = path.Split('/');
    var name = parts[parts.Length - 1];
    var parent = parts.Length > 1 ? parts[parts.Length - 2] : null;
    foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None))
        if (t.name == name && t.gameObject.scene == world && (parent == null || (t.parent && t.parent.name == parent))) return t;
    missing.Add("no " + path + " in MainWorld");
    return null;
}
UnityEngine.Bounds BoundsOf(UnityEngine.GameObject o)
{
    var rs = o.GetComponentsInChildren<UnityEngine.Renderer>();
    var b = rs.Length > 0 ? rs[0].bounds : new UnityEngine.Bounds(o.transform.position, UnityEngine.Vector3.zero);
    foreach (var r in rs) b.Encapsulate(r.bounds);
    return b;
}
WorldGag Gag(string key, string day = "", string island = "")
{
    var g = new UnityEngine.GameObject(key);
    g.transform.SetParent(root.transform, false);
    var gag = g.AddComponent<WorldGag>();
    gag.Key = key; gag.Day = day; gag.Island = island;
    gag.Texts = new TMPro.TextMeshPro[0]; gag.TextKeys = new string[0];
    return gag;
}
UnityEngine.GameObject Put(string path, UnityEngine.Transform parent, UnityEngine.Vector3 at, float yaw, float scale = 1f)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (!prefab) { missing.Add(path); return null; }
    var o = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
    o.transform.SetPositionAndRotation(at, UnityEngine.Quaternion.Euler(0f, yaw, 0f));
    o.transform.localScale = UnityEngine.Vector3.one * scale;
    foreach (var c in o.GetComponentsInChildren<UnityEngine.Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
    return o;
}
// an animal from the Low Poly pack, in a holder of its own (its clips move its root): its own scripts off, a quiet
// receiver for its clips' sound events, and a state to hold
UnityEngine.GameObject Animal(string name, UnityEngine.Transform parent, UnityEngine.Vector3 at, float yaw, float scale, string state = null, bool still = false, string[] bools = null)
{
    var holder = new UnityEngine.GameObject(name);
    holder.transform.SetParent(parent, false);
    holder.transform.SetPositionAndRotation(at, UnityEngine.Quaternion.Euler(0f, yaw, 0f));
    holder.transform.localScale = UnityEngine.Vector3.one * scale;
    var o = Put(Animals + name + ".prefab", holder.transform, at, yaw, 1f);
    if (!o) return null;
    o.transform.localPosition = UnityEngine.Vector3.zero;
    o.transform.localRotation = UnityEngine.Quaternion.identity;
    o.transform.localScale = UnityEngine.Vector3.one;
    // the pack's prefabs hold a script that no longer exists: unpacked, so the missing one can be taken off
    UnityEditor.PrefabUtility.UnpackPrefabInstance(o, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
    foreach (var t in o.GetComponentsInChildren<UnityEngine.Transform>(true)) UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
    foreach (var m in o.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true)) if (m) UnityEngine.Object.DestroyImmediate(m);
    // a navmesh agent would pull it to the nearest walkable ground (off the anvil); no physics or sounds either
    foreach (var a in o.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true)) UnityEngine.Object.DestroyImmediate(a);
    foreach (var a in o.GetComponentsInChildren<UnityEngine.Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(a);
    foreach (var a in o.GetComponentsInChildren<UnityEngine.AudioSource>(true)) UnityEngine.Object.DestroyImmediate(a);
    var animator = o.GetComponentInChildren<UnityEngine.Animator>();
    if (animator)
    {
        animator.cullingMode = UnityEngine.AnimatorCullingMode.CullCompletely;
        animator.applyRootMotion = true; // the clips' own root movement stays a (tiny) motion instead of a jump
        animator.gameObject.AddComponent<MonkeyVoice>(); // the clips' AnimalSound events, answered quietly
        if (!string.IsNullOrEmpty(state) || bools != null)
        {
            var pose = animator.gameObject.AddComponent<SillyAnimalPose>();
            pose.State = state;
            pose.TrueBools = bools;
        }
        if (still) animator.enabled = false;
    }
    return holder;
}
// a standing sign board with words on it, facing yaw (the words face the board's +Z)
UnityEngine.GameObject Sign(WorldGag gag, UnityEngine.Vector3 at, float yaw, string textKey, float scale = 1.3f)
{
    var post = Put(Board, gag.transform, at - UnityEngine.Vector3.up * 0.04f, yaw, scale);
    if (!post) return null;
    UnityEngine.Transform face = null;
    foreach (var mf in post.GetComponentsInChildren<UnityEngine.MeshFilter>()) if (mf.name.EndsWith("Text")) face = mf.transform;
    if (!face) { missing.Add("no text face on the board"); return post; }
    var mb = face.GetComponent<UnityEngine.MeshFilter>().sharedMesh.bounds;
    face.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial = boardFace;
    var label = new UnityEngine.GameObject("Words");
    label.transform.SetParent(face, false);
    var tmp = label.AddComponent<TMPro.TextMeshPro>();
    if (font) tmp.font = font;
    tmp.rectTransform.sizeDelta = new UnityEngine.Vector2(mb.size.x * 0.84f, mb.size.y * 0.82f);
    label.transform.localPosition = new UnityEngine.Vector3(mb.center.x, mb.center.y, mb.max.z + 0.006f);
    label.transform.localRotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f);
    tmp.enableAutoSizing = true; tmp.fontSizeMin = 0.05f; tmp.fontSizeMax = 1.2f;
    tmp.alignment = TMPro.TextAlignmentOptions.Center;
    tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
    tmp.color = new UnityEngine.Color(0.97f, 0.91f, 0.74f);
    tmp.fontStyle = TMPro.FontStyles.Normal; // the font is the bold cut already
    tmp.lineSpacing = -12f;
    var field = typeof(Localization).GetField(textKey);
    tmp.text = field != null ? (string)field.GetValue(null) : textKey;
    var texts = new System.Collections.Generic.List<TMPro.TextMeshPro>(gag.Texts) { tmp };
    var keys = new System.Collections.Generic.List<string>(gag.TextKeys) { textKey };
    gag.Texts = texts.ToArray(); gag.TextKeys = keys.ToArray();
    return post;
}

// ---- Ironhill: a sheep on the anvil -----------------------------------------------------------------------------
{
    var gag = Gag("ironhill-sheep-on-the-anvil");
    var anvil = FindInWorld("Dwarf Hold/SM_Prop_Anvil_01");
    if (anvil)
    {
        var top = BoundsOf(anvil.gameObject);
        Animal("Sheep", gag.transform, new UnityEngine.Vector3(top.center.x, top.max.y - 0.03f, top.center.z), anvil.eulerAngles.y + 170f, 0.68f, "idle");
        log.AppendLine("sheep on the anvil at " + top.center + " top " + top.max.y);
    }
    Sign(gag, Ground(27.2f, 5.4f, 455.6f), 60f, "UI_GAG_SIGN_ANVIL", 1.15f);
}

// ---- Home: a seagull guarding a stolen sandwich on a sign by the fishing spot -------------------------------------
{
    var gag = Gag("home-seagull-sandwich");
    var post = Sign(gag, Ground(121.6f, -2.7f, 79.0f), 250f, "UI_GAG_SIGN_SEAGULLS", 1.35f);
    if (post)
    {
        // the board's top edge: the posts stick out above it a little, so the gull and its loot sit between them
        UnityEngine.Bounds face = BoundsOf(post);
        foreach (var mf in post.GetComponentsInChildren<UnityEngine.MeshRenderer>()) if (mf.name.EndsWith("Text")) face = mf.bounds;
        var top = new UnityEngine.Vector3(face.center.x, face.max.y + 0.01f, face.center.z);
        var side = post.transform.right;
        Animal("Seagul", gag.transform, top + side * 0.18f, 250f - 90f, 1.0f, "Sitting", false, new[] { "isSitting" });
        Put("Assets/Synty/PolygonFantasyVillage/Prefabs/Props/SM_Prop_Food_Sandwich_01.prefab", gag.transform, top - side * 0.2f, 230f, 0.7f);
    }
}

// ---- Away: wanted, one left boot ---------------------------------------------------------------------------------
{
    var gag = Gag("away-wanted-left-boot");
    Sign(gag, Ground(228.6f, -1.6f, -287.6f), 200f, "UI_GAG_SIGN_BOOT", 1.4f);
}

// ---- Heim: the skeleton who waited for a bite, and a fish in a scarf ----------------------------------------------
{
    var gag = Gag("heim-skeleton-fisher");
    Put("Assets/PolygonDungeon/Prefabs/Props/SM_Prop_Skeleton_01.prefab", gag.transform, Ground(-298.6f, -2f, -381.9f) + UnityEngine.Vector3.up * 0.02f, 95f, 0.95f);
    // the rod leans out over the water on the pier's east side
    var rod = Put("Assets/PolygonPirates/Prefabs/Props/SM_Prop_FishingRod_01.prefab", gag.transform, Ground(-297.5f, -2f, -382.2f) + UnityEngine.Vector3.up * 0.04f, 0f, 1f);
    if (rod) rod.transform.rotation = UnityEngine.Quaternion.FromToRotation(UnityEngine.Vector3.up, new UnityEngine.Vector3(0.85f, 0.55f, 0.25f).normalized);
    Sign(gag, Ground(-297.0f, -2f, -384.2f), 200f, "UI_GAG_SIGN_BITING", 1.0f);

    // a fish standing up in the half barrel on the shore, a scarf round its neck (bottle note 32: three scarves)
    var scarf = Gag("heim-fish-in-a-scarf");
    var bucket = FindInWorld("SM_Prop_Barrel_Half_01_Snow 3");
    if (bucket)
    {
        var bb = BoundsOf(bucket.gameObject);
        // lying on the rim on its belly, its side to the pier; the scarves round its neck, their ends blowing back
        var yaw = -45f;
        var fish = Animal("Fish", scarf.transform, new UnityEngine.Vector3(bb.center.x, bb.max.y, bb.center.z), yaw, 0.55f, null, true);
        if (fish)
        {
            var fb = BoundsOf(fish);
            fish.transform.position += UnityEngine.Vector3.up * (bb.max.y - 0.06f - fb.min.y);
            fb = BoundsOf(fish);
            var forward = UnityEngine.Quaternion.Euler(0f, yaw, 0f) * UnityEngine.Vector3.forward;
            for (var i = 0; i < 3; ++i)
            {
                var neck = fb.center + forward * fb.extents.z * (0.34f - i * 0.2f) + UnityEngine.Vector3.up * fb.extents.y * 0.12f;
                var wrap = Put("Assets/Synty/PolygonFantasyVillage/Prefabs/Characters/Chr_Attach/SM_Chr_Attach_Scarf_01.prefab", scarf.transform, neck, 0f, 1.2f - i * 0.1f);
                if (!wrap) continue;
                // the ring (round a neck, so flat) stood up round the body, its ends trailing back; then centred on the neck
                wrap.transform.rotation = UnityEngine.Quaternion.Euler(0f, yaw + 90f, 0f) * UnityEngine.Quaternion.Euler(-90f + (i - 1) * 12f, 0f, 0f);
                var wb = BoundsOf(wrap);
                wrap.transform.position += neck - wb.center;
            }
        }
    }
}

// ---- Kyo: Quackers in the water by the hot spring ----------------------------------------------------------------
{
    var gag = Gag("kyo-quackers-in-the-onsen");
    var duck = Animal("Chick", gag.transform, new UnityEngine.Vector3(-551.5f, SeaLevel - 0.11f, 67.5f), 210f, 3.2f, null, true);
    if (duck) duck.AddComponent<SillyBob>();
}

// ---- Ravebridge: there is no cow level ---------------------------------------------------------------------------
{
    var gag = Gag("ravebridge-no-cow-level", "", "Ravebridge");
    Sign(gag, Ground(-1589.6f, 2.6f, 1699.2f), 160f, "UI_GAG_SIGN_COW_LEVEL", 1.35f);
}

// ---- Home, Friday the 13th: a black cat on the town hall square --------------------------------------------------
{
    var gag = Gag("home-friday-13th-black-cat", "fri13");
    Animal("Cat_Black", gag.transform, Ground(-15.2f, 8f, -120.4f), 150f, 1.7f, "Cat_Sit", false, new[] { "isSitting" });
    Sign(gag, Ground(-16.5f, 8f, -118.9f), 145f, "UI_GAG_SIGN_FRIDAY", 1.1f);
}

// ---- Home, April 1st: King Clucksworth ----------------------------------------------------------------------------
// a hen in a big crown in front of the town hall fountain (every town hall tier has it there), and the sign; the
// real Sir Clucksworth the Dethroned only sits on the top tier's fountain, so the king is a visiting hen
{
    var gag = Gag("home-april-king-clucksworth", "04-01");
    var at = Ground(-19.6f, 8f, -118.4f);
    var hen = Animal("Hen", gag.transform, at, 200f, 1.5f, null);
    if (hen)
    {
        // on the hen's head bone, so it goes where the head goes
        UnityEngine.Transform head = null;
        foreach (var t in hen.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name.StartsWith("Head") && head == null) head = t;
        var crownAt = head ? head.position + UnityEngine.Vector3.up * 0.11f : BoundsOf(hen).max;
        var crown = Put("Assets/EpiXR_PolygonHats/Prefabs/Lp_CrownHat01.prefab", gag.transform, crownAt, 200f, 0.95f);
        if (crown && head) crown.transform.SetParent(head, true);
        log.AppendLine("king's head bone: " + (head ? head.name + " at " + head.position : "none"));
    }
    Sign(gag, Ground(-17.7f, 8f, -119.6f), 200f, "UI_GAG_SIGN_KING", 1.3f);
}

// ---- Templates: hidden, copied when needed -----------------------------------------------------------------------
{
    var templates = new UnityEngine.GameObject("Templates");
    templates.transform.SetParent(root.transform, false);
    var fish = Animal("Fish", templates.transform, UnityEngine.Vector3.zero, 0f, 0.45f);
    if (fish) fish.SetActive(false);
    var hat = Put("Assets/EpiXR_PolygonHats/Prefabs/Lp_PartyHat01.prefab", templates.transform, UnityEngine.Vector3.zero, 0f, 1f);
    if (hat) { hat.name = "Party Hat"; hat.SetActive(false); }
    var cone = Put("Assets/EpiXR_PolygonHats/Prefabs/Lp_TrafficCone01.prefab", templates.transform, UnityEngine.Vector3.zero, 0f, 0.8f);
    if (cone) { cone.name = "Cone"; cone.SetActive(false); }
}

UnityEditor.AssetDatabase.SaveAssetIfDirty(boardFace);
UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, Out, out var ok);
UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
return (ok ? "saved " + Out : "FAILED to save (see the console)") + "\n" + log + (missing.Count > 0 ? "missing: " + string.Join("; ", missing) : "");
