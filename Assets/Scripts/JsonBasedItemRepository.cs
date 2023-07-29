using RavenNest.Models;
using System.Collections.Generic;

public class JsonBasedItemRepository
{
    private readonly string file;
    private readonly object mutex = new object();
    private List<Item> items;

    public static JsonBasedItemRepository Instance { get; private set; }
    public JsonBasedItemRepository(string file = @"C:\git\Ravenfall Legacy\Data\Repositories\items.json")
    {
        this.file = file;
        Instance = this;
    }

    public List<Item> GetItems()
    {
        lock (mutex)
        {
            if (items == null)
            {
                Load();
            }

            return items;
        }
    }
    public void Load()
    {
        lock (mutex)
        {
            if (!System.IO.File.Exists(file))
            {
                this.items = new List<Item>();
                return;
            }

            var content = System.IO.File.ReadAllText(file);
            this.items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Item>>(content);
        }
    }
}
