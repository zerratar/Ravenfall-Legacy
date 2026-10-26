// after reshaping Ironhill: moves the woodcutting tree out of the yard and seats every resource spot on the island on the ground
UnityEngine.Terrain t = null; foreach (var x in UnityEngine.Terrain.activeTerrains) if (x.name == "Terrain") t = x;
float G(float x, float z) => t.SampleHeight(new UnityEngine.Vector3(x, 0, z)) + t.transform.position.y;
var sb = new System.Text.StringBuilder(); var moved = 0;
foreach (var f in UnityEngine.Object.FindObjectsByType<ResourceField>())
{
    var changed = false;
    for (var i = 0; i < f.Count; i++)
    {
        var inst = f.Instances[i]; var p = inst.position;
        if (p.x < -110 || p.x > 120 || p.z < 270 || p.z > 575) continue;
        if ((p.x > 10 && p.x < 24 && p.z > 438 && p.z < 450) || (UnityEngine.Mathf.Abs(p.x + 27) < 0.5f && UnityEngine.Mathf.Abs(p.z - 437) < 0.5f)) { p.x = -31; p.z = 404; sb.Append("moved " + f.name + " #" + i + " out of the yard; "); }
        var g = G(p.x, p.z);
        if (UnityEngine.Mathf.Abs(p.y - g) > 0.3f) { sb.Append(f.name + " #" + i + " " + (f.TypeOf(i) ? f.TypeOf(i).name : "?") + " " + p.y.ToString("F1") + "->" + g.ToString("F1") + "; "); p.y = g; moved++; }
        if (p != inst.position) { inst.position = p; UnityEditor.Undo.RecordObject(f, "Ironhill spots"); f.Set(i, inst); changed = true; }
    }
    if (changed) { f.Changed(); UnityEditor.EditorUtility.SetDirty(f); }
}
return moved + " seated. " + sb;
