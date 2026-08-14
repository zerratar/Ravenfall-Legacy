// Assets/Obscured/Editor/ObscuredSplitDrawers.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Obscured.Editor
{
    // We mirror the hashing used at runtime so we don't depend on internal SplitHash
    static class SplitHashEditor
    {
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

    // -------------------------
    // ObscuredIntSplit Drawer
    // -------------------------
    [CustomPropertyDrawer(typeof(ObscuredIntSplit))]
    public class ObscuredIntSplitDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var partA     = property.FindPropertyRelative("partA");
            var partB     = property.FindPropertyRelative("partB");
            var checksum  = property.FindPropertyRelative("checksum");
            var salt      = property.FindPropertyRelative("salt");
            var inited    = property.FindPropertyRelative("inited");
            var opCounter = property.FindPropertyRelative("opCounter");
            var opsToReseal = property.FindPropertyRelative("opsToReseal");

            EditorGUI.BeginProperty(position, label, property);

            // Safe init mirrors runtime EnsureInit()
            if (!inited.boolValue)
            {
                salt.intValue = SecureRng.NextNonZeroInt();
                partA.intValue = 0;
                partB.intValue = 0;
                checksum.intValue = SplitHashEditor.Mix32(partA.intValue, partB.intValue, salt.intValue, 0x5A17);
                inited.boolValue = true;
                opCounter.intValue = 0;
                opsToReseal.intValue = (byte)(4 + (salt.intValue & 0x0F));
            }

            int current = unchecked(partA.intValue + partB.intValue);

            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUI.IntField(position, label, current);
            if (EditorGUI.EndChangeCheck())
            {
                // Simulate Set(value, countOp:false) - reseal parts with current salt
                int a = SecureRng.NextNonZeroInt();
                int b = unchecked(newValue - a);
                partA.intValue = a;
                partB.intValue = b;
                checksum.intValue = SplitHashEditor.Mix32(a, b, salt.intValue, 0x5A17);
                opCounter.intValue = 0; // editing in inspector shouldn't consume mutation budget
                // keep opsToReseal as-is
            }

            EditorGUI.EndProperty();
        }
    }

    // --------------------------
    // ObscuredLongSplit Drawer
    // --------------------------
    [CustomPropertyDrawer(typeof(ObscuredLongSplit))]
    public class ObscuredLongSplitDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var partA     = property.FindPropertyRelative("partA");
            var partB     = property.FindPropertyRelative("partB");
            var checksum  = property.FindPropertyRelative("checksum");
            var salt      = property.FindPropertyRelative("salt");
            var inited    = property.FindPropertyRelative("inited");
            var opCounter = property.FindPropertyRelative("opCounter");
            var opsToReseal = property.FindPropertyRelative("opsToReseal");

            EditorGUI.BeginProperty(position, label, property);

            if (!inited.boolValue)
            {
                salt.intValue = SecureRng.NextNonZeroInt();
                partA.longValue = 0L;
                partB.longValue = 0L;
                checksum.longValue = SplitHashEditor.Mix64(partA.longValue, partB.longValue, salt.intValue);
                inited.boolValue = true;
                opCounter.intValue = 0;
                opsToReseal.intValue = (byte)(4 + (salt.intValue & 0x0F));
            }

            long current = unchecked(partA.longValue + partB.longValue);

            EditorGUI.BeginChangeCheck();
            long newValue = EditorGUI.LongField(position, label, current);
            if (EditorGUI.EndChangeCheck())
            {
                long a = SecureRng.NextNonZeroLong();
                long b = unchecked(newValue - a);
                partA.longValue = a;
                partB.longValue = b;
                checksum.longValue = SplitHashEditor.Mix64(a, b, salt.intValue);
                opCounter.intValue = 0;
            }

            EditorGUI.EndProperty();
        }
    }

    // ---------------------------
    // ObscuredFloatSplit Drawer
    // ---------------------------
    [CustomPropertyDrawer(typeof(ObscuredFloatSplit))]
    public class ObscuredFloatSplitDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var partA     = property.FindPropertyRelative("partA");
            var partB     = property.FindPropertyRelative("partB");
            var xorKey    = property.FindPropertyRelative("xorKey");
            var checksum  = property.FindPropertyRelative("checksum");
            var salt      = property.FindPropertyRelative("salt");
            var inited    = property.FindPropertyRelative("inited");
            var opCounter = property.FindPropertyRelative("opCounter");
            var opsToReseal = property.FindPropertyRelative("opsToReseal");

            EditorGUI.BeginProperty(position, label, property);

            if (!inited.boolValue)
            {
                salt.intValue   = SecureRng.NextNonZeroInt();
                xorKey.intValue = SecureRng.NextNonZeroInt();
                partA.intValue  = 0;
                partB.intValue  = 0;
                checksum.intValue = SplitHashEditor.Mix32(partA.intValue, partB.intValue, xorKey.intValue, salt.intValue);
                inited.boolValue = true;
                opCounter.intValue = 0;
                opsToReseal.intValue = (byte)(4 + (salt.intValue & 0x0F));
            }

            // Reconstruct
            int bitsXor = (partA.intValue ^ partB.intValue);
            int bits    = bitsXor ^ xorKey.intValue;
            float current = System.BitConverter.Int32BitsToSingle(bits);

            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUI.FloatField(position, label, current);
            if (EditorGUI.EndChangeCheck())
            {
                // Simulate Set(value, countOp:false): new xorKey each write
                int newKey = SecureRng.NextNonZeroInt();
                int newBits = System.BitConverter.SingleToInt32Bits(newValue) ^ newKey;
                int a = SecureRng.NextNonZeroInt();
                int b = newBits ^ a;

                xorKey.intValue = newKey;
                partA.intValue  = a;
                partB.intValue  = b;
                checksum.intValue = SplitHashEditor.Mix32(a, b, newKey, salt.intValue);
                opCounter.intValue = 0;
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif
