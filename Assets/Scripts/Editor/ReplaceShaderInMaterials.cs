using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ReplaceShaderInMaterials : EditorWindow
{
    private Shader oldShader;
    private Shader newShader;

    // A container to hold both the property type and its value
    private struct PropertyData
    {
        public ShaderUtil.ShaderPropertyType propType;
        public object value;
    }

    [MenuItem("Tools/Replace Shader in Materials")]
    private static void ShowWindow()
    {
        var window = GetWindow<ReplaceShaderInMaterials>();
        window.titleContent = new GUIContent("Replace Shader");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace Shader In Materials", EditorStyles.boldLabel);

        oldShader = EditorGUILayout.ObjectField("Old Shader", oldShader, typeof(Shader), false) as Shader;
        newShader = EditorGUILayout.ObjectField("New Shader", newShader, typeof(Shader), false) as Shader;

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical();
        try
        {
            if (GUILayout.Button("Test (Simulate)"))
            {
                if (oldShader == null || newShader == null)
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        "You must assign both old and new shaders before testing.",
                        "OK"
                    );
                    return;
                }

                SimulateShaderReplacement(oldShader, newShader);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Replace (Actual)"))
            {
                if (oldShader == null || newShader == null)
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        "You must assign both old and new shaders before replacing.",
                        "OK"
                    );
                    return;
                }

                ReplaceShaders(oldShader, newShader);
            }
        }
        finally
        {
            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// Simulate the replacement process without actually modifying materials.
    /// </summary>
    private void SimulateShaderReplacement(Shader sourceShader, Shader targetShader)
    {
        string[] allMaterialGuids = AssetDatabase.FindAssets("t:Material");
        int materialsToUpdateCount = 0;

        Debug.Log("=== Simulation Start ===");
        Debug.Log($"Simulating replacement of '{sourceShader.name}' with '{targetShader.name}'");

        for (int i = 0; i < allMaterialGuids.Length; i++)
        {
            string guid = allMaterialGuids[i];
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && mat.shader == sourceShader)
            {
                materialsToUpdateCount++;
                Debug.Log($"[SIMULATE] Material: {path}");

                // Gather the old properties
                Dictionary<string, PropertyData> propertyValues =
                    CopyShaderProperties(mat, sourceShader);

                // Build a hash set of property names in the new shader
                int targetPropertyCount = ShaderUtil.GetPropertyCount(targetShader);
                var newShaderProps = new HashSet<string>();
                for (int tp = 0; tp < targetPropertyCount; tp++)
                {
                    newShaderProps.Add(ShaderUtil.GetPropertyName(targetShader, tp));
                }

                // Compare each old property to see if it's in the new shader
                List<string> missingProps = new List<string>();
                foreach (var kvp in propertyValues)
                {
                    string oldPropName = kvp.Key;
                    var oldPropType = kvp.Value.propType;
                    var oldValue = kvp.Value.value;

                    if (newShaderProps.Contains(oldPropName))
                    {
                        // Check if the type is the same
                        var newPropType = GetShaderPropertyType(targetShader, oldPropName);
                        if (newPropType != oldPropType)
                        {
                            Debug.LogWarning(
                                $"  Property '{oldPropName}' has a type mismatch: " +
                                $"old shader type = {oldPropType}, new shader type = {newPropType}. " +
                                "This property would be skipped."
                            );
                        }
                        else
                        {
                            // Types match
                            Debug.Log(
                                $"  Would set property '{oldPropName}' (type: {oldPropType}) to value: {oldValue}"
                            );
                        }
                    }
                    else
                    {
                        missingProps.Add(oldPropName);
                    }
                }

                if (missingProps.Count > 0)
                {
                    Debug.LogWarning(
                        $"  Missing {missingProps.Count} properties in the new shader: " +
                        $"{string.Join(", ", missingProps)}"
                    );
                }
            }
        }

        Debug.Log($"Simulation complete. Found {materialsToUpdateCount} material(s) that would be updated.");
        Debug.Log("=== Simulation End ===");
    }

    /// <summary>
    /// Actual replacement: set materials to the new shader and copy over properties.
    /// </summary>
    private void ReplaceShaders(Shader sourceShader, Shader targetShader)
    {
        string[] allMaterialGuids = AssetDatabase.FindAssets("t:Material");
        int updatedMaterialsCount = 0;

        try
        {
            for (int i = 0; i < allMaterialGuids.Length; i++)
            {
                string guid = allMaterialGuids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (mat != null && mat.shader == sourceShader)
                {
                    // Copy old properties
                    Dictionary<string, PropertyData> propertyValues =
                        CopyShaderProperties(mat, sourceShader);

                    // Switch to the new shader
                    mat.shader = targetShader;

                    // Apply old properties to the new shader
                    ApplyShaderProperties(mat, targetShader, propertyValues);
                    EditorUtility.SetDirty(mat);
                    updatedMaterialsCount++;
                }

                // Progress bar
                float progress = (float)(i + 1) / allMaterialGuids.Length;
                EditorUtility.DisplayProgressBar(
                    "Replacing Shaders in Materials",
                    $"Processing material {i + 1}/{allMaterialGuids.Length}",
                    progress
                );
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Replace Shader Complete",
            $"Updated {updatedMaterialsCount} material(s) from {sourceShader.name} to {targetShader.name}.",
            "OK"
        );
    }

    /// <summary>
    /// Reads property values + their types from a material's old shader
    /// and returns a dictionary keyed by property name.
    /// </summary>
    private Dictionary<string, PropertyData> CopyShaderProperties(Material mat, Shader oldShader)
    {
        int propertyCount = ShaderUtil.GetPropertyCount(oldShader);
        var dict = new Dictionary<string, PropertyData>();

        for (int i = 0; i < propertyCount; i++)
        {
            var propName = ShaderUtil.GetPropertyName(oldShader, i);
            var propType = ShaderUtil.GetPropertyType(oldShader, i);

            PropertyData data = new PropertyData { propType = propType };

            switch (propType)
            {
                case ShaderUtil.ShaderPropertyType.Color:
                    data.value = mat.GetColor(propName);
                    break;
                case ShaderUtil.ShaderPropertyType.Vector:
                    data.value = mat.GetVector(propName);
                    break;
                case ShaderUtil.ShaderPropertyType.Float:
                case ShaderUtil.ShaderPropertyType.Range:
                    data.value = mat.GetFloat(propName);
                    break;
                case ShaderUtil.ShaderPropertyType.TexEnv:
                    data.value = mat.GetTexture(propName);
                    break;
            }

            dict[propName] = data;
        }

        return dict;
    }

    /// <summary>
    /// Applies property values to a material's new shader, skipping
    /// any mismatched property types or missing properties.
    /// </summary>
    private void ApplyShaderProperties(Material mat,
                                       Shader newShader,
                                       Dictionary<string, PropertyData> propertyValues)
    {
        int propertyCount = ShaderUtil.GetPropertyCount(newShader);

        // Collect new shader properties in a dictionary for quick lookup:
        var newShaderProps = new Dictionary<string, ShaderUtil.ShaderPropertyType>();
        for (int i = 0; i < propertyCount; i++)
        {
            var propName = ShaderUtil.GetPropertyName(newShader, i);
            var propType = ShaderUtil.GetPropertyType(newShader, i);
            newShaderProps[propName] = propType;
        }

        // For each old property, see if the new shader has it + same type
        foreach (var kvp in propertyValues)
        {
            string oldPropName = kvp.Key;
            ShaderUtil.ShaderPropertyType oldPropType = kvp.Value.propType;
            object oldValue = kvp.Value.value;

            if (!newShaderProps.ContainsKey(oldPropName))
            {
                // Property doesn't exist in new shader
                continue;
            }

            var newPropType = newShaderProps[oldPropName];
            if (newPropType != oldPropType)
            {
                // skip or log warning if desired
                // Debug.LogWarning($"Skipping property '{oldPropName}' type mismatch: old {oldPropType}, new {newPropType}");
                continue;
            }

            // Types match, so safely set the property
            switch (newPropType)
            {
                case ShaderUtil.ShaderPropertyType.Color:
                    mat.SetColor(oldPropName, (Color)oldValue);
                    break;
                case ShaderUtil.ShaderPropertyType.Vector:
                    mat.SetVector(oldPropName, (Vector4)oldValue);
                    break;
                case ShaderUtil.ShaderPropertyType.Float:
                case ShaderUtil.ShaderPropertyType.Range:
                    mat.SetFloat(oldPropName, (float)oldValue);
                    break;
                case ShaderUtil.ShaderPropertyType.TexEnv:
                    mat.SetTexture(oldPropName, (Texture)oldValue);
                    break;
            }
        }
    }

    /// <summary>
    /// Convenience function to get the ShaderPropertyType for a given property
    /// name in a given shader.
    /// </summary>
    private ShaderUtil.ShaderPropertyType GetShaderPropertyType(Shader shader, string propertyName)
    {
        int count = ShaderUtil.GetPropertyCount(shader);
        for (int i = 0; i < count; i++)
        {
            string pName = ShaderUtil.GetPropertyName(shader, i);
            if (pName == propertyName)
            {
                return ShaderUtil.GetPropertyType(shader, i);
            }
        }
        return (ShaderUtil.ShaderPropertyType)(-1); // invalid
    }
}
