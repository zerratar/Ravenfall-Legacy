// A house's window glass drawn again over itself (see VillageLife): lit from inside, the glass's own texture times a warm
// colour past white, added on, so it glows and blooms; or, with no one home, a dark tint multiplied over it. The house's
// own material (Shader Graphs/Lit) has no emission wired, so it cannot glow by itself.
//   _Mode 0: glow, drawn additive (_SrcBlend One, _DstBlend One)
//   _Mode 1: darken, drawn multiplied (_SrcBlend DstColor, _DstBlend Zero)
Shader "Hidden/Ravenfall/WindowGlow"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        [HDR] _Color("Colour", Color) = (1, 1, 1, 1)
        _Mode("Glow (0) or darken (1)", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination blend", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        // drawn on the glass itself, so pulled a little toward the eye to win the depth test
        Offset -1, -1

        Pass
        {
            Name "WindowGlow"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Color;
                float _Mode;
                float _SrcBlend;
                float _DstBlend;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fog : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // far off in the fog the window fades with everything else
                half seen = 1.0;
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                    seen = saturate(ComputeFogIntensity(input.fog));
                #endif
                if (_Mode > 0.5) return half4(lerp(half3(1, 1, 1), _Color.rgb, seen), 1);
                half3 glass = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                return half4(glass * _Color.rgb * seen, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
