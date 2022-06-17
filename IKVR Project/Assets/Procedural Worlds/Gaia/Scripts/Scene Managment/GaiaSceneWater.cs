using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Gaia
{
    public class GaiaSceneWater : MonoBehaviour
    {
        public void SaveToGaiaDefault(GaiaWaterProfileValues profileValues)
        {
            if (profileValues == null)
            {
                return;
            }

            GaiaWaterProfileValues newProfileValues = new GaiaWaterProfileValues();
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
                GaiaWaterProfile waterProfile = gaiaSettings.m_gaiaWaterProfile;
                if (waterProfile != null)
                {
                    bool addProfile = true;
                    int indexForReplacement = 0;
                    for (int i = 0; i < waterProfile.m_waterProfiles.Count; i++)
                    {
                        if (waterProfile.m_waterProfiles[i].m_typeOfWater == newProfileValues.m_typeOfWater)
                        {
                            addProfile = false;
                            indexForReplacement = i;
                        }
                    }

                    if (addProfile)
                    {
                        SaveColorAndCubemapFields(newProfileValues, profileValues);
                        waterProfile.m_waterProfiles.Add(newProfileValues);
                    }
                    else
                    {
                        #if UNITY_EDITOR
                        if (EditorUtility.DisplayDialog("Profile Already Exists", "This profile " + newProfileValues.m_typeOfWater + " already exists the the default Gaia water profile. Do you want to replace this profile?", "Yes", "No"))
                        {
                            GaiaUtils.CopyFields(newProfileValues, waterProfile.m_waterProfiles[indexForReplacement]);
                            SaveColorAndCubemapFields(waterProfile.m_waterProfiles[indexForReplacement], profileValues);
#if UNITY_POST_PROCESSING_STACK_V2
                            waterProfile.m_waterProfiles[indexForReplacement].PostProcessProfileBuiltIn = profileValues.PostProcessProfileBuiltIn;
                            waterProfile.m_waterProfiles[indexForReplacement].m_postProcessingProfileGUIDBuiltIn = profileValues.m_postProcessingProfileGUIDBuiltIn;
#endif
#if UPPipeline
                            waterProfile.m_waterProfiles[indexForReplacement].PostProcessProfileURP = profileValues.PostProcessProfileURP;
                            waterProfile.m_waterProfiles[indexForReplacement].m_postProcessingProfileGUIDURP = profileValues.m_postProcessingProfileGUIDURP;
#endif
#if HDPipeline
                            waterProfile.m_waterProfiles[indexForReplacement].PostProcessProfileHDRP = profileValues.PostProcessProfileHDRP;
                            waterProfile.m_waterProfiles[indexForReplacement].m_postProcessingProfileGUIDHDRP = profileValues.m_postProcessingProfileGUIDHDRP;
#endif
                        }
                        #endif
                    }

                    #if UNITY_EDITOR
                    EditorUtility.SetDirty(waterProfile);
                    #endif

                    Debug.Log("Profile successfully added to the Gaia Water Profile. Remember to save your project to save the changes");
                }
            }
        }
        private void SaveColorAndCubemapFields(GaiaWaterProfileValues newProfileValues, GaiaWaterProfileValues profileValues)
        {
            newProfileValues.m_underwaterFogGradient = profileValues.m_underwaterFogGradient;
            newProfileValues.m_gradientUnderwaterPostExposure = profileValues.m_gradientUnderwaterPostExposure;
            newProfileValues.m_gradientUnderwaterColorFilter = profileValues.m_gradientUnderwaterColorFilter;
            newProfileValues.m_colorDepthRamp = profileValues.m_colorDepthRamp;
            newProfileValues.m_normalLayer0 = profileValues.m_normalLayer0;
            newProfileValues.m_normalLayer1 = profileValues.m_normalLayer1;
            newProfileValues.m_fadeNormal = profileValues.m_fadeNormal;
            newProfileValues.m_foamTexture = profileValues.m_foamTexture;
            newProfileValues.m_foamAlphaRamp = profileValues.m_foamAlphaRamp;
            newProfileValues.m_renderTexture = profileValues.m_renderTexture;
            newProfileValues.m_foamBubbleTexture = profileValues.m_foamBubbleTexture;
            newProfileValues.m_refractionRenderResolution = profileValues.m_refractionRenderResolution;
            newProfileValues.m_underwaterFogColor = profileValues.m_underwaterFogColor;
            newProfileValues.m_constUnderwaterColorFilter = profileValues.m_constUnderwaterColorFilter;
            newProfileValues.m_specularColor = profileValues.m_specularColor;
        }
    }
}