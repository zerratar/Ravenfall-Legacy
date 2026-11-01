// The regatta's props (docs/banana-island.md, "Pickups, hazards and meddling"): run with
//   unity command eval_file --file tools/mangrove/regatta-props.cs --timeout 120
// Makes the things the regatta puts on the water as prefabs in Assets/Resources/Mangrove/Regatta (each with its pivot
// at the waterline), and Assets/Resources/Mangrove/Regatta Props.prefab, whose RegattaProps holds them, the kit's
// effects and the start's horn. Pirate Realm's props, effects and tentacle (the Kraken's arms), the horn and the Kraken's roar.
const string Kit = "Assets/Synty/PolygonPirateRealm/Prefabs/";
const string Out = "Assets/Resources/Mangrove/Regatta/";
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources/Mangrove/Regatta")) UnityEditor.AssetDatabase.CreateFolder("Assets/Resources/Mangrove", "Regatta");

UnityEngine.GameObject Load(string path)
{
    var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
    if (!go) throw new System.Exception("Missing " + path);
    return go;
}
UnityEngine.GameObject Put(string path, UnityEngine.Transform parent, UnityEngine.Vector3 at, UnityEngine.Vector3 euler, float scale)
{
    var o = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(Load(path));
    UnityEditor.PrefabUtility.UnpackPrefabInstance(o, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
    o.transform.SetParent(parent, false);
    o.transform.localPosition = at;
    o.transform.localEulerAngles = euler;
    o.transform.localScale = UnityEngine.Vector3.one * scale;
    foreach (var c in o.GetComponentsInChildren<UnityEngine.Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
    return o;
}
UnityEngine.GameObject Save(UnityEngine.GameObject root, string name)
{
    var prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, Out + name + ".prefab");
    UnityEngine.Object.DestroyImmediate(root);
    return prefab;
}
UnityEngine.GameObject New(string name) => new UnityEngine.GameObject(name);

// a message in a bottle, as big as a person, lying in the water, glittering so it reads as a pickup from afar
var bottle = New("Regatta Bottle");
Put(Kit + "Props/SM_Prop_Bottle_01.prefab", bottle.transform, new UnityEngine.Vector3(0f, 0.15f, -1.2f), new UnityEngine.Vector3(72f, 0f, 0f), 4f);
Put(Kit + "FX/FX_Sparkles_01.prefab", bottle.transform, new UnityEngine.Vector3(0f, 0.6f, 0f), UnityEngine.Vector3.zero, 1f);
var bottlePrefab = Save(bottle, "Regatta Bottle");

// a powder keg riding low in the water, its fuse fizzing on top
var keg = New("Regatta Keg");
Put(Kit + "Props/SM_Prop_Barrel_01.prefab", keg.transform, new UnityEngine.Vector3(0f, -0.7f, 0f), UnityEngine.Vector3.zero, 1.6f);
Put(Kit + "FX/FX_Sparks_01.prefab", keg.transform, new UnityEngine.Vector3(0f, 1.0f, 0f), UnityEngine.Vector3.zero, 0.8f);
var kegPrefab = Save(keg, "Regatta Keg");

// a banana as long as a rowboat is wide: what a monkey would call a peel
var peel = New("Regatta Peel");
Put(Kit + "Props/SM_Prop_Food_Banana_01.prefab", peel.transform, new UnityEngine.Vector3(0f, 0.05f, 0.2f), new UnityEngine.Vector3(0f, 0f, 90f), 7f);
var peelPrefab = Save(peel, "Regatta Peel");

// a reef: two spiky rocks breaking the surface
var reef = New("Regatta Reef");
Put(Kit + "Environment/SM_Env_Rock_Spikes_01.prefab", reef.transform, new UnityEngine.Vector3(0.7f, -3f, 0f), UnityEngine.Vector3.zero, 0.25f);
Put(Kit + "Environment/SM_Env_Rock_Spikes_03.prefab", reef.transform, new UnityEngine.Vector3(-2.2f, -1.6f, 1.6f), new UnityEngine.Vector3(0f, 130f, 0f), 0.14f);
var reefPrefab = Save(reef, "Regatta Reef");

// floating wreckage: a broken barrel, a whole one on its side and a crate, half under
var wreck = New("Regatta Wreck");
Put(Kit + "Props/SM_Prop_Barrel_Broken_01.prefab", wreck.transform, new UnityEngine.Vector3(0f, -0.4f, 0f), new UnityEngine.Vector3(0f, 20f, 8f), 1.3f);
Put(Kit + "Props/SM_Prop_Barrel_01.prefab", wreck.transform, new UnityEngine.Vector3(1.6f, 0.05f, 1.1f), new UnityEngine.Vector3(0f, 60f, 90f), 1.1f);
Put(Kit + "Props/SM_Prop_Crate_01.prefab", wreck.transform, new UnityEngine.Vector3(-1.5f, -0.5f, -0.9f), new UnityEngine.Vector3(12f, 35f, 6f), 1.2f);
var wreckPrefab = Save(wreck, "Regatta Wreck");

var ball = New("Regatta Cannonball");
Put(Kit + "Props/SM_Prop_Cannonball_01.prefab", ball.transform, UnityEngine.Vector3.zero, UnityEngine.Vector3.zero, 4f);
var ballPrefab = Save(ball, "Regatta Cannonball");

// the Kraken's arm: Pirate Realm's tentacle, 27 m from its root at the pivot to the tip, its joints waved by
// KrakenTentacle (the kit's own animator has no controller and goes)
var tentacle = Put(Kit + "Environment/SM_Env_Tentacle_01.prefab", null, UnityEngine.Vector3.zero, UnityEngine.Vector3.zero, 1f);
tentacle.name = "Regatta Tentacle";
foreach (var a in tentacle.GetComponentsInChildren<UnityEngine.Animator>(true)) UnityEngine.Object.DestroyImmediate(a);
var joints = new System.Collections.Generic.List<UnityEngine.Transform>();
for (var j = tentacle.transform.Find("Root/TentacleJoint_01"); j; j = j.childCount > 0 ? j.GetChild(0) : null) joints.Add(j);
if (joints.Count < 5) throw new System.Exception("The tentacle's joints were not found");
tentacle.AddComponent<KrakenTentacle>().Joints = joints.ToArray();
foreach (var smr in tentacle.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true)) smr.updateWhenOffscreen = true;
var tentaclePrefab = Save(tentacle, "Regatta Tentacle");
// the Quirky octopus the Kraken used to be
UnityEditor.AssetDatabase.DeleteAsset(Out + "Regatta Kraken.prefab");

var root = New("Regatta Props");
var props = root.AddComponent<RegattaProps>();
props.Bottle = bottlePrefab;
props.Keg = kegPrefab;
props.Peel = peelPrefab;
props.Reef = reefPrefab;
props.Wreck = wreckPrefab;
props.Cannonball = ballPrefab;
props.Tentacle = tentaclePrefab;
props.Splash = Load(Kit + "FX/FX_Canon_Water_01.prefab");
props.Blast = Load(Kit + "FX/FX_Explosion_01.prefab");
props.CannonShot = Load(Kit + "FX/FX_CannonShot_01.prefab");
props.Sparkle = Load(Kit + "FX/FX_Sparkles_01.prefab");
props.Bubbles = Load(Kit + "FX/FX_Bubbles_01.prefab");
props.Horn = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>("Assets/Sounds/War Horn.mp3");
props.Roar = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>("Assets/Sounds/Kraken Roar.mp3");
var hornFound = props.Horn != null && props.Roar != null;
UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Mangrove/Regatta Props.prefab");
UnityEngine.Object.DestroyImmediate(root);
UnityEditor.AssetDatabase.SaveAssets();
return "made the regatta's props; horn and roar " + hornFound + ", tentacle joints " + joints.Count;
