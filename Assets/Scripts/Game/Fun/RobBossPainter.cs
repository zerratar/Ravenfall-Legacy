using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Rob Boss, the painter on the hill on Home (docs/easter-eggs.md), at last paints on his blank canvas. Now and then
/// (every 9 to 16 minutes) he starts a painting: he says something encouraging, and over half a minute the picture
/// appears on the canvas dab by dab; when it is done its title and price show over it. The picture is a real snapshot
/// of something in the world, run through a painterly filter (oil paint smoothing, fewer colors, canvas grain): the
/// view from his hill, the player standing nearest to him, a woodcutter about to ruin a tree's day, Sir Clucksworth,
/// the screaming lady, the Mangrove monkeys, or a self-portrait that is only his hair. On April 1st he paints only
/// chickens.
/// </summary>
/// <remarks>
/// The scene's "RobBoss" and "RobBossPainting/Frame" (the canvas) are found by name: MainWorld is not changed. One
/// snapshot per painting, drawn once by a camera of its own (never every frame); the filter runs on 288 x 384 pixels
/// once, a few rows a frame. Only while the Silly surprises setting is on (never in potato mode or with graphics off) and while the main
/// camera is near enough to see it; otherwise the canvas keeps its last painting.
/// </remarks>
public sealed class RobBossPainter : MonoBehaviour
{
    public const int Width = 288, Height = 384; // the canvas is a portrait (0.91 by 1.25 m)
    public const float MinMinutes = 9f, MaxMinutes = 16f;
    public const float PaintSeconds = 28f;
    /// <summary>He paints only while the main camera is this close (nobody would see it otherwise).</summary>
    public const float SeenWithin = 160f;

    private static readonly Color Gallery = new Color(1f, 0.84f, 0.32f);
    private static readonly Color Talk = new Color(1f, 0.97f, 0.88f);

    private Transform rob;
    private Renderer canvas;
    private Material canvasMaterial;
    private Texture2D shown;
    private Color32[] blank;
    private float nextPainting;
    private bool painting;
    private readonly System.Random random = new System.Random();

    /// <summary>For the debug tools and tests: paints now (a subject from <see cref="Subjects"/>, or null for any).</summary>
    public static bool PaintNow(string subject = null)
    {
        var painter = FindAnyObjectByType<RobBossPainter>();
        if (!painter || painter.painting || !painter.Ready()) return false;
        painter.StartCoroutine(painter.Paint(subject));
        return true;
    }

    public static readonly string[] Subjects = { "view", "player", "woodcutter", "clucksworth", "lady", "monkeys", "hair" };

    /// <summary>Finds Rob and his canvas in the loaded world; null when this world has no Rob.</summary>
    public static RobBossPainter Attach()
    {
        var robObject = GameObject.Find("RobBoss");
        var frame = GameObject.Find("RobBossPainting/Frame");
        if (!robObject || !frame) return null;
        var painter = robObject.GetComponent<RobBossPainter>() ?? robObject.AddComponent<RobBossPainter>();
        painter.rob = robObject.transform;
        painter.canvas = frame.GetComponent<Renderer>();
        return painter.canvas ? painter : null;
    }

    private void Start()
    {
        // the first painting comes soon, so a streamer who looks his way finds him busy
        nextPainting = Time.time + 120f + (float)random.NextDouble() * 120f;
    }

    private bool Ready() => rob && canvas && SettingsApplier.SillySurprises && GraphicsToggler.GraphicsEnabled;

    private void Update()
    {
        if (painting || Time.time < nextPainting) return;
        nextPainting = Time.time + 60f;
        if (!Ready()) return;
        var cam = Camera.main;
        if (!cam || (cam.transform.position - rob.position).sqrMagnitude > SeenWithin * SeenWithin) return;
        StartCoroutine(Paint(null));
    }

    private IEnumerator Paint(string subject)
    {
        painting = true;
        try
        {
            var raw = Snapshot(subject, out var key, out var titleArg);
            if (raw == null) yield break;
            // the painterly filter, a few rows a frame so the game never stutters
            var picture = new Color32[raw.Length];
            for (var row = 0; row < Height; row += 24)
            {
                Painterly(raw, picture, row, Mathf.Min(Height, row + 24));
                yield return null;
            }

            EnsureCanvas();
            SillySurprises.Say(rob, Quote(), Talk, 3f);
            Array.Copy(blank, 0, pixels, 0, blank.Length);
            shown.SetPixels32(pixels);
            shown.Apply(false);

            // dab by dab: blobs of the finished picture in a random order, then the rest
            const int Dabs = 70;
            for (var i = 0; i < Dabs; ++i)
            {
                Dab(picture, random.Next(Width), random.Next(Height), 24 + random.Next(40));
                shown.SetPixels32(pixels);
                shown.Apply(false);
                yield return new WaitForSeconds(PaintSeconds / Dabs);
                if (!canvas) yield break;
            }
            Array.Copy(picture, pixels, picture.Length);
            shown.SetPixels32(pixels);
            shown.Apply(false);

            var title = string.Format(Title(key), titleArg);
            var price = (5 + random.Next(95)) * 10;
            SillySurprises.Say(Marker(canvas.bounds.center + Vector3.up * 0.2f, 8f), string.Format(Localization.UI_ROB_GALLERY, title, price), Gallery, 3.5f);
        }
        finally
        {
            painting = false;
            nextPainting = Time.time + (float)(MinMinutes + random.NextDouble() * (MaxMinutes - MinMinutes)) * 60f;
        }
    }

    private Color32[] pixels;

    private void EnsureCanvas()
    {
        if (shown) return;
        shown = new Texture2D(Width, Height, TextureFormat.RGBA32, false) { name = "Rob Boss Painting", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        blank = new Color32[Width * Height];
        pixels = new Color32[Width * Height];
        for (var y = 0; y < Height; ++y)
            for (var x = 0; x < Width; ++x)
            {
                var g = Grain(x, y);
                var v = (byte)Mathf.Clamp(238 + g, 0, 255);
                blank[y * Width + x] = new Color32(v, v, (byte)Mathf.Clamp(v - 6, 0, 255), 255);
            }

        // the easel's own plain material, with the picture on it (the canvas's shader graph has no texture to set)
        var easel = canvas.transform.parent ? canvas.transform.parent.GetComponentInChildren<MeshRenderer>() : null;
        var source = easel && easel != canvas ? easel.sharedMaterial : canvas.sharedMaterial;
        canvasMaterial = new Material(source) { name = "Rob Boss Canvas" };
        canvasMaterial.SetTexture("_BaseMap", shown);
        canvasMaterial.SetColor("_BaseColor", Color.white);
        if (canvasMaterial.HasProperty("_Smoothness")) canvasMaterial.SetFloat("_Smoothness", 0.15f);
        canvasMaterial.mainTexture = shown;
        canvas.sharedMaterial = canvasMaterial;
    }

    private void OnDestroy()
    {
        if (canvasMaterial) Destroy(canvasMaterial);
        if (shown) Destroy(shown);
    }

    private void Dab(Color32[] picture, int cx, int cy, int r)
    {
        var r2 = r * r;
        for (var y = Mathf.Max(0, cy - r); y < Mathf.Min(Height, cy + r); ++y)
            for (var x = Mathf.Max(0, cx - r); x < Mathf.Min(Width, cx + r); ++x)
            {
                var dx = x - cx; var dy = y - cy;
                // a ragged edge, as a brush leaves it
                if (dx * dx + dy * dy > r2 * (0.7f + 0.3f * Hash(x, y))) continue;
                pixels[y * Width + x] = picture[y * Width + x];
            }
    }

    // ---- What he paints ----------------------------------------------------------------------------------------

    private Color32[] Snapshot(string subject, out string key, out string titleArg)
    {
        key = null;
        titleArg = "";
        var options = new List<string>(subject != null ? new[] { subject } : Subjects);
        // April 1st: only chickens (if there is no chicken to paint, he paints what he would any other day)
        if (SillySurprises.IsAprilFools && subject == null && Aim("chickens", out _, out _, out _, out _)) options = new List<string> { "chickens" };
        Shuffle(options);
        foreach (var s in options)
        {
            if (!Aim(s, out var eye, out var look, out var fov, out titleArg)) continue;
            var picture = Render(eye, look, fov);
            if (picture == null) continue;
            key = s;
            return picture;
        }
        return null;
    }

    /// <summary>Where the painter's eye is for a subject; false when it is not in the world now.</summary>
    private bool Aim(string subject, out Vector3 eye, out Vector3 look, out float fov, out string titleArg)
    {
        eye = look = Vector3.zero;
        fov = 40f;
        titleArg = "";
        var head = rob.position + Vector3.up * 1.7f;
        switch (subject)
        {
            case "view":
                eye = head + rob.forward * 0.5f;
                look = head + rob.forward * 30f - Vector3.up * 6f;
                fov = 50f;
                return true;
            case "hair":
                eye = head + Vector3.up * 0.45f - rob.forward * 0.55f;
                look = head + Vector3.up * 0.15f;
                fov = 55f;
                return true;
            case "player":
            {
                var p = Nearest(x => true, 45f);
                if (!p) return false;
                titleArg = p.PlayerName;
                return Portrait(p.transform.position + Vector3.up * 1.4f, p.transform.forward, 3.2f, out eye, out look, out fov);
            }
            case "woodcutter":
            {
                var p = Nearest(x => x.GetTask() == TaskType.Woodcutting && x.HasTarget, float.MaxValue);
                if (!p) return false;
                titleArg = p.PlayerName;
                var tree = p.TargetPosition;
                var side = Vector3.Cross(Vector3.up, (tree - p.transform.position).normalized);
                eye = (tree + p.transform.position) * 0.5f + side * 7f + Vector3.up * 2.5f;
                look = (tree + p.transform.position) * 0.5f + Vector3.up * 2.2f;
                fov = 50f;
                return true;
            }
            case "clucksworth":
            case "chickens":
            {
                // the fountain's Sir Clucksworth (the top town hall tier), or on April 1st King Clucksworth, the
                // crowned hen of the silly surprises
                var chicken = Find("Sir Clucksworth");
                var king = FindObjectsByType<WorldGag>(FindObjectsSortMode.None);
                foreach (var gag in king)
                    if (gag.Key == "home-april-king-clucksworth" && gag.transform.childCount > 0 && gag.transform.GetChild(0).gameObject.activeInHierarchy)
                        chicken = gag.transform.GetChild(0);
                if (!chicken) return false;
                var cb = BoundsOf(chicken);
                var angle = subject == "chickens" ? 145f + (float)random.NextDouble() * 70f : 200f; // in front of it (the models face back), a little to one side
                var dir = Quaternion.Euler(0f, chicken.eulerAngles.y + angle, 0f) * Vector3.forward;
                return Portrait(cb.center + Vector3.up * cb.size.y * 0.2f, dir, cb.size.y * 2.4f + 0.6f, out eye, out look, out fov);
            }
            case "lady":
            {
                var lady = FindAnyObjectByType<ScreamingLady>();
                if (!lady) return false;
                return Portrait(lady.transform.position + Vector3.up * 1.5f, lady.transform.forward, 2.6f, out eye, out look, out fov);
            }
            case "monkeys":
            {
                var chief = Find("Monkey Chief");
                if (!chief) return false;
                return Portrait(chief.position + Vector3.up * 1f, chief.forward, 6f, out eye, out look, out fov);
            }
        }
        return false;
    }

    private static bool Portrait(Vector3 at, Vector3 facing, float distance, out Vector3 eye, out Vector3 look, out float fov)
    {
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.01f) facing = Vector3.forward;
        eye = at + facing.normalized * distance + Vector3.up * distance * 0.15f;
        look = at;
        fov = 42f;
        return true;
    }

    private PlayerController Nearest(Func<PlayerController, bool> can, float within)
    {
        var game = GameManager.Instance;
        if (!game || game.Players == null) return null;
        PlayerController best = null;
        var bestDistance = within * within;
        var anyDistance = float.MaxValue;
        PlayerController any = null;
        foreach (var p in game.Players.GetAllPlayers())
        {
            if (!p || p.isDestroyed || p.InRavebridge || !can(p)) continue;
            var d = (p.transform.position - rob.position).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = p; }
            if (d < anyDistance) { anyDistance = d; any = p; }
        }
        return best ? best : within >= float.MaxValue ? any : null;
    }

    private static Bounds BoundsOf(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>();
        var b = rs.Length > 0 ? rs[0].bounds : new Bounds(t.position, Vector3.one * 0.5f);
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    private static Transform Find(string part)
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0 && t.gameObject.activeInHierarchy) return t;
        return null;
    }

    /// <summary>One snapshot from the painter's eye, read back and painted over (null when it could not be drawn).</summary>
    private Color32[] Render(Vector3 eye, Vector3 look, float fov)
    {
        var main = Camera.main;
        var rt = RenderTexture.GetTemporary(Width * 2, Height * 2, 24, RenderTextureFormat.ARGB32);
        var go = new GameObject("Rob Boss Eye") { hideFlags = HideFlags.DontSave };
        try
        {
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 400f;
            cam.cullingMask = main ? main.cullingMask & ~LayerMask.GetMask("UI") : ~LayerMask.GetMask("UI");
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.targetTexture = rt;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderShadows = true;
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            go.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye));
            cam.Render();

            var read = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            var small = RenderTexture.GetTemporary(Width, Height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(rt, small);
            var was = RenderTexture.active;
            RenderTexture.active = small;
            read.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
            RenderTexture.active = was;
            RenderTexture.ReleaseTemporary(small);
            var raw = read.GetPixels32();
            Destroy(read);
            return raw;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("Rob Boss could not paint: " + exc.Message);
            return null;
        }
        finally
        {
            RenderTexture.ReleaseTemporary(rt);
            Destroy(go);
        }
    }

    // ---- The painterly look ------------------------------------------------------------------------------------

    /// <summary>
    /// Oil paint smoothing (a Kuwahara filter: each pixel takes the calmest of the four squares around it), fewer
    /// colors, a little warmth, canvas grain, and a ragged unpainted rim.
    /// </summary>
    public static void Painterly(Color32[] src, Color32[] dst, int fromRow, int toRow)
    {
        const int R = 3;
        var w = Width; var h = Height;
        for (var y = fromRow; y < toRow; ++y)
            for (var x = 0; x < w; ++x)
            {
                var bestVar = float.MaxValue;
                float br = 0, bg = 0, bb = 0;
                for (var q = 0; q < 4; ++q)
                {
                    var x0 = (q & 1) == 0 ? x - R : x;
                    var y0 = (q & 2) == 0 ? y - R : y;
                    float sr = 0, sg = 0, sb = 0, sl = 0, sl2 = 0; var n = 0;
                    for (var yy = y0; yy <= y0 + R; ++yy)
                    {
                        if (yy < 0 || yy >= h) continue;
                        for (var xx = x0; xx <= x0 + R; ++xx)
                        {
                            if (xx < 0 || xx >= w) continue;
                            var i = yy * w + xx;
                            var c = src[i];
                            var l = 0.3f * c.r + 0.59f * c.g + 0.11f * c.b;
                            sr += c.r; sg += c.g; sb += c.b;
                            sl += l; sl2 += l * l;
                            n++;
                        }
                    }
                    if (n == 0) continue;
                    var mean = sl / n;
                    var variance = sl2 / n - mean * mean;
                    if (variance < bestVar) { bestVar = variance; br = sr / n; bg = sg / n; bb = sb / n; }
                }

                // a little more color, fewer colors, a touch warmer and brighter, as paint is
                var grey = (br + bg + bb) / 3f;
                br = grey + (br - grey) * 1.2f; bg = grey + (bg - grey) * 1.2f; bb = grey + (bb - grey) * 1.2f;
                br = Poster(br * 1.08f + 12f); bg = Poster(bg * 1.06f + 9f); bb = Poster(bb * 1.0f + 3f);
                var grain = Grain(x, y);
                // the rim: canvas left bare in a ragged band round the edge
                var edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                var bare = Mathf.Clamp01((10f + 8f * Hash(x / 4, y / 4) - edge) / 5f);
                br = Mathf.Lerp(br, 238f, bare); bg = Mathf.Lerp(bg, 238f, bare); bb = Mathf.Lerp(bb, 232f, bare);
                dst[y * w + x] = new Color32((byte)Mathf.Clamp(br + grain, 0, 255), (byte)Mathf.Clamp(bg + grain, 0, 255), (byte)Mathf.Clamp(bb + grain, 0, 255), 255);
            }
    }

    private static float Poster(float v) => Mathf.Round(Mathf.Clamp(v, 0f, 255f) / 20f) * 20f;

    /// <summary>Canvas weave and a little noise, in brightness steps (-6 to 6).</summary>
    private static float Grain(int x, int y) => (((x & 3) < 2) ^ ((y & 3) < 2) ? 2.5f : -2.5f) + (Hash(x, y) - 0.5f) * 7f;

    private static float Hash(int x, int y)
    {
        unchecked
        {
            var h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h ^ (h >> 16)) / (float)uint.MaxValue;
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (var i = list.Count - 1; i > 0; --i)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static Transform Marker(Vector3 at, float seconds)
    {
        var marker = new GameObject("Rob Boss Gallery Card");
        marker.transform.position = at;
        Destroy(marker, seconds);
        return marker.transform;
    }

    // ---- His words ---------------------------------------------------------------------------------------------

    private string Quote() => Pick("UI_ROB_QUOTE_");

    private string Title(string subject) => Pick("UI_ROB_TITLE_" + subject.ToUpperInvariant() + "_");

    private string Pick(string prefix)
    {
        var lines = new List<string>();
        for (var n = 1; n < 40; ++n)
        {
            var field = typeof(Localization).GetField(prefix + n, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (field == null) break;
            if (field.GetValue(null) is string s && !string.IsNullOrEmpty(s)) lines.Add(s);
        }
        return lines.Count == 0 ? "" : lines[random.Next(lines.Count)];
    }
}
