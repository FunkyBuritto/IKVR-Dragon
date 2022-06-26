
#ifndef PW_GENERALVARS
#define PW_GENERALVARS

struct EchoData
{
	half4  coverRGBA;
	half3  worldNormal;
	half   thickness;
	half3  screenPos;
	half4  vertexColor;
	float2 worldUV;
};

struct SurfaceOutputGaia
{
	half3  		Albedo;
	float3 		Normal;
	half3  		Emission;
	half   		Metallic;
	half   		Smoothness;
	half   		Occlusion;
	half  		Alpha;
	EchoData 	e;
};

struct PWCoverLayer
{
	half3 albedo;
	half  alpha;
	half3 normal;
	half4 colorTint;
	half  alphaClamp;
	half  smoothness;
	half  metallic;
	half  coverAmount;
	half  progress;
	half  amount;
	half  wrap;
};

struct PWCoverSurface
{
	half3 albedo;
	half3 albedoSSS;
	half3 normal;
	half  smoothness;
	half  metallic;
	half  alwaysUpDir;
	half  worldNormalY;
};

half4       _PW_MainLightDir;
half4       _PW_MainLightColor;
half4       _PW_MainLightSpecular;
half 		_PW_MainLightIntensity;

float     	_PW_ShaderMode;
float 		_PW_TerrainSizeX;
float 		_PW_TerrainSizeZ;

sampler2D 	_MainTex;
sampler2D 	_BumpMap;
sampler2D	_MetallicGlossMap;
half 		_BumpMapScale;
half     	_Cutoff;
half 	 	_Glossiness;
half 	 	_Metallic;
half 	 	_WrapLighting;

half     	_AOPower;
half     	_AOVertexMask;
half     	_AOPowerExp;

sampler2D 	_PW_WorldMap;
half4 		_PW_WorldMapColorObject;
half4 		_PW_WorldMapColorCover0;
half4 		_PW_WorldMapColorCover1;
float 		_PW_WorldMapUVScale;

// alpha add
// alpha expand

sampler2D 	_PW_CoverLayer0;
half4	  	_PW_CoverLayer0Color;
sampler2D 	_PW_CoverLayer0Normal;
half      	_PW_CoverLayer0NormalScale;
half        _PW_CoverLayer0AlphaClamp;
half      	_PW_CoverLayer0Edge;
half        _PW_CoverLayer0Tiling;
half      	_PW_CoverLayer0Smoothness;
half      	_PW_CoverLayer0Metallic;
half      	_PW_CoverLayer0Wrap;
half      	_PW_CoverLayer0Progress;
half      	_PW_CoverLayer0FadeStart;
half      	_PW_CoverLayer0FadeDist;

sampler2D 	_PW_CoverLayer1;
half4	  	_PW_CoverLayer1Color;
sampler2D 	_PW_CoverLayer1Normal;
half      	_PW_CoverLayer1NormalScale;
half        _PW_CoverLayer1AlphaClamp;
half      	_PW_CoverLayer1Edge;
half        _PW_CoverLayer1Tiling;
half      	_PW_CoverLayer1Smoothness;
half      	_PW_CoverLayer1Metallic;
half      	_PW_CoverLayer1Wrap;
half      	_PW_CoverLayer1Progress;
half      	_PW_CoverLayer1FadeStart;
half      	_PW_CoverLayer1FadeDist;

float2 		_PW_WindTreeWidthHeight;
float3		_PW_WindTreeFlex;
float3 		_PW_WindTreeFrequency;
float4 		_PW_WindGlobals;
float4 		_PW_WindGlobalsB;

half   		_PW_SSSPower;
half4   	_PW_SSSTint;
half   		_PW_SSSDistortion;
half   		_PW_SSSThickness;

half        _PW_Global_SeasonalTintAmount;

// global uniforms set by system
half 		_PW_Global_CoverLayer0Progress;
half 		_PW_Global_CoverLayer1Progress;

half      	_PW_Global_CoverLayer0FadeStart;
half      	_PW_Global_CoverLayer0FadeDist;

half      	_PW_Global_CoverLayer1FadeStart;
half      	_PW_Global_CoverLayer1FadeDist;

half 		_PW_Global_Metallic;
half 		_PW_Global_Smoothness;
half        _PW_Global_TerrainScale;

half4       _PW_Global_SeasonalTint;

float4   	_PW_Global_WindDirection;
float    	_PW_Global_WindGustDistance;
float    	_PW_Global_WindSpeed;

#endif //PW_GENERALVARS