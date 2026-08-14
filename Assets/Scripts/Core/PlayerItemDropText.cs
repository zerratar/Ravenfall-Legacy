using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

public class PlayerItemDropText
{
    private readonly IReadOnlyList<string> messages;

    public int Count { get; private set; }
    public IReadOnlyList<string> Messages => messages;
    public PlayerItemDropText(
        Dictionary<string, List<string>> droppedItems,
        PlayerItemDropMessageSettings settings)
    {
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
                        Append(playersRef[i] + " you found " + (IsVocal(itemName[0]) ? "an " : "a ") + itemName);
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
                    var prefix = itemName + " was found by ";
                    var group = new List<string>();
                    // prefix plus the trailing ". "
                    var groupLength = prefix.Length + 2;

                    void FlushGroup()
                    {
                        if (group.Count == 0) return;

                        var text = prefix;
                        for (int i = 0; i < group.Count; i++)
                        {
                            if (i > 0)
                            {
                                text += (i == group.Count - 1) ? " and " : ", ";
                            }
                            text += group[i];
                        }
                        text += ". ";

                        Append(text);
                        group.Clear();
                        groupLength = prefix.Length + 2;
                    }

                    for (int i = 0; i < playersRef.Count; i++)
                    {
                        var player = playersRef[i];
                        // worst case separator is " and " (5 chars)
                        var separatorLength = group.Count == 0 ? 0 : 5;

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