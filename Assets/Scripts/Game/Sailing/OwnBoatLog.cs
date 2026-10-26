using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Where each player's own boat is at sea, kept in states/own-boats.json so a restart puts them back aboard where they
/// were, still going where they were going. The server only knows a player at sea as one on no island (as on the
/// ferry); the boat's position and course stay on this computer, which is all a restart needs. Written a few seconds
/// apart at most, and when a boat lands or is put away.
/// </summary>
public static class OwnBoatLog
{
    public sealed class Entry
    {
        public float X, Z, Heading;
        /// <summary>The island it sails for; null while wandering.</summary>
        public string Destination;
        public DateTime Saved;
    }

    private const float WriteEvery = 5f;
    private static Dictionary<Guid, Entry> entries;
    private static bool dirty;
    private static float lastWrite;

    static OwnBoatLog()
    {
        Application.quitting += () => Flush(true);
    }

    private static string FilePath => Path.Combine(Shinobytes.IO.Path.GameFolder, "states", "own-boats.json");

    private static Dictionary<Guid, Entry> All()
    {
        if (entries != null) return entries;
        try
        {
            entries = File.Exists(FilePath) ? JsonConvert.DeserializeObject<Dictionary<Guid, Entry>>(File.ReadAllText(FilePath)) : null;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The own boats' positions could not be read: " + exc.Message);
        }
        entries ??= new Dictionary<Guid, Entry>();
        return entries;
    }

    public static bool TryGet(Guid characterId, out Entry entry)
    {
        // older than a day: the player has been elsewhere since, the ferry's restore is better than a stale course
        return All().TryGetValue(characterId, out entry) && (DateTime.UtcNow - entry.Saved).TotalHours < 24;
    }

    public static void Set(Guid characterId, OwnBoat boat)
    {
        if (!boat) return;
        var p = boat.SeaPosition;
        All()[characterId] = new Entry { X = p.x, Z = p.z, Heading = boat.Heading, Destination = boat.Destination, Saved = DateTime.UtcNow };
        dirty = true;
        Flush(false);
    }

    public static void Remove(Guid characterId)
    {
        if (All().Remove(characterId))
        {
            dirty = true;
            Flush(true);
        }
    }

    public static void Flush(bool now)
    {
        if (!dirty || (!now && Time.realtimeSinceStartup - lastWrite < WriteEvery)) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(entries, Formatting.Indented));
            dirty = false;
            lastWrite = Time.realtimeSinceStartup;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The own boats' positions could not be saved: " + exc.Message);
        }
    }
}
