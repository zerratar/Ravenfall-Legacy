using System.Collections.Generic;
using System;
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
            break;
        }
    }

    public static void Run(System.Func<bool> action)
    {
        actions.Enqueue(action);
    }
}

//public class PlayerStatistics
//{
//    public Statistics this[int index] => new Statistics();
//}