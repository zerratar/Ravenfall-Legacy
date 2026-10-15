// Cumulus with a body: a fullscreen pass (see SkyClouds) that marches each pixel's ray through a slab of cloud above
// the islands and shades what it finds. The slab is shaped by 3D noise: a slow field decides where the banks and the
// gaps are, coverage thins toward the ceiling so every puff tops out in a dome, and a fine field tears the edges. Each
// sample is lit by a few steps toward the sun through the cloud's own bulk (Beer's law, with a floor for the light
// that scatters round several times), so the sun's side is white and the bellies are the sky's blue-grey; far cloud
// melts into the horizon's haze.
//
// The march runs at half the screen's size into a buffer of its own (colour, and how much of the scene still shows
// through); the composite lays it over the picture with a small filter that takes out the march's grain. A ray that
// never reaches the slab (looking down at the islands from under it, the usual view) ends at once.
//
// Ported from FF3Unity's clouds (the sky slab; not the sea of cloud nor the sculpted towers).
Shader "Hidden/Ravenfall/SkyClouds"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // x floor (m), y ceiling (m), z coverage, w puff size (m)
            float4 _CloudSlab;
            // x density, y steps, z absorption, w how much the fine noise tears the edges
            float4 _CloudParams;
            // xy wind offset (m, already times time), z haze per metre, w haze start (m)
            float4 _CloudWind;
            float4 _CloudSunDir;     // toward the sun
            float4 _CloudSunColor;   // the sun's light on a cloud (linear)
            float4 _CloudSkyColor;   // ambient from above (linear)
            float4 _CloudShadeColor; // ambient in the shade (linear)
            float4 _CloudHazeColor;  // the horizon's haze (linear)
            // x sun light, y ambient light, z shadow depth (0 white, 1 dark bellies)
            float4 _CloudLook;

            // ---- noise ----

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            // smooth value noise, 0..1
            float Noise3(float3 p)
            {
                float3 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = Hash13(i), n100 = Hash13(i + float3(1, 0, 0)), n010 = Hash13(i + float3(0, 1, 0)), n110 = Hash13(i + float3(1, 1, 0));
                float n001 = Hash13(i + float3(0, 0, 1)), n101 = Hash13(i + float3(1, 0, 1)), n011 = Hash13(i + float3(0, 1, 1)), n111 = Hash13(i + float3(1, 1, 1));
                float x00 = lerp(n000, n100, f.x), x10 = lerp(n010, n110, f.x), x01 = lerp(n001, n101, f.x), x11 = lerp(n011, n111, f.x);
                return lerp(lerp(x00, x10, f.y), lerp(x01, x11, f.y), f.z);
            }

            // three octaves of smooth noise, turned against one another so the lattice does not show as rows; smooth
            // rather than folded, as a folded octave (1 - |2n - 1|) peaks in a crease and the puffs came out as spikes
            static const float3x3 Turn = float3x3(0.36, 0.48, -0.8, -0.8, 0.6, 0.0, 0.48, 0.64, 0.6);

            float Fbm3(float3 p)
            {
                float a = Noise3(p);
                p = mul(Turn, p) * 2.07 + 11.3;
                float b = Noise3(p);
                p = mul(Turn, p) * 2.13 + 27.1;
                float c = Noise3(p);
                return a * 0.55 + b * 0.3 + c * 0.15;
            }

            // ---- the slab ----

            // how much cloud there is at p, 0..1; hf out the height in the slab, 0..1
            float Shape(float3 p, out float hf, bool fine)
            {
                hf = saturate((p.y - _CloudSlab.x) / (_CloudSlab.y - _CloudSlab.x));
                if (p.y < _CloudSlab.x || p.y > _CloudSlab.y) return 0.0;
                float3 q = (p + float3(_CloudWind.x, 0.0, _CloudWind.y)) / _CloudSlab.w;
                // the puffs stand taller than they are wide
                q.y *= 0.5;
                // banks and gaps: a slow field over the slab, the coverage swinging round the setting
                float banks = Noise3(q * 0.23 + 5.7);
                float cov = saturate(_CloudSlab.z * (0.7 + 0.6 * banks));
                // the body (about 0.5 on average), cut at a threshold the coverage sets, thinned toward the ceiling so the tops are domes, and
                // the lowest few percent fading in so the underside is not a flat cut
                float body = Fbm3(q * 1.7);
                // a thin cover thins out much faster, so a clear day is clear; at about half cover and over it is as before
                float bare = 1.0 - cov;
                float thresh = 0.68 - 0.28 * cov + 0.12 * bare * bare * bare + hf * hf * 0.22;
                float d = saturate((body - thresh) / 0.06);
                d *= smoothstep(0.0, 0.1, hf);
                if (fine && d > 0.0 && d < 0.98)
                {
                    // torn edges: fine noise eats at the thin parts
                    float detail = Noise3(q * 9.1 + 3.3) * 0.65 + Noise3(q * 21.7 + 8.8) * 0.35;
                    d = saturate(d - (1.0 - d) * detail * _CloudParams.w);
                }
                return d * _CloudParams.x;
            }

        ENDHLSL

        Pass
        {
            Name "Sky Clouds March"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            // light reaching p from the sun through the cloud: a short march sunward
            float SunLight(float3 p, float3 sun, float sigma, float step, float jitter)
            {
                float od = 0.0, hf;
                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    float3 q = p + sun * (step * (i + 0.5 + jitter * 0.5));
                    od += Shape(q, hf, false) * sigma * step;
                }
                float k = _CloudParams.z * 2.5;
                // Beer's law, with a floor for the light scattered round several times
                float shadow = _CloudLook.z;
                float floorLight = lerp(0.95, 0.12, shadow), floorDecay = lerp(0.02, 0.2, shadow);
                return max(exp(-od * k), floorLight * exp(-od * k * floorDecay));
            }

            // Henyey-Greenstein, for the silver lining toward the sun
            float Phase(float c, float g)
            {
                float g2 = g * g;
                return (1.0 - g2) / (4.0 * PI * pow(1.0 + g2 - 2.0 * g * c, 1.5));
            }

            // where the ray enters and leaves the slab, clipped to the scene; false if it misses
            bool SlabSpan(float3 cam, float3 dir, float sceneDist, float maxLen, out float t0, out float t1)
            {
                t0 = 0.0; t1 = 0.0;
                float floorY = _CloudSlab.x, ceilY = _CloudSlab.y;
                if (abs(dir.y) < 1e-4)
                {
                    if (cam.y < floorY || cam.y > ceilY) return false;
                    t1 = min(sceneDist, maxLen);
                    return t1 > 0.0;
                }
                float ta = (floorY - cam.y) / dir.y, tb = (ceilY - cam.y) / dir.y;
                t0 = max(min(ta, tb), 0.0);
                t1 = min(max(ta, tb), sceneDist);
                t1 = min(t1, t0 + maxLen);
                return t1 > t0 + 0.5;
            }

            // colour in rgb, how much of the scene still shows through in a
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                float rawDepth = SampleSceneDepth(uv);
                float3 posWS = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 cam = GetCameraPositionWS();
                float3 ray = posWS - cam;
                float sceneDist = length(ray);
                float3 dir = ray / max(sceneDist, 1e-4);
            #if UNITY_REVERSED_Z
                bool sky = rawDepth <= 1e-6;
            #else
                bool sky = rawDepth >= 1.0 - 1e-6;
            #endif
                if (sky) sceneDist = 1e7;

                float t0, t1;
                if (_CloudParams.x <= 0.0 || !SlabSpan(cam, dir, sceneDist, _CloudSlab.w * 16.0, t0, t1)) return half4(0, 0, 0, 1);

                float3 color = 0.0;
                float T = 1.0;
                int steps = (int)_CloudParams.y;
                float jitter = InterleavedGradientNoise(input.positionCS.xy, 0);

                float3 sun = _CloudSunDir.xyz;
                float phase = 0.45 + 1.3 * Phase(dot(dir, sun), 0.35) + 0.6 * Phase(dot(dir, sun), -0.2);
                float hazeK = _CloudWind.z, hazeStart = _CloudWind.w;
                // extinction per metre of full cloud: a puff is opaque a twentieth of its size in
                float sigma = 10.0 / _CloudSlab.w;
                // the step grows with distance, between a twentieth and a half of the puff size, and never coarser than
                // a slice of the slab, or the slices show as rows seen from below
                float thick = _CloudSlab.y - _CloudSlab.x;
                float dtMin = min(_CloudSlab.w * 0.05, thick / 48.0), dtMax = _CloudSlab.w * 0.5;
                float lightStep = min(thick * 0.28, _CloudSlab.w * 0.15);
                float t = t0 + clamp(t0 * 0.015, dtMin, dtMax) * jitter;
                for (int i = 0; i < steps * 8 && t < t1 && T > 0.015; i++)
                {
                    float3 p = cam + dir * t;
                    float dt = clamp(t * 0.015, dtMin, dtMax);
                    float hf;
                    float ext = Shape(p, hf, true) * sigma;
                    if (ext > 1e-5)
                    {
                        float light = SunLight(p, sun, sigma, lightStep, jitter);
                        float3 ambient = lerp(_CloudShadeColor.rgb, _CloudSkyColor.rgb, hf * hf) * _CloudLook.y;
                        // half the sun's colour: a cloud is whiter than the light that falls on it looks on the ground
                        float3 sunColour = lerp(_CloudSunColor.rgb, dot(_CloudSunColor.rgb, float3(0.3, 0.59, 0.11)).xxx, 0.5);
                        float3 c = sunColour * light * phase * _CloudLook.x + ambient;
                        // the far cloud is in the same air as the horizon
                        float haze = 1.0 - exp(-max(t - hazeStart, 0.0) * hazeK);
                        c = lerp(c, _CloudHazeColor.rgb, haze);
                        float alpha = 1.0 - exp(-ext * dt);
                        color += T * alpha * c;
                        T *= 1.0 - alpha;
                    }
                    t += dt;
                }
                return half4(color, T);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Sky Clouds Composite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D_X(_SkyCloudBuffer); // half the screen's size
            float4 _SkyCloudTexel;        // xy one texel of the buffer in uv
            float _CloudNight;            // 0 by day, 1 in the dark: how much the stars show

            float StarHash(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            // the stars in a direction: one maybe in each cell of a grid laid through the sky, a small soft dot at a
            // random place in it, some brighter, twinkling slowly; none low in the horizon's haze
            half3 Stars(float3 dir)
            {
                float3 p = dir * 260.0;
                float3 cell = floor(p);
                float h = StarHash(cell);
                if (h < 0.985) return 0;
                float3 at = cell + float3(StarHash(cell + 7.1), StarHash(cell + 13.7), StarHash(cell + 29.3));
                float d = length(p - at);
                float bright = (h - 0.985) / 0.015;
                float twinkle = 0.75 + 0.25 * sin(_Time.y * (1.0 + bright * 2.0) + h * 80.0);
                float spot = smoothstep(0.32, 0.0, d);
                half3 tint = lerp(half3(0.75, 0.85, 1.0), half3(1.0, 0.92, 0.8), StarHash(cell + 3.3));
                return tint * spot * (0.4 + bright * bright * 2.0) * twinkle * saturate(dir.y * 5.0 - 0.1);
            }

            // the cloud over the scene; a tent over the 3x3 round each texel takes the march's grain out, weighted by
            // how alike the neighbours' transmittance is, so where cloud meets crag or sky the edge stays put
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 scene = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);
                float2 px = _SkyCloudTexel.xy;
                half4 centre = SAMPLE_TEXTURE2D_X_LOD(_SkyCloudBuffer, sampler_LinearClamp, uv, 0);
                half4 acc = centre * 4.0;
                half wsum = 4.0;
                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        if (x == 0 && y == 0) continue;
                        half4 n = SAMPLE_TEXTURE2D_X_LOD(_SkyCloudBuffer, sampler_LinearClamp, uv + float2(x, y) * px, 0);
                        half w = (x == 0 || y == 0) ? 2.0 : 1.0;
                        w *= max(1.0 - abs(n.a - centre.a) * 3.0, 0.15);
                        acc += n * w;
                        wsum += w;
                    }
                }
                acc /= wsum;
                // the stars behind the clouds, where the sky shows
                [branch] if (_CloudNight > 0.01)
                {
                    float raw = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    bool sky = raw <= 1e-6;
                #else
                    bool sky = raw >= 1.0 - 1e-6;
                #endif
                    if (sky)
                    {
                        float3 dir = normalize(ComputeWorldSpacePosition(uv, raw, UNITY_MATRIX_I_VP) - GetCameraPositionWS());
                        scene.rgb += Stars(dir) * _CloudNight;
                    }
                }
                return half4(scene.rgb * acc.a + acc.rgb, scene.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Sky Clouds Shadow"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            // the light's own space, as URP projects the sun's cookie in it
            float4x4 _CloudShadowLightToWorld;
            // xy the window's centre in the light's space (m), z its size (m), w how dark a full cloud's shadow is
            float4 _CloudShadowWindow;

            // The sun's cookie, drawn from the cloud itself: each texel is a line along the sun's direction in the
            // light's space, and what comes through is how much cloud that line crosses in the slab. So the shadows
            // on the ground are the shadows of the clouds overhead.
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 ls = (input.texcoord - 0.5) * _CloudShadowWindow.z + _CloudShadowWindow.xy;
                float3 origin = mul(_CloudShadowLightToWorld, float4(ls, 0.0, 1.0)).xyz;
                float3 forward = normalize(mul((float3x3)_CloudShadowLightToWorld, float3(0.0, 0.0, 1.0)));
                // the sun at or under the horizon casts no cloud shadow worth drawing
                if (forward.y > -0.05) return half4(1, 1, 1, 1);
                float z0 = (_CloudSlab.y - origin.y) / forward.y;
                float z1 = (_CloudSlab.x - origin.y) / forward.y;
                const int samples = 14;
                float dz = (z1 - z0) / samples;
                float od = 0.0, hf;
                [loop]
                for (int i = 0; i < samples; i++)
                {
                    float3 p = origin + forward * (z0 + dz * (i + 0.5));
                    od += Shape(p, hf, false);
                }
                float sigma = 10.0 / _CloudSlab.w;
                float through = exp(-od * sigma * abs(dz) * _CloudParams.z * 0.6);
                float light = lerp(1.0, through, _CloudShadowWindow.w);
                return half4(light, light, light, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
