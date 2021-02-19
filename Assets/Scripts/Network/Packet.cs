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

public class GamePacket
{
    public GamePacket(string destination, string id, string format, string[] args)
    {
        this.Destination = destination;
        this.Id = id;
        this.Format = format;
        this.Args = args;
    }

    public string Destination { get; }
    public string Id { get; }
    public string Format { get; }
    public string[] Args { get; }
}