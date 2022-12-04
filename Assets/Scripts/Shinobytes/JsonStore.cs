public class JsonStore<T> where T : new()
{
    private readonly string storeFile;
    private readonly string name;
    public JsonStore(string name)
    {
        this.name = name;
        this.storeFile = Shinobytes.IO.Path.GetFilePath(System.IO.Path.GetFileNameWithoutExtension(name) + ".json");
    }

    public static JsonStore<T> Create(string name)
    {
        return new JsonStore<T>(name);
    }

    public T Get()
    {
        if (Shinobytes.IO.File.Exists(storeFile))
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Shinobytes.IO.File.ReadAllText(storeFile));
        }

        return new T();
    }

    public void Save(T obj)
    {
        Shinobytes.IO.File.WriteAllText(storeFile, Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented));
    }
}
