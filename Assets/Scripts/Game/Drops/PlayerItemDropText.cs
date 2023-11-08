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

    private IReadOnlyList<string> Process(Dictionary<string, List<string>> items, PlayerItemDropMessageSettings settings, int maxLength = 500)
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

            if (sb.Length + msg.Length >= sb.Capacity)
            {
                Next();
            }

            sb.Append(msg);
            return;
        }

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
                if (settings == PlayerItemDropMessageSettings.OnePlayerPerRow)
                {
                    for (int i = 0; i < playersRef.Count; i++)
                    {
                        Append(playersRef[i] + " you found " + (IsVocal(itemName[0]) ? "an " : "a ") + itemName);
                        Next();
                    }
                    continue;
                }

                var toAppend = itemName + " was found by ";
                for (int i = 0; i < playersRef.Count; i++)
                {
                    // last player in list
                    var lastInList = playersRef.Count - 1 == i;
                    var player = playersRef[i];
                    if (i > 0)
                    {
                        toAppend += lastInList ? " and " : ", ";
                    }
                    toAppend += player;
                }
                toAppend += ". ";

                // append the whole text at once instead, since it will check if we need to break it into a new message or not.
                Append(toAppend);

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
    Minimal = 2
}