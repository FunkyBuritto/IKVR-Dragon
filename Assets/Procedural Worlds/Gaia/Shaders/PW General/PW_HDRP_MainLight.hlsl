#ifndef GETLIGHT_INCLUDED
#define GETLIGHT_INCLUDED

#if defined(_PW_SF_SSS_ON)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
#endif

void MainLightHDRP_half(float3 i_worldPos, out half3 i_lightDir, out half3 i_color )
{
#if defined (SHADERGRAPH_PREVIEW) || !defined(_PW_SF_SSS_ON)
   i_lightDir = half3( 0.5, 0.5, 0 );
   i_color = 1;
#else
    if ( _DirectionalLightCount > 0 )
    {
        DirectionalLightData light = _DirectionalLightDatas[0];
        i_lightDir 				   = -light.forward.xyz;
        i_color 				   = light.color;
    }
    else
    {
        i_lightDir = float3(1, 0, 0);
        i_color = 0;
    }
#endif
}

#endif

