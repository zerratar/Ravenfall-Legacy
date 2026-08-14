using System;
using UnityEngine;

namespace Obscured
{
    [Serializable]
    public struct ObscuredLong : ISerializationCallbackReceiver, IEquatable<ObscuredLong>, IEquatable<long>, IObscured
    {
        [SerializeField] private long encrypted;
        [SerializeField] private long key;
        [SerializeField] private long fakeValue;
        [SerializeField] private bool inited;

        public ObscuredLong(long value)
        {
            key        = SecureRng.NextNonZeroLong();
            encrypted  = value ^ key;
            fakeValue  = (value ^ ~key);
            inited     = true;
        }

        private long Decrypt()
        {
            if (!inited || key == 0)
            {
                key = SecureRng.NextNonZeroLong();
                encrypted = 0 ^ key;
                fakeValue = (0 ^ ~key);
                inited = true;
                return 0L;
            }

            long v = encrypted ^ key;
            long mirror = fakeValue ^ ~key;
            if (mirror != v) TamperDetector.Raise(nameof(ObscuredLong), "Mirror mismatch");
            return v;
        }

        private void Encrypt(long v)
        {
            if (!inited || key == 0)
            {
                key = SecureRng.NextNonZeroLong();
                inited = true;
            }
            encrypted = v ^ key;
            fakeValue = (v ^ ~key);
        }

        public void RandomizeKey()
        {
            var v = Decrypt();
            key = SecureRng.NextNonZeroLong();
            Encrypt(v);
        }

        public static implicit operator ObscuredLong(long v) => new ObscuredLong(v);
        public static implicit operator long(ObscuredLong v) => v.Decrypt();

        public static ObscuredLong operator +(ObscuredLong a, long b) { var v = a.Decrypt() + b; a.Encrypt(v); return a; }
        public static ObscuredLong operator -(ObscuredLong a, long b) { var v = a.Decrypt() - b; a.Encrypt(v); return a; }
        public static ObscuredLong operator *(ObscuredLong a, long b) { var v = a.Decrypt() * b; a.Encrypt(v); return a; }
        public static ObscuredLong operator /(ObscuredLong a, long b) { var v = a.Decrypt() / b; a.Encrypt(v); return a; }
        public static ObscuredLong operator ++(ObscuredLong a) { var v = a.Decrypt() + 1; a.Encrypt(v); return a; }
        public static ObscuredLong operator --(ObscuredLong a) { var v = a.Decrypt() - 1; a.Encrypt(v); return a; }

        public bool Equals(ObscuredLong other) => Decrypt() == other.Decrypt();
        public bool Equals(long other) => Decrypt() == other;
        public override bool Equals(object obj) => obj is ObscuredLong o && Equals(o) || obj is long l && Equals(l);
        public override int GetHashCode() => Decrypt().GetHashCode();

        public void OnBeforeSerialize() {}
        public void OnAfterDeserialize()
        {
            if (!inited || key == 0)
            {
                key = SecureRng.NextNonZeroLong();
                inited = true;
                Encrypt(0L);
            }
        }

        public void SetRaw(object value) => Encrypt(Convert.ToInt64(value));
        public object GetRaw() => Decrypt();

        public override string ToString() => Decrypt().ToString();
    }
}
