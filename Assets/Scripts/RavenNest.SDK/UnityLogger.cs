using System;

namespace RavenNest.SDK
{
    public class UnityLogger : ILogger
    {
        public void Debug(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public void Error(string errorMessage)
        {
            UnityEngine.Debug.LogError(errorMessage);
        }

        public void Write(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public void WriteLine(string message)
        {
            UnityEngine.Debug.Log(message);
        }
    }
}