// Lines up the village house prefabs far from the island for review shots, in three runs so nothing is
// drawn as a ghost (see the README): STEP 1 places them, STEP 2 renders, STEP 3 removes them.
// Replace STEP first. Temporary objects only; the scene is not saved.
var step = STEP;
var sb = new System.Text.StringBuilder("LINEUP|");
var basePos = new UnityEngine.Vector3(3000, 400, 3000);
var existing = UnityEngine.GameObject.Find("House Lineup (temporary)");
var ci = System.Globalization.CultureInfo.InvariantCulture;
if (step == 1)
{
    if (existing) UnityEngine.Object.DestroyImmediate(existing);
    var root = new UnityEngine.GameObject("House Lineup (temporary)");
    root.hideFlags = UnityEngine.HideFlags.DontSave;
    root.transform.position = basePos;
    var defs = new System.Collections.Generic.List<TownHouse>();
    foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:TownHouse", new[] { "Assets/Prefabs/Buildings" }))
        defs.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<TownHouse>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)));
    defs.Sort((a, b) => ((int)a.Type).CompareTo((int)b.Type));
    var x = 0f; var seen = new System.Collections.Generic.HashSet<UnityEngine.GameObject>();
    // a ground plate so the shots read like the island
    var ground = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
    ground.transform.SetParent(root.transform, false);
    foreach (var d in defs)
    {
        if (!d.Prefab || !seen.Add(d.Prefab)) { sb.Append(d.Name).Append(" shares ").Append(d.Prefab ? d.Prefab.name : "nothing").Append(" || "); continue; }
        var inst = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(d.Prefab, root.transform);
        inst.transform.localPosition = new UnityEngine.Vector3(x, 0, 0);
        var rs = inst.GetComponentsInChildren<UnityEngine.Renderer>();
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        var local = b.center - inst.transform.position;
        sb.Append((int)d.Type).Append(" ").Append(d.Name).Append(" (").Append(d.Prefab.name).Append(") size ")
          .Append(b.size.x.ToString("F1", ci)).Append("x").Append(b.size.y.ToString("F1", ci)).Append("x").Append(b.size.z.ToString("F1", ci))
          .Append(" centre off ").Append(local.x.ToString("F1", ci)).Append(",").Append(local.y.ToString("F1", ci)).Append(",").Append(local.z.ToString("F1", ci))
          .Append(" min y ").Append((b.min.y - inst.transform.position.y).ToString("F1", ci)).Append(" || ");
        x += 16f;
    }
    ground.transform.localPosition = new UnityEngine.Vector3(x / 2 - 8f, -0.25f, 0);
    ground.transform.localScale = new UnityEngine.Vector3(x + 8f, 0.5f, 18f);
    sb.Append("placed, width ").Append(x);
}
else if (step == 2)
{
    var dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../Temp/village-shots")) + "/";
    var go = new UnityEngine.GameObject("LineupCam"); go.hideFlags = UnityEngine.HideFlags.DontSave;
    var cam = go.AddComponent<UnityEngine.Camera>();
    cam.enabled = false; cam.fieldOfView = 30; cam.farClipPlane = 3000;
    cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor; cam.backgroundColor = new UnityEngine.Color(0.55f, 0.7f, 0.85f);
    var rt = new UnityEngine.RenderTexture(2400, 700, 24); cam.targetTexture = rt;
    System.Action<string, UnityEngine.Vector3, UnityEngine.Vector3> shot = (name, pos, look) =>
    {
        go.transform.position = pos; go.transform.LookAt(look); cam.Render();
        UnityEngine.RenderTexture.active = rt;
        var tex = new UnityEngine.Texture2D(rt.width, rt.height, UnityEngine.TextureFormat.RGB24, false);
        tex.ReadPixels(new UnityEngine.Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
        System.IO.File.WriteAllBytes(dir + name + ".png", UnityEngine.ImageConversion.EncodeToPNG(tex));
        UnityEngine.Object.DestroyImmediate(tex);
    };
    // the row in groups of five houses, from the front-right like the build camera
    var count = existing ? existing.transform.childCount - 1 : 0;
    for (var k = 0; k * 5 < count; ++k)
    {
        var cx = k * 80f + 32f;
        shot("houses_" + (char)('a' + k), basePos + new UnityEngine.Vector3(cx + 22f, 24f, -62f), basePos + new UnityEngine.Vector3(cx, 3f, 0));
    }
    UnityEngine.RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(go); rt.Release();
    sb.Append("rendered");
}
else
{
    if (existing) UnityEngine.Object.DestroyImmediate(existing);
    sb.Append("removed");
}
UnityEngine.Debug.Log(sb.ToString());
return null;
