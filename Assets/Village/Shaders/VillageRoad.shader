// Village roads: the island's own terrain textures, laid along the road and blended into the
// ground around it.
//
// The road strip carries one value across it in uv.x: 0 down the middle, 1 at the edge (and at
// the rim of the rounded ends). The surface texture covers the middle; towards the edge it gives
// way to the shoulder texture, and then fades out into the terrain. Both borders are ragged:
// a world space noise moves them in and out, and the textures' own brightness pushes stones and
// pebbles further out than the dirt between them, so cobbles end in loose stones rather than a
// straight line.
//
// Textures are mapped in world space, metres per tile, as the terrain maps its layers. Two roads
// that meet or cross carry on the same pattern, with no seam, and a dirt road matches the
// terrain's dirt where the two touch.
//
// Each road draws twice: once whole, fringe and all, and once more a queue later with only its
// solid middle (_Core). Every road's middle then lies over every other road's fringe, so where roads
// meet or cross the fading shoulder of one never smudges the surface of the other.
Shader "Ravenfall/Village Road"
{
    Properties
    {
        [MainTexture] _BaseMap("Surface", 2D) = "white" {}
        [Normal][NoScaleOffset] _BumpMap("Surface normals", 2D) = "bump" {}
        _Tile("Surface tile size (m)", Float) = 4
        _EdgeMap("Shoulder", 2D) = "white" {}
        [Normal][NoScaleOffset] _EdgeBumpMap("Shoulder normals", 2D) = "bump" {}
        _EdgeTile("Shoulder tile size (m)", Float) = 4
        _BumpScale("Normal strength", Range(0, 2)) = 1
        [MainColor] _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _EdgeColor("Shoulder tint", Color) = (1, 1, 1, 1)
        _Shoulder("Shoulder starts (0 middle, 1 edge)", Range(0, 1)) = 0.55
        _ShoulderSoftness("Shoulder softness", Range(0.01, 0.5)) = 0.1
        _Fade("Fade starts", Range(0, 1)) = 0.72
        _FadeWidth("Fade width", Range(0.02, 0.5)) = 0.2
        _Ragged("Raggedness", Range(0, 0.5)) = 0.16
        _NoiseScale("Noise per metre", Float) = 0.55
        _HeightBlend("Stones push out", Range(0, 1)) = 0.35
        _Smoothness("Smoothness", Range(0, 1)) = 0.08
        [Toggle] _Core("Middle only", Float) = 0
    }

    SubShader
    {
        // After the terrain and the buildings, which then hide what is under them; blended, so
        // the ground shows through the fading edge.
        Tags { "RenderType" = "Transparent" "Queue" = "Geometry+10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma target 3.0
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DistanceFog.hlsl"

            TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
            TEXTURE2D(_EdgeMap);        SAMPLER(sampler_EdgeMap);
            TEXTURE2D(_EdgeBumpMap);    SAMPLER(sampler_EdgeBumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _EdgeMap_ST;
                half4 _BaseColor;
                half4 _EdgeColor;
                float _Tile;
                float _EdgeTile;
                half _BumpScale;
                half _Shoulder;
                half _ShoulderSoftness;
                half _Fade;
                half _FadeWidth;
                half _Ragged;
                float _NoiseScale;
                half _HeightBlend;
                half _Smoothness;
                half _Core;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Gradient noise, not value noise: value noise's contours follow its grid, and at a steep fade
            // they showed as square notches along the road's edge.
            float2 Gradient(float2 cell)
            {
                float a = Hash(cell) * 6.2831853;
                return float2(cos(a), sin(a));
            }

            float GradientNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
                float n00 = dot(Gradient(i), f);
                float n10 = dot(Gradient(i + float2(1, 0)), f - float2(1, 0));
                float n01 = dot(Gradient(i + float2(0, 1)), f - float2(0, 1));
                float n11 = dot(Gradient(i + float2(1, 1)), f - float2(1, 1));
                return lerp(lerp(n00, n10, u.x), lerp(n01, n11, u.x), u.y) * 1.4142;
            }

            // -1 to 1: big wobbles with some finer ones on top, each octave turned so no grid lines up
            float EdgeNoise(float2 p)
            {
                const float2x2 turn = float2x2(0.8, -0.6, 0.6, 0.8);
                float n = GradientNoise(p) * 0.6;
                p = mul(turn, p) * 2.7 + 13.1;
                n += GradientNoise(p) * 0.3;
                p = mul(turn, p) * 2.7 + 5.7;
                n += GradientNoise(p) * 0.1;
                return clamp(n * 1.5, -1.0, 1.0);
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 world = input.positionWS.xz;
                float2 surfaceUV = world / max(_Tile, 0.01) * _BaseMap_ST.xy + _BaseMap_ST.zw;
                float2 edgeUV = world / max(_EdgeTile, 0.01) * _EdgeMap_ST.xy + _EdgeMap_ST.zw;

                half4 surface = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, surfaceUV) * _BaseColor;
                half4 shoulder = SAMPLE_TEXTURE2D(_EdgeMap, sampler_EdgeMap, edgeUV) * _EdgeColor;
                half3 surfaceNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, surfaceUV), _BumpScale);
                half3 shoulderNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_EdgeBumpMap, sampler_EdgeBumpMap, edgeUV), _BumpScale);

                // 0 in the middle, 1 at the edge
                float edge = saturate(input.uv.x);
                float noise = EdgeNoise(world * _NoiseScale);
                float fine = EdgeNoise(world * _NoiseScale * 4.0 + 31.7);

                // brightness stands in for height: stones are lighter than the dirt between them
                half surfaceHeight = dot(surface.rgb, half3(0.333, 0.333, 0.333));
                half shoulderHeight = dot(shoulder.rgb, half3(0.333, 0.333, 0.333));
                float toShoulder = edge + noise * _Ragged + (shoulderHeight - surfaceHeight) * _HeightBlend;
                half shoulderMask = smoothstep(_Shoulder - _ShoulderSoftness, _Shoulder + _ShoulderSoftness, toShoulder);

                float toGround = edge + noise * _Ragged * 1.2 + fine * _Ragged * 0.6;
                half alpha = 1.0 - smoothstep(_Fade, _Fade + _FadeWidth, toGround);
                // always gone by the mesh's edge, whatever the noise
                alpha *= saturate((1.0 - edge) * 10.0);

                half3 albedo = lerp(surface.rgb, shoulder.rgb, shoulderMask);
                half3 normalTS = normalize(lerp(surfaceNormal, shoulderNormal, shoulderMask));
                if (_Core > 0.5)
                {
                    albedo = surface.rgb;
                    normalTS = surfaceNormal;
                    alpha *= 1.0 - shoulderMask;
                }

                // world space mapping: tangent along x, bitangent along z
                half3 normalWS = normalize(input.normalWS);
                half3 tangentWS = normalize(half3(1, 0, 0) - normalWS * normalWS.x);
                half3 bitangentWS = cross(tangentWS, normalWS);
                half3x3 tangentToWorld = half3x3(tangentWS, bitangentWS, normalWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.metallic = 0;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = 1;
                surfaceData.alpha = alpha;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.tangentToWorld = tangentToWorld;
                inputData.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld));
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
                giParams.isSurfaceTypeTransparent = true;
                InitializeBakedGI(giParams, inputData.bakedGI, inputData.shadowMask);

                half4 color = (half4)UniversalFragmentPBR(inputData, surfaceData,
                    false, true, false, false, true, true, false);
                color.rgb = BlendDistanceFog(color.rgb, input.positionCS);
                color.a = alpha;
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
