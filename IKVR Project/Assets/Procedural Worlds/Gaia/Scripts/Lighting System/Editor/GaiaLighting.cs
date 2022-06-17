using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
using UnityEditor.SceneManagement;
using Gaia.Pipeline;
using Gaia.Pipeline.HDRP;
using Gaia.Pipeline.LWRP;
using Gaia.Pipeline.URP;
using UnityEngine.Rendering;
using System.IO;
using ProceduralWorlds.WaterSystem;
using Object = UnityEngine.Object;
#if UPPipeline
using UnityEngine.Rendering.Universal;
#endif
#if GENA_PRO
using GeNa.Core;  
#endif


namespace Gaia
{
    public static class GaiaLighting
    {
        #region Variables

        //Lighting profiles
        //private static List<GaiaLightingProfileValues> m_lightingProfiles;
        private static SceneProfile m_lightingProfile;

        //Sun Values
        private static GameObject m_sunObject;
        private static Light m_sunLight;

        //Camera Values
        private static GameObject m_mainCamera;

        //Parent Object Values
        private static GameObject m_parentObject;

        //Post Processing Values
#if UNITY_POST_PROCESSING_STACK_V2
        private static PostProcessLayer m_processLayer;
        private static PostProcessVolume m_processVolume;
#endif

#if GAIA_PRO_PRESENT
        //Ambient Audio Values
        private static GaiaAudioManager m_gaiaAudioManager;
#endif

        //Stores gaia settings
        private static GaiaSettings m_gaiaSettings;

        //Where saved settings are kept
        private static GaiaGlobal m_savedSettings;

#if UPPipeline

        private static UniversalRenderPipelineAsset URPipelineAsset;

#endif

        #endregion

        #region Setup

        /// <summary>
        /// Starts the setup process for selected lighting
        /// </summary>
        /// <param name="typeOfDay"></param>
        /// <param name="renderPipeline"></param>
        public static void GetProfile(SceneProfile lightProfile, UnityPipelineProfile pipelineProfile, GaiaConstants.EnvironmentRenderer renderPipeline, bool applyProfile = false)
        {
            try
            {
                m_lightingProfile = lightProfile;
                if (m_lightingProfile == null)
                {
                    Debug.LogError("[GaiaLighting.GetProfile()] Asset 'Scene Profile' could not be found please make sure it exists withiny our project or that the name has not been changed. Due to this error the method will now exit.");
                }
                else
                {
                    m_parentObject = GetOrCreateParentObject(GaiaConstants.gaiaLightingObject, true);
                    bool wasSuccessfull = false;
                    foreach (GaiaLightingProfileValues profile in m_lightingProfile.m_lightingProfiles)
                    {
                        if (profile.m_typeOfLighting == lightProfile.m_lightingProfiles[lightProfile.m_selectedLightingProfileValuesIndex].m_typeOfLighting)
                        {
                            if (renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
                            {
                                GaiaHDRPPipelineUtils.UpdateSceneLighting(profile, pipelineProfile, lightProfile, applyProfile);
                            }
                            else
                            {
                                UpdateGlobalLighting(lightProfile, profile, renderPipeline);
                            }

                            wasSuccessfull = true;
                            break;
                        }
                    }
                    if (!wasSuccessfull)
                    {
                        Debug.LogError("Selected Profile Index" + lightProfile.m_selectedLightingProfileValuesIndex.ToString());
                        Debug.LogError("[GaiaLighting.GetProfile()] No profile type matches one you have selected. Have you modified GaiaConstants.GaiaLightingProfileType?");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Setting up the lighting profile had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Starts the setup process for selected lighting
        /// </summary>
        /// <param name="typeOfDay"></param>
        /// <param name="renderPipeline"></param>
        public static void GetProfile(GaiaLightingProfile lightProfile, UnityPipelineProfile pipelineProfile, GaiaConstants.EnvironmentRenderer renderPipeline, bool applyProfile = false)
        {
            try
            {
                SceneProfile sceneProfile = ScriptableObject.CreateInstance<SceneProfile>();
                GaiaSceneManagement.CopySettingsTo(lightProfile, sceneProfile);
                m_lightingProfile = sceneProfile;
                if (m_lightingProfile == null)
                {
                    Debug.LogError("[GaiaLighting.GetProfile()] Asset 'Scene Profile' could not be found please make sure it exists withiny our project or that the name has not been changed. Due to this error the method will now exit.");
                }
                else
                {
                    m_parentObject = GetOrCreateParentObject(GaiaConstants.gaiaLightingObject, true);
                    bool wasSuccessfull = false;
                    foreach (GaiaLightingProfileValues profile in m_lightingProfile.m_lightingProfiles)
                    {
                        if (profile.m_typeOfLighting == lightProfile.m_lightingProfiles[lightProfile.m_selectedLightingProfileValuesIndex].m_typeOfLighting)
                        {
                            if (renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
                            {
                                GaiaHDRPPipelineUtils.UpdateSceneLighting(profile, pipelineProfile, sceneProfile, applyProfile);
                            }
                            else
                            {
                                UpdateGlobalLighting(sceneProfile, profile, renderPipeline);
                            }

                            wasSuccessfull = true;
                            break;
                        }
                    }
                    if (!wasSuccessfull)
                    {
                        Debug.LogError("[GaiaLighting.GetProfile()] No profile type matches one you haev selected. Have you modified GaiaConstants.GaiaLightingProfileType?");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Setting up the lighting profile had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }

        #endregion

        #region Apply Settings

        /// <summary>
        /// Updates the global lighting settings in your scene
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="renderPipeline"></param>
        private static void UpdateGlobalLighting(SceneProfile lightProfile, GaiaLightingProfileValues profile, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            try
            {
                //Removes the old content from the scene
                RemoveOldLighting();

                if (profile.m_profileType != GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                {
                    //Applies sun settings
                    ApplySunSettings(profile, renderPipeline);
                    //Applies the ambient light
                    ApplyAmbientLighting(profile);
                    //Applies the fog settings
                    ApplyFogSettings(profile);
                }

                //Applies the Skybox
                SetSkyboxSettings(profile);
                //Sets the lightmapping settings
                SetLightmappingSettings(lightProfile);
                //Sets the shadow settings
                SetShadowSettings(profile);
                //Applies the scene post processing
                SetupPostProcessing(profile, lightProfile, renderPipeline);
                //Sets the ambient audio 
                SetupAmbientAudio(profile);
                //Sets up the global reflection probe in the scene
                NewGlobalReflectionProbe(lightProfile);
                ///Applies LOD Bias
                ApplyLODBias(lightProfile);
                //Sets the hdri in the clouds system
                SetGlobalWeather(profile, lightProfile, renderPipeline);
                //Update GeNa light sync validation
                UpdateGeNaLightSync();

                SetupPhysicalCameraLens(lightProfile);

                if (lightProfile.m_lightingEditSettings)
                {
                    lightProfile.m_isUserProfileSet = true;
                }
                else
                {
                    lightProfile.m_isUserProfileSet = profile.m_userCustomProfile;
                }

                UpdateAmbientEnvironment();

                ApplyWaterShaderUpdates(PWS_WaterSystem.Instance, lightProfile);

                SetupWind();

                UpdateWeatherBools();

                //Marks the scene as dirty
                MarkSceneDirty(false);
            }
            catch (Exception e)
            {
                Debug.LogError("Updating the global lighting had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Applies the water shader updates to apply ambient light and the sun light recaculations
        /// </summary>
        /// <param name="system"></param>
        private static void ApplyWaterShaderUpdates(PWS_WaterSystem system, SceneProfile profile)
        {
            if (system == null || profile == null)
            {
                return;
            }

            system.UpdateShaderValues(system.m_waterProfileValues);
            GaiaWater.SetReflectionTexture(profile);
        }
        /// <summary>
        /// Configures the physical camera setup
        /// </summary>
        /// <param name="lightingProfile"></param>
        private static void SetupPhysicalCameraLens(SceneProfile lightingProfile)
        {
            try
            {
                Camera camera = GaiaUtils.GetCamera();
                if (camera != null)
                {
                    camera.usePhysicalProperties = lightingProfile.m_usePhysicalCamera;
                    camera.focalLength = lightingProfile.m_cameraFocalLength;
                    camera.sensorSize = lightingProfile.m_cameraSensorSize;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Setting up physical camera settings had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets up the wind manager
        /// </summary>
        public static void SetupWind()
        {
            if (m_gaiaSettings == null)
            {
                m_gaiaSettings = GaiaUtils.GetGaiaSettings();
            }

            if (m_gaiaSettings != null)
            {
                if (m_gaiaSettings.m_createWind)
                {
                    GameObject windObject = GameObject.Find("PW Wind Zone");
                    if (windObject == null)
                    {
                        windObject = AssetDatabase.LoadAssetAtPath<GameObject>(GaiaUtils.GetAssetPath("PW Wind Zone.prefab"));
                        if (windObject != null)
                        {
                            windObject = PrefabUtility.InstantiatePrefab(windObject) as GameObject;
                        }
                        if (windObject == null)
                        {
                            windObject = new GameObject("PW Wind Zone");
                        }
                    }
                    windObject.transform.SetParent(GaiaUtils.GetRuntimeSceneObject().transform);

                    WindZone windZone = windObject.GetComponent<WindZone>();
                    if (windZone == null)
                    {
                        windZone = windObject.AddComponent<WindZone>();
                    }

                    WindManager windManager = windObject.GetComponent<WindManager>();
                    if (windManager == null)
                    {
                        windManager = windObject.AddComponent<WindManager>();
                    }

                    if (windManager.m_windAudioClip == null)
                    {
                        windManager.m_windAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(GaiaUtils.GetAssetPath("Gaia Ambient Wind.mp3"));
                    }
                    windManager.InstantWindApply();
                }
            }
        }
        /// <summary>
        /// Apply sun settings
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="renderPipeline"></param>
        private static void ApplySunSettings(GaiaLightingProfileValues profile, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            try
            {
                if (m_sunObject == null || m_sunLight == null)
                {
                    //Get the sun object and sun light
                    m_sunLight = GaiaUtils.GetMainDirectionalLight();
                    if (m_sunLight != null)
                    {
                        m_sunObject = m_sunLight.gameObject;
                    }
                }
                else
                {
                    if (!m_sunObject.activeInHierarchy)
                    {
                        //Get the sun object and sun light
                        m_sunLight = GaiaUtils.GetMainDirectionalLight();
                        if (m_sunLight != null)
                        {
                            m_sunObject = m_sunLight.gameObject;
                        }
                    }
                }

                //Rotates the sun to specified values
                RotateSun(profile.m_sunRotation, profile.m_sunPitch);
                if (profile.m_useKelvin)
                {
                    profile.m_sunColor = GaiaUtils.ExecuteKelvinColor(profile.m_kelvinValue);
                }

                switch (renderPipeline)
                {
                    case GaiaConstants.EnvironmentRenderer.BuiltIn:
                        m_sunLight.color = profile.m_sunColor;
                        m_sunLight.intensity = profile.m_sunIntensity;
                        m_sunLight.shadows = profile.m_shadowCastingMode;
                        m_sunLight.shadowStrength = profile.m_shadowStrength;
                        m_sunLight.shadowResolution = profile.m_sunShadowResolution;
                        break;
                    case GaiaConstants.EnvironmentRenderer.Universal:
                        GaiaLWRPPipelineUtils.SetSunSettings(m_sunLight, profile);
                        break;
                }

                RenderSettings.sun = m_sunLight;
                EditorUtility.SetDirty(RenderSettings.sun);
            }
            catch (Exception e)
            {
                Debug.LogError("Applying sun settings had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Set the suns rotation and pitch
        /// </summary>
        /// <param name="rotation"></param>
        /// <param name="pitch"></param>
        private static void RotateSun(float rotation, float pitch)
        {
            try
            {
                //Set new directional light rotation
                if (m_sunObject != null)
                {
                    m_sunObject.transform.localEulerAngles = new Vector3(pitch, rotation, 0f);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Roating the sun had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets up the skybox
        /// </summary>
        /// <param name="profile"></param>
        private static void SetSkyboxSettings(GaiaLightingProfileValues profile)
        {
            try
            {
                Material material = m_lightingProfile.m_masterSkyboxMaterial;
                if (material == null)
                {
                    material = AssetDatabase.LoadAssetAtPath<Material>(GaiaUtils.GetAssetPath("Gaia Sky.mat"));
                    if (material != null)
                    {
                        m_lightingProfile.m_masterSkyboxMaterial = material;
                    }
                }

                if (material != null)
                {
                    switch (profile.m_profileType)
                    {
                        case GaiaConstants.GaiaLightingProfileType.HDRI:
                            SetupHDRISky(profile, material);
                            break;
                        case GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky:
                            SetupPWSky(profile, material);
                            break;
                        case GaiaConstants.GaiaLightingProfileType.Procedural:
                            SetupProceduralSky(profile, material);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Setting skybox settings had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets up HDRI
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="masterMaterial"></param>
        private static void SetupHDRISky(GaiaLightingProfileValues profile, Material masterMaterial)
        {
            try
            {
                if (profile == null || masterMaterial == null)
                {
                    return;
                }

                bool rebuild = false;
                Material currentMat = RenderSettings.skybox;
                if (currentMat == null)
                {
                    rebuild = true;
                }

                if (currentMat != null)
                {
                    if (currentMat.shader != Shader.Find(GaiaShaderID.m_unitySkyboxShaderHDRI))
                    {
                        rebuild = true;
                    }
                }

                if (rebuild)
                {
                    Material instancedMaterial = new Material(Shader.Find(GaiaShaderID.m_unitySkyboxShaderHDRI));
                    if (instancedMaterial.shader != Shader.Find(GaiaShaderID.m_unitySkyboxShaderHDRI))
                    {
                        instancedMaterial.shader = Shader.Find(GaiaShaderID.m_unitySkyboxShaderHDRI);
                    }

                    currentMat = instancedMaterial;
                }

                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_unitySkyboxCubemap))
                {
                    currentMat.SetTexture(GaiaShaderID.m_unitySkyboxCubemap, profile.m_skyboxHDRI);
                }
                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_unitySkyboxRotation))
                {
                    currentMat.SetFloat(GaiaShaderID.m_unitySkyboxRotation, -profile.m_sunRotation - profile.m_skyboxRotationOffset);
                }
                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_unitySkyboxTintHDRI))
                {
                    currentMat.SetColor(GaiaShaderID.m_unitySkyboxTintHDRI, profile.m_skyboxTint);
                }
                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_unitySkyboxExposure))
                {
                    currentMat.SetFloat(GaiaShaderID.m_unitySkyboxExposure, profile.m_skyboxExposure);
                }
                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_pwSkyFogHeight))
                {
                    currentMat.SetFloat(GaiaShaderID.m_pwSkyFogHeight, profile.m_skyboxFogHeight);
                }
                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_pwSkyFogGradient))
                {
                    currentMat.SetFloat(GaiaShaderID.m_pwSkyFogGradient, profile.m_skyboxFogGradient);
                }

                RenderSettings.skybox = currentMat;
            }
            catch (Exception e)
            {
                Debug.LogError("Setting up HDRI had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets up Procedural
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="masterMaterial"></param>
        private static void SetupProceduralSky(GaiaLightingProfileValues profile, Material masterMaterial)
        {
            try
            {
                if (profile == null || masterMaterial == null)
                {
                    return;
                }

                if (profile.m_skyboxHDRI == null && profile.m_profileType != GaiaConstants.GaiaLightingProfileType.HDRI)
                {
                    Debug.LogError("[GaiaLighting.SetSkyboxHDRI()] HDRI map is missing. Go to Gaia Lighting System Profile and add one");
                    return;
                }

                bool rebuild = false;
                Material currentMat = RenderSettings.skybox;
                if (currentMat == null)
                {
                    rebuild = true;
                }

                if (currentMat != null)
                {
                    if (currentMat.shader != Shader.Find(GaiaShaderID.m_unitySkyboxShader))
                    {
                        rebuild = true;
                    }
                }

                if (rebuild)
                {
                    Material instancedMaterial = new Material(Shader.Find(GaiaShaderID.m_unitySkyboxShader));
                    if (instancedMaterial.shader != Shader.Find(GaiaShaderID.m_unitySkyboxShader))
                    {
                        instancedMaterial.shader = Shader.Find(GaiaShaderID.m_unitySkyboxShader);
                    }

                    currentMat = instancedMaterial;
                }

                currentMat.shader = Shader.Find(GaiaShaderID.m_unitySkyboxShader);
                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_unitySkyboxSunDisk))
                {
                    currentMat.SetInt(GaiaShaderID.m_unitySkyboxSunDisk, (int)GaiaConstants.ProceduralSkySunTypes.HighQuality);
                }
                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_unitySunQualityKeyword))
                {
                    currentMat.EnableKeyword(GaiaShaderID.m_unitySunQualityKeyword);
                    List<string> keywords = new List<string>
                    {
                        GaiaShaderID.m_unitySunQualityKeyword
                    };
                    currentMat.shaderKeywords = keywords.ToArray();
                }
                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_unitySkyboxTint))
                {
                    currentMat.SetColor(GaiaShaderID.m_unitySkyboxTint, profile.m_skyboxTint);
                }
                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_unitySkyboxExposure))
                {
                    currentMat.SetFloat(GaiaShaderID.m_unitySkyboxExposure, profile.m_skyboxExposure);
                }
                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_unitySkyboxSunSize))
                {
                    currentMat.SetFloat(GaiaShaderID.m_unitySkyboxSunSize, profile.m_sunSize);
                }
                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_unitySkyboxSunSizeConvergence))
                {
                    currentMat.SetFloat(GaiaShaderID.m_unitySkyboxSunSizeConvergence, profile.m_sunConvergence);
                }
                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_unitySkyboxAtmosphereThickness))
                {
                    currentMat.SetFloat(GaiaShaderID.m_unitySkyboxAtmosphereThickness, profile.m_atmosphereThickness);
                }
                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_unitySkyboxGroundColor))
                {
                    currentMat.SetColor(GaiaShaderID.m_unitySkyboxGroundColor, profile.m_groundColor);
                }
                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_pwSkyFogHeight))
                {
                    currentMat.SetFloat(GaiaShaderID.m_pwSkyFogHeight, profile.m_skyboxFogHeight);
                }
                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_pwSkyFogGradient))
                {
                    currentMat.SetFloat(GaiaShaderID.m_pwSkyFogGradient, profile.m_skyboxFogGradient);
                }

                RenderSettings.skybox = currentMat;
            }
            catch (Exception e)
            {
                Debug.LogError("Setting up procedural had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets up PW Sky
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="masterMaterial"></param>
        private static void SetupPWSky(GaiaLightingProfileValues profile, Material masterMaterial)
        {
            try
            {
                bool rebuild = false;
                Material currentMat = RenderSettings.skybox;
                if (currentMat == null)
                {
                    rebuild = true;
                }

                if (currentMat != null)
                {
                    if (currentMat.shader != Shader.Find(GaiaShaderID.m_pwSkySkyboxShader))
                    {
                        rebuild = true;
                    }
                }

                if (rebuild)
                {
                    Material instancedMaterial = new Material(Shader.Find(GaiaShaderID.m_pwSkySkyboxShader));
                    if (instancedMaterial.shader != Shader.Find(GaiaShaderID.m_pwSkySkyboxShader))
                    {
                        instancedMaterial.shader = Shader.Find(GaiaShaderID.m_pwSkySkyboxShader);
                    }

                    currentMat = instancedMaterial;
                }

                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_unitySkyboxSunDisk))
                {
                    currentMat.SetInt(GaiaShaderID.m_unitySkyboxSunDisk, (int)GaiaConstants.ProceduralSkySunTypes.HighQuality);
                }
                if (GaiaUtils.ValidateShaderProperty(currentMat, GaiaShaderID.m_unitySkyboxSunDisk))
                {
                    currentMat.EnableKeyword(GaiaShaderID.m_unitySunQualityKeyword);
                    List<string> keywords = new List<string>
                    {
                        GaiaShaderID.m_unitySunQualityKeyword
                    };
                    currentMat.shaderKeywords = keywords.ToArray();
                }

                RenderSettings.skybox = currentMat;
            }
            catch (Exception e)
            {
                Debug.LogError("Settings up PW Sky had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Updates the weather present bools in systems in the scene
        /// </summary>
        public static void UpdateWeatherBools()
        {
#if GAIA_PRO_PRESENT
            if (PWS_WaterSystem.Instance != null)
            {
                PWS_WaterSystem.Instance.m_weatherSystemPresent = ProceduralWorldsGlobalWeather.Instance;
            }

            if (GaiaGlobal.Instance != null)
            {
                GaiaGlobal.Instance.WeatherPresent = ProceduralWorldsGlobalWeather.Instance;
            }

            if (PW_VFX_Atmosphere.Instance != null)
            {
                PW_VFX_Atmosphere.Instance.m_weatherManagerExists = ProceduralWorldsGlobalWeather.Instance;
            }

            if (GaiaUnderwaterEffects.Instance != null)
            {
                GaiaUnderwaterEffects.Instance.m_weatherSystemExists = ProceduralWorldsGlobalWeather.Instance;
            }
#endif
        }
        /// <summary>
        /// Updates the global weather
        /// </summary>
        /// <param name="profile"></param>
        public static void SetGlobalWeather(GaiaLightingProfileValues profile, SceneProfile lightingProfile, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            try
            {
#if GAIA_PRO_PRESENT
                if (profile.m_profileType == GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                {
#if !GAIA_EXPERIMENTAL
                    if (renderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
                    {
                        ProceduralWorldsGlobalWeather pwGlobal = GameObject.FindObjectOfType<ProceduralWorldsGlobalWeather>();
                        if (pwGlobal == null)
                        {
                            pwGlobal = ProceduralWorldsGlobalWeather.AddGlobalWeather(GaiaConstants.GaiaGlobalWindType.Custom, profile);
                        }
                        if (pwGlobal != null)
                        {
#if UNITY_POST_PROCESSING_STACK_V2
                            pwGlobal.SetPostProcessingProfile(profile.PostProcessProfileBuiltIn, profile.m_directToCamera);
#endif
                            pwGlobal.m_enableAutoDOF = lightingProfile.m_enableAutoDOF;
                            pwGlobal.m_depthOfFieldDetectionLayers = lightingProfile.m_dofLayerDetection;
                            SetLightingGlobalWeather(profile, renderPipeline);
                            pwGlobal.UpdateLightShadows(profile);
                        }

                        PW_VFX_Clouds clouds = GameObject.FindObjectOfType<PW_VFX_Clouds>();
                        if (clouds != null)
                        {
                            clouds.PW_Clouds_HDRI = profile.m_skyboxHDRI;
                        }
                        if (PWS_WaterSystem.Instance != null)
                        {
                            PWS_WaterSystem.Instance.m_weatherSystemPresent = true;
                        }
                        if (PW_VFX_Atmosphere.Instance != null)
                        {
                            PW_VFX_Atmosphere.Instance.SetCloudShaderSettings(profile, GaiaShaderID.m_cloudShaderName, renderPipeline);
                            PW_VFX_Atmosphere.Instance.UpdateSunRotationY(profile.m_pwSkySunRotation);
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Not Yet Supported", "Procedural Worlds Sky is not yet available in HDRP, this will be available soon. The default light profile will be set by default.", "Ok");
                        ProceduralWorldsGlobalWeather.RemoveGlobalWindShader();
                    }
#else
                    ProceduralWorldsGlobalWeather pwGlobal = GameObject.FindObjectOfType<ProceduralWorldsGlobalWeather>();
                    if (pwGlobal == null)
                    {
                        pwGlobal = ProceduralWorldsGlobalWeather.AddGlobalWeather(GaiaConstants.GaiaGlobalWindType.Custom, profile);
                    }
                    if (pwGlobal != null)
                    {
#if UNITY_POST_PROCESSING_STACK_V2
                            pwGlobal.SetPostProcessingProfile(profile.PostProcessProfileBuiltIn, profile.m_directToCamera);
#endif
                        pwGlobal.m_enableAutoDOF = lightingProfile.m_enableAutoDOF;
                        pwGlobal.m_depthOfFieldDetectionLayers = lightingProfile.m_dofLayerDetection;
                        SetLightingGlobalWeather(profile, renderPipeline);
                        pwGlobal.UpdateLightShadows(profile);
                    }

                    PW_VFX_Clouds clouds = GameObject.FindObjectOfType<PW_VFX_Clouds>();
                    if (clouds != null)
                    {
                        clouds.PW_Clouds_HDRI = profile.m_skyboxHDRI;
                    }
                    if (PWS_WaterSystem.Instance != null)
                    {
                        PWS_WaterSystem.Instance.m_weatherSystemPresent = true;
                    }
                    if (PW_VFX_Atmosphere.Instance != null)
                    {
                        PW_VFX_Atmosphere.Instance.SetCloudShaderSettings(profile, GaiaShaderID.m_cloudShaderName, renderPipeline);
                        PW_VFX_Atmosphere.Instance.UpdateSunRotationY(profile.m_pwSkySunRotation);
                    }
#endif
                }
                else
                {
                    ProceduralWorldsGlobalWeather.RemoveGlobalWindShader();
                    SetDefaultGlobalShader();

                    if (PWS_WaterSystem.Instance != null)
                    {
                        PWS_WaterSystem.Instance.m_weatherSystemPresent = false;
                    }
                }

                GaiaGlobal.CheckWeatherPresent(true);
#endif
            }
            catch (Exception e)
            {
                Debug.LogError("Setting up PW Sky weather components had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Updates settings when global weather is enabled
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="renderPipeline"></param>
        private static void SetLightingGlobalWeather(GaiaLightingProfileValues profile, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            try
            {
                //Sun
                if (m_sunObject == null || m_sunLight == null)
                {
                    //Get the sun object and sun light
                    m_sunLight = GaiaUtils.GetMainDirectionalLight();
                    if (m_sunLight != null)
                    {
                        m_sunObject = m_sunLight.gameObject;
                    }
                }
                if (renderPipeline == GaiaConstants.EnvironmentRenderer.BuiltIn)
                {
                    m_sunLight.shadows = profile.m_shadowCastingMode;
                    m_sunLight.shadowResolution = profile.m_sunShadowResolution;

                    Light moonLight = GaiaUtils.GetMainMoonLight();
                    if (moonLight != null)
                    {
                        moonLight.shadows = profile.m_shadowCastingMode;
                        moonLight.shadowResolution = profile.m_sunShadowResolution;
                    }
                }

                //Fog
                RenderSettings.fogMode = profile.m_fogMode;
            }
            catch (Exception e)
            {
                Debug.LogError("Settings up PW Sky sun and moon settings had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// sets the ambient lighting
        /// </summary>
        /// <param name="profile"></param>
        private static void ApplyAmbientLighting(GaiaLightingProfileValues profile)
        {
            RenderSettings.ambientMode = profile.m_ambientMode;
            RenderSettings.ambientIntensity = profile.m_ambientIntensity;
            RenderSettings.ambientSkyColor = profile.m_skyAmbient;
            RenderSettings.ambientEquatorColor = profile.m_equatorAmbient;
            RenderSettings.ambientGroundColor = profile.m_groundAmbient;
        }
        /// <summary>
        /// Sets up and applies the fog in your scene
        /// </summary>
        /// <param name="profile"></param>
        private static void ApplyFogSettings(GaiaLightingProfileValues profile)
        {
            RenderSettings.fog = m_lightingProfile.m_enableFog;
            RenderSettings.fogMode = profile.m_fogMode;
            RenderSettings.fogColor = profile.m_fogColor;
            RenderSettings.fogDensity = profile.m_fogDensity;
            RenderSettings.fogStartDistance = profile.m_fogStartDistance;
            RenderSettings.fogEndDistance = profile.m_fogEndDistance;
        }
        /// <summary>
        /// Sets up the LOD Bias
        /// </summary>
        /// <param name="profile"></param>
        private static void ApplyLODBias(SceneProfile profile)
        {
            QualitySettings.lodBias = profile.m_lodBias;
        }
        /// <summary>
        /// Sets the lightmap settings
        /// </summary>
        /// <param name="profile"></param>
        private static void SetLightmappingSettings(SceneProfile profile)
        {
            try
            {
#if UNITY_2020_1_OR_NEWER
                CreateLightingSettingsAsset();
                Lightmapping.lightingSettings.lightmapper = profile.m_lightmappingMode;
#else
                LightmapEditorSettings.lightmapper = profile.m_lightmappingMode;
#endif
            }
            catch (Exception e)
            {
                Debug.LogError("Setting up lightmapping settings had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets the shadow distance
        /// </summary>
        /// <param name="profile"></param>
        private static void SetShadowSettings(GaiaLightingProfileValues profile)
        {
            QualitySettings.shadowDistance = profile.m_shadowDistance;

#if UPPipeline

            GaiaURPRuntimeUtils.SetShadowDistance(GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset, profile);

#endif
        }
        /// <summary>
        /// Sets up the post processing in the scene
        /// </summary>
        /// <param name="profile"></param>
        public static void SetupPostProcessing(GaiaLightingProfileValues profile, SceneProfile lightProfile, GaiaConstants.EnvironmentRenderer renderPipeline, bool setupLayerOnly = false)
        {
            try
            {
                m_lightingProfile = lightProfile;
                if (m_lightingProfile == null || profile == null)
                {
                    return;
                }

                if (m_parentObject == null)
                {
                    m_parentObject = GetOrCreateParentObject(GaiaConstants.gaiaLightingObject, true);
                }

                if (renderPipeline == GaiaConstants.EnvironmentRenderer.BuiltIn)
                {
#if !UNITY_POST_PROCESSING_STACK_V2
                    Debug.Log("[GaiaLighting.SetupPostProcessing()] Post Processing was not found. Please install post processing from the package manager to allow post processing to be setup");
#else
                    if (m_lightingProfile.m_enablePostProcessing)
                    {
                        if (m_mainCamera == null)
                        {
                            m_mainCamera = GaiaUtils.GetCamera().gameObject;
                        }

                        m_processLayer = m_mainCamera.GetComponent<PostProcessLayer>();
                        if (m_processLayer == null)
                        {
                            m_processLayer = m_mainCamera.AddComponent<PostProcessLayer>();
                        }

                        m_processLayer.volumeLayer = 2;
                        m_processLayer.finalBlitToCameraTarget = profile.m_directToCamera;
                        //Sets up antialiasing
                        ConfigureAntiAliasing(lightProfile, renderPipeline);

                        if (profile.PostProcessProfileBuiltIn != null)
                        {
                            if (!setupLayerOnly)
                            {
                                if (m_processVolume == null)
                                {
                                    m_processVolume = GetOrCreatePostProcessVolume();
                                }

                                m_processVolume.sharedProfile = profile.PostProcessProfileBuiltIn;
                                m_processVolume.gameObject.layer = LayerMask.NameToLayer("TransparentFX");

                                if (profile.PostProcessProfileBuiltIn.TryGetSettings(out AutoExposure autoExposure))
                                {
                                    autoExposure.active = true;
                                    autoExposure.enabled.value = true;
                                    autoExposure.keyValue.value = profile.m_postProcessExposure;
                                }

                                if (m_processVolume != null)
                                {
                                    if (m_lightingProfile.m_parentObjects)
                                    {
                                        m_processVolume.gameObject.transform.SetParent(m_parentObject.transform);
                                    }
                                    if (m_lightingProfile.m_hideProcessVolume)
                                    {
                                        UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(m_processVolume, false);
                                    }
                                    else
                                    {
                                        UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(m_processVolume, true);
                                    }
                                }
                            }

                            CheckPostFXV2ColorFilter(profile, Color.black);
                            GaiaSceneManagement.SetupAutoDepthOfField(lightProfile);
                        }
                    }
                    else
                    {
                        RemovePostProcessing();
                        lightProfile.m_enableAutoDOF = false;
                        GaiaSceneManagement.SetupAutoDepthOfField(lightProfile);
                    }
#endif
                }
                else
                {
                    GaiaURPPipelineUtils.ApplyURPPostProcessing(profile, lightProfile);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Setting up post processing had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets up the ambient audio
        /// </summary>
        /// <param name="profile"></param>
        public static void SetupAmbientAudio(GaiaLightingProfileValues profile)
        {
            try
            {
                if (m_lightingProfile.m_enableAmbientAudio)
                {
#if GAIA_PRO_PRESENT
                    m_gaiaAudioManager = GetOrCreateAmbientAudio();

                    AudioSource audioSource = m_gaiaAudioManager.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        GameObject.DestroyImmediate(audioSource);
                    }

                    if (m_lightingProfile.m_parentObjects)
                    {
                        if (m_gaiaAudioManager != null)
                        {
                            m_gaiaAudioManager.gameObject.transform.SetParent(GaiaUtils.GetRuntimeSceneObject().transform);
                        }
                    }
#else
                    GameObject ambientAudioObject = GetOrCreateGaia2AmbientAudio();
                    AudioSource audioSource = ambientAudioObject.GetComponent<AudioSource>();
                    if (audioSource == null)
                    {
                        audioSource = ambientAudioObject.AddComponent<AudioSource>();
                    }

                    audioSource.volume = profile.m_ambientVolume;
                    audioSource.clip = profile.m_ambientAudio;
                    audioSource.maxDistance = 5000f;
                    audioSource.loop = true;

                    if (m_lightingProfile.m_parentObjects)
                    {
                        if (ambientAudioObject != null)
                        {
                            ambientAudioObject.gameObject.transform.SetParent(GaiaUtils.GetRuntimeSceneObject().transform);
                        }
                    }
#endif
                }
                else
                {
                    RemoveAmbientAudio();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Setting up ambient audio had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets up AntiAliasing on the camera
        /// </summary>
        /// <param name="antiAliasingMode"></param>
        /// <param name="profile"></param>
        public static void ConfigureAntiAliasing(SceneProfile sceneProfile, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            try
            {
                //If we have a lighting profile and PP is disabled here, we abort.
                if (m_lightingProfile != null)
                {
                    if (!m_lightingProfile.m_enablePostProcessing)
                    {
                        return;
                    }
                }
                if (m_mainCamera == null)
                {
                    m_mainCamera = GaiaUtils.GetCamera().gameObject;
                }

                if (renderPipeline == GaiaConstants.EnvironmentRenderer.BuiltIn)
                {
#if UNITY_POST_PROCESSING_STACK_V2
                    m_processLayer = m_mainCamera.GetComponent<PostProcessLayer>();
                    if (m_processLayer == null)
                    {
                        Debug.LogError("[GaiaProLighting.ConfigureAntiAliasing() Post Processing Layer could not be found on the main camera");
                    }
                    else
                    {
                        Camera camera = m_mainCamera.GetComponent<Camera>();
                        switch (sceneProfile.m_antiAliasingMode)
                        {
                            case GaiaConstants.GaiaProAntiAliasingMode.None:
                                m_processLayer.antialiasingMode = PostProcessLayer.Antialiasing.None;
                                camera.allowMSAA = false;
                                break;
                            case GaiaConstants.GaiaProAntiAliasingMode.FXAA:
                                m_processLayer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
                                camera.allowMSAA = false;
                                break;
                            case GaiaConstants.GaiaProAntiAliasingMode.MSAA:
                                m_processLayer.antialiasingMode = PostProcessLayer.Antialiasing.None;
                                camera.allowMSAA = true;
                                break;
                            case GaiaConstants.GaiaProAntiAliasingMode.SMAA:
                                m_processLayer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
                                camera.allowMSAA = false;
                                break;
                            case GaiaConstants.GaiaProAntiAliasingMode.TAA:

                                Transform playerTransform = GaiaUtils.GetPlayerTransform();
                                if (playerTransform != null && playerTransform.name == GaiaConstants.playerXRName)
                                {
                                    Debug.Log("XR Player type is in use, changing Anti-Aliasing to SMAA since TAA can cause issues in XR");
                                    m_processLayer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
                                    break;
                                }

                                if (renderPipeline == GaiaConstants.EnvironmentRenderer.Lightweight)
                                {
                                    Debug.Log("Temporal Anti Aliasing is not recommended to use in LWRP. We recommend using FXAA or SMAA");
                                }
                                else
                                {
                                    m_processLayer.antialiasingMode = PostProcessLayer.Antialiasing.TemporalAntialiasing;
                                    m_processLayer.temporalAntialiasing.jitterSpread = sceneProfile.m_AAJitterSpread;
                                    m_processLayer.temporalAntialiasing.stationaryBlending = sceneProfile.m_AAStationaryBlending;
                                    m_processLayer.temporalAntialiasing.motionBlending = sceneProfile.m_AAMotionBlending;
                                    m_processLayer.temporalAntialiasing.sharpness = sceneProfile.m_AASharpness;
                                }
                                camera.allowMSAA = false;
                                break;
                        }
                    }
#endif
                }
                else if (renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
                {
#if UPPipeline
                    UniversalAdditionalCameraData cameraData = m_mainCamera.GetComponent<UniversalAdditionalCameraData>();
                    Camera camera = m_mainCamera.GetComponent<Camera>();
                    if (cameraData != null)
                    {
                        switch (sceneProfile.m_antiAliasingMode)
                        {
                            case GaiaConstants.GaiaProAntiAliasingMode.None:
                                cameraData.antialiasing = AntialiasingMode.None;
                                break;
                            case GaiaConstants.GaiaProAntiAliasingMode.FXAA:
                                cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                                camera.allowMSAA = false;
                                break;
                            case GaiaConstants.GaiaProAntiAliasingMode.MSAA:
                                cameraData.antialiasing = AntialiasingMode.None;
                                camera.allowMSAA = true;
                                break;
                            case GaiaConstants.GaiaProAntiAliasingMode.SMAA:
                                cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                                camera.allowMSAA = false;
                                break;
                            case GaiaConstants.GaiaProAntiAliasingMode.TAA:
                                //Debug.Log("Temporal Anti Aliasing is not recommended to use in URP. We recommend using FXAA or SMAA. We will switch it to SMAA by default.");
                                cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                                if(m_lightingProfile!=null)
                                {                                
                                    m_lightingProfile.m_antiAliasingMode = GaiaConstants.GaiaProAntiAliasingMode.SMAA;
                                }
                                camera.allowMSAA = false;
                                break;
                        }
                    }
#endif
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Setting up anti aliasing had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets the post processing component status
        /// </summary>
        /// <param name="enabled"></param>
        public static void SetPostProcessingStatus(bool enabled)
        {
            try
            {
#if UNITY_POST_PROCESSING_STACK_V2
                if (m_gaiaSettings == null)
                {
                    m_gaiaSettings = GaiaUtils.GetGaiaSettings();
                }

                if (m_gaiaSettings != null)
                {
                    if (!m_gaiaSettings.m_gaiaLightingProfile.m_enablePostProcessing)
                    {
                        return;
                    }
                }

                if (!Application.isPlaying)
                {
                    PostProcessLayer layer = GameObject.FindObjectOfType<PostProcessLayer>();
                    if (layer != null)
                    {
                        if (enabled)
                        {
                            layer.fog.enabled = true;
                        }
                        else
                        {
                            layer.fog.enabled = false;
                        }
                    }
                }
#endif
            }
            catch (Exception e)
            {
                Debug.LogError("Setting post processing enable/disbale state had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }

        #endregion

        #region GX Only Functions

        /// <summary>
        /// Sets up and bakes occlusion culling
        /// </summary>
        /// <param name="occlusionCullingEnabled"></param>
        /// <param name="bakeOcclusionCulling"></param>
        /// <param name="clearBakedData"></param>
        public static void AddOcclusionCulling(bool occlusionCullingEnabled, bool bakeOcclusionCulling, bool clearBakedData)
        {
            try
            {
                GameObject parentObject = GetOrCreateParentObject(GaiaConstants.gaiaLightingObject, true);
                Terrain terrain = TerrainHelper.GetActiveTerrain();

                GameObject occlusionCullObject = GameObject.Find("Occlusion Culling Volume");
                if (occlusionCullObject == null)
                {
                    occlusionCullObject = new GameObject("Occlusion Culling Volume");
                    occlusionCullObject.transform.SetParent(parentObject.transform);
                    OcclusionArea occlusionArea = occlusionCullObject.AddComponent<OcclusionArea>();
                    if (terrain != null)
                    {
                        occlusionArea.size = new Vector3(terrain.terrainData.size.x, terrain.terrainData.size.y, terrain.terrainData.size.z);
                    }
                    else
                    {
                        occlusionArea.size = new Vector3(2000f, 1000f, 2000f);
                    }

                    StaticOcclusionCulling.smallestOccluder = 4f;
                    StaticOcclusionCulling.smallestHole = 0.2f;
                    StaticOcclusionCulling.backfaceThreshold = 15f;
                }
                else
                {
                    OcclusionArea occlusionArea = occlusionCullObject.GetComponent<OcclusionArea>();
                    if (occlusionArea != null)
                    {
                        if (terrain != null)
                        {
                            occlusionArea.size = new Vector3(terrain.terrainData.size.x, terrain.terrainData.size.y, terrain.terrainData.size.z);
                        }
                        else
                        {
                            occlusionArea.size = new Vector3(2000f, 1000f, 2000f);
                        }

                        StaticOcclusionCulling.smallestOccluder = 4f;
                        StaticOcclusionCulling.smallestHole = 0.2f;
                        StaticOcclusionCulling.backfaceThreshold = 15f;
                    }
                }

                Selection.activeGameObject = occlusionCullObject;
                EditorGUIUtility.PingObject(occlusionCullObject);
            }
            catch (Exception e)
            {
                Debug.LogError("Adding occlusion culling had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets up and bakes occlusion culling
        /// </summary>
        /// <param name="occlusionCullingEnabled"></param>
        /// <param name="bakeOcclusionCulling"></param>
        /// <param name="clearBakedData"></param>
        public static void RemoveOcclusionCulling(bool occlusionCullingEnabled, bool bakeOcclusionCulling, bool clearBakedData)
        {
            GameObject occlusionObject = GameObject.Find("Occlusion Culling Volume");
            if (occlusionObject != null)
            {
                Object.DestroyImmediate(occlusionObject);
            }
        }
        /// <summary>
        /// Sets up and bakes occlusion culling
        /// </summary>
        /// <param name="occlusionCullingEnabled"></param>
        /// <param name="bakeOcclusionCulling"></param>
        /// <param name="clearBakedData"></param>
        public static void BakeOcclusionCulling(bool occlusionCullingEnabled, bool bakeOcclusionCulling, bool clearBakedData)
        {
            StaticOcclusionCulling.GenerateInBackground();
        }
        /// <summary>
        /// Sets up and bakes occlusion culling
        /// </summary>
        /// <param name="occlusionCullingEnabled"></param>
        /// <param name="bakeOcclusionCulling"></param>
        /// <param name="clearBakedData"></param>
        public static void CancelOcclusionCulling(bool occlusionCullingEnabled, bool bakeOcclusionCulling, bool clearBakedData)
        {
            StaticOcclusionCulling.Cancel();
        }
        /// <summary>
        /// Sets up and bakes occlusion culling
        /// </summary>
        /// <param name="occlusionCullingEnabled"></param>
        /// <param name="bakeOcclusionCulling"></param>
        /// <param name="clearBakedData"></param>
        public static void ClearOcclusionCulling(bool occlusionCullingEnabled, bool bakeOcclusionCulling, bool clearBakedData)
        {
            StaticOcclusionCulling.Clear();
        }
        /// <summary>
        /// Sets a default ambient lighting bright and high contrast color to remove dark shaodws when developing your terrain
        /// </summary>
        public static void SetDefaultAmbientLight(GaiaLightingProfile profile)
        {
            try
            {
#if UNITY_2020_1_OR_NEWER
                LightingSettings lightingSettings = new LightingSettings();
                if (!Lightmapping.TryGetLightingSettings(out lightingSettings))
                {
#else
                if (Lightmapping.lightingDataAsset == null && Lightmapping.bakedGI && !Lightmapping.realtimeGI)
                {
#endif
                    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                    RenderSettings.ambientSkyColor = new Color(0.635f, 0.696f, 0.735f, 0f);
                    RenderSettings.ambientEquatorColor = new Color(0.509f, 0.509f, 0.509f, 0f);
                    RenderSettings.ambientGroundColor = new Color(0.389f, 0.449f, 0.518f, 0f);

#if UNITY_2020_1_OR_NEWER
                    CreateLightingSettingsAsset();
                    Lightmapping.lightingSettings.bakedGI = false;
                    Lightmapping.lightingSettings.realtimeGI = false;
                    Lightmapping.lightingSettings.autoGenerate = false;
                    Lightmapping.lightingSettings.lightmapper = profile.m_lightmappingMode;
#else
                    Lightmapping.bakedGI = false;
                    Lightmapping.realtimeGI = false;
                    Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
                    LightmapEditorSettings.lightmapper = profile.m_lightmappingMode;
#endif
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Setting up default lightmap and ambient settings had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
        /// <summary>
        /// Sets the auto bake on or off based on the enable bool value
        /// </summary>
        /// <param name="enabled"></param>
        public static void EnableNewSceneQuickBake(bool enabled)
        {
            if (enabled)
            {
                if (Lightmapping.lightingDataAsset == null || !Lightmapping.isRunning)
                {
                    Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.Iterative;
                }
            }
            else
            {
                if (Lightmapping.giWorkflowMode == Lightmapping.GIWorkflowMode.Iterative)
                {
                    Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
                }
            }
        }
        /// <summary>
        /// Creates lighting profile asset in 2020 for scene lighting
        /// </summary>
        public static void CreateLightingSettingsAsset()
        {
#if UNITY_EDITOR && UNITY_2020_1_OR_NEWER
            LightingSettings lightingSettings = new LightingSettings();
            if (!Lightmapping.TryGetLightingSettings(out lightingSettings))
            {
                Lightmapping.lightingSettings = new LightingSettings() { name = "Gaia Lighting Settings" };
                string path = GaiaDirectories.GetSessionSubFolderPath(GaiaSessionManager.GetSessionManager().m_session, true);
                AssetDatabase.CreateAsset(Lightmapping.lightingSettings, path + Path.DirectorySeparatorChar + GaiaConstants.lightingSettingsName);
                AssetDatabase.SaveAssets();
            }
#endif
        }
        /// <summary>
        /// Bakes lightmapping in ASync
        /// </summary>
        public static void BakeLighting(GaiaConstants.BakeMode bakeMode, GaiaLightingProfileValues profile)
        {
            if (profile == null)
            {
                Debug.LogError("GaiaLightingProfile was not found. Unable to complete the bake lighting process, exiting!");
            }
            else
            {
                RenderSettings.ambientMode = profile.m_ambientMode;
                switch (bakeMode)
                {
                    case GaiaConstants.BakeMode.Baked:
                        Lightmapping.bakedGI = true;
                        Lightmapping.realtimeGI = false;
                        break;
                    case GaiaConstants.BakeMode.Realtime:
                        Lightmapping.bakedGI = false;
                        Lightmapping.realtimeGI = true;
                        break;
                    case GaiaConstants.BakeMode.Both:
                        Lightmapping.bakedGI = true;
                        Lightmapping.realtimeGI = true;
                        break;
                }

                Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
                Lightmapping.BakeAsync();
            }
        }
        /// <summary>
        /// Bakes lightmapping in ASync
        /// </summary>
        public static void BakeLighting(GaiaConstants.BakeMode bakeMode)
        {
            switch (bakeMode)
            {
                case GaiaConstants.BakeMode.Baked:
                    Lightmapping.bakedGI = true;
                    Lightmapping.realtimeGI = false;
                    break;
                case GaiaConstants.BakeMode.Realtime:
                    Lightmapping.bakedGI = false;
                    Lightmapping.realtimeGI = true;
                    break;
                case GaiaConstants.BakeMode.Both:
                    Lightmapping.bakedGI = true;
                    Lightmapping.realtimeGI = true;
                    break;
            }

            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
            Lightmapping.BakeAsync();
        }
        /// <summary>
        /// Bakes auto lightmaps only no Realtime or Baked GI
        /// </summary>
        public static void QuickBakeLighting(GaiaLightingProfileValues profile, bool bakeAnyways = false)
        {
            if (profile != null)
            {
                RenderSettings.ambientMode = profile.m_ambientMode;
            }

            Lightmapping.bakedGI = false;
            Lightmapping.realtimeGI = false;
            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
            Lightmapping.BakeAsync();
        }
        /// <summary>
        /// Bakes auto lightmaps only no Realtime or Baked GI
        /// </summary>
        public static void QuickBakeLighting()
        {
            Lightmapping.bakedGI = false;
            Lightmapping.realtimeGI = false;
            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
            Lightmapping.BakeAsync();
        }
        /// <summary>
        /// Cancels the lightmap baking process
        /// </summary>
        public static void CancelLightmapBaking()
        {
            Lightmapping.bakedGI = false;
            Lightmapping.realtimeGI = false;
            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
            Lightmapping.Cancel();
        }
        /// <summary>
        /// Clears the baked lightmap data
        /// </summary>
        public static void ClearBakedLightmaps()
        {
            Lightmapping.Clear();
        }
        /// <summary>
        /// Clears the baked light data of your disk
        /// </summary>
        public static void ClearLightmapDataOnDisk()
        {
            if (EditorUtility.DisplayDialog("Warning!", "Would you like to clear all the baked lightmap GI data on your hard drive?", "Yes", "No"))
            {
                Lightmapping.ClearDiskCache();
            }
        }
        /// <summary>
        /// Removes gaia lighting from the scene
        /// </summary>
        public static void RemoveGaiaLighting()
        {
            GameObject gaiaLighting = GameObject.Find(GaiaConstants.gaiaLightingObject);
            if (gaiaLighting != null)
            {
                Object.DestroyImmediate(gaiaLighting);
            }
        }
        /// <summary>
        /// Opens the profile selection
        /// </summary>
        public static void OpenProfileSelection()
        {
            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
            if (gaiaSettings != null)
            {
                SelectProfileWindowEditor.ShowProfileManager(gaiaSettings.m_gaiaLightingProfile, gaiaSettings.m_gaiaWaterProfile, gaiaSettings);
            }
        }

        #endregion

        #region Utils

        private static void UpdateGeNaLightSync()
        {
#if GENA_PRO
            GaiaTimeOfDayLightSync[] lightSyncs = GameObject.FindObjectsOfType<GaiaTimeOfDayLightSync>();
            if (lightSyncs.Length > 0)
            {
                foreach (GaiaTimeOfDayLightSync sync in lightSyncs)
                {
                    sync.ValidateComponents();
                }
            }
#endif
        }

        /// <summary>
        /// Disable all global shader value (Snow and seasons)
        /// </summary>
        private static void SetDefaultGlobalShader()
        {
#if GAIA_PRO_PRESENT
            Shader.SetGlobalFloat(ProceduralWorldsGlobalWeather.m_globalSnowIntensity, 0f);
            Shader.SetGlobalFloat(ProceduralWorldsGlobalWeather.m_globalCoverLayer1FadeStart, 0f);
            Shader.SetGlobalFloat(ProceduralWorldsGlobalWeather.m_globalCoverLayer1FadeDist, 0f);
            Shader.SetGlobalColor(ProceduralWorldsGlobalWeather.m_globalSeasonTint, GaiaUtils.ColorInvert(Color.white));
#endif
        }
        /// <summary>
        /// Gets or creates the post process volume
        /// </summary>
        /// <returns></returns>
#if UNITY_POST_PROCESSING_STACK_V2
        private static PostProcessVolume GetOrCreatePostProcessVolume()
        {
            try
            {
                PostProcessVolume processVolume = null;
                GameObject processVolumeObject = GameObject.Find("Global Post Processing");
                if (processVolumeObject == null)
                {
                    processVolume = new GameObject("Global Post Processing").AddComponent<PostProcessVolume>();
                    processVolume.gameObject.layer = LayerMask.NameToLayer("TransparentFX");
                    processVolume.isGlobal = true;
                }
                else
                {
                    processVolume = processVolumeObject.GetComponent<PostProcessVolume>();
                    if (processVolume == null)
                    {
                        processVolume = processVolumeObject.AddComponent<PostProcessVolume>();
                    }

                    processVolume.isGlobal = true;
                }

                return processVolume;
            }
            catch (Exception e)
            {
                Debug.LogError("Get or create  post fx volume had a issue " + e.Message + " This came from " + e.StackTrace);
                return null;
            }
        }
#endif
        /// <summary>
        /// Removes the post processing from the scene
        /// </summary>
        public static void RemovePostProcessing()
        {
#if UNITY_POST_PROCESSING_STACK_V2
            PostProcessLayer processLayer = Object.FindObjectOfType<PostProcessLayer>();
            if (processLayer != null)
            {
                Object.DestroyImmediate(processLayer);
                m_processLayer = null;
            }

            GameObject processVolume = GameObject.Find("Global Post Processing");
            if (processVolume != null)
            {
                Object.DestroyImmediate(processVolume);
                m_processVolume = null;
            }
#endif
        }
        /// <summary>
        /// Removes all post processing v2 from the scene
        /// </summary>
        public static void RemoveAllPostProcessV2()
        {
#if UNITY_POST_PROCESSING_STACK_V2
            m_processLayer = Object.FindObjectOfType<PostProcessLayer>();
            if (m_processLayer != null)
            {
                Object.DestroyImmediate(m_processLayer);
                m_processLayer = null;
            }

            PostProcessVolume[] postProcessVolumes = Object.FindObjectsOfType<PostProcessVolume>();
            if (postProcessVolumes != null)
            {
                foreach (PostProcessVolume volume in postProcessVolumes)
                {
                    Object.DestroyImmediate(volume.gameObject);
                }
            }
#endif
        }
        /// <summary>
        /// Removes all post processing v2 from the scene
        /// </summary>
        public static void RemoveAllPostProcessV2CameraLayer()
        {
#if UNITY_POST_PROCESSING_STACK_V2
            m_processLayer = Object.FindObjectOfType<PostProcessLayer>();
            if (m_processLayer != null)
            {
                Object.DestroyImmediate(m_processLayer);
                m_processLayer = null;
            }
#endif
        }

        private static void CheckPostFXV2ColorFilter(GaiaLightingProfileValues profile, Color checkColor)
        {
            try
            {
                if (profile == null)
                {
                    return;
                }

#if UNITY_POST_PROCESSING_STACK_V2
                if (profile.PostProcessProfileBuiltIn != null)
                {
                    if (profile.PostProcessProfileBuiltIn.TryGetSettings(out ColorGrading colorGrading))
                    {
                        if (GaiaUtils.CheckColorKey(colorGrading.colorFilter.value, checkColor))
                        {
                            colorGrading.colorFilter.value = Color.white;
                        }
                    }
                }
#endif
            }
            catch (Exception e)
            {
                Debug.LogError("Checking post processing v2 color filter had a issue " + e.Message + " This came from " + e.StackTrace);
            }
        }
#if GAIA_PRO_PRESENT
        /// <summary>
        /// Gets or creates the ambient audio volume
        /// </summary>
        /// <returns></returns>
        private static GaiaAudioManager GetOrCreateAmbientAudio()
        {
            try
            {
                GaiaAudioManager gaiaAudioManager = null;
                GameObject ambientAudio = GameObject.Find(GaiaConstants.gaiaAudioObject);
                if (ambientAudio == null)
                {
                    ambientAudio = new GameObject(GaiaConstants.gaiaAudioObject);
                }

                gaiaAudioManager = ambientAudio.GetComponent<GaiaAudioManager>();
                if (gaiaAudioManager == null)
                {
                    gaiaAudioManager = ambientAudio.AddComponent<GaiaAudioManager>();
                }

                GameObject player = GaiaUtils.GetCharacter();
                if (player != null)
                {
                    CharacterController controller = player.GetComponent<CharacterController>();
                    if (controller != null)
                    {
                        gaiaAudioManager.m_player = controller.gameObject;
                    }
                }

                return gaiaAudioManager;
            }
            catch (Exception e)
            {
                Debug.LogError("Get or create ambient audio had a issue " + e.Message + " This came from " + e.StackTrace);
                return null;
            }
        }
#endif
        /// <summary>
        /// Gets or creates the gaia audio
        /// </summary>
        /// <returns></returns>
        public static GameObject GetOrCreateGaia2AmbientAudio()
        {
            GameObject ambientAudio = GameObject.Find(GaiaConstants.gaiaAudioObject);
            if (ambientAudio == null)
            {
                ambientAudio = new GameObject(GaiaConstants.gaiaAudioObject);
            }

            return ambientAudio;
        }
        /// <summary>
        /// Removes ambient audio from the scene
        /// </summary>
        private static void RemoveAmbientAudio()
        {
            GameObject ambientAudio = GameObject.Find(GaiaConstants.gaiaAudioObject);
            if (ambientAudio != null)
            {
                Object.DestroyImmediate(ambientAudio);
#if GAIA_PRO_PRESENT
                m_gaiaAudioManager = null;
#endif
            }
        }
        /// <summary>
        /// Get or create a parent object
        /// </summary>
        /// <param name="parentGameObject"></param>
        /// <param name="parentToGaia"></param>
        /// <returns>Parent Object</returns>
        public static GameObject GetOrCreateParentObject(string parentGameObject, bool parentToGaia)
        {
            try
            {
                //Get the parent object
                GameObject theParentGo = GameObject.Find(parentGameObject);

                if (theParentGo == null)
                {
                    theParentGo = GameObject.Find(GaiaConstants.gaiaLightingObject);

                    if (theParentGo == null)
                    {
                        theParentGo = new GameObject(GaiaConstants.gaiaLightingObject);
                    }
                }

                if (theParentGo.GetComponent<GaiaSceneLighting>() == null)
                {
                    theParentGo.AddComponent<GaiaSceneLighting>();
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
                Debug.LogError("Get or creating the aprent object had a issue " + e.Message + " This came from " + e.StackTrace);
                return null;
            }
        }
        /// <summary>
        /// Focuses the profile and selects it in your project
        /// </summary>
        public static void FocusGaiaLightingProfile()
        {
            GameObject lightingProfile = GameObject.Find(GaiaConstants.gaiaLightingObject);
            if (lightingProfile != null)
            {
                EditorGUIUtility.PingObject(lightingProfile);
                Selection.activeObject = lightingProfile;
            }
            else
            {
                Debug.LogError("[GaiaLighting.FocusGaiaLightingProfile()] Unable to focus profile as it does not exists. Please make sure the Gaia Lighting System Profile is set to Gaia Lighting System Profile within your project");
            }
        }
        /// <summary>
        /// Save the assets and marks scene as dirty
        /// </summary>
        /// <param name="saveAlso"></param>
        private static void MarkSceneDirty(bool saveAlso)
        {
            if (!Application.isPlaying)
            {
                GaiaEditorUtils.MarkSceneDirty();

                if (saveAlso)
                {
                    AssetDatabase.SaveAssets();
                }
            }
        }

        #endregion

        #region Pro Utils

#if UNITY_POST_PROCESSING_STACK_V2
        /// <summary>
        /// Creates a biome post processing profile
        /// </summary>
        /// <param name="biomeName"></param>
        /// <param name="postProcessProfile"></param>
        /// <param name="size"></param>
        /// <param name="blendDistance"></param>
        public static void PostProcessingBiomeSpawning(string biomeName, PostProcessProfile postProcessProfile, float size, float blendDistance, GaiaConstants.BiomePostProcessingVolumeSpawnMode spawnMode)
        {
            try
            {
                if (m_parentObject == null)
                {
                    m_parentObject = GetOrCreateParentObject(GaiaConstants.gaiaLightingObject, true);
                }

                GameObject biomeObject = GameObject.Find(biomeName);
                if (biomeObject == null)
                {
                    Debug.LogError("[GaiaLighting.PostProcessingBiomeSpawning()] biomeName could not be found. Does " + biomeName + " object exist in the scene?");
                }
                else
                {
                    string objectName = biomeObject.name;
                    Transform objectTransform = biomeObject.transform;

                    GameObject ppVolumeObject = null;
                    PostProcessVolume processVolume = null;
                    BoxCollider collider = null;
                    GaiaPostProcessBiome postProcessBiome = null;


                    if (spawnMode == GaiaConstants.BiomePostProcessingVolumeSpawnMode.Replace)
                    {
                        ppVolumeObject = GameObject.Find(objectName + " Post Processing");
                        if (ppVolumeObject != null)
                        {
                            processVolume = ppVolumeObject.GetComponent<PostProcessVolume>();
                            collider = processVolume.GetComponent<BoxCollider>();
                            postProcessBiome = processVolume.GetComponent<GaiaPostProcessBiome>();
                        }
                    }


                    if (ppVolumeObject == null || spawnMode == GaiaConstants.BiomePostProcessingVolumeSpawnMode.Add)
                    {
                        ppVolumeObject = new GameObject(objectName + " Post Processing")
                        {
                            layer = LayerMask.NameToLayer("TransparentFX")
                        };
                    }

                    if (processVolume == null)
                    {
                        processVolume = ppVolumeObject.AddComponent<PostProcessVolume>();
                        processVolume.priority = 1;
                        if (m_lightingProfile.m_hideProcessVolume)
                        {
                            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(processVolume, false);
                        }
                        else
                        {
                            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(processVolume, true);
                        }
                    }

                    if (collider == null)
                    {
                        collider = processVolume.gameObject.AddComponent<BoxCollider>();
                        collider.isTrigger = true;
                    }

                    if (postProcessBiome == null)
                    {
                        postProcessBiome = processVolume.gameObject.AddComponent<GaiaPostProcessBiome>();
                    }

                    if (postProcessProfile == null)
                    {
                        Debug.LogError("[GaiaLighting.PostProcessingBiomeSpawning()] Missing post processing profile. Please check that a valid profile is present.");
                    }
                    else
                    {
                        processVolume.sharedProfile = postProcessProfile;
                    }

                    ppVolumeObject.transform.position = objectTransform.position;
                    processVolume.gameObject.transform.SetParent(m_parentObject.transform);
                    processVolume.blendDistance = blendDistance;
                    collider.size = new Vector3(size, size, size);

                    postProcessBiome.m_postProcessProfile = postProcessProfile;
                    postProcessBiome.m_postProcessVolume = processVolume;
                    postProcessBiome.m_blendDistance = blendDistance;
                    postProcessBiome.m_priority = 1;

                    postProcessBiome.m_triggerCollider = collider;
                    postProcessBiome.m_triggerSize = collider.size;

                    postProcessBiome.PostProcessingFileName = postProcessProfile.name;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
#endif

        /// <summary>
        /// Removes old objects from the scene
        /// </summary>
        private static void RemoveOldLighting()
        {
            GameObject oldObjects = GameObject.Find("Ambient Skies Samples");
            if (oldObjects != null)
            {
                Object.DestroyImmediate(oldObjects);
            }
        }

        /// <summary>
        /// Sets up a new global reflection probe in the scene
        /// </summary>
        private static void ProGlobalReflectionProbe()
        {
            try
            {
                GaiaSceneInfo gaiaInfo = GaiaSceneInfo.GetSceneInfo();
                GameObject reflectionProbeObject = GameObject.Find("Pro Global Reflection Probe");
                if (reflectionProbeObject == null)
                {
                    reflectionProbeObject = new GameObject("Pro Global Reflection Probe");
                }

                GameObject parentObject = GetOrCreateParentObject(GaiaConstants.gaiaLightingObject, true);
                reflectionProbeObject.transform.SetParent(parentObject.transform);

                ReflectionProbe probe = reflectionProbeObject.GetComponent<ReflectionProbe>();
                if (probe == null)
                {
                    probe = reflectionProbeObject.AddComponent<ReflectionProbe>();
                    probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
                    probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
                    probe.resolution = 128;
                }
                else
                {
                    probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
                    probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
                    probe.resolution = 128;
                }

                Terrain terrain = Terrain.activeTerrain;
                if (terrain == null)
                {
                    probe.size = new Vector3(5000f, 2000f, 5000f);
                    if (gaiaInfo != null)
                    {
                        Vector3 location = reflectionProbeObject.transform.position;
                        location.y = gaiaInfo.m_seaLevel + 1.5f;
                        reflectionProbeObject.transform.position = location;
                    }
                }
                else
                {
                    probe.size = new Vector3(terrain.terrainData.size.x, 2000f, terrain.terrainData.size.z);
                    Vector3 location = reflectionProbeObject.transform.position;
                    location.y = terrain.SampleHeight(location) + 1.5f;
                    if (location.y < gaiaInfo.m_seaLevel)
                    {
                        location.y = gaiaInfo.m_seaLevel + 1.5f;
                    }
                    reflectionProbeObject.transform.position = location;
                }

                probe.RenderProbe();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Creates the new gaia reflection probe for 2019.1+
        /// Supports multi terrain size and position
        /// </summary>
        public static void NewGlobalReflectionProbe(SceneProfile profile)
        {
            try
            {
                //No reflection probe without a terrain being present
                if (Terrain.activeTerrain == null)
                {
                    return;
                }

                //Setup Probe
                int probeSize = 0;
                float sampledHeight = 0f;
                float seaLevel = GaiaSceneInfo.GetSceneInfo().m_seaLevel + 1.5f;
                GameObject parentObject = GetOrCreateParentObject(GaiaConstants.gaiaLightingObject, true);
                Terrain[] terrains = Terrain.activeTerrains;
                if (terrains != null)
                {
                    foreach (Terrain terrain in terrains)
                    {
                        probeSize += (int)terrain.terrainData.size.x;
                    }
                }

                bool multiTerrains = terrains.Length > 1;

                //Create Probe Object
                GameObject probeObject = GameObject.Find("Global Gaia Reflection Probe");
                if (!profile.m_globalReflectionProbe)
                {
                    if (probeObject != null)
                    {
                        Object.DestroyImmediate(probeObject);
                    }
                }
                else
                {
                    if (probeObject == null)
                    {
                        probeObject = new GameObject("Global Gaia Reflection Probe");
                    }

                    //Parent Object
                    probeObject.transform.SetParent(parentObject.transform);

                    //Set Position
                    sampledHeight = Terrain.activeTerrain.SampleHeight(probeObject.transform.position);
                    if (sampledHeight < seaLevel)
                    {
                        probeObject.transform.position = new Vector3(0f, seaLevel + 1.5f, 0f);
                    }
                    else
                    {
                        probeObject.transform.position = new Vector3(0f, sampledHeight + 1.5f, 0f);
                    }

                    //Create Probe Component
                    ReflectionProbe probe = probeObject.GetComponent<ReflectionProbe>();
                    if (probe == null)
                    {
                        probe = probeObject.AddComponent<ReflectionProbe>();
                    }

                    //Set Probe Setup
                    if (multiTerrains)
                    {
                        probe.size = new Vector3(probeSize / 2, probeSize / 2, probeSize / 2);
                    }
                    else
                    {
                        probe.size = new Vector3(probeSize, probeSize, probeSize);
                    }

                    //Renders only Skybox
                    probe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
                    //probe.cullingMask = 0 << 2;
                    probe.cullingMask = 1;

                    probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
                    probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;
                    probe.hdr = true;
                    probe.shadowDistance = 50f;

                    //Render Probe
                    if (probe.IsFinishedRendering(probe.GetInstanceID()))
                    {
                        probe.RenderProbe();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Removes systems from scene
        /// </summary>
        public static void RemoveSystems()
        {
            GameObject sampleContent = GameObject.Find("Ambient Skies Samples Environment");
            if (sampleContent != null)
            {
                Object.DestroyImmediate(sampleContent);
            }
        }

        /// <summary>
        /// Sets a quick optimization for targeted environment quality
        /// </summary>
        /// <param name="gaiaSettings"></param>
        public static void QuickOptimize(GaiaSettings gaiaSettings)
        {
            try
            {
                if (EditorUtility.DisplayDialog("Warning!", "Proceeding with this optimization will modify your 'Terrain Settings, LOD Bias Settings, Shadow Distance and Water Reflections. Would you like to proceed?", "Yes", "No"))
                {
                    Terrain[] terrains = Terrain.activeTerrains;
                    float lodBias = QualitySettings.lodBias;
                    float shadowDistance = QualitySettings.shadowDistance;
                    bool waterReflections = gaiaSettings.m_gaiaWaterProfile.m_enableReflections;

                    if (gaiaSettings.m_currentEnvironment != GaiaConstants.EnvironmentTarget.Custom)
                    {
                        Debug.Log("Configuring your scene to " + gaiaSettings.m_currentEnvironment.ToString());
                    }

                    switch (gaiaSettings.m_currentEnvironment)
                    {
                        case GaiaConstants.EnvironmentTarget.UltraLight:
                            {
                                if (terrains != null)
                                {
                                    foreach (Terrain terrain in terrains)
                                    {
                                        terrain.detailObjectDensity = 0.1f;
                                        terrain.detailObjectDistance = 40f;
                                        terrain.heightmapPixelError = 45f;
                                    }
                                }

                                if (lodBias > 1)
                                {
                                    QualitySettings.lodBias = 0.7f;
                                }

                                if (shadowDistance > 100)
                                {
                                    QualitySettings.shadowDistance = 50f;
                                }

                                QualitySettings.shadowResolution = UnityEngine.ShadowResolution.Low;
                                QualitySettings.shadowCascade4Split = new Vector3(0.1f, 0.3f, 0.5f);

                                if (waterReflections)
                                {
                                    GaiaWater.SetWaterReflectionsType(false, gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, gaiaSettings.m_gaiaWaterProfile, gaiaSettings.m_gaiaWaterProfile.m_waterProfiles[gaiaSettings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex]);
                                }
                                break;
                            }
                        case GaiaConstants.EnvironmentTarget.MobileAndVR:
                            {
                                if (terrains != null)
                                {
                                    foreach (Terrain terrain in terrains)
                                    {
                                        terrain.detailObjectDensity = 0.2f;
                                        terrain.detailObjectDistance = 60f;
                                        terrain.heightmapPixelError = 22f;
                                    }
                                }

                                if (lodBias > 1)
                                {
                                    QualitySettings.lodBias = 0.9f;
                                }

                                if (shadowDistance > 100)
                                {
                                    QualitySettings.shadowDistance = 65f;
                                }

                                QualitySettings.shadowResolution = UnityEngine.ShadowResolution.Low;
                                QualitySettings.shadowCascade4Split = new Vector3(0.1f, 0.4f, 0.6f);

                                if (waterReflections)
                                {
                                    GaiaWater.SetWaterReflectionsType(false, gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, gaiaSettings.m_gaiaWaterProfile, gaiaSettings.m_gaiaWaterProfile.m_waterProfiles[gaiaSettings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex]);
                                }
                                break;
                            }
                        case GaiaConstants.EnvironmentTarget.Desktop:
                            {
                                if (terrains != null)
                                {
                                    foreach (Terrain terrain in terrains)
                                    {
                                        terrain.detailObjectDensity = 0.3f;
                                        terrain.detailObjectDistance = 120f;
                                        terrain.heightmapPixelError = 13f;
                                    }
                                }

                                if (lodBias > 1)
                                {
                                    QualitySettings.lodBias = 1.1f;
                                }

                                if (shadowDistance > 150)
                                {
                                    QualitySettings.shadowDistance = 100f;
                                }

                                QualitySettings.shadowResolution = UnityEngine.ShadowResolution.Medium;
                                QualitySettings.shadowCascade4Split = new Vector3(0.3f, 0.45f, 0.7f);

                                if (waterReflections)
                                {
                                    GaiaWater.SetWaterReflectionsType(false, gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, gaiaSettings.m_gaiaWaterProfile, gaiaSettings.m_gaiaWaterProfile.m_waterProfiles[gaiaSettings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex]);
                                }
                                break;
                            }
                        case GaiaConstants.EnvironmentTarget.PowerfulDesktop:
                            {
                                if (terrains != null)
                                {
                                    foreach (Terrain terrain in terrains)
                                    {
                                        terrain.detailObjectDensity = 0.5f;
                                        terrain.detailObjectDistance = 175f;
                                        terrain.heightmapPixelError = 7f;
                                    }
                                }

                                if (lodBias >= 2)
                                {
                                    QualitySettings.lodBias = 1.5f;
                                }

                                if (shadowDistance > 200)
                                {
                                    QualitySettings.shadowDistance = 125f;
                                }

                                QualitySettings.shadowResolution = UnityEngine.ShadowResolution.High;
                                QualitySettings.shadowCascade4Split = new Vector3(0.25f, 0.5f, 0.7f);

                                if (waterReflections)
                                {
                                    GaiaWater.SetWaterReflectionsType(false, gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, gaiaSettings.m_gaiaWaterProfile, gaiaSettings.m_gaiaWaterProfile.m_waterProfiles[gaiaSettings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex]);
                                }
                                break;
                            }
                        case GaiaConstants.EnvironmentTarget.Custom:
                            {
                                Debug.Log("The target environment is set to 'Custom'. Unable to modify quick optimization settings in custom setting");
                                break;
                            }
                    }

                    if (EditorUtility.DisplayDialog("Info!", "Would you like to show the current settings that have been set in the console?", "Yes", "No"))
                    {
                        if (gaiaSettings.m_currentEnvironment != GaiaConstants.EnvironmentTarget.Custom)
                        {
                            if (terrains != null)
                            {
                                foreach (Terrain terrain in terrains)
                                {
                                    Debug.Log(terrain.name + " Detail Density is " + terrain.detailObjectDensity);
                                    Debug.Log(terrain.name + " Detail Distance is " + terrain.detailObjectDistance);
                                    Debug.Log(terrain.name + " Pixel Error is " + terrain.heightmapPixelError);
                                }
                            }

                            Debug.Log("LOD Bias in quality settings is set to " + QualitySettings.lodBias);
                            Debug.Log("Shadow Distance in quality settings is set to " + QualitySettings.shadowDistance);
                            Debug.Log("Shadow Resolution in quality settings is set to " + QualitySettings.shadowResolution.ToString());
                            Debug.Log("Water reflections is set to " + gaiaSettings.m_gaiaWaterProfile.m_enableReflections);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Updates the dynamic GI
        /// </summary>
        public static void UpdateAmbientEnvironment()
        {
            DynamicGI.UpdateEnvironment();
        }

        #endregion
    }
}
