﻿
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Tests
{
    internal class GameUnitTests
    {
        [Test]
        public void TestPlayerDropTexts_Minimal_CreatesMultipleMessages()
        {
            Dictionary<string, List<string>> dropped = new Dictionary<string, List<string>>
            {
                ["Rune 2H Sword"] = new List<string> { "Zerratar", "TripTheFirst", "RavenMMO", "Madgarou", "Beredis", "Kohrean", "Grandmazc00kies" },
                ["Dragon Helmet"] = new List<string> { "Zerratar", "TripTheFirst", "RavenMMO", "Madgarou", "Beredis", "Kohrean", "Grandmazc00kies" },
                ["Black Cat"] = new List<string> { "Zerratar", "TripTheFirst", "RavenMMO", "Madgarou", "Beredis", "Kohrean", "Grandmazc00kies" },
                ["Magic Wizard Eye Patch"] = new List<string> { "Zerratar", "TripTheFirst", "RavenMMO", "Madgarou", "Beredis", "Kohrean", "Grandmazc00kies" },
                ["Wonderstick"] = new List<string> { "Zerratar", "TripTheFirst", "RavenMMO", "Madgarou", "Beredis", "Kohrean", "Grandmazc00kies" },
                ["Banana Peel Pie"] = new List<string> { "Zerratar", "TripTheFirst", "RavenMMO", "Madgarou", "Beredis", "Kohrean", "Grandmazc00kies" },
                ["Plastic Bag"] = new List<string> { "Zerratar", "TripTheFirst", "RavenMMO", "Madgarou", "Beredis", "Kohrean", "Grandmazc00kies" },
                ["Michael Jacksons Secret Door Key"] = new List<string> { "Zerratar", "TripTheFirst", "RavenMMO", "Madgarou", "Beredis", "Kohrean", "Grandmazc00kies" },
                ["Letter That Shallnt Be Open"] = new List<string> { "Zerratar", "TripTheFirst", "RavenMMO", "Madgarou", "Beredis", "Kohrean", "Grandmazc00kies" },
            };
            //new Dictionary<string, List<string>>();

            var text = new PlayerItemDropText(dropped, PlayerItemDropMessageSettings.Minimal);
            Assert.IsTrue(text.Messages.Count > 0);

            foreach (var t in text.Messages)
            {
                Assert.IsTrue(t.Length <= 475);
            }
        }
    }

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

                if (sb.Length + msg.Length >= sb.Capacity)
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
}