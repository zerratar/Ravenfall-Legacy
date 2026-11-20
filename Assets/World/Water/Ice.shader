// Solid ice (the Frost Vault's pillars and cavern ice, the Glacier's ice cliffs; concept docs/mockups/tower/concept-frostvault.jpg):
// crystal you look into rather than a blue plastic.
//
//   - facets: lit with the face's own normal (from the screen-space change of the position), so every face of the
//     low-poly mesh catches the light differently, as cut crystal does
//   - depth: veins and cracks inside the ice, worked out in 3D at two depths along the view (parallax), the deep ones
//     glowing a cold blue, so the ice looks thick and lit from within
//   - edges: a bright rim where the ice turns away from the camera, and a sharp glint of the sun and of the lights near by
//     (braziers) off the faces
//
// The patterns use the position relative to the object (the tower's places are 6 km out, where world positions lose
// too much precision for hashes).
Shader "Ravenfall/Ice"
{
    Properties
    {
        _SurfaceColor("Surface", Color) = (0.62, 0.84, 0.97, 1)
        _DeepColor("Deep inside", Color) = (0.06, 0.24, 0.42, 1)
        _VeinColor("Veins and cracks", Color) = (0.9, 0.97, 1, 1)
        [HDR] _GlowColor("Glow from within", Color) = (0.12, 0.45, 0.9, 1)
        [HDR] _RimColor("Rim", Color) = (0.7, 0.9, 1.0, 1)
        _VeinScale("Cracks per metre", Float) = 0.3
        _Depth("Depth of the veins (m)", Range(0, 3)) = 1.2
        _Glow("Glow", Range(0, 2)) = 0.6
        _Rim("Rim", Range(0, 2)) = 0.7
        _Glint("Glint", Range(0, 3)) = 1.2
        [Header(Clear ice (things frozen inside it))]
        _Alpha("How much shows through (1 solid)", Range(0, 1)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            // solid by default; clear ice (a block with a find frozen inside) blends over what is in it
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SurfaceColor;
                half4 _DeepColor;
                half4 _VeinColor;
                half4 _GlowColor;
                half4 _RimColor;
                float _VeinScale;
                float _Depth;
                half _Glow;
                half _Rim;
                half _Glint;
                half _Alpha;
            CBUFFER_END

            // the scene's ambient light (SceneAmbient)
            float4 _RavenfallSH[7];

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 local : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fog : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                // relative to the object, in world orientation and metres (for the patterns)
                output.local = output.positionWS - UNITY_MATRIX_M._m03_m13_m23;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            float Hash3(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float Noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(lerp(Hash3(i), Hash3(i + float3(1, 0, 0)), f.x), lerp(Hash3(i + float3(0, 1, 0)), Hash3(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(Hash3(i + float3(0, 0, 1)), Hash3(i + float3(1, 0, 1)), f.x), lerp(Hash3(i + float3(0, 1, 1)), Hash3(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            float3 Hash33(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            // fractures through the ice: the faces between 3D Voronoi cells, straight-sided as ice cracks (ridged noise
            // looked like the light on the floor of a swimming pool); thin, missing in places
            float Cracks(float3 p, float width)
            {
                float3 cell = floor(p);
                float3 f = frac(p);
                float d1 = 8, d2 = 8;
                for (int z = -1; z <= 1; z++)
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float3 o = float3(x, y, z);
                    float3 c = o + Hash33(cell + o) - f;
                    float d = dot(c, c);
                    if (d < d1) { d2 = d1; d1 = d; }
                    else if (d < d2) d2 = d;
                }
                float edge = sqrt(d2) - sqrt(d1);
                float present = smoothstep(0.42, 0.72, Noise3(p * 0.37 + 5.1));
                return (1 - smoothstep(0, width, edge)) * present;
            }

            half3 SceneAmbientLight(half3 normal)
            {
                half4 n = half4(normal, 1);
                half3 x1 = half3(dot(_RavenfallSH[0], n), dot(_RavenfallSH[1], n), dot(_RavenfallSH[2], n));
                half4 b = n.xyzz * n.yzzx;
                half3 x2 = half3(dot(_RavenfallSH[3], b), dot(_RavenfallSH[4], b), dot(_RavenfallSH[5], b));
                half3 x3 = _RavenfallSH[6].xyz * (n.x * n.x - n.y * n.y);
                half3 sh = max(half3(0, 0, 0), x1 + x2 + x3);
                return _RavenfallSH[0].w + _RavenfallSH[1].w + _RavenfallSH[2].w > 0.0001 ? sh : unity_AmbientSky.rgb;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 P = input.positionWS;
                float3 V = normalize(_WorldSpaceCameraPos - P);
                // the face's own normal, so the facets read; turned to the camera if the mesh's points away
                float3 N = normalize(cross(ddy(P), ddx(P)));
                if (dot(N, input.normalWS) < 0) N = -N;
                if (dot(N, V) < 0) N = -N;
                float nv = saturate(dot(N, V));

                // veins near the surface, and deeper ones seen through the ice along the view
                float3 q = input.local * _VeinScale;
                float near = Cracks(q, 0.06);
                float deep = Cracks(q * 0.6 - V * _Depth * _VeinScale + 13.1, 0.12);
                float cloud = Noise3(q * 0.35 + 3.3);

                half3 body = lerp(_DeepColor.rgb, _SurfaceColor.rgb, saturate(0.35 + 0.65 * nv * (0.6 + 0.6 * cloud)));
                body = lerp(body, _VeinColor.rgb, saturate(near * 0.8));

                float4 shadowCoord = TransformWorldToShadowCoord(P);
                Light sun = GetMainLight(shadowCoord);
                half shadow = sun.shadowAttenuation;
                half nl = saturate(dot(N, sun.direction));
                half3 ambient = SceneAmbientLight(N);
                // ice lets light through: lit softly all round, more on the faces to the sun
                half3 color = body * (ambient * 1.1 + sun.color * (0.25 + 0.75 * nl) * shadow);
                float3 H = normalize(sun.direction + V);
                color += sun.color * shadow * (pow(saturate(dot(N, H)), 220) * 3 + pow(saturate(dot(N, H)), 30) * 0.15) * _Glint;

                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = P;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                    uint lightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(lightCount)
                        Light near2 = GetAdditionalLight(lightIndex, P, half4(1, 1, 1, 1));
                        half3 lc = near2.color * near2.distanceAttenuation * near2.shadowAttenuation;
                        half nnl = saturate(dot(N, near2.direction) * 0.7 + 0.3);
                        color += body * lc * nnl;
                        float3 Hn = normalize(near2.direction + V);
                        color += lc * pow(saturate(dot(N, Hn)), 120) * 2 * _Glint;
                    LIGHT_LOOP_END
                #endif

                // the glow from within, along the deep veins and in the ice's cloudy heart
                color += _GlowColor.rgb * _Glow * (deep * 0.9 + 0.12 * cloud) * (0.4 + 0.6 * nv);
                // the rim where the ice turns away
                color += _RimColor.rgb * _Rim * pow(1 - nv, 3) * (0.5 + ambient);

                color = MixFog(color, input.fog);
                // clear ice: what is inside shows through the faces, less at the rim and along the cracks
                half alpha = lerp(_Alpha, 1, saturate(pow(1 - nv, 3) * 0.6 + near * 0.6));
                return half4(color, alpha);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
