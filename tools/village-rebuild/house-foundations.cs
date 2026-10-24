// Run with: unity command eval_file --file tools/village-rebuild/house-foundations.cs
//
// Gives every village house prefab (Assets/Village/Prefabs/House_*.prefab) a foundation: the plinth's outline carried
// straight down to FoundationDepth below the pivot, in the plinth's own brick and with its texture running on. On a
// slope a house sits at the ground height under its middle, and the plinth only reaches 0.9 m down, so the downhill
// side floated; build mode refused anything steeper than 2 m across a footprint for that reason, which ruled out much
// of the island (around the bridge, for one). With a foundation the downhill side shows more stone instead, as a house
// built into a hillside would.
//
// The outline comes from the plinth's side faces: every triangle of the plinth submesh (the lowest one) with two
// corners on its bottom and one above gives a bottom edge and the side's facing. Running it again replaces the old
// foundation. The meshes go to Assets/Village/Meshes/Foundations.
const float FoundationDepth = 3.2f;
const string MeshFolder = "Assets/Village/Meshes/Foundations";
var sb = new System.Text.StringBuilder();
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Village/Meshes")) UnityEditor.AssetDatabase.CreateFolder("Assets/Village", "Meshes");
if (!UnityEditor.AssetDatabase.IsValidFolder(MeshFolder)) UnityEditor.AssetDatabase.CreateFolder("Assets/Village/Meshes", "Foundations");

foreach (var guid in UnityEditor.AssetDatabase.FindAssets("House_ t:Prefab", new[] { "Assets/Village/Prefabs" }))
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var name = System.IO.Path.GetFileNameWithoutExtension(path);
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    try
    {
        var old = root.transform.Find("Foundation");
        if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);

        // the house model: the mesh renderer with the lowest submesh (the plinth)
        UnityEngine.MeshFilter model = null; var plinth = -1; var bottom = float.MaxValue;
        foreach (var mf in root.GetComponentsInChildren<UnityEngine.MeshFilter>(true))
        {
            if (!mf.sharedMesh) continue;
            var verts = mf.sharedMesh.vertices;
            for (var s = 0; s < mf.sharedMesh.subMeshCount; ++s)
            {
                foreach (var i in mf.sharedMesh.GetTriangles(s))
                {
                    var y = mf.transform.TransformPoint(verts[i]).y - root.transform.position.y;
                    if (y < bottom - 0.001f) { bottom = y; model = mf; plinth = s; }
                }
            }
        }
        if (model == null) { sb.AppendLine(name + ": no mesh"); continue; }

        var mesh = model.sharedMesh;
        var v = mesh.vertices; var uv = mesh.uv; var tris = mesh.GetTriangles(plinth);
        var toRoot = root.transform.worldToLocalMatrix * model.transform.localToWorldMatrix;
        System.Func<int, UnityEngine.Vector3> P = i => toRoot.MultiplyPoint3x4(v[i]);
        var ov = new System.Collections.Generic.List<UnityEngine.Vector3>();
        var on = new System.Collections.Generic.List<UnityEngine.Vector3>();
        var ouv = new System.Collections.Generic.List<UnityEngine.Vector2>();
        var ot = new System.Collections.Generic.List<int>();
        var seen = new System.Collections.Generic.HashSet<string>();
        for (var t = 0; t < tris.Length; t += 3)
        {
            var idx = new[] { tris[t], tris[t + 1], tris[t + 2] };
            var low = new System.Collections.Generic.List<int>(); var up = -1;
            foreach (var i in idx) { if (P(i).y < bottom + 0.02f) low.Add(i); else up = i; }
            if (low.Count != 2 || up < 0) continue;

            var a = low[0]; var b = low[1];
            var pa = P(a); var pb = P(b); var pc = P(up);
            var key = pa.x.ToString("0.00") + "," + pa.z.ToString("0.00") + "|" + pb.x.ToString("0.00") + "," + pb.z.ToString("0.00");
            var rev = pb.x.ToString("0.00") + "," + pb.z.ToString("0.00") + "|" + pa.x.ToString("0.00") + "," + pa.z.ToString("0.00");
            if (!seen.Add(key) || seen.Contains(rev)) continue;

            var normal = UnityEngine.Vector3.Cross(P(idx[1]) - P(idx[0]), P(idx[2]) - P(idx[0])).normalized;
            var drop = FoundationDepth + bottom; // bottom is negative: from the plinth's foot down to -FoundationDepth
            var down = new UnityEngine.Vector3(0f, -FoundationDepth - bottom, 0f);
            // how fast the texture runs up the wall, taken from the side face itself
            var rise = pc.y - pa.y;
            var dvdy = UnityEngine.Mathf.Abs(rise) > 0.01f ? (uv[up].y - uv[a].y) / rise : 0.2f;
            var depth = -down.y;
            var baseIndex = ov.Count;
            ov.Add(pa); ov.Add(pb); ov.Add(pb + down); ov.Add(pa + down);
            ouv.Add(uv[a]); ouv.Add(uv[b]); ouv.Add(new UnityEngine.Vector2(uv[b].x, uv[b].y - dvdy * depth)); ouv.Add(new UnityEngine.Vector2(uv[a].x, uv[a].y - dvdy * depth));
            for (var k = 0; k < 4; ++k) on.Add(normal);
            // wound to face the same way as the plinth's side
            var test = UnityEngine.Vector3.Cross(pb - pa, (pb + down) - pa);
            if (UnityEngine.Vector3.Dot(test, normal) >= 0f) ot.AddRange(new[] { baseIndex, baseIndex + 1, baseIndex + 2, baseIndex, baseIndex + 2, baseIndex + 3 });
            else ot.AddRange(new[] { baseIndex, baseIndex + 2, baseIndex + 1, baseIndex, baseIndex + 3, baseIndex + 2 });
        }

        if (ot.Count == 0) { sb.AppendLine(name + ": no plinth sides found"); continue; }

        var foundationMesh = new UnityEngine.Mesh { name = name + "_Foundation" };
        foundationMesh.SetVertices(ov); foundationMesh.SetNormals(on); foundationMesh.SetUVs(0, ouv); foundationMesh.SetTriangles(ot, 0);
        foundationMesh.RecalculateBounds(); foundationMesh.RecalculateTangents();
        var meshPath = MeshFolder + "/" + name + "_Foundation.asset";
        UnityEditor.AssetDatabase.DeleteAsset(meshPath);
        UnityEditor.AssetDatabase.CreateAsset(foundationMesh, meshPath);

        var go = new UnityEngine.GameObject("Foundation");
        go.transform.SetParent(root.transform, false);
        go.layer = model.gameObject.layer;
        go.AddComponent<UnityEngine.MeshFilter>().sharedMesh = foundationMesh;
        var renderer = go.AddComponent<UnityEngine.MeshRenderer>();
        renderer.sharedMaterial = model.GetComponent<UnityEngine.MeshRenderer>().sharedMaterials[plinth];
        go.isStatic = model.gameObject.isStatic;

        UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
        sb.AppendLine(name + ": " + (ot.Count / 6) + " sides, plinth foot " + bottom.ToString("0.00") + " m, down to -" + FoundationDepth + " m");
    }
    finally
    {
        UnityEditor.PrefabUtility.UnloadPrefabContents(root);
    }
}
UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString();
