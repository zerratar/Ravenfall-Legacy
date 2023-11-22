using System.IO;
using System.Linq;

public class JsonStore<T> where T : new()
{
    private readonly string storeFile;
    private readonly string name;
    private readonly ObjectJsonSerializer<T> serializer;

    public JsonStore(string name, ObjectJsonSerializer<T> serializer = null)
    {
        this.name = name;
        this.serializer = serializer;
        this.storeFile = Shinobytes.IO.Path.GetFilePath(System.IO.Path.GetFileNameWithoutExtension(name) + ".json");
    }

    public static JsonStore<T> Create(string name, ObjectJsonSerializer<T> serializer = null)
    {
        return new JsonStore<T>(name, serializer);
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
        if (serializer != null)
        {
            Shinobytes.IO.File.WriteAllText(storeFile, serializer.Serialize(obj));
        }
        else
        {
            Shinobytes.IO.File.WriteAllText(storeFile, Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented));
        }
    }
}

public abstract class ObjectJsonSerializer<T> where T : new()
{
    public abstract string Serialize(T obj);

    public void Serialize(StringWriter sw, string name, bool? value)
    {
        sw.WriteLine($"  \"{name}\": " + (value.HasValue ? value.ToString().ToLower() : "null") + ",");
    }

    public void Serialize(StringWriter sw, string name, float? value)
    {
        sw.WriteLine($"  \"{name}\": " + (value.HasValue ? value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "null") + ",");
    }

    public void Serialize(StringWriter sw, string name, int? value)
    {
        sw.WriteLine($"  \"{name}\": " + (value.HasValue ? value.Value.ToString() : "null") + ",");
    }

    public void Serialize(StringWriter sw, string name, double? value)
    {
        sw.WriteLine($"  \"{name}\": " + (value.HasValue ? value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "null") + ",");
    }

    public void Serialize(StringWriter sw, string name, string value)
    {
        sw.WriteLine($"  \"{name}\": " + (value != null ? $"\"{EscapeString(value)}\"" : "null") + ",");
    }

    public void Serialize(StringWriter sw, string name, float[] array)
    {
        sw.WriteLine($"  \"{name}\": " + Serialize(array) + ",");
    }

    public string EscapeString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    public string Serialize(float value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public string Serialize(float[] array)
    {
        if (array == null) return "null";
        return "[" + string.Join(",\r\n ", array.Select(Serialize)) + "]";
    }
}
