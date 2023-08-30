using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;

public static class GameSystems
{
    public static int frameCount;
    public static float time;
    public static void Awake()
    {
        ActionSystem.Init();
    }

    public static void Start()
    {
    }

    public static void Update()
    {
        frameCount = UnityEngine.Time.frameCount;
        time = UnityEngine.Time.time;
        ActionSystem.Update();
    }
}

public static class ActionSystem
{
    private static Queue<System.Func<bool>> actions = new Queue<System.Func<bool>>();
    public static void Init()
    {
        actions = new Queue<System.Func<bool>>();
    }

    public static void Update()
    {
        for (var i = 0; i < actions.Count; ++i)
        {
            if (actions.TryDequeue(out var action))
            {
                if (!action()) actions.Enqueue(action);
                continue;
            }
            //break;
        }

    }

    public static void Run(System.Func<bool> action)
    {
        actions.Enqueue(action);
    }
}

public class ScheduledAction
{
    private readonly Func<Task> asyncAction;
    private readonly Action onInterrupt;
    private readonly DateTime executeTime;
    private bool interrupted;
    private bool invoked;

    public ScheduledAction(Func<Task> asyncAction, Action onInterrupt, double duration)
    {
        this.executeTime = DateTime.UtcNow.AddSeconds(duration);
        this.asyncAction = asyncAction;
        this.onInterrupt = onInterrupt;
    }
    public bool Invoked => invoked;
    public bool Interrupted => interrupted;

    public bool CanInvoke()
    {
        return !invoked && !interrupted && DateTime.UtcNow >= executeTime;
    }

    public async Task InvokeAsync()
    {
        try
        {
            if (!CanInvoke())
            {
                return;
            }

            invoked = true;

            await asyncAction();
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Invoke Scheduled Action Failed: " + exc);
        }
    }

    public void Interrupt()
    {
        if (interrupted)
        {
            return;
        }

        try
        {
            interrupted = true;

            if (onInterrupt != null)
            {
                onInterrupt();
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Interrupt Scheduled Action Failed: " + exc);
        }
    }
}
//public class PlayerStatistics
//{
//    public Statistics this[int index] => new Statistics();
//}