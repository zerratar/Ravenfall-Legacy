public interface IBotRequest
{
}
public interface IBotRequest<TValueType> : IBotRequest
{
    TwitchPlayerInfo Player { get; }
    TValueType Value { get; }
}
