using UnityEngine;
using UnityEditor;

public class AdvancedCelShader : ShaderGUI {

    Material material;
    MaterialProperty _AlbedoColor;
    MaterialProperty _UseEmission;
    MaterialProperty _Emission;
    MaterialProperty _EmissionPower;
    MaterialProperty _SpecularRamp;
    MaterialProperty _SpecularColor;
    MaterialProperty _Normal;
    MaterialProperty _NormalIntensity;
    MaterialProperty _UseGlossSpec;
    MaterialProperty _RimLightColor;
    MaterialProperty _RimLightThickness;
    MaterialProperty _UseOutline;
    MaterialProperty _UseFade;
    MaterialProperty _Color;

    override public void OnGUI (MaterialEditor materialEditor, MaterialProperty[] properties) {

        material = materialEditor.target as Material;

        MaterialProperty _UseAlbedo = FindProperty ("_UseAlbedo", properties);
        
        if (material.HasProperty ("_AlbedoColor")) {
            _AlbedoColor = FindProperty ("_AlbedoColor", properties); 
        }
        if (material.HasProperty ("_Color")) {
            _Color = FindProperty ("_Color", properties);
        }
        MaterialProperty _Albedo = FindProperty ("_Albedo", properties);
        if (material.HasProperty ("_UseEmission") || material.HasProperty ("_Emission") || material.HasProperty ("_EmissionPower")) {
            _UseEmission = FindProperty ("_UseEmission", properties);
            _Emission = FindProperty ("_Emission", properties);
            _EmissionPower = FindProperty ("_EmissionPower", properties);
        }
        //__________________________________________________________________________________
        MaterialProperty _LightRamp = FindProperty ("_LightRamp", properties);
        if (material.HasProperty ("_SpecularRamp") || material.HasProperty ("_SpecularColor")) {
            _SpecularRamp = FindProperty ("_SpecularRamp", properties);
            _SpecularColor = FindProperty ("_SpecularColor", properties);
        }
        //__________________________________________________________________________________
        if (material.HasProperty ("_Normal")) {
            _Normal = FindProperty ("_Normal", properties); 
        }
        if (material.HasProperty ("_NormalIntensity")) {
            _NormalIntensity = FindProperty ("_NormalIntensity", properties);
        }
        //__________________________________________________________________________________
        if (material.HasProperty ("_UseGlossSpec")) {
            _UseGlossSpec = FindProperty ("_UseGlossSpec", properties); 
        }
        //__________________________________________________________________________________
        if (material.HasProperty ("_RimLightColor")) {
            _RimLightColor = FindProperty ("_RimLightColor", properties);
            _RimLightThickness = FindProperty ("_RimLightThickness", properties); 
        }
        //__________________________________________________________________________________
        if (material.HasProperty ("_UseOutline")) {
            _UseOutline = FindProperty ("_UseOutline", properties); 
        }
        //__________________________________________________________________________________
        if (material.HasProperty ("_UseFade")) {
            _UseFade = FindProperty ("_UseFade", properties); 
        }

        EditorGUILayout.Space ();
        materialEditor.ShaderProperty (_UseAlbedo, _UseAlbedo.displayName);

        if (_UseAlbedo.floatValue == 1) {

            if (material.HasProperty ("_Color")) {
                materialEditor.TexturePropertySingleLine (new GUIContent ("Albedo", ""), _Albedo, _Color);
            } else {
                materialEditor.TexturePropertySingleLine (new GUIContent ("Albedo", ""), _Albedo, _AlbedoColor);
            }

        } else {

            if (material.HasProperty ("_AlbedoColor")) {
                materialEditor.ShaderProperty (_AlbedoColor, _AlbedoColor.displayName); 
            }
            if (material.HasProperty ("_Color")) {
                materialEditor.ShaderProperty (_Color, _Color.displayName);
            }

        }
        if (material.HasProperty ("_Normal")) {
            if (material.HasProperty ("_NormalIntensity")) {
                materialEditor.TexturePropertySingleLine (new GUIContent ("Normal", ""), _Normal, _NormalIntensity);
            } else {
                materialEditor.TexturePropertySingleLine (new GUIContent ("Normal", ""), _Normal);
            }
        }
        
        EditorGUILayout.Space ();
        //__________________________________________________________________________________

        if (material.HasProperty ("_UseEmission") || material.HasProperty ("_Emission") || material.HasProperty ("_EmissionPower")) {
            materialEditor.ShaderProperty (_UseEmission, _UseEmission.displayName);

            if (_UseEmission.floatValue == 1) {

                materialEditor.TexturePropertySingleLine (new GUIContent ("Emission", ""), _Emission, _EmissionPower);

            } 
        }
        if (!material.HasProperty ("_Color")) {
            EditorGUILayout.Space ();
            materialEditor.TextureScaleOffsetProperty (_Albedo); 
        }
        if (material.HasProperty ("_Emission")) {
            _Emission.textureScaleAndOffset = _Normal.textureScaleAndOffset = _Albedo.textureScaleAndOffset;
            EditorGUILayout.Space ();
        }
        
        //__________________________________________________________________________________
        materialEditor.TexturePropertySingleLine (new GUIContent ("Light Ramp", ""), _LightRamp);
        if (material.HasProperty ("_SpecularRamp")) {
            materialEditor.TexturePropertySingleLine (new GUIContent ("Specular Ramp", ""), _SpecularRamp); 
        }
        EditorGUILayout.Space ();
        //__________________________________________________________________________________
        if (material.HasProperty ("_UseGlossSpec")) {
            EditorGUILayout.Space ();

            materialEditor.ShaderProperty (_UseGlossSpec, _UseGlossSpec.displayName);
            if (_UseGlossSpec.floatValue == 1) {

                MaterialProperty _GlossSpec = FindProperty ("_GlossSpec", properties);
                materialEditor.TexturePropertySingleLine (new GUIContent ("GlossSpec", ""), _GlossSpec);

            } else {

                if (material.HasProperty ("_SpecularColor")) {
                    materialEditor.ShaderProperty (_SpecularColor, _SpecularColor.displayName);
                }

            }
        }
        

        EditorGUILayout.Space ();
        //__________________________________________________________________________________
        if (material.HasProperty ("_RimLightColor")) {
            materialEditor.ShaderProperty (_RimLightColor, _RimLightColor.displayName);
            materialEditor.ShaderProperty (_RimLightThickness, _RimLightThickness.displayName); 
        }
        //__________________________________________________________________________________
        if (material.HasProperty ("_UseOutline")) {
            EditorGUILayout.Space ();
            materialEditor.ShaderProperty (_UseOutline, _UseOutline.displayName);

            if (_UseOutline.floatValue == 1) {

                MaterialProperty _OutlineThickness = FindProperty ("_OutlineThickness", properties);
                MaterialProperty _OutlineColor = FindProperty ("_OutlineColor", properties);
                materialEditor.ShaderProperty (_OutlineThickness, _OutlineThickness.displayName);
                materialEditor.ShaderProperty (_OutlineColor, _OutlineColor.displayName);

            } 
        }
        //__________________________________________________________________________________
        if (material.HasProperty ("_UseFade")) {
            materialEditor.ShaderProperty (_UseFade, _UseFade.displayName);
            if (_UseFade.floatValue == 1) {

                MaterialProperty _FadeDistance = FindProperty ("_FadeDistance", properties);
                MaterialProperty _FarFadeDistance = FindProperty ("_FarFadeDistance", properties);
                materialEditor.ShaderProperty (_FadeDistance, _FadeDistance.displayName);
                materialEditor.ShaderProperty (_FarFadeDistance, _FarFadeDistance.displayName);

            }
        }

    }

}
