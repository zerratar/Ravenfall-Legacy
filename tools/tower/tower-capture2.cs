// Renders a tower floor scene from a few cameras in edit mode (opened additively, closed after), with the floor's
// fog and light for the shots; MainWorld's settings are put back. Scene path from %TEMP%/tower-capture.txt.
var tmp = System.IO.Path.GetTempPath();
var log = new System.Text.StringBuilder();
var path = System.IO.File.ReadAllText(System.IO.Path.Combine(tmp, "tower-capture.txt")).Trim();
var main = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var fog = UnityEngine.RenderSettings.fog; var fogColor = UnityEngine.RenderSettings.fogColor; var fogDensity = UnityEngine.RenderSettings.fogDensity; var fogMode = UnityEngine.RenderSettings.fogMode;
var amb = UnityEngine.RenderSettings.ambientMode; var sky = UnityEngine.RenderSettings.ambientSkyColor; var eq = UnityEngine.RenderSettings.ambientEquatorColor; var gr = UnityEngine.RenderSettings.ambientGroundColor;
var sun = UnityEngine.RenderSettings.sun;
var sunRot = sun ? sun.transform.rotation : UnityEngine.Quaternion.identity; var sunColor = sun ? sun.color : UnityEngine.Color.white; var sunInt = sun ? sun.intensity : 1f;
try
{
    var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
    var floor = scene.GetRootGameObjects().Select(g => g.GetComponent<TowerPlace>()).FirstOrDefault(f => f);
    var o = floor.transform.position;
    UnityEngine.RenderSettings.fog = true; UnityEngine.RenderSettings.fogMode = UnityEngine.FogMode.ExponentialSquared;
    UnityEngine.RenderSettings.fogColor = floor.FogColor; UnityEngine.RenderSettings.fogDensity = floor.FogDensity;
    UnityEngine.RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
    UnityEngine.RenderSettings.ambientSkyColor = floor.SkyAmbient; UnityEngine.RenderSettings.ambientEquatorColor = floor.EquatorAmbient; UnityEngine.RenderSettings.ambientGroundColor = floor.GroundAmbient;
    if (sun) { sun.transform.rotation = UnityEngine.Quaternion.Euler(floor.SunAngles); sun.color = floor.SunColor; sun.intensity = floor.SunIntensity; }

    var camGo = new UnityEngine.GameObject("capture cam");
    var cam = camGo.AddComponent<UnityEngine.Camera>();
    cam.fieldOfView = 50f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 1500f;
    var rt = new UnityEngine.RenderTexture(1600, 900, 24);
    cam.targetTexture = rt;
    var shotText = System.IO.File.ReadAllLines(System.IO.Path.Combine(tmp, "tower-shots.txt"));
    var shots = shotText.Where(l => l.Trim().Length > 0).Select(l => { var p = l.Split(' '); System.Func<int, float> f = i => float.Parse(p[i], System.Globalization.CultureInfo.InvariantCulture); return (name: p[0], pos: new UnityEngine.Vector3(f(1), f(2), f(3)), look: new UnityEngine.Vector3(f(4), f(5), f(6))); }).ToArray();

    foreach (var s in shots)
    {
        cam.transform.position = o + s.pos;
        cam.transform.LookAt(o + s.look);
        cam.Render();
        UnityEngine.RenderTexture.active = rt;
        var tex = new UnityEngine.Texture2D(1600, 900, UnityEngine.TextureFormat.RGB24, false);
        tex.ReadPixels(new UnityEngine.Rect(0, 0, 1600, 900), 0, 0);
        tex.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(tmp, "tower-shot-" + s.name + ".png"), UnityEngine.ImageConversion.EncodeToPNG(tex));
        UnityEngine.Object.DestroyImmediate(tex);
        log.AppendLine("shot " + s.name);
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
System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "tower-capture-log.txt"), log.ToString());
return null;
