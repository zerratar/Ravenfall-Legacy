public class TwitchSubscription
{
    public string UserId { get; }
    public string ReceiverUserId { get; }
    public string UserName { get; }
    public string DisplayName { get; }
    public int Months { get; }
    public bool IsNew { get; }

    public TwitchSubscription(
        string userId,
        string userName,
        string displayName,
        string receiverUserId,
        int months,
        bool isNew)
    {
        UserId = userId;
        ReceiverUserId = receiverUserId;
        UserName = userName;
        DisplayName = displayName;
        Months = months;
        IsNew = isNew;
    }
}
