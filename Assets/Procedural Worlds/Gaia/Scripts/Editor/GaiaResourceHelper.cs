using Gaia.Internal;
using PWCommon4;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
    [System.Serializable]
    public enum GaiaResourceHelperOperation { CopyResources, RemoveResources }

    /// <summary>
    /// Utility to copy or remove resources / prototypes from terrains in the scene
    /// </summary>

    class GaiaResourceHelper : EditorWindow, IPWEditor
    {

        private EditorUtils m_editorUtils;
        bool m_targetAllTerrains = true;
        public Enum m_operation = GaiaResourceHelperOperation.CopyResources;
        Terrain m_sourceTerrain;
        private bool m_layersSelected = true;
        private bool m_terrainTreesSelected = true;
        private bool m_terrainDetailsSelected = true;
        Terrain m_targetTerrain;
        private GaiaSettings m_settings;

        public bool PositionChecked { get => true; set => PositionChecked = value; }

        void OnEnable()
        {
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
            titleContent = m_editorUtils.GetContent("WindowTitle");
        }

        void OnGUI()
        {
            m_editorUtils.Initialize();

            m_editorUtils.Panel("ResourceHelper", DrawOperation, true);

         
        }

        private void DrawOperation(bool helpEnabled)
        {
            EditorGUILayout.BeginVertical();
            m_operation = m_editorUtils.EnumPopup("Operation", m_operation, helpEnabled);

            switch (m_operation)
            {
                case GaiaResourceHelperOperation.CopyResources:
                    DrawCopyOperation(helpEnabled);
                    break;
                case GaiaResourceHelperOperation.RemoveResources:
                    DrawRemoveOperation(helpEnabled);
                    break;
            }

            EditorGUILayout.EndVertical();

        }

        private void DrawRemoveOperation(bool helpEnabled)
        {
            m_targetAllTerrains = m_editorUtils.Toggle("DeleteAll", m_targetAllTerrains, helpEnabled);
            GUI.enabled = !m_targetAllTerrains;
            m_targetTerrain = (Terrain)m_editorUtils.ObjectField("TargetTerrain", m_targetTerrain, typeof(Terrain), true, helpEnabled);
            GUI.enabled = true;
            m_layersSelected = m_editorUtils.Toggle("TerrainLayers", m_layersSelected, helpEnabled);
            m_terrainTreesSelected = m_editorUtils.Toggle("TerrainTrees", m_terrainTreesSelected, helpEnabled);
            m_terrainDetailsSelected = m_editorUtils.Toggle("TerrainDetails", m_terrainDetailsSelected, helpEnabled);
            Color normalBGColor = GUI.backgroundColor;
            if (m_settings == null)
            {
                m_settings = GaiaUtils.GetGaiaSettings();
            }

            GUI.backgroundColor = m_settings.GetActionButtonColor();
            if (m_editorUtils.Button("StartRemoval"))
            {
                if (EditorUtility.DisplayDialog("Remove resources from target terrain(s)?", "This will remove the selected resources from to the source terrain to the given target terrains. This can heavily impact your scene, so please make a backup if you are not sure about this.", "Continue", "Cancel"))
                {
                    if (m_targetAllTerrains)
                    {
                        if (GaiaUtils.HasDynamicLoadedTerrains())
                        {
                            GaiaUtils.CallFunctionOnDynamicLoadedTerrains(RemoveResourcesFromTerrain, false);
                        }
                        foreach (Terrain t in Terrain.activeTerrains)
                        {
                            RemoveResourcesFromTerrain(t);
                        }
                        
                    }
                    else
                    {
                        RemoveResourcesFromTerrain(m_targetTerrain);
                    }
                }
            }
            GUI.backgroundColor = normalBGColor;

        }

        private void RemoveResourcesFromTerrain(Terrain t)
        {
            if (m_layersSelected)
            {
                t.terrainData.terrainLayers = new TerrainLayer[0];
            }
            if (m_terrainTreesSelected)
            {
                t.terrainData.treePrototypes = new TreePrototype[0];
            }
            if (m_terrainDetailsSelected)
            {
                t.terrainData.detailPrototypes = new DetailPrototype[0];
            }
            t.Flush();
        }

        private void DrawCopyOperation(bool helpEnabled)
        {
            m_sourceTerrain = (Terrain)m_editorUtils.ObjectField("SourceTerrain", m_sourceTerrain, typeof(Terrain), true, helpEnabled);
            m_layersSelected = m_editorUtils.Toggle("TerrainLayers", m_layersSelected, helpEnabled);
            m_terrainTreesSelected = m_editorUtils.Toggle("TerrainTrees", m_terrainTreesSelected, helpEnabled);
            m_terrainDetailsSelected = m_editorUtils.Toggle("TerrainDetails", m_terrainDetailsSelected, helpEnabled);
            m_targetAllTerrains = m_editorUtils.Toggle("CopyAll", m_targetAllTerrains, helpEnabled);
            GUI.enabled = !m_targetAllTerrains;
            m_targetTerrain = (Terrain)m_editorUtils.ObjectField("TargetTerrain", m_targetTerrain, typeof(Terrain), true, helpEnabled);
            GUI.enabled = true;
            Color normalBGColor = GUI.backgroundColor;
            if (m_settings == null)
            {
                m_settings = GaiaUtils.GetGaiaSettings();
            }

            GUI.backgroundColor = m_settings.GetActionButtonColor();
            if (m_editorUtils.Button("StartCopy"))
            {
                if (EditorUtility.DisplayDialog("Copy terrain resources to target?", "This will copy the selected resources from to the source terrain to the given target terrains. This can heavily impact your scene, so please make a backup if you are not sure about this.", "Continue", "Cancel"))
                {
                    if (m_targetAllTerrains)
                    {
                        if (GaiaUtils.HasDynamicLoadedTerrains())
                        {
                            GaiaUtils.CallFunctionOnDynamicLoadedTerrains(CopyResourcesToTerrain, false);
                        }
                        foreach (Terrain t in Terrain.activeTerrains)
                        {
                            CopyResourcesToTerrain(t);
                        }
                    }
                    else
                    {
                        CopyResourcesToTerrain(m_targetTerrain);
                    }
                }
            }
            GUI.backgroundColor = normalBGColor;
        }

        private void CopyResourcesToTerrain(Terrain t)
        {
            if (m_layersSelected)
            {
                t.terrainData.terrainLayers = m_sourceTerrain.terrainData.terrainLayers;
            }
            if (m_terrainTreesSelected)
            {
                t.terrainData.treePrototypes = m_sourceTerrain.terrainData.treePrototypes;
            }
            if (m_terrainDetailsSelected)
            {
                t.terrainData.detailPrototypes = m_sourceTerrain.terrainData.detailPrototypes;
            }
            t.Flush();
        }
    }
}