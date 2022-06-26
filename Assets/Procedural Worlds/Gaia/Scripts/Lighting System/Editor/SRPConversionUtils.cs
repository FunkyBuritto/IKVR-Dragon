using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
    public class SRPConversionUtils
    {
        #region Universal Utils

        public static void ConvertProfileToURP(SceneProfile lightingProfile, GaiaLightingProfileValues profile, bool convertToNewProfile = false)
        {
            try
            {
                if (GaiaUtils.GetActivePipeline() != GaiaConstants.EnvironmentRenderer.Universal)
                {
                    Debug.Log("Need to be using Universal render pipeline to use this function");
                    return;
                }

                GaiaLightingProfileValues profileValue = profile;
                if (convertToNewProfile)
                {
                    GaiaLightingProfileValues newProfileValues = new GaiaLightingProfileValues();
                    GaiaUtils.CopyFields(profile, newProfileValues);
                    profileValue = newProfileValues;
                    lightingProfile.m_lightingProfiles.Add(profileValue);
                    profileValue.m_typeOfLighting = "Converted URP Profile " + (lightingProfile.m_lightingProfiles.Count - 1); 
                    lightingProfile.m_selectedLightingProfileValuesIndex = lightingProfile.m_lightingProfiles.Count - 1;
                }

                if (EditorUtility.DisplayDialog("Convert Sun", "Would you like to convert the Sun Values", "Yes", "No"))
                {
                    ConvertSun(profileValue, GaiaConstants.EnvironmentRenderer.Universal);
                }

                //ConvertPWWeather(profileValue, GaiaConstants.EnvironmentRenderer.Universal);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #endregion

        #region High Definition Utils

        public static void ConvertProfileToHDRP(SceneProfile lightingProfile, GaiaLightingProfileValues profile, bool convertToNewProfile = false)
        {
            try
            {
                if (GaiaUtils.GetActivePipeline() != GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    Debug.Log("Need to be using High Definition render pipeline to use this function");
                    return;
                }

                GaiaLightingProfileValues profileValue = profile;
                if (convertToNewProfile)
                {
                    GaiaLightingProfileValues newProfileValues = new GaiaLightingProfileValues();
                    GaiaUtils.CopyFields(profile, newProfileValues);
                    profileValue = newProfileValues;
                    lightingProfile.m_lightingProfiles.Add(profileValue);
                    profileValue.m_typeOfLighting = "Converted HDRP Profile " + (lightingProfile.m_lightingProfiles.Count - 1); 
                    lightingProfile.m_selectedLightingProfileValuesIndex = lightingProfile.m_lightingProfiles.Count - 1;
                }

                if (EditorUtility.DisplayDialog("Convert Sun", "Would you like to convert the Sun Values", "Yes", "No"))
                {
                    ConvertSun(profileValue, GaiaConstants.EnvironmentRenderer.HighDefinition);
                }

                //ConvertPWWeather(profileValue, GaiaConstants.EnvironmentRenderer.HighDefinition);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #endregion

        #region Utils

        private static void ConvertSun(GaiaLightingProfileValues profile, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            if (renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
            {
                profile.m_lWSunColor = profile.m_sunColor;
                profile.m_lWSunIntensity = profile.m_sunIntensity + 0.15f;
            }
            else
            {
                profile.m_hDSunColor = profile.m_sunColor;
                profile.m_hDSunIntensity = profile.m_sunIntensity * 3f;
            }
        }
        private static void ConvertPWWeather(GaiaLightingProfileValues profile, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
           ConvertCloudBrightness(profile, renderPipeline);
           ConvertAmbientIntensity(profile, renderPipeline);
           ConvertAtmosphereThickness(profile, renderPipeline);
           ConvertCloudHeightLevelDensity(profile, renderPipeline);
           ConvertCloudHeightLevelThickness(profile, renderPipeline);
           ConvertCloudOpacity(profile, renderPipeline);
           ConvertFogDensity(profile, renderPipeline);
           ConvertSunIntensity(profile, renderPipeline);
        }
        private static void ConvertCloudBrightness(GaiaLightingProfileValues profile, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            if (renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
            {
                for (int i = 0; i < profile.m_pwSkyAtmosphereData.CloudDomeBrightness.keys.Length; i++)
                {
                    profile.m_pwSkyAtmosphereData.CloudDomeBrightness.keys[i].value += 0.1f;
                }
            }
            else
            {
                for (int i = 0; i < profile.m_pwSkyAtmosphereData.CloudDomeBrightness.keys.Length; i++)
                {
                    profile.m_pwSkyAtmosphereData.CloudDomeBrightness.keys[i].value += 0.15f;
                }
            }
        }
        private static void ConvertAmbientIntensity(GaiaLightingProfileValues profile, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            if (renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
            {
                for (int i = 0; i < profile.m_pwSkyAtmosphereData.TODAmbientIntensity.keys.Length; i++)
                {
                    profile.m_pwSkyAtmosphereData.TODAmbientIntensity.keys[i].value += 0.05f;
                }
            }
            else
            {
                for (int i = 0; i < profile.m_pwSkyAtmosphereData.TODAmbientIntensity.keys.Length; i++)
                {
                    profile.m_pwSkyAtmosphereData.TODAmbientIntensity.keys[i].value += 0.15f;
                }
            }
        }
        private static void ConvertAtmosphereThickness(GaiaLightingProfileValues profile, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            if (renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
            {
                for (int i = 0; i < profile.m_pwSkyAtmosphereData.TODAtmosphereThickness.keys.Length; i++)
                {
                    profile.m_pwSkyAtmosphereData.TODAtmosphereThickness.keys[i].value += 0.05f;
                }
            }
            else
            {
                for (int i = 0; i < profile.m_pwSkyAtmosphereData.TODAtmosphereThickness.keys.Length; i++)
                {
                    profile.m_pwSkyAtmosphereData.TODAtmosphereThickness.keys[i].value += 0.075f;
                }
            }
        }
        private static void ConvertCloudHeightLevelDensity(GaiaLightingProfileValues profile, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            if (renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
            {
                for (int i = 0; i < profile.m_pwSkyAtmosphereData.TODCloudHeightLevelDensity.keys.Length; i++)
                {
                    profile.m_pwSkyAtmosphereData.TODCloudHeightLevelDensity.keys[i].value += 0.01f;
                }
            }
            else
            {
                for (int i = 0; i < profile.m_pwSkyAtmosphereData.TODCloudHeightLevelDensity.keys.Length; i++)
                {
                    profile.m_pwSkyAtmosphereData.TODCloudHeightLevelDensity.keys[i].value += 0.02f;
                }
            }
        }
        private static void ConvertCloudHeightLevelThickness(GaiaLightingProfileValues profile, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            if (renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
            {
                for (int i = 0; i < profile.m_pwSkyAtmosphereData.TODCloudHeightLevelThickness.keys.Length; i++)
                {
                    profile.m_pwSkyAtmosphereData.TODCloudHeightLevelThickness.keys[i].value += 0.1f;
                }
            }
            else
            {
                for (int i = 0; i < profile.m_pwSkyAtmosphereData.TODCloudHeightLevelThickness.keys.Length; i++)
                {
                    profile.m_pwSkyAtmosphereData.TODCloudHeightLevelThickness.keys[i].value += 0.125f;
                }
            }
        }
        private static void ConvertCloudOpacity(GaiaLightingProfileValues profile, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            if (renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
            {
                for (int i = 0; i < profile.m_pwSkyAtmosphereData.TODCloudOpacity.keys.Length; i++)
                {
                    profile.m_pwSkyAtmosphereData.TODCloudOpacity.keys[i].value += 0.01f;
                }
            }
            else
            {
                for (int i = 0; i < profile.m_pwSkyAtmosphereData.TODCloudOpacity.keys.Length; i++)
                {
                    profile.m_pwSkyAtmosphereData.TODCloudOpacity.keys[i].value += 0.05f;
                }
            }
        }
        private static void ConvertFogDensity(GaiaLightingProfileValues profile, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            if (renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
            {
                //Does not need changing
            }
            else
            {
                for (int i = 0; i < profile.m_pwSkyAtmosphereData.TODFogDensity.keys.Length; i++)
                {
                    profile.m_pwSkyAtmosphereData.TODFogDensity.keys[i].value += 0.005f;
                }
            }
        }
        private static void ConvertSunIntensity(GaiaLightingProfileValues profile, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            if (renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
            {
                for (int i = 0; i < profile.m_pwSkyAtmosphereData.TODSunIntensity.keys.Length; i++)
                {
                    profile.m_pwSkyAtmosphereData.TODSunIntensity.keys[i].value += 0.06f;
                }
            }
            else
            {
                for (int i = 0; i < profile.m_pwSkyAtmosphereData.TODSunIntensity.keys.Length; i++)
                {
                    profile.m_pwSkyAtmosphereData.TODSunIntensity.keys[i].value *= 3f;
                }
            }
        }

        #endregion
    }
}