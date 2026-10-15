// Ravenfall/Ocean: the sea. Not a tinted sheet but water the light goes down through and comes back up out of:
//
//   - four Gerstner waves move the surface (the vertex stage) and shape its normal (the pixel stage, recomputed there
//     so the coarse far rings still have a true swell); where the surface folds over itself the crest breaks into a
//     whitecap;
//   - three scrolling layers of ripple normals ride on the swell and flatten with distance, so the horizon does not
//     sparkle;
//   - what lies under the surface is read from the opaque colour through a refraction offset, then absorbed per
//     channel over the light's path (red first, then green: turquoise over sand, blue over the deep), with a
//     scattering colour filling in what was absorbed;
//   - the sky sits in the surface by Fresnel, from the live sky reflection (SkyReflection) when there is one; the
//     sun's glitter is a GGX lobe; the island's and the clouds' shadows fall on all of it;
//   - foam: a wash where the water is thin over the sand, a line rolling in toward the shore, whitecaps; caustics on
//     the seabed under clear water;
//   - the far rings are kept at the far plane instead of being cut off, and the water there blends into the sky's
//     horizon, so the sea reaches the horizon whatever the camera's far clip.
//
// _OCEAN_SIMPLE (potato mode, or detailed water off) keeps the colour, the sky, the sun and the shore foam, and drops
// the refraction, the caustics, the light through the crests, the whitecaps and the third ripple layer.
//
// Ported from FF3Unity's sea. The two textures (ripple normals; foam and caustic noise) are drawn at runtime by
// WaterTextures. In the transparent queue; the colour is composited here from the opaque buffer, so no blending.
Shader "Ravenfall/Ocean"
{
    Properties
    {
        [Header(Colour)]
        _ScatterColor ("Shallow water (scattering)", Color) = (0.12, 0.78, 0.84, 1)
        _DeepColor ("Deep water", Color) = (0.02, 0.32, 0.56, 1)
        _Absorption ("Absorption per metre (R, G, B)", Vector) = (0.45, 0.1, 0.05, 0)
        _Murk ("Murk (scales absorption)", Range(0.1, 4)) = 1
        _DeepDepth ("Metres to deep", Float) = 9
        _SunScatter ("Sun through the crests", Range(0, 3)) = 1
        _Refraction ("Refraction", Range(0, 1)) = 0.35

        [Header(Waves)]
        _WaveA ("Wave A (dir x, dir z, steepness, length m)", Vector) = (1, 0.35, 0.1, 26)
        _WaveB ("Wave B", Vector) = (-0.45, 1, 0.09, 13)
        _WaveC ("Wave C", Vector) = (0.7, -0.8, 0.08, 6.5)
        _WaveD ("Wave D", Vector) = (-0.9, -0.35, 0.07, 3.2)
        _WaveHeight ("Wave height", Range(0, 3)) = 0.7
        _WaveSpeed ("Wave speed", Range(0, 3)) = 0.9

        [Header(Surface)]
        [NoScaleOffset] _NormalMap ("Ripple normals", 2D) = "bump" {}
        _RippleScale ("Ripple scale (m)", Float) = 4.5
        _RippleStrength ("Ripple strength", Range(0, 2)) = 0.75
        _RippleSpeed ("Ripple speed", Range(0, 3)) = 1
        _Smoothness ("Smoothness", Range(0.5, 1)) = 0.92
        _SkyTint ("Sky in the surface", Color) = (1, 1, 1, 1)
        _Reflectivity ("Reflectivity at a grazing angle (1 is a true mirror)", Range(0.02, 1)) = 0.6
        _SunGlitter ("Sun glitter", Range(0, 3)) = 1
        _FlattenDistance ("Ripples flatten beyond (m)", Float) = 400

        [Header(Foam)]
        [NoScaleOffset] _FoamTex ("Foam and caustic noise", 2D) = "gray" {}
        _FoamColor ("Foam", Color) = (1, 1, 1, 1)
        _ShoreFoam ("Shore foam reach (m)", Float) = 1.6
        _ShoreFoamAmount ("Shore foam amount", Range(0, 2)) = 1
        _CrestFoam ("Whitecaps", Range(0, 2)) = 0.5
        _FoamScale ("Foam scale (m)", Float) = 3.5

        [Header(Caustics)]
        _Caustics ("Caustics", Range(0, 4)) = 1.2
        _CausticScale ("Caustic scale (m)", Float) = 2.5
        _CausticDepth ("Caustics fade depth (m)", Float) = 6

        [Header(Horizon)]
        _HorizonStart ("The sea starts to become the sky's horizon at (m)", Float) = 400
        _HorizonEnd ("And is the horizon at (m)", Float) = 6000
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-50" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend Off
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fog
            #pragma multi_compile_local _ _OCEAN_SIMPLE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_FoamTex); SAMPLER(sampler_FoamTex);
            // the live sky (SkyReflection), when there is one; otherwise the scene's own reflection
            TEXTURECUBE(_RavenfallSky); SAMPLER(sampler_RavenfallSky);
            float _RavenfallHasSky;
            // how overcast the sky is, 0 to 1 (Weather): the sea mirrors grey cloud then, and the sun hardly glints
            float _RavenfallOvercast;

            CBUFFER_START(UnityPerMaterial)
            float4 _ScatterColor, _DeepColor, _Absorption, _SkyTint, _FoamColor;
            float4 _WaveA, _WaveB, _WaveC, _WaveD;
            float _Murk, _DeepDepth, _SunScatter, _Refraction;
            float _WaveHeight, _WaveSpeed;
            float _RippleScale, _RippleStrength, _RippleSpeed, _Smoothness, _Reflectivity, _SunGlitter, _FlattenDistance;
            float _ShoreFoam, _ShoreFoamAmount, _CrestFoam, _FoamScale;
            float _Caustics, _CausticScale, _CausticDepth;
            float _HorizonStart, _HorizonEnd;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                // the point on the flat plane this vertex was lifted from, and how far up it was lifted
                float3 originCrest : TEXCOORD2;
                float fog : TEXCOORD3;
            };

            // One Gerstner wave: (dir x, dir z, steepness, length). Adds its displacement and its part of the tangent
            // frame. reach: how many wavelengths out the wave still counts, as the rings grow coarser with distance.
            void Gerstner(float4 w, float2 xz, float t, float dist, float reach, inout float3 disp, inout float3 tangent, inout float3 binormal)
            {
                // waves come in groups, an envelope drifting with the wave, so the sea is not ruled in parallel lines
                float2 d = normalize(w.xy);
                float env = SAMPLE_TEXTURE2D_LOD(_FoamTex, sampler_FoamTex, (xz + w.xy * 173.0) / (w.w * 9.0 + 120.0) - d * t * 0.004, 0).b;
                env = lerp(0.1, 1.6, env * env);
                float steep = w.z * _WaveHeight * env * saturate(2.0 - dist / (w.w * reach));
                float k = TWO_PI / max(w.w, 0.5);
                float c = sqrt(9.8 / k);
                float f = k * (dot(d, xz) - c * t);
                float a = steep / k;
                float s, co;
                sincos(f, s, co);
                disp += float3(d.x * a * co, a * s, d.y * a * co);
                tangent += float3(-d.x * d.x * steep * s, d.x * steep * co, -d.x * d.y * steep * s);
                binormal += float3(-d.x * d.y * steep * s, d.y * steep * co, -d.y * d.y * steep * s);
            }

            void Swell(float2 xz, float t, float dist, float reach, out float3 disp, out float3 tangent, out float3 binormal)
            {
                disp = 0; tangent = float3(1, 0, 0); binormal = float3(0, 0, 1);
                Gerstner(_WaveA, xz, t, dist, reach, disp, tangent, binormal);
                Gerstner(_WaveB, xz, t, dist, reach, disp, tangent, binormal);
                Gerstner(_WaveC, xz, t, dist, reach, disp, tangent, binormal);
                Gerstner(_WaveD, xz, t, dist, reach, disp, tangent, binormal);
            }
            #define LIFT_REACH 2.0
            #define NORMAL_REACH 25.0

            // The sky in a direction, blurred by roughness.
            float3 Sky(float3 dir, float3 positionWS, float roughness)
            {
                float3 sky;
                [branch] if (_RavenfallHasSky > 0.5)
                    sky = SAMPLE_TEXTURECUBE_LOD(_RavenfallSky, sampler_RavenfallSky, dir, PerceptualRoughnessToMipmapLevel(roughness)).rgb;
                else
                    sky = GlossyEnvironmentReflection(dir, roughness, 1.0);
                // under a grey sky the reflection is of the cloud, the colour of the sky's light
                float3 cloud = lerp(unity_AmbientSky.rgb, unity_AmbientEquator.rgb, 0.5) * 0.9;
                sky = lerp(sky, cloud, _RavenfallOvercast * 0.85);
                return sky;
            }

            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 p = TransformObjectToWorld(v.positionOS.xyz);
                float t = _Time.y * _WaveSpeed;
                float3 disp, tangent, binormal;
                Swell(p.xz, t, distance(_WorldSpaceCameraPos, p), LIFT_REACH, disp, tangent, binormal);
                o.originCrest = float3(p.xz, disp.y);
                p += disp;
                o.positionWS = p;
                o.positionCS = TransformWorldToHClip(p);
                // past the far plane the water is kept at it rather than cut off, so the sea reaches the horizon
                #if UNITY_REVERSED_Z
                    o.positionCS.z = max(o.positionCS.z, o.positionCS.w * 1e-7);
                #else
                    o.positionCS.z = min(o.positionCS.z, o.positionCS.w * (1.0 - 1e-7));
                #endif
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _WaveSpeed;
                float3 P = i.positionWS;
                float3 toEye = _WorldSpaceCameraPos - P;
                float dist = length(toEye);
                float3 V = toEye / max(dist, 1e-3);
                float far = saturate(dist / max(_FlattenDistance, 1.0));

                // ---- the surface: the swell's normal and fold, the ripples on it ----
                float3 disp, tangent, binormal;
                Swell(i.originCrest.xy, t, dist, NORMAL_REACH, disp, tangent, binormal);
                float3 Nw = normalize(lerp(cross(binormal, tangent), float3(0, 1, 0), far * 0.8));
                float fold = tangent.x * binormal.z - tangent.z * binormal.x; // under about 0.5 the surface folds over

                float rt = _Time.y * _RippleSpeed;
                float2 uv1 = P.xz / _RippleScale + rt * float2(0.030, 0.021);
                float2 uv2 = P.xz / (_RippleScale * 2.63) + rt * float2(-0.017, 0.026);
                float3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1));
                float3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2));
                float detail = _RippleStrength * (1.0 - 0.85 * far);
                float2 ripple = n1.xy + n2.xy * 0.8;
                #if !defined(_OCEAN_SIMPLE)
                    float2 uv3 = P.xz / (_RippleScale * 0.37) + rt * float2(0.050, -0.040);
                    float3 n3 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv3));
                    ripple += n3.xy * 0.5 * (1.0 - saturate(dist / 80.0));
                #endif
                ripple *= detail;
                float3 N = normalize(Nw + float3(ripple.x, 0, ripple.y));
                bool under = _WorldSpaceCameraPos.y < P.y;
                if (under) N = -N;

                // ---- what is under the surface: the bed's colour and depth ----
                float2 uv = i.screenPos.xy / i.screenPos.w;
                float surfEye = i.screenPos.w;
                float raw = SampleSceneDepth(uv);
                float3 bedWS = ComputeWorldSpacePosition(uv, raw, UNITY_MATRIX_I_VP);
                float vd = max(P.y - bedWS.y, 0.0);        // how deep the water is, straight down
                float path = max(distance(P, bedWS), vd);  // how far the light went through it to reach the eye
                // nothing under the water but the sky (open sea, or water past the far clip, kept at the far plane):
                // deep water, no bed, no shore
                #if UNITY_REVERSED_Z
                    bool open = raw <= 1e-6;
                #else
                    bool open = raw >= 1.0 - 1e-6;
                #endif
                open = open || LinearEyeDepth(raw, _ZBufferParams) < surfEye - 0.05;
                if (open) { vd = 1000.0; path = 1000.0; bedWS = P - float3(0, 1000, 0); }
                float2 ruv = uv;
                #if !defined(_OCEAN_SIMPLE)
                    // the offset grows with depth and shrinks with distance, so the far sea does not swim
                    ruv = uv + N.xz * (_Refraction * 0.06 * saturate(vd * 0.4) / max(1.0, surfEye * 0.02));
                    float rraw = SampleSceneDepth(ruv);
                    float3 rWS = ComputeWorldSpacePosition(ruv, rraw, UNITY_MATRIX_I_VP);
                    // no good when it lands on something in front of the water, or on land above it
                    if (LinearEyeDepth(rraw, _ZBufferParams) < surfEye || rWS.y > P.y + 0.05) ruv = uv;
                #endif
                float3 bed = SampleSceneColor(ruv);

                // ---- light ----
                Light sun = GetMainLight(TransformWorldToShadowCoord(P), P, half4(1, 1, 1, 1));
                float shadow = sun.shadowAttenuation;
                float sunUp = saturate(sun.direction.y);
                float3 ambient = SampleSH(float3(0, 1, 0));
                float3 daylight = sun.color * (0.15 + 0.85 * sunUp) * lerp(0.6, 1.0, shadow) + ambient * 0.5;

                #if !defined(_OCEAN_SIMPLE)
                    // ---- caustics on the bed, under clear water ----
                    float2 cuv = (bedWS.xz + sun.direction.xz * vd * 0.4) / max(_CausticScale, 0.1);
                    float c1 = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, cuv + t * float2(0.021, 0.013)).g;
                    float c2 = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, cuv * 1.37 - t * float2(0.016, 0.019)).g;
                    float caustic = pow(saturate(min(c1, c2)), 2.0) * 2.5;
                    caustic *= _Caustics * exp(-vd / max(_CausticDepth, 0.1)) * sunUp * shadow * saturate(vd * 2.0) * (1.0 - far);
                    bed *= 1.0 + caustic * sun.color;
                #endif

                // ---- the water's colour: the bed through it, the scatter that replaces what was absorbed ----
                float3 T = exp(-_Absorption.rgb * _Murk * path);
                float deepK = 1.0 - exp(-vd / max(_DeepDepth, 0.1));
                float3 scatter = lerp(_ScatterColor.rgb, _DeepColor.rgb, deepK) * daylight;
                float3 water = bed * T + scatter * (1.0 - T);

                #if !defined(_OCEAN_SIMPLE)
                    // the sun through the back of a crest toward the eye
                    float crest = saturate(i.originCrest.z / (0.9 * _WaveHeight + 0.01) * 0.5 + 0.5);
                    float back = saturate(dot(-V, sun.direction));
                    float sss = (pow(back, 3.0) * 0.8 + 0.15) * crest * crest * _SunScatter * (1.0 - far);
                    water += _ScatterColor.rgb * sun.color * (sss * 0.6 * shadow);
                #endif

                if (under) return half4(MixFog(_DeepColor.rgb * daylight, i.fog), 1);

                // ---- the sky in it, the sun on it ----
                float ndv = saturate(dot(N, V));
                float fres = 0.02 + (_Reflectivity - 0.02) * pow(1.0 - ndv, 5.0);
                float3 R = reflect(-V, N);
                float rough = (1.0 - _Smoothness) + far * 0.15;
                // never the ground under the horizon: a rough lookup blurs it in from above the line
                R.y = max(R.y, 0.05 + rough * 0.6);
                float3 sky = Sky(normalize(R), P, rough) * _SkyTint.rgb;

                float3 H = normalize(sun.direction + V);
                float ndh = saturate(dot(N, H)), ndl = saturate(dot(N, sun.direction)), ldh = saturate(dot(sun.direction, H));
                float alpha = max(rough * rough, 0.02); // the sun is not a point
                float a2 = alpha * alpha;
                float dd = ndh * ndh * (a2 - 1.0) + 1.0;
                float D = a2 / (PI * dd * dd);
                float F = 0.02 + 0.98 * pow(1.0 - ldh, 5.0);
                float spec = min(D * F / (4.0 * max(ndv, 0.02)), 24.0) * step(0.001, ndl) * _SunGlitter * shadow * (1.0 - 0.85 * _RavenfallOvercast);
                float3 glitter = sun.color * spec;

                // ---- foam ----
                float2 fuv = P.xz / max(_FoamScale, 0.1);
                float4 f1 = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, fuv + t * float2(0.012, 0.009));
                float4 f2 = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, fuv * 0.53 - t * float2(0.007, 0.011));
                float noise = f1.r * 0.6 + f2.r * 0.4;
                float reach = max(_ShoreFoam, 0.01);
                float shore = 1.0 - saturate(vd / reach);
                // lace along the edge: solid where the water is thinnest, torn to holes as it goes out
                float lace = saturate(f1.a * 0.7 + f2.r * 0.9 - 0.45);
                float wash = saturate((shore - noise - 0.05) * 4.0) * (0.25 + 0.75 * lace);
                // a line rolling in toward the shore, broken up by the noise
                float phase = vd / reach * 2.0 + t * 0.4;
                float roll = smoothstep(0.6, 0.85, 1.0 - abs(frac(phase) - 0.5) * 2.0) * shore * shore * saturate(f2.b * 1.8 - 0.3);
                float cap = 0.0;
                #if !defined(_OCEAN_SIMPLE)
                    // whitecaps where the crest folds over
                    cap = saturate((0.55 - fold) * 2.5) * _CrestFoam * saturate(f1.a * 1.6 + f1.r * 0.4);
                #endif
                float foam = saturate((wash + roll * 0.6) * _ShoreFoamAmount + cap) * (1.0 - far);
                float3 foamCol = _FoamColor.rgb * (ambient + sun.color * (0.25 + 0.75 * ndl) * shadow);

                float3 col = lerp(lerp(water, sky, fres), foamCol, foam) + glitter * (1.0 - foam);

                // ---- the far sea becomes the sky's horizon, so there is no edge ----
                float2 flat = -V.xz;
                float flatLength = max(length(flat), 1e-4);
                float3 horizonDir = normalize(float3(flat.x / flatLength, 0.02, flat.y / flatLength));
                float3 horizon = Sky(horizonDir, P, 0.35);
                float h = smoothstep(_HorizonStart, _HorizonEnd, dist);
                col = lerp(col, horizon, h * h);

                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
