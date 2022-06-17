using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using PWCommon4;
using Gaia.Internal;
using Gaia;

namespace ProceduralWorlds.WaterSystem
{
    /// <summary>
    /// Editor for the PWS_WaterReflections
    /// </summary>
    [CustomEditor(typeof(PWS_WaterSystem))]
    public class PWS_WaterReflectionsEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private PWS_WaterSystem WaterReflections;

        private void OnEnable()
        {
            WaterReflections = (PWS_WaterSystem)target;

            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
        }

        #region Inspector Region

        /// <summary>
        /// Custom editor for PWS_WaterReflections
        /// </summary
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!

            if (WaterReflections == null)
            {
                WaterReflections = (PWS_WaterSystem)target;
            }

            m_editorUtils.Panel("GlobalSettings", GlobalSettings, true);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(WaterReflections);
            }
        }

        #endregion

        #region Panel

        /// <summary>
        /// Global Main Panel
        /// </summary>
        /// <param name="helpEnabled"></param>
        private void GlobalSettings(bool helpEnabled)
        {
            m_editorUtils.Heading("Setup");
            EditorGUI.indentLevel++;
            WaterReflections.SunLight = (Light)m_editorUtils.ObjectField("SunLight", WaterReflections.SunLight, typeof(Light), true, helpEnabled);
            WaterReflections.m_player = (Transform)m_editorUtils.ObjectField("Player", WaterReflections.m_player, typeof(Transform), true, helpEnabled);
            WaterReflections.SeaLevel = m_editorUtils.FloatField("SeaLevel", WaterReflections.SeaLevel, helpEnabled);
            WaterReflections.InfiniteMode = m_editorUtils.Toggle("InfiniteMode", WaterReflections.InfiniteMode, helpEnabled);
            EditorGUI.indentLevel--;

            /*m_editorUtils.Heading("SurfaceSettings");
            EditorGUI.indentLevel++;
            WaterReflections.m_minSurfaceLight = m_editorUtils.FloatField("MinSurfaceLight", WaterReflections.m_minSurfaceLight, helpEnabled);
            if (WaterReflections.m_minSurfaceLight > WaterReflections.m_maxSurfaceLight)
            {
                WaterReflections.m_minSurfaceLight = WaterReflections.m_maxSurfaceLight - 0.1f;
            }

            if (WaterReflections.m_minSurfaceLight < 0f)
            {
                WaterReflections.m_minSurfaceLight = 0f;
            }
            WaterReflections.m_maxSurfaceLight = m_editorUtils.FloatField("MaxSurfaceLight", WaterReflections.m_maxSurfaceLight, helpEnabled);
            if (WaterReflections.m_maxSurfaceLight < 0.1f)
            {
                WaterReflections.m_maxSurfaceLight = 0.1f;
            }
            EditorGUI.indentLevel--;*/

            if (m_editorUtils.Button("EditReflectionSettings"))
            {
                GaiaUtils.FocusWaterProfile();
            }
        }

        #endregion
    }
}
