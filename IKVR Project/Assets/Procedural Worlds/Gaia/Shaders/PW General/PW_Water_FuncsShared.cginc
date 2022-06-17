#define PW_DEFAULT_TERRAIN_SIZE 2048.0
#define PW_DEFAULT_TERRAIN_SCALE ( 1.0 / PW_DEFAULT_TERRAIN_SIZE )
//#define PW_GLOBAL_SCALE (_PW_Global_TerrainScale+1.0)

/*
_Metallic            = metallic of deep ocean
_Smoothness 	     = smoothness of deep ocean
_Foam_Metallic       = metallic of where foam exists
_Foam_Smoothness 	 = smoothness of where foam exists
_UnderWaterBlurScale = how much to distort refraction under water 0 = perfectly clear
_FoamTex			 = Foam texture with Mask Map on Alpha channel 
_FoamTexTile		 = tiling for above
_FoamBubbles		 = the close shorline bubbles and alpha channel is just like normal alpha
_FoamBubblesTile	 = tiling for above
_FoamBubblesScale    = 0 = no bubbles, 0.5 would make bubbble go half way thru foam .. 1.0 bubbles would consume all the foam
**_FoamStrength        = lessen or over saturate foam
**_FoamBubblesStrength = lessen or over saturate foam

There are 2 textures i made PW_ShoreBubbles and PW_ShoreFoamWithMask for examples

*/

//-----------------------------------------------------------------
inline void EchoWave_float ( in float3 i_position, in float2 i_direction, in float i_waveLength, in float i_speed, in float i_size, in float i_scale, out float o_offset )
{
	float  dir = ( dot ( i_position.xz, i_direction ) );

	float3 vert = i_position;

	float speed = i_speed * -_Time.x;

	o_offset = sin ( i_waveLength * dir + speed ) * i_size * i_scale;
}

//-------------------------------------------------------------------------------------
inline void BlendUnpacked_float ( in float3 i_n1, in float3 i_n2, out float3 o_normal )
{
	i_n1 += float3( 0,  0, 1);
	i_n2 *= float3(-1, -1, 1);

	 o_normal = i_n1 * dot(i_n1, i_n2) / i_n1.z - i_n2;
}

//-------------------------------------------------------------------------------------
inline void CalcUVNormals_float ( in float2 i_worldMapUV, in float2 i_waveDir, in float i_normalTile, in float i_timeX, in float i_waveSpeed, out float2 o_uv0, out float2 o_uv1, out float2 o_uv2, out float2 o_normalFadeUV  )
{
	float timeScale = i_timeX * ( i_waveSpeed * 0.00033 );

	float2 t; 
	t.x = i_waveDir.x * timeScale;
	t.y = i_waveDir.y * timeScale;

	o_uv0 = ( i_worldMapUV - t ) * i_normalTile;
	o_uv1 = ( i_worldMapUV + t ) * i_normalTile * 0.333;
	o_uv2 = i_worldMapUV * i_normalTile * 0.0333;

	o_normalFadeUV 	= i_worldMapUV;
}

//-------------------------------------------------------------------------------------
inline void CalcFoamUV_float ( in float i_foamTile, in float2 i_worldMapUV, in float3 i_bumpMap, in float i_sinTime, out float2 o_foamUV )
{
	o_foamUV = ( i_foamTile * i_worldMapUV + float2 ( i_sinTime * 0.1, i_sinTime / 4.0 * 0.1 ) ) + i_bumpMap.xy * 0.2;
}

//-------------------------------------------------------------------------------------
inline void  CalcFoamUVS_float ( in float3 i_worldUV, in float i_foamTile, in float i_bubbleTile, in float i_time, in float i_moveYSpeed, out float2 o_foamUV1, out float2 o_foamUV2, out float2 o_bubbleUV1,  out float2 o_bubbleUV2 )
{
	float2 worldUV 			=  i_worldUV.xz;
	float2 scaledUV1 		= worldUV * i_foamTile;
	float2 scaledUV2 		= worldUV * ( i_foamTile * 0.8 );
	float2 scaledBubbleUV1 	= worldUV * i_bubbleTile;
	float2 scaledBubbleUV2 	= worldUV * ( i_bubbleTile * 1.8 );


	float scroll = i_time * 0.025;

	float yMovement = i_worldUV.y * PW_DEFAULT_TERRAIN_SIZE * i_moveYSpeed;

	o_foamUV1 	= scaledUV1 - float2 ( scroll, cos ( i_worldUV.x ) + sin ( yMovement ) );
	o_foamUV2 	= float2(0.0,1.0) - scaledUV2 * 0.5 + float2 ( sin ( yMovement ), scroll );
	o_bubbleUV1	= scaledBubbleUV1;
	o_bubbleUV2	= scaledBubbleUV2 + float2 ( cos ( yMovement ), sin ( yMovement ) );
}

//-------------------------------------------------------------------------------------
inline void NormalsFade_float ( in float3 i_normalFadeMap, in float3 i_worldCameraPos, in float3 i_worldPos, in float i_foamAmt, in float i_normalFadeStart, in float i_normalFadeDistance, in float i_normalLayer0Scale, in float i_normalLayer1Scale, in float i_normalLayer2Scale, out float o_normalLayer0Scale, out float o_normalLayer1Scale, out float o_normalLayer2Scale, out float o_distFade )
{
	float scaleNormal 		= 1.0 - i_foamAmt;
	float dist2Cam 			= distance ( i_worldCameraPos, i_worldPos );
	float dist 				= dist2Cam - i_normalFadeStart;

	o_distFade 				= clamp ( dist / i_normalFadeDistance, 0.0, 1.0 );
	o_normalLayer0Scale 	= i_normalLayer0Scale * scaleNormal * i_normalFadeMap.r;
	o_normalLayer1Scale 	= i_normalLayer1Scale * scaleNormal * i_normalFadeMap.g;
	o_normalLayer2Scale 	= i_normalLayer2Scale * i_normalFadeMap.b;
}

//-------------------------------------------------------------------------------------
inline void RefractionUV_float ( in float4 i_screenPos, in float3 i_bumpUV, out float4 o_screenPos )
{
	float2 uvOffset = i_bumpUV.xy ;

	o_screenPos = i_screenPos;
	o_screenPos.x += uvOffset;
	o_screenPos.y -= uvOffset;
}

/*
	Metallic in out
	Smoothness in out
	Albedo in out
	underwater color in
	front faceing bool in
	Emmissive out

*/

//-------------------------------------------------------------------------------------
inline void CalcUnderOver_float ( in bool i_frontFacing, in float3 i_albedo, in float i_metallic, in float i_smoothness, in float3 i_sceneColor,in float3 i_sceneColorBlended, in float4 i_underWaterColor, out float3 o_albedo, out float3 o_emissive, out float o_metallic, out float o_smoothness )
{
	float lerpVal = i_frontFacing ? 1.0 : 0.0;

	o_albedo 		= i_albedo * float3( lerpVal, lerpVal, lerpVal );
	o_emissive      = lerp( i_sceneColor, i_sceneColorBlended, lerpVal);
	o_metallic 		= i_metallic * lerpVal;
	o_smoothness 	= i_smoothness * lerpVal;
}

//-------------------------------------------------------------------------------------
inline void BackFaceLerp_float ( in bool i_frontFacing, out float3 o_lerpVal )
{
	//o_lerpVal = float3(1,1,1);
	//if ( i_frontFacing )
	//	o_lerpVal = float3(0,0,0);


	o_lerpVal = i_frontFacing ? float3( 0.0, 0.0, 0.0 ) : float3 ( 1.0, 1.0, 1.0 );
}

//-------------------------------------------------------------------------------------
inline void PW_WorldScaleGraph_float ( in float i_global_TerrainScale, out float3 o_worldScale )
{
	float tscale = i_global_TerrainScale + 1.0;
	o_worldScale.x = PW_DEFAULT_TERRAIN_SCALE * tscale;
	o_worldScale.y = PW_DEFAULT_TERRAIN_SCALE * tscale;
	o_worldScale.z = PW_DEFAULT_TERRAIN_SCALE * tscale;
}

//-------------------------------------------------------------------------------------
inline void WaveLengthVertex_float ( in float i_waveLength, in float i_globalTerrainScale, out float o_waveLength )
{
	o_waveLength = PW_DEFAULT_TERRAIN_SCALE * (i_globalTerrainScale+1.0);
}

//-----------------------------------------------------------------
inline void ShoreLine_float ( in float i_linearDepth01, in float i_farPlane, in float i_screenW, in float i_transparentDepth, in float i_foamDepth, out float o_foamAmount, out float o_underWaterAmount, out float o_edgeBlend )
{
	float depth 		= i_linearDepth01 * i_farPlane;
	float foamDiff   	= i_screenW - i_foamDepth;

//	o_foamAmount		= 1.0 - clamp ( ( depth - foamDiff ), 0.0, 1.0 );

	float fa = ( depth - i_screenW ) / i_foamDepth;

	o_foamAmount		= 1.0 - clamp ( fa, 0.0, 1.0 );
	o_underWaterAmount  = clamp ( ( depth - i_screenW ) / i_transparentDepth, 0.0, 1.0 );
	o_edgeBlend 		= clamp ( ( depth-i_screenW ), 0.0, 1.0 );
}

//-----------------------------------------------------------------
inline float Remap ( in float In, in float InMinMax )
{
    return ( In *  1.0 / InMinMax );
}   												 


//-----------------------------------------------------------------
inline void ShoreFoamOld_float ( in float i_linearDepth01, in float3 i_worldPosition, in float3 i_cameraPosition, in float3 i_cameraDirection, in float i_cameraFarPlane, in float i_foamDepth, in float i_transparentDepth, out float o_shorelineAmount, out float o_foamAmount, out float o_underwaterAmount, out float o_edgeBlend )
{
	float3 dir 		= i_worldPosition - i_cameraPosition;
	float dotProd 	= dot ( i_cameraDirection, dir );

	float foamEdge1 		= Remap ( dotProd, i_cameraFarPlane );
	float foamEdge2 		= Remap ( dotProd+i_foamDepth, i_cameraFarPlane );
	float underwaterEdge2 	= Remap ( dotProd+i_transparentDepth, i_cameraFarPlane );
	float blendEdge2 		= Remap ( dotProd+0.5, i_cameraFarPlane );
	float shorelineEdge2 	= Remap ( dotProd + ( i_foamDepth * 0.2 ), i_cameraFarPlane );

	o_shorelineAmount 	= smoothstep ( shorelineEdge2, foamEdge1, i_linearDepth01 );
	o_foamAmount 		= smoothstep ( foamEdge2, foamEdge1, i_linearDepth01 );
	o_underwaterAmount 	= 1.0 - smoothstep ( underwaterEdge2, foamEdge1 , i_linearDepth01 );
	o_edgeBlend 		= smoothstep ( foamEdge1, blendEdge2, i_linearDepth01 );
}


//-----------------------------------------------------------------
inline void ShoreFoam_float ( in float i_sceneDepth, in float3 i_worldPosition, in float3 i_worldNormal, in float4 i_screenPosition, in float3 i_cameraPosition, in float3 i_cameraDirection, in float i_cameraFarPlane, in float i_foamDepth, in float i_bubblesScale, in float i_transparentDepth, in float i_shorelineEdge, out float o_shorelineAmount, out float o_foamAmount, out float o_underwaterAmount, out float o_edgeBlend )
{
	float diff = i_sceneDepth - i_screenPosition.w;

	float3 dir 			= normalize ( i_cameraPosition - i_worldPosition );
	float dotProd 		= 1.0 - max ( dot ( i_worldNormal, dir ), 0 );
	float foamDepth     = i_foamDepth * ( dotProd + 1 );

	o_shorelineAmount 	= 1.0 - saturate ( diff / ( ( foamDepth * i_bubblesScale ) ) );
	o_foamAmount 		= 1.0 - saturate ( diff / foamDepth );
	o_underwaterAmount 	= 1-saturate ( exp2(-diff * i_transparentDepth));
	//o_underwaterAmount 	= saturate ( diff / i_transparentDepth);
	o_edgeBlend 		= saturate ( diff / i_shorelineEdge );

	o_edgeBlend = 1.0 - o_edgeBlend;
	o_edgeBlend = o_edgeBlend * o_edgeBlend * o_edgeBlend;
	o_edgeBlend = 1.0 - o_edgeBlend;
}


//-------------------------------------------------------------------------------------
inline void ShorelineApply_float (  in float3 i_albedo, in float4 i_foamColor, in float4 i_foamBubbles, in float i_foamAmt, in float i_foamMask, in float i_shorelineAmount, in float i_foamStrength, in float i_bubblesStrength, in float i_metallic, in float i_smoothness, in float i_foamMetallic, in float i_foamSmoothness, out float3 o_albedo, out float o_metallic, out float o_smoothness, out float o_height )
{
	float foamAmount 		= i_foamAmt * i_foamAmt * i_foamAmt;
	float shorelineAmount 	= i_shorelineAmount * i_shorelineAmount * i_shorelineAmount;

	float3 foamRGB 		= clamp ( i_foamColor.rgb - float3 ( i_foamMask,i_foamMask,i_foamMask ), 0.0, 1.0 );
	float4 bubblesRGBA 	= i_foamBubbles;

	float  foamSmoothness 	= ( 1.0 - bubblesRGBA.a ) * shorelineAmount;
	float3 addFoam 			= foamRGB * foamAmount;
	float4 addBubbles 		= bubblesRGBA;

	addFoam *= i_foamStrength;												

	o_albedo = min ( i_albedo + addFoam.rgb, 1 );

	o_albedo = lerp ( o_albedo, addBubbles.rgb, saturate(shorelineAmount * i_bubblesStrength));

	float surfaceAmount   = clamp ( foamSmoothness + addFoam.b, 0.0, 1.0 );

	o_metallic 	 	= lerp ( i_metallic, i_foamMetallic, surfaceAmount );
	o_smoothness 	= lerp ( i_smoothness, i_foamSmoothness, surfaceAmount );

	o_height = bubblesRGBA.a * shorelineAmount * 0.1;
}

//-----------------------------------------------------------------
inline void ShoreLineVertex_float ( in float i_linearDepth01, in float4 i_screenPos, in float i_waveShoreClamp, out float o_shoreClamp )
{
	float shoreDiff   	= saturate ( i_linearDepth01 - i_screenPos.w );

	o_shoreClamp = clamp ( shoreDiff / 32.0 + i_waveShoreClamp, 0.0, 1.0 );
}

//-----------------------------------------------------------------
inline float EchoWave ( in float3 i_position, in float2 i_direction, in float i_waveLength, in float i_speed, in float i_size, in float i_scale, in float i_timeX )
{
	float  dir  = ( dot ( i_position.xz, i_direction ) );
	float3 vert = i_position;
	float speed = i_speed * - i_timeX;
	
	float offset = sin ( i_waveLength * dir + speed ) * i_size;

	return (  offset );
}

//-----------------------------------------------------------------
inline void EchoWave_float ( in float i_globalTerrainScale, in float3 i_worldPosition, in float4 i_waveDirection, in float i_waveLength, in float i_speed, in float i_steepness, in float i_shoreClamp, in float i_timeX, out float3 o_worldPosition )
{
	float2 waveDir      = i_waveDirection.xz;
	float3 v0 			= i_worldPosition.xyz;
	float steepness 	= i_steepness;

	i_timeX /= 20;

	v0.y += sin (i_timeX * i_speed ) * i_steepness;

	o_worldPosition = v0;
}

//-----------------------------------------------------------------
inline void EchoWaveOld_float ( in float i_globalTerrainScale, in float3 i_worldPosition, in float4 i_waveDirection, in float i_waveLength, in float i_speed, in float i_steepness, in float i_shoreClamp, in float i_timeX, out float3 o_worldPosition )
{
	float2 waveDir      = i_waveDirection.xz;
	float3 v0 			= i_worldPosition.xyz;
	float steepness 	= i_steepness;
	float waveLength 	= i_waveLength * PW_DEFAULT_TERRAIN_SCALE * ( i_globalTerrainScale + 1.0 );

	i_timeX /= 20;

	v0.y += EchoWave ( v0, waveDir, i_waveLength, i_speed, i_steepness, i_shoreClamp, i_timeX );
	float2 perp;
	perp.x = waveDir.y;
	perp.y = -waveDir.x;

	v0.y += EchoWave ( v0, perp, waveLength * 0.25, - i_speed, steepness * 0.66, i_shoreClamp, i_timeX );

	waveDir = normalize ( ( perp + waveDir ) * 0.5 );
	v0.y += EchoWave ( v0, waveDir, waveLength, i_speed, steepness * 0.33, i_shoreClamp, i_timeX );

	o_worldPosition = v0;
}
// ----------------------------------------------------------------------
inline void DepthComparison_float (in float i_depthA, in float i_depthB, out float o_result)
{
	o_result = i_depthA > i_depthB ? 0 : 1;
}

