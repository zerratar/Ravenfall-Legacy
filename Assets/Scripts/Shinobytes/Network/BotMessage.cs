using System;

public class BotMessage
{
    public BotMessage(GameClient client, GameMessage message)
    {
        Client = client;
        Message = message;
    }

    public GameClient Client { get; }
    public GameMessage Message { get; }
}

public class GameMessage
{
    public string Identifier { get; set; }
    public string Content { get; set; }
    public string CorrelationId { get; set; }
    public User Sender { get; set; }
}

public class GameMessageResponse
{
    public GameMessageResponse(
        string identifier,
        GameMessageRecipent recipent,
        string format,
        object[] args,
        string[] tags,
        string category,
        string correlationId)
    {
        this.Identifier = identifier;
        this.Format = format;
        this.Recipent = recipent;
        this.Args = args;
        this.Tags = tags;
        this.Category = category;
        this.CorrelationId = correlationId;
    }

    public static GameMessageResponse CreateReply(
        string identifier,
        GameMessageRecipent recipent,
        string format,
        object[] args,
        string correlationId)
    {
        return new GameMessageResponse(
            identifier, recipent, format,
            args, new string[0], string.Empty, correlationId);
    }

    public static GameMessageResponse CreateArgs(
        string identifier,
        params object[] args)
    {
        return new GameMessageResponse(identifier, GameMessageRecipent.System, "", args, new string[0], "", "");
    }

    public static GameMessageResponse CreateEmptyReply(
        string identifier,
        string correlationId)
    {
        return new GameMessageResponse(identifier, GameMessageRecipent.System, "", new string[0], new string[0], "", correlationId);
    }


    public string Identifier { get; }
    public GameMessageRecipent Recipent { get; }
    public string Format { get; }
    public object[] Args { get; }
    public string[] Tags { get; }
    public string Category { get; }
    public string CorrelationId { get; }
}

public class GameMessageRecipent
{
    public static GameMessageRecipent System { get; }
        = new GameMessageRecipent(Guid.Empty, "system", string.Empty, string.Empty);

    public GameMessageRecipent(Guid userId, string platform, string platformId, string platformUserName)
    {
        UserId = userId;
        Platform = platform;
        PlatformId = platformId;
        PlatformUserName = platformUserName;
    }

    public static GameMessageRecipent Create(User user)
    {
        return new GameMessageRecipent(user.Id, user.Platform, user.PlatformId, user.Username);
    }

    public Guid UserId { get; }
    public string Platform { get; }
    public string PlatformId { get; }
    public string PlatformUserName { get; }

}