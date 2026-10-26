// The Image Generator's worker: a small local service the Unity editor starts (Ravenfall > Image Generator, ImageGen.cs)
// so that pictures in flight survive script reloads, editor restarts and crashes. It keeps the job list on disk, sends
// the requests to OpenAI's image models (GPT-Image-2.5 Flare and Sunburst), writes each picture to its output path and
// answers how the jobs are doing. Anything that needs the picture itself (the cutout check, installing an icon) is done
// by the editor when it sees a job finished.
//
// Listens on 127.0.0.1 only (IMAGEGEN_PORT, default 57421). The API key comes from OPENAI_API_KEY in its environment,
// or from the X-OpenAI-Key header of the editor's requests; it is kept in memory only, never written or logged. Jobs live
// in IMAGEGEN_DATA/jobs.json (the editor passes the project's Library/ImageGen/service). A request that was sent but not
// answered when the service stopped is never sent again: it may already be billed, so it is marked as interrupted.
// Following what the Buildathon studio learned (D:\Git\Buildathon, OpenAiImageProvider): quoted multipart names, no
// input_fidelity, rate limits and 5xx retried, quota and spend limits not, never a resend after a timeout.
//
// Endpoints: GET /health; POST /jobs; GET /jobs (?unhandled=true); GET /jobs/{id}; POST /jobs/{id}/handled;
// DELETE /jobs/{id} (a queued one); POST /shutdown. It stops by itself after 30 minutes with nothing to do.
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

var port = int.TryParse(Environment.GetEnvironmentVariable("IMAGEGEN_PORT"), out var p) ? p : 57421;
var dataDir = Environment.GetEnvironmentVariable("IMAGEGEN_DATA");
if (string.IsNullOrWhiteSpace(dataDir)) dataDir = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDir);

var store = new JobStore(Path.Combine(dataDir, "jobs.json"), Path.Combine(dataDir, "service.log"));
store.Key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")?.Trim();
store.Load();

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:" + port);
builder.Logging.ClearProviders();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
var app = builder.Build();
var stopping = app.Lifetime.ApplicationStopping;

// every request may bring the editor's key (kept in memory only), and counts as activity
app.Use(async (context, next) =>
{
    var key = context.Request.Headers["X-OpenAI-Key"].ToString();
    if (!string.IsNullOrWhiteSpace(key)) store.Key = key.Trim();
    store.Touch();
    await next();
});

app.MapGet("/health", () => Results.Json(new { ok = true, version = 1, hasKey = !string.IsNullOrEmpty(store.Key), queued = store.Count(JobState.Queued), running = store.Count(JobState.Running) }));
app.MapPost("/jobs", (JobRequest request) =>
{
    var error = request.Validate();
    if (error != null) return Results.BadRequest(new { error });
    return Results.Json(store.Add(request));
});
app.MapGet("/jobs", (bool? unhandled, int? last) => Results.Json(store.List(unhandled == true, last ?? 200)));
app.MapGet("/jobs/{id}", (string id) => store.Get(id) is { } job ? Results.Json(job) : Results.NotFound());
app.MapPost("/jobs/{id}/handled", (string id) => store.MarkHandled(id) ? Results.Ok() : Results.NotFound());
app.MapDelete("/jobs/{id}", (string id) => store.Cancel(id) ? Results.Ok() : Results.Conflict(new { error = "only a queued job can be cancelled" }));
app.MapPost("/shutdown", () => { _ = Task.Run(async () => { await Task.Delay(200); app.Lifetime.StopApplication(); }); return Results.Ok(); });

var worker = new Worker(store);
_ = Task.Run(() => worker.RunAsync(stopping));
_ = Task.Run(async () =>
{
    // nothing to do for half an hour: stop (the editor starts it again when it needs it)
    while (!stopping.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromMinutes(1));
        if (store.Count(JobState.Queued) + store.Count(JobState.Running) == 0 && DateTime.UtcNow - store.LastActivity > TimeSpan.FromMinutes(30))
        {
            store.Log("idle for 30 minutes, stopping");
            app.Lifetime.StopApplication();
        }
    }
});

store.Log("started on port " + port + ", " + store.Count(JobState.Queued) + " queued");
app.Run();
store.Save();

// ---- The jobs ----------------------------------------------------------------------------------------------------

enum JobState { Queued, Running, Done, Failed, Interrupted }

sealed class JobRequest
{
    public string Kind { get; set; } = "generate";   // generate or edit
    public string Model { get; set; } = "flare";     // flare or sunburst
    public string Prompt { get; set; }
    public string[] Inputs { get; set; } = Array.Empty<string>();
    public string Mask { get; set; }
    public string OutPath { get; set; }
    public string Size { get; set; } = "1024x1024";
    public string Quality { get; set; } = "high";
    public bool Transparent { get; set; } = true;
    public bool AutoCutout { get; set; } = true;
    /// <summary>For the editor: what to do with the picture when it is done ("icon:raw-tuna").</summary>
    public string Tag { get; set; }

    public string Validate()
    {
        if (Kind != "generate" && Kind != "edit") return "kind is generate or edit";
        if (Model != "flare" && Model != "sunburst") return "model is flare or sunburst";
        if (string.IsNullOrWhiteSpace(Prompt)) return "no prompt";
        if (string.IsNullOrWhiteSpace(OutPath) || !Path.IsPathRooted(OutPath)) return "outPath must be a full path";
        if (Kind == "edit" && (Inputs == null || Inputs.Length == 0 || Inputs.Length > 16)) return "an edit takes 1 to 16 input images";
        foreach (var input in Inputs ?? Array.Empty<string>()) if (!File.Exists(input)) return "no such input: " + input;
        return null;
    }
}

sealed class Job
{
    public string Id { get; set; }
    public JobRequest Request { get; set; }
    public JobState State { get; set; }
    public string Error { get; set; }
    public bool Handled { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Started { get; set; }
    public DateTime? Finished { get; set; }
}

sealed class JobStore
{
    private readonly string path, logPath;
    private readonly object sync = new();
    private List<Job> jobs = new();
    private int next;

    public string Key;
    public DateTime LastActivity { get; private set; } = DateTime.UtcNow;
    public readonly SemaphoreSlim Wake = new(0);

    public JobStore(string path, string logPath) { this.path = path; this.logPath = logPath; }

    public void Touch() => LastActivity = DateTime.UtcNow;

    public void Load()
    {
        lock (sync)
        {
            if (File.Exists(path))
            {
                try { jobs = JsonSerializer.Deserialize<List<Job>>(File.ReadAllText(path), Json.Options) ?? new(); }
                catch (Exception e) { Log("could not read the jobs: " + e.Message); jobs = new(); }
            }
            foreach (var job in jobs.Where(j => j.State == JobState.Running))
            {
                // sent but not answered when the service stopped: it may be billed, so it is never sent again
                job.State = JobState.Interrupted;
                job.Error = "interrupted while waiting for the answer; not sent again, as it may already be billed";
                job.Finished = DateTime.UtcNow;
            }
            next = jobs.Count == 0 ? 0 : jobs.Max(j => int.TryParse(j.Id.AsSpan(3), out var n) ? n : 0);
            // done and handled more than a week ago: forgotten
            jobs.RemoveAll(j => j.Handled && j.Finished < DateTime.UtcNow.AddDays(-7));
            SaveLocked();
        }
    }

    public Job Add(JobRequest request)
    {
        lock (sync)
        {
            var job = new Job { Id = "img" + (++next), Request = request, State = JobState.Queued, Created = DateTime.UtcNow };
            jobs.Add(job);
            SaveLocked();
            Wake.Release();
            return job;
        }
    }

    public Job TakeQueued()
    {
        lock (sync)
        {
            var job = jobs.FirstOrDefault(j => j.State == JobState.Queued);
            if (job == null) return null;
            job.State = JobState.Running;
            job.Started = DateTime.UtcNow;
            SaveLocked();
            return job;
        }
    }

    public void Finish(Job job, JobState state, string error = null)
    {
        lock (sync)
        {
            job.State = state;
            job.Error = error == null ? null : Regex.Replace(error, @"sk-[A-Za-z0-9_\-\*]+", "(the key)");
            job.Finished = DateTime.UtcNow;
            SaveLocked();
        }
        Log(job.Id + " " + state + (error != null ? ": " + job.Error : ""));
    }

    public Job Get(string id) { lock (sync) return jobs.FirstOrDefault(j => j.Id == id); }
    public int Count(JobState state) { lock (sync) return jobs.Count(j => j.State == state); }

    public List<Job> List(bool unhandled, int last)
    {
        lock (sync)
        {
            var list = unhandled ? jobs.Where(j => !j.Handled && (j.State == JobState.Done || j.State == JobState.Failed || j.State == JobState.Interrupted)).ToList() : jobs;
            return list.Skip(Math.Max(0, list.Count - last)).ToList();
        }
    }

    public bool MarkHandled(string id)
    {
        lock (sync)
        {
            var job = jobs.FirstOrDefault(j => j.Id == id);
            if (job == null) return false;
            job.Handled = true;
            SaveLocked();
            return true;
        }
    }

    public bool Cancel(string id)
    {
        lock (sync)
        {
            var job = jobs.FirstOrDefault(j => j.Id == id);
            if (job == null || job.State != JobState.Queued) return false;
            job.State = JobState.Failed;
            job.Error = "cancelled";
            job.Finished = DateTime.UtcNow;
            SaveLocked();
            return true;
        }
    }

    public void Save() { lock (sync) SaveLocked(); }

    private void SaveLocked()
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(jobs, Json.Options));
        File.Move(temp, path, true);
    }

    public void Log(string line)
    {
        try { lock (sync) File.AppendAllText(logPath, DateTime.UtcNow.ToString("o") + " " + line + Environment.NewLine); }
        catch { /* the log is a convenience */ }
    }
}

static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
}

// ---- Sending ---------------------------------------------------------------------------------------------------

sealed class Worker
{
    private const int InFlight = 4;
    private const int MaxRetries = 3;
    private static readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(6) };
    private readonly JobStore store;
    private readonly SemaphoreSlim slots = new(InFlight);

    public Worker(JobStore store) { this.store = store; }

    public async Task RunAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            await slots.WaitAsync(stop);
            var job = store.TakeQueued();
            if (job == null)
            {
                slots.Release();
                await Task.WhenAny(store.Wake.WaitAsync(stop), Task.Delay(1000, stop));
                continue;
            }
            _ = Task.Run(async () =>
            {
                try { await SendAsync(job); }
                catch (Exception e) { store.Finish(job, JobState.Failed, e.Message); }
                finally { slots.Release(); }
            });
        }
    }

    private static string ModelId(string model) => model == "sunburst" ? "gpt-image-2.5-sunburst" : "gpt-image-2.5-flare";

    private async Task SendAsync(Job job)
    {
        var key = store.Key;
        if (string.IsNullOrEmpty(key))
        {
            store.Finish(job, JobState.Failed, "no OpenAI key: save one in Ravenfall > Image Generator or set OPENAI_API_KEY");
            return;
        }
        var r = job.Request;
        string text = null;
        for (var attempt = 0; ; attempt++)
        {
            using var message = Build(r);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            HttpResponseMessage response;
            try
            {
                response = await http.SendAsync(message);
            }
            catch (Exception e) when (e is TaskCanceledException || e is HttpRequestException)
            {
                store.Finish(job, JobState.Interrupted, "no answer (" + e.Message + "); not sent again, as it may already be billed");
                return;
            }
            using (response)
            {
                text = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode) break;
                var status = (int)response.StatusCode;
                var (code, error) = ErrorOf(text);
                var quota = code is "insufficient_quota" or "credit_balance_exhausted" or "organization_spend_limit_exceeded" or "project_spend_limit_exceeded" or "organization_usage_limit_exceeded";
                var retry = (status == 429 && !quota) || status >= 500;
                if (!retry || attempt >= MaxRetries)
                {
                    store.Finish(job, JobState.Failed, status + (quota ? " quota or spend limit: " : " ") + error);
                    return;
                }
                var wait = TimeSpan.FromSeconds(2 << attempt);
                if (response.Headers.RetryAfter?.Delta is TimeSpan after && after > wait) wait = after;
                await Task.Delay(wait);
            }
        }

        var b64 = (string)JsonNode.Parse(text)?["data"]?[0]?["b64_json"];
        if (string.IsNullOrEmpty(b64))
        {
            store.Finish(job, JobState.Failed, "no picture in the answer");
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(r.OutPath));
        var temp = r.OutPath + ".part";
        await File.WriteAllBytesAsync(temp, Convert.FromBase64String(b64));
        File.Move(temp, r.OutPath, true);
        store.Finish(job, JobState.Done);
    }

    private static HttpRequestMessage Build(JobRequest r)
    {
        const string endpoint = "https://api.openai.com/v1/images/";
        if (r.Kind == "generate")
        {
            var body = new JsonObject
            {
                ["model"] = ModelId(r.Model),
                ["prompt"] = r.Prompt,
                ["size"] = r.Size,
                ["quality"] = r.Quality,
                ["output_format"] = "png",
                ["n"] = 1,
            };
            if (r.Transparent) body["background"] = "transparent";
            return new HttpRequestMessage(HttpMethod.Post, endpoint + "generations") { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };
        }

        // quoted explicitly, so the wire shows name="image[]"; filename="image0.png" (unquoted, the brackets are not read)
        var form = new MultipartFormDataContent();
        void Text(string name, string value) => form.Add(new StringContent(value), "\"" + name + "\"");
        void Png(string name, string fileName, string file)
        {
            var part = new ByteArrayContent(File.ReadAllBytes(file));
            part.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            part.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "\"" + name + "\"", FileName = "\"" + fileName + "\"" };
            form.Add(part);
        }
        Text("model", ModelId(r.Model));
        Text("prompt", r.Prompt);
        Text("size", r.Size);
        Text("quality", r.Quality);
        Text("output_format", "png");
        Text("n", "1");
        if (r.Transparent) Text("background", "transparent");
        for (var i = 0; i < r.Inputs.Length; i++) Png("image[]", "image" + i + ".png", r.Inputs[i]);
        if (!string.IsNullOrEmpty(r.Mask)) Png("mask", "mask.png", r.Mask);
        return new HttpRequestMessage(HttpMethod.Post, endpoint + "edits") { Content = form };
    }

    private static (string code, string message) ErrorOf(string text)
    {
        try
        {
            var e = JsonNode.Parse(text)?["error"];
            var code = (string)e?["code"] ?? (string)e?["type"];
            var message = (string)e?["message"] ?? text;
            if (code == "moderation_blocked" && e?["moderation_details"] is JsonObject md)
            {
                var cats = md["categories"] is JsonArray c ? string.Join(", ", c.Select(x => (string)x)) : "";
                message += " (moderation stage: " + ((string)md["moderation_stage"] ?? "unknown") + (cats.Length > 0 ? "; categories: " + cats : "") + ")";
            }
            return (code, message);
        }
        catch
        {
            return (null, text.Length > 300 ? text[..300] : text);
        }
    }
}
