
public class TwitchCheer
{
    public string UserId { get; }
    public string UserName { get; }
    public string DisplayName { get; }
    public int Bits { get; }

    public TwitchCheer(
        string userId,
        string userName,
        string displayName,
        int bits)
    {
        UserId = userId;
        UserName = userName;
        DisplayName = displayName;
        Bits = bits;
    }
}

public class TwitchSubscription
{
    public string UserId { get; }
    public string UserName { get; }
    public string DisplayName { get; }
    public int Months { get; }
    public bool IsNew { get; }

    public TwitchSubscription(
        string userId, string userName, string displayName, int months, bool isNew)
    {
        UserName = userName;
        UserId = userId;
        DisplayName = displayName;
        Months = months;
        IsNew = isNew;
    }
}