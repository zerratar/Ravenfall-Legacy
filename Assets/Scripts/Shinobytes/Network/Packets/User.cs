using System;
using System.Collections.Generic;

public class User
{
    private static readonly HashSet<char> allowedCharacters = new HashSet<char>(new[] { '_', '=', 'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p', 'å', 'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'ö', 'ä', 'z', 'x', 'c', 'v', 'b', 'n', 'm', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' });
    public User() { }

    public User(
        Guid id,
        Guid characterId,
        string username,
        string displayName,
        string color,
        string platform,
        string platformId,
        bool isBroadcaster,
        bool isModerator,
        bool isSubscriber,
        bool isVip,
        string identifier)
    {
        if (string.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username));

        Id = id;
        CharacterId = characterId;
        Username = username.StartsWith("@") ? username.Substring(1) : username;
        PlatformId = platformId;
        Platform = platform;
        DisplayName = displayName;
        Color = color;
        IsBroadcaster = isBroadcaster;
        IsModerator = isModerator;
        IsSubscriber = isSubscriber;
        IsVip = isVip;
        Identifier = identifier;

        if (Identifier != null && Identifier.Length > 0)
        {
            //var allowedCharacters = "_=qwertyuiopåasdfghjklöäzxcvbnm1234567890".ToArray();
            //Identifier = string.Join("", Identifier.ToArray().Where(x => allowedCharacters.Contains(Char.ToLower(x))));

            if (Identifier != null && Identifier.Length > 0)
            {
                var newIdentifier = "";
                for (var i = 0; i < Identifier.Length; i++)
                {
                    var c = Identifier[i];
                    if (allowedCharacters.Contains(char.ToLower(c)))
                    {
                        newIdentifier += c;
                    }
                }

                Identifier = newIdentifier;
            }
        }
        //if (Identifier[0] == '󠀀')
        //{
        //}
    }

    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public string Platform { get; set; }
    public string PlatformId { get; set; }
    public string Username { get; set; }
    public string DisplayName { get; set; }
    public string Color { get; set; }
    public bool IsBroadcaster { get; set; }
    public bool IsModerator { get; set; }
    public bool IsSubscriber { get; set; }
    public bool IsVip { get; set; }
    public int SubTier { get; set; }
    public string Identifier { get; set; }

}