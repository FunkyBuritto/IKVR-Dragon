
using System.Collections;
using System.Collections.Generic;
using Gaia.Internal;
using PWCommon4;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
    [CustomEditor(typeof(ReflectionMasker))]
    public class ReflectionMaskerEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private ReflectionMasker m_profile;
        private Color GUIBackground;

        private void OnEnable()
        {
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
        }

        /// <summary>
        /// Setup on destroy
        /// </summary>
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
                m_profile = (ReflectionMasker) target;
            }

            m_editorUtils.Panel("GlobalSettings", GlobalSettings, true);
        }

        private void GlobalSettings(bool helpEnabled)
        {
            EditorGUI.BeginChangeCheck();
            GUIBackground = GUI.backgroundColor;
            m_profile.Player = (Transform)m_editorUtils.ObjectField("Player", m_profile.Player, typeof(Transform), true, helpEnabled, GUILayout.MaxHeight(16f));
            if (m_profile.Player == null)
            {
                m_profile.Player = m_profile.GetPlayer();
            }
            if (m_profile.ReflectionMaskerData.m_maskingTexture == null)
            {
                EditorGUILayout.HelpBox("The mask texture is null. This system needs a mask texture to function please provide one.", MessageType.Error);
                GUI.backgroundColor = Color.red;
            }
            else
            {
                if (!m_profile.ReflectionMaskerData.m_maskingTexture.isReadable)
                {
                    EditorGUILayout.HelpBox("The mask texture " + m_profile.ReflectionMaskerData.m_maskingTexture.name + " is not Read/Write enabled please go to the texture import settings and enable this setting then click apply.", MessageType.Error);
                    GUI.backgroundColor = Color.red;
                }
            }
            m_profile.ReflectionMaskerData.m_maskingTexture = (Texture2D)m_editorUtils.ObjectField("MaskTexture", m_profile.ReflectionMaskerData.m_maskingTexture, typeof(Texture2D), false, helpEnabled, GUILayout.MaxHeight(16f));
            GUI.backgroundColor = GUIBackground;
            EditorGUI.indentLevel++;
            m_profile.ReflectionMaskerData.m_channelSelection = (ReflectionMaskerChannelSelection)m_editorUtils.EnumPopup("ChannelSelection", m_profile.ReflectionMaskerData.m_channelSelection, helpEnabled);
            m_profile.ReflectionMaskerData.m_minValue = m_editorUtils.Slider("MinValue",m_profile.ReflectionMaskerData.m_minValue, 0f, m_profile.ReflectionMaskerData.m_maxValue - 0.01f, helpEnabled);
            m_profile.ReflectionMaskerData.m_maxValue = m_editorUtils.Slider("MaxValue",m_profile.ReflectionMaskerData.m_maxValue, m_profile.ReflectionMaskerData.m_minValue + 0.01f, 1f, helpEnabled);
            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(m_profile);
            }
        }
    }
}