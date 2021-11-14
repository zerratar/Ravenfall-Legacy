using System;

namespace RavenNest.SDK
{
    public class UnityLogger : ILogger
    {
        public void Debug(string message)
        {
            Shinobytes.Debug.Log(message);
        }

        public void Error(string message)
        {
            Shinobytes.Debug.LogError(message);
        }

        public void Write(string message)
        {
            Shinobytes.Debug.Log(message);
        }

        public void WriteLine(string message)
        {
            Shinobytes.Debug.Log(message);
        }
    }
}

namespace Shinobytes
{
    public static class Debug
    {
        public static void Log(string message)
        {
#if DEBUG            
            UnityEngine.Debug.Log("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message);
#endif
        }
        public static void Log(object message)
        {
#if DEBUG            
            UnityEngine.Debug.Log("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message);
#endif
        }
        public static void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message);
        }
        public static void LogError(string message)
        {
            UnityEngine.Debug.LogError("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message);
        }
        public static void LogError(Exception message)
        {
            UnityEngine.Debug.LogError("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message.Message);
        }
    }
}
