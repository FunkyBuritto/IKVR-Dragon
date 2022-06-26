using Gaia.Internal;
using UnityEngine;
using UnityEditor;
using PWCommon4;
using UnityEngine.Rendering;

namespace Gaia
{
    [CustomEditor(typeof(CommandBufferManager))]
    public class CommandBufferManagerEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private CommandBufferManager m_bufferManager;

        //Variables
        private bool HDR;
        private PW_RENDER_SIZE RenderSize;
        private CameraEvent RefractionCameraEvent;

        private void OnEnable()
        {
            if (m_editorUtils == null)
            {
                m_editorUtils = PWApp.GetEditorUtils(this, null, null, null);
            }

            m_bufferManager = (CommandBufferManager)target;
        }

        public override void OnInspectorGUI()
        {
            m_editorUtils.Initialize();
            if (m_bufferManager == null)
            {
                m_bufferManager = (CommandBufferManager)target;
            }

            m_editorUtils.Panel("GlobalSettings", GlobalPanel, true);
        }

        private void GlobalPanel(bool helpEnabled)
        {
            EditorGUI.BeginChangeCheck();
            
            m_editorUtils.Heading("GlobalSettings");
            HDR = m_bufferManager.m_allowHDR;
            HDR = m_editorUtils.Toggle("AllowHDR", HDR, helpEnabled);
            RenderSize = m_bufferManager.m_renderSize;
            RenderSize = (PW_RENDER_SIZE)m_editorUtils.EnumPopup("RenderResolution", RenderSize, helpEnabled);
            EditorGUILayout.Space();
            m_editorUtils.Heading("RefreactionSettings");
            RefractionCameraEvent = m_bufferManager.m_cameraEventRefraction;
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("CantBeChangedAtRunTime"), MessageType.Info);
                GUI.enabled = false;
            }
            RefractionCameraEvent = (CameraEvent)m_editorUtils.EnumPopup("RefractionCameraEvent", RefractionCameraEvent, helpEnabled);
            GUI.enabled = true;

            if (EditorGUI.EndChangeCheck())
            {
                if (ChangesMade())
                {
                    m_bufferManager.ClearBuffers();
                    m_bufferManager.m_allowHDR = HDR;
                    m_bufferManager.m_renderSize = RenderSize;
                    m_bufferManager.m_cameraEventRefraction = RefractionCameraEvent;
                    m_bufferManager.RebuildBuffers(false);
                    EditorUtility.SetDirty(m_bufferManager);
                }
            }
        }

        /// <summary>
        /// Has one of the variables been changed?
        /// </summary>
        /// <returns></returns>
        private bool ChangesMade()
        {
            if (HDR != m_bufferManager.m_allowHDR)
            {
                return true;
            }

            if (RenderSize != m_bufferManager.m_renderSize)
            {
                return true;
            }

            if (RefractionCameraEvent != m_bufferManager.m_cameraEventRefraction)
            {
                return true;
            }

            return false;
        }
    }
}