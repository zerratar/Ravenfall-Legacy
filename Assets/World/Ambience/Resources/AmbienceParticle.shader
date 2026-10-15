// The particles of the islands' weather and life (see Ambience): drawn without textures, the shape made in the
// pixel stage from the quad's UVs, so there is nothing to import.
//   _Shape 0: a soft disc (a snowflake)
//   _Shape 1: a petal, a pointed oval lighter toward its tip
//   _Shape 2: a glow, a bright core in a soft halo (a firefly); drawn additive by its material
// _AmbienceLight is the light the flake or petal is lit by (the sun and the sky now), so snow at night is not white.
Shader "Hidden/Ravenfall/AmbienceParticle"
{
    Properties
    {
        _Shape("Shape", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination blend", Float) = 10
        _Lit("Lit by the scene (1) or glowing (0)", Float) = 1
        _Intensity("Brightness", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Ambience"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Shape;
                float _SrcBlend;
                float _DstBlend;
                float _Lit;
                float _Intensity;
            CBUFFER_END
            float4 _AmbienceLight;

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float fog : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                half alpha;
                half3 colour = input.color.rgb;
                if (_Shape < 0.5)
                {
                    alpha = smoothstep(1.0, 0.25, length(p));
                }
                else if (_Shape < 1.5)
                {
                    // a pointed oval: narrow toward both ends, the tip lighter
                    float along = p.y;
                    float width = 0.55 * (1.0 - along * along);
                    alpha = smoothstep(width, width * 0.6, abs(p.x)) * smoothstep(1.0, 0.85, abs(along));
                    colour = lerp(colour * 0.82, colour * 1.12, saturate(along * 0.5 + 0.5));
                }
                else
                {
                    float r = length(p);
                    alpha = saturate(exp(-r * r * 9.0) + 0.35 * exp(-r * r * 2.2)) * smoothstep(1.0, 0.7, r);
                }
                alpha *= input.color.a;
                colour *= _Intensity;
                if (_Lit > 0.5) colour *= _AmbienceLight.rgb;
                colour = MixFog(colour, input.fog);
                // additive glows carry their strength in the colour
                if (_DstBlend < 1.5) return half4(colour * alpha, 1);
                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
