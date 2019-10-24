// Shader created with Shader Forge v1.38 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.38;sub:START;pass:START;ps:flbk:Standard,iptp:0,cusa:False,bamd:0,cgin:,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:True,hqlp:False,rprd:True,enco:False,rmgx:True,imps:False,rpth:0,vtps:0,hqsc:False,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:0,bdst:1,dpts:2,wrdp:True,dith:1,atcv:False,rfrpo:False,rfrpn:Refraction,coma:15,ufog:True,aust:True,igpj:False,qofs:0,qpre:1,rntp:1,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,atwp:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False,fsmp:False;n:type:ShaderForge.SFN_Final,id:2865,x:35054,y:32930,varname:node_2865,prsc:2|normal-1699-RGB,emission-236-OUT,custl-7289-OUT,olwid-3467-OUT,olcol-2753-RGB;n:type:ShaderForge.SFN_NormalVector,id:6703,x:30805,y:31419,prsc:2,pt:False;n:type:ShaderForge.SFN_Dot,id:5764,x:31131,y:31370,varname:node_5764,prsc:2,dt:4|A-3161-OUT,B-6703-OUT;n:type:ShaderForge.SFN_LightVector,id:3161,x:30805,y:31279,varname:node_3161,prsc:2;n:type:ShaderForge.SFN_Tex2d,id:8756,x:31362,y:31083,varname:node_8756,prsc:2,tex:7ea4fa8f6637d234cabf1448f60f5b81,ntxv:1,isnm:False|UVIN-9693-OUT,TEX-9522-TEX;n:type:ShaderForge.SFN_Append,id:9693,x:31574,y:31349,varname:node_9693,prsc:2|A-4750-OUT,B-4750-OUT;n:type:ShaderForge.SFN_Multiply,id:4750,x:31711,y:31879,varname:node_4750,prsc:2|A-1463-OUT,B-8746-OUT;n:type:ShaderForge.SFN_Blend,id:7458,x:32133,y:31130,varname:node_7458,prsc:2,blmd:12,clmp:False|SRC-8263-OUT,DST-6637-OUT;n:type:ShaderForge.SFN_Tex2d,id:1699,x:30111,y:31980,ptovrint:False,ptlb:Normal,ptin:_Normal,varname:node_1699,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:True,ntxv:3,isnm:True;n:type:ShaderForge.SFN_Dot,id:1463,x:31171,y:31585,varname:node_1463,prsc:2,dt:4|A-4518-OUT,B-1699-RGB;n:type:ShaderForge.SFN_ViewReflectionVector,id:3643,x:30983,y:32121,varname:node_3643,prsc:2;n:type:ShaderForge.SFN_Dot,id:8302,x:31263,y:31942,varname:node_8302,prsc:2,dt:0|A-1463-OUT,B-9414-OUT;n:type:ShaderForge.SFN_Append,id:195,x:32039,y:32177,varname:node_195,prsc:2|A-390-OUT,B-390-OUT;n:type:ShaderForge.SFN_Tex2d,id:5713,x:32234,y:32082,varname:node_5713,prsc:2,tex:94a5cd6cfabc7e843abe56079809ea1b,ntxv:0,isnm:False|UVIN-195-OUT,TEX-4047-TEX;n:type:ShaderForge.SFN_Add,id:6692,x:33056,y:31520,varname:node_6692,prsc:2|A-7458-OUT,B-682-OUT;n:type:ShaderForge.SFN_Color,id:8131,x:32043,y:32404,ptovrint:False,ptlb:Specular Color,ptin:_SpecularColor,varname:node_8131,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0.5,c2:0.5,c3:0.5,c4:0;n:type:ShaderForge.SFN_Multiply,id:682,x:32785,y:32015,varname:node_682,prsc:2|A-5713-RGB,B-2321-OUT;n:type:ShaderForge.SFN_Clamp,id:2321,x:32779,y:32275,varname:node_2321,prsc:2|IN-8754-OUT,MIN-5106-OUT,MAX-376-OUT;n:type:ShaderForge.SFN_Vector1,id:5106,x:32795,y:32425,varname:node_5106,prsc:2,v1:0;n:type:ShaderForge.SFN_NormalVector,id:8590,x:30996,y:32253,prsc:2,pt:False;n:type:ShaderForge.SFN_Dot,id:9414,x:31242,y:32165,varname:node_9414,prsc:2,dt:0|A-3643-OUT,B-8590-OUT;n:type:ShaderForge.SFN_Tex2dAsset,id:4047,x:30105,y:31574,ptovrint:False,ptlb:Specular Ramp,ptin:_SpecularRamp,varname:node_4047,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:True,tagnrm:False,tex:94a5cd6cfabc7e843abe56079809ea1b,ntxv:1,isnm:False;n:type:ShaderForge.SFN_Tex2d,id:5491,x:32043,y:32628,ptovrint:False,ptlb:GlossSpec,ptin:_GlossSpec,varname:node_5491,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:2,isnm:False;n:type:ShaderForge.SFN_ConstantClamp,id:9976,x:32129,y:32805,varname:node_9976,prsc:2,min:0,max:0.2|IN-5491-A;n:type:ShaderForge.SFN_ToggleProperty,id:2051,x:32447,y:32254,ptovrint:False,ptlb:Use GlossSpec,ptin:_UseGlossSpec,varname:node_2051,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:False;n:type:ShaderForge.SFN_If,id:8754,x:32458,y:32320,varname:node_8754,prsc:2|A-2051-OUT,B-5415-OUT,GT-8131-RGB,EQ-5491-RGB,LT-8131-RGB;n:type:ShaderForge.SFN_Vector1,id:5415,x:32447,y:32161,varname:node_5415,prsc:2,v1:1;n:type:ShaderForge.SFN_If,id:376,x:32733,y:32497,varname:node_376,prsc:2|A-2051-OUT,B-5415-OUT,GT-8131-A,EQ-9976-OUT,LT-8131-A;n:type:ShaderForge.SFN_Multiply,id:390,x:31577,y:32138,varname:node_390,prsc:2|A-8302-OUT,B-8746-OUT;n:type:ShaderForge.SFN_Tex2dAsset,id:9522,x:30095,y:32452,ptovrint:False,ptlb:Light Ramp,ptin:_LightRamp,varname:node_9522,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:7ea4fa8f6637d234cabf1448f60f5b81,ntxv:1,isnm:False;n:type:ShaderForge.SFN_LightPosition,id:4423,x:32147,y:34146,varname:node_4423,prsc:2;n:type:ShaderForge.SFN_Distance,id:5489,x:32479,y:34257,varname:node_5489,prsc:2|A-4423-XYZ,B-6492-XYZ;n:type:ShaderForge.SFN_Divide,id:3449,x:32496,y:34120,varname:node_3449,prsc:2|A-4214-OUT,B-5489-OUT;n:type:ShaderForge.SFN_Power,id:464,x:32518,y:33821,varname:node_464,prsc:2|VAL-3449-OUT,EXP-5919-OUT;n:type:ShaderForge.SFN_Clamp01,id:2458,x:33436,y:34402,varname:node_2458,prsc:2|IN-2754-OUT;n:type:ShaderForge.SFN_Multiply,id:2754,x:33212,y:34402,varname:node_2754,prsc:2|A-464-OUT,B-3727-OUT;n:type:ShaderForge.SFN_LightColor,id:9832,x:32650,y:34354,varname:node_9832,prsc:2;n:type:ShaderForge.SFN_FragmentPosition,id:6492,x:32130,y:34321,varname:node_6492,prsc:2;n:type:ShaderForge.SFN_Vector1,id:7755,x:32646,y:34089,varname:node_7755,prsc:2,v1:10;n:type:ShaderForge.SFN_Exp,id:5919,x:32646,y:33940,varname:node_5919,prsc:2,et:0|IN-7755-OUT;n:type:ShaderForge.SFN_If,id:3727,x:32957,y:34508,varname:node_3727,prsc:2|A-4423-PNT,B-3539-OUT,GT-9832-A,EQ-3539-OUT,LT-3539-OUT;n:type:ShaderForge.SFN_If,id:4518,x:30850,y:31011,varname:node_4518,prsc:2|A-7336-OUT,B-160-OUT,GT-5764-OUT,EQ-5764-OUT,LT-4410-OUT;n:type:ShaderForge.SFN_Length,id:7336,x:30554,y:31149,varname:node_7336,prsc:2|IN-3161-OUT;n:type:ShaderForge.SFN_Vector1,id:160,x:30510,y:30992,varname:node_160,prsc:2,v1:1;n:type:ShaderForge.SFN_Vector1,id:4410,x:30522,y:31081,varname:node_4410,prsc:2,v1:0;n:type:ShaderForge.SFN_LightAttenuation,id:1060,x:31054,y:31751,varname:node_1060,prsc:2;n:type:ShaderForge.SFN_Multiply,id:6637,x:31784,y:31097,varname:node_6637,prsc:2|A-8756-RGB,B-9489-OUT;n:type:ShaderForge.SFN_LightColor,id:7978,x:31862,y:31271,varname:node_7978,prsc:2;n:type:ShaderForge.SFN_Vector1,id:3539,x:32630,y:34660,varname:node_3539,prsc:2,v1:0;n:type:ShaderForge.SFN_Multiply,id:7289,x:33858,y:33993,varname:node_7289,prsc:2|A-4564-OUT,B-2458-OUT;n:type:ShaderForge.SFN_LightVector,id:7741,x:33022,y:33585,varname:node_7741,prsc:2;n:type:ShaderForge.SFN_Dot,id:1128,x:33238,y:33632,varname:node_1128,prsc:2,dt:1|A-7741-OUT,B-5703-OUT;n:type:ShaderForge.SFN_NormalVector,id:5703,x:33031,y:33748,prsc:2,pt:False;n:type:ShaderForge.SFN_Multiply,id:4564,x:33577,y:34019,varname:node_4564,prsc:2|A-1195-RGB,B-9832-RGB;n:type:ShaderForge.SFN_Append,id:5492,x:33611,y:33571,varname:node_5492,prsc:2|A-1128-OUT,B-1128-OUT;n:type:ShaderForge.SFN_Tex2d,id:1195,x:33521,y:33725,varname:node_1195,prsc:2,tex:7ea4fa8f6637d234cabf1448f60f5b81,ntxv:0,isnm:False|UVIN-5492-OUT,TEX-9522-TEX;n:type:ShaderForge.SFN_LightAttenuation,id:7706,x:31845,y:33847,varname:node_7706,prsc:2;n:type:ShaderForge.SFN_Multiply,id:4214,x:32222,y:33982,varname:node_4214,prsc:2|A-7706-OUT,B-7100-OUT;n:type:ShaderForge.SFN_Vector1,id:5417,x:31840,y:34003,varname:node_5417,prsc:2,v1:8;n:type:ShaderForge.SFN_Exp,id:7100,x:32222,y:33812,varname:node_7100,prsc:2,et:0|IN-5417-OUT;n:type:ShaderForge.SFN_Tex2dAsset,id:5700,x:30089,y:30124,ptovrint:False,ptlb:Main Texture,ptin:_MainTexture,varname:node_1017,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:68d6e826d13d4d144a70bf55bb49fec6,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Tex2d,id:4387,x:30480,y:29396,varname:node_7614,prsc:2,tex:68d6e826d13d4d144a70bf55bb49fec6,ntxv:0,isnm:False|UVIN-3133-OUT,TEX-5700-TEX;n:type:ShaderForge.SFN_Tex2d,id:6859,x:30480,y:29575,varname:node_5979,prsc:2,tex:68d6e826d13d4d144a70bf55bb49fec6,ntxv:0,isnm:False|UVIN-6019-OUT,TEX-5700-TEX;n:type:ShaderForge.SFN_Tex2d,id:52,x:30480,y:29760,varname:node_8327,prsc:2,tex:68d6e826d13d4d144a70bf55bb49fec6,ntxv:0,isnm:False|UVIN-4659-OUT,TEX-5700-TEX;n:type:ShaderForge.SFN_NormalVector,id:9248,x:29849,y:29211,prsc:2,pt:False;n:type:ShaderForge.SFN_Abs,id:5328,x:30055,y:29211,varname:node_5328,prsc:2|IN-9248-OUT;n:type:ShaderForge.SFN_Multiply,id:8294,x:30272,y:29211,varname:node_8294,prsc:2|A-5328-OUT,B-5328-OUT;n:type:ShaderForge.SFN_ChannelBlend,id:4079,x:30850,y:29244,varname:node_4079,prsc:2,chbt:0|M-8294-OUT,R-4387-RGB,G-6859-RGB,B-52-RGB;n:type:ShaderForge.SFN_Append,id:3133,x:30192,y:29446,varname:node_3133,prsc:2|A-8857-Z,B-8857-Y;n:type:ShaderForge.SFN_Append,id:6019,x:30192,y:29625,varname:node_6019,prsc:2|A-8857-Z,B-8857-X;n:type:ShaderForge.SFN_Append,id:4659,x:30192,y:29810,varname:node_4659,prsc:2|A-8857-X,B-8857-Y;n:type:ShaderForge.SFN_FragmentPosition,id:8857,x:29829,y:29562,varname:node_8857,prsc:2;n:type:ShaderForge.SFN_NormalVector,id:7522,x:31087,y:30444,prsc:2,pt:False;n:type:ShaderForge.SFN_Tex2dAsset,id:5850,x:30089,y:30349,ptovrint:False,ptlb:Top Texture,ptin:_TopTexture,varname:node_1907,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:a428f85ca436ee8419b58fe2c6ba8365,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Slider,id:767,x:31234,y:30327,ptovrint:False,ptlb:Top Spread,ptin:_TopSpread,varname:node_9918,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0.1,cur:0.5,max:1;n:type:ShaderForge.SFN_Tex2d,id:6901,x:30480,y:29959,varname:node_2528,prsc:2,tex:a428f85ca436ee8419b58fe2c6ba8365,ntxv:0,isnm:False|UVIN-3133-OUT,TEX-5850-TEX;n:type:ShaderForge.SFN_Tex2d,id:4933,x:30480,y:30153,varname:node_436,prsc:2,tex:a428f85ca436ee8419b58fe2c6ba8365,ntxv:0,isnm:False|UVIN-6019-OUT,TEX-5850-TEX;n:type:ShaderForge.SFN_Tex2d,id:2008,x:30480,y:30366,varname:node_4080,prsc:2,tex:a428f85ca436ee8419b58fe2c6ba8365,ntxv:0,isnm:False|UVIN-4659-OUT,TEX-5850-TEX;n:type:ShaderForge.SFN_ChannelBlend,id:3077,x:30887,y:29759,varname:node_3077,prsc:2,chbt:0|M-8294-OUT,R-6901-RGB,G-4933-RGB,B-2008-RGB;n:type:ShaderForge.SFN_Multiply,id:2858,x:31888,y:30347,varname:node_2858,prsc:2|A-767-OUT,B-123-OUT;n:type:ShaderForge.SFN_Lerp,id:8263,x:31425,y:29688,varname:node_8263,prsc:2|A-4079-OUT,B-3077-OUT,T-557-RGB;n:type:ShaderForge.SFN_ComponentMask,id:123,x:31449,y:30446,varname:node_123,prsc:2,cc1:1,cc2:-1,cc3:-1,cc4:-1|IN-7522-OUT;n:type:ShaderForge.SFN_ConstantClamp,id:4831,x:30913,y:29953,varname:node_4831,prsc:2,min:0,max:1|IN-2858-OUT;n:type:ShaderForge.SFN_Normalize,id:8746,x:31347,y:31794,varname:node_8746,prsc:2|IN-1060-OUT;n:type:ShaderForge.SFN_Tex2d,id:557,x:31659,y:29884,varname:node_557,prsc:2,tex:94a5cd6cfabc7e843abe56079809ea1b,ntxv:0,isnm:False|UVIN-7319-OUT,TEX-4047-TEX;n:type:ShaderForge.SFN_Append,id:6081,x:31253,y:29895,varname:node_6081,prsc:2|A-4831-OUT,B-4831-OUT;n:type:ShaderForge.SFN_Tex2d,id:5612,x:30829,y:30249,ptovrint:False,ptlb:Noise,ptin:_Noise,varname:node_5612,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-122-OUT;n:type:ShaderForge.SFN_Multiply,id:2201,x:31213,y:30123,varname:node_2201,prsc:2|A-8142-OUT,B-5612-R;n:type:ShaderForge.SFN_Vector1,id:8142,x:30964,y:30123,varname:node_8142,prsc:2,v1:3;n:type:ShaderForge.SFN_Multiply,id:5393,x:31671,y:30133,varname:node_5393,prsc:2|A-2201-OUT,B-6081-OUT;n:type:ShaderForge.SFN_FragmentPosition,id:4420,x:30737,y:30570,varname:node_4420,prsc:2;n:type:ShaderForge.SFN_Append,id:122,x:30996,y:30706,varname:node_122,prsc:2|A-4420-X,B-4420-Z;n:type:ShaderForge.SFN_Add,id:7319,x:31942,y:30123,varname:node_7319,prsc:2|A-5393-OUT,B-123-OUT;n:type:ShaderForge.SFN_If,id:3467,x:35032,y:33530,varname:node_3467,prsc:2|A-6154-OUT,B-5719-OUT,GT-5434-OUT,EQ-2145-OUT,LT-5434-OUT;n:type:ShaderForge.SFN_ToggleProperty,id:6154,x:34765,y:33494,ptovrint:False,ptlb:UseOutline,ptin:_UseOutline,varname:node_1500,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:False;n:type:ShaderForge.SFN_Vector1,id:5719,x:34765,y:33547,varname:node_5719,prsc:2,v1:1;n:type:ShaderForge.SFN_Slider,id:2145,x:34561,y:33644,ptovrint:False,ptlb:OutlineThickness,ptin:_OutlineThickness,varname:node_4598,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0.005,max:0.01;n:type:ShaderForge.SFN_Vector1,id:5434,x:35013,y:33697,varname:node_5434,prsc:2,v1:0;n:type:ShaderForge.SFN_Color,id:2753,x:34713,y:33300,ptovrint:False,ptlb:OutlineColor,ptin:_OutlineColor,varname:node_8873,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0,c2:0,c3:0,c4:1;n:type:ShaderForge.SFN_AmbientLight,id:3258,x:34353,y:32631,varname:node_3258,prsc:2;n:type:ShaderForge.SFN_Blend,id:236,x:34600,y:32430,varname:node_236,prsc:2,blmd:10,clmp:True|SRC-6692-OUT,DST-3258-RGB;n:type:ShaderForge.SFN_Vector1,id:6293,x:31864,y:31412,varname:node_6293,prsc:2,v1:0.3;n:type:ShaderForge.SFN_Add,id:9489,x:32067,y:31324,varname:node_9489,prsc:2|A-7978-RGB,B-6293-OUT;proporder:1699-2051-8131-5491-9522-4047-5700-5850-767-5612-6154-2145-2753;pass:END;sub:END;*/

Shader "Stylized/TerrainCel" {
    Properties {
        [Normal]_Normal ("Normal", 2D) = "bump" {}
        [MaterialToggle] _UseGlossSpec ("Use GlossSpec", Float ) = 0
        _SpecularColor ("Specular Color", Color) = (0.5,0.5,0.5,0)
        _GlossSpec ("GlossSpec", 2D) = "black" {}
        _LightRamp ("Light Ramp", 2D) = "gray" {}
        [NoScaleOffset]_SpecularRamp ("Specular Ramp", 2D) = "gray" {}
        _MainTexture ("Main Texture", 2D) = "white" {}
        _TopTexture ("Top Texture", 2D) = "white" {}
        _TopSpread ("Top Spread", Range(0.1, 1)) = 0.5
        _Noise ("Noise", 2D) = "white" {}
        [MaterialToggle] _UseOutline ("UseOutline", Float ) = 0
        _OutlineThickness ("OutlineThickness", Range(0, 0.01)) = 0.005
        _OutlineColor ("OutlineColor", Color) = (0,0,0,1)
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
        Pass {
            Name "Outline"
            Tags {
            }
            Cull Front
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define _GLOSSYENV 1
            #include "UnityCG.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal d3d11_9x xboxone ps4 psp2 n3ds wiiu 
            #pragma target 3.0
            uniform fixed _UseOutline;
            uniform float _OutlineThickness;
            uniform float4 _OutlineColor;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                UNITY_FOG_COORDS(0)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                float node_3467_if_leA = step(_UseOutline,1.0);
                float node_3467_if_leB = step(1.0,_UseOutline);
                float node_5434 = 0.0;
                o.pos = UnityObjectToClipPos( float4(v.vertex.xyz + v.normal*lerp((node_3467_if_leA*node_5434)+(node_3467_if_leB*node_5434),_OutlineThickness,node_3467_if_leA*node_3467_if_leB),1) );
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                return fixed4(_OutlineColor.rgb,0);
            }
            ENDCG
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #define _GLOSSYENV 1
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal d3d11_9x xboxone ps4 psp2 n3ds wiiu 
            #pragma target 3.0
            uniform sampler2D _Normal; uniform float4 _Normal_ST;
            uniform float4 _SpecularColor;
            uniform sampler2D _SpecularRamp;
            uniform sampler2D _GlossSpec; uniform float4 _GlossSpec_ST;
            uniform fixed _UseGlossSpec;
            uniform sampler2D _LightRamp; uniform float4 _LightRamp_ST;
            uniform sampler2D _MainTexture; uniform float4 _MainTexture_ST;
            uniform sampler2D _TopTexture; uniform float4 _TopTexture_ST;
            uniform float _TopSpread;
            uniform sampler2D _Noise; uniform float4 _Noise_ST;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float3 tangentDir : TEXCOORD3;
                float3 bitangentDir : TEXCOORD4;
                LIGHTING_COORDS(5,6)
                UNITY_FOG_COORDS(7)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.bitangentDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos( v.vertex );
                UNITY_TRANSFER_FOG(o,o.pos);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.bitangentDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 _Normal_var = UnpackNormal(tex2D(_Normal,TRANSFORM_TEX(i.uv0, _Normal)));
                float3 normalLocal = _Normal_var.rgb;
                float3 normalDirection = normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
                float3 viewReflectDirection = reflect( -viewDirection, normalDirection );
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightColor = _LightColor0.rgb;
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
////// Emissive:
                float3 node_5328 = abs(i.normalDir);
                float3 node_8294 = (node_5328*node_5328);
                float2 node_3133 = float2(i.posWorld.b,i.posWorld.g);
                float4 node_7614 = tex2D(_MainTexture,TRANSFORM_TEX(node_3133, _MainTexture));
                float2 node_6019 = float2(i.posWorld.b,i.posWorld.r);
                float4 node_5979 = tex2D(_MainTexture,TRANSFORM_TEX(node_6019, _MainTexture));
                float2 node_4659 = float2(i.posWorld.r,i.posWorld.g);
                float4 node_8327 = tex2D(_MainTexture,TRANSFORM_TEX(node_4659, _MainTexture));
                float4 node_2528 = tex2D(_TopTexture,TRANSFORM_TEX(node_3133, _TopTexture));
                float4 node_436 = tex2D(_TopTexture,TRANSFORM_TEX(node_6019, _TopTexture));
                float4 node_4080 = tex2D(_TopTexture,TRANSFORM_TEX(node_4659, _TopTexture));
                float2 node_122 = float2(i.posWorld.r,i.posWorld.b);
                float4 _Noise_var = tex2D(_Noise,TRANSFORM_TEX(node_122, _Noise));
                float node_123 = i.normalDir.g;
                float node_4831 = clamp((_TopSpread*node_123),0,1);
                float2 node_7319 = (((3.0*_Noise_var.r)*float2(node_4831,node_4831))+node_123);
                float4 node_557 = tex2D(_SpecularRamp,node_7319);
                float node_4518_if_leA = step(length(lightDirection),1.0);
                float node_4518_if_leB = step(1.0,length(lightDirection));
                float node_5764 = 0.5*dot(lightDirection,i.normalDir)+0.5;
                float node_1463 = 0.5*dot(lerp((node_4518_if_leA*0.0)+(node_4518_if_leB*node_5764),node_5764,node_4518_if_leA*node_4518_if_leB),_Normal_var.rgb)+0.5;
                float node_8746 = normalize(attenuation);
                float node_4750 = (node_1463*node_8746);
                float2 node_9693 = float2(node_4750,node_4750);
                float4 node_8756 = tex2D(_LightRamp,TRANSFORM_TEX(node_9693, _LightRamp));
                float3 node_6637 = (node_8756.rgb*(_LightColor0.rgb+0.3));
                float node_390 = (dot(node_1463,dot(viewReflectDirection,i.normalDir))*node_8746);
                float2 node_195 = float2(node_390,node_390);
                float4 node_5713 = tex2D(_SpecularRamp,node_195);
                float node_5415 = 1.0;
                float node_8754_if_leA = step(_UseGlossSpec,node_5415);
                float node_8754_if_leB = step(node_5415,_UseGlossSpec);
                float4 _GlossSpec_var = tex2D(_GlossSpec,TRANSFORM_TEX(i.uv0, _GlossSpec));
                float node_376_if_leA = step(_UseGlossSpec,node_5415);
                float node_376_if_leB = step(node_5415,_UseGlossSpec);
                float3 node_682 = (node_5713.rgb*clamp(lerp((node_8754_if_leA*_SpecularColor.rgb)+(node_8754_if_leB*_SpecularColor.rgb),_GlossSpec_var.rgb,node_8754_if_leA*node_8754_if_leB),0.0,lerp((node_376_if_leA*_SpecularColor.a)+(node_376_if_leB*_SpecularColor.a),clamp(_GlossSpec_var.a,0,0.2),node_376_if_leA*node_376_if_leB)));
                float3 emissive = saturate(( UNITY_LIGHTMODEL_AMBIENT.rgb > 0.5 ? (1.0-(1.0-2.0*(UNITY_LIGHTMODEL_AMBIENT.rgb-0.5))*(1.0-((lerp((node_8294.r*node_7614.rgb + node_8294.g*node_5979.rgb + node_8294.b*node_8327.rgb),(node_8294.r*node_2528.rgb + node_8294.g*node_436.rgb + node_8294.b*node_4080.rgb),node_557.rgb) > 0.5 ?  (1.0-(1.0-2.0*(lerp((node_8294.r*node_7614.rgb + node_8294.g*node_5979.rgb + node_8294.b*node_8327.rgb),(node_8294.r*node_2528.rgb + node_8294.g*node_436.rgb + node_8294.b*node_4080.rgb),node_557.rgb)-0.5))*(1.0-node_6637)) : (2.0*lerp((node_8294.r*node_7614.rgb + node_8294.g*node_5979.rgb + node_8294.b*node_8327.rgb),(node_8294.r*node_2528.rgb + node_8294.g*node_436.rgb + node_8294.b*node_4080.rgb),node_557.rgb)*node_6637)) +node_682))) : (2.0*UNITY_LIGHTMODEL_AMBIENT.rgb*((lerp((node_8294.r*node_7614.rgb + node_8294.g*node_5979.rgb + node_8294.b*node_8327.rgb),(node_8294.r*node_2528.rgb + node_8294.g*node_436.rgb + node_8294.b*node_4080.rgb),node_557.rgb) > 0.5 ?  (1.0-(1.0-2.0*(lerp((node_8294.r*node_7614.rgb + node_8294.g*node_5979.rgb + node_8294.b*node_8327.rgb),(node_8294.r*node_2528.rgb + node_8294.g*node_436.rgb + node_8294.b*node_4080.rgb),node_557.rgb)-0.5))*(1.0-node_6637)) : (2.0*lerp((node_8294.r*node_7614.rgb + node_8294.g*node_5979.rgb + node_8294.b*node_8327.rgb),(node_8294.r*node_2528.rgb + node_8294.g*node_436.rgb + node_8294.b*node_4080.rgb),node_557.rgb)*node_6637)) +node_682)) ));
                float node_1128 = max(0,dot(lightDirection,i.normalDir));
                float2 node_5492 = float2(node_1128,node_1128);
                float4 node_1195 = tex2D(_LightRamp,TRANSFORM_TEX(node_5492, _LightRamp));
                float node_3539 = 0.0;
                float node_3727_if_leA = step(_WorldSpaceLightPos0.a,node_3539);
                float node_3727_if_leB = step(node_3539,_WorldSpaceLightPos0.a);
                float3 finalColor = emissive + ((node_1195.rgb*_LightColor0.rgb)*saturate((pow(((attenuation*exp(8.0))/distance(_WorldSpaceLightPos0.rgb,i.posWorld.rgb)),exp(10.0))*lerp((node_3727_if_leA*node_3539)+(node_3727_if_leB*_LightColor0.a),node_3539,node_3727_if_leA*node_3727_if_leB))));
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
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDADD
            #define _GLOSSYENV 1
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal d3d11_9x xboxone ps4 psp2 n3ds wiiu 
            #pragma target 3.0
            uniform sampler2D _Normal; uniform float4 _Normal_ST;
            uniform float4 _SpecularColor;
            uniform sampler2D _SpecularRamp;
            uniform sampler2D _GlossSpec; uniform float4 _GlossSpec_ST;
            uniform fixed _UseGlossSpec;
            uniform sampler2D _LightRamp; uniform float4 _LightRamp_ST;
            uniform sampler2D _MainTexture; uniform float4 _MainTexture_ST;
            uniform sampler2D _TopTexture; uniform float4 _TopTexture_ST;
            uniform float _TopSpread;
            uniform sampler2D _Noise; uniform float4 _Noise_ST;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float3 tangentDir : TEXCOORD3;
                float3 bitangentDir : TEXCOORD4;
                LIGHTING_COORDS(5,6)
                UNITY_FOG_COORDS(7)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.bitangentDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos( v.vertex );
                UNITY_TRANSFER_FOG(o,o.pos);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.bitangentDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 _Normal_var = UnpackNormal(tex2D(_Normal,TRANSFORM_TEX(i.uv0, _Normal)));
                float3 normalLocal = _Normal_var.rgb;
                float3 normalDirection = normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
                float3 viewReflectDirection = reflect( -viewDirection, normalDirection );
                float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.posWorld.xyz,_WorldSpaceLightPos0.w));
                float3 lightColor = _LightColor0.rgb;
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float node_1128 = max(0,dot(lightDirection,i.normalDir));
                float2 node_5492 = float2(node_1128,node_1128);
                float4 node_1195 = tex2D(_LightRamp,TRANSFORM_TEX(node_5492, _LightRamp));
                float node_3539 = 0.0;
                float node_3727_if_leA = step(_WorldSpaceLightPos0.a,node_3539);
                float node_3727_if_leB = step(node_3539,_WorldSpaceLightPos0.a);
                float3 finalColor = ((node_1195.rgb*_LightColor0.rgb)*saturate((pow(((attenuation*exp(8.0))/distance(_WorldSpaceLightPos0.rgb,i.posWorld.rgb)),exp(10.0))*lerp((node_3727_if_leA*node_3539)+(node_3727_if_leB*_LightColor0.a),node_3539,node_3727_if_leA*node_3727_if_leB))));
                fixed4 finalRGBA = fixed4(finalColor * 1,0);
                UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
                return finalRGBA;
            }
            ENDCG
        }
    }
    FallBack "Standard"
    CustomEditor "AdvancedCelShader_Terrain"
}
