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
    [CustomEditor(typeof(BiomePreset))]
    public class BiomePresetEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private BiomePreset m_biomePreset;
        private UnityEditorInternal.ReorderableList m_spawnerPresetList;

        private void OnEnable()
        {
            m_biomePreset = (BiomePreset)target;
            //Init editor utils
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
            m_biomePreset.RefreshSpawnerListEntries();
            CreateSpawnerPresetList();
        }

        public override void OnInspectorGUI()
        {
            m_editorUtils.Initialize(); // Do not remove this!
            m_biomePreset = (BiomePreset)target;
            serializedObject.Update();

            m_editorUtils.Panel("BiomeSettings", DrawBiomeSettings,true);

            if (GUILayout.Button(m_editorUtils.GetContent("AddToScene")))
            {
                BiomeController newBiome = m_biomePreset.CreateBiome(true);
                Selection.activeGameObject = newBiome.gameObject;
            }
            
        }

        private void DrawBiomeSettings(bool helpEnabled)
        {
            m_biomePreset.m_orderNumber = m_editorUtils.IntField("OrderNumber", m_biomePreset.m_orderNumber, helpEnabled);
            Rect listRect = EditorGUILayout.GetControlRect(true, m_spawnerPresetList.GetHeight());
            m_spawnerPresetList.DoList(listRect);
            m_editorUtils.InlineHelp("SpawnerAdded", helpEnabled);


            if (m_biomePreset.m_spawnerPresetList.Count == 0)
            {
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("NoSpawnersYet"), MessageType.Warning);
                GUILayout.Space(10);
                if (m_editorUtils.Button("CreateFirstSpawner"))
                {
                    GameObject spawnerObj = new GameObject("New Spawner");
                    Spawner spawner = spawnerObj.AddComponent<Spawner>();
                    spawner.m_createdfromBiomePreset = true;
                    Selection.activeGameObject = spawnerObj;
                }
                GUILayout.Space(20);
            }
            else
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (m_editorUtils.Button("CreateAdditionalSpawner",GUILayout.MaxWidth(200)))
                {
                    GameObject spawnerObj = new GameObject("New Spawner");
                    Spawner spawner = spawnerObj.AddComponent<Spawner>();
                    spawner.m_createdfromBiomePreset = true;
                    Selection.activeGameObject = spawnerObj;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            m_biomePreset.GaiaSceneCullingProfile = (GaiaSceneCullingProfile)m_editorUtils.ObjectField("GaiaSceneCullingProfile", m_biomePreset.GaiaSceneCullingProfile, typeof(GaiaSceneCullingProfile), false, helpEnabled);

#if UNITY_POST_PROCESSING_STACK_V2
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            m_biomePreset.postProcessProfile = (PostProcessProfile)m_editorUtils.ObjectField("PostProcessingProfile", m_biomePreset.postProcessProfile, typeof(PostProcessProfile), false, helpEnabled);
#endif
            GUILayout.Space(EditorGUIUtility.singleLineHeight);


            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_biomePreset);
        }

        #region Spawner Preset List

        void CreateSpawnerPresetList()
        {
            m_spawnerPresetList = new UnityEditorInternal.ReorderableList(m_biomePreset.m_spawnerPresetList, typeof(BiomeSpawnerListEntry), true, true, true, true);
            m_spawnerPresetList.elementHeightCallback = OnElementHeightSpawnerPresetListEntry;
            m_spawnerPresetList.drawElementCallback = DrawSpawnerPresetListElement;
            m_spawnerPresetList.drawHeaderCallback = DrawSpawnerPresetListHeader;
            m_spawnerPresetList.onAddCallback = OnAddSpawnerPresetListEntry;
            m_spawnerPresetList.onRemoveCallback = OnRemoveSpawnerPresetListEntry;
            m_spawnerPresetList.onReorderCallback = OnReorderSpawnerPresetList;
        }

        private void OnReorderSpawnerPresetList(ReorderableList list)
        {
            //Do nothing, changing the order does not immediately affect anything in this window
        }

        private void OnRemoveSpawnerPresetListEntry(ReorderableList list)
        {
            m_biomePreset.m_spawnerPresetList = SpawnerPresetListEditor.OnRemoveListEntry(m_biomePreset.m_spawnerPresetList, m_spawnerPresetList.index);
            list.list = m_biomePreset.m_spawnerPresetList;
        }

        private void OnAddSpawnerPresetListEntry(ReorderableList list)
        {
            m_biomePreset.m_spawnerPresetList = SpawnerPresetListEditor.OnAddListEntry(m_biomePreset.m_spawnerPresetList);
            list.list = m_biomePreset.m_spawnerPresetList;
        }

        private void DrawSpawnerPresetListHeader(Rect rect)
        {
            SpawnerPresetListEditor.DrawListHeader(rect, true, m_biomePreset.m_spawnerPresetList, m_editorUtils, "SpawnerAdded");
        }

        private void DrawSpawnerPresetListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SpawnerPresetListEditor.DrawListElement(rect, m_biomePreset.m_spawnerPresetList[index], m_editorUtils);
        }

        private float OnElementHeightSpawnerPresetListEntry(int index)
        {
            return SpawnerPresetListEditor.OnElementHeight();
        }



        #endregion

    }
}
