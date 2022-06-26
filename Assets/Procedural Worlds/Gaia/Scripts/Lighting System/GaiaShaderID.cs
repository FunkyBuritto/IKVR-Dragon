using UnityEngine;

namespace Gaia
{
    /// <summary>
    /// Contains all the shader ID's for all shaders needed for weather and water
    /// </summary>
    public static class GaiaShaderID
    {
        //PW Sky
        public static readonly int m_cloudFade;
        public static readonly int m_cloudBrightness;
        public static readonly int m_cloudAmbientColor;
        public static readonly int m_SunDirection;
        public static readonly int m_SunColor;
        public static readonly int m_cloudDomeBrightness;
        public static readonly int m_cloudSunDirection;
        public static readonly int m_skySunDirection;
        public static readonly int m_cloudOpacity;
        public static readonly int m_cloudDomeFogColor;
        public static readonly int m_cloudDomeSunColor;
        public static readonly int m_cloudDomeFinalCloudColor;
        public static readonly int m_cloudDomeFinalSkyColor;
        public static readonly int m_cloudHeightDensity;
        public static readonly int m_cloudHeightThickness;
        public static readonly int m_cloudSpeed;
        public static readonly int m_pwSkyFogHeight;
        public static readonly int m_pwSkyFogGradient;
        public static readonly string m_cloudShaderName = "PWS/VFX/PW_SkydomeCloudsHight";
        public static readonly string m_pwSkySkyboxShader = "PWS/Skybox/PW_Procedural";

        //PW VFX
        public static readonly int m_rainIntensity;
        public static readonly int m_weatherMainColor;
        public static readonly int m_weatherColor;

        //PW Water
        public static readonly int m_waterDepthRamp;
        public static readonly int m_waterSmoothness;
        public static readonly int m_waterFoamTex;
        public static readonly int m_waterFoamTexTiling;
        public static readonly int m_waterFoamRampAlpha;
        public static readonly int m_waterFoamDepth;
        public static readonly int m_waterTransparentDepth;
        public static readonly int m_waterReflectionDistortion;
        public static readonly int m_waterReflectionStrength;
        public static readonly int m_waterWaveShoreMove;
        public static readonly int m_waterMetallic;
        public static readonly string m_waterGrabPass = "GrabPass";
        public static readonly int m_underwaterColor;
        public static readonly int InvViewProjection;
        public static readonly int CameraRoll;
        public static readonly int _normalLayer0_ID;
        public static readonly int _normalLayer1_ID;
        public static readonly int _normalLayer2_ID;
        public static readonly int _normalLayer0Scale_ID;
        public static readonly int _normalLayer1Scale_ID;
        public static readonly int _normalLayer2Scale_ID;
        public static readonly int _normalSpeed_ID;
        public static readonly int _normalFadeStart_ID;
        public static readonly int _normalFadeDistance_ID;
        public static readonly int _normalTile_ID;
        public static readonly int _waveShoreClamp_ID;
        public static readonly int _waveLength_ID;
        public static readonly int _waveSteepness_ID;
        public static readonly int _waveSpeed_ID;
        public static readonly int _waveDirection_ID;
        public static readonly int _edgeWaterColor_ID;
        public static readonly int _edgeWaterDist_ID;
        public static readonly string _cbufName = "Echo_Refaction";
        public static readonly int _grabID	= 0;
        public static readonly int m_foamBubblesTexture;
        public static readonly int m_foamBubblesScale;
        public static readonly int m_foamBubblesTiling;
        public static readonly int m_foamStrength;
        public static readonly int m_foamBubblesStrength;
        public static readonly int m_foamEdge;
        public static readonly int m_foamMoveSpeed;
        public static readonly string m_cameraOpaqueTexture = "_CameraOpaqueTexture";
        public static readonly string m_refractionOn = "_PW_MC_REFRACTION_ON";
        public static readonly string m_refractionOff = "_PW_MC_REFRACTION_OFF";
        public static readonly int m_blendSRC;
        public static readonly int m_blendDST;

        //PW GlobalGlobal
        public static readonly int m_globalLightDirection;
        public static readonly int m_globalLightColor;
        public static readonly int m_globalLightIntensity;
        public static readonly int m_globalLightSpecColor;
        public static readonly int m_globalWind;
        public static readonly int m_globalReflectionTexture;
        public static readonly int m_globalAmbientColor;
        public static readonly int m_hdrpZTest;


        //Unity Skybox
        public static readonly string m_unitySkyboxShader = "PWS/Skybox/PW_Procedural";
        public static readonly string m_unitySkyboxShaderHDRI = "PWS/Skybox/PW_HDRI";
        public static readonly int m_unitySkyboxGroundColor;
        public static readonly int m_unitySkyboxAtmosphereThickness;
        public static readonly int m_unitySkyboxSunSize;
        public static readonly int m_unitySkyboxSunSizeConvergence;
        public static readonly int m_unitySkyboxTint;
        public static readonly int m_unitySkyboxTintHDRI;
        public static readonly int m_unitySkyboxCubemap;
        public static readonly int m_unitySkyboxExposure;
        public static readonly int m_unitySkyboxRotation;
        public static readonly int m_unitySkyboxSunDisk;
        public static readonly string m_unitySunQualityKeyword = "_SUNDISK_HIGH_QUALITY";

        //Check strings
        public static readonly string m_checkNameSpace = "Space";
        public static readonly string m_checkCloudHeight = "CloudsHight";

        //Water Shaders
        public static readonly string BuiltInWaterShader = "PWS/PW_Water";
        public static readonly string HDRPWaterShader = "Shader Graphs/PW_Water_RP";
        public static readonly string HDRPWaterShaderFile = "PW_Water_RP";

        static GaiaShaderID()
        {
            //PW Sky
            m_cloudFade = Shader.PropertyToID(("PW_Clouds_Fade"));
            m_cloudBrightness = Shader.PropertyToID(("PW_Cloud_Brightness"));
            m_cloudAmbientColor = Shader.PropertyToID(("PW_AmbientColor"));
            m_SunDirection = Shader.PropertyToID(("PW_SunDirection"));
            m_SunColor = Shader.PropertyToID(("PW_SunColor"));
            m_cloudDomeBrightness = Shader.PropertyToID(("PW_SkyDome_Brightness"));
            m_cloudSunDirection = Shader.PropertyToID(("PW_SunDirection_Clouds_HA"));
            m_skySunDirection = Shader.PropertyToID(("PW_SunDirection_Sky"));
            m_cloudOpacity = Shader.PropertyToID(("PW_Clouds_Opacity"));
            m_cloudDomeFogColor = Shader.PropertyToID(("PW_SkyDome_Fog_Color"));
            m_cloudDomeSunColor = Shader.PropertyToID(("PW_SkyDome_Sun_Color"));
            m_cloudDomeFinalCloudColor = Shader.PropertyToID(("PW_SkyDome_FinalClouds_Color"));
            m_cloudDomeFinalSkyColor = Shader.PropertyToID(("PW_SkyDome_FinalSky_Color"));
            m_cloudHeightDensity = Shader.PropertyToID(("PW_Clouds_Hight_Density"));
            m_cloudHeightThickness = Shader.PropertyToID(("PW_Clouds_Hight_Thickness"));
            m_cloudSpeed = Shader.PropertyToID(("PW_Clouds_Speed_HA"));
            m_pwSkyFogHeight = Shader.PropertyToID(("_FogHeight"));
            m_pwSkyFogGradient = Shader.PropertyToID(("_FogGradient"));

            //PW VFX
            m_rainIntensity = Shader.PropertyToID(("_PW_VFX_Weather_Intensity"));
            m_weatherMainColor = Shader.PropertyToID(("_MainColor"));
            m_weatherColor = Shader.PropertyToID(("_Color"));

            //PW Water
            m_waterDepthRamp = Shader.PropertyToID(("_WaterDepthRamp"));
            m_waterSmoothness = Shader.PropertyToID(("_Smoothness"));
            m_waterFoamTex = Shader.PropertyToID(("_FoamTex"));
            m_waterFoamTexTiling = Shader.PropertyToID(("_FoamTexTile"));
            m_waterFoamRampAlpha = Shader.PropertyToID(("_FoamRampAlpha"));
            m_waterFoamDepth = Shader.PropertyToID(("_FoamDepth"));
            m_waterTransparentDepth = Shader.PropertyToID(("_TransparentDepth"));
            m_waterReflectionDistortion = Shader.PropertyToID(("_ReflectionDistortion"));
            m_waterReflectionStrength = Shader.PropertyToID(("_ReflectionStrength"));
            m_waterWaveShoreMove = Shader.PropertyToID(("_WaveShoreMove"));
            m_waterMetallic = Shader.PropertyToID(("_Metallic"));
            m_underwaterColor = Shader.PropertyToID("_UnderWaterColor");
            InvViewProjection = Shader.PropertyToID("_InvViewProjection");
            CameraRoll = Shader.PropertyToID("_CameraRoll");
            _normalLayer0_ID = Shader.PropertyToID("_NormalLayer0");       
            _normalLayer1_ID = Shader.PropertyToID("_NormalLayer1");       
            _normalLayer2_ID = Shader.PropertyToID("_NormalLayer2");       
            _normalLayer0Scale_ID = Shader.PropertyToID("_NormalLayer0Scale");  
            _normalLayer1Scale_ID = Shader.PropertyToID("_NormalLayer1Scale");  
            _normalLayer2Scale_ID = Shader.PropertyToID("_NormalLayer2Scale");  
            _normalSpeed_ID = Shader.PropertyToID("_NormalSpeed");  
            _normalFadeStart_ID = Shader.PropertyToID("_NormalFadeStart");  
            _normalFadeDistance_ID = Shader.PropertyToID("_NormalFadeDistance");  
            _normalTile_ID = Shader.PropertyToID("_NormalTile");         
            _waveShoreClamp_ID = Shader.PropertyToID("_WaveShoreClamp");     
            _waveLength_ID = Shader.PropertyToID("_WaveLength");     
            _waveSteepness_ID = Shader.PropertyToID("_WaveSteepness");     
            _waveSpeed_ID = Shader.PropertyToID("_WaveSpeed");     
            _waveDirection_ID = Shader.PropertyToID("_WaveDirection"); 
            _edgeWaterColor_ID = Shader.PropertyToID("_EdgeWaterColor");
            _edgeWaterDist_ID = Shader.PropertyToID("_EdgeWaterDist");
            _grabID	= Shader.PropertyToID ( "_EchoTemp");
            m_foamBubblesTexture = Shader.PropertyToID ( "_FoamBubbles");
            m_foamBubblesScale = Shader.PropertyToID ( "_FoamBubblesScale");
            m_foamBubblesTiling = Shader.PropertyToID ( "_FoamBubblesTile");
            m_foamStrength = Shader.PropertyToID ( "_FoamStrength");
            m_foamBubblesStrength = Shader.PropertyToID ( "_FoamBubblesStrength");
            m_foamEdge = Shader.PropertyToID ( "_FoamEdge");
            m_foamMoveSpeed = Shader.PropertyToID ( "_FoamMoveSpeed");
            m_blendSRC = Shader.PropertyToID ("_PW_SrcBlend");
            m_blendDST = Shader.PropertyToID ("_PW_DstBlend");

            //PW Global
            m_globalLightDirection = Shader.PropertyToID(("_PW_MainLightDir"));
            m_globalLightColor = Shader.PropertyToID(("_PW_MainLightColor"));
            m_globalLightIntensity = Shader.PropertyToID(("_PW_MainLightIntensity"));
            m_globalLightSpecColor = Shader.PropertyToID(("_PW_MainLightSpecular"));
            m_globalWind = Shader.PropertyToID(("_WaveDirection"));
            m_globalReflectionTexture = Shader.PropertyToID(("_ReflectionTex"));
            m_globalAmbientColor = Shader.PropertyToID(("_AmbientColor"));
            m_hdrpZTest = Shader.PropertyToID(("_ZTestTransparent"));

            //Unity Skybox
            m_unitySkyboxGroundColor = Shader.PropertyToID(("_GroundColor"));
            m_unitySkyboxAtmosphereThickness = Shader.PropertyToID(("_AtmosphereThickness"));
            m_unitySkyboxSunDisk = Shader.PropertyToID(("_SunDisk"));
            m_unitySkyboxSunSize = Shader.PropertyToID(("_SunSize"));
            m_unitySkyboxSunSizeConvergence = Shader.PropertyToID(("_SunSizeConvergence"));
            m_unitySkyboxTint = Shader.PropertyToID(("_SkyTint"));
            m_unitySkyboxTintHDRI = Shader.PropertyToID(("_Tint"));
            m_unitySkyboxCubemap = Shader.PropertyToID(("_Tex"));
            m_unitySkyboxExposure = Shader.PropertyToID(("_Exposure"));
            m_unitySkyboxRotation = Shader.PropertyToID(("_Rotation"));
        }
    }
}