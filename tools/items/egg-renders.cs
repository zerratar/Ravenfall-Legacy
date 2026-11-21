// Pet egg icons and the website's pictures of the hatched pets, step one (tools/items/egg-icons.py is step two): run
// with eval_file in play mode; every hatched species is rendered at each stage, alone, against black and against white
// into %TEMP%/egg-renders ("wolf-pup", "wolf-pup-1", "wolf-pup-2"), framed on the posed model (a flying idle lifts it
// out of its rest pose's bounds, which is why tools/website/pet-renders.cs misses some of them).
//
// One stage per step, a few frames apart (an eval that renders everything at once is over the editor's five seconds,
// and a model destroyed in the same frame as the next render still shows in it). It ends by writing done.txt there:
//     until [ -f "$TEMP/egg-renders/done.txt" ]; do sleep 3; done
// Options, as files in %TEMP%/egg-renders: only.txt (the species ids to render, any white space between them;
// without it, all of them) and yaw.txt (how far the pet is turned from the camera, in degrees; 20 without it).
AdminControlData.NoChatBotMessages = true; // play mode has the live chat bot connected: nothing here may reach chat
var outDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "egg-renders") + System.IO.Path.DirectorySeparatorChar;
System.IO.Directory.CreateDirectory(outDir);
if (System.IO.File.Exists(outDir + "done.txt")) System.IO.File.Delete(outDir + "done.txt");
var only = System.IO.File.Exists(outDir + "only.txt")
    ? new System.Collections.Generic.HashSet<string>(System.IO.File.ReadAllText(outDir + "only.txt").Split(new[] { ' ', ',', '\r', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries))
    : null;
var yaw = System.IO.File.Exists(outDir + "yaw.txt") ? float.Parse(System.IO.File.ReadAllText(outDir + "yaw.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture) : 20f;
var graphField = typeof(PetClipPlayer).GetField("graph", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

async System.Threading.Tasks.Task Run()
{
    var log = new System.Text.StringBuilder();
    var n = 0;
    UnityEngine.GameObject camGo = null;
    UnityEngine.RenderTexture rt = null;
    try
    {
        // the same daylight every time, whatever the hour of the session
        var day = UnityEngine.Object.FindAnyObjectByType<DayNightCycle>();
        if (day) day.TotalTime = day.CycleLength * 0.25f;
        camGo = new UnityEngine.GameObject("Egg icon camera");
        var cam = camGo.AddComponent<UnityEngine.Camera>();
        cam.enabled = false; // it only draws when asked to
        cam.orthographic = true; cam.nearClipPlane = 0.05f; cam.farClipPlane = 100;
        cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor; cam.backgroundColor = new UnityEngine.Color(0, 0, 0, 0);
        var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() ?? camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        data.renderPostProcessing = false; data.renderShadows = false;
        rt = new UnityEngine.RenderTexture(512, 512, 24, UnityEngine.RenderTextureFormat.ARGB32);
        await System.Threading.Tasks.Task.Delay(300);

        foreach (var s in RavenNest.Combat.PetEggs.Species)
        {
            if (only != null && !only.Contains(s.Id)) continue;
            for (var stage = 0; stage < 3; ++stage)
            {
                var m = PetModels.Create(s, stage, null);
                var file = s.Id + (stage > 0 ? "-" + stage : "");
                if (m == null) { log.AppendLine("no model: " + file); continue; }
                var origin = new UnityEngine.Vector3(0, 450, 0);
                m.Root.transform.position = origin;
                m.Root.transform.rotation = UnityEngine.Quaternion.Euler(0, 180 + yaw, 0);
                foreach (var tr in m.Root.GetComponentsInChildren<UnityEngine.Transform>(true)) tr.gameObject.layer = 31;
                var p = new PetClipPlayer(m.Animator, m.Clips, s.Strike == RavenNest.Combat.PetStrike.Bolt);
                for (var t = 0; t < 3; ++t) p.Tick();
                var g = (UnityEngine.Playables.PlayableGraph)graphField.GetValue(p);
                if (g.IsValid()) { g.Stop(); g.Evaluate(0.3f); } // one pose, held while the frames pass
                await System.Threading.Tasks.Task.Delay(120);
                if (!m.Root) { log.AppendLine("gone: " + file); continue; }

                var b = new UnityEngine.Bounds(origin, UnityEngine.Vector3.zero); var any = false;
                foreach (var r in m.Root.GetComponentsInChildren<UnityEngine.Renderer>()) { if (r is UnityEngine.ParticleSystemRenderer) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                cam.transform.rotation = UnityEngine.Quaternion.Euler(10, 0, 0);
                cam.transform.position = b.center - cam.transform.forward * 20f;
                cam.orthographicSize = UnityEngine.Mathf.Max(b.extents.y, b.extents.x) * 1.08f;
                cam.cullingMask = 1 << 31;
                cam.targetTexture = rt;
                // the renderer bounds are the rest pose's (a flying idle lifts the model out of them): a wide look first, then
                // framed on what the picture shows
                // (against magenta: a black horse or a dark tail does not stand out from black, and was cut off)
                cam.orthographicSize *= 3f;
                cam.backgroundColor = new UnityEngine.Color(1, 0, 1, 1);
                cam.Render();
                UnityEngine.RenderTexture.active = rt;
                var probe = new UnityEngine.Texture2D(512, 512, UnityEngine.TextureFormat.RGBA32, false);
                probe.ReadPixels(new UnityEngine.Rect(0, 0, 512, 512), 0, 0); probe.Apply();
                UnityEngine.RenderTexture.active = null;
                int minX = 512, minY = 512, maxX = -1, maxY = -1;
                var px = probe.GetPixels32();
                for (var y = 0; y < 512; ++y) for (var x = 0; x < 512; ++x) { var c = px[y * 512 + x]; if ((255 - c.r) + c.g + (255 - c.b) > 40) { if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; } }
                UnityEngine.Object.Destroy(probe);
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
                    UnityEngine.Object.Destroy(tex);
                }
                cam.targetTexture = null;
                log.AppendLine("ok " + file + " <- " + s.ModelAt(stage) + (maxX < 0 ? " (nothing seen in the wide look)" : ""));
                p.Dispose(); UnityEngine.Object.Destroy(m.Root); n++;
                await System.Threading.Tasks.Task.Delay(120);
            }
        }
    }
    catch (System.Exception e) { log.AppendLine("ERROR " + e); }
    if (rt) { rt.Release(); UnityEngine.Object.Destroy(rt); }
    if (camGo) UnityEngine.Object.Destroy(camGo);
    log.AppendLine("done " + n);
    System.IO.File.WriteAllText(outDir + "done.txt", log.ToString());
}
_ = Run();
return "rendering into " + outDir;
