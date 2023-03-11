public class PlayerAppearanceRequest
{
    public PlayerAppearanceRequest(User player, string appearance)
    {
        Player = player;
        Appearance = appearance;
    }

    public User Player { get; }
    public string Appearance { get; }
}
