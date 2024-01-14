using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TMPro;


using Shinobytes.IO;

using UnityEngine;
using System.Linq;
using Assets.Scripts.Overlay;
using System.Diagnostics;

public class GameUpdater : MonoBehaviour
{
    public string CheckUpdateUri = "https://localhost:5001/";

    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private GameProgressBar progressBar;

    public bool EditorOnlyStartAsOverlay;

    private GameUpdateHandler gameUpdater;
    private UpdateResult updateResult;
    private bool startingPatcher = false;
    private bool loadingScene;
    private int lastAcceptedVersion;

    private void Start()
    {
#if UNITY_STANDALONE_LINUX
        Overlay.IsGame = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
        return;
#endif

        var startupArgs = System.Environment.GetCommandLineArgs().Select(x => x.ToLower()).ToArray();
        var forceUpdate = startupArgs.Any(x => x.Contains("forceupdate") || x.Contains("force-update") || x.Contains("reinstall"));


        this.lastAcceptedVersion = PlayerPrefs.GetInt(CodeOfConductController.CoCLastAcceptedVersion_SettingsName, CodeOfConductController.CoCLastAcceptedVersion_DefaultValue);

        if (Application.isEditor)
        {
            Shinobytes.Debug.Log("Starting game using args: " + string.Join(",", startupArgs) + ", (IsEditor=True)");

            Overlay.IsGame = !EditorOnlyStartAsOverlay;
            StartUpdate(forceUpdate);
        }
        else
        {
            Shinobytes.Debug.Log("Starting game using args: " + string.Join(",", startupArgs));            

            CheckIfGameAsync(startupArgs).ContinueWith(async x =>
            {
                Overlay.IsGame = await x;
                StartUpdate(forceUpdate);
            });
        }

        if (progressBar)
        {
            progressBar.gameObject.SetActive(false);
        }
    }

    private void StartUpdate(bool forceUpdate)
    {
        Shinobytes.Debug.Log("Checking for updates: " + CheckUpdateUri);
        gameUpdater = new GameUpdateHandler(CheckUpdateUri);
        gameUpdater.UpdateAsync(forceUpdate, lastAcceptedVersion).ContinueWith(async res =>
        {
            updateResult = await res;
        });
    }

    private async Task<bool> CheckIfGameAsync(string[] args)
    {
        // if we contain the "overlay" argument
        // we should always start as an overlay.

        // if we dont contain it. We should assume we start as a game.
        // However, if there already is an instance running. We will assume it is an overlay as well.

        // Note: Final check could be to see whether or not its possible to connect to the overlay server.
        // if its not possible, we can only assume that the existing one is a game otherwise its an overlay.
        // 

        // We don't do that check for now, so players will have to close down both instances if they f' things up.        

        var isGame = args.Length == 0 || args.All(x => !x.Contains("overlay"));

        if (isGame)
        {
            try
            {
                isGame = System.Diagnostics.Process.GetProcesses().Count(x => x.ProcessName.ToLower().Contains("ravenfall") || x.ProcessName.ToLower().Contains("unity editor")) == 1;
            }
            catch { }

            if (!isGame)
            {
                var canConnectToServer = await OverlayClient.TestServerAvailabilityAsync();
                // finally, check 
                // if we can't connect to a server, assume the game is not running.
                return !canConnectToServer;

            }
        }

        return isGame;
    }

    private void Update()
    {
        if (gameUpdater == null)
        {
            return;
        }

        if (loadingScene)
        {
            return;
        }

        if (versionText)
        {
            versionText.text = "VERSION " + Ravenfall.Version;
        }

        if (!UnityEngine.Application.isEditor && UnityEngine.Debug.isDebugBuild)
        {
            loadingScene = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene(1);
            return;
        }

        if (updateResult == UpdateResult.CodeOfConductModified)
        {
            loadingScene = true;

            if (Overlay.IsGame)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(3);
                return;
            }


            UnityEngine.SceneManagement.SceneManager.LoadScene(2);
            return;
        }

        if (updateResult == UpdateResult.UpToDate)
        {
            loadingScene = true;

            if (Overlay.IsGame)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(1);
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(2);
            return;
        }

        if (startingPatcher)
        {
            return;
        }

        if (updateResult == UpdateResult.Failed)
        {
            DisplayError_Generic();
            return;
        }

        if (updateResult == UpdateResult.Failed_NoInternet)
        {
            // display error message on screen
            DisplayError_NoConnection();
            return;
        }

        var progress = gameUpdater.GetDownloadProgress();
        if (progress == null)
        {
            return;
        }

        if (updateResult == UpdateResult.Success)
        {
            StartPatcher();
            return;
        }

        UpdateProgress((double)progress.BytesDownloaded / progress.FileSize);
    }


    private void DisplayError_Generic()
    {
        if (label)
        {
            label.text = "Error occurred when trying to update the game. \r\nPress space to open the log folder.";
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TryOpenLogFolder();
            }
        }
    }

    private static void TryOpenLogFolder()
    {
        try
        {
            var path = System.IO.Path.GetDirectoryName(Application.consoleLogPath);
            System.Diagnostics.Process.Start("explorer.exe", path);
        }
        catch { }
    }

    private void DisplayError_NoConnection()
    {
        if (label)
        {
            label.text = "Unable to connect to the server. Please check your internet connection.";
        }
    }

    private void UpdateProgress(double progressPercent)
    {

        if (label)
        {
            var updateInfo = gameUpdater.GetUpdateInfo();
            label.text = "Downloading update " + updateInfo?.Version;
        }

        if (progressBar)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.displayProgress = true;
            progressBar.Progress = (float)progressPercent;
        }
    }

    private void StartPatcher()
    {
        startingPatcher = true;

        CreateMetaDataFile();

        if (label)
        {
            label.text = "Initializing update.";
        }

        // KillRavenBot();

        // 2. start patcher
        if (File.Exists("RavenWeave.exe"))
        {
            var updater = new ProcessStartInfo
            {
                FileName = Path.Combine("RavenWeave.exe"),
                WorkingDirectory = Path.GameFolder
            };

            Process.Start(updater);
        }
        else
        {
            Shinobytes.Debug.LogWarning("RavenWeave.exe could not be found. Unable to start the patcher.");
        }

        // 3. stop unity game
        Application.Quit();
    }

    private void KillRavenBot()
    {
        try
        {
            // 1. stop the bot if its running
            var processes = System.Diagnostics.Process.GetProcesses();
            var botProcess = processes.FirstOrDefault(x =>
            {
                if (x.HasExited)
                {
                    return false;
                }
                string name = "";
                try
                {
                    name = x.ProcessName;
                    if (name.IndexOf("ravenbot", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                catch
                {
                    // ignored. we dont have read access to all names.
                }
                return false;
            });

            if (botProcess != null)
            {
                TryStopProcess(botProcess);
            }
        }
        catch (Exception exc)
        {
            // Note: one or more processes has been terminated as this was doing a test.
            // does not necessarily have to be ravenbot, but could be anything. Casting 
            // InvalidOperationException: Process has exited or is inaccessible, so the requested information is not available.
            Shinobytes.Debug.LogWarning("Failed to stop local running RavenBot. This can be ignored. Error: " + exc.Message);
        }
    }

    private void TryStopProcess(System.Diagnostics.Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

            process.Refresh();
            if (process.HasExited)
            {
                return;
            }

            process.Kill();
        }
        catch
        {
            Shinobytes.Debug.LogError("Failed to stop the ravenbot before running the patcher.");
        }
    }

    private void CreateMetaDataFile()
    {
        var metadata = new MetaData
        {
            Version = Ravenfall.Version
        };
        var metadataContent = JsonConvert.SerializeObject(metadata);
        File.WriteAllText("metadata.json", metadataContent);
    }
}

public class MetaData
{
    public string Version { get; set; }
    public bool IsAlpha => Version?.IndexOf("a", StringComparison.OrdinalIgnoreCase) >= 0;
    public bool IsBeta => Version?.IndexOf("b", StringComparison.OrdinalIgnoreCase) >= 0;
}
public enum UpdateResult
{
    None,
    UpToDate,
    CodeOfConductModified,
    Success,
    Failed_NoInternet,
    Failed
}
public class GameUpdateHandler
{
    private readonly object mutex = new object();
    private readonly string host;
    private readonly string version;
    private DownloadProgress lastDownloadProgress;
    private UpdateData latestUpdate;
    public GameUpdateHandler(string host)
    {
        this.host = host;
        version = Ravenfall.Version;
        if (!this.host.EndsWith("/")) this.host += "/";
    }

    public async Task<UpdateResult> UpdateAsync(bool forceUpdate = false, int lastAcceptedCoCVersion = -1)
    {
        Shinobytes.Debug.Log("Downloading update information."); ;
        latestUpdate = await DownloadUpdateInfoAsync();
        if (latestUpdate == null)
        {
            Shinobytes.Debug.Log("Updating failed, no response from server.");
            return UpdateResult.Failed_NoInternet;
        }

        var coc = latestUpdate.CodeOfConduct;
        if (coc != null && lastAcceptedCoCVersion != coc.Revision)
        {
            if (Application.isEditor || coc.VisibleInClient)
            {
                CodeOfConductController.CodeOfConduct = coc;
                return UpdateResult.CodeOfConductModified;
            }
        }

        if (GameVersion.GetApplicationVersion() >= latestUpdate.GetVersion() && !forceUpdate)
        {
            Shinobytes.Debug.Log("Client is up to date.");
            return UpdateResult.UpToDate;
        }

        Shinobytes.Debug.Log("Downloading update " + latestUpdate.DownloadUrl + "..."); ;
        if (await DownloadUpdateAsync(latestUpdate.DownloadUrl, OnDownloadProgressChanged))
        {
            Shinobytes.Debug.Log("Update downloaded successfully."); ;
            return UpdateResult.Success;
        }


        return UpdateResult.Failed;
    }

    public DownloadProgress GetDownloadProgress()
    {
        lock (mutex)
        {
            return lastDownloadProgress;
        }
    }

    private void OnDownloadProgressChanged(DownloadProgress progress)
    {
        lock (mutex)
        {
            lastDownloadProgress = progress;
        }
    }

    private async Task<bool> DownloadUpdateAsync(
        string downloadUrl,
        Action<DownloadProgress> downloadProgress)
    {
        Shinobytes.Debug.Log("Downloading update file: " + downloadUrl);
        try
        {
            var fileName = downloadUrl.Split('/').LastOrDefault();

#if UNITY_STANDALONE_LINUX
            downloadUrl = downloadUrl.Replace("update.7z", "linux-update.7z");
#endif

            var updateFile = "update/" + fileName;

            if (!Directory.Exists("update"))
            {
                Directory.CreateDirectory("update");
            }

            if (File.Exists(updateFile))
            {
                File.Delete(updateFile);
            }

            updateFile = Shinobytes.IO.Path.GetFilePath(updateFile);

            var start = DateTime.Now;
            WebRequest.DefaultWebProxy = null;
            using (var wc = new WebClient())
            {
                wc.Proxy = null;
                wc.DownloadProgressChanged += (s, e) =>
                {
                    var elapsed = DateTime.Now - start;
                    downloadProgress?.Invoke(new DownloadProgress(e.TotalBytesToReceive, e.BytesReceived, e.BytesReceived / elapsed.TotalSeconds));
                };

                await wc.DownloadFileTaskAsync(downloadUrl, updateFile);
                var el = DateTime.Now - start;
                Shinobytes.Debug.Log("Download took " + el.TotalSeconds + " seconds.");
            }

            return true;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Unable to download update: " + exc.Message);
            return false;
        }
    }
    public async Task<UpdateData> DownloadUpdateInfoAsync()
    {
        try
        {
            var url = host + "api/version/check";
            var req = (HttpWebRequest)HttpWebRequest.Create(url);
            req.Method = HttpMethod.Get.Method;
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36";
            req.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;

            Shinobytes.Debug.Log("HTTP GET: " + url);
            using (var res = await req.GetResponseAsync())
            using (var stream = res.GetResponseStream())
            using (var sr = new System.IO.StreamReader(stream))
            {
                var update = await sr.ReadToEndAsync();
                var updateData = JsonConvert.DeserializeObject<UpdateData>(update);

                if (updateData.Version == version)
                {
                    return updateData;
                }

                try
                {
                    if (!Directory.Exists("update"))
                    {
                        Directory.CreateDirectory("update");
                    }

                    File.WriteAllText("update/update.json", update);
                }
                catch (Exception exc)
                {
                    Shinobytes.Debug.LogError("Unable to save update json: " + exc.Message);
                }

                return updateData;
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Error downloading update information: " + exc.Message);
            // Ignore request issues.
        }
        return null;
    }

    public UpdateData GetUpdateInfo()
    {
        return latestUpdate;
    }
}


public class UpdateData
{
    public string DownloadUrl { get; set; }
    public string Version { get; set; }
    public bool IsAlpha => Version?.IndexOf("a", StringComparison.OrdinalIgnoreCase) >= 0;
    public bool IsBeta => Version?.IndexOf("b", StringComparison.OrdinalIgnoreCase) >= 0;
    public DateTime Released { get; set; }
    public RavenNest.Models.CodeOfConduct CodeOfConduct { get; set; }
    public Version GetVersion()
    {
        if (GameVersion.TryParse(Version, out var version))
            return version;

        return new Version();
    }
}

public static class GameVersion
{
    public static Version GetApplicationVersion()
    {
        if (TryParse(Ravenfall.Version, out var version))
        {
            return version;
        }
        return new Version();
    }

    public static bool TryParse(string input, out Version version)
    {
        if (string.IsNullOrEmpty(input))
        {
            version = null;
            return false;
        }

        var versionToLower = input.ToLower();
        var versionString = versionToLower.Replace("a-alpha", "").Replace("v", "").Replace("a", "").Replace("b", "");
        return System.Version.TryParse(versionString, out version);
    }
}