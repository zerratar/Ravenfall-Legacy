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
