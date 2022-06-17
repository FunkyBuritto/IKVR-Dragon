using Gaia.Internal;
using PWCommon4;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Gaia
{
    public enum GaiaStopwatchOrderBy { FirstStart,Name,TotalDuration }
    

    [CustomEditor(typeof(GaiaStopwatchControl))]
    public class GaiaStopwatchControlEditor : PWEditor, IPWEditor
    {
        private GaiaStopwatchControl m_gaiastopwatchControl;
        private EditorUtils m_editorUtils;
       
        public void OnEnable()
        {
            m_gaiastopwatchControl = (GaiaStopwatchControl)target;
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
            m_gaiastopwatchControl.transform.name = GaiaConstants.gaiaStopWatchDataObject;
        }

        public override void OnInspectorGUI()
        {
            m_editorUtils.Initialize(); // Do not remove this!

#if GAIA_DEBUG
            if (m_editorUtils.Button("DisableStopwatch"))
            { 
             bool isChanged = false;
                string currBuildSettings = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                 if (currBuildSettings.Contains("GAIA_DEBUG"))
                    {
                        currBuildSettings = currBuildSettings.Replace("GAIA_DEBUG;", "");
                        currBuildSettings = currBuildSettings.Replace("GAIA_DEBUG", "");
                        isChanged = true;
                    }
                if (isChanged)
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, currBuildSettings);
                }
            }
            if (!GaiaStopwatch.m_isEnabled)
            {
                if (m_editorUtils.Button("StartStopwatch"))
                {
                    GaiaStopwatch.m_isEnabled = true;
                }
            }
            else
            {
                if (m_editorUtils.Button("StopStopwatch"))
                {
                    GaiaStopwatch.Stop();
                }
            }
#else
            if (m_editorUtils.Button("EnableStopwatch"))
            {
                bool isChanged = false;
                string currBuildSettings = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                if (!currBuildSettings.Contains("GAIA_DEBUG"))
                {
                    if (string.IsNullOrEmpty(currBuildSettings))
                    {
                        currBuildSettings = "GAIA_DEBUG";
                    }
                    else
                    {
                        currBuildSettings += ";GAIA_DEBUG";
                    }
                    isChanged = true;
                }
                if (isChanged)
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, currBuildSettings);
                }
            }

            GUI.enabled = false;
            m_editorUtils.Button("StartStopwatch");
#endif

        }
    }
}