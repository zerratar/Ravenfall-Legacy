using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshRenderer), true)]
[CanEditMultipleObjects]
public class CustomMeshRendererInspector : Editor
{
    SerializedProperty materials;
    SerializedProperty shadowCastingMode;
    SerializedProperty receiveShadows;
    SerializedProperty motionVectorGenerationMode;
    SerializedProperty lightProbeUsage;
    SerializedProperty reflectionProbeUsage;
    SerializedProperty allowOcclusionWhenDynamic;

    void OnEnable()
    {
        materials = serializedObject.FindProperty("m_Materials");
        shadowCastingMode = serializedObject.FindProperty("m_ShadowCastingMode");
        receiveShadows = serializedObject.FindProperty("m_ReceiveShadows");
        motionVectorGenerationMode = serializedObject.FindProperty("m_MotionVectors");
        lightProbeUsage = serializedObject.FindProperty("m_LightProbeUsage");
        reflectionProbeUsage = serializedObject.FindProperty("m_ReflectionProbeUsage");
        allowOcclusionWhenDynamic = serializedObject.FindProperty("m_AllowOcclusionWhenDynamic");
    }

    public override void OnInspectorGUI()
    {
        if (targets.Length > 1)
        {

            serializedObject.Update();

            // Display and edit the "enabled" property for multiple objects
            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.Toggle("Enabled", ((MeshRenderer)target).enabled);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in targets)
                {
                    MeshRenderer meshRenderer = (MeshRenderer)obj;
                    Undo.RecordObject(meshRenderer, "Toggle MeshRenderer Enabled");
                    meshRenderer.enabled = enabled;
                }
            }

            // Display and edit the materials list
            EditorGUILayout.PropertyField(materials, true);

            // Display and edit the shadow casting mode
            EditorGUILayout.PropertyField(shadowCastingMode);

            // Display and edit whether the renderer receives shadows
            EditorGUILayout.PropertyField(receiveShadows);

            // Display and edit the motion vector generation mode
            EditorGUILayout.PropertyField(motionVectorGenerationMode);

            // Display and edit the light probe usage
            EditorGUILayout.PropertyField(lightProbeUsage);

            // Display and edit the reflection probe usage
            EditorGUILayout.PropertyField(reflectionProbeUsage);

            // Display and edit the dynamic occlusion
            EditorGUILayout.PropertyField(allowOcclusionWhenDynamic);

            // Apply changes to the serializedObject
            serializedObject.ApplyModifiedProperties();
        }
        else
        {
            MeshRenderer meshRenderer = (MeshRenderer)target;

            // Display and edit the "enabled" property
            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.Toggle("Enabled", meshRenderer.enabled);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(meshRenderer, "Toggle MeshRenderer Enabled");
                meshRenderer.enabled = enabled;
            }

            // Display and edit the materials list
            SerializedProperty materials = serializedObject.FindProperty("m_Materials");
            EditorGUILayout.PropertyField(materials, true);

            // Display and edit the shadow casting mode
            meshRenderer.shadowCastingMode = (UnityEngine.Rendering.ShadowCastingMode)EditorGUILayout.EnumPopup("Cast Shadows", meshRenderer.shadowCastingMode);

            // Display and edit whether the renderer receives shadows
            meshRenderer.receiveShadows = EditorGUILayout.Toggle("Receive Shadows", meshRenderer.receiveShadows);

            // Display and edit the motion vector generation mode
            meshRenderer.motionVectorGenerationMode = (MotionVectorGenerationMode)EditorGUILayout.EnumPopup("Motion Vectors", meshRenderer.motionVectorGenerationMode);

            // Display and edit the light probe usage
            meshRenderer.lightProbeUsage = (UnityEngine.Rendering.LightProbeUsage)EditorGUILayout.EnumPopup("Light Probes", meshRenderer.lightProbeUsage);

            // Display and edit the reflection probe usage
            meshRenderer.reflectionProbeUsage = (UnityEngine.Rendering.ReflectionProbeUsage)EditorGUILayout.EnumPopup("Reflection Probes", meshRenderer.reflectionProbeUsage);

            // Display and edit the dynamic occlusion
            meshRenderer.allowOcclusionWhenDynamic = EditorGUILayout.Toggle("Dynamic Occlusion", meshRenderer.allowOcclusionWhenDynamic);

            // Apply changes to the serializedObject
            serializedObject.ApplyModifiedProperties();
        }
    }
}

//using UnityEngine;
//using UnityEditor;

//[CustomEditor(typeof(MeshRenderer))]
//public class CustomMeshRendererInspector : Editor
//{
//    public override void OnInspectorGUI()
//    {
//        MeshRenderer meshRenderer = (MeshRenderer)target;

//        // Display and edit the "enabled" property
//        EditorGUI.BeginChangeCheck();
//        bool enabled = EditorGUILayout.Toggle("Enabled", meshRenderer.enabled);
//        if (EditorGUI.EndChangeCheck())
//        {
//            Undo.RecordObject(meshRenderer, "Toggle MeshRenderer Enabled");
//            meshRenderer.enabled = enabled;
//        }

//        // Display and edit the materials list
//        SerializedProperty materials = serializedObject.FindProperty("m_Materials");
//        EditorGUILayout.PropertyField(materials, true);

//        // Display and edit the shadow casting mode
//        meshRenderer.shadowCastingMode = (UnityEngine.Rendering.ShadowCastingMode)EditorGUILayout.EnumPopup("Cast Shadows", meshRenderer.shadowCastingMode);

//        // Display and edit whether the renderer receives shadows
//        meshRenderer.receiveShadows = EditorGUILayout.Toggle("Receive Shadows", meshRenderer.receiveShadows);

//        // Display and edit the motion vector generation mode
//        meshRenderer.motionVectorGenerationMode = (MotionVectorGenerationMode)EditorGUILayout.EnumPopup("Motion Vectors", meshRenderer.motionVectorGenerationMode);

//        // Display and edit the light probe usage
//        meshRenderer.lightProbeUsage = (UnityEngine.Rendering.LightProbeUsage)EditorGUILayout.EnumPopup("Light Probes", meshRenderer.lightProbeUsage);

//        // Display and edit the reflection probe usage
//        meshRenderer.reflectionProbeUsage = (UnityEngine.Rendering.ReflectionProbeUsage)EditorGUILayout.EnumPopup("Reflection Probes", meshRenderer.reflectionProbeUsage);

//        // Display and edit the dynamic occlusion
//        meshRenderer.allowOcclusionWhenDynamic = EditorGUILayout.Toggle("Dynamic Occlusion", meshRenderer.allowOcclusionWhenDynamic);

//        // Apply changes to the serializedObject
//        serializedObject.ApplyModifiedProperties();
//    }
//}

