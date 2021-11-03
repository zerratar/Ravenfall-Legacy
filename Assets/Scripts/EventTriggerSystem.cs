using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public class EventTriggerSystem : IDisposable
{
    private readonly Thread sysThread;
    private readonly ConcurrentDictionary<string, SysEventStats> sourceStatistics = new ConcurrentDictionary<string, SysEventStats>();
    private readonly ConcurrentQueue<SysEventInput> activeInputs = new ConcurrentQueue<SysEventInput>();
    private readonly object triggerMutex = new object();
    private bool disposed;

    private List<SysEventTrigger> activeEvents = new List<SysEventTrigger>();

    public event EventHandler<SysEventStats> SourceTripped;

    public EventTriggerSystem()
    {
        sysThread = new System.Threading.Thread(Process);
        sysThread.Start();
    }

    public void TriggerEvent(string eventKey, TimeSpan lifeSpan, params string[] triggers)
    {
        lock (triggerMutex)
        {
            if (triggers == null || triggers.Length == 0)
                triggers = new string[] { eventKey };

            activeEvents.Add(new SysEventTrigger(eventKey, lifeSpan, triggers));
        }
    }

    public void SendInput(string source, string input)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(input))
        {
            return;
        }

        if (source.StartsWith("#"))
        {
            return;
        }

        lock (triggerMutex)
        {
            activeInputs.Enqueue(new SysEventInput(source, input));
        }
    }

    private void Process(object obj)
    {
        while (!disposed)
        {
            var newTriggers = false;
            lock (triggerMutex)
            {
                while (activeInputs.TryDequeue(out var input))
                {
                    if (input == null || string.IsNullOrEmpty(input.Source))
                    {
                        continue;
                    }

                    var alreadyTriggered = false;
                    var triggered = false;
                    foreach (var activeEvent in activeEvents)
                    {
                        if (!activeEvent.Triggered.Add(input.Source))
                        {
                            alreadyTriggered = true;
                            continue;
                        }
                        if (!activeEvent.IsAlive)
                            continue;

                        if (activeEvent.Created > input.Sent)
                            continue; // input has to be after event

                        if (CheckTrigger(activeEvent, input))
                        {
                            var triggerDelay = input.Sent - activeEvent.Created;
                            newTriggers = true;
                            triggered = true;
                            if (!sourceStatistics.TryGetValue(input.Source, out var stats))
                                sourceStatistics[input.Source] = stats = new SysEventStats(input.Source);
                            stats.TriggerCount.TryGetValue(input.Input, out var tc);
                            stats.TriggerCount[input.Input] = tc + 1;
                            stats.InputCount.TryGetValue(input.Input, out var ic);
                            stats.InputCount[input.Input] = ic + 1;
                            ++stats.TriggerStreak;
                            stats.HighestTriggerStreak = Math.Max(stats.TriggerStreak, stats.HighestTriggerStreak);
                            if (stats.FirstTrigger == DateTime.MinValue)
                                stats.FirstTrigger = DateTime.UtcNow;
                            if (stats.TriggerStreakStart == DateTime.MinValue)
                                stats.TriggerStreakStart = DateTime.UtcNow;
                            stats.TriggerTime.TryGetValue(input.Input, out var tt);
                            stats.TriggerTime[input.Input] = tt + triggerDelay;
                            stats.TotalTriggerTime += triggerDelay;
                            stats.LastTriggerDelay = triggerDelay;
                            stats.LastTrigger = DateTime.UtcNow;
                            stats.TriggerRangeMin.TryGetValue(input.Input, out var trmin);
                            stats.TriggerRangeMin[input.Input] = trmin > 0 ? Math.Min(trmin, triggerDelay.TotalSeconds) : triggerDelay.TotalSeconds;
                            stats.TriggerRangeMax.TryGetValue(input.Input, out var trmax);
                            stats.TriggerRangeMax[input.Input] = Math.Max(trmax, triggerDelay.TotalSeconds);
                        }
                    }

                    if (!triggered)
                    {
                        if (!sourceStatistics.TryGetValue(input.Source, out var stats))
                            sourceStatistics[input.Source] = stats = new SysEventStats(input.Source);
                        stats.InputCount.TryGetValue(input.Input, out var count);
                        stats.InputCount[input.Input] = count + 1;
                    }

                    var triggerStreak = false;
                    foreach (var deadEvent in activeEvents.Where(x => !x.IsAlive))
                        triggerStreak = triggerStreak || CheckTrigger(deadEvent, input);

                    if (!triggerStreak && !triggered && !alreadyTriggered)
                    {
                        if (!sourceStatistics.TryGetValue(input.Source, out var stats))
                            sourceStatistics[input.Source] = stats = new SysEventStats(input.Source);
                        stats.HighestTriggerStreak = Math.Max(stats.TriggerStreak, stats.HighestTriggerStreak);
                        stats.TriggerStreakBreak = DateTime.UtcNow;
                        ++stats.TriggerStreakBreakCount;
                        stats.TriggerStreak = 0;
                        stats.TriggerStreakStart = DateTime.MinValue;
                    }
                }
                activeEvents.RemoveAll(x => !x.IsAlive);
            }

            if (newTriggers)
                ReinvalidateAndInspectSuspects();

            System.Threading.Thread.Sleep(10);
        }
    }

    private void ReinvalidateAndInspectSuspects()
    {
        foreach (var s in sourceStatistics)
        {
            var stats = s.Value;
            if (Inspect(stats))
            {
                ++stats.InspectCount;
                SourceTripped?.Invoke(this, stats);
            }
        }
    }

    private bool Inspect(SysEventStats stats)
    {
        var playduration = stats.LastTrigger - stats.FirstTrigger;
        var avgSecPerTrigger = stats.TotalTriggerTime.TotalSeconds / stats.TotalTriggerCount;

        if (avgSecPerTrigger <= 0.2)
            return true;

        if (stats.TriggerStreak >= 20)
            return true;

        if (stats.HighestTriggerStreak >= 3)
        {
            //var avgTriggerTimes = stats.GetAverageTriggerTimes();
            if (avgSecPerTrigger <= 0.45 && stats.TriggerStreak == stats.HighestTriggerStreak)
                return true;
            if (avgSecPerTrigger <= 0.6 && stats.TriggerStreak >= 12 && stats.TriggerStreak == stats.HighestTriggerStreak)
                return true;
            if (avgSecPerTrigger <= 0.7 && stats.TriggerStreak >= 16 && stats.TriggerStreak == stats.HighestTriggerStreak)
                return true;

            var streakDuration = stats.LastTrigger - stats.TriggerStreakStart;
            if (avgSecPerTrigger <= 3.0 && stats.TriggerStreak >= 24 && streakDuration >= TimeSpan.FromHours(8))
                return true;
            if (avgSecPerTrigger <= 2.0 && stats.TriggerStreak >= 12 && streakDuration >= TimeSpan.FromHours(4))
                return true;
            if (avgSecPerTrigger <= 1.0 && stats.TriggerStreak >= 06 && streakDuration >= TimeSpan.FromHours(2))
                return true;
        }

        return false;
    }

    private bool CheckTrigger(SysEventTrigger activeEvent, SysEventInput input)
    {
        return activeEvent.Triggers.Any(x => x.StartsWith(input.Input.Trim(), StringComparison.OrdinalIgnoreCase) || input.Input.Trim().StartsWith(x, StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        sysThread.Join();
    }

    internal bool IsFlagged(string source)
    {
        return this.sourceStatistics.TryGetValue(source, out var stat) && stat.Flagged;
    }

    public class SysEventStats
    {
        public string Source { get; }
        public ConcurrentDictionary<string, long> InputCount { get; } = new ConcurrentDictionary<string, long>();
        public ConcurrentDictionary<string, long> TriggerCount { get; } = new ConcurrentDictionary<string, long>();
        public ConcurrentDictionary<string, TimeSpan> TriggerTime { get; } = new ConcurrentDictionary<string, TimeSpan>();
        public ConcurrentDictionary<string, double> TriggerRangeMin { get; } = new ConcurrentDictionary<string, double>();
        public ConcurrentDictionary<string, double> TriggerRangeMax { get; } = new ConcurrentDictionary<string, double>();
        public long TotalTriggerCount => TriggerCount.Sum(x => x.Value);
        public long TriggerStreak { get; set; }
        public long HighestTriggerStreak { get; set; }
        public long TriggerStreakBreakCount { get; set; }
        public DateTime LastTrigger { get; set; }
        public DateTime FirstTrigger { get; set; }
        public TimeSpan TotalTriggerTime { get; set; }
        public TimeSpan LastTriggerDelay { get; set; }
        public DateTime TriggerStreakStart { get; set; }
        public DateTime TriggerStreakBreak { get; set; }
        public bool Flagged => InspectCount > 0;
        public long InspectCount { get; set; }
        public SysEventStats(string source)
        {
            Source = source;
        }

        public Dictionary<string, TimeSpan> GetAverageTriggerTimes()
        {
            var output = new Dictionary<string, TimeSpan>();
            foreach (var tk in TriggerCount.Keys)
                output[tk] = TimeSpan.FromSeconds(TriggerTime[tk].TotalSeconds / TriggerCount[tk]);
            return output;
        }
    }

    public class SysEventInput
    {
        public string Source { get; }
        public string Input { get; }
        public DateTime Sent { get; }
        public SysEventInput(string source, string input)
        {
            Source = source;
            Input = input;
            Sent = DateTime.UtcNow;
        }
    }

    public class SysEventTrigger
    {
        public string Key { get; }
        public TimeSpan LifeSpan { get; }
        public string[] Triggers { get; }
        public HashSet<string> Triggered { get; }
        public DateTime Created { get; }
        public bool IsAlive => DateTime.UtcNow < Created + LifeSpan;
        public SysEventTrigger(string key, TimeSpan lifeSpan, string[] triggers)
        {
            Key = key;
            LifeSpan = lifeSpan;
            Triggers = triggers;
            Triggered = new HashSet<string>();
            Created = DateTime.UtcNow;
        }
    }
}
