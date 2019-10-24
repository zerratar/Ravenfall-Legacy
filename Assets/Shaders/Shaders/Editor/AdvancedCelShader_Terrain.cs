using UnityEngine;
using UnityEditor;

public class AdvancedCelShader_Terrain : ShaderGUI {

    override public void OnGUI (MaterialEditor materialEditor, MaterialProperty[] properties) {

        MaterialProperty _MainTexture = ShaderGUI.FindProperty ("_MainTexture", properties);
        MaterialProperty _TopTexture = ShaderGUI.FindProperty ("_TopTexture", properties);
        MaterialProperty _TopSpread = ShaderGUI.FindProperty ("_TopSpread", properties);
        //__________________________________________________________________________________
        MaterialProperty _LightRamp = ShaderGUI.FindProperty ("_LightRamp", properties);
        MaterialProperty _SpecularRamp = ShaderGUI.FindProperty ("_SpecularRamp", properties);
        //__________________________________________________________________________________
        MaterialProperty _Noise = ShaderGUI.FindProperty ("_Noise", properties);
        //__________________________________________________________________________________
        MaterialProperty _Normal = ShaderGUI.FindProperty ("_Normal", properties);
        MaterialProperty _SpecularColor = ShaderGUI.FindProperty ("_SpecularColor", properties);
        //__________________________________________________________________________________
        MaterialProperty _UseGlossSpec = ShaderGUI.FindProperty ("_UseGlossSpec", properties);
        //__________________________________________________________________________________
        MaterialProperty _UseOutline = ShaderGUI.FindProperty ("_UseOutline", properties);

        EditorGUILayout.Space ();
        materialEditor.TexturePropertySingleLine (new GUIContent ("Main Texture", ""), _MainTexture);
        materialEditor.TexturePropertySingleLine (new GUIContent ("Top Texture", ""), _TopTexture, _TopSpread);
        materialEditor.TexturePropertySingleLine (new GUIContent ("Normal", ""), _Normal);
        materialEditor.TextureScaleOffsetProperty (_MainTexture);
        _TopTexture.textureScaleAndOffset = _Normal.textureScaleAndOffset = _MainTexture.textureScaleAndOffset;
        EditorGUILayout.Space ();
        //__________________________________________________________________________________
        materialEditor.TexturePropertySingleLine (new GUIContent ("Light Ramp", ""), _LightRamp);
        materialEditor.TexturePropertySingleLine (new GUIContent ("Specular Ramp", ""), _SpecularRamp);
        EditorGUILayout.Space ();
        //__________________________________________________________________________________
        materialEditor.TexturePropertySingleLine (new GUIContent ("Noise", ""), _Noise);
        materialEditor.TextureScaleOffsetProperty (_Noise);
        EditorGUILayout.Space ();
        //__________________________________________________________________________________
        EditorGUILayout.Space ();
        materialEditor.ShaderProperty (_UseGlossSpec, _UseGlossSpec.displayName);
        
        if (_UseGlossSpec.floatValue == 1) {

            MaterialProperty _GlossSpec = ShaderGUI.FindProperty ("_GlossSpec", properties);
            materialEditor.TexturePropertySingleLine (new GUIContent ("GlossSpec", ""), _GlossSpec);

        } else {

            materialEditor.ShaderProperty (_SpecularColor, _SpecularColor.displayName);

        }

        EditorGUILayout.Space ();
        //__________________________________________________________________________________
        EditorGUILayout.Space ();
        materialEditor.ShaderProperty (_UseOutline, _UseOutline.displayName);

        if (_UseOutline.floatValue == 1) {

            MaterialProperty _OutlineThickness = ShaderGUI.FindProperty ("_OutlineThickness", properties);
            MaterialProperty _OutlineColor = ShaderGUI.FindProperty ("_OutlineColor", properties);
            materialEditor.ShaderProperty (_OutlineThickness, _OutlineThickness.displayName);
            materialEditor.ShaderProperty (_OutlineColor, _OutlineColor.displayName);

        }

    }

}
