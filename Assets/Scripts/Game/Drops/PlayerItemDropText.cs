using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

public class PlayerItemDropText
{
    private readonly Dictionary<string, List<string>> droppedItems;
    private readonly bool newMessagePerItem;
    private readonly IReadOnlyList<string> messages;
    public int Count { get; private set; }
    public IReadOnlyList<string> Messages => messages;
    public PlayerItemDropText(
        Dictionary<string, List<string>> droppedItems,
        bool newMessagePerItem = false)
    {
        this.droppedItems = droppedItems;
        this.newMessagePerItem = newMessagePerItem;
        this.messages = this.Process(droppedItems);
    }

    private IReadOnlyList<string> Process(Dictionary<string, List<string>> items)
    {
        // no need for linq here since we need to enumerate everything anyway.
        //Count = items.Values.Sum(x => x.Count);
        var count = 0;
        var output = new List<string>();
        var sb = new StringBuilder(500);

        void Next()
        {
            output.Add(sb.ToString());
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

            Append((IsVocal(itemName[0]) ? "An " : "A ") + itemName);
            Append(" was found by ");

            for (int i = 0; i < playersRef.Count; i++)
            {
                // last player in list
                var lastInList = playersRef.Count - 1 == i;
                var player = playersRef[i];
                if (i > 0)
                {
                    Append(lastInList ? " and " : ", ");
                }
                Append(player);
            }
            Append(". ");

            if (newMessagePerItem)
                Next();

            count += playersRef.Count;
        }

        var remaining = sb.ToString();
        if (!string.IsNullOrEmpty(remaining))
            output.Add(remaining);

        return output;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsVocal(char c)
    {
        c = char.ToLower(c);
        return c == 'a' || c == 'i' || c == 'e' || c == 'u' || c == 'o';
    }
}
