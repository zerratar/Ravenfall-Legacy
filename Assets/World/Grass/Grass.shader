// Grass, drawn by GrassRenderer through a BatchRendererGroup: thousands of small meshes, each with its own colour
// (the terrain's colour under it, given per instance), swaying in the wind and shrinking away to nothing at the end
// of the grass distance so cells never pop in or out.
//
// The same shader draws the generated blade clumps (white texture, coloured all by the instance) and the Synty tufts
// and flowers (their own texture, pulled towards the ground colour by _GroundTint). The mesh's height decides how
// far a vertex bends and how light it is.
//
// Blending into the ground: at the root a blade has the ground's colour and is lit as if it faced straight up, as
// the terrain is, so grass grows out of the ground without a seam; towards the tip it gets lighter and its own
// normal shows more (_UpNormals). Towards the end of the grass distance it melts into the ground colour, so the
// edge of the grass never shows as a line.
//
// The little tricks: a blade bending in the wind catches more light (_WindLight), so gusts roll across a meadow as
// waves of lighter grass; and with the sun behind it a blade's tip glows a little (_Translucency). Both cost a few
// instructions. Opaque, no alpha test, so the GPU keeps its early depth rejection; it receives shadows and casts
// none.
Shader "Ravenfall/Grass"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Colour (per instance)", Color) = (1, 1, 1, 1)
        _GroundTint("Ground colour, 0 texture only to 1 ground only", Range(0, 1)) = 1
        _Height("Mesh height (object units)", Float) = 1
        _RootShade("Root shade", Range(0, 1)) = 0.1
        _TipLight("Tip light", Range(0, 1)) = 0.3
        _UpNormals("Lit like the ground at the tip", Range(0, 1)) = 0.6
        _Bend("Wind bend", Range(0, 2)) = 1
        _WindLight("Light from bending", Range(0, 1)) = 0.35
        _Translucency("Sun through the tips", Range(0, 1)) = 0.35
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

    CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        half _GroundTint;
        float _Height;
        half _RootShade;
        half _TipLight;
        half _UpNormals;
        half _Bend;
        half _WindLight;
        half _Translucency;
    CBUFFER_END

    #ifdef UNITY_DOTS_INSTANCING_ENABLED
    UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
        UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
    #define _BaseColor UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
    #endif

    // set by GrassRenderer: x, y the wind direction, z strength, w speed; the fade from x to y metres
    float4 _GrassWind;
    float4 _GrassFade;

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
        half height : TEXCOORD3;
        half bend : TEXCOORD4;      // how far the wind bends it now, 0 to 1
        half far : TEXCOORD5;       // 0 near, 1 at the end of the grass distance
        UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings GrassVertex(Attributes input)
    {
        Varyings output = (Varyings)0;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_TRANSFER_INSTANCE_ID(input, output);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        float height = saturate(input.positionOS.y / max(_Height, 0.0001));
        float3 rootWS = TransformObjectToWorld(float3(0, 0, 0));

        // shrink towards the root near the end of the grass distance
        float distance = length(rootWS - _WorldSpaceCameraPos);
        float fade = 1 - saturate((distance - _GrassFade.x) / max(_GrassFade.y - _GrassFade.x, 0.001));
        float3 positionWS = TransformObjectToWorld(input.positionOS.xyz * fade);

        // a wave travelling with the wind, with a slower gust over it; the tip moves, the root stays
        float2 direction = _GrassWind.xy;
        float along = dot(rootWS.xz, direction);
        float wave = sin(_Time.y * _GrassWind.w - along * 0.35 + rootWS.x * 0.11) * 0.5 + 0.5;
        float gust = sin(_Time.y * _GrassWind.w * 0.23 - along * 0.05) * 0.5 + 0.5;
        float push = (0.35 + wave * 0.65) * (0.5 + gust);
        float bend = _GrassWind.z * _Bend * height * height * push;
        positionWS.xz += direction * bend;
        positionWS.y -= bend * bend * 0.35;

        output.positionWS = positionWS;
        output.positionCS = TransformWorldToHClip(positionWS);
        // straight up at the root, like the ground; the blade's own normal shows more towards the tip
        float up = lerp(1, _UpNormals, height);
        output.normalWS = normalize(lerp(TransformObjectToWorldNormal(input.normalOS), float3(0, 1, 0), up));
        output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
        output.height = height;
        output.bend = saturate(push * 0.67) * height;
        output.far = saturate(distance / max(_GrassFade.y, 0.001));
        return output;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex GrassVertex
            #pragma fragment Fragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DistanceFog.hlsl"

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                half3 ground = _BaseColor.rgb;
                half3 albedo = lerp(texel.rgb, ground, _GroundTint);
                // darker at the root, lighter at the tip, lighter still while the wind bends it
                albedo *= lerp(1 - _RootShade, 1 + _TipLight, input.height) * (1 + _WindLight * input.bend);
                // far away it becomes the ground, so the end of the grass is not a line
                albedo = lerp(albedo, ground, input.far * input.far * 0.8);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light light = GetMainLight(shadowCoord);
                half3 normal = normalize(input.normalWS);
                half shadow = light.shadowAttenuation * light.distanceAttenuation;
                // soft wrap: grass is never fully dark on its far side, light shines through blades
                half diffuse = saturate(dot(normal, light.direction) * 0.6 + 0.4);
                half3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                half through = pow(saturate(dot(-view, light.direction)), 3) * input.height * _Translucency;
                half3 lit = albedo * (SampleSH(normal) + light.color * (diffuse + through) * shadow);

                half fog = ComputeFogFactor(TransformWorldToHClip(input.positionWS).z);
                return half4(MixFog(lit, fog), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex GrassVertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            half DepthFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex GrassVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            half4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return half4(NormalizeNormalPerPixel(input.normalWS), 0);
            }
            ENDHLSL
        }
    }
}
