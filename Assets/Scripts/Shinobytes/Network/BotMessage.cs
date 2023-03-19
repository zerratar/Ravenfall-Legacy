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

public static class MessageUtilities
{
    private const string TagsStart = "%[tags:";
    private const string TagsEnd = "]%";
    private const char TagsSeparator = ';';

    private const string CategoryStart = "%[category:";
    private const string CategoryEnd = "]%";

    public static string Meta(string category, params string[] tags)
    {
        return Category(category) + Tags(tags);
    }

    public static string Tags(params string[] tags)
    {
        if (tags == null || tags.Length == 0) return string.Empty;
        return TagsStart + string.Join(TagsSeparator, tags) + TagsEnd;
    }

    public static string Category(string category)
    {
        return CategoryStart + category + CategoryEnd;
    }

    public static string TryExtractTags(string format, out string[] tags)
    {
        tags = new string[0];
        if (string.IsNullOrEmpty(format))
            return string.Empty;

        var start = format.IndexOf(TagsStart);
        // format MUST start with this.
        // or it will be ignored.
        if (start != 0) return format;

        var tagsString = format.Split(TagsEnd)[0].Substring(TagsStart.Length);

        tags = tagsString.Split(TagsSeparator);

        return format.Replace(TagsStart + tagsString + TagsEnd, "");
    }

    public static string TryExtractCategory(string format, out string category)
    {
        category = string.Empty;

        if (string.IsNullOrEmpty(format))
            return string.Empty;

        var start = format.IndexOf(CategoryStart);
        // format MUST start with this.
        // or it will be ignored.
        if (start != 0) return format;

        category = format.Split(CategoryEnd)[0].Substring(CategoryStart.Length);

        return format.Replace(CategoryStart + category + CategoryEnd, "");
    }
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
        if (!string.IsNullOrEmpty(format) && format.StartsWith("%["))
        {
            format = MessageUtilities.TryExtractCategory(format, out category);
            format = MessageUtilities.TryExtractTags(format, out tags);
        }

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
        // to make sure that messages dont get contaminated with user input and tricks the system that certain messages
        // comes in certain categories or tags, they must be at the start of the message. Category must always come first
        // and be trimmed out when parsed. Afterwards tags can be read which also expects to be at index 0.
        format = MessageUtilities.TryExtractCategory(format, out var category);
        format = MessageUtilities.TryExtractTags(format, out var tags);
        return new GameMessageResponse(
            identifier, recipent, format,
            args, tags, category, correlationId);
    }

    public static GameMessageResponse CreateArgs(
        string identifier,
        params object[] args)
    {
        return new GameMessageResponse(
            identifier, 
            GameMessageRecipent.System, "", args, new string[0], "", "");
    }

    public static GameMessageResponse CreateEmptyReply(
        string identifier,
        string correlationId)
    {
        return new GameMessageResponse(identifier, GameMessageRecipent.System, "", new string[0], new string[0], "", correlationId);
    }


    public string Identifier { get; }
    public GameMessageRecipent Recipent { get; }
    public string Format { get; set; }
    public object[] Args { get; set; }
    public string[] Tags { get; set; }
    public string Category { get; set; }
    public string CorrelationId { get; }
}

public class GameMessageRecipent
{
    public static GameMessageRecipent System { get; }
        = new GameMessageRecipent(Guid.Empty, Guid.Empty, "system", string.Empty, string.Empty);

    public GameMessageRecipent(Guid userId, Guid characterId, string platform, string platformId, string platformUserName)
    {
        UserId = userId;
        CharacterId = characterId;
        Platform = platform;
        PlatformId = platformId;
        PlatformUserName = platformUserName;
    }

    public static GameMessageRecipent Create(PlayerController user)
    {
        return new GameMessageRecipent(user.UserId, user.Id, user.Platform, user.PlatformId, user.Name);
    }

    public static GameMessageRecipent Create(User user)
    {
        return new GameMessageRecipent(user.Id, user.CharacterId, user.Platform, user.PlatformId, user.Username);
    }

    public Guid UserId { get; set; }
    public Guid CharacterId { get; set; }
    public string Platform { get; set; }
    public string PlatformId { get; set; }
    public string PlatformUserName { get; set; }

}