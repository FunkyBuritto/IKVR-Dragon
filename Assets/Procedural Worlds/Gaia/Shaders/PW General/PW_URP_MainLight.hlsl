#ifndef GETLIGHTURP_INCLUDED
#define GETLIGHTURP_INCLUDED

void MainLightURP_half(float3 i_worldPos, out half3 i_lightDir, out half3 i_color )
{

#if defined (SHADERGRAPH_PREVIEW) || !defined(_PW_SF_SSS_ON)
   i_lightDir = half3(0.5, 0.5, 0);
   i_color = 1;
#else
	#if SHADOWS_SCREEN
	half4 clipPos = TransformWorldToHClip(i_worldPos);
	half4 shadowCoord = ComputeScreenPos(clipPos);
	#else
	half4 shadowCoord = TransformWorldToShadowCoord(i_worldPos);
	#endif

	Light mainLight = GetMainLight(shadowCoord);
	i_lightDir 		= mainLight.direction;
	i_color 		= mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation;
#endif
}
#else
void MainLightURP_half(float3 i_worldPos, out half3 i_lightDir, out half3 i_color )
{
    i_lightDir = float3(1, 0, 0);
	i_color = 0;
}

#endif

