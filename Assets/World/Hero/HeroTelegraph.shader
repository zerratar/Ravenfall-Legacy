// Hero mode's enemy moves, seen coming (WaveRun.Moves): a circle round a boss's slam, or a line ahead of a lunge or a
// bolt, drawn on a flat quad on the ground. A faint fill with a bright rim, and a brighter fill growing out from the
// enemy as _Progress goes from 0 to 1: when it reaches the rim, the blow lands. _Line 1 draws the line (the quad's v
// runs from the enemy out), 0 the circle. Unlit, on top of the ground.
Shader "Ravenfall/Hero Telegraph"
{
    Properties
    {
        [MainColor] _BaseColor("Colour", Color) = (1, 0.22, 0.12, 1)
        _Progress("Progress", Range(0, 1)) = 0
        _Line("Line", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
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
                float _Line;
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
                float2 uv = input.uv;
                float inside, rim, fill;
                if (_Line > 0.5)
                {
                    // across the line from its middle (0) to its edges (1), and along it from the enemy (0) out (1)
                    float across = abs(uv.x * 2 - 1);
                    float along = uv.y;
                    float ends = min(along, 1 - along) * 12;
                    inside = (1 - smoothstep(0.9, 1.0, across)) * saturate(ends * 4);
                    rim = saturate(smoothstep(0.72, 0.86, across) * (1 - smoothstep(0.92, 1.0, across)) + (1 - saturate(ends * 1.6)) * (1 - smoothstep(0.9, 1.0, across)));
                    fill = 1 - smoothstep(_Progress - 0.02, _Progress, along);
                }
                else
                {
                    float r = length(uv * 2 - 1);
                    inside = 1 - smoothstep(0.96, 1.0, r);
                    rim = smoothstep(0.84, 0.92, r) * (1 - smoothstep(0.96, 1.0, r));
                    fill = 1 - smoothstep(_Progress - 0.03, _Progress, r);
                }
                // the rim pulses faster as the blow nears
                float pulse = 0.75 + 0.25 * sin(_Time.y * (8 + 16 * _Progress));
                float a = inside * (0.16 + 0.34 * fill) + rim * 0.85 * pulse;
                return half4(_BaseColor.rgb * (1 + 0.5 * fill), saturate(a) * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
