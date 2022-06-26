using System.Collections;
using System.Collections.Generic;
using Gaia.Internal;
using PWCommon4;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
    [CustomEditor(typeof(PWSkyPostProcessingManager))]
    public class PWSkyPostProcessingManagerEditor : PWEditor
    {
        private PWSkyPostProcessingManager m_profile;
        private EditorUtils m_editorUtils;
        private GaiaConstants.EnvironmentRenderer m_renderpipeline;
        private const string m_builtInText = "Post processing settigns are modified in the Gaia PW Sky Weather and Time Of Day system. This will change most of the common settings such as.";
        private const string m_srpText = "";

        private void OnEnable()
        {
            m_renderpipeline = GaiaUtils.GetActivePipeline();
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
        }

        public override void OnInspectorGUI()
        {
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!
            m_profile = (PWSkyPostProcessingManager) target;

            m_editorUtils.Panel("GlobalSettings", GlobalSettings, true);
        }

        private void GlobalSettings(bool helpEnabled)
        {
#if GAIA_PRO_PRESENT
            if (m_renderpipeline == GaiaConstants.EnvironmentRenderer.BuiltIn)
            {
                //EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("BuiltInHelpText"), MessageType.Info);
                m_editorUtils.TextNonLocalized(m_editorUtils.GetTextValue("BuiltInHelpText"));
                if (ProceduralWorldsGlobalWeather.Instance != null)
                {
                    if (ProceduralWorldsGlobalWeather.Instance.m_modifyPostProcessing)
                    {
                        //if (GUILayout.Button(new GUIContent("Disable Post FX In Weather", "Disables the post processing fx to be used/synced in the PW Sky weather and time of day system")))
                        if (m_editorUtils.Button("DisablePostFXInWeather"))
                        {
                            m_profile.DisableWeatherPostFX();
                        }
                    }
                    else
                    {
                        //if (GUILayout.Button(new GUIContent("Enable Post FX In Weather", "Enables the post processing fx to be used/synced in the PW Sky weather and time of day system")))
                        if (m_editorUtils.Button("EnablePostFXInWeather"))
                        {
                            m_profile.EnableWeatherPostFX();
                        }
                    }
                }
            }
            else
            {
                //EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("SRPHelpText"), MessageType.Info);
                m_editorUtils.TextNonLocalized(m_editorUtils.GetTextValue("SRPHelpText"));
            }
#endif
        }
    }
}