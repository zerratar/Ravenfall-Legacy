/// <summary>
/// What an auto join came to, per character, as the server says (RavenNest AutoJoinSpending.Result). Each join costs
/// coins; patrons get some free every day (UTC) and a discount on the rest. A player can set their own daily limit
/// (!auto limit, or the website's dashboard); past it paid auto joins stop for the day.
/// </summary>
public static class AutoJoinResult
{
    public const int NotEnoughCoins = 0;
    public const int Paid = 1;
    public const int Free = 2;
    public const int PersonalLimitReached = 3;

    public static bool Joined(int result) => result == Paid || result == Free;
}
