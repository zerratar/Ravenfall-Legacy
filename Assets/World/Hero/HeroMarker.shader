// Hero mode's click marker (HeroController): a ring on the ground where the hero was sent, growing and fading as
// _Progress goes from 0 to 1. Drawn on a flat quad, unlit, on top of the ground.
Shader "Ravenfall/Hero Marker"
{
    Properties
    {
        [MainColor] _BaseColor("Colour", Color) = (1, 0.82, 0.35, 1)
        _Progress("Progress", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            Offset -2, -2

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Progress;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float r = length(input.uv * 2 - 1);
                float t = _Progress;
                float eased = 1 - (1 - t) * (1 - t);
                // an outer ring that grows, and an inner one that shrinks to the point
                float outer = 1 - smoothstep(0.0, 0.07, abs(r - lerp(0.35, 0.95, eased)));
                float inner = (1 - smoothstep(0.0, 0.06, abs(r - lerp(0.45, 0.05, eased)))) * (1 - t);
                float centre = (1 - smoothstep(0.06, 0.12, r)) * (1 - t) * 0.8;
                float a = saturate(outer + inner + centre) * (1 - t) * _BaseColor.a;
                return half4(_BaseColor.rgb * 1.6, a);
            }
            ENDHLSL
        }
    }
}
