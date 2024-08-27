using System;

public class GameEventManager
{
    public const float EventCooldown = 60f;
    public float LastEventEnded { get; private set; }

    private IEvent activeEvent;

    public float RescheduleTime { get; internal set; } = 60;
    public bool IsActive => activeEvent != null;

    public bool IsEventCooldownActive => GameTime.time - LastEventEnded < EventCooldown;
    public float EventCooldownTimeLeft => EventCooldown - (GameTime.time - LastEventEnded);

    internal bool TryStart(IEvent eventManager)
    {
        if (IsActive) return false;
        if (LastEventEnded > 0 && GameTime.time - LastEventEnded < EventCooldown) return false;
        activeEvent = eventManager;
        return true;
    }

    internal void End(IEvent eventManager)
    {
        if (activeEvent != eventManager)
            return;

        LastEventEnded = GameTime.time;
        // we dont want events to trigger too close to eachother

        activeEvent = null;
    }
}
