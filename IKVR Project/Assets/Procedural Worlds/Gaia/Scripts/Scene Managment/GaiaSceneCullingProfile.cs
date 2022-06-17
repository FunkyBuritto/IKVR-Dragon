using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gaia
{
    public class GaiaSceneCullingProfile : ScriptableObject
    {
        public enum ShadowCullingType
        {
            Small,
            Medium,
            Large
        }

        [Header("Global Settings")] 
        //public bool m_enableLayerCulling = true;
        public bool m_applyToEditorCamera = false;
        public bool m_realtimeUpdate = false;
        public float[] m_layerDistances = new float[32];
        public string[] m_layerNames = new string[32];
        public float[] m_shadowLayerDistances = new float[32];
        public void UpdateCulling(GaiaSettings gaiaSettings)
        {
            if (!GaiaUtils.CheckIfSceneProfileExists())
            {
                return;
            }
            if (GaiaGlobal.Instance.m_mainCamera == null)
            {
                GaiaGlobal.Instance.m_mainCamera = GaiaUtils.GetCamera();
            }

            float farClipPlane = 2000f;
            if (GaiaGlobal.Instance.m_mainCamera != null)
            {
                farClipPlane = GaiaGlobal.Instance.m_mainCamera.farClipPlane;
            }

            if (GaiaGlobal.Instance.SceneProfile.m_sunLight == null)
            {
                GaiaGlobal.Instance.SceneProfile.m_sunLight = GaiaUtils.GetMainDirectionalLight();
            }

            Terrain terrain = TerrainHelper.GetActiveTerrain();

            //Objects
            m_layerDistances = new float[32];
            for (int i = 0; i < m_layerDistances.Length; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                switch (layerName)
                {
                    case "Default":
                    case "Water":
                    case "PW_VFX":
                        m_layerDistances[i] = 0f;
                        break;
                    case "PW_Object_Small":
                        m_layerDistances[i] = GaiaUtils.CalculateCameraCullingLayerValue(terrain, gaiaSettings.m_currentEnvironment, 5f);
                        break;
                    case "PW_Object_Medium":
                        m_layerDistances[i] = GaiaUtils.CalculateCameraCullingLayerValue(terrain, gaiaSettings.m_currentEnvironment, 3f);
                        break;
                    case "PW_Object_Large":
                        m_layerDistances[i] = GaiaUtils.CalculateCameraCullingLayerValue(terrain, gaiaSettings.m_currentEnvironment);
                        break;
                    default:
                        m_layerDistances[i] = 0f;
                        break;
                }
            }
        }
        public void UpdateShadow()
        {
            //Shadows
            m_shadowLayerDistances = new float[32];
            for (int i = 0; i < m_shadowLayerDistances.Length; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                switch (layerName)
                {
                    case "Default":
                    case "Water":
                    case "PW_VFX":
                        m_shadowLayerDistances[i] = 0f;
                        break;
                    case "PW_Object_Small":
                        m_shadowLayerDistances[i] = GaiaUtils.CalculateShadowCullingLayerValue(ShadowCullingType.Small, QualitySettings.shadowDistance, 0f, 0f, 5f);
                        break;
                    case "PW_Object_Medium":
                        m_shadowLayerDistances[i] = GaiaUtils.CalculateShadowCullingLayerValue(ShadowCullingType.Medium, QualitySettings.shadowDistance, 0f, 3f, 0f);
                        break;
                    case "PW_Object_Large":
                        m_shadowLayerDistances[i] = GaiaUtils.CalculateShadowCullingLayerValue(ShadowCullingType.Large, QualitySettings.shadowDistance, 1f, 0f, 0f);
                        break;
                    default:
                        m_shadowLayerDistances[i] = 0f;
                        break;
                }
            }
        }

        /// <summary>
        /// Create Gaia Culling System Profile asset
        /// </summary>
#if UNITY_EDITOR
        public static GaiaSceneCullingProfile CreateCullingProfile()
        {
            GaiaSceneCullingProfile asset = ScriptableObject.CreateInstance<GaiaSceneCullingProfile>();
            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
            asset.UpdateCulling(gaiaSettings);
            asset.UpdateShadow();
            AssetDatabase.CreateAsset(asset, "Assets/Gaia Scene Culling Profile.asset");
            AssetDatabase.SaveAssets();
            return asset;
        }
        [MenuItem("Assets/Create/Procedural Worlds/Gaia/Gaia Scene Culling Profile")]
        public static void CreateCullingProfileMenu()
        {
            GaiaSceneCullingProfile asset = ScriptableObject.CreateInstance<GaiaSceneCullingProfile>();
            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
            asset.UpdateCulling(gaiaSettings);
            asset.UpdateShadow();
            AssetDatabase.CreateAsset(asset, "Assets/Gaia Scene Culling Profile.asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
#endif
    }
}