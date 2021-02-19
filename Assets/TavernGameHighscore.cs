using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TavernGameHighscore
{
    private readonly string gameName;
    private readonly string storageKeyName;
    private List<TavernGameHighscoreItem> records = new List<TavernGameHighscoreItem>();
    private bool loaded;

    public TavernGameHighscore(string gameName)
    {
        this.gameName = gameName;
        this.storageKeyName = "highscore_" + gameName;
    }

    public bool IsLoaded => loaded;

    public void Load()
    {
        if (!loaded)
        {
            loaded = true;
            var highscoreData = PlayerPrefs.GetString(storageKeyName, "[]");
            records = JsonConvert.DeserializeObject<List<TavernGameHighscoreItem>>(highscoreData);
        }
    }

    public void Save()
    {
        var highscoreData = JsonConvert.SerializeObject(records);
        PlayerPrefs.SetString(storageKeyName, highscoreData);
        PlayerPrefs.Save();
    }

    public TavernGameHighscoreItem Add(TavernGameHighscoreItem item)
    {
        records.Add(item);
        Save();
        return item;
    }

    public TavernGameHighscoreItem Get(string userId, string username = null)
    {
        var item = records.FirstOrDefault(x => x.UserId == userId);
        if (item != null)
        {
            // in case player has left in previous games and didnt have a username.
            if (string.IsNullOrEmpty(item.UserName) && !string.IsNullOrEmpty(username))
            {
                item.UserName = username;
                Save();
            }
            return item;
        }

        return Add(new TavernGameHighscoreItem
        {
            UserId = userId,
            UserName = username
        });
    }

    public IReadOnlyList<TavernGameHighscoreItem> GetTop(int count)
    {
        if (records.Count == 0) return records;
        return records.OrderByDescending(x => x.Score).Take(count).ToList();
    }
}
