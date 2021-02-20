// Upgrade NOTE: upgraded instancing buffer 'Weapon' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Weapon"
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
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _TextureSample1;

		UNITY_INSTANCING_BUFFER_START(Weapon)
			UNITY_DEFINE_INSTANCED_PROP(float4, _HandleColor)
#define _HandleColor_arr Weapon
			UNITY_DEFINE_INSTANCED_PROP(float4, _BodyColor)
#define _BodyColor_arr Weapon
			UNITY_DEFINE_INSTANCED_PROP(float4, _InnerEdgeColor)
#define _InnerEdgeColor_arr Weapon
			UNITY_DEFINE_INSTANCED_PROP(float4, _EdgeColor)
#define _EdgeColor_arr Weapon
			UNITY_DEFINE_INSTANCED_PROP(float4, _TextureSample1_ST)
#define _TextureSample1_ST_arr Weapon
			UNITY_DEFINE_INSTANCED_PROP(float4, _Emission)
#define _Emission_arr Weapon
		UNITY_INSTANCING_BUFFER_END(Weapon)

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float4 _HandleColor_Instance = UNITY_ACCESS_INSTANCED_PROP(_HandleColor_arr, _HandleColor);
			float2 appendResult10_g10 = (float2(1.0 , 0.5));
			float2 temp_output_11_0_g10 = ( abs( (i.uv_texcoord*2.0 + -1.0) ) - appendResult10_g10 );
			float2 break16_g10 = ( 1.0 - ( temp_output_11_0_g10 / fwidth( temp_output_11_0_g10 ) ) );
			float temp_output_38_0 = saturate( min( break16_g10.x , break16_g10.y ) );
			float2 temp_cast_0 = ((i.uv_texcoord.x*1.0 + 0.5)).xx;
			float2 appendResult10_g9 = (float2(0.67 , 1.0));
			float2 temp_output_11_0_g9 = ( abs( (temp_cast_0*2.0 + -1.0) ) - appendResult10_g9 );
			float2 break16_g9 = ( 1.0 - ( temp_output_11_0_g9 / fwidth( temp_output_11_0_g9 ) ) );
			float temp_output_3_0 = saturate( min( break16_g9.x , break16_g9.y ) );
			float2 temp_cast_1 = ((i.uv_texcoord.x*1.49 + -0.2)).xx;
			float2 appendResult10_g7 = (float2(0.5 , 0.5));
			float2 temp_output_11_0_g7 = ( abs( (temp_cast_1*2.0 + -1.0) ) - appendResult10_g7 );
			float2 break16_g7 = ( 1.0 - ( temp_output_11_0_g7 / fwidth( temp_output_11_0_g7 ) ) );
			float4 _BodyColor_Instance = UNITY_ACCESS_INSTANCED_PROP(_BodyColor_arr, _BodyColor);
			float2 temp_cast_2 = ((i.uv_texcoord.x*1.0 + -0.38)).xx;
			float2 appendResult10_g8 = (float2(0.5 , 0.5));
			float2 temp_output_11_0_g8 = ( abs( (temp_cast_2*2.0 + -1.0) ) - appendResult10_g8 );
			float2 break16_g8 = ( 1.0 - ( temp_output_11_0_g8 / fwidth( temp_output_11_0_g8 ) ) );
			float4 _InnerEdgeColor_Instance = UNITY_ACCESS_INSTANCED_PROP(_InnerEdgeColor_arr, _InnerEdgeColor);
			float2 appendResult10_g11 = (float2(0.5 , 0.23));
			float2 temp_output_11_0_g11 = ( abs( ((i.uv_texcoord*1.0 + 0.03)*2.0 + -1.0) ) - appendResult10_g11 );
			float2 break16_g11 = ( 1.0 - ( temp_output_11_0_g11 / fwidth( temp_output_11_0_g11 ) ) );
			float temp_output_10_0 = saturate( min( break16_g11.x , break16_g11.y ) );
			float4 _EdgeColor_Instance = UNITY_ACCESS_INSTANCED_PROP(_EdgeColor_arr, _EdgeColor);
			o.Albedo = ( ( _HandleColor_Instance * temp_output_38_0 ) + ( ( ( ( ( _HandleColor_Instance * temp_output_3_0 ) + ( ( ( saturate( min( break16_g7.x , break16_g7.y ) ) * _BodyColor_Instance ) + ( saturate( min( break16_g8.x , break16_g8.y ) ) * _InnerEdgeColor_Instance ) ) * ( 1.0 - temp_output_3_0 ) ) ) * ( 1.0 - temp_output_38_0 ) ) * ( 1.0 - temp_output_10_0 ) ) + ( temp_output_10_0 * _EdgeColor_Instance ) ) ).rgb;
			float4 _TextureSample1_ST_Instance = UNITY_ACCESS_INSTANCED_PROP(_TextureSample1_ST_arr, _TextureSample1_ST);
			float2 uv_TextureSample1 = i.uv_texcoord * _TextureSample1_ST_Instance.xy + _TextureSample1_ST_Instance.zw;
			float4 _Emission_Instance = UNITY_ACCESS_INSTANCED_PROP(_Emission_arr, _Emission);
			o.Emission = ( tex2D( _TextureSample1, uv_TextureSample1 ) * _Emission_Instance ).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18500
211;441;2153;769;3411.326;75.76605;2.660793;True;True
Node;AmplifyShaderEditor.TexCoordVertexDataNode;6;-2251.909,-317.4402;Inherit;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScaleAndOffsetNode;49;-1872.337,76.81;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;-0.38;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;48;-1887.839,-232.821;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;1.49;False;2;FLOAT;-0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;46;-1544.567,-74.51701;Inherit;True;Rectangle;-1;;7;6b23e0c975270fb4084c354b2c83366a;0;3;1;FLOAT2;0,0;False;2;FLOAT;0.5;False;3;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;47;-1572.803,174.7579;Inherit;True;Rectangle;-1;;8;6b23e0c975270fb4084c354b2c83366a;0;3;1;FLOAT2;0,0;False;2;FLOAT;0.5;False;3;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;45;-2311.365,520.697;Inherit;False;InstancedProperty;_InnerEdgeColor;Inner Edge Color;5;0;Create;True;0;0;False;0;False;0,1,0.9608457,0;1,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;44;-2298.854,264.0796;Inherit;False;InstancedProperty;_BodyColor;Body Color;4;0;Create;True;0;0;False;0;False;0,0.6602616,1,0;1,0.5209654,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScaleAndOffsetNode;4;-1501.371,-537.3174;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;50;-1124.316,325.5158;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;51;-1266.297,104.3109;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;3;-1210.505,-532.2025;Inherit;True;Rectangle;-1;;9;6b23e0c975270fb4084c354b2c83366a;0;3;1;FLOAT2;0,0;False;2;FLOAT;0.67;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;17;-905.637,-220.8664;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;52;-1083.364,182.4046;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;26;-2312.492,782.0015;Inherit;False;InstancedProperty;_HandleColor;Handle Color;2;0;Create;True;0;0;False;0;False;0.3773585,0.1495812,0.01957992,0;0.3773585,0.1495812,0.01957992,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScaleAndOffsetNode;37;-742.8874,645.8502;Inherit;True;3;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT;0.03;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;28;-201.0796,-0.859754;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;38;-312.9556,356.5397;Inherit;True;Rectangle;-1;;10;6b23e0c975270fb4084c354b2c83366a;0;3;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-198.9843,-333.2923;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;27;164.1405,-228.4772;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;40;1.410014,426.6602;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;10;-285.7381,687.2734;Inherit;True;Rectangle;-1;;11;6b23e0c975270fb4084c354b2c83366a;0;3;1;FLOAT2;0,0;False;2;FLOAT;0.5;False;3;FLOAT;0.23;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;29;317.3518,592.448;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;31;-2308.499,1030.539;Inherit;False;InstancedProperty;_EdgeColor;Edge Color;3;0;Create;True;0;0;False;0;False;0,0.9631133,1,0;0,0.9631133,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;41;511.8982,-151.5371;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;191.2104,902.9064;Inherit;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;575.6898,47.31976;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;53;-2319.548,1424.95;Inherit;False;InstancedProperty;_Emission;Emission;6;0;Create;True;0;0;False;0;False;1,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-1793.148,1143.785;Inherit;True;Property;_TextureSample1;Texture Sample 1;1;0;Create;True;0;0;False;0;False;-1;4956ed4698e0e8d4c9045cb551724fe0;4956ed4698e0e8d4c9045cb551724fe0;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;33;734.3643,446.8669;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;39;215.7803,211.8902;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;1;-211.0686,-699.047;Inherit;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;False;0;False;-1;efd6bf6a048996b4ba490638b4b11bdf;efd6bf6a048996b4ba490638b4b11bdf;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;43;891.9189,31.70957;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;54;-715.3347,1139.048;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1185.436,-230.3206;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Weapon;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;49;0;6;1
WireConnection;48;0;6;1
WireConnection;46;1;48;0
WireConnection;47;1;49;0
WireConnection;4;0;6;1
WireConnection;50;0;47;0
WireConnection;50;1;45;0
WireConnection;51;0;46;0
WireConnection;51;1;44;0
WireConnection;3;1;4;0
WireConnection;17;0;3;0
WireConnection;52;0;51;0
WireConnection;52;1;50;0
WireConnection;37;0;6;0
WireConnection;28;0;26;0
WireConnection;28;1;3;0
WireConnection;38;1;6;0
WireConnection;16;0;52;0
WireConnection;16;1;17;0
WireConnection;27;0;28;0
WireConnection;27;1;16;0
WireConnection;40;0;38;0
WireConnection;10;1;37;0
WireConnection;29;0;10;0
WireConnection;41;0;27;0
WireConnection;41;1;40;0
WireConnection;32;0;10;0
WireConnection;32;1;31;0
WireConnection;30;0;41;0
WireConnection;30;1;29;0
WireConnection;33;0;30;0
WireConnection;33;1;32;0
WireConnection;39;0;26;0
WireConnection;39;1;38;0
WireConnection;43;0;39;0
WireConnection;43;1;33;0
WireConnection;54;0;2;0
WireConnection;54;1;53;0
WireConnection;0;0;43;0
WireConnection;0;2;54;0
ASEEND*/
//CHKSM=0A7D7342DD14EBA7DE00248A92239A869F60642F