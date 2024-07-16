using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
public class PlayerLootManager
{
    private readonly List<PlayerLootRecord> records = new List<PlayerLootRecord>();

    internal void Add(PlayerLootRecord record)
    {
        records.Add(record);
    }

    internal void Clear()
    {
        records.Clear();
    }

    internal IReadOnlyList<PlayerLootRecord> Query(string filterQuery)
    {
        if (string.IsNullOrEmpty(filterQuery))
        {
            return records;
        }

        // Normalize the query by converting it to lowercase and splitting by "and" or "or"
        var filters = filterQuery.ToLower().Split(new[] { " and ", " or " }, StringSplitOptions.None);
        var andConditions = filterQuery.ToLower().Contains(" and ");
        var orConditions = filterQuery.ToLower().Contains(" or ");

        var filteredRecords = records.AsEnumerable();

        foreach (var filter in filters)
        {
            if (filter.Contains("last raid"))
            {
                var lastRaid = records.Where(r => r.RaidIndex != -1).OrderByDescending(r => r.Time).FirstOrDefault();
                if (!string.IsNullOrEmpty(lastRaid.ItemName))
                {
                    filteredRecords = filteredRecords.Where(r => r.RaidIndex == lastRaid.RaidIndex);
                }
            }
            else if (filter.Contains("last dungeon"))
            {
                var lastDungeon = records.Where(r => r.DungeonIndex != -1).OrderByDescending(r => r.Time).FirstOrDefault();
                if (!string.IsNullOrEmpty(lastDungeon.ItemName))
                {
                    filteredRecords = filteredRecords.Where(r => r.DungeonIndex == lastDungeon.DungeonIndex);
                }
            }
            else if (filter.Contains("raid"))
            {
                filteredRecords = filteredRecords.Where(r => r.RaidIndex != -1);
            }
            else if (filter.Contains("dungeon"))
            {
                filteredRecords = filteredRecords.Where(r => r.DungeonIndex != -1);
            }
            else if (filter.Contains("all") || string.IsNullOrEmpty(filterQuery))
            {
                filteredRecords = records.AsEnumerable();
            }
            else if (filter.Contains("from"))
            {
                var timeString = filter.Replace("from", "").Trim();
                if (TimeParser.TryParseTimeAgo(timeString, out var dt))
                {
                    filteredRecords = filteredRecords.Where(r => r.Time >= dt);
                }
            }
            else
            {
                if (TimeParser.TryParseTimeAgo(filter, out var dt))
                {
                    filteredRecords = filteredRecords.Where(r => r.Time >= dt);
                }
                else
                {
                    // Check for item name filter
                    filteredRecords = filteredRecords.Where(r => r.ItemName.ToLower().Contains(filter.Trim()));
                }
            }
        }

        // Combining the results based on the presence of AND or OR conditions
        if (andConditions)
        {
            // If there are AND conditions, all filters must be satisfied
            filteredRecords = filters.Aggregate(records.AsEnumerable(), (current, filter) => current.Where(r => FilterRecord(r, filter)));
        }
        else if (orConditions)
        {
            // If there are OR conditions, any of the filters must be satisfied
            filteredRecords = records.AsEnumerable().Where(r => filters.Any(filter => FilterRecord(r, filter)));
        }

        return filteredRecords.ToList();
    }

    private bool FilterRecord(PlayerLootRecord record, string filter)
    {
        if (filter.Contains("last raid"))
        {
            var lastRaid = records.Where(r => r.RaidIndex != -1).OrderByDescending(r => r.Time).FirstOrDefault();
            return !string.IsNullOrEmpty(lastRaid.ItemName) && record.RaidIndex == lastRaid.RaidIndex;
        }
        else if (filter.Contains("last dungeon"))
        {
            var lastDungeon = records.Where(r => r.DungeonIndex != -1).OrderByDescending(r => r.Time).FirstOrDefault();
            return !string.IsNullOrEmpty(lastDungeon.ItemName) && record.DungeonIndex == lastDungeon.DungeonIndex;
        }
        else if (filter.Contains("raid"))
        {
            return record.RaidIndex != -1;
        }
        else if (filter.Contains("dungeon"))
        {
            return record.DungeonIndex != -1;
        }
        else if (filter.Contains("all") || string.IsNullOrEmpty(filter))
        {
            return true;
        }
        else if (filter.Contains("from"))
        {
            var timeString = filter.Replace("from", "").Trim();
            try
            {
                var fromTime = TimeParser.ParseTimeAgo(timeString);
                return record.Time >= fromTime;
            }
            catch (ArgumentException)
            {
                // Handle invalid time format
            }
        }
        else
        {
            // first try get a DateTime, in case we used !loot 4 minutes ago, and not by its name

            if (TimeParser.TryParseTimeAgo(filter.Trim(), out var dt))
            {
                return record.Time >= dt;
            }
            else
            {
                // Check for item name filter
                return record.ItemName.ToLower().Contains(filter.Trim());
            }
        }

        return false;
    }
}

public struct PlayerLootRecord
{
    public DateTime Time;
    public string ItemName;
    public int Amount;

    /// <summary>
    ///     Which dungeon this was dropped from, if -1 it was not dropped from a dungeon.
    /// </summary>
    public int DungeonIndex;

    /// <summary>
    ///     Which raid this was dropped from, if -1 it was not dropped from a raid.
    /// </summary>
    public int RaidIndex;
}

public static class TimeParser
{
    private static readonly Dictionary<string, int> numberWords = new Dictionary<string, int>
    {
        { "zero", 0 }, { "one", 1 }, { "two", 2 }, { "three", 3 }, { "four", 4 },
        { "five", 5 }, { "six", 6 }, { "seven", 7 }, { "eight", 8 }, { "nine", 9 },
        { "ten", 10 }, { "eleven", 11 }, { "twelve", 12 }, { "thirteen", 13 }, { "fourteen", 14 },
        { "fifteen", 15 }, { "sixteen", 16 }, { "seventeen", 17 }, { "eighteen", 18 },
        { "nineteen", 19 }, { "twenty", 20 }, { "thirty", 30 }, { "forty", 40 },
        { "fifty", 50 }, { "sixty", 60 }, { "seventy", 70 }, { "eighty", 80 }, { "ninety", 90 }
    };

    public static bool IsTimeAgoFormat(string input)
    {
        input = input.ToLowerInvariant().Trim();
        return Regex.IsMatch(input, @"(\d+|\b" + string.Join(@"\b|\b", numberWords.Keys) + @"\b)\s+(second|minute|hour|day|week|month|year)s?\s*(and\s*(\d+|\b" + string.Join(@"\b|\b", numberWords.Keys) + @"\b)\s+(second|minute|hour|day|week|month|year)s?)*\s*ago");
    }

    public static bool TryParseTimeAgo(string timeAgo, out DateTime dt)
    {
        try
        {
            dt = ParseTimeAgo(timeAgo);
            return true;
        }
        catch
        {
            dt = DateTime.MinValue;
            return false;
        }
    }
    public static DateTime ParseTimeAgo(string timeAgo)
    {
        timeAgo = timeAgo.ToLowerInvariant().Trim();
        var matches = Regex.Matches(timeAgo, @"(\d+|\b" + string.Join(@"\b|\b", numberWords.Keys) + @"\b)\s+(s|sec|sec|second|m|min|minute|h|hour|d|day|w|week|month|year)s?");
        var totalSeconds = 0;

        foreach (Match match in matches)
        {
            var value = match.Groups[1].Value;
            var unit = match.Groups[2].Value;

            int number;
            if (numberWords.ContainsKey(value))
            {
                number = numberWords[value];
            }
            else if (!int.TryParse(value, out number))
            {
                throw new ArgumentException("Invalid time format");
            }

            switch (unit)
            {
                case "s":
                case "sec":
                case "secs":
                case "second":
                case "seconds":
                    totalSeconds += number;
                    break;
                case "m":
                case "min":
                case "mins":
                case "minute":
                case "minutes":
                    totalSeconds += number * 60;
                    break;
                case "h":
                case "hour":
                case "hours":
                    totalSeconds += number * 3600;
                    break;
                case "d":
                case "day":
                case "days":
                    totalSeconds += number * 86400;
                    break;
                case "w":
                case "week":
                case "weeks":
                    totalSeconds += number * 604800;
                    break;
                case "month":
                case "months":
                    totalSeconds += number * 2592000;
                    break;
                case "year":
                case "years":
                    totalSeconds += number * 31536000;
                    break;
                default:
                    throw new ArgumentException("Invalid time unit");
            }
        }

        return DateTime.UtcNow.AddSeconds(-totalSeconds);
    }

    public static string NumberToWords(int number)
    {
        if (number == 0)
            return "zero";

        if (number < 0)
            return "minus " + NumberToWords(Math.Abs(number));

        string words = "";

        if ((number / 1000000) > 0)
        {
            words += NumberToWords(number / 1000000) + " million ";
            number %= 1000000;
        }

        if ((number / 1000) > 0)
        {
            words += NumberToWords(number / 1000) + " thousand ";
            number %= 1000;
        }

        if ((number / 100) > 0)
        {
            words += NumberToWords(number / 100) + " hundred ";
            number %= 100;
        }

        if (number > 0)
        {
            if (words != "")
                words += "and ";

            var unitsMap = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
            var tensMap = new[] { "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

            if (number < 20)
                words += unitsMap[number];
            else
            {
                words += tensMap[number / 10];
                if ((number % 10) > 0)
                    words += "-" + unitsMap[number % 10];
            }
        }

        return words;
    }
}