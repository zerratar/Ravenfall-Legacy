using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Tests
{
    /// <summary>
    /// Tests for the drop-announcement text builder.
    /// <para>
    /// This file previously carried a verbatim copy of PlayerItemDropText (and its settings enum)
    /// because Assembly-CSharp cannot be referenced from a test assembly, so the real type was
    /// unreachable. The test therefore exercised the duplicate and told us nothing about shipped
    /// behaviour. PlayerItemDropText now lives in Ravenfall.Core, so these run against the real
    /// implementation and the copy is gone.
    /// </para>
    /// </summary>
    internal class GameUnitTests
    {
        private const int TwitchMessageLimit = 475;

        private static Dictionary<string, List<string>> SampleDrops()
        {
            var receivers = new List<string>
            {
                "Zerratar", "TripTheFirst", "RavenMMO", "Madgarou",
                "Beredis", "Kohrean", "Grandmazc00kies"
            };

            return new Dictionary<string, List<string>>
            {
                ["Rune 2H Sword"] = new List<string>(receivers),
                ["Dragon Helmet"] = new List<string>(receivers),
                ["Black Cat"] = new List<string>(receivers),
                ["Magic Wizard Eye Patch"] = new List<string>(receivers),
                ["Wonderstick"] = new List<string>(receivers),
                ["Banana Peel Pie"] = new List<string>(receivers),
                ["Plastic Bag"] = new List<string>(receivers),
                ["Michael Jacksons Secret Door Key"] = new List<string>(receivers),
                ["Letter That Shallnt Be Open"] = new List<string>(receivers),
            };
        }

        [Test]
        public void TestPlayerDropTexts_Minimal_CreatesMultipleMessages()
        {
            var text = new PlayerItemDropText(SampleDrops(), PlayerItemDropMessageSettings.Minimal);

            Assert.IsTrue(text.Messages.Count > 0, "expected at least one message");

            foreach (var message in text.Messages)
            {
                Assert.LessOrEqual(message.Length, TwitchMessageLimit,
                    $"message exceeds the {TwitchMessageLimit} char limit and would be rejected: {message}");
            }
        }

        [Test]
        public void PlayerDropText_MentionsEveryItem()
        {
            var drops = SampleDrops();
            var text = new PlayerItemDropText(drops, PlayerItemDropMessageSettings.Minimal);
            var combined = string.Join(" ", text.Messages);

            foreach (var item in drops.Keys)
            {
                Assert.IsTrue(combined.Contains(item),
                    $"'{item}' was dropped but never appears in the announcement text.");
            }
        }

        [Test]
        public void PlayerDropText_NoDrops_ProducesNoMessages()
        {
            var text = new PlayerItemDropText(
                new Dictionary<string, List<string>>(), PlayerItemDropMessageSettings.Minimal);

            Assert.AreEqual(0, text.Messages.Count(),
                "an empty drop set should not announce anything");
        }

        [Test]
        public void PlayerDropText_RespectsMessageLimit_ForManyReceivers()
        {
            // A raid can drop one item to a very large number of players; the builder has to split
            // that across messages rather than emit one oversized line.
            var many = Enumerable.Range(0, 400).Select(i => "Player" + i).ToList();
            var drops = new Dictionary<string, List<string>> { ["Dragon Helmet"] = many };

            var text = new PlayerItemDropText(drops, PlayerItemDropMessageSettings.Minimal);

            Assert.IsTrue(text.Messages.Count > 0, "expected at least one message");
            foreach (var message in text.Messages)
            {
                Assert.LessOrEqual(message.Length, TwitchMessageLimit,
                    $"message exceeds the {TwitchMessageLimit} char limit: {message}");
            }
        }
    }
}
