using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Moves a new updater into place. The updater's own files cannot come with an update the normal way, since they are
/// in use while it runs (Windows: RavenWeave.exe and its libraries; Linux: update.sh, which bash reads while it
/// runs), so the release tool puts them in updater\ inside the update, the updater installs that folder like any
/// other, and the game moves its contents over the old ones when it next starts. Before this, an updater was only
/// ever replaced by a full download.
/// </summary>
public static class UpdaterSwap
{
    private const int Attempts = 30;

    public static void Apply()
    {
        var gameFolder = Shinobytes.IO.Path.GameFolder;
        var source = Path.Combine(gameFolder, "updater");
        if (!Directory.Exists(source))
        {
            return;
        }

        // in the background: the updater that started the game may take a moment to close and let go of its files
        Task.Run(() =>
        {
            for (var attempt = 1; attempt <= Attempts; ++attempt)
            {
                try
                {
                    foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
                    {
                        var relative = file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        var target = Path.Combine(gameFolder, relative);
                        var targetFolder = Path.GetDirectoryName(target);
                        if (!string.IsNullOrEmpty(targetFolder)) Directory.CreateDirectory(targetFolder);
                        if (File.Exists(target)) File.SetAttributes(target, FileAttributes.Normal);
                        File.Copy(file, target, true);
                    }

#if UNITY_STANDALONE_LINUX
                    MakeExecutable(Path.Combine(gameFolder, "update.sh"));
#endif
                    Directory.Delete(source, true);
                    Shinobytes.Debug.Log("Installed the new updater.");
                    return;
                }
                catch (Exception exc) when (exc is IOException || exc is UnauthorizedAccessException)
                {
                    if (attempt == Attempts)
                    {
                        Shinobytes.Debug.LogWarning("Could not install the new updater yet (tried again at the next start): " + exc.Message);
                        return;
                    }

                    Thread.Sleep(1000);
                }
                catch (Exception exc)
                {
                    Shinobytes.Debug.LogWarning("Could not install the new updater: " + exc.Message);
                    return;
                }
            }
        });
    }

#if UNITY_STANDALONE_LINUX
    private static void MakeExecutable(string file)
    {
        if (!File.Exists(file)) return;
        try
        {
            using (var chmod = Process.Start(new ProcessStartInfo("/bin/chmod", "+x \"" + file + "\"") { UseShellExecute = false, CreateNoWindow = true }))
            {
                chmod?.WaitForExit(5000);
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("Could not make " + file + " executable: " + exc.Message);
        }
    }
#endif
}
