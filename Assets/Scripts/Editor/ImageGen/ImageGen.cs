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

        public override string ToString() =>
            Id + " " + Kind + " " + State + (State == JobState.Failed ? ": " + Error : "") + " -> " + OutPath +
            " (" + (int)((State == JobState.Running ? DateTime.UtcNow : Finished) - Started).TotalSeconds + " s)";
    }

    private const string KeyPref = "Ravenfall.ImageGen.OpenAIKey";
    private const string Endpoint = "https://api.openai.com/v1/images/";
    private static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromMinutes(6) };
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
        foreach (var f in files)
        {
            if (File.Exists(f)) continue;
            Fail(job, "no such input: " + f);
            return job;
        }
        Run(job, () =>
        {
            var form = new MultipartFormDataContent
            {
                { new StringContent(ModelId(options.Model)), "model" },
                { new StringContent(prompt), "prompt" },
                { new StringContent(options.Size), "size" },
                { new StringContent(options.Quality), "quality" },
                { new StringContent("png"), "output_format" },
                { new StringContent("1"), "n" },
            };
            if (options.Transparent) form.Add(new StringContent("transparent"), "background");
            foreach (var f in files) form.Add(Png(f), "image[]", Path.GetFileName(f));
            if (!string.IsNullOrEmpty(mask)) form.Add(Png(Path.GetFullPath(mask)), "mask", Path.GetFileName(mask));
            return new HttpRequestMessage(HttpMethod.Post, Endpoint + "edits") { Content = form };
        });
        return job;
    }

    private static ByteArrayContent Png(string path)
    {
        var content = new ByteArrayContent(File.ReadAllBytes(path));
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        return content;
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
            try
            {
                using var message = request();
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                using var response = await http.SendAsync(message).ConfigureAwait(false);
                var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    Fail(job, (int)response.StatusCode + " " + ErrorOf(text));
                    return;
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
                job.State = JobState.Done;
                job.Finished = DateTime.UtcNow;
                Remember(job);
                if (job.Then != null) mainThread.Enqueue(() => job.Then(job));
            }
            catch (Exception exc)
            {
                Fail(job, exc.Message);
            }
        });
    }

    private static string ErrorOf(string text)
    {
        try { return (string)JObject.Parse(text)["error"]?["message"] ?? text; }
        catch { return text.Length > 300 ? text.Substring(0, 300) : text; }
    }

    private static void Fail(Job job, string error)
    {
        job.State = JobState.Failed;
        job.Error = error;
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
