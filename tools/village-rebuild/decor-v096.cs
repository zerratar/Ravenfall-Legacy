// Run with: unity command eval_file --file tools/village-rebuild/decor-v096.cs
//
// Village decor phase 4 (docs/village-decor-design.md): the pieces the newer content unlocks (hero mode's waves 10, 25
// and 50, the regatta, the pirates' raid, Ravebridge's quests), the patron ravens, and the road west's signpost that
// the village story gave without a prefab. Each is built from parts into its own decor prefab in
// Assets/Village/Decor/Resources/Decor, which VillageDecor.PrefabFor finds by key without the scene's list, so the
// main scene does not need saving. The model's foot is centred under the root, and the root carries a VillageDecorItem
// measured as a rectangle, as decor-defaults.cs does for the phase 2 props.
var sb = new System.Text.StringBuilder();
var kingdom = "Assets/PolygonFantasyKingdom/Prefabs/Props/";
var crow = "Assets/Assets/Asset Series/Quirky Series/Forest/Crow/";
var folder = "Assets/Village/Decor/Resources/Decor";
foreach (var (parent, name) in new[] { ("Assets/Village/Decor", "Resources"), ("Assets/Village/Decor/Resources", "Decor"), ("Assets/Village/Decor", "Materials") })
    if (!UnityEditor.AssetDatabase.IsValidFolder(parent + "/" + name)) UnityEditor.AssetDatabase.CreateFolder(parent, name);

// the golden raven's gold (the crow pack's own diamond material is a built-in pipeline one, pink under URP)
var goldPath = "Assets/Village/Decor/Materials/Decor_GoldRaven.mat";
var gold = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(goldPath);
if (!gold)
{
    gold = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));
    UnityEditor.AssetDatabase.CreateAsset(gold, goldPath);
}
gold.SetColor("_BaseColor", new UnityEngine.Color(1f, 0.76f, 0.3f));
gold.SetFloat("_Metallic", 1f);
gold.SetFloat("_Smoothness", 0.72f);
UnityEditor.EditorUtility.SetDirty(gold);

// a part: its prefab, where it sits (x, z), its turn and scale, an optional material; "on" puts it on top of the part
// before it instead of on the ground. An effect (an FX prefab: the disco raven's sparkles) sits at the top of the part
// before it, and is left out of the measuring
(string path, float x, float z, float turn, float scale, string material, bool on) P(string path, float scale = 1f, float x = 0f, float z = 0f, float turn = 0f, string material = null, bool on = false) => (path, x, z, turn, scale, material, on);

var pieces = new (string key, (string path, float x, float z, float turn, float scale, string material, bool on)[] parts)[]
{
    ("hero_banner_01", new[] { P(kingdom + "Banners/SM_Prop_Battle_Banner_03.prefab", 2f) }),
    ("goblin_drum_01", new[] { P("Assets/PolygonDungeon/Prefabs/Props/SM_Prop_Goblin_Spikes_02.prefab", 0.8f, 0f, 0.9f), P("Assets/PolygonDungeon/Prefabs/Props/SM_Prop_Goblin_Drum_01.prefab", 1.6f, 0f, -0.3f) }),
    ("statue_hero_ravebridge_01", new[] { P(kingdom + "SM_Prop_Statue_Hero_02.prefab") }),
    ("regatta_trophy_01", new[] { P(kingdom + "SM_Prop_Statue_Plinth_03.prefab", 0.6f), P("Assets/PolygonPirates/Prefabs/Props/SM_Prop_ShipWheel_01.prefab", 1f, on: true) }),
    ("pirate_cannon_01", new[] { P("Assets/PolygonPirates/Prefabs/Props/SM_Prop_Cannon_01.prefab", 1.3f), P("Assets/PolygonPirates/Prefabs/Props/SM_Prop_CannonBalls_03.prefab", 1.6f, 1.0f, 0.9f) }),
    ("ravebridge_lion_01", new[] { P("Assets/PolygonAncientEmpire/Prefabs/Props/SM_Prop_Statue_Lion_01.prefab") }),
    ("signpost_ravebridge_01", new[] { P(kingdom + "SM_Prop_Direction_Sign_02.prefab", 1.2f), P(kingdom + "Banners/SM_Prop_Battle_Banner_01.prefab", 1.3f, 1.0f, 0f) }),
    ("raven_01", new[] { P(kingdom + "SM_Prop_Statue_Plinth_02.prefab", 0.7f), P(crow + "Prefab/Crow.prefab", 0.45f, turn: 200f, material: crow + "Materials/Mat_Crow.mat", on: true) }),
    ("raven_gold_01", new[] { P(kingdom + "SM_Prop_Statue_Plinth_02.prefab", 0.7f), P(crow + "Prefab/Crow.prefab", 0.45f, turn: 200f, material: goldPath, on: true) }),
    ("raven_disco_01", new[] { P(kingdom + "SM_Prop_Statue_Plinth_02.prefab", 0.7f), P(crow + "Prefab/Crow.prefab", 0.45f, turn: 200f, material: crow + "Materials/Disco Crow.mat", on: true), P("Assets/Synty/PolygonPirateRealm/Prefabs/FX/FX_Sparkles_01.prefab", 0.8f, on: true) }),
};

UnityEngine.Bounds BoundsOf(UnityEngine.GameObject go)
{
    var b = new UnityEngine.Bounds(go.transform.position, UnityEngine.Vector3.zero); var first = true;
    foreach (var r in go.GetComponentsInChildren<UnityEngine.Renderer>())
    {
        if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
    }
    return b;
}

foreach (var piece in pieces)
{
    var root = new UnityEngine.GameObject(piece.key);
    try
    {
        var model = new UnityEngine.GameObject("Model");
        model.transform.SetParent(root.transform, false);
        UnityEngine.GameObject previous = null;
        var ok = true;
        foreach (var part in piece.parts)
        {
            var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(part.path);
            if (!source) { sb.AppendLine(piece.key + ": missing " + part.path); ok = false; break; }
            var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source);
            go.transform.SetParent(model.transform, false);
            go.transform.localPosition = new UnityEngine.Vector3(part.x, 0f, part.z);
            go.transform.localRotation = UnityEngine.Quaternion.Euler(0f, part.turn, 0f);
            go.transform.localScale = UnityEngine.Vector3.one * part.scale;
            // only what is drawn: the animal packs bring animators and scripts
            foreach (var a in go.GetComponentsInChildren<UnityEngine.Animator>(true)) UnityEngine.Object.DestroyImmediate(a);
            if (part.material != null)
            {
                var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(part.material);
                if (m) foreach (var r in go.GetComponentsInChildren<UnityEngine.Renderer>(true)) r.sharedMaterial = m;
            }
            var effect = part.path.Contains("/FX/");
            if (effect)
            {
                go.transform.position += UnityEngine.Vector3.up * (previous ? BoundsOf(previous).max.y - 0.2f : 1f);
                continue;
            }
            // on the ground, or on top of the part before it
            var b = BoundsOf(go);
            var floor = part.on && previous ? BoundsOf(previous).max.y : 0f;
            go.transform.position += UnityEngine.Vector3.up * (floor - b.min.y);
            previous = go;
        }
        if (!ok) continue;

        // the foot's middle at the root (effects not measured)
        var all = new UnityEngine.Bounds(); var firstPart = true;
        foreach (UnityEngine.Transform child in model.transform)
        {
            if (child.GetComponentInChildren<UnityEngine.ParticleSystem>()) continue;
            var cb = BoundsOf(child.gameObject);
            if (firstPart) { all = cb; firstPart = false; } else all.Encapsulate(cb);
        }
        model.transform.localPosition = new UnityEngine.Vector3(-all.center.x, 0f, -all.center.z);

        var item = root.AddComponent<VillageDecorItem>();
        var so = new UnityEditor.SerializedObject(item);
        so.FindProperty("piece").stringValue = piece.key;
        so.FindProperty("rectangle").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        var path = folder + "/Decor_" + piece.key + ".prefab";
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
        sb.AppendLine(piece.key + ": " + all.size.x.ToString("0.0") + " x " + all.size.z.ToString("0.0") + ", " + all.size.y.ToString("0.0") + " m high");
    }
    finally
    {
        UnityEngine.Object.DestroyImmediate(root);
    }
}
UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString();
