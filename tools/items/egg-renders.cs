// Pet egg icons and the website's pictures of the hatched pets, step one (tools/items/egg-icons.py is step two): run
// with eval_file in play mode; every hatched species is rendered at each stage, alone, against black and against white
// into %TEMP%/egg-renders ("wolf-pup", "wolf-pup-1", "wolf-pup-2"), framed on the posed model (a flying idle lifts it
// out of its rest pose's bounds, which is why tools/website/pet-renders.cs misses some of them).
// each hatched species' first stage, alone, transparent background, for the egg icons
var outDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "egg-renders") + System.IO.Path.DirectorySeparatorChar;
System.IO.Directory.CreateDirectory(outDir);
var graphField = typeof(PetClipPlayer).GetField("graph", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var camGo = new UnityEngine.GameObject("Egg icon camera");
var cam = camGo.AddComponent<UnityEngine.Camera>();
cam.orthographic = true; cam.nearClipPlane = 0.05f; cam.farClipPlane = 100;
cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor; cam.backgroundColor = new UnityEngine.Color(0, 0, 0, 0);
var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() ?? camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
data.renderPostProcessing = false; data.renderShadows = false;
var rt = new UnityEngine.RenderTexture(512, 512, 24, UnityEngine.RenderTextureFormat.ARGB32);
var n = 0;
foreach (var s in RavenNest.Combat.PetEggs.Species)
for (var stage = 0; stage < 3; ++stage)
{
    var m = PetModels.Create(s, stage, null);
    var file = s.Id + (stage > 0 ? "-" + stage : "");
    if (m == null) continue;
    var origin = new UnityEngine.Vector3(0, 450, 0);
    m.Root.transform.position = origin;
    m.Root.transform.rotation = UnityEngine.Quaternion.Euler(0, 180 + 20, 0);
    var p = new PetClipPlayer(m.Animator, m.Clips, s.Strike == RavenNest.Combat.PetStrike.Bolt);
    for (var t = 0; t < 3; ++t) p.Tick();
    var g = (UnityEngine.Playables.PlayableGraph)graphField.GetValue(p);
    if (g.IsValid()) g.Evaluate(0.3f);
    var b = new UnityEngine.Bounds(origin, UnityEngine.Vector3.zero); var any = false;
    foreach (var r in m.Root.GetComponentsInChildren<UnityEngine.Renderer>()) { if (r is UnityEngine.ParticleSystemRenderer) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
    cam.transform.rotation = UnityEngine.Quaternion.Euler(10, 0, 0);
    cam.transform.position = b.center - cam.transform.forward * 20f;
    cam.orthographicSize = UnityEngine.Mathf.Max(b.extents.y, b.extents.x) * 1.08f;
    foreach (var tr in m.Root.GetComponentsInChildren<UnityEngine.Transform>(true)) tr.gameObject.layer = 31;
    cam.cullingMask = 1 << 31;
    cam.targetTexture = rt;
    // the renderer bounds are the rest pose's (a flying idle lifts the model out of them): a wide look first, then
    // framed on what the picture shows
    cam.orthographicSize *= 3f;
    cam.backgroundColor = new UnityEngine.Color(0, 0, 0, 1);
    cam.Render();
    UnityEngine.RenderTexture.active = rt;
    var probe = new UnityEngine.Texture2D(512, 512, UnityEngine.TextureFormat.RGBA32, false);
    probe.ReadPixels(new UnityEngine.Rect(0, 0, 512, 512), 0, 0); probe.Apply();
    UnityEngine.RenderTexture.active = null;
    int minX = 512, minY = 512, maxX = -1, maxY = -1;
    var px = probe.GetPixels32();
    for (var y = 0; y < 512; ++y) for (var x = 0; x < 512; ++x) { var c = px[y * 512 + x]; if (c.r + c.g + c.b > 12) { if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; } }
    UnityEngine.Object.DestroyImmediate(probe);
    if (maxX >= 0)
    {
        var unit = cam.orthographicSize * 2f / 512f; // world units per pixel
        var centre = new UnityEngine.Vector2((minX + maxX) / 2f - 256f, (minY + maxY) / 2f - 256f) * unit;
        cam.transform.position += cam.transform.right * centre.x + cam.transform.up * centre.y;
        cam.orthographicSize = UnityEngine.Mathf.Max(maxX - minX, maxY - minY) * unit * 0.5f * 1.06f;
    }
    else cam.orthographicSize /= 3f;
    foreach (var bg in new[] { "black", "white" })
    {
        cam.backgroundColor = bg == "black" ? new UnityEngine.Color(0, 0, 0, 1) : new UnityEngine.Color(1, 1, 1, 1);
        cam.Render();
        UnityEngine.RenderTexture.active = rt;
        var tex = new UnityEngine.Texture2D(512, 512, UnityEngine.TextureFormat.RGBA32, false);
        tex.ReadPixels(new UnityEngine.Rect(0, 0, 512, 512), 0, 0); tex.Apply();
        UnityEngine.RenderTexture.active = null;
        System.IO.File.WriteAllBytes(outDir + file + "-" + bg + ".png", tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }
    p.Dispose(); UnityEngine.Object.DestroyImmediate(m.Root); n++;
}
cam.targetTexture = null; rt.Release(); UnityEngine.Object.Destroy(camGo);
return "ok " + n;
