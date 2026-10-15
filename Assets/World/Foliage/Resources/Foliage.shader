// Ravenfall/Foliage: trees, bushes and plants that move in the wind. Lit as the project's Lit shader graph lights them
// (specular workflow: _MainTex times _Color, _SpecColor, _Smoothness), with the vertices pushed along the wind by how
// high above the plant's own base they are: the trunk stands, the crown sways, and every plant has its own phase so a
// forest does not move as one. A slow gust rolls across the islands on top.
//
// _RavenfallWind (set by Wind): xy the wind's direction, z its strength (0 still: the setting off, or potato mode),
// w its speed. The shadow and depth passes sway the same, so the shadows move with the leaves.
Shader "Ravenfall/Foliage"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Colour", Color) = (1, 1, 1, 1)
        _SpecColor("Specular", Color) = (0.2, 0.2, 0.2, 1)
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _WindReach("Height at which the sway is full (m)", Float) = 5
        _WindAmount("How far the crown moves (m)", Float) = 0.22
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

    CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        half4 _Color;
        half4 _SpecColor;
        half _Smoothness;
        float _WindReach;
        float _WindAmount;
    CBUFFER_END

    float4 _RavenfallWind;

    struct Attributes
    {
        float4 positionOS : POSITION;
        float3 normalOS : NORMAL;
        float2 uv : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    // the vertex in the world, moved by the wind
    float3 Sway(float3 positionOS)
    {
        float3 positionWS = TransformObjectToWorld(positionOS);
        float3 root = TransformObjectToWorld(float3(0, 0, 0));
        float height = max(positionWS.y - root.y - 0.25, 0.0);
        float weight = pow(saturate(height / max(_WindReach, 0.1)), 1.6) * (1.0 + saturate(height / max(_WindReach, 0.1) - 1.0));
        if (weight <= 0.0 || _RavenfallWind.z <= 0.0) return positionWS;

        float t = _Time.y * _RavenfallWind.w;
        float phase = dot(root.xz, float2(0.37, 0.61));
        float2 direction = _RavenfallWind.xy;
        // a slow gust rolling over the islands along the wind
        float gust = sin(t * 0.21 - dot(root.xz, direction) * 0.015) * 0.5 + 0.5;
        float sway = sin(t * 0.9 + phase) * 0.6 + sin(t * 2.13 + phase * 1.7) * 0.25 + 0.35;
        // the leaves flutter a little on top of the sway
        float flutter = sin(t * 5.3 + dot(positionWS, float3(1.7, 2.3, 1.3))) * 0.06;
        float push = (sway * (0.45 + gust * 0.8) + flutter) * _WindAmount * _RavenfallWind.z * weight;
        positionWS.xz += direction * push;
        positionWS.y -= push * push * 0.25;
        return positionWS;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DistanceFog.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionWS = Sway(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = texel.rgb * _Color.rgb;
                surfaceData.specular = _SpecColor.rgb;
                surfaceData.metallic = 0;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 1;
                surfaceData.alpha = 1;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);
                inputData.preExposureMultiplier = 1;

                GIParams giParams = (GIParams)0;
                giParams.positionWS = inputData.positionWS;
                giParams.normalWS = inputData.normalWS;
                giParams.viewDirWS = inputData.viewDirectionWS;
                giParams.positionSS = input.positionCS.xy;
                giParams.isSurfaceTypeTransparent = false;
                InitializeBakedGI(giParams, inputData.bakedGI, inputData.shadowMask);

                half4 color = (half4)UniversalFragmentPBR(inputData, surfaceData,
                    true, true, false, false, true, false, true);   // specular workflow, as the Lit graph
                color.rgb = BlendDistanceFog(color.rgb, input.positionCS);
                color.a = 1;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            float4 ShadowVertex(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = Sway(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                return ApplyShadowClamping(positionCS);
            }

            half4 ShadowFragment() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            float4 DepthVertex(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformWorldToHClip(Sway(input.positionOS.xyz));
            }

            half DepthFragment(float4 positionCS : SV_POSITION) : SV_Target { return positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex NormalsVertex
            #pragma fragment NormalsFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            struct NormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            NormalsVaryings NormalsVertex(Attributes input)
            {
                NormalsVaryings output = (NormalsVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformWorldToHClip(Sway(input.positionOS.xyz));
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 NormalsFragment(NormalsVaryings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
                return half4(packedNormalWS, 0.0);
            #else
                return half4(normalWS, 0.0);
            #endif
            }
            ENDHLSL
        }
    }

    FallBack Off
}
