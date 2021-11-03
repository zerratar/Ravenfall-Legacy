public class TwitchSubscription
{
    public string UserId { get; }
    public string ReceiverUserId { get; }
    public bool IsModerator { get; }
    public bool IsSubscriber { get; }
    public string UserName { get; }
    public string DisplayName { get; }
    public int Months { get; }
    public bool IsNew { get; }

    public TwitchSubscription(
        string userId,
        string userName,
        string displayName,
        string receiverUserId,
        bool isModerator,
        bool isSubscriber,
        int months,
        bool isNew)
    {
        UserId = userId;
        ReceiverUserId = receiverUserId;
        IsModerator = isModerator;
        IsSubscriber = isSubscriber;
        UserName = userName;
        DisplayName = displayName;
        Months = months;
        IsNew = isNew;
    }
}

public class TwitchHypeTrain
{

}
