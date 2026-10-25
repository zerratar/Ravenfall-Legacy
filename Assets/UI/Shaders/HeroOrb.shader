// Hero mode's health and mana orbs (HeroHudView): a glass sphere of liquid, drawn into a small render texture each
// frame (Graphics.Blit) that the HUD shows. Built after docs/Unity-Glass-Orb-Kit and the user's reference: clear glass
// that shows the world behind it, a luminous liquid lit from within, light added rather than painted on.
//
// - An orthographic view looking a little down (Tilt) at a unit sphere: each pixel's ray enters and leaves it.
// - The liquid is the half space under its surface (the fill's height, small waves and a slosh). The ray's part in it
//   is integrated in steps, emission and absorption (Beer-Lambert): it scatters its colour where light reaches, glows
//   in its currents (the flow noise at two scales) and toward its core, and burns brightest in the caustics low down;
//   thick paths are deep, thin edges dark. The liquid body artwork adds stationary depth.
// - Where the ray meets the surface first, the surface disk shows (an ellipse from this angle) with a glowing band
//   where it meets the front glass, and the liquid's light spilling onto the glass just above it.
// - The glass: mostly see-through above the liquid (a faint dark tint), its reflections (the glass artwork and a thin
//   fresnel rim) added as light. Everything is summed as premultiplied light and coverage; the HUD's element blends it
//   One, OneMinusSrcAlpha (the HeroOrbLight UI Shader Graph), so added light adds over what is behind.
// - Between _Fill and _Trail (where the level was a moment ago) a pale band shows what a hit took.
Shader "Ravenfall/Hero Orb"
{
    Properties
    {
        _Color("Liquid (scattering)", Color) = (0.95, 0.06, 0.05, 1)
        _Deep("Liquid deep (absorption tint)", Color) = (0.22, 0.0, 0.02, 1)
        _Glow("Glow (currents, core, caustics)", Color) = (1, 0.3, 0.2, 1)
        _Fill("Fill", Range(0, 1)) = 0.7
        _Trail("Trail", Range(0, 1)) = 0.7
        _Slosh("Slosh", Range(-1, 1)) = 0
        _Clock("Time", Float) = 0
        _Tilt("View from above (radians)", Range(0, 0.6)) = 0.23
        _Absorption("Absorption", Range(0.5, 8)) = 2.4
        _GlassStrength("Glass strength", Range(0, 1)) = 0.55
        _GlassTint("Empty glass coverage", Range(0, 1)) = 0.3
        [Toggle] _Premultiplied("Premultiplied output (the HUD blends One, OneMinusSrcAlpha)", Float) = 1
        [NoScaleOffset] _Glass("Glass overlay", 2D) = "black" {}
        [NoScaleOffset] _Body("Liquid body", 2D) = "gray" {}
        [NoScaleOffset] _Flow("Flow noise", 2D) = "gray" {}
        // where the sphere sits in each texture (centre u, v and radius), measured from the artwork
        _GlassFit("Glass fit", Vector) = (0.4976, 0.5028, 0.4348, 0)
        _BodyFit("Body fit", Vector) = (0.4996, 0.5032, 0.4559, 0)
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
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _Color, _Deep, _Glow;
            float _Fill, _Trail, _Slosh, _Clock, _Tilt, _Absorption, _GlassStrength, _GlassTint, _Premultiplied;
            sampler2D _Glass, _Body, _Flow;
            float4 _GlassFit, _BodyFit;

            #define STEPS 12

            // the surface's height (y up, the sphere's radius 1) under a world point
            float SurfaceAt(float3 w, float fill)
            {
                if (fill >= 0.999) return 2;
                if (fill <= 0.001) return -2;
                float h = lerp(-1, 1, fill);
                float calm = saturate(1 - abs(h)); // the waves fade toward empty and full
                float t = _Clock;
                float wave = sin(w.x * 3.0 + t * 1.9) * 0.022 + sin(w.z * 3.7 - t * 2.4) * 0.016 + sin((w.x - w.z) * 6.5 + t * 3.9) * 0.006;
                return h + (wave * (0.6 + abs(_Slosh) * 2.5) + _Slosh * w.x * 0.3) * calm;
            }

            // the flow noise's brightness, at two scales drifting apart
            float Flow(float3 w)
            {
                float t = _Clock;
                float a = tex2Dlod(_Flow, float4(w.xz * 0.45 + float2(t * 0.03, t * 0.017) + w.y * 0.15, 0, 0)).r;
                float b = tex2Dlod(_Flow, float4(w.xy * 0.8 + float2(-t * 0.021, t * 0.035) + 0.37, 0, 0)).r;
                return a * 0.6 + b * 0.4;
            }

            float Luma(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 p = i.uv * 2 - 1;
                float r = length(p);
                float aa = fwidth(r) * 1.5;
                float edgeAlpha = 1 - smoothstep(1 - aa, 1, r);
                if (edgeAlpha <= 0) return 0;

                float zf = sqrt(saturate(1 - r * r));
                float c = cos(_Tilt), s = sin(_Tilt);
                // a point on the ray (view z) in the world: tilted so the viewer looks down
                #define WORLD(z) float3(p.x, p.y * c + (z) * s, -p.y * s + (z) * c)

                float fill = _Fill;
                float3 front = WORLD(zf);
                float3 back = WORLD(-zf);
                float frontH = SurfaceAt(front, fill);

                // the ray's part under the surface: from where it goes under (the front, or the surface) to the back
                float zEnter = zf;
                bool surface = false;
                bool any = back.y < SurfaceAt(back, fill);
                if (any && front.y > frontH)
                {
                    float lo = zf, hi = -zf;
                    for (int k = 0; k < 7; k++)
                    {
                        float mid = (lo + hi) * 0.5;
                        float3 wm = WORLD(mid);
                        if (wm.y < SurfaceAt(wm, fill)) hi = mid; else lo = mid;
                    }
                    zEnter = (lo + hi) * 0.5;
                    surface = true;
                }

                // premultiplied: light (rgb) and coverage (a)
                float3 light = 0;
                float cover = 0;

                if (any)
                {
                    float body = tex2D(_Body, _BodyFit.xy + p * _BodyFit.z).r;
                    float len = zEnter + zf;
                    float ds = len / STEPS;
                    float3 acc = 0;
                    float trans = 1;
                    for (int j = 0; j < STEPS; j++)
                    {
                        float z = zEnter - (j + 0.5) * ds;
                        float3 w = WORLD(z);
                        float under = SurfaceAt(w, fill) - w.y;
                        float f = Flow(w);
                        float density = 0.5 + f * 0.8;
                        float lit = exp(-under * 1.2);
                        // emission: scattered colour where light reaches, the currents, the core, the caustics low down
                        float currents = smoothstep(0.55, 0.9, f);
                        float core = pow(saturate(1 - length(float2(w.x, w.y + 0.3)) * 0.95), 2);
                        float caustic = smoothstep(-0.5, -0.95, w.y) * (0.4 + body);
                        float3 emit = _Color.rgb * (0.04 + 0.4 * lit) + _Glow.rgb * (currents * 0.42 + core * 0.7 + caustic * 0.75);
                        float a = 1 - exp(-_Absorption * density * ds);
                        acc += trans * a * emit;
                        trans *= 1 - a;
                    }
                    float3 liquid = acc + trans * _Deep.rgb * 0.4;
                    // depth: the body art, darker toward its edge; the sphere's edge darker still
                    liquid *= (0.35 + body * 0.9) * lerp(0.3, 1, pow(zf, 0.6));

                    // a few motes drifting up, catching the light
                    for (int m = 0; m < 6; m++)
                    {
                        float fm = m;
                        float2 at = float2(frac(sin(fm * 12.9898) * 437.585) * 1.2 - 0.6, frac(_Clock * (0.05 + frac(fm * 0.37) * 0.05) + fm * 0.29) * 1.8 - 0.95);
                        float mote = smoothstep(0.02, 0.0, length(p - at)) * (0.5 + 0.5 * sin(_Clock * 3 + fm * 2));
                        float3 wm = WORLD(sqrt(saturate(1 - dot(at, at))));
                        liquid += mote * step(wm.y, SurfaceAt(wm, fill) - 0.04) * lerp(_Glow.rgb, 1, 0.5) * 0.6;
                    }

                    if (surface)
                    {
                        // the surface disk: the liquid's colour, brighter toward the back, a glowing ring at the glass
                        float3 w = WORLD(zEnter);
                        float disk = sqrt(saturate(1 - w.y * w.y));
                        float edge = saturate(length(w.xz) / max(disk, 0.001));
                        float rear = saturate(-w.z / max(disk, 0.001) * 0.5 + 0.5);
                        float ripple = Flow(w * 1.2) - 0.5;
                        float3 top = (_Color.rgb * (0.12 + 0.3 * rear * rear) + _Glow.rgb * 0.08) * (1 + ripple * 0.35);
                        liquid = lerp(liquid, top, 0.4);
                        liquid += smoothstep(0.85, 1.0, edge) * _Glow.rgb * 0.35;
                    }
                    light = liquid;
                    cover = 1;
                }
                else
                {
                    // the empty glass: a faint dark tint over what is behind
                    cover = _GlassTint * (0.6 + 0.4 * (1 - zf));
                    light = _Deep.rgb * 0.05 * cover;
                    // where the level was a moment ago: a pale band
                    float trail = max(_Trail, _Fill);
                    if (trail > fill + 0.002 && front.y < SurfaceAt(front, trail))
                    {
                        light += _Color.rgb * 0.25;
                        cover = max(cover, 0.5);
                    }
                    // the liquid's light spilling onto the glass just above it
                    if (fill > 0.001 && fill < 0.999)
                    {
                        float above = front.y - frontH;
                        light += _Glow.rgb * exp(-max(above, 0) * 9) * 0.22 * smoothstep(-0.02, 0.0, above);
                    }
                }

                // the glowing band where the surface meets the front glass
                if (fill > 0.001 && fill < 0.999)
                {
                    float lip = front.y - frontH;
                    light += smoothstep(0.035, 0.0, abs(lip)) * lerp(_Glow.rgb, 1, 0.35) * 0.55;
                }

                // the glass's reflections, added as light: the artwork (masked to the sphere, weaker at its bright edge) and
                // a thin fresnel rim
                float4 glass = tex2D(_Glass, _GlassFit.xy + p * _GlassFit.z);
                float mask = smoothstep(1.0, 0.96, r);
                float3 reflection = glass.rgb * glass.a * _GlassStrength * mask * lerp(0.35, 1.0, smoothstep(0.97, 0.78, r));
                reflection += smoothstep(0.93, 0.99, r) * smoothstep(1.0, 0.985, r) * 0.25;
                light += reflection;

                // premultiplied for the HUD's One, OneMinusSrcAlpha blend: added light simply adds over what is behind
                if (_Premultiplied > 0.5) return float4(light * edgeAlpha, saturate(cover) * edgeAlpha);
                // straight alpha (a plain blend): added light raises the coverage it needs to show
                cover = saturate(max(cover, max(light.r, max(light.g, light.b))));
                float3 colour = cover > 0.0001 ? saturate(light / cover) : 0;
                return fixed4(colour, cover * edgeAlpha);
            }
            ENDCG
        }
    }
}
