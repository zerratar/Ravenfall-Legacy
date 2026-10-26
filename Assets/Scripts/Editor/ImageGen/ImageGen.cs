using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Image generation and editing in the editor through OpenAI's image models (GPT-Image-2.5 Flare and Sunburst), for
/// item icons, their cooked and burned variants, and UI pictures. Each request runs off the main thread as a
/// <see cref="Job"/>: it writes the picture to its output path and then runs its follow-up on the main thread (to
/// import it, say), so a caller (the Image Generator window, or an eval) starts it and checks back later.
/// </summary>
/// <remarks>
/// Following what the Buildathon studio (D:\Git\Buildathon, OpenAiImageProvider and docs/research/ai-providers.md) found:
/// multipart field names are quoted (name="image[]"), or the edits endpoint does not read them; input_fidelity is
/// never sent (older models only); a 429 for quota or spend is not retried, a rate limit or a 5xx is (honouring
/// Retry-After), and a request that timed out or lost its connection is never sent again, as it may already be billed;
/// and both models often answer "transparent background" with a painted glow or backdrop, so a picture whose border
/// is not transparent gets one more pass with Flare asking for the background to be removed.
///
/// The API key is the user's own: the OPENAI_API_KEY environment variable, or the one saved in the Image Generator
/// window (EditorPrefs, on this computer only). It is never written to the project, logged or shown again. Every
/// request costs money on that key's account. What was asked for and what came back is kept in
/// Library/ImageGen/history.jsonl (not versioned), without the key.
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
        public string Id;
        public string Kind;
        public string Prompt;
        public string OutPath;
        public string[] Inputs;
        public Model Model;
        public JobState State;
        public string Error;
        public DateTime Started;
        public DateTime Finished;
        internal Action<Job> Then;
        internal bool Cutout;

        public override string ToString() =>
            Id + " " + Kind + " " + State + (State == JobState.Failed ? ": " + Error : "") + " -> " + OutPath +
            " (" + (int)((State == JobState.Running ? DateTime.UtcNow : Finished) - Started).TotalSeconds + " s)";
    }

    private const string KeyPref = "Ravenfall.ImageGen.OpenAIKey";
    private const string Endpoint = "https://api.openai.com/v1/images/";
    private static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromMinutes(6) };
    private const int MaxRetries = 3;
    /// <summary>Requests in flight at once; the rest wait their turn, so a batch of icons does not hit the rate limit.</summary>
    private static readonly System.Threading.SemaphoreSlim inFlight = new System.Threading.SemaphoreSlim(4);

    /// <summary>The cutout pass's prompt, as the Buildathon studio's.</summary>
    public const string CutoutPrompt =
        "Remove the background completely and make it fully transparent. Keep only the main subject exactly as it is, with clean " +
        "edges: no glow, no sparkles, no shadow, no backdrop.";
    private static readonly ConcurrentQueue<Action> mainThread = new ConcurrentQueue<Action>();
    private static readonly List<Job> jobs = new List<Job>();
    private static int next;

    static ImageGen()
    {
        EditorApplication.update += () =>
        {
            while (mainThread.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception exc) { Debug.LogError("Image generator: " + exc.Message); }
            }
        };
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

    /// <summary>Saves the key on this computer (EditorPrefs); empty forgets it.</summary>
    public static void SaveKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) EditorPrefs.DeleteKey(KeyPref);
        else EditorPrefs.SetString(KeyPref, key.Trim());
    }

    // ---- Requests ------------------------------------------------------------------------------------------

    public static string ModelId(Model model) => model == Model.Sunburst ? "gpt-image-2.5-sunburst" : "gpt-image-2.5-flare";

    /// <summary>A new picture from a prompt, written to outPath (png); then runs on the main thread when it is there.</summary>
    public static Job Generate(string prompt, string outPath, Options options = null, Action<Job> then = null)
    {
        options ??= new Options();
        var body = new JObject
        {
            ["model"] = ModelId(options.Model),
            ["prompt"] = prompt,
            ["size"] = options.Size,
            ["quality"] = options.Quality,
            ["output_format"] = "png",
            ["n"] = 1,
        };
        if (options.Transparent) body["background"] = "transparent";
        var job = NewJob("generate", prompt, outPath, Array.Empty<string>(), options.Model, then);
        job.Cutout = options.Transparent && options.AutoCutout;
        Run(job, () => new HttpRequestMessage(HttpMethod.Post, Endpoint + "generations")
        {
            Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json"),
        });
        return job;
    }

    /// <summary>
    /// A picture made from others: the inputs (png files) are what to change or the style to follow, the prompt says
    /// how; a mask (png with alpha, the first input's size) marks where. Written to outPath.
    /// </summary>
    public static Job Edit(string prompt, IReadOnlyList<string> inputs, string outPath, Options options = null, string mask = null, Action<Job> then = null)
    {
        options ??= new Options();
        var files = inputs.Select(Path.GetFullPath).ToArray();
        var job = NewJob("edit", prompt, outPath, files, options.Model, then);
        job.Cutout = options.Transparent && options.AutoCutout;
        if (files.Length == 0 || files.Length > 16)
        {
            Fail(job, "an edit takes 1 to 16 input images");
            return job;
        }
        foreach (var f in files)
        {
            if (File.Exists(f)) continue;
            Fail(job, "no such input: " + f);
            return job;
        }
        Run(job, () =>
        {
            var form = new MultipartFormDataContent();
            Text(form, "model", ModelId(options.Model));
            Text(form, "prompt", prompt);
            Text(form, "size", options.Size);
            Text(form, "quality", options.Quality);
            Text(form, "output_format", "png");
            Text(form, "n", "1");
            if (options.Transparent) Text(form, "background", "transparent");
            for (var i = 0; i < files.Length; i++) Png(form, "image[]", "image" + i + ".png", files[i]);
            if (!string.IsNullOrEmpty(mask)) Png(form, "mask", "mask.png", Path.GetFullPath(mask));
            return new HttpRequestMessage(HttpMethod.Post, Endpoint + "edits") { Content = form };
        });
        return job;
    }

    // quoted explicitly, so the wire shows name="image[]"; filename="image0.png" (unquoted, the brackets are not read)
    private static void Text(MultipartFormDataContent form, string name, string value) => form.Add(new StringContent(value), "\"" + name + "\"");

    private static void Png(MultipartFormDataContent form, string name, string fileName, string path)
    {
        var part = new ByteArrayContent(File.ReadAllBytes(path));
        part.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        part.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "\"" + name + "\"", FileName = "\"" + fileName + "\"" };
        form.Add(part);
    }

    // ---- Jobs ----------------------------------------------------------------------------------------------

    public static IReadOnlyList<Job> Jobs { get { lock (jobs) return jobs.ToList(); } }

    public static Job Find(string id) { lock (jobs) return jobs.FirstOrDefault(j => j.Id == id); }

    /// <summary>The latest jobs, one a line, for an eval to read.</summary>
    public static string Status(int last = 10)
    {
        lock (jobs) return string.Join("\n", jobs.Skip(Math.Max(0, jobs.Count - last)).Select(j => j.ToString()));
    }

    private static Job NewJob(string kind, string prompt, string outPath, string[] inputs, Model model, Action<Job> then)
    {
        var job = new Job
        {
            Id = "img" + (++next),
            Kind = kind,
            Prompt = prompt,
            OutPath = outPath,
            Inputs = inputs,
            Model = model,
            State = JobState.Running,
            Started = DateTime.UtcNow,
            Then = then,
        };
        lock (jobs) jobs.Add(job);
        return job;
    }

    private static void Run(Job job, Func<HttpRequestMessage> request)
    {
        var key = Key;
        if (string.IsNullOrEmpty(key))
        {
            Fail(job, "no OpenAI key: set OPENAI_API_KEY or save one in Ravenfall > Image Generator");
            return;
        }

        Task.Run(async () =>
        {
            await inFlight.WaitAsync().ConfigureAwait(false);
            try
            {
                string text = null;
                for (var attempt = 0; ; attempt++)
                {
                    using var message = request();
                    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                    HttpResponseMessage response;
                    try
                    {
                        response = await http.SendAsync(message).ConfigureAwait(false);
                    }
                    catch (Exception exc) when (exc is TaskCanceledException || exc is HttpRequestException)
                    {
                        // timed out or the connection went: never sent again, the picture may already be paid for
                        Fail(job, "no answer (" + exc.Message + "); not sent again, as it may already be billed");
                        return;
                    }
                    using (response)
                    {
                        text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (response.IsSuccessStatusCode) break;
                        var status = (int)response.StatusCode;
                        var (code, error) = ErrorOf(text);
                        var quota = code == "insufficient_quota" || code == "credit_balance_exhausted" || code == "organization_spend_limit_exceeded"
                                    || code == "project_spend_limit_exceeded" || code == "organization_usage_limit_exceeded";
                        var retry = (status == 429 && !quota) || status >= 500;
                        if (!retry || attempt >= MaxRetries)
                        {
                            Fail(job, status + (quota ? " quota or spend limit: " : " ") + error);
                            return;
                        }
                        var wait = TimeSpan.FromSeconds(2 << attempt);
                        if (response.Headers.RetryAfter?.Delta is TimeSpan after && after > wait) wait = after;
                        await Task.Delay(wait).ConfigureAwait(false);
                    }
                }
                var b64 = (string)JObject.Parse(text)["data"]?[0]?["b64_json"];
                if (string.IsNullOrEmpty(b64))
                {
                    Fail(job, "no picture in the answer");
                    return;
                }
                var dir = Path.GetDirectoryName(Path.GetFullPath(job.OutPath));
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(job.OutPath, Convert.FromBase64String(b64));
                // a "transparent" picture whose border came back painted: one cutout pass first (on the main thread to look)
                if (job.Cutout)
                {
                    mainThread.Enqueue(() => CutoutIfNeeded(job));
                    return;
                }
                Finish(job);
            }
            catch (Exception exc)
            {
                Fail(job, exc.Message);
            }
            finally
            {
                inFlight.Release();
            }
        });
    }

    private static void Finish(Job job)
    {
        job.State = JobState.Done;
        job.Finished = DateTime.UtcNow;
        Remember(job);
        if (job.Then != null) mainThread.Enqueue(() => job.Then(job));
    }

    private static void CutoutIfNeeded(Job job)
    {
        if (HasTransparentBorder(File.ReadAllBytes(job.OutPath)))
        {
            Finish(job);
            return;
        }
        // the cutout writes over the same file, then the job is done as asked
        var options = new Options { Model = Model.Flare, Size = "auto", Quality = "high", Transparent = true, AutoCutout = false };
        var raw = Path.ChangeExtension(job.OutPath, ".uncut.png");
        File.Copy(job.OutPath, raw, true);
        Edit(CutoutPrompt, new[] { raw }, job.OutPath, options, null, cut =>
        {
            if (cut.State != JobState.Done) File.Copy(raw, job.OutPath, true); // best effort: keep the original
            Finish(job);
        });
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

    /// <summary>The error's code and message (with the moderation stage and categories when a request was blocked).</summary>
    private static (string code, string message) ErrorOf(string text)
    {
        try
        {
            var e = JObject.Parse(text)["error"];
            var code = (string)e?["code"] ?? (string)e?["type"];
            var message = (string)e?["message"] ?? text;
            if (code == "moderation_blocked" && e?["moderation_details"] is JObject md)
            {
                var cats = md["categories"] is JArray c ? string.Join(", ", c.Values<string>()) : "";
                message += " (moderation stage: " + ((string)md["moderation_stage"] ?? "unknown") + (cats.Length > 0 ? "; categories: " + cats : "") + ")";
            }
            return (code, message);
        }
        catch
        {
            return (null, text.Length > 300 ? text.Substring(0, 300) : text);
        }
    }

    private static void Fail(Job job, string error)
    {
        job.State = JobState.Failed;
        // OpenAI repeats a masked piece of a rejected key in its message: never kept or shown
        job.Error = System.Text.RegularExpressions.Regex.Replace(error ?? "", @"sk-[A-Za-z0-9_\-\*]+", "(the key)");
        job.Finished = DateTime.UtcNow;
        Remember(job);
        mainThread.Enqueue(() => Debug.LogWarning("Image generator " + job.Id + " failed: " + error));
    }

    private static void Remember(Job job)
    {
        try
        {
            var path = Path.Combine("Library", "ImageGen", "history.jsonl");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var line = new JObject
            {
                ["time"] = job.Finished.ToString("o"),
                ["kind"] = job.Kind,
                ["model"] = ModelId(job.Model),
                ["prompt"] = job.Prompt,
                ["inputs"] = new JArray(job.Inputs),
                ["out"] = job.OutPath,
                ["state"] = job.State.ToString(),
                ["error"] = job.Error,
            };
            lock (jobs) File.AppendAllText(path, line.ToString(Formatting.None) + "\n");
        }
        catch { /* the history is a convenience */ }
    }
}
