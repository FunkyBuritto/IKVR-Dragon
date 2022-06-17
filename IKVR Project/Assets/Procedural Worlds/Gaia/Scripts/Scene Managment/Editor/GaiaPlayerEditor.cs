using System;
using System.Collections;
using System.Collections.Generic;
using Gaia.Internal;
using PWCommon4;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
    [CustomEditor(typeof(GaiaScenePlayer))]
    public class GaiaPlayerEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private GaiaSettings m_gaiaSettings;
        private SceneProfile m_profile;
        private GaiaLightingProfileValues profile;

        public void OnEnable()
        {
            //Get Gaia Lighting Profile object
            if (GaiaGlobal.Instance != null)
            {
                m_profile = GaiaGlobal.Instance.SceneProfile;
            }

            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            if (m_gaiaSettings == null)
            {
                m_gaiaSettings = GaiaUtils.GetGaiaSettings();
            }
        }

        public override void OnInspectorGUI()
        {
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!

            if (m_profile != null)
            {
                //Monitor for changes
                EditorGUI.BeginChangeCheck();

                if (m_gaiaSettings == null)
                {
                    m_gaiaSettings = GaiaUtils.GetGaiaSettings();
                }

                m_editorUtils.Panel("GlobalSettings", GlobalSettings, false, true, true);

                //Check for changes, make undo record, make changes and let editor know we are dirty
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_profile, "Made changes");
                    EditorUtility.SetDirty(m_profile);
                }
            }
            else
            {
                if (GaiaGlobal.Instance != null)
                {
                    m_profile = GaiaGlobal.Instance.SceneProfile;
                }
            }


        }

        private void GlobalSettings(bool helpEnabled)
        {
            if (GaiaGlobal.Instance.SceneProfile.m_lightingProfiles.Count > 0)
            {
                if (m_profile.m_selectedLightingProfileValuesIndex != -99)
                {
                    profile = GaiaGlobal.Instance.SceneProfile.m_lightingProfiles[m_profile.m_selectedLightingProfileValuesIndex];
                }
            }

            if (m_gaiaSettings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.BuiltIn)
            {
                bool postfx = m_profile.m_setupPostFX;
                m_profile.m_setupPostFX = m_editorUtils.Toggle("SetupPostFX", m_profile.m_setupPostFX, helpEnabled);
                if (postfx != m_profile.m_setupPostFX)
                {
                    if (m_profile.m_setupPostFX)
                    {
                        if (profile != null)
                        {
                            GaiaLighting.SetupPostProcessing(profile, m_profile, GaiaUtils.GetActivePipeline(), true);
                        }
                    }
                    else
                    {
                        GaiaLighting.RemoveAllPostProcessV2CameraLayer();
                    }
                }
            }
            m_profile.m_spawnPlayerAtCurrentLocation = m_editorUtils.Toggle("SpawnPlayerAtCurrentLocation", m_profile.m_spawnPlayerAtCurrentLocation, helpEnabled);

            GaiaConstants.EnvironmentControllerType controller = m_profile.m_controllerType;
            controller = (GaiaConstants.EnvironmentControllerType)m_editorUtils.EnumPopup("ControllerType", controller, helpEnabled);
            if (controller != m_profile.m_controllerType)
            {
                m_gaiaSettings.m_currentController = controller;
                switch (controller)
                {
                    case GaiaConstants.EnvironmentControllerType.FirstPerson:
                        GaiaSceneManagement.CreatePlayer(m_gaiaSettings, GaiaConstants.playerFirstPersonName, m_profile.m_spawnPlayerAtCurrentLocation, null, null, true);
                        break;
                    case GaiaConstants.EnvironmentControllerType.FlyingCamera:
                        GaiaSceneManagement.CreatePlayer(m_gaiaSettings, GaiaConstants.playerFlyCamName, m_profile.m_spawnPlayerAtCurrentLocation, null, null, true);
                        break;
                    case GaiaConstants.EnvironmentControllerType.ThirdPerson:
                        GaiaSceneManagement.CreatePlayer(m_gaiaSettings, GaiaConstants.playerThirdPersonName, m_profile.m_spawnPlayerAtCurrentLocation, null, null, true);
                        break;
                    case GaiaConstants.EnvironmentControllerType.Car:
                        GaiaSceneManagement.CreatePlayer(m_gaiaSettings, GaiaConstants.m_carPlayerPrefabName, m_profile.m_spawnPlayerAtCurrentLocation, null, null, true);
                        break;
                    case GaiaConstants.EnvironmentControllerType.XRController:
#if GAIA_XR
                        GaiaSceneManagement.CreatePlayer(m_gaiaSettings, "XRController", m_profile.m_spawnPlayerAtCurrentLocation);
#else
                        EditorUtility.DisplayDialog("XR Support not enabled", "The XR Controller is a default player for Virtual / Augmented Reality projects. Please open the Setup Panel in the Gaia Manager Standard Tab to enable XR Support in order to use the XR Player Controller. Please also make sure you have the Unity XR Interaction Toolkit package installed before doing so.", "OK");
                        controller = GaiaConstants.EnvironmentControllerType.FlyingCamera;
                        m_gaiaSettings.m_currentController = GaiaConstants.EnvironmentControllerType.FlyingCamera;
#endif
                        break;

                }

                SetPostProcessing(profile);
#if GAIA_PRO_PRESENT
                if (GaiaUtils.HasDynamicLoadedTerrains())
                {
                    TerrainLoader tl = ((GaiaScenePlayer)target).GetComponentInChildren<TerrainLoader>();
                    if (tl != null)
                    {
                        m_profile.UpdateTerrainLoaderFromProfile(ref tl);
                    }
                }
#endif
                m_profile.m_controllerType = controller;
            }

            if (controller == GaiaConstants.EnvironmentControllerType.Custom)
            {
                m_profile.m_customPlayer = (GameObject)m_editorUtils.ObjectField("MainPlayer", m_profile.m_customPlayer, typeof(GameObject), true, helpEnabled);
                m_profile.m_customCamera = (Camera)m_editorUtils.ObjectField("MainCamera", m_profile.m_customCamera, typeof(Camera), true, helpEnabled);

                if (m_editorUtils.Button("ApplySetup"))
                {
                    GaiaSceneManagement.CreatePlayer(m_gaiaSettings, "Custom", m_profile.m_spawnPlayerAtCurrentLocation, m_profile.m_customPlayer, m_profile.m_customCamera, true);
                    SetPostProcessing(profile);
#if GAIA_PRO_PRESENT
                    if (GaiaUtils.HasDynamicLoadedTerrains())
                    {
                        TerrainLoader tl = ((GaiaScenePlayer)target).GetComponentInChildren<TerrainLoader>();
                        if (tl != null)
                        {
                            m_profile.UpdateTerrainLoaderFromProfile(ref tl);
                        }
                    }
#endif
                }
            }

            EditorGUILayout.BeginHorizontal();

            if (m_editorUtils.Button("MoveCameraToPlayer"))
            {
                GaiaSceneManagement.MoveCameraToPlayer(m_gaiaSettings);
            }

            if (m_editorUtils.Button("MovePlayerToCamera"))
            {
                GaiaSceneManagement.MovePlayerToCamera(m_gaiaSettings);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            m_editorUtils.Panel("AutoDepthOfFieldSettings", AutoDepthOfField);
            m_editorUtils.Panel("LocationManagerSettings", LocationManagerSettings);
            m_editorUtils.Panel("PlayerCameraCullingSettings", CameraCullingSettings);
            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                m_editorUtils.Panel("TerrainLoadingSettings", TerrainLoadingSettings);
            }
        }

        private void TerrainLoadingSettings(bool helpEnabled)
        {
#if GAIA_PRO_PRESENT
            EditorGUI.BeginChangeCheck();
            if (!TerrainLoaderManager.Instance.TerrainSceneStorage.m_terrainLoadingEnabled)
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("AutoLoadTerrainsDisabled"), MessageType.Warning);
                GUI.enabled = false;
            }

            if (TerrainLoaderManager.ColliderOnlyLoadingActive)
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("ColliderOnlyLoadingWarning"), MessageType.Warning);
            }

            m_profile.m_terrainLoaderLoadMode = (LoadMode)m_editorUtils.EnumPopup("LoadMode", m_profile.m_terrainLoaderLoadMode, helpEnabled);
            if (m_profile.m_terrainLoaderLoadMode == LoadMode.RuntimeAlways)
            {
                EditorGUI.indentLevel++;
                m_editorUtils.InlineHelp("RefreshRates", helpEnabled);
                m_profile.m_terrainLoaderMinRefreshDistance = m_editorUtils.FloatField("MinRefreshDistance", m_profile.m_terrainLoaderMinRefreshDistance, helpEnabled);
                m_profile.m_terrainLoaderMaxRefreshDistance = m_editorUtils.FloatField("MaxRefreshDistance", m_profile.m_terrainLoaderMaxRefreshDistance, helpEnabled);
                m_profile.m_terrainLoaderMinRefreshMS = m_editorUtils.FloatField("MinRefreshMS", m_profile.m_terrainLoaderMinRefreshMS, helpEnabled);
                m_profile.m_terrainLoaderMaxRefreshMS = m_editorUtils.FloatField("MaxRefreshMS", m_profile.m_terrainLoaderMaxRefreshMS, helpEnabled);
                EditorGUI.indentLevel--;
            }
            m_editorUtils.Heading("LoadRange");
            EditorGUI.indentLevel++;
            m_profile.m_terrainLoaderFollowTransform = m_editorUtils.Toggle("FollowTransform", m_profile.m_terrainLoaderFollowTransform, helpEnabled);
            if (TerrainLoaderManager.ColliderOnlyLoadingActive)
            {
                if (!m_profile.m_terrainLoaderFollowTransform)
                {
                    m_profile.m_terrainLoaderLoadingBoundsCollider.center = m_editorUtils.Vector3Field("LoadingBoundsColliderCenter", m_profile.m_terrainLoaderLoadingBoundsCollider.center, helpEnabled);
                }
                m_profile.m_terrainLoaderLoadingBoundsCollider.extents = m_editorUtils.Vector3Field("LoadingBoundsColliderExtents", m_profile.m_terrainLoaderLoadingBoundsCollider.extents, helpEnabled);
            }
            else
            {

                
                if (!m_profile.m_terrainLoaderFollowTransform)
                {
                    m_profile.m_terrainLoaderLoadingBoundsRegular.center = m_editorUtils.Vector3Field("LoadingBoundsCenter", m_profile.m_terrainLoaderLoadingBoundsRegular.center, helpEnabled);
                }
                m_profile.m_terrainLoaderLoadingBoundsRegular.extents = m_editorUtils.Vector3Field("LoadingBoundsExtents", m_profile.m_terrainLoaderLoadingBoundsRegular.extents, helpEnabled);
                if (!m_profile.m_terrainLoaderFollowTransform)
                {
                    m_profile.m_terrainLoaderLoadingBoundsImpostor.center = m_editorUtils.Vector3Field("LoadingBoundsImpostorCenter", m_profile.m_terrainLoaderLoadingBoundsImpostor.center, helpEnabled);
                }
                m_profile.m_terrainLoaderLoadingBoundsImpostor.extents = m_editorUtils.Vector3Field("LoadingBoundsImpostorExtents", m_profile.m_terrainLoaderLoadingBoundsImpostor.extents, helpEnabled);
            }
            EditorGUI.indentLevel--;
            if (EditorGUI.EndChangeCheck())
            {
                TerrainLoader tl = ((GaiaScenePlayer)target).GetComponentInChildren<TerrainLoader>();
                m_profile.UpdateTerrainLoaderFromProfile(ref tl);
            }
#endif
        }

        private void AutoDepthOfField(bool helpEnabled)
        {
#if UNITY_POST_PROCESSING_STACK_V2
            if (GaiaUtils.GetActivePipeline() == GaiaConstants.EnvironmentRenderer.BuiltIn)
            {
                //Get Auto Focus object
                AutoDepthOfField autoFocus = GaiaSceneManagement.SetupAutoDepthOfField(m_profile);

                //Monitor for changes
                EditorGUI.BeginChangeCheck();
                bool enableDOF = m_profile.m_enableAutoDOF;
                enableDOF = m_editorUtils.Toggle("EnableAutoDepthOfField", enableDOF, helpEnabled);
                if (m_profile.m_enableAutoDOF != enableDOF)
                {
                    m_profile.m_enableAutoDOF = enableDOF;
                    autoFocus = GaiaSceneManagement.SetupAutoDepthOfField(m_profile);
                }
                if (autoFocus != null && m_profile.m_enableAutoDOF)
                {
                    if (enableDOF)
                    {
                        bool oldDebugValue = autoFocus.m_debug;
                        autoFocus.m_debug = m_editorUtils.Toggle("ShowDebugInfo", autoFocus.m_debug);
                        if (oldDebugValue != autoFocus.m_debug && !autoFocus.m_debug)
                        {
                            autoFocus.RemoveDebugSphere();
                        }
                    }

#if !UNITY_POST_PROCESSING_STACK_V2
                    EditorGUILayout.HelpBox("Post Processing is not installed. Please install post processing from the package manager to use this feature. (Window/Package Manager)", MessageType.Warning);
                    GUI.enabled = false;
#endif
                    m_editorUtils.Heading("AutoFocusDependencies");
                    EditorGUI.indentLevel++;
                    autoFocus.m_sourceCamera = (Camera)m_editorUtils.ObjectField("SourceCamera", autoFocus.m_sourceCamera, typeof(Camera), true, helpEnabled);
                    if (autoFocus.m_sourceCamera == null)
                    {
                        EditorGUILayout.HelpBox("Source Camera is missing. Either add it manually or it will be added on start of application if present", MessageType.Error);
                    }
                    autoFocus.m_targetObject = (GameObject)m_editorUtils.ObjectField("TargetObject", autoFocus.m_targetObject, typeof(GameObject), true, helpEnabled);
                    if (autoFocus.m_targetObject == null && autoFocus.m_trackingType == GaiaConstants.DOFTrackingType.FollowTarget)
                    {
                        EditorGUILayout.HelpBox("Target Object is missing. Add it to use target focus mode", MessageType.Error);
                    }
                    autoFocus.m_targetLayer = GaiaEditorUtils.LayerMaskField(new GUIContent(m_editorUtils.GetTextValue("TargetLayer"), m_editorUtils.GetTooltip("TargetLayer")), autoFocus.m_targetLayer);
                    m_editorUtils.InlineHelp("TargetLayer", helpEnabled);
                    EditorGUI.indentLevel--;

                    m_editorUtils.Heading("AutoFocusConfiguration");
                    EditorGUI.indentLevel++;
                    autoFocus.m_interactWithPlayer = m_editorUtils.Toggle("InteractWithPlayer", autoFocus.m_interactWithPlayer, helpEnabled);
                    autoFocus.m_trackingType = (GaiaConstants.DOFTrackingType)m_editorUtils.EnumPopup("TrackingMode", autoFocus.m_trackingType, helpEnabled);
                    autoFocus.m_focusOffset = m_editorUtils.FloatField("FocusOffset", autoFocus.m_focusOffset, helpEnabled);
                    autoFocus.m_maxFocusDistance = m_editorUtils.FloatField("MaxFocusDistance", autoFocus.m_maxFocusDistance, helpEnabled);
                    autoFocus.m_dofAperture = m_editorUtils.Slider("Aperture", autoFocus.m_dofAperture, 0.1f, 32f, helpEnabled);
                    autoFocus.m_dofFocalLength = m_editorUtils.Slider("FocalLength", autoFocus.m_dofFocalLength, 1f, 300f, helpEnabled);
                    if (Application.isPlaying)
                    {
                        EditorGUILayout.LabelField("Actual Focal Distance: " + autoFocus.m_actualFocusDistance);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Actual Focal Distance: Will be updated at runtime");
                    }
                    m_editorUtils.InlineHelp("ActualFocalDistance", helpEnabled);

                    EditorGUI.indentLevel--;
                }

                //Check for changes, make undo record, make changes and let editor know we are dirty
                if (EditorGUI.EndChangeCheck())
                {
                    if (autoFocus != null)
                    {
                        Undo.RecordObject(autoFocus, "Made depth of field changes");
                        EditorUtility.SetDirty(autoFocus);
                        autoFocus.SetDOFMainSettings(autoFocus.m_dofAperture, autoFocus.m_dofFocalLength);
                    }

                    EditorUtility.SetDirty(m_profile);
                }
            }
            else
            {
                m_profile.m_enableAutoDOF = false;
                GaiaSceneManagement.SetupAutoDepthOfField(m_profile);
                EditorGUILayout.HelpBox("Auto Depth Of Field is not yet supported for SRP this will be available in a near future update", MessageType.Info);
            }
#else

            EditorGUILayout.HelpBox("Post Processing V2 has not been isntalled. Please install Post Processing from the package manager to use auto depth of field.", MessageType.Info);

#endif
        }
        private void CameraCullingSettings(bool helpEnabled)
        {
            //Monitor for changes
            EditorGUI.BeginChangeCheck();

            GaiaSceneCullingProfile cullingProfile = m_profile.CullingProfile;
            m_editorUtils.Heading("TerrainCulling");
            EditorGUI.indentLevel++;
            m_profile.m_terrainCullingEnabled = m_editorUtils.Toggle("UseTerrainCulling", m_profile.m_terrainCullingEnabled, helpEnabled);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            m_editorUtils.Heading("CameraCulling");
            EditorGUI.indentLevel++;

            bool cullingEnabled = m_profile.m_enableLayerCulling;
            cullingEnabled = m_editorUtils.Toggle("EnableLayerCulling", cullingEnabled, helpEnabled);
            if (m_profile.m_enableLayerCulling != cullingEnabled)
            {
                m_profile.m_enableLayerCulling = cullingEnabled;
                if (Application.isPlaying)
                {
                    GaiaScenePlayer.UpdateCullingDistances();
                }
                else
                {
                    GaiaScenePlayer.ApplySceneSetup(cullingEnabled);
                }
            }
            if (m_profile.m_enableLayerCulling)
            {
                m_editorUtils.LabelField("GeneralSettings", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                cullingProfile = (GaiaSceneCullingProfile)m_editorUtils.ObjectField("CullingProfile", cullingProfile, typeof(GaiaSceneCullingProfile), false);
                if (m_editorUtils.Button("NewProfile", GUILayout.MaxWidth(40f)))
                {
                    GaiaSceneCullingProfile profile = GaiaSceneCullingProfile.CreateCullingProfile();
                    cullingProfile = AssetDatabase.LoadAssetAtPath<GaiaSceneCullingProfile>(AssetDatabase.GetAssetPath(profile));
                    m_profile.CullingProfile = cullingProfile;
                    GUIUtility.ExitGUI();

                }
                EditorGUILayout.EndHorizontal();
                m_editorUtils.InlineHelp("CullingProfile", helpEnabled);
                if (cullingProfile != m_profile.CullingProfile)
                {
                    m_profile.CullingProfile = cullingProfile;
                    if (cullingProfile != null)
                    {
                        GaiaScenePlayer.ApplySceneSetup(cullingProfile.m_applyToEditorCamera);
                    }
                }
#if GAIA_PRO_PRESENT
                if (ProceduralWorldsGlobalWeather.Instance == null)
                {
                    m_profile.m_sunLight = (Light)m_editorUtils.ObjectField("SunLight", m_profile.m_sunLight, typeof(Light), true, helpEnabled);
                    if (m_profile.m_sunLight == null)
                    {
                        m_profile.m_sunLight = GaiaUtils.GetMainDirectionalLight();
                    }
                }
#else
                m_profile.m_sunLight = (Light)m_editorUtils.ObjectField("SunLight", m_profile.m_sunLight, typeof(Light), true, helpEnabled);
                if (m_profile.m_sunLight == null)
                {
                    m_profile.m_sunLight = GaiaUtils.GetMainDirectionalLight();
                }
#endif

                if (m_profile.CullingProfile != null)
                {
                    m_profile.CullingProfile.m_applyToEditorCamera = m_editorUtils.Toggle("ApplyInEditor", m_profile.CullingProfile.m_applyToEditorCamera, helpEnabled);
                    m_profile.CullingProfile.m_realtimeUpdate = m_editorUtils.Toggle("RealtimeUpdate", m_profile.CullingProfile.m_realtimeUpdate, helpEnabled);
                }
                EditorGUI.indentLevel--;

                if (m_profile.CullingProfile != null)
                {
                    //Objects
                    EditorGUILayout.Space();
                    m_editorUtils.LabelField("ObjectCullingSettings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    m_editorUtils.InlineHelp("ObjectCullingSettings", helpEnabled);
                    for (int i = 0; i < m_profile.CullingProfile.m_layerDistances.Length; i++)
                    {
                        string layerName = LayerMask.LayerToName(i);
                        if (!string.IsNullOrEmpty(layerName))
                        {
                            m_profile.CullingProfile.m_layerDistances[i] = EditorGUILayout.FloatField(string.Format("[{0}] {1}", i, layerName), m_profile.CullingProfile.m_layerDistances[i]);
                        }
                    }
                    EditorGUI.indentLevel--;

                    if (m_editorUtils.Button("RevertCullingToDefaults"))
                    {
                        m_profile.CullingProfile.UpdateCulling(m_gaiaSettings);
                        EditorUtility.SetDirty(m_profile.CullingProfile);
                    }

                    //Shadows
                    EditorGUILayout.Space();
                    m_editorUtils.LabelField("ShadowCullingSettings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    m_editorUtils.InlineHelp("ShadowCullingSettings", helpEnabled);
                    for (int i = 0; i < m_profile.CullingProfile.m_shadowLayerDistances.Length; i++)
                    {
                        string layerName = LayerMask.LayerToName(i);
                        if (!string.IsNullOrEmpty(layerName))
                        {
                            m_profile.CullingProfile.m_shadowLayerDistances[i] = EditorGUILayout.FloatField(string.Format("[{0}] {1}", i, layerName), m_profile.CullingProfile.m_shadowLayerDistances[i]);
                        }
                    }

                    EditorGUI.indentLevel--;

                    if (m_editorUtils.Button("RevertShadowToDefaults"))
                    {
                        m_profile.CullingProfile.UpdateShadow();
                        EditorUtility.SetDirty(m_profile.CullingProfile);
                    }

                    EditorGUI.indentLevel--;
                }

                //Check for changes, make undo record, make changes and let editor know we are dirty
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_profile, "Made camera culling changes");
                    EditorUtility.SetDirty(m_profile);
                    if (m_profile.CullingProfile != null)
                    {
                        EditorUtility.SetDirty(m_profile.CullingProfile);
                        if (Application.isPlaying)
                        {
                            GaiaScenePlayer.UpdateCullingDistances();
                        }
                        else
                        {
                            GaiaScenePlayer.ApplySceneSetup(m_profile.CullingProfile.m_applyToEditorCamera);
                        }
                    }
                }
            }
        }
        private void LocationManagerSettings(bool helpEnabled)
        {
            if (m_gaiaSettings != null)
            {
                EditorGUI.BeginChangeCheck();
                m_gaiaSettings.m_enableLocationManager = m_editorUtils.Toggle("EnableLocationManager", m_gaiaSettings.m_enableLocationManager, helpEnabled);
                if (m_gaiaSettings.m_enableLocationManager)
                {
                    m_editorUtils.Text("LocationManagerInfo");
                    if (m_editorUtils.Button("OpenLocationManager"))
                    {
                        LocationManagerEditor.ShowLocationManager();
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    if (m_gaiaSettings.m_enableLocationManager)
                    {
                        LocationManagerEditor.AddLocationSystem();
                    }
                    else
                    {
                        LocationManagerEditor.RemoveLocationSystem();
                    }
                }
            }
        }
        private void SetPostProcessing(GaiaLightingProfileValues profile)
        {
            if (m_gaiaSettings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.BuiltIn)
            {
                if (m_profile.m_setupPostFX)
                {
                    if (profile != null)
                    {
                        GaiaLighting.SetupPostProcessing(profile, m_profile, GaiaUtils.GetActivePipeline(), true);
                    }
                }
            }
        }
    }
}