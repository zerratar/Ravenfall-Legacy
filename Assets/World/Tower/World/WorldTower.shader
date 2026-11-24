// The tower that stands in the main world (docs/world-tower.md): one shader for all of it, the stone, the windows and
// runes that light up with the stream's progress, the waterfalls, the cloud round its top, the green fire and the
// lightning. The meshes (tools/tower/world/world_tower.py) carry everything in their vertices:
//
//   color rgb   the face's color (sRGB); for a glowing face the color it glows in
//   color a     how much the face glows when its floor is lit
//   uv.x        the kind: 0 solid, 1 falling water, 2 cloud, 3 flame, 4 pane (a window or a rune: dark glass until
//               lit), 5 lightning
//   uv.y        along the flow for water, a phase for clouds, flames and lightning
//
// Seen from every island: the tower stands far past the camera's view distance (650 m, 100 in potato mode), so the
// vertex stage pulls what lies beyond nine tenths of the view distance in toward the camera along its own line of
// sight. On screen nothing moves (a point stays on its ray), only its depth is squeezed into the last tenth of the
// view, in the same order, so the tower hides itself correctly and is never cut off by the far plane. The game's haze reads that
// squeezed depth, so the rest of the air between here and the tower's real place is added in the fragment stage
// (_WorldTowerHaze, set by WorldTower.cs from the Haze setting). What is under the sea is cut away when squeezed (the
// far sea writes no depth to hide it).
//
// Per floor (one material instance each, WorldTower.cs): _Glow 0 for a floor not reached, 1 for a cleared one, and
// _Pulse for the floor the stream is on. _WorldTowerAnimate 0 (potato mode) stops the pulse, the bobbing cloud, the
// flicker and the lightning; steady light stays.
Shader "Ravenfall/WorldTower"
{
    Properties
    {
        [HDR] _GlowTint("Glow tint", Color) = (1, 1, 1, 1)
        _Glow("Glow (0 dark, 1 lit)", Range(0, 4)) = 1
        _Pulse("Pulse (0 steady)", Range(0, 1)) = 0
        _GlowBoost("Glow brightness", Float) = 2.4
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _GlowTint;
            half _Glow;
            half _Pulse;
            half _GlowBoost;
        CBUFFER_END

        // set by WorldTower.cs for every tower material
        float _WorldTowerAnimate;       // 1 normally, 0 in potato mode
        float _WorldTowerSea;           // the sea's height
        float4 _WorldTowerHaze;         // x density per metre (0: no haze), y 1 / height scale, z base height, w start distance
        float4 _WorldTowerHazeColor;    // linear

        #define KIND_WATER(k) ((k) > 0.5 && (k) < 1.5)
        #define KIND_CLOUD(k) ((k) > 1.5 && (k) < 2.5)
        #define KIND_FLAME(k) ((k) > 2.5 && (k) < 3.5)
        #define KIND_PANE(k) ((k) > 3.5 && (k) < 4.5)
        #define KIND_BOLT(k) ((k) > 4.5)

        // The world position after the kind's own movement: clouds bob, flames waver.
        float3 TowerWorld(float3 positionOS, float2 data)
        {
            float3 positionWS = TransformObjectToWorld(positionOS);
            float t = _Time.y * _WorldTowerAnimate;
            if (KIND_CLOUD(data.x)) positionWS.y += sin(t * 0.22 + data.y * 6.2832) * 1.4;
            else if (KIND_FLAME(data.x))
            {
                // a phase of 1 or more marks a small flame (a brazier's): it wavers less
                float sway = data.y >= 1.0 ? 0.07 : 0.45;
                float quick = data.y >= 1.0 ? 2.2 : 1.0;
                positionWS.x += sin(t * 5.3 * quick + positionWS.y * 0.9 * quick + data.y * 31.0) * sway;
                positionWS.z += cos(t * 4.1 * quick + positionWS.y * 0.7 * quick + data.y * 17.0) * sway;
            }
            return positionWS;
        }

        // To clip space, with what lies past nine tenths of the view distance squeezed in front of the far plane.
        // ratio is how much nearer the point was drawn (1 for no change).
        float4 TowerClip(float3 positionWS, out float ratio)
        {
            float3 positionVS = TransformWorldToView(positionWS);
            ratio = 1.0;
            // a perspective camera only: an orthographic one (a map) has no line of sight to move along
            if (unity_OrthoParams.w < 0.5)
            {
                float z = -positionVS.z;
                float far = _ProjectionParams.z;
                // the last tenth of the view: nearer than that everything is where it is, so an island between the
                // camera and the tower hides the tower's foot as it should
                float z0 = far * 0.9;
                float range = far * 0.99 - z0;
                if (z > z0)
                {
                    float u = (z - z0) / range;
                    ratio = (z0 + range * u / (1.0 + u)) / z;
                    positionVS *= ratio;
                }
            }
            return TransformWViewToHClip(positionVS);
        }

        // What is not drawn at all: lightning between strikes, fire on a floor that is dark, and the part under the
        // sea of a tower seen from afar. Every pass asks this, so the depth agrees with the color.
        void TowerCut(float2 data, float3 positionWS, float ratio)
        {
            float t = _Time.y;
            if (KIND_BOLT(data.x))
            {
                float cycle = frac(t * 0.19 + data.y);
                float on = step(cycle, 0.03) + step(abs(cycle - 0.065), 0.012);
                clip(on * _WorldTowerAnimate - 0.5);
            }
            else if (KIND_FLAME(data.x)) clip(_Glow - 0.03);
            if (ratio < 0.999) clip(positionWS.y - _WorldTowerSea + 0.35);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // the scene's ambient light (SceneAmbient)
            float4 _RavenfallSH[7];

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                nointerpolation float kind : TEXCOORD2;
                float flow : TEXCOORD3;
                float ratio : TEXCOORD4;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionWS = TowerWorld(input.positionOS.xyz, input.uv);
                output.positionCS = TowerClip(output.positionWS, output.ratio);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.kind = input.uv.x;
                output.flow = input.uv.y;
                return output;
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

            // the haze's optical depth along a ray of length d from the camera (as Haze.shader works it out)
            float HazeDepth(float d, float3 camWS, float dirY)
            {
                float D = _WorldTowerHaze.x, k = _WorldTowerHaze.y, base = _WorldTowerHaze.z, start = _WorldTowerHaze.w;
                d = max(d - start, 0.0);
                float a = exp(-k * max(camWS.y + dirY * start - base, 0.0));
                float kd = k * dirY * d;
                float f = abs(kd) > 1e-3 ? (1.0 - exp(-kd)) / kd : 1.0 - 0.5 * kd;
                return D * a * f * d;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 data = float2(input.kind, input.flow);
                TowerCut(data, input.positionWS, input.ratio);

                float3 P = input.positionWS;
                float t = _Time.y * _WorldTowerAnimate;
                half3 base = SRGBToLinear(input.color.rgb);
                half mask = input.color.a;
                half3 N = normalize(input.normalWS);
                // the floor's light: steady, or breathing on the floor the stream is on
                half glow = _Glow * (1.0 - _Pulse * (0.5 - 0.5 * sin(t * 1.6)));
                half3 color;

                if (KIND_BOLT(input.kind))
                {
                    color = base * 7.0;
                }
                else if (KIND_FLAME(input.kind))
                {
                    half flicker = 0.82 + 0.18 * sin(t * 9.0 + input.flow * 40.0);
                    color = base * _GlowTint.rgb * glow * flicker * _GlowBoost * 0.85;
                }
                else
                {
                    Light sun = GetMainLight(TransformWorldToShadowCoord(P));
                    half shadow = sun.shadowAttenuation;
                    half nl = saturate(dot(N, sun.direction));
                    half3 ambient = SceneAmbientLight(N);
                    half3 albedo = base;
                    half3 lightIn;
                    if (KIND_PANE(input.kind))
                    {
                        // dark glass, a carved line: what glows is added below
                        albedo = base * 0.05 + half3(0.012, 0.014, 0.02);
                        lightIn = ambient + sun.color * nl * shadow;
                    }
                    else if (KIND_CLOUD(input.kind))
                    {
                        // cloud takes the light in from all round
                        lightIn = ambient * 1.05 + sun.color * (0.2 + 0.5 * saturate(nl * 0.6 + 0.4));
                    }
                    else if (KIND_WATER(input.kind))
                    {
                        // bands of white running down the fall
                        half foam = 0.22 * (0.5 + 0.5 * sin(6.2832 * (input.flow * 0.9 - t * 0.5)))
                                  + 0.14 * (0.5 + 0.5 * sin(6.2832 * (input.flow * 2.3 - t * 0.8) + 1.7));
                        albedo = lerp(albedo, half3(1, 1, 1), foam);
                        lightIn = ambient * 1.15 + sun.color * (0.35 + 0.65 * nl) * shadow;
                    }
                    else
                    {
                        lightIn = ambient + sun.color * nl * shadow;
                    }

                    #if defined(_ADDITIONAL_LIGHTS)
                    if (!KIND_CLOUD(input.kind) && input.ratio > 0.999)
                    {
                        InputData inputData = (InputData)0;
                        inputData.positionWS = P;
                        inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                        uint lightCount = GetAdditionalLightsCount();
                        LIGHT_LOOP_BEGIN(lightCount)
                            Light near = GetAdditionalLight(lightIndex, P, half4(1, 1, 1, 1));
                            lightIn += near.color * near.distanceAttenuation * near.shadowAttenuation * saturate(dot(N, near.direction));
                        LIGHT_LOOP_END
                    }
                    #endif

                    color = albedo * lightIn + base * _GlowTint.rgb * (mask * glow * _GlowBoost);
                }

                // the air between the squeezed place and the real one
                if (_WorldTowerHaze.x > 0.0 && input.ratio < 0.999)
                {
                    float3 camWS = GetCameraPositionWS();
                    float3 ray = P - camWS;
                    float dist = length(ray);
                    float dirY = ray.y / max(dist, 1e-3);
                    float extra = HazeDepth(dist, camWS, dirY) - HazeDepth(dist * input.ratio, camWS, dirY);
                    color = lerp(color, _WorldTowerHazeColor.rgb, saturate(1.0 - exp(-max(extra, 0.0))));
                }
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            float4 Vertex(Attributes input) : SV_POSITION
            {
                // stone and water cast shadows; cloud, fire and lightning do not
                if (input.uv.x > 1.5 && !KIND_PANE(input.uv.x)) return float4(2, 2, 2, 1);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            half4 Fragment() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                nointerpolation float2 data : TEXCOORD1;
                float ratio : TEXCOORD2;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionWS = TowerWorld(input.positionOS.xyz, input.uv);
                output.positionCS = TowerClip(output.positionWS, output.ratio);
                output.data = input.uv;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                TowerCut(input.data, input.positionWS, input.ratio);
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                nointerpolation float2 data : TEXCOORD1;
                float ratio : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionWS = TowerWorld(input.positionOS.xyz, input.uv);
                output.positionCS = TowerClip(output.positionWS, output.ratio);
                output.data = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                TowerCut(input.data, input.positionWS, input.ratio);
                return half4(normalize(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
