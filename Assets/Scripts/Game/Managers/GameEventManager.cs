public class GameEventManager
{
    private IEvent activeEvent;

    public float RescheduleTime { get; internal set; } = 60;
    public bool IsActive => activeEvent != null;

    internal bool TryStart(IEvent eventManager)
    {
        if (IsActive) return false;
        activeEvent = eventManager;
        return true;
    }

    internal void End(IEvent eventManager)
    {
        if (activeEvent != eventManager)
            return;

        activeEvent = null;
    }
}
