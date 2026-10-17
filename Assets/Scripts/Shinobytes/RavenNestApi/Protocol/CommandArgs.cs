// Copied from RavenNest/src/RavenNest.Protocol by tools/sync-protocol.ps1. Change it there, not here.
using System;
using System.Buffers;
using MessagePack;

namespace RavenNest.Protocol
{
    /// <summary>
    /// Writes and reads a command's arguments as a MessagePack array, each with its own type. Reading tolerates
    /// missing trailing arguments (they get their default), so a command can gain an optional argument later
    /// without breaking older clients.
    /// </summary>
    public static class CommandArgs
    {
        private static MessagePackSerializerOptions Options => ProtocolSerializer.Values;

        public static byte[] Pack()
        {
            return Write(0, (ref MessagePackWriter w) => { });
        }

        public static byte[] Pack<T1>(T1 a)
        {
            return Write(1, (ref MessagePackWriter w) => Put(ref w, a));
        }

        public static byte[] Pack<T1, T2>(T1 a, T2 b)
        {
            return Write(2, (ref MessagePackWriter w) => { Put(ref w, a); Put(ref w, b); });
        }

        public static byte[] Pack<T1, T2, T3>(T1 a, T2 b, T3 c)
        {
            return Write(3, (ref MessagePackWriter w) => { Put(ref w, a); Put(ref w, b); Put(ref w, c); });
        }

        public static byte[] Pack<T1, T2, T3, T4>(T1 a, T2 b, T3 c, T4 d)
        {
            return Write(4, (ref MessagePackWriter w) => { Put(ref w, a); Put(ref w, b); Put(ref w, c); Put(ref w, d); });
        }

        public static void Unpack<T1>(byte[] data, out T1 a)
        {
            var r = Open(data, out var count);
            a = Take<T1>(ref r, ref count);
        }

        public static void Unpack<T1, T2>(byte[] data, out T1 a, out T2 b)
        {
            var r = Open(data, out var count);
            a = Take<T1>(ref r, ref count);
            b = Take<T2>(ref r, ref count);
        }

        public static void Unpack<T1, T2, T3>(byte[] data, out T1 a, out T2 b, out T3 c)
        {
            var r = Open(data, out var count);
            a = Take<T1>(ref r, ref count);
            b = Take<T2>(ref r, ref count);
            c = Take<T3>(ref r, ref count);
        }

        public static void Unpack<T1, T2, T3, T4>(byte[] data, out T1 a, out T2 b, out T3 c, out T4 d)
        {
            var r = Open(data, out var count);
            a = Take<T1>(ref r, ref count);
            b = Take<T2>(ref r, ref count);
            c = Take<T3>(ref r, ref count);
            d = Take<T4>(ref r, ref count);
        }

        private delegate void WriteArgs(ref MessagePackWriter writer);

        private static byte[] Write(int count, WriteArgs write)
        {
            var buffer = new ArrayBufferWriter<byte>(64);
            var writer = new MessagePackWriter(buffer);
            writer.WriteArrayHeader(count);
            write(ref writer);
            writer.Flush();
            return buffer.WrittenSpan.ToArray();
        }

        private static void Put<T>(ref MessagePackWriter writer, T value)
        {
            Options.Resolver.GetFormatterWithVerify<T>().Serialize(ref writer, value, Options);
        }

        private static MessagePackReader Open(byte[] data, out int count)
        {
            var reader = new MessagePackReader(new ReadOnlyMemory<byte>(data ?? Array.Empty<byte>()));
            count = data == null || data.Length == 0 ? 0 : reader.ReadArrayHeader();
            return reader;
        }

        private static T Take<T>(ref MessagePackReader reader, ref int count)
        {
            if (count <= 0)
            {
                return default;
            }
            count--;
            return Options.Resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, Options);
        }
    }
}
