using System;
using UnityEngine;

namespace Obscured
{
    [Serializable]
    public struct ObscuredDouble : ISerializationCallbackReceiver, IEquatable<ObscuredDouble>, IEquatable<double>, IObscured
    {
        [SerializeField] private long encrypted; // raw bits XOR key
        [SerializeField] private long key;
        [SerializeField] private long fakeBits;
        [SerializeField] private bool inited;

        public ObscuredDouble(double value)
        {
            key = SecureRng.NextNonZeroLong();
            long bits = BitConverter.DoubleToInt64Bits(value);
            encrypted = bits ^ key;
            fakeBits  = bits ^ ~key;
            inited = true;
        }

        private double Decrypt()
        {
            if (!inited || key == 0)
            {
                key = SecureRng.NextNonZeroLong();
                encrypted = 0 ^ key;
                fakeBits = 0 ^ ~key;
                inited = true;
                return 0d;
            }
            long bits = encrypted ^ key;
            long mirror = fakeBits ^ ~key;
            if (mirror != bits) TamperDetector.Raise(nameof(ObscuredDouble), "Mirror mismatch");
            return BitConverter.Int64BitsToDouble(bits);
        }

        private void Encrypt(double v)
        {
            if (!inited || key == 0)
            {
                key = SecureRng.NextNonZeroLong();
                inited = true;
            }
            long bits = BitConverter.DoubleToInt64Bits(v);
            encrypted = bits ^ key;
            fakeBits = bits ^ ~key;
        }

        public void RandomizeKey()
        {
            var v = Decrypt();
            key = SecureRng.NextNonZeroLong();
            Encrypt(v);
        }

        public static implicit operator ObscuredDouble(double v) => new ObscuredDouble(v);
        public static implicit operator double(ObscuredDouble v) => v.Decrypt();

        public static ObscuredDouble operator +(ObscuredDouble a, double b) { var v = a.Decrypt() + b; a.Encrypt(v); return a; }
        public static ObscuredDouble operator -(ObscuredDouble a, double b) { var v = a.Decrypt() - b; a.Encrypt(v); return a; }
        public static ObscuredDouble operator *(ObscuredDouble a, double b) { var v = a.Decrypt() * b; a.Encrypt(v); return a; }
        public static ObscuredDouble operator /(ObscuredDouble a, double b) { var v = a.Decrypt() / b; a.Encrypt(v); return a; }
        public static ObscuredDouble operator ++(ObscuredDouble a) { var v = a.Decrypt() + 1d; a.Encrypt(v); return a; }
        public static ObscuredDouble operator --(ObscuredDouble a) { var v = a.Decrypt() - 1d; a.Encrypt(v); return a; }

        public bool Equals(ObscuredDouble other) => Decrypt().Equals(other.Decrypt());
        public bool Equals(double other) => Decrypt().Equals(other);
        public override bool Equals(object obj) => obj is ObscuredDouble o && Equals(o) || obj is double d && Equals(d);
        public override int GetHashCode() => Decrypt().GetHashCode();

        public void OnBeforeSerialize() {}
        public void OnAfterDeserialize()
        {
            if (!inited || key == 0)
            {
                key = SecureRng.NextNonZeroLong();
                inited = true;
                Encrypt(0d);
            }
        }

        public void SetRaw(object value) => Encrypt(Convert.ToDouble(value));
        public object GetRaw() => Decrypt();

        public override string ToString() => Decrypt().ToString();
    }
}
