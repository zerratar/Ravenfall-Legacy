using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

/// <summary>
/// The words around the names in an item drop message. English by default; the game passes its
/// translation, which this assembly cannot see.
/// </summary>
public sealed class PlayerItemDropWording
{
    /// <summary>{0} is the player, {1} an item starting with a consonant.</summary>
    public string YouFoundA = "{0} you found a {1}";
    /// <summary>{0} is the player, {1} an item starting with a vowel.</summary>
    public string YouFoundAn = "{0} you found an {1}";
    /// <summary>{0} is the item, {1} the list of players.</summary>
    public string FoundBy = "{0} was found by {1}.";
    /// <summary>Before the last name in a list.</summary>
    public string And = " and ";

    public static readonly PlayerItemDropWording English = new PlayerItemDropWording();
}

public class PlayerItemDropText
{
    private readonly IReadOnlyList<string> messages;
    private readonly PlayerItemDropWording wording;

    public int Count { get; private set; }
    public IReadOnlyList<string> Messages => messages;
    public PlayerItemDropText(
        Dictionary<string, List<string>> droppedItems,
        PlayerItemDropMessageSettings settings,
        PlayerItemDropWording wording = null)
    {
        this.wording = wording ?? PlayerItemDropWording.English;
        this.messages = this.Process(droppedItems, settings);
    }

    private IReadOnlyList<string> Process(
        Dictionary<string, List<string>> items,
        PlayerItemDropMessageSettings settings,
        int maxLength = 475)
    {
        // no need for linq here since we need to enumerate everything anyway.
        //Count = items.Values.Sum(x => x.Count);
        var count = 0;
        var output = new List<string>();
        var sb = new StringBuilder(maxLength);

        void Next()
        {
            output.Add(sb.ToString().Trim());
            sb.Clear();
        }

        void Append(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;

            // Compare against maxLength, not sb.Capacity: StringBuilder grows its capacity as it
            // fills, so using Capacity here let the effective limit drift upwards.
            if (sb.Length > 0 && sb.Length + msg.Length >= maxLength)
            {
                Next();
            }

            sb.Append(msg);
            return;
        }

        var appendStrings = new List<string>();

        // we will rely on word wrapping so we don't split player names and
        // they can get properly pinged in the chat.
        foreach (var kvp in items)
        {
            var itemName = kvp.Key;
            var playersRef = kvp.Value;
            if (string.IsNullOrEmpty(itemName))
                continue;
            try
            {
                if (settings == PlayerItemDropMessageSettings.NoMessage)
                {
                    continue;
                }

                if (settings == PlayerItemDropMessageSettings.ItemNameOnly)
                {
                    appendStrings.Add(itemName);
                    //Append(itemName + ", ");
                    //Next();
                    continue;
                }

                if (settings == PlayerItemDropMessageSettings.ItemNameAndCountOnly)
                {
                    if (playersRef.Count > 1)
                        appendStrings.Add(playersRef.Count + "x " + itemName);
                    else
                        appendStrings.Add(itemName);

                    //Next();
                    continue;
                }

                if (settings == PlayerItemDropMessageSettings.OnePlayerPerRow)
                {
                    for (int i = 0; i < playersRef.Count; i++)
                    {
                        Append(string.Format(IsVocal(itemName[0]) ? wording.YouFoundAn : wording.YouFoundA, playersRef[i], itemName));
                        Next();
                    }
                    continue;
                }
                else
                {
                    // One item can be dropped to hundreds of players in a raid. Building the whole
                    // "<item> was found by ..." line in one go produced a single message far over
                    // the limit, which chat then rejects - so the players were never told. Split
                    // the receiver list into as many messages as it takes instead, repeating the
                    // item prefix on each.
                    var format = wording.FoundBy;
                    var and = wording.And;
                    var group = new List<string>();
                    // the sentence around the names, plus the trailing space
                    var overhead = string.Format(format, itemName, string.Empty).Length + 1;
                    var groupLength = overhead;

                    void FlushGroup()
                    {
                        if (group.Count == 0) return;

                        var names = new StringBuilder();
                        for (int i = 0; i < group.Count; i++)
                        {
                            if (i > 0)
                            {
                                names.Append((i == group.Count - 1) ? and : ", ");
                            }
                            names.Append(group[i]);
                        }

                        Append(string.Format(format, itemName, names) + " ");
                        group.Clear();
                        groupLength = overhead;
                    }

                    for (int i = 0; i < playersRef.Count; i++)
                    {
                        var player = playersRef[i];
                        // worst case separator is the "and" before the last name
                        var separatorLength = group.Count == 0 ? 0 : System.Math.Max(and.Length, 2);

                        if (group.Count > 0 && groupLength + separatorLength + player.Length >= maxLength)
                        {
                            FlushGroup();
                            separatorLength = 0;
                        }

                        group.Add(player);
                        groupLength += separatorLength + player.Length;
                    }

                    FlushGroup();
                }
                if (settings == PlayerItemDropMessageSettings.OneItemPerRow)
                {
                    Next();
                }
            }
            finally
            {
                count += playersRef.Count;
            }
        }

        if (appendStrings.Count > 0)
        {
            Append(string.Join(", ", appendStrings) + ".");
        }

        var remaining = sb.ToString();
        if (!string.IsNullOrEmpty(remaining))
            output.Add(remaining);

        Count = count;
        return output;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsVocal(char c)
    {
        c = char.ToLower(c);
        return c == 'a' || c == 'i' || c == 'e' || c == 'u' || c == 'o';
    }
}

public enum PlayerItemDropMessageSettings : int
{
    OneItemPerRow = 0,
    OnePlayerPerRow = 1,
    Minimal = 2,
    ItemNameAndCountOnly = 3,
    ItemNameOnly = 4,
    NoMessage = 5
}