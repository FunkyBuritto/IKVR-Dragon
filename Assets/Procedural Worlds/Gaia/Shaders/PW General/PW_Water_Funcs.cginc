
//-------------------------------------------------------------------------------------
inline half3 WaterEdge ( half3 i_rgb, half3 i_worldPos, half i_dist2Cam, const half c_mult )
{
	half fade 	= clamp ( i_dist2Cam / ( _EdgeWaterDist * c_mult), 0.0, 1.0 );

	fade = lerp ( 1.0, fade, _EdgeWaterColor.a );

	return ( lerp ( _EdgeWaterColor.rgb, i_rgb, fade * fade * fade ) );
}

//-------------------------------------------------------------------------------------
inline half TrowbridgeReitzNDF ( in half i_dotNH )
{
	half roughnessX2 = 1.0 - _Smoothness;

	roughnessX2 = roughnessX2 * roughnessX2;

	half dist = i_dotNH * i_dotNH * ( roughnessX2 - 1.0 ) + 1.0;
	return roughnessX2 / ( 3.14159 * dist * dist );
}

//-------------------------------------------------------------------------------------
inline half GGXNDF ( in half i_dotNH, in half i_roughness, in half i_roughnessX2  )
{
	half dotNHX2 = i_dotNH * i_dotNH;

	half tanDotNHX2 = ( 1 - dotNHX2 ) / dotNHX2;

	half st =  i_roughness / ( dotNHX2 * ( i_roughnessX2 + tanDotNHX2 ) );

	return ( 1.0 / 3.14159 ) * ( st * st );
}

//-------------------------------------------------------------------------------------
inline half SpecularStandard (  in half i_dotNH, in half i_dotLH, in half i_roughness, in half i_roughnessX2   )
{
	half	specTerm;

	half d = i_dotNH * i_dotNH * ( i_roughnessX2 - 1.0h) + 1.00001h;

	specTerm = i_roughnessX2 / ( ( d * d ) * max ( 0.1h, i_dotLH * i_dotLH ) * ( i_roughness * 4.0h + 2.0h ) );

	return ( specTerm );
}

//-------------------------------------------------------------------------------------
inline half3 FresnelReflection ( in PWSurface i_surf)
{
	half f = i_surf.dotNV * i_surf.dotNV * i_surf.dotNV * i_surf.dotNV;// * 0.7;

	half3 grazing = saturate ( ( 1.0 - i_surf.roughness ) + i_surf.reflectivity );// * i_color;

	return (  lerp ( i_surf.specularColor, grazing, f ) );
}

inline half3 FesnelShlick(float cosTheta, half3 F0)
{
	return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}


//-------------------------------------------------------------------------------------
inline void SurfacePBR( inout PWSurface i_surf, in half i_foamAmount, in half i_metallic, in half3 i_worldNormal, in half3 i_viewDir, in half4 i_screenPos, in half2 i_distortion )
{
	i_surf.roughnessX2 			= i_surf.roughness * i_surf.roughness;
	i_surf.metallic 			= i_metallic * ( 1.0 - i_foamAmount );

	half3 F0 = lerp(DielectricSpec.rgb,i_surf.depthColor,i_surf.metallic);

	i_surf.specularColor	= _PW_MainLightColor.rgb * F0;

	i_surf.reflectInverse 	= DielectricSpec.a - i_surf.metallic * DielectricSpec.a;

	i_surf.reflectivity 	= 1.0h - i_surf.reflectInverse;

	i_surf.dotNV =  dot ( i_viewDir, i_worldNormal );
	half flatwaterNV = dot ( i_viewDir, float3(0,1,0));
	half smoothedNV = lerp(i_surf.dotNV,flatwaterNV,saturate(flatwaterNV * 5));

	
	half4 ruv = i_screenPos;
	ruv.xy += i_distortion;

	half3  reflectionSample = tex2Dproj (_ReflectionTex, UNITY_PROJ_COORD(ruv * 5)).rgb;
	reflectionSample = lerp(reflectionSample,_AmbientColor, i_surf.roughnessX2);
	
	i_surf.reflection = reflectionSample * FesnelShlick(smoothedNV,F0);
	i_surf.reflection *= 1.0 / ( i_surf.roughnessX2 + 1.0 );
	
	
	//i_surf.debug = FresnelReflection ( i_surf);
}


//-----------------------------------------------------------------
inline void PBRDirLighting ( inout PWSurface i_surf, in half3 i_worldPosition, in half3 i_worldNormal, in half3 i_viewDir )
{
	half3 direction = normalize(_PW_MainLightDir.xyz);

	half dotNL   	= saturate ( dot ( i_worldNormal, direction ) );
	half3 halfDir	= normalize ( direction + i_viewDir );
	half dotNH   	= max ( dot ( halfDir,i_worldNormal ), HALF_MIN );
	half spec = GGXNDF ( dotNH, i_surf.roughness, i_surf.roughnessX2  );

  //  spec = min ( 3, spec * 0.111 );
 //   			half spec = SpecularStandard ( dotNH, dotLH, i_surf.roughness, i_surf.roughnessX2  );

	//half amt = 0.9 + 1;
	//half wrap = ( dotNL + 0.9 ) / ( amt * amt );
	
	half wrap = saturate( dotNL);
	i_surf.reflection += ( spec * i_surf.specularColor ) * dotNL;
	i_surf.finalRGBA.rgb *= ( _PW_MainLightColor.rgb * _PW_MainLightIntensity * wrap ) + _AmbientColor.rgb;
	i_surf.finalRGBA.rgb *= i_surf.reflectInverse;
	
}



//-----------------------------------------------------------------
inline float EchoWave ( in float3 i_position, in float2 i_direction, in float i_waveLength, in float i_speed, in half i_size, in float i_scale )
{
	float  dir = ( dot ( i_position.xz, i_direction ) );

	float3 vert = i_position;

	float speed = i_speed * -_Time.x;

	float offset = sin ( i_waveLength * dir + speed ) * i_size * i_scale;

	return (  offset );
}

//-------------------------------------------------------------------------------------
float3 GerstnerWave2 ( float4 wave, float3 p, inout float3 tangent, inout float3 binormal )
{
	float steepness = wave.z;
	float wavelength = wave.w;
	float k = 2 * UNITY_PI / wavelength;
	float c = sqrt(9.8 / k);
	float2 d = normalize(wave.xy);
	float f = k * (dot(d, p.xz) - c * (_Time.y ));
	float a = steepness / k;

	//p.x += d.x * (a * cos(f));
	//p.y = a * sin(f);
	//p.z += d.y * (a * cos(f));

	tangent += float3(
		-d.x * d.x * (steepness * sin(f)),
		d.x * (steepness * cos(f)),
		-d.x * d.y * (steepness * sin(f))
	);

	binormal += float3(
		-d.x * d.y * (steepness * sin(f)),
		d.y * (steepness * cos(f)),
		-d.y * d.y * (steepness * sin(f))
	);

	return float3(
		d.x * (a * cos(f)),
		a * sin(f),
		d.y * (a * cos(f))
	);
}

#define GRAVITY 9.81

//-------------------------------------------------------------------------------------
float3 GerstnerWave ( in float i_id, float2 i_dir, in float i_waveLength, in float i_steepness, in float i_scale, in float i_speed, in float3 i_point, inout float3 tangent, inout float3 binormal )
{
	float waveLength = i_waveLength;
	float steepness = pow ( i_steepness, i_id ) * i_scale;

	float 	k 		= 6.2831853 / waveLength;
	float 	c 		= sqrt ( GRAVITY / k );
	float2 	dir 	= normalize ( i_dir );
	float 	f 		= k * ( dot ( dir, i_point.xz ) - c * ( _Time.y * i_speed ) );
	float 	a 		= steepness / k;

	// gotta scale tanget and binormal
	tangent += float3 ( -dir.x * dir.x * (steepness * sin(f)),
		dir.x * (steepness * cos(f)),
		-dir.x * dir.y * (steepness * sin(f))
	);

	binormal += float3 ( -dir.x * dir.y * (steepness * sin(f)),
		dir.y * (steepness * cos(f)),
		-dir.y * dir.y * (steepness * sin(f))
	);

	float3 newPos = float3( dir.x * (a * cos(f)), a * sin(f), dir.y * (a * cos(f) ) );

	return ( newPos );
}

//-------------------------------------------------------------------------------------
inline half2 CalcLayerUVRotation ( in float2 iuv, in float i_angle )
{
	float2x2 	rotationMatrix;
	float 		sinX;
	float 		cosX;

	sincos ( i_angle, sinX, cosX );
	rotationMatrix = float2x2 ( cosX, -sinX, sinX, cosX );

	iuv -= float2 ( 0.5, 0.5 );

	iuv = mul ( iuv, rotationMatrix );

	iuv += float2 ( 0.5, 0.5 );

	return ( iuv );
}

//-------------------------------------------------------------------------------------
half3 BlendUnpacked(half3 n1, half3 n2)
{
	n1 += half3( 0,  0, 1);
	n2 *= half3(-1, -1, 1);
	return n1*dot(n1, n2)/n1.z - n2;
}

//-------------------------------------------------------------------------------------
inline half4 FlowMap ( in sampler2D i_normalTex, in half2 i_uv, in half2 i_flowXY, in half i_speed )
{
//	half4 flowDir = tex2D ( i_flowMap, i_layer.uv ).rgba;

	i_flowXY = ( i_flowXY * 2.0 - 1.0 ) * half2 ( i_speed, i_speed );

	half flowPhase0 = frac ( _Time.y * 0.5f + 0.5f );
	half flowPhase1 = frac ( _Time.y * 0.5f + 1.0f );

	half flowLerp = abs ( ( 0.5f - flowPhase0 ) / 0.5f );

	 half flowUV0 = i_uv + i_flowXY * flowPhase0;
	 half flowUV1 = i_uv + i_flowXY * flowPhase1;

	 half4 tex0 	= tex2D ( i_normalTex, flowUV0 );
	 half4 tex1 	= tex2D ( i_normalTex, flowUV1 );

	 return ( lerp ( tex0, tex1, flowLerp ) );
}



