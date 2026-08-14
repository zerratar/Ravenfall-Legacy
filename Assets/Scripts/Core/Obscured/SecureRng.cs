using System.Security.Cryptography;

namespace Obscured
{
    public static class SecureRng
    {
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public static int NextNonZeroInt()
        {
            int value;
            var bytes = new byte[4];
            do
            {
                Rng.GetBytes(bytes);
                value = System.BitConverter.ToInt32(bytes, 0);
            } while (value == 0);
            return value;
        }

        public static long NextNonZeroLong()
        {
            long value;
            var bytes = new byte[8];
            do
            {
                Rng.GetBytes(bytes);
                value = System.BitConverter.ToInt64(bytes, 0);
            } while (value == 0);
            return value;
        }
    }
}
