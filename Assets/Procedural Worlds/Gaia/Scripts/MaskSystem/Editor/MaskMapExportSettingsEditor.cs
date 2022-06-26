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
    [CustomEditor(typeof(MaskMapExportSettings))]
    public class MaskMapExportSettingsEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private MaskMapExportSettings m_maskMapExportSettings;

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
#if GAIA_PRO_PRESENT
            m_editorUtils.Initialize(); // Do not remove this!
            m_maskMapExportSettings = (MaskMapExportSettings)target;

            string message = m_editorUtils.GetTextValue("Intro"); ;
            EditorGUILayout.HelpBox(message, MessageType.Info, true);
            if (m_editorUtils.Button("AddToScene"))
            {
                GameObject sessionTempObj = GaiaUtils.GetTempSessionToolsObject();
                GameObject maskMapExporterObj = new GameObject("Mask Map Exporter");
                maskMapExporterObj.transform.parent = sessionTempObj.transform;
                MaskMapExport maskMapExport = maskMapExporterObj.AddComponent<MaskMapExport>();
                maskMapExport.LoadSettings(m_maskMapExportSettings);
            }
            m_editorUtils.Heading("Stored Settings");
            DrawDefaultInspector();
            // Update is called once per frame
#else
            string message = m_editorUtils.GetTextValue("GaiaProInfo"); ;
            EditorGUILayout.HelpBox(message, MessageType.Info, true);
#endif

        }
    }
}
