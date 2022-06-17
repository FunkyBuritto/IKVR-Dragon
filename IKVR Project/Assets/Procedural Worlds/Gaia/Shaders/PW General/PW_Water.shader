Shader "PWS/PW_Water"
{
    Properties
    {
		_AmbientColor ("AmbientColor", Color) 									= (0,0,0,0)
		_PW_MainLightColor ("Main Light Color", Color) 							= (1,1,1,1)
		_PW_MainLightSpecular ("Main Light Spec", Color) 						= (0.1,0.1,0.1,1)
		_PW_MainLightDir ("Main Light Dir", Vector ) 							= (0,-1,0,0) 
		_PW_MainLightIntensity("Main Light Intensity", float)					= 1.0       	
		_Metallic ("Metallic", Range (0, 1)) 									= 0.1
		_Smoothness ("Smoothness", Range (0, 1)) 								= 0.8
//        [NoScaleOffset]_ReflectionTex ("Reflection Texture", 2D) 				= "white" {}
		_ReflectionColor ("Reflection Color", Color) 							= (0.1,0.12,0.17,1)
//		_ReflectionDistortion ("Reflection Distortion", Range (0, 16.0)) 		= 1.2
//		_ReflectionStrength("Reflection Strength", Range (0, 1.0))				= 0.8
		[KeywordEnum(Off,On)] _PW_SSS("Translucent", float)  					= 0
		_PW_SSSPower( "Power", Range (0.01, 8 ) ) 	= 1
		_PW_SSSDistortion( "Distortion", Range (0.001, 1 ) ) 					= 0.5
        _PW_SSSTint ("Tint", Color) 										   	= (1,1,1,1)
        [NoScaleOffset]_WaterDepthRamp ("Color Depth Ramp", 2D) 				= "white" {}

		[NoScaleOffset][Normal] _NormalLayer0 ("Normal Layer 0", 2D) 			= "bump" {}	
		_NormalLayer0Scale ("NormalLayer0 Scale", Range ( 0.0, 3.0 ) ) 			= 1.0
		[NoScaleOffset][Normal] _NormalLayer1 ("Normal Layer 1", 2D)			= "bump" {}	
		_NormalLayer1Scale ("NormalLayer1 Scale", Range ( 0.0, 3.0 ) ) 			= 1.0
		[NoScaleOffset][Normal] _NormalLayer2 ("Normal Distance", 2D)			= "bump" {}	
		_NormalLayer2Scale ("Normal Distance Scale", Range ( 0.0, 3.0 ) ) 		= 0.5
		[NoScaleOffset] _FoamTex ("Foam texture", 2D) 							= "white" {}                 	
		_NormalFadeMap ("Normal Fade Map", 2D ) 								= "white" {} 
		_NormalTile ("Water Tiling", Float ) 									= 128
		_NormalFadeStart ("Fade Start", Float )									= 64  
		_NormalFadeDistance("Fade Distance", Float )							= 512
		_NormalMoveScale("Normal Move Scale", Range (0.0001, 0.0005) )			= 0.0003
		_FoamTexTile ("Foam Tiling", Float ) 									= 256
		_FoamDepth ("Foam Height", Range (0, 16.0)) 							= 0.15
		_FoamStrength ("Foam Strength", Range (0, 2.0)) 						= 0.6

		_TransparentMin  ("Min Transparency", Range (0, 1.0)) 					= 0.45
		_TransparentDepth ("Transparent Depth", Range (0, 32.0)) 				= 8.0

		_WaveShoreClamp ("Shore Movment", Range ( 0.0, 1.0 ) ) 					= 0.1
		_WaveLength ("Wavelength", Float ) 										= 64
		_WaveSteepness ("Wavelength", Float ) 									= 0.3
		_WaveSpeed ("Wave Speed", Float ) 										= 0.4
		_WaveDirection ("Wave Direction", Vector ) 								= (1,0,0,0)        	
		_WaveDirGlobal ("WaveDirToggle", Float )								= 1  
		_WaveBackwashToggle ("Wave Backwash Toggle", Float )					= 1  
		_WavePeakToggle ("Wave Peak Toggle", Float )							= 1  
   		_PW_SrcBlend 			("", float) 										= 1
   		_PW_DstBlend 			("", float)											= 0

		_EdgeWaterColor ("UnderWater Color", Color) 							= (0.1,0.1,0.2,1)
		_EdgeWaterDist  ("Edge Fade Distance", Float )							= 5  
//        [NoScaleOffset]_ScreenRender ("HeightMap", 2D) 						= "white" {}
    }

	SubShader
    {
        Tags {"Queue" = "Transparent-2" "RenderType"="Transparent" }

        Pass
        {
			//ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile_fog
	        #pragma target 3.5

			#pragma multi_compile __ _PW_MC_REFRACTION_ON
			#pragma shader_feature_local _PW_SF_SSS_ON
//			#pragma shader_feature_local _PW_SF_WAVEDIR_GLOBAL_ON
//			#pragma shader_feature_local _PW_SF_WAVE_PEAK_ON
//			#pragma shader_feature_local _PW_SF_WAVE_BACKWASH_ON

            #include "UnityCG.cginc"
			#include "UnityPBSLighting.cginc"
			#include "PW_GeneralVars.cginc"
			#include "PW_GeneralFuncs.cginc"
			#include "PW_Water_Vars.cginc"
			#include "PW_Water_Funcs.cginc"

			PWSurface surface;

            struct appdata
            {
				float4 vertex 	: POSITION;
				float2 uv 		: TEXCOORD0;
				float3 normal   : NORMAL;
				float4 tangent  : TANGENT;
            };

            struct v2f
            {
				float4 vertex 		: SV_POSITION;
				float2 foamuv 		: TEXCOORD0;
				float4 screenPos	: TEXCOORD1;
				float2 bumpuv0 		: TEXCOORD2;
				float2 bumpuv1 		: TEXCOORD3;
				float2 bumpuv2 		: TEXCOORD4;
				float2 normalFadeUV	: TEXCOORD5;
				float3 viewDir 		: TEXCOORD6;
				float3 worldPos 	: TEXCOORD7;
				float3 worldNormal  : TEXCOORD13;
				half3 tspace0 		: TEXCOORD8; 
				half3 tspace1 		: TEXCOORD9; 
				half3 tspace2 		: TEXCOORD10;
                UNITY_FOG_COORDS(11)
            	half edgefade 		: TEXCOORD12;
            };
	
			//#define _PW_MC_REFRACTION_ON


			//-----------------------------------------------------------------
			inline void EdgeBlends_half ( in float i_sceneDepth, in float3 i_worldPosition, in float3 i_worldNormal, in float4 i_screenPosition, in float3 i_cameraDirection, in float i_cameraFarPlane, in float i_foamDepth, in float i_transparentDepth, in float i_shorelineEdge, out float o_shorelineAmount, out float o_foamAmount, out float o_underwaterAmount, out float o_edgeBlend )
			{
				half diff = i_sceneDepth - i_screenPosition.w;

				half3 dir 			= normalize(i_cameraDirection );
				half dotProd 		= 1.0 - max ( dot ( i_worldNormal, dir ), 0 );
				half foamDepth     = i_foamDepth * ( dotProd + 1 );

				o_shorelineAmount 	= min ( 1.0 - saturate ( diff / ( foamDepth * 0.2 ) ), 0.6 );
				o_foamAmount 		= 1.0 - saturate ( diff / foamDepth );
				o_underwaterAmount 	= 1.0 - saturate (exp2(-diff * i_transparentDepth));
				o_edgeBlend 		= saturate ( diff / i_shorelineEdge );
			}

            v2f vert (appdata v)
            {
                v2f o;

				o = (v2f)0;

				float4 screenPos 		= ComputeScreenPos(UnityObjectToClipPos(v.vertex));
				float sceneZ 			= LinearEyeDepth (tex2Dlod(_CameraDepthTexture, float4(screenPos.xy / screenPos.w, 0.0, 0.0)).r);
				float objectZ 			= screenPos.w;
				float shoreClamp 		= clamp ( saturate( (sceneZ - objectZ) / 32 ) + _WaveShoreClamp, 0.0, 1.0 );    


				float2 worldUVScale;
				worldUVScale.x = PW_DEFAULT_TERRAIN_SCALE * PW_GLOBAL_SCALE;
				worldUVScale.y = PW_DEFAULT_TERRAIN_SCALE * PW_GLOBAL_SCALE;

				

				// before the displacment
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				float3 worldPos = mul ((float3x3)unity_ObjectToWorld, v.vertex.xyz );

				float3 v0 = worldPos;
				float2 waveDir;

				waveDir = _WaveDirection.xz;

				float steepness = _WaveSteepness;
				float waveLength = _WaveLength * PW_DEFAULT_TERRAIN_SCALE * PW_GLOBAL_SCALE;

				v0.y += EchoWave ( v0, waveDir, waveLength, _WaveSpeed, steepness, shoreClamp );
				float2 perp;
				perp.x = waveDir.y;
				perp.y = -waveDir.x;

				v0.y += EchoWave ( v0, perp, waveLength * 0.25, -_WaveSpeed, steepness * 0.66, shoreClamp );

				waveDir = normalize ( ( perp + waveDir ) * 0.5 );
				v0.y += EchoWave ( v0, waveDir, waveLength, _WaveSpeed, steepness * 0.33, shoreClamp );

				v.vertex.xyz = mul ((float3x3)unity_WorldToObject, v0 );

				o.screenPos = ComputeScreenPos( UnityObjectToClipPos ( v.vertex ) );

				float3 wNormal = UnityObjectToWorldNormal(v.normal);
				o.worldPos = mul ( unity_ObjectToWorld, v.vertex );

				float2 worldMapUV	= float2 ( 1.0, 1.0 ) - ( ( o.worldPos.xz * worldUVScale ) + half2 ( 0.5, 0.5 ) );
				
				float timeScale = _Time.x * ( _WaveSpeed * 0.00033 );

				float2 t; 
				t.x = waveDir.x * timeScale;
				t.y = waveDir.y * timeScale;

				o.bumpuv0 = ( worldMapUV - t ) * _NormalTile;
				o.bumpuv1 = ( worldMapUV + t ) * _NormalTile * 0.333;
				o.bumpuv2 = worldMapUV * _NormalTile * 0.0333;

				o.normalFadeUV 	= worldMapUV;

				o.foamuv 		= _FoamTexTile * worldMapUV + float2 ( _SinTime.z*0.1, _SinTime.x*0.1);
				o.vertex 		= UnityObjectToClipPos(v.vertex);

				//float4 tangent;
                float4 wTangent;
                wTangent.xyz    = UnityObjectToWorldDir ( v.tangent );
                wTangent.w		= -1;

                half  tangentSign 	= v.tangent.w * unity_WorldTransformParams.w;
                half3 wBitangent 	= cross ( wNormal, wTangent )  * tangentSign;

                o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
                o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
                o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);

                UNITY_TRANSFER_FOG(o,o.vertex);

				half3 eViewDir = o.worldPos.xyz - _WorldSpaceCameraPos.xyz;
				half eViewdot = saturate(dot(normalize(-eViewDir), half3(0,1,0)));
				o.edgefade = saturate((1-saturate(length(o.worldPos.xz - unity_ObjectToWorld._m03_m23) * _ProjectionParams.w))* (16 * eViewdot * eViewdot + 2));
				o.edgefade = smoothstep(0,1,o.edgefade);
				
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
				surface = (PWSurface)0;

/*
				half existingDepth 				= tex2Dproj( _CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)).r;
				half existingDepthLinear 		= LinearEyeDepth(existingDepth);
				half depthDifference 			= existingDepthLinear - i.screenPos.w;
				half waterDepthDifference 		= saturate ( depthDifference / _TransparentDepth );
				half edgeBlend 					= 1.0 - clamp ( waterDepthDifference / 0.05, 0, 1 );
				half foamAmt;

				// try to keep foam from changing with view angle
				foamAmt = min ( 1.0 - clamp ( waterDepthDifference / _FoamDepth, 0, 1 ), 1 );
				*/

				//======================================================================================================
				half sceneDepth 				= tex2Dproj( _CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)).r;
				half eyeDepth 					= LinearEyeDepth(sceneDepth);
				half edgeBlend;
				half foamAmt;
				half underwaterAmount;
				half shorelineEdge = 0.05;
				half shorelineAmount = 0;



				EdgeBlends_half ( eyeDepth, i.worldPos, i.worldNormal, i.screenPos, _WorldSpaceCameraPos.xyz, _ProjectionParams.z, _FoamDepth, _TransparentDepth, shorelineEdge, shorelineAmount, foamAmt, underwaterAmount, edgeBlend );

				// Normal mapping
				half scaleNormal = 1.0 - foamAmt;

				half4 normalFadeMap	= tex2D( _NormalFadeMap, i.normalFadeUV );

				half4 dist2Cam = distance ( _WorldSpaceCameraPos.xyz, i.worldPos );

				half dist 		= dist2Cam - _NormalFadeStart;
				half distFade 	= clamp ( dist / _NormalFadeDistance, 0.0, 1.0 );

				half3 bump1 = UnpackScaleNormal ( tex2D( _NormalLayer0, i.bumpuv0 ),_NormalLayer0Scale * scaleNormal * normalFadeMap.r  );
				half3 bump2 = UnpackScaleNormal ( tex2D( _NormalLayer1, i.bumpuv1 ), _NormalLayer1Scale * scaleNormal * normalFadeMap.g );
				half3 bump3 = UnpackScaleNormal ( tex2D( _NormalLayer2, i.bumpuv2 ), _NormalLayer2Scale * normalFadeMap.b );

				half3 bump  = BlendUnpacked( bump1, bump2 );

				bump 		= normalize ( lerp ( bump, bump3, distFade ) );

				half3 worldNormal;
                worldNormal.x = dot ( i.tspace0, bump );
                worldNormal.y = dot ( i.tspace1, bump );
                worldNormal.z = dot ( i.tspace2, bump );

                half3 worldViewDir 			= normalize(UnityWorldSpaceViewDir(i.worldPos));

#ifdef _PW_MC_REFRACTION_ON
				half2 uvOffset 				= bump.xy * 0.1;
				half2 uv 					= (i.screenPos.xy + uvOffset) / i.screenPos.w;
				half3 underWaterColor	 	= tex2D( _CameraOpaqueTexture, uv ).rgb;
#else
				half3 underWaterColor	 	= half3(1,1,1);
#endif
				surface.depthColor 			= tex2D( _WaterDepthRamp, half2( underwaterAmount, 0.5 ) );

            	// Foam Calculations
				half2  foamDistortUV		= bump.xy * 0.2;
				half4  foamColor 			= tex2D( _FoamTex, i.foamuv + foamDistortUV );
				foamColor.a 				= dot(foamColor.rgb,half3(0.333,0.333,0.333));

				foamColor.a = saturate(( foamColor.a + shorelineAmount, 1.0 ) * _FoamStrength);
				surface.finalRGBA.rgb = min ( surface.finalRGBA.rgb + ( foamColor.rgb * foamAmt * foamColor.a ), 1.0 );
				surface.finalRGBA.rgb = lerp ( surface.finalRGBA.rgb, min ( foamColor.rgb+half3(0.2,0.2,0.2), 1.0 ), shorelineAmount);
            	surface.finalRGBA.rgb += surface.depthColor * underwaterAmount;
            	surface.finalRGBA.rgb = saturate(surface.finalRGBA.rgb);
        

            	float perceptualRoughness = 1 - lerp(_Smoothness,0.1,foamColor.a * foamAmt);
            	
            	surface.roughness = perceptualRoughness * perceptualRoughness;

            	surface.debug = perceptualRoughness;
            	
            	saturate(surface.finalRGBA.rgb);

				half peakFade = 0;

#ifdef _PW_SF_WAVE_PEAK_ON
				half peak = pow ( saturate ( worldNormal.y ), 32 );

				half peakDist 	= length ( max ( i.worldPos.y, 0 ) - 0.1 );
				peakFade 		= clamp ( peakDist / 8, 0.0, 1.0 );

				surface.finalRGBA.rgb += foamColor * peakFade * peak * 2;
#endif

#ifdef _PW_SF_WAVE_BACKWASH_ON
				float3 waveDir;

		#ifdef _PW_SF_WAVEDIR_GLOBAL_ON
				waveDir = _PW_Global_WindDirection.xyz;
		#else
				waveDir = _WaveDirection.xyz;
		#endif

				waveDir.y = 0;

				half backWave = saturate ( -dot ( worldNormal, waveDir ) );

#ifndef _PW_MC_REFRACTION_ON
				surface.finalRGBA.rgb = lerp (surface.finalRGBA.rgb, foamColor, backWave );
#endif

#endif

            	
				SurfacePBR ( surface, foamAmt, _Metallic, worldNormal, worldViewDir, i.screenPos, bump1.xy );
				PBRDirLighting ( surface, i.worldPos, worldNormal, worldViewDir );


#ifdef _PW_MC_REFRACTION_ON

            	surface.finalRGBA.rgb   +=    surface.reflection;
            	surface.finalRGBA.rgb   +=    underWaterColor * surface.depthColor * (1-underwaterAmount);
            	surface.finalRGBA.a 	= edgeBlend;
            	
#else
				surface.finalRGBA.rgb 		= surface.depthColor; // Missing colorDepth ???;
				surface.finalRGBA.a 		= 1;//min ( waterDepthDifference+_TransparentMin + (foamAmt * foamColor.a), 1 ) * edgeBlend;
				surface.finalRGBA.rgb += surface.reflection;
#endif


				//surface.finalRGBA.rgb = underWaterColor;
				//surface.finalRGBA.rgb 		= WaterEdge ( surface.finalRGBA.rgb, i.worldPos, dist2Cam, 1 );

#ifdef _PW_MC_REFRACTION_ON
#endif

                UNITY_APPLY_FOG(i.fogCoord, surface.finalRGBA);

				/*
				half4 addLight = 0;

				Translucent_half ( colorDepth, 
						 i.worldNormal,
						 _PW_MainLightDir.xyz, 
						 _PW_MainLightColor.rgb, 
						 worldViewDir,
						 foamColor, 
						 peakFade,
						 _PW_SSSTint.rgb,
						 _PW_SSSPower,
						 _PW_SSSDistortion,
						 addLight
						 );
				*/

				//surface.finalRGBA.rgb += addLight;

            	half fadeDepthtest = eyeDepth < _ProjectionParams.z -1 ? 1 : 0;
            	i.edgefade = max (i.edgefade , fadeDepthtest);
				surface.finalRGBA.a *= edgeBlend * i.edgefade;

            	//surface.finalRGBA.rgb = surface.debug;
            	
                return ( surface.finalRGBA );
            	//return float4(surface.depthColor,1);
			//float4 col;
            //col.a = 1;
            //
            //col.rgb = surface.depthColor * underWaterColor  * (1-underwaterAmount);
            //
            //return col;
            }
            ENDCG
        }

    }
	CustomEditor "PW_Water_MaterialEditor"
}



