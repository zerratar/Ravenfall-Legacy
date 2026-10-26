using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Image generation and editing in the editor through OpenAI's image models (GPT-Image-2.5 Flare and Sunburst), for
/// item icons, their cooked and burned variants, UI pictures and mockups. The requests are made by a small local
/// service (tools/ImageGenService), which the editor builds on first use and starts when needed: it keeps the jobs on
/// disk, so pictures in flight survive script reloads, editor restarts and crashes. A caller (the Image Generator
/// window, IconForge, an eval) starts a <see cref="Job"/> and checks back; when the editor sees a job finished it runs
/// the follow-up: the callback given (in the same session), or the handler for the job's tag (<see cref="FollowUps"/>,
/// after a reload or a restart).
/// </summary>
/// <remarks>
/// Both models often answer "transparent background" with a painted glow or backdrop, so a finished picture whose
/// border is not transparent gets one more pass with Flare asking for the background to be removed, before its
/// follow-up (as the Buildathon studio does). The rest of what the studio learned about the API is in the service.
///
/// The API key is the user's own: the OPENAI_API_KEY environment variable, or the one saved in the Image Generator
/// window (EditorPrefs, on this computer only). It goes to the service in its environment and with each job, over
/// 127.0.0.1 only, and is never written to the project, logged or shown again. Every picture costs money on that key's
/// account.
/// </remarks>
[InitializeOnLoad]
public static class ImageGen
{
    public enum Model { Flare, Sunburst }

    public sealed class Options
    {
        /// <summary>Flare is quick and the default; Sunburst keeps closer to the input images in edits.</summary>
        public Model Model = Model.Flare;
        public string Size = "1024x1024";
        /// <summary>low, medium, high, xhigh, max or auto.</summary>
        public string Quality = "high";
        public bool Transparent = true;
        /// <summary>With a transparent background asked for: a picture whose border came back painted gets a cutout pass.</summary>
        public bool AutoCutout = true;
    }

    public enum JobState { Running, Done, Failed }

    public sealed class Job
    {
        /// <summary>The service's id ("img12"), or "new-3" until the service has taken it.</summary>
        public string Id;
        public string Kind;
        public string Prompt;
        public string OutPath;
        public string[] Inputs;
        public Model Model;
        public JobState State;
        public string Error;
        public string Tag;
        public DateTime Started;
        public DateTime Finished;
        internal Action<Job> Then;
        internal bool Transparent, AutoCutout;

        public override string ToString() =>
            Id + " " + Kind + " " + State + (State == JobState.Failed ? ": " + Error : "") + " -> " + OutPath +
            " (" + (int)((State == JobState.Running ? DateTime.UtcNow : Finished) - Started).TotalSeconds + " s)";
    }

    /// <summary>The cutout pass's prompt, as the Buildathon studio's.</summary>
    public const string CutoutPrompt =
        "Remove the background completely and make it fully transparent. Keep only the main subject exactly as it is, with clean " +
        "edges: no glow, no sparkles, no shadow, no backdrop.";

    /// <summary>
    /// What to do with a finished job by its tag's kind ("icon:raw-tuna" runs FollowUps["icon"] with "raw-tuna"): how a
    /// follow-up survives a reload, when the callback is gone. Registered by the tools that make pictures (IconForge).
    /// </summary>
    public static readonly Dictionary<string, Action<Job, string>> FollowUps = new Dictionary<string, Action<Job, string>>();

    private const string KeyPref = "Ravenfall.ImageGen.OpenAIKey";
    private const int Port = 57421;
    private static readonly string Service = "http://127.0.0.1:" + Port + "/";
    private static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
    private static readonly ConcurrentQueue<Action> mainThread = new ConcurrentQueue<Action>();
    private static readonly Dictionary<string, Job> jobs = new Dictionary<string, Job>();
    private static readonly Dictionary<string, Action<Job>> thens = new Dictionary<string, Action<Job>>();
    private static readonly HashSet<string> handling = new HashSet<string>();
    private static readonly object sync = new object();
    private static Task<bool> starting;
    private static int pending;
    private static double nextPoll;
    private static bool polling, lookAfterLoad = true;

    static ImageGen()
    {
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        while (mainThread.TryDequeue(out var action))
        {
            try { action(); }
            catch (Exception exc) { Debug.LogError("Image generator: " + exc.Message); }
        }

        // asks the service how the jobs are doing: every second while any of ours run, and once after a reload for the
        // ones finished while the editor was away (only if the service is up; it is not started just to look)
        if (polling || EditorApplication.timeSinceStartup < nextPoll) return;
        bool busy;
        lock (sync) busy = jobs.Values.Any(j => j.State == JobState.Running);
        if (!busy && !lookAfterLoad) return;
        nextPoll = EditorApplication.timeSinceStartup + 1.0;
        var look = lookAfterLoad;
        lookAfterLoad = false;
        polling = true;
        Task.Run(async () =>
        {
            try { await PollAsync(); }
            catch (Exception exc) { if (!look) mainThread.Enqueue(() => Debug.LogWarning("Image generator: the service did not answer: " + exc.Message)); }
            finally { polling = false; }
        });
    }

    // ---- The key -------------------------------------------------------------------------------------------

    private static string Key
    {
        get
        {
            var env = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            return !string.IsNullOrWhiteSpace(env) ? env.Trim() : EditorPrefs.GetString(KeyPref, "").Trim();
        }
    }

    public static bool HasKey => !string.IsNullOrEmpty(Key);
    public static bool KeyFromEnvironment => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

    /// <summary>Saves the key on this computer (EditorPrefs); empty forgets it. The service gets it with the next job.</summary>
    public static void SaveKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) EditorPrefs.DeleteKey(KeyPref);
        else EditorPrefs.SetString(KeyPref, key.Trim());
    }

    public static string ModelId(Model model) => model == Model.Sunburst ? "gpt-image-2.5-sunburst" : "gpt-image-2.5-flare";

    // ---- Requests ------------------------------------------------------------------------------------------

    /// <summary>A new picture from a prompt, written to outPath (png); then runs on the main thread when it is there.</summary>
    public static Job Generate(string prompt, string outPath, Options options = null, Action<Job> then = null, string tag = null)
    {
        return Submit("generate", prompt, Array.Empty<string>(), null, outPath, options ?? new Options(), then, tag);
    }

    /// <summary>
    /// A picture made from others: the inputs (png files) are what to change or the style to follow, the prompt says
    /// how; a mask (png with alpha, the first input's size) marks where. Written to outPath.
    /// </summary>
    public static Job Edit(string prompt, IReadOnlyList<string> inputs, string outPath, Options options = null, string mask = null, Action<Job> then = null, string tag = null)
    {
        return Submit("edit", prompt, inputs.ToArray(), mask, outPath, options ?? new Options(), then, tag);
    }

    private static Job Submit(string kind, string prompt, string[] inputs, string mask, string outPath, Options options, Action<Job> then, string tag)
    {
        var job = new Job
        {
            Id = "new-" + (++pending),
            Kind = kind,
            Prompt = prompt,
            OutPath = Path.GetFullPath(outPath),
            Inputs = inputs.Select(Path.GetFullPath).ToArray(),
            Model = options.Model,
            State = JobState.Running,
            Tag = tag,
            Started = DateTime.UtcNow,
            Then = then,
            Transparent = options.Transparent,
            AutoCutout = options.AutoCutout,
        };
        lock (sync) jobs[job.Id] = job;

        var key = Key;
        if (string.IsNullOrEmpty(key))
        {
            Fail(job, "no OpenAI key: set OPENAI_API_KEY or save one in Ravenfall > Image Generator");
            return job;
        }
        var body = new JObject
        {
            ["kind"] = kind,
            ["model"] = options.Model == Model.Sunburst ? "sunburst" : "flare",
            ["prompt"] = prompt,
            ["inputs"] = new JArray(job.Inputs),
            ["mask"] = string.IsNullOrEmpty(mask) ? null : Path.GetFullPath(mask),
            ["outPath"] = job.OutPath,
            ["size"] = options.Size,
            ["quality"] = options.Quality,
            ["transparent"] = options.Transparent,
            ["autoCutout"] = options.AutoCutout,
            ["tag"] = tag,
        };
        var serviceDir = Path.GetFullPath("tools/ImageGenService");
        var dataDir = Path.GetFullPath("Library/ImageGen/service");
        Task.Run(async () =>
        {
            try
            {
                if (!await EnsureServiceAsync(key, serviceDir, dataDir))
                {
                    Fail(job, "the image service could not be started (tools/ImageGenService)");
                    return;
                }
                using var request = new HttpRequestMessage(HttpMethod.Post, Service + "jobs") { Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json") };
                request.Headers.Add("X-OpenAI-Key", key);
                using var response = await http.SendAsync(request);
                var text = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    Fail(job, (string)JObject.Parse(text)["error"] ?? text);
                    return;
                }
                var id = (string)JObject.Parse(text)["id"];
                lock (sync)
                {
                    jobs.Remove(job.Id);
                    job.Id = id;
                    jobs[id] = job;
                    if (then != null) thens[id] = then;
                }
            }
            catch (Exception exc)
            {
                Fail(job, exc.Message);
            }
        });
        return job;
    }

    // ---- The service ---------------------------------------------------------------------------------------

    private static async Task<bool> HealthyAsync()
    {
        try
        {
            using var cts = new System.Threading.CancellationTokenSource(1500);
            using var response = await http.GetAsync(Service + "health", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    /// <summary>Starts the service if it is not up, building it first if it was never built.</summary>
    private static Task<bool> EnsureServiceAsync(string key, string serviceDir, string dataDir)
    {
        lock (sync)
        {
            if (starting == null || starting.IsCompleted) starting = StartAsync(key, serviceDir, dataDir);
            return starting;
        }
    }

    private static async Task<bool> StartAsync(string key, string serviceDir, string dataDir)
    {
        if (await HealthyAsync()) return true;
        var exe = Path.Combine(serviceDir, "bin", "Release", "net10.0", "ImageGenService.exe");
        if (!File.Exists(exe))
        {
            mainThread.Enqueue(() => Debug.Log("Image generator: building the image service (once)..."));
            var build = Process.Start(new ProcessStartInfo("dotnet", "build -c Release -v q")
            {
                WorkingDirectory = serviceDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            if (build == null) return false;
            await build.StandardOutput.ReadToEndAsync();
            build.WaitForExit(180000);
            if (!File.Exists(exe)) return false;
        }
        Directory.CreateDirectory(dataDir);
        var start = new ProcessStartInfo(exe)
        {
            WorkingDirectory = Path.GetDirectoryName(exe),
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        start.Environment["OPENAI_API_KEY"] = key;
        start.Environment["IMAGEGEN_PORT"] = Port.ToString();
        start.Environment["IMAGEGEN_DATA"] = dataDir;
        Process.Start(start);
        for (var i = 0; i < 40; i++)
        {
            await Task.Delay(250);
            if (await HealthyAsync()) return true;
        }
        return false;
    }

    private static async Task PollAsync()
    {
        using var response = await http.GetAsync(Service + "jobs?last=300");
        if (!response.IsSuccessStatusCode) return;
        var list = JArray.Parse(await response.Content.ReadAsStringAsync());
        foreach (var j in list)
        {
            var id = (string)j["id"];
            var r = j["request"];
            var state = (string)j["state"];
            var mapped = state == "Done" ? JobState.Done : state == "Failed" || state == "Interrupted" ? JobState.Failed : JobState.Running;
            Job job;
            lock (sync)
            {
                if (!jobs.TryGetValue(id, out job))
                {
                    job = new Job { Id = id };
                    jobs[id] = job;
                }
                job.Kind = (string)r["kind"];
                job.Prompt = (string)r["prompt"];
                job.OutPath = (string)r["outPath"];
                job.Inputs = r["inputs"]?.ToObject<string[]>() ?? Array.Empty<string>();
                job.Model = (string)r["model"] == "sunburst" ? Model.Sunburst : Model.Flare;
                job.Tag = (string)r["tag"];
                job.Transparent = (bool?)r["transparent"] ?? false;
                job.AutoCutout = (bool?)r["autoCutout"] ?? false;
                job.Started = (DateTime?)j["started"] ?? (DateTime?)j["created"] ?? DateTime.UtcNow;
                job.Finished = (DateTime?)j["finished"] ?? DateTime.MinValue;
                job.State = mapped;
                job.Error = (string)j["error"];
                if (mapped == JobState.Running || ((bool?)j["handled"] ?? false) || !handling.Add(id)) continue;
            }
            mainThread.Enqueue(() => Finished(job));
        }
    }

    // ---- Finishing -----------------------------------------------------------------------------------------

    /// <summary>A job the service has finished and no one has handled: the cutout pass if needed, then its follow-up.</summary>
    private static void Finished(Job job)
    {
        Action<Job> then;
        lock (sync) thens.TryGetValue(job.Id, out then);
        try
        {
            if (job.State == JobState.Done && job.Transparent && job.AutoCutout && File.Exists(job.OutPath) && !HasTransparentBorder(File.ReadAllBytes(job.OutPath)))
            {
                // the cutout writes over the same file and carries the follow-up; if it fails, the picture stays as it came
                var raw = Path.ChangeExtension(job.OutPath, ".uncut.png");
                File.Copy(job.OutPath, raw, true);
                var cut = new Options { Model = Model.Flare, Size = "auto", Quality = "high", Transparent = true, AutoCutout = false };
                Edit(CutoutPrompt, new[] { raw }, job.OutPath, cut, null, then, job.Tag);
                return;
            }
            if (job.State == JobState.Failed && job.Prompt != CutoutPrompt)
            {
                Debug.LogWarning("Image generator " + job.Id + " failed: " + job.Error);
                return;
            }
            // done; or a cutout that failed, which leaves the picture as it came: its follow-up runs all the same
            if (job.State == JobState.Failed) job.State = JobState.Done;
            if (then != null) then(job);
            else if (!string.IsNullOrEmpty(job.Tag))
            {
                var colon = job.Tag.IndexOf(':');
                var kind = colon < 0 ? job.Tag : job.Tag.Substring(0, colon);
                if (FollowUps.TryGetValue(kind, out var followUp)) followUp(job, colon < 0 ? "" : job.Tag.Substring(colon + 1));
            }
        }
        finally
        {
            lock (sync) thens.Remove(job.Id);
            MarkHandled(job.Id);
        }
    }

    private static void MarkHandled(string id)
    {
        Task.Run(async () =>
        {
            try { using var _ = await http.PostAsync(Service + "jobs/" + id + "/handled", new StringContent("")); }
            catch { /* asked again next time */ }
        });
    }

    private static void Fail(Job job, string error)
    {
        job.State = JobState.Failed;
        job.Error = System.Text.RegularExpressions.Regex.Replace(error ?? "", @"sk-[A-Za-z0-9_\-\*]+", "(the key)");
        job.Finished = DateTime.UtcNow;
        mainThread.Enqueue(() => Debug.LogWarning("Image generator " + job.Id + " failed: " + job.Error));
    }

    // ---- Looking at the jobs -------------------------------------------------------------------------------

    public static IReadOnlyList<Job> Jobs { get { lock (sync) return jobs.Values.OrderBy(j => j.Started).ToList(); } }

    public static Job Find(string id) { lock (sync) return jobs.TryGetValue(id, out var job) ? job : null; }

    /// <summary>The latest jobs, one a line, for an eval to read (this session's, and the service's since the last look).</summary>
    public static string Status(int last = 10)
    {
        var list = Jobs;
        return string.Join("\n", list.Skip(Math.Max(0, list.Count - last)).Select(j => j.ToString()));
    }

    /// <summary>
    /// Whether the picture's outermost ring of pixels is (nearly) all transparent, as a cut out picture's is (the
    /// Buildathon studio's check: 95% of samples with alpha under 16).
    /// </summary>
    public static bool HasTransparentBorder(byte[] png)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!tex.LoadImage(png) || tex.width < 2 || tex.height < 2) return false;
            int w = tex.width, h = tex.height, step = Math.Max(1, Math.Min(w, h) / 64), total = 0, clean = 0;
            var px = tex.GetPixels32();
            void Sample(int x, int y) { total++; if (px[y * w + x].a < 16) clean++; }
            for (var x = 0; x < w; x += step) { Sample(x, 0); Sample(x, h - 1); }
            for (var y = 0; y < h; y += step) { Sample(0, y); Sample(w - 1, y); }
            return total > 0 && clean >= 0.95 * total;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
