#ifndef PW_GENERALFUNCS
#define PW_GENERALFUNCS

#define HALF_MIN 6.103515625e-5

#define PW_DEFAULT_TERRAIN_SIZE 2048.0
#define PW_DEFAULT_TERRAIN_SCALE ( 1.0 / PW_DEFAULT_TERRAIN_SIZE )
#define PW_GLOBAL_SCALE (_PW_Global_TerrainScale+1.0)

#define _PW_GD_NORMAL_ON
#define _PW_GD_METALLICMAP_ON
#define _PW_SF_GLOBAL_CONTROL_ON

///SCOTTFIND add keywords to graph stuff
/// ADD COVER and WORLDMAP keywords
inline void PW_WorldScaleGraph_half ( in half i_global_TerrainScale, out half2 o_worldScale )
{
//#if defined(_PW_SF_COVER_ON) || defined (_PW_SF_WORLDMAP_ON)
	float tscale = i_global_TerrainScale + 1.0;
	o_worldScale.x = PW_DEFAULT_TERRAIN_SCALE * tscale;
	o_worldScale.y = PW_DEFAULT_TERRAIN_SCALE * tscale;
//#else
//	o_worldScale = half2(1,1);
//#endif
}

//=============================================================================
inline void CoverVertexUV ( in float2 i_worldUVScale, in float4 i_vertex, inout float2 i_coverLayer0UV,inout float2 i_coverLayer1UV )
{
#if defined(_PW_SF_COVER_ON)

	float4 worldPosition = mul ( UNITY_MATRIX_M, i_vertex );

	half2 guv = ( ( worldPosition.xz * i_worldUVScale ) - float2 ( 0.5, 0.5 ) );

	i_coverLayer0UV = guv * _PW_CoverLayer0Tiling;
	i_coverLayer1UV = guv * _PW_CoverLayer1Tiling;

#endif
}

inline void AlphaTestSRP_float (in bool i_vface, in float i_cutoff, out float o_out )
{	
	float alphaTest = 0;
	#if defined(_ALPHATEST_ON)
		alphaTest += i_cutoff;
	#endif

	#if defined(_PW_SF_BILLBOARD_ON)
		#ifndef SHADERPASS_SHADOWCASTER
			alphaTest += (i_vface) ? 0 : 1; 	
		#endif
	#endif

	o_out = alphaTest;
}

inline void AlphaTestSRP_half (in bool i_vface, in half i_cutoff, out half o_out )
{	
	half alphaTest = 0;
	#if defined(_ALPHATEST_ON)
	alphaTest += i_cutoff;
	#endif

	#if defined(_PW_SF_BILLBOARD_ON)
		#ifndef SHADERPASS_SHADOWCASTER
			alphaTest += (i_vface) ? 0 : 1; 	
		#endif
	#endif

	o_out = alphaTest;
}

//=============================================================================
inline void WorldMapObject_half ( in half4 i_worldMap, in half3 i_albedo, in half3 i_worldMapColorObject, out half3 o_albedo )
{
#ifdef _PW_SF_WORLDMAP_ON
	half3 worldMapColor = i_albedo * i_worldMapColorObject;
	o_albedo 			= lerp ( i_albedo, worldMapColor, i_worldMap.r );
#else
	o_albedo = i_albedo;
#endif
}

//=============================================================================
inline void WorldMapUV ( in half2 i_worldUVScale, in half i_worldMapUVScale, out half2 o_uv )
{
	half4 worldPosition	= mul ( UNITY_MATRIX_M, float4(0,0,0,1) );
	half2 worldMapUV 	= ( ( worldPosition.xz * i_worldUVScale ) + float2 ( 0.5, 0.5 ) ) * i_worldMapUVScale;

	worldMapUV.x = abs ( fmod ( worldMapUV.x, 1.0 ) );
	worldMapUV.y = 1.0 - abs ( fmod ( worldMapUV.y, 1.0 ) );

	o_uv = worldMapUV;
}

/*
//=============================================================================
inline void WorldMapVertex ( in float2 i_worldUVScale, inout half4 i_variationMap )
{
#ifdef _PW_SF_WORLDMAP_ON
	float4 worldPosition	= mul ( UNITY_MATRIX_M, float4(0,0,0,1) );
	float2 worldMapUV 		= ( ( worldPosition.xz * i_worldUVScale ) + float2 ( 0.5, 0.5 ) ) * _PW_WorldMapUVScale;

	worldMapUV.x = abs ( fmod ( worldMapUV.x, 1.0 ) );
	worldMapUV.y = 1.0 - abs ( fmod ( worldMapUV.y, 1.0 ) );

	i_variationMap 			= tex2Dlod ( __PW_WorldMapColorCover1.rgbPW_WorldMap, float4 ( worldMapUV, 0, 0 ) );
#endif
}
*/


//=============================================================================
inline void CoverUVNode_half ( in half3 i_worldPos ,in half2 i_worldUVScale, in half i_userTiling, out half2 i_coverUV )
{
#if defined(_PW_SF_COVER_ON)
	i_coverUV = ( ( i_worldPos.xz * i_worldUVScale ) - float2 ( 0.5, 0.5 ) ) * i_userTiling;
#else
	i_coverUV = half2(0,0);
#endif
}

//-------------------------------------------------------------------------------------
inline void SeasonalTint_half ( in half3 i_albedo, in half3 i_globalTint, in half i_amount, out half3 o_albedo )
{
	half3 seasonColor = half3(1,1,1) - i_globalTint;

	o_albedo 			= lerp ( i_albedo, i_albedo * seasonColor, i_amount );
}

//-------------------------------------------------------------------------------------
inline void BackFaceCheck_half ( in half3 i_normal, in half3 i_viewDir, out half3 o_normal )
{
	o_normal = dot(i_viewDir, float3(0, 0, 1)) >= 0.0 ? i_normal : -i_normal;


/*
	half dotNV = dot ( i_viewDir, half3(0, 0, 1) );

	half facing = sign ( dotNV );

	facing = lerp ( 1.0, facing, abs(facing) );

	o_normal = i_normal * facing;
*/
}

//-------------------------------------------------------------------------------------
inline void BlendVertexMaskAO_half ( in half i_power, in half i_vertexMask, in half i_expPower, in half i_vertexAO, in half i_mapAO, out half o_ao )
{
	half ao = abs ( lerp ( i_vertexAO * i_mapAO, i_mapAO, i_vertexMask ) );
	half val = pow ( ao, i_expPower );

	o_ao = clamp ( ( 1.0 - i_power ) + val, 0, 1 );
}

//-----------------------------------------------------------------------------
inline void CombineLocalGlobalClamp_half ( in half i_valLocal, in half i_valGlobal, out half o_result )
{
	#if defined(_PW_SF_GLOBAL_CONTROL_ON)
		o_result = clamp ( i_valLocal + i_valGlobal, 0.0, 1.0 );
	#else
		o_result = i_valLocal;
	#endif
}

//-----------------------------------------------------------------------------
inline void CombineLocalGlobal ( in half i_valLocal, in half i_valGlobal, out half o_val )
{
	half result;

	#if defined(_PW_SF_GLOBAL_CONTROL_ON)
		result = i_valLocal + i_valGlobal;
	#else
		result = i_valLocal;
	#endif

	o_val = result;
}

inline void CombineLocalGlobal ( in float4 i_valLocal, in float4 i_valGlobal, out float4 o_val )
{
	float4 result;

	#if defined(_PW_SF_GLOBAL_CONTROL_ON)
	result = i_valLocal + i_valGlobal;
	#else
	result = i_valLocal;
	#endif

	o_val = result;
}

//=============================================================================
inline void PW_ScaleNormal_half ( in half4 i_packedNormal, in half i_scale, in bool i_facing, out half3 o_normal )
{
	half facing = i_facing ? 1 : -1;

	o_normal = i_packedNormal.xyz;

	//o_normal.xy *= i_scale;

	o_normal.z *= facing;
}




/*
	Variation Map
	R = object amount
	G = cover vary amount layer 0
	B = cover vary amount layer 1
	A = cover layer 1 fade 0 -1
*/


//=============================================================================
inline void VaryationObject_half ( in half3 i_albedo, in half4 i_variationMap, in half3 i_variationTint, out half3 o_albedo )
{
#ifdef _PW_SF_WORLDMAP_ON
	half3 worldMapColor = i_albedo * i_variationTint;

	o_albedo 		= lerp ( i_albedo, worldMapColor, i_variationMap.r );
#endif
}

/*
//=============================================================================
inline void WorldMapCover ( in half4 i_worldMap, inout half3 i_layer0, inout half3 i_layer1, inout half i_progress )
{
#ifdef _PW_SF_WORLDMAP_ON

	half3 worldMapColor = i_layer0 * _PW_WorldMapColorCover0.rgb;
	i_layer0 		= lerp ( i_layer0, worldMapColor, i_worldMap.g );

	worldMapColor = i_layer1 * _PW_WorldMapColorCover1.rgb;
	i_layer1 		= lerp ( i_layer1, worldMapColor, i_worldMap.b );

	i_progress *= i_worldMap.a;

#endif
}
*/

//=============================================================================
inline void DitherCrossFade ( half2 i_vpos )
{
#ifdef LOD_FADE_CROSSFADE
	//UnityApplyDitherCrossFade(i_vpos);
	float4x4 ditherMatrix =
		{  1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
		  13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
		   4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
		  16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
		};

	half mask = ditherMatrix [i_vpos.x%4][i_vpos.y%4];

	float sgn = sign(unity_LODFade.x);
	sgn = lerp ( 1.0, sgn, abs(sgn) );

	clip ( unity_LODFade.x - mask * sgn );
#endif
}

inline void NrmAdd_float(in float3 i_nrmA, in float3 i_nrmB, out float3 o_nrm)
{
	o_nrm = normalize(float3(i_nrmA.xy + i_nrmB.xy, i_nrmA.z));
}

inline void NrmAdd_half(in half3 i_nrmA, in half3 i_nrmB, out half3 o_nrm)
{
	o_nrm =  normalize(half3(i_nrmA.xy + i_nrmB.xy, i_nrmA.z));
}

inline void WindMovement_float ( in float3 i_vertexPos, in float i_windSpeed, in float i_windGust, in float3 i_windDirection, out float3 o_vertexPos )
{
	o_vertexPos = i_vertexPos;
}

//=============================================================================
inline void Translucent_half ( 
						   in half3 i_albedo,
						   in half3 i_worldNormal, 
						   in half3 i_lightDir,
						   in half3 i_lightColor, 
						   in half3 i_viewDir,
						   in half4 i_coverRGBA, 
						   in half i_thickness, 

						   in half3 i_user_Tint,
						   in half i_user_Power,
						   in half i_user_Distortion,

						   out half4 o_addLight 
						   )
{
	#ifdef _PW_SF_SSS_ON
	half3 	color;
	half    thickness;

	#ifdef _PW_SF_COVER_ON
		color 		= lerp ( i_albedo, i_coverRGBA.rgb, i_coverRGBA.a ) * i_user_Tint;
		thickness 	= max ( i_thickness - ( i_coverRGBA.a * 0.25 ), 0 );
	#else
		color 		= i_albedo * i_user_Tint;
		thickness 	= i_thickness;
	#endif

	half3 transLightDir	= i_lightDir + i_worldNormal * i_user_Distortion;
	half transDot 		= max ( 0, dot ( i_viewDir, -transLightDir ) );

	half transLight = ( transDot * transDot * transDot ) * i_user_Power * thickness;

	o_addLight 			= half4 ( color * i_lightColor * transLight, 1 );
	#else
	o_addLight = half4(0,0,0,0);
	#endif
}

//=============================================================================
// does SSS and Translucent/SSS
inline void AddLighting_half ( 
						   in half3 i_albedo,
						   in half3 i_worldNormal, 
						   in half3 i_lightDir,
						   in half3 i_lightColor, 
						   in half3 i_viewDir,
						   in half4 i_coverRGBA, 
						   in half i_thickness, 

						   in half3 i_user_Tint,
						   in half i_user_Power,
						   in half i_user_Distortion,

						   in half  i_wrapLighting,

						   out half4 o_addLight 
						   )
{
	#ifdef _PW_SF_SSS_ON
	half3 	color;
	half    thickness;
	
	#ifdef _PW_SF_COVER_ON
		color 		= lerp ( i_albedo, i_coverRGBA.rgb, i_coverRGBA.a ) * i_user_Tint;
		thickness 	= max ( i_thickness - ( i_coverRGBA.a * 0.25 ), 0 );
	#else
		color 		= i_albedo * i_user_Tint;
		thickness 	= i_thickness;
	#endif

	half3 transLightDir	= i_lightDir + i_worldNormal * i_user_Distortion;
	half transDot 		= max ( 0, dot ( i_viewDir, -transLightDir ) );

	half transLight = pow( transDot, 16 ) * i_user_Power * thickness;

	o_addLight 			= half4 ( color * i_lightColor * transLight, 1 );

	//half fakeShadow = clamp ( ( i_vertexShade - 0.5 ) * 2 + 0.6, 0, 1 );
	//o_addLight *= fakeShadow;

	#else
	o_addLight = half4(0,0,0,0);

	#endif
	
// i_wraping should be a uniform
	if ( i_wrapLighting > 0.0 )
	{
		half dotP = 1.0 - abs ( dot ( i_worldNormal, i_lightDir ) );
		o_addLight.rgb += i_lightColor * i_albedo * dotP * dotP * dotP * i_wrapLighting;
	}
}

//=============================================================================
inline void CoverSurface_half ( 
						  in half3	i_albedo,
						  in half3 	i_normal,
						  in half 	i_metallic,
						  in half 	i_smoothness,
						  in half4  i_sss,
						  in half   i_worldPosY,
						  in half 	i_worldNormalY,

						  in half4  i_user_Texture,
						  in half3 	i_user_Normal,
						  in half   i_user_EdgeFade,
						  in half   i_global_Progress,
						  in half   i_user_Progress,
						  in half 	i_user_Metallic,
						  in half 	i_user_Smoothness,
						  in half   i_user_AlphaClamp,
						  in half   i_user_Wrap,
						  in half   i_user_FadeStartY,
						  in half   i_user_FadeDistance,
						  in half3  i_user_VaryColor,

						  in half 	i_varyColorAmount,

						  out half3 o_albedo,
						  out half3 o_normal,
						  out half  o_metallic,
						  out half  o_smoothness,
						  out half4 o_sss
						  )
{
#ifdef _PW_SF_COVER_ON
	half3 albedo;
	half  alpha;
	half  alwaysUpY = abs ( i_worldNormalY );

	albedo = i_user_Texture.rgb;

	alpha = clamp ( 0, 1, abs ( i_user_AlphaClamp ) + ( i_user_Texture.a * i_user_AlphaClamp * 10 ) );

	half dissolve   = 1.0 - ( alwaysUpY * alpha );
	half edgeSize 	= lerp ( dissolve + i_user_EdgeFade, dissolve - i_user_EdgeFade, dissolve );
	half progress;
	CombineLocalGlobalClamp_half ( i_user_Progress, i_global_Progress, progress );

    progress	= smoothstep (  progress + i_user_EdgeFade, progress - i_user_EdgeFade, edgeSize );

	#ifdef _PW_SF_WORLDMAP_ON
	albedo 		= lerp ( albedo, albedo * i_user_VaryColor, i_varyColorAmount );
	#endif

	// fade cover
	half delta = i_worldPosY - i_user_FadeStartY;
	half sgn   = max ( sign ( delta ), 0 );
	half fade  = clamp ( length ( delta ) / max ( i_user_FadeDistance, HALF_MIN ), 0.0, 1.0 ) * sgn;

	progress *= fade;

	// progress on top only
	half worldY = saturate ( i_worldNormalY ); 
	half amount = progress * min ( worldY * worldY + i_user_Wrap, 1 ) * alpha;

	// color for translucent on top
	#ifdef _PW_SF_SSS_ON
	half4 tempSSSCover;
	half  tempAmount = progress * alwaysUpY * alpha;

	tempSSSCover.rgb	= albedo;
	tempSSSCover.a 		= tempAmount;
	
	o_sss = lerp ( i_sss, tempSSSCover, tempAmount );
	#else
	o_sss = half4(0,0,0,0);
	#endif

	o_albedo		= lerp ( i_albedo, albedo, amount );
	o_normal   		= lerp ( i_normal, i_user_Normal, amount*0.25 );
	o_metallic 		= lerp ( i_metallic, i_user_Metallic, amount );
	o_smoothness	= lerp ( i_smoothness, i_user_Smoothness, amount );
#else
	o_albedo		= i_albedo;
	o_normal   		= i_normal;
	o_metallic 		= i_metallic;
	o_smoothness	= i_smoothness;
	o_sss 			= half4(0,0,0,0);
#endif
}

#endif // PW_GENERALFUNCS



