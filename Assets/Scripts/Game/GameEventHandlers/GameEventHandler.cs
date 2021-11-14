using Newtonsoft.Json;
using System;

public abstract class GameEventHandler<TEventData> : IGameEventHandler
{
    protected abstract void Handle(GameManager gameManager, TEventData data);
    public void Handle(GameManager gameManager, string data)
    {
        try
        {
            Handle(gameManager, JsonConvert.DeserializeObject<TEventData>(data));
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("Failed to deserialize event data: " + exc.Message + "; " + data);
        }
    }
}