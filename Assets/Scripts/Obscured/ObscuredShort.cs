using System;
using UnityEngine;

namespace Obscured
{
    [Serializable]
    public struct ObscuredShort : ISerializationCallbackReceiver, IEquatable<ObscuredShort>, IEquatable<short>, IObscured
    {
        [SerializeField] private short encrypted;
        [SerializeField] private short key;
        [SerializeField] private short fakeValue;
        [SerializeField] private bool inited;

        public ObscuredShort(short value)
        {
            key        = (short)(SecureRng.NextNonZeroInt() & 0x7FFF);
            if (key == 0) key = 1;
            encrypted  = (short)(value ^ key);
            fakeValue  = (short)(value ^ ~key);
            inited     = true;
        }

        private short Decrypt()
        {
            if (!inited || key == 0)
            {
                key = (short)(SecureRng.NextNonZeroInt() & 0x7FFF);
                if (key == 0) key = 1;
                encrypted = (short)(0 ^ key);
                fakeValue = (short)(0 ^ ~key);
                inited = true;
                return 0;
            }
            short v = (short)(encrypted ^ key);
            short mirror = (short)(fakeValue ^ ~key);
            if (mirror != v) TamperDetector.Raise(nameof(ObscuredShort), "Mirror mismatch");
            return v;
        }

        private void Encrypt(short v)
        {
            if (!inited || key == 0)
            {
                key = (short)(SecureRng.NextNonZeroInt() & 0x7FFF);
                if (key == 0) key = 1;
                inited = true;
            }
            encrypted = (short)(v ^ key);
            fakeValue = (short)(v ^ ~key);
        }

        public void RandomizeKey()
        {
            var v = Decrypt();
            key = (short)(SecureRng.NextNonZeroInt() & 0x7FFF);
            if (key == 0) key = 1;
            Encrypt(v);
        }

        public static implicit operator ObscuredShort(short v) => new ObscuredShort(v);
        public static implicit operator short(ObscuredShort v) => v.Decrypt();

        public static ObscuredShort operator +(ObscuredShort a, short b) { var v = (short)(a.Decrypt() + b); a.Encrypt(v); return a; }
        public static ObscuredShort operator -(ObscuredShort a, short b) { var v = (short)(a.Decrypt() - b); a.Encrypt(v); return a; }
        public static ObscuredShort operator ++(ObscuredShort a) { var v = (short)(a.Decrypt() + 1); a.Encrypt(v); return a; }
        public static ObscuredShort operator --(ObscuredShort a) { var v = (short)(a.Decrypt() - 1); a.Encrypt(v); return a; }

        public bool Equals(ObscuredShort other) => Decrypt() == other.Decrypt();
        public bool Equals(short other) => Decrypt() == other;
        public override bool Equals(object obj) => obj is ObscuredShort o && Equals(o) || obj is short s && Equals(s);
        public override int GetHashCode() => Decrypt().GetHashCode();

        public void OnBeforeSerialize() {}
        public void OnAfterDeserialize()
        {
            if (!inited || key == 0)
            {
                key = (short)(SecureRng.NextNonZeroInt() & 0x7FFF);
                if (key == 0) key = 1;
                inited = true;
                Encrypt(0);
            }
        }

        public void SetRaw(object value) => Encrypt(Convert.ToInt16(value));
        public object GetRaw() => Decrypt();

        public override string ToString() => Decrypt().ToString();
    }
}
