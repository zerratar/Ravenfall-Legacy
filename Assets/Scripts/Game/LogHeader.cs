using System;
using UnityEngine;

/// <summary>
/// The first lines the game writes to the log (player.log and ravenfall.log): which version runs, on what, from which
/// folder and since when. Before this the version only showed when a session started, so a log from a game that had
/// lost its connection said nothing about it until hundreds of lines in, and a streamer with two installs could not
/// tell from the log which one was running.
/// </summary>
public static class LogHeader
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void Write()
    {
        try
        {
            var runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            var folder = System.IO.Path.GetDirectoryName(Application.dataPath);
            Debug.Log("==== Ravenfall Legacy " + Ravenfall.Version + " ====" + Environment.NewLine
                + "Started:  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " (UTC " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + ")" + Environment.NewLine
                + "Runtime:  " + runtime + ", Unity " + Application.unityVersion + ", " + Application.platform + Environment.NewLine
                + "OS:       " + SystemInfo.operatingSystem + Environment.NewLine
                + "Folder:   " + folder);
        }
        catch (Exception exc)
        {
            Debug.Log("==== Ravenfall Legacy " + Application.version + " ==== (" + exc.Message + ")");
        }
    }
}
