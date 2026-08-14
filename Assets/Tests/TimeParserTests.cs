using NUnit.Framework;
using System;

namespace Assets.Scripts.Tests
{
    /// <summary>
    /// TimeParser turns free-typed chat input such as "5 minutes ago" or "two hours ago" into a
    /// timestamp, so it is exposed directly to whatever viewers type. These tests pin the parsing
    /// rules and, importantly, the behaviour on input it cannot understand.
    /// </summary>
    public class TimeParserTests
    {
        // ParseTimeAgo returns DateTime.UtcNow minus the parsed duration, so assertions compare the
        // resulting offset with a tolerance rather than an absolute instant.
        private static void AssertAgo(string input, TimeSpan expected)
        {
            var before = DateTime.UtcNow;
            Assert.IsTrue(TimeParser.TryParseTimeAgo(input, out var parsed), $"failed to parse '{input}'");
            var after = DateTime.UtcNow;

            var maxOffset = after - parsed;
            var minOffset = before - parsed;

            Assert.LessOrEqual(minOffset, expected + TimeSpan.FromSeconds(2),
                $"'{input}' parsed as further back than expected");
            Assert.GreaterOrEqual(maxOffset, expected - TimeSpan.FromSeconds(2),
                $"'{input}' parsed as more recent than expected");
        }

        [TestCase("5 seconds ago", 5)]
        [TestCase("30 seconds ago", 30)]
        [TestCase("1 minute ago", 60)]
        [TestCase("5 minutes ago", 300)]
        [TestCase("2 hours ago", 7200)]
        [TestCase("1 day ago", 86400)]
        public void ParsesNumericDurations(string input, int expectedSeconds)
        {
            AssertAgo(input, TimeSpan.FromSeconds(expectedSeconds));
        }

        [TestCase("five minutes ago", 300)]
        [TestCase("two hours ago", 7200)]
        [TestCase("ten seconds ago", 10)]
        [TestCase("twenty minutes ago", 1200)]
        public void ParsesWordNumbers(string input, int expectedSeconds)
        {
            AssertAgo(input, TimeSpan.FromSeconds(expectedSeconds));
        }

        [TestCase("5 minutes ago")]
        [TestCase("two hours ago")]
        [TestCase("1 day ago")]
        public void IsTimeAgoFormat_AcceptsValidInput(string input)
        {
            Assert.IsTrue(TimeParser.IsTimeAgoFormat(input), $"'{input}' should be recognised");
        }

        [TestCase("")]
        [TestCase("hello")]
        [TestCase("dragon helmet")]
        [TestCase("ago")]
        [TestCase("minutes")]
        public void IsTimeAgoFormat_RejectsNonTimeInput(string input)
        {
            Assert.IsFalse(TimeParser.IsTimeAgoFormat(input), $"'{input}' should not be recognised as a time");
        }

        [Test]
        public void TryParseTimeAgo_DoesNotThrowOnArbitraryInput()
        {
            // Viewers can type anything at all; this must never take the game down.
            foreach (var junk in new[] { "", "   ", "!@#$%", "999999999999999999 years ago", "-5 minutes ago" })
            {
                Assert.DoesNotThrow(() => TimeParser.TryParseTimeAgo(junk, out _),
                    $"threw on input '{junk}'");
            }
        }

        [Test]
        public void TryParseTimeAgo_IsCaseInsensitive()
        {
            Assert.IsTrue(TimeParser.TryParseTimeAgo("5 MINUTES AGO", out _));
            Assert.IsTrue(TimeParser.TryParseTimeAgo("Five Minutes Ago", out _));
        }
    }
}
