Shader "Sprites/Outline"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		_Brightness("Outline Brightness", Range(0,8)) = 3.2
		_Width("Outline Width", Range(0,0.05)) = 0.003
		_OutlineTex("Outline Texture", 2D) = "white" {}
		_SpeedX("Scroll Speed X", Range(-20,20)) = 0.003
		_SpeedY("Scroll Speed Y", Range(-20,20)) = 0.003
		_OutlineColor("OutlineColor", Color) = (1,1,1,1)
		[MaterialToggle] JustOutline("JustOutline", Float) = 0
		[MaterialToggle] TexturedOutline("TexturedOutline", Float) = 0
			// stencil for (UI) Masking
			_StencilComp("Stencil Comparison", Float) = 8
			_Stencil("Stencil ID", Float) = 0
			_StencilOp("Stencil Operation", Float) = 0
			_StencilWriteMask("Stencil Write Mask", Float) = 255
			_StencilReadMask("Stencil Read Mask", Float) = 255
			_ColorMask("Color Mask", Float) = 15
	}

		SubShader
		{
			Tags
			{
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
				"PreviewType" = "Plane"
				"CanUseSpriteAtlas" = "True"
			}
			// stencil for (UI) Masking
			 Stencil
			{
				Ref[_Stencil]
				Comp[_StencilComp]
				Pass[_StencilOp]
				ReadMask[_StencilReadMask]
				WriteMask[_StencilWriteMask]
			}
			Cull Off
			Lighting Off
			ZWrite Off
			Blend One OneMinusSrcAlpha


			Pass
			{
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile _ JUSTOUTLINE_ON
				#pragma multi_compile _ TEXTUREDOUTLINE_ON
				#include "UnityCG.cginc"

				struct appdata_t
				{
					float4 vertex   : POSITION;
					float4 color    : COLOR;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f
				{
					float4 vertex   : SV_POSITION;
					fixed4 color : COLOR;
					float2 texcoord  : TEXCOORD0;
				};


				v2f vert(appdata_t IN)
				{
					v2f OUT;
					 UNITY_INITIALIZE_OUTPUT(v2f,OUT)
					OUT.vertex = UnityObjectToClipPos(IN.vertex);
					OUT.texcoord = IN.texcoord;
					return OUT;
				}

				fixed4 _Color;
				fixed4 _OutlineColor;
				sampler2D _MainTex;
				sampler2D _OutlineTex;
				float _Brightness;
				float _Width;
				float _SpeedX, _SpeedY;

				fixed4 frag(v2f IN) : SV_Target
				{
					fixed4 c = tex2D(_MainTex, IN.texcoord) * _Color;
					fixed4 t = tex2D(_OutlineTex, float2(IN.texcoord.x + (_Time.x * _SpeedX), IN.texcoord.y + (_Time.x * _SpeedY))) * _Color;
					c.rgb *= c.a;

					// Move sprite in 4 directions according to width, we only care about the alpha
					float spriteLeft = tex2D(_MainTex, IN.texcoord + float2(_Width, 0)).a;
					float spriteRight = tex2D(_MainTex, IN.texcoord - float2(_Width,  0)).a;
					float spriteBottom = tex2D(_MainTex, IN.texcoord + float2(0 ,_Width)).a;
					float spriteTop = tex2D(_MainTex, IN.texcoord - float2(0 , _Width)).a;

					// then combine
					float result = (spriteRight + spriteLeft + spriteTop + spriteBottom);
					// delete original alpha to only leave outline
					result *= (1 - c.a);
					// add color and brightness
					float4 outlines = result * _OutlineColor * _Brightness;

					#ifdef TEXTUREDOUTLINE_ON
					outlines *= t;
					#endif
					#ifdef JUSTOUTLINE_ON
					// only show outlines
					c = outlines;
					#else
					// show outlines +sprite
					c.rgb = c.rgb + outlines;
					#endif

					return  c;
				}
			ENDCG
			}
		}
}