using UnityEngine;

//using UnityEngine.Experimental.Rendering.HDPipeline;
public static class Ravenfall
{
    private static bool _isBatchMode = false;

    static Ravenfall()
    {
        _isBatchMode = Application.isBatchMode;

//#if UNITY_EDITOR
//        _isBatchMode = true;
//#endif
    }

    public static bool isBatchMode
    {
        get
        {
            return _isBatchMode;
        }
    }


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