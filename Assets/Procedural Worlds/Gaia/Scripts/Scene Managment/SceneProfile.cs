using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace Gaia
{
    [System.Serializable]
    public class ReflectionProbeData
    {
        [Header("Reflection Probe Settings")]
        public ReflectionProbeMode reflectionProbeMode = ReflectionProbeMode.Realtime;
        public GaiaConstants.ReflectionProbeRefreshModePW reflectionProbeRefresh = GaiaConstants.ReflectionProbeRefreshModePW.OnAwake;
        public ReflectionCubemapCompression reflectionCubemapCompression = ReflectionCubemapCompression.Auto;
        public ReflectionProbeTimeSlicingMode reflectionProbeTimeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        public int reflectionProbesPerRow = 5;
        public float reflectionProbeOffset = 1.8f;
        public float reflectionProbeClipPlaneDistance = 1000f;
        public float reflectionProbeBlendDistance = 5f;
        public LayerMask reflectionprobeCullingMask = 1;
        public float reflectionProbeShadowDistance = 100f;
        public GaiaConstants.ReflectionProbeResolution reflectionProbeResolution = GaiaConstants.ReflectionProbeResolution.Resolution64;
        public int lightProbesPerRow = 50;
        public int lightProbeSpawnRadius = 25;
        public float seaLevel = 50f;
#if UNITY_EDITOR
#if !UNITY_2020_1_OR_NEWER
        public LightmapEditorSettings.FilterMode filterMode = LightmapEditorSettings.FilterMode.Auto;
#else
        public LightingSettings.FilterMode filterMode = LightingSettings.FilterMode.Auto;
#endif
#endif
    }

    public class SceneProfile : ScriptableObject
    {
        public string ProfileVersion = "None";

        #region Global

        //Global
        public bool m_reimportHDRPShader = false;
        public string m_savedFromScene;
        public GaiaTimeOfDay m_gaiaTimeOfDay = new GaiaTimeOfDay();
        public GaiaWeather m_gaiaWeather = new GaiaWeather();
        public float m_lodBias = 2f;

        #endregion

        #region Lighting

        //Lighting
        public Texture2D m_kelvinTexture;
        public GaiaConstants.GlobalSystemMode m_lightSystemMode = GaiaConstants.GlobalSystemMode.Gaia;
        public GameObject m_thirdPartyLightObject;
        public bool DefaultLightingSet = false;
        public bool m_lightingMultiSceneLightingSupport = true;
        public bool m_lightingUpdateInRealtime = false;
        public int m_selectedLightingProfileValuesIndex = 0;
        public GaiaConstants.GaiaLightingProfileType m_profileType = GaiaConstants.GaiaLightingProfileType.HDRI;
        public bool m_renamingProfile = false;
        public bool m_lightingEditSettings = false;
        public GaiaConstants.BakeMode m_lightingBakeMode = GaiaConstants.BakeMode.Realtime;
#if UNITY_EDITOR
#if UNITY_2020_1_OR_NEWER
        public LightingSettings.Lightmapper m_lightmappingMode = LightingSettings.Lightmapper.ProgressiveGPU;
#else
        public LightmapEditorSettings.Lightmapper m_lightmappingMode = LightmapEditorSettings.Lightmapper.ProgressiveGPU;
#endif
#endif
        public Material m_masterSkyboxMaterial;
        public List<GaiaLightingProfileValues> m_lightingProfiles = new List<GaiaLightingProfileValues>();
        public bool m_parentObjects = true;
        public bool m_hideProcessVolume = true;
        public bool m_enablePostProcessing = true;
        public bool m_enableAmbientAudio = true;
        public bool m_enableFog = true;
        public GaiaConstants.GaiaProAntiAliasingMode m_antiAliasingMode = GaiaConstants.GaiaProAntiAliasingMode.TAA;
        public float m_AAJitterSpread = 0.55f;
        public float m_AAStationaryBlending = 0.95f;
        public float m_AAMotionBlending = 0.9f;
        public float m_AASharpness = 0.25f;
        public float m_antiAliasingTAAStrength = 0.7f;
        public bool m_cameraDithering = true;
        public float m_cameraAperture = 16f;
        public bool m_usePhysicalCamera = false;
        public Vector2 m_cameraSensorSize = new Vector2(70.41f, 52.63f);
        public float m_cameraFocalLength = 50f;
        public bool m_globalReflectionProbe = true;
        public bool m_isUserProfileSet = false;

        //Auto DOF
        public bool m_enableAutoDOF = true;
        public LayerMask m_dofLayerDetection = 1;
        public float m_depthOfFieldFocusDistance = 50f;

        //Light Probes
        public ReflectionProbeData m_reflectionProbeData = new ReflectionProbeData();

        #endregion

        #region SRP Lighting & Post FX

        #if UPPipeline
        public VolumeProfile m_universalPostFXProfile;
        #endif

        #if HDPipeline
        public VolumeProfile m_highDefinitionLightingProfile;
        public VolumeProfile m_highDefinitionPostFXProfile;
        #endif

        #endregion

        #region Water

        //Water
        public GaiaConstants.GlobalSystemMode m_waterSystemMode = GaiaConstants.GlobalSystemMode.Gaia;
        public GameObject m_thirdPartyWaterObject;
        public bool DefaultWaterSet = false;
        public bool m_waterMultiSceneLightingSupport = true;
        public bool m_waterRenamingProfile = false;
        public bool m_waterUpdateInRealtime = false;
        public bool m_allowMSAA = false;
        public bool m_useHDR = false;
        public bool m_enableDisabeHeightFeature = true;
        public float m_disableHeight = 100f;
        public string m_selectedProfile = "Deep Blue Ocean";
        public float m_interval = 0.25f;
        public bool m_useCustomRenderDistance = false;
        public bool m_enableLayerDistances = false;
        public float m_customRenderDistance = 500f;
        public float[] m_customRenderDistances = new float[32];
        public GaiaPlanarReflections.PlanarReflectionSettings m_reflectionSettingsData;
        public bool m_waterEditSettings = false;
        public int m_selectedWaterProfileValuesIndex;
        public bool m_autoRefresh = true;
        public bool m_ignoreSceneConditions = true;
        public float m_refreshRate = 0.5f;
        public GaiaConstants.WaterAutoUpdateMode m_autoUpdateMode = GaiaConstants.WaterAutoUpdateMode.Interval;

        public bool m_useCastics = true;
        public Light m_mainCausticLight;
        public int m_causticFramePerSecond = 24;
        public float m_causticSize = 15f;

        public bool InfiniteMode = true;
        public GameObject m_waterPrefab;
        public GameObject m_underwaterParticles;
        public GameObject m_underwaterHorizonPrefab;
        public GameObject m_hdPlanarReflections;
        public GameObject m_transitionFXPrefab;
        public List<GaiaWaterProfileValues> m_waterProfiles = new List<GaiaWaterProfileValues>();
        public Material m_activeWaterMaterial;

        public bool m_enableWaterMeshQuality = false;
        public GaiaConstants.WaterMeshQuality m_waterMeshQuality = GaiaConstants.WaterMeshQuality.Medium;
        public GaiaConstants.MeshType m_meshType = GaiaConstants.MeshType.Plane;
        public int m_zSize = 1000;
        public int m_xSize = 1000;
        public int m_customMeshQuality = 100;

        public bool m_enableReflections = true;
        public bool m_disaleSkyboxReflection = false;
        public bool m_disablePixelLights = true;
        public GaiaConstants.GaiaProWaterReflectionsQuality m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution512;
        public bool m_verifiedHDRPReflections = false;
        public int m_textureResolution = 512;
        public float m_clipPlaneOffset = 40f;
        public LayerMask m_reflectedLayers = 1;
        public float m_hdrpReflectionIntensity = 1f;

        public bool m_enableOceanFoam = true;
        public bool m_enableBeachFoam = true;
        public bool m_enableGPUInstancing = true;
        public bool m_autoWindControlOnWater = true;

        public bool m_supportUnderwaterEffects = true;
        public bool m_supportUnderwaterPostProcessing = true;
        public bool m_supportUnderwaterFog = true;
        public bool m_supportUnderwaterParticles = true;

        #endregion

        #region Player

        public GaiaConstants.EnvironmentControllerType m_controllerType = GaiaConstants.EnvironmentControllerType.FlyingCamera;
        public bool m_setupPostFX = true;
        public bool m_spawnPlayerAtCurrentLocation = true;
        public GameObject m_customPlayer;
        public Camera m_customCamera;

        //Auto DOF

        //Camera Culling
        public bool m_enableLayerCulling = true;
        [SerializeField]
        private GaiaSceneCullingProfile m_cullingProfile;
        public GaiaSceneCullingProfile CullingProfile {
            get
            {
                return m_cullingProfile;
            }
            set
            {
                m_cullingProfile = value;
                RefreshCullingProfileLayers();
            }
        }

        public Light m_sunLight;

        //Terrain Culling
        public bool m_terrainCullingEnabled = true;

        //Terrain Loading
#if GAIA_PRO_PRESENT
        public LoadMode m_terrainLoaderLoadMode;
        public float m_terrainLoaderMinRefreshDistance;
        public float m_terrainLoaderMaxRefreshDistance;
        public float m_terrainLoaderMinRefreshMS;
        public float m_terrainLoaderMaxRefreshMS;
        public bool m_terrainLoaderFollowTransform;
        public BoundsDouble m_terrainLoaderLoadingBoundsRegular;
        public BoundsDouble m_terrainLoaderLoadingBoundsImpostor;
        public BoundsDouble m_terrainLoaderLoadingBoundsCollider;
        public bool m_terrainLoaderDataInitialized;
#endif

        #endregion

        #region Utils

        private void RefreshCullingProfileLayers()
        {
            if (m_cullingProfile == null)
            {
                return;
            }

            bool madeChanges = false;

            float[] correctedDistances = new float[32];
            for (int i = 0; i < correctedDistances.Length; i++)
            {
                correctedDistances[i] = 0;
                string layerName = LayerMask.LayerToName(i);
                for (int targetIndex = 0; targetIndex < m_cullingProfile.m_layerNames.Length; targetIndex++)
                {
                    if (m_cullingProfile.m_layerNames[targetIndex] == layerName)
                    {
                        correctedDistances[i] = m_cullingProfile.m_layerDistances[targetIndex];
                        if (i != targetIndex)
                        {
                            madeChanges = true;
                        }
                        break;
                    }
                }
            }
            //only assign the corrected distance if there is an actual difference
            if (madeChanges)
            {
                m_cullingProfile.m_layerDistances = correctedDistances;
            }

            bool madeShadowChanges = false;

            float[] correctedShadowDistances = new float[32];
            for (int i = 0; i < correctedShadowDistances.Length; i++)
            {
                correctedShadowDistances[i] = 0;
                string layerName = LayerMask.LayerToName(i);
                for (int targetIndex = 0; targetIndex < m_cullingProfile.m_layerNames.Length; targetIndex++)
                {
                    if (m_cullingProfile.m_layerNames[targetIndex] == layerName)
                    {
                        correctedShadowDistances[i] = m_cullingProfile.m_shadowLayerDistances[targetIndex];
                        if (i != targetIndex)
                        {
                            madeShadowChanges = true;
                        }
                        break;
                    }
                }
            }
            //only assign the corrected shadow distance if there is an actual difference
            if (madeShadowChanges)
            {
                m_cullingProfile.m_shadowLayerDistances = correctedShadowDistances;
            }

            bool madeLayerNameChanges = false;

            for (int i = 0; i < 32; i++)
            {
                if (m_cullingProfile.m_layerNames[i] != LayerMask.LayerToName(i))
                {
                    m_cullingProfile.m_layerNames[i] = LayerMask.LayerToName(i);
                    madeLayerNameChanges = true;
                }
            }
#if UNITY_EDITOR
            if (madeChanges || madeShadowChanges || madeLayerNameChanges)
            {
                EditorUtility.SetDirty(m_cullingProfile);
            }
#endif
        }

#if GAIA_PRO_PRESENT
        public void UpdateTerrainLoaderFromProfile(ref TerrainLoader tl)
        {
            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                if (tl != null)
                {
                    tl.LoadMode = m_terrainLoaderLoadMode;
                    tl.m_minRefreshDistance = m_terrainLoaderMinRefreshDistance;
                    tl.m_maxRefreshDistance = m_terrainLoaderMaxRefreshDistance;
                    tl.m_minRefreshMS = m_terrainLoaderMinRefreshMS;
                    tl.m_maxRefreshMS = m_terrainLoaderMaxRefreshMS;
                    tl.m_followTransform = m_terrainLoaderFollowTransform;
                    tl.m_loadingBoundsRegular = m_terrainLoaderLoadingBoundsRegular;
                    tl.m_loadingBoundsImpostor = m_terrainLoaderLoadingBoundsImpostor;
                    tl.m_loadingBoundsCollider = m_terrainLoaderLoadingBoundsCollider;
                }
            }
        }
#endif

        public void UpdateTextureResolution()
        {
            switch (m_reflectionResolution)
            {
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution8:
                    m_textureResolution = 8;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution16:
                    m_textureResolution = 16;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution32:
                    m_textureResolution = 32;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution64:
                    m_textureResolution = 64;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution128:
                    m_textureResolution = 128;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution256:
                    m_textureResolution = 256;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution512:
                    m_textureResolution = 512;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution1024:
                    m_textureResolution = 1024;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution2048:
                    m_textureResolution = 2048;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution4096:
                    m_textureResolution = 4096;
                    break;
                case GaiaConstants.GaiaProWaterReflectionsQuality.Resolution8192:
                    m_textureResolution = 8192;
                    break;
            }
        }

#endregion
    }
}
