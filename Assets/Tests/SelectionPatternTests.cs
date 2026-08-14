using NUnit.Framework;
using System;
using System.Linq;

namespace Assets.Scripts.Tests
{
    /// <summary>
    /// Locks in the rewrite applied to the per-frame target selection in RaidHandler and
    /// StreamRaidHandler, where "sort everything then take the first" was replaced with a single
    /// pass.
    /// <para>
    /// These do not exercise the handlers themselves - those live in Assembly-CSharp and are not
    /// reachable from a test assembly. What they verify is the transformation rule itself, which is
    /// the part that could silently change which player gets healed or attacked: that a single pass
    /// keeping the first element among equals picks exactly the same element as LINQ's stable
    /// OrderBy/ThenBy followed by FirstOrDefault. Ties are the interesting case, so the generated
    /// keys deliberately use a tiny range to produce lots of them.
    /// </para>
    /// </summary>
    public class SelectionPatternTests
    {
        [Test]
        public void SinglePassMax_MatchesOrderByDescendingThenFirst()
        {
            var rng = new Random(12345);
            for (var trial = 0; trial < 20000; trial++)
            {
                var n = rng.Next(0, 12);
                var key = new int[n];
                for (var i = 0; i < n; i++) key[i] = rng.Next(0, 4);

                var expected = Enumerable.Range(0, n)
                    .OrderByDescending(i => key[i])
                    .Select(i => (int?)i)
                    .FirstOrDefault();

                int? actual = null;
                var best = int.MinValue;
                for (var i = 0; i < n; i++)
                {
                    if (actual == null || key[i] > best)
                    {
                        actual = i;
                        best = key[i];
                    }
                }

                Assert.AreEqual(expected, actual, $"trial {trial} with keys [{string.Join(",", key)}]");
            }
        }

        [Test]
        public void SinglePassMultiKeyMin_MatchesOrderByThenByThenBy()
        {
            var rng = new Random(67890);
            for (var trial = 0; trial < 20000; trial++)
            {
                var n = rng.Next(0, 12);
                int[] k1 = new int[n], k3 = new int[n];
                float[] k2 = new float[n];
                for (var i = 0; i < n; i++)
                {
                    k1[i] = rng.Next(0, 3);
                    k2[i] = rng.Next(0, 3);
                    k3[i] = rng.Next(0, 3);
                }

                var expected = Enumerable.Range(0, n)
                    .OrderBy(i => k1[i])
                    .ThenBy(i => k2[i])
                    .ThenBy(i => k3[i])
                    .Select(i => (int?)i)
                    .FirstOrDefault();

                int? actual = null;
                int b1 = 0, b3 = 0;
                var b2 = 0f;
                for (var i = 0; i < n; i++)
                {
                    if (actual == null
                        || k1[i] < b1
                        || (k1[i] == b1 && k2[i] < b2)
                        || (k1[i] == b1 && k2[i] == b2 && k3[i] < b3))
                    {
                        actual = i;
                        b1 = k1[i];
                        b2 = k2[i];
                        b3 = k3[i];
                    }
                }

                Assert.AreEqual(expected, actual, $"trial {trial}");
            }
        }

        [Test]
        public void SquaredDistance_OrdersIdenticallyToDistance()
        {
            // StreamRaidHandler now compares squared distance instead of Vector3.Distance. sqrt is
            // monotonic over non-negative values, so ordering and equality must both be preserved.
            var rng = new Random(24680);
            for (var trial = 0; trial < 20000; trial++)
            {
                var a = (float)rng.Next(0, 50);
                var b = (float)rng.Next(0, 50);

                Assert.AreEqual(Math.Sign(a.CompareTo(b)), Math.Sign((a * a).CompareTo(b * b)),
                    $"ordering differed for {a} vs {b}");
            }
        }
    }
}
