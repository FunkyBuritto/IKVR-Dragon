using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ProceduralWorlds.WaterSystem;

namespace Gaia
{
    public class GaiaWaterProfile : ScriptableObject
    {
        [HideInInspector] 
        public bool m_reimportHDRPShader = false;
        [HideInInspector]
        public bool m_multiSceneLightingSupport = true;
        [HideInInspector]
        public bool m_renamingProfile = false;
        [HideInInspector]
        public bool m_updateInRealtime = false;
        [HideInInspector]
        public bool m_allowMSAA = false;
        [HideInInspector]
        public bool m_useHDR = false;
        [HideInInspector]
        public bool m_enableDisabeHeightFeature = true;
        [HideInInspector]
        public float m_disableHeight = 100f;
        [HideInInspector]
        public string m_selectedProfile = "Deep Blue Ocean";
        // [HideInInspector]
        // public GaiaConstants.RenderUpdateMode m_waterRenderUpdateMode = GaiaConstants.RenderUpdateMode.Update;
        [HideInInspector]
        public float m_interval = 0.25f;
        [HideInInspector]
        public bool m_useCustomRenderDistance = false;
        [HideInInspector]
        public bool m_enableLayerDistances = false;
        [HideInInspector]
        public float m_customRenderDistance = 500f;
        [HideInInspector]
        public float[] m_customRenderDistances = new float[32];
        [HideInInspector]
        public GaiaPlanarReflections.PlanarReflectionSettings m_reflectionSettingsData;
        [HideInInspector]
        public bool m_editSettings = false;
        [HideInInspector]
        public int m_selectedWaterProfileValuesIndex;

        [HideInInspector]
        public bool m_useCastics = true;
        [HideInInspector]
        public Light m_mainCausticLight;
        [HideInInspector]
        public int m_causticFramePerSecond = 24;
        [HideInInspector]
        public float m_causticSize = 15f;

        //[HideInInspector]
        //public Material m_masterWaterMaterial;
        [HideInInspector]
        public GameObject m_waterPrefab;
        [HideInInspector]
        public GameObject m_underwaterParticles;
        [HideInInspector]
        public GameObject m_underwaterHorizonPrefab;
        [HideInInspector]
        public GameObject m_hdPlanarReflections;
        [HideInInspector]
        public GameObject m_transitionFXPrefab;

        public List<GaiaWaterProfileValues> m_waterProfiles = new List<GaiaWaterProfileValues>();
        [HideInInspector]
        public Material m_activeWaterMaterial;

        [HideInInspector]
        public bool m_enableWaterMeshQuality = false;
        [HideInInspector]
        public GaiaConstants.WaterMeshQuality m_waterMeshQuality = GaiaConstants.WaterMeshQuality.Medium;
        [HideInInspector]
        public GaiaConstants.MeshType m_meshType = GaiaConstants.MeshType.Plane;
        [HideInInspector]
        public int m_zSize = 1000;
        [HideInInspector]
        public int m_xSize = 1000;
        [HideInInspector]
        public int m_customMeshQuality = 100;

        [HideInInspector]
        public bool m_enableReflections = true;
        [HideInInspector]
        public bool m_disablePixelLights = true;
        [HideInInspector]
        public GaiaConstants.GaiaProWaterReflectionsQuality m_reflectionResolution = GaiaConstants.GaiaProWaterReflectionsQuality.Resolution512;
        [HideInInspector]
        public int m_textureResolution = 512;
        [HideInInspector]
        public float m_clipPlaneOffset = 40f;
        [HideInInspector]
        public LayerMask m_reflectedLayers = 1;
        [HideInInspector]
        public float m_hdrpReflectionIntensity = 1f;

        [HideInInspector]
        public bool m_enableOceanFoam = true;
        [HideInInspector]
        public bool m_enableBeachFoam = true;
        [HideInInspector]
        public bool m_enableGPUInstancing = true;
        [HideInInspector]
        public bool m_autoWindControlOnWater = true;

        [HideInInspector]
        public bool m_supportUnderwaterEffects = true;
        [HideInInspector]
        public bool m_supportUnderwaterPostProcessing = true;
        [HideInInspector]
        public bool m_supportUnderwaterFog = true;
        [HideInInspector]
        public bool m_supportUnderwaterParticles = true;

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

        /// <summary>
        /// Create Gaia Lighting System Profile asset
        /// </summary>
#if UNITY_EDITOR
        [MenuItem("Assets/Create/Procedural Worlds/Gaia/Water Profile")]
        public static void CreateSkyProfiles()
        {
            GaiaWaterProfile asset = ScriptableObject.CreateInstance<GaiaWaterProfile>();
            AssetDatabase.CreateAsset(asset, "Assets/Gaia Water System Profile.asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
#endif
    }
}