using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace Gaia
{
    public class GaiaSceneLighting : MonoBehaviour
    {
        public void SaveToGaiaDefault(GaiaLightingProfileValues profileValues)
        {
            if (profileValues == null)
            {
                return;
            }

            GaiaLightingProfileValues newProfileValues = new GaiaLightingProfileValues();
            GaiaUtils.CopyFields(profileValues, newProfileValues);
            newProfileValues.m_userCustomProfile = false;
#if UNITY_POST_PROCESSING_STACK_V2
            newProfileValues.PostProcessProfileBuiltIn = profileValues.PostProcessProfileBuiltIn;
            newProfileValues.m_postProcessingProfileGUIDBuiltIn = profileValues.m_postProcessingProfileGUIDBuiltIn;
#endif
#if UPPipeline
            newProfileValues.PostProcessProfileURP = profileValues.PostProcessProfileURP;
            newProfileValues.m_postProcessingProfileGUIDURP = profileValues.m_postProcessingProfileGUIDURP;
#endif
#if HDPipeline
            newProfileValues.PostProcessProfileHDRP = profileValues.PostProcessProfileHDRP;
            newProfileValues.m_postProcessingProfileGUIDHDRP = profileValues.m_postProcessingProfileGUIDHDRP;
#endif

            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
            if (gaiaSettings != null)
            {
                GaiaLightingProfile lightingProfile = gaiaSettings.m_gaiaLightingProfile;
                if (lightingProfile != null)
                {
                    bool addProfile = true;
                    int indexForReplacement = 0;
                    for (int i = 0; i < lightingProfile.m_lightingProfiles.Count; i++)
                    {
                        if (lightingProfile.m_lightingProfiles[i].m_typeOfLighting == newProfileValues.m_typeOfLighting)
                        {
                            addProfile = false;
                            indexForReplacement = i;
                        }
                    }

                    if (addProfile)
                    {
                        SaveColorAndCubemapFields(newProfileValues, profileValues);
                        lightingProfile.m_lightingProfiles.Add(newProfileValues);
#if UNITY_EDITOR
                        EditorUtility.SetDirty(lightingProfile);
#endif
                    }
                    else
                    {
                        #if UNITY_EDITOR
                        if (EditorUtility.DisplayDialog("Profile Already Exists", "This profile " + newProfileValues.m_typeOfLighting + " already exists the the default Gaia lighting profile. Do you want to replace this profile?", "Yes", "No"))
                        {
                            GaiaUtils.CopyFields(newProfileValues, lightingProfile.m_lightingProfiles[indexForReplacement]);
                            SaveColorAndCubemapFields( lightingProfile.m_lightingProfiles[indexForReplacement], profileValues);
#if UNITY_POST_PROCESSING_STACK_V2
                            lightingProfile.m_lightingProfiles[indexForReplacement].PostProcessProfileBuiltIn = profileValues.PostProcessProfileBuiltIn;
                            lightingProfile.m_lightingProfiles[indexForReplacement].m_postProcessingProfileGUIDBuiltIn = profileValues.m_postProcessingProfileGUIDBuiltIn;
#endif
#if UPPipeline
                            lightingProfile.m_lightingProfiles[indexForReplacement].PostProcessProfileURP = profileValues.PostProcessProfileURP;
                            lightingProfile.m_lightingProfiles[indexForReplacement].m_postProcessingProfileGUIDURP = profileValues.m_postProcessingProfileGUIDURP;
#endif
#if HDPipeline
                            lightingProfile.m_lightingProfiles[indexForReplacement].PostProcessProfileHDRP = profileValues.PostProcessProfileHDRP;
                            lightingProfile.m_lightingProfiles[indexForReplacement].m_postProcessingProfileGUIDHDRP = profileValues.m_postProcessingProfileGUIDHDRP;
#endif
                        }
                        #endif
                    }

                    #if UNITY_EDITOR
                    EditorUtility.SetDirty(lightingProfile);
                    #endif

                    Debug.Log("Profile successfully added to the Gaia Lighting Profile. Remember to save your project to save the changes");
                }
            }
        }
#if GAIA_PRO_PRESENT
        public void SaveToGaiaDefault(GaiaLightingProfileValues profileValues, ProceduralWorldsGlobalWeather globalWeather)
        {
            if (profileValues == null)
            {
                return;
            }

            GaiaLightingProfileValues newProfileValues = new GaiaLightingProfileValues();
            GaiaUtils.CopyFields(profileValues, newProfileValues);
            newProfileValues.m_userCustomProfile = false;
#if UNITY_POST_PROCESSING_STACK_V2
            newProfileValues.PostProcessProfileBuiltIn = profileValues.PostProcessProfileBuiltIn;
            newProfileValues.m_postProcessingProfileGUIDBuiltIn = profileValues.m_postProcessingProfileGUIDBuiltIn;
#endif
#if UPPipeline
            newProfileValues.PostProcessProfileURP = profileValues.PostProcessProfileURP;
            newProfileValues.m_postProcessingProfileGUIDURP = profileValues.m_postProcessingProfileGUIDURP;
#endif
#if HDPipeline
            newProfileValues.PostProcessProfileHDRP = profileValues.PostProcessProfileHDRP;
            newProfileValues.m_postProcessingProfileGUIDHDRP = profileValues.m_postProcessingProfileGUIDHDRP;
            newProfileValues.EnvironmentProfileHDRP = profileValues.EnvironmentProfileHDRP;
            newProfileValues.m_environmentProfileGUIDHDRP = profileValues.m_environmentProfileGUIDHDRP;
#endif

            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
            if (gaiaSettings != null)
            {
                GaiaLightingProfile lightingProfile = gaiaSettings.m_gaiaLightingProfile;
                if (lightingProfile != null)
                {
                    bool addProfile = true;
                    int indexForReplacement = 0;
                    for (int i = 0; i < lightingProfile.m_lightingProfiles.Count; i++)
                    {
                        if (lightingProfile.m_lightingProfiles[i].m_typeOfLighting == newProfileValues.m_typeOfLighting)
                        {
                            addProfile = false;
                            indexForReplacement = i;
                        }
                    }

                    if (addProfile)
                    {
                        SavePWSkyAtmosphere(newProfileValues, globalWeather);
                        SavePWSeason(newProfileValues, globalWeather);
                        SavePWSkyCloud(newProfileValues, globalWeather);
                        SavePWSkyWeather(newProfileValues, globalWeather);
                        SavePWSkyWind(newProfileValues, globalWeather);
                        SaveColorAndCubemapFields(newProfileValues, profileValues);
                        lightingProfile.m_lightingProfiles.Add(newProfileValues);
                    }
                    else
                    {
                        #if UNITY_EDITOR
                        if (EditorUtility.DisplayDialog("Profile Already Exists", "This profile " + newProfileValues.m_typeOfLighting + " already exists the the default Gaia lighting profile. Do you want to replace this profile?", "Yes", "No"))
                        {
                            GaiaUtils.CopyFields(newProfileValues, lightingProfile.m_lightingProfiles[indexForReplacement]);
                            SavePWSkyAtmosphere(lightingProfile.m_lightingProfiles[indexForReplacement], globalWeather);
                            SavePWSeason(lightingProfile.m_lightingProfiles[indexForReplacement], globalWeather);
                            SavePWSkyCloud(lightingProfile.m_lightingProfiles[indexForReplacement], globalWeather);
                            SavePWSkyWeather(lightingProfile.m_lightingProfiles[indexForReplacement], globalWeather);
                            SavePWSkyWind(lightingProfile.m_lightingProfiles[indexForReplacement], globalWeather);
                            SaveColorAndCubemapFields( lightingProfile.m_lightingProfiles[indexForReplacement], profileValues);
#if UNITY_POST_PROCESSING_STACK_V2
                            lightingProfile.m_lightingProfiles[indexForReplacement].PostProcessProfileBuiltIn = profileValues.PostProcessProfileBuiltIn;
                            lightingProfile.m_lightingProfiles[indexForReplacement].m_postProcessingProfileGUIDBuiltIn = profileValues.m_postProcessingProfileGUIDBuiltIn;
#endif
#if UPPipeline
                            lightingProfile.m_lightingProfiles[indexForReplacement].PostProcessProfileURP = profileValues.PostProcessProfileURP;
                            lightingProfile.m_lightingProfiles[indexForReplacement].m_postProcessingProfileGUIDURP = profileValues.m_postProcessingProfileGUIDURP;
#endif
#if HDPipeline
                            lightingProfile.m_lightingProfiles[indexForReplacement].PostProcessProfileHDRP = profileValues.PostProcessProfileHDRP;
                            lightingProfile.m_lightingProfiles[indexForReplacement].m_postProcessingProfileGUIDHDRP = profileValues.m_postProcessingProfileGUIDHDRP;
#endif
                        }
                        #endif
                    }

                    #if UNITY_EDITOR
                    EditorUtility.SetDirty(lightingProfile);
                    #endif

                    Debug.Log("Profile successfully added to the Gaia Lighting Profile. Remember to save your project to save the changes");
                }
            }
        }
        private void SavePWSkyAtmosphere(GaiaLightingProfileValues profileValues, ProceduralWorldsGlobalWeather globalWeather)
        {
            if (profileValues == null || globalWeather == null)
            {
                return;
            }

            profileValues.m_pwSkyAtmosphereData.TODSkyboxTint = globalWeather.TODSkyboxTint;
            profileValues.m_pwSkyAtmosphereData.TODSkyboxExposure = globalWeather.TODSkyboxExposure;
            profileValues.m_pwSkyAtmosphereData.TODSunColor = globalWeather.TODSunColor;
            profileValues.m_pwSkyAtmosphereData.TODFogColor = globalWeather.TODFogColor;
            profileValues.m_pwSkyAtmosphereData.TODAmbientSkyColor = globalWeather.TODAmbientSkyColor;
            profileValues.m_pwSkyAtmosphereData.TODAmbientEquatorColor = globalWeather.TODAmbientEquatorColor;
            profileValues.m_pwSkyAtmosphereData.TODAmbientGroundColor = globalWeather.TODAmbientGroundColor;
            profileValues.m_pwSkyAtmosphereData.TODSunIntensity = globalWeather.TODSunIntensity;
            profileValues.m_pwSkyAtmosphereData.TODSunShadowStrength = globalWeather.TODSunShadowStrength;
            profileValues.m_pwSkyAtmosphereData.TODCloudHeightLevelDensity = globalWeather.TODCloudHeightLevelDensity;
            profileValues.m_pwSkyAtmosphereData.TODCloudHeightLevelThickness = globalWeather.TODCloudHeightLevelThickness;
            profileValues.m_pwSkyAtmosphereData.TODCloudHeightLevelSpeed = globalWeather.TODCloudHeightLevelSpeed;
            profileValues.m_pwSkyAtmosphereData.TODCloudOpacity = globalWeather.TODCloudOpacity;
            profileValues.m_pwSkyAtmosphereData.CloudDomeBrightness = globalWeather.CloudDomeBrightness;
            profileValues.m_pwSkyAtmosphereData.TODAmbientIntensity = globalWeather.TODAmbientIntensity;
            profileValues.m_pwSkyAtmosphereData.TODAtmosphereThickness = globalWeather.TODAtmosphereThickness;
            profileValues.m_pwSkyAtmosphereData.TODFogDensity = globalWeather.TODFogDensity;
            profileValues.m_pwSkyAtmosphereData.TODFogStartDistance = globalWeather.TODFogStartDistance;
            profileValues.m_pwSkyAtmosphereData.TODFogEndDistance = globalWeather.TODFogEndDistance;
            profileValues.m_pwSkyAtmosphereData.TODHDRPFogBaseHeight = globalWeather.TODHDRPFogBaseHeight;
            profileValues.m_pwSkyAtmosphereData.TODHDRPFogAnisotropy = globalWeather.TODHDRPFogAnisotropy;
            profileValues.m_pwSkyAtmosphereData.TODHDRPFogLightProbeDimmer = globalWeather.TODHDRPFogLightProbeDimmer;
            profileValues.m_pwSkyAtmosphereData.TODHDRPFogDepthExtent = globalWeather.TODHDRPFogDepthExtent;
            profileValues.m_pwSkyAtmosphereData.TODHDRPGroundTint = globalWeather.TODHDRPGroundTint;
            profileValues.m_pwSkyAtmosphereData.TODHDRPFogAlbedo = globalWeather.TODHDRPFogAlbedo;
            profileValues.m_pwSkyAtmosphereData.TODSunSize = globalWeather.TODSunSize;
            profileValues.m_pwSkyAtmosphereData.TODSunSizeConvergence = globalWeather.TODSunSizeConvergence;
        }
        private void SavePWSkyCloud(GaiaLightingProfileValues profileValues, ProceduralWorldsGlobalWeather globalWeather)
        {
            if (profileValues == null || globalWeather == null)
            {
                return;
            }

            profileValues.m_pwSkyCloudData.EnableClouds = globalWeather.EnableClouds;
            profileValues.m_pwSkyCloudData.CloudHeight = globalWeather.CloudHeight;
            profileValues.m_pwSkyCloudData.CloudAmbientColor = globalWeather.CloudAmbientColor;
            profileValues.m_pwSkyCloudData.CloudScale = globalWeather.CloudScale;
            profileValues.m_pwSkyCloudData.CloudOffset = globalWeather.CloudOffset;
            profileValues.m_pwSkyCloudData.CloudBrightness = globalWeather.CloudBrightness;
            profileValues.m_pwSkyCloudData.CloudFade = globalWeather.CloudFade;
            profileValues.m_pwSkyCloudData.CloudTilingAndWind = globalWeather.CloudTilingAndWind;
            profileValues.m_pwSkyCloudData.CloudOpacity = globalWeather.CloudOpacity;
            profileValues.m_pwSkyCloudData.CloudRotationSpeedLow = globalWeather.CloudRotationSpeedLow;
            profileValues.m_pwSkyCloudData.CloudRotationSpeedMiddle = globalWeather.CloudRotationSpeedMiddle;
            profileValues.m_pwSkyCloudData.CloudRotationSpeedFar = globalWeather.CloudRotationSpeedFar;
        }
        private void SavePWSeason(GaiaLightingProfileValues profileValues, ProceduralWorldsGlobalWeather globalWeather)
        {
            if (profileValues == null || globalWeather == null)
            {
                return;
            }

            profileValues.m_pwSkySeasonData.EnableSeasons = globalWeather.EnableSeasons;
            profileValues.m_pwSkySeasonData.SeasonMode = globalWeather.SeasonMode;
            profileValues.m_pwSkySeasonData.Season = globalWeather.Season;
            profileValues.m_pwSkySeasonData.SeasonWinterTint = globalWeather.SeasonWinterTint;
            profileValues.m_pwSkySeasonData.SeasonSpringTint = globalWeather.SeasonSpringTint;
            profileValues.m_pwSkySeasonData.SeasonSummerTint = globalWeather.SeasonSummerTint;
            profileValues.m_pwSkySeasonData.SeasonAutumnTint = globalWeather.SeasonAutumnTint;
            profileValues.m_pwSkySeasonData.m_seasonTransitionDuration = globalWeather.m_seasonTransitionDuration;
        }
        private void SavePWSkyWind(GaiaLightingProfileValues profileValues, ProceduralWorldsGlobalWeather globalWeather)
        {
            if (profileValues == null || globalWeather == null)
            {
                return;
            }

            profileValues.m_pwSkyWindData.WindSpeed = globalWeather.WindSpeed;
            profileValues.m_pwSkyWindData.WindTurbulence = globalWeather.WindTurbulence;
            profileValues.m_pwSkyWindData.WindFrequency = globalWeather.WindFrequency;
            profileValues.m_pwSkyWindData.WindDirection = globalWeather.WindDirection;
            profileValues.m_pwSkyWindData.WindMultiplier = globalWeather.WindMultiplier;
        }
        private void SavePWSkyWeather(GaiaLightingProfileValues profileValues, ProceduralWorldsGlobalWeather globalWeather)
        {
            if (profileValues == null || globalWeather == null)
            {
                return;
            }

            profileValues.m_pwSkyWeatherData.m_weatherFadeDuration = globalWeather.m_weatherFadeDuration;
            profileValues.m_pwSkyWeatherData.m_modifyFog = globalWeather.m_modifyFog;
            profileValues.m_pwSkyWeatherData.m_modifyWind = globalWeather.m_modifyWind;
            profileValues.m_pwSkyWeatherData.m_modifySkybox = globalWeather.m_modifySkybox;
            profileValues.m_pwSkyWeatherData.m_modifyPostProcessing = globalWeather.m_modifyPostProcessing;
            profileValues.m_pwSkyWeatherData.m_modifySun = globalWeather.m_modifySun;
            profileValues.m_pwSkyWeatherData.m_modifyAmbient = globalWeather.m_modifyAmbient;
            profileValues.m_pwSkyWeatherData.m_modifyClouds = globalWeather.m_modifyClouds;

            profileValues.m_pwSkyWeatherData.m_rainSettings.EnableRain = globalWeather.EnableRain;
            profileValues.m_pwSkyWeatherData.m_rainSettings.RainIntensity = globalWeather.RainIntensity;
            profileValues.m_pwSkyWeatherData.m_rainSettings.m_rainMode = globalWeather.m_rainMode;
            profileValues.m_pwSkyWeatherData.m_rainSettings.m_rainHeight = globalWeather.m_rainHeight;
            profileValues.m_pwSkyWeatherData.m_rainSettings.m_rainStepSize = globalWeather.m_rainStepSize;
            GaiaUtils.CopyFields(globalWeather.m_rainWeatherSettings, profileValues.m_pwSkyWeatherData.m_rainSettings.m_rainWeatherSettings);

            profileValues.m_pwSkyWeatherData.m_snowSettings.EnableSnow = globalWeather.EnableSnow;
            profileValues.m_pwSkyWeatherData.m_snowSettings.SnowCoverAlwaysEnabled = globalWeather.SnowCoverAlwaysEnabled;
            profileValues.m_pwSkyWeatherData.m_snowSettings.SnowIntensity = globalWeather.SnowIntensity;
            profileValues.m_pwSkyWeatherData.m_snowSettings.SnowHeight = globalWeather.SnowHeight;
            profileValues.m_pwSkyWeatherData.m_snowSettings.PermanentSnowHeight = globalWeather.PermanentSnowHeight;
            profileValues.m_pwSkyWeatherData.m_snowSettings.SnowingHeight = globalWeather.SnowingHeight;
            profileValues.m_pwSkyWeatherData.m_snowSettings.SnowFadeHeight = globalWeather.SnowFadeHeight;
            profileValues.m_pwSkyWeatherData.m_snowSettings.m_snowStormChance = globalWeather.m_snowStormChance;
            profileValues.m_pwSkyWeatherData.m_snowSettings.m_snowStepSize = globalWeather.m_snowStepSize;
            profileValues.m_pwSkyWeatherData.m_snowSettings.m_snowMode = globalWeather.m_snowMode;
            GaiaUtils.CopyFields(globalWeather.m_snowWeatherSettings, profileValues.m_pwSkyWeatherData.m_snowSettings.m_snowWeatherSettings);

            profileValues.m_pwSkyWeatherData.m_thunderSettings.m_enableThunder = globalWeather.m_enableThunder;
            profileValues.m_pwSkyWeatherData.m_thunderSettings.ThunderWaitDuration = globalWeather.ThunderWaitDuration;
            profileValues.m_pwSkyWeatherData.m_thunderSettings.m_thunderChance = globalWeather.m_thunderChance;
            profileValues.m_pwSkyWeatherData.m_thunderSettings.ThunderLightColor = globalWeather.ThunderLightColor;
            profileValues.m_pwSkyWeatherData.m_thunderSettings.ThunderLightIntensity = globalWeather.ThunderLightIntensity;
            profileValues.m_pwSkyWeatherData.m_thunderSettings.ThunderAudioSources = globalWeather.ThunderAudioSources;
            profileValues.m_pwSkyWeatherData.m_thunderSettings.ThunderLightRadius = globalWeather.ThunderLightRadius;
        }
#endif
        private void SaveColorAndCubemapFields(GaiaLightingProfileValues newProfileValues, GaiaLightingProfileValues profileValues)
        {
            newProfileValues.m_sunColor = profileValues.m_sunColor;
            newProfileValues.m_lWSunColor = profileValues.m_lWSunColor;
            newProfileValues.m_hDSunColor = profileValues.m_hDSunColor;
            newProfileValues.m_skyboxTint = profileValues.m_skyboxTint;
            newProfileValues.m_hDGradientTopColor = profileValues.m_hDGradientTopColor;
            newProfileValues.m_hDGradientMiddleColor = profileValues.m_hDGradientMiddleColor;
            newProfileValues.m_hDGradientBottomColor = profileValues.m_hDGradientBottomColor;
            newProfileValues.m_hDHDRISkybox = profileValues.m_hDHDRISkybox;
            newProfileValues.m_hDProceduralSkyTint = profileValues.m_hDProceduralSkyTint;
            newProfileValues.m_hDProceduralGroundColor = profileValues.m_hDProceduralGroundColor;
            newProfileValues.m_hDPBSGroundAlbedoTexture = profileValues.m_hDPBSGroundAlbedoTexture;
            newProfileValues.m_hDPBSGroundEmissionTexture = profileValues.m_hDPBSGroundEmissionTexture;
            newProfileValues.m_hDPBSSpaceEmissionTexture = profileValues.m_hDPBSSpaceEmissionTexture;
            newProfileValues.m_hDPBSAirOpacity = profileValues.m_hDPBSAirOpacity;
            newProfileValues.m_hDPBSAirAlbedo = profileValues.m_hDPBSAirAlbedo;
            newProfileValues.m_hDPBSAirTint = profileValues.m_hDPBSAirTint;
            newProfileValues.m_hDPBSAerosolAlbedo = profileValues.m_hDPBSAerosolAlbedo;
            newProfileValues.m_hDPBSHorizonTint = profileValues.m_hDPBSHorizonTint;
            newProfileValues.m_hDPBSZenithTint = profileValues.m_hDPBSZenithTint;
            newProfileValues.m_skyAmbient = profileValues.m_skyAmbient;
            newProfileValues.m_equatorAmbient = profileValues.m_equatorAmbient;
            newProfileValues.m_groundAmbient = profileValues.m_groundAmbient;
            newProfileValues.m_fogColor = profileValues.m_fogColor;
            newProfileValues.m_hDVolumetricFogScatterColor = profileValues.m_hDVolumetricFogScatterColor;
            newProfileValues.m_profileType = profileValues.m_profileType;
        }
        /// <summary>
        /// Adds post processing fog layer to the camera
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="enabled"></param>
        public static void AddReflectionFog(Camera camera, bool enabled)
        {
#if UNITY_POST_PROCESSING_STACK_V2
            if (camera == null)
            {
                return;
            }

            if (enabled)
            {
                PostProcessLayer layer = camera.GetComponent<PostProcessLayer>();
                if (layer == null)
                {
                    layer = camera.gameObject.AddComponent<PostProcessLayer>();
                    layer.fog.enabled = true;
                    layer.fog.excludeSkybox = true;
                    layer.antialiasingMode = PostProcessLayer.Antialiasing.None;
                    layer.volumeLayer = 2;
                    layer.volumeTrigger = camera.transform;
                }
            }
            else
            {
                PostProcessLayer layer = camera.GetComponent<PostProcessLayer>();
                if (layer != null)
                {
                    DestroyImmediate(layer);
                }
            }
#endif
        }
    }
}