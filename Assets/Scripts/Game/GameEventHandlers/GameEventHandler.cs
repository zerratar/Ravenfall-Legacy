using Newtonsoft.Json;

public abstract class GameEventHandler<TEventData> : IGameEventHandler
{
    protected abstract void Handle(GameManager gameManager, TEventData data);
    public void Handle(GameManager gameManager, string data)
    {
        Handle(gameManager, JsonConvert.DeserializeObject<TEventData>(data));
    }
}