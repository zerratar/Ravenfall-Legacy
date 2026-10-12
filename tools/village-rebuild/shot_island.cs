// Renders views of the village to PNGs for review. Read only: a temporary camera, destroyed after.
var dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../Temp/village-shots")) + "/"; System.IO.Directory.CreateDirectory(dir);
var hall = new UnityEngine.Vector3(-23.64f, 8f, -109.72f);
var go = new UnityEngine.GameObject("ReviewCam");
var cam = go.AddComponent<UnityEngine.Camera>();
cam.enabled = false; cam.fieldOfView = 50; cam.farClipPlane = 3000;
var main = UnityEngine.Camera.main;
if (main) { cam.clearFlags = main.clearFlags; cam.backgroundColor = main.backgroundColor; }
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
shot("top", hall + new UnityEngine.Vector3(0, 300, -1), hall + new UnityEngine.Vector3(0, 0, 30));
shot("persp_s", hall + new UnityEngine.Vector3(0, 110, -170), hall + new UnityEngine.Vector3(0, 0, 30));
shot("persp_n", hall + new UnityEngine.Vector3(30, 80, 190), hall);
shot("close", hall + new UnityEngine.Vector3(30, 30, -50), hall);
UnityEngine.RenderTexture.active = null;
UnityEngine.Object.DestroyImmediate(go); rt.Release();
UnityEngine.Debug.Log("SHOTS|hall=" + hall);
