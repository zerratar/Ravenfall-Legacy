/// <summary>
/// !weather: tells the weather, or brings another for the broadcaster, the moderators and the game's admins. A weather
/// brought in comes as a front, as any change does, but over a minute or so instead of its usual few, then carries on
/// changing on its own (<see cref="Weather"/>).
/// </summary>
public class SetWeather : ChatBotCommandHandler<string>
{
    /// <summary>How long a weather asked for takes to come in, seconds; a storm a little longer, to be seen coming.</summary>
    private const float ComeInSeconds = 60f, StormComeInSeconds = 90f;

    public SetWeather(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(string data, GameMessage gm, GameClient client)
    {
        if (!Weather.Changing)
        {
            client.SendReply(gm, Localization.MSG_WEATHER_OFF);
            return;
        }

        var words = (data ?? string.Empty).Trim();
        if (words.Length == 0)
        {
            if (Weather.Coming != Weather.Current) client.SendReply(gm, Localization.MSG_WEATHER_TURNING, Name(Weather.Current), Name(Weather.Coming));
            else client.SendReply(gm, Localization.MSG_WEATHER_NOW, Name(Weather.Current));
            return;
        }

        if (!MayChange(gm))
        {
            client.SendReply(gm, Localization.MSG_WEATHER_ONLY_MODERATORS);
            return;
        }

        if (!TryParse(words, out var kind))
        {
            client.SendReply(gm, Localization.MSG_WEATHER_UNKNOWN, words);
            return;
        }

        Weather.Bring(kind, kind == Weather.Kind.Thunderstorm ? StormComeInSeconds : ComeInSeconds);
        client.SendReply(gm, Localization.MSG_WEATHER_CHANGING, Name(kind));
    }

    private bool MayChange(GameMessage gm)
    {
        var user = gm.Sender;
        if (user.IsModerator || user.IsBroadcaster) return true;
        var player = PlayerManager.GetPlayer(user);
        return player && (player.IsGameAdmin || player.IsGameModerator);
    }

    private static string Name(Weather.Kind kind)
    {
        var names = Localization.MSG_WEATHER_NAMES;
        var i = (int)kind;
        return names != null && i < names.Length ? names[i] : kind.ToString();
    }

    /// <summary>The weather meant, from the words chat uses for it ("sunny", "heavy rain", "thunder").</summary>
    public static bool TryParse(string words, out Weather.Kind kind)
    {
        var w = words.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
        switch (w)
        {
            case "clear": case "clearsky": case "clearskies": case "sun": case "sunny": case "sunshine":
                kind = Weather.Kind.Clear; return true;
            case "fair": case "fine": case "nice":
                kind = Weather.Kind.Fair; return true;
            case "cloud": case "clouds": case "cloudy":
                kind = Weather.Kind.Cloudy; return true;
            case "overcast": case "grey": case "gray": case "dull":
                kind = Weather.Kind.Overcast; return true;
            case "rain": case "rainy": case "raining": case "drizzle": case "shower": case "showers":
                kind = Weather.Kind.Rain; return true;
            case "heavy": case "heavyrain": case "downpour": case "pour": case "pouring": case "torrential":
                kind = Weather.Kind.HeavyRain; return true;
            case "storm": case "stormy": case "storms": case "thunder": case "thunderstorm": case "thunderstorms": case "lightning":
                kind = Weather.Kind.Thunderstorm; return true;
        }
        kind = Weather.Kind.Fair;
        return false;
    }
}
