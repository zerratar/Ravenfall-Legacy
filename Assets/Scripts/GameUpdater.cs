using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TMPro;

using UnityEngine;
using System.Linq;
public class GameUpdater : MonoBehaviour
{
    public string CheckUpdateUri = "https://localhost:5001/";

    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private GameProgressBar progressBar;

    private GameUpdateHandler gameUpdater;
    private UpdateResult updateResult;
    private bool startingPatcher = false;
    private bool loadingScene;

    private void Start()
    {
        gameUpdater = new GameUpdateHandler(CheckUpdateUri);
        gameUpdater.UpdateAsync().ContinueWith(async res =>
        {
            updateResult = await res;
        });

        if (progressBar)
        {
            progressBar.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (loadingScene)
        {
            return;
        }

        if (versionText)
        {
            versionText.text = "VERSION " + Application.version;
        }

        if (updateResult == UpdateResult.UpToDate)
        {
            loadingScene = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene(1);
            return;
        }

        if (startingPatcher)
        {
            return;
        }

        if (updateResult == UpdateResult.Failed)
        {
            // display error message on screen            
            DisplayError();
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

    private void DisplayError()
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

        // 1. start patcher
        System.Diagnostics.Process.Start("RavenWeave.exe");

        // 2. stop unity game
        Application.Quit();
    }

    private void CreateMetaDataFile()
    {
        var metadata = new MetaData
        {
            Version = Application.version
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
    Success,
    Failed,
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
        version = Application.version;
        if (!this.host.EndsWith("/")) this.host += "/";
    }

    public async Task<UpdateResult> UpdateAsync()
    {
        latestUpdate = await DownloadUpdateInfoAsync();
        if (latestUpdate == null)
        {
            return UpdateResult.Failed;
        }

        if (latestUpdate.Version == version)
        {
            return UpdateResult.UpToDate;
        }

        if (await DownloadUpdateAsync(latestUpdate.DownloadUrl, OnDownloadProgressChanged))
        {
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
        try
        {
            var fileName = downloadUrl.Split('/').LastOrDefault();
            var updateFile = "update/" + fileName;
            if (!Directory.Exists("update"))
            {
                Directory.CreateDirectory("update");
            }

            if (File.Exists(updateFile))
            {
                File.Delete(updateFile);
            }

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
                Debug.LogWarning("Download took " + el.TotalSeconds + " seconds.");
            }

            //downloadProgress?.Invoke(new DownloadProgress(fileSize, totalRead, 0));

            //var req = (HttpWebRequest)HttpWebRequest.Create(downloadUrl);
            //req.Method = HttpMethod.Get.Method;
            //using (var res = await req.GetResponseAsync())
            //using (var str = res.GetResponseStream())
            //using (var fs = new FileStream(updateFile, FileMode.Create, FileAccess.Write))
            //{
            //    var fileSize = res.ContentLength;
            //    var buffer = new byte[4096 * 2];
            //    var read = 0;
            //    var totalRead = 0;
            //    var lastProgressChange = DateTime.Now;
            //    downloadProgress?.Invoke(new DownloadProgress(fileSize, totalRead, 0));

            //    await str.CopyToAsync(fs);

            //    while ((read = await str.ReadAsync(buffer, 0, buffer.Length)) != 0)
            //    {


            //        await fs.WriteAsync(buffer, 0, read);
            //        totalRead += read;

            //        var elapsed = DateTime.Now - start;
            //        var elapsedProgressChange = DateTime.Now - lastProgressChange;
            //        if (elapsedProgressChange >= TimeSpan.FromSeconds(1))
            //        {
            //            downloadProgress?.Invoke(new DownloadProgress(fileSize, totalRead, totalRead / elapsed.TotalSeconds));
            //            lastProgressChange = DateTime.Now;
            //        }
            //    }

            //downloadProgress?.Invoke(new DownloadProgress(fileSize, totalRead, 0));
            //}

            return true;
        }
        catch (Exception exc)
        {   
            Debug.LogError("Unable to download update: " + exc.ToString());
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

            using (var res = await req.GetResponseAsync())
            using (var stream = res.GetResponseStream())
            using (var sr = new StreamReader(stream))
            {
                var update = await sr.ReadToEndAsync();
                var updateData = JsonConvert.DeserializeObject<UpdateData>(update);
                if (!Directory.Exists("update"))
                {
                    Directory.CreateDirectory("update");
                }

                File.WriteAllText("update/update.json", update);
                return updateData;
            }
        }
        catch (Exception exc)
        {
            Debug.LogError("Error downloading update information: " + exc.ToString());
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
}