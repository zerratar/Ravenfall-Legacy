/// <summary>A world event pulls the stream to Ravebridge, unlocked or not (game event 56, RavebridgeForced).</summary>
public class RavebridgeForcedEventHandler : GameEventHandler<RavebridgeForcedData>
{
    public override void Handle(GameManager gameManager, RavebridgeForcedData data)
    {
        if (gameManager.Ravebridge) gameManager.Ravebridge.Begin(true, string.IsNullOrEmpty(data?.Reason) ? null : data.Reason);
    }
}

public class RavebridgeForcedData
{
    public string Reason { get; set; }
}
