using System;
using UnityEngine;

namespace Obscured
{
    public static class TamperDetector
    {
        public static event Action<string> OnCheatDetected;

        public static void Raise(string source, string detail = null)
        {
            Shinobytes.Debug.LogError($"[TamperDetector] Potential cheat detected from {source}" +
                           (string.IsNullOrEmpty(detail) ? "" : $": {detail}"));

            OnCheatDetected?.Invoke(source);
        }
    }
}
