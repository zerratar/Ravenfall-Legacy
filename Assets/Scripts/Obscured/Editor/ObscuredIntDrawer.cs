// Assets/Obscured/Editor/ObscuredDrawers.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Obscured.Editor
{
    //
    // INT
    //
    [CustomPropertyDrawer(typeof(ObscuredInt))]
    public class ObscuredIntDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var enc  = property.FindPropertyRelative("encrypted");
            var key  = property.FindPropertyRelative("key");
            var fake = property.FindPropertyRelative("fakeValue");
            var init = property.FindPropertyRelative("inited");

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            // safe init
            if (!init.boolValue || key.intValue == 0)
            {
                key.intValue = SecureRng.NextNonZeroInt();
                enc.intValue = 0 ^ key.intValue;
                fake.intValue = 0 ^ ~key.intValue;
                init.boolValue = true;
            }

            int value = enc.intValue ^ key.intValue; // decrypt
            value = EditorGUI.IntField(position, label, value);

            if (EditorGUI.EndChangeCheck())
            {
                enc.intValue = value ^ key.intValue;
                fake.intValue = value ^ ~key.intValue;
            }

            EditorGUI.EndProperty();
        }
    }

    //
    // LONG
    //
    [CustomPropertyDrawer(typeof(ObscuredLong))]
    public class ObscuredLongDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var enc  = property.FindPropertyRelative("encrypted");
            var key  = property.FindPropertyRelative("key");
            var fake = property.FindPropertyRelative("fakeValue");
            var init = property.FindPropertyRelative("inited");

            EditorGUI.BeginProperty(position, label, property);
            if (!init.boolValue || key.longValue == 0)
            {
                key.longValue = SecureRng.NextNonZeroLong();
                enc.longValue = 0L ^ key.longValue;
                fake.longValue = 0L ^ ~key.longValue;
                init.boolValue = true;
            }
            long value = enc.longValue ^ key.longValue;
            EditorGUI.BeginChangeCheck();
            value = EditorGUI.LongField(position, label, value);
            if (EditorGUI.EndChangeCheck())
            {
                enc.longValue = value ^ key.longValue;
                fake.longValue = value ^ ~key.longValue;
            }
            EditorGUI.EndProperty();
        }
    }

    //
    // SHORT
    //
    [CustomPropertyDrawer(typeof(ObscuredShort))]
    public class ObscuredShortDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var enc  = property.FindPropertyRelative("encrypted");
            var key  = property.FindPropertyRelative("key");
            var fake = property.FindPropertyRelative("fakeValue");
            var init = property.FindPropertyRelative("inited");

            EditorGUI.BeginProperty(position, label, property);
            if (!init.boolValue || key.intValue == 0)
            {
                short k = (short)(SecureRng.NextNonZeroInt() & 0x7FFF);
                if (k == 0) k = 1;
                key.intValue = k;
                enc.intValue = (short)(0 ^ key.intValue);
                fake.intValue = (short)(0 ^ ~key.intValue);
                init.boolValue = true;
            }
            short value = (short)(enc.intValue ^ key.intValue);
            EditorGUI.BeginChangeCheck();
            int intField = EditorGUI.IntField(position, label, value);
            if (EditorGUI.EndChangeCheck())
            {
                short v = (short)Mathf.Clamp(intField, short.MinValue, short.MaxValue);
                enc.intValue = (short)(v ^ key.intValue);
                fake.intValue = (short)(v ^ ~key.intValue);
            }
            EditorGUI.EndProperty();
        }
    }

    //
    // FLOAT
    //
    [CustomPropertyDrawer(typeof(ObscuredFloat))]
    public class ObscuredFloatDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var enc  = property.FindPropertyRelative("encrypted");
            var key  = property.FindPropertyRelative("key");
            var fake = property.FindPropertyRelative("fakeBits");
            var init = property.FindPropertyRelative("inited");

            EditorGUI.BeginProperty(position, label, property);
            if (!init.boolValue || key.intValue == 0)
            {
                key.intValue = SecureRng.NextNonZeroInt();
                enc.intValue = 0 ^ key.intValue;
                fake.intValue = 0 ^ ~key.intValue;
                init.boolValue = true;
            }
            int bits = enc.intValue ^ key.intValue;
            float value = System.BitConverter.Int32BitsToSingle(bits);

            EditorGUI.BeginChangeCheck();
            value = EditorGUI.FloatField(position, label, value);
            if (EditorGUI.EndChangeCheck())
            {
                int nb = System.BitConverter.SingleToInt32Bits(value);
                enc.intValue = nb ^ key.intValue;
                fake.intValue = nb ^ ~key.intValue;
            }
            EditorGUI.EndProperty();
        }
    }

    //
    // DOUBLE
    //
    [CustomPropertyDrawer(typeof(ObscuredDouble))]
    public class ObscuredDoubleDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var enc  = property.FindPropertyRelative("encrypted");
            var key  = property.FindPropertyRelative("key");
            var fake = property.FindPropertyRelative("fakeBits");
            var init = property.FindPropertyRelative("inited");

            EditorGUI.BeginProperty(position, label, property);
            if (!init.boolValue || key.longValue == 0)
            {
                key.longValue = SecureRng.NextNonZeroLong();
                enc.longValue = 0L ^ key.longValue;
                fake.longValue = 0L ^ ~key.longValue;
                init.boolValue = true;
            }
            long bits = enc.longValue ^ key.longValue;
            double value = System.BitConverter.Int64BitsToDouble(bits);

            EditorGUI.BeginChangeCheck();
            value = EditorGUI.DoubleField(position, label, value);
            if (EditorGUI.EndChangeCheck())
            {
                long nb = System.BitConverter.DoubleToInt64Bits(value);
                enc.longValue = nb ^ key.longValue;
                fake.longValue = nb ^ ~key.longValue;
            }
            EditorGUI.EndProperty();
        }
    }
}
#endif
