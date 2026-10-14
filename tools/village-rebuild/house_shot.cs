// Review shots of one village house, far from the island, in three runs so nothing is drawn as a ghost (see the
// README): run it with Temp/village-shots/house_shot.txt holding "<step> <Name>", step 1 places the house's model
// (its prefab, else the baked model) on a ground plate, step 2 renders a wide front corner, the other corner and a
// low close-up to Temp/village-shots/unity_<Name>_<n>.png, step 3 removes it. Temporary objects only.
var shots = "G:/Ravenfall/Projects/Ravenfall Legacy/Temp/village-shots/";
var args = System.IO.File.ReadAllText(shots + "house_shot.txt").Trim().Split(' ');
var step = int.Parse(args[0]);
var name = args[1];
var basePos = new UnityEngine.Vector3(3000, 400, 3000);
var existing = UnityEngine.GameObject.Find("House Shot (temporary)");
if (step == 1)
{
    if (existing) UnityEngine.Object.DestroyImmediate(existing);
    var root = new UnityEngine.GameObject("House Shot (temporary)");
    root.hideFlags = UnityEngine.HideFlags.DontSave;
    root.transform.position = basePos;
    // the prefab (baked model and yards) when there is one, else the model
    var model = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Village/Prefabs/House_" + name + ".prefab")
        ?? UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Village/Models/Baked/Village_House_" + name + "_Baked.fbx")
        ?? UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Village/Models/Village_House_" + name + ".fbx");
    var inst = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(model, root.transform);
    inst.transform.localPosition = UnityEngine.Vector3.zero;
    var ground = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
    ground.transform.SetParent(root.transform, false);
    ground.transform.localPosition = new UnityEngine.Vector3(0, -0.25f, 0);
    ground.transform.localScale = new UnityEngine.Vector3(24f, 0.5f, 24f);
    var plate = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));
    plate.SetColor("_BaseColor", new UnityEngine.Color(0.42f, 0.5f, 0.28f));
    ground.GetComponent<UnityEngine.Renderer>().sharedMaterial = plate;
    System.IO.File.WriteAllText(shots + "house_shot_done.txt", "placed");
}
else if (step == 2)
{
    var target = basePos + new UnityEngine.Vector3(0, 1.6f, 0);
    var views = new[] { new UnityEngine.Vector3(-10f, 8f, -13f), new UnityEngine.Vector3(11f, 7f, -12f), new UnityEngine.Vector3(-3f, 2.2f, -8.5f) };
    for (var i = 0; i < views.Length; ++i)
    {
        var go = new UnityEngine.GameObject("ZZ Shot Camera");
        var cam = go.AddComponent<UnityEngine.Camera>();
        cam.fieldOfView = i == 2 ? 55f : 45f;
        cam.farClipPlane = 600f;
        cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
        cam.backgroundColor = new UnityEngine.Color(0.55f, 0.72f, 0.88f);
        cam.transform.position = target + views[i];
        cam.transform.LookAt(i == 2 ? target + new UnityEngine.Vector3(0, -0.6f, 1.5f) : target);
        var rt = new UnityEngine.RenderTexture(1400, 1000, 24);
        cam.targetTexture = rt;
        cam.Render();
        UnityEngine.RenderTexture.active = rt;
        var tex = new UnityEngine.Texture2D(1400, 1000, UnityEngine.TextureFormat.RGB24, false);
        tex.ReadPixels(new UnityEngine.Rect(0, 0, 1400, 1000), 0, 0);
        tex.Apply();
        UnityEngine.RenderTexture.active = null;
        System.IO.File.WriteAllBytes(shots + "unity_" + name + "_" + i + ".png", UnityEngine.ImageConversion.EncodeToPNG(tex));
        cam.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);
    }
    System.IO.File.WriteAllText(shots + "house_shot_done.txt", "rendered");
}
else
{
    if (existing) UnityEngine.Object.DestroyImmediate(existing);
    System.IO.File.WriteAllText(shots + "house_shot_done.txt", "removed");
}
return null;
