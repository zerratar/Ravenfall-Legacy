// Ironhill's lava cracks (LavaCrack): sparse glowing seams in the scorched east, a few running from the mountain's foot
// to the dragon eggs (the mountain keeps them warm), and one in the forge yard by the smelter. Rerunnable (clears its group).
var island = UnityEngine.GameObject.Find("============ WORLD/Islands/Ironhill Island").transform;
var group = island.Find("Lava Cracks");
if (group) UnityEngine.Object.DestroyImmediate(group.gameObject);
group = new UnityEngine.GameObject("Lava Cracks").transform; group.SetParent(island, false);
var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/World/Ironhill/Lava Crack.mat");
UnityEngine.Terrain t = null; foreach (var x in UnityEngine.Terrain.activeTerrains) if (x.name == "Terrain") t = x;
float G(float x, float z) => t.SampleHeight(new UnityEngine.Vector3(x, 0, z)) + t.transform.position.y;
// x, z, heading (degrees, 0 = north), seed, length, seam width, scorch width, branches, heat
var list = new[]
{
    // from the mountain's foot to the nest
    (61f, 442f, 140f, 3, 9f, 0.56f, 3.5f, 3, 1.1f),
    (75f, 443f, 200f, 7, 7f, 0.42f, 2.9f, 2, 0.9f),
    // the scorched east, few and apart
    (52f, 381f, 60f, 11, 10f, 0.49f, 3.3f, 3, 0.9f),
    (61f, 414f, 110f, 5, 7f, 0.39f, 2.6f, 2, 0.75f),
    (88f, 372f, 330f, 9, 8f, 0.42f, 2.9f, 2, 0.8f),
    (50f, 446f, 170f, 13, 6f, 0.35f, 2.4f, 2, 0.7f),
    // the forge yard: a seam from the mountain wall towards the smelter
    (8f, 468f, 215f, 17, 8f, 0.55f, 3.0f, 2, 1f),
};
var n = 0;
foreach (var (x, z, yaw, seed, len, w, scorch, br, heat) in list)
{
    var go = new UnityEngine.GameObject("Lava Crack");
    go.transform.SetParent(group, false);
    go.transform.SetPositionAndRotation(new UnityEngine.Vector3(x, G(x, z), z), UnityEngine.Quaternion.Euler(0, yaw, 0));
    go.AddComponent<LavaCrack>().Set(mat, seed, len, w, scorch, br, heat);
    n++;
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(island.gameObject.scene);
return n + " cracks";
