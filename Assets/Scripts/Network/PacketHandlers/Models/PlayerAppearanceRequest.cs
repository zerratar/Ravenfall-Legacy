public class PlayerAppearanceRequest
{
    public PlayerAppearanceRequest(Player player, string appearance)
    {
        Player = player;
        Appearance = appearance;
    }

    public Player Player { get; }
    public string Appearance { get; }
}