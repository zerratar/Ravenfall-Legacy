using System;
using System.Threading;
using UnityEngine;

public static class IntegrityCheck
{
    private const float integrityMarginSeconds = 60f;
    private const float integrityMarginSafety = 0.5f;

    private static int tripCount;
    private static long lastIntegrityCheckTicks;
    private static float lastIntegrityCheckTime;
    private static float totalTimeDelta;

    public static bool Enabled = false;

    public static bool IsCompromised
    {
        get
        {
            if (!Enabled) return false;
            if (Application.isEditor) return false;
            return Volatile.Read(ref tripCount) > 0;
        }
        set
        {
            if (value)
            {
                Interlocked.Increment(ref tripCount);
            }
        }
    }

    internal static void Update()
    {
        if (!Enabled) return;
        if (Application.isEditor)
        {
            // ignore it if we run in the editor.
            return;
        }

        if (IsCompromised)
        {
            return;
        }

        var oldTicks = Volatile.Read(ref lastIntegrityCheckTicks);
        var oldTime = Volatile.Read(ref lastIntegrityCheckTime);

        var ticks = DateTime.Now.Ticks;
        var gtime = Time.unscaledTime;

        if (oldTicks > 0 && oldTime > 0)
        {
            var deltaTicks = ticks - oldTicks;
            var deltaTime = gtime - oldTime;
            var ts = TimeSpan.FromTicks(deltaTicks);
            var delta = Volatile.Read(ref totalTimeDelta);
            var addedDelta = (float)Math.Abs(ts.TotalSeconds - deltaTime);
            var newDelta = addedDelta + delta;
            if (newDelta > integrityMarginSafety)
            {
                IsCompromised = newDelta >= integrityMarginSeconds;
                Volatile.Write(ref totalTimeDelta, newDelta);
            }
        }

        Volatile.Write(ref lastIntegrityCheckTicks, ticks);
        Volatile.Write(ref lastIntegrityCheckTime, gtime);
    }
}

