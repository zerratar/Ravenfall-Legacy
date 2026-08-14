using System;

namespace RavenfallDataPipe
{
    /// <summary>
    /// Pure string-distance helpers used by the query engine when resolving a
    /// misspelled table name to the closest match.
    /// <para>
    /// This lives here rather than in the game assembly so PipeSQL stays free of
    /// game dependencies. <c>ItemResolver.LevenshteinDistance</c> delegates to it,
    /// so there is still only one implementation.
    /// </para>
    /// </summary>
    public static class StringDistance
    {
        /// <summary>
        /// Number of single-character edits (insert, delete, substitute) needed to
        /// turn <paramref name="s"/> into <paramref name="t"/>.
        /// </summary>
        public static int Levenshtein(string s, string t)
        {
            int[,] d = new int[s.Length + 1, t.Length + 1];

            for (int i = 0; i <= s.Length; i++)
            {
                d[i, 0] = i;
            }

            for (int j = 0; j <= t.Length; j++)
            {
                d[0, j] = j;
            }

            for (int j = 1; j <= t.Length; j++)
            {
                for (int i = 1; i <= s.Length; i++)
                {
                    if (s[i - 1] == t[j - 1])
                    {
                        d[i, j] = d[i - 1, j - 1];
                    }
                    else
                    {
                        d[i, j] = Math.Min(d[i - 1, j] + 1, Math.Min(d[i, j - 1] + 1, d[i - 1, j - 1] + 1));
                    }
                }
            }

            return d[s.Length, t.Length];
        }
    }
}
