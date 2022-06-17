using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if LWPipeline
using UnityEngine.Rendering.LWRP;
#endif
using System.Collections;
using UnityEditor.SceneManagement;
using ProceduralWorlds.WaterSystem;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
using System.Collections.Generic;

namespace Gaia.Pipeline.LWRP
{
    /// <summary>
    /// Static class that handles all the LWRP setup in Gaia
    /// </summary>
    public static class GaiaLWRPPipelineUtils
    {
        public static float m_waitTimer1 = 1f;
        public static float m_waitTimer2 = 3f;

        /// <summary>
        /// Configures project for LWRP
        /// </summary>
        /// <param name="profile"></param>
        private static void ConfigureSceneToLWRP(UnityPipelineProfile profile)
        {
            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
            if (gaiaSettings.m_currentRenderer != GaiaConstants.EnvironmentRenderer.Lightweight)
            {
                Debug.LogError("Unable to configure your scene/project to LWRP as the current render inside of gaia does not equal Lightweight as it's active render pipeline. This process [GaiaLWRPPipelineUtils.ConfigureSceneToLWRP()] will now exit.");
                return;
            }

            if (profile.m_setLWPipelineProfile)
            {
                SetPipelineAsset(profile);
            }

            if (profile.m_LWAutoConfigureCamera)
            {
                ConfigureCamera();
            }

            if (profile.m_LWAutoConfigureLighting)
            {
                ConfigureLighting(gaiaSettings.m_gaiaLightingProfile);
            }

            if (profile.m_LWAutoConfigureWater)
            {
                ConfigureWater(profile, gaiaSettings);
            }

            if (profile.m_LWAutoConfigureProbes)
            {
                ConfigureReflectionProbes();
            }

            if (profile.m_LWAutoConfigureTerrain)
            {
                ConfigureTerrain(profile);
            }

            FinalizeLWRP(profile, gaiaSettings);
        }

        /// <summary>
        /// Configures scripting defines in the project
        /// </summary>
        public static void SetScriptingDefines(UnityPipelineProfile profile)
        {
            bool isChanged = false;
            string currBuildSettings = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (!currBuildSettings.Contains("LWPipeline"))
            {
                if (string.IsNullOrEmpty(currBuildSettings))
                {
                    currBuildSettings = "LWPipeline";
                }
                else
                {
                    currBuildSettings += ";LWPipeline";
                }
                isChanged = true;
            }

            if (currBuildSettings.Contains("HDPipeline"))
            {
                currBuildSettings = currBuildSettings.Replace("HDPipeline;", "");
                currBuildSettings = currBuildSettings.Replace("HDPipeline", "");
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
            if (GraphicsSettings.renderPipelineAsset == null)
            {
                GaiaPackageVersion unityVersion = GaiaPackageVersion.Unity2019_1;

                //Installation setup
                if (Application.unityVersion.Contains("2019.2"))
                {
                    unityVersion = GaiaPackageVersion.Unity2019_2;
                }
                else if (Application.unityVersion.Contains("2019.3"))
                {
                    unityVersion = GaiaPackageVersion.Unity2019_3;
                }

                UnityVersionPipelineAsset mapping = profile.m_lightweightPipelineProfiles.Find(x => x.m_unityVersion == unityVersion);
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

                GraphicsSettings.renderPipelineAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(GetAssetPath(pipelineAssetName));
            }

            profile.m_pipelineSwitchUpdates = true;
        }

        /// <summary>
        /// Configures camera to LWRP
        /// </summary>
        private static void ConfigureCamera()
        {
            Camera camera = GetCamera();
            if (camera == null)
            {
                Debug.LogWarning("[GaiaLWRPPipelineUtils.ConfigureCamera()] A camera could not be found to upgrade in your scene.");
            }
            else
            {
#if LWPipeline
                LWRPAdditionalCameraData cameraData = camera.gameObject.GetComponent<LWRPAdditionalCameraData>();
                if (cameraData == null)
                {
                    cameraData = camera.gameObject.AddComponent<LWRPAdditionalCameraData>();
                    cameraData.renderShadows = true;
                }
                else
                {
                    cameraData.renderShadows = true;
                }
#endif
            }
        }

        /// <summary>
        /// Configures lighting to LWRP
        /// </summary>
        private static void ConfigureLighting(GaiaLightingProfile profile)
        {
#if LWPipeline
            LWRPAdditionalLightData[] lightsData = Object.FindObjectsOfType<LWRPAdditionalLightData>();
            if (lightsData != null)
            {
                foreach (LWRPAdditionalLightData data in lightsData)
                {
                    if (data.gameObject.GetComponent<LWRPAdditionalLightData>() == null)
                    {
                        data.gameObject.AddComponent<LWRPAdditionalLightData>();
                    }
                }
            }
#endif

            if (profile.m_antiAliasingMode == GaiaConstants.GaiaProAntiAliasingMode.TAA)
            {
                EditorUtility.SetDirty(profile);
                profile.m_antiAliasingMode = GaiaConstants.GaiaProAntiAliasingMode.FXAA;
                Debug.Log("Antialiasing mode has been switched to FXAA. TAA in LWRP is not supported. You can change the Antialiasing mode in the Lighting profile if you wish to use a different mode.");

                SetAntiAliasingMode(profile.m_antiAliasingMode);
            }

            SetMotionBlurPostFX(false);
        }

        /// <summary>
        /// Sets the sun intensity
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="volumeProfile"></param>
        public static void SetSunSettings(Light light, GaiaLightingProfileValues profile)
        {
            if (light == null)
            {
                light = GetSunLight();
            }

            if (profile.m_useKelvin)
            {
                profile.m_lWSunColor = GaiaUtils.ExecuteKelvinColor(profile.m_kelvinValue);
            }
            if (light != null)
            {
                light.color = profile.m_lWSunColor;
                light.intensity = profile.m_lWSunIntensity;
                light.shadows = profile.m_shadowCastingMode;
                light.shadowStrength = profile.m_shadowStrength;
                light.shadowResolution = profile.m_sunShadowResolution;
            }
        }

        /// <summary>
        /// Configures water to LWRP
        /// </summary>
        /// <param name="profile"></param>
        private static void ConfigureWater(UnityPipelineProfile profile, GaiaSettings gaiaSettings)
        {
            if (gaiaSettings == null)
            {
                Debug.LogError("Gaia settings could not be found. Please make sure gaia settings is import into your project");
            }
            else
            {
                if (profile.m_underwaterHorizonMaterial != null)
                {
                    profile.m_underwaterHorizonMaterial.shader = Shader.Find(profile.m_lightweightHorizonObjectShader);
                }

                //Increasing Water mesh quality for LWRP
                gaiaSettings.m_gaiaWaterProfile.m_customMeshQuality = 8;
                if (GaiaGlobal.Instance != null)
                {
                    GaiaWater.UpdateWaterMeshQuality(GaiaGlobal.Instance.SceneProfile, GaiaGlobal.Instance.SceneProfile.m_waterPrefab);
                }

                GameObject waterObject = GameObject.Find(gaiaSettings.m_gaiaWaterProfile.m_waterPrefab.name);
                if (waterObject != null)
                {
                    Material waterMat = GaiaWater.GetGaiaOceanMaterial();
                    if (waterMat != null)
                    {
                        if (GaiaGlobal.Instance != null)
                        {
                            GaiaWater.GetProfile(0, waterMat, GaiaGlobal.Instance.SceneProfile, true, false);
                        }
                    }
                    else
                    {
                        Debug.Log("Material could not be found");
                    }
                }
            }

            PWS_WaterSystem reflection = Object.FindObjectOfType<PWS_WaterSystem>();
            if (reflection != null)
            {
                Object.DestroyImmediate(reflection);
            }
        }

        /// <summary>
        /// Configures reflections to LWRP
        /// </summary>
        private static void ConfigureReflectionProbes()
        {
            ReflectionProbe[] reflectionProbes = Object.FindObjectsOfType<ReflectionProbe>();
            if (reflectionProbes != null)
            {
                foreach(ReflectionProbe probe in reflectionProbes)
                {
                    if (probe.resolution > 512)
                    {
                        Debug.Log(probe.name + " This probes resolution is quite high and could cause performance issues in Lightweight Pipeline. Recommend lowing the resolution if you're targeting mobile platform");
                    }
                }
            }
        }

        /// <summary>
        /// Configures and setup the terrain
        /// </summary>
        /// <param name="profile"></param>
        private static void ConfigureTerrain(UnityPipelineProfile profile)
        {
            Terrain[] terrains = Terrain.activeTerrains;
            if (terrains != null)
            {
                foreach (Terrain terrain in terrains)
                {
#if !UNITY_2019_2_OR_NEWER
                    terrain.materialType = Terrain.MaterialType.Custom;
#endif
                    terrain.materialTemplate = profile.m_lightweightTerrainMaterial;
                }
            }
        }

        /// <summary>
        /// Finalizes the LWRP setup for your project 
        /// </summary>
        /// <param name="profile"></param>
        private static void FinalizeLWRP(UnityPipelineProfile profile, GaiaSettings gaiaSettings)
        {
            MarkSceneDirty(true);
            EditorUtility.SetDirty(profile);
            profile.m_activePipelineInstalled = GaiaConstants.EnvironmentRenderer.Lightweight;

            GaiaManagerEditor manager = EditorWindow.GetWindow<Gaia.GaiaManagerEditor>(false, "Gaia Manager");
            if (manager != null)
            {
                manager.GaiaManagerStatusCheck(true);
            }
        }

        /// <summary>
        /// Cleans up LWRP components in the scene
        /// </summary>
        public static void CleanUpLWRP(UnityPipelineProfile profile, GaiaSettings gaiaSettings)
        {
#if LWPipeline
            LWRPAdditionalCameraData[] camerasData = Object.FindObjectsOfType<LWRPAdditionalCameraData>();
            if (camerasData != null)
            {
                foreach (LWRPAdditionalCameraData data in camerasData)
                {
                    Object.DestroyImmediate(data);
                }
            }

            LWRPAdditionalLightData[] lightsData = Object.FindObjectsOfType<LWRPAdditionalLightData>();
            if (lightsData != null)
            {
                foreach (LWRPAdditionalLightData data in lightsData)
                {
                    Object.DestroyImmediate(data);
                }
            }
#endif

            if (profile.m_underwaterHorizonMaterial != null)
            {
                profile.m_underwaterHorizonMaterial.shader = Shader.Find(profile.m_builtInHorizonObjectShader);
            }

            //reverting default water mesh quality
            gaiaSettings.m_gaiaWaterProfile.m_customMeshQuality = 2;
            if (GaiaGlobal.Instance != null)
            {
                GaiaWater.UpdateWaterMeshQuality(GaiaGlobal.Instance.SceneProfile, gaiaSettings.m_gaiaWaterProfile.m_waterPrefab);
            }

            GameObject waterPrefab = GameObject.Find(gaiaSettings.m_gaiaWaterProfile.m_waterPrefab.name);
            if (waterPrefab != null)
            {
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

            GameObject LWRPReflections = GameObject.Find("LWRP Water Reflection Probe");
            if (LWRPReflections != null)
            {
                Object.DestroyImmediate(LWRPReflections);
            }

            GraphicsSettings.renderPipelineAsset = null;

            if (GaiaGlobal.Instance != null)
            {
                GaiaUtils.GetRuntimeSceneObject();
                GaiaLighting.GetProfile(GaiaGlobal.Instance.SceneProfile, gaiaSettings.m_pipelineProfile, GaiaConstants.EnvironmentRenderer.BuiltIn);
            }

            if (waterPrefab != null)
            {
                Material waterMat = GaiaWater.GetGaiaOceanMaterial();
                if (waterMat != null)
                {
                    if (GaiaGlobal.Instance != null)
                    {
                        GaiaWater.GetProfile(0, waterMat, GaiaGlobal.Instance.SceneProfile, true, false);
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
        /// Enables or disables motion blur
        /// </summary>
        /// <param name="enabled"></param>
        private static void SetMotionBlurPostFX(bool enabled)
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

        /// <summary>
        /// Sets the AA mode ont he camera
        /// </summary>
        /// <param name="antiAliasingMode"></param>
        private static void SetAntiAliasingMode(GaiaConstants.GaiaProAntiAliasingMode antiAliasingMode)
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

        /// <summary>
        /// Gets and returns the main camera in the scene
        /// </summary>
        /// <returns></returns>
        private static Camera GetCamera()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                return camera;
            }

            camera = Object.FindObjectOfType<Camera>();
            if (camera != null)
            {
                return camera;
            }

            return null;
        }

        /// <summary>
        /// Gets and returns the sun light in the scene
        /// </summary>
        /// <returns></returns>
        private static Light GetSunLight()
        {
            Light light = null;
            GameObject lightObject = GameObject.Find("Directional Light");
            if (lightObject != null)
            {
                light = Object.FindObjectOfType<Light>();
                if (light != null)
                {
                    if (light.type == LightType.Directional)
                    {
                        return light;
                    }
                }
            }

            Light[] lights = Object.FindObjectsOfType<Light>();
            foreach(Light activeLight in lights)
            {
                if (activeLight.type == LightType.Directional)
                {
                    return activeLight;
                }
            }

            return null;
        }

        /// <summary>
        /// Starts the LWRP Setup
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public static IEnumerator StartLWRPSetup(UnityPipelineProfile profile)
        {
            if (profile == null)
            {
                Debug.LogError("UnityPipelineProfile is empty");
                yield return null;
            }
            else
            {
                EditorUtility.DisplayProgressBar("Installing LightWeight", "Updating scripting defines", 0.5f);
                m_waitTimer1 -= Time.deltaTime;
                if (m_waitTimer1 < 0)
                {
                    SetScriptingDefines(profile);
                }
                else
                {
                    yield return null;
                }

                while (EditorApplication.isCompiling == true)
                {
                    yield return null;
                }

                EditorUtility.DisplayProgressBar("Installing LightWeight", "Updating scene to LightWeight", 0.75f);
                m_waitTimer2 -= Time.deltaTime;
                if (m_waitTimer2 < 0)
                {
                    ConfigureSceneToLWRP(profile);
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
        /// Get the asset path of the first thing that matches the name
        /// </summary>
        /// <param name="name">Name to search for</param>
        /// <returns>The path or null</returns>
        private static string GetAssetPath(string name)
        {
            string[] assets = AssetDatabase.FindAssets(name, null);
            if (assets.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(assets[0]);
            }
            return null;
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