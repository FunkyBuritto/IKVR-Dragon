using System.Collections;
using System.Collections.Generic;
using Gaia.Internal;
using PWCommon4;
using UnityEngine;
using UnityEditor;

namespace Gaia
{
    [CustomEditor(typeof(DisableUnderwaterFXTrigger))]
    public class DisableUnderwaterFXTriggerEditor : PWEditor
    {
        private DisableUnderwaterFXTrigger m_profile;
        private EditorUtils m_editorUtils;

        private void OnEnable()
        {
            m_profile = (DisableUnderwaterFXTrigger) target;

            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
        }
        private void OnDestroy()
        {
            if (m_editorUtils != null)
            {
                m_editorUtils.Dispose();
            }
        }

        public override void OnInspectorGUI()
        {
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!

            if (m_profile == null)
            {
                m_profile = (DisableUnderwaterFXTrigger) target;
            }

            m_editorUtils.Panel("GlobalSettings", GlobalPanel, true);
        }

        private void GlobalPanel(bool helpEnabled)
        {
            EditorGUILayout.BeginHorizontal();
            m_profile.m_tagCheck = m_editorUtils.TextField("TagCheck", m_profile.m_tagCheck);
            if (m_editorUtils.Button("LoadTagFromGaia", GUILayout.MaxWidth(130f)))
            {
                m_profile.LoadTagFromGaia();
            }
            EditorGUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("TagCheck", helpEnabled);
        }
    }
}