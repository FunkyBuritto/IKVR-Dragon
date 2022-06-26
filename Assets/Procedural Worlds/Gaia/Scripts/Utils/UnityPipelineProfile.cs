using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Gaia.Pipeline
{
    public enum GaiaPackageVersion { Unity2019_1, Unity2019_2, Unity2019_3, Unity2019_4, Unity2020_1, Unity2020_2, Unity2020_3, Unity2020_4, Unity2021_1, Unity2021_2, Unity2021_3, Unity2021_4, Unity2022_1, Unity2022_2, Unity2022_3, Unity2022_4, Unity2023_1 }

    /// <summary>
    /// Mapping between Unity Version and Renderpipeline Asset Filename
    /// </summary>
    [System.Serializable]
    public class UnityVersionPipelineAsset
    {
        public GaiaPackageVersion m_unityVersion;
        public string m_pipelineAssetName;
        public string m_minURPVersion = "5.7.2";
        public string m_maxURPVersion = "5.13.0";
        public string m_minHDRPVersion = "5.7.2";
        public string m_maxHDRPVersion = "5.13.0";
    }

    [System.Serializable]
    public class GaiaPackageSettings
    {
        [Header("Global Settings")]
        public string m_version = "Add Unity Version... Example: 2019.1, 2019.2";
        public bool m_isSupported = true;
        public GaiaPackageVersion m_unityVersion = GaiaPackageVersion.Unity2019_1;
    }


    /// <summary>
    /// Data structure to configure different Shader configurations for different rendering pipelines. Each entry is based on a shader in the built-in pipeline and
    /// its replacement shader in the URP and HDRP pipeline. 
    /// </summary>
    [System.Serializable]
    public class ShaderMappingEntry
    {
        //public string m_name;
        //public bool m_supportUnitypackages = false;
        public string m_builtInShaderName;
        //public string m_builtInMaterialUnitypackage;
        public string m_URPReplacementShaderName;
        //public string m_URPMaterialUnitypackage;
        public string m_HDRPReplacementShaderName;
        //public string m_HDRPMaterialUnitypackage;
        //[HideInInspector]
        //public bool m_floatChecksFoldedOut = false;
        //public List<ShaderFloatCheck> m_floatChecks = new List<ShaderFloatCheck>();
        //[HideInInspector]
        //public bool m_materialsFoldedOut = false;
        //public List<Material> m_materials;
    }

    /// <summary>
    /// Data structure to configure keyword updates in a shader based on a float value in the material
    /// </summary>
    [System.Serializable]
    public class ShaderFloatCheck
    {
        public string m_floatValue;
        public string m_shaderKeyWord;
    }

    public class UnityPipelineProfile : ScriptableObject
    {
        [Header("Global Settings")]
        public GaiaConstants.EnvironmentRenderer m_activePipelineInstalled = GaiaConstants.EnvironmentRenderer.BuiltIn;
        public GaiaSettings m_gaiaSettings;
        [HideInInspector]
        public bool m_editorUpdates = false;
        [HideInInspector]
        public bool m_pipelineSwitchUpdates = false;

        [Header("Material Settings")]
        public Material m_underwaterHorizonMaterial;

        [Header("Built-In Render Pipeline")]
        public string m_builtInHorizonObjectShader = "Standard";
        public Material m_builtInTerrainMaterial;
        public bool m_BuiltInAutoConfigureWater = true;

        [Header("Universal Render Pipeline")]
        //public string m_universalPipelineProfile = "Procedural Worlds Universal Pipeline";
        public List<UnityVersionPipelineAsset> m_universalPipelineProfiles = new List<UnityVersionPipelineAsset>();
        public string m_universalScriptDefine = "UPPipeline";
        public string m_universalHorizonObjectShader = "Universal Render Pipeline/Lit";
        public Material m_universalTerrainMaterial;
        public bool m_setUPPipelineProfile = true;  
        public bool m_UPAutoConfigureTerrain = true; 
        public bool m_UPAutoConfigureWater = true;
        public bool m_UPAutoConfigureCamera = true;
        public bool m_UPAutoConfigureProbes = true;
        public bool m_UPAutoConfigureLighting = true;
        public bool m_UPAutoConfigureBiomePostFX = true;

        //[Header("Lightweight Render Pipeline")]
        //public string m_lightweightPipelineProfile = "Procedural Worlds Lightweight Pipeline Profile";
        [HideInInspector]
        public List<UnityVersionPipelineAsset> m_lightweightPipelineProfiles = new List<UnityVersionPipelineAsset>();
        [HideInInspector]
        public string m_lightweightScriptDefine = "LWPipeline";
        [HideInInspector]
        public string m_lightweightHorizonObjectShader = "Lightweight Render Pipeline/Lit";
        [HideInInspector]
        public Material m_lightweightTerrainMaterial;
        [HideInInspector]
        public bool m_setLWPipelineProfile = true;
        [HideInInspector]
        public bool m_LWAutoConfigureTerrain = true;
        [HideInInspector]
        public bool m_LWAutoConfigureWater = true;
        [HideInInspector]
        public bool m_LWAutoConfigureCamera = true;
        [HideInInspector]
        public bool m_LWAutoConfigureProbes = true;
        [HideInInspector]
        public bool m_LWAutoConfigureLighting = true;

        [Header("High Definition Render Pipeline")]
        //public string m_highDefinitionPipelineProfile = "Procedural Worlds HDRenderPipelineAsset";
        public List<UnityVersionPipelineAsset> m_highDefinitionPipelineProfiles = new List<UnityVersionPipelineAsset>();
        public string m_highDefinitionScriptDefine = "HDPipeline";
        public string m_highDefinitionHorizonObjectShader = "HDRP/Lit";
        public Material m_highDefinitionTerrainMaterial;
        public string m_HDVolumeObjectName = "HD Environment Volume";
        public string m_HDPostVolumeObjectName = "HD Post Processing Environment Volume";
        public string m_HDDefaultPostProcessing = "Default HDRP Post Processing Profile";
        public string m_HDDefaultSceneLighting = "Default Volume Profile";
        public string m_2019_3HDDefaultSceneLighting = "Default Volume Profile";
        public string m_HDSceneLighting = "HD Volume Profile";
        public string m_2019_3HDSceneLighting = "HD Volume Profile";
        public bool m_setHDPipelineProfile = true;
        public bool m_HDAutoConfigureTerrain = true;
        public bool m_HDAutoConfigureWater = true;
        public bool m_HDAutoConfigureCamera = true;
        public bool m_HDAutoConfigureProbes = true;
        public bool m_HDAutoConfigureLighting = true;
        public bool m_HDDisableTerrainDetails = true;
        public bool m_HDAutoConfigureBiomePostFX = true;

        [Header("Package Settings")]
        //Material Library entries will be drawn from the editor script since they use extra buttons etc.
        [HideInInspector]
        public ShaderMappingEntry[] m_ShaderMappingLibrary = new ShaderMappingEntry[0];
        //public Material[] m_vegetationMaterialLibrary;
        //public Material[] m_waterMaterialLibrary;
        public GaiaPackageSettings[] m_packageSetupSettings;

        /// <summary>
        /// Create Gaia Lighting Profile asset
        /// </summary>
#if UNITY_EDITOR
        [MenuItem("Assets/Create/Procedural Worlds/Gaia/Unity Pipeline Profile")]
        public static void CreateSkyProfiles()
        {
            UnityPipelineProfile asset = ScriptableObject.CreateInstance<UnityPipelineProfile>();
            AssetDatabase.CreateAsset(asset, "Assets/Unity Pipeline Profile.asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
#endif
    }
}