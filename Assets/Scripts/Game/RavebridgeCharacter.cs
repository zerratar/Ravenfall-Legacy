using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RavenNest.Models;
using UnityEngine;

/// <summary>
/// The streamer's Ravebridge character (phase 6d of docs/ravebridge-design.md): made for the island only, it levels up
/// there and never leaves it, and it is never sent to the server. It is a local player (PlayerController.IsLocal, a
/// "#" platform id like the bots', so saves, the session's list and the server's item calls leave it alone), saved in
/// states/ravebridge/character.json with its look, skills, items and island quest progress. It exists in the game
/// only while it is played: spawned on Ravebridge when the streamer picks it, saved and taken away when the event
/// ends. Export writes a dated copy next to the game and puts it on the clipboard; import reads one back from the
/// clipboard or from states/ravebridge/import/. Items come from the island itself: a small drop table per skill.
/// </summary>
public static class RavebridgeCharacter
{
    public const string PlatformPrefix = "#ravebridge-";
    private const int Version = 1;
    private const float SaveEverySeconds = 60f;

    [Serializable]
    public class ItemData
    {
        public Guid ItemId;
        public long Amount;
        public bool Equipped;
    }

    [Serializable]
    public class Data
    {
        public int Version = RavebridgeCharacter.Version;
        public Guid Id;
        public string Name;
        public SyntyAppearance Appearance;
        public RavenNest.Models.Skills Skills;
        public List<ItemData> Items = new List<ItemData>();
        public long Coins;

        /// <summary>The island quests: their step, or -1 when done (RavebridgeQuests).</summary>
        public Dictionary<string, int> Quests = new Dictionary<string, int>();
        public HashSet<string> QuestItems = new HashSet<string>();
        public int QuestPoints;
        public DateTime Created;
        public DateTime Saved;
    }

    private static Data data;
    private static float nextSave;
    private static readonly Dictionary<Skill, double> lastExperience = new Dictionary<Skill, double>();
    private static EnemyController lastEnemy;

    public static PlayerController Spawned { get; private set; }

    private static string Folder => Path.Combine(Shinobytes.IO.Path.GameFolder, "states", "ravebridge");
    private static string FilePath => Path.Combine(Folder, "character.json");
    private static string ImportFolder => Path.Combine(Folder, "import");

    public static Data Current => data ?? Load();
    public static bool Exists => Current != null;

    public static Data Load()
    {
        if (data != null) return data;
        try
        {
            if (!File.Exists(FilePath)) return null;
            data = Parse(File.ReadAllText(FilePath));
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("The Ravebridge character could not be read: " + exc.Message);
        }
        return data;
    }

    private static Data Parse(string json)
    {
        var d = JsonConvert.DeserializeObject<Data>(json);
        if (d == null || string.IsNullOrWhiteSpace(d.Name) || d.Id == Guid.Empty) return null;
        d.Skills ??= NewSkills();
        d.Items ??= new List<ItemData>();
        d.Quests ??= new Dictionary<string, int>();
        d.QuestItems ??= new HashSet<string>();
        return d;
    }

    /// <summary>A new character at level 1 (3 for health, as everyone starts), with the look picked in the creator.</summary>
    public static Data Create(string name, SyntyAppearance look)
    {
        if (data != null && File.Exists(FilePath)) Backup("replaced");
        data = new Data
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(name) ? "Ravebridger" : name.Trim(),
            Appearance = look,
            Skills = NewSkills(),
            Created = DateTime.UtcNow,
        };
        Save();
        return data;
    }

    private static RavenNest.Models.Skills NewSkills()
    {
        var s = new RavenNest.Models.Skills { Id = Guid.NewGuid() };
        foreach (var p in typeof(RavenNest.Models.Skills).GetProperties())
            if (p.PropertyType == typeof(int) && p.Name.EndsWith("Level")) p.SetValue(s, p.Name == "HealthLevel" ? 10 : 1);
        return s;
    }

    /// <summary>Writes the file: the character as it is now if it is in the game. A copy of the last good one is kept.</summary>
    public static void Save()
    {
        if (data == null) return;
        try
        {
            if (Spawned)
            {
                data.Skills = Spawned.Stats;
                data.Appearance = Spawned.Appearance.ToSyntyAppearanceData();
                data.Coins = (long)Spawned.Resources.Coins;
                data.Items = Spawned.Inventory.GetAllItems().Where(i => i?.Item != null).Select(i => new ItemData { ItemId = i.Item.Id, Amount = i.Amount, Equipped = i.InventoryItem != null && i.InventoryItem.Equipped }).ToList();
            }
            data.Saved = DateTime.UtcNow;
            Directory.CreateDirectory(Folder);
            var temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonConvert.SerializeObject(data, Formatting.Indented));
            if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".bak", true);
            File.Copy(temp, FilePath, true);
            File.Delete(temp);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("The Ravebridge character could not be saved: " + exc.Message);
        }
    }

    private static void Backup(string why)
    {
        try { File.Copy(FilePath, Path.Combine(Folder, "character." + why + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json"), true); }
        catch (Exception exc) { Shinobytes.Debug.LogWarning("The Ravebridge character could not be backed up: " + exc.Message); }
    }

    /// <summary>A dated copy next to the game, and on the clipboard. Returns the file's path.</summary>
    public static string Export()
    {
        if (Current == null) return null;
        Save();
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        var safe = new string(data.Name.Where(char.IsLetterOrDigit).ToArray());
        var path = Path.Combine(Shinobytes.IO.Path.GameFolder, "ravebridge-character-" + (safe.Length > 0 ? safe : "export") + "-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".json");
        File.WriteAllText(path, json);
        GUIUtility.systemCopyBuffer = json;
        return path;
    }

    /// <summary>From the clipboard, else the newest file in states/ravebridge/import/. The one it replaces is backed up.</summary>
    public static bool Import()
    {
        Data imported = null;
        try { imported = Parse(GUIUtility.systemCopyBuffer ?? string.Empty); } catch { }
        if (imported == null && Directory.Exists(ImportFolder))
        {
            var newest = new DirectoryInfo(ImportFolder).GetFiles("*.json").OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
            try { if (newest != null) imported = Parse(File.ReadAllText(newest.FullName)); } catch { }
        }
        if (imported == null) return false;
        if (Spawned) Despawn();
        if (File.Exists(FilePath)) Backup("before-import");
        data = imported;
        Save();
        return true;
    }

    // ---- In the game --------------------------------------------------------------------------------------

    /// <summary>Puts the character on Ravebridge (the event must be running).</summary>
    public static PlayerController Spawn()
    {
        var game = GameManager.Instance;
        var rb = RavebridgeManager.Instance;
        if (Current == null || !game || !rb || !rb.Active || !rb.Island) return null;
        if (Spawned) return Spawned;

        var spawn = rb.Island.SpawnPosition;
        var player = new RavenNest.Models.Player
        {
            Id = data.Id,
            UserId = data.Id,
            UserName = data.Name,
            Name = data.Name,
            Identifier = "0",
            Appearance = data.Appearance,
            Skills = data.Skills,
            Resources = new RavenNest.Models.Resources { Coins = data.Coins },
            Statistics = new Statistics(),
            InventoryItems = data.Items.Select(i => new InventoryItem { Id = Guid.NewGuid(), ItemId = i.ItemId, Amount = i.Amount, Equipped = i.Equipped }).ToList(),
            State = new CharacterState { Island = rb.Island.Identifier, Health = data.Skills.HealthLevel, X = spawn.x, Y = spawn.y, Z = spawn.z },
        };
        var user = new User(data.Id, data.Id, data.Name, data.Name, "#E8A33D", "system", PlatformPrefix + data.Id.ToString("N"), false, false, false, false, false, false, "0");
        var controller = game.SpawnPlayer(player, user);
        if (!controller) return null;

        controller.IsLocal = true;
        Spawned = controller;
        lastExperience.Clear();
        lastEnemy = null;
        nextSave = Time.time + SaveEverySeconds;
        rb.Join(controller);
        return controller;
    }

    /// <summary>Saved and taken out of the game: it never leaves the island.</summary>
    public static void Despawn()
    {
        var player = Spawned;
        if (!player) { Spawned = null; return; }
        Save();
        Spawned = null;
        var game = GameManager.Instance;
        if (game) game.RemovePlayer(player, false);
    }

    /// <summary>Every frame from RavebridgeManager: the island's items for what the character does, and saving now and then.</summary>
    public static void Tick()
    {
        var player = Spawned;
        if (!player) return;

        foreach (var skill in DropSkills)
        {
            var stat = player.Stats[skill];
            if (stat == null) continue;
            var exp = stat.Experience;
            if (lastExperience.TryGetValue(skill, out var before) && exp > before) Drop(player, skill);
            lastExperience[skill] = exp;
        }

        // a kill: what the enemy carried
        var enemy = player.taskTarget as EnemyController;
        if (enemy) lastEnemy = enemy;
        if (lastEnemy && lastEnemy.Stats.IsDead)
        {
            Loot(player, lastEnemy);
            lastEnemy = null;
        }

        if (Time.time >= nextSave)
        {
            nextSave = Time.time + SaveEverySeconds;
            Save();
        }
    }

    private static readonly Skill[] DropSkills = { Skill.Woodcutting, Skill.Mining, Skill.Fishing, Skill.Farming, Skill.Gathering };

    private static void Drop(PlayerController player, Skill skill)
    {
        string item = null;
        var roll = UnityEngine.Random.value;
        switch (skill)
        {
            case Skill.Woodcutting: if (roll < 0.35f) item = "Wood Plank"; break;
            case Skill.Mining: if (roll < 0.35f) item = roll < 0.175f ? "Copper Ore" : "Tin Ore"; break;
            case Skill.Fishing: if (roll < 0.35f) item = roll < 0.25f ? "Shrimp" : "Sprat"; break;
            case Skill.Farming: if (roll < 0.5f) item = roll < 0.35f ? "Wheat" : "Cabbage"; break;
            case Skill.Gathering: if (roll < 0.3f) item = "Eggs"; break;
        }
        Give(player, item, 1);
    }

    private static void Loot(PlayerController player, EnemyController enemy)
    {
        var name = enemy.name.ToLowerInvariant();
        if (name.Contains("cow")) { Give(player, "Raw Meat", 1); if (UnityEngine.Random.value < 0.4f) Give(player, "Milk", 1); }
        else if (name.Contains("goblin"))
        {
            player.Resources.Coins += UnityEngine.Random.Range(2, 12);
            if (UnityEngine.Random.value < 0.08f) Give(player, new[] { "Bronze Sword", "Bronze Helmet", "Bronze Boots", "Bronze Gloves", "Bronze Leggings", "Bronze Chest" }[UnityEngine.Random.Range(0, 6)], 1, true);
        }
        else if (name.Contains("ghost")) player.Resources.Coins += UnityEngine.Random.Range(20, 60);
    }

    /// <summary>A catalog item into the character's backpack (by name); gear it can wear goes on.</summary>
    public static void Give(PlayerController player, string itemName, long amount, bool equip = false)
    {
        if (string.IsNullOrEmpty(itemName) || !player) return;
        var game = GameManager.Instance;
        var item = game ? game.Items.GetItems().FirstOrDefault(i => i.Name == itemName) : null;
        if (item == null) return;
        player.Inventory.AddToBackpack(item, amount);
        if (equip) player.EquipBestItems();
    }
}
