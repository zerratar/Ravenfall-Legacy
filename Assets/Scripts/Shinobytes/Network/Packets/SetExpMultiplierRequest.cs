public class SetExpMultiplierRequest
{
    public SetExpMultiplierRequest(TwitchPlayerInfo player, int expMultiplier)
    {
        this.Player = player;
        this.ExpMultiplier = expMultiplier;
    }

    public int ExpMultiplier { get; }
    public TwitchPlayerInfo Player { get; }
}
