using Gaia.Internal;
using PWCommon4;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace Gaia
{

    /// <summary>
    /// Editor for Biome Preset settings, only offers a text & a button to create the spawner in the scene
    /// If the user wants to edit or create new spawner settings, they can do so by saving a spawner settings file from a spawner directly.
    /// </summary>
    [CustomEditor(typeof(UserFiles))]
    public class UserFilesEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private UserFiles m_userFiles;
        private UnityEditorInternal.ReorderableList m_spawnerPresetList;

        private void OnEnable()
        {
            m_userFiles = (UserFiles)target;
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
            m_editorUtils.Panel("UserFiles",DrawUserFiles,true);
            
               
        }

        private void DrawUserFiles(bool helpEnabled)
        {
            m_userFiles.m_autoAddNewFiles = m_editorUtils.Toggle("AutoAddNewFiles", m_userFiles.m_autoAddNewFiles, helpEnabled);
            m_userFiles.m_updateFilesWithGaiaUpdate = m_editorUtils.Toggle("UpdateWithGaia", m_userFiles.m_updateFilesWithGaiaUpdate, helpEnabled);
            GUILayout.Space(10);
            if (m_editorUtils.Button("AddDefaults"))
            {
                if (EditorUtility.DisplayDialog("Add Gaia Default Biomes?", "This will add the default Biomes & Spawners of Gaia back to the lists in case they are missing. Continue?", "Add Defaults", "Cancel"))
                {
                    GaiaUtils.ResetBiomePresets(true);
                }
            }
            GUILayout.Space(10);
            m_editorUtils.Heading("BiomePresetHeading");
            m_editorUtils.InlineHelp("BiomePresetHeading", helpEnabled);

            float deleteButtonWidth = 50;

            for (int i = 0; i < m_userFiles.m_gaiaManagerBiomePresets.Count; i++)
            {
                GUILayout.BeginHorizontal();
                {
                    m_userFiles.m_gaiaManagerBiomePresets[i] = (BiomePreset)EditorGUILayout.ObjectField(m_userFiles.m_gaiaManagerBiomePresets[i], typeof(BiomePreset), false);
                    if (m_editorUtils.Button("DeleteBiomePreset", GUILayout.Width(deleteButtonWidth)))
                    {
                        m_userFiles.m_gaiaManagerBiomePresets.RemoveAt(i);
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                if (m_editorUtils.Button("AddBiomePreset"))
                {
                    m_userFiles.m_gaiaManagerBiomePresets.Add(null);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            m_editorUtils.Heading("SpawnerSettingsHeading");
            m_editorUtils.InlineHelp("SpawnerSettingsHeading", helpEnabled);
            for (int i = 0; i < m_userFiles.m_gaiaManagerSpawnerSettings.Count; i++)
            {
                GUILayout.BeginHorizontal();
                {
                    m_userFiles.m_gaiaManagerSpawnerSettings[i] = (SpawnerSettings)EditorGUILayout.ObjectField(m_userFiles.m_gaiaManagerSpawnerSettings[i], typeof(SpawnerSettings), false);
                    if (m_editorUtils.Button("DeleteSpawnerSettings", GUILayout.Width(deleteButtonWidth)))
                    {
                        m_userFiles.m_gaiaManagerSpawnerSettings.RemoveAt(i);
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                if (m_editorUtils.Button("AddSpawnerSettings"))
                {
                    m_userFiles.m_gaiaManagerSpawnerSettings.Add(null);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            m_editorUtils.Heading("ExportTerrainSettingsHeading");
            m_editorUtils.InlineHelp("ExportTerrainSettingsHeading", helpEnabled);
            for (int i = 0; i < m_userFiles.m_exportTerrainSettings.Count; i++)
            {
                GUILayout.BeginHorizontal();
                {
                    m_userFiles.m_exportTerrainSettings[i] = (ExportTerrainSettings)EditorGUILayout.ObjectField(m_userFiles.m_exportTerrainSettings[i], typeof(ExportTerrainSettings), false);
                    if (m_editorUtils.Button("DeleteExportTerrainSettings", GUILayout.Width(deleteButtonWidth)))
                    {
                        m_userFiles.m_exportTerrainSettings.RemoveAt(i);
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                if (m_editorUtils.Button("AddExportTerrainSettings"))
                {
                    m_userFiles.m_exportTerrainSettings.Add(null);
                }
            }
            GUILayout.EndHorizontal();
        }
    }
         
}
