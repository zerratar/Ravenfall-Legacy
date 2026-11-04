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

    /// <summary>
    /// Chat text without its invisible characters: zero-width spaces and joiners, word joiners, byte order marks and the
    /// tag characters some chat clients add to a repeated message (a Unicode format character, category Cf), and the
    /// blank braille cell and grapheme joiner others use. One stuck to a word made "!quest begin shipwright" find no chain
    /// while the same line typed again worked (the user, 2026-11-04).
    /// </summary>
    public static string WithoutInvisibles(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        System.Text.StringBuilder sb = null;
        for (var i = 0; i < text.Length; ++i)
        {
            var c = text[i];
            var pair = char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]);
            // the tag block whole: its first, U+E0000 (what Chatterino adds), is unassigned rather than a format character
            var tag = pair && char.ConvertToUtf32(c, text[i + 1]) is >= 0xE0000 and <= 0xE007F;
            var invisible = tag || c == '\u2800' || c == '\u034F' || char.GetUnicodeCategory(text, i) == System.Globalization.UnicodeCategory.Format;
            if (invisible)
            {
                sb ??= new System.Text.StringBuilder(text, 0, i, text.Length);
                if (c == '\u2800') sb.Append(' ');
            }
            else sb?.Append(c);
            if (pair)
            {
                ++i;
                if (!invisible) sb?.Append(text[i]);
            }
        }
        return sb == null ? text : sb.ToString().Trim();
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
        if (request is string text) request = (TPacketType)(object)WithoutInvisibles(text);
        Handle(request, packet.Message, packet.Client);
    }

    public abstract void Handle(TPacketType data, GameMessage gm, GameClient client);
}