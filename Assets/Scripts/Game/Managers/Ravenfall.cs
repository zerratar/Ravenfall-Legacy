using UnityEngine;

//using UnityEngine.Experimental.Rendering.HDPipeline;
public static class Ravenfall
{
    public static string Version
    {
        get
        {
            var ver = Application.version;
            if (UnityEngine.Debug.isDebugBuild && !UnityEngine.Application.isEditor && ver == "0.1.0")
                return "0.7.8.11a";
            return ver;
        }
    }
}