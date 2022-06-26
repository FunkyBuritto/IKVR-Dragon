using Gaia.Internal;
using PWCommon4;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gaia
{

    /// <summary>
    /// Editor for Spawner settings, only offers a text & a button to create the spawner in the scene
    /// If the user wants to edit or create new spawner settings, they can do so by saving a spawner settings file from a spawner directly.
    /// </summary>
    [CustomEditor(typeof(SpawnerSettings))]
    public class SpawnerSettingsEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private SpawnerSettings m_spawnerSettings;

        private void OnEnable()
        {
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
            m_spawnerSettings = (SpawnerSettings)target;

            string message = m_editorUtils.GetTextValue("Intro"); ;
            EditorGUILayout.HelpBox(message, MessageType.Info, true);

            if (GUILayout.Button(m_editorUtils.GetContent("AddToScene")))
            {
                m_spawnerSettings.CreateSpawner();
            }

            GUILayout.Label(m_editorUtils.GetContent("SpawnRulePreview"), m_editorUtils.Styles.heading);

            GaiaResource resource = m_spawnerSettings.m_resources;// (GaiaResource)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_spawnerSettings.m_resourcesGUID), typeof(GaiaResource));

            foreach (SpawnRule rule in m_spawnerSettings.m_spawnerRules)
            {
                m_editorUtils.LabelField("Name", new GUIContent(rule.m_name));
                m_editorUtils.LabelField("Type", new GUIContent(rule.m_resourceType.ToString()));


                Texture2D resourceTexture = SpawnerEditor.GetSpawnRulePreviewTexture(rule, resource);

                if (resourceTexture != null)
                {
                    m_editorUtils.Image(resourceTexture, 100, 100);
                }

            }

        }

    }
}
