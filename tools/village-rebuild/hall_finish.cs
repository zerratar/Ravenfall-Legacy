// After all tiers are built: ground the resource piles, move default plots out of the biggest hall, save.
{
    // resource piles: placed by their visible bounds, the yard centre at a set spot beside each hall
    var yards = new System.Collections.Generic.Dictionary<int, UnityEngine.Vector3> {
        { 2, new UnityEngine.Vector3(-17f, 0, 10f) }, { 3, new UnityEngine.Vector3(21f, 0, 12f) },
        { 4, new UnityEngine.Vector3(21f, 0, 14f) }, { 5, new UnityEngine.Vector3(22f, 0, 21f) } };
    foreach (var kv in yards)
    {
        var root = tierRoot(kv.Key); var res = root.Find("Resources"); if (!res) continue;
        res.rotation = root.rotation;
        var rs = res.GetComponentsInChildren<UnityEngine.Renderer>(true); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        var target = root.TransformPoint(kv.Value);
        res.position += new UnityEngine.Vector3(target.x - b.center.x, root.position.y - b.min.y, target.z - b.center.z);
        sb.Append("piles ").Append(kv.Key).Append(" size ").Append(b.size.ToString("F1")).Append(" || ");
    }
    // plots inside the tier 5 footprint (castle, plaza, stalls, resource yard) move outward to clear ground
    var slots = new System.Collections.Generic.List<TownHouseSlot>(UnityEngine.Object.FindObjectsByType<TownHouseSlot>(UnityEngine.FindObjectsInactive.Include));
    System.Func<UnityEngine.Vector3, bool> inHall = p => { var r = p - plaza; return r.x > -31 && r.x < 33 && r.z > -21 && r.z < 33; };
    UnityEngine.Transform trees = null;
    foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsInactive.Include)) if (t.name == "Tree Belt") trees = t;
    var moved = 0;
    foreach (var s in slots)
    {
        if (!inHall(s.transform.position)) continue;
        var from = s.transform.position - plaza; from.y = 0;
        var best = UnityEngine.Vector3.zero; var found = false;
        for (var ring = 40f; ring < 90f && !found; ring += 4f)
            for (var a = 0; a < 360 && !found; a += 10)
            {
                var dir = UnityEngine.Quaternion.Euler(0, UnityEngine.Mathf.Atan2(from.x, from.z) * UnityEngine.Mathf.Rad2Deg + (a % 20 == 0 ? a / 2 : -(a + 10) / 2), 0) * UnityEngine.Vector3.forward;
                var p = plaza + dir * ring; p.y = H(p.x, p.z);
                if (p.y < 6.5f || inHall(p)) continue;
                var ok = true;
                foreach (var o in slots) if (o != s && UnityEngine.Vector2.Distance(new UnityEngine.Vector2(o.transform.position.x, o.transform.position.z), new UnityEngine.Vector2(p.x, p.z)) < 15f) ok = false;
                if (!ok) continue;
                best = p; found = true;
            }
        if (!found) { sb.Append("no room for plot at ").Append(from.ToString("F0")).Append(" || "); continue; }
        s.transform.position = best; moved++;
        if (trees) for (var i = trees.childCount - 1; i >= 0; --i)
        {
            var tp = trees.GetChild(i).position;
            if (UnityEngine.Vector2.Distance(new UnityEngine.Vector2(tp.x, tp.z), new UnityEngine.Vector2(best.x, best.z)) < 9f) UnityEngine.Object.DestroyImmediate(trees.GetChild(i).gameObject);
        }
        sb.Append("plot ").Append(from.ToString("F0")).Append(" -> ").Append((best - plaza).ToString("F0")).Append(" || ");
    }
    // tier 5 is the one standing in the editor; the game picks the tier from the village level
    for (var n = 1; n <= 5; ++n) tierRoot(n).gameObject.SetActive(n == 5);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    UnityEditor.AssetDatabase.SaveAssets();
    sb.Append("moved=").Append(moved).Append(" saved || ");
}
