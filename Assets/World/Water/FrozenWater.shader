// A frozen pond (FrozenPond): clear ice you look down into, laid over the pond's flat bed.
//
//   - depth: the water under the ice is pale at the shore and deep blue-green in the middle; stones and weed lie dark on
//     the bottom and air bubbles hang frozen at three depths, all shifted with the view (parallax), so the ice has depth
//     as the camera moves
//   - cracks: a net of fine white fractures, thicker in places and missing in others, with a fainter copy of it deeper
//     in the ice
//   - frost: matte white patches drifting over it, thickest at the shore, where it fades raggedly into the snow
//   - light: the sun glints off the clear ice and sparkles on the frost; the sky sits in it by Fresnel (SkyReflection's
//     live sky when there is one); the scene's ambient from SceneAmbient; it takes the sun's shadows
//   - holes: up to 8 holes cut in the ice (ice fishing), dark open water with a slow ripple and a rim of slush, set per
//     pond by FrozenPond as world x, z and radius
//
// The mesh is a disc: uv is the local position over the disc's radius, so length(uv) is 0 in the middle and 1 at the rim.
Shader "Ravenfall/Frozen Water"
{
    Properties
    {
        _ShallowColor("Water at the shore", Color) = (0.42, 0.66, 0.78, 1)
        _DeepColor("Water in the middle", Color) = (0.03, 0.15, 0.24, 1)
        _FrostColor("Frost", Color) = (0.93, 0.96, 1, 1)
        _CrackColor("Cracks", Color) = (0.86, 0.95, 1, 1)
        _HoleColor("Open water in a hole", Color) = (0.015, 0.05, 0.07, 1)
        _Depth("Depth seen through the ice (m)", Range(0.1, 3)) = 1.1
        _CrackScale("Crack cells per metre", Float) = 0.55
        _Cracks("Cracks", Range(0, 1)) = 0.75
        _Frost("Frost", Range(0, 1)) = 0.45
        _Bubbles("Bubbles", Range(0, 1)) = 0.6
        _Glint("Sun glint", Range(0, 3)) = 1.4
        _Sky("Sky in the ice", Range(0, 1)) = 0.5
    }

    SubShader
    {
        // after the terrain and the roads, with the other things laid flat on the ground
        Tags { "RenderType" = "Transparent" "Queue" = "Geometry+20" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FrostColor;
                half4 _CrackColor;
                half4 _HoleColor;
                float _Depth;
                float _CrackScale;
                half _Cracks;
                half _Frost;
                half _Bubbles;
                half _Glint;
                half _Sky;
            CBUFFER_END

            // per pond (FrozenPond, a property block): the holes, world x, z and radius
            float4 _Holes[8];
            float _HoleCount;

            // the scene's ambient light (SceneAmbient)
            float4 _RavenfallSH[7];
            // the live sky (SkyReflection), when there is one
            TEXTURECUBE(_RavenfallSky); SAMPLER(sampler_RavenfallSky);
            float _RavenfallHasSky;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float fog : TEXCOORD2;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 Hash2(float2 p)
            {
                return float2(Hash(p), Hash(p + 19.19));
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            float Fbm(float2 p)
            {
                return Noise(p) * 0.55 + Noise(p * 2.13 + 7.1) * 0.3 + Noise(p * 4.37 + 3.3) * 0.15;
            }

            // how far a point is from the nearest edge between Voronoi cells: 0 on a crack
            float CrackDistance(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                float d1 = 8, d2 = 8;
                float2 nearest = 0;
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float2 o = float2(x, y);
                    float2 c = o + Hash2(cell + o) * 0.85 + 0.075 - f;
                    float d = dot(c, c);
                    if (d < d1) { d2 = d1; d1 = d; nearest = c; }
                    else if (d < d2) d2 = d;
                }
                // the distance to the bisector between the two nearest points
                float best = 8;
                for (int yy = -1; yy <= 1; yy++)
                for (int xx = -1; xx <= 1; xx++)
                {
                    float2 o = float2(xx, yy);
                    float2 c = o + Hash2(cell + o) * 0.85 + 0.075 - f;
                    float2 dd = c - nearest;
                    if (dot(dd, dd) > 1e-5) best = min(best, dot(0.5 * (nearest + c), normalize(dd)));
                }
                return best;
            }

            // air bubbles frozen at one depth: small round specks in some cells
            // a hash with no lines in it (Hash put the bubbles in dotted rows)
            float3 Hash3(float2 p)
            {
                float3 q = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                q += dot(q, q.yxz + 33.33);
                return frac((q.xxy + q.yzz) * q.zyx);
            }

            float Bubbles(float2 p, float seed)
            {
                p = float2(p.x * 0.8 - p.y * 0.6, p.x * 0.6 + p.y * 0.8) + seed * 17.0;
                float2 cell = floor(p);
                float2 f = frac(p) - 0.5;
                float3 h = Hash3(cell * 1.37 + seed);
                float there = step(0.8, h.z);
                float2 c = (h.xy - 0.5) * 0.6;
                float r = 0.06 + 0.1 * frac(h.x * 7.31 + h.y * 3.17);
                float d = length(f - c);
                return there * (1 - smoothstep(r * 0.55, r, d));
            }

            half3 SceneAmbientLight(half3 normal)
            {
                half4 n = half4(normal, 1);
                half3 x1 = half3(dot(_RavenfallSH[0], n), dot(_RavenfallSH[1], n), dot(_RavenfallSH[2], n));
                half4 b = n.xyzz * n.yzzx;
                half3 x2 = half3(dot(_RavenfallSH[3], b), dot(_RavenfallSH[4], b), dot(_RavenfallSH[5], b));
                half3 x3 = _RavenfallSH[6].xyz * (n.x * n.x - n.y * n.y);
                half3 sh = max(half3(0, 0, 0), x1 + x2 + x3);
                // before SceneAmbient has set it (in the editor at once, in the game always): the flat ambient
                return _RavenfallSH[0].w + _RavenfallSH[1].w + _RavenfallSH[2].w > 0.0001 ? sh : unity_AmbientSky.rgb;
            }

            float3 Sky(float3 dir, float roughness)
            {
                float3 sky;
                [branch] if (_RavenfallHasSky > 0.5)
                    sky = SAMPLE_TEXTURECUBE_LOD(_RavenfallSky, sampler_RavenfallSky, dir, PerceptualRoughnessToMipmapLevel(roughness)).rgb;
                else
                    sky = GlossyEnvironmentReflection(dir, roughness, 1.0);
                // the live sky is HDR, and straight up it can hold values far too big (seen from above the whole pond went a
                // flat cyan): clamped first (min and max also drop a NaN), then softly held under 1.2
                sky = min(max(sky, 0), 4);
                return sky * 1.2 / (1.0 + sky);
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float3 P = input.positionWS;
                float2 xz = P.xz;
                float r = length(input.uv);
                float3 V = normalize(_WorldSpaceCameraPos - P);
                float t = _Time.y;
                float n = Fbm(xz * 0.45);

                // ---- under the ice: water deepening to the middle, the bottom, bubbles, all shifted with the view
                float2 shift = -V.xz / max(V.y, 0.25);
                float deep = smoothstep(1.0, 0.15, r + (n - 0.5) * 0.25);
                half3 body = lerp(_ShallowColor.rgb, _DeepColor.rgb, deep);
                float2 bottom = xz + shift * _Depth;
                float stones = smoothstep(0.58, 0.78, Fbm(bottom * 0.7 + 11.3)) * (1 - deep * 0.6);
                float weed = smoothstep(0.62, 0.8, Noise(bottom * 1.9 + 4.4)) * deep;
                body *= 1 - 0.45 * stones - 0.3 * weed;
                float bubbles = 0;
                bubbles += Bubbles((xz + shift * _Depth * 0.15) * 3.1, 1.0) * 0.9;
                bubbles += Bubbles((xz + shift * _Depth * 0.4) * 2.3, 7.0) * 0.6;
                bubbles += Bubbles((xz + shift * _Depth * 0.75) * 1.7, 13.0) * 0.35;
                body = lerp(body, _CrackColor.rgb, saturate(bubbles * _Bubbles));

                // ---- cracks: a net of fractures, thicker here, missing there, and a faint copy deeper in
                float2 cp = xz * _CrackScale + (Noise(xz * 0.8) - 0.5) * 0.35;
                float width = 0.012 + 0.03 * Noise(xz * 0.9 + 5.0);
                float present = smoothstep(0.45, 0.75, Noise(xz * 0.22 + 2.2)) * (0.55 + 0.45 * Noise(xz * 0.9 + 8.0));
                float crack = (1 - smoothstep(0, width, CrackDistance(cp))) * present;
                float crackDeep = (1 - smoothstep(0, width * 1.6, CrackDistance(cp * 0.6 + 3.7 + shift * _Depth * 0.3 * _CrackScale))) * 0.45;
                body = lerp(body, _CrackColor.rgb, saturate(max(crack, crackDeep * present) * _Cracks));

                // ---- frost: matte patches, thickest at the shore
                float frost = saturate(smoothstep(0.5, 0.78, Fbm(xz * 0.28 + 9.1)) * _Frost * 1.6 + smoothstep(0.7, 1.0, r + (n - 0.5) * 0.3));

                // ---- light
                float4 shadowCoord = TransformWorldToShadowCoord(P);
                Light sun = GetMainLight(shadowCoord);
                half shadow = sun.shadowAttenuation;
                float3 N = float3(0, 1, 0);
                // the ice is not quite flat: a gentle unevenness breaks up the glint
                float2 tilt = float2(Noise(xz * 1.7) - 0.5, Noise(xz * 1.7 + 31.0) - 0.5) * 0.08;
                float3 Ni = normalize(float3(tilt.x, 1, tilt.y));
                half3 ambient = SceneAmbientLight(N);
                half nl = saturate(dot(N, sun.direction));
                // the water seen through the ice is lit from above, through it
                half3 ice = body * (ambient + sun.color * (0.35 + 0.45 * nl) * shadow);
                half3 frosted = _FrostColor.rgb * (ambient * 1.1 + sun.color * nl * shadow);

                // the sky in the ice, and the sun's glint off it
                float3 R = reflect(-V, Ni);
                float fresnel = 0.04 + 0.96 * pow(1 - saturate(dot(V, Ni)), 5);
                ice = lerp(ice, Sky(R, 0.08), saturate(fresnel * _Sky + 0.06));
                float3 H = normalize(sun.direction + V);
                half glint = pow(saturate(dot(Ni, H)), 280) * 6 + pow(saturate(dot(Ni, H)), 40) * 0.25;
                ice += sun.color * glint * _Glint * shadow;
                // frost sparkles here and there as the view moves
                float2 sc = floor(xz * 9);
                float sparkle = step(0.985, Hash(sc + floor(V.xz * 6)));
                frosted += sun.color * sparkle * shadow * 1.5 * nl;

                half3 color = lerp(ice, frosted, frost);

                // ---- holes cut in the ice: open dark water, a slow ripple, a rim of slush
                for (int k = 0; k < 8; k++)
                {
                    if (k >= (int)_HoleCount) break;
                    float4 hole = _Holes[k];
                    float d = length(xz - hole.xy) / max(hole.z, 0.01) + (Noise(xz * 6 + k) - 0.5) * 0.12;
                    if (d > 1.5) continue;
                    // dark open water, a touch lighter toward its edge, the sky faintly in it, a slow shimmer
                    half3 water = _HoleColor.rgb * (ambient * 0.6 + 0.25) * (0.75 + 0.5 * smoothstep(0.3, 1.0, d));
                    water += Sky(reflect(-V, float3(0, 1, 0)), 0.05) * (fresnel * 0.5 + 0.03);
                    water += sun.color * shadow * 0.04 * smoothstep(0.6, 0.9, Noise(xz * 5 + float2(t * 0.3, -t * 0.2)));
                    float inHole = 1 - smoothstep(0.92, 1.0, d);
                    float slush = smoothstep(0.9, 1.05, d) * (1 - smoothstep(1.2, 1.45, d));
                    color = lerp(color, frosted, slush * 0.85);
                    color = lerp(color, water, inHole);
                }

                // ---- fading raggedly into the snow at the rim
                half alpha = 1 - smoothstep(0.86, 1.0, r + (n - 0.5) * 0.14);
                color = MixFog(color, input.fog);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
