// Shader created with Shader Forge v1.38 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.38;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:True,hqlp:False,rprd:True,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:2,bsrc:3,bdst:7,dpts:2,wrdp:False,dith:3,atcv:False,rfrpo:True,rfrpn:Refraction,coma:15,ufog:True,aust:False,igpj:False,qofs:0,qpre:3,rntp:1,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.7725491,fgcg:1,fgcb:1,fgca:1,fgde:0.02,fgrn:10,fgrf:250,stcl:False,atwp:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False,fsmp:False;n:type:ShaderForge.SFN_Final,id:2865,x:34490,y:32345,varname:node_2865,prsc:2|emission-100-OUT,custl-4969-OUT,voffset-9828-OUT,tess-632-OUT;n:type:ShaderForge.SFN_Color,id:6665,x:31733,y:31824,ptovrint:False,ptlb:Shoreline Color,ptin:_ShorelineColor,varname:_Color,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:1,c3:1,c4:1;n:type:ShaderForge.SFN_Lerp,id:3859,x:32679,y:31892,varname:node_3859,prsc:2|A-7342-OUT,B-5959-OUT,T-6800-OUT;n:type:ShaderForge.SFN_Color,id:9363,x:31719,y:32014,ptovrint:False,ptlb:Deep Water Color,ptin:_DeepWaterColor,varname:_Color_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0,c2:0.6705883,c3:1,c4:1;n:type:ShaderForge.SFN_DepthBlend,id:1125,x:32356,y:32452,varname:node_1125,prsc:2|DIST-606-OUT;n:type:ShaderForge.SFN_Slider,id:5454,x:31047,y:32474,ptovrint:False,ptlb:Shoreline Depth,ptin:_ShorelineDepth,varname:node_5454,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0.5,cur:0.5,max:5;n:type:ShaderForge.SFN_Slider,id:8510,x:31815,y:33395,ptovrint:False,ptlb:Water Speed,ptin:_WaterSpeed,varname:node_8510,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0.01,max:1;n:type:ShaderForge.SFN_TexCoord,id:8939,x:31972,y:33035,varname:node_8939,prsc:2,uv:0,uaff:False;n:type:ShaderForge.SFN_Time,id:7870,x:31972,y:33183,varname:node_7870,prsc:2;n:type:ShaderForge.SFN_Panner,id:2263,x:32224,y:33112,varname:node_2263,prsc:2,spu:1,spv:1|UVIN-8939-UVOUT,DIST-3777-OUT;n:type:ShaderForge.SFN_Multiply,id:3777,x:32180,y:33288,varname:node_3777,prsc:2|A-7870-TSL,B-8234-OUT;n:type:ShaderForge.SFN_Slider,id:7872,x:32390,y:33427,ptovrint:False,ptlb:Waves Intensity,ptin:_WavesIntensity,varname:node_7872,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0.25,cur:0.3,max:1;n:type:ShaderForge.SFN_Relay,id:8234,x:31936,y:33306,varname:node_8234,prsc:2|IN-8510-OUT;n:type:ShaderForge.SFN_Vector3,id:1212,x:32906,y:33399,varname:node_1212,prsc:2,v1:0,v2:0.5,v3:0;n:type:ShaderForge.SFN_Multiply,id:9828,x:32851,y:33263,varname:node_9828,prsc:2|A-1757-OUT,B-1212-OUT;n:type:ShaderForge.SFN_Multiply,id:1757,x:32642,y:33149,varname:node_1757,prsc:2|A-8747-OUT,B-9826-OUT;n:type:ShaderForge.SFN_Code,id:8747,x:31895,y:33532,varname:node_8747,prsc:2,code:ZgBsAG8AYQB0ACAAcgBlAHQAIAA9ACAAMAA7AA0ACgAgACAAaQBuAHQAIABpAHQAZQByAGEAdABpAG8AbgBzACAAPQAgADYAOwANAAoAIAAgAGYAbwByACAAKABpAG4AdAAgAGkAIAA9ACAAMAA7ACAAaQAgADwAIABpAHQAZQByAGEAdABpAG8AbgBzADsAIAArACsAaQApAA0ACgAgACAAewANAAoAIAAgACAAIAAgAGYAbABvAGEAdAAyACAAcAAgAD0AIABmAGwAbwBvAHIAKABVAFYAIAAqACAAKABpACsAMQApACkAOwANAAoAIAAgACAAIAAgAGYAbABvAGEAdAAyACAAZgAgAD0AIABmAHIAYQBjACgAVQBWACAAKgAgACgAaQArADEAKQApADsADQAKACAAIAAgACAAIABmACAAPQAgAGYAIAAqACAAZgAgACoAIAAoADMALgAwACAALQAgADIALgAwACAAKgAgAGYAKQA7AA0ACgAgACAAIAAgACAAZgBsAG8AYQB0ACAAbgAgAD0AIABwAC4AeAAgACsAIABwAC4AeQAgACoAIAA1ADcALgAwADsADQAKACAAIAAgACAAIABmAGwAbwBhAHQANAAgAG4AbwBpAHMAZQAgAD0AIABmAGwAbwBhAHQANAAoAG4ALAAgAG4AIAArACAAMQAsACAAbgAgACsAIAA1ADcALgAwACwAIABuACAAKwAgADUAOAAuADAAKQA7AA0ACgAgACAAIAAgACAAbgBvAGkAcwBlACAAPQAgAGYAcgBhAGMAKABzAGkAbgAoAG4AbwBpAHMAZQApACoANAAzADcALgA1ADgANQA0ADUAMwApADsADQAKACAAIAAgACAAIAByAGUAdAAgACsAPQAgAGwAZQByAHAAKABsAGUAcgBwACgAbgBvAGkAcwBlAC4AeAAsACAAbgBvAGkAcwBlAC4AeQAsACAAZgAuAHgAKQAsACAAbABlAHIAcAAoAG4AbwBpAHMAZQAuAHoALAAgAG4AbwBpAHMAZQAuAHcALAAgAGYALgB4ACkALAAgAGYALgB5ACkAIAAqACAAKAAgAGkAdABlAHIAYQB0AGkAbwBuAHMAIAAvACAAKABpACsAMQApACkAOwANAAoAIAAgAH0ADQAKACAAIAByAGUAdAB1AHIAbgAgAHIAZQB0AC8AaQB0AGUAcgBhAHQAaQBvAG4AcwA7AA==,output:1,fname:NoiseGen,width:801,height:251,input:1,input_1_label:UV|A-2528-OUT;n:type:ShaderForge.SFN_Multiply,id:2528,x:31637,y:33609,varname:node_2528,prsc:2|A-2263-UVOUT,B-9392-OUT;n:type:ShaderForge.SFN_Slider,id:3312,x:31773,y:33960,ptovrint:False,ptlb:Waves Density,ptin:_WavesDensity,varname:node_3312,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:10,max:10;n:type:ShaderForge.SFN_LightPosition,id:8985,x:32918,y:33929,varname:node_8985,prsc:2;n:type:ShaderForge.SFN_Distance,id:1459,x:33281,y:33902,varname:node_1459,prsc:2|A-8985-XYZ,B-4042-XYZ;n:type:ShaderForge.SFN_Divide,id:1897,x:33313,y:33720,varname:node_1897,prsc:2|A-2957-OUT,B-1459-OUT;n:type:ShaderForge.SFN_Power,id:9857,x:33346,y:33529,varname:node_9857,prsc:2|VAL-1897-OUT,EXP-3225-OUT;n:type:ShaderForge.SFN_Clamp01,id:4666,x:34189,y:34139,varname:node_4666,prsc:2|IN-8011-OUT;n:type:ShaderForge.SFN_Multiply,id:8011,x:33965,y:34139,varname:node_8011,prsc:2|A-9857-OUT,B-4997-OUT;n:type:ShaderForge.SFN_LightColor,id:1585,x:33448,y:34009,varname:node_1585,prsc:2;n:type:ShaderForge.SFN_FragmentPosition,id:4042,x:32918,y:34060,varname:node_4042,prsc:2;n:type:ShaderForge.SFN_Vector1,id:8827,x:33574,y:33842,varname:node_8827,prsc:2,v1:10;n:type:ShaderForge.SFN_Exp,id:3225,x:33574,y:33693,varname:node_3225,prsc:2,et:0|IN-8827-OUT;n:type:ShaderForge.SFN_If,id:4997,x:33754,y:34213,varname:node_4997,prsc:2|A-8985-PNT,B-1746-OUT,GT-1585-A,EQ-1746-OUT,LT-1746-OUT;n:type:ShaderForge.SFN_Vector1,id:1746,x:33754,y:34388,varname:node_1746,prsc:2,v1:0;n:type:ShaderForge.SFN_Multiply,id:4969,x:35034,y:33438,varname:node_4969,prsc:2|A-2111-OUT,B-4666-OUT;n:type:ShaderForge.SFN_NormalVector,id:3433,x:33784,y:33485,prsc:2,pt:False;n:type:ShaderForge.SFN_Multiply,id:2111,x:34330,y:33756,varname:node_2111,prsc:2|A-7240-RGB,B-1585-RGB;n:type:ShaderForge.SFN_LightVector,id:3721,x:33775,y:33322,varname:node_3721,prsc:2;n:type:ShaderForge.SFN_Dot,id:6072,x:33991,y:33369,varname:node_6072,prsc:2,dt:1|A-3721-OUT,B-3433-OUT;n:type:ShaderForge.SFN_Append,id:3505,x:34364,y:33308,varname:node_3505,prsc:2|A-6072-OUT,B-6072-OUT;n:type:ShaderForge.SFN_Tex2d,id:7240,x:34274,y:33462,ptovrint:False,ptlb:Light Ramp,ptin:_LightRamp,varname:node_1195,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:True,tagnrm:False,tex:7ea4fa8f6637d234cabf1448f60f5b81,ntxv:0,isnm:False|UVIN-3505-OUT;n:type:ShaderForge.SFN_Multiply,id:2957,x:33117,y:33638,varname:node_2957,prsc:2|A-5033-OUT,B-900-OUT;n:type:ShaderForge.SFN_LightAttenuation,id:5033,x:32887,y:33539,varname:node_5033,prsc:2;n:type:ShaderForge.SFN_Vector1,id:204,x:32918,y:33862,varname:node_204,prsc:2,v1:8;n:type:ShaderForge.SFN_Exp,id:900,x:32918,y:33686,varname:node_900,prsc:2,et:0|IN-204-OUT;n:type:ShaderForge.SFN_DepthBlend,id:7663,x:32248,y:32708,varname:node_7663,prsc:2|DIST-9848-OUT;n:type:ShaderForge.SFN_Vector1,id:6675,x:32066,y:32872,varname:node_6675,prsc:2,v1:3;n:type:ShaderForge.SFN_Lerp,id:5959,x:32285,y:32008,varname:node_5959,prsc:2|A-5335-OUT,B-9363-RGB,T-7663-OUT;n:type:ShaderForge.SFN_Color,id:7907,x:31733,y:32204,ptovrint:False,ptlb:shallow Water Color,ptin:_shallowWaterColor,varname:node_7907,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0.5588235,c2:0.9634888,c3:1,c4:1;n:type:ShaderForge.SFN_Multiply,id:9848,x:32056,y:32708,varname:node_9848,prsc:2|A-5454-OUT,B-6675-OUT;n:type:ShaderForge.SFN_Vector4,id:9686,x:32930,y:32918,varname:node_9686,prsc:2,v1:0,v2:0,v3:0,v4:0;n:type:ShaderForge.SFN_Lerp,id:8380,x:32865,y:32703,varname:node_8380,prsc:2|A-3609-OUT,B-9686-OUT,T-2009-OUT;n:type:ShaderForge.SFN_ComponentMask,id:4369,x:33133,y:32608,varname:node_4369,prsc:2,cc1:0,cc2:-1,cc3:-1,cc4:-1|IN-8887-OUT;n:type:ShaderForge.SFN_OneMinus,id:7507,x:33133,y:32443,varname:node_7507,prsc:2|IN-4369-OUT;n:type:ShaderForge.SFN_Lerp,id:513,x:33102,y:32106,varname:node_513,prsc:2|A-7342-OUT,B-3859-OUT,T-7424-OUT;n:type:ShaderForge.SFN_Vector1,id:779,x:33329,y:32652,varname:node_779,prsc:2,v1:4;n:type:ShaderForge.SFN_Power,id:3705,x:32601,y:32422,varname:node_3705,prsc:2|VAL-1125-OUT,EXP-7896-OUT;n:type:ShaderForge.SFN_Vector1,id:7896,x:32601,y:32587,varname:node_7896,prsc:2,v1:16;n:type:ShaderForge.SFN_Clamp01,id:6800,x:32601,y:32280,varname:node_6800,prsc:2|IN-3705-OUT;n:type:ShaderForge.SFN_Multiply,id:606,x:32098,y:32449,varname:node_606,prsc:2|A-5454-OUT,B-4268-OUT;n:type:ShaderForge.SFN_Vector1,id:4268,x:32098,y:32613,varname:node_4268,prsc:2,v1:0.25;n:type:ShaderForge.SFN_ComponentMask,id:304,x:31587,y:32931,varname:node_304,prsc:2,cc1:0,cc2:-1,cc3:-1,cc4:-1|IN-3347-OUT;n:type:ShaderForge.SFN_Clamp01,id:3609,x:32332,y:32951,varname:node_3609,prsc:2|IN-304-OUT;n:type:ShaderForge.SFN_Multiply,id:9392,x:31558,y:33789,varname:node_9392,prsc:2|A-3312-OUT,B-7504-OUT;n:type:ShaderForge.SFN_Vector1,id:7504,x:31558,y:33948,varname:node_7504,prsc:2,v1:10;n:type:ShaderForge.SFN_Multiply,id:9826,x:32400,y:33255,varname:node_9826,prsc:2|A-9122-OUT,B-7872-OUT;n:type:ShaderForge.SFN_Vector1,id:9122,x:32595,y:33308,varname:node_9122,prsc:2,v1:2;n:type:ShaderForge.SFN_Slider,id:1226,x:33292,y:33205,ptovrint:False,ptlb:Tessellation Value,ptin:_TessellationValue,varname:node_1226,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:1,cur:32,max:64;n:type:ShaderForge.SFN_If,id:632,x:33898,y:33121,varname:node_632,prsc:2|A-1719-OUT,B-839-OUT,GT-1226-OUT,EQ-1226-OUT,LT-6633-OUT;n:type:ShaderForge.SFN_Vector1,id:839,x:33658,y:33121,varname:node_839,prsc:2,v1:1;n:type:ShaderForge.SFN_ToggleProperty,id:1719,x:33658,y:33054,ptovrint:False,ptlb:Use Tessellation,ptin:_UseTessellation,varname:node_1719,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:True;n:type:ShaderForge.SFN_Vector1,id:6633,x:33576,y:33290,varname:node_6633,prsc:2,v1:1;n:type:ShaderForge.SFN_Fresnel,id:4933,x:33118,y:31878,varname:node_4933,prsc:2|EXP-5365-OUT;n:type:ShaderForge.SFN_Vector1,id:5365,x:33102,y:32026,varname:node_5365,prsc:2,v1:32;n:type:ShaderForge.SFN_Add,id:8042,x:33501,y:32139,varname:node_8042,prsc:2|A-4933-OUT,B-513-OUT;n:type:ShaderForge.SFN_AmbientLight,id:7976,x:33942,y:31898,varname:node_7976,prsc:2;n:type:ShaderForge.SFN_OneMinus,id:8128,x:33136,y:33381,varname:node_8128,prsc:2|IN-8747-OUT;n:type:ShaderForge.SFN_RemapRange,id:5986,x:33370,y:33363,varname:node_5986,prsc:2,frmn:0,frmx:1,tomn:-1,tomx:1|IN-8128-OUT;n:type:ShaderForge.SFN_Tex2dAsset,id:5960,x:30721,y:32779,ptovrint:False,ptlb:Foam,ptin:_Foam,varname:node_5960,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:True,tagnrm:False,tex:4b328c7b4ac6c9b458659368c1b4330b,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Tex2d,id:6701,x:31015,y:32919,varname:node_6701,prsc:2,tex:4b328c7b4ac6c9b458659368c1b4330b,ntxv:0,isnm:False|UVIN-9433-OUT,TEX-5960-TEX;n:type:ShaderForge.SFN_TexCoord,id:4087,x:30521,y:33032,varname:node_4087,prsc:2,uv:0,uaff:False;n:type:ShaderForge.SFN_Multiply,id:9433,x:30751,y:33114,varname:node_9433,prsc:2|A-4087-UVOUT,B-9585-OUT;n:type:ShaderForge.SFN_Slider,id:9585,x:30553,y:33287,ptovrint:False,ptlb:Foam Scale,ptin:_FoamScale,varname:node_9585,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:1,cur:1,max:512;n:type:ShaderForge.SFN_Vector1,id:3248,x:31233,y:33226,varname:node_3248,prsc:2,v1:5;n:type:ShaderForge.SFN_Power,id:3347,x:31290,y:33016,varname:node_3347,prsc:2|VAL-6701-RGB,EXP-3248-OUT;n:type:ShaderForge.SFN_DepthBlend,id:2009,x:31812,y:32628,varname:node_2009,prsc:2|DIST-7418-OUT;n:type:ShaderForge.SFN_Vector1,id:4092,x:31630,y:32792,varname:node_4092,prsc:2,v1:0.5;n:type:ShaderForge.SFN_Multiply,id:7418,x:31620,y:32628,varname:node_7418,prsc:2|A-5454-OUT,B-4092-OUT;n:type:ShaderForge.SFN_Multiply,id:8887,x:32868,y:32477,varname:node_8887,prsc:2|A-7065-OUT,B-8380-OUT;n:type:ShaderForge.SFN_Vector1,id:7065,x:32850,y:32355,varname:node_7065,prsc:2,v1:2;n:type:ShaderForge.SFN_Clamp01,id:7424,x:33304,y:32324,varname:node_7424,prsc:2|IN-3802-OUT;n:type:ShaderForge.SFN_Add,id:5335,x:32285,y:32324,varname:node_5335,prsc:2|A-7907-RGB,B-3198-OUT;n:type:ShaderForge.SFN_Multiply,id:3198,x:32616,y:32976,varname:node_3198,prsc:2|A-482-OUT,B-3609-OUT;n:type:ShaderForge.SFN_Vector1,id:482,x:32564,y:32893,varname:node_482,prsc:2,v1:0.25;n:type:ShaderForge.SFN_Multiply,id:3802,x:33526,y:32552,varname:node_3802,prsc:2|A-7507-OUT,B-779-OUT;n:type:ShaderForge.SFN_Add,id:7342,x:32465,y:31783,varname:node_7342,prsc:2|A-1463-OUT,B-6665-RGB;n:type:ShaderForge.SFN_Vector1,id:1463,x:32434,y:31699,varname:node_1463,prsc:2,v1:0.3;n:type:ShaderForge.SFN_Multiply,id:9804,x:33501,y:31979,varname:node_9804,prsc:2|A-4177-OUT,B-8042-OUT;n:type:ShaderForge.SFN_LightColor,id:2246,x:33424,y:31756,varname:node_2246,prsc:2;n:type:ShaderForge.SFN_Blend,id:100,x:34223,y:32017,varname:node_100,prsc:2,blmd:14,clmp:True|SRC-7976-RGB,DST-9804-OUT;n:type:ShaderForge.SFN_Vector1,id:8410,x:33435,y:31902,varname:node_8410,prsc:2,v1:0.3;n:type:ShaderForge.SFN_Add,id:4177,x:33628,y:31811,varname:node_4177,prsc:2|A-2246-RGB,B-8410-OUT;proporder:9363-6665-5454-8510-7872-3312-7240-7907-1226-1719-5960-9585;pass:END;sub:END;*/

Shader "Stylized/Water" {
    Properties {
        _DeepWaterColor ("Deep Water Color", Color) = (0,0.6705883,1,1)
        _ShorelineColor ("Shoreline Color", Color) = (1,1,1,1)
        _ShorelineDepth ("Shoreline Depth", Range(0.5, 5)) = 0.5
        _WaterSpeed ("Water Speed", Range(0, 1)) = 0.01
        _WavesIntensity ("Waves Intensity", Range(0.25, 1)) = 0.3
        _WavesDensity ("Waves Density", Range(0, 10)) = 10
        [NoScaleOffset]_LightRamp ("Light Ramp", 2D) = "white" {}
        _shallowWaterColor ("shallow Water Color", Color) = (0.5588235,0.9634888,1,1)
        _TessellationValue ("Tessellation Value", Range(1, 64)) = 32
        [MaterialToggle] _UseTessellation ("Use Tessellation", Float ) = 1
        [NoScaleOffset]_Foam ("Foam", 2D) = "white" {}
        _FoamScale ("Foam Scale", Range(1, 512)) = 1
    }
    SubShader {
        Tags {
            "Queue"="Transparent"
            "RenderType"="Opaque"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            
            CGPROGRAM
            #pragma hull hull
            #pragma domain domain
            #pragma vertex tessvert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #define _GLOSSYENV 1
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #include "Tessellation.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal d3d11_9x xboxone ps4 psp2 n3ds wiiu 
            #pragma target 5.0
            uniform sampler2D _CameraDepthTexture;
            uniform float4 _ShorelineColor;
            uniform float4 _DeepWaterColor;
            uniform float _ShorelineDepth;
            uniform float _WaterSpeed;
            uniform float _WavesIntensity;
            float2 NoiseGen( float2 UV ){
            float ret = 0;
              int iterations = 6;
              for (int i = 0; i < iterations; ++i)
              {
                 float2 p = floor(UV * (i+1));
                 float2 f = frac(UV * (i+1));
                 f = f * f * (3.0 - 2.0 * f);
                 float n = p.x + p.y * 57.0;
                 float4 noise = float4(n, n + 1, n + 57.0, n + 58.0);
                 noise = frac(sin(noise)*437.585453);
                 ret += lerp(lerp(noise.x, noise.y, f.x), lerp(noise.z, noise.w, f.x), f.y) * ( iterations / (i+1));
              }
              return ret/iterations;
            }
            
            uniform float _WavesDensity;
            uniform sampler2D _LightRamp;
            uniform float4 _shallowWaterColor;
            uniform float _TessellationValue;
            uniform fixed _UseTessellation;
            uniform sampler2D _Foam;
            uniform float _FoamScale;
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
                float4 projPos : TEXCOORD3;
                LIGHTING_COORDS(4,5)
                UNITY_FOG_COORDS(6)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                float4 node_7870 = _Time;
                float2 node_8747 = NoiseGen( ((o.uv0+(node_7870.r*_WaterSpeed)*float2(1,1))*(_WavesDensity*10.0)) );
                v.vertex.xyz += (float3((node_8747*(2.0*_WavesIntensity)),0.0)*float3(0,0.5,0));
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos( v.vertex );
                UNITY_TRANSFER_FOG(o,o.pos);
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            #ifdef UNITY_CAN_COMPILE_TESSELLATION
                struct TessVertex {
                    float4 vertex : INTERNALTESSPOS;
                    float3 normal : NORMAL;
                    float4 tangent : TANGENT;
                    float2 texcoord0 : TEXCOORD0;
                };
                struct OutputPatchConstant {
                    float edge[3]         : SV_TessFactor;
                    float inside          : SV_InsideTessFactor;
                    float3 vTangent[4]    : TANGENT;
                    float2 vUV[4]         : TEXCOORD;
                    float3 vTanUCorner[4] : TANUCORNER;
                    float3 vTanVCorner[4] : TANVCORNER;
                    float4 vCWts          : TANWEIGHTS;
                };
                TessVertex tessvert (VertexInput v) {
                    TessVertex o;
                    o.vertex = v.vertex;
                    o.normal = v.normal;
                    o.tangent = v.tangent;
                    o.texcoord0 = v.texcoord0;
                    return o;
                }
                float Tessellation(TessVertex v){
                    float node_632_if_leA = step(_UseTessellation,1.0);
                    float node_632_if_leB = step(1.0,_UseTessellation);
                    return lerp((node_632_if_leA*1.0)+(node_632_if_leB*_TessellationValue),_TessellationValue,node_632_if_leA*node_632_if_leB);
                }
                float4 Tessellation(TessVertex v, TessVertex v1, TessVertex v2){
                    float tv = Tessellation(v);
                    float tv1 = Tessellation(v1);
                    float tv2 = Tessellation(v2);
                    return float4( tv1+tv2, tv2+tv, tv+tv1, tv+tv1+tv2 ) / float4(2,2,2,3);
                }
                OutputPatchConstant hullconst (InputPatch<TessVertex,3> v) {
                    OutputPatchConstant o = (OutputPatchConstant)0;
                    float4 ts = Tessellation( v[0], v[1], v[2] );
                    o.edge[0] = ts.x;
                    o.edge[1] = ts.y;
                    o.edge[2] = ts.z;
                    o.inside = ts.w;
                    return o;
                }
                [domain("tri")]
                [partitioning("fractional_odd")]
                [outputtopology("triangle_cw")]
                [patchconstantfunc("hullconst")]
                [outputcontrolpoints(3)]
                TessVertex hull (InputPatch<TessVertex,3> v, uint id : SV_OutputControlPointID) {
                    return v[id];
                }
                [domain("tri")]
                VertexOutput domain (OutputPatchConstant tessFactors, const OutputPatch<TessVertex,3> vi, float3 bary : SV_DomainLocation) {
                    VertexInput v = (VertexInput)0;
                    v.vertex = vi[0].vertex*bary.x + vi[1].vertex*bary.y + vi[2].vertex*bary.z;
                    v.normal = vi[0].normal*bary.x + vi[1].normal*bary.y + vi[2].normal*bary.z;
                    v.tangent = vi[0].tangent*bary.x + vi[1].tangent*bary.y + vi[2].tangent*bary.z;
                    v.texcoord0 = vi[0].texcoord0*bary.x + vi[1].texcoord0*bary.y + vi[2].texcoord0*bary.z;
                    VertexOutput o = vert(v);
                    return o;
                }
            #endif
            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                i.normalDir = normalize(i.normalDir);
                i.normalDir *= faceSign;
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalDirection = i.normalDir;
                float sceneZ = max(0,LinearEyeDepth (UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)))) - _ProjectionParams.g);
                float partZ = max(0,i.projPos.z - _ProjectionParams.g);
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightColor = _LightColor0.rgb;
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
////// Emissive:
                float3 node_7342 = (0.3+_ShorelineColor.rgb);
                float2 node_9433 = (i.uv0*_FoamScale);
                float4 node_6701 = tex2D(_Foam,node_9433);
                float node_3609 = saturate(pow(node_6701.rgb,5.0).r);
                float3 node_9804 = ((_LightColor0.rgb+0.3)*(pow(1.0-max(0,dot(normalDirection, viewDirection)),32.0)+lerp(node_7342,lerp(node_7342,lerp((_shallowWaterColor.rgb+(0.25*node_3609)),_DeepWaterColor.rgb,saturate((sceneZ-partZ)/(_ShorelineDepth*3.0))),saturate(pow(saturate((sceneZ-partZ)/(_ShorelineDepth*0.25)),16.0))),saturate(((1.0 - (2.0*lerp(float4(node_3609,node_3609,node_3609,node_3609),float4(0,0,0,0),saturate((sceneZ-partZ)/(_ShorelineDepth*0.5)))).r)*4.0)))));
                float3 emissive = saturate(( UNITY_LIGHTMODEL_AMBIENT.rgb > 0.5 ? (node_9804 + 2.0*UNITY_LIGHTMODEL_AMBIENT.rgb -1.0) : (node_9804 + 2.0*(UNITY_LIGHTMODEL_AMBIENT.rgb-0.5))));
                float node_6072 = max(0,dot(lightDirection,i.normalDir));
                float2 node_3505 = float2(node_6072,node_6072);
                float4 _LightRamp_var = tex2D(_LightRamp,node_3505);
                float node_1746 = 0.0;
                float node_4997_if_leA = step(_WorldSpaceLightPos0.a,node_1746);
                float node_4997_if_leB = step(node_1746,_WorldSpaceLightPos0.a);
                float3 finalColor = emissive + ((_LightRamp_var.rgb*_LightColor0.rgb)*saturate((pow(((attenuation*exp(8.0))/distance(_WorldSpaceLightPos0.rgb,i.posWorld.rgb)),exp(10.0))*lerp((node_4997_if_leA*node_1746)+(node_4997_if_leB*_LightColor0.a),node_1746,node_4997_if_leA*node_4997_if_leB))));
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
            ZWrite Off
            
            CGPROGRAM
            #pragma hull hull
            #pragma domain domain
            #pragma vertex tessvert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDADD
            #define _GLOSSYENV 1
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #include "Tessellation.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal d3d11_9x xboxone ps4 psp2 n3ds wiiu 
            #pragma target 5.0
            uniform sampler2D _CameraDepthTexture;
            uniform float4 _ShorelineColor;
            uniform float4 _DeepWaterColor;
            uniform float _ShorelineDepth;
            uniform float _WaterSpeed;
            uniform float _WavesIntensity;
            float2 NoiseGen( float2 UV ){
            float ret = 0;
              int iterations = 6;
              for (int i = 0; i < iterations; ++i)
              {
                 float2 p = floor(UV * (i+1));
                 float2 f = frac(UV * (i+1));
                 f = f * f * (3.0 - 2.0 * f);
                 float n = p.x + p.y * 57.0;
                 float4 noise = float4(n, n + 1, n + 57.0, n + 58.0);
                 noise = frac(sin(noise)*437.585453);
                 ret += lerp(lerp(noise.x, noise.y, f.x), lerp(noise.z, noise.w, f.x), f.y) * ( iterations / (i+1));
              }
              return ret/iterations;
            }
            
            uniform float _WavesDensity;
            uniform sampler2D _LightRamp;
            uniform float4 _shallowWaterColor;
            uniform float _TessellationValue;
            uniform fixed _UseTessellation;
            uniform sampler2D _Foam;
            uniform float _FoamScale;
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
                float4 projPos : TEXCOORD3;
                LIGHTING_COORDS(4,5)
                UNITY_FOG_COORDS(6)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                float4 node_7870 = _Time;
                float2 node_8747 = NoiseGen( ((o.uv0+(node_7870.r*_WaterSpeed)*float2(1,1))*(_WavesDensity*10.0)) );
                v.vertex.xyz += (float3((node_8747*(2.0*_WavesIntensity)),0.0)*float3(0,0.5,0));
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos( v.vertex );
                UNITY_TRANSFER_FOG(o,o.pos);
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            #ifdef UNITY_CAN_COMPILE_TESSELLATION
                struct TessVertex {
                    float4 vertex : INTERNALTESSPOS;
                    float3 normal : NORMAL;
                    float4 tangent : TANGENT;
                    float2 texcoord0 : TEXCOORD0;
                };
                struct OutputPatchConstant {
                    float edge[3]         : SV_TessFactor;
                    float inside          : SV_InsideTessFactor;
                    float3 vTangent[4]    : TANGENT;
                    float2 vUV[4]         : TEXCOORD;
                    float3 vTanUCorner[4] : TANUCORNER;
                    float3 vTanVCorner[4] : TANVCORNER;
                    float4 vCWts          : TANWEIGHTS;
                };
                TessVertex tessvert (VertexInput v) {
                    TessVertex o;
                    o.vertex = v.vertex;
                    o.normal = v.normal;
                    o.tangent = v.tangent;
                    o.texcoord0 = v.texcoord0;
                    return o;
                }
                float Tessellation(TessVertex v){
                    float node_632_if_leA = step(_UseTessellation,1.0);
                    float node_632_if_leB = step(1.0,_UseTessellation);
                    return lerp((node_632_if_leA*1.0)+(node_632_if_leB*_TessellationValue),_TessellationValue,node_632_if_leA*node_632_if_leB);
                }
                float4 Tessellation(TessVertex v, TessVertex v1, TessVertex v2){
                    float tv = Tessellation(v);
                    float tv1 = Tessellation(v1);
                    float tv2 = Tessellation(v2);
                    return float4( tv1+tv2, tv2+tv, tv+tv1, tv+tv1+tv2 ) / float4(2,2,2,3);
                }
                OutputPatchConstant hullconst (InputPatch<TessVertex,3> v) {
                    OutputPatchConstant o = (OutputPatchConstant)0;
                    float4 ts = Tessellation( v[0], v[1], v[2] );
                    o.edge[0] = ts.x;
                    o.edge[1] = ts.y;
                    o.edge[2] = ts.z;
                    o.inside = ts.w;
                    return o;
                }
                [domain("tri")]
                [partitioning("fractional_odd")]
                [outputtopology("triangle_cw")]
                [patchconstantfunc("hullconst")]
                [outputcontrolpoints(3)]
                TessVertex hull (InputPatch<TessVertex,3> v, uint id : SV_OutputControlPointID) {
                    return v[id];
                }
                [domain("tri")]
                VertexOutput domain (OutputPatchConstant tessFactors, const OutputPatch<TessVertex,3> vi, float3 bary : SV_DomainLocation) {
                    VertexInput v = (VertexInput)0;
                    v.vertex = vi[0].vertex*bary.x + vi[1].vertex*bary.y + vi[2].vertex*bary.z;
                    v.normal = vi[0].normal*bary.x + vi[1].normal*bary.y + vi[2].normal*bary.z;
                    v.tangent = vi[0].tangent*bary.x + vi[1].tangent*bary.y + vi[2].tangent*bary.z;
                    v.texcoord0 = vi[0].texcoord0*bary.x + vi[1].texcoord0*bary.y + vi[2].texcoord0*bary.z;
                    VertexOutput o = vert(v);
                    return o;
                }
            #endif
            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                i.normalDir = normalize(i.normalDir);
                i.normalDir *= faceSign;
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalDirection = i.normalDir;
                float sceneZ = max(0,LinearEyeDepth (UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)))) - _ProjectionParams.g);
                float partZ = max(0,i.projPos.z - _ProjectionParams.g);
                float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.posWorld.xyz,_WorldSpaceLightPos0.w));
                float3 lightColor = _LightColor0.rgb;
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float node_6072 = max(0,dot(lightDirection,i.normalDir));
                float2 node_3505 = float2(node_6072,node_6072);
                float4 _LightRamp_var = tex2D(_LightRamp,node_3505);
                float node_1746 = 0.0;
                float node_4997_if_leA = step(_WorldSpaceLightPos0.a,node_1746);
                float node_4997_if_leB = step(node_1746,_WorldSpaceLightPos0.a);
                float3 finalColor = ((_LightRamp_var.rgb*_LightColor0.rgb)*saturate((pow(((attenuation*exp(8.0))/distance(_WorldSpaceLightPos0.rgb,i.posWorld.rgb)),exp(10.0))*lerp((node_4997_if_leA*node_1746)+(node_4997_if_leB*_LightColor0.a),node_1746,node_4997_if_leA*node_4997_if_leB))));
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
            #pragma hull hull
            #pragma domain domain
            #pragma vertex tessvert
            #pragma fragment frag
            #define UNITY_PASS_SHADOWCASTER
            #define _GLOSSYENV 1
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "Tessellation.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal d3d11_9x xboxone ps4 psp2 n3ds wiiu 
            #pragma target 5.0
            uniform float _WaterSpeed;
            uniform float _WavesIntensity;
            float2 NoiseGen( float2 UV ){
            float ret = 0;
              int iterations = 6;
              for (int i = 0; i < iterations; ++i)
              {
                 float2 p = floor(UV * (i+1));
                 float2 f = frac(UV * (i+1));
                 f = f * f * (3.0 - 2.0 * f);
                 float n = p.x + p.y * 57.0;
                 float4 noise = float4(n, n + 1, n + 57.0, n + 58.0);
                 noise = frac(sin(noise)*437.585453);
                 ret += lerp(lerp(noise.x, noise.y, f.x), lerp(noise.z, noise.w, f.x), f.y) * ( iterations / (i+1));
              }
              return ret/iterations;
            }
            
            uniform float _WavesDensity;
            uniform float _TessellationValue;
            uniform fixed _UseTessellation;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                V2F_SHADOW_CASTER;
                float2 uv0 : TEXCOORD1;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                float4 node_7870 = _Time;
                float2 node_8747 = NoiseGen( ((o.uv0+(node_7870.r*_WaterSpeed)*float2(1,1))*(_WavesDensity*10.0)) );
                v.vertex.xyz += (float3((node_8747*(2.0*_WavesIntensity)),0.0)*float3(0,0.5,0));
                o.pos = UnityObjectToClipPos( v.vertex );
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }
            #ifdef UNITY_CAN_COMPILE_TESSELLATION
                struct TessVertex {
                    float4 vertex : INTERNALTESSPOS;
                    float3 normal : NORMAL;
                    float4 tangent : TANGENT;
                    float2 texcoord0 : TEXCOORD0;
                };
                struct OutputPatchConstant {
                    float edge[3]         : SV_TessFactor;
                    float inside          : SV_InsideTessFactor;
                    float3 vTangent[4]    : TANGENT;
                    float2 vUV[4]         : TEXCOORD;
                    float3 vTanUCorner[4] : TANUCORNER;
                    float3 vTanVCorner[4] : TANVCORNER;
                    float4 vCWts          : TANWEIGHTS;
                };
                TessVertex tessvert (VertexInput v) {
                    TessVertex o;
                    o.vertex = v.vertex;
                    o.normal = v.normal;
                    o.tangent = v.tangent;
                    o.texcoord0 = v.texcoord0;
                    return o;
                }
                float Tessellation(TessVertex v){
                    float node_632_if_leA = step(_UseTessellation,1.0);
                    float node_632_if_leB = step(1.0,_UseTessellation);
                    return lerp((node_632_if_leA*1.0)+(node_632_if_leB*_TessellationValue),_TessellationValue,node_632_if_leA*node_632_if_leB);
                }
                float4 Tessellation(TessVertex v, TessVertex v1, TessVertex v2){
                    float tv = Tessellation(v);
                    float tv1 = Tessellation(v1);
                    float tv2 = Tessellation(v2);
                    return float4( tv1+tv2, tv2+tv, tv+tv1, tv+tv1+tv2 ) / float4(2,2,2,3);
                }
                OutputPatchConstant hullconst (InputPatch<TessVertex,3> v) {
                    OutputPatchConstant o = (OutputPatchConstant)0;
                    float4 ts = Tessellation( v[0], v[1], v[2] );
                    o.edge[0] = ts.x;
                    o.edge[1] = ts.y;
                    o.edge[2] = ts.z;
                    o.inside = ts.w;
                    return o;
                }
                [domain("tri")]
                [partitioning("fractional_odd")]
                [outputtopology("triangle_cw")]
                [patchconstantfunc("hullconst")]
                [outputcontrolpoints(3)]
                TessVertex hull (InputPatch<TessVertex,3> v, uint id : SV_OutputControlPointID) {
                    return v[id];
                }
                [domain("tri")]
                VertexOutput domain (OutputPatchConstant tessFactors, const OutputPatch<TessVertex,3> vi, float3 bary : SV_DomainLocation) {
                    VertexInput v = (VertexInput)0;
                    v.vertex = vi[0].vertex*bary.x + vi[1].vertex*bary.y + vi[2].vertex*bary.z;
                    v.normal = vi[0].normal*bary.x + vi[1].normal*bary.y + vi[2].normal*bary.z;
                    v.tangent = vi[0].tangent*bary.x + vi[1].tangent*bary.y + vi[2].tangent*bary.z;
                    v.texcoord0 = vi[0].texcoord0*bary.x + vi[1].texcoord0*bary.y + vi[2].texcoord0*bary.z;
                    VertexOutput o = vert(v);
                    return o;
                }
            #endif
            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
        Pass {
            Name "Meta"
            Tags {
                "LightMode"="Meta"
            }
            Cull Off
            
            CGPROGRAM
            #pragma hull hull
            #pragma domain domain
            #pragma vertex tessvert
            #pragma fragment frag
            #define UNITY_PASS_META 1
            #define _GLOSSYENV 1
            #include "UnityCG.cginc"
            #include "Tessellation.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #include "UnityMetaPass.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal d3d11_9x xboxone ps4 psp2 n3ds wiiu 
            #pragma target 5.0
            uniform sampler2D _CameraDepthTexture;
            uniform float4 _ShorelineColor;
            uniform float4 _DeepWaterColor;
            uniform float _ShorelineDepth;
            uniform float _WaterSpeed;
            uniform float _WavesIntensity;
            float2 NoiseGen( float2 UV ){
            float ret = 0;
              int iterations = 6;
              for (int i = 0; i < iterations; ++i)
              {
                 float2 p = floor(UV * (i+1));
                 float2 f = frac(UV * (i+1));
                 f = f * f * (3.0 - 2.0 * f);
                 float n = p.x + p.y * 57.0;
                 float4 noise = float4(n, n + 1, n + 57.0, n + 58.0);
                 noise = frac(sin(noise)*437.585453);
                 ret += lerp(lerp(noise.x, noise.y, f.x), lerp(noise.z, noise.w, f.x), f.y) * ( iterations / (i+1));
              }
              return ret/iterations;
            }
            
            uniform float _WavesDensity;
            uniform float4 _shallowWaterColor;
            uniform float _TessellationValue;
            uniform fixed _UseTessellation;
            uniform sampler2D _Foam;
            uniform float _FoamScale;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float4 projPos : TEXCOORD3;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                float4 node_7870 = _Time;
                float2 node_8747 = NoiseGen( ((o.uv0+(node_7870.r*_WaterSpeed)*float2(1,1))*(_WavesDensity*10.0)) );
                v.vertex.xyz += (float3((node_8747*(2.0*_WavesIntensity)),0.0)*float3(0,0.5,0));
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityMetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST );
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                return o;
            }
            #ifdef UNITY_CAN_COMPILE_TESSELLATION
                struct TessVertex {
                    float4 vertex : INTERNALTESSPOS;
                    float3 normal : NORMAL;
                    float4 tangent : TANGENT;
                    float2 texcoord0 : TEXCOORD0;
                    float2 texcoord1 : TEXCOORD1;
                    float2 texcoord2 : TEXCOORD2;
                };
                struct OutputPatchConstant {
                    float edge[3]         : SV_TessFactor;
                    float inside          : SV_InsideTessFactor;
                    float3 vTangent[4]    : TANGENT;
                    float2 vUV[4]         : TEXCOORD;
                    float3 vTanUCorner[4] : TANUCORNER;
                    float3 vTanVCorner[4] : TANVCORNER;
                    float4 vCWts          : TANWEIGHTS;
                };
                TessVertex tessvert (VertexInput v) {
                    TessVertex o;
                    o.vertex = v.vertex;
                    o.normal = v.normal;
                    o.tangent = v.tangent;
                    o.texcoord0 = v.texcoord0;
                    o.texcoord1 = v.texcoord1;
                    o.texcoord2 = v.texcoord2;
                    return o;
                }
                float Tessellation(TessVertex v){
                    float node_632_if_leA = step(_UseTessellation,1.0);
                    float node_632_if_leB = step(1.0,_UseTessellation);
                    return lerp((node_632_if_leA*1.0)+(node_632_if_leB*_TessellationValue),_TessellationValue,node_632_if_leA*node_632_if_leB);
                }
                float4 Tessellation(TessVertex v, TessVertex v1, TessVertex v2){
                    float tv = Tessellation(v);
                    float tv1 = Tessellation(v1);
                    float tv2 = Tessellation(v2);
                    return float4( tv1+tv2, tv2+tv, tv+tv1, tv+tv1+tv2 ) / float4(2,2,2,3);
                }
                OutputPatchConstant hullconst (InputPatch<TessVertex,3> v) {
                    OutputPatchConstant o = (OutputPatchConstant)0;
                    float4 ts = Tessellation( v[0], v[1], v[2] );
                    o.edge[0] = ts.x;
                    o.edge[1] = ts.y;
                    o.edge[2] = ts.z;
                    o.inside = ts.w;
                    return o;
                }
                [domain("tri")]
                [partitioning("fractional_odd")]
                [outputtopology("triangle_cw")]
                [patchconstantfunc("hullconst")]
                [outputcontrolpoints(3)]
                TessVertex hull (InputPatch<TessVertex,3> v, uint id : SV_OutputControlPointID) {
                    return v[id];
                }
                [domain("tri")]
                VertexOutput domain (OutputPatchConstant tessFactors, const OutputPatch<TessVertex,3> vi, float3 bary : SV_DomainLocation) {
                    VertexInput v = (VertexInput)0;
                    v.vertex = vi[0].vertex*bary.x + vi[1].vertex*bary.y + vi[2].vertex*bary.z;
                    v.normal = vi[0].normal*bary.x + vi[1].normal*bary.y + vi[2].normal*bary.z;
                    v.tangent = vi[0].tangent*bary.x + vi[1].tangent*bary.y + vi[2].tangent*bary.z;
                    v.texcoord0 = vi[0].texcoord0*bary.x + vi[1].texcoord0*bary.y + vi[2].texcoord0*bary.z;
                    v.texcoord1 = vi[0].texcoord1*bary.x + vi[1].texcoord1*bary.y + vi[2].texcoord1*bary.z;
                    VertexOutput o = vert(v);
                    return o;
                }
            #endif
            float4 frag(VertexOutput i, float facing : VFACE) : SV_Target {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                i.normalDir = normalize(i.normalDir);
                i.normalDir *= faceSign;
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalDirection = i.normalDir;
                float sceneZ = max(0,LinearEyeDepth (UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)))) - _ProjectionParams.g);
                float partZ = max(0,i.projPos.z - _ProjectionParams.g);
                float3 lightColor = _LightColor0.rgb;
                UnityMetaInput o;
                UNITY_INITIALIZE_OUTPUT( UnityMetaInput, o );
                
                float3 node_7342 = (0.3+_ShorelineColor.rgb);
                float2 node_9433 = (i.uv0*_FoamScale);
                float4 node_6701 = tex2D(_Foam,node_9433);
                float node_3609 = saturate(pow(node_6701.rgb,5.0).r);
                float3 node_9804 = ((_LightColor0.rgb+0.3)*(pow(1.0-max(0,dot(normalDirection, viewDirection)),32.0)+lerp(node_7342,lerp(node_7342,lerp((_shallowWaterColor.rgb+(0.25*node_3609)),_DeepWaterColor.rgb,saturate((sceneZ-partZ)/(_ShorelineDepth*3.0))),saturate(pow(saturate((sceneZ-partZ)/(_ShorelineDepth*0.25)),16.0))),saturate(((1.0 - (2.0*lerp(float4(node_3609,node_3609,node_3609,node_3609),float4(0,0,0,0),saturate((sceneZ-partZ)/(_ShorelineDepth*0.5)))).r)*4.0)))));
                o.Emission = saturate(( UNITY_LIGHTMODEL_AMBIENT.rgb > 0.5 ? (node_9804 + 2.0*UNITY_LIGHTMODEL_AMBIENT.rgb -1.0) : (node_9804 + 2.0*(UNITY_LIGHTMODEL_AMBIENT.rgb-0.5))));
                
                float3 diffColor = float3(0,0,0);
                o.Albedo = diffColor;
                
                return UnityMetaFragment( o );
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "AdvancedCelShader_Water"
}
