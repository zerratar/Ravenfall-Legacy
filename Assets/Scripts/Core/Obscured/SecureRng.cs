using System.Security.Cryptography;

namespace Obscured
{
    /// <summary>
    /// The keys the obscured values hide behind. They only have to be unpredictable to someone reading the game's
    /// memory, so they come from a fast xorshift generator per thread, seeded from the cryptographic generator and
    /// seeded again every 4096 keys. Every arithmetic step on an obscured value takes a new key: asking the
    /// cryptographic generator each time, with a new buffer, was a measurable part of the frame with many players
    /// (and most of its garbage).
    /// </summary>
    public static class SecureRng
    {
        private const int KeysPerSeed = 4096;

        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        [System.ThreadStatic] private static ulong state0;
        [System.ThreadStatic] private static ulong state1;
        [System.ThreadStatic] private static int left;
        [System.ThreadStatic] private static byte[] seed;

        public static int NextNonZeroInt()
        {
            int value;
            do
            {
                value = (int)Next();
            } while (value == 0);
            return value;
        }

        public static long NextNonZeroLong()
        {
            long value;
            do
            {
                value = (long)Next();
            } while (value == 0);
            return value;
        }

        // xorshift128+
        private static ulong Next()
        {
            if (left <= 0) Reseed();
            left--;
            var s1 = state0;
            var s0 = state1;
            state0 = s0;
            s1 ^= s1 << 23;
            state1 = s1 ^ s0 ^ (s1 >> 17) ^ (s0 >> 26);
            return state1 + s0;
        }

        private static void Reseed()
        {
            seed ??= new byte[16];
            lock (Rng)
            {
                Rng.GetBytes(seed);
            }
            state0 = System.BitConverter.ToUInt64(seed, 0);
            state1 = System.BitConverter.ToUInt64(seed, 8);
            if (state0 == 0 && state1 == 0) state1 = 0x9E3779B97F4A7C15UL;
            left = KeysPerSeed;
        }
    }
}
