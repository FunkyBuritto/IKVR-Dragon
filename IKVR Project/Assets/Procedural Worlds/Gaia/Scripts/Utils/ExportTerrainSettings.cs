using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gaia
{
    public enum ExportPreset { ExportToOBJFiles, ConvertToMesh, CreateImpostors, ConvertToMeshAndImpostors, ConvertToLowPolyMesh, ConvertToLowPolyMeshAndImpostors, Custom}
    public enum ExportSelection { AllTerrains, SingleTerrainOnly }
    public enum ConversionAction { MeshTerrain, ColliderOnly, OBJFileExport }
    public enum LODSettingsMode { Impostor, LowPoly, Custom }
    public enum SaveFormat { Triangles, Quads }
    public enum SaveResolution { Full = 0, Half, Quarter, Eighth, Sixteenth }
    public enum NormalEdgeMode { Smooth, Sharp }
    public enum TextureExportMethod { OrthographicBake, BaseMapExport }
    public enum BakeLighting { NeutralLighting, CurrentSceneLighting }
    public enum AddAlphaChannel { None, Heightmap }
    public enum ExportedTerrainShader { Standard, VertexColor }
    public enum TextureExportResolution { x32 = 32, x64 = 64, x128 = 128, x256 = 256, x512 = 512, x1024 = 1024, x2048 = 2048, x4096 = 4096, x8192 = 8192 }
    public enum SourceTerrainTreatment { Nothing, Deactivate, StoreInBackupScenes, Delete }


    [System.Serializable]
    public class ExportTerrainLODSettings
    {
        public SaveResolution m_saveResolution = SaveResolution.Half;
        public NormalEdgeMode m_normalEdgeMode = NormalEdgeMode.Smooth;
        public LODSettingsMode m_LODSettingsMode = LODSettingsMode.Impostor;
        public bool m_settingsFoldedOut = false;
        public bool m_exportTextures = true;
        public bool m_exportNormalMaps = true;
        public bool m_exportSplatmaps = true;
        public bool m_createMaterials = true;
        public ExportedTerrainShader m_materialShader = ExportedTerrainShader.Standard;
        public bool m_bakeVertexColors = true;
        public int m_VertexColorSmoothing = 3;
        public LayerMask m_bakeLayerMask = ~0; //equals "Everything"
        public TextureExportMethod m_textureExportMethod = TextureExportMethod.OrthographicBake;
        public AddAlphaChannel m_addAlphaChannel = AddAlphaChannel.Heightmap;
        public TextureExportResolution m_textureExportResolution = TextureExportResolution.x2048;
        public BakeLighting m_bakeLighting = BakeLighting.NeutralLighting;
        public string namePrefix;
        public bool m_captureBaseMapTextures = false;
        public float m_LODGroupScreenRelativeTransitionHeight = 0.8f;

        public bool CompareTo(ExportTerrainLODSettings compareToLOD)
        {
            if (m_saveResolution != compareToLOD.m_saveResolution ||
        m_normalEdgeMode != compareToLOD.m_normalEdgeMode ||
        m_LODSettingsMode != compareToLOD.m_LODSettingsMode ||
        m_exportTextures != compareToLOD.m_exportTextures ||
        m_exportNormalMaps != compareToLOD.m_exportNormalMaps ||
        m_exportSplatmaps != compareToLOD.m_exportSplatmaps ||
        m_createMaterials != compareToLOD.m_createMaterials ||
        m_materialShader != compareToLOD.m_materialShader ||
        m_bakeVertexColors != compareToLOD.m_bakeVertexColors ||
        m_VertexColorSmoothing != compareToLOD.m_VertexColorSmoothing ||
        m_bakeLayerMask != compareToLOD.m_bakeLayerMask ||
        m_textureExportMethod != compareToLOD.m_textureExportMethod ||
        m_addAlphaChannel != compareToLOD.m_addAlphaChannel ||
        m_textureExportResolution != compareToLOD.m_textureExportResolution ||
        m_bakeLighting != compareToLOD.m_bakeLighting ||
        m_captureBaseMapTextures != compareToLOD.m_captureBaseMapTextures)
            {
                return false;
            }

            return true;
        }
    }


    [System.Serializable]
    public class ExportTerrainSettings : ScriptableObject, ISerializationCallbackReceiver
    {
        //public bool m_deactivateOriginalTerrains = true;
        public SourceTerrainTreatment m_sourceTerrainTreatment = SourceTerrainTreatment.Deactivate;
        public SaveFormat m_saveFormat = SaveFormat.Triangles;
        public bool m_addMeshCollider = true;
        public bool m_addMeshColliderImpostor = true;
        public ExportSelection m_exportSelection = ExportSelection.AllTerrains;
        public Texture2D m_terrainExportMask;
        public Gaia.GaiaConstants.ImageChannel m_terrainExportMaskChannel = GaiaConstants.ImageChannel.R;
        public bool m_terrainExportInvertMask = false;
        public bool m_copyGaiaGameObjects = true;
        public bool m_copyGaiaGameObjectsImpostor = false;
        public bool m_convertSourceTerrains = false;
        public ConversionAction m_convertSourceTerrainsAction = ConversionAction.MeshTerrain;
        public SaveResolution m_colliderExportResolution = SaveResolution.Full;
        public bool m_colliderExportAddTreeColliders = true;
        public bool m_colliderExportAddGameObjectColliders = true;
        public bool m_colliderExportCreateColliderScenes = true;
        public bool m_colliderExportBakeCombinedCollisionMesh = true;
        public bool m_createImpostorScenes = false;
        public List<ExportTerrainLODSettings> m_exportTerrainLODSettingsSourceTerrains = new List<ExportTerrainLODSettings>();
        public List<ExportTerrainLODSettings> m_exportTerrainLODSettingsImpostors = new List<ExportTerrainLODSettings>();
        public string m_exportPath;
        //public LODSettingsMode m_exportPreset = LODSettingsMode.Impostor;
        public ExportPreset m_newExportPreset = ExportPreset.ConvertToMesh;
        public bool m_customSettingsFoldedOut;
        public int m_presetIndex = -99;
        public string m_lastUsedPresetName = "";
        public Mesh m_colliderTreeReplacement = null;

        public bool CompareTo(ExportTerrainSettings compareSettings)
        {
            if (m_saveFormat != compareSettings.m_saveFormat ||
            m_addMeshCollider != compareSettings.m_addMeshCollider ||
            m_addMeshColliderImpostor != compareSettings.m_addMeshColliderImpostor ||
            m_terrainExportMask != compareSettings.m_terrainExportMask ||
            m_terrainExportMaskChannel != compareSettings.m_terrainExportMaskChannel ||
            m_terrainExportInvertMask != compareSettings.m_terrainExportInvertMask ||
            m_convertSourceTerrainsAction != compareSettings.m_convertSourceTerrainsAction ||
            m_colliderExportResolution != compareSettings.m_colliderExportResolution ||
            m_colliderExportCreateColliderScenes != compareSettings.m_colliderExportCreateColliderScenes ||
            m_colliderExportAddTreeColliders != compareSettings.m_colliderExportAddTreeColliders ||
            m_colliderExportAddGameObjectColliders != compareSettings.m_colliderExportAddGameObjectColliders ||
            m_copyGaiaGameObjects != compareSettings.m_copyGaiaGameObjects ||
            m_copyGaiaGameObjectsImpostor != compareSettings.m_copyGaiaGameObjectsImpostor ||
            m_convertSourceTerrains != compareSettings.m_convertSourceTerrains ||
            m_createImpostorScenes != compareSettings.m_createImpostorScenes)
                return false;

            if (m_exportTerrainLODSettingsSourceTerrains.Count != compareSettings.m_exportTerrainLODSettingsSourceTerrains.Count)
                return false;
            if (m_exportTerrainLODSettingsImpostors.Count != compareSettings.m_exportTerrainLODSettingsImpostors.Count)
                return false;

            for (int i = 0; i < m_exportTerrainLODSettingsSourceTerrains.Count; i++)
            {
                if (!m_exportTerrainLODSettingsSourceTerrains[i].CompareTo(compareSettings.m_exportTerrainLODSettingsSourceTerrains[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < m_exportTerrainLODSettingsSourceTerrains.Count; i++)
            {
                if (!m_exportTerrainLODSettingsSourceTerrains[i].CompareTo(compareSettings.m_exportTerrainLODSettingsSourceTerrains[i]))
                {
                    return false;
                }
            }

            return true;

        }


        #region Serialization

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
        }
        #endregion

        
    }
}