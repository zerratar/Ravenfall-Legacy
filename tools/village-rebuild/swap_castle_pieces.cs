// Upgrades tiers 4 and 5 in place to the Blender towers and the frontispiece, without rebuilding the halls,
// so hand edits elsewhere in them survive. Run as hall_common.cs + this file. It does not save the scene.
// (A full rebuild with hall_tier4.cs and hall_tier5.cs gives the same pieces.)
{
    System.Func<UnityEngine.Transform, bool> replaced = t =>
        t.name.StartsWith("SM_Bld_Castle_Wall_Tower_") || t.name.StartsWith("SM_Bld_Castle_Roof_Cap_Round_") ||
        t.name == "Rose Window" || t.name.StartsWith("Village_Tower_") ||
        t.name.StartsWith("Village_Turret") || t.name == "Frontispiece";
    foreach (var n in new[] { 4, 5 })
    {
        var hall = tierRoot(n).Find("Hall"); var gone = 0;
        for (var i = hall.childCount - 1; i >= 0; --i)
        {
            var t = hall.GetChild(i);
            // tier 5 only: the old door and the banners (tier 4 keeps its big door)
            var banner = n == 5 && t.name.StartsWith("SM_Prop_Flag_11") && UnityEngine.Mathf.Abs(t.localPosition.y - 14.6f) < 0.05f;
            var door = n == 5 && t.name == "SM_Bld_Castle_Wall_Door_Big_01";
            if (replaced(t) || banner || door) { UnityEngine.Object.DestroyImmediate(t.gameObject); gone++; }
        }
        sb.Append("tier").Append(n).Append(" removed ").Append(gone).Append(" || ");
    }
    {
        var hall = tierRoot(4).Find("Hall");
        foreach (var x in new[] { -7.5f, 7.5f })
        {
            Put(hall, VM + "Village_Tower_T4_Front.fbx", new UnityEngine.Vector3(x, 0, 8f), 0, 1f);
            Put(hall, VM + "Village_Tower_T4_Back.fbx", new UnityEngine.Vector3(x, 0, 18f), 0, 1f);
        }
    }
    {
        var hall = tierRoot(5).Find("Hall");
        const float Z0 = 10f, Z1 = 25f, X0 = -12.5f;
        Put(hall, VM + "Village_Frontispiece.fbx", new UnityEngine.Vector3(X0 + 15f, 0f, Z0), 0, 1f).name = "Frontispiece";
        Put(hall, VM + "Village_Tower_T5_Spire.fbx", new UnityEngine.Vector3(0, 0, Z1 + 1.5f), 0, 1f);
        foreach (var x in new[] { X0, -X0 })
        {
            Put(hall, VM + "Village_Tower_T5_Front.fbx", new UnityEngine.Vector3(x, 0, Z0), 0, 1f);
            Put(hall, VM + "Village_Tower_T5_Back.fbx", new UnityEngine.Vector3(x, 0, Z1), 0, 1f);
        }
        foreach (var x in new[] { -5f, 5f }) Put(hall, VM + "Village_Turret.fbx", new UnityEngine.Vector3(x, 15f, Z0), 0, 1f);
        foreach (var x in new[] { -7.5f, 7.5f }) Banner(hall, new UnityEngine.Vector3(x, 14.6f, Z0 - 0.45f), 0, 1.3f);
    }
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    sb.Append("swapped, not saved || ");
}
