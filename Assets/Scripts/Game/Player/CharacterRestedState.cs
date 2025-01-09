public class CharacterRestedState
{
    public double ExpBoost;
    public double RestedPercent;
    public double RestedTime;
    public double AutoRestTime;
    public double CombatStatsBoost;
    internal double? AutoRestTarget;
    internal double? AutoRestStart;
    public const double RestedTimeMax = 2 * 60 * 60; // 2 hours (Seconds)
}
