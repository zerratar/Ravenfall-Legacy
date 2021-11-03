public class SetScaleRequest
{
    public TwitchPlayerInfo Player { get; }
    public float Scale { get; }

    public SetScaleRequest(TwitchPlayerInfo player, float scale)
    {
        Player = player;
        Scale = scale;
    }
}