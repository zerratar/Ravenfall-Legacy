// Renders a contact sheet of prefabs (listed in gallery.txt) to shots/gallery.png. Temporary objects only.
var dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../Temp/village-shots")) + "/"; System.IO.Directory.CreateDirectory(dir + "shots");
var paths = System.IO.File.ReadAllLines(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../tools/village-rebuild/gallery.txt"));
int cols = 5, tw = 400, th = 300;
int rows = (paths.Length + cols - 1) / cols;
var sheet = new UnityEngine.Texture2D(cols * tw, rows * th, UnityEngine.TextureFormat.RGB24, false);
var go = new UnityEngine.GameObject("GalleryCam");
var cam = go.AddComponent<UnityEngine.Camera>();
cam.enabled = false; cam.fieldOfView = 40; cam.farClipPlane = 2000;
cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor; cam.backgroundColor = new UnityEngine.Color(0.55f, 0.7f, 0.85f);
var rt = new UnityEngine.RenderTexture(tw, th, 24);
cam.targetTexture = rt;
var basePos = new UnityEngine.Vector3(3000, 400, 3000);
var sb = new System.Text.StringBuilder("GAL|");
for (var i = 0; i < paths.Length; ++i)
{
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(paths[i].Trim());
    if (!prefab) { sb.Append("missing ").Append(paths[i]).Append(" || "); continue; }
    var inst = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
    inst.transform.position = basePos;
    var rs = inst.GetComponentsInChildren<UnityEngine.Renderer>();
    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
    var radius = b.extents.magnitude;
    var view = new UnityEngine.Vector3(0.75f, 0.45f, -1f).normalized;
    go.transform.position = b.center + view * radius * 2.6f;
    go.transform.LookAt(b.center);
    cam.Render();
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(tw, th, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, tw, th), 0, 0); tex.Apply();
    int cx = (i % cols) * tw, cy = (rows - 1 - i / cols) * th;
    sheet.SetPixels(cx, cy, tw, th, tex.GetPixels());
    UnityEngine.Object.DestroyImmediate(tex);
    sb.Append(i).Append(":").Append(System.IO.Path.GetFileNameWithoutExtension(paths[i])).Append(" size=").Append(b.size.ToString("F1")).Append(" || ");
    UnityEngine.Object.DestroyImmediate(inst);
}
sheet.Apply();
System.IO.File.WriteAllBytes(dir + "shots/gallery.png", UnityEngine.ImageConversion.EncodeToPNG(sheet));
UnityEngine.RenderTexture.active = null;
UnityEngine.Object.DestroyImmediate(go); rt.Release();
UnityEngine.Debug.Log(sb.ToString());
