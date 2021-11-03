public class PlayerAppearanceRequest
{
    public PlayerAppearanceRequest(TwitchPlayerInfo player, string appearance)
    {
        Player = player;
        Appearance = appearance;
    }

    public TwitchPlayerInfo Player { get; }
    public string Appearance { get; }
}