using System;
using System.Linq;

public class TwitchPlayerInfo
{
    public TwitchPlayerInfo(
        string userId,
        string username,
        string displayName,
        string color,
        bool isBroadcaster,
        bool isModerator,
        bool isSubscriber,
        bool isVip,
        string identifier)
    {
        if (string.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username));
        Username = username.StartsWith("@") ? username.Substring(1) : username;
        UserId = userId;
        DisplayName = displayName;
        Color = color;
        IsBroadcaster = isBroadcaster;
        IsModerator = isModerator;
        IsSubscriber = isSubscriber;
        IsVip = isVip;
        Identifier = identifier;

        if (Identifier != null && Identifier.Length > 0)
        {
            var allowedCharacters = "_=qwertyuiopåasdfghjklöäzxcvbnm1234567890".ToArray();
            Identifier = string.Join("", Identifier.ToArray().Where(x => allowedCharacters.Contains(Char.ToLower(x))));
        }
        //if (Identifier[0] == '󠀀')
        //{
        //}

    }

    public string Username { get; }
    public string UserId { get; }
    public string DisplayName { get; }
    public string Color { get; }
    public bool IsBroadcaster { get; set; }
    public bool IsModerator { get; set; }
    public bool IsSubscriber { get; set; }
    public bool IsVip { get; set; }
    public string Identifier { get; }
}