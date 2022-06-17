using UnityEngine;
using Gaia.Internal;
using UnityEditor;
using PWCommon4;

namespace Gaia
{
    [CustomEditor(typeof(WindManager))]
    public class WindManagerEditor : PWEditor
    {
        public EditorUtils m_editorUtils;
        private WindManager m_manager;

        private void OnEnable()
        {
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
        }
        public override void OnInspectorGUI()
        {
            m_editorUtils.Initialize();
            if (m_manager == null)
            {
                m_manager = (WindManager)target;
            }

            m_editorUtils.Panel("GlobalSettings", GlobalPanel, true);
        }
        private void GlobalPanel(bool helpEnabled)
        {
            EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("WindManagerInfo"), MessageType.Info);
            m_manager.windGlobalMaxDist = m_editorUtils.Slider("GlobalWindDistance", m_manager.windGlobalMaxDist, 100f, 10000f, helpEnabled);
            EditorGUILayout.Space();

            m_editorUtils.Heading("WindSettings");
            m_manager.m_useWindAudio = m_editorUtils.Toggle("UseWindAudio", m_manager.m_useWindAudio, helpEnabled);
            if (m_manager.m_useWindAudio)
            {
                EditorGUI.indentLevel++;
                m_manager.m_windTransitionTime = m_editorUtils.FloatField("WindTransitionTime", m_manager.m_windTransitionTime, helpEnabled);
                if (m_manager.m_windTransitionTime < 0.1f)
                {
                    m_manager.m_windTransitionTime = 0.1f;
                }
                m_manager.m_windAudioClip = (AudioClip)m_editorUtils.ObjectField("WindAudioClip", m_manager.m_windAudioClip, typeof(AudioClip), false, helpEnabled);
                EditorGUI.indentLevel--;
            }
        }
    }
}