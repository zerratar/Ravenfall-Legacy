// Ravenfall/Terrain: the islands' ground. What the Triplanar Terrain graph did, and more:
//
//   - sand below the beach line and the grass texture above it, blended over a metre either side and by how much the
//     ground (and its normal map) faces up; a footpath channel draws dirt (darken), a snow channel draws snow
//     (lighten), both from the terrain's own splat maps; as the graph did, with its property names, so a material
//     made for it works here;
//   - the grass is not one flat colour any more: two slow noise fields tint it lusher or drier over tens of metres,
//     and a second sample of the grass texture, turned and at another size, breaks up the tiling;
//   - the sand darkens where the sea wets it;
//   - the painted layers the graph ignored are drawn: the second grass where it is painted (the terrain's first splat
//     map's alpha), mud and rock (the second map's red and blue). The first layer (meadow dirt) stays grass, as it is
//     the terrain's default and covers whole islands.
//
// The variation costs one more texture read and a few noise lookups a pixel, and a painted layer a read where it is. Lit like the rest of the scene: shadows,
// cookies (the cloud shadows), ambient occlusion, additional lights, fog.
Shader "Ravenfall/Terrain"
{
    Properties
    {
        [NoScaleOffset] _Main("Sand (below the beach line)", 2D) = "white" {}
        [NoScaleOffset] _Normal("Normal", 2D) = "bump" {}
        _Normal_Strength("Normal strength", Range(0, 2)) = 0.5
        _MainUVScale("Sand and normal tiling (over the terrain)", Vector) = (500, 500, 0, 0)
        [NoScaleOffset] _Secondary("Grass", 2D) = "white" {}
        _SecondaryUVScale("Grass tiling (over the terrain)", Vector) = (500, 500, 0, 0)
        _SecondaryHeight("Beach line (world height)", Float) = -3.11
        _Threshold("How much the ground must face up for grass", Range(0, 1)) = 0.04
        _MainSmoothness("Sand smoothness", Range(0, 1)) = 0
        _SecondarySmoothness("Grass smoothness", Range(0, 1)) = 0
        _MainMetallic("Sand metallic", Range(0, 1)) = 0
        _SecondaryMetalic("Grass metallic", Range(0, 1)) = 0
        [NoScaleOffset] _Road("Footpath", 2D) = "white" {}
        [NoScaleOffset] _RoadMask("Footpath mask (the terrain's first splat map)", 2D) = "black" {}
        [NoScaleOffset] _SnowMask("Snow mask (the terrain's second splat map)", 2D) = "black" {}
        _SnowColor("Snow", Color) = (0.867, 0.98, 1, 1)
        _Specular("Specular (grey); the graph had 0.5", Range(0, 1)) = 0.4
        _Road_Edge_1("Footpath edge from", Range(0, 1)) = 0
        _Road_Edge_2("Footpath edge to", Range(0, 1)) = 1

        [Header(Variation)]
        _GrassTint("Grass colour", Color) = (0.72, 0.88, 0.96, 1)
        _LushTint("Lush grass tint", Color) = (0.86, 1.03, 1.02, 1)
        _DryTint("Dry grass tint", Color) = (1.14, 0.98, 0.92, 1)
        _Variation("How much the grass varies", Range(0, 1)) = 0.6
        _VariationScale("Size of the variation (m)", Float) = 70
        _AntiTile("Tiling break-up", Range(0, 1)) = 0.5
        _WetSand("Wet sand darkening", Range(0, 1)) = 0.35
        _Brightness("Brightness of the ground", Range(0.3, 1.5)) = 0.8
        _SeaLevel("Sea level (world height)", Float) = -3.31

        [Header(Painted layers)]
        [NoScaleOffset] _Grass2("Second grass", 2D) = "white" {}
        [NoScaleOffset] _Mud("Mud", 2D) = "white" {}
        [NoScaleOffset] _Rock("Rock", 2D) = "white" {}
        _LayerUVScale("Painted layers tiling (over the terrain)", Vector) = (500, 500, 0, 0)
        _Grass2Tint("Second grass colour", Color) = (0.8, 0.95, 1.0, 1)
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    TEXTURE2D(_Main);       SAMPLER(sampler_Main);
    TEXTURE2D(_Normal);     SAMPLER(sampler_Normal);
    TEXTURE2D(_Secondary);  SAMPLER(sampler_Secondary);
    TEXTURE2D(_Road);       SAMPLER(sampler_Road);
    TEXTURE2D(_RoadMask);   SAMPLER(sampler_RoadMask);
    TEXTURE2D(_SnowMask);   SAMPLER(sampler_SnowMask);
    TEXTURE2D(_Grass2);     SAMPLER(sampler_Grass2);
    TEXTURE2D(_Mud);        SAMPLER(sampler_Mud);
    TEXTURE2D(_Rock);       SAMPLER(sampler_Rock);

    CBUFFER_START(UnityPerMaterial)
        float4 _MainUVScale;
        float4 _SecondaryUVScale;
        float4 _LayerUVScale;
        half4 _Grass2Tint;
        half4 _SnowColor;
        half4 _LushTint;
        half4 _DryTint;
        half4 _GrassTint;
        float _SecondaryHeight;
        float _SeaLevel;
        float _VariationScale;
        half _Normal_Strength;
        half _Threshold;
        half _MainSmoothness;
        half _SecondarySmoothness;
        half _MainMetallic;
        half _SecondaryMetalic;
        half _Road_Edge_1;
        half _Road_Edge_2;
        half _Variation;
        half _AntiTile;
        half _WetSand;
        half _Specular;
        half _Brightness;
    CBUFFER_END
    // how wet the rain has made the ground, 0 to 1 (Weather)
    float _RavenfallWetness;

    struct Attributes
    {
        float4 positionOS : POSITION;
        float3 normalOS : NORMAL;
        float2 uv : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    // smooth value noise in 2D, 0..1, for the slow variation
    float TerrainHash(float2 p)
    {
        p = frac(p * float2(0.1031, 0.1030));
        p += dot(p, p.yx + 33.33);
        return frac((p.x + p.y) * p.x);
    }

    float TerrainNoise(float2 p)
    {
        float2 i = floor(p), f = frac(p);
        f = f * f * (3.0 - 2.0 * f);
        float a = TerrainHash(i), b = TerrainHash(i + float2(1, 0)), c = TerrainHash(i + float2(0, 1)), d = TerrainHash(i + float2(1, 1));
        return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" "TerrainCompatible" = "True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

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
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float2 uv = input.uv;
                float3 positionWS = input.positionWS;
                float2 uvMain = uv * _MainUVScale.xy;
                float2 uvGrass = uv * _SecondaryUVScale.xy;

                half3 sand = SAMPLE_TEXTURE2D(_Main, sampler_Main, uvMain).rgb;
                half3 grass = SAMPLE_TEXTURE2D(_Secondary, sampler_Secondary, uvGrass).rgb;

                // the tiling broken up: a second read of the grass, turned and at another size, faded in and out by
                // a slow field so no repeat lines up across the meadow
                float2 turned = float2(uvGrass.x * 0.8 - uvGrass.y * 0.6, uvGrass.x * 0.6 + uvGrass.y * 0.8) * 0.43 + 0.37;
                half3 grassB = SAMPLE_TEXTURE2D(_Secondary, sampler_Secondary, turned).rgb;
                float mixB = smoothstep(0.3, 0.7, TerrainNoise(positionWS.xz / 17.0 + 3.1)) * _AntiTile;
                grass = lerp(grass, grassB, mixB);

                // the meadow's own colour: lusher here, drier there, over tens of metres
                float slow = TerrainNoise(positionWS.xz / _VariationScale) * 0.65 + TerrainNoise(positionWS.xz / (_VariationScale * 0.36) + 7.3) * 0.35;
                half3 tint = lerp(_LushTint.rgb, _DryTint.rgb, smoothstep(0.25, 0.75, slow));
                // the splat maps: the first holds layers 0 to 3 (meadow dirt, footpath, grass, second grass), the second
                // 4 to 7 (mud, snow, rock)
                half4 splat0 = SAMPLE_TEXTURE2D(_RoadMask, sampler_RoadMask, uv);
                half4 splat1 = SAMPLE_TEXTURE2D(_SnowMask, sampler_SnowMask, uv);
                float2 uvLayer = uv * _LayerUVScale.xy;

                // the second grass where it is painted, its share of the grassy layers
                half grassy = splat0.r + splat0.b + splat0.a;
                half second = grassy > 0.001 ? splat0.a / grassy : 0.0;
                [branch] if (second > 0.004)
                {
                    half3 grass2 = SAMPLE_TEXTURE2D(_Grass2, sampler_Grass2, uvLayer).rgb * _Grass2Tint.rgb / max(_GrassTint.rgb, 0.01);
                    grass = lerp(grass, grass2, second);
                }

                grass *= lerp(half3(1, 1, 1), tint, _Variation);
                grass *= _GrassTint.rgb;

                half4 packedNormal = SAMPLE_TEXTURE2D(_Normal, sampler_Normal, uvMain);
                half3 normalTS = UnpackNormalScale(packedNormal, _Normal_Strength);

                // world space mapping: tangent along x, bitangent along z
                half3 geometricNormal = normalize(input.normalWS);
                half3 tangentWS = normalize(half3(1, 0, 0) - geometricNormal * geometricNormal.x);
                half3 bitangentWS = cross(tangentWS, geometricNormal);
                half3x3 tangentToWorld = half3x3(tangentWS, bitangentWS, geometricNormal);
                half3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld));

                // sand below the beach line, grass above it where the ground faces up
                float height = smoothstep(_SecondaryHeight - 1.0, _SecondaryHeight + 1.0, positionWS.y);
                half onGrass = smoothstep(0.0, 1.0 - _Threshold, height * normalWS.y);
                // the sand is darker where the sea wets it
                half wet = 1.0 - smoothstep(_SeaLevel - 0.3, _SeaLevel + 0.9, positionWS.y);
                sand *= 1.0 - wet * _WetSand;
                half3 albedo = lerp(sand, grass, onGrass);
                // mud and rock where they are painted
                [branch] if (splat1.r > 0.004) albedo = lerp(albedo, SAMPLE_TEXTURE2D(_Mud, sampler_Mud, uvLayer).rgb, splat1.r);
                [branch] if (splat1.b > 0.004) albedo = lerp(albedo, SAMPLE_TEXTURE2D(_Rock, sampler_Rock, uvLayer * 0.8).rgb, splat1.b);
                albedo *= _Brightness;

                // snow and the footpaths from the splat maps, blended as the graph did (lighten, darken)
                half snow = splat1.g;
                albedo = lerp(albedo, max(albedo, _SnowColor.rgb * snow), snow);
                half path = splat0.g;
                half3 road = SAMPLE_TEXTURE2D(_Road, sampler_Road, uv).rgb;
                albedo = lerp(albedo, min(albedo, road * path), smoothstep(_Road_Edge_1, _Road_Edge_2, path));
                // after rain the ground is darker and has a sheen, the bare paths and sand most, snow not at all
                half soaked = _RavenfallWetness * (1.0 - snow);
                albedo *= 1.0 - soaked * lerp(0.32, 0.2, onGrass);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.specular = _Specular.xxx;
                surfaceData.metallic = 0;
                surfaceData.smoothness = lerp(_MainSmoothness, _SecondarySmoothness, onGrass) + wet * 0.25 * (1.0 - onGrass) + soaked * lerp(0.45, 0.2, onGrass);
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = 1;
                surfaceData.alpha = 1;

                InputData inputData = (InputData)0;
                inputData.positionWS = positionWS;
                inputData.positionCS = input.positionCS;
                inputData.tangentToWorld = tangentToWorld;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
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
                    true, true, false, false, true, false, true);   // specular workflow (as the graph), highlights, opaque, sky reflections
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
            #pragma target 3.0
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            float4 ShadowVertex(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
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
            #pragma target 3.0
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing

            float4 DepthVertex(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.positionOS.xyz);
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
            #pragma target 3.0
            #pragma vertex NormalsVertex
            #pragma fragment NormalsFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            struct NormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            NormalsVaryings NormalsVertex(Attributes input)
            {
                NormalsVaryings output = (NormalsVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 NormalsFragment(NormalsVaryings input) : SV_Target
            {
                // the normal map's relief too, so ambient occlusion sees the same ground the light does
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal, sampler_Normal, input.uv * _MainUVScale.xy), _Normal_Strength);
                half3 n = normalize(input.normalWS);
                half3 t = normalize(half3(1, 0, 0) - n * n.x);
                half3 normalWS = normalize(TransformTangentToWorld(normalTS, half3x3(t, cross(t, n), n)));
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
