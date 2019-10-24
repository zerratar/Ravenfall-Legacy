public class Packet
{
    public Packet(GameClient client, string jsonDataType, string jsonData)
    {
        Client = client;
        JsonDataType = jsonDataType;
        JsonData = jsonData;
    }

    public GameClient Client { get; }
    public string JsonDataType { get; }
    public string JsonData { get; }
}