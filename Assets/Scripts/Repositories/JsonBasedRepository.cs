using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public abstract class JsonBasedRepository<T> : IRepository<T>
{
    protected readonly ConcurrentDictionary<string, T> items
        = new ConcurrentDictionary<string, T>();

    private readonly string repoFolder;

    protected JsonBasedRepository(string repositoryFolder)
    {
        repoFolder = repositoryFolder;
        Load();
    }

    public void Add(string key, T item)
    {
        items[key] = item;
        Save();
    }

    public T Get(string key)
    {
        if (items.TryGetValue(key, out var value))
        {
            return value;
        }

        return default(T);
    }

    public IReadOnlyList<T> All()
    {
        return items.Values.ToList();
    }

    public bool Remove(T item)
    {
        var key = GetKey(item);
        return items.TryRemove(key, out _);
    }

    public T Update(T x)
    {
        var key = GetKey(x);
        return items[key] = x;
    }

    protected abstract string GetKey(T item);

    public async Task SaveAsync()
    {
        if (!System.IO.Directory.Exists(repoFolder))
            System.IO.Directory.CreateDirectory(repoFolder);
        var json = JsonConvert.SerializeObject(items.Values.ToList());
        var repoFile = System.IO.Path.Combine(repoFolder, "repository.json");
        using (var stream = System.IO.File.OpenWrite(repoFile))
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }
    }

    public void Load()
    {
        var repoFile = System.IO.Path.Combine(repoFolder, "repository.json");
        if (!System.IO.File.Exists(repoFile))
        {
            return;
        }

        var readAllText = System.IO.File.ReadAllText(repoFile);
        var values = JsonConvert.DeserializeObject<List<T>>(readAllText);
        values.ForEach(x => Update(x));
    }

    public void Save()
    {
        if (!System.IO.Directory.Exists(repoFolder))
            System.IO.Directory.CreateDirectory(repoFolder);
        var json = JsonConvert.SerializeObject(items.Values.ToList());
        var repoFile = System.IO.Path.Combine(repoFolder, "repository.json");
        System.IO.File.WriteAllText(repoFile, json);
        Debug.Log(repoFile + " saved.");
    }
}