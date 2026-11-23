// Run with: unity command eval_file --file tools/dark-fortress/dark-fortress-prefabs.cs after dark-fortress-prefabs.py
// wrote the table. Makes the Dark Fortress weapon and head prefabs (docs/dark-fortress-design.md) from the pack's.
// Table %TEMP%/df-make.txt: out prefab path | source prefab path | lp x,y,z | euler x,y,z | scale | [model-space offset x,y,z]
// Without an offset the prefab root is the mesh (the game's usual weapon prefab); with one the root holds the transform
// and a child "Model" is the source's meshes moved by the offset (so the hand holds the grip, not the pivot).
var inv = System.Globalization.CultureInfo.InvariantCulture;
UnityEngine.Vector3 V(string s) { var p = s.Split(','); if (p.Length == 1) { var f = float.Parse(p[0], inv); return new UnityEngine.Vector3(f, f, f); } return new UnityEngine.Vector3(float.Parse(p[0], inv), float.Parse(p[1], inv), float.Parse(p[2], inv)); }
var log = new System.Text.StringBuilder();
void CopyMeshes(UnityEngine.Transform from, UnityEngine.GameObject to)
{
    var smf = from.GetComponent<UnityEngine.MeshFilter>(); var smr = from.GetComponent<UnityEngine.MeshRenderer>();
    if (smf && smr)
    {
        var mf = to.AddComponent<UnityEngine.MeshFilter>(); mf.sharedMesh = smf.sharedMesh;
        var mr = to.AddComponent<UnityEngine.MeshRenderer>(); mr.sharedMaterials = smr.sharedMaterials;
    }
    for (int i = 0; i < from.childCount; i++)
    {
        var ch = from.GetChild(i);
        if (!ch.GetComponentInChildren<UnityEngine.MeshRenderer>(true)) continue;
        var go = new UnityEngine.GameObject(ch.name);
        go.transform.SetParent(to.transform, false);
        go.transform.localPosition = ch.localPosition; go.transform.localRotation = ch.localRotation; go.transform.localScale = ch.localScale;
        CopyMeshes(ch, go);
    }
}
foreach (var raw in System.IO.File.ReadAllLines(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "df-make.txt")))
{
    if (raw.Trim().Length == 0 || raw.StartsWith("#")) continue;
    var c = raw.Split('|');
    var outPath = c[0].Trim(); var src = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(c[1].Trim());
    if (!src) { log.AppendLine("missing " + c[1]); continue; }
    var name = System.IO.Path.GetFileNameWithoutExtension(outPath);
    var go = new UnityEngine.GameObject(name);
    try
    {
        var hasOffset = c.Length > 5 && c[5].Trim().Length > 0;
        var hasChildren = src.transform.childCount > 0;
        if (hasOffset || hasChildren)
        {
            var model = new UnityEngine.GameObject("Model");
            model.transform.SetParent(go.transform, false);
            model.transform.localPosition = hasOffset ? V(c[5].Trim()) : UnityEngine.Vector3.zero;
            CopyMeshes(src.transform, model);
        }
        else CopyMeshes(src.transform, go);
        go.transform.localPosition = V(c[2].Trim());
        go.transform.localEulerAngles = V(c[3].Trim());
        go.transform.localScale = V(c[4].Trim());
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outPath));
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(go, outPath);
        log.AppendLine("saved " + outPath);
    }
    finally { UnityEngine.Object.DestroyImmediate(go); }
}
return log.ToString();
