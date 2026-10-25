/// <summary>
/// A world event pulls the stream to Ravebridge, unlocked or not (game event 56, RavebridgeForced). A storm also puts
/// the streamer's character straight into hero mode and a wave run (HeroStart).
/// </summary>
public class RavebridgeForcedEventHandler : GameEventHandler<RavebridgeForcedData>
{
    public override void Handle(GameManager gameManager, RavebridgeForcedData data)
    {
        var reason = string.IsNullOrEmpty(data?.Reason) ? null : data.Reason;
        if (data != null && data.Storm) HeroStart.Begin(gameManager, true, reason ?? Localization.MSG_HERO_STORM);
        else if (gameManager.Ravebridge) gameManager.Ravebridge.Begin(true, reason);
    }
}

public class RavebridgeForcedData
{
    public string Reason { get; set; }
    public bool Storm { get; set; }
}
