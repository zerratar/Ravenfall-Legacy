using System.Collections.Generic;
using System;
using System.Threading.Tasks;

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

public abstract class ScheduledAction
{
    public ScheduledAction(object state, double duration, string description, object tag)
    {
        this.State = state;
        this.ExecuteTime = DateTime.UtcNow.AddSeconds(duration);
        this.Description = description;
        this.Tag = tag;
    }

    public object State { get; }

    public string Description { get; }
    public object Tag { get; }

    public bool Invoked { get; protected set; }
    public bool Interrupted { get; protected set; }
    public DateTime ExecuteTime { get; protected set; }

    public bool CanInvoke()
    {
        return !Invoked && !Interrupted && DateTime.UtcNow >= ExecuteTime;
    }

    public abstract Task InvokeAsync();
    public abstract void Interrupt();
}

public class ScheduledAction<TState> : ScheduledAction
    where TState : class
{
    private readonly Func<TState, Task> asyncAction;
    private readonly Action<TState> onInterrupt;

    public ScheduledAction(TState state, Func<TState, Task> asyncAction, Action<TState> onInterrupt, double duration, string description, object tag)
        : base(state, duration, description, tag)
    {
        this.asyncAction = asyncAction;
        this.onInterrupt = onInterrupt;
    }

    public override async Task InvokeAsync()
    {
        try
        {
            if (Interrupted || Invoked)
            {
                return;
            }

            Invoked = true;

            await asyncAction(State as TState);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Invoke Scheduled Action Failed: " + exc);
        }
    }

    public override void Interrupt()
    {
        if (Interrupted)
        {
            return;
        }

        try
        {
            Interrupted = true;

            if (onInterrupt != null)
            {
                onInterrupt(State as TState);
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