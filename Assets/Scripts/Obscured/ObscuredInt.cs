using System;
using UnityEngine;

namespace Obscured
{
    [Serializable]
    public struct ObscuredInt : ISerializationCallbackReceiver, IEquatable<ObscuredInt>, IEquatable<int>, IObscured
    {
        [SerializeField] private int encrypted;
        [SerializeField] private int key;
        [SerializeField] private int fakeValue;     // optional mirror for tamper check (obfuscated via ~key)
        [SerializeField] private bool inited;

        public ObscuredInt(int value)
        {
            key        = SecureRng.NextNonZeroInt();
            encrypted  = value ^ key;
            fakeValue  = (value ^ ~key);
            inited     = true;
        }

        private int Decrypt()
        {
            if (!inited || key == 0)
            {
                key = SecureRng.NextNonZeroInt();
                encrypted = 0 ^ key;
                fakeValue = (0 ^ ~key);
                inited = true;
                return 0;
            }

            int v = encrypted ^ key;

            // Tamper check: ensure our mirror matches
            int mirror = fakeValue ^ ~key;
            if (mirror != v)
            {
                TamperDetector.Raise(nameof(ObscuredInt), "Mirror mismatch");
            }
            return v;
        }

        private void Encrypt(int value)
        {
            if (!inited || key == 0)
            {
                key = SecureRng.NextNonZeroInt();
                inited = true;
            }
            encrypted = value ^ key;
            fakeValue = (value ^ ~key);
        }

        public void RandomizeKey()
        {
            int v = Decrypt();
            key = SecureRng.NextNonZeroInt();
            Encrypt(v);
        }

        // Operators/Conversions
        public static implicit operator ObscuredInt(int value) => new ObscuredInt(value);
        public static implicit operator int(ObscuredInt value) => value.Decrypt();

        public static ObscuredInt operator +(ObscuredInt a, int b) { var v = a.Decrypt() + b; a.Encrypt(v); return a; }
        public static ObscuredInt operator -(ObscuredInt a, int b) { var v = a.Decrypt() - b; a.Encrypt(v); return a; }
        public static ObscuredInt operator *(ObscuredInt a, int b) { var v = a.Decrypt() * b; a.Encrypt(v); return a; }
        public static ObscuredInt operator /(ObscuredInt a, int b) { var v = a.Decrypt() / b; a.Encrypt(v); return a; }
        public static ObscuredInt operator ++(ObscuredInt a) { var v = a.Decrypt() + 1; a.Encrypt(v); return a; }
        public static ObscuredInt operator --(ObscuredInt a) { var v = a.Decrypt() - 1; a.Encrypt(v); return a; }

        // Comparisons
        public bool Equals(ObscuredInt other) => Decrypt() == other.Decrypt();
        public bool Equals(int other) => Decrypt() == other;
        public override bool Equals(object obj) => obj is ObscuredInt o && Equals(o) || obj is int i && Equals(i);
        public override int GetHashCode() => Decrypt().GetHashCode();

        // Serialization hooks (Unity inspector safety)
        public void OnBeforeSerialize() { /* keep encrypted */ }
        public void OnAfterDeserialize()
        {
            // if someone edited serialized fields by hand, this will trip at first access
            if (!inited || key == 0)
            {
                key = SecureRng.NextNonZeroInt();
                inited = true;
                Encrypt(0);
            }
        }

        // IObscured
        public void SetRaw(object value) => Encrypt(Convert.ToInt32(value));
        public object GetRaw() => Decrypt();

        public override string ToString() => Decrypt().ToString();
    }
}
