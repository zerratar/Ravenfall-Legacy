// Ironhill's lava cracks (LavaCrack): glowing seams in the ground with scorched edges, laid flat on the terrain.
//
// The crack is a strip along a jagged line. uv.x runs along it in metres, uv.y across it, 0 down the middle and 1 at
// the strip's edge, uv.z how far it is from either end of the strip in strip widths; uv2.x is the share of the strip that is open crack (the rest is scorched ground fading into the
// terrain), uv2.y how hot this crack is. The middle of the seam glows yellow, cooling to a deep red at its lips; slow
// blobs of heat drift along it and the whole crack breathes. Around it the ground is burnt dark, with a faint red
// glow spilling over it. Both borders are ragged with a world space noise, so the seam is never a clean line.
//
// Premultiplied alpha: the scorch darkens the ground under it, the glow adds on top.
Shader "Ravenfall/Lava Crack"
{
    Properties
    {
        [HDR] _HotColor("Hot (middle of the seam)", Color) = (6, 2.6, 0.5, 1)
        [HDR] _LipColor("Lips of the seam", Color) = (1.3, 0.1, 0.015, 1)
        [HDR] _SpillColor("Glow on the ground", Color) = (0.9, 0.12, 0.02, 1)
        _ScorchColor("Scorched ground", Color) = (0.05, 0.036, 0.03, 1)
        _Scorch("Scorch strength", Range(0, 1)) = 0.85
        _Spill("Glow spill", Range(0, 1)) = 0.35
        _Ragged("Raggedness", Range(0, 1)) = 0.45
        _NoiseScale("Noise per metre", Float) = 2.2
        _FlowSpeed("Heat drift (m/s)", Float) = 0.35
        _Pulse("Breathing", Range(0, 1)) = 0.25
    }

    SubShader
    {
        // after the terrain and the roads (Geometry+10), before anything see-through
        Tags { "RenderType" = "Transparent" "Queue" = "Geometry+20" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _HotColor;
                half4 _LipColor;
                half4 _SpillColor;
                half4 _ScorchColor;
                half _Scorch;
                half _Spill;
                half _Ragged;
                float _NoiseScale;
                float _FlowSpeed;
                half _Pulse;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fog : TEXCOORD3;
            };

            Varyings Vertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                output.uv2 = input.uv2;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float across = abs(input.uv.y);
                float open = input.uv2.x;
                float heat = input.uv2.y;
                float2 xz = input.positionWS.xz * _NoiseScale;
                float n = Noise(xz) * 0.65 + Noise(xz * 2.7 + 17.3) * 0.35;

                // the seam: wider and narrower along its length, with ragged lips
                // never quite zero: where the seam closes, smoothstep would divide by nothing and leave stray dots
                float seam = max(open * (1 + (n - 0.5) * 2 * _Ragged), 1e-3);
                float inside = (1 - smoothstep(seam * 0.55, seam, across)) * saturate(open * 40);
                float middle = 1 - smoothstep(0, seam * 0.45, across);

                // heat drifting along the seam, and the whole crack breathing
                float t = _Time.y;
                float drift = Noise(float2(input.uv.x * 0.9 - t * _FlowSpeed, 3.1)) * 0.7 + Noise(float2(input.uv.x * 2.3 + t * _FlowSpeed * 0.6, 9.7)) * 0.3;
                float breath = 1 - _Pulse + _Pulse * (0.5 + 0.5 * sin(t * 1.1 + input.uv.x * 0.35));
                float glowMix = saturate(middle * (0.3 + drift * 1.0));
                half3 glow = lerp(_LipColor.rgb, _HotColor.rgb, glowMix) * inside * heat * breath;

                // the burnt ground round it, fading raggedly into the terrain, and the red light spilling onto it
                float edge = across + (n - 0.5) * 0.35 * _Ragged;
                // dark close to the seam, thinning out slowly: charred rock, then soot over the ground
                // and round at the strip's ends: the tip of the seam ends in a burn
                float tip = smoothstep(0, 0.9, input.uv.z + (n - 0.5) * 0.3);
                float scorch = pow(1 - smoothstep(seam, 1, edge), 0.6) * _Scorch * tip;
                float outside = saturate((across - seam) / max(1e-3, 1 - seam));
                half3 spill = _SpillColor.rgb * _Spill * heat * breath * exp(-outside * 6) * (1 - inside);

                half alpha = saturate(max(scorch, inside));
                half3 color = _ScorchColor.rgb * scorch * (1 - inside) + glow + spill;
                color = MixFogColor(color, unity_FogColor.rgb * alpha, input.fog);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
