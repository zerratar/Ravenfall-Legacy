// Renders one tier (TIER) on its own: front view from the south, and a 3/4 view like the build camera.
// Run it twice. The GPU Resident Drawer applies activation changes at the next editor frame, so a render in
// the same eval as SetActive still draws the tiers that were just hidden (ghosts). The first run switches to
// the tier and stops; the second renders. The tier is left showing; run with TIER 5 twice to go back.
var dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../Temp/village-shots")) + "/"; System.IO.Directory.CreateDirectory(dir);
var mgr = UnityEngine.Object.FindAnyObjectByType<TownHallManager>(UnityEngine.FindObjectsInactive.Include);
var tier = TIER;
var switched = false;
foreach (UnityEngine.Transform t in mgr.transform)
{
    var want = t.name == "TownHall_Tier_" + tier;
    if (t.gameObject.activeSelf != want) { t.gameObject.SetActive(want); switched = true; }
}
if (switched) { UnityEngine.Debug.Log("HSHOT|switched to tier " + tier + ", run again to render"); return null; }
var c = mgr.transform.Find("TownHall_Tier_" + tier).position;
var go = new UnityEngine.GameObject("ReviewCam");
var cam = go.AddComponent<UnityEngine.Camera>();
cam.enabled = false; cam.fieldOfView = 40; cam.farClipPlane = 3000;
var rt = new UnityEngine.RenderTexture(1600, 900, 24);
cam.targetTexture = rt;
System.Action<string, UnityEngine.Vector3, UnityEngine.Vector3> shot = (name, pos, look) =>
{
    go.transform.position = pos; go.transform.LookAt(look);
    cam.Render();
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(1600, 900, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
    System.IO.File.WriteAllBytes(dir + name + ".png", UnityEngine.ImageConversion.EncodeToPNG(tex));
    UnityEngine.Object.DestroyImmediate(tex);
};
var dist = DIST;
shot("tier" + tier + "_front", c + new UnityEngine.Vector3(0, dist * 0.45f, -dist), c + new UnityEngine.Vector3(0, dist * 0.12f, 0));
shot("tier" + tier + "_34", c + new UnityEngine.Vector3(dist * 0.7f, dist * 0.75f, -dist * 0.7f), c + new UnityEngine.Vector3(0, dist * 0.08f, 0));
UnityEngine.RenderTexture.active = null;
UnityEngine.Object.DestroyImmediate(go); rt.Release();
UnityEngine.Debug.Log("HSHOT|done tier " + tier);
