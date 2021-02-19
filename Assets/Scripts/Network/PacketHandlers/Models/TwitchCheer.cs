public class TwitchCheer
{
    public string UserId { get; }
    public string UserName { get; }
    public string DisplayName { get; }
    public bool IsModerator { get; }
    public bool IsSubscriber { get; }
    public bool IsVip { get; }
    public int Bits { get; }

    public TwitchCheer(
        string userId,
        string userName,
        string displayName,
        bool isModerator,
        bool isSubscriber,
        bool isVip,
        int bits)
    {
        UserId = userId;
        UserName = userName;
        IsModerator = isModerator;
        IsSubscriber = isSubscriber;
        IsVip = isVip;
        DisplayName = displayName;
        Bits = bits;
    }
}