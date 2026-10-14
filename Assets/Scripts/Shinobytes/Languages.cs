using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Languages for every text in <see cref="Localization"/>: the interface and the bot's chat messages.
/// English is built in, written in Localization.cs itself. Other languages are json files in
/// StreamingAssets/Languages, one per language, so players can add or correct one without a new
/// build: drop a file in the game's Ravenfall_Data/StreamingAssets/Languages folder.
/// </summary>
/// <remarks>
/// A language file:
/// <code>
/// { "code": "sv", "name": "Svenska", "strings": { "UI_SETTINGS_TITLE": "INSTÄLLNINGAR", "UI_SETTINGS_BOOST_OPTIONS": ["Prenumeranter", "Alla"] } }
/// </code>
/// Anything a file leaves out stays English. A translation that drops or renames a {placeholder} is
/// ignored for that text, with a warning, since the game fills those in. Chat messages start with
/// routing tags for the bot (%[category:...]%); the files hold only the readable text and the tags
/// are put back from the English. Ravenfall > Localization > Export template writes every text with
/// its English, for translators to start from.
/// </remarks>
public static class Languages
{
    public const string English = "en";
    public const string FolderName = "Languages";

    public sealed class Info
    {
        public string Code;
        public string Name;
        public string Path;
    }

    /// <summary>The language now in use, by its code.</summary>
    public static string Current { get; private set; } = English;

    /// <summary>Raised after a language is applied, for windows to redraw their text.</summary>
    public static event Action Changed;

    private static Dictionary<FieldInfo, object> english;
    private static List<Info> available;

    private static readonly Regex Placeholder = new Regex(@"\{[^{}]+\}", RegexOptions.Compiled);
    private static readonly Regex MetaPrefix = new Regex(@"^(%\[(category|tags):[^\]]*\]%)+", RegexOptions.Compiled);

    public static string Folder => System.IO.Path.Combine(Application.streamingAssetsPath, FolderName);

    /// <summary>Before any scene, so the first window already draws in the chosen language.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadSaved()
    {
        var code = PlayerSettings.Instance.Language;
        if (!string.IsNullOrEmpty(code) && code != English) Apply(code, notify: false);
    }

    /// <summary>English first, then every language file found, by name.</summary>
    public static IReadOnlyList<Info> Available
    {
        get
        {
            if (available != null) return available;
            available = new List<Info> { new Info { Code = English, Name = "English" } };
            try
            {
                if (Directory.Exists(Folder))
                {
                    foreach (var file in Directory.GetFiles(Folder, "*.json"))
                    {
                        // files starting with an underscore, such as the template, are not languages
                        if (System.IO.Path.GetFileName(file).StartsWith("_")) continue;
                        var root = JObject.Parse(File.ReadAllText(file));
                        var code = (string)root["code"] ?? System.IO.Path.GetFileNameWithoutExtension(file);
                        if (code == English || available.Any(a => a.Code == code)) continue;
                        available.Add(new Info { Code = code, Name = (string)root["name"] ?? code, Path = file });
                    }
                }
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("Could not read the language files: " + exc.Message);
            }

            available = available.Take(1).Concat(available.Skip(1).OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            return available;
        }
    }

    /// <summary>Switches every text to <paramref name="code"/>, English for anything the file lacks.</summary>
    public static void Apply(string code, bool notify = true)
    {
        CaptureEnglish();
        foreach (var kv in english) kv.Key.SetValue(null, kv.Value);
        Current = English;

        var info = code == English ? null : Available.FirstOrDefault(a => a.Code == code);
        if (info != null)
        {
            try
            {
                ApplyFile(info);
                Current = info.Code;
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("Could not load the language " + code + ": " + exc.Message);
                foreach (var kv in english) kv.Key.SetValue(null, kv.Value);
            }
        }

        if (notify) Changed?.Invoke();
    }

    /// <summary>Remembers the language and switches to it.</summary>
    public static void Set(string code)
    {
        PlayerSettings.Instance.Language = code;
        Apply(code);
    }

    private static void ApplyFile(Info info)
    {
        var root = JObject.Parse(File.ReadAllText(info.Path));
        var strings = root["strings"] as JObject;
        if (strings == null) return;

        foreach (var kv in english)
        {
            var token = strings[kv.Key.Name];
            if (token == null) continue;

            if (kv.Value is string source && token.Type == JTokenType.String)
            {
                var translated = (string)token;
                if (string.IsNullOrEmpty(translated)) continue;
                var prefix = MetaPrefix.Match(source).Value;
                var text = source.Substring(prefix.Length);
                if (!SamePlaceholders(text, translated))
                {
                    Shinobytes.Debug.LogWarning(info.Code + ": " + kv.Key.Name + " left in English, its {placeholders} differ from the English.");
                    continue;
                }

                kv.Key.SetValue(null, prefix + translated);
            }
            else if (kv.Value is string[] sourceArray && token.Type == JTokenType.Array)
            {
                var translated = token.ToObject<string[]>();
                // the game indexes these, so a list of another length would break a menu
                if (translated == null || translated.Length != sourceArray.Length)
                {
                    Shinobytes.Debug.LogWarning(info.Code + ": " + kv.Key.Name + " left in English, it needs exactly " + sourceArray.Length + " entries.");
                    continue;
                }

                // each entry keeps its English routing tags, as single messages do
                kv.Key.SetValue(null, translated.Select((t, i) =>
                {
                    if (string.IsNullOrEmpty(t)) return sourceArray[i];
                    var prefix = MetaPrefix.Match(sourceArray[i]).Value;
                    return SamePlaceholders(sourceArray[i].Substring(prefix.Length), t) ? prefix + t : sourceArray[i];
                }).ToArray());
            }
        }
    }

    private static bool SamePlaceholders(string a, string b)
    {
        var pa = Placeholder.Matches(a).Cast<Match>().Select(m => m.Value).OrderBy(x => x, StringComparer.Ordinal);
        var pb = Placeholder.Matches(b).Cast<Match>().Select(m => m.Value).OrderBy(x => x, StringComparer.Ordinal);
        return pa.SequenceEqual(pb);
    }

    private static IEnumerable<FieldInfo> TextFields()
    {
        return typeof(Localization)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => !f.IsInitOnly && !f.IsLiteral && (f.FieldType == typeof(string) || f.FieldType == typeof(string[])));
    }

    private static void CaptureEnglish()
    {
        if (english != null) return;
        english = new Dictionary<FieldInfo, object>();
        foreach (var f in TextFields())
        {
            var value = f.GetValue(null);
            english[f] = value is string[] arr ? arr.ToArray() : value;
        }
    }

    /// <summary>
    /// Every text with its English, as a starting point for a new language. Chat messages lose their
    /// routing tags here; they are put back when the language loads.
    /// </summary>
    public static string ExportTemplate()
    {
        CaptureEnglish();
        var strings = new JObject();
        foreach (var kv in english.OrderBy(k => k.Key.Name, StringComparer.Ordinal))
        {
            if (kv.Value is string s) strings[kv.Key.Name] = s.Substring(MetaPrefix.Match(s).Value.Length);
            else if (kv.Value is string[] arr) strings[kv.Key.Name] = new JArray(arr.Select(x => x.Substring(MetaPrefix.Match(x).Value.Length)));
        }

        var root = new JObject
        {
            ["code"] = "xx",
            ["name"] = "Language name, as its speakers write it",
            ["notes"] = "Translate the values, keep the keys. Keep every {placeholder} exactly as it is, in any order. " +
                        "Lists must keep their number of entries. Chat commands such as !join stay as they are. " +
                        "Leave out anything not translated yet; it stays English.",
            ["strings"] = strings,
        };
        return root.ToString(Newtonsoft.Json.Formatting.Indented);
    }

    /// <summary>Forgets the scanned files, so a new or edited one is seen.</summary>
    public static void Rescan() => available = null;
}
