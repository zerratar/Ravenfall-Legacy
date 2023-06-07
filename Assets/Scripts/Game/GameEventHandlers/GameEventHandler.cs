using MessagePack;
using Newtonsoft.Json;
using System;

public abstract class GameEventHandler<TEventData> : IGameEventHandler<TEventData>
{
    public abstract void Handle(GameManager gameManager, TEventData data);
    public void Handle(GameManager gameManager, byte[] data)
    {
        try
        {
            //Handle(gameManager, JsonConvert.DeserializeObject<TEventData>(data));
            var packet = MessagePackSerializer.Deserialize<TEventData>(data, MessagePack.Resolvers.ContractlessStandardResolver.Options);
            Handle(gameManager, packet);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("Failed to deserialize event data: " + exc.Message + "; " + data);
        }
    }
}