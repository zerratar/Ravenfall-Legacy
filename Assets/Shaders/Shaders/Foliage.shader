// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Shader created with Shader Forge v1.38 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.38;sub:START;pass:START;ps:flbk:Standard,iptp:0,cusa:False,bamd:0,cgin:,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:True,nrmq:0,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:2,bsrc:0,bdst:1,dpts:2,wrdp:True,dith:1,atcv:False,rfrpo:True,rfrpn:Refraction,coma:15,ufog:True,aust:True,igpj:False,qofs:0,qpre:2,rntp:3,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.086,fgcg:0.0509804,fgcb:0.2901961,fgca:1,fgde:0.01,fgrn:25,fgrf:500,stcl:False,atwp:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False,fsmp:False;n:type:ShaderForge.SFN_Final,id:4013,x:33566,y:32682,varname:node_4013,prsc:2|emission-5402-OUT,custl-3955-OUT,clip-8239-OUT,voffset-4846-OUT;n:type:ShaderForge.SFN_Color,id:1304,x:31692,y:32036,ptovrint:False,ptlb:Color,ptin:_Color,varname:node_1304,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:1,c3:1,c4:1;n:type:ShaderForge.SFN_Append,id:6981,x:31624,y:32629,varname:node_6981,prsc:2|A-9354-OUT,B-9354-OUT;n:type:ShaderForge.SFN_Tex2d,id:4602,x:31667,y:32394,ptovrint:False,ptlb:Light Ramp,ptin:_LightRamp,varname:node_4602,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:7ea4fa8f6637d234cabf1448f60f5b81,ntxv:0,isnm:False|UVIN-6981-OUT;n:type:ShaderForge.SFN_LightAttenuation,id:9477,x:31358,y:32828,varname:node_9477,prsc:2;n:type:ShaderForge.SFN_LightColor,id:2000,x:32101,y:32747,varname:node_2000,prsc:2;n:type:ShaderForge.SFN_Multiply,id:9649,x:32155,y:32576,varname:node_9649,prsc:2|A-7721-OUT,B-9185-OUT;n:type:ShaderForge.SFN_Blend,id:7721,x:32153,y:32354,varname:node_7721,prsc:2,blmd:12,clmp:True|SRC-5860-OUT,DST-4602-RGB;n:type:ShaderForge.SFN_If,id:5860,x:32197,y:32009,varname:node_5860,prsc:2|A-2542-OUT,B-1799-OUT,GT-1304-RGB,EQ-5089-OUT,LT-1304-RGB;n:type:ShaderForge.SFN_Vector1,id:1799,x:31783,y:31940,varname:node_1799,prsc:2,v1:1;n:type:ShaderForge.SFN_ToggleProperty,id:2542,x:31783,y:31878,ptovrint:False,ptlb:Use Albedo,ptin:_UseAlbedo,varname:node_2542,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:False;n:type:ShaderForge.SFN_Multiply,id:5089,x:31940,y:32179,varname:node_5089,prsc:2|A-1304-RGB,B-7006-RGB;n:type:ShaderForge.SFN_Tex2d,id:7006,x:31692,y:32201,ptovrint:False,ptlb:Albedo,ptin:_Albedo,varname:node_7006,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False;n:type:ShaderForge.SFN_VertexColor,id:2359,x:32066,y:33217,varname:node_2359,prsc:2;n:type:ShaderForge.SFN_Multiply,id:4846,x:32404,y:33196,varname:node_4846,prsc:2|A-2359-R,B-4953-OUT;n:type:ShaderForge.SFN_Multiply,id:8284,x:32127,y:33476,varname:node_8284,prsc:2|A-5990-OUT,B-2639-OUT;n:type:ShaderForge.SFN_Multiply,id:2639,x:32127,y:33624,varname:node_2639,prsc:2|A-9111-OUT,B-7813-OUT;n:type:ShaderForge.SFN_Multiply,id:7813,x:32127,y:33762,varname:node_7813,prsc:2|A-1007-V,B-1095-OUT;n:type:ShaderForge.SFN_FragmentPosition,id:2245,x:31500,y:33269,varname:node_2245,prsc:2;n:type:ShaderForge.SFN_Slider,id:9111,x:31632,y:33539,ptovrint:False,ptlb:Wind Intensity,ptin:_WindIntensity,varname:node_9111,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0.25,max:0.5;n:type:ShaderForge.SFN_TexCoord,id:1007,x:31632,y:33714,varname:node_1007,prsc:2,uv:0,uaff:False;n:type:ShaderForge.SFN_Cos,id:1095,x:32147,y:33963,varname:node_1095,prsc:2|IN-2719-OUT;n:type:ShaderForge.SFN_Time,id:7596,x:31902,y:33963,varname:node_7596,prsc:2;n:type:ShaderForge.SFN_Append,id:6497,x:32419,y:33537,varname:node_6497,prsc:2|A-8284-OUT,B-2361-OUT,C-8284-OUT;n:type:ShaderForge.SFN_Vector1,id:2361,x:32398,y:33724,varname:node_2361,prsc:2,v1:0;n:type:ShaderForge.SFN_Multiply,id:2719,x:32147,y:34077,varname:node_2719,prsc:2|A-7596-TSL,B-7155-OUT;n:type:ShaderForge.SFN_Vector1,id:7155,x:32188,y:34222,varname:node_7155,prsc:2,v1:15;n:type:ShaderForge.SFN_Sin,id:5990,x:31732,y:33336,varname:node_5990,prsc:2|IN-2245-X;n:type:ShaderForge.SFN_If,id:4953,x:32808,y:33472,varname:node_4953,prsc:2|A-7220-OUT,B-7240-OUT,GT-8284-OUT,EQ-6497-OUT,LT-8284-OUT;n:type:ShaderForge.SFN_Vector1,id:7220,x:32595,y:33406,varname:node_7220,prsc:2,v1:1;n:type:ShaderForge.SFN_ToggleProperty,id:7240,x:32595,y:33480,ptovrint:False,ptlb:Is Grass,ptin:_IsGrass,varname:node_7240,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:True;n:type:ShaderForge.SFN_NormalVector,id:543,x:30579,y:32681,prsc:2,pt:False;n:type:ShaderForge.SFN_Dot,id:5980,x:30862,y:32589,varname:node_5980,prsc:2,dt:4|A-9266-OUT,B-543-OUT;n:type:ShaderForge.SFN_If,id:9354,x:31344,y:32433,varname:node_9354,prsc:2|A-7220-OUT,B-7240-OUT,GT-6634-OUT,EQ-9477-OUT,LT-6634-OUT;n:type:ShaderForge.SFN_LightVector,id:9266,x:30579,y:32514,varname:node_9266,prsc:2;n:type:ShaderForge.SFN_Multiply,id:6634,x:31261,y:32614,varname:node_6634,prsc:2|A-9140-OUT,B-9477-OUT;n:type:ShaderForge.SFN_If,id:5334,x:32541,y:32240,varname:node_5334,prsc:2|A-2542-OUT,B-1799-OUT,GT-6069-OUT,EQ-7006-A,LT-6069-OUT;n:type:ShaderForge.SFN_Vector1,id:6069,x:32541,y:32394,varname:node_6069,prsc:2,v1:1;n:type:ShaderForge.SFN_FaceSign,id:3314,x:30579,y:32933,varname:node_3314,prsc:2,fstp:0;n:type:ShaderForge.SFN_If,id:9140,x:30875,y:32884,varname:node_9140,prsc:2|A-3314-VFACE,B-1475-OUT,GT-5980-OUT,EQ-6363-OUT,LT-5980-OUT;n:type:ShaderForge.SFN_Vector1,id:1475,x:30579,y:33091,varname:node_1475,prsc:2,v1:0;n:type:ShaderForge.SFN_Dot,id:6363,x:30862,y:32732,varname:node_6363,prsc:2,dt:4|A-9266-OUT,B-9906-OUT;n:type:ShaderForge.SFN_Negate,id:9906,x:31074,y:32896,varname:node_9906,prsc:2|IN-543-OUT;n:type:ShaderForge.SFN_Multiply,id:8230,x:33177,y:32459,varname:node_8230,prsc:2|A-9022-OUT,B-5334-OUT;n:type:ShaderForge.SFN_LightPosition,id:3750,x:33147,y:33812,varname:node_3750,prsc:2;n:type:ShaderForge.SFN_Distance,id:743,x:33479,y:33923,varname:node_743,prsc:2|A-3750-XYZ,B-9827-XYZ;n:type:ShaderForge.SFN_Divide,id:9829,x:33496,y:33786,varname:node_9829,prsc:2|A-2001-OUT,B-743-OUT;n:type:ShaderForge.SFN_Power,id:4123,x:33518,y:33487,varname:node_4123,prsc:2|VAL-9829-OUT,EXP-560-OUT;n:type:ShaderForge.SFN_Clamp01,id:2613,x:34436,y:34068,varname:node_2613,prsc:2|IN-2355-OUT;n:type:ShaderForge.SFN_Multiply,id:2355,x:34212,y:34068,varname:node_2355,prsc:2|A-4123-OUT,B-1068-OUT;n:type:ShaderForge.SFN_LightColor,id:7480,x:33695,y:33938,varname:node_7480,prsc:2;n:type:ShaderForge.SFN_FragmentPosition,id:9827,x:33165,y:33989,varname:node_9827,prsc:2;n:type:ShaderForge.SFN_Vector1,id:9003,x:33646,y:33755,varname:node_9003,prsc:2,v1:10;n:type:ShaderForge.SFN_Exp,id:560,x:33646,y:33606,varname:node_560,prsc:2,et:0|IN-9003-OUT;n:type:ShaderForge.SFN_If,id:1068,x:33957,y:34174,varname:node_1068,prsc:2|A-3750-PNT,B-6461-OUT,GT-7480-A,EQ-6461-OUT,LT-6461-OUT;n:type:ShaderForge.SFN_Vector1,id:6461,x:33630,y:34326,varname:node_6461,prsc:2,v1:0;n:type:ShaderForge.SFN_Multiply,id:3955,x:34858,y:33659,varname:node_3955,prsc:2|A-7012-OUT,B-2613-OUT;n:type:ShaderForge.SFN_Multiply,id:7012,x:34577,y:33685,varname:node_7012,prsc:2|A-4602-RGB,B-7480-RGB;n:type:ShaderForge.SFN_LightAttenuation,id:1293,x:33163,y:33318,varname:node_1293,prsc:2;n:type:ShaderForge.SFN_Exp,id:4002,x:33166,y:33454,varname:node_4002,prsc:2,et:0|IN-1161-OUT;n:type:ShaderForge.SFN_Vector1,id:1161,x:33166,y:33258,varname:node_1161,prsc:2,v1:8;n:type:ShaderForge.SFN_Multiply,id:2001,x:33180,y:33626,varname:node_2001,prsc:2|A-1293-OUT,B-4002-OUT;n:type:ShaderForge.SFN_Slider,id:9678,x:33256,y:31285,ptovrint:False,ptlb:Fade Distance,ptin:_FadeDistance,varname:_FadeDistance_copy_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:1,max:1;n:type:ShaderForge.SFN_FragmentPosition,id:1839,x:32789,y:31338,varname:node_1839,prsc:2;n:type:ShaderForge.SFN_ViewPosition,id:3794,x:32789,y:31483,varname:node_3794,prsc:2;n:type:ShaderForge.SFN_Distance,id:4793,x:33093,y:31407,varname:node_4793,prsc:2|A-1839-XYZ,B-3794-XYZ;n:type:ShaderForge.SFN_Clamp01,id:9142,x:33752,y:31413,varname:node_9142,prsc:2|IN-2653-OUT;n:type:ShaderForge.SFN_Subtract,id:2653,x:33335,y:31413,varname:node_2653,prsc:2|A-4793-OUT,B-9678-OUT;n:type:ShaderForge.SFN_ViewPosition,id:4821,x:32988,y:32158,varname:node_4821,prsc:2;n:type:ShaderForge.SFN_Distance,id:8370,x:33190,y:32049,varname:node_8370,prsc:2|A-1839-XYZ,B-4821-XYZ;n:type:ShaderForge.SFN_Divide,id:1415,x:33380,y:31905,varname:node_1415,prsc:2|A-1772-OUT,B-8370-OUT;n:type:ShaderForge.SFN_Vector1,id:1772,x:33339,y:31776,varname:node_1772,prsc:2,v1:5;n:type:ShaderForge.SFN_Multiply,id:7324,x:33358,y:31648,varname:node_7324,prsc:2|A-1415-OUT,B-4172-OUT;n:type:ShaderForge.SFN_Multiply,id:9022,x:34155,y:31507,varname:node_9022,prsc:2|A-9142-OUT,B-6654-OUT;n:type:ShaderForge.SFN_Clamp01,id:6654,x:33698,y:31556,varname:node_6654,prsc:2|IN-7324-OUT;n:type:ShaderForge.SFN_Slider,id:4172,x:32872,y:31706,ptovrint:False,ptlb:Far Fade Distance,ptin:_FarFadeDistance,varname:_FadeDistance_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:2,cur:2,max:50;n:type:ShaderForge.SFN_AmbientLight,id:6398,x:32665,y:32476,varname:node_6398,prsc:2;n:type:ShaderForge.SFN_Blend,id:5402,x:33186,y:32802,varname:node_5402,prsc:2,blmd:10,clmp:True|SRC-6398-RGB,DST-9649-OUT;n:type:ShaderForge.SFN_Vector1,id:7131,x:32124,y:32885,varname:node_7131,prsc:2,v1:0.3;n:type:ShaderForge.SFN_Add,id:9185,x:32346,y:32775,varname:node_9185,prsc:2|A-2000-RGB,B-7131-OUT;n:type:ShaderForge.SFN_ToggleProperty,id:276,x:32933,y:32548,ptovrint:False,ptlb:UseFade,ptin:_UseFade,varname:node_276,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:False;n:type:ShaderForge.SFN_If,id:8239,x:33186,y:32653,varname:node_8239,prsc:2|A-276-OUT,B-1971-OUT,GT-5334-OUT,EQ-8230-OUT,LT-5334-OUT;n:type:ShaderForge.SFN_Vector1,id:1971,x:32933,y:32628,varname:node_1971,prsc:2,v1:1;proporder:1304-2542-7006-4602-9111-7240-276-9678-4172;pass:END;sub:END;*/

Shader "Stylized/Foliage" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        [MaterialToggle] _UseAlbedo ("Use Albedo", Float ) = 0
        _Albedo ("Albedo", 2D) = "white" {}
        _LightRamp ("Light Ramp", 2D) = "white" {}
        _WindIntensity ("Wind Intensity", Range(0, 0.5)) = 0.25
        [MaterialToggle] _IsGrass ("Is Grass", Float ) = 1
        [MaterialToggle] _UseFade ("UseFade", Float ) = 0
        _FadeDistance ("Fade Distance", Range(0, 1)) = 1
        _FarFadeDistance ("Far Fade Distance", Range(2, 50)) = 2
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Cull Off
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal d3d11_9x xboxone ps4 psp2 n3ds wiiu 
            #pragma target 3.0
            // Dithering function, to use with scene UVs (screen pixel coords)
            // 2x2 Bayer matrix, based on https://en.wikipedia.org/wiki/Ordered_dithering
            float BinaryDither2x2( float value, float2 sceneUVs ) {
                float2x2 mtx = float2x2(
                    float2( 1, 3 )/5.0,
                    float2( 4, 2 )/5.0
                );
                float2 px = floor(_ScreenParams.xy * sceneUVs);
                int xSmp = fmod(px.x,2);
                int ySmp = fmod(px.y,2);
                float2 xVec = 1-saturate(abs(float2(0,1) - xSmp));
                float2 yVec = 1-saturate(abs(float2(0,1) - ySmp));
                float2 pxMult = float2( dot(mtx[0],yVec), dot(mtx[1],yVec) );
                return round(value + dot(pxMult, xVec));
            }
            uniform float4 _Color;
            uniform sampler2D _LightRamp; uniform float4 _LightRamp_ST;
            uniform fixed _UseAlbedo;
            uniform sampler2D _Albedo; uniform float4 _Albedo_ST;
            uniform float _WindIntensity;
            uniform fixed _IsGrass;
            uniform float _FadeDistance;
            uniform float _FarFadeDistance;
            uniform fixed _UseFade;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float4 vertexColor : COLOR;
                float4 projPos : TEXCOORD3;
                LIGHTING_COORDS(4,5)
                UNITY_FOG_COORDS(6)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                float node_7220 = 1.0;
                float node_4953_if_leA = step(node_7220,_IsGrass);
                float node_4953_if_leB = step(_IsGrass,node_7220);
                float4 node_7596 = _Time;
                float node_8284 = (sin(mul(unity_ObjectToWorld, v.vertex).r)*(_WindIntensity*(o.uv0.g*cos((node_7596.r*15.0)))));
                v.vertex.xyz += (o.vertexColor.r*lerp((node_4953_if_leA*node_8284)+(node_4953_if_leB*node_8284),float3(node_8284,0.0,node_8284),node_4953_if_leA*node_4953_if_leB));
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos( v.vertex );
                UNITY_TRANSFER_FOG(o,o.pos);
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalDirection = i.normalDir;
                float2 sceneUVs = (i.projPos.xy / i.projPos.w);
                float node_8239_if_leA = step(_UseFade,1.0);
                float node_8239_if_leB = step(1.0,_UseFade);
                float node_1799 = 1.0;
                float node_5334_if_leA = step(_UseAlbedo,node_1799);
                float node_5334_if_leB = step(node_1799,_UseAlbedo);
                float node_6069 = 1.0;
                float4 _Albedo_var = tex2D(_Albedo,TRANSFORM_TEX(i.uv0, _Albedo));
                float node_5334 = lerp((node_5334_if_leA*node_6069)+(node_5334_if_leB*node_6069),_Albedo_var.a,node_5334_if_leA*node_5334_if_leB);
                float node_2653 = (distance(i.posWorld.rgb,_WorldSpaceCameraPos)-_FadeDistance);
                float node_7324 = ((5.0/distance(i.posWorld.rgb,_WorldSpaceCameraPos))*_FarFadeDistance);
                clip( BinaryDither2x2(lerp((node_8239_if_leA*node_5334)+(node_8239_if_leB*node_5334),((saturate(node_2653)*saturate(node_7324))*node_5334),node_8239_if_leA*node_8239_if_leB) - 1.5, sceneUVs) );
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightColor = _LightColor0.rgb;
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
////// Emissive:
                float node_5860_if_leA = step(_UseAlbedo,node_1799);
                float node_5860_if_leB = step(node_1799,_UseAlbedo);
                float node_7220 = 1.0;
                float node_9354_if_leA = step(node_7220,_IsGrass);
                float node_9354_if_leB = step(_IsGrass,node_7220);
                float node_9140_if_leA = step(isFrontFace,0.0);
                float node_9140_if_leB = step(0.0,isFrontFace);
                float node_5980 = 0.5*dot(lightDirection,i.normalDir)+0.5;
                float node_6634 = (lerp((node_9140_if_leA*node_5980)+(node_9140_if_leB*node_5980),0.5*dot(lightDirection,(-1*i.normalDir))+0.5,node_9140_if_leA*node_9140_if_leB)*attenuation);
                float node_9354 = lerp((node_9354_if_leA*node_6634)+(node_9354_if_leB*node_6634),attenuation,node_9354_if_leA*node_9354_if_leB);
                float2 node_6981 = float2(node_9354,node_9354);
                float4 _LightRamp_var = tex2D(_LightRamp,TRANSFORM_TEX(node_6981, _LightRamp));
                float3 emissive = saturate(( (saturate((lerp((node_5860_if_leA*_Color.rgb)+(node_5860_if_leB*_Color.rgb),(_Color.rgb*_Albedo_var.rgb),node_5860_if_leA*node_5860_if_leB) > 0.5 ?  (1.0-(1.0-2.0*(lerp((node_5860_if_leA*_Color.rgb)+(node_5860_if_leB*_Color.rgb),(_Color.rgb*_Albedo_var.rgb),node_5860_if_leA*node_5860_if_leB)-0.5))*(1.0-_LightRamp_var.rgb)) : (2.0*lerp((node_5860_if_leA*_Color.rgb)+(node_5860_if_leB*_Color.rgb),(_Color.rgb*_Albedo_var.rgb),node_5860_if_leA*node_5860_if_leB)*_LightRamp_var.rgb)) )*(_LightColor0.rgb+0.3)) > 0.5 ? (1.0-(1.0-2.0*((saturate((lerp((node_5860_if_leA*_Color.rgb)+(node_5860_if_leB*_Color.rgb),(_Color.rgb*_Albedo_var.rgb),node_5860_if_leA*node_5860_if_leB) > 0.5 ?  (1.0-(1.0-2.0*(lerp((node_5860_if_leA*_Color.rgb)+(node_5860_if_leB*_Color.rgb),(_Color.rgb*_Albedo_var.rgb),node_5860_if_leA*node_5860_if_leB)-0.5))*(1.0-_LightRamp_var.rgb)) : (2.0*lerp((node_5860_if_leA*_Color.rgb)+(node_5860_if_leB*_Color.rgb),(_Color.rgb*_Albedo_var.rgb),node_5860_if_leA*node_5860_if_leB)*_LightRamp_var.rgb)) )*(_LightColor0.rgb+0.3))-0.5))*(1.0-UNITY_LIGHTMODEL_AMBIENT.rgb)) : (2.0*(saturate((lerp((node_5860_if_leA*_Color.rgb)+(node_5860_if_leB*_Color.rgb),(_Color.rgb*_Albedo_var.rgb),node_5860_if_leA*node_5860_if_leB) > 0.5 ?  (1.0-(1.0-2.0*(lerp((node_5860_if_leA*_Color.rgb)+(node_5860_if_leB*_Color.rgb),(_Color.rgb*_Albedo_var.rgb),node_5860_if_leA*node_5860_if_leB)-0.5))*(1.0-_LightRamp_var.rgb)) : (2.0*lerp((node_5860_if_leA*_Color.rgb)+(node_5860_if_leB*_Color.rgb),(_Color.rgb*_Albedo_var.rgb),node_5860_if_leA*node_5860_if_leB)*_LightRamp_var.rgb)) )*(_LightColor0.rgb+0.3))*UNITY_LIGHTMODEL_AMBIENT.rgb) ));
                float node_6461 = 0.0;
                float node_1068_if_leA = step(_WorldSpaceLightPos0.a,node_6461);
                float node_1068_if_leB = step(node_6461,_WorldSpaceLightPos0.a);
                float3 finalColor = emissive + ((_LightRamp_var.rgb*_LightColor0.rgb)*saturate((pow(((attenuation*exp(8.0))/distance(_WorldSpaceLightPos0.rgb,i.posWorld.rgb)),exp(10.0))*lerp((node_1068_if_leA*node_6461)+(node_1068_if_leB*_LightColor0.a),node_6461,node_1068_if_leA*node_1068_if_leB))));
                fixed4 finalRGBA = fixed4(finalColor,1);
                UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
                return finalRGBA;
            }
            ENDCG
        }
        Pass {
            Name "FORWARD_DELTA"
            Tags {
                "LightMode"="ForwardAdd"
            }
            Blend One One
            Cull Off
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDADD
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal d3d11_9x xboxone ps4 psp2 n3ds wiiu 
            #pragma target 3.0
            // Dithering function, to use with scene UVs (screen pixel coords)
            // 2x2 Bayer matrix, based on https://en.wikipedia.org/wiki/Ordered_dithering
            float BinaryDither2x2( float value, float2 sceneUVs ) {
                float2x2 mtx = float2x2(
                    float2( 1, 3 )/5.0,
                    float2( 4, 2 )/5.0
                );
                float2 px = floor(_ScreenParams.xy * sceneUVs);
                int xSmp = fmod(px.x,2);
                int ySmp = fmod(px.y,2);
                float2 xVec = 1-saturate(abs(float2(0,1) - xSmp));
                float2 yVec = 1-saturate(abs(float2(0,1) - ySmp));
                float2 pxMult = float2( dot(mtx[0],yVec), dot(mtx[1],yVec) );
                return round(value + dot(pxMult, xVec));
            }
            uniform float4 _Color;
            uniform sampler2D _LightRamp; uniform float4 _LightRamp_ST;
            uniform fixed _UseAlbedo;
            uniform sampler2D _Albedo; uniform float4 _Albedo_ST;
            uniform float _WindIntensity;
            uniform fixed _IsGrass;
            uniform float _FadeDistance;
            uniform float _FarFadeDistance;
            uniform fixed _UseFade;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float4 vertexColor : COLOR;
                float4 projPos : TEXCOORD3;
                LIGHTING_COORDS(4,5)
                UNITY_FOG_COORDS(6)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                float node_7220 = 1.0;
                float node_4953_if_leA = step(node_7220,_IsGrass);
                float node_4953_if_leB = step(_IsGrass,node_7220);
                float4 node_7596 = _Time;
                float node_8284 = (sin(mul(unity_ObjectToWorld, v.vertex).r)*(_WindIntensity*(o.uv0.g*cos((node_7596.r*15.0)))));
                v.vertex.xyz += (o.vertexColor.r*lerp((node_4953_if_leA*node_8284)+(node_4953_if_leB*node_8284),float3(node_8284,0.0,node_8284),node_4953_if_leA*node_4953_if_leB));
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos( v.vertex );
                UNITY_TRANSFER_FOG(o,o.pos);
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalDirection = i.normalDir;
                float2 sceneUVs = (i.projPos.xy / i.projPos.w);
                float node_8239_if_leA = step(_UseFade,1.0);
                float node_8239_if_leB = step(1.0,_UseFade);
                float node_1799 = 1.0;
                float node_5334_if_leA = step(_UseAlbedo,node_1799);
                float node_5334_if_leB = step(node_1799,_UseAlbedo);
                float node_6069 = 1.0;
                float4 _Albedo_var = tex2D(_Albedo,TRANSFORM_TEX(i.uv0, _Albedo));
                float node_5334 = lerp((node_5334_if_leA*node_6069)+(node_5334_if_leB*node_6069),_Albedo_var.a,node_5334_if_leA*node_5334_if_leB);
                float node_2653 = (distance(i.posWorld.rgb,_WorldSpaceCameraPos)-_FadeDistance);
                float node_7324 = ((5.0/distance(i.posWorld.rgb,_WorldSpaceCameraPos))*_FarFadeDistance);
                clip( BinaryDither2x2(lerp((node_8239_if_leA*node_5334)+(node_8239_if_leB*node_5334),((saturate(node_2653)*saturate(node_7324))*node_5334),node_8239_if_leA*node_8239_if_leB) - 1.5, sceneUVs) );
                float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.posWorld.xyz,_WorldSpaceLightPos0.w));
                float3 lightColor = _LightColor0.rgb;
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float node_7220 = 1.0;
                float node_9354_if_leA = step(node_7220,_IsGrass);
                float node_9354_if_leB = step(_IsGrass,node_7220);
                float node_9140_if_leA = step(isFrontFace,0.0);
                float node_9140_if_leB = step(0.0,isFrontFace);
                float node_5980 = 0.5*dot(lightDirection,i.normalDir)+0.5;
                float node_6634 = (lerp((node_9140_if_leA*node_5980)+(node_9140_if_leB*node_5980),0.5*dot(lightDirection,(-1*i.normalDir))+0.5,node_9140_if_leA*node_9140_if_leB)*attenuation);
                float node_9354 = lerp((node_9354_if_leA*node_6634)+(node_9354_if_leB*node_6634),attenuation,node_9354_if_leA*node_9354_if_leB);
                float2 node_6981 = float2(node_9354,node_9354);
                float4 _LightRamp_var = tex2D(_LightRamp,TRANSFORM_TEX(node_6981, _LightRamp));
                float node_6461 = 0.0;
                float node_1068_if_leA = step(_WorldSpaceLightPos0.a,node_6461);
                float node_1068_if_leB = step(node_6461,_WorldSpaceLightPos0.a);
                float3 finalColor = ((_LightRamp_var.rgb*_LightColor0.rgb)*saturate((pow(((attenuation*exp(8.0))/distance(_WorldSpaceLightPos0.rgb,i.posWorld.rgb)),exp(10.0))*lerp((node_1068_if_leA*node_6461)+(node_1068_if_leB*_LightColor0.a),node_6461,node_1068_if_leA*node_1068_if_leB))));
                fixed4 finalRGBA = fixed4(finalColor * 1,0);
                UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
                return finalRGBA;
            }
            ENDCG
        }
        Pass {
            Name "ShadowCaster"
            Tags {
                "LightMode"="ShadowCaster"
            }
            Offset 1, 1
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_SHADOWCASTER
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal d3d11_9x xboxone ps4 psp2 n3ds wiiu 
            #pragma target 3.0
            // Dithering function, to use with scene UVs (screen pixel coords)
            // 2x2 Bayer matrix, based on https://en.wikipedia.org/wiki/Ordered_dithering
            float BinaryDither2x2( float value, float2 sceneUVs ) {
                float2x2 mtx = float2x2(
                    float2( 1, 3 )/5.0,
                    float2( 4, 2 )/5.0
                );
                float2 px = floor(_ScreenParams.xy * sceneUVs);
                int xSmp = fmod(px.x,2);
                int ySmp = fmod(px.y,2);
                float2 xVec = 1-saturate(abs(float2(0,1) - xSmp));
                float2 yVec = 1-saturate(abs(float2(0,1) - ySmp));
                float2 pxMult = float2( dot(mtx[0],yVec), dot(mtx[1],yVec) );
                return round(value + dot(pxMult, xVec));
            }
            uniform fixed _UseAlbedo;
            uniform sampler2D _Albedo; uniform float4 _Albedo_ST;
            uniform float _WindIntensity;
            uniform fixed _IsGrass;
            uniform float _FadeDistance;
            uniform float _FarFadeDistance;
            uniform fixed _UseFade;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                V2F_SHADOW_CASTER;
                float2 uv0 : TEXCOORD1;
                float4 posWorld : TEXCOORD2;
                float4 vertexColor : COLOR;
                float4 projPos : TEXCOORD3;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                float node_7220 = 1.0;
                float node_4953_if_leA = step(node_7220,_IsGrass);
                float node_4953_if_leB = step(_IsGrass,node_7220);
                float4 node_7596 = _Time;
                float node_8284 = (sin(mul(unity_ObjectToWorld, v.vertex).r)*(_WindIntensity*(o.uv0.g*cos((node_7596.r*15.0)))));
                v.vertex.xyz += (o.vertexColor.r*lerp((node_4953_if_leA*node_8284)+(node_4953_if_leB*node_8284),float3(node_8284,0.0,node_8284),node_4953_if_leA*node_4953_if_leB));
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos( v.vertex );
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }
            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                float2 sceneUVs = (i.projPos.xy / i.projPos.w);
                float node_8239_if_leA = step(_UseFade,1.0);
                float node_8239_if_leB = step(1.0,_UseFade);
                float node_1799 = 1.0;
                float node_5334_if_leA = step(_UseAlbedo,node_1799);
                float node_5334_if_leB = step(node_1799,_UseAlbedo);
                float node_6069 = 1.0;
                float4 _Albedo_var = tex2D(_Albedo,TRANSFORM_TEX(i.uv0, _Albedo));
                float node_5334 = lerp((node_5334_if_leA*node_6069)+(node_5334_if_leB*node_6069),_Albedo_var.a,node_5334_if_leA*node_5334_if_leB);
                float node_2653 = (distance(i.posWorld.rgb,_WorldSpaceCameraPos)-_FadeDistance);
                float node_7324 = ((5.0/distance(i.posWorld.rgb,_WorldSpaceCameraPos))*_FarFadeDistance);
                clip( BinaryDither2x2(lerp((node_8239_if_leA*node_5334)+(node_8239_if_leB*node_5334),((saturate(node_2653)*saturate(node_7324))*node_5334),node_8239_if_leA*node_8239_if_leB) - 1.5, sceneUVs) );
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Standard"
    CustomEditor "AdvancedCelShader"
}
