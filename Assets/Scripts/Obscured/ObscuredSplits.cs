// Assets/Obscured/ObscuredSplit.cs
using System;
using System.Runtime.Serialization;
using UnityEngine;

#if UNITY_NETCODE
using Unity.Netcode;
#endif

namespace Obscured
{
    // ---------------------------
    // Helpers
    // ---------------------------
    internal static class SplitHash
    {
        // fast 32-bit mix
        public static int Mix32(int a, int b, int c, int d)
        {
            unchecked
            {
                uint x = (uint)a;
                x ^= (uint)b + 0x9E3779B9u + (x << 6) + (x >> 2);
                x ^= (uint)c + 0x85EBCA6Bu + (x << 13) + (x >> 7);
                x ^= (uint)d; x ^= x >> 16; x *= 0x7FEB352Du; x ^= x >> 15; x *= 0x846CA68Bu; x ^= x >> 16;
                return (int)x;
            }
        }

        // fast 64-bit mix
        public static long Mix64(long a, long b, int s)
        {
            unchecked
            {
                ulong x = (ulong)a;
                x ^= (ulong)b + 0x9E3779B97F4A7C15UL + (x << 6) + (x >> 2);
                x ^= (uint)s;
                x ^= x >> 30; x *= 0xBF58476D1CE4E5B9UL;
                x ^= x >> 27; x *= 0x94D049BB133111EBUL;
                x ^= x >> 31;
                return (long)x;
            }
        }
    }

    // ============================================================
    // ObscuredIntSplit
    // ============================================================
    [Serializable]
    public struct ObscuredIntSplit :
#if UNITY_NETCODE
        INetworkSerializable ,
#endif
    ISerializationCallbackReceiver, IEquatable<ObscuredIntSplit>, IEquatable<int>, ISerializable
    {
        [SerializeField] private int partA;
        [SerializeField] private int partB;
        [SerializeField] private int checksum;
        [SerializeField] private int salt;
        [SerializeField] private bool inited;

        // auto-rekey counters (tiny)
        [SerializeField] private byte opCounter;
        [SerializeField] private byte opsToReseal;

        public ObscuredIntSplit(int value)
        {
            salt = SecureRng.NextNonZeroInt();
            partA = SecureRng.NextNonZeroInt();
            partB = unchecked(value - partA);
            checksum = SplitHash.Mix32(partA, partB, salt, 0x5A17);
            inited = true;
            opCounter = 0;
            opsToReseal = (byte)(4 + (salt & 0x0F)); // 4..19
        }

        // ISerializable (BinaryFormatter)
        private ObscuredIntSplit(SerializationInfo info, StreamingContext ctx)
        {
            partA = info.GetInt32("a");
            partB = info.GetInt32("b");
            checksum = info.GetInt32("c");
            salt = info.GetInt32("s");
            inited = info.GetBoolean("i");
            opCounter = info.GetByte("oc");
            opsToReseal = info.GetByte("or");
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("a", partA);
            info.AddValue("b", partB);
            info.AddValue("c", checksum);
            info.AddValue("s", salt);
            info.AddValue("i", inited);
            info.AddValue("oc", opCounter);
            info.AddValue("or", opsToReseal);
        }

        private void EnsureInit()
        {
            if (inited) return;
            salt = SecureRng.NextNonZeroInt();
            partA = 0; partB = 0;
            checksum = SplitHash.Mix32(partA, partB, salt, 0x5A17);
            inited = true;
            opCounter = 0;
            opsToReseal = (byte)(4 + (salt & 0x0F));
        }

        private int Reconstruct()
        {
            EnsureInit();
            if (checksum != SplitHash.Mix32(partA, partB, salt, 0x5A17))
                TamperDetector.Raise(nameof(ObscuredIntSplit), "Checksum failure");
            return unchecked(partA + partB);
        }

        private void Set(int value, bool countOp = false)
        {
            EnsureInit();
            partA = SecureRng.NextNonZeroInt();
            partB = unchecked(value - partA);
            checksum = SplitHash.Mix32(partA, partB, salt, 0x5A17);
            if (countOp) AfterMutation(value);
        }

        private void AfterMutation(int current)
        {
            if (++opCounter >= opsToReseal)
            {
                // reseal with new salt and plan
                salt = SecureRng.NextNonZeroInt();
                opCounter = 0;
                opsToReseal = (byte)(4 + (salt & 0x0F));
                // re-scramble with new salt
                partA = SecureRng.NextNonZeroInt();
                partB = unchecked(current - partA);
                checksum = SplitHash.Mix32(partA, partB, salt, 0x5A17);
            }
        }

        public void RandomizeSalt()
        {
            var v = Reconstruct();
            salt = SecureRng.NextNonZeroInt();
            opCounter = 0;
            opsToReseal = (byte)(4 + (salt & 0x0F));
            Set(v);
        }

        // Operators
        public static implicit operator ObscuredIntSplit(int v) => new ObscuredIntSplit(v);
        public static implicit operator int(ObscuredIntSplit v) => v.Reconstruct();

        public static ObscuredIntSplit operator +(ObscuredIntSplit a, int b) { var v = unchecked(a.Reconstruct() + b); a.Set(v, true); return a; }
        public static ObscuredIntSplit operator -(ObscuredIntSplit a, int b) { var v = unchecked(a.Reconstruct() - b); a.Set(v, true); return a; }
        public static ObscuredIntSplit operator ++(ObscuredIntSplit a) { var v = unchecked(a.Reconstruct() + 1); a.Set(v, true); return a; }
        public static ObscuredIntSplit operator --(ObscuredIntSplit a) { var v = unchecked(a.Reconstruct() - 1); a.Set(v, true); return a; }

        public bool Equals(ObscuredIntSplit other) => Reconstruct() == other.Reconstruct();
        public bool Equals(int other) => Reconstruct() == other;
        public override bool Equals(object obj) => obj is ObscuredIntSplit o && Equals(o) || obj is int i && Equals(i);
        public override int GetHashCode() => Reconstruct().GetHashCode();

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { if (!inited) EnsureInit(); }

#if UNITY_NETCODE
        // NGO serialization
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref partA);
            serializer.SerializeValue(ref partB);
            serializer.SerializeValue(ref checksum);
            serializer.SerializeValue(ref salt);
            serializer.SerializeValue(ref inited);
            serializer.SerializeValue(ref opCounter);
            serializer.SerializeValue(ref opsToReseal);
        }
#endif

        public override string ToString() => Reconstruct().ToString();
    }

    // ============================================================
    // ObscuredLongSplit
    // ============================================================
    [Serializable]
    public struct ObscuredLongSplit :
#if UNITY_NETCODE
        INetworkSerializable ,
#endif
    ISerializationCallbackReceiver, IEquatable<ObscuredLongSplit>, IEquatable<long>, ISerializable
    {
        [SerializeField] private long partA;
    [SerializeField] private long partB;
    [SerializeField] private long checksum;
    [SerializeField] private int salt;
    [SerializeField] private bool inited;

    [SerializeField] private byte opCounter;
    [SerializeField] private byte opsToReseal;

    public ObscuredLongSplit(long value)
    {
        salt = SecureRng.NextNonZeroInt();
        partA = SecureRng.NextNonZeroLong();
        partB = unchecked(value - partA);
        checksum = SplitHash.Mix64(partA, partB, salt);
        inited = true;
        opCounter = 0;
        opsToReseal = (byte)(4 + (salt & 0x0F));
    }

    // ISerializable
    private ObscuredLongSplit(SerializationInfo info, StreamingContext ctx)
    {
        partA = info.GetInt64("a");
        partB = info.GetInt64("b");
        checksum = info.GetInt64("c");
        salt = info.GetInt32("s");
        inited = info.GetBoolean("i");
        opCounter = info.GetByte("oc");
        opsToReseal = info.GetByte("or");
    }
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("a", partA);
        info.AddValue("b", partB);
        info.AddValue("c", checksum);
        info.AddValue("s", salt);
        info.AddValue("i", inited);
        info.AddValue("oc", opCounter);
        info.AddValue("or", opsToReseal);
    }

    private void EnsureInit()
    {
        if (inited) return;
        salt = SecureRng.NextNonZeroInt();
        partA = 0; partB = 0;
        checksum = SplitHash.Mix64(partA, partB, salt);
        inited = true;
        opCounter = 0;
        opsToReseal = (byte)(4 + (salt & 0x0F));
    }

    private long Reconstruct()
    {
        EnsureInit();
        if (checksum != SplitHash.Mix64(partA, partB, salt))
            TamperDetector.Raise(nameof(ObscuredLongSplit), "Checksum failure");
        return unchecked(partA + partB);
    }

    private void Set(long value, bool countOp = false)
    {
        EnsureInit();
        partA = SecureRng.NextNonZeroLong();
        partB = unchecked(value - partA);
        checksum = SplitHash.Mix64(partA, partB, salt);
        if (countOp) AfterMutation(value);
    }

    private void AfterMutation(long current)
    {
        if (++opCounter >= opsToReseal)
        {
            salt = SecureRng.NextNonZeroInt();
            opCounter = 0;
            opsToReseal = (byte)(4 + (salt & 0x0F));
            partA = SecureRng.NextNonZeroLong();
            partB = unchecked(current - partA);
            checksum = SplitHash.Mix64(partA, partB, salt);
        }
    }

    public void RandomizeSalt()
    {
        var v = Reconstruct();
        salt = SecureRng.NextNonZeroInt();
        opCounter = 0;
        opsToReseal = (byte)(4 + (salt & 0x0F));
        Set(v);
    }

    public static implicit operator ObscuredLongSplit(long v) => new ObscuredLongSplit(v);
    public static implicit operator long(ObscuredLongSplit v) => v.Reconstruct();

    public static ObscuredLongSplit operator +(ObscuredLongSplit a, long b) { var v = unchecked(a.Reconstruct() + b); a.Set(v, true); return a; }
    public static ObscuredLongSplit operator -(ObscuredLongSplit a, long b) { var v = unchecked(a.Reconstruct() - b); a.Set(v, true); return a; }
    public static ObscuredLongSplit operator *(ObscuredLongSplit a, long b) { var v = unchecked(a.Reconstruct() * b); a.Set(v, true); return a; }
    public static ObscuredLongSplit operator /(ObscuredLongSplit a, long b) { var v = a.Reconstruct() / b; a.Set(v, true); return a; }
    public static ObscuredLongSplit operator %(ObscuredLongSplit a, long b) { var v = a.Reconstruct() % b; a.Set(v, true); return a; }
    public static ObscuredLongSplit operator ++(ObscuredLongSplit a) { var v = unchecked(a.Reconstruct() + 1L); a.Set(v, true); return a; }
    public static ObscuredLongSplit operator --(ObscuredLongSplit a) { var v = unchecked(a.Reconstruct() - 1L); a.Set(v, true); return a; }

    public bool Equals(ObscuredLongSplit other) => Reconstruct() == other.Reconstruct();
    public bool Equals(long other) => Reconstruct() == other;
    public override bool Equals(object obj) => obj is ObscuredLongSplit o && Equals(o) || obj is long l && Equals(l);
    public override int GetHashCode() => Reconstruct().GetHashCode();

    public void OnBeforeSerialize() { }
    public void OnAfterDeserialize() { if (!inited) EnsureInit(); }

#if UNITY_NETCODE
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref partA);
            serializer.SerializeValue(ref partB);
            serializer.SerializeValue(ref checksum);
            serializer.SerializeValue(ref salt);
            serializer.SerializeValue(ref inited);
            serializer.SerializeValue(ref opCounter);
            serializer.SerializeValue(ref opsToReseal);
        }
#endif

    public override string ToString() => Reconstruct().ToString();
}

// ============================================================
// ObscuredFloatSplit
// ============================================================
[Serializable]
public struct ObscuredFloatSplit :
#if UNITY_NETCODE
        INetworkSerializable ,
#endif
    ISerializationCallbackReceiver, IEquatable<ObscuredFloatSplit>, IEquatable<float>, ISerializable
    {
        [SerializeField] private int partA;     // XOR-split of raw float bits
[SerializeField] private int partB;
[SerializeField] private int xorKey;
[SerializeField] private int checksum;
[SerializeField] private int salt;
[SerializeField] private bool inited;

[SerializeField] private byte opCounter;
[SerializeField] private byte opsToReseal;

public ObscuredFloatSplit(float value)
{
    salt = SecureRng.NextNonZeroInt();
    xorKey = SecureRng.NextNonZeroInt();
    int bits = BitConverter.SingleToInt32Bits(value) ^ xorKey;
    partA = SecureRng.NextNonZeroInt();
    partB = bits ^ partA;
    checksum = SplitHash.Mix32(partA, partB, xorKey, salt);
    inited = true;
    opCounter = 0;
    opsToReseal = (byte)(4 + (salt & 0x0F));
}

// ISerializable
private ObscuredFloatSplit(SerializationInfo info, StreamingContext ctx)
{
    partA = info.GetInt32("a");
    partB = info.GetInt32("b");
    xorKey = info.GetInt32("k");
    checksum = info.GetInt32("c");
    salt = info.GetInt32("s");
    inited = info.GetBoolean("i");
    opCounter = info.GetByte("oc");
    opsToReseal = info.GetByte("or");
}
public void GetObjectData(SerializationInfo info, StreamingContext context)
{
    info.AddValue("a", partA);
    info.AddValue("b", partB);
    info.AddValue("k", xorKey);
    info.AddValue("c", checksum);
    info.AddValue("s", salt);
    info.AddValue("i", inited);
    info.AddValue("oc", opCounter);
    info.AddValue("or", opsToReseal);
}

private void EnsureInit()
{
    if (inited) return;
    salt = SecureRng.NextNonZeroInt();
    xorKey = SecureRng.NextNonZeroInt();
    partA = 0; partB = 0;
    checksum = SplitHash.Mix32(partA, partB, xorKey, salt);
    inited = true;
    opCounter = 0;
    opsToReseal = (byte)(4 + (salt & 0x0F));
}

private float Reconstruct()
{
    EnsureInit();
    if (checksum != SplitHash.Mix32(partA, partB, xorKey, salt))
        TamperDetector.Raise(nameof(ObscuredFloatSplit), "Checksum failure");

    int bitsXor = (partA ^ partB);
    int bits = bitsXor ^ xorKey;
    return BitConverter.Int32BitsToSingle(bits);
}

private void Set(float value, bool countOp = false)
{
    EnsureInit();
    xorKey = SecureRng.NextNonZeroInt();
    int bits = BitConverter.SingleToInt32Bits(value) ^ xorKey;
    partA = SecureRng.NextNonZeroInt();
    partB = bits ^ partA;
    checksum = SplitHash.Mix32(partA, partB, xorKey, salt);
    if (countOp) AfterMutation(value);
}

private void AfterMutation(float current)
{
    if (++opCounter >= opsToReseal)
    {
        salt = SecureRng.NextNonZeroInt();
        opCounter = 0;
        opsToReseal = (byte)(4 + (salt & 0x0F));

        xorKey = SecureRng.NextNonZeroInt();
        int bits = BitConverter.SingleToInt32Bits(current) ^ xorKey;
        partA = SecureRng.NextNonZeroInt();
        partB = bits ^ partA;
        checksum = SplitHash.Mix32(partA, partB, xorKey, salt);
    }
}

public void RandomizeSaltAndKey()
{
    float v = Reconstruct();
    salt = SecureRng.NextNonZeroInt();
    opCounter = 0;
    opsToReseal = (byte)(4 + (salt & 0x0F));
    Set(v);
}

public static implicit operator ObscuredFloatSplit(float v) => new ObscuredFloatSplit(v);
public static implicit operator float(ObscuredFloatSplit v) => v.Reconstruct();

public static ObscuredFloatSplit operator +(ObscuredFloatSplit a, float b) { var v = a.Reconstruct() + b; a.Set(v, true); return a; }
public static ObscuredFloatSplit operator -(ObscuredFloatSplit a, float b) { var v = a.Reconstruct() - b; a.Set(v, true); return a; }
public static ObscuredFloatSplit operator *(ObscuredFloatSplit a, float b) { var v = a.Reconstruct() * b; a.Set(v, true); return a; }
public static ObscuredFloatSplit operator /(ObscuredFloatSplit a, float b) { var v = a.Reconstruct() / b; a.Set(v, true); return a; }
public static ObscuredFloatSplit operator ++(ObscuredFloatSplit a) { var v = a.Reconstruct() + 1f; a.Set(v, true); return a; }
public static ObscuredFloatSplit operator --(ObscuredFloatSplit a) { var v = a.Reconstruct() - 1f; a.Set(v, true); return a; }

public bool Equals(ObscuredFloatSplit other) => Reconstruct().Equals(other.Reconstruct());
public bool Equals(float other) => Reconstruct().Equals(other);
public override bool Equals(object obj) => obj is ObscuredFloatSplit o && Equals(o) || obj is float f && Equals(f);
public override int GetHashCode() => Reconstruct().GetHashCode();

public void OnBeforeSerialize() { }
public void OnAfterDeserialize() { if (!inited) EnsureInit(); }

#if UNITY_NETCODE
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref partA);
            serializer.SerializeValue(ref partB);
            serializer.SerializeValue(ref xorKey);
            serializer.SerializeValue(ref checksum);
            serializer.SerializeValue(ref salt);
            serializer.SerializeValue(ref inited);
            serializer.SerializeValue(ref opCounter);
            serializer.SerializeValue(ref opsToReseal);
        }
#endif

public override string ToString() => Reconstruct().ToString();
    }
}
