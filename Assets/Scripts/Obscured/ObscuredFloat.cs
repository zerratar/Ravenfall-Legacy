using System;
using UnityEngine;

namespace Obscured
{
    [Serializable]
    public struct ObscuredFloat : ISerializationCallbackReceiver, IEquatable<ObscuredFloat>, IEquatable<float>, IObscured
    {
        [SerializeField] private int encrypted;  // store raw bits XOR key
        [SerializeField] private int key;
        [SerializeField] private int fakeBits;   // mirror check
        [SerializeField] private bool inited;

        public ObscuredFloat(float value)
        {
            key = SecureRng.NextNonZeroInt();
            int bits = BitConverter.SingleToInt32Bits(value);
            encrypted = bits ^ key;
            fakeBits  = bits ^ ~key;
            inited = true;
        }

        private float Decrypt()
        {
            if (!inited || key == 0)
            {
                key = SecureRng.NextNonZeroInt();
                encrypted = 0 ^ key;
                fakeBits  = 0 ^ ~key;
                inited = true;
                return 0f;
            }
            int bits = encrypted ^ key;
            int mirror = fakeBits ^ ~key;
            if (mirror != bits) TamperDetector.Raise(nameof(ObscuredFloat), "Mirror mismatch");
            return BitConverter.Int32BitsToSingle(bits);
        }

        private void Encrypt(float v)
        {
            if (!inited || key == 0)
            {
                key = SecureRng.NextNonZeroInt();
                inited = true;
            }
            int bits = BitConverter.SingleToInt32Bits(v);
            encrypted = bits ^ key;
            fakeBits  = bits ^ ~key;
        }

        public void RandomizeKey()
        {
            var v = Decrypt();
            key = SecureRng.NextNonZeroInt();
            Encrypt(v);
        }

        public static implicit operator ObscuredFloat(float v) => new ObscuredFloat(v);
        public static implicit operator float(ObscuredFloat v) => v.Decrypt();

        public static ObscuredFloat operator +(ObscuredFloat a, float b) { var v = a.Decrypt() + b; a.Encrypt(v); return a; }
        public static ObscuredFloat operator -(ObscuredFloat a, float b) { var v = a.Decrypt() - b; a.Encrypt(v); return a; }
        public static ObscuredFloat operator *(ObscuredFloat a, float b) { var v = a.Decrypt() * b; a.Encrypt(v); return a; }
        public static ObscuredFloat operator /(ObscuredFloat a, float b) { var v = a.Decrypt() / b; a.Encrypt(v); return a; }
        public static ObscuredFloat operator ++(ObscuredFloat a) { var v = a.Decrypt() + 1f; a.Encrypt(v); return a; }
        public static ObscuredFloat operator --(ObscuredFloat a) { var v = a.Decrypt() - 1f; a.Encrypt(v); return a; }

        public bool Equals(ObscuredFloat other) => Decrypt().Equals(other.Decrypt());
        public bool Equals(float other) => Decrypt().Equals(other);
        public override bool Equals(object obj) => obj is ObscuredFloat o && Equals(o) || obj is float f && Equals(f);
        public override int GetHashCode() => Decrypt().GetHashCode();

        public void OnBeforeSerialize() {}
        public void OnAfterDeserialize()
        {
            if (!inited || key == 0)
            {
                key = SecureRng.NextNonZeroInt();
                inited = true;
                Encrypt(0f);
            }
        }

        public void SetRaw(object value) => Encrypt(Convert.ToSingle(value));
        public object GetRaw() => Decrypt();

        public override string ToString() => Decrypt().ToString();
    }
}
