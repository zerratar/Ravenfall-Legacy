using Shinobytes.Linq;
using System.Collections.Generic;
using UnityEngine;

public class EventCode
{
    private static readonly object mutex = new object();
    private static List<string> wordList;
    private static bool isLoaded;
    public static void LoadWords()
    {
        //lock (mutex)
        {
            if (isLoaded || wordList != null && wordList.Count > 0) return;
            try
            {
                var txt = Resources.Load<TextAsset>("wordlist")
                       ?? Resources.Load<TextAsset>("wordlist.txt")
                       ?? Resources.Load<TextAsset>("Resources\\wordlist")
                       ?? Resources.Load<TextAsset>("Resources\\wordlist.txt");

                if (txt != null)
                {
                    wordList = txt.text.Split('\n').Select(x => x.Trim('\r')).AsList(x => !string.IsNullOrEmpty(x));
                    isLoaded = true;
                    return;
                }
                Shinobytes.Debug.LogError("Resources\\wordlist.txt does not exist.");
            }
            catch (System.Exception exc)
            {
                // failed t
                Shinobytes.Debug.LogError("EventCode.LoadWords: " + exc);
            }
        }
    }

    public static string New()
    {
        //lock (mutex)
        {
            LoadWords();

            if (wordList == null)
            {
                return null;
            }

            return wordList.Random();
        }
    }

    /// <summary>
    /// Whether <paramref name="typed"/> is the code a raid or dungeon asks for. No code asked
    /// for lets anything through. Case and surrounding spaces are ignored, since chat and phone
    /// keyboards capitalise as they please.
    /// </summary>
    public static bool Matches(string required, string typed)
    {
        if (string.IsNullOrWhiteSpace(required))
        {
            return true;
        }

        return typed != null && string.Equals(required.Trim(), typed.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }
}
