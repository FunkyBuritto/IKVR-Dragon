using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if UPPipeline
using UnityEngine.Rendering.Universal;
#endif
using System.Collections;
using UnityEditor.SceneManagement;
using ProceduralWorlds.WaterSystem;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Gaia.Pipeline.URP
{
    /// <summary>
    /// Static class that handles all the LWRP setup in Gaia
    /// </summary>
    public static class GaiaURPPipelineUtils
    {
        public static float m_waitTimer1 = 1f;
        public static float m_waitTimer2 = 3f;

        /// <summary>
        /// Configures project for LWRP
        /// </summary>
        /// <param name="profile"></param>
        private static void ConfigureSceneToURP(UnityPipelineProfile profile)
        {
            try
            {
                GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
                if (gaiaSettings.m_currentRenderer != GaiaConstants.EnvironmentRenderer.Universal)
                {
                    Debug.LogError("Unable to configure your scene/project to URP as the current render inside of gaia does not equal Universal as it's active render pipeline. This process [GaiaLWRPPipelineUtils.ConfigureSceneToURP()] will now exit.");
                    return;
                }

                if (profile.m_setUPPipelineProfile)
                {
                    SetPipelineAsset(profile);
                }

                if (profile.m_UPAutoConfigureCamera)
                {
                    ConfigureCamera();
                }

                if (gaiaSettings.m_gaiaLightingProfile.m_enablePostProcessing)
                {
                    if (GaiaGlobal.Instance != null)
                    {
                        if (gaiaSettings.m_gaiaLightingProfile.m_selectedLightingProfileValuesIndex <= gaiaSettings.m_gaiaLightingProfile.m_lightingProfiles.Count - 1)
                        {
                            ApplyURPPostProcessing(gaiaSettings.m_gaiaLightingProfile.m_lightingProfiles[gaiaSettings.m_gaiaLightingProfile.m_selectedLightingProfileValuesIndex], GaiaGlobal.Instance.SceneProfile);
                        }
                    }
                }

                if (profile.m_UPAutoConfigureLighting)
                {
                    ConfigureLighting();
                }

                if (profile.m_UPAutoConfigureWater)
                {
                    ConfigureWater(profile, gaiaSettings);
                }

                if (profile.m_UPAutoConfigureProbes)
                {
                    GaiaURPRuntimeUtils.ConfigureReflectionProbes();
                }

                if (profile.m_UPAutoConfigureTerrain)
                {
                    GaiaURPRuntimeUtils.ConfigureTerrain(profile);
                }

                if (profile.m_UPAutoConfigureBiomePostFX)
                {
                    UpdateBiomePostFX();
                }

                FinalizeURP(profile);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// Configures scripting defines in the project
        /// </summary>
        public static void SetScriptingDefines(UnityPipelineProfile profile)
        {
            bool isChanged = false;
            string currBuildSettings = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (!currBuildSettings.Contains("UPPipeline"))
            {
                if (string.IsNullOrEmpty(currBuildSettings))
                {
                    currBuildSettings = "UPPipeline";
                }
                else
                {
                    currBuildSettings += ";UPPipeline";
                }
                isChanged = true;
            }

            if (currBuildSettings.Contains("HDPipeline"))
            {
                currBuildSettings = currBuildSettings.Replace("HDPipeline;", "");
                currBuildSettings = currBuildSettings.Replace("HDPipeline", "");
                isChanged = true;
            }

            if (currBuildSettings.Contains("LWPipeline"))
            {
                currBuildSettings = currBuildSettings.Replace("LWPipeline;", "");
                currBuildSettings = currBuildSettings.Replace("LWPipeline", "");
                isChanged = true;
            }

            if (isChanged)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, currBuildSettings);
            }
        }
        /// <summary>
        /// Sets the pipeline asset to the procedural worlds asset if the profile is set yo change it
        /// </summary>
        /// <param name="profile"></param>
        public static void SetPipelineAsset(UnityPipelineProfile profile)
        {
            try
            {
                GaiaPackageVersion unityVersion = GaiaManagerEditor.GetPackageVersion();
                UnityVersionPipelineAsset mapping = profile.m_universalPipelineProfiles.Find(x => x.m_unityVersion == unityVersion);
                string pipelineAssetName = "";
                if (mapping != null)
                {
                    pipelineAssetName = mapping.m_pipelineAssetName;
                }
                else
                {
                    Debug.LogError("Could not determine the correct render pipeline settings asset for this unity version / rendering pipeline!");
                    return;
                }

                GraphicsSettings.renderPipelineAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(GaiaUtils.GetAssetPath(pipelineAssetName + GaiaConstants.gaiaFileFormatAsset));
                profile.m_pipelineSwitchUpdates = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// Updates the pipeline asset depth/opaque settings
        /// </summary>
        /// <param name="updateDepth"></param>
        /// <param name="updateOpaque"></param>
        public static void UpdateURPPipelineSettings(bool updateDepth, bool updateOpaque)
        {
            #if UPPipeline
            UniversalRenderPipelineAsset pipelineAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            if (pipelineAsset != null)
            {
                if (updateDepth)
                {
                    pipelineAsset.supportsCameraDepthTexture = false;
                    pipelineAsset.supportsCameraDepthTexture = true;
                }

                if (updateOpaque)
                {
                    pipelineAsset.supportsCameraOpaqueTexture = false;
                    pipelineAsset.supportsCameraOpaqueTexture = true;
                }

                QualitySettings.renderPipeline = pipelineAsset;
                pipelineAsset.OnAfterDeserialize();
                EditorUtility.SetDirty(pipelineAsset);
                AssetDatabase.SaveAssets();
            }
            #endif
        }
        /// <summary>
        /// Configures camera to LWRP
        /// </summary>
        private static void ConfigureCamera()
        {
            try
            {
                Camera camera = GaiaUtils.GetCamera();
                if (camera == null)
                {
                    Debug.LogWarning("[GaiaUPRPPipelineUtils.ConfigureCamera()] A camera could not be found to upgrade in your scene.");
                }
                else
                {
#if UPPipeline
                    UniversalAdditionalCameraData cameraData = GaiaURPRuntimeUtils.GetUPCameraData(camera);
                    cameraData.renderShadows = true;
#endif
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// Configures lighting to LWRP
        /// </summary>
        private static void ConfigureLighting()
        {
            try
            {
#if UPPipeline
                Light[] lights = Object.FindObjectsOfType<Light>();
                if (lights.Length > 0)
                {
                    foreach (Light data in lights)
                    {
                        GaiaURPRuntimeUtils.GetUPLightData(data);
                    }
                }
#endif
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// Sets the sun intensity
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="volumeProfile"></param>
        public static void SetSunSettings(GaiaLightingProfileValues profile)
        {
            try
            {
                Light light = GaiaUtils.GetMainDirectionalLight();
                if (light != null)
                {
                    light.color = profile.m_lWSunColor;
                    light.intensity = profile.m_lWSunIntensity;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// Configures water to URP
        /// </summary>
        /// <param name="profile"></param>
        private static void ConfigureWater(UnityPipelineProfile profile, GaiaSettings gaiaSettings)
        {
            try
            {
                if (gaiaSettings == null)
                {
                    Debug.LogError("Gaia settings could not be found. Please make sure gaia settings is import into your project");
                }
                else
                {
                    if (profile.m_underwaterHorizonMaterial != null)
                    {
                        profile.m_underwaterHorizonMaterial.shader = Shader.Find(profile.m_universalHorizonObjectShader);
                    }

                    GameObject waterObject = GameObject.Find(gaiaSettings.m_gaiaWaterProfile.m_waterPrefab.name);
                    if (waterObject != null)
                    {
                        Material waterMat = GaiaWater.GetGaiaOceanMaterial();
                        if (waterMat != null)
                        {
                            if (GaiaGlobal.Instance != null)
                            {
                                GaiaWater.GetProfile(gaiaSettings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex, waterMat, GaiaGlobal.Instance.SceneProfile, true, false);
                            }
                        }
                        else
                        {
                            Debug.Log("Material could not be found");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// Apply URP post fx
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="lightingProfile"></param>
        public static GameObject ApplyURPPostProcessing(GaiaLightingProfileValues profile, SceneProfile lightingProfile)
        {
            try
            {
                GameObject volumeObject = null;
#if UPPipeline
                if (lightingProfile.m_enablePostProcessing)
                {
                    volumeObject = GameObject.Find("Global Post Processing");
                    if (volumeObject == null)
                    {
                        volumeObject = new GameObject("Global Post Processing");
                    }

                    GameObject parentObject = GaiaLighting.GetOrCreateParentObject(GaiaConstants.gaiaLightingObject, true);
                    if (parentObject != null)
                    {
                        volumeObject.transform.SetParent(parentObject.transform);
                    }
                    volumeObject.layer = 0;

                    Volume volume = volumeObject.GetComponent<Volume>();
                    if (volume == null)
                    {
                        volume = volumeObject.AddComponent<Volume>();
                    }

                    if (GaiaGlobal.Instance != null)
                    {
                        SceneProfile sceneProfile = GaiaGlobal.Instance.SceneProfile;
                        if (sceneProfile != null)
                        {
                            if (sceneProfile.m_lightingEditSettings || profile.m_userCustomProfile)
                            {
                                sceneProfile.m_universalPostFXProfile = profile.PostProcessProfileURP;
                            }
                            else
                            {
                                CreatePostFXProfileInstance(sceneProfile, profile);
                            }

                            volume.sharedProfile = sceneProfile.m_universalPostFXProfile;

                            Camera camera = GaiaUtils.GetCamera();
                            if (camera != null)
                            {
                                UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
                                if (cameraData == null)
                                {
                                    cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                                }

                                cameraData.renderPostProcessing = true;
                                GaiaLighting.ConfigureAntiAliasing(sceneProfile, GaiaConstants.EnvironmentRenderer.Universal);
                            }
                        }
                    }
                }
                else
                {
                    volumeObject = GameObject.Find("Global Post Processing");
                    if (volumeObject != null)
                    {
                        GameObject.DestroyImmediate(volumeObject);
                    }

                    Camera camera = GaiaUtils.GetCamera();
                    if (camera != null)
                    {
                        UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
                        if (cameraData == null)
                        {
                            cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                        }

                        cameraData.renderPostProcessing = false;
                    }
                }
    #endif
    #if UNITY_POST_PROCESSING_STACK_V2
                PostProcessLayer postProcessLayer = GameObject.FindObjectOfType<PostProcessLayer>();
                if (postProcessLayer != null)
                {
                    GameObject.DestroyImmediate(postProcessLayer);
                }

                PostProcessVolume postProcessVolume = GameObject.FindObjectOfType<PostProcessVolume>();
                if (postProcessVolume != null)
                {
                    GameObject.DestroyImmediate(postProcessVolume);
                }
    #endif

                return volumeObject;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// Crate post processing instance profile
        /// </summary>
        /// <param name="sceneProfile"></param>
        public static void CreatePostFXProfileInstance(SceneProfile sceneProfile, GaiaLightingProfileValues profile)
        {
            try
            {
                if (sceneProfile == null)
                {
                    return;
                }

#if UPPipeline
                VolumeProfile volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GaiaUtils.GetAssetPath("URP Global Post Processing Profile.asset"));
                if (profile.PostProcessProfileURP != null)
                {
                    volumeProfile = profile.PostProcessProfileURP;
                }
                if (volumeProfile == null)
                {
                    return;
                }

                GaiaSessionManager session = GaiaSessionManager.GetSessionManager();
                if (session != null)
                {
                    string path = GaiaDirectories.GetSceneProfilesFolderPath(session.m_session);
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (EditorSceneManager.GetActiveScene() != null)
                        {
                            if (!string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
                            {
                                path = path + "/" + EditorSceneManager.GetActiveScene().name + " " + volumeProfile.name + " Profile.asset";
                            }
                            else
                            {
                                path = path + "/" + volumeProfile.name + " HDRP Post FX Profile.asset";
                            }
                        }
                        else
                        {
                            path = path + "/" + volumeProfile.name + " HDRP Post FX Profile.asset";
                        }

                        if (sceneProfile.m_universalPostFXProfile != null)
                        {
                            if (!sceneProfile.m_isUserProfileSet)
                            {
                                if (!sceneProfile.m_universalPostFXProfile.name.Contains(volumeProfile.name))
                                {
                                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(sceneProfile.m_universalPostFXProfile));
                                    sceneProfile.m_universalPostFXProfile = null;
                                }
                            }
                        }

                        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) == null)
                        {
                            FileUtil.CopyFileOrDirectory(AssetDatabase.GetAssetPath(volumeProfile), path);
                            AssetDatabase.ImportAsset(path);
                        }

                        sceneProfile.m_universalPostFXProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                    }
                }
#endif
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// Update the shadow distance
        /// </summary>
        private static void UpdateShadowDistance()
        {
#if UPPipeline
            UniversalRenderPipelineAsset asset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            if (asset != null)
            {
                bool setDirty = false;
                if (asset.shadowDistance != QualitySettings.shadowDistance)
                {
                    asset.shadowDistance = QualitySettings.shadowDistance;
                    setDirty = true;
                }

                if (asset.shadowCascadeOption != ShadowCascadesOption.FourCascades)
                {
                    asset.shadowCascadeOption = ShadowCascadesOption.FourCascades;
                    setDirty = true;
                }

                if (setDirty)
                {
                    EditorUtility.SetDirty(asset);
                }
            }
#endif
        }
        /// <summary>
        /// Finalizes the LWRP setup for your project 
        /// </summary>
        /// <param name="profile"></param>
        private static void FinalizeURP(UnityPipelineProfile profile)
        {
            try
            {
                MarkSceneDirty(true);
                EditorUtility.SetDirty(profile);
                profile.m_activePipelineInstalled = GaiaConstants.EnvironmentRenderer.Universal;
                UpdateURPPipelineSettings(true, true);

                Debug.Log("Finalized URP");

                GaiaManagerEditor manager = EditorWindow.GetWindow<Gaia.GaiaManagerEditor>(false, "Gaia Manager");
                if (manager != null)
                {
                    manager.GaiaManagerStatusCheck(true);
                    manager.CheckForSetupIssues();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// Updates the biome post fx to SRP volume profile
        /// </summary>
        private static void UpdateBiomePostFX()
        {
            try
            {
                GaiaPostProcessBiome[] gaiaPostProcessBiomes = GameObject.FindObjectsOfType<GaiaPostProcessBiome>();
                if (gaiaPostProcessBiomes.Length > 0)
                {
                    foreach (GaiaPostProcessBiome biome in gaiaPostProcessBiomes)
                    {
#if UNITY_POST_PROCESSING_STACK_V2
                        PostProcessVolume processVolume = biome.GetComponent<PostProcessVolume>();
                        if (processVolume != null)
                        {
                            GameObject.DestroyImmediate(processVolume);
                        }
#endif

#if UPPipeline
                    Volume volumeComp = biome.GetComponent<Volume>();
                    if (volumeComp == null)
                    {
                        volumeComp = biome.gameObject.AddComponent<Volume>();
                        volumeComp.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GaiaUtils.GetAssetPath("UP " + biome.PostProcessingFileName + ".asset"));
                    }
#endif
                    }
                }
                UpdateShadowDistance();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// Cleans up LWRP components in the scene
        /// </summary>
        public static void CleanUpURP(UnityPipelineProfile profile, GaiaSettings gaiaSettings)
        {
            try
            {
#if UPPipeline
                UniversalAdditionalCameraData[] camerasData = GameObject.FindObjectsOfType<UniversalAdditionalCameraData>();
                GaiaURPRuntimeUtils.ClearUPCameraData(camerasData);

                UniversalAdditionalLightData[] lightsData = GameObject.FindObjectsOfType<UniversalAdditionalLightData>();
                GaiaURPRuntimeUtils.ClearUPLightData(lightsData);

                GameObject volumeObject = GameObject.Find("Global Post Processing");
                if (volumeObject != null)
                {
                    GameObject.DestroyImmediate(volumeObject);
                }

                Camera camera = GaiaUtils.GetCamera();
                if (camera != null)
                {
                    UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
                    if (cameraData == null)
                    {
                        cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                    }

                    cameraData.renderPostProcessing = false;
                }
    #endif

                if (profile.m_underwaterHorizonMaterial != null)
                {
                    profile.m_underwaterHorizonMaterial.shader = Shader.Find(profile.m_builtInHorizonObjectShader);
                }

                GameObject waterPrefab = GameObject.Find(gaiaSettings.m_gaiaWaterProfile.m_waterPrefab.name);
                if (waterPrefab != null)
                {
                    //reverting default water mesh quality
                    gaiaSettings.m_gaiaWaterProfile.m_customMeshQuality = 2;
                    if (GaiaGlobal.Instance != null)
                    {
                        GaiaWater.UpdateWaterMeshQuality(GaiaGlobal.Instance.SceneProfile, gaiaSettings.m_gaiaWaterProfile.m_waterPrefab);
                    }

                    PWS_WaterSystem reflection = waterPrefab.GetComponent<PWS_WaterSystem>();
                    if (reflection == null)
                    {
                        reflection = waterPrefab.AddComponent<PWS_WaterSystem>();
                    }
                }

                Terrain[] terrains = Terrain.activeTerrains;
                if (terrains != null)
                {
                    foreach (Terrain terrain in terrains)
                    {
    #if !UNITY_2019_2_OR_NEWER
                        terrain.materialType = Terrain.MaterialType.BuiltInStandard;
    #else
                        terrain.materialTemplate = profile.m_builtInTerrainMaterial;
    #endif
                    }
                }

                Terrain terrainDetail = Terrain.activeTerrain;
                if (terrainDetail != null)
                {
                    if (terrainDetail.detailObjectDensity == 0f)
                    {
                        if (EditorUtility.DisplayDialog("Detail Density Disabled!", "Details density is disabled on your terrain would you like to activate it?", "Yes", "No"))
                        {
                            terrainDetail.detailObjectDensity = 0.3f;
                        }
                    }
                }

                GameObject LWRPReflections = GameObject.Find("URP Water Reflection Probe");
                if (LWRPReflections != null)
                {
                    Object.DestroyImmediate(LWRPReflections);
                }

                GraphicsSettings.renderPipelineAsset = null;
                QualitySettings.renderPipeline = null;

                if (GaiaGlobal.Instance != null)
                {
                    //GaiaUtils.GetRuntimeSceneObject();
                    if (GaiaGlobal.Instance.SceneProfile != null && GaiaGlobal.Instance.SceneProfile.m_lightingProfiles.Count>0)
                    {
                        GaiaLighting.GetProfile(GaiaGlobal.Instance.SceneProfile, gaiaSettings.m_pipelineProfile, GaiaConstants.EnvironmentRenderer.BuiltIn);
                    }
                }

                //Clean up the UPR post processing objects in the underwater effects
                //We need to look for transform instead of GameObjects, since the GOs can be disabled and won't be found then
                GameObject uwe = GameObject.Find(GaiaConstants.underwaterEffectsName);
                if (uwe != null)
                {

                    Transform utoTransform  = uwe.transform.Find(GaiaConstants.underwaterTransitionObjectName);
                    if (utoTransform != null)
                    {
                        Object.DestroyImmediate(utoTransform.gameObject);
                    }

                    Transform uppTransform = uwe.transform.Find(GaiaConstants.underwaterPostProcessingName);
                    if (uppTransform != null)
                    {
                        Object.DestroyImmediate(uppTransform.gameObject);
                    }

                    Transform horizonTransform = uwe.transform.Find(GaiaConstants.underwaterHorizonName);
                    if (horizonTransform != null)
                    {
                        Object.DestroyImmediate(horizonTransform.gameObject);
                    }
                }


                if (waterPrefab != null)
                {
                    Material waterMat = GaiaWater.GetGaiaOceanMaterial();
                    if (waterMat != null)
                    {
                        if (GaiaGlobal.Instance != null)
                        {
                            GaiaWater.GetProfile(gaiaSettings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex, waterMat, GaiaGlobal.Instance.SceneProfile, true, false);
                        }
                    }
                    else
                    {
                        Debug.Log("Material could not be found");
                    }
                }

                MarkSceneDirty(false);
                EditorUtility.SetDirty(profile);
                profile.m_activePipelineInstalled = GaiaConstants.EnvironmentRenderer.BuiltIn;

                GaiaManagerEditor manager = EditorWindow.GetWindow<Gaia.GaiaManagerEditor>(false, "Gaia Manager");
                if (manager != null)
                {
                    manager.GaiaManagerStatusCheck(true);
                }

                bool isChanged = false;
                string currBuildSettings = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                if (currBuildSettings.Contains("UPPipeline"))
                {
                    currBuildSettings = currBuildSettings.Replace("UPPipeline;", "");
                    currBuildSettings = currBuildSettings.Replace("UPPipeline", "");
                    isChanged = true;
                }

                if (isChanged)
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, currBuildSettings);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// Enables or disables motion blur
        /// </summary>
        /// <param name="enabled"></param>
        private static void SetMotionBlurPostFX(bool enabled)
        {
            try
            {
#if UNITY_POST_PROCESSING_STACK_V2
                //Create profile list
                List<PostProcessProfile> postProcessProfiles = new List<PostProcessProfile>();
                //Clear List
                postProcessProfiles.Clear();

                //All volumes in scene
                PostProcessVolume[] postProcessVolumes = Object.FindObjectsOfType<PostProcessVolume>();
                if (postProcessVolumes != null)
                {
                    foreach(PostProcessVolume volume in postProcessVolumes)
                    {
                        //Check it has a profile
                        if (volume.sharedProfile != null)
                        {
                            //Add the profile to array
                            postProcessProfiles.Add(volume.sharedProfile);
                        }
                    }
                }

                if (postProcessProfiles != null)
                {
                    //Motion blur
                    foreach (PostProcessProfile profile in postProcessProfiles)
                    {
                        //Check if profile has setting
                        if (profile.TryGetSettings(out UnityEngine.Rendering.PostProcessing.MotionBlur motionBlur))
                        {

                            EditorUtility.SetDirty(profile);
                            //Set ture/false based on input
                            motionBlur.active = enabled;
                            motionBlur.enabled.value = enabled;

                            Debug.Log(profile.name + ": Motion blur has been disabled in this profile");
                        }
                    }
                }
#endif
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// Sets the AA mode ont he camera
        /// </summary>
        /// <param name="antiAliasingMode"></param>
        private static void SetAntiAliasingMode(GaiaConstants.GaiaProAntiAliasingMode antiAliasingMode)
        {
            try
            {
#if UNITY_POST_PROCESSING_STACK_V2
                PostProcessLayer processLayer = Object.FindObjectOfType<PostProcessLayer>();
                if (processLayer != null)
                {
                    Camera camera = Camera.main;
                    switch (antiAliasingMode)
                    {
                        case GaiaConstants.GaiaProAntiAliasingMode.None:
                            processLayer.antialiasingMode = PostProcessLayer.Antialiasing.None;
                            camera.allowMSAA = false;
                            break;
                        case GaiaConstants.GaiaProAntiAliasingMode.FXAA:
                            processLayer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
                            camera.allowMSAA = false;
                            break;
                        case GaiaConstants.GaiaProAntiAliasingMode.MSAA:
                            processLayer.antialiasingMode = PostProcessLayer.Antialiasing.None;
                            camera.allowMSAA = true;
                            break;
                        case GaiaConstants.GaiaProAntiAliasingMode.SMAA:
                            processLayer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
                            camera.allowMSAA = false;
                            break;
                        case GaiaConstants.GaiaProAntiAliasingMode.TAA:
                            processLayer.antialiasingMode = PostProcessLayer.Antialiasing.TemporalAntialiasing;
                            camera.allowMSAA = false;
                            break;
                    }
                }
#endif
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// Starts the LWRP Setup
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public static IEnumerator StartURPSetup(UnityPipelineProfile profile)
        {
            if (profile == null)
            {
                Debug.LogError("UnityPipelineProfile is empty");
                yield return null;
            }
            else
            {
                EditorUtility.DisplayProgressBar("Installing Universal", "Updating scripting defines", 0.5f);
                m_waitTimer1 -= Time.deltaTime;
                if (m_waitTimer1 < 0)
                {
                    SetScriptingDefines(profile);
                }
                else
                {
                    yield return null;
                }

                while (EditorApplication.isCompiling)
                {
                    yield return null;
                }

                EditorUtility.DisplayProgressBar("Installing Universal", "Updating scene to Universal", 0.75f);
                m_waitTimer2 -= Time.deltaTime;
                if (m_waitTimer2 < 0)
                {
                    ConfigureSceneToURP(profile);
                    profile.m_pipelineSwitchUpdates = false;
                    EditorUtility.ClearProgressBar();
                }
                else
                {
                    yield return null;
                }
            }
        }
        /// <summary>
        /// Save the assets and marks scene as dirty
        /// </summary>
        /// <param name="saveAlso"></param>
        private static void MarkSceneDirty(bool saveAlso)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            if (saveAlso)
            {
                if (EditorSceneManager.GetActiveScene().isDirty)
                {
                    EditorSceneManager.SaveOpenScenes();
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}