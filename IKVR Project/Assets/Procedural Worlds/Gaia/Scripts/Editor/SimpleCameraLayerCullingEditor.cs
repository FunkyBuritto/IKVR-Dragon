using System;
using System.Collections;
using System.Collections.Generic;
using Gaia.Internal;
using PWCommon4;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
    [CustomEditor(typeof(SimpleCameraLayerCulling))]
    public class SimpleCameraLayerCullingEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private SimpleCameraLayerCulling m_simpleCameraLayerCulling;
            
        public void OnEnable()
        {
            m_simpleCameraLayerCulling = (SimpleCameraLayerCulling)target;

            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
                m_simpleCameraLayerCulling.Initialize();
                ApplyToSceneCamera();
            }
        }

        private void ApplyToSceneCamera()
        {
            if (m_simpleCameraLayerCulling.m_applyToSceneCamera)
            {
                foreach (var sceneCamera in SceneView.GetAllSceneCameras())
                {
                    sceneCamera.layerCullDistances = m_simpleCameraLayerCulling.m_profile.m_layerDistances;
                }
                m_simpleCameraLayerCulling.ApplyToDirectionalLight();
            }
            else
            {
                float[] layerCulls = new float[32];
                for (int i = 0; i < layerCulls.Length; i++)
                {
                    layerCulls[i] = 0f;
                }
                foreach (var sceneCamera in SceneView.GetAllSceneCameras())
                {
                    sceneCamera.layerCullDistances = layerCulls;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!

            m_editorUtils.Panel("SimpleLayerCulling", SimpleLayerCulling, true);

        }

        private void SimpleLayerCulling(bool helpEnabled)
        {
            EditorGUI.BeginChangeCheck();
            m_simpleCameraLayerCulling.m_applyToGameCamera = m_editorUtils.Toggle("ApplyToGameCamera", m_simpleCameraLayerCulling.m_applyToGameCamera);
            m_simpleCameraLayerCulling.m_applyToSceneCamera = m_editorUtils.Toggle("ApplyToSceneCamera", m_simpleCameraLayerCulling.m_applyToSceneCamera);

            if (m_simpleCameraLayerCulling.m_profile != null)
            {
                EditorGUILayout.Space();
                m_editorUtils.LabelField("ObjectCullingSettings", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                m_editorUtils.InlineHelp("ObjectCullingSettings", helpEnabled);
                for (int i = 0; i < m_simpleCameraLayerCulling.m_profile.m_layerDistances.Length; i++)
                {
                    string layerName = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(layerName))
                    {
                        m_simpleCameraLayerCulling.m_profile.m_layerDistances[i] = EditorGUILayout.FloatField(string.Format("[{0}] {1}", i, layerName), m_simpleCameraLayerCulling.m_profile.m_layerDistances[i]);
                    }
                }
                EditorGUI.indentLevel--;
                if (m_editorUtils.Button("RevertCullingToDefaults"))
                {
                    GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
                    m_simpleCameraLayerCulling.m_profile.UpdateCulling(gaiaSettings);
                    EditorUtility.SetDirty(m_simpleCameraLayerCulling.m_profile);
                }

                EditorGUILayout.Space();
                m_editorUtils.LabelField("ShadowCullingSettings", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                m_editorUtils.InlineHelp("ShadowCullingSettings", helpEnabled);
                for (int i = 0; i < m_simpleCameraLayerCulling.m_profile.m_shadowLayerDistances.Length; i++)
                {
                    string layerName = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(layerName))
                    {
                        m_simpleCameraLayerCulling.m_profile.m_shadowLayerDistances[i] = EditorGUILayout.FloatField(string.Format("[{0}] {1}", i, layerName), m_simpleCameraLayerCulling.m_profile.m_shadowLayerDistances[i]);
                    }
                }

                EditorGUI.indentLevel--;

                if (m_editorUtils.Button("RevertShadowToDefaults"))
                {
                    m_simpleCameraLayerCulling.m_profile.UpdateShadow();
                    EditorUtility.SetDirty(m_simpleCameraLayerCulling.m_profile);
                }

                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(m_simpleCameraLayerCulling);
                ApplyToSceneCamera();
                m_simpleCameraLayerCulling.ApplyToGameCamera();
                m_simpleCameraLayerCulling.ResetDirectionalLight();
                SceneView.RepaintAll();

            }

        }
    }
}
