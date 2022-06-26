using Gaia.Internal;
using PWCommon4;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Gaia
{

    /// <summary>
    /// Editor for Biome Preset settings, only offers a text & a button to create the spawner in the scene
    /// If the user wants to edit or create new spawner settings, they can do so by saving a spawner settings file from a spawner directly.
    /// </summary>
    [CustomEditor(typeof(WorldMap))]
    public class WorldMapEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        WorldMap m_worldMap;


        private void OnEnable()
        {
            m_worldMap = (WorldMap)target;
            //Init editor utils
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
        }

        public override void OnInspectorGUI()
        {
            m_editorUtils.Initialize(); // Do not remove this!
            m_worldMap = (WorldMap)target;
            serializedObject.Update();

            if (m_worldMap.m_worldMapTerrain == null)
            {
                m_editorUtils.Panel("CreateWorldMap", CreateWorldMap, true);

                
            }
            else
            {
                m_editorUtils.Panel("CreateWorldMap", CreateWorldMap, false);
                m_editorUtils.Panel("EditWorldMap", DrawEditWorldMap, true);
                m_editorUtils.Panel("SyncHeightmap", DrawSyncHeightmap, true);
            }



        }

        private void CreateWorldMap(bool obj)
        {
            if (m_worldMap.m_worldMapTerrain == null)
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("TerrainMissing"), MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("CreateTerrainInfo"), MessageType.Info);
            }
            m_worldMap.m_worldMapTileSize = (GaiaConstants.EnvironmentSize)m_editorUtils.EnumPopup("TileSize", m_worldMap.m_worldMapTileSize);
            m_worldMap.m_heightmapResolution = (GaiaConstants.HeightmapResolution)m_editorUtils.EnumPopup("HeightmapResolution", m_worldMap.m_heightmapResolution);
            if (m_editorUtils.ButtonAutoIndent("CreateTerrainButton"))
            {
                m_worldMap.CreateWorldMapTerrain();
                m_worldMap.LookAtWorldMap();
            }
            return;
        }

        private void DrawEditWorldMap(bool obj)
        {
            if (m_editorUtils.Button("RandomTerrainGeneration"))
            {
                WorldMap.ShowWorldMapStampSpawner();
            }
            //if (m_editorUtils.Button("DefineWorldMapMasks"))
            //{
            //    WorldMap.ShowWorldBiomeMasksSpawner();
            //}
            if (m_editorUtils.Button("StampWorldMap"))
            {
                GameObject stamperObj = WorldMap.GetOrCreateWorldMapStamper();
#if UNITY_EDITOR
                Selection.activeGameObject = stamperObj;
#endif
            }

        }


        private void DrawSyncHeightmap(bool obj)
        {
            GUILayout.BeginVertical();
            if (m_editorUtils.Button("SyncWorldMapToTerrain"))
            {
                m_worldMap.SyncWorldMapToLocalMap();
            }
            if (m_editorUtils.Button("SyncTerrainToWorldMap"))
            {
                m_worldMap.SyncLocalMapToWorldMap();
            }
            GUILayout.EndVertical();
        }
    }
}

