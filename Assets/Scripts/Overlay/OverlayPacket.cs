public class OverlayPacket
{
    public string Name { get; private set; }
    public string Data { get; private set; }

    private OverlayPacket()
    {
    }

    public static OverlayPacket FromJson(string name, string jsonData)
    {
        return new OverlayPacket
        {
            Name = name,
            Data = jsonData
        };
    }

    public OverlayPacket(object model)
    {
        this.Name = model.GetType().Name;
        if (model != null)
        {
            this.Data = Newtonsoft.Json.JsonConvert.SerializeObject(model);
        }
    }

    public OverlayPacket(string name, object model)
    {
        this.Name = name;
        if (model != null)
        {
            this.Data = Newtonsoft.Json.JsonConvert.SerializeObject(model);
        }
    }

    public T GetValue<T>()
    {
        if (string.IsNullOrEmpty(Data) || Data.Trim() == "{}")
            return default(T);

        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Data);
    }
}
