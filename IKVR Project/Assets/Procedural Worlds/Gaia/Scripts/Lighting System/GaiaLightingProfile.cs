using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Gaia
{
    public class GaiaLightingProfile : ScriptableObject
    {
        [HideInInspector]
        public bool m_multiSceneLightingSupport = true;
        [HideInInspector]
        public bool m_updateInRealtime = false;
        //[HideInInspector]
        //public GaiaConstants.GaiaLightingProfileType m_lightingProfile = GaiaConstants.GaiaLightingProfileType.Day;
        [HideInInspector]
        public int m_selectedLightingProfileValuesIndex = 0;
        [HideInInspector]
        public bool m_renamingProfile = false;
        [HideInInspector]
        public bool m_editSettings = false;
        [HideInInspector]
        public GaiaConstants.BakeMode m_lightingBakeMode = GaiaConstants.BakeMode.Realtime;
#if UNITY_EDITOR
#if UNITY_2020_1_OR_NEWER
        public LightingSettings.Lightmapper m_lightmappingMode = LightingSettings.Lightmapper.ProgressiveGPU;
#else
        public LightmapEditorSettings.Lightmapper m_lightmappingMode = LightmapEditorSettings.Lightmapper.ProgressiveGPU;
#endif
#endif
        [HideInInspector]
        public Material m_masterSkyboxMaterial;
        public List<GaiaLightingProfileValues> m_lightingProfiles = new List<GaiaLightingProfileValues>();

        [HideInInspector]
        public bool m_parentObjects = true;
        [HideInInspector]
        public bool m_hideProcessVolume = true;
        [HideInInspector]
        public bool m_enablePostProcessing = true;
        [HideInInspector]
        public bool m_enableAmbientAudio = true;
        [HideInInspector]
        public bool m_enableFog = true;
        [HideInInspector]
        public GaiaConstants.GaiaProAntiAliasingMode m_antiAliasingMode = GaiaConstants.GaiaProAntiAliasingMode.TAA;
        [HideInInspector]
        public float m_AAJitterSpread = 0.55f;
        [HideInInspector]
        public float m_AAStationaryBlending = 0.95f;
        [HideInInspector]
        public float m_AAMotionBlending = 0.9f;
        [HideInInspector]
        public float m_AASharpness = 0.25f;
        [HideInInspector]
        public float m_antiAliasingTAAStrength = 0.7f;
        [HideInInspector]
        public bool m_cameraDithering = true;
        [HideInInspector]
        public float m_cameraAperture = 16f;
        [HideInInspector]
        public bool m_usePhysicalCamera = false;
        [HideInInspector]
        public Vector2 m_cameraSensorSize = new Vector2(70.41f, 52.63f);
        [HideInInspector]
        public float m_cameraFocalLength = 50f;
        [HideInInspector]
        public bool m_globalReflectionProbe = true;

        //Auto DOF
        public bool m_enableAutoDOF = true;
        public LayerMask m_dofLayerDetection = 1;

        /// <summary>
        /// Create Gaia Lighting System Profile asset
        /// </summary>
#if UNITY_EDITOR
        [MenuItem("Assets/Create/Procedural Worlds/Gaia/Lighting Profile")]
        public static void CreateSkyProfiles()
        {
            GaiaLightingProfile asset = ScriptableObject.CreateInstance<GaiaLightingProfile>();
            AssetDatabase.CreateAsset(asset, "Assets/Gaia Lighting System Profile.asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
#endif
    }
}