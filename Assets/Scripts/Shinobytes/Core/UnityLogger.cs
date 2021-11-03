using System;

namespace RavenNest.SDK
{
    public class UnityLogger : ILogger
    {
        public void Debug(string message)
        {
            UnityEngine.Debug.Log($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}");
        }

        public void Error(string message)
        {
            UnityEngine.Debug.LogError($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}");
        }

        public void Write(string message)
        {
            UnityEngine.Debug.Log($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}");
        }

        public void WriteLine(string message)
        {
            UnityEngine.Debug.Log($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}");
        }
    }
}