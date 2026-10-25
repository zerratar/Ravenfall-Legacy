// Hero mode's health and mana orbs (HeroHudView): a glass sphere of liquid, drawn into a small render texture each
// frame (Graphics.Blit) that the HUD shows inside its gold rim.
//
// The square is the sphere seen head on: from uv the shader finds the sphere's normal, lights it from up and to the
// left, and fills it to _Fill with liquid. The liquid's surface waves, and tilts with _Slosh (the HUD sets it from how
// fast the level just moved, so a hit makes it rock); clouds swirl inside it and bubbles rise. Between _Fill and
// _Trail (where the level was a moment ago) the liquid is paler, so a hit shows how much it took. Over everything the
// glass: a rim of light at the edge, a soft highlight and a sharp glint.
Shader "Ravenfall/Hero Orb"
{
    Properties
    {
        _Color("Liquid", Color) = (0.85, 0.12, 0.12, 1)
        _Deep("Liquid deep", Color) = (0.25, 0.01, 0.03, 1)
        _Glow("Inner glow", Color) = (1, 0.45, 0.3, 1)
        _Fill("Fill", Range(0, 1)) = 0.7
        _Trail("Trail", Range(0, 1)) = 0.7
        _Slosh("Slosh", Range(-1, 1)) = 0
        _Clock("Time", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color, _Deep, _Glow;
            float _Fill, _Trail, _Slosh, _Clock;

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
            }

            float clouds(float2 p)
            {
                float v = 0, a = 0.5;
                for (int o = 0; o < 4; o++)
                {
                    v += noise(p) * a;
                    p = p * 2.03 + float2(1.7, 9.2);
                    a *= 0.5;
                }
                return v;
            }

            // the surface's height at x, for a level from -1 (empty) to 1 (full)
            float surface(float x, float level)
            {
                float t = _Clock;
                float calm = 1 - abs(level); // flat when nearly empty or full
                float wave = sin(x * 3.1 + t * 2.2) * 0.035 + sin(x * 5.3 - t * 3.4) * 0.018 + sin(x * 9.0 + t * 5.1) * 0.006;
                return level + (wave * (0.4 + abs(_Slosh) * 2.5) + _Slosh * x * 0.35) * calm;
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 p = i.uv * 2 - 1;
                float r = length(p);
                float edge = fwidth(r) * 1.5;
                float alpha = 1 - smoothstep(1 - edge, 1, r);
                if (alpha <= 0) return 0;

                float3 n = float3(p, sqrt(saturate(1 - r * r)));
                float3 light = normalize(float3(-0.45, 0.6, 0.65));

                float level = _Fill * 2 - 1;
                float trail = max(_Trail, _Fill) * 2 - 1;
                float top = surface(p.x, level);
                float trailTop = surface(p.x, trail);

                // the empty glass: dark, a little lighter low in the middle
                float3 colour = lerp(float3(0.02, 0.02, 0.035), float3(0.09, 0.085, 0.12), saturate(1 - length(p - float2(0, -0.35)) / 1.4));

                // the liquid: deeper toward the bottom and the edge, lit through, clouds swirling in it
                if (p.y < trailTop)
                {
                    float depth = saturate((top - p.y) * 0.55);
                    float2 swirl = n.xy / (1.2 + n.z);
                    float angle = _Clock * 0.25;
                    float2 rotated = float2(swirl.x * cos(angle) - swirl.y * sin(angle), swirl.x * sin(angle) + swirl.y * cos(angle));
                    float c = clouds(rotated * 3 + float2(_Clock * 0.15, -_Clock * 0.35));
                    float3 liquid = lerp(_Color.rgb, _Deep.rgb, saturate(depth * 0.9 + (1 - n.z) * 0.6));
                    liquid *= 0.7 + c * 0.6;
                    liquid += _Glow.rgb * pow(saturate(1 - length(p - float2(0.05, -0.2)) * 0.9), 3) * 0.55;
                    liquid *= 0.65 + 0.45 * saturate(dot(n, light));

                    // bubbles rising
                    for (int b = 0; b < 6; b++)
                    {
                        float fb = b;
                        float x = frac(sin(fb * 12.9898) * 437.585) * 1.4 - 0.7;
                        float speed = 0.18 + frac(fb * 0.37) * 0.2;
                        float y = frac(_Clock * speed + fb * 0.31) * 2.2 - 1.1;
                        float2 at = float2(x + sin(_Clock * 2 + fb) * 0.04, y);
                        float size = 0.025 + frac(fb * 0.71) * 0.025;
                        float d = length(p - at);
                        float bubble = (1 - smoothstep(size * 0.6, size, d)) * step(at.y, top - 0.02);
                        liquid += bubble * 0.35 * lerp(_Glow.rgb, 1, 0.5);
                    }

                    // between the level and where it was a moment ago: paler, fading
                    if (p.y > top) liquid = lerp(liquid, lerp(_Color.rgb, 1, 0.55), 0.55) * 0.9;

                    // a bright line at the surface
                    float foam = 1 - smoothstep(0, 0.035, abs(p.y - top));
                    liquid += foam * lerp(_Glow.rgb, 1, 0.4) * 0.6 * step(-0.98, level) * step(level, 0.98);
                    colour = liquid;
                }

                // the glass: light along the rim, a soft highlight up left, a sharp glint, darker toward the edge
                float rim = pow(1 - n.z, 3);
                colour = colour * (1 - rim * 0.55) + rim * 0.35 * float3(1, 0.95, 0.85);
                float3 view = float3(0, 0, 1);
                float3 h = normalize(light + view);
                colour += pow(saturate(dot(n, h)), 60) * 0.9;
                float soft = saturate(1 - length((p - float2(-0.36, 0.42)) / float2(0.42, 0.26)));
                colour += soft * soft * 0.22;
                float glint = saturate(1 - length((p - float2(0.42, -0.6)) / float2(0.16, 0.05)));
                colour += glint * 0.35;

                return fixed4(colour, alpha);
            }
            ENDCG
        }
    }
}
