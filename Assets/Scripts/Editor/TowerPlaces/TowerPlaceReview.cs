using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
///     The owner's review pass over the tower's places (Ravenfall > Tower Places): a reviewed tick and a note per part,
///     town or place, kept in git in tools/tower/layouts/review.json, one place per line so a diff reads well.
/// </summary>
internal static class TowerPlaceReview
{
    internal const string Folder = "tools/tower/layouts";
    internal const string FilePath = Folder + "/review.json";

    [Serializable]
    internal sealed class Item
    {
        public string key = "";
        public bool reviewed;
        public string note = "";
        public string when = "";
    }

    [Serializable]
    private sealed class File_
    {
        public List<Item> places = new List<Item>();
    }

    private static Dictionary<string, Item> items;

    private static Dictionary<string, Item> Items
    {
        get
        {
            if (items == null) Load();
            return items;
        }
    }

    internal static void Load()
    {
        items = new Dictionary<string, Item>();
        try
        {
            if (!File.Exists(FilePath)) return;
            var file = JsonUtility.FromJson<File_>(File.ReadAllText(FilePath));
            if (file?.places == null) return;
            foreach (var i in file.places) if (!string.IsNullOrEmpty(i.key)) items[i.key] = i;
        }
        catch (Exception e) { Debug.LogWarning("Tower Places: could not read " + FilePath + ": " + e.Message); }
    }

    internal static Item Get(string key) => Items.TryGetValue(key, out var i) ? i : null;

    internal static bool Reviewed(string key) => Get(key)?.reviewed ?? false;

    internal static string Note(string key) => Get(key)?.note ?? "";

    internal static void Set(string key, bool reviewed, string note)
    {
        note = note ?? "";
        if (!Items.TryGetValue(key, out var item))
        {
            if (!reviewed && note.Length == 0) return;
            Items[key] = item = new Item { key = key };
        }
        if (item.reviewed == reviewed && item.note == note) return;
        if (reviewed && !item.reviewed) item.when = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        item.reviewed = reviewed;
        item.note = note;
        if (!reviewed && note.Length == 0) Items.Remove(key);
        Save();
    }

    private static void Save()
    {
        Directory.CreateDirectory(Folder);
        var sb = new StringBuilder();
        sb.Append("{\n  \"places\": [\n");
        // tower order is the window's; here a stable order (parts by number) keeps the diffs small
        var ordered = Items.Values.OrderBy(i => Order(i.key)).ThenBy(i => i.key, StringComparer.Ordinal).ToList();
        for (var n = 0; n < ordered.Count; n++)
        {
            var i = ordered[n];
            sb.Append("    {\"key\": ").Append(TowerPlaceJson.Str(i.key))
              .Append(", \"reviewed\": ").Append(i.reviewed ? "true" : "false")
              .Append(", \"when\": ").Append(TowerPlaceJson.Str(i.when ?? ""))
              .Append(", \"note\": ").Append(TowerPlaceJson.Str(i.note ?? "")).Append('}')
              .Append(n + 1 < ordered.Count ? ",\n" : "\n");
        }
        sb.Append("  ]\n}\n");
        File.WriteAllText(FilePath, sb.ToString());
    }

    private static int Order(string key)
    {
        if (key.StartsWith("part ") && int.TryParse(key.Substring(5), out var step)) return step * 10 + 1;
        if (key.StartsWith("town ")) return 0;
        return 1000000;
    }
}

/// <summary>Small helpers for the hand-written, diff-friendly JSON of the tower place files.</summary>
internal static class TowerPlaceJson
{
    internal static string Str(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (var c in s ?? "")
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 32) sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }
        return sb.Append('"').ToString();
    }

    internal static string Num(float f)
    {
        if (Mathf.Abs(f) < 0.00005f) f = 0f;
        return f.ToString("0.####", CultureInfo.InvariantCulture);
    }

    internal static string Vec(Vector3 v) => "[" + Num(v.x) + ", " + Num(v.y) + ", " + Num(v.z) + "]";

    internal static Vector3 ToVec(float[] a, Vector3 fallback) => a != null && a.Length >= 3 ? new Vector3(a[0], a[1], a[2]) : fallback;
}
