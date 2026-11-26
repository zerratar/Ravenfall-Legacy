using System.Collections.Generic;
using System.IO;
using System.Linq;
using RavenNest.Combat;

/// <summary>
///     Every place of the tower in climbing order, for Ravenfall > Tower Places: each floor's town, then its parts, read
///     from RavenNest.Combat.Tower, so new floors and parts show up by themselves. Tower scenes no part or town uses yet
///     (the great hall) come last.
/// </summary>
internal static class TowerPlaceCatalog
{
    internal sealed class Entry
    {
        /// <summary>The review file's key: "part 5", "town mossgate", "place Tower_GreatHall".</summary>
        public string Key;
        public string Title;
        public string Detail;
        public string Group;
        public string Layout;
        public string ScenePath;
        public int Step;
        public string TownKey;
        public bool IsTown => TownKey != null;
        public bool IsPart => Step > 0;
    }

    internal const string SceneFolder = "Assets/Scenes/Tower";

    internal static string ScenePath(string layout) => SceneFolder + "/" + layout + ".unity";

    internal static List<Entry> Build()
    {
        var list = new List<Entry>();
        var used = new HashSet<string>();
        foreach (var band in Tower.Bands)
        {
            var group = "Floor " + band.Number + ": " + band.Name;
            foreach (var town in Tower.Towns.Where(t => t.Band == band.Number))
            {
                used.Add(town.Layout);
                list.Add(new Entry
                {
                    Key = "town " + town.Key,
                    Title = town.Name + " (town)",
                    Detail = town.Layout,
                    Group = group,
                    Layout = town.Layout,
                    ScenePath = ScenePath(town.Layout),
                    TownKey = town.Key,
                });
            }
            // which parts share a layout, for "also parts 2, 5"
            var byLayout = new Dictionary<string, List<int>>();
            for (var step = band.FirstFloor; step <= band.LastFloor; step++)
            {
                var part = Tower.Get(step);
                if (part == null) continue;
                var layout = part.Layout ?? "";
                if (!byLayout.TryGetValue(layout, out var steps)) byLayout[layout] = steps = new List<int>();
                steps.Add(step);
            }
            for (var step = band.FirstFloor; step <= band.LastFloor; step++)
            {
                var part = Tower.Get(step);
                if (part == null) continue;
                used.Add(part.Layout);
                var others = byLayout[part.Layout ?? ""].Where(s => s != step).Select(s => Tower.PartOf(s).ToString()).ToList();
                var area = Tower.AreaName(part.Layout);
                list.Add(new Entry
                {
                    Key = "part " + step,
                    Title = "Part " + Tower.PartOf(step) + ": " + part.Name + (part.IsFloorBoss ? " (floor boss)" : ""),
                    Detail = area + " (" + part.Layout + ")" + (others.Count > 0 ? ", the same scene as part" + (others.Count > 1 ? "s " : " ") + string.Join(", ", others) : ""),
                    Group = group,
                    Layout = part.Layout,
                    ScenePath = ScenePath(part.Layout),
                    Step = step,
                });
            }
        }
        // tower scenes no part or town uses yet
        if (Directory.Exists(SceneFolder))
        {
            foreach (var file in Directory.GetFiles(SceneFolder, "Tower_*.unity").OrderBy(f => f))
            {
                var layout = Path.GetFileNameWithoutExtension(file);
                if (used.Contains(layout)) continue;
                list.Add(new Entry
                {
                    Key = "place " + layout,
                    Title = Tower.AreaName(layout),
                    Detail = layout + ", not in the climb yet",
                    Group = "Not in the climb yet",
                    Layout = layout,
                    ScenePath = ScenePath(layout),
                });
            }
        }
        return list;
    }
}
