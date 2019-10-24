using UnityEngine;
using UnityEditor;

public class AdvancedCelShader_Water : ShaderGUI {

    override public void OnGUI (MaterialEditor materialEditor, MaterialProperty[] properties) {

        Material material;
        MaterialProperty _DeepWaterColor;
        MaterialProperty _ShorelineColor;
        MaterialProperty _ShorelineDepth;
        MaterialProperty _WaterSpeed;
        MaterialProperty _WavesIntensity;
        MaterialProperty _WavesDensity;
        MaterialProperty _LightRamp;
        MaterialProperty _shallowWaterColor;
        MaterialProperty _TessellationValue;
        MaterialProperty _UseTessellation;
        MaterialProperty _Foam;
        MaterialProperty _FoamScale;

        material = materialEditor.target as Material;

        _DeepWaterColor = ShaderGUI.FindProperty ("_DeepWaterColor", properties);
        _ShorelineColor = ShaderGUI.FindProperty ("_ShorelineColor", properties);
        _ShorelineDepth = ShaderGUI.FindProperty ("_ShorelineDepth", properties);
        _WaterSpeed = ShaderGUI.FindProperty ("_WaterSpeed", properties);
        _WavesIntensity = ShaderGUI.FindProperty ("_WavesIntensity", properties);
        _WavesDensity = ShaderGUI.FindProperty ("_WavesDensity", properties);
        _LightRamp = ShaderGUI.FindProperty ("_LightRamp", properties);
        _shallowWaterColor = ShaderGUI.FindProperty ("_shallowWaterColor", properties);
        _TessellationValue = ShaderGUI.FindProperty ("_TessellationValue", properties);
        _UseTessellation = ShaderGUI.FindProperty ("_UseTessellation", properties);
        _Foam = ShaderGUI.FindProperty ("_Foam", properties);
        _FoamScale = ShaderGUI.FindProperty ("_FoamScale", properties);
        //__________________________________________________________________________________
        EditorGUILayout.Space ();
        materialEditor.TexturePropertySingleLine (new GUIContent ("Light Ramp", ""), _LightRamp);
        materialEditor.TexturePropertySingleLine (new GUIContent ("Foam", ""), _Foam, _FoamScale);
        //__________________________________________________________________________________
        EditorGUILayout.Space ();
        materialEditor.ShaderProperty (_ShorelineDepth, _ShorelineDepth.displayName);
        //__________________________________________________________________________________
        EditorGUILayout.Space ();
        materialEditor.ShaderProperty (_ShorelineColor, _ShorelineColor.displayName);
        materialEditor.ShaderProperty (_DeepWaterColor, _DeepWaterColor.displayName);
        materialEditor.ShaderProperty (_shallowWaterColor, _shallowWaterColor.displayName);
        //__________________________________________________________________________________
        EditorGUILayout.Space ();
        materialEditor.ShaderProperty (_WaterSpeed, _WaterSpeed.displayName);
        materialEditor.ShaderProperty (_WavesDensity, _WavesDensity.displayName);
        materialEditor.ShaderProperty (_WavesIntensity, _WavesIntensity.displayName);
        //__________________________________________________________________________________
        EditorGUILayout.Space ();
        materialEditor.ShaderProperty (_UseTessellation, _UseTessellation.displayName);
        if (_UseTessellation.floatValue == 1) {
            materialEditor.ShaderProperty (_TessellationValue, _TessellationValue.displayName);
        }

    }

}
