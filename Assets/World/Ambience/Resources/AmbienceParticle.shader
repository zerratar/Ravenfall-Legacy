// The particles of the islands' weather and life (see Ambience): drawn without textures, the shape made in the
// pixel stage from the quad's UVs, so there is nothing to import.
//   _Shape 0: a soft disc (a snowflake)
//   _Shape 1: a petal, a pointed oval lighter toward its tip
//   _Shape 2: a glow, a bright core in a soft halo (a firefly); drawn additive by its material
//   _Shape 3: a butterfly
//   _Shape 4: a gull
//   _Shape 5: a puff of smoke, soft all the way to its middle
//   _Shape 6: a tongue of flame, round at its root and pointed at its tip, hottest low in its middle
//   _Shape 7: a glow without a core, the light falling off softly from its middle to nothing at its edge
// _SoftFade, in metres, fades the particle out where it meets the scene behind it (a glow on the ground, smoke on a
// roof) instead of cutting it off hard; 0 for none.
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
        _SoftFade("Soft against the scene, in metres (0 for none)", Float) = 0
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Shape;
                float _SrcBlend;
                float _DstBlend;
                float _Lit;
                float _Intensity;
                float _SoftFade;
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
                float4 screen : TEXCOORD2;
                float eye : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                output.fog = ComputeFogFactor(output.positionCS.z);
                output.screen = ComputeScreenPos(output.positionCS);
                output.eye = -TransformWorldToView(TransformObjectToWorld(input.positionOS.xyz)).z;
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
                else if (_Shape < 2.5)
                {
                    float r = length(p);
                    alpha = saturate(exp(-r * r * 9.0) + 0.35 * exp(-r * r * 2.2)) * smoothstep(1.0, 0.7, r);
                }
                else if (_Shape < 3.5)
                {
                    // a butterfly: two rounded upper wings, two smaller lower ones, a thin dark body; the wings are
                    // darker at their edges (the particle system beats them by squeezing the quad across)
                    float2 q = float2(abs(p.x), p.y);
                    float upper = length((q - float2(0.45, 0.22)) / float2(0.5, 0.55));
                    float lower = length((q - float2(0.32, -0.42)) / float2(0.32, 0.38));
                    float wing = min(upper, lower);
                    float body = abs(p.x) < 0.07 && abs(p.y) < 0.7 ? 1.0 : 0.0;
                    alpha = max(smoothstep(1.0, 0.85, wing), body);
                    colour = body > 0.5 ? colour * 0.2 : colour * lerp(1.08, 0.45, smoothstep(0.55, 1.0, wing));
                }
                else if (_Shape < 4.5)
                {
                    // a gull: a thin bent stroke, the wings raised from the body and swept back at the tips
                    float wingY = 0.32 * abs(p.x) - 0.42 * p.x * p.x - 0.05;
                    float thickness = 0.3 * (1.0 - abs(p.x)) + 0.09;
                    alpha = smoothstep(thickness, thickness * 0.4, abs(p.y - wingY)) * smoothstep(0.98, 0.85, abs(p.x));
                    colour = lerp(colour * 0.55, colour, smoothstep(0.75, 0.3, abs(p.x)));
                }
                else if (_Shape < 5.5)
                {
                    float r2 = saturate(dot(p, p));
                    alpha = (1.0 - r2) * (1.0 - r2);
                    // a little darker under its middle, as if lit from above
                    colour *= lerp(0.82, 1.06, saturate(p.y * 0.5 + 0.5));
                }
                else if (_Shape < 6.5)
                {
                    float t = p.y * 0.5 + 0.5;
                    float width = max(0.62 * pow(saturate(1.0 - t), 0.6) * saturate(t * 4.0 + 0.3), 0.001);
                    alpha = smoothstep(width, width * 0.1, abs(p.x)) * smoothstep(0.0, 0.15, t);
                    colour *= 1.0 + 0.8 * smoothstep(width * 0.6, 0.0, abs(p.x)) * saturate(1.0 - t * 1.6);
                }
                else
                {
                    // exp(-4 r^2), lowered to reach nothing at the edge
                    alpha = saturate((exp(-4.0 * dot(p, p)) - 0.0183) / 0.9817);
                }
                alpha *= input.color.a;
                if (_SoftFade > 0.0)
                {
                    float scene = LinearEyeDepth(SampleSceneDepth(input.screen.xy / input.screen.w), _ZBufferParams);
                    alpha *= saturate((scene - input.eye) / _SoftFade);
                }
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
