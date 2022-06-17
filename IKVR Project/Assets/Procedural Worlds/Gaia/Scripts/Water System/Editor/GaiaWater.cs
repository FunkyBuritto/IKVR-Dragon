using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ProceduralWorlds.WaterSystem;
using System.IO;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
#if HDPipeline
using UnityEngine.Rendering.HighDefinition;
#endif
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace Gaia
{
    public static class GaiaWater
    {
        #region Variables

        //Water profiles
        public static List<GaiaWaterProfileValues> m_waterProfiles;
        public static SceneProfile m_waterProfile;

        //Water shader that is found
        private static string m_unityVersion;
        public static string m_waterShader = "PWS/SP/Water/Ocean vP2.1 2019_01_14";
        public static List<string> m_profileList = new List<string>();
        public static List<Material> m_allMaterials = new List<Material>();

        private const string m_builtInKeyWord = "SP";
        private const string m_lightweightKeyWord = "LW";
        private const string m_universalKeyWord = "UP";
        private const string m_highDefinitionKeyWord = "HD";

        //Parent Object Values
        public static GameObject m_parentObject;

        //Stores the gaia settings
        private static GaiaSettings m_gaiaSettings;

        //Stores the camera
        private static Camera m_camera;

        #endregion

        #region Setup

        /// <summary>
        /// Used to set the profile
        /// </summary>
        /// <param name="selectedProfile"></param>
        /// <param name="masterMaterial"></param>
        /// <param name="waterProfile"></param>
        /// <param name="spawnWater"></param>
        /// <param name="updateSettingsOnly"></param>
        public static void GetProfile(int selectedProfile, Material masterMaterial, SceneProfile waterProfile, bool spawnWater, bool updateSettingsOnly)
        {
            try
            {
                m_waterProfile = waterProfile;
                if (m_waterProfile == null)
                {
                    Debug.LogError("[GaiaProWater.GetProfile()] Asset 'Gaia Water System Profile' could not be found please make sure it exists within your project or that the name has not been changed. Due to this error the method will now exit.");
                }
                else
                {
                    if (selectedProfile == m_waterProfile.m_waterProfiles.Count)
                    {
                        return;
                    }

                    UpdateGlobalWater(masterMaterial, waterProfile.m_waterProfiles[selectedProfile], spawnWater, updateSettingsOnly, waterProfile);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Setting up the profile to update water had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Used to set the profile
        /// </summary>
        /// <param name="selectedProfile"></param>
        /// <param name="masterMaterial"></param>
        /// <param name="waterProfile"></param>
        /// <param name="spawnWater"></param>
        /// <param name="updateSettingsOnly"></param>
        public static void GetProfile(int selectedProfile, Material masterMaterial, GaiaWaterProfile waterProfile, bool spawnWater, bool updateSettingsOnly)
        {
            try
            {
                SceneProfile sceneProfile = ScriptableObject.CreateInstance<SceneProfile>();
                GaiaSceneManagement.CopySettingsTo(waterProfile, sceneProfile);
                m_waterProfile = sceneProfile;
                if (m_waterProfile == null)
                {
                    Debug.LogError("[GaiaProWater.GetProfile()] Asset 'Gaia Water System Profile' could not be found please make sure it exists within your project or that the name has not been changed. Due to this error the method will now exit.");
                }
                else
                {
                    if (selectedProfile == m_waterProfile.m_waterProfiles.Count)
                    {
                        return;
                    }

                    UpdateGlobalWater(masterMaterial, sceneProfile.m_waterProfiles[selectedProfile], spawnWater, updateSettingsOnly, sceneProfile);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Setting up the profile to update water had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }

        #endregion

        #region Apply Settings

        /// <summary>
        /// Updates the global lighting settings in your scene
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="renderPipeline"></param>
        private static void UpdateGlobalWater(Material masterMaterial, GaiaWaterProfileValues profile, bool spawnWater, bool updateSettingsOnly, SceneProfile sceneProfile)
        {
            try
            {
                GaiaUtils.GetRuntimeSceneObject();
                GaiaUtils.RemoveAquas(GaiaUtils.GetCamera());

                if (GaiaUtils.GetActivePipeline() == GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    CheckHDRPShader(GaiaShaderID.HDRPWaterShaderFile, sceneProfile);
                }

                //Spawns the water prefab in the scene
                if (spawnWater)
                {
                    SpawnWater(m_waterProfile.m_waterPrefab);
                }

                if (!updateSettingsOnly)
                {
                    //Water mesh generation
                    UpdateWaterMeshQuality(m_waterProfile, m_waterProfile.m_waterPrefab, spawnWater);
                }

                //Update the settings on the material
                SetWaterShaderSettings(profile, masterMaterial);

                //Creates command buffer manager
                CommandBufferManager.CreateBufferManager();

                //Sets the underwater effects in the scene
                SetUnderwaterEffects(m_waterProfile, profile);

                //Sets the waters reflection settings
                SetupWaterReflectionSettings(m_waterProfile, profile, true);

                Material underwaterMaterial = GaiaUtils.GetWaterMaterial(GaiaConstants.waterSurfaceObject, true);
                if (underwaterMaterial == null)
                {
                    underwaterMaterial = GaiaUtils.LoadUnderwaterMaterial();
                }
                UpdateMeshRendererMaterials(masterMaterial, underwaterMaterial);
                if (GaiaUtils.GetActivePipeline() == GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    UpdateWaterMaterialInstances(masterMaterial, underwaterMaterial, true);
                }
                else
                {
                    UpdateWaterMaterialInstances(masterMaterial, underwaterMaterial);
                }

                //Mark water as dirty
                MarkWaterMaterialDirty(m_waterProfile.m_activeWaterMaterial);

                SetReflectionTexture(sceneProfile);
                SetDefaultLayerCull(sceneProfile);
            }
            catch (Exception e)
            {
                Debug.LogError("Updating the water system settings had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        public static void SetReflectionTexture(SceneProfile profile)
        {
            if (profile == null)
            {
                return;
            }
            PWS_WaterSystem system = PWS_WaterSystem.Instance;
            if (system != null)
            {
                system.Generate(profile);
            }
        }
        private static void SetDefaultLayerCull(SceneProfile profile)
        {
            if (profile == null)
            {
                return;
            }
            PWS_WaterSystem system = PWS_WaterSystem.Instance;
            if (system != null)
            {
                if (!system.m_layersHasBeenSet)
                {
                    List<string> layers = new List<string>();
                    layers.Clear();
                    int layerCount = 0;
                    for (int i = 0; i < 32; i++)
                    {
                        string layerName = LayerMask.LayerToName(i);
                        layers.Add(layerName);
                        layerCount++;
                    }

                    for (int i = 0; i < layers.Count; i++)
                    {
                        if (layers[i].Contains(GaiaPrefabUtility.m_objectDefaultLayerName))
                        {
                            profile.m_customRenderDistances[i] = 350f;
                        }
                        else if (layers[i].Contains(GaiaPrefabUtility.m_vfxLayerName))
                        {
                            profile.m_customRenderDistances[i] = 0f;
                        }
                        else if (layers[i].Contains(GaiaPrefabUtility.m_objectSmallLayerName))
                        {
                            profile.m_customRenderDistances[i] = 50f;
                        }
                        else if (layers[i].Contains(GaiaPrefabUtility.m_objectMediumLayerName))
                        {
                            profile.m_customRenderDistances[i] = 100f;
                        }
                        else if (layers[i].Contains(GaiaPrefabUtility.m_objectLargeLayerName))
                        {
                            profile.m_customRenderDistances[i] = 200f;
                        }
                        else
                        {
                            profile.m_customRenderDistances[i] = 300f;
                        }
                    }

                    profile.m_reflectedLayers = ~0;
                    profile.m_reflectionSettingsData.m_ReflectLayers = ~0;

                    system.m_layersHasBeenSet = true;
                }
            }
        }
        /// <summary>
        /// Sets the waters main settings
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="waterMaterial"></param>
        private static void SetWaterShaderSettings(GaiaWaterProfileValues profile, Material waterMaterial)
        {
            try
            {
                //Tiling
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_waterFoamTexTiling))
                {
                    waterMaterial.SetFloat(GaiaShaderID.m_waterFoamTexTiling, profile.m_foamTiling);
                }
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID._normalTile_ID))
                {
                    waterMaterial.SetFloat(GaiaShaderID._normalTile_ID, profile.m_waterTiling);
                }
                //Color
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_waterDepthRamp))
                {
                    waterMaterial.SetTexture(GaiaShaderID.m_waterDepthRamp, profile.m_colorDepthRamp);
                }
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_waterTransparentDepth))
                {
                    waterMaterial.SetFloat(GaiaShaderID.m_waterTransparentDepth, profile.m_transparentDistance);
                }
                //Normal
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID._normalLayer0_ID))
                {
                    waterMaterial.SetTexture(GaiaShaderID._normalLayer0_ID, profile.m_normalLayer0);
                }
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID._normalLayer0Scale_ID))
                {
                    waterMaterial.SetFloat(GaiaShaderID._normalLayer0Scale_ID, profile.m_normalStrength0);
                }
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID._normalLayer1_ID))
                {
                    waterMaterial.SetTexture(GaiaShaderID._normalLayer1_ID, profile.m_normalLayer1);
                }
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID._normalLayer1Scale_ID))
                {
                    waterMaterial.SetFloat(GaiaShaderID._normalLayer1Scale_ID, profile.m_normalStrength1);
                }
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID._normalLayer2_ID))
                {
                    waterMaterial.SetTexture(GaiaShaderID._normalLayer2_ID, profile.m_fadeNormal);
                }
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID._normalLayer2Scale_ID))
                {
                    waterMaterial.SetFloat(GaiaShaderID._normalLayer2Scale_ID, profile.m_fadeNormalStrength);
                }
                //Foam
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_waterFoamTex))
                {
                    waterMaterial.SetTexture(GaiaShaderID.m_waterFoamTex, profile.m_foamTexture);
                }
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_waterFoamRampAlpha))
                {
                    waterMaterial.SetTexture(GaiaShaderID.m_waterFoamRampAlpha, profile.m_foamAlphaRamp);
                }
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_waterFoamDepth))
                {
                    waterMaterial.SetFloat(GaiaShaderID.m_waterFoamDepth, profile.m_foamDistance);
                }
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_foamStrength))
                {
                    waterMaterial.SetFloat(GaiaShaderID.m_foamStrength, profile.m_foamStrength);
                }
                //Reflection
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_waterReflectionDistortion))
                {
                    waterMaterial.SetFloat(GaiaShaderID.m_waterReflectionDistortion, profile.m_reflectionDistortion);
                }
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_waterReflectionStrength))
                {
                    waterMaterial.SetFloat(GaiaShaderID.m_waterReflectionStrength, profile.m_reflectionStrength);
                }
                //Wave
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_waterWaveShoreMove))
                {
                    waterMaterial.SetFloat(GaiaShaderID.m_waterWaveShoreMove, profile.m_shorelineMovement);
                }
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID._waveLength_ID))
                {
                    waterMaterial.SetFloat(GaiaShaderID._waveLength_ID, profile.m_waveCount);
                }
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID._waveSpeed_ID))
                {
                    waterMaterial.SetFloat(GaiaShaderID._waveSpeed_ID, profile.m_waveSpeed);
                }
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID._waveSteepness_ID))
                {
                    waterMaterial.SetFloat(GaiaShaderID._waveSteepness_ID, profile.m_waveSize);
                }
                //Lighting
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_waterMetallic))
                {
                    waterMaterial.SetFloat(GaiaShaderID.m_waterMetallic, profile.m_metallic);
                }
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_waterSmoothness))
                {
                    waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, profile.m_smoothness);
                }
                //Fade
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID._normalFadeStart_ID))
                {
                    waterMaterial.SetFloat(GaiaShaderID._normalFadeStart_ID, profile.m_fadeStart);
                }
                if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID._normalFadeDistance_ID))
                {
                    waterMaterial.SetFloat(GaiaShaderID._normalFadeDistance_ID, profile.m_fadeDistance);
                }
                //HDRP
                if (GaiaUtils.GetActivePipeline() == GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    //Underwater Color
                    if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_underwaterColor))
                    {
                        waterMaterial.SetColor(GaiaShaderID.m_underwaterColor, profile.m_underwaterFogColor);
                    }
                    //Normals
                    if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID._normalSpeed_ID))
                    {
                        waterMaterial.SetFloat(GaiaShaderID._normalSpeed_ID, profile.m_waveSpeed / 10f);
                    }
                    if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID._normalLayer0Scale_ID))
                    {
                        waterMaterial.SetFloat(GaiaShaderID._normalLayer0Scale_ID, profile.m_normalStrength0 / 4f);
                    }
                    if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID._normalLayer1Scale_ID))
                    {
                        waterMaterial.SetFloat(GaiaShaderID._normalLayer1Scale_ID, profile.m_normalStrength1 / 4f);
                    }
                    if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID._normalLayer2Scale_ID))
                    {
                        waterMaterial.SetFloat(GaiaShaderID._normalLayer2Scale_ID, profile.m_fadeNormalStrength / 4f);
                    }
                    //Foam
                    if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_foamBubblesTexture))
                    {
                        waterMaterial.SetTexture(GaiaShaderID.m_foamBubblesTexture, profile.m_foamBubbleTexture);
                    }
                    if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_foamBubblesScale))
                    {
                        waterMaterial.SetFloat(GaiaShaderID.m_foamBubblesScale, profile.m_foamBubbleScale);
                    }
                    if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_foamBubblesTiling))
                    {
                        waterMaterial.SetInt(GaiaShaderID.m_foamBubblesTiling, profile.m_foamBubbleTiling);
                    }
                    if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_foamBubblesStrength))
                    {
                        waterMaterial.SetFloat(GaiaShaderID.m_foamBubblesStrength, profile.m_foamBubblesStrength);
                    }
                    if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_foamEdge))
                    {
                        waterMaterial.SetFloat(GaiaShaderID.m_foamEdge, profile.m_foamEdge);
                    }
                    if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_foamMoveSpeed))
                    {
                        waterMaterial.SetFloat(GaiaShaderID.m_foamMoveSpeed, profile.m_foamMoveSpeed);
                    }
                    //HDRP Z Test
                    if (GaiaUtils.ValidateShaderProperty(waterMaterial, GaiaShaderID.m_hdrpZTest))
                    {
                        if (waterMaterial.GetInt(GaiaShaderID.m_hdrpZTest) != (int)GaiaConstants.HDRPDepthTest.LessEqual)
                        {
                            waterMaterial.SetInt(GaiaShaderID.m_hdrpZTest, (int)GaiaConstants.HDRPDepthTest.LessEqual);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Setting the water shader settings had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets the underwater effects
        /// </summary>
        /// <param name="profile"></param>
        private static void SetUnderwaterEffects(SceneProfile profile, GaiaWaterProfileValues profileValues)
        {
            try
            {
                if (!Application.isPlaying)
                {
                    if (profile.m_supportUnderwaterEffects)
                    {
                        GaiaConstants.EnvironmentRenderer renderPipeline = GaiaUtils.GetActivePipeline();

                        float seaLevel = 0f;
                        PWS_WaterSystem waterSystem = PWS_WaterSystem.Instance;
                        if (waterSystem != null)
                        {
                            seaLevel = waterSystem.SeaLevel;
                        }
                        if (m_parentObject == null)
                        {
                            m_parentObject = GetOrCreateParentObject(GaiaConstants.gaiaWaterObject, true);
                        }
                        GameObject underwaterEffectsObject = GameObject.Find(GaiaConstants.underwaterEffectsName);
                        if (underwaterEffectsObject == null)
                        {
                            underwaterEffectsObject = new GameObject(GaiaConstants.underwaterEffectsName);
                        }
                        underwaterEffectsObject.transform.SetParent(m_parentObject.transform);

                        GaiaUnderwaterEffects underwaterEffects = underwaterEffectsObject.GetComponent<GaiaUnderwaterEffects>();
                        if (underwaterEffects == null)
                        {
                            underwaterEffects = underwaterEffectsObject.AddComponent<GaiaUnderwaterEffects>();
                            underwaterEffects.LoadUnderwaterSystemAssets();
                        }

                        FollowPlayerSystem followPlayer = null;
                        if (profile.m_supportUnderwaterParticles)
                        {
                            if (underwaterEffects.m_underwaterParticles == null)
                            {
                                underwaterEffects.m_underwaterParticles = PrefabUtility.InstantiatePrefab(profile.m_underwaterParticles) as GameObject;
                                if (underwaterEffects.m_underwaterParticles)
                                {
                                    followPlayer = underwaterEffects.m_underwaterParticles.GetComponent<FollowPlayerSystem>();
                                    if (followPlayer == null)
                                    {
                                        followPlayer = underwaterEffects.m_underwaterParticles.AddComponent<FollowPlayerSystem>();
                                    }
                                }

                                followPlayer.m_particleObjects.Add(underwaterEffects.m_underwaterParticles);
                                if (underwaterEffects.m_playerCamera == null)
                                {
                                    if (GaiaUtils.GetCamera() != null)
                                    {
                                        underwaterEffects.m_playerCamera = GaiaUtils.GetCamera().transform;
                                    }
                                }
                                if (underwaterEffects.m_playerCamera != null)
                                {
                                    underwaterEffects.m_underwaterParticles.transform.position = underwaterEffects.m_playerCamera.transform.position;
                                    followPlayer.m_player = underwaterEffects.m_playerCamera;
                                }
                            }

                            underwaterEffects.m_underwaterParticles.transform.SetParent(underwaterEffectsObject.transform);
                        }
                        else
                        {
                            if (underwaterEffects.m_underwaterParticles != null)
                            {
                                Object.DestroyImmediate(underwaterEffects.m_underwaterParticles);
                            }
                        }

                        underwaterEffects.m_framesPerSecond = profile.m_causticFramePerSecond;
                        underwaterEffects.m_causticSize = profile.m_causticSize;
                        underwaterEffects.m_useCaustics = profile.m_useCastics;
                        underwaterEffects.m_mainLight = profile.m_mainCausticLight;
                        underwaterEffects.m_seaLevel = seaLevel;
                        underwaterEffects.m_fogColorGradient = profileValues.m_underwaterFogGradient;
                        underwaterEffects.m_fogDistance = profileValues.m_underwaterFogDistance;
                        underwaterEffects.m_fogDensity = profileValues.m_underwaterFogDensity;
                        underwaterEffects.m_nearFogDistance = profileValues.m_underwaterNearFogDistance;
                        underwaterEffects.m_constUnderwaterPostExposure = profileValues.m_constUnderwaterPostExposure;
                        underwaterEffects.m_constUnderwaterColorFilter = profileValues.m_constUnderwaterColorFilter;
                        underwaterEffects.m_gradientUnderwaterPostExposure = profileValues.m_gradientUnderwaterPostExposure;
                        underwaterEffects.m_gradientUnderwaterColorFilter = profileValues.m_gradientUnderwaterColorFilter;
                        if (profile.m_supportUnderwaterFog)
                        {
                            Transform horizonTransform = underwaterEffectsObject.transform.Find(profile.m_underwaterHorizonPrefab.name);
                            GameObject underwaterHorizon = null;
                            if (horizonTransform != null)
                            {
                                underwaterHorizon = horizonTransform.gameObject;
                            }
                            if (underwaterHorizon == null)
                            {
                                underwaterHorizon = PrefabUtility.InstantiatePrefab(profile.m_underwaterHorizonPrefab) as GameObject;
                                underwaterEffects.m_horizonObject = underwaterHorizon;
                                if (underwaterHorizon != null)
                                {
                                    FollowPlayerSystem followPlayerHorizon = underwaterHorizon.GetComponent<FollowPlayerSystem>();
                                    if (followPlayerHorizon == null)
                                    {
                                        followPlayerHorizon = underwaterHorizon.AddComponent<FollowPlayerSystem>();
                                    }

                                    followPlayerHorizon.m_followPlayer = true;
                                    followPlayerHorizon.m_isWaterObject = true;
                                    followPlayerHorizon.m_particleObjects.Add(underwaterHorizon);
                                    followPlayerHorizon.m_player = underwaterEffects.m_playerCamera;
                                    followPlayerHorizon.m_useOffset = true;
                                    followPlayerHorizon.m_xoffset = 250f;
                                    followPlayerHorizon.m_zoffset = 0f;
                                    followPlayerHorizon.m_yOffset = 580f;
                                    underwaterHorizon.transform.SetParent(underwaterEffectsObject.transform);
                                    underwaterHorizon.transform.position = new Vector3(0f, -4000f, 0f);
                                }
                            }
                        }
                        else
                        {
                            GameObject underwaterHorizon = GameObject.Find(profile.m_underwaterHorizonPrefab.name);
                            if (underwaterHorizon != null)
                            {
                                Object.DestroyImmediate(underwaterHorizon);
                            }
                        }

                        switch (renderPipeline)
                        {
                            case GaiaConstants.EnvironmentRenderer.BuiltIn:
                                SetupBuiltInUnderwaterPostProcessing(profile, profileValues, underwaterEffects, underwaterEffectsObject, seaLevel);
                                break;
                            case GaiaConstants.EnvironmentRenderer.Universal:
                                SetupUniversalUnderwaterPostProcessing(profile, profileValues, underwaterEffects, underwaterEffectsObject, seaLevel);
                                break;
                            case GaiaConstants.EnvironmentRenderer.HighDefinition:
                                SetupHighDefinitionUnderwaterPostProcessing(profile, profileValues, underwaterEffects, underwaterEffectsObject, seaLevel);
                                break;
                        }
                    }
                    else
                    {
                        GameObject underwaterEffects = GameObject.Find(GaiaConstants.underwaterEffectsName);
                        if (underwaterEffects != null)
                        {
                            Object.DestroyImmediate(underwaterEffects);
                        }
                    }
                }
                else
                {
                    if (profile.m_supportUnderwaterEffects)
                    {
                        float seaLevel = 0f;
                        PWS_WaterSystem waterSystem = PWS_WaterSystem.Instance;
                        if (waterSystem != null)
                        {
                            seaLevel = waterSystem.SeaLevel;
                        }

                        GameObject underwaterEffectsObject = GameObject.Find(GaiaConstants.underwaterEffectsName);
                        if (underwaterEffectsObject != null)
                        {
                            GaiaUnderwaterEffects underwaterEffects = underwaterEffectsObject.GetComponent<GaiaUnderwaterEffects>();
                            if (underwaterEffects == null)
                            {
                                Debug.LogWarning("Underwater effects can't be added during runtime. Please exit play mode and then apply water changes to spawn the underwater effects.");
                            }

                            FollowPlayerSystem followPlayer = null;
                            if (profile.m_supportUnderwaterParticles)
                            {
                                if (underwaterEffects.m_underwaterParticles == null)
                                {
                                    underwaterEffects.m_underwaterParticles = PrefabUtility.InstantiatePrefab(profile.m_underwaterParticles) as GameObject;
                                    if (underwaterEffects.m_underwaterParticles)
                                    {
                                        followPlayer = underwaterEffects.m_underwaterParticles.GetComponent<FollowPlayerSystem>();
                                        if (followPlayer == null)
                                        {
                                            followPlayer = underwaterEffects.m_underwaterParticles.AddComponent<FollowPlayerSystem>();
                                        }
                                    }

                                    followPlayer.m_particleObjects.Add(underwaterEffects.m_underwaterParticles);
                                    if (underwaterEffects.m_playerCamera == null)
                                    {
                                        if (GaiaUtils.GetCamera() != null)
                                        {
                                            underwaterEffects.m_playerCamera = GaiaUtils.GetCamera().transform;
                                        }
                                    }
                                    if (underwaterEffects.m_playerCamera != null)
                                    {
                                        underwaterEffects.m_underwaterParticles.transform.position = underwaterEffects.m_playerCamera.transform.position;
                                        followPlayer.m_player = underwaterEffects.m_playerCamera;
                                    }
                                }

                                underwaterEffects.m_underwaterParticles.transform.SetParent(underwaterEffectsObject.transform);
                            }
                            else
                            {
                                if (underwaterEffects.m_underwaterParticles != null)
                                {
                                    Object.DestroyImmediate(underwaterEffects.m_underwaterParticles);
                                }
                            }

                            underwaterEffects.m_framesPerSecond = profile.m_causticFramePerSecond;
                            underwaterEffects.m_causticSize = profile.m_causticSize;
                            underwaterEffects.m_useCaustics = profile.m_useCastics;
                            underwaterEffects.m_mainLight = profile.m_mainCausticLight;
                            underwaterEffects.m_seaLevel = seaLevel;
                            underwaterEffects.m_fogColorGradient = profileValues.m_underwaterFogGradient;
                            underwaterEffects.m_fogDistance = profileValues.m_underwaterFogDistance;
                            underwaterEffects.m_fogDensity = profileValues.m_underwaterFogDensity;
                            underwaterEffects.m_nearFogDistance = profileValues.m_underwaterNearFogDistance;
                            underwaterEffects.m_constUnderwaterPostExposure = profileValues.m_constUnderwaterPostExposure;
                            underwaterEffects.m_constUnderwaterColorFilter = profileValues.m_constUnderwaterColorFilter;
                            underwaterEffects.m_gradientUnderwaterPostExposure = profileValues.m_gradientUnderwaterPostExposure;
                            underwaterEffects.m_gradientUnderwaterColorFilter = profileValues.m_gradientUnderwaterColorFilter;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Updating underwater effects had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets the water mesh quality
        /// </summary>
        /// <param name="profile"></param>
        public static void UpdateWaterMeshQuality(SceneProfile profile, GameObject waterObject, bool isNewSpawn = false)
        {
            try
            {
                if (profile.m_enableWaterMeshQuality)
                {
                    GameObject waterGameObject = GameObject.Find(waterObject.name);
                    if (waterGameObject == null)
                    {
                        Debug.LogWarning("Water has not been added to the scene. Please add it to the scene then try configure the water mesh quality.");
                    }
                    else
                    {
                        bool regenerate = isNewSpawn;
                        PWS_WaterSystem waterGeneration = waterGameObject.GetComponent<PWS_WaterSystem>();
                        if (waterGeneration == null)
                        {
                            waterGeneration = waterGameObject.AddComponent<PWS_WaterSystem>();
                            waterGeneration.m_MeshType = profile.m_meshType;
                            if (waterGeneration.m_Size.x != profile.m_xSize || waterGeneration.m_Size.z != profile.m_zSize)
                            {
                                regenerate = true;
                            }

                            if (waterGeneration.m_meshDensity.x != profile.m_customMeshQuality || waterGeneration.m_meshDensity.y != profile.m_customMeshQuality)
                            {
                                regenerate = true;
                            }

                            waterGeneration.m_Size.x = profile.m_xSize;
                            waterGeneration.m_Size.z = profile.m_zSize;

                            switch (profile.m_waterMeshQuality)
                            {
                                case GaiaConstants.WaterMeshQuality.VeryLow:
                                    waterGeneration.m_meshDensity.x = 2;
                                    waterGeneration.m_meshDensity.y = 2;
                                    break;
                                case GaiaConstants.WaterMeshQuality.Low:
                                    waterGeneration.m_meshDensity.x = 4;
                                    waterGeneration.m_meshDensity.y = 4;
                                    break;
                                case GaiaConstants.WaterMeshQuality.Medium:
                                    waterGeneration.m_meshDensity.x = 6;
                                    waterGeneration.m_meshDensity.y = 6;
                                    break;
                                case GaiaConstants.WaterMeshQuality.High:
                                    waterGeneration.m_meshDensity.x = 8;
                                    waterGeneration.m_meshDensity.y = 8;
                                    break;
                                case GaiaConstants.WaterMeshQuality.VeryHigh:
                                    waterGeneration.m_meshDensity.x = 10;
                                    waterGeneration.m_meshDensity.y = 10;

                                    break;
                                case GaiaConstants.WaterMeshQuality.Ultra:
                                    waterGeneration.m_meshDensity.x = 12;
                                    waterGeneration.m_meshDensity.y = 12;

                                    break;
                                case GaiaConstants.WaterMeshQuality.Cinematic:
                                    waterGeneration.m_meshDensity.x = 14;
                                    waterGeneration.m_meshDensity.y = 14;

                                    break;
                                case GaiaConstants.WaterMeshQuality.Custom:
                                    waterGeneration.m_meshDensity.x = profile.m_customMeshQuality;
                                    waterGeneration.m_meshDensity.y = profile.m_customMeshQuality;
                                    break;
                            }
                        }
                        else
                        {
                            waterGeneration.m_MeshType = profile.m_meshType;
                            waterGeneration.m_Size.x = profile.m_xSize;
                            waterGeneration.m_Size.z = profile.m_zSize;

                            switch (profile.m_waterMeshQuality)
                            {
                                case GaiaConstants.WaterMeshQuality.VeryLow:
                                    waterGeneration.m_meshDensity.x = 2;
                                    waterGeneration.m_meshDensity.y = 2;
                                    break;
                                case GaiaConstants.WaterMeshQuality.Low:
                                    waterGeneration.m_meshDensity.x = 4;
                                    waterGeneration.m_meshDensity.y = 4;
                                    break;
                                case GaiaConstants.WaterMeshQuality.Medium:
                                    waterGeneration.m_meshDensity.x = 6;
                                    waterGeneration.m_meshDensity.y = 6;
                                    break;
                                case GaiaConstants.WaterMeshQuality.High:
                                    waterGeneration.m_meshDensity.x = 8;
                                    waterGeneration.m_meshDensity.y = 8;
                                    break;
                                case GaiaConstants.WaterMeshQuality.VeryHigh:
                                    waterGeneration.m_meshDensity.x = 10;
                                    waterGeneration.m_meshDensity.y = 10;

                                    break;
                                case GaiaConstants.WaterMeshQuality.Ultra:
                                    waterGeneration.m_meshDensity.x = 12;
                                    waterGeneration.m_meshDensity.y = 12;

                                    break;
                                case GaiaConstants.WaterMeshQuality.Cinematic:
                                    waterGeneration.m_meshDensity.x = 14;
                                    waterGeneration.m_meshDensity.y = 14;

                                    break;
                                case GaiaConstants.WaterMeshQuality.Custom:
                                    waterGeneration.m_meshDensity.x = profile.m_customMeshQuality;
                                    waterGeneration.m_meshDensity.y = profile.m_customMeshQuality;
                                    break;
                            }
                        }

                        if (regenerate)
                        {
                            waterGeneration.ProceduralMeshGeneration();
                        }
                    }
                }
                else
                {
                    GameObject waterGameObject = GameObject.Find(profile.m_waterPrefab.name);
                    if (waterGameObject == null)
                    {
                        Debug.LogWarning("Water has not been added to the scene. Please add it to the scene then try configure the water mesh quality.");
                    }
                    else
                    {
                        PWS_WaterSystem waterGeneration = waterGameObject.GetComponent<PWS_WaterSystem>();
                        if (waterGeneration != null)
                        {
                            waterGeneration.ClearMeshData();
                        }

                        MeshFilter meshFilter = waterGameObject.GetComponent<MeshFilter>();
                        if (meshFilter != null)
                        {
                            if (meshFilter.sharedMesh.name != profile.m_waterPrefab.name)
                            {
                                meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(GaiaUtils.GetAssetPath(profile.m_waterPrefab.name));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Updating water mesh generation had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }

        #endregion

        #region Utils

        /// <summary>
        /// Sets the built-in underwater post fx
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="profileValues"></param>
        /// <param name="underwaterEffects"></param>
        /// <param name="underwaterEffectsObject"></param>
        /// <param name="seaLevel"></param>
        private static void SetupBuiltInUnderwaterPostProcessing(SceneProfile profile, GaiaWaterProfileValues profileValues, GaiaUnderwaterEffects underwaterEffects, GameObject underwaterEffectsObject, float seaLevel)
        {
            try
            {
#if UNITY_POST_PROCESSING_STACK_V2
                if (profile.m_supportUnderwaterPostProcessing)
                {
                    GameObject postProcessObject = underwaterEffects.m_underwaterPostFX;
                    if (underwaterEffects.m_underwaterPostFX != null)
                    {
                        underwaterEffects.m_underwaterPostFX.SetActive(true);
                    }
                    if (postProcessObject == null)
                    {
                        postProcessObject = new GameObject(GaiaConstants.underwaterPostProcessingName);
                        postProcessObject.transform.SetParent(underwaterEffectsObject.transform);
                        postProcessObject.transform.position = new Vector3(0f, -3500f + seaLevel, 0f);
                        postProcessObject.layer = LayerMask.NameToLayer("TransparentFX");

                        PostProcessVolume postProcessVolume = postProcessObject.AddComponent<PostProcessVolume>();
                        postProcessVolume.sharedProfile = profileValues.PostProcessProfileBuiltIn;
                        postProcessVolume.priority = 3f;

                        BoxCollider boxCollider = postProcessObject.AddComponent<BoxCollider>();
                        boxCollider.isTrigger = true;
                        boxCollider.size = new Vector3(10000f, 7000f, 10000f);
                    }
                    else
                    {
                        postProcessObject.transform.SetParent(underwaterEffectsObject.transform);
                        postProcessObject.transform.position = new Vector3(0f, -3500f + seaLevel, 0f);
                        postProcessObject.layer = LayerMask.NameToLayer("TransparentFX");

                        PostProcessVolume postProcessVolume = postProcessObject.GetComponent<PostProcessVolume>();
                        if (postProcessVolume != null)
                        {
                            postProcessVolume.sharedProfile = profileValues.PostProcessProfileBuiltIn;
                            postProcessVolume.priority = 3f;
                        }

                        BoxCollider boxCollider = postProcessObject.GetComponent<BoxCollider>();
                        if (boxCollider != null)
                        {
                            boxCollider.isTrigger = true;
                            boxCollider.size = new Vector3(10000f, 7000f, 10000f);
                        }
                    }

                    underwaterEffects.m_underwaterPostFX = postProcessObject;
                    if (underwaterEffects.m_underwaterPostFX != null)
                    {
                        underwaterEffects.m_underwaterPostFX.SetActive(false);
                    }
                }
                else
                {
                    GameObject postProcessObject = GameObject.Find(GaiaConstants.underwaterPostProcessingName);
                    if (postProcessObject != null)
                    {
                        Object.DestroyImmediate(postProcessObject);
                    }
                }
#endif
            }
            catch (Exception e)
            {
                Debug.LogError("Setting up built-in post fx had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets the URP underwater post fx
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="profileValues"></param>
        /// <param name="underwaterEffects"></param>
        /// <param name="underwaterEffectsObject"></param>
        /// <param name="seaLevel"></param>
        private static void SetupUniversalUnderwaterPostProcessing(SceneProfile profile, GaiaWaterProfileValues profileValues, GaiaUnderwaterEffects underwaterEffects, GameObject underwaterEffectsObject, float seaLevel)
        {
            try
            {
#if UPPipeline
                if (profile.m_supportUnderwaterPostProcessing)
                {
                    GameObject postProcessObject = underwaterEffects.m_underwaterPostFX;
                    if (underwaterEffects.m_underwaterPostFX != null)
                    {
                        underwaterEffects.m_underwaterPostFX.SetActive(true);
                    }
                    
                    if (postProcessObject == null)
                    {
                        postProcessObject = new GameObject(GaiaConstants.underwaterPostProcessingName);
                        postProcessObject.transform.SetParent(underwaterEffectsObject.transform);
                        postProcessObject.transform.position = new Vector3(0f, -3500f + seaLevel, 0f);
                    }
                    postProcessObject.layer = 0;

                    Volume postProcessVolume = postProcessObject.GetComponent<Volume>();
                    if (postProcessVolume == null)
                    {
                        postProcessVolume = postProcessObject.AddComponent<Volume>();
                    }

                    postProcessVolume.sharedProfile = profileValues.PostProcessProfileURP;
                    postProcessVolume.priority = 3f;
                    postProcessVolume.isGlobal = false;

                    BoxCollider boxCollider = postProcessObject.GetComponent<BoxCollider>();
                    if (boxCollider == null)
                    {
                        boxCollider = postProcessObject.AddComponent<BoxCollider>();
                    }
                    boxCollider.isTrigger = true;
                    boxCollider.size = new Vector3(10000f, 7000f, 10000f);

                    underwaterEffects.m_underwaterPostFX = postProcessObject;
                    if (underwaterEffects.m_underwaterPostFX != null)
                    {
                        underwaterEffects.m_underwaterPostFX.SetActive(false);
                    }

                    GaiaUtils.RemovePostPorcessV2VolumeComponent(postProcessObject);
                }
                else
                {
                    GameObject postProcessObject = GameObject.Find(GaiaConstants.underwaterPostProcessingName);
                    if (postProcessObject != null)
                    {
                        Object.DestroyImmediate(postProcessObject);
                    }
                }
#endif
            }
            catch (Exception e)
            {
                Debug.LogError("Setting up URP post fx had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets the HDRP underwater post fx
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="profileValues"></param>
        /// <param name="underwaterEffects"></param>
        /// <param name="underwaterEffectsObject"></param>
        /// <param name="seaLevel"></param>
        private static void SetupHighDefinitionUnderwaterPostProcessing(SceneProfile profile, GaiaWaterProfileValues profileValues, GaiaUnderwaterEffects underwaterEffects, GameObject underwaterEffectsObject, float seaLevel)
        {
            try
            {
#if HDPipeline
                if (profile.m_supportUnderwaterPostProcessing)
                {
                    GameObject postProcessObject = underwaterEffects.m_underwaterPostFX;
                    if (underwaterEffects.m_underwaterPostFX != null)
                    {
                        underwaterEffects.m_underwaterPostFX.SetActive(true);
                    }
                    if (postProcessObject == null)
                    {
                        postProcessObject = new GameObject(GaiaConstants.underwaterPostProcessingName);
                        postProcessObject.transform.SetParent(underwaterEffectsObject.transform);
                        postProcessObject.transform.position = new Vector3(0f, -3500f + seaLevel, 0f);
                    }
                    postProcessObject.layer = 0;

                    Volume postProcessVolume = postProcessObject.GetComponent<Volume>();
                    if (postProcessVolume == null)
                    {
                        postProcessVolume = postProcessObject.AddComponent<Volume>();
                    }

                    postProcessVolume.sharedProfile = profileValues.PostProcessProfileHDRP;
                    postProcessVolume.priority = 3f;
                    postProcessVolume.isGlobal = false;

                    BoxCollider boxCollider = postProcessObject.GetComponent<BoxCollider>();
                    if (boxCollider == null)
                    {
                        boxCollider = postProcessObject.AddComponent<BoxCollider>();
                    }
                    boxCollider.isTrigger = true;
                    boxCollider.size = new Vector3(10000f, 7000f, 10000f);

                    underwaterEffects.m_underwaterPostFX = postProcessObject;
                    if (underwaterEffects.m_underwaterPostFX != null)
                    {
                        underwaterEffects.m_underwaterPostFX.SetActive(false);
                    }

                    GaiaUtils.RemovePostPorcessV2VolumeComponent(postProcessObject);
                }
                else
                {
                    GameObject postProcessObject = GameObject.Find(GaiaConstants.underwaterPostProcessingName);
                    if (postProcessObject != null)
                    {
                        Object.DestroyImmediate(postProcessObject);
                    }
                }
#endif
            }
            catch (Exception e)
            {
                Debug.LogError("Setting up HDRP post fx had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets water reflections up in the scene
        /// </summary>
        /// <param name="reflectionOn"></param>
        public static void SetWaterReflectionsType(bool reflectionOn, GaiaConstants.EnvironmentRenderer renderPipeline, GaiaWaterProfile profile, GaiaWaterProfileValues waterProfileValues)
        {
            try
            {
                if (m_waterProfile == null)
                {
                    GaiaUtils.GetRuntimeSceneObject();
                    if (GaiaGlobal.Instance != null)
                    {
                        m_waterProfile = GaiaGlobal.Instance.SceneProfile;
                    }
                }

                GameObject waterObject = GameObject.Find(profile.m_waterPrefab.name);

                if (renderPipeline != GaiaConstants.EnvironmentRenderer.BuiltIn)
                {
                    if (waterObject != null)
                    {
                        PWS_WaterSystem reflection = waterObject.GetComponent<PWS_WaterSystem>();
                        if (reflection != null)
                        {
                            Object.DestroyImmediate(reflection);
                        }
                    }

                    return;
                }

                if (CheckWaterMaterialAndShader(m_waterProfile.m_activeWaterMaterial))
                {
                    m_waterProfile.m_enableReflections = reflectionOn;

                    if (reflectionOn)
                    {
                        if (waterObject != null)
                        {
                            PWS_WaterSystem reflection = waterObject.GetComponent<PWS_WaterSystem>();
                            if (reflection == null)
                            {
                                reflection = waterObject.AddComponent<PWS_WaterSystem>();
                            }
                        }
                    }
                    else
                    {
                        if (waterObject != null)
                        {
                            PWS_WaterSystem reflection = waterObject.GetComponent<PWS_WaterSystem>();
                            if (reflection == null)
                            {
                                reflection = waterObject.AddComponent<PWS_WaterSystem>();

                            }
                        }
                    }

                    SetupWaterReflectionSettings(m_waterProfile, waterProfileValues, true);
                }
                else
                {
                    Debug.LogError("[GaiaProWater.SetWaterReflections()] Shader of the material does not = " + m_waterShader + " Or master water material in the water profile is empty");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Updating water reflection settings had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Get or create a parent object
        /// </summary>
        /// <param name="parentGameObject"></param>
        /// <param name="parentToGaia"></param>
        /// <returns>Parent Object</returns>
        private static GameObject GetOrCreateParentObject(string parentGameObject, bool parentToGaia)
        {
            try
            {
                //Get the parent object
                GameObject theParentGo = GameObject.Find(parentGameObject);

                if (theParentGo == null)
                {
                    theParentGo = GameObject.Find(GaiaConstants.gaiaWaterObject);

                    if (theParentGo == null)
                    {
                        theParentGo = new GameObject(GaiaConstants.gaiaWaterObject);
                    }
                }

                if (theParentGo.GetComponent<GaiaSceneWater>() == null)
                {
                    theParentGo.AddComponent<GaiaSceneWater>();
                }

                if (parentToGaia)
                {
                    GameObject gaiaParent = GaiaUtils.GetRuntimeSceneObject();
                    if (gaiaParent != null)
                    {
                        theParentGo.transform.SetParent(gaiaParent.transform);
                    }
                }

                return theParentGo;
            }
            catch (Exception e)
            {
                Debug.LogError("Get or creating the parent object had a issue " + e.Message + " This came from " + e.StackTrace);
                return null;
            }
        }
        /// <summary>
        /// Checks to see if the profile needs to be reimported
        /// </summary>
        /// <param name="hdrpWaterShader"></param>
        public static void CheckHDRPShader(string hdrpWaterShader, SceneProfile profile, bool forceImport = false)
        {
            try
            {
                if (profile.m_reimportHDRPShader || forceImport)
                {
                    if (!System.IO.Directory.Exists(GaiaUtils.GetAssetPath("Dev Utilities")))
                    {
                        if (!string.IsNullOrEmpty(hdrpWaterShader))
                        {
                            UnityEngine.Object hdrpShader = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GaiaUtils.GetAssetPath(hdrpWaterShader + ".shadergraph"));
                            if (hdrpShader != null)
                            {
                                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(hdrpShader));
                                AssetDatabase.SaveAssets();
                                profile.m_reimportHDRPShader = false;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Checking HDRP shaders had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Checks if the water object exists
        /// </summary>
        /// <returns></returns>
        public static bool DoesWaterExist()
        {
            return GameObject.Find(GaiaConstants.gaiaWaterObject) != null;
        }
        /// <summary>
        /// Mark the water material as dirty to be saved
        /// </summary>
        /// <param name="waterMaterial"></param>
        private static void MarkWaterMaterialDirty(Material waterMaterial)
        {
            if (waterMaterial != null)
            {
                EditorUtility.SetDirty(waterMaterial);

                GaiaEditorUtils.MarkSceneDirty();
            }
        }

        #endregion

        #region Utils Pro

        /// <summary>
        /// Checks to see if the shader is good to begin applying settings
        /// </summary>
        /// <param name="waterMaterial"></param>
        /// <returns></returns>
        private static bool CheckWaterMaterialAndShader(Material waterMaterial)
        {
            try
            {
                if (waterMaterial == null)
                {
                    return false;
                }
                if (waterMaterial.shader == Shader.Find(m_waterShader))
                {
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                Debug.LogError("Checking material/shader had a issue maybe the material is null " + e.Message + " This came from " + e.StackTrace);
                return false;
            }
        }
        /// <summary>
        /// Spawns the water prefab
        /// </summary>
        /// <param name="waterPrefab"></param>
        private static void SpawnWater(GameObject waterPrefab)
        {
            try
            {
                float seaLevel = 0f;
                GaiaSessionManager sceneInfo = GaiaSessionManager.GetSessionManager(false, false);
                bool gaiaSeaLevelExists = false;
                if (sceneInfo != null)
                {
                    seaLevel = sceneInfo.GetSeaLevel();
                    gaiaSeaLevelExists = true;
                }

                m_parentObject = GetOrCreateParentObject(GaiaConstants.gaiaWaterObject, true);
                if (waterPrefab == null)
                {
                    Debug.LogError("[GaiaProWater.SpawnWater()] Water prefab is empty please make sure a prefab is present that you want to spawn");
                }
                else
                {
                    RemoveOldWater();

                    float waterLocationXZ = 0f;

                    GameObject waterObject = GameObject.Find(waterPrefab.name);
                    if (waterObject == null)
                    {
                        waterObject = PrefabUtility.InstantiatePrefab(waterPrefab) as GameObject;
                        waterObject.transform.SetParent(m_parentObject.transform);
                        waterObject.transform.position = new Vector3(waterLocationXZ, seaLevel, waterLocationXZ);
                    }
                    else
                    {
                        if (!gaiaSeaLevelExists)
                        {
                            seaLevel = waterObject.transform.position.y;
                        }
                        waterObject.transform.SetParent(m_parentObject.transform);
                        waterObject.transform.position = new Vector3(waterLocationXZ, seaLevel, waterLocationXZ);
                    }
                }

                foreach (Stamper stamper in Resources.FindObjectsOfTypeAll<Stamper>())
                {
                    stamper.m_showSeaLevelPlane = false;
                }

                foreach (Spawner spawner in Resources.FindObjectsOfTypeAll<Spawner>())
                {
                    spawner.m_showSeaLevelPlane = false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Spawning the water had a issue maybe the prefab set is empty " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Updates the sea level for scene
        /// </summary>
        /// <param name="waterPrefab"></param>
        /// <param name="seaLevel"></param>
        public static void UpdateWaterSeaLevel(GameObject waterPrefab, float seaLevel)
        {
            try
            {
                if (m_parentObject == null)
                {
                    m_parentObject = GetOrCreateParentObject(GaiaConstants.gaiaWaterObject, true);
                }

                float waterLocationXZ = 0f;
                GameObject waterObject = GameObject.Find(waterPrefab.name);
                if (waterObject != null)
                {
                    waterObject.transform.SetParent(m_parentObject.transform);
                    waterObject.transform.position = new Vector3(waterLocationXZ, seaLevel, waterLocationXZ);
                }

                GameObject postProcessVolumeObject = GameObject.Find(GaiaConstants.underwaterPostProcessingName);
                if (postProcessVolumeObject != null)
                {
                    postProcessVolumeObject.transform.SetParent(m_parentObject.transform);
                    postProcessVolumeObject.transform.position = new Vector3(0f, seaLevel + 4.9f, 0f);
                }

                GaiaUnderwaterEffects underwaterEffects = GameObject.FindObjectOfType<GaiaUnderwaterEffects>();
                if (underwaterEffects != null)
                {
                    underwaterEffects.m_seaLevel = seaLevel;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Updating the sea level had a issue maybe the water object in the scene has been deleted or disabled " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Removes old water prefab from the scene
        /// </summary>
        private static void RemoveOldWater()
        {
            GameObject oldUnderwaterFX = GameObject.Find("Ambient Water Samples");
            if (oldUnderwaterFX != null)
            {
                Object.DestroyImmediate(oldUnderwaterFX);
            }
        }
        /// <summary>
        /// Configures the water reflection settings
        /// </summary>
        /// <param name="profile"></param>
        public static void SetupWaterReflectionSettings(SceneProfile profile, GaiaWaterProfileValues waterProfileValues, bool forceUpdate)
        {
            try
            {
                if (m_camera == null)
                {
                    m_camera = GaiaUtils.GetCamera();
                }

                PWS_WaterSystem reflection = Object.FindObjectOfType<PWS_WaterSystem>();
                if (reflection != null)
                {
                    if (profile.m_enableReflections)
                    {
                        reflection.m_disableAllReflections = false;
                    }
                    else
                    {
                        reflection.m_disableAllReflections = profile.m_disaleSkyboxReflection;
                    }

                    reflection.m_autoRefresh = profile.m_autoRefresh;
                    reflection.m_ignoreSceneConditions = profile.m_ignoreSceneConditions;
                    reflection.m_refreshRate = profile.m_refreshRate;
                    reflection.m_autoUpdateMode = profile.m_autoUpdateMode;
                    reflection.InfiniteMode = profile.InfiniteMode;
                    reflection.m_waterProfile = profile;
                    profile.UpdateTextureResolution();
                    reflection.m_waterProfileValues = waterProfileValues;
                    reflection.renderSize = waterProfileValues.m_refractionRenderResolution;
                    reflection.UpdateShaderValues(waterProfileValues);
                    if (waterProfileValues.m_waterGradient != null && waterProfileValues.m_colorDepthRamp == null)
                    {
                        reflection.GenerateColorDepth();
                    }
                    else
                    {
                        reflection.m_waterTexture = waterProfileValues.m_colorDepthRamp;
                    }

                    if (forceUpdate)
                    {
                        //reflection.Generate(profile);
                        SceneView.lastActiveSceneView.Repaint();
                    }

                    if (GaiaUtils.GetActivePipeline() != GaiaConstants.EnvironmentRenderer.HighDefinition)
                    {
                        GaiaPlanarReflections planarReflections = Object.FindObjectOfType<GaiaPlanarReflections>();
                        if (planarReflections != null)
                        {
                            profile.m_reflectionSettingsData.m_ClipPlaneOffset = reflection.SeaLevel + profile.m_clipPlaneOffset;
                            planarReflections.m_settings = profile.m_reflectionSettingsData;
                        }
                    }
                    else
                    {
#if HDPipeline
                        VerifyHDRPReflectionResolution(profile);
                        PlanarReflectionProbe hdrpPlanarReflection = GaiaUtils.GetHDRPPlanarReflectionProbe();
                        if (hdrpPlanarReflection != null)
                        {
#if !UNITY_2020_2_OR_NEWER
                            switch (profile.m_reflectionResolution)
                            {
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution8:
                                    Debug.Log("Resolution of '8' is not supported in HDRP please use '64' or higher.");
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution16:
                                    Debug.Log("Resolution of '16' is not supported in HDRP please use '64' or higher.");
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution32:
                                    Debug.Log("Resolution of '32' is not supported in HDRP please use '64' or higher.");
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution64:
                                    hdrpPlanarReflection.settingsRaw.resolution = PlanarReflectionAtlasResolution.PlanarReflectionResolution64;
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution128:
                                    hdrpPlanarReflection.settingsRaw.resolution = PlanarReflectionAtlasResolution.PlanarReflectionResolution128;
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution256:
                                    hdrpPlanarReflection.settingsRaw.resolution = PlanarReflectionAtlasResolution.PlanarReflectionResolution256;
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution512:
                                    hdrpPlanarReflection.settingsRaw.resolution = PlanarReflectionAtlasResolution.PlanarReflectionResolution512;
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution1024:
                                    hdrpPlanarReflection.settingsRaw.resolution = PlanarReflectionAtlasResolution.PlanarReflectionResolution1024;
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution2048:
                                    hdrpPlanarReflection.settingsRaw.resolution = PlanarReflectionAtlasResolution.PlanarReflectionResolution2048;
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution4096:
                                    hdrpPlanarReflection.settingsRaw.resolution = PlanarReflectionAtlasResolution.PlanarReflectionResolution4096;
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution8192:
                                    hdrpPlanarReflection.settingsRaw.resolution = PlanarReflectionAtlasResolution.PlanarReflectionResolution8192;
                                    break;
                            }

#else
                            //TODO : Josh : Fix so it uses new 2020.2 code
                            /*switch (profile.m_reflectionResolution)
                            {
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution8:
                                    Debug.Log("Resolution of '8' is not supported in HDRP please use '64' or higher.");
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution16:
                                    Debug.Log("Resolution of '16' is not supported in HDRP please use '64' or higher.");
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution32:
                                    Debug.Log("Resolution of '32' is not supported in HDRP please use '64' or higher.");
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution64:
                                    hdrpPlanarReflection.settingsRaw.resolutionScalable PlanarReflectionAtlasResolution.Resolution64;
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution128:
                                    hdrpPlanarReflection.settingsRaw.resolution = PlanarReflectionAtlasResolution.PlanarReflectionResolution128;
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution256:
                                    hdrpPlanarReflection.settingsRaw.resolution = PlanarReflectionAtlasResolution.PlanarReflectionResolution256;
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution512:
                                    hdrpPlanarReflection.settingsRaw.resolution = PlanarReflectionAtlasResolution.PlanarReflectionResolution512;
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution1024:
                                    hdrpPlanarReflection.settingsRaw.resolution = PlanarReflectionAtlasResolution.PlanarReflectionResolution1024;
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution2048:
                                    hdrpPlanarReflection.settingsRaw.resolution = PlanarReflectionAtlasResolution.PlanarReflectionResolution2048;
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution4096:
                                    hdrpPlanarReflection.settingsRaw.resolution = PlanarReflectionAtlasResolution.PlanarReflectionResolution4096;
                                    break;
                                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution8192:
                                    hdrpPlanarReflection.settingsRaw.resolution = PlanarReflectionAtlasResolution.PlanarReflectionResolution8192;
                                    break;
                            }*/
#endif

                            if (profile.m_enableReflections)
                            {
                                hdrpPlanarReflection.settingsRaw.cameraSettings.culling.cullingMask = ~(1 << 4) & profile.m_reflectedLayers;
                            }
                            else
                            {
                                hdrpPlanarReflection.settingsRaw.cameraSettings.culling.cullingMask = 0;
                            }
                            hdrpPlanarReflection.mode = ProbeSettings.Mode.Realtime;
                            hdrpPlanarReflection.settingsRaw.frustum.viewerScale = 1.1f;
                            hdrpPlanarReflection.settingsRaw.frustum.automaticScale = 1.1f;
                            hdrpPlanarReflection.settingsRaw.lighting.multiplier = profile.m_hdrpReflectionIntensity;
                            hdrpPlanarReflection.gameObject.transform.position = new Vector3(0f, reflection.SeaLevel + profile.m_clipPlaneOffset, 0f);
                        }
#endif
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Updating all reflection settings had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets the water reflections state if set true reflectiosn will be enabled. False and the will be disabled.
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="state"></param>
        public static void SetXRWaterReflectionState(SceneProfile profile, bool state)
        {
            if (profile == null)
            {
                return;
            }

            PWS_WaterSystem reflection = Object.FindObjectOfType<PWS_WaterSystem>();
            GameObject xrPlayer = GameObject.Find(GaiaConstants.playerXRName);
            if (state)
            {
                if (reflection != null)
                {
                    if (xrPlayer != null)
                    {
                        profile.m_enableReflections = true;
                        profile.m_disaleSkyboxReflection = false;
                        reflection.m_disableAllReflections = false;
                        return;
                    }
                }
            }
            else
            {
                if (reflection != null)
                {
                    if (xrPlayer != null)
                    {
                        if (profile.m_enableReflections)
                        {
                            Debug.Log("Water reflections was disabled because you are using a XR Controller. We recommend that water reflections are disabled but you can re-enable them in Gaia Water under reflections panel.");
                        }
                        profile.m_enableReflections = false;
                        profile.m_disaleSkyboxReflection = true;
                        reflection.m_disableAllReflections = true;
                        reflection.ClearData();
                        return;
                    }
                }
            }
        }
        /// <summary>
        /// Verifiy the HDRP water reflection resolution with the HDRP asset to set up based on the atlas size to remove an loop error
        /// </summary>
        /// <param name="profile"></param>
        private static void VerifyHDRPReflectionResolution(SceneProfile profile)
        {
            if (profile == null || profile.m_verifiedHDRPReflections)
            {
                return;
            }

#if HDPipeline
            HDRenderPipelineAsset pipelineAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            if (pipelineAsset != null)
            {
                GaiaConstants.GaiaProWaterReflectionsQuality currentRes = profile.m_reflectionResolution;
#if !UNITY_2020_2_OR_NEWER
                switch (pipelineAsset.currentPlatformRenderPipelineSettings.lightLoopSettings.planarReflectionAtlasSize)
                {
                    case PlanarReflectionAtlasResolution.PlanarReflectionResolution64:
                        profile.m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution64;
                        break;
                    case PlanarReflectionAtlasResolution.PlanarReflectionResolution128:
                        profile.m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution128;
                        break;
                    case PlanarReflectionAtlasResolution.PlanarReflectionResolution256:
                        profile.m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution256;
                        break;
                    case PlanarReflectionAtlasResolution.PlanarReflectionResolution512:
                        profile.m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution512;
                        break;
                    case PlanarReflectionAtlasResolution.PlanarReflectionResolution1024:
                        profile.m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution1024;
                        break;
                    case PlanarReflectionAtlasResolution.PlanarReflectionResolution2048:
                        profile.m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution2048;
                        break;
                    case PlanarReflectionAtlasResolution.PlanarReflectionResolution4096:
                        profile.m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution4096;
                        break;
                    case PlanarReflectionAtlasResolution.PlanarReflectionResolution8192:
                        profile.m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution8192;
                        break;
                }
#else
                //TODO : Josh : Fix so it uses new 2020.2 code
                /*switch (pipelineAsset.currentPlatformRenderPipelineSettings.lightLoopSettings.planarReflectionAtlasSize)
                {
                    case PlanarReflectionAtlasResolution.PlanarReflectionResolution64:
                        profile.m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution64;
                        break;
                    case PlanarReflectionAtlasResolution.PlanarReflectionResolution128:
                        profile.m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution128;
                        break;
                    case PlanarReflectionAtlasResolution.PlanarReflectionResolution256:
                        profile.m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution256;
                        break;
                    case PlanarReflectionAtlasResolution.PlanarReflectionResolution512:
                        profile.m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution512;
                        break;
                    case PlanarReflectionAtlasResolution.PlanarReflectionResolution1024:
                        profile.m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution1024;
                        break;
                    case PlanarReflectionAtlasResolution.PlanarReflectionResolution2048:
                        profile.m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution2048;
                        break;
                    case PlanarReflectionAtlasResolution.PlanarReflectionResolution4096:
                        profile.m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution4096;
                        break;
                    case PlanarReflectionAtlasResolution.PlanarReflectionResolution8192:
                        profile.m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution8192;
                        break;
                }*/
#endif

                if (profile.m_reflectionResolution != currentRes)
                {
                    Debug.Log("Reflection Resolution has been set to " + profile.m_reflectionResolution.ToString() + " This has been updated due to the render pipeline planar reflection atlas size. You can change this in the HDRP Pipeline Asset and then will allow you to have higher resolution on the water reflection");
                }

                profile.m_verifiedHDRPReflections = true;
            }
#endif
        }
        /// <summary>
        /// Updates the water material isntaces on the water mesh in the scene
        /// </summary>
        /// <param name="masterMaterial"></param>
        /// <param name="underwaterMaterial"></param>
        /// <param name="singleMaterial"></param>
        private static void UpdateWaterMaterialInstances(Material masterMaterial, Material underwaterMaterial, bool singleMaterial = false)
        {
            try
            {
                List<Material> waterMaterials = new List<Material>();
                if (masterMaterial != null)
                {
                    waterMaterials.Add(masterMaterial);
                }

                if (!singleMaterial)
                {
                    if (underwaterMaterial != null)
                    {
                        waterMaterials.Add(underwaterMaterial);
                    }

                    if (waterMaterials.Count == 2)
                    {
                        PWS_WaterSystem waterSystem = GameObject.FindObjectOfType<PWS_WaterSystem>();
                        if (waterSystem != null)
                        {
                            waterSystem.SetWaterShader(waterMaterials[0], GaiaUtils.GetActivePipeline());
                            waterSystem.CreateWaterMaterialInstances(waterMaterials);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Water Materials instances could not be created due to a missing material not being found.");
                    }
                }
                else
                {
                    if (waterMaterials.Count == 1)
                    {
                        PWS_WaterSystem waterSystem = GameObject.FindObjectOfType<PWS_WaterSystem>();
                        if (waterSystem != null)
                        {
                            waterSystem.SetWaterShader(waterMaterials[0], GaiaUtils.GetActivePipeline());
                            waterSystem.CreateWaterMaterialInstances(waterMaterials);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Water Materials instances could not be created due to a missing material not being found.");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Setting up the water material instances had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets the materials on the mesh renderer
        /// </summary>
        /// <param name="masterMaterial"></param>
        /// <param name="underwaterMaterial"></param>
        private static void UpdateMeshRendererMaterials(Material masterMaterial, Material underwaterMaterial)
        {
            try
            {
                GaiaConstants.EnvironmentRenderer renderPipeline = GaiaUtils.GetActivePipeline();
                GameObject waterObject = GameObject.Find(GaiaConstants.waterSurfaceObject);
                if (renderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    if (waterObject != null)
                    {
                        MeshRenderer renderer = waterObject.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                            List<Material> materials = new List<Material>();
                            if (masterMaterial != null)
                            {
                                materials.Add(masterMaterial);
                            }
                            if (underwaterMaterial != null)
                            {
                                materials.Add(underwaterMaterial);
                            }

                            renderer.sharedMaterials = materials.ToArray();
                        }
                    }
                }
                else
                {
                    if (waterObject != null)
                    {
                        MeshRenderer renderer = waterObject.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                            List<Material> materials = new List<Material>();
                            if (masterMaterial != null)
                            {
                                materials.Add(masterMaterial);
                            }

                            renderer.sharedMaterials = materials.ToArray();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Updating the mesh renderer with new generated materials had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Gets the gaia ocean material
        /// </summary>
        /// <returns></returns>
        public static Material GetGaiaOceanMaterial()
        {
            return AssetDatabase.LoadAssetAtPath<Material>(GaiaUtils.GetAssetPath("Gaia Ocean.mat"));
        }

        #endregion
    }
}