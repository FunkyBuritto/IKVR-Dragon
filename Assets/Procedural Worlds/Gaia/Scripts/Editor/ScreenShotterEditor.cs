using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Gaia.Internal;
using PWCommon4;

namespace Gaia
{
    [CustomEditor(typeof(ScreenShotter))]
    public class ScreenShotterEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private ScreenShotter m_profile;
        
        private void OnEnable()
        {
            //Get Gaia Lighting Profile object
            m_profile = (ScreenShotter)target;

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

            m_editorUtils.Panel("GlobalSettings", GlobalSettings, false, true, true);
        }

        private void GlobalSettings(bool helpEnabled)
        {
            m_editorUtils.Heading("ScreenshotSetup");
            EditorGUI.indentLevel++;
            m_profile.m_mainCamera = (Camera)m_editorUtils.ObjectField("MainCamera", m_profile.m_mainCamera, typeof(Camera), true, helpEnabled);
            m_profile.m_watermark = (Texture2D)m_editorUtils.ObjectField("Watermark", m_profile.m_watermark, typeof(Texture2D), true, helpEnabled, GUILayout.MaxHeight(16f));
            m_profile.m_targetDirectory = m_editorUtils.TextField("TargetDirectory", m_profile.m_targetDirectory, helpEnabled);
            m_profile.m_screenShotKey = (KeyCode)m_editorUtils.EnumPopup("ScreenshotKey", m_profile.m_screenShotKey, helpEnabled);
            m_profile.m_imageFormat = (GaiaConstants.ImageFileType)m_editorUtils.EnumPopup("ImageFormat", m_profile.m_imageFormat, helpEnabled);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            m_editorUtils.Heading("ScreenshotResolutionSetup");
            EditorGUI.indentLevel++;
            m_profile.m_useScreenSize = m_editorUtils.Toggle("UseScreenSize", m_profile.m_useScreenSize, helpEnabled);
            if (!m_profile.m_useScreenSize)
            {
                GaiaConstants.ScreenshotResolution screenshotResolution = m_profile.m_screenshotResolution;
                screenshotResolution = (GaiaConstants.ScreenshotResolution)m_editorUtils.EnumPopup("ScreenshotResolution", screenshotResolution, helpEnabled);
                if (screenshotResolution != m_profile.m_screenshotResolution)
                {
                    m_profile.m_screenshotResolution = screenshotResolution;
                    m_profile.UpdateScreenshotResolution(m_profile.m_screenshotResolution);
                }

                if (screenshotResolution == GaiaConstants.ScreenshotResolution.Custom)
                {
                    EditorGUI.indentLevel++;
                    m_profile.m_targetWidth = m_editorUtils.IntField("TargetWidth", m_profile.m_targetWidth, helpEnabled);
                    m_profile.m_targetHeight = m_editorUtils.IntField("TargetHeight", m_profile.m_targetHeight, helpEnabled);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;
        }
    }
}