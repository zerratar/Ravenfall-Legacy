using Newtonsoft.Json;

public abstract class ChatBotCommandHandler
{
    protected ChatBotCommandHandler(GameManager game, RavenBotConnection server, PlayerManager playerManager)
    {
        Game = game;
        Server = server;
        PlayerManager = playerManager;
    }

    protected GameManager Game { get; }
    protected RavenBotConnection Server { get; }
    protected PlayerManager PlayerManager { get; }

    public abstract void Handle(Packet packet);
}

public abstract class ChatBotCommandHandler<TPacketType> : ChatBotCommandHandler
{
    protected ChatBotCommandHandler(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(Packet packet)
    {
        var request = JsonConvert.DeserializeObject<TPacketType>(packet.JsonData);
        Handle(request, packet.Client);
    }

    public abstract void Handle(TPacketType data, GameClient client);

    protected void UpdateTwitchPlayer(TwitchPlayerInfo data)
    {
        var player = PlayerManager.GetPlayer(data);
    }
}