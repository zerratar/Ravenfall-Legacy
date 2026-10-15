// The air between the camera and the far islands: a fullscreen pass (see Haze) that reads the depth and lays the
// haze over the picture, thicker low over the sea and thinner with height, so a far island sits in the air and a
// near one stays crisp; brighter and warmer toward the sun, so distance reads as light in the air rather than a grey
// wash; and the sky hazed only at the horizon, so the far sea and land melt into it.
// Ported from FF3Unity's atmosphere (the haze; not the sun shafts).
Shader "Hidden/Ravenfall/Haze"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "Haze"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // x density (optical depth per metre at the base height), y height falloff (1/m), z base height (m),
            // w distance before the haze begins (m)
            float4 _HazeParams;
            // x the sun's light through the haze, y how tightly it gathers round the sun, z the horizon haze on the
            // sky (0..1), w how far up the sky it reaches (0..1 of the view's height)
            float4 _HazeSun;
            float4 _HazeColor;    // linear
            float4 _HazeSunColor; // linear, with its strength
            float4 _HazeSunDir;   // toward the sun

            // optical depth of a ray of length d from height h0 rising dy over its length, through air whose density
            // is D at the base height and falls by exp(-k * height above it)
            float OpticalDepth(float d, float h0, float dy, float D, float k, float base)
            {
                float a = exp(-k * max(h0 - base, 0.0));
                float kd = k * dy;
                float f = abs(kd) > 1e-3 ? (1.0 - exp(-kd)) / kd : 1.0 - 0.5 * kd;
                return D * a * f * d;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);
                float rawDepth = SampleSceneDepth(uv);
                float3 posWS = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 camWS = GetCameraPositionWS();
                float3 ray = posWS - camWS;
                float dist = length(ray);
                float3 dir = ray / max(dist, 1e-4);
                float D = _HazeParams.x, k = _HazeParams.y, base = _HazeParams.z, start = _HazeParams.w;

            #if UNITY_REVERSED_Z
                bool sky = rawDepth <= 1e-6;
            #else
                bool sky = rawDepth >= 1.0 - 1e-6;
            #endif
                // below the horizon the sky stands for the open sea, which writes no depth: hazed as the water's plane
                // out where the ray meets it
                float toBase = (base - camWS.y) / min(dir.y, -1e-4);
                if (sky && dir.y < 0.0 && toBase > 0.0)
                {
                    sky = false;
                    dist = min(toBase, 60000.0);
                }
                float amount;
                if (sky)
                {
                    float up = saturate(dir.y / max(_HazeSun.w, 1e-3));
                    amount = _HazeSun.z * (1.0 - up) * (1.0 - up);
                    float rim = saturate(1.0 - up * 8.0);
                    amount = lerp(amount, 1.0, rim * rim);
                    amount *= dir.y >= 0.0 ? 1.0 : saturate(1.0 + dir.y * 6.0);
                }
                else
                {
                    float d = max(dist - start, 0.0);
                    float od = OpticalDepth(d, camWS.y + dir.y * start, dir.y * d, D, k, base);
                    amount = 1.0 - exp(-od);
                }
                // toward the sun the haze is lit through, its colour lifting toward the sun's
                float sunDot = saturate(dot(dir, _HazeSunDir.xyz));
                float glow = pow(sunDot, _HazeSun.y) * _HazeSun.x;
                half3 haze = _HazeColor.rgb + _HazeSunColor.rgb * glow;
                color.rgb = lerp(color.rgb, haze, saturate(amount));
                return color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
