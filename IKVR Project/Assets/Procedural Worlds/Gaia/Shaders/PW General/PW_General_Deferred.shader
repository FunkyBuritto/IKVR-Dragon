﻿Shader "PWS/PW_General_Deferred"
{
    Properties
    {
		_PW_GlobalControl ( "", float )											= 1
   		_PW_ShaderMode ( "Mode", float ) 										= 0
   		_PW_TerrainSizeX ( "", float ) 											= 2048
   		_PW_TerrainSizeZ ( "", float ) 											= 2048

		[Enum(UnityEngine.Rendering.CullMode)]_CullMode("Cull Mode", Int) 		= 0
		[KeywordEnum(Off,On)] _AlphaTest("Cutout", Float) 						= 0
        [HDR]_Color ("Color", Color) 											= (1,1,1,1)
        [HDR]_MainTex ("Albedo (RGB)", 2D) 										= "white" {}
		_Cutoff ("Alpha Cutoff", Range(0,1)) 									= 0.2
		[NoScaleOffset][Normal]_BumpMap ("", 2D)  								= "bump" {}
		_BumpMapScale ( "BumpMap Scale", Range( 0, 3 ) ) 						= 1.0
		[NoScaleOffset]_MetallicGlossMap ("Metallic Map (RGBA)", 2D) 			= "white" {}
        _Glossiness ("Smoothness", Range(0,1)) 									= 0.5
        _Metallic ("Metallic", Range(0,1)) 										= 0.0
        _WrapLighting ("Wrap Lighting", Range(0,1)) 							= 0.0

		_AOPower ("Occlusion Power", Range(0,1))   								= 1.0
		_AOPowerExp ("Occlusion Power", Range(0,1))   					   		= 1.0
		_AOVertexMask ("Vertex Mask", Range(0,1))   				   			= 1.0

		_PW_WorldMap ("Variation Map", 2D)  		   							= "white" {}
   		_PW_WorldMapUVScale ( "Variation Scale", float ) 						= 1
        [HDR]_PW_WorldMapColorObject ("Color Object", Color) 					= (1,1,1,1)
   		[HDR]_PW_WorldMapColorCover0 ("Color Cover", Color) 					= (1,1,1,1)
   		[HDR]_PW_WorldMapColorCover1 ("Color Cover", Color) 					= (1,1,1,1)

		_PW_CoverLayer0 ("Image", 2D)  		   									= "white" {}
        _PW_CoverLayer0Color ("Color", Color) 									= (1,1,1,1)
		_PW_CoverLayer0Edge ( "Edge", Range( 0.01, 1 ) ) 						= 0.1
		_PW_CoverLayer0Tiling ( "Edge", float ) 								= 64
		_PW_CoverLayer0AlphaClamp ( "Add", Range( -1, 1 ) ) 		   			= 1.0
		_PW_CoverLayer0Normal ("Normal", 2D)  									= "bump" {}
		_PW_CoverLayer0NormalScale ( "Normal Scale", Range( 0, 3 ) ) 			= 1.0
        _PW_CoverLayer0Smoothness ("Smoothness", Range( 0, 1 ) ) 				= 0.75
        _PW_CoverLayer0Metallic ("Metallic", Range ( 0, 1 ) ) 	   				= 0.2
   		_PW_CoverLayer0Wrap ( "Amount", Range( 0, 1 ) ) 						= 0.0
   		_PW_CoverLayer0Progress ( "Amount", Range( 0, 1 ) ) 					= 0.0
   		_PW_CoverLayer0FadeStart ( "FadeStart", float ) 							= 0.0
   		_PW_CoverLayer0FadeDist ( "FadeDist", float ) 							= 0.0

		_PW_CoverLayer1 ("Image", 2D)  		   									= "white" {}
        _PW_CoverLayer1Color ("Color", Color) 									= (1,1,1,1)
		_PW_CoverLayer1Edge ( "Edge", Range( 0.01, 1 ) ) 						= 0.1
		_PW_CoverLayer1Tiling ( "Edge", float ) 								= 64
		_PW_CoverLayer1AlphaClamp ( "Add", Range( -1, 1 ) ) 						= 1.0
		_PW_CoverLayer1Normal ("Normal", 2D)  									= "bump" {}
		_PW_CoverLayer1NormalScale ( "Normal Scale", Range( 0, 3 ) ) 			= 1.0
        _PW_CoverLayer1Smoothness ("Smoothness", Range( 0, 1 ) ) 				= 0.75
        _PW_CoverLayer1Metallic ("Metallic", Range ( 0, 1 ) ) 	   				= 0.2
   		_PW_CoverLayer1Wrap ( "Amount", Range( 0, 1 ) ) 						= 0.0
   		_PW_CoverLayer1Progress ( "Amount", Range( 0, 1 ) ) 					= 0.0
   		_PW_CoverLayer1FadeStart ( "FadeStart", float ) 			   			= 0.0
   		_PW_CoverLayer1FadeDist ( "FadeDist", float ) 							= 0.0

		_PW_SSSPower( "Power", Range (0.01, 8 ) ) 	= 1
		_PW_SSSDistortion( "Distortion", Range (0.001, 1 ) ) 					= 0.5
        _PW_SSSTint ("Tint", Color) 										   	= (1,1,1,1)

		_PW_WindTreeWidthHeight("Normalization Scalers Width and Height", vector) = (4,8,0,0)
    	_PW_WindTreeFlex("(x) stem, (y) branch, (z) leaf",vector) 				= (0.8,1.15,0.1,0)
    	_PW_WindTreeFrequency ("(x) stem, (y) branch, (z) leaf",vector)			= (0.25,0.5,1.3,0)

   		_PW_Global_SeasonalTintAmount ( "Seaonal Tint", Range(0.0, 1.0 ) ) 			= 0.0
    	
    	//Keyword Toggles
    	[Toggle(_ALPHATEST_ON)] _ALPHATEST ("_ALPHATEST_ON ", Float) 				= 0.0
		[Toggle(_PW_SF_COVER_ON)] _PW_SF_COVER ("_PW_SF_COVER_ON ", Float) 			= 0.0
    	[Toggle(_PW_SF_SSS_ON)] _PW_SF_SSS ("_PW_SF_SSS_ON ", Float) 				= 0.0
    	[Toggle(_PW_SF_WIND_ON)] _PW_SF_WIND ("_PW_SF_WIND_ON ", Float) 				= 0.0
    	[Toggle(_PW_SF_WORLDMAP_ON)] _PW_SF_WORLDMAP ("_PW_SF_WORLDMAP_ON ", Float) = 0.0
    	[Toggle(_PW_SF_BILLBOARD_ON)] _PW_SF_BILLBOARD ("_PW_SF_BILLBOARD_ON ", Float) = 0.0
    	
    	[Enum(Gaia.ShaderUtilitys.ShaderIDs)] _PW_ShaderID ("Shader ID", Float) = 0
    }

    SubShader
    {
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry" }
		Cull [_CullMode]

        CGPROGRAM
		#pragma surface surf Standard fullforwardshadows keepalpha vertex:vert addshadow 
        #pragma target 3.5

		#pragma multi_compile __ LOD_FADE_CROSSFADE
		#pragma shader_feature_local _ALPHATEST_ON
		#pragma shader_feature_local _PW_SF_COVER_ON
		#pragma shader_feature_local _PW_SF_WIND_ON
		#pragma shader_feature_local _PW_SF_WORLDMAP_ON
        #pragma shader_feature_local _PW_SF_BILLBOARD_ON

		#define _PW_SF_GLOBAL_CONTROL_ON
        #define _PW_VERTEX_SHADOWS

		#include "UnityCG.cginc"
		#include "UnityPBSLighting.cginc"
		#include "PW_GeneralVars.cginc"
		#include "PW_GeneralFuncs.cginc"
		#include "PW_GeneralWind.cginc"

		struct Input
		{
			float2 uv_MainTex;
			float2 coverLayer0UV;
			float2 coverLayer1UV;
			float3 Normal;
			float3 worldNormal;
			float3 worldPos;
			float3 viewDir;
			fixed4 color : COLOR;
			fixed4 variationMap;
			float4 screenPosition;
			INTERNAL_DATA
		};

        UNITY_INSTANCING_BUFFER_START(Props)
		half4   _Color;
        UNITY_INSTANCING_BUFFER_END(Props)

		//-------------------------------------------------------------------------------------
		void vert ( inout appdata_full v, out Input data )
		{
			UNITY_INITIALIZE_OUTPUT(Input,data);

			float2 worldUVScale;

			#if defined(_PW_SF_COVER_ON) || defined (_PW_SF_WORLDMAP_ON)
			worldUVScale.x = PW_DEFAULT_TERRAIN_SCALE * PW_GLOBAL_SCALE;
			worldUVScale.y = PW_DEFAULT_TERRAIN_SCALE * PW_GLOBAL_SCALE;
			#endif

			CoverVertexUV ( worldUVScale, v.vertex, data.coverLayer0UV, data.coverLayer1UV );
			WindCalculations_float ( v.vertex.xyz, _PW_WindTreeWidthHeight, _PW_WindTreeFlex, _PW_WindTreeFrequency, v.vertex.xyz );

#if defined (_PW_SF_WORLDMAP_ON)
			half2 wmuv;
			WorldMapUV ( worldUVScale, _PW_WorldMapUVScale, wmuv );
			data.variationMap = tex2Dlod ( _PW_WorldMap, float4 ( wmuv, 0, 0 ) );
#endif

			data.screenPosition = ComputeScreenPos( UnityObjectToClipPos( v.vertex ) );
		}

		//=====================================================================
        void surf ( Input IN, inout SurfaceOutputStandard o)
        {
			EchoData e;

			e = (EchoData)0;

			half4 rgba 			= tex2D ( _MainTex, IN.uv_MainTex ) * UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

			half3 seasonColor = half3(1,1,1) - _PW_Global_SeasonalTint.rgb;

			o.Albedo 			= lerp ( rgba.rgb, rgba.rgb * seasonColor, _PW_Global_SeasonalTintAmount );
			o.Alpha 			= rgba.a;

#ifdef _ALPHATEST_ON
			clip ( o.Alpha - _Cutoff );
#endif

			o.Normal    		= UnpackScaleNormal ( tex2D ( _BumpMap, IN.uv_MainTex ), _BumpMapScale );

#ifdef _ALPHATEST_ON
			BackFaceCheck_half ( o.Normal, IN.viewDir, o.Normal );
#endif

			e.worldNormal 		= WorldNormalVector ( IN, o.Normal );

			half4 surfaceMap 	= tex2D ( _MetallicGlossMap, IN.uv_MainTex );
			CombineLocalGlobalClamp_half ( _Metallic * surfaceMap.r, _PW_Global_Metallic, o.Metallic );
			CombineLocalGlobalClamp_half ( _Glossiness * surfaceMap.a, _PW_Global_Smoothness, o.Smoothness );
			BlendVertexMaskAO_half ( _AOPower, _AOVertexMask, _AOPowerExp, IN.color.r, surfaceMap.g, o.Occlusion );
			e.thickness       = 1.0 - surfaceMap.b;

			WorldMapObject_half ( IN.variationMap, o.Albedo, _PW_WorldMapColorObject.rgb, o.Albedo );

#ifdef _PW_SF_COVER_ON
			half4 layer0_CoverRGBA 		= tex2D ( _PW_CoverLayer0, IN.coverLayer0UV ) * _PW_CoverLayer0Color;
			half3 layer0_CoverNormal 	= UnpackScaleNormal ( tex2D ( _PW_CoverLayer0Normal, IN.coverLayer0UV ), _PW_CoverLayer0NormalScale );

			half3 out_Albedo 		= half3(0,0,0);
			half3 out_Normal		= half3(0,0,0);
			half  out_Metallic		= 0;
			half  out_Smoothness	= 0;
			half4 out_SSS   		= half4(0,0,0,0);
			half  fadeStart;
			half  fadeDist;

			CombineLocalGlobal ( _PW_CoverLayer0FadeStart, _PW_Global_CoverLayer0FadeStart, fadeStart );
			CombineLocalGlobal ( _PW_CoverLayer0FadeDist, _PW_Global_CoverLayer0FadeDist, fadeDist );

			CoverSurface_half ( 
						   o.Albedo, 
						   o.Normal, 
						   o.Metallic, 
						   o.Smoothness, 
						   out_SSS,
						   IN.worldPos.y,
						   e.worldNormal.y,
						   layer0_CoverRGBA, 
						   layer0_CoverNormal, 
						   _PW_CoverLayer0Edge,
						   _PW_Global_CoverLayer0Progress,
						   _PW_CoverLayer0Progress,
						   _PW_CoverLayer0Metallic, 
						   _PW_CoverLayer0Smoothness, 
						   _PW_CoverLayer0AlphaClamp, 
						   _PW_CoverLayer0Wrap,
						   fadeStart,
						   fadeDist,
						   _PW_WorldMapColorCover0.rgb,

						   IN.variationMap.g,

						   out_Albedo,      
						   out_Normal,      
						   out_Metallic,    
						   out_Smoothness,  
						   out_SSS );

			half4 layer1_CoverRGBA 		= tex2D ( _PW_CoverLayer1, IN.coverLayer1UV ) * _PW_CoverLayer1Color;
			half3 layer1_CoverNormal 	= UnpackScaleNormal ( tex2D ( _PW_CoverLayer1Normal, IN.coverLayer1UV ), _PW_CoverLayer1NormalScale );

			CombineLocalGlobal ( _PW_CoverLayer1FadeStart, _PW_Global_CoverLayer1FadeStart, fadeStart );
			CombineLocalGlobal ( _PW_CoverLayer1FadeDist, _PW_Global_CoverLayer1FadeDist, fadeDist );

			CoverSurface_half ( 
						   out_Albedo, 
						   out_Normal, 
						   out_Metallic, 
						   out_Smoothness, 
						   out_SSS,
						   IN.worldPos.y,
						   e.worldNormal.y,
						   layer1_CoverRGBA, 
						   layer1_CoverNormal, 
						   _PW_CoverLayer1Edge,
						   _PW_Global_CoverLayer1Progress,
						   _PW_CoverLayer1Progress,
						   _PW_CoverLayer1Metallic, 
						   _PW_CoverLayer1Smoothness, 
						   _PW_CoverLayer1AlphaClamp, 
						   _PW_CoverLayer1Wrap,
						   fadeStart,
						   fadeDist,
						   _PW_WorldMapColorCover1.rgb,

						   IN.variationMap.g,

						   o.Albedo,      
						   o.Normal,      
						   o.Metallic,    
						   o.Smoothness,  
						   e.coverRGBA
						   );
#endif

			half4 addLight = 0;
			half3 viewDir = normalize ( _WorldSpaceCameraPos.xyz - IN.worldPos.xyz );

#ifdef LOD_FADE_CROSSFADE
			float vpos = IN.screenPosition.xy / IN.screenPosition.w * _ScreenParams.xy;
			UnityApplyDitherCrossFade ( vpos );
			//DitherCrossFade ( vpos );
#endif

        }
        ENDCG

	}

	FallBack "Diffuse"

	CustomEditor "PW_General_MaterialEditor"
}


