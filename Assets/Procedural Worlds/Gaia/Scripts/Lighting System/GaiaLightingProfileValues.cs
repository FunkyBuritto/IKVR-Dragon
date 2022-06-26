using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gaia
{
    [System.Serializable]
    public class PWSkyAtmosphere
    {
        public Gradient TODSkyboxTint;
        public AnimationCurve TODSkyboxExposure;
        public Gradient TODSunColor;
        public Gradient TODFogColor;
        public Gradient TODAmbientSkyColor;
        public Gradient TODAmbientEquatorColor;
        public Gradient TODAmbientGroundColor;
        public AnimationCurve TODSunIntensity;
        public AnimationCurve TODSunShadowStrength;
        public AnimationCurve TODCloudHeightLevelDensity;
        public AnimationCurve TODCloudHeightLevelThickness;
        public AnimationCurve TODCloudHeightLevelSpeed;
        public AnimationCurve TODCloudOpacity;
        public AnimationCurve CloudDomeBrightness;
        public AnimationCurve TODAmbientIntensity;
        public AnimationCurve TODAtmosphereThickness;
        public AnimationCurve TODFogDensity;
        public AnimationCurve TODFogStartDistance;
        public AnimationCurve TODFogEndDistance;
        public AnimationCurve TODHDRPFogBaseHeight;
        public AnimationCurve TODHDRPFogAnisotropy;
        public AnimationCurve TODHDRPFogLightProbeDimmer;
        public AnimationCurve TODHDRPFogDepthExtent;
        public Gradient TODHDRPGroundTint;
        public Gradient TODHDRPFogAlbedo;
        public AnimationCurve TODSunSize;
        public AnimationCurve TODSunSizeConvergence;
        public AnimationCurve TODPostProcessExposure;
        public AnimationCurve TODSkyboxFogHeight;
        public AnimationCurve TODSkyboxFogGradient;
        public GaiaConstants.CloudRenderQueue CloudRenderQueue = GaiaConstants.CloudRenderQueue.Background1000;
        public bool CloudGPUInstanced = true;
        
#if GAIA_PRO_PRESENT
        public void SetDefaults()
        {
            if (GaiaUtils.CheckGradientColorKeys(TODSunColor.colorKeys, Color.white))
            {
                TODSunColor = CreateTODSunColor();
            }
            if (GaiaUtils.CheckGradientColorKeys(TODFogColor.colorKeys, Color.white))
            {
                TODFogColor = CreateTODFogColor();
            }
            if (GaiaUtils.CheckGradientColorKeys(TODAmbientSkyColor.colorKeys, Color.white))
            {
                TODAmbientSkyColor = CreateTODAmbientSkyColor();
            }
            if (GaiaUtils.CheckGradientColorKeys(TODAmbientEquatorColor.colorKeys, Color.white))
            {
                TODAmbientEquatorColor = CreateTODAmbientEquatorColor();
            }
            if (GaiaUtils.CheckGradientColorKeys(TODAmbientGroundColor.colorKeys, Color.white))
            {
                TODAmbientGroundColor = CreateTODAmbientGroundColor();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODSunIntensity.keys, 0f))
            {
                TODSunIntensity = CreateTODSunBrightnessIntensity();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODSunShadowStrength.keys, 0f))
            {
                TODSunShadowStrength = CreateTODSunShadowStrength();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODCloudHeightLevelDensity.keys, 0f))
            {
                TODCloudHeightLevelDensity = CreateTODCloudHeightDensity();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODCloudHeightLevelThickness.keys, 0f))
            {
                TODCloudHeightLevelThickness = CreateTODCloudHeightThickness();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODCloudHeightLevelSpeed.keys, 0f))
            {
                TODCloudHeightLevelSpeed = CreateTODCloudHeightSpeed();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODCloudOpacity.keys, 0f))
            {
                TODCloudOpacity = CreateTODCloudOpacity();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(CloudDomeBrightness.keys, 0f))
            {
                CloudDomeBrightness = CreateTODCloudDomeBrightness();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODAmbientIntensity.keys, 0f))
            {
                TODAmbientIntensity = CreateTODAmbientIntensity();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODAtmosphereThickness.keys, 0f))
            {
                TODAtmosphereThickness = CreateTODAtmosphereThickness();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODFogDensity.keys, 0f))
            {
                TODFogDensity = CreateTODFogDensity();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODFogStartDistance.keys, 0f))
            {
                TODFogStartDistance = CreateTODFogStartDistance();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODFogEndDistance.keys, 0f))
            {
                TODFogEndDistance = CreateTODFogEndDistance();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODSunSize.keys, 0f))
            {
                TODSunSize = CreateTODSunSizeCurve();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODSunSizeConvergence.keys, 0f))
            {
                TODSunSizeConvergence = CreateTODSunSizeConvergenceCurve();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODPostProcessExposure.keys, 0f))
            {
                TODPostProcessExposure = CreateTODPostProcessExposureCurve();
            }
            if (GaiaUtils.CheckGradientColorKeys(TODSkyboxTint.colorKeys, Color.white))
            {
                TODSkyboxTint = CreateTODSkyboxTint();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODSkyboxExposure.keys, 0f))
            {
                TODSkyboxExposure = CreateTODSkyboxExposure();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODHDRPFogAnisotropy.keys, 0f))
            {
                TODHDRPFogAnisotropy = CreateTODHDRPFogAnisotropy();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODHDRPFogBaseHeight.keys, 0f))
            {
                TODHDRPFogBaseHeight = CreateTODHDRPFogBaseHeight();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODHDRPFogDepthExtent.keys, 0f))
            {
                TODHDRPFogDepthExtent = CreateTODHDRPFogDepthExtent();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODHDRPFogLightProbeDimmer.keys, 0f))
            {
                TODHDRPFogLightProbeDimmer = CreateTODHDRPFogLightProbeDimmer();
            }
            if (GaiaUtils.CheckGradientColorKeys(TODHDRPGroundTint.colorKeys, Color.white))
            {
                TODHDRPGroundTint = CreateTODHDRPGroundTint();
            }
            if (GaiaUtils.CheckGradientColorKeys(TODHDRPFogAlbedo.colorKeys, Color.white))
            {
                TODHDRPFogAlbedo = CreateTODHDRPFogAlbedo();
            }
            if (TODSkyboxFogHeight == null || GaiaUtils.CheckAnimationCurveKeys(TODSkyboxFogHeight.keys, 0f))
            {
                TODSkyboxFogHeight = CreateTODSkyboxFogHeight();
            }
            if (TODSkyboxFogGradient == null || GaiaUtils.CheckAnimationCurveKeys(TODSkyboxFogGradient.keys, 0f))
            {
                TODSkyboxFogGradient = CreateTODSkyboxFogGradient();
            }
        }
        private bool CheckIfDefaultsNeedToBeSet()
        {
            if (GaiaUtils.CheckGradientColorKeys(TODSkyboxTint.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(TODSunColor.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(TODFogColor.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(TODAmbientSkyColor.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(TODAmbientEquatorColor.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(TODAmbientGroundColor.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODSunIntensity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODSunShadowStrength.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODCloudHeightLevelDensity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODCloudHeightLevelThickness.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODCloudHeightLevelSpeed.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODCloudOpacity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(CloudDomeBrightness.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODAmbientIntensity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODAtmosphereThickness.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODFogDensity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODFogStartDistance.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODFogEndDistance.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODHDRPFogBaseHeight.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODHDRPFogAnisotropy.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODHDRPFogLightProbeDimmer.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODHDRPFogDepthExtent.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(TODHDRPGroundTint.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(TODHDRPFogAlbedo.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODSunSize.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODSunSizeConvergence.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODPostProcessExposure.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(TODSkyboxExposure.keys, 0f))
            {
                return true;
            }
            if (TODSkyboxFogHeight == null || GaiaUtils.CheckAnimationCurveKeys(TODSkyboxFogHeight.keys, 0f))
            {
                return true;
            }
            if (TODSkyboxFogGradient == null || GaiaUtils.CheckAnimationCurveKeys(TODSkyboxFogGradient.keys, 0f))
            {
                return true;
            }

            return false;
        }
        public void Load(ProceduralWorldsGlobalWeather globalWeather, bool checkDefaults = true)
        {
            if (globalWeather == null)
            {
                return;
            }

            if (checkDefaults)
            {
                if (CheckIfDefaultsNeedToBeSet())
                {
                    SetDefaults();
                }
            }

            globalWeather.TODSkyboxTint = TODSkyboxTint;
            globalWeather.TODSkyboxExposure = TODSkyboxExposure;
            globalWeather.TODSunColor = TODSunColor;
            globalWeather.TODFogColor = TODFogColor;
            globalWeather.TODAmbientSkyColor = TODAmbientSkyColor;
            globalWeather.TODAmbientEquatorColor = TODAmbientEquatorColor;
            globalWeather.TODAmbientGroundColor = TODAmbientGroundColor;
            globalWeather.TODSunIntensity = TODSunIntensity;
            globalWeather.TODSunShadowStrength = TODSunShadowStrength;
            globalWeather.TODCloudHeightLevelDensity = TODCloudHeightLevelDensity;
            globalWeather.TODCloudHeightLevelThickness = TODCloudHeightLevelThickness;
            globalWeather.TODCloudHeightLevelSpeed = TODCloudHeightLevelSpeed;
            globalWeather.TODCloudOpacity = TODCloudOpacity;
            globalWeather.CloudDomeBrightness = CloudDomeBrightness;
            globalWeather.TODAmbientIntensity = TODAmbientIntensity;
            globalWeather.TODAtmosphereThickness = TODAtmosphereThickness;
            globalWeather.TODFogDensity = TODFogDensity;
            globalWeather.TODFogStartDistance = TODFogStartDistance;
            globalWeather.TODFogEndDistance = TODFogEndDistance;
            globalWeather.TODHDRPFogBaseHeight = TODHDRPFogBaseHeight;
            globalWeather.TODHDRPFogAnisotropy = TODHDRPFogAnisotropy;
            globalWeather.TODHDRPFogLightProbeDimmer = TODHDRPFogLightProbeDimmer;
            globalWeather.TODHDRPFogDepthExtent = TODHDRPFogDepthExtent;
            globalWeather.TODHDRPGroundTint = TODHDRPGroundTint;
            globalWeather.TODHDRPFogAlbedo = TODHDRPFogAlbedo;
            globalWeather.TODSunSize = TODSunSize;
            globalWeather.TODSunSizeConvergence = TODSunSizeConvergence;
            globalWeather.TODPostProcessExposure = TODPostProcessExposure;
            globalWeather.TODSkyboxFogHeight = TODSkyboxFogHeight;
            globalWeather.TODSkyboxFogGradient = TODSkyboxFogGradient;
        }
        public void Save(ProceduralWorldsGlobalWeather globalWeather)
        {
            if (globalWeather == null)
            {
                return;
            }

            TODSkyboxTint = globalWeather.TODSkyboxTint;
            TODSkyboxExposure = globalWeather.TODSkyboxExposure;
            TODSunColor = globalWeather.TODSunColor;
            TODFogColor = globalWeather.TODFogColor;
            TODAmbientSkyColor = globalWeather.TODAmbientSkyColor;
            TODAmbientEquatorColor = globalWeather.TODAmbientEquatorColor;
            TODAmbientGroundColor = globalWeather.TODAmbientGroundColor;
            TODSunIntensity = globalWeather.TODSunIntensity;
            TODSunShadowStrength = globalWeather.TODSunShadowStrength;
            TODCloudHeightLevelDensity = globalWeather.TODCloudHeightLevelDensity;
            TODCloudHeightLevelThickness = globalWeather.TODCloudHeightLevelThickness;
            TODCloudHeightLevelSpeed = globalWeather.TODCloudHeightLevelSpeed;
            TODCloudOpacity = globalWeather.TODCloudOpacity;
            CloudDomeBrightness = globalWeather.CloudDomeBrightness;
            TODAmbientIntensity = globalWeather.TODAmbientIntensity;
            TODAtmosphereThickness = globalWeather.TODAtmosphereThickness;
            TODFogDensity = globalWeather.TODFogDensity;
            TODFogStartDistance = globalWeather.TODFogStartDistance;
            TODFogEndDistance = globalWeather.TODFogEndDistance;
            TODHDRPFogBaseHeight = globalWeather.TODHDRPFogBaseHeight;
            TODHDRPFogAnisotropy = globalWeather.TODHDRPFogAnisotropy;
            TODHDRPFogLightProbeDimmer = globalWeather.TODHDRPFogLightProbeDimmer;
            TODHDRPFogDepthExtent = globalWeather.TODHDRPFogDepthExtent;
            TODHDRPGroundTint = globalWeather.TODHDRPGroundTint;
            TODHDRPFogAlbedo = globalWeather.TODHDRPFogAlbedo;
            TODSunSize = globalWeather.TODSunSize;
            TODSunSizeConvergence = globalWeather.TODSunSizeConvergence;
            TODPostProcessExposure = globalWeather.TODPostProcessExposure;
            TODSkyboxFogHeight = globalWeather.TODSkyboxFogHeight;
            TODSkyboxFogGradient = globalWeather.TODSkyboxFogGradient;
        }
        public void New(ProceduralWorldsGlobalWeather globalWeather)
        {
            if (globalWeather == null)
            {
                return;
            }

            globalWeather.TODSkyboxTint = TODSkyboxTint;
            globalWeather.TODSkyboxExposure = TODSkyboxExposure;
            globalWeather.TODSunColor = TODSunColor;
            globalWeather.TODFogColor = TODFogColor;
            globalWeather.TODAmbientSkyColor = TODAmbientSkyColor;
            globalWeather.TODAmbientEquatorColor = TODAmbientEquatorColor;
            globalWeather.TODAmbientGroundColor = TODAmbientGroundColor;
            globalWeather.TODSunIntensity = TODSunIntensity;
            globalWeather.TODSunShadowStrength = TODSunShadowStrength;
            globalWeather.TODCloudHeightLevelDensity = TODCloudHeightLevelDensity;
            globalWeather.TODCloudHeightLevelThickness = TODCloudHeightLevelThickness;
            globalWeather.TODCloudHeightLevelSpeed = TODCloudHeightLevelSpeed;
            globalWeather.TODCloudOpacity = TODCloudOpacity;
            globalWeather.CloudDomeBrightness = CloudDomeBrightness;
            globalWeather.TODAmbientIntensity = TODAmbientIntensity;
            globalWeather.TODAtmosphereThickness = TODAtmosphereThickness;
            globalWeather.TODFogDensity = TODFogDensity;
            globalWeather.TODFogStartDistance = TODFogStartDistance;
            globalWeather.TODFogEndDistance = TODFogEndDistance;
            globalWeather.TODHDRPFogBaseHeight = TODHDRPFogBaseHeight;
            globalWeather.TODHDRPFogAnisotropy = TODHDRPFogAnisotropy;
            globalWeather.TODHDRPFogLightProbeDimmer = TODHDRPFogLightProbeDimmer;
            globalWeather.TODHDRPFogDepthExtent = TODHDRPFogDepthExtent;
            globalWeather.TODHDRPGroundTint = TODHDRPGroundTint;
            globalWeather.TODHDRPFogAlbedo = TODHDRPFogAlbedo;
            globalWeather.TODSunSize = TODSunSize;
            globalWeather.TODSunSizeConvergence = TODSunSizeConvergence;
            globalWeather.TODPostProcessExposure = TODPostProcessExposure;
        }
#endif

        #region Set Defaults Utils

        private Gradient CreateTODSunColor()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[7];
            colorKeys[0].time = 0f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("6191CF");

            colorKeys[1].time = 0.24f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("6B9CBE");

            colorKeys[2].time = 0.26f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("FFCB97");

            colorKeys[3].time = 0.5f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("FFEBD8");

            colorKeys[4].time = 0.74f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("FFBD96");

            colorKeys[5].time = 0.76f;
            colorKeys[5].color = GaiaUtils.GetColorFromHTML("AAD2EE");

            colorKeys[6].time = 1f;
            colorKeys[6].color = GaiaUtils.GetColorFromHTML("6191CF");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }
        private Gradient CreateTODFogColor()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[8];
            colorKeys[0].time = 0.245f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("041216");

            colorKeys[1].time = 0.253f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("63533D");

            colorKeys[2].time = 0.271f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("A6A6A6");

            colorKeys[3].time = 0.3f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("A2D3FF");

            colorKeys[4].time = 0.7f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("6C97BE");

            colorKeys[5].time = 0.729f;
            colorKeys[5].color = GaiaUtils.GetColorFromHTML("8E8E8E");

            colorKeys[6].time = 0.74f;
            colorKeys[6].color = GaiaUtils.GetColorFromHTML("8E7B6C");

            colorKeys[7].time = 0.75f;
            colorKeys[7].color = GaiaUtils.GetColorFromHTML("181817");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }
        private Gradient CreateTODAmbientSkyColor()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[7];
            colorKeys[0].time = 0.24f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("282B31");

            colorKeys[1].time = 0.26f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("CC938C");

            colorKeys[2].time = 0.441f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("9BCAFF");

            colorKeys[3].time = 0.507f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("9EC9FF");

            colorKeys[4].time = 0.74f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("FFC4AA");

            colorKeys[5].time = 0.76f;
            colorKeys[5].color = GaiaUtils.GetColorFromHTML("1F3959");

            colorKeys[6].time = 1f;
            colorKeys[6].color = GaiaUtils.GetColorFromHTML("111213");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }
        private Gradient CreateTODAmbientEquatorColor()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[8];
            colorKeys[0].time = 0.24f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("282B31");

            colorKeys[1].time = 0.26f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("6D9AF3");

            colorKeys[2].time = 0.317f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("EEFFAC");

            colorKeys[3].time = 0.407f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("77E1F1");

            colorKeys[4].time = 0.494f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("9BBCFF");

            colorKeys[5].time = 0.74f;
            colorKeys[5].color = GaiaUtils.GetColorFromHTML("FF7945");

            colorKeys[6].time = 0.76f;
            colorKeys[6].color = GaiaUtils.GetColorFromHTML("485678");

            colorKeys[7].time = 0.976f;
            colorKeys[7].color = GaiaUtils.GetColorFromHTML("282B31");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }
        private Gradient CreateTODAmbientGroundColor()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[8];
            colorKeys[0].time = 0f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("282B31");

            colorKeys[1].time = 0.24f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("001F3A");

            colorKeys[2].time = 0.26f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("FFD499");

            colorKeys[3].time = 0.456f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("A3D5EE");

            colorKeys[4].time = 0.603f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("85BBC8");

            colorKeys[5].time = 0.74f;
            colorKeys[5].color = GaiaUtils.GetColorFromHTML("FF7945");

            colorKeys[6].time = 0.76f;
            colorKeys[6].color = GaiaUtils.GetColorFromHTML("485678");

            colorKeys[7].time = 1f;
            colorKeys[7].color = GaiaUtils.GetColorFromHTML("1A1B1D");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }
        private AnimationCurve CreateTODSunBrightnessIntensity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 1.2f), new Keyframe(0.225f, 0.95f), new Keyframe(0.35f, 1.3f), new Keyframe(0.625f, 1.45f), new Keyframe(0.745f, 0.95f), new Keyframe(1f, 1.2f));
            return animationCurve;
        }
        private AnimationCurve CreateTODSunShadowStrength()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.85f), new Keyframe(0.25f, 0.95f), new Keyframe(0.5f, 0.8f), new Keyframe(0.75f, 0.95f),  new Keyframe(1f, 0.85f));
            return animationCurve;
        }
        private AnimationCurve CreateTODCloudHeightDensity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.08964729f), new Keyframe(0.24f, 0.1575743f), new Keyframe(0.366f, 0.0305139f), new Keyframe(0.5f, 0.1434333f), new Keyframe(0.763f, 0.05979303f), new Keyframe(1f, 0.08964729f));
            return animationCurve;
        }
        private AnimationCurve CreateTODCloudHeightThickness()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.8173501f), new Keyframe(0.124f, 1.25209f), new Keyframe(0.316f, 1.32671f), new Keyframe(0.5f, 1.025839f), new Keyframe(0.61f, 1.299839f), new Keyframe(0.72f, 1.102154f), new Keyframe(1f, 0.95f));
            return animationCurve;
        }
        private AnimationCurve CreateTODCloudHeightSpeed()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.5f, 1.00359f), new Keyframe(1f, 1f));
            return animationCurve;
        }
        private AnimationCurve CreateTODCloudDomeBrightness()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.25f), new Keyframe(0.25f, 0.15f), new Keyframe(0.5f, 0.65f), new Keyframe(0.75f, 0.15f), new Keyframe(1f, 0.25f));
            return animationCurve;
        }
        private AnimationCurve CreateTODCloudOpacity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(0.25f, 0.2f), new Keyframe(0.3f, 0.45f), new Keyframe(0.5f, 0.85f), new Keyframe(0.725f, 0.45f), new Keyframe(0.75f, 0.23f), new Keyframe(1f, 0.1f));
            return animationCurve;
        }
        private AnimationCurve CreateTODAmbientIntensity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.6250473f), new Keyframe(0.1617298f, 0.7450473f), new Keyframe(0.3f, 0.7750473f), new Keyframe(0.62f, 0.7450473f), new Keyframe(1f, 0.6250473f));
            return animationCurve;
        }
        private AnimationCurve CreateTODAtmosphereThickness()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(0.15f, 0.125f), new Keyframe(0.3f, 0.95f), new Keyframe(0.5f, 0.85f), new Keyframe(0.725f, 0.95f), new Keyframe(0.775f, 0.125f), new Keyframe(1f, 0.1f));
            return animationCurve;
        }
        private AnimationCurve CreateTODFogDensity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.015f), new Keyframe(0.1f, 0.0048f), new Keyframe(0.3f, 0.0014f), new Keyframe(0.5f, 0.0005f), new Keyframe(0.75f, 0.0022f), new Keyframe(1f, 0.015f));
            return animationCurve;
        }
        private AnimationCurve CreateTODFogStartDistance()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, -5f), new Keyframe(0.25f, 0f), new Keyframe(0.5f, 15f), new Keyframe(0.75f, 0f), new Keyframe(1f, -5f));
            return animationCurve;
        }
        private AnimationCurve CreateTODFogEndDistance()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 900f), new Keyframe(0.25f, 1250f), new Keyframe(0.5f, 4500f), new Keyframe(0.75f, 1250f), new Keyframe(1f, 900f));
            return animationCurve;
        }
        private AnimationCurve CreateTODIntensityCurve()
        {         
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.336431f, 0.02609337f), new Keyframe(0.425f, 0.2532162f), new Keyframe(1f, 1f));
            return animationCurve;
        }
        private AnimationCurve CreateTODSunSizeCurve()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.02f), new Keyframe(0.2f, 0.021f), new Keyframe(0.275f, 0.045f), new Keyframe(0.5f, 0.037f), new Keyframe(0.735f, 0.045f), new Keyframe(0.76f, 0.03f), new Keyframe(1f, 0.02f));
            return animationCurve;
        }
        private AnimationCurve CreateTODSunSizeConvergenceCurve()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.25f, 1f), new Keyframe(0.5f, 4f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0.5f));
            return animationCurve;
        }
        private AnimationCurve CreateTODPostProcessExposureCurve()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.25f, 1.1f), new Keyframe(0.5f, 1.25f), new Keyframe(0.75f, 1.1f), new Keyframe(1f, 1f));
            return animationCurve;
        }
        private Gradient CreateTODSkyboxTint()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0].time = 0f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("828282");

            colorKeys[1].time = 0.5f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("C1C1C1");

            colorKeys[2].time = 1f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("828282");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }
        private AnimationCurve CreateTODSkyboxExposure()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.25f, 0.75f), new Keyframe(0.5f, 1f), new Keyframe(0.75f, 0.75f), new Keyframe(1f, 0.6f));
            return animationCurve;
        }
        private AnimationCurve CreateTODHDRPFogAnisotropy()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.85f), new Keyframe(0.25f, 0.95f), new Keyframe(0.5f, 0.85f), new Keyframe(0.75f, 0.95f), new Keyframe(1f, 0.85f));
            return animationCurve;
        }
        private AnimationCurve CreateTODHDRPFogBaseHeight()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 250f), new Keyframe(0.25f, 300f), new Keyframe(0.5f, 275f), new Keyframe(0.75f, 300f), new Keyframe(1f, 250f));
            return animationCurve;
        }
        private AnimationCurve CreateTODHDRPFogDepthExtent()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 550f), new Keyframe(0.25f, 1000f), new Keyframe(0.5f, 550f), new Keyframe(0.75f, 1000f), new Keyframe(1f, 550f));
            return animationCurve;
        }
        private AnimationCurve CreateTODHDRPFogLightProbeDimmer()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.25f, 0.65f), new Keyframe(0.5f, 0.75f), new Keyframe(0.75f, 0.65f), new Keyframe(1f, 0.6f));
            return animationCurve;
        }
        private Gradient CreateTODHDRPGroundTint()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[7];
            colorKeys[0].time = 0.2f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("58AAFF");

            colorKeys[1].time = 0.3f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("FFDAC2");

            colorKeys[2].time = 0.32f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("A6CDEC");

            colorKeys[3].time = 0.5f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("83C7FF");

            colorKeys[4].time = 0.68f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("9ECCF0");

            colorKeys[5].time = 0.7f;
            colorKeys[5].color = GaiaUtils.GetColorFromHTML("FFDAC2");

            colorKeys[6].time = 0.8f;
            colorKeys[6].color = GaiaUtils.GetColorFromHTML("58AAFF");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }
        private Gradient CreateTODHDRPFogAlbedo()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0].time = 0.235f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("57758C");

            colorKeys[1].time = 0.25f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("FFDBC7");

            colorKeys[2].time = 0.5f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("D2E8FF");

            colorKeys[3].time = 0.75f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("FFDBC7");

            colorKeys[4].time = 0.765f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("57758C");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }
        private AnimationCurve CreateTODSkyboxFogHeight()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.01f), new Keyframe(1f, 0.01f));
            return animationCurve;
        }
        private AnimationCurve CreateTODSkyboxFogGradient()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(1f, 0.1f));
            return animationCurve;
        }

        #endregion
    }

    [System.Serializable]
    public class PWSkyWind
    {
        //Wind Settings
        public float WindSpeed = 0.15f;
        public float WindTurbulence = 0.1f;
        public float WindFrequency = 0.2f;
        public float WindDirection = 0f;
        public float WindMultiplier = 1f;

#if GAIA_PRO_PRESENT
        public void Load(ProceduralWorldsGlobalWeather globalWeather)
        {
            if (globalWeather == null)
            {
                return;
            }

            globalWeather.WindSpeed = WindSpeed;
            globalWeather.WindTurbulence = WindTurbulence;
            globalWeather.WindFrequency = WindFrequency;
            globalWeather.WindDirection = WindDirection;
            globalWeather.WindMultiplier = WindMultiplier;
        }
        public void Save(ProceduralWorldsGlobalWeather globalWeather)
        {
            if (globalWeather == null)
            {
                return;
            }

            WindSpeed = globalWeather.WindSpeed;
            WindTurbulence = globalWeather.WindTurbulence;
            WindFrequency = globalWeather.WindFrequency;
            WindDirection = globalWeather.WindDirection;
            WindMultiplier = globalWeather.WindMultiplier;
        }
        public void New(ProceduralWorldsGlobalWeather globalWeather)
        {
            if (globalWeather == null)
            {
                return;
            }

            globalWeather.WindSpeed = WindSpeed;
            globalWeather.WindTurbulence = WindTurbulence;
            globalWeather.WindFrequency = WindFrequency;
            globalWeather.WindDirection = WindDirection;
            globalWeather.WindMultiplier = WindMultiplier;
        }
#endif
    }

    [System.Serializable]
    public class PWSkyWeather
    {
        //Weather
        public float m_weatherFadeDuration = 10f;
        public bool m_modifyFog = true;
        public bool m_modifyWind = true;
        public bool m_modifySkybox = true;
        public bool m_modifyPostProcessing = true;
        public bool m_modifySun = true;
        public bool m_modifyAmbient = true;
        public bool m_modifyClouds = true;

        public PWSkyRain m_rainSettings = new PWSkyRain();
        public PWSkySnow m_snowSettings = new PWSkySnow();
        public PWSkyThunder m_thunderSettings = new PWSkyThunder();

#if GAIA_PRO_PRESENT
        public void Load(ProceduralWorldsGlobalWeather globalWeather, bool checkDefaults = true)
        {
            if (globalWeather == null)
            {
                return;
            }

            globalWeather.m_weatherFadeDuration = m_weatherFadeDuration;
            globalWeather.m_modifyFog = m_modifyFog;
            globalWeather.m_modifyWind = m_modifyWind;
            globalWeather.m_modifySkybox = m_modifySkybox;
            globalWeather.m_modifyPostProcessing = m_modifyPostProcessing;
            globalWeather.m_modifySun = m_modifySun;
            globalWeather.m_modifyAmbient = m_modifyAmbient;
            globalWeather.m_modifyClouds = m_modifyClouds;

            m_rainSettings.Load(globalWeather, checkDefaults);
            m_snowSettings.Load(globalWeather, checkDefaults);
            m_thunderSettings.Load(globalWeather, checkDefaults);
        }
        public void Save(ProceduralWorldsGlobalWeather globalWeather)
        {
            if (globalWeather == null)
            {
                return;
            }

            m_weatherFadeDuration = globalWeather.m_weatherFadeDuration;
            m_modifyFog = globalWeather.m_modifyFog;
            m_modifyWind = globalWeather.m_modifyWind;
            m_modifySkybox = globalWeather.m_modifySkybox;
            m_modifyPostProcessing = globalWeather.m_modifyPostProcessing;
            m_modifySun = globalWeather.m_modifySun;
            m_modifyAmbient = globalWeather.m_modifyAmbient;
            m_modifyClouds = globalWeather.m_modifyClouds;

            m_rainSettings.Save(globalWeather);
            m_snowSettings.Save(globalWeather);
            m_thunderSettings.Save(globalWeather);
        }
        public void New(ProceduralWorldsGlobalWeather globalWeather, PWSkyWeather sourceValues, PWSkyWeather targetValues)
        {
            if (globalWeather == null)
            {
                return;
            }

            globalWeather.m_weatherFadeDuration = m_weatherFadeDuration;
            globalWeather.m_modifyFog = m_modifyFog;
            globalWeather.m_modifyWind = m_modifyWind;
            globalWeather.m_modifySkybox = m_modifySkybox;
            globalWeather.m_modifyPostProcessing = m_modifyPostProcessing;
            globalWeather.m_modifySun = m_modifySun;
            globalWeather.m_modifyAmbient = m_modifyAmbient;
            globalWeather.m_modifyClouds = m_modifyClouds;

            m_rainSettings = new PWSkyRain();
            GaiaUtils.CopyFields(sourceValues.m_rainSettings, targetValues.m_rainSettings);
            m_rainSettings.New(globalWeather);
            m_snowSettings = new PWSkySnow();
            GaiaUtils.CopyFields(sourceValues.m_snowSettings, targetValues.m_snowSettings);
            m_snowSettings.New(globalWeather);
            m_thunderSettings = new PWSkyThunder();
            GaiaUtils.CopyFields(sourceValues.m_thunderSettings, targetValues.m_thunderSettings);
            m_thunderSettings.New(globalWeather);
        }
#endif
    }

    [System.Serializable]
    public class PWSkyRain
    {
        //Rain Settings
        public bool EnableRain = true;
        public float RainIntensity = 400f;
        public float m_rainHeight = 400f;
        public float m_rainStepSize = 0.05f;

#if GAIA_PRO_PRESENT
        public RainMode m_rainMode = RainMode.RandomChance;
        public WeatherSettings m_rainWeatherSettings = new WeatherSettings();

        public void SetDefaults()
        {
            m_rainHeight = 400f;
            m_rainMode = RainMode.RandomChance;
            m_rainWeatherSettings.m_chance = 0.7f;
            m_rainWeatherSettings.m_channelSelection = ChannelSelection.G;
            m_rainWeatherSettings.m_durationMinWaitTime = 120f;
            m_rainWeatherSettings.m_durationMaxWaitTime = 700f;
            m_rainWeatherSettings.m_minWaitTime = 120f;
            m_rainWeatherSettings.m_maxWaitTime = 500f;
            m_rainWeatherSettings.m_windSpeed = 0.8f;
            m_rainWeatherSettings.m_windTurbulence = 0.7f;
            m_rainWeatherSettings.m_windFrequency = 0.6f;
            m_rainWeatherSettings.m_windMultiplier = 5f;

            if (GaiaUtils.CheckGradientColorKeys(m_rainWeatherSettings.m_fogColor.colorKeys, Color.white))
            {
                m_rainWeatherSettings.m_fogColor = CreateWeatherRainFogColor();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_fogDensity.keys, 0f))
            {
                m_rainWeatherSettings.m_fogDensity = CreateWeatherRainFogDensity();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_fogStartDistance.keys, 0f))
            {
                m_rainWeatherSettings.m_fogStartDistance = CreateWeatherRainFogStartDistance();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_fogEndDistance.keys, 0f))
            {
                m_rainWeatherSettings.m_fogEndDistance = CreateWeatherRainFogEndDistance();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_skyboxExposure.keys, 0f))
            {
                m_rainWeatherSettings.m_skyboxExposure = CreateWeatherRainSkyboxExposure();
            }
            if (GaiaUtils.CheckGradientColorKeys(m_rainWeatherSettings.m_skyboxTint.colorKeys, Color.white))
            {
                m_rainWeatherSettings.m_skyboxTint = CreateWeatherRainSkyboxTint();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_skyboxAtmosphereThickness.keys, 0f))
            {
                m_rainWeatherSettings.m_skyboxAtmosphereThickness = CreateWeatherRainSkyboxAtmosphereThickness();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_volumetricDepthExtent.keys, 0f))
            {
                m_rainWeatherSettings.m_volumetricDepthExtent = CreateWeatherRainHDRPFogDepthExtent();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_volumetricGlobalAnisotropy.keys, 0f))
            {
                m_rainWeatherSettings.m_volumetricGlobalAnisotropy = CreateWeatherRainHDRPGlobalAnisotropy();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_volumetricGlobalProbeDimmer.keys, 0f))
            {
                m_rainWeatherSettings.m_volumetricGlobalProbeDimmer = CreateWeatherRainHDRPGlobalProbeDimmer();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_fogHeight.keys, 0f))
            {
                m_rainWeatherSettings.m_fogHeight = CreateWeatherRainHDRPFogHeight();
            }
            if (GaiaUtils.CheckGradientColorKeys(m_rainWeatherSettings.m_sunColor.colorKeys, Color.white))
            {
                m_rainWeatherSettings.m_sunColor = CreateWeatherRainSunColor();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_sunIntensity.keys, 0f))
            {
                m_rainWeatherSettings.m_sunIntensity = CreateWeatherRainSunIntensity();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_fXBloomIntensity.keys, 0f))
            {
                m_rainWeatherSettings.m_fXBloomIntensity = CreateWeatherRainFXBloomIntensity();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_fXTemerature.keys, 0f))
            {
                m_rainWeatherSettings.m_fXTemerature = CreateWeatherRainFXTemperature();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_fXTint.keys, 0f))
            {
                m_rainWeatherSettings.m_fXTint = CreateWeatherRainFXTint();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_fXVignetteIntensity.keys, 0f))
            {
                m_rainWeatherSettings.m_fXVignetteIntensity = CreateWeatherRainFXVignetteIntensity();
            }
            if (GaiaUtils.CheckGradientColorKeys(m_rainWeatherSettings.m_fXColorFilter.colorKeys, Color.white))
            {
                m_rainWeatherSettings.m_fXColorFilter = CreateWeatherRainFXColorFilter();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_ambientIntensity.keys, 0f))
            {
                m_rainWeatherSettings.m_ambientIntensity = CreateWeatherRainAmbientIntensity();
            }
            if (GaiaUtils.CheckGradientColorKeys(m_rainWeatherSettings.m_ambientSkyColor.colorKeys, Color.white))
            {
                m_rainWeatherSettings.m_ambientSkyColor = CreateWeatherRainAmbientSky();
            }
            if (GaiaUtils.CheckGradientColorKeys(m_rainWeatherSettings.m_ambientEquatorColor.colorKeys, Color.white))
            {
                m_rainWeatherSettings.m_ambientEquatorColor = CreateWeatherRainAmbientEquator();
            }
            if (GaiaUtils.CheckGradientColorKeys(m_rainWeatherSettings.m_ambientGroundColor.colorKeys, Color.white))
            {
                m_rainWeatherSettings.m_ambientGroundColor = CreateWeatherRainAmbientGround();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_cloudDomeBrightness.keys, 0f))
            {
                m_rainWeatherSettings.m_cloudDomeBrightness = CreateWeatherRainCloudDomeBrightness();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_cloudDensity.keys, 0f))
            {
                m_rainWeatherSettings.m_cloudDensity = CreateWeatherRainCloudDensity();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_cloudThickness.keys, 0f))
            {
                m_rainWeatherSettings.m_cloudThickness = CreateWeatherRainCloudThickness();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_newCloudOpacity.keys, 0f))
            {
                m_rainWeatherSettings.m_newCloudOpacity = CreateWeatherRainCloudOpacity();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_cloudSpeed.keys, 0f))
            {
                m_rainWeatherSettings.m_cloudSpeed = CreateWeatherRainCloudSpeed();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_weatherParticleAlpha.keys, 0f))
            {
                m_rainWeatherSettings.m_weatherParticleAlpha = CreateWeatherRainParticleOpacity();
            } 
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_skyboxSkyboxFogHeight.keys, 0f))
            {
                m_rainWeatherSettings.m_skyboxSkyboxFogHeight = CreateTODSkyboxFogHeight();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_skyboxSkyboxFogGradient.keys, 0f))
            {
                m_rainWeatherSettings.m_skyboxSkyboxFogGradient = CreateTODSkyboxFogGradient();
            }
        }
        private bool CheckIfDefaultsNeedToBeSet()
        {
            if (GaiaUtils.CheckGradientColorKeys(m_rainWeatherSettings.m_fogColor.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_fogStartDistance.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_fogEndDistance.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_fogDensity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_skyboxExposure.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(m_rainWeatherSettings.m_skyboxTint.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_skyboxAtmosphereThickness.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(m_rainWeatherSettings.m_sunColor.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_sunIntensity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_fXTemerature.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_fXTint.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(m_rainWeatherSettings.m_fXColorFilter.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_fXBloomIntensity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_fXVignetteIntensity.keys, 0f))

            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_ambientIntensity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(m_rainWeatherSettings.m_ambientSkyColor.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(m_rainWeatherSettings.m_ambientEquatorColor.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(m_rainWeatherSettings.m_ambientGroundColor.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_cloudDomeBrightness.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_volumetricDepthExtent.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_volumetricGlobalAnisotropy.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_volumetricGlobalProbeDimmer.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_fogHeight.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_cloudDensity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_cloudThickness.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_cloudSpeed.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_newCloudOpacity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_weatherParticleAlpha.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_skyboxSkyboxFogHeight.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_rainWeatherSettings.m_skyboxSkyboxFogGradient.keys, 0f))
            {
                return true;
            }
            return false;
        }
        public void Load(ProceduralWorldsGlobalWeather globalWeather, bool checkDefaults = true)
        {
            if (globalWeather == null)
            {
                return;
            }

            if (checkDefaults)
            {
                if (CheckIfDefaultsNeedToBeSet())
                {
                    SetDefaults();
                }
            }

            globalWeather.EnableRain = EnableRain;
            globalWeather.RainIntensity = RainIntensity;
            globalWeather.m_rainMode = m_rainMode;
            globalWeather.m_rainHeight = m_rainHeight;
            globalWeather.m_rainStepSize = m_rainStepSize;
            globalWeather.m_rainWeatherSettings.Load(m_rainWeatherSettings, globalWeather, true);
        }
        public void Save(ProceduralWorldsGlobalWeather globalWeather)
        {
            if (globalWeather == null)
            {
                return;
            }

            EnableRain = globalWeather.EnableRain;
            RainIntensity = globalWeather.RainIntensity;
            m_rainMode = globalWeather.m_rainMode;
            m_rainHeight = globalWeather.m_rainHeight;
            m_rainStepSize = globalWeather.m_rainStepSize;
            globalWeather.m_rainWeatherSettings.Save(m_rainWeatherSettings, globalWeather, true);
        }
        public void New(ProceduralWorldsGlobalWeather globalWeather)
        {
            m_rainHeight = 400f;
            m_rainMode = RainMode.RandomChance;
            m_rainWeatherSettings.m_chance = 0.7f;
            m_rainWeatherSettings.m_channelSelection = ChannelSelection.G;
            m_rainWeatherSettings.m_durationMinWaitTime = 120f;
            m_rainWeatherSettings.m_durationMaxWaitTime = 700f;
            m_rainWeatherSettings.m_minWaitTime = 120f;
            m_rainWeatherSettings.m_maxWaitTime = 500f;
            m_rainWeatherSettings.m_windSpeed = 0.8f;
            m_rainWeatherSettings.m_windTurbulence = 0.7f;
            m_rainWeatherSettings.m_windFrequency = 0.6f;
            m_rainWeatherSettings.m_windMultiplier = 5f;

            globalWeather.m_rainWeatherSettings.Save(m_rainWeatherSettings, globalWeather, true);
        }
#endif

        #region Set Defaults Utils

        private Gradient CreateWeatherRainFogColor()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[8];
            colorKeys[0].time = 0.245f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("051F26");

            colorKeys[1].time = 0.26f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("483D2D");

            colorKeys[2].time = 0.271f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("6A6A6A");

            colorKeys[3].time = 0.3f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("3E4957");

            colorKeys[4].time = 0.7f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("3E4957");

            colorKeys[5].time = 0.729f;
            colorKeys[5].color = GaiaUtils.GetColorFromHTML("4B4B4B");

            colorKeys[6].time = 0.74f;
            colorKeys[6].color = GaiaUtils.GetColorFromHTML("594D44");

            colorKeys[7].time = 0.75f;
            colorKeys[7].color = GaiaUtils.GetColorFromHTML("091C22");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        private AnimationCurve CreateWeatherRainFogDensity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.05f), new Keyframe(0.1f, 0.01f), new Keyframe(0.3f, 0.0085f), new Keyframe(0.5f, 0.007f), new Keyframe(0.75f, 0.0085f), new Keyframe(1f, 0.05f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherRainFogStartDistance()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, -5f), new Keyframe(0.25f, 0f), new Keyframe(0.5f, 15f), new Keyframe(0.75f, 0f), new Keyframe(1f, -5f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherRainFogEndDistance()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 600f), new Keyframe(0.25f, 900f), new Keyframe(0.5f, 2300f), new Keyframe(0.75f, 900f), new Keyframe(1f, 600f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherRainSkyboxExposure()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(0.5f, 0.55f), new Keyframe(1f, 0.1f));
            return animationCurve;
        }

        private Gradient CreateWeatherRainSkyboxTint()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0].time = 0.24f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("24262B");

            colorKeys[1].time = 0.26f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("A4816E");

            colorKeys[2].time = 0.5f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("6C7F98");

            colorKeys[3].time = 0.74f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("A16A54");

            colorKeys[4].time = 0.76f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("24262B");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        private AnimationCurve CreateWeatherRainSkyboxAtmosphereThickness()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.125f), new Keyframe(0.15f, 0.125f), new Keyframe(0.3f, 0.7f), new Keyframe(0.5f, 0.65f), new Keyframe(0.725f, 0.7f), new Keyframe(0.775f, 0.125f), new Keyframe(1f, 0.125f));
            return animationCurve;
        }

        private Gradient CreateWeatherRainSunColor()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[7];
            colorKeys[0].time = 0f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("507AB0");

            colorKeys[1].time = 0.24f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("5F89A6");

            colorKeys[2].time = 0.26f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("D1A87F");

            colorKeys[3].time = 0.5f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("C8B5A3");

            colorKeys[4].time = 0.74f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("DDA380");

            colorKeys[5].time = 0.76f;
            colorKeys[5].color = GaiaUtils.GetColorFromHTML("82A1B7");

            colorKeys[6].time = 1f;
            colorKeys[6].color = GaiaUtils.GetColorFromHTML("5476A1");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        private AnimationCurve CreateWeatherRainSunIntensity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.225f, 0.3f), new Keyframe(0.35f, 0.9f), new Keyframe(0.625f, 1f), new Keyframe(0.745f, 0.3f), new Keyframe(1f, 0.5f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherRainFXTemperature()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, -4f), new Keyframe(0.225f, -4f), new Keyframe(0.35f, -1f), new Keyframe(0.625f, 0f), new Keyframe(0.745f, -4f), new Keyframe(1f, -4f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherRainFXTint()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.225f, 0f), new Keyframe(0.35f, -2f), new Keyframe(0.625f, -1.8f), new Keyframe(0.745f, 0f), new Keyframe(1f, 0f));
            return animationCurve;
        }

        private Gradient CreateWeatherRainFXColorFilter()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0].time = 0f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("B8E9FF");

            colorKeys[1].time = 0.5f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("FFFFFF");

            colorKeys[2].time = 1f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("B8E9FF");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        private AnimationCurve CreateWeatherRainFXBloomIntensity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 3f), new Keyframe(1f, 3f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherRainFXVignetteIntensity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.225f, 0.45f), new Keyframe(0.39f, 0.35f), new Keyframe(0.625f, 0.39f), new Keyframe(0.745f, 0.45f), new Keyframe(1f, 0.45f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherRainAmbientIntensity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.5f, 0.65f), new Keyframe(1f, 0.45f));
            return animationCurve;
        }
        private AnimationCurve CreateTODSkyboxFogHeight()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.01f), new Keyframe(1f, 0.01f));
            return animationCurve;
        }
        private AnimationCurve CreateTODSkyboxFogGradient()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(1f, 0.1f));
            return animationCurve;
        }

        private Gradient CreateWeatherRainAmbientSky()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[7];
            colorKeys[0].time = 0.24f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("141518");

            colorKeys[1].time = 0.26f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("9C726C");

            colorKeys[2].time = 0.441f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("6385AB");

            colorKeys[3].time = 0.507f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("6D88AB");

            colorKeys[4].time = 0.74f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("B98B76");

            colorKeys[5].time = 0.76f;
            colorKeys[5].color = GaiaUtils.GetColorFromHTML("101E30");

            colorKeys[6].time = 1f;
            colorKeys[6].color = GaiaUtils.GetColorFromHTML("111213");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        private Gradient CreateWeatherRainAmbientEquator()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[8];
            colorKeys[0].time = 0.24f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("121316");

            colorKeys[1].time = 0.26f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("567AC0");

            colorKeys[2].time = 0.317f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("B8BC86");

            colorKeys[3].time = 0.407f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("559FAB");

            colorKeys[4].time = 0.494f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("6981B2");

            colorKeys[5].time = 0.74f;
            colorKeys[5].color = GaiaUtils.GetColorFromHTML("B75A36");

            colorKeys[6].time = 0.76f;
            colorKeys[6].color = GaiaUtils.GetColorFromHTML("2A3243");

            colorKeys[7].time = 0.976f;
            colorKeys[7].color = GaiaUtils.GetColorFromHTML("141518");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        private Gradient CreateWeatherRainAmbientGround()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[8];
            colorKeys[0].time = 0f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("16171A");

            colorKeys[1].time = 0.24f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("021322");

            colorKeys[2].time = 0.26f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("B7996F");

            colorKeys[3].time = 0.456f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("7698A8");

            colorKeys[4].time = 0.603f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("68909A");

            colorKeys[5].time = 0.74f;
            colorKeys[5].color = GaiaUtils.GetColorFromHTML("BC5E3A");

            colorKeys[6].time = 0.76f;
            colorKeys[6].color = GaiaUtils.GetColorFromHTML("384259");

            colorKeys[7].time = 1f;
            colorKeys[7].color = GaiaUtils.GetColorFromHTML("0F1011");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        private AnimationCurve CreateWeatherRainCloudDomeBrightness()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.5f, 0.3f), new Keyframe(1f, 0.2f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherRainCloudDensity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.017f), new Keyframe(0.5f, 0.012f), new Keyframe(1f, 0.017f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherRainCloudThickness()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.5f, 0.85f), new Keyframe(1f, 0.7f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherRainCloudSpeed()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 2f), new Keyframe(0.5f, 2.5f), new Keyframe(1f, 2f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherRainCloudOpacity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(0.5f, 0.45f), new Keyframe(1f, 0.15f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherRainParticleOpacity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.25f), new Keyframe(0.5f, 0.6f), new Keyframe(1f, 0.25f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherRainHDRPFogHeight()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 725f), new Keyframe(0.5f, 650f), new Keyframe(1f, 750f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherRainHDRPGlobalAnisotropy()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.5f, 0.75f), new Keyframe(1f, 0.6f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherRainHDRPGlobalProbeDimmer()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.5f, 0.75f), new Keyframe(1f, 0.6f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherRainHDRPFogDepthExtent()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 75f), new Keyframe(0.5f, 35f), new Keyframe(1f, 75f));
            return animationCurve;
        }

        #endregion
    }

    [System.Serializable]
    public class PWSkySnow
    {
        //Snow Settings
        public bool EnableSnow = true;
        public bool SnowCoverAlwaysEnabled = false;
        public float SnowIntensity;
        public float SnowHeight = 650f;
        public float PermanentSnowHeight = 650f;
        public float SnowingHeight = 60f;
        public float SnowFadeHeight = 50f;
        public float m_snowStormChance = 0.2f;
        public float m_snowStepSize = 0.05f;

#if GAIA_PRO_PRESENT
        public SnowMode m_snowMode = SnowMode.SampledHeight;
        public WeatherSettings m_snowWeatherSettings = new WeatherSettings();

        public void SetDefaults()
        {
            m_snowWeatherSettings.m_chance = 0.65f;
            m_snowWeatherSettings.m_channelSelection = ChannelSelection.R;
            m_snowWeatherSettings.m_durationMinWaitTime = 120f;
            m_snowWeatherSettings.m_durationMaxWaitTime = 700f;
            m_snowWeatherSettings.m_minWaitTime = 120f;
            m_snowWeatherSettings.m_maxWaitTime = 500f;
            m_snowWeatherSettings.m_windSpeed = 0.8f;
            m_snowWeatherSettings.m_windTurbulence = 0.7f;
            m_snowWeatherSettings.m_windFrequency = 0.6f;
            m_snowWeatherSettings.m_windMultiplier = 5f;
            if (GaiaUtils.CheckGradientColorKeys(m_snowWeatherSettings.m_fogColor.colorKeys, Color.white))
            {
                m_snowWeatherSettings.m_fogColor = CreateWeatherSnowFogColor();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_fogDensity.keys, 0f))
            {
                m_snowWeatherSettings.m_fogDensity = CreateWeatherSnowFogDensity();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_fogStartDistance.keys, 0f))
            {
                m_snowWeatherSettings.m_fogStartDistance = CreateWeatherSnowFogStartDistance();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_fogEndDistance.keys, 0f))
            {
                m_snowWeatherSettings.m_fogEndDistance = CreateWeatherSnowFogEndDistance();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_skyboxExposure.keys, 0f))
            {
                m_snowWeatherSettings.m_skyboxExposure = CreateWeatherSnowSkyboxExposure();
            }
            if (GaiaUtils.CheckGradientColorKeys(m_snowWeatherSettings.m_skyboxTint.colorKeys, Color.white))
            {
                m_snowWeatherSettings.m_skyboxTint = CreateWeatherSnowSkyboxTint();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_skyboxAtmosphereThickness.keys, 0f))
            {
                m_snowWeatherSettings.m_skyboxAtmosphereThickness = CreateWeatherSnowSkyboxAtmosphereThickness();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_volumetricDepthExtent.keys, 0f))
            {
                m_snowWeatherSettings.m_volumetricDepthExtent = CreateWeatherSnowHDRPFogDepthExtent();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_volumetricGlobalAnisotropy.keys, 0f))
            {
                m_snowWeatherSettings.m_volumetricGlobalAnisotropy = CreateWeatherSnowHDRPGlobalAnisotropy();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_volumetricGlobalProbeDimmer.keys, 0f))
            {
                m_snowWeatherSettings.m_volumetricGlobalProbeDimmer = CreateWeatherSnowHDRPGlobalProbeDimmer();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_fogHeight.keys, 0f))
            {
                m_snowWeatherSettings.m_fogHeight = CreateWeatherSnowHDRPFogHeight();
            }
            if (GaiaUtils.CheckGradientColorKeys(m_snowWeatherSettings.m_sunColor.colorKeys, Color.white))
            {
                m_snowWeatherSettings.m_sunColor = CreateWeatherSnowSunColor();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_sunIntensity.keys, 0f))
            {
                m_snowWeatherSettings.m_sunIntensity = CreateWeatherSnowSunIntensity();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_fXBloomIntensity.keys, 0f))
            {
                m_snowWeatherSettings.m_fXBloomIntensity = CreateWeatherSnowFXBloomIntensity();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_fXTemerature.keys, 0f))
            {
                m_snowWeatherSettings.m_fXTemerature = CreateWeatherSnowFXTemperature();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_fXTint.keys, 0f))
            {
                m_snowWeatherSettings.m_fXTint = CreateWeatherSnowFXTint();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_fXVignetteIntensity.keys, 0f))
            {
                m_snowWeatherSettings.m_fXVignetteIntensity = CreateWeatherSnowFXVignetteIntensity();
            }
            if (GaiaUtils.CheckGradientColorKeys(m_snowWeatherSettings.m_fXColorFilter.colorKeys, Color.white))
            {
                m_snowWeatherSettings.m_fXColorFilter = CreateWeatherSnowFXColorFilter();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_ambientIntensity.keys, 0f))
            {
                m_snowWeatherSettings.m_ambientIntensity = CreateWeatherSnowAmbientIntensity();
            }
            if (GaiaUtils.CheckGradientColorKeys(m_snowWeatherSettings.m_ambientSkyColor.colorKeys, Color.white))
            {
                m_snowWeatherSettings.m_ambientSkyColor = CreateWeatherSnowAmbientSky();
            }
            if (GaiaUtils.CheckGradientColorKeys(m_snowWeatherSettings.m_ambientEquatorColor.colorKeys, Color.white))
            {
                m_snowWeatherSettings.m_ambientEquatorColor = CreateWeatherSnowAmbientEquator();
            }
            if (GaiaUtils.CheckGradientColorKeys(m_snowWeatherSettings.m_ambientGroundColor.colorKeys, Color.white))
            {
                m_snowWeatherSettings.m_ambientGroundColor = CreateWeatherSnowAmbientGround();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_cloudDomeBrightness.keys, 0f))
            {
                m_snowWeatherSettings.m_cloudDomeBrightness = CreateWeatherSnowCloudDomeBrightness();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_cloudDensity.keys, 0f))
            {
                m_snowWeatherSettings.m_cloudDensity = CreateWeatherSnowCloudDensity();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_cloudThickness.keys, 0f))
            {
                m_snowWeatherSettings.m_cloudThickness = CreateWeatherSnowCloudThickness();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_newCloudOpacity.keys, 0f))
            {
                m_snowWeatherSettings.m_newCloudOpacity = CreateWeatherSnowCloudOpacity();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_cloudSpeed.keys, 0f))
            {
                m_snowWeatherSettings.m_cloudSpeed = CreateWeatherSnowCloudSpeed();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_weatherParticleAlpha.keys, 0f))
            {
                m_snowWeatherSettings.m_weatherParticleAlpha = CreateWeatherSnowParticleOpacity();
            }  
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_skyboxSkyboxFogHeight.keys, 0f))
            {
                m_snowWeatherSettings.m_skyboxSkyboxFogHeight = CreateTODSkyboxFogHeight();
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_skyboxSkyboxFogGradient.keys, 0f))
            {
                m_snowWeatherSettings.m_skyboxSkyboxFogGradient = CreateTODSkyboxFogGradient();
            }
        }
        private bool CheckIfDefaultsNeedToBeSet()
        {
            if (GaiaUtils.CheckGradientColorKeys(m_snowWeatherSettings.m_fogColor.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_fogStartDistance.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_fogEndDistance.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_fogDensity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_skyboxExposure.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(m_snowWeatherSettings.m_skyboxTint.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_skyboxAtmosphereThickness.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(m_snowWeatherSettings.m_sunColor.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_sunIntensity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_fXTemerature.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_fXTint.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(m_snowWeatherSettings.m_fXColorFilter.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_fXBloomIntensity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_fXVignetteIntensity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_ambientIntensity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(m_snowWeatherSettings.m_ambientSkyColor.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(m_snowWeatherSettings.m_ambientEquatorColor.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckGradientColorKeys(m_snowWeatherSettings.m_ambientGroundColor.colorKeys, Color.white))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_cloudDomeBrightness.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_volumetricDepthExtent.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_volumetricGlobalAnisotropy.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_volumetricGlobalProbeDimmer.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_fogHeight.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_cloudDensity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_cloudThickness.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_cloudSpeed.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_newCloudOpacity.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_weatherParticleAlpha.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_skyboxSkyboxFogHeight.keys, 0f))
            {
                return true;
            }
            if (GaiaUtils.CheckAnimationCurveKeys(m_snowWeatherSettings.m_skyboxSkyboxFogGradient.keys, 0f))
            {
                return true;
            }
            return false;
        }
        public void Load(ProceduralWorldsGlobalWeather globalWeather, bool checkDefaults = true)
        {
            if (globalWeather == null)
            {
                return;
            }

            if (checkDefaults)
            {
                if (CheckIfDefaultsNeedToBeSet())
                {
                    SetDefaults();
                }
            }

            globalWeather.EnableSnow = EnableSnow;
            globalWeather.SnowCoverAlwaysEnabled = SnowCoverAlwaysEnabled;
            globalWeather.SnowIntensity = SnowIntensity;
            globalWeather.SnowHeight = SnowHeight;
            globalWeather.PermanentSnowHeight = PermanentSnowHeight;
            globalWeather.SnowingHeight = SnowingHeight;
            globalWeather.SnowFadeHeight = SnowFadeHeight;
            globalWeather.m_snowStormChance = m_snowStormChance;
            globalWeather.m_snowStepSize = m_snowStepSize;
            globalWeather.m_snowMode = m_snowMode;
            globalWeather.m_snowWeatherSettings.Load(m_snowWeatherSettings, globalWeather, false);
        }
        public void Save(ProceduralWorldsGlobalWeather globalWeather)
        {
            if (globalWeather == null)
            {
                return;
            }

            EnableSnow = globalWeather.EnableSnow;
            SnowCoverAlwaysEnabled = globalWeather.SnowCoverAlwaysEnabled;
            SnowIntensity = globalWeather.SnowIntensity;
            SnowHeight = globalWeather.SnowHeight;
            PermanentSnowHeight = globalWeather.PermanentSnowHeight;
            SnowingHeight = globalWeather.SnowingHeight;
            SnowFadeHeight = globalWeather.SnowFadeHeight;
            m_snowStormChance = globalWeather.m_snowStormChance;
            m_snowStepSize = globalWeather.m_snowStepSize;
            m_snowMode = globalWeather.m_snowMode;
            globalWeather.m_snowWeatherSettings.Save(m_snowWeatherSettings, globalWeather, false);
        }
        public void New(ProceduralWorldsGlobalWeather globalWeather)
        {
            EnableSnow = globalWeather.EnableSnow;
            SnowCoverAlwaysEnabled = globalWeather.SnowCoverAlwaysEnabled;
            SnowIntensity = globalWeather.SnowIntensity;
            SnowHeight = globalWeather.SnowHeight;
            PermanentSnowHeight = globalWeather.PermanentSnowHeight;
            SnowingHeight = globalWeather.SnowingHeight;
            SnowFadeHeight = globalWeather.SnowFadeHeight;
            m_snowStormChance = globalWeather.m_snowStormChance;
            m_snowStepSize = globalWeather.m_snowStepSize;
            m_snowMode = globalWeather.m_snowMode;
            globalWeather.m_snowWeatherSettings.Save(m_snowWeatherSettings, globalWeather, false);
        }
#endif

        #region Set Defaults Utils

        private Gradient CreateWeatherSnowFogColor()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[8];
            colorKeys[0].time = 0.245f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("051F26");

            colorKeys[1].time = 0.26f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("483D2D");

            colorKeys[2].time = 0.271f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("6A6A6A");

            colorKeys[3].time = 0.3f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("5C7293");

            colorKeys[4].time = 0.7f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("5C7293");

            colorKeys[5].time = 0.729f;
            colorKeys[5].color = GaiaUtils.GetColorFromHTML("4B4B4B");

            colorKeys[6].time = 0.74f;
            colorKeys[6].color = GaiaUtils.GetColorFromHTML("594D44");

            colorKeys[7].time = 0.75f;
            colorKeys[7].color = GaiaUtils.GetColorFromHTML("091C22");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        private AnimationCurve CreateWeatherSnowFogDensity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.05f), new Keyframe(0.1f, 0.01f), new Keyframe(0.3f, 0.0085f), new Keyframe(0.5f, 0.007f), new Keyframe(0.75f, 0.0085f), new Keyframe(1f, 0.05f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherSnowFogStartDistance()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, -5f), new Keyframe(0.25f, 0f), new Keyframe(0.5f, 15f), new Keyframe(0.75f, 0f), new Keyframe(1f, -5f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherSnowFogEndDistance()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 600f), new Keyframe(0.25f, 900f), new Keyframe(0.5f, 2300f), new Keyframe(0.75f, 900f), new Keyframe(1f, 600f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherSnowSkyboxExposure()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.325f), new Keyframe(0.5f, 0.85f), new Keyframe(1f, 0.325f));
            return animationCurve;
        }

        private Gradient CreateWeatherSnowSkyboxTint()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0].time = 0.24f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("4F5460");

            colorKeys[1].time = 0.26f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("C09D89");

            colorKeys[2].time = 0.5f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("CCD5E2");

            colorKeys[3].time = 0.74f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("C3876F");

            colorKeys[4].time = 0.76f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("4F5460");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        private AnimationCurve CreateWeatherSnowSkyboxAtmosphereThickness()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.125f), new Keyframe(0.15f, 0.125f), new Keyframe(0.3f, 0.85f), new Keyframe(0.5f, 0.8f), new Keyframe(0.725f, 0.85f), new Keyframe(0.775f, 0.125f), new Keyframe(1f, 0.125f));
            return animationCurve;
        }

        private Gradient CreateWeatherSnowSunColor()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[7];
            colorKeys[0].time = 0f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("507AB0");

            colorKeys[1].time = 0.24f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("5F89A6");

            colorKeys[2].time = 0.26f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("D1A87F");

            colorKeys[3].time = 0.5f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("C8B5A3");

            colorKeys[4].time = 0.74f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("DDA380");

            colorKeys[5].time = 0.76f;
            colorKeys[5].color = GaiaUtils.GetColorFromHTML("82A1B7");

            colorKeys[6].time = 1f;
            colorKeys[6].color = GaiaUtils.GetColorFromHTML("5476A1");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        private AnimationCurve CreateWeatherSnowSunIntensity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.225f, 0.3f), new Keyframe(0.35f, 0.9f), new Keyframe(0.625f, 1f), new Keyframe(0.745f, 0.3f), new Keyframe(1f, 0.5f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherSnowFXTemperature()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, -8f), new Keyframe(0.225f, -8f), new Keyframe(0.35f, -4f), new Keyframe(0.625f, -3f), new Keyframe(0.745f, -8f), new Keyframe(1f, -8f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherSnowFXTint()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.225f, 0f), new Keyframe(0.35f, -2f), new Keyframe(0.625f, -1.8f), new Keyframe(0.745f, 0f), new Keyframe(1f, 0f));
            return animationCurve;
        }

        private Gradient CreateWeatherSnowFXColorFilter()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0].time = 0f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("92BACC");

            colorKeys[1].time = 0.5f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("DAEBFF");

            colorKeys[2].time = 1f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("92BACC");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        private AnimationCurve CreateWeatherSnowFXBloomIntensity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 5f), new Keyframe(1f, 5f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherSnowFXVignetteIntensity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.225f, 0.45f), new Keyframe(0.39f, 0.35f), new Keyframe(0.625f, 0.39f), new Keyframe(0.745f, 0.45f), new Keyframe(1f, 0.45f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherSnowAmbientIntensity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.5f, 0.65f), new Keyframe(1f, 0.45f));
            return animationCurve;
        }

        private Gradient CreateWeatherSnowAmbientSky()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[7];
            colorKeys[0].time = 0.24f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("141518");

            colorKeys[1].time = 0.26f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("A67C76");

            colorKeys[2].time = 0.441f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("89A0B9");

            colorKeys[3].time = 0.507f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("7F98B9");

            colorKeys[4].time = 0.74f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("A47B68");

            colorKeys[5].time = 0.76f;
            colorKeys[5].color = GaiaUtils.GetColorFromHTML("101E30");

            colorKeys[6].time = 1f;
            colorKeys[6].color = GaiaUtils.GetColorFromHTML("111213");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        private Gradient CreateWeatherSnowAmbientEquator()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[8];
            colorKeys[0].time = 0.24f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("121316");

            colorKeys[1].time = 0.26f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("567AC0");

            colorKeys[2].time = 0.317f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("B8BC86");

            colorKeys[3].time = 0.407f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("559FAB");

            colorKeys[4].time = 0.494f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("6981B2");

            colorKeys[5].time = 0.74f;
            colorKeys[5].color = GaiaUtils.GetColorFromHTML("B75A36");

            colorKeys[6].time = 0.76f;
            colorKeys[6].color = GaiaUtils.GetColorFromHTML("2A3243");

            colorKeys[7].time = 0.976f;
            colorKeys[7].color = GaiaUtils.GetColorFromHTML("141518");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        private Gradient CreateWeatherSnowAmbientGround()
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[8];
            colorKeys[0].time = 0f;
            colorKeys[0].color = GaiaUtils.GetColorFromHTML("16171A");

            colorKeys[1].time = 0.24f;
            colorKeys[1].color = GaiaUtils.GetColorFromHTML("021322");

            colorKeys[2].time = 0.26f;
            colorKeys[2].color = GaiaUtils.GetColorFromHTML("B7996F");

            colorKeys[3].time = 0.456f;
            colorKeys[3].color = GaiaUtils.GetColorFromHTML("7698A8");

            colorKeys[4].time = 0.603f;
            colorKeys[4].color = GaiaUtils.GetColorFromHTML("68909A");

            colorKeys[5].time = 0.74f;
            colorKeys[5].color = GaiaUtils.GetColorFromHTML("BC5E3A");

            colorKeys[6].time = 0.76f;
            colorKeys[6].color = GaiaUtils.GetColorFromHTML("384259");

            colorKeys[7].time = 1f;
            colorKeys[7].color = GaiaUtils.GetColorFromHTML("0F1011");

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].time = 0f;
            alphaKeys[0].alpha = 1f;

            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        private AnimationCurve CreateWeatherSnowCloudDomeBrightness()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.25f), new Keyframe(0.5f, 0.5f), new Keyframe(1f, 0.25f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherSnowCloudDensity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.017f), new Keyframe(0.5f, 0.012f), new Keyframe(1f, 0.017f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherSnowCloudThickness()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.5f, 0.85f), new Keyframe(1f, 0.7f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherSnowCloudSpeed()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 2f), new Keyframe(0.5f, 2.5f), new Keyframe(1f, 2f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherSnowCloudOpacity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(0.5f, 0.45f), new Keyframe(1f, 0.15f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherSnowParticleOpacity()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.5f, 0.5f), new Keyframe(1f, 0.2f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherSnowHDRPFogHeight()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 750f), new Keyframe(0.5f, 650f), new Keyframe(1f, 750f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherSnowHDRPGlobalAnisotropy()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.5f, 0.75f), new Keyframe(1f, 0.6f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherSnowHDRPGlobalProbeDimmer()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.5f, 0.75f), new Keyframe(1f, 0.6f));
            return animationCurve;
        }

        private AnimationCurve CreateWeatherSnowHDRPFogDepthExtent()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 75f), new Keyframe(0.5f, 35f), new Keyframe(1f, 75f));
            return animationCurve;
        }
        private AnimationCurve CreateTODSkyboxFogHeight()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.01f), new Keyframe(1f, 0.01f));
            return animationCurve;
        }
        private AnimationCurve CreateTODSkyboxFogGradient()
        {
            AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(1f, 0.1f));
            return animationCurve;
        }

        #endregion
    }

    [System.Serializable]
    public class PWSkyThunder
    {
        //Thunder
        public bool m_enableThunder = true;
        public float ThunderWaitDuration = 30f;
        public float m_thunderChance = 0.45f;
        public Color ThunderLightColor;
        public float ThunderLightIntensity = 2f;
        public List<AudioClip> ThunderAudioSources;
        public float ThunderLightRadius = 500f;

#if GAIA_PRO_PRESENT
        public void SetDefaults()
        {
            if (GaiaUtils.CheckColorKey(ThunderLightColor, Color.black))
            {
                ThunderLightColor = GaiaUtils.GetColorFromHTML("00A1FF");
            }
        }
        private bool CheckIfDefaultsNeedToBeSet()
        {
            if (GaiaUtils.CheckColorKey(ThunderLightColor, Color.black))
            {
                return true;
            }

            return false;
        }
        public void Load(ProceduralWorldsGlobalWeather globalWeather, bool checkDefaults = true)
        {
            if (globalWeather == null)
            {
                return;
            }

            if (checkDefaults)
            {
                if (CheckIfDefaultsNeedToBeSet())
                {
                    SetDefaults();
                }
            }

            globalWeather.m_enableThunder = m_enableThunder;
            globalWeather.ThunderWaitDuration = ThunderWaitDuration;
            globalWeather.m_thunderChance = m_thunderChance;
            globalWeather.ThunderLightColor = ThunderLightColor;
            globalWeather.ThunderLightIntensity = ThunderLightIntensity;
            globalWeather.ThunderAudioSources = ThunderAudioSources;
            globalWeather.ThunderLightRadius = ThunderLightRadius;
        }
        public void Save(ProceduralWorldsGlobalWeather globalWeather)
        {
            if (globalWeather == null)
            {
                return;
            }

            m_enableThunder = globalWeather.m_enableThunder;
            ThunderWaitDuration = globalWeather.ThunderWaitDuration;
            m_thunderChance = globalWeather.m_thunderChance;
            ThunderLightColor = globalWeather.ThunderLightColor;
            ThunderLightIntensity = globalWeather.ThunderLightIntensity;
            ThunderAudioSources = globalWeather.ThunderAudioSources;
            ThunderLightRadius = globalWeather.ThunderLightRadius;
        }
        public void New(ProceduralWorldsGlobalWeather globalWeather)
        {
            if (globalWeather == null)
            {
                return;
            }

            m_enableThunder = globalWeather.m_enableThunder;
            ThunderWaitDuration = globalWeather.ThunderWaitDuration;
            m_thunderChance = globalWeather.m_thunderChance;
            ThunderLightColor = globalWeather.ThunderLightColor;
            ThunderLightIntensity = globalWeather.ThunderLightIntensity;
            ThunderAudioSources = globalWeather.ThunderAudioSources;
            ThunderLightRadius = globalWeather.ThunderLightRadius;
        }
#endif
    }

    [System.Serializable]
    public class PWSkyCloud
    {
        //Cloud Particles
        public bool EnableClouds = true;
        public int CloudHeight = 50;
        public Color CloudAmbientColor;
        public float CloudScale = 5f;
        public float CloudOffset = 50f;
        public float CloudBrightness = 1f;
        public float CloudFade = 250f;
        //X-Y tile, Z wind speed
        public Vector4 CloudTilingAndWind = new Vector4(1.5f, 1f, 1f, -16f);
        //X cutout, Y opacity, Z density, W dome opacity
        public Vector4 CloudOpacity = new Vector4(0.2f, 1f, 0.45f, 0.75f);
        public float CloudRotationSpeedLow = 0.3f;
        public float CloudRotationSpeedMiddle = 0.2f;
        public float CloudRotationSpeedFar = 0.1f;

#if GAIA_PRO_PRESENT
        public void Load(ProceduralWorldsGlobalWeather globalWeather)
        {
            if (globalWeather == null)
            {
                return;
            }

            if (GaiaUtils.CheckColorKey(CloudAmbientColor, Color.white))
            {
                CloudAmbientColor = GaiaUtils.GetColorFromHTML("5DC8FF");
            }

            globalWeather.EnableClouds = EnableClouds;
            globalWeather.CloudHeight = CloudHeight;
            globalWeather.CloudAmbientColor = CloudAmbientColor;
            globalWeather.CloudScale = CloudScale;
            globalWeather.CloudOffset = CloudOffset;
            globalWeather.CloudBrightness = CloudBrightness;
            globalWeather.CloudFade = CloudFade;
            globalWeather.CloudTilingAndWind = CloudTilingAndWind;
            globalWeather.CloudOpacity = CloudOpacity;
            globalWeather.CloudRotationSpeedLow = CloudRotationSpeedLow;
            globalWeather.CloudRotationSpeedMiddle = CloudRotationSpeedMiddle;
            globalWeather.CloudRotationSpeedFar = CloudRotationSpeedFar;
        }
        public void Save(ProceduralWorldsGlobalWeather globalWeather)
        {
            if (globalWeather == null)
            {
                return;
            }

            EnableClouds = globalWeather.EnableClouds;
            CloudHeight = globalWeather.CloudHeight;
            CloudAmbientColor = globalWeather.CloudAmbientColor;
            CloudScale = globalWeather.CloudScale;
            CloudOffset = globalWeather.CloudOffset;
            CloudBrightness = globalWeather.CloudBrightness;
            CloudFade = globalWeather.CloudFade;
            CloudTilingAndWind = globalWeather.CloudTilingAndWind;
            CloudOpacity = globalWeather.CloudOpacity;
            CloudRotationSpeedLow = globalWeather.CloudRotationSpeedLow;
            CloudRotationSpeedMiddle = globalWeather.CloudRotationSpeedMiddle;
            CloudRotationSpeedFar = globalWeather.CloudRotationSpeedFar;
        }
        public void New(ProceduralWorldsGlobalWeather globalWeather)
        {
            if (globalWeather == null)
            {
                return;
            }

            globalWeather.EnableClouds = EnableClouds;
            globalWeather.CloudHeight = CloudHeight;
            globalWeather.CloudAmbientColor = CloudAmbientColor;
            globalWeather.CloudScale = CloudScale;
            globalWeather.CloudOffset = CloudOffset;
            globalWeather.CloudBrightness = CloudBrightness;
            globalWeather.CloudFade = CloudFade;
            globalWeather.CloudTilingAndWind = CloudTilingAndWind;
            globalWeather.CloudOpacity = CloudOpacity;
            globalWeather.CloudRotationSpeedLow = CloudRotationSpeedLow;
            globalWeather.CloudRotationSpeedMiddle = CloudRotationSpeedMiddle;
            globalWeather.CloudRotationSpeedFar = CloudRotationSpeedFar;
        }
#endif
    }

    [System.Serializable]
    public class PWSkySeason
    {
        //Season
        public bool EnableSeasons;
        public float Season;
        public Color SeasonWinterTint;
        public Color SeasonSpringTint;
        public Color SeasonSummerTint;
        public Color SeasonAutumnTint;
        public float m_seasonTransitionDuration;

#if GAIA_PRO_PRESENT
        public SeasonMode SeasonMode;
        public void SetDefaults()
        {
            bool setRest = false;
            if (GaiaUtils.CheckColorKey(SeasonWinterTint, Color.black))
            {
                SeasonWinterTint = GaiaUtils.GetColorFromHTML("D3EFFF");
                setRest = true;
            }

            if (GaiaUtils.CheckColorKey(SeasonSpringTint, Color.black))
            {
                SeasonSpringTint = GaiaUtils.GetColorFromHTML("BCFF96");
                setRest = true;
            }

            if (GaiaUtils.CheckColorKey(SeasonSummerTint, Color.black))
            {
                SeasonSummerTint = GaiaUtils.GetColorFromHTML("FFB960");
                setRest = true;
            }

            if (GaiaUtils.CheckColorKey(SeasonAutumnTint, Color.black))
            {
                SeasonAutumnTint = GaiaUtils.GetColorFromHTML("FFE8E6");
                setRest = true;
            }

            if (setRest)
            {
                EnableSeasons = true;
                Season = 1.5f;
                m_seasonTransitionDuration = 2000f;
            }
        }
        private bool CheckIfDefaultsNeedToBeSet()
        {
            if (GaiaUtils.CheckColorKey(SeasonWinterTint, Color.black))
            {
                return true;
            }

            if (GaiaUtils.CheckColorKey(SeasonSpringTint, Color.black))
            {
                return true;
            }

            if (GaiaUtils.CheckColorKey(SeasonSummerTint, Color.black))
            {
                return true;
            }

            if (GaiaUtils.CheckColorKey(SeasonAutumnTint, Color.black))
            {
                return true;
            }

            return false;
        }
        public void Load(ProceduralWorldsGlobalWeather globalWeather, bool checkDefaults = true)
        {
            if (globalWeather == null)
            {
                return;
            }

            if (checkDefaults)
            {
                if (CheckIfDefaultsNeedToBeSet())
                {
                    SetDefaults();
                }
            }

            globalWeather.EnableSeasons = EnableSeasons;
            globalWeather.SeasonMode = SeasonMode;
            globalWeather.Season = Season;
            globalWeather.SeasonWinterTint = SeasonWinterTint;
            globalWeather.SeasonSpringTint = SeasonSpringTint;
            globalWeather.SeasonSummerTint = SeasonSummerTint;
            globalWeather.SeasonAutumnTint = SeasonAutumnTint;
            globalWeather.m_seasonTransitionDuration = m_seasonTransitionDuration;
        }
        public void Save(ProceduralWorldsGlobalWeather globalWeather)
        {
            if (globalWeather == null)
            {
                return;
            }

            EnableSeasons = globalWeather.EnableSeasons;
            SeasonMode = globalWeather.SeasonMode;
            Season = globalWeather.Season;
            SeasonWinterTint = globalWeather.SeasonWinterTint;
            SeasonSpringTint = globalWeather.SeasonSpringTint;
            SeasonSummerTint = globalWeather.SeasonSummerTint;
            SeasonAutumnTint = globalWeather.SeasonAutumnTint;
            m_seasonTransitionDuration = globalWeather.m_seasonTransitionDuration;
        }
        public void New(ProceduralWorldsGlobalWeather globalWeather)
        {
            if (globalWeather == null)
            {
                return;
            }

            EnableSeasons = globalWeather.EnableSeasons;
            SeasonMode = globalWeather.SeasonMode;
            Season = globalWeather.Season;
            SeasonWinterTint = globalWeather.SeasonWinterTint;
            SeasonSpringTint = globalWeather.SeasonSpringTint;
            SeasonSummerTint = globalWeather.SeasonSummerTint;
            SeasonAutumnTint = globalWeather.SeasonAutumnTint;
            m_seasonTransitionDuration = globalWeather.m_seasonTransitionDuration;
        }
#endif
    }

    [System.Serializable]
    public class GaiaLightingProfileValues
    {
        [Header("Global Settings")]
        public string m_typeOfLighting = "Morning";
        public GaiaConstants.GaiaLightingProfileType m_profileType = GaiaConstants.GaiaLightingProfileType.HDRI;
        public bool m_userCustomProfile = false;
        public string m_profileRename;
        [Header("Post Processing Settings")] 
        public float m_postProcessExposure = 1f;
#if UNITY_POST_PROCESSING_STACK_V2
        [SerializeField]
        private PostProcessProfile m_postProcessProfileBuiltIn = null;
        public PostProcessProfile PostProcessProfileBuiltIn 
        {
            get 
            {
                if (m_postProcessProfileBuiltIn == null && m_postProcessingProfileGUIDBuiltIn !=null)
                {
#if UNITY_EDITOR
                    m_postProcessProfileBuiltIn = (PostProcessProfile)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_postProcessingProfileGUIDBuiltIn),typeof(PostProcessProfile));
#endif
                }
                return m_postProcessProfileBuiltIn;
            }
            set 
            {
#if UNITY_EDITOR
                m_postProcessingProfileGUIDBuiltIn = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
                m_postProcessProfileBuiltIn = value;
#endif
            }
        }
#endif
        //need this serialized to remember the GUID even when PP is not installed in the project
        public string m_postProcessingProfileGUIDBuiltIn = "";
        public bool m_directToCamera = true;
#if UPPipeline
        [Header("URP Post Processign Settings")]
        [SerializeField]
        private VolumeProfile m_postProcessProfileURP = null;
        public VolumeProfile PostProcessProfileURP
        {
            get 
            {
                if (m_postProcessProfileURP == null && m_postProcessingProfileGUIDURP !=null)
                {
#if UNITY_EDITOR
                    m_postProcessProfileURP = (VolumeProfile)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_postProcessingProfileGUIDURP),typeof(VolumeProfile));
#endif
                }
                return m_postProcessProfileURP;
            }
            set 
            {
#if UNITY_EDITOR
                m_postProcessingProfileGUIDURP = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
                m_postProcessProfileURP = value;
#endif
            }
        }
#endif
        //need this serialized to remember the GUID even when PP is not installed in the project
        public string m_postProcessingProfileGUIDURP = "";

#if HDPipeline
        [Header("HDRP Volume Settings")]
        [SerializeField]
        private VolumeProfile m_environmentProfileHDRP = null;
        public VolumeProfile EnvironmentProfileHDRP
        {
            get 
            {
                if (m_environmentProfileHDRP == null && m_environmentProfileGUIDHDRP !=null)
                {
#if UNITY_EDITOR
                    m_environmentProfileHDRP = (VolumeProfile)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_environmentProfileGUIDHDRP),typeof(VolumeProfile));
#endif
                }
                return m_environmentProfileHDRP;
            }
            set 
            {
#if UNITY_EDITOR
                m_environmentProfileGUIDHDRP = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
                m_environmentProfileHDRP = value;
#endif
            }
        }
        [SerializeField]
        private VolumeProfile m_postProcessProfileHDRP = null;
        public VolumeProfile PostProcessProfileHDRP
        {
            get 
            {
                if (m_postProcessProfileHDRP == null && m_postProcessingProfileGUIDHDRP !=null)
                {
#if UNITY_EDITOR
                    m_postProcessProfileHDRP = (VolumeProfile)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_postProcessingProfileGUIDHDRP),typeof(VolumeProfile));
#endif
                }
                return m_postProcessProfileHDRP;
            }
            set 
            {
#if UNITY_EDITOR
                m_postProcessingProfileGUIDHDRP = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
                m_postProcessProfileHDRP = value;
#endif
            }
        }
#endif
        //need this serialized to remember the GUID even when PP is not installed in the project
        public string m_postProcessingProfileGUIDHDRP = "";
        public string m_environmentProfileGUIDHDRP = "";

        [Header("Ambient Audio Settings")]
        public AudioClip m_ambientAudio;
        [Range(0f, 1f)]
        public float m_ambientVolume = 0.55f;

        [Header("Sun Settings")] 
        public bool m_useKelvin = false;
        [Range(1000f, 20000f)]
        public float m_kelvinValue = 5500;
        [Range(0f, 360f)]
        public float m_sunRotation = 0f;
        public float m_pwSkySunRotation = 0f;
        [Range(0f, 360f)]
        public float m_sunPitch = 65f;
        public Color m_sunColor = Color.white;
        public float m_sunIntensity = 1f;
        [Header("LWRP Sun Settings")]
        public Color m_lWSunColor = Color.white;
        public float m_lWSunIntensity = 1f;
        //HDRP

        [Header("HDRP Sun Settings")]
        public Color m_hDSunColor = Color.white;
        public float m_hDSunIntensity = 1f;
        public float m_hDSunVolumetricMultiplier = 1f;
        [Header("Sun Shadow Settings")]
        public LightShadows m_shadowCastingMode = LightShadows.Soft;
        [Range(0f, 1f)]
        public float m_shadowStrength = 1f;
        public LightShadowResolution m_sunShadowResolution = LightShadowResolution.FromQualitySettings;
        public float m_shadowDistance = 400f;
        [Header("HDRP Shadow Settings")]
        public float m_hDShadowDistance = 700f;
        public GaiaConstants.HDShadowResolution m_hDShadowResolution = GaiaConstants.HDShadowResolution.Resolution1024;
        public bool m_hDContactShadows = true;
        public GaiaConstants.ContactShadowsQuality m_hDContactShadowQuality = GaiaConstants.ContactShadowsQuality.Medium;
        public int m_hDContactShadowCustomQuality = 10;
        public float m_hDContactShadowsDistance = 150f;
        [Range(0f, 1f)]
        public float m_hDContactShadowOpacity = 1f;
        public bool m_hDMicroShadows = true;
        [Range(0f, 1f)]
        public float m_hDMicroShadowOpacity = 1f;
        [Header("Skybox Settings")]
        public Cubemap m_skyboxHDRI;
        public Color m_skyboxTint = new Color(0.5f, 0.5f, 0.5f, 1f);
        [Range(0f, 8f)]
        public float m_skyboxExposure = 1.6f;
        [Range(-180f, 180f)]
        public float m_skyboxRotationOffset = 0f;
        [Space(15)]
        [Range(0f, 1f)]
        public float m_sunSize = 0.04f;
        [Range(0.01f, 10f)]
        public float m_sunConvergence = 10f;
        [Range(0f, 5f)]
        public float m_atmosphereThickness = 1f;
        public Color m_groundColor = Color.gray;
        [Header("HDRP Skybox Settings")]
        public GaiaConstants.HDSkyType m_hDSkyType = GaiaConstants.HDSkyType.HDRI;
        public GaiaConstants.HDSkyUpdateMode m_hDSkyUpdateMode = GaiaConstants.HDSkyUpdateMode.OnChanged;
        [Space(10)]
        //HDRI
        public Cubemap m_hDHDRISkybox;
        public float m_hDHDRIExposure = 0.75f;
        public float m_hDHDRIMultiplier = 1f;
        [Space(10)]
        //Gradient
        public Color m_hDGradientTopColor = Color.blue;
        public Color m_hDGradientMiddleColor = Color.cyan;
        public Color m_hDGradientBottomColor = Color.white;
        public float m_hDGradientDiffusion = 1f;
        public float m_hDGradientExposure = 0f;
        public float m_hDGradientMultiplier = 1f;
        [Space(10)]
        //Procedural
        public bool m_hDProceduralEnableSunDisk = true;
        public bool m_hDProceduralIncludeSunInBaking = true;
        public float m_hDProceduralSunSize = 0.015f;
        public float m_hDProceduralSunSizeConvergence = 9.5f;
        public float m_hDProceduralAtmosphereThickness = 1f;
        public Color32 m_hDProceduralSkyTint = new Color32(128, 128, 128, 128);
        public Color32 m_hDProceduralGroundColor = new Color32(148, 161, 176, 255);
        public float m_hDProceduralExposure = 1f;
        public float m_hDProceduralMultiplier = 2.5f;
        //Physically Based Sky
        //Planet
        public bool m_hDPBSEarthPreset = true;
        public float m_hDPBSPlanetaryRadius = 6378.759f;
        public bool m_hDPBSSphericalMode = true;
        public float m_hDPBSSeaLevel = 50f;
        public Vector3 m_hDPBSPlantetCenterPosition = new Vector3(0f, -6378.759f, 0f);
        public Vector3 m_hDPBSPlanetRotation = new Vector3(0f, 0f, 0f);
        public Cubemap m_hDPBSGroundAlbedoTexture;
        public Color m_hDPBSGroundTint = new Color(0.5803922f, 0.6313726f, 0.6901961f);
        public Cubemap m_hDPBSGroundEmissionTexture;
        public float m_hDPBSGroundEmissionMultiplier = 1f;
        //Space
        public Vector3 m_hDPBSSpaceRotation = new Vector3(0f, 0f, 0f);
        public Cubemap m_hDPBSSpaceEmissionTexture;
        public float m_hDPBSSpaceEmissionMultiplier = 1f;
        //Air
        public float m_hDPBSAirMaximumAltitude = 70f;
        public Color m_hDPBSAirOpacity = Color.white;
        public Color m_hDPBSAirAlbedo = Color.white;
        public float m_hDPBSAirDensityBlue = 0.232f;
        public Color m_hDPBSAirTint = new Color(0.172f, 0.074f, 0.030f);
        //Aerosols
        public float m_hDPBSAerosolMaximumAltitude = 8.3f;
        public float m_hDPBSAerosolDensity = 0.5f;
        public Color m_hDPBSAerosolAlbedo = Color.white;
        public float m_hDPBSAerosolAnisotropy = 0f;
        //Artistic Overrides
        public float m_hDPBSColorSaturation = 1f;
        public float m_hDPBSAlphaSaturation = 1f;
        public float m_hDPBSAlphaMultiplier = 1f;
        public Color m_hDPBSHorizonTint = Color.white;
        public float m_hDPBSHorizonZenithShift = 0f;
        public Color m_hDPBSZenithTint = Color.white;
        //Miscellaneous
        public int m_hDPBSNumberOfBounces = 8;
        public GaiaConstants.HDIntensityMode m_hDPBSIntensityMode = GaiaConstants.HDIntensityMode.Exposure;
        public float m_hDPBSMultiplier = 1f;
        public float m_hDPBSExposure = 1f;
        public bool m_hDPBSIncludeSunInBaking = true;

        [Header("Ambient Light Settings")]
        public AmbientMode m_ambientMode = AmbientMode.Trilight;
        [Range(0f, 10f)]
        public float m_ambientIntensity = 1f;
        public Color m_skyAmbient = Color.white;
        public Color m_equatorAmbient = Color.gray;
        public Color m_groundAmbient = Color.gray;
        [Header("HDRP Ambient Light Settings")]
        public GaiaConstants.HDAmbientMode m_hDAmbientMode = GaiaConstants.HDAmbientMode.Static;
        public float m_hDAmbientDiffuseIntensity = 1f;
        public float m_hDAmbientSpecularIntensity = 1f;
        [Header("Fog Settings")] 
        public int m_skyboxFogHeight = 0;
        public float m_skyboxFogGradient = 0.75f;
        public FogMode m_fogMode = FogMode.Linear;
        public Color m_fogColor = Color.white;
        [Range(0f, 1f)]
        public float m_fogDensity = 0.01f;
        public float m_fogStartDistance = 15f;
        public float m_fogEndDistance = 800f;
        [Header("HDRP Fog Settings")]
        public GaiaConstants.HDFogType m_hDFogType = GaiaConstants.HDFogType.Volumetric;
        public GaiaConstants.HDFogType2019_3 m_hDFogType2019_3 = GaiaConstants.HDFogType2019_3.Volumetric;
        [Space(10)]
        //Exponential
        [Range(0f, 1f)]
        public float m_hDExponentialFogDensity = 1f;
        public float m_hDExponentialFogDistance = 200f;
        public float m_hDExponentialFogBaseHeight = 0f;
        [Range(0f, 1f)]
        public float m_hDExponentialFogHeightAttenuation = 0.2f;
        public float m_hDExponentialFogMaxDistance = 5000f;
        [Space(10)]
        //Linear
        [Range(0f, 1f)]
        public float m_hDLinearFogDensity = 1f;
        public float m_hDLinearFogStart = 5f;
        public float m_hDLinearFogEnd = 1200f;
        public float m_hDLinearFogHeightStart = 100f;
        public float m_hDLinearFogHeightEnd = 800f;
        public float m_hDLinearFogMaxDistance = 5000f;
        [Space(10)]
        //Volumetric
        public Color m_hDVolumetricFogScatterColor = Color.white;
        public float m_hDVolumetricFogDistance = 1000f;
        public float m_hDVolumetricFogBaseHeight = 100f;
        public float m_hDVolumetricFogMeanHeight = 200f;
        [Range(0f, 1f)]
        public float m_hDVolumetricFogAnisotropy = 0.75f;
        [Range(0f, 1f)]
        public float m_hDVolumetricFogProbeDimmer = 0.8f;
        public float m_hDVolumetricFogMaxDistance = 5000f;
        public float m_hDVolumetricFogDepthExtent = 50f;
        [Range(0f, 1f)]
        public float m_hDVolumetricFogSliceDistribution = 0f;
        [Header("PW Sky Settings")]
        public PWSkyAtmosphere m_pwSkyAtmosphereData = new PWSkyAtmosphere();
        public PWSkyCloud m_pwSkyCloudData = new PWSkyCloud();
        public PWSkySeason m_pwSkySeasonData = new PWSkySeason();
        public PWSkyWind m_pwSkyWindData = new PWSkyWind();
        public PWSkyWeather m_pwSkyWeatherData = new PWSkyWeather();

        private void FixWarnings()
        {
            if (m_postProcessingProfileGUIDBuiltIn.Length > 0)
            {
                m_postProcessingProfileGUIDBuiltIn = "";
            }

            if (m_postProcessingProfileGUIDURP.Length > 0)
            {
                m_postProcessingProfileGUIDURP = "";
            }

            if (m_postProcessingProfileGUIDHDRP.Length > 0)
            {
                m_postProcessingProfileGUIDHDRP = "";
            }
        }
    }
}