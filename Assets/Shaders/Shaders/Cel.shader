// Shader created with Shader Forge v1.38 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.38;sub:START;pass:START;ps:flbk:Standard,iptp:0,cusa:False,bamd:0,cgin:,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:True,hqlp:False,rprd:True,enco:False,rmgx:True,imps:False,rpth:0,vtps:0,hqsc:False,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:0,bdst:1,dpts:2,wrdp:True,dith:1,atcv:False,rfrpo:False,rfrpn:Refraction,coma:15,ufog:True,aust:True,igpj:False,qofs:0,qpre:2,rntp:3,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.703,fgcg:1,fgcb:1,fgca:1,fgde:0.06,fgrn:0,fgrf:300,stcl:False,atwp:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False,fsmp:False;n:type:ShaderForge.SFN_Final,id:2865,x:35054,y:32930,varname:node_2865,prsc:2|normal-2366-OUT,emission-2111-OUT,custl-7289-OUT,clip-6047-OUT,olwid-858-OUT,olcol-8873-RGB;n:type:ShaderForge.SFN_NormalVector,id:6703,x:30805,y:31419,prsc:2,pt:False;n:type:ShaderForge.SFN_Dot,id:5764,x:31131,y:31370,varname:node_5764,prsc:2,dt:4|A-3161-OUT,B-6703-OUT;n:type:ShaderForge.SFN_LightVector,id:3161,x:30805,y:31279,varname:node_3161,prsc:2;n:type:ShaderForge.SFN_Tex2d,id:8756,x:31362,y:31083,varname:node_8756,prsc:2,tex:7ea4fa8f6637d234cabf1448f60f5b81,ntxv:1,isnm:False|UVIN-9693-OUT,TEX-9522-TEX;n:type:ShaderForge.SFN_Append,id:9693,x:31574,y:31349,varname:node_9693,prsc:2|A-4750-OUT,B-4750-OUT;n:type:ShaderForge.SFN_Multiply,id:4750,x:31711,y:31879,varname:node_4750,prsc:2|A-1463-OUT,B-1060-OUT;n:type:ShaderForge.SFN_Tex2d,id:9673,x:31554,y:30742,ptovrint:False,ptlb:Albedo,ptin:_Albedo,varname:node_9673,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:1,isnm:False;n:type:ShaderForge.SFN_Blend,id:7458,x:32133,y:31130,varname:node_7458,prsc:2,blmd:12,clmp:True|SRC-8031-OUT,DST-6637-OUT;n:type:ShaderForge.SFN_Tex2d,id:1699,x:30584,y:31626,ptovrint:False,ptlb:Normal,ptin:_Normal,varname:node_1699,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:True,ntxv:3,isnm:True;n:type:ShaderForge.SFN_Dot,id:1463,x:31171,y:31585,varname:node_1463,prsc:2,dt:4|A-4518-OUT,B-2366-OUT;n:type:ShaderForge.SFN_ViewReflectionVector,id:3643,x:30983,y:32121,varname:node_3643,prsc:2;n:type:ShaderForge.SFN_Dot,id:8302,x:31263,y:31942,varname:node_8302,prsc:2,dt:0|A-1463-OUT,B-9414-OUT;n:type:ShaderForge.SFN_Append,id:195,x:32039,y:32177,varname:node_195,prsc:2|A-390-OUT,B-390-OUT;n:type:ShaderForge.SFN_Tex2d,id:5713,x:32234,y:32082,varname:node_5713,prsc:2,tex:94a5cd6cfabc7e843abe56079809ea1b,ntxv:0,isnm:False|UVIN-195-OUT,TEX-4047-TEX;n:type:ShaderForge.SFN_Add,id:6692,x:33056,y:31520,varname:node_6692,prsc:2|A-7458-OUT,B-682-OUT;n:type:ShaderForge.SFN_Color,id:8131,x:32160,y:32258,ptovrint:False,ptlb:Specular Color,ptin:_SpecularColor,varname:node_8131,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0.5,c2:0.5,c3:0.5,c4:0;n:type:ShaderForge.SFN_Multiply,id:682,x:32706,y:32065,varname:node_682,prsc:2|A-5713-RGB,B-2321-OUT;n:type:ShaderForge.SFN_Clamp,id:2321,x:32779,y:32275,varname:node_2321,prsc:2|IN-8754-OUT,MIN-5106-OUT,MAX-376-OUT;n:type:ShaderForge.SFN_Vector1,id:5106,x:32263,y:32449,varname:node_5106,prsc:2,v1:0;n:type:ShaderForge.SFN_Color,id:4151,x:31554,y:30566,ptovrint:False,ptlb:Albedo Color,ptin:_AlbedoColor,varname:node_4151,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:1,c3:1,c4:1;n:type:ShaderForge.SFN_Multiply,id:1577,x:32539,y:30822,varname:node_1577,prsc:2|A-4151-RGB,B-9673-RGB;n:type:ShaderForge.SFN_NormalVector,id:8590,x:30996,y:32253,prsc:2,pt:False;n:type:ShaderForge.SFN_Dot,id:9414,x:31242,y:32165,varname:node_9414,prsc:2,dt:0|A-3643-OUT,B-8590-OUT;n:type:ShaderForge.SFN_Slider,id:5727,x:35238,y:33651,ptovrint:False,ptlb:Fade Distance,ptin:_FadeDistance,varname:node_5727,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:1,max:1;n:type:ShaderForge.SFN_FragmentPosition,id:6650,x:34771,y:33704,varname:node_6650,prsc:2;n:type:ShaderForge.SFN_ViewPosition,id:7845,x:34771,y:33849,varname:node_7845,prsc:2;n:type:ShaderForge.SFN_Distance,id:7314,x:35075,y:33773,varname:node_7314,prsc:2|A-6650-XYZ,B-7845-XYZ;n:type:ShaderForge.SFN_Clamp01,id:1574,x:35904,y:33909,varname:node_1574,prsc:2|IN-6153-OUT;n:type:ShaderForge.SFN_Subtract,id:6153,x:35317,y:33779,varname:node_6153,prsc:2|A-7314-OUT,B-5727-OUT;n:type:ShaderForge.SFN_Fresnel,id:6366,x:32915,y:31850,varname:node_6366,prsc:2|NRM-166-OUT;n:type:ShaderForge.SFN_Add,id:853,x:33821,y:32718,varname:node_853,prsc:2|A-6692-OUT,B-4851-OUT;n:type:ShaderForge.SFN_Divide,id:9574,x:33219,y:32696,varname:node_9574,prsc:2|A-1523-OUT,B-9745-OUT;n:type:ShaderForge.SFN_Tex2d,id:1146,x:33421,y:31929,varname:node_1146,prsc:2,tex:94a5cd6cfabc7e843abe56079809ea1b,ntxv:0,isnm:False|UVIN-6576-OUT,TEX-4047-TEX;n:type:ShaderForge.SFN_Append,id:6576,x:33519,y:32702,varname:node_6576,prsc:2|A-4971-OUT,B-4971-OUT;n:type:ShaderForge.SFN_Multiply,id:4217,x:33805,y:32025,varname:node_4217,prsc:2|A-1146-RGB,B-5701-RGB;n:type:ShaderForge.SFN_Color,id:5701,x:33601,y:32101,ptovrint:False,ptlb:Rim Light Color,ptin:_RimLightColor,varname:node_5701,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0.5,c2:0.5,c3:0.5,c4:1;n:type:ShaderForge.SFN_Clamp,id:4851,x:34006,y:32115,varname:node_4851,prsc:2|IN-4217-OUT,MIN-3565-OUT,MAX-2501-OUT;n:type:ShaderForge.SFN_Vector1,id:2501,x:33805,y:32204,varname:node_2501,prsc:2,v1:0.5;n:type:ShaderForge.SFN_Vector1,id:3565,x:33805,y:32149,varname:node_3565,prsc:2,v1:0;n:type:ShaderForge.SFN_Slider,id:9745,x:32974,y:32891,ptovrint:False,ptlb:Rim Light Thickness,ptin:_RimLightThickness,varname:node_9745,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0.5,cur:1.1,max:5;n:type:ShaderForge.SFN_If,id:8031,x:32566,y:30630,varname:node_8031,prsc:2|A-9293-OUT,B-6985-OUT,GT-1577-OUT,EQ-1577-OUT,LT-4151-RGB;n:type:ShaderForge.SFN_ToggleProperty,id:9293,x:32280,y:30528,ptovrint:False,ptlb:Use Albedo,ptin:_UseAlbedo,varname:node_9293,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:True;n:type:ShaderForge.SFN_Vector1,id:6985,x:32280,y:30608,varname:node_6985,prsc:2,v1:1;n:type:ShaderForge.SFN_Tex2dAsset,id:4047,x:32458,y:31900,ptovrint:False,ptlb:Specular Ramp,ptin:_SpecularRamp,varname:node_4047,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:True,tagnrm:False,tex:94a5cd6cfabc7e843abe56079809ea1b,ntxv:1,isnm:False;n:type:ShaderForge.SFN_Tex2d,id:5491,x:31966,y:32379,ptovrint:False,ptlb:GlossSpec,ptin:_GlossSpec,varname:node_5491,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:2,isnm:False;n:type:ShaderForge.SFN_ConstantClamp,id:9976,x:32052,y:32556,varname:node_9976,prsc:2,min:0,max:0.2|IN-5491-A;n:type:ShaderForge.SFN_ToggleProperty,id:2051,x:32447,y:32254,ptovrint:False,ptlb:Use GlossSpec,ptin:_UseGlossSpec,varname:node_2051,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:False;n:type:ShaderForge.SFN_If,id:8754,x:32458,y:32320,varname:node_8754,prsc:2|A-5415-OUT,B-2051-OUT,GT-8131-RGB,EQ-5491-RGB,LT-8131-RGB;n:type:ShaderForge.SFN_Vector1,id:5415,x:32447,y:32182,varname:node_5415,prsc:2,v1:1;n:type:ShaderForge.SFN_If,id:376,x:32733,y:32497,varname:node_376,prsc:2|A-5415-OUT,B-2051-OUT,GT-8131-A,EQ-9976-OUT,LT-8131-A;n:type:ShaderForge.SFN_Tex2d,id:894,x:32945,y:33147,ptovrint:False,ptlb:Emission,ptin:_Emission,varname:node_894,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:2,isnm:False;n:type:ShaderForge.SFN_Slider,id:4563,x:32804,y:33341,ptovrint:False,ptlb:Emission Power,ptin:_EmissionPower,varname:node_4563,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0,max:5;n:type:ShaderForge.SFN_Multiply,id:7559,x:33201,y:33192,varname:node_7559,prsc:2|A-5758-OUT,B-4563-OUT;n:type:ShaderForge.SFN_Multiply,id:390,x:31774,y:32159,varname:node_390,prsc:2|A-8302-OUT,B-1060-OUT;n:type:ShaderForge.SFN_Multiply,id:4971,x:33587,y:32895,varname:node_4971,prsc:2|A-9574-OUT,B-6340-OUT;n:type:ShaderForge.SFN_Tex2dAsset,id:9522,x:31045,y:30941,ptovrint:False,ptlb:Light Ramp,ptin:_LightRamp,varname:node_9522,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:7ea4fa8f6637d234cabf1448f60f5b81,ntxv:1,isnm:False;n:type:ShaderForge.SFN_NormalVector,id:166,x:32519,y:31718,prsc:2,pt:False;n:type:ShaderForge.SFN_LightPosition,id:4423,x:32147,y:34146,varname:node_4423,prsc:2;n:type:ShaderForge.SFN_Distance,id:5489,x:32479,y:34257,varname:node_5489,prsc:2|A-4423-XYZ,B-6492-XYZ;n:type:ShaderForge.SFN_Divide,id:3449,x:32496,y:34120,varname:node_3449,prsc:2|A-4214-OUT,B-5489-OUT;n:type:ShaderForge.SFN_Power,id:464,x:32518,y:33821,varname:node_464,prsc:2|VAL-3449-OUT,EXP-5919-OUT;n:type:ShaderForge.SFN_Clamp01,id:2458,x:33436,y:34402,varname:node_2458,prsc:2|IN-2754-OUT;n:type:ShaderForge.SFN_Multiply,id:2754,x:33212,y:34402,varname:node_2754,prsc:2|A-464-OUT,B-3727-OUT;n:type:ShaderForge.SFN_LightColor,id:9832,x:32650,y:34354,varname:node_9832,prsc:2;n:type:ShaderForge.SFN_FragmentPosition,id:6492,x:32130,y:34321,varname:node_6492,prsc:2;n:type:ShaderForge.SFN_Vector1,id:7755,x:32646,y:34089,varname:node_7755,prsc:2,v1:10;n:type:ShaderForge.SFN_Exp,id:5919,x:32646,y:33940,varname:node_5919,prsc:2,et:0|IN-7755-OUT;n:type:ShaderForge.SFN_If,id:3727,x:32957,y:34508,varname:node_3727,prsc:2|A-4423-PNT,B-3539-OUT,GT-9832-A,EQ-3539-OUT,LT-3539-OUT;n:type:ShaderForge.SFN_If,id:4518,x:30850,y:31011,varname:node_4518,prsc:2|A-7336-OUT,B-160-OUT,GT-5764-OUT,EQ-5764-OUT,LT-4410-OUT;n:type:ShaderForge.SFN_Length,id:7336,x:30554,y:31149,varname:node_7336,prsc:2|IN-3161-OUT;n:type:ShaderForge.SFN_Vector1,id:160,x:30510,y:30992,varname:node_160,prsc:2,v1:1;n:type:ShaderForge.SFN_Vector1,id:4410,x:30522,y:31081,varname:node_4410,prsc:2,v1:0;n:type:ShaderForge.SFN_LightAttenuation,id:1060,x:31054,y:31751,varname:node_1060,prsc:2;n:type:ShaderForge.SFN_Multiply,id:6637,x:31784,y:31097,varname:node_6637,prsc:2|A-8756-RGB,B-42-OUT;n:type:ShaderForge.SFN_LightColor,id:7978,x:31844,y:31559,varname:node_7978,prsc:2;n:type:ShaderForge.SFN_LightAttenuation,id:6340,x:33330,y:32911,varname:node_6340,prsc:2;n:type:ShaderForge.SFN_Vector1,id:3539,x:32630,y:34660,varname:node_3539,prsc:2,v1:0;n:type:ShaderForge.SFN_Multiply,id:7289,x:33858,y:33993,varname:node_7289,prsc:2|A-4564-OUT,B-2458-OUT;n:type:ShaderForge.SFN_LightVector,id:7741,x:33022,y:33585,varname:node_7741,prsc:2;n:type:ShaderForge.SFN_Dot,id:1128,x:33238,y:33632,varname:node_1128,prsc:2,dt:1|A-7741-OUT,B-5703-OUT;n:type:ShaderForge.SFN_NormalVector,id:5703,x:33031,y:33748,prsc:2,pt:False;n:type:ShaderForge.SFN_Multiply,id:4564,x:33577,y:34019,varname:node_4564,prsc:2|A-1195-RGB,B-9832-RGB;n:type:ShaderForge.SFN_Append,id:5492,x:33611,y:33571,varname:node_5492,prsc:2|A-1128-OUT,B-1128-OUT;n:type:ShaderForge.SFN_Tex2d,id:1195,x:33521,y:33725,varname:node_1195,prsc:2,tex:7ea4fa8f6637d234cabf1448f60f5b81,ntxv:0,isnm:False|UVIN-5492-OUT,TEX-9522-TEX;n:type:ShaderForge.SFN_Lerp,id:3427,x:34079,y:32892,varname:node_3427,prsc:2|A-7559-OUT,B-853-OUT,T-8348-OUT;n:type:ShaderForge.SFN_If,id:299,x:34416,y:33103,varname:node_299,prsc:2|A-9520-OUT,B-2500-OUT,GT-853-OUT,EQ-3427-OUT,LT-853-OUT;n:type:ShaderForge.SFN_ToggleProperty,id:9520,x:33706,y:33020,ptovrint:False,ptlb:Use Emission,ptin:_UseEmission,varname:node_9520,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:False;n:type:ShaderForge.SFN_Vector1,id:2500,x:33527,y:33064,varname:node_2500,prsc:2,v1:1;n:type:ShaderForge.SFN_Color,id:1731,x:32945,y:32993,ptovrint:False,ptlb:Emission Color,ptin:_EmissionColor,varname:node_1731,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0.5,c2:0.5,c3:0.5,c4:1;n:type:ShaderForge.SFN_Multiply,id:5758,x:33201,y:33035,varname:node_5758,prsc:2|A-1731-RGB,B-894-RGB;n:type:ShaderForge.SFN_Add,id:9564,x:33675,y:33278,varname:node_9564,prsc:2|A-894-R,B-894-G,C-894-B;n:type:ShaderForge.SFN_OneMinus,id:8348,x:34027,y:33266,varname:node_8348,prsc:2|IN-9564-OUT;n:type:ShaderForge.SFN_LightAttenuation,id:7706,x:31845,y:33847,varname:node_7706,prsc:2;n:type:ShaderForge.SFN_Multiply,id:4214,x:32222,y:33982,varname:node_4214,prsc:2|A-7706-OUT,B-7100-OUT;n:type:ShaderForge.SFN_Vector1,id:5417,x:31840,y:34003,varname:node_5417,prsc:2,v1:8;n:type:ShaderForge.SFN_Exp,id:7100,x:32222,y:33812,varname:node_7100,prsc:2,et:0|IN-5417-OUT;n:type:ShaderForge.SFN_ViewPosition,id:4190,x:35140,y:34654,varname:node_4190,prsc:2;n:type:ShaderForge.SFN_Distance,id:355,x:35342,y:34545,varname:node_355,prsc:2|A-6650-XYZ,B-4190-XYZ;n:type:ShaderForge.SFN_Divide,id:8992,x:35532,y:34401,varname:node_8992,prsc:2|A-752-OUT,B-355-OUT;n:type:ShaderForge.SFN_Vector1,id:752,x:35491,y:34272,varname:node_752,prsc:2,v1:5;n:type:ShaderForge.SFN_Multiply,id:6010,x:35510,y:34144,varname:node_6010,prsc:2|A-8992-OUT,B-622-OUT;n:type:ShaderForge.SFN_Multiply,id:4493,x:36307,y:34003,varname:node_4493,prsc:2|A-1574-OUT,B-9412-OUT;n:type:ShaderForge.SFN_Clamp01,id:9412,x:35850,y:34052,varname:node_9412,prsc:2|IN-6010-OUT;n:type:ShaderForge.SFN_ToggleProperty,id:1461,x:35633,y:33378,ptovrint:False,ptlb:Use Fade,ptin:_UseFade,varname:node_1461,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:False;n:type:ShaderForge.SFN_Vector1,id:1339,x:35633,y:33445,varname:node_1339,prsc:2,v1:1;n:type:ShaderForge.SFN_If,id:6047,x:35958,y:33501,varname:node_6047,prsc:2|A-1461-OUT,B-1339-OUT,GT-6228-OUT,EQ-4493-OUT,LT-6228-OUT;n:type:ShaderForge.SFN_Vector1,id:6228,x:35633,y:33542,varname:node_6228,prsc:2,v1:1;n:type:ShaderForge.SFN_Slider,id:622,x:35013,y:34182,ptovrint:False,ptlb:Far Fade Distance,ptin:_FarFadeDistance,varname:_FadeDistance_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:2,cur:2,max:50;n:type:ShaderForge.SFN_Dot,id:1523,x:33276,y:32314,varname:node_1523,prsc:2,dt:0|A-6366-OUT,B-2366-OUT;n:type:ShaderForge.SFN_If,id:858,x:34968,y:33466,varname:node_858,prsc:2|A-1500-OUT,B-628-OUT,GT-3676-OUT,EQ-4598-OUT,LT-3676-OUT;n:type:ShaderForge.SFN_ToggleProperty,id:1500,x:34701,y:33430,ptovrint:False,ptlb:UseOutline,ptin:_UseOutline,varname:node_1500,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:False;n:type:ShaderForge.SFN_Vector1,id:628,x:34701,y:33483,varname:node_628,prsc:2,v1:1;n:type:ShaderForge.SFN_Slider,id:4598,x:34497,y:33580,ptovrint:False,ptlb:OutlineThickness,ptin:_OutlineThickness,varname:node_4598,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0.005,max:0.01;n:type:ShaderForge.SFN_Vector1,id:3676,x:34949,y:33633,varname:node_3676,prsc:2,v1:0;n:type:ShaderForge.SFN_Color,id:8873,x:34649,y:33236,ptovrint:False,ptlb:OutlineColor,ptin:_OutlineColor,varname:node_8873,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0,c2:0,c3:0,c4:1;n:type:ShaderForge.SFN_AmbientLight,id:4332,x:34394,y:32746,varname:node_4332,prsc:2;n:type:ShaderForge.SFN_ValueProperty,id:1544,x:30561,y:31831,ptovrint:False,ptlb:Normal Intensity,ptin:_NormalIntensity,varname:node_1544,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_If,id:1540,x:35224,y:33722,varname:node_1540,prsc:2|A-6206-OUT,B-5992-OUT,GT-4109-OUT,EQ-1324-OUT,LT-4109-OUT;n:type:ShaderForge.SFN_ToggleProperty,id:6206,x:34957,y:33686,ptovrint:False,ptlb:UseOutline_copy,ptin:_UseOutline_copy,varname:_UseOutline_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:False;n:type:ShaderForge.SFN_Vector1,id:5992,x:34957,y:33739,varname:node_5992,prsc:2,v1:1;n:type:ShaderForge.SFN_Slider,id:1324,x:34753,y:33836,ptovrint:False,ptlb:OutlineThickness_copy,ptin:_OutlineThickness_copy,varname:_OutlineThickness_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0.005,max:0.01;n:type:ShaderForge.SFN_Vector1,id:4109,x:35205,y:33889,varname:node_4109,prsc:2,v1:0;n:type:ShaderForge.SFN_Color,id:1852,x:34905,y:33492,ptovrint:False,ptlb:OutlineColor_copy,ptin:_OutlineColor_copy,varname:_OutlineColor_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0,c2:0,c3:0,c4:1;n:type:ShaderForge.SFN_Multiply,id:2366,x:30865,y:31701,varname:node_2366,prsc:2|A-1699-RGB,B-1544-OUT;n:type:ShaderForge.SFN_Blend,id:2111,x:34753,y:32883,varname:node_2111,prsc:2,blmd:10,clmp:True|SRC-4332-RGB,DST-299-OUT;n:type:ShaderForge.SFN_Add,id:42,x:32069,y:31410,varname:node_42,prsc:2|A-7248-OUT,B-7978-RGB;n:type:ShaderForge.SFN_Vector1,id:7248,x:31812,y:31477,varname:node_7248,prsc:2,v1:0.3;proporder:9293-4151-9673-1699-2051-8131-5491-9522-4047-5701-9745-9520-1731-894-4563-1461-5727-622-1500-4598-8873-1544;pass:END;sub:END;*/

Shader "Stylized/Cel" {
    Properties {
        [MaterialToggle] _UseAlbedo ("Use Albedo", Float ) = 1
        _AlbedoColor ("Albedo Color", Color) = (1,1,1,1)
        _Albedo ("Albedo", 2D) = "gray" {}
        [Normal]_Normal ("Normal", 2D) = "bump" {}
        [MaterialToggle] _UseGlossSpec ("Use GlossSpec", Float ) = 0
        _SpecularColor ("Specular Color", Color) = (0.5,0.5,0.5,0)
        _GlossSpec ("GlossSpec", 2D) = "black" {}
        _LightRamp ("Light Ramp", 2D) = "gray" {}
        [NoScaleOffset]_SpecularRamp ("Specular Ramp", 2D) = "gray" {}
        _RimLightColor ("Rim Light Color", Color) = (0.5,0.5,0.5,1)
        _RimLightThickness ("Rim Light Thickness", Range(0.5, 5)) = 1.1
        [MaterialToggle] _UseEmission ("Use Emission", Float ) = 0
        _EmissionColor ("Emission Color", Color) = (0.5,0.5,0.5,1)
        _Emission ("Emission", 2D) = "black" {}
        _EmissionPower ("Emission Power", Range(0, 5)) = 0
        [MaterialToggle] _UseFade ("Use Fade", Float ) = 0
        _FadeDistance ("Fade Distance", Range(0, 1)) = 1
        _FarFadeDistance ("Far Fade Distance", Range(2, 50)) = 2
        [MaterialToggle] _UseOutline ("UseOutline", Float ) = 0
        _OutlineThickness ("OutlineThickness", Range(0, 2)) = 0.005
        _OutlineColor ("OutlineColor", Color) = (0,0,0,1)
        _NormalIntensity ("Normal Intensity", Float ) = 1
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
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
            uniform float _FadeDistance;
            uniform fixed _UseFade;
            uniform float _FarFadeDistance;
            uniform fixed _UseOutline;
            uniform float _OutlineThickness;
            uniform float4 _OutlineColor;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 posWorld : TEXCOORD0;
                float4 projPos : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float node_858_if_leA = step(_UseOutline,1.0);
                float node_858_if_leB = step(1.0,_UseOutline);
                float node_3676 = 0.0;
                o.pos = UnityObjectToClipPos( float4(v.vertex.xyz + v.normal*lerp((node_858_if_leA*node_3676)+(node_858_if_leB*node_3676),_OutlineThickness,node_858_if_leA*node_858_if_leB),1) );
                UNITY_TRANSFER_FOG(o,o.pos);
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                float2 sceneUVs = (i.projPos.xy / i.projPos.w);
                float node_6047_if_leA = step(_UseFade,1.0);
                float node_6047_if_leB = step(1.0,_UseFade);
                float node_6228 = 1.0;
                clip( BinaryDither2x2(lerp((node_6047_if_leA*node_6228)+(node_6047_if_leB*node_6228),(saturate((distance(i.posWorld.rgb,_WorldSpaceCameraPos)-_FadeDistance))*saturate(((5.0/distance(i.posWorld.rgb,_WorldSpaceCameraPos))*_FarFadeDistance))),node_6047_if_leA*node_6047_if_leB) - 1.5, sceneUVs) );
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
            uniform sampler2D _Albedo; uniform float4 _Albedo_ST;
            uniform sampler2D _Normal; uniform float4 _Normal_ST;
            uniform float4 _SpecularColor;
            uniform float4 _AlbedoColor;
            uniform float _FadeDistance;
            uniform float4 _RimLightColor;
            uniform float _RimLightThickness;
            uniform fixed _UseAlbedo;
            uniform sampler2D _SpecularRamp;
            uniform sampler2D _GlossSpec; uniform float4 _GlossSpec_ST;
            uniform fixed _UseGlossSpec;
            uniform sampler2D _Emission; uniform float4 _Emission_ST;
            uniform float _EmissionPower;
            uniform sampler2D _LightRamp; uniform float4 _LightRamp_ST;
            uniform fixed _UseEmission;
            uniform float4 _EmissionColor;
            uniform fixed _UseFade;
            uniform float _FarFadeDistance;
            uniform float _NormalIntensity;
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
                float4 projPos : TEXCOORD5;
                LIGHTING_COORDS(6,7)
                UNITY_FOG_COORDS(8)
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
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.bitangentDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 _Normal_var = UnpackNormal(tex2D(_Normal,TRANSFORM_TEX(i.uv0, _Normal)));
                float3 node_2366 = (_Normal_var.rgb*_NormalIntensity);
                float3 normalLocal = node_2366;
                float3 normalDirection = normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
                float3 viewReflectDirection = reflect( -viewDirection, normalDirection );
                float2 sceneUVs = (i.projPos.xy / i.projPos.w);
                float node_6047_if_leA = step(_UseFade,1.0);
                float node_6047_if_leB = step(1.0,_UseFade);
                float node_6228 = 1.0;
                clip( BinaryDither2x2(lerp((node_6047_if_leA*node_6228)+(node_6047_if_leB*node_6228),(saturate((distance(i.posWorld.rgb,_WorldSpaceCameraPos)-_FadeDistance))*saturate(((5.0/distance(i.posWorld.rgb,_WorldSpaceCameraPos))*_FarFadeDistance))),node_6047_if_leA*node_6047_if_leB) - 1.5, sceneUVs) );
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightColor = _LightColor0.rgb;
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
////// Emissive:
                float node_299_if_leA = step(_UseEmission,1.0);
                float node_299_if_leB = step(1.0,_UseEmission);
                float node_8031_if_leA = step(_UseAlbedo,1.0);
                float node_8031_if_leB = step(1.0,_UseAlbedo);
                float4 _Albedo_var = tex2D(_Albedo,TRANSFORM_TEX(i.uv0, _Albedo));
                float3 node_1577 = (_AlbedoColor.rgb*_Albedo_var.rgb);
                float node_4518_if_leA = step(length(lightDirection),1.0);
                float node_4518_if_leB = step(1.0,length(lightDirection));
                float node_5764 = 0.5*dot(lightDirection,i.normalDir)+0.5;
                float node_1463 = 0.5*dot(lerp((node_4518_if_leA*0.0)+(node_4518_if_leB*node_5764),node_5764,node_4518_if_leA*node_4518_if_leB),node_2366)+0.5;
                float node_4750 = (node_1463*attenuation);
                float2 node_9693 = float2(node_4750,node_4750);
                float4 node_8756 = tex2D(_LightRamp,TRANSFORM_TEX(node_9693, _LightRamp));
                float node_390 = (dot(node_1463,dot(viewReflectDirection,i.normalDir))*attenuation);
                float2 node_195 = float2(node_390,node_390);
                float4 node_5713 = tex2D(_SpecularRamp,node_195);
                float node_5415 = 1.0;
                float node_8754_if_leA = step(node_5415,_UseGlossSpec);
                float node_8754_if_leB = step(_UseGlossSpec,node_5415);
                float4 _GlossSpec_var = tex2D(_GlossSpec,TRANSFORM_TEX(i.uv0, _GlossSpec));
                float node_376_if_leA = step(node_5415,_UseGlossSpec);
                float node_376_if_leB = step(_UseGlossSpec,node_5415);
                float node_4971 = ((dot((1.0-max(0,dot(i.normalDir, viewDirection))),node_2366)/_RimLightThickness)*attenuation);
                float2 node_6576 = float2(node_4971,node_4971);
                float4 node_1146 = tex2D(_SpecularRamp,node_6576);
                float3 node_853 = ((saturate((lerp((node_8031_if_leA*_AlbedoColor.rgb)+(node_8031_if_leB*node_1577),node_1577,node_8031_if_leA*node_8031_if_leB) > 0.5 ?  (1.0-(1.0-2.0*(lerp((node_8031_if_leA*_AlbedoColor.rgb)+(node_8031_if_leB*node_1577),node_1577,node_8031_if_leA*node_8031_if_leB)-0.5))*(1.0-(node_8756.rgb*(0.3+_LightColor0.rgb)))) : (2.0*lerp((node_8031_if_leA*_AlbedoColor.rgb)+(node_8031_if_leB*node_1577),node_1577,node_8031_if_leA*node_8031_if_leB)*(node_8756.rgb*(0.3+_LightColor0.rgb)))) )+(node_5713.rgb*clamp(lerp((node_8754_if_leA*_SpecularColor.rgb)+(node_8754_if_leB*_SpecularColor.rgb),_GlossSpec_var.rgb,node_8754_if_leA*node_8754_if_leB),0.0,lerp((node_376_if_leA*_SpecularColor.a)+(node_376_if_leB*_SpecularColor.a),clamp(_GlossSpec_var.a,0,0.2),node_376_if_leA*node_376_if_leB))))+clamp((node_1146.rgb*_RimLightColor.rgb),0.0,0.5));
                float4 _Emission_var = tex2D(_Emission,TRANSFORM_TEX(i.uv0, _Emission));
                float3 node_299 = lerp((node_299_if_leA*node_853)+(node_299_if_leB*node_853),lerp(((_EmissionColor.rgb*_Emission_var.rgb)*_EmissionPower),node_853,(1.0 - (_Emission_var.r+_Emission_var.g+_Emission_var.b))),node_299_if_leA*node_299_if_leB);
                float3 emissive = saturate(( node_299 > 0.5 ? (1.0-(1.0-2.0*(node_299-0.5))*(1.0-UNITY_LIGHTMODEL_AMBIENT.rgb)) : (2.0*node_299*UNITY_LIGHTMODEL_AMBIENT.rgb) ));
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
            uniform sampler2D _Albedo; uniform float4 _Albedo_ST;
            uniform sampler2D _Normal; uniform float4 _Normal_ST;
            uniform float4 _SpecularColor;
            uniform float4 _AlbedoColor;
            uniform float _FadeDistance;
            uniform float4 _RimLightColor;
            uniform float _RimLightThickness;
            uniform fixed _UseAlbedo;
            uniform sampler2D _SpecularRamp;
            uniform sampler2D _GlossSpec; uniform float4 _GlossSpec_ST;
            uniform fixed _UseGlossSpec;
            uniform sampler2D _Emission; uniform float4 _Emission_ST;
            uniform float _EmissionPower;
            uniform sampler2D _LightRamp; uniform float4 _LightRamp_ST;
            uniform fixed _UseEmission;
            uniform float4 _EmissionColor;
            uniform fixed _UseFade;
            uniform float _FarFadeDistance;
            uniform float _NormalIntensity;
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
                float4 projPos : TEXCOORD5;
                LIGHTING_COORDS(6,7)
                UNITY_FOG_COORDS(8)
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
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.bitangentDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 _Normal_var = UnpackNormal(tex2D(_Normal,TRANSFORM_TEX(i.uv0, _Normal)));
                float3 node_2366 = (_Normal_var.rgb*_NormalIntensity);
                float3 normalLocal = node_2366;
                float3 normalDirection = normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
                float3 viewReflectDirection = reflect( -viewDirection, normalDirection );
                float2 sceneUVs = (i.projPos.xy / i.projPos.w);
                float node_6047_if_leA = step(_UseFade,1.0);
                float node_6047_if_leB = step(1.0,_UseFade);
                float node_6228 = 1.0;
                clip( BinaryDither2x2(lerp((node_6047_if_leA*node_6228)+(node_6047_if_leB*node_6228),(saturate((distance(i.posWorld.rgb,_WorldSpaceCameraPos)-_FadeDistance))*saturate(((5.0/distance(i.posWorld.rgb,_WorldSpaceCameraPos))*_FarFadeDistance))),node_6047_if_leA*node_6047_if_leB) - 1.5, sceneUVs) );
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
        Pass {
            Name "ShadowCaster"
            Tags {
                "LightMode"="ShadowCaster"
            }
            Offset 1, 1
            Cull Back
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_SHADOWCASTER
            #define _GLOSSYENV 1
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "UnityPBSLighting.cginc"
            #include "UnityStandardBRDF.cginc"
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
            uniform float _FadeDistance;
            uniform fixed _UseFade;
            uniform float _FarFadeDistance;
            struct VertexInput {
                float4 vertex : POSITION;
            };
            struct VertexOutput {
                V2F_SHADOW_CASTER;
                float4 posWorld : TEXCOORD1;
                float4 projPos : TEXCOORD2;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos( v.vertex );
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                float2 sceneUVs = (i.projPos.xy / i.projPos.w);
                float node_6047_if_leA = step(_UseFade,1.0);
                float node_6047_if_leB = step(1.0,_UseFade);
                float node_6228 = 1.0;
                clip( BinaryDither2x2(lerp((node_6047_if_leA*node_6228)+(node_6047_if_leB*node_6228),(saturate((distance(i.posWorld.rgb,_WorldSpaceCameraPos)-_FadeDistance))*saturate(((5.0/distance(i.posWorld.rgb,_WorldSpaceCameraPos))*_FarFadeDistance))),node_6047_if_leA*node_6047_if_leB) - 1.5, sceneUVs) );
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Standard"
    CustomEditor "AdvancedCelShader"
}
