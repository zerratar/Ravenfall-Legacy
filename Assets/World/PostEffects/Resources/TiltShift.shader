// Tilt shift: the world as a miniature. A band across the screen stays sharp and above and below it the picture
// blurs more and more, as a close up photo of a model would.
//
// TiltShift.cs runs it: the picture is copied to half and to quarter size and each is blurred (passes 0 and 1, a
// separable Gaussian); the composite (pass 2) blends from the sharp picture to the half size blur and then to the
// quarter size blur the further a pixel is from the band, so the blur grows smoothly and costs little.
Shader "Hidden/Ravenfall/Tilt Shift"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    TEXTURE2D_X(_TiltHalf);
    TEXTURE2D_X(_TiltQuarter);
    float _TiltFocus;       // the band's middle, 0 bottom to 1 top
    float _TiltBand;        // half the band's height, sharp
    float _TiltFalloff;     // how far beyond it the blur reaches its strongest
    float _TiltStrength;    // 0 to 1

    // nine taps, Gaussian weights, two texels apart with bilinear filtering in between
    half4 Blur(float2 uv, float2 step)
    {
        static const float weights[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };
        half4 sum = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv) * weights[0];
        [unroll] for (int i = 1; i < 5; ++i)
        {
            float2 offset = step * (i * 1.5);
            sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + offset) * weights[i];
            sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - offset) * weights[i];
        }
        return sum;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "Blur Across"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            half4 Fragment(Varyings input) : SV_Target
            {
                return Blur(input.texcoord, float2(_BlitTexture_TexelSize.x, 0));
            }
            ENDHLSL
        }

        Pass
        {
            Name "Blur Down"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            half4 Fragment(Varyings input) : SV_Target
            {
                return Blur(input.texcoord, float2(0, _BlitTexture_TexelSize.y));
            }
            ENDHLSL
        }

        Pass
        {
            Name "Composite"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            half4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 sharp = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                half4 half_ = SAMPLE_TEXTURE2D_X(_TiltHalf, sampler_LinearClamp, uv);
                half4 quarter = SAMPLE_TEXTURE2D_X(_TiltQuarter, sampler_LinearClamp, uv);

                // 0 in the band, 1 at the strongest blur; eased so the band has no edge
                float away = abs(uv.y - _TiltFocus);
                float blur = smoothstep(_TiltBand, _TiltBand + _TiltFalloff, away) * _TiltStrength;
                half4 colour = lerp(sharp, half_, saturate(blur * 2));
                colour = lerp(colour, quarter, saturate(blur * 2 - 1));

                return colour;
            }
            ENDHLSL
        }
    }
}
