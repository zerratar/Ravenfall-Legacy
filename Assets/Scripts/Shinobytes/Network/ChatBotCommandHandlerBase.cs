using Newtonsoft.Json;

public abstract class ChatBotCommandHandlerBase
{
    protected ChatBotCommandHandlerBase(GameManager game, RavenBotConnection server, PlayerManager playerManager)
    {
        Game = game;
        Server = server;
        PlayerManager = playerManager;
    }

    protected GameManager Game { get; }
    protected RavenBotConnection Server { get; }
    protected PlayerManager PlayerManager { get; }

    public abstract void Handle(BotMessage packet);
    
    public bool TryGetPlayer(GameMessage gm, GameClient client, out PlayerController player)
    {
        player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return false;
        }
        return true;
    }
}

public abstract class ChatBotCommandHandler : ChatBotCommandHandlerBase
{
    protected ChatBotCommandHandler(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(BotMessage packet)
    {
        // ignore content 
        Handle(packet.Message, packet.Client);
    }

    public abstract void Handle(GameMessage gm, GameClient client);
}

public abstract class ChatBotCommandHandler<TPacketType> : ChatBotCommandHandlerBase
{
    protected ChatBotCommandHandler(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(BotMessage packet)
    {
        var request = JsonConvert.DeserializeObject<TPacketType>(packet.Message.Content);
        Handle(request, packet.Message, packet.Client);
    }

    public abstract void Handle(TPacketType data, GameMessage gm, GameClient client);
}