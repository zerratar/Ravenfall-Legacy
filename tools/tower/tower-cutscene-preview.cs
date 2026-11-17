// Previews a town's cutscene path in edit mode: opens the scene (first line of %TEMP%/tower-cutscene.txt) additively with
// its fog and light, and renders frames along the camera path the game would fly (TowerCutscene.Evaluate, eased) to
// %TEMP%/cut-NN.jpg. The other lines are poses in the town's own space, "x y z lookx looky lookz" (where the camera is,
// what it looks at); "scene" instead of poses uses the scene's own Cutscene poses (Arrival, then Opening).
// A line "frames N" sets how many frames (default 12). Writes %TEMP%/tower-cutscene-log.txt.
var tmp = System.IO.Path.GetTempPath();
var log = new System.Text.StringBuilder();
var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(tmp, "tower-cutscene.txt")).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#")).ToArray();
var path = lines[0];
var main = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var fog = UnityEngine.RenderSettings.fog; var fogColor = UnityEngine.RenderSettings.fogColor; var fogDensity = UnityEngine.RenderSettings.fogDensity; var fogMode = UnityEngine.RenderSettings.fogMode;
var amb = UnityEngine.RenderSettings.ambientMode; var sky = UnityEngine.RenderSettings.ambientSkyColor; var eq = UnityEngine.RenderSettings.ambientEquatorColor; var gr = UnityEngine.RenderSettings.ambientGroundColor;
var sun = UnityEngine.RenderSettings.sun;
var sunRot = sun ? sun.transform.rotation : UnityEngine.Quaternion.identity; var sunColor = sun ? sun.color : UnityEngine.Color.white; var sunInt = sun ? sun.intensity : 1f;
var ci = System.Globalization.CultureInfo.InvariantCulture;
try
{
    var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
    var place = scene.GetRootGameObjects().Select(g => g.GetComponent<TowerPlace>()).FirstOrDefault(f => f);
    var o = place.transform.position;
    UnityEngine.RenderSettings.fog = true; UnityEngine.RenderSettings.fogMode = UnityEngine.FogMode.ExponentialSquared;
    UnityEngine.RenderSettings.fogColor = place.FogColor; UnityEngine.RenderSettings.fogDensity = place.FogDensity;
    UnityEngine.RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
    UnityEngine.RenderSettings.ambientSkyColor = place.SkyAmbient; UnityEngine.RenderSettings.ambientEquatorColor = place.EquatorAmbient; UnityEngine.RenderSettings.ambientGroundColor = place.GroundAmbient;
    if (sun) { sun.transform.rotation = UnityEngine.Quaternion.Euler(place.SunAngles); sun.color = place.SunColor; sun.intensity = place.SunIntensity; }

    var frames = 12;
    var pos = new System.Collections.Generic.List<UnityEngine.Vector3>();
    var fwd = new System.Collections.Generic.List<UnityEngine.Vector3>();
    foreach (var l in lines.Skip(1))
    {
        var p = l.Split(' ');
        if (p[0] == "frames") { frames = int.Parse(p[1]); continue; }
        if (p[0] == "scene")
        {
            var town = place as TowerTown;
            foreach (var t in town.CutscenePoses(p.Length > 1 && p[1] == "opening")) { pos.Add(t.position); fwd.Add(t.forward); }
            continue;
        }
        System.Func<int, float> f = i => float.Parse(p[i], ci);
        var at = o + new UnityEngine.Vector3(f(0), f(1), f(2));
        pos.Add(at);
        fwd.Add((o + new UnityEngine.Vector3(f(3), f(4), f(5)) - at).normalized);
    }
    log.AppendLine("poses " + pos.Count);

    var camGo = new UnityEngine.GameObject("cutscene preview cam");
    var cam = camGo.AddComponent<UnityEngine.Camera>();
    // the game camera's own field of view
    var gameCam = UnityEngine.Camera.main;
    cam.fieldOfView = gameCam ? gameCam.fieldOfView : 60f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 1500f;
    log.AppendLine("fov " + cam.fieldOfView);
    var rt = new UnityEngine.RenderTexture(960, 540, 24);
    cam.targetTexture = rt;
    for (var k = 0; k < frames; k++)
    {
        var x = frames == 1 ? 1f : k / (float)(frames - 1);
        var (p, r) = TowerCutscene.Evaluate(pos.ToArray(), fwd.ToArray(), TowerCutscene.Ease(x));
        cam.transform.SetPositionAndRotation(p, r);
        cam.Render();
        UnityEngine.RenderTexture.active = rt;
        var tex = new UnityEngine.Texture2D(960, 540, UnityEngine.TextureFormat.RGB24, false);
        tex.ReadPixels(new UnityEngine.Rect(0, 0, 960, 540), 0, 0);
        tex.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(tmp, "cut-" + k.ToString("00") + ".jpg"), UnityEngine.ImageConversion.EncodeToJPG(tex, 85));
        UnityEngine.Object.DestroyImmediate(tex);
        var local = p - o;
        log.AppendLine(k + " at " + local.x.ToString("0.0", ci) + " " + local.y.ToString("0.0", ci) + " " + local.z.ToString("0.0", ci));
    }
    UnityEngine.RenderTexture.active = null;
    cam.targetTexture = null;
    UnityEngine.Object.DestroyImmediate(rt);
    UnityEngine.Object.DestroyImmediate(camGo);
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
}
catch (System.Exception e) { log.AppendLine("ERROR " + e); }
UnityEngine.RenderSettings.fog = fog; UnityEngine.RenderSettings.fogColor = fogColor; UnityEngine.RenderSettings.fogDensity = fogDensity; UnityEngine.RenderSettings.fogMode = fogMode;
UnityEngine.RenderSettings.ambientMode = amb; UnityEngine.RenderSettings.ambientSkyColor = sky; UnityEngine.RenderSettings.ambientEquatorColor = eq; UnityEngine.RenderSettings.ambientGroundColor = gr;
if (sun) { sun.transform.rotation = sunRot; sun.color = sunColor; sun.intensity = sunInt; }
UnityEngine.SceneManagement.SceneManager.SetActiveScene(main);
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-cutscene-log.txt"), log.ToString());
return null;
