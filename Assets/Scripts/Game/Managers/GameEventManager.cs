using System;
using UnityEngine;

public class GameEventManager
{
    public const float EventCooldown = 60f;
    public float LastEventEnded { get; private set; }

    private IEvent activeEvent;
    private float activeEventStarted;

    public float RescheduleTime { get; internal set; } = 60;
    public bool IsActive => activeEvent != null;

    public bool IsEventCooldownActive => LastEventEnded > 0 && GameTime.time - LastEventEnded < EventCooldown;
    public float EventCooldownTimeLeft => LastEventEnded <= 0 ? 0 : EventCooldown - (GameTime.time - LastEventEnded);

    internal bool TryStart(IEvent @event, bool userInitiated)
    {
        if (activeEvent != null)
        {
            if (activeEvent.IsEventActive)
                return false;

            Shinobytes.Debug.LogWarning("Event " + @event.EventName + " did not end before the new event tried to start.");
        }

        if (!userInitiated && LastEventEnded > 0 && GameTime.time - LastEventEnded < EventCooldown) return false;
        activeEvent = @event;
        activeEventStarted = GameTime.time;
        Shinobytes.Debug.Log("Event started: " + @event.EventName);

        return true;
    }

    internal void End(IEvent eventManager)
    {
        if (activeEvent != eventManager)
            return;

        LastEventEnded = GameTime.time;
        // we dont want events to trigger too close to eachother

        Shinobytes.Debug.Log("Event ended: " + eventManager.EventName);

        activeEvent = null;
    }
}
