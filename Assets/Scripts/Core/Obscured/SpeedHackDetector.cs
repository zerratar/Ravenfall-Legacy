// Assets/Obscured/SpeedHackDetector.cs
using System.Diagnostics;
using UnityEngine;

namespace Obscured
{
    /// <summary>
    /// Detects process/game speed manipulation by comparing multiple time sources.
    /// </summary>
    public class SpeedHackDetector : MonoBehaviour
    {
        [Header("Sampling")]
        [Tooltip("How often (seconds) to evaluate drift.")]
        public float sampleInterval = 1.0f;

        [Tooltip("How many consecutive bad samples before triggering.")]
        public int strikesToTrigger = 3;

        [Header("Thresholds")]
        [Tooltip("Allowed ratio drift between Unity realtimeSinceStartup and Stopwatch elapsed. For example 0.05 = ±5%.")]
        [Range(0.0f, 0.5f)] public float ratioTolerance = 0.05f;

        [Tooltip("Allowed sudden delta spike (seconds) vs moving average.")]
        public float spikeToleranceSeconds = 0.050f;

        [Header("Options")]
        [Tooltip("If true, also compare with DateTime.UtcNow to catch broader timer tampering.")]
        public bool useUtcNowCrossCheck = true;

        private Stopwatch sw;
        private double lastUnityRealtime;
        private double lastStopwatch;
        private double lastUtcNow;
        private float timer;
        private int strikes;

        private double avgUnityDelta;
        private double avgStopwatchDelta;
        private const double ALPHA = 0.10; // EMA smoothing

        private void Awake()
        {
            sw = Stopwatch.StartNew();
            lastUnityRealtime = Time.realtimeSinceStartupAsDouble;
            lastStopwatch = sw.Elapsed.TotalSeconds;
            lastUtcNow = System.DateTime.UtcNow.Ticks / 10000000.0; // seconds
            timer = 0f;
            strikes = 0;
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < sampleInterval) return;
            timer = 0f;

            double nowUnity = Time.realtimeSinceStartupAsDouble;
            double nowSw = sw.Elapsed.TotalSeconds;
            double nowUtc = System.DateTime.UtcNow.Ticks / 10000000.0;

            double dUnity = nowUnity - lastUnityRealtime;
            double dSw = nowSw - lastStopwatch;
            double dUtc = nowUtc - lastUtcNow;

            // update last samples
            lastUnityRealtime = nowUnity;
            lastStopwatch = nowSw;
            lastUtcNow = nowUtc;

            // EMA to track "normal"
            avgUnityDelta = avgUnityDelta <= 0 ? dUnity : (ALPHA * dUnity + (1 - ALPHA) * avgUnityDelta);
            avgStopwatchDelta = avgStopwatchDelta <= 0 ? dSw : (ALPHA * dSw + (1 - ALPHA) * avgStopwatchDelta);

            // Ratio check
            double ratio = dUnity > 0.000001 ? dSw / dUnity : 1.0;
            bool ratioBad = System.Math.Abs(1.0 - ratio) > ratioTolerance;

            // Spike check (either clock jumping or timeScale tricks)
            bool spikeBad = System.Math.Abs(dUnity - avgUnityDelta) > spikeToleranceSeconds ||
                            System.Math.Abs(dSw - avgStopwatchDelta) > spikeToleranceSeconds;

            // Optional cross-check vs UTC
            bool utcBad = false;
            if (useUtcNowCrossCheck)
            {
                // If UTC diverges a lot from both Unity and Stopwatch deltas, something fishy
                double avg = 0.5 * (dUnity + dSw);
                utcBad = System.Math.Abs(dUtc - avg) > (spikeToleranceSeconds * 2.0);
            }

            if (ratioBad || spikeBad || utcBad)
            {
                strikes++;
                if (strikes >= strikesToTrigger)
                {
                    TamperDetector.Raise(nameof(SpeedHackDetector),
                        $"ratio={ratio:F3}, dU={dUnity:F4}, dS={dSw:F4}, dUtc={dUtc:F4}, spikes={spikeBad}, utcChk={utcBad}");
                    strikes = 0; // keep reporting periodically
                }
            }
            else
            {
                // decay strikes if all good
                if (strikes > 0) strikes--;
            }
        }
    }
}
