using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

/// <summary>
/// The release builds: Windows and Linux, CoreCLR, not Development, into Build-release-windows and Build-release-linux,
/// which the release tool in RavenNest (src/tools/RavenNest.Tools) packs into the GitHub release files.
/// </summary>
/// <remarks>
/// Three ways in: Ravenfall > Release > Build Windows and Linux, Ravenfall > Release > Build and Package (also starts
/// the release tool when both builds succeed), and the release tool itself ("RavenNest.Tools package --build"), which
/// runs <see cref="BuildFromCommandLine"/> in a batch mode editor when this project is not open. Each build folder is
/// emptied first, so nothing left over from running a build in place gets shipped. The outcome is written to
/// Logs/release-build.txt.
/// </remarks>
public static class ReleaseBuild
{
    private const string PackagerPath = @"G:\Ravenfall\Projects\RavenNest\src\tools\RavenNest.Tools\bin\Release\net9.0\RavenNest.Tools.exe";
    private const string LogPath = "Logs/release-build.txt";

    private static readonly (string folder, BuildTarget target, string extension)[] Builds =
    {
        ("Build-release-windows", BuildTarget.StandaloneWindows64, ".exe"),
        ("Build-release-linux", BuildTarget.StandaloneLinux64, ".x86_64"),
    };

    [MenuItem("Ravenfall/Release/Build Windows and Linux")]
    private static void BuildMenu()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            Report(BuildAll());
    }

    [MenuItem("Ravenfall/Release/Build and Package")]
    private static void BuildAndPackageMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (!Report(BuildAll()))
            return;

        if (!File.Exists(PackagerPath))
        {
            EditorUtility.DisplayDialog("Release", "Both builds are done, but the release tool is not built: " + PackagerPath, "OK");
            return;
        }

        // in its own window, so the packaging (a few minutes) shows its progress and the editor stays free
        Process.Start(new ProcessStartInfo("cmd.exe", "/k \"\"" + PackagerPath + "\" package\"")
        {
            WorkingDirectory = Path.GetDirectoryName(PackagerPath),
            UseShellExecute = true,
        });
    }

    /// <summary>For -executeMethod ReleaseBuild.BuildFromCommandLine: exits 0 when both builds succeed, 1 otherwise.</summary>
    public static void BuildFromCommandLine()
    {
        var ok = false;
        try
        {
            ok = BuildAll().ok;
        }
        catch (Exception exc)
        {
            File.AppendAllText(LogPath, "exception " + exc + Environment.NewLine);
        }
        finally
        {
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }

    private static bool Report((bool ok, string log) result)
    {
        EditorUtility.DisplayDialog("Release", (result.ok ? "Both release builds succeeded." : "A release build failed.") + Environment.NewLine + Environment.NewLine + result.log, "OK");
        return result.ok;
    }

    private static (bool ok, string log) BuildAll()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
        var log = new StringBuilder();
        log.AppendLine("Ravenfall " + UnityEditor.PlayerSettings.bundleVersion + ", started " + DateTime.Now);
        File.WriteAllText(LogPath, log.ToString());

        var standalone = NamedBuildTarget.Standalone;
        var backend = UnityEditor.PlayerSettings.GetScriptingBackend(standalone);
        var scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
        var ok = true;
        try
        {
            UnityEditor.PlayerSettings.SetScriptingBackend(standalone, ScriptingImplementation.CoreCLR);
            foreach (var build in Builds)
            {
                if (Directory.Exists(build.folder))
                    Directory.Delete(build.folder, true);

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = Path.Combine(build.folder, UnityEditor.PlayerSettings.productName + build.extension),
                    target = build.target,
                    options = BuildOptions.None,
                });

                var summary = report.summary;
                log.AppendLine($"{build.folder}: {summary.result} in {summary.totalTime:mm\\:ss}, {summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors");
                ok &= summary.result == BuildResult.Succeeded;
                File.WriteAllText(LogPath, log.ToString());
            }
        }
        finally
        {
            UnityEditor.PlayerSettings.SetScriptingBackend(standalone, backend);
        }

        log.AppendLine(ok ? "done" : "failed");
        File.WriteAllText(LogPath, log.ToString());
        return (ok, log.ToString());
    }
}
