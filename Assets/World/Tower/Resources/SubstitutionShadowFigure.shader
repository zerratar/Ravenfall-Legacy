// The shadow phase's figure (SubstitutionFx, docs/substitution-escape-idea.md): the player's shape baked from their
// meshes, drawn as a near black silhouette with a bright violet rim, so it reads on dark floors (the rim) and on bright
// ones, snow and sand (the dark body). It sinks into the floor where the player stood and rises out of it where they
// appear: everything under _FloorY is cut away, and a glowing band runs along the cut, the line where it meets the floor.
//
// Unlit: the figure is a shadow, light does not touch it. _Fade takes it away at the end.
Shader "Ravenfall/Substitution Shadow Figure"
{
    Properties
    {
        _Color("Body", Color) = (0.02, 0.008, 0.045, 1)
        [HDR] _RimColor("Rim", Color) = (1.3, 0.75, 2.2, 1)
        _RimPower("Rim width (lower is wider)", Range(0.5, 8)) = 4.5
        [HDR] _EdgeColor("Band at the floor", Color) = (2.2, 1.4, 3.4, 1)
        _EdgeWidth("Band height (m)", Range(0.01, 0.4)) = 0.12
        _FloorY("Floor height (world)", Float) = -10000
        _Fade("Fade", Range(0, 1)) = 1
    }

    SubShader
    {
        // with the transparent effects, so the smoke and wisps around it blend over it
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _RimColor;
                half _RimPower;
                half4 _EdgeColor;
                half _EdgeWidth;
                float _FloorY;
                half _Fade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half fog : TEXCOORD2;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 Fragment(Varyings i) : SV_Target
            {
                // under the floor: gone
                float above = i.positionWS.y - _FloorY;
                clip(above);

                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                half rim = pow(saturate(1.0 - abs(dot(n, v))), _RimPower);
                half3 color = lerp(_Color.rgb, _RimColor.rgb, rim);

                // the glowing band where it meets the floor
                half band = 1.0 - saturate(above / max(_EdgeWidth, 0.001));
                color = lerp(color, _EdgeColor.rgb, band * band);

                color = MixFog(color, i.fog);
                return half4(color, _Color.a * _Fade);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
