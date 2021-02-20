// Upgrade NOTE: upgraded instancing buffer 'MultiWeapon' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "MultiWeapon"
{
	Properties
	{
		_TextureSample1("Texture Sample 1", 2D) = "white" {}
		_HandleColor("Handle Color", Color) = (0.3773585,0.1495812,0.01957992,0)
		_EdgeColor("Edge Color", Color) = (0,0.9631133,1,0)
		_BodyColor("Body Color", Color) = (0,0.6602616,1,0)
		_InnerEdgeColor("Inner Edge Color", Color) = (0,1,0.9608457,0)
		_Emission("Emission", Color) = (1,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows exclude_path:deferred 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _TextureSample1;

		UNITY_INSTANCING_BUFFER_START(MultiWeapon)
			UNITY_DEFINE_INSTANCED_PROP(float4, _HandleColor)
#define _HandleColor_arr MultiWeapon
			UNITY_DEFINE_INSTANCED_PROP(float4, _BodyColor)
#define _BodyColor_arr MultiWeapon
			UNITY_DEFINE_INSTANCED_PROP(float4, _InnerEdgeColor)
#define _InnerEdgeColor_arr MultiWeapon
			UNITY_DEFINE_INSTANCED_PROP(float4, _EdgeColor)
#define _EdgeColor_arr MultiWeapon
			UNITY_DEFINE_INSTANCED_PROP(float4, _TextureSample1_ST)
#define _TextureSample1_ST_arr MultiWeapon
			UNITY_DEFINE_INSTANCED_PROP(float4, _Emission)
#define _Emission_arr MultiWeapon
		UNITY_INSTANCING_BUFFER_END(MultiWeapon)


		float3 HSVToRGB( float3 c )
		{
			float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
			float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
			return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float clampResult72 = clamp( _CosTime.z , -0.5 , 0.5 );
			float2 temp_cast_0 = ((i.uv_texcoord.x*1.0 + clampResult72)).xx;
			float2 appendResult10_g19 = (float2(1.0 , 1.0));
			float2 temp_output_11_0_g19 = ( abs( (temp_cast_0*2.0 + -1.0) ) - appendResult10_g19 );
			float2 break16_g19 = ( 1.0 - ( temp_output_11_0_g19 / fwidth( temp_output_11_0_g19 ) ) );
			float temp_output_70_0 = saturate( min( break16_g19.x , break16_g19.y ) );
			float mulTime57 = _Time.y * 0.1;
			float3 hsvTorgb3_g13 = HSVToRGB( float3(mulTime57,1.0,1.0) );
			float3 temp_output_56_6 = hsvTorgb3_g13;
			float4 _HandleColor_Instance = UNITY_ACCESS_INSTANCED_PROP(_HandleColor_arr, _HandleColor);
			float2 appendResult10_g17 = (float2(1.0 , 0.5));
			float2 temp_output_11_0_g17 = ( abs( (i.uv_texcoord*2.0 + -1.0) ) - appendResult10_g17 );
			float2 break16_g17 = ( 1.0 - ( temp_output_11_0_g17 / fwidth( temp_output_11_0_g17 ) ) );
			float temp_output_38_0 = saturate( min( break16_g17.x , break16_g17.y ) );
			float2 temp_cast_2 = ((i.uv_texcoord.x*1.0 + 0.5)).xx;
			float2 appendResult10_g16 = (float2(0.67 , 1.0));
			float2 temp_output_11_0_g16 = ( abs( (temp_cast_2*2.0 + -1.0) ) - appendResult10_g16 );
			float2 break16_g16 = ( 1.0 - ( temp_output_11_0_g16 / fwidth( temp_output_11_0_g16 ) ) );
			float temp_output_3_0 = saturate( min( break16_g16.x , break16_g16.y ) );
			float2 temp_cast_3 = ((i.uv_texcoord.x*1.49 + -0.2)).xx;
			float2 appendResult10_g14 = (float2(0.5 , 0.5));
			float2 temp_output_11_0_g14 = ( abs( (temp_cast_3*2.0 + -1.0) ) - appendResult10_g14 );
			float2 break16_g14 = ( 1.0 - ( temp_output_11_0_g14 / fwidth( temp_output_11_0_g14 ) ) );
			float4 _BodyColor_Instance = UNITY_ACCESS_INSTANCED_PROP(_BodyColor_arr, _BodyColor);
			float2 temp_cast_5 = ((i.uv_texcoord.x*1.0 + -0.38)).xx;
			float2 appendResult10_g15 = (float2(0.5 , 0.5));
			float2 temp_output_11_0_g15 = ( abs( (temp_cast_5*2.0 + -1.0) ) - appendResult10_g15 );
			float2 break16_g15 = ( 1.0 - ( temp_output_11_0_g15 / fwidth( temp_output_11_0_g15 ) ) );
			float4 _InnerEdgeColor_Instance = UNITY_ACCESS_INSTANCED_PROP(_InnerEdgeColor_arr, _InnerEdgeColor);
			float2 appendResult10_g18 = (float2(0.5 , 0.23));
			float2 temp_output_11_0_g18 = ( abs( ((i.uv_texcoord*1.0 + 0.03)*2.0 + -1.0) ) - appendResult10_g18 );
			float2 break16_g18 = ( 1.0 - ( temp_output_11_0_g18 / fwidth( temp_output_11_0_g18 ) ) );
			float temp_output_10_0 = saturate( min( break16_g18.x , break16_g18.y ) );
			float4 _EdgeColor_Instance = UNITY_ACCESS_INSTANCED_PROP(_EdgeColor_arr, _EdgeColor);
			float4 temp_output_43_0 = ( ( ( float4( temp_output_56_6 , 0.0 ) * _HandleColor_Instance ) * temp_output_38_0 ) + ( ( ( ( ( _HandleColor_Instance * temp_output_3_0 ) + ( ( ( saturate( min( break16_g14.x , break16_g14.y ) ) * ( float4( temp_output_56_6 , 0.0 ) * _BodyColor_Instance ) ) + ( saturate( min( break16_g15.x , break16_g15.y ) ) * ( float4( temp_output_56_6 , 0.0 ) * _InnerEdgeColor_Instance ) ) ) * ( 1.0 - temp_output_3_0 ) ) ) * ( 1.0 - temp_output_38_0 ) ) * ( 1.0 - temp_output_10_0 ) ) + ( temp_output_10_0 * ( float4( temp_output_56_6 , 0.0 ) * _EdgeColor_Instance ) ) ) );
			float clampResult68 = clamp( (i.uv_texcoord.x*_CosTime.w + 0.0) , 0.25 , 1.0 );
			float4 temp_output_76_0 = ( ( temp_output_70_0 * temp_output_43_0 ) + ( ( 1.0 - temp_output_70_0 ) * ( clampResult68 * temp_output_43_0 ) ) );
			o.Albedo = temp_output_76_0.rgb;
			float4 _TextureSample1_ST_Instance = UNITY_ACCESS_INSTANCED_PROP(_TextureSample1_ST_arr, _TextureSample1_ST);
			float2 uv_TextureSample1 = i.uv_texcoord * _TextureSample1_ST_Instance.xy + _TextureSample1_ST_Instance.zw;
			float4 _Emission_Instance = UNITY_ACCESS_INSTANCED_PROP(_Emission_arr, _Emission);
			o.Emission = ( tex2D( _TextureSample1, uv_TextureSample1 ) * ( float4( temp_output_56_6 , 0.0 ) * _Emission_Instance ) ).rgb;
			float grayscale78 = Luminance(temp_output_76_0.rgb);
			o.Metallic = grayscale78;
			o.Occlusion = grayscale78;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18500
164;534;2153;740;-1597.908;245.4398;1.080815;True;True
Node;AmplifyShaderEditor.TexCoordVertexDataNode;6;-2251.909,-317.4402;Inherit;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleTimeNode;57;-2916.147,55.85325;Inherit;False;1;0;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;49;-1872.337,76.81;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;-0.38;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;45;-2960.918,476.4865;Inherit;False;InstancedProperty;_InnerEdgeColor;Inner Edge Color;4;0;Create;True;0;0;False;0;False;0,1,0.9608457,0;0.9150943,0.9150943,0.9150943,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;56;-2658.031,-150.5348;Inherit;True;Simple HUE;-1;;13;32abb5f0db087604486c2db83a2e817a;0;1;1;FLOAT;0;False;4;FLOAT3;6;FLOAT;7;FLOAT;5;FLOAT;8
Node;AmplifyShaderEditor.ColorNode;44;-2948.407,219.8691;Inherit;False;InstancedProperty;_BodyColor;Body Color;3;0;Create;True;0;0;False;0;False;0,0.6602616,1,0;0.6603774,0.6603774,0.6603774,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScaleAndOffsetNode;48;-1887.839,-232.821;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;1.49;False;2;FLOAT;-0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;4;-1501.371,-537.3174;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;59;-2309.109,534.3058;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;46;-1544.567,-74.51701;Inherit;True;Rectangle;-1;;14;6b23e0c975270fb4084c354b2c83366a;0;3;1;FLOAT2;0,0;False;2;FLOAT;0.5;False;3;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;58;-2265.761,249.2395;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;47;-1550.924,315.8606;Inherit;True;Rectangle;-1;;15;6b23e0c975270fb4084c354b2c83366a;0;3;1;FLOAT2;0,0;False;2;FLOAT;0.5;False;3;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;50;-1215.657,679.8785;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;51;-1279.648,126.1589;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;3;-1210.505,-532.2025;Inherit;True;Rectangle;-1;;16;6b23e0c975270fb4084c354b2c83366a;0;3;1;FLOAT2;0,0;False;2;FLOAT;0.67;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;52;-1083.364,182.4046;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;26;-2924.93,751.0463;Inherit;False;InstancedProperty;_HandleColor;Handle Color;1;0;Create;True;0;0;False;0;False;0.3773585,0.1495812,0.01957992,0;0.3207546,0.3207546,0.3207546,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;17;-851.9561,-306.7556;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;28;-265.3914,-6.984686;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;37;-742.8874,645.8502;Inherit;True;3;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT;0.03;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;38;-312.9556,356.5397;Inherit;True;Rectangle;-1;;17;6b23e0c975270fb4084c354b2c83366a;0;3;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-198.9843,-333.2923;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;40;1.410014,426.6602;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;10;-285.7381,687.2734;Inherit;True;Rectangle;-1;;18;6b23e0c975270fb4084c354b2c83366a;0;3;1;FLOAT2;0,0;False;2;FLOAT;0.5;False;3;FLOAT;0.23;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;27;164.1405,-228.4772;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;31;-2958.052,986.3284;Inherit;False;InstancedProperty;_EdgeColor;Edge Color;2;0;Create;True;0;0;False;0;False;0,0.9631133,1,0;0.4811321,0.4811321,0.4811321,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;29;317.3518,592.448;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CosTime;71;1010.902,-740.8118;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;41;511.8982,-151.5371;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;61;-2285.713,1000.393;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;575.6898,47.31976;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;191.2104,902.9064;Inherit;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;60;-2300.835,814.1824;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CosTime;64;299.9213,-445.8947;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ClampOpNode;72;1218.902,-677.8118;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;-0.5;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;69;1222.19,-526.4571;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;63;550.6221,-602.3455;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;33;734.3643,446.8669;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;39;-18.16146,246.3513;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;43;671.1686,-344.1828;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;70;1510.602,-663.4686;Inherit;True;Rectangle;-1;;19;6b23e0c975270fb4084c354b2c83366a;0;3;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;68;838.3065,-653.1025;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0.25;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;74;1832.902,-588.8118;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;65;979.0424,-348.627;Inherit;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;75;1806.902,-218.8118;Inherit;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;73;1966.902,-467.8118;Inherit;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;53;-2969.102,1380.74;Inherit;False;InstancedProperty;_Emission;Emission;5;0;Create;True;0;0;False;0;False;1,0,0,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;76;2527.808,-228.6971;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;2;-1824.341,1041.887;Inherit;True;Property;_TextureSample1;Texture Sample 1;0;0;Create;True;0;0;False;0;False;-1;4956ed4698e0e8d4c9045cb551724fe0;4956ed4698e0e8d4c9045cb551724fe0;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;62;-2302.368,1241.173;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCGrayscale;78;2451.753,182.5628;Inherit;True;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;54;-715.3347,1139.048;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;2839.424,-138.643;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;MultiWeapon;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;ForwardOnly;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0.005;0.1226415,0.1226415,0.1226415,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;49;0;6;1
WireConnection;56;1;57;0
WireConnection;48;0;6;1
WireConnection;4;0;6;1
WireConnection;59;0;56;6
WireConnection;59;1;45;0
WireConnection;46;1;48;0
WireConnection;58;0;56;6
WireConnection;58;1;44;0
WireConnection;47;1;49;0
WireConnection;50;0;47;0
WireConnection;50;1;59;0
WireConnection;51;0;46;0
WireConnection;51;1;58;0
WireConnection;3;1;4;0
WireConnection;52;0;51;0
WireConnection;52;1;50;0
WireConnection;17;0;3;0
WireConnection;28;0;26;0
WireConnection;28;1;3;0
WireConnection;37;0;6;0
WireConnection;38;1;6;0
WireConnection;16;0;52;0
WireConnection;16;1;17;0
WireConnection;40;0;38;0
WireConnection;10;1;37;0
WireConnection;27;0;28;0
WireConnection;27;1;16;0
WireConnection;29;0;10;0
WireConnection;41;0;27;0
WireConnection;41;1;40;0
WireConnection;61;0;56;6
WireConnection;61;1;31;0
WireConnection;30;0;41;0
WireConnection;30;1;29;0
WireConnection;32;0;10;0
WireConnection;32;1;61;0
WireConnection;60;0;56;6
WireConnection;60;1;26;0
WireConnection;72;0;71;3
WireConnection;69;0;6;1
WireConnection;69;2;72;0
WireConnection;63;0;6;1
WireConnection;63;1;64;4
WireConnection;33;0;30;0
WireConnection;33;1;32;0
WireConnection;39;0;60;0
WireConnection;39;1;38;0
WireConnection;43;0;39;0
WireConnection;43;1;33;0
WireConnection;70;1;69;0
WireConnection;68;0;63;0
WireConnection;74;0;70;0
WireConnection;65;0;68;0
WireConnection;65;1;43;0
WireConnection;75;0;74;0
WireConnection;75;1;65;0
WireConnection;73;0;70;0
WireConnection;73;1;43;0
WireConnection;76;0;73;0
WireConnection;76;1;75;0
WireConnection;62;0;56;6
WireConnection;62;1;53;0
WireConnection;78;0;76;0
WireConnection;54;0;2;0
WireConnection;54;1;62;0
WireConnection;0;0;76;0
WireConnection;0;2;54;0
WireConnection;0;3;78;0
WireConnection;0;5;78;0
ASEEND*/
//CHKSM=4244BCF206FCF188BA59278E36C10F46D6C93516