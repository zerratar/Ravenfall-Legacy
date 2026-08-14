using NUnit.Framework;

namespace Assets.Scripts.Tests
{
    /// <summary>
    /// Characterization tests for the progression maths.
    /// <para>
    /// These do not assert that the numbers are <i>correct</i> - they assert that they are
    /// <i>unchanged</i>. Every expected value here was read out of the running editor before any
    /// refactoring, so if one of these fails it means a change altered live game balance. That is
    /// either a bug, or an intentional balance change that should be made deliberately and the
    /// expected value updated in the same commit.
    /// </para>
    /// <para>
    /// This file is only possible because GameMath now lives in the Ravenfall.Core assembly. A
    /// test assembly can reference an asmdef but never Assembly-CSharp, so while this code sat in
    /// Assembly-CSharp it was unreachable from any test.
    /// </para>
    /// </summary>
    public class GameMathCharacterizationTests
    {
        // level -> cumulative experience required, captured from the live editor.
        private static readonly object[] ExperienceForLevelCases =
        {
            new object[] {   1,        0d },
            new object[] {   2,      201d },
            new object[] {   5,      504d },
            new object[] {  10,     1009d },
            new object[] {  50,    17249d },
            new object[] { 100,    85249d },
            new object[] { 150,   263999d },
            new object[] { 200,   650999d },
            new object[] { 250,  1381249d },
            new object[] { 300,  2627249d },
            new object[] { 350,  4598999d },
            new object[] { 380,  6231809d },
            new object[] { 400,  7543999d },
        };

        [TestCaseSource(nameof(ExperienceForLevelCases))]
        public void ExperienceForLevel_MatchesShippedCurve(int level, double expected)
        {
            Assert.AreEqual(expected, GameMath.ExperienceForLevel(level), 0d,
                $"XP curve changed at level {level}. If this was intentional, update the expected value.");
        }

        [Test]
        public void ExperienceForLevel_IsMonotonicallyIncreasing()
        {
            var previous = GameMath.ExperienceForLevel(1);
            for (var level = 2; level <= 400; level++)
            {
                var current = GameMath.ExperienceForLevel(level);
                Assert.GreaterOrEqual(current, previous,
                    $"XP required decreased going from level {level - 1} to {level}.");
                previous = current;
            }
        }

        // level -> minutes to reach it, captured from the live editor.
        private static readonly object[] MaxMinutesCases =
        {
            new object[] {   1,    0d },
            new object[] {  10,    4.073142857142857d },
            new object[] { 100,  103.35600000000001d },
            new object[] { 300,  705.12742857142848d },
            new object[] { 400, 1203.156d },
        };

        [TestCaseSource(nameof(MaxMinutesCases))]
        public void GetMaxMinutesForLevel_MatchesShippedCurve(int level, double expected)
        {
            Assert.AreEqual(expected, GameMath.Exp.GetMaxMinutesForLevel(level), 1e-9,
                $"Training time curve changed at level {level}.");
        }
    }
}
