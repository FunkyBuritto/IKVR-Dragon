#define HALF_MIN 6.103515625e-5

#ifdef SO_COLORSPACE_GAMMA
	#define DielectricSpec half4(0.220916301, 0.220916301, 0.220916301, 1.0 - 0.220916301)
	#define ColorSpaceDouble half4(2.0, 2.0, 2.0, 2.0)
#else
	#define DielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04)
	#define ColorSpaceDouble half4(4.59479380, 4.59479380, 4.59479380, 2.0)
#endif


uniform float       _PW_SrcBlend;
uniform float       _PW_DstBlend;

uniform sampler2D   _CameraOpaqueTexture;
uniform sampler2D   _ScreenNormals;
uniform sampler2D 	_CameraDepthTexture; 

uniform sampler2D 	_DataMap;
uniform half        _DataMapOffsetX;
uniform half        _DataMapOffsetZ;
uniform half        _DataMapPointScale;

uniform sampler2D 	_WaterDepthRamp;
uniform half        _TransparentMin;
uniform half 		_TransparentDepth;

uniform half        _ReflectionStrength;
uniform sampler2D 	_NormalLayer0; 
uniform sampler2D 	_NormalLayer1; 
uniform sampler2D 	_NormalLayer2; 
uniform sampler2D 	_NormalFadeMap; 
uniform half 		_NormalLayer0Scale; 
uniform half 		_NormalLayer1Scale; 
uniform half 		_NormalLayer2Scale; 
uniform half        _NormalFadeStart;
uniform half        _NormalFadeDistance;
uniform float       _NormalMoveScale;
uniform half 		_FoamDepth;
uniform half 		_FoamStrength;
uniform sampler2D 	_ReflectionTex; 
uniform half4       _ReflectionColor;
uniform half4       _UnderWaterColor;
uniform half4       _EdgeWaterColor;
uniform half        _EdgeWaterDist;
half      			_NormalTile;
half                _FoamTexTile;
half      			_WaveSpeed;
half      			_WaveLength;
float 				_WaveSteepness; 
float 				_WaveScale; 
float               _ReflectionDistortion;
float4              _WaveDirection;
float               _WaveShoreClamp;
float               _WaveDirGlobal;
float               _WaveBackwashToggle;
float               _WavePeakToggle;

//			half                _Metallic;
half                _Smoothness;
half4 				_AmbientColor;
sampler2D 			_FoamTex;

struct PWSurface
{
	half  	roughness;
	half  	roughnessX2;
	half  	metallic;
	half4 	finalRGBA;
	half3 	specularColor;
	half    reflectInverse;
	half3 	reflection;
	half    reflectivity;
	half    dotNV;
	half3   depthColor;
	half3    debug;
};



