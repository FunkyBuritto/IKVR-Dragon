using System.Linq;
using Gaia.Internal;
using Gaia.Pipeline.HDRP;
using PWCommon4;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace Gaia
{
    [CustomEditor(typeof(GaiaSceneLighting))]
    public class GaiaLightingEditor : PWEditor
    {
        #region Variables

        private EditorUtils m_editorUtils;
        private Color defaultBackground;
        private GUIStyle m_boxStyle;
        private GaiaSettings m_gaiaSettings;
        private SceneProfile m_profile;
        private GaiaSceneLighting m_sceneLighting;
        private GaiaLightingProfileValues m_profileValues;
#if GAIA_PRO_PRESENT
        private ProceduralWorldsGlobalWeather m_globalWeather;
#endif
        private GaiaConstants.EnvironmentRenderer m_renderPipeline;
        private bool enableEditMode;
        private string probeSpawnCount;
        private string lightProbeSpawnCount;

        #endregion

        #region Unity Functions

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

            m_renderPipeline = m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled;

#if GAIA_PRO_PRESENT
            if (m_globalWeather == null)
            {
                m_globalWeather = ProceduralWorldsGlobalWeather.Instance;
            }
#endif

            if (m_profile != null)
            {
                if (m_profile.m_lightingProfiles.Count > 0)
                {
                    if (m_profile.m_selectedLightingProfileValuesIndex != -99)
                    {
                        m_profileValues = m_profile.m_lightingProfiles[m_profile.m_selectedLightingProfileValuesIndex];
                        if (m_renderPipeline != GaiaConstants.EnvironmentRenderer.BuiltIn)
                        {
                            foreach (var profile in m_profile.m_lightingProfiles)
                            {
                                profile.m_pwSkyAtmosphereData.CloudRenderQueue = GaiaConstants.CloudRenderQueue.Transparent3000;
                            }
                        }
                        GaiaSceneManagement.FetchSceneSettigns(m_profile, m_profileValues);
                        if (m_profileValues != null)
                        {
#if GAIA_PRO_PRESENT
                            m_profileValues.m_pwSkyAtmosphereData.Load(m_globalWeather);
                            m_profileValues.m_pwSkyWindData.Load(m_globalWeather);
                            m_profileValues.m_pwSkyCloudData.Load(m_globalWeather);
                            m_profileValues.m_pwSkySeasonData.Load(m_globalWeather);
                            m_profileValues.m_pwSkyWeatherData.Load(m_globalWeather);
#endif
                        }
                    }
                }
            }

            enableEditMode = System.IO.Directory.Exists(GaiaUtils.GetAssetPath("Dev Utilities"));

            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }
        private void OnDestroy()
        {
            if (m_editorUtils != null)
            {
                m_editorUtils.Dispose();
            }

            EditorApplication.update -= EditorUpdate;
        }
        public override void OnInspectorGUI()
        {
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!

            //Set up the box style
            if (m_boxStyle == null)
            {
                m_boxStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = {textColor = GUI.skin.label.normal.textColor},
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft
                };
            }

            if (m_sceneLighting == null)
            {
                m_sceneLighting = (GaiaSceneLighting) target;
            }

            defaultBackground = GUI.backgroundColor;
            if (GaiaGlobal.Instance != null)
            {
                m_profile = GaiaGlobal.Instance.SceneProfile;
            }

#if GAIA_PRO_PRESENT
            if (m_globalWeather == null)
            {
                m_globalWeather = ProceduralWorldsGlobalWeather.Instance;
            }
#endif

            if (m_gaiaSettings == null)
            {
                m_gaiaSettings = GaiaUtils.GetGaiaSettings();
            }

            if (m_profile == null)
            {
                return;
            }

            if (m_renderPipeline != m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled)
            {
                m_renderPipeline = m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled;
            }

            if (m_profile.m_selectedLightingProfileValuesIndex > m_profile.m_lightingProfiles.Count - 1)
            {
                m_profile.m_selectedLightingProfileValuesIndex = 0;
            }

            if (m_profile.m_lightingProfiles.Count > 0)
            {
                m_editorUtils.Panel("ProfileSettings", ProfileSettings, false, true, true);
            }
            else
            {
                EditorGUILayout.HelpBox("No Light profile have been set in the Scene Profile. Please go to Gaia Runtime and go to 'Save And Load' and click Revert To Defaults. Or go to the Gaia Manager and go to Runtime in Standard tab and click 'Create/Update Runtime'", MessageType.Info);
            }
        }

        #endregion

        #region Utils

        private void ProfileSettings(bool helpEnabled)
        {
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("EditLightingInPlayMode"), MessageType.Warning);
            }
            if (enableEditMode)
            {
                m_profile.m_lightingEditSettings = EditorGUILayout.ToggleLeft(new GUIContent("Use Procedural Worlds Editor Settings", "Enable PW Editor features used for development. Users will not see or be able to use this featue."), m_profile.m_lightingEditSettings);
            }
            else
            {
                m_profile.m_lightingEditSettings = false;
            }

            GaiaConstants.GlobalSystemMode mode = m_profile.m_lightSystemMode;
            mode = (GaiaConstants.GlobalSystemMode)m_editorUtils.EnumPopup("LightSystemMode", mode, helpEnabled);
            if (mode != m_profile.m_lightSystemMode)
            {
                m_profile.m_lightSystemMode = mode;
                if (mode == GaiaConstants.GlobalSystemMode.Gaia)
                {
                    GaiaUtils.RemoveEnviro();
                    GaiaLighting.GetProfile(m_profile, m_gaiaSettings.m_pipelineProfile, m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, true);
                }
            }

            EditorGUILayout.BeginVertical(m_boxStyle);
            if (m_profile.m_lightSystemMode == GaiaConstants.GlobalSystemMode.Gaia)
            {
                if (m_profile.m_lightingProfiles.Count > 0)
                {
                    //Building up a value array of incrementing ints of the size of the lighting profile values array, this array will then match the displayed string selection in the popup
                    int[] lightingProfileValuesIndices = Enumerable
                                        .Repeat(0, (int)((m_profile.m_lightingProfiles.Count - 1) / 1) + 1)
                                        .Select((tr, ti) => tr + (1 * ti))
                                        .ToArray();

                    string[] profileNames = m_profile.m_lightingProfiles.Select(x => x.m_typeOfLighting).ToArray();

                    //Injecting the "None" option
                    lightingProfileValuesIndices = GaiaUtils.AddElementToArray(lightingProfileValuesIndices, -99);
                    profileNames = GaiaUtils.AddElementToArray(profileNames, "None");
                    if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
                    {
                        if (Application.isPlaying)
                        {
                            EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("HDRPProfileHelpPlayMode"), MessageType.Info);
                            GUI.enabled = false;
                        }
                    }
                    EditorGUILayout.BeginHorizontal();
                    if (m_profile.m_renamingProfile)
                    {
                        m_profileValues.m_profileRename = m_editorUtils.TextField("NewProfileName", m_profileValues.m_profileRename);
                    }
                    else
                    {
                        int selectedProfile = m_profile.m_selectedLightingProfileValuesIndex;
                        selectedProfile = EditorGUILayout.IntPopup("Lighting Profile", selectedProfile, profileNames, lightingProfileValuesIndices);
                        if (selectedProfile != m_profile.m_selectedLightingProfileValuesIndex)
                        {
#if !GAIA_EXPERIMENTAL
                            if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
                            {
                                if (m_profile.m_lightingProfiles[selectedProfile].m_profileType == GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                                {
                                    EditorUtility.DisplayDialog("Not Yet Supported", GaiaConstants.HDRPPWSkyExperimental, "Ok");
                                    return;
                                }
                            }
#endif
                            m_profile.m_selectedLightingProfileValuesIndex = selectedProfile;
                            if (m_profile.m_selectedLightingProfileValuesIndex != -99)
                            {
                                m_profileValues = m_profile.m_lightingProfiles[m_profile.m_selectedLightingProfileValuesIndex];
                            }

                            if (m_profileValues != null)
                            {
#if GAIA_PRO_PRESENT
                                m_profileValues.m_pwSkyAtmosphereData.Load(m_globalWeather);
                                m_profileValues.m_pwSkyWindData.Load(m_globalWeather);
                                m_profileValues.m_pwSkyCloudData.Load(m_globalWeather);
                                m_profileValues.m_pwSkySeasonData.Load(m_globalWeather);
                                m_profileValues.m_pwSkyWeatherData.Load(m_globalWeather);
#endif
                            }
                            GaiaUtils.UpdateProbeDataDefaults(m_profile);
                            if (m_profile.m_selectedLightingProfileValuesIndex != -99)
                            {
                                GaiaLighting.GetProfile(m_profile, m_gaiaSettings.m_pipelineProfile, m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, true);
                            }
                            EditorGUIUtility.ExitGUI();
                        }
                    }

                    if (m_profile.m_selectedLightingProfileValuesIndex != -99)
                    {
                        m_profileValues = m_profile.m_lightingProfiles[m_profile.m_selectedLightingProfileValuesIndex];
                        if (m_profileValues.m_userCustomProfile)
                        {
                            if (m_profile.m_renamingProfile)
                            {
                                if (m_editorUtils.Button("Save", GUILayout.MaxWidth(55f)))
                                {
                                    m_profileValues.m_typeOfLighting = m_profileValues.m_profileRename;
                                    m_profile.m_renamingProfile = false;
                                }
                            }
                            else
                            {
                                if (m_editorUtils.Button("Rename", GUILayout.MaxWidth(55f)))
                                {
                                    m_profile.m_renamingProfile = true;
                                    m_profileValues.m_profileRename = m_profileValues.m_typeOfLighting;
                                }
                            }

                            if (m_editorUtils.Button("Remove", GUILayout.MaxWidth(30f)))
                            {
                                m_profile.m_renamingProfile = false;
                                m_profile.m_lightingProfiles.RemoveAt(m_profile.m_selectedLightingProfileValuesIndex);
                                m_profile.m_selectedLightingProfileValuesIndex--;
                                if (m_profile.m_selectedLightingProfileValuesIndex != -99)
                                {
                                    GaiaUtils.GetRuntimeSceneObject();
                                    GaiaLighting.GetProfile(m_profile, m_gaiaSettings.m_pipelineProfile, m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled);
                                }
                            }
                        }
                        if (m_editorUtils.Button("MakeCopy", GUILayout.MaxWidth(30f)))
                        {
                            AddNewCustomProfile();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    m_editorUtils.InlineHelp("LightingProfile", helpEnabled);
                }

                //Monitor for changes
                EditorGUI.BeginChangeCheck();

                m_editorUtils.Panel("LightingProfileSettings", LightingProfileSettingsEnabled, true);
                GUI.enabled = true;
                m_editorUtils.Panel("GlobalSettings", GlobalSettingsEnabled);
                m_editorUtils.Panel("AutoReflectionAndLightProbeSettings", LightAndReflectionProbeSettings);

                //Check for changes, make undo record, make changes and let editor know we are dirty
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_profile, "Made changes");
                    EditorUtility.SetDirty(m_profile);

                    if (m_profile.m_lightingUpdateInRealtime)
                    {
                        if (m_profile.m_selectedLightingProfileValuesIndex != -99)
                        {
                            GaiaUtils.GetRuntimeSceneObject();
                            GaiaLighting.GetProfile(m_profile, m_gaiaSettings.m_pipelineProfile, m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, true);
                        }
                    }
                }

                m_editorUtils.Panel("SaveAndLoad", SaveAndLoad);

                if (m_profile.m_lightingEditSettings)
                {
                    DrawDefaultInspector();
                }
            }
            else if (m_profile.m_lightSystemMode == GaiaConstants.GlobalSystemMode.ThirdParty)
            {
                m_editorUtils.Panel("ThirdPartySettings", ThirdPartyPanel, true);
            }
            else
            {
                EditorGUILayout.HelpBox("System Mode is set to 'None' to use lighting set the mode to 'Gaia or ThirdParty'", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
        }
        private void GlobalSettingsEnabled(bool helpEnabled)
        {
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                GUI.backgroundColor = Color.yellow;
                EditorGUILayout.HelpBox("Gaia lighting looks best in Linear Color Space. Go to Gaia standard tab and press Set Linear Deferred", MessageType.Warning);
            }

            GUI.backgroundColor = defaultBackground;

            m_editorUtils.Heading("SetupSettings");
            EditorGUI.indentLevel++;
            //m_profile.m_lightingMultiSceneLightingSupport = m_editorUtils.Toggle("MultiSceneSupport", m_profile.m_lightingMultiSceneLightingSupport, helpEnabled);
            if (m_renderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                m_profile.m_masterSkyboxMaterial = (Material)m_editorUtils.ObjectField("MasterSkyboxMaterial", m_profile.m_masterSkyboxMaterial, typeof(Material), false, helpEnabled, GUILayout.Height(16f));
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            m_editorUtils.Heading("LightmappingSettings");
            EditorGUI.indentLevel++;
            m_profile.m_lightingBakeMode = (GaiaConstants.BakeMode)m_editorUtils.EnumPopup("LightmappingBakeMode", m_profile.m_lightingBakeMode, helpEnabled);
#if UNITY_2020_1_OR_NEWER
            m_profile.m_lightmappingMode = (LightingSettings.Lightmapper)m_editorUtils.EnumPopup("LightmappingMode", m_profile.m_lightmappingMode, helpEnabled);
#else
            m_profile.m_lightmappingMode = (LightmapEditorSettings.Lightmapper)m_editorUtils.EnumPopup("LightmappingMode", m_profile.m_lightmappingMode, helpEnabled);
#endif
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            m_editorUtils.Heading("PostProcessingSettings");
            EditorGUI.indentLevel++;
            m_profile.m_enablePostProcessing = m_editorUtils.Toggle("EnablePostProcessing", m_profile.m_enablePostProcessing, helpEnabled);
            if (m_profile.m_enablePostProcessing)
            {
                m_profile.m_hideProcessVolume = m_editorUtils.Toggle("HidePostProcessingVolumesInScene", m_profile.m_hideProcessVolume, helpEnabled);
                m_profile.m_antiAliasingMode = (GaiaConstants.GaiaProAntiAliasingMode)m_editorUtils.EnumPopup("Anti-AliasingMode", m_profile.m_antiAliasingMode, helpEnabled);
                if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.BuiltIn)
                {
                    if (m_profile.m_antiAliasingMode == GaiaConstants.GaiaProAntiAliasingMode.TAA)
                    {
                        m_profile.m_AAJitterSpread = m_editorUtils.Slider("AAJitterSpread", m_profile.m_AAJitterSpread, 0f, 1f, helpEnabled);
                        m_profile.m_AAStationaryBlending = m_editorUtils.Slider("AAStationaryBlending", m_profile.m_AAStationaryBlending, 0f, 1f, helpEnabled);
                        m_profile.m_AAMotionBlending = m_editorUtils.Slider("AAMotionBlending", m_profile.m_AAMotionBlending, 0f, 1f, helpEnabled);
                        m_profile.m_AASharpness = m_editorUtils.Slider("AASharpness", m_profile.m_AASharpness, 0f, 1f, helpEnabled);
                    }
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            m_editorUtils.Heading("CameraSettings");
            EditorGUI.indentLevel++;
            m_profile.m_enableAutoDOF = m_editorUtils.Toggle("UseAutoDOF", m_profile.m_enableAutoDOF, helpEnabled);
            if (m_profile.m_enableAutoDOF)
            {
                m_profile.m_dofLayerDetection = GaiaEditorUtils.LayerMaskField(new GUIContent(m_editorUtils.GetTextValue("DOFLayerDetection"), m_editorUtils.GetTooltip("DOFLayerDetection")), m_profile.m_dofLayerDetection);
                m_editorUtils.InlineHelp("DOFLayerDetection", helpEnabled);
            }

            m_profile.m_usePhysicalCamera = m_editorUtils.Toggle("UsePhysicalCamera", m_profile.m_usePhysicalCamera, helpEnabled);
            if (m_profile.m_usePhysicalCamera)
            {
                m_profile.m_cameraFocalLength = m_editorUtils.FloatField("CameraFocalLength", m_profile.m_cameraFocalLength, helpEnabled);
                m_profile.m_cameraSensorSize = m_editorUtils.Vector2Field("CameraSensorSize", m_profile.m_cameraSensorSize, helpEnabled);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            m_editorUtils.Heading("MiscellaneousSettings");
            EditorGUI.indentLevel++;
            m_profile.m_lodBias = m_editorUtils.FloatField("LODBias", m_profile.m_lodBias, helpEnabled);
            if (m_profile.m_lodBias < 0.01f)
            {
                m_profile.m_lodBias = 0.01f;
            }
            m_profile.m_parentObjects = m_editorUtils.Toggle("ParentObjectsToGaia", m_profile.m_parentObjects, helpEnabled);
            m_profile.m_enableAmbientAudio = m_editorUtils.Toggle("EnableAmbientAudio", m_profile.m_enableAmbientAudio, helpEnabled);

            if (m_renderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                m_profile.m_enableFog = m_editorUtils.Toggle("EnableFog", m_profile.m_enableFog, helpEnabled);
            }
            else
            {
#if UNITY_2019_3_OR_NEWER

                EditorGUILayout.Space();
                m_editorUtils.Heading("HDRPSettings");
                if (m_profile.m_antiAliasingMode == GaiaConstants.GaiaProAntiAliasingMode.TAA)
                {
                    m_profile.m_antiAliasingTAAStrength = m_editorUtils.Slider("TAAStrength", m_profile.m_antiAliasingTAAStrength, 0f, 2f, helpEnabled);
                }
                m_profile.m_cameraDithering = m_editorUtils.Toggle("CameraDithering", m_profile.m_cameraDithering, helpEnabled);
                m_profile.m_cameraAperture = m_editorUtils.Slider("CameraAperture", m_profile.m_cameraAperture, 1f, 32f, helpEnabled);
#endif
            }

            EditorGUI.indentLevel--;
        }
        private void LightingProfileSettingsEnabled(bool helpEnabled)
        {
            if (m_profileValues == null)
            {
                if (m_profile.m_selectedLightingProfileValuesIndex != -99)
                {
                    if (m_profile.m_selectedLightingProfileValuesIndex > m_profile.m_lightingProfiles.Count - 1)
                    {
                        m_profile.m_selectedLightingProfileValuesIndex = 0;
                    }

                    m_profileValues = m_profile.m_lightingProfiles[m_profile.m_selectedLightingProfileValuesIndex];
                }
            }
#if !GAIA_PRO_PRESENT
            if (m_profileValues.m_profileType != GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
            {
#endif
                if (!m_profileValues.m_userCustomProfile && !m_profile.m_lightingEditSettings)
                {
                    EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("NewProfileInfo"), MessageType.Info);
                    if (m_editorUtils.Button("CreateNewProfileButton"))
                    {
                        AddNewCustomProfile();
                        GUIUtility.ExitGUI();
                    }
                    GUI.enabled = false;
                }


                m_profile.m_lightingUpdateInRealtime = m_editorUtils.ToggleLeft("UpdateChangesInRealtime", m_profile.m_lightingUpdateInRealtime, helpEnabled);

#if !GAIA_PRO_PRESENT
            }
            else
            {
                if (!m_profileValues.m_userCustomProfile && !m_profile.m_lightingEditSettings)
                {
                    GUI.enabled = false;
                }
            }
#endif

            if (!m_profile.m_lightingUpdateInRealtime)
            {
                EditorGUILayout.BeginHorizontal();
                if (m_editorUtils.Button("UpdateToScene"))
                {
                    if (m_profile.m_selectedLightingProfileValuesIndex != -99)
                    {
                        GaiaUtils.GetRuntimeSceneObject();
                        GaiaLighting.GetProfile(m_profile, m_gaiaSettings.m_pipelineProfile, m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled);
                    }
                }

                if (m_editorUtils.Button("UpdateDynamicGI"))
                {
                    GaiaLighting.UpdateAmbientEnvironment();
                }
                EditorGUILayout.EndHorizontal();

                m_editorUtils.InlineHelp("UpdateToScene", helpEnabled);
                m_editorUtils.InlineHelp("UpdateDynamicGI", helpEnabled);
            }

            if (m_profile.m_selectedLightingProfileValuesIndex == -99)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.HelpBox("No Profile selected. Select another profile that is not 'None' to view settings to edit", MessageType.Info);
            }
            else
            {
                GaiaConstants.GaiaLightingProfileType type = m_profileValues.m_profileType;
                type = (GaiaConstants.GaiaLightingProfileType)m_editorUtils.EnumPopup("SkyboxType", type, helpEnabled);
                if (m_profileValues.m_profileType != type)
                {
                    m_profileValues.m_profileType = type;
                    if (!m_profile.m_lightingUpdateInRealtime)
                    {
                        if (m_profile.m_selectedLightingProfileValuesIndex != -99)
                        {
                            GaiaUtils.GetRuntimeSceneObject();
                            GaiaLighting.GetProfile(m_profile, m_gaiaSettings.m_pipelineProfile, m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, true);
                        }
                    }
                }
                if (m_profileValues.m_profileType == GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                {
                    GUI.enabled = true;
                    EditorGUI.BeginChangeCheck();
#if GAIA_PRO_PRESENT
#if !GAIA_EXPERIMENTAL
                    if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
                    {
                        EditorGUILayout.HelpBox(GaiaConstants.HDRPPWSkyExperimental, MessageType.Info);
                    }
                    else
#else
                    if (m_renderPipeline != GaiaConstants.EnvironmentRenderer.Lightweight)
#endif
                    {
                        if (m_globalWeather != null)
                        {
                            m_editorUtils.LabelField("TimeOfDay", EditorStyles.boldLabel);
                            EditorGUI.indentLevel++;
                            m_profile.m_gaiaTimeOfDay.m_todHour = m_editorUtils.IntSlider("TODHour", m_profile.m_gaiaTimeOfDay.m_todHour, 0, 23, helpEnabled);
                            if (m_profile.m_gaiaTimeOfDay.m_todHour > 23)
                            {
                                m_profile.m_gaiaTimeOfDay.m_todHour = 0;
                            }
                            m_profile.m_gaiaTimeOfDay.m_todMinutes = m_editorUtils.Slider("TODMinutes", m_profile.m_gaiaTimeOfDay.m_todMinutes, 0f, 59f, helpEnabled);
                            if (m_profile.m_gaiaTimeOfDay.m_todMinutes > 60f)
                            {
                                m_profile.m_gaiaTimeOfDay.m_todMinutes = 0f;
                            }
                            m_profile.m_gaiaTimeOfDay.m_todEnabled = m_editorUtils.Toggle("TODEnable", m_profile.m_gaiaTimeOfDay.m_todEnabled, helpEnabled);
                            if (m_profile.m_gaiaTimeOfDay.m_todEnabled)
                            {
                                EditorGUI.indentLevel++;
                                m_profile.m_gaiaTimeOfDay.m_todDayTimeScale = m_editorUtils.Slider("TODScale", m_profile.m_gaiaTimeOfDay.m_todDayTimeScale, 0f, 500f, helpEnabled);
                                EditorGUI.indentLevel--;
                            }
                            EditorGUI.indentLevel--;
                            EditorGUILayout.Space();

                            if (EditorGUI.EndChangeCheck())
                            {
                                if (GaiaGlobal.Instance != null)
                                {
                                    EditorUtility.SetDirty(GaiaGlobal.Instance);
                                    Undo.RecordObject(GaiaGlobal.Instance, "Changes Made");

                                    GaiaGlobal.Instance.UpdateGaiaTimeOfDay(false);
                                    GaiaGlobal.Instance.UpdateGaiaWeather();
                                }
                            }

                            if (!m_profileValues.m_userCustomProfile && !m_profile.m_lightingEditSettings)
                            {
                                GUI.enabled = false;
                            }
                            
                            if (m_profile.m_enablePostProcessing)
                            {
                                if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
                                {
                                    m_editorUtils.Heading("PostProcessingSettings");
                                    EditorGUI.indentLevel++;
            #if UPPipeline
                                    m_profileValues.PostProcessProfileURP = (VolumeProfile)m_editorUtils.ObjectField("PostProcessingProfile", m_profileValues.PostProcessProfileURP, typeof(VolumeProfile), false, helpEnabled);
            #endif
                                    EditorGUI.indentLevel--;
                                }
                                else if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.BuiltIn)
                                {
                                    m_editorUtils.Heading("PostProcessingSettings");
                                    EditorGUI.indentLevel++;
            #if UNITY_POST_PROCESSING_STACK_V2
                                    m_profileValues.PostProcessProfileBuiltIn = (PostProcessProfile)m_editorUtils.ObjectField("PostProcessingProfile", m_profileValues.PostProcessProfileBuiltIn, typeof(PostProcessProfile), false, helpEnabled);
                                    m_profileValues.m_directToCamera = m_editorUtils.Toggle("DirectToCamera", m_profileValues.m_directToCamera, helpEnabled);
                                    if (m_profileValues.m_profileType == GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                                    {
                                        if (m_globalWeather != null)
                                        {
                                            m_globalWeather.TODPostProcessExposure = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("PostProcessingExpsoure"), m_editorUtils.GetTooltip("PostProcessingExpsoure")), m_globalWeather.TODPostProcessExposure);
                                            m_editorUtils.InlineHelp("PostProcessingExpsoure", helpEnabled);
                                            GaiaEditorUtils.DrawTimeOfDayLine(GaiaGlobal.GetTimeOfDayMainValue());
                                        }
                                    }
                                    else
                                    {
                                        m_profileValues.m_postProcessExposure = m_editorUtils.FloatField("PostProcessingExpsoure", m_profileValues.m_postProcessExposure, helpEnabled);
                                    }
            #else
                                    EditorGUILayout.HelpBox("Post Processing is not installed. Install it from the package manager to use the post processing setup features.", MessageType.Info);
            #endif
                                    EditorGUI.indentLevel--;
                                }
                                else
                                {
            #if HDPipeline
                                    m_editorUtils.Heading("VolumeSettings");
                                    EditorGUI.indentLevel++;
                                    m_profileValues.PostProcessProfileHDRP = (VolumeProfile)m_editorUtils.ObjectField("PostProcessingProfile", m_profileValues.PostProcessProfileHDRP, typeof(VolumeProfile), false, helpEnabled);
                                    m_profileValues.EnvironmentProfileHDRP = (VolumeProfile)m_editorUtils.ObjectField("EnvironmentProfile", m_profileValues.EnvironmentProfileHDRP, typeof(VolumeProfile), false, helpEnabled);
                                    EditorGUI.indentLevel--;
    #endif
                                }

                                GUILayout.Space(10f);
                            }

                            EditorGUI.BeginChangeCheck();
                            m_editorUtils.Heading("SkyboxSettings");
                            EditorGUI.indentLevel++;
                            m_profileValues.m_pwSkySunRotation = m_editorUtils.Slider("SunRotation", m_profileValues.m_pwSkySunRotation, 0f, 360f, helpEnabled);
                            if (m_globalWeather.m_renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
                            {
                                if (m_globalWeather.TODHDRPGroundTint != null)
                                {
                                    m_globalWeather.TODHDRPGroundTint = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("TODHDRPGroundTint"), m_editorUtils.GetTooltip("TODHDRPGroundTint")), m_globalWeather.TODHDRPGroundTint);
                                    m_editorUtils.InlineHelp("TODHDRPGroundTint", helpEnabled);
                                }

                                m_globalWeather.TODSkyboxExposure = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODSkyboxExposure"), m_editorUtils.GetTooltip("TODSkyboxExposure")), m_globalWeather.TODSkyboxExposure);
                                m_editorUtils.InlineHelp("TODSkyboxExposure", helpEnabled);
                            }
                            else
                            {
                                m_globalWeather.TODSkyboxExposure = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODSkyboxExposure"), m_editorUtils.GetTooltip("TODSkyboxExposure")), m_globalWeather.TODSkyboxExposure);
                                m_editorUtils.InlineHelp("TODSkyboxExposure", helpEnabled);

                                m_globalWeather.TODAtmosphereThickness = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODAtmosphereThickness"), m_editorUtils.GetTooltip("TODAtmosphereThickness")), m_globalWeather.TODAtmosphereThickness);
                                m_editorUtils.InlineHelp("TODAtmosphereThickness", helpEnabled);

                                m_globalWeather.TODSunSize = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODSunSize"), m_editorUtils.GetTooltip("TODSunSize")), m_globalWeather.TODSunSize);
                                m_editorUtils.InlineHelp("TODSunSize", helpEnabled);

                                m_globalWeather.TODSunSizeConvergence = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODSunSizeConvergence"), m_editorUtils.GetTooltip("TODSunSizeConvergence")), m_globalWeather.TODSunSizeConvergence);
                                m_editorUtils.InlineHelp("TODSunSizeConvergence", helpEnabled);

                                m_globalWeather.TODSkyboxTint = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("TODSkyboxTint"), m_editorUtils.GetTooltip("TODSkyboxTint")), m_globalWeather.TODSkyboxTint);
                                m_editorUtils.InlineHelp("TODSkyboxTint", helpEnabled);

                                m_globalWeather.TODSkyboxFogHeight = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODSkyboxFogHeight"), m_editorUtils.GetTooltip("TODSkyboxFogHeight")), m_globalWeather.TODSkyboxFogHeight);
                                m_editorUtils.InlineHelp("TODSkyboxFogHeight", helpEnabled);

                                m_globalWeather.TODSkyboxFogGradient = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODSkyboxFogGradient"), m_editorUtils.GetTooltip("TODSkyboxFogGradient")), m_globalWeather.TODSkyboxFogGradient);
                                m_editorUtils.InlineHelp("TODSkyboxFogGradient", helpEnabled);
                            }
                            EditorGUI.indentLevel--;
                            if (m_globalWeather.m_renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
                            {
                                GaiaEditorUtils.DrawTimeOfDayLine(GaiaGlobal.GetTimeOfDayMainValue(), 1.2f);
                            }
                            else
                            {
                                GaiaEditorUtils.DrawTimeOfDayLine(GaiaGlobal.GetTimeOfDayMainValue(), 6.7f);
                            }
                            EditorGUILayout.Space();

                            m_editorUtils.Heading("SunSettings");
                            EditorGUI.indentLevel++;
                            m_globalWeather.TODSunColor = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("TODSunColor"), m_editorUtils.GetTooltip("TODSunColor")), m_globalWeather.TODSunColor);
                            m_editorUtils.InlineHelp("TODSunColor", helpEnabled);

                            m_globalWeather.TODSunIntensity = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODSunIntensity"), m_editorUtils.GetTooltip("TODSunIntensity")), m_globalWeather.TODSunIntensity);
                            m_editorUtils.InlineHelp("TODSunIntensity", helpEnabled);

                            if (m_globalWeather.m_renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
                            {
                                GaiaEditorUtils.DrawTimeOfDayLine(GaiaGlobal.GetTimeOfDayMainValue(), 1.2f);
                            }
                            else
                            {
                                GaiaEditorUtils.DrawTimeOfDayLine(GaiaGlobal.GetTimeOfDayMainValue(), 1.2f);
                            }

                            EditorGUI.indentLevel--;
                            EditorGUILayout.Space();

                            m_editorUtils.Heading("ShadowSettings");
                            EditorGUI.indentLevel++;
                            if (m_globalWeather.m_renderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
                            {
                                m_globalWeather.TODSunShadowStrength = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODSunShadowStregth"), m_editorUtils.GetTooltip("TODSunShadowStregth")), m_globalWeather.TODSunShadowStrength);
                                m_editorUtils.InlineHelp("TODSunShadowStregth", helpEnabled);
                            }
                            GaiaEditorUtils.DrawTimeOfDayLine(GaiaGlobal.GetTimeOfDayMainValue());
                            m_profileValues.m_shadowCastingMode = (LightShadows)m_editorUtils.EnumPopup("SunShadowCastingMode", m_profileValues.m_shadowCastingMode, helpEnabled);
                            m_profileValues.m_sunShadowResolution = (LightShadowResolution)m_editorUtils.EnumPopup("SunShadowResolution", m_profileValues.m_sunShadowResolution, helpEnabled);
                            m_profileValues.m_shadowDistance = m_editorUtils.Slider("ShadowDistance", m_profileValues.m_shadowDistance, 0f, 10000f, helpEnabled);
                            EditorGUI.indentLevel--;
                            EditorGUILayout.Space();

                            if (m_globalWeather.m_renderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
                            {
                                m_editorUtils.Heading("AmbientSettings");
                                EditorGUI.indentLevel++;
                                switch (RenderSettings.ambientMode)
                                    {
                                        case AmbientMode.Skybox:
                                            m_globalWeather.TODAmbientIntensity = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODAmbientIntensity"), m_editorUtils.GetTooltip("TODAmbientIntensity")), m_globalWeather.TODAmbientIntensity);
                                            m_editorUtils.InlineHelp("TODAmbientIntensity", helpEnabled);
                                            break;
                                        case AmbientMode.Trilight:
                                            m_globalWeather.TODAmbientSkyColor = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("TODAmbientSkyColor"), m_editorUtils.GetTooltip("TODAmbientSkyColor")), m_globalWeather.TODAmbientSkyColor, true);
                                            m_editorUtils.InlineHelp("TODAmbientSkyColor", helpEnabled);

                                            m_globalWeather.TODAmbientEquatorColor = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("TODAmbientEquatorColor"), m_editorUtils.GetTooltip("TODAmbientEquatorColor")), m_globalWeather.TODAmbientEquatorColor, true);
                                            m_editorUtils.InlineHelp("TODAmbientEquatorColor", helpEnabled);

                                            m_globalWeather.TODAmbientGroundColor = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("TODAmbientGroundColor"), m_editorUtils.GetTooltip("TODAmbientGroundColor")), m_globalWeather.TODAmbientGroundColor, true);
                                            m_editorUtils.InlineHelp("TODAmbientGroundColor", helpEnabled);
                                            break;
                                        default:
                                            m_globalWeather.TODAmbientSkyColor = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("TODAmbientSkyColor"), m_editorUtils.GetTooltip("TODAmbientSkyColor")), m_globalWeather.TODAmbientSkyColor, true);
                                            m_editorUtils.InlineHelp("TODAmbientSkyColor", helpEnabled);
                                            break;
                                    }
                                EditorGUI.indentLevel--;
                                GaiaEditorUtils.DrawTimeOfDayLine(GaiaGlobal.GetTimeOfDayMainValue(), 2.2f);
                                EditorGUILayout.Space();
                            }

                            m_editorUtils.Heading("FogSettings");
                            EditorGUI.indentLevel++;
                            if (m_globalWeather.m_renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
                            {
                                m_globalWeather.TODFogColor = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("TODFogColor"), m_editorUtils.GetTooltip("TODFogColor")), m_globalWeather.TODFogColor);
                                m_editorUtils.InlineHelp("TODFogColor", helpEnabled);
                                m_globalWeather.TODHDRPFogAlbedo = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("TODHDRPFogAlbedo"), m_editorUtils.GetTooltip("TODHDRPFogAlbedo")), m_globalWeather.TODHDRPFogAlbedo);
                                m_editorUtils.InlineHelp("TODHDRPFogAlbedo", helpEnabled);
                                m_globalWeather.TODFogEndDistance = m_editorUtils.CurveField("TODFogEndDistance", m_globalWeather.TODFogEndDistance, helpEnabled);
                                m_globalWeather.TODHDRPFogAnisotropy = m_editorUtils.CurveField("TODHDRPFogAnisotropy", m_globalWeather.TODHDRPFogAnisotropy, helpEnabled);
                                m_globalWeather.TODHDRPFogBaseHeight = m_editorUtils.CurveField("TODHDRPFogBaseHeight", m_globalWeather.TODHDRPFogBaseHeight, helpEnabled);
                                m_globalWeather.TODHDRPFogDepthExtent = m_editorUtils.CurveField("TODHDRPFogDepthExtent", m_globalWeather.TODHDRPFogDepthExtent, helpEnabled);
                                m_globalWeather.TODHDRPFogLightProbeDimmer = m_editorUtils.CurveField("TODHDRPFogLightProbeDimmer", m_globalWeather.TODHDRPFogLightProbeDimmer, helpEnabled);
                            }
                            else
                            {
                                FogMode fogMode = m_profileValues.m_fogMode;
                                fogMode = (FogMode)m_editorUtils.EnumPopup("FogMode", fogMode, helpEnabled);
                                if (fogMode != m_profileValues.m_fogMode)
                                {
                                    m_profileValues.m_fogMode = fogMode;
                                    m_globalWeather.UpdateFogMode(m_profileValues.m_fogMode);
                                }
                                m_globalWeather.TODFogColor = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("TODFogColor"), m_editorUtils.GetTooltip("TODFogColor")), m_globalWeather.TODFogColor);
                                m_editorUtils.InlineHelp("TODFogColor", helpEnabled);

                                if (RenderSettings.fogMode == FogMode.Linear)
                                {
                                    m_globalWeather.TODFogStartDistance = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODFogStartDistance"), m_editorUtils.GetTooltip("TODFogStartDistance")), m_globalWeather.TODFogStartDistance);
                                    m_editorUtils.InlineHelp("TODFogStartDistance", helpEnabled);

                                    m_globalWeather.TODFogEndDistance = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODFogEndDistance"), m_editorUtils.GetTooltip("TODFogEndDistance")), m_globalWeather.TODFogEndDistance);
                                    m_editorUtils.InlineHelp("TODFogEndDistance", helpEnabled);
                                }
                                else
                                {
                                    m_globalWeather.TODFogDensity = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODFogDensity"), m_editorUtils.GetTooltip("TODFogDensity")), m_globalWeather.TODFogDensity);
                                    m_editorUtils.InlineHelp("TODFogDensity", helpEnabled);
                                }
                            }
                            EditorGUI.indentLevel--;
                            if (m_globalWeather.m_renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
                            {
                                GaiaEditorUtils.DrawTimeOfDayLine(GaiaGlobal.GetTimeOfDayMainValue(), 6.8f);
                            }
                            else
                            {
                                if (RenderSettings.fogMode == FogMode.Linear)
                                {
                                    GaiaEditorUtils.DrawTimeOfDayLine(GaiaGlobal.GetTimeOfDayMainValue(), 2.2f);
                                }
                                else
                                {
                                    GaiaEditorUtils.DrawTimeOfDayLine(GaiaGlobal.GetTimeOfDayMainValue(), 1.2f);
                                }
                            }
                            EditorGUILayout.Space();

                            m_editorUtils.Heading("CloudSettings");
                            EditorGUI.indentLevel++;
                            EditorGUI.BeginChangeCheck();

                            m_globalWeather.EnableClouds = m_editorUtils.Toggle("EnableClouds", m_globalWeather.EnableClouds, helpEnabled);
                            if (m_globalWeather.EnableClouds)
                            {
                                EditorGUI.indentLevel++;
                                m_profileValues.m_pwSkyAtmosphereData.CloudGPUInstanced = m_editorUtils.Toggle("CloudGPUInstancing", m_profileValues.m_pwSkyAtmosphereData.CloudGPUInstanced, helpEnabled);
                                m_profileValues.m_pwSkyAtmosphereData.CloudRenderQueue = (GaiaConstants.CloudRenderQueue)m_editorUtils.EnumPopup("CloudRenderQueue", m_profileValues.m_pwSkyAtmosphereData.CloudRenderQueue, helpEnabled);
                                m_globalWeather.CloudRotationSpeedLow = m_editorUtils.Slider("CloudRotationSpeedLow", m_globalWeather.CloudRotationSpeedLow, -5f, 5f, helpEnabled);
                                m_globalWeather.CloudRotationSpeedMiddle = m_editorUtils.Slider("CloudRotationSpeedMiddle", m_globalWeather.CloudRotationSpeedMiddle, -5f, 5f, helpEnabled);
                                m_globalWeather.CloudRotationSpeedFar = m_editorUtils.Slider("CloudRotationSpeedFar", m_globalWeather.CloudRotationSpeedFar, -5f, 5f, helpEnabled);
                                m_globalWeather.CloudHeight = m_editorUtils.IntField("CloudHeight", m_globalWeather.CloudHeight, helpEnabled);
                                if (m_globalWeather.CloudHeight < 0)
                                {
                                    m_globalWeather.CloudHeight = 0;
                                }
                                m_globalWeather.CloudScale = m_editorUtils.Slider("CloudScale", m_globalWeather.CloudScale, 1f, 5f, helpEnabled);
                                m_globalWeather.CloudOffset = m_editorUtils.Slider("CloudOffset", m_globalWeather.CloudOffset, 1f, 500f, helpEnabled);
                                EditorGUI.indentLevel--;
                                EditorGUILayout.Space();

                                m_editorUtils.LabelField("LightingSettings", EditorStyles.boldLabel);
                                EditorGUI.indentLevel++;
                                //m_globalWeather.CloudBrightness = m_editorUtils.Slider("CloudBrightness", m_globalWeather.CloudBrightness, 0f, 8f, helpEnabled);
                                m_globalWeather.CloudDomeBrightness = m_editorUtils.CurveField("CloudDomeBrightness", m_globalWeather.CloudDomeBrightness, helpEnabled);
                                GaiaEditorUtils.DrawTimeOfDayLine(GaiaGlobal.GetTimeOfDayMainValue());
                                m_globalWeather.CloudFade = m_editorUtils.Slider("CloudFadeDistance", m_globalWeather.CloudFade, 0f, 500f, helpEnabled);
                                EditorGUI.indentLevel--;
                                EditorGUILayout.Space();

                                m_editorUtils.LabelField("CloudSetup", EditorStyles.boldLabel);
                                EditorGUI.indentLevel++;
                                m_globalWeather.TODCloudHeightLevelDensity = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODCloudHeightLevelDensity"), m_editorUtils.GetTooltip("TODCloudHeightLevelDensity")), m_globalWeather.TODCloudHeightLevelDensity);
                                m_editorUtils.InlineHelp("TODCloudHeightLevelDensity", helpEnabled);

                                m_globalWeather.TODCloudHeightLevelThickness = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODCloudHeightLevelThickness"), m_editorUtils.GetTooltip("TODCloudHeightLevelThickness")), m_globalWeather.TODCloudHeightLevelThickness);
                                m_editorUtils.InlineHelp("TODCloudHeightLevelThickness", helpEnabled);

                                m_globalWeather.TODCloudHeightLevelSpeed = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODCloudHeightLevelSpeed"), m_editorUtils.GetTooltip("TODCloudHeightLevelSpeed")), m_globalWeather.TODCloudHeightLevelSpeed);
                                m_editorUtils.InlineHelp("TODCloudHeightLevelSpeed", helpEnabled);

                                m_globalWeather.TODCloudOpacity = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("TODCloudOpacity"), m_editorUtils.GetTooltip("TODCloudOpacity")), m_globalWeather.TODCloudOpacity);
                                m_editorUtils.InlineHelp("TODCloudOpacity", helpEnabled);
                                GaiaEditorUtils.DrawTimeOfDayLine(GaiaGlobal.GetTimeOfDayMainValue(), 3.2f);
                                EditorGUI.indentLevel--;
                            }

                            EditorGUI.indentLevel--;
                            EditorGUILayout.Space();

                            if (EditorGUI.EndChangeCheck())
                            {
                                m_profileValues.m_pwSkyCloudData.Save(m_globalWeather);
                            }

                            EditorGUILayout.Space();
                            GUI.enabled = true;
                            m_editorUtils.LabelField("Weather", EditorStyles.boldLabel);

                            EditorGUILayout.HelpBox("You are using Procedural Worlds Sky which has time of day and weather. To edit Weather/Time Of Day setup go to 'Advanced Weather Settings'", MessageType.Info);

                            GUILayout.BeginHorizontal();
                            if (m_editorUtils.Button("OpenWeatherSettings"))
                            {
                                GaiaUtils.FocusWeatherObject();
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
#else
                    if (m_profileValues.m_profileType == GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                    {
                        GUI.enabled = true;
                        EditorGUILayout.HelpBox("The 'Procedural Worlds Sky' profile sky type is available in Gaia Pro only, please choose a different profile type.", MessageType.Warning);
                        GUI.enabled = false;
                    }
#endif
                }
                else
                {
                    if (!m_profileValues.m_userCustomProfile && !m_profile.m_lightingEditSettings)
                    {
                        GUI.enabled = false;
                    }
                    EditorGUI.BeginChangeCheck();
                    if (m_renderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
                    {
                        if (m_profile.m_enablePostProcessing)
                        {
                            m_editorUtils.Heading("PostProcessingSettings");
                            EditorGUI.indentLevel++;
                            if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
                            {
        #if UPPipeline
                                m_profileValues.PostProcessProfileURP = (VolumeProfile)m_editorUtils.ObjectField("PostProcessingProfile", m_profileValues.PostProcessProfileURP, typeof(VolumeProfile), false, helpEnabled);
        #endif
                            }
                            else if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.BuiltIn)
                            {
        #if UNITY_POST_PROCESSING_STACK_V2
                                m_profileValues.PostProcessProfileBuiltIn = (PostProcessProfile)m_editorUtils.ObjectField("PostProcessingProfile", m_profileValues.PostProcessProfileBuiltIn, typeof(PostProcessProfile), false, helpEnabled);
                                m_profileValues.m_directToCamera = m_editorUtils.Toggle("DirectToCamera", m_profileValues.m_directToCamera, helpEnabled);
                                if (m_profileValues.m_profileType == GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                                {
#if GAIA_PRO_PRESENT
                                    if (m_globalWeather != null)
                                    {
                                        m_globalWeather.TODPostProcessExposure = EditorGUILayout.CurveField(new GUIContent(m_editorUtils.GetTextValue("PostProcessingExpsoure"), m_editorUtils.GetTooltip("PostProcessingExpsoure")), m_globalWeather.TODPostProcessExposure);
                                        m_editorUtils.InlineHelp("PostProcessingExpsoure", helpEnabled);
                                        GaiaEditorUtils.DrawTimeOfDayLine(GaiaGlobal.GetTimeOfDayMainValue());
                                    }
#endif
                                }
                                else
                                {
                                    m_profileValues.m_postProcessExposure = m_editorUtils.FloatField("PostProcessingExpsoure", m_profileValues.m_postProcessExposure, helpEnabled);
                                }
        #else
                                EditorGUILayout.HelpBox("Post Processing is not installed. Install it from the package manager to use the post processing setup features.", MessageType.Info);
        #endif
                            }

                            EditorGUI.indentLevel--;
                            GUILayout.Space(10f);
                        }

                        m_editorUtils.Heading("SkyboxSettings");
                        EditorGUI.indentLevel++;
    #if !GAIA_PRO_PRESENT
                        if (m_profileValues.m_profileType == GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky && m_profileValues.m_userCustomProfile)
                        {
                            GUI.enabled = true;
                        }
    #endif
                        if (m_profileValues.m_profileType == GaiaConstants.GaiaLightingProfileType.Procedural)
                        {
                            m_profileValues.m_sunSize = m_editorUtils.Slider("SunSize", m_profileValues.m_sunSize, 0f, 1f, helpEnabled);
                            m_profileValues.m_sunConvergence = m_editorUtils.Slider("SunConvergence", m_profileValues.m_sunConvergence, 0f, 10f, helpEnabled);
                            m_profileValues.m_atmosphereThickness = m_editorUtils.Slider("AtmosphereThickness", m_profileValues.m_atmosphereThickness, 0f, 5f, helpEnabled);
                            m_profileValues.m_skyboxExposure = m_editorUtils.Slider("SkyboxExposure", m_profileValues.m_skyboxExposure, 0f, 8f, helpEnabled);
                            m_profileValues.m_skyboxTint = m_editorUtils.ColorField("SkyboxTint", m_profileValues.m_skyboxTint, helpEnabled);
                            m_profileValues.m_groundColor = m_editorUtils.ColorField("GroundColor", m_profileValues.m_groundColor, helpEnabled);
                        }
                        else if (m_profileValues.m_profileType == GaiaConstants.GaiaLightingProfileType.HDRI)
                        {
                            m_profileValues.m_skyboxHDRI = (Cubemap)m_editorUtils.ObjectField("HDRISkybox", m_profileValues.m_skyboxHDRI, typeof(Cubemap), false, helpEnabled, GUILayout.Height(16f));
                            m_profileValues.m_skyboxTint = m_editorUtils.ColorField("SkyboxTint", m_profileValues.m_skyboxTint, helpEnabled);
                            m_profileValues.m_skyboxExposure = m_editorUtils.Slider("SkyboxExposure", m_profileValues.m_skyboxExposure, 0f, 8f, helpEnabled);
                            m_profileValues.m_skyboxRotationOffset = m_editorUtils.Slider("SkyboxRotationOffset", m_profileValues.m_skyboxRotationOffset, -180f, 180f, helpEnabled);
                        }

                        EditorGUI.indentLevel--;
                        GUILayout.Space(10f);

                        m_editorUtils.Heading("SunSettings");
                        EditorGUI.indentLevel++;
                        m_profileValues.m_sunRotation = m_editorUtils.Slider("SunRotation", m_profileValues.m_sunRotation, 0f, 360f, helpEnabled);
                        m_profileValues.m_sunPitch = m_editorUtils.Slider("SunPitch", m_profileValues.m_sunPitch, 0f, 360f, helpEnabled);

                        m_profileValues.m_useKelvin = m_editorUtils.Toggle("UseKelvin", m_profileValues.m_useKelvin, helpEnabled);
                        if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.BuiltIn)
                        {
                            if (m_profileValues.m_useKelvin)
                            {
                                EditorGUI.BeginChangeCheck();
                                if (m_profile.m_kelvinTexture == null)
                                {
                                    EditorGUILayout.HelpBox("Kelvin texture is missing from the scene profile. Will try load up the default if this warning still shows then the texture could not be loaded.", MessageType.Warning);
                                    m_profile.m_kelvinTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("Kelvin Image.png"));
                                }
                                else
                                {
                                    GUIContent imageContent = new GUIContent(m_profile.m_kelvinTexture);
                                    EditorGUI.indentLevel++;
                                    EditorGUILayout.LabelField(new GUIContent(m_editorUtils.GetTextValue("KelvinGradient"), m_editorUtils.GetTooltip("KelvinGradient")), imageContent, GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
                                    GaiaEditorUtils.DrawKelvinLine(Mathf.InverseLerp(1000f, 20000f, m_profileValues.m_kelvinValue));
                                    m_profileValues.m_kelvinValue = m_editorUtils.Slider("KelvinValue", m_profileValues.m_kelvinValue, 1000f, 20000f, helpEnabled);
                                    EditorGUI.indentLevel--;
                                }

                                if (EditorGUI.EndChangeCheck())
                                {
                                    m_profileValues.m_sunColor = GaiaUtils.ExecuteKelvinColor(m_profileValues.m_kelvinValue);
                                }
                            }
                            else
                            {
                                m_profileValues.m_sunColor = m_editorUtils.ColorField("SunColor", m_profileValues.m_sunColor, helpEnabled);
                            }

                            m_profileValues.m_sunIntensity = m_editorUtils.FloatField("SunIntensity", m_profileValues.m_sunIntensity, helpEnabled);
                        }
                        else
                        {
                            if (m_profileValues.m_useKelvin)
                            {
                                EditorGUI.BeginChangeCheck();
                                if (m_profile.m_kelvinTexture == null)
                                {
                                    EditorGUILayout.HelpBox("Kelvin texture is missing from the scene profile. Will try load up the default if this warning still shows then the texture could not be loaded.", MessageType.Warning);
                                    m_profile.m_kelvinTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("Kelvin Image.png"));
                                }
                                else
                                {
                                    GUIContent imageContent = new GUIContent(m_profile.m_kelvinTexture);
                                    EditorGUI.indentLevel++;
                                    EditorGUILayout.LabelField(new GUIContent(m_editorUtils.GetTextValue("KelvinGradient"), m_editorUtils.GetTooltip("KelvinGradient")), imageContent, GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
                                    GaiaEditorUtils.DrawKelvinLine(Mathf.InverseLerp(1000f, 20000f, m_profileValues.m_kelvinValue));
                                    m_profileValues.m_kelvinValue = m_editorUtils.Slider("KelvinValue", m_profileValues.m_kelvinValue, 1000f, 20000f, helpEnabled);
                                    EditorGUI.indentLevel--;
                                }

                                if (EditorGUI.EndChangeCheck())
                                {
                                    m_profileValues.m_lWSunColor = GaiaUtils.ExecuteKelvinColor(m_profileValues.m_kelvinValue);
                                }
                            }
                            else
                            {
                                m_profileValues.m_lWSunColor = m_editorUtils.ColorField("SunColor", m_profileValues.m_lWSunColor, helpEnabled);
                            }

                            m_profileValues.m_lWSunIntensity = m_editorUtils.FloatField("SunIntensity", m_profileValues.m_lWSunIntensity, helpEnabled);
                        }

                        EditorGUI.indentLevel--;
                        GUILayout.Space(10f);

                        m_editorUtils.Heading("ShadowSettings");
                        EditorGUI.indentLevel++;
                        if (m_profileValues.m_profileType != GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                        {
                            m_profileValues.m_shadowStrength = m_editorUtils.Slider("SunShadowStrength", m_profileValues.m_shadowStrength, 0f, 1f, helpEnabled);
                        }
                        m_profileValues.m_shadowCastingMode = (LightShadows)m_editorUtils.EnumPopup("SunShadowCastingMode", m_profileValues.m_shadowCastingMode, helpEnabled);
                        m_profileValues.m_sunShadowResolution = (LightShadowResolution)m_editorUtils.EnumPopup("SunShadowResolution", m_profileValues.m_sunShadowResolution, helpEnabled);
                        m_profileValues.m_shadowDistance = m_editorUtils.Slider("ShadowDistance", m_profileValues.m_shadowDistance, 0f, 10000f, helpEnabled);
                        EditorGUI.indentLevel--;
                        GUILayout.Space(10f);

                        m_editorUtils.Heading("AmbientSettings");
                        EditorGUI.indentLevel++;
                        m_profileValues.m_ambientMode = (AmbientMode)m_editorUtils.EnumPopup("AmbientMode", m_profileValues.m_ambientMode, helpEnabled);
                        if ( m_profileValues.m_ambientMode == AmbientMode.Skybox)
                        {
                            m_profileValues.m_ambientIntensity = m_editorUtils.Slider("AmbientIntensity", m_profileValues.m_ambientIntensity, 0f, 10f, helpEnabled);
                            if (Lightmapping.lightingDataAsset == null)
                            {
                                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("AmbientSkiyboxLightmapAssetMissing"), MessageType.Warning);
                            }
                        }
                        else if ( m_profileValues.m_ambientMode == AmbientMode.Flat)
                        {
                            m_profileValues.m_skyAmbient = EditorGUILayout.ColorField(new GUIContent(m_editorUtils.GetTextValue("SkyAmbient"), m_editorUtils.GetTooltip("SkyAmbient")), m_profileValues.m_skyAmbient, true, false, true);
                            m_editorUtils.InlineHelp("SkyAmbient", helpEnabled);
                        }
                        else
                        {
                            m_profileValues.m_skyAmbient = EditorGUILayout.ColorField(new GUIContent(m_editorUtils.GetTextValue("SkyAmbient"), m_editorUtils.GetTooltip("SkyAmbient")), m_profileValues.m_skyAmbient, true, false, true);
                            m_editorUtils.InlineHelp("SkyAmbient", helpEnabled);

                            m_profileValues.m_equatorAmbient = EditorGUILayout.ColorField(new GUIContent(m_editorUtils.GetTextValue("EquatorAmbient"), m_editorUtils.GetTooltip("EquatorAmbient")), m_profileValues.m_equatorAmbient, true, false, true);
                            m_editorUtils.InlineHelp("EquatorAmbient", helpEnabled);

                            m_profileValues.m_groundAmbient = EditorGUILayout.ColorField(new GUIContent(m_editorUtils.GetTextValue("GroundAmbient"), m_editorUtils.GetTooltip("GroundAmbient")), m_profileValues.m_groundAmbient, true, false, true);
                            m_editorUtils.InlineHelp("GroundAmbient", helpEnabled);
                        }

                        EditorGUI.indentLevel--;
                        GUILayout.Space(10f);

                        if (m_profile.m_enableFog)
                        {
                            m_editorUtils.Heading("FogSettings");
                            EditorGUI.indentLevel++;
                            m_profileValues.m_fogMode = (FogMode)m_editorUtils.EnumPopup("FogMode", m_profileValues.m_fogMode, helpEnabled);
                            if (m_profileValues.m_fogMode == FogMode.Linear)
                            {
                                m_profileValues.m_fogColor = m_editorUtils.ColorField("FogColor", m_profileValues.m_fogColor, helpEnabled);
                                m_profileValues.m_fogStartDistance = m_editorUtils.FloatField("FogStartDistance", m_profileValues.m_fogStartDistance, helpEnabled);
                                m_profileValues.m_fogEndDistance = m_editorUtils.FloatField("FogEndDistance", m_profileValues.m_fogEndDistance, helpEnabled);
                            }
                            else
                            {
                                m_profileValues.m_fogColor = m_editorUtils.ColorField("FogColor", m_profileValues.m_fogColor, helpEnabled);
                                m_profileValues.m_fogDensity = m_editorUtils.Slider("FogDensity", m_profileValues.m_fogDensity, 0f, 1f, helpEnabled);
                            }
                            m_profileValues.m_skyboxFogHeight = m_editorUtils.IntSlider("TODSkyboxFogHeight", m_profileValues.m_skyboxFogHeight, -1000, 8000, helpEnabled);
                            m_profileValues.m_skyboxFogGradient = m_editorUtils.Slider("TODSkyboxFogGradient", m_profileValues.m_skyboxFogGradient, 0f, 1f, helpEnabled);

                            EditorGUI.indentLevel--;
                            GUILayout.Space(10f);
                        }
                    }
                    else
                    {
                        EditorGUI.BeginChangeCheck();

                        m_editorUtils.Heading("SunSettings");
                        EditorGUI.indentLevel++;
                        m_profileValues.m_sunRotation = m_editorUtils.Slider("SunRotation", m_profileValues.m_sunRotation, 0f, 360f, helpEnabled);
                        m_profileValues.m_sunPitch = m_editorUtils.Slider("SunPitch", m_profileValues.m_sunPitch, 0f, 360f, helpEnabled);

                        m_profileValues.m_useKelvin = m_editorUtils.Toggle("UseKelvin", m_profileValues.m_useKelvin, helpEnabled);
                        if (m_profileValues.m_useKelvin)
                        {
                            EditorGUI.BeginChangeCheck();
                            if (m_profile.m_kelvinTexture == null)
                            {
                                EditorGUILayout.HelpBox("Kelvin texture is missing from the scene profile. Will try load up the default if this warning still shows then the texture could not be loaded.", MessageType.Warning);
                                m_profile.m_kelvinTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("Kelvin Image.png"));
                            }
                            else
                            {
                                GUIContent imageContent = new GUIContent(m_profile.m_kelvinTexture);
                                EditorGUI.indentLevel++;
                                EditorGUILayout.LabelField(new GUIContent(m_editorUtils.GetTextValue("KelvinGradient"), m_editorUtils.GetTooltip("KelvinGradient")), imageContent, GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth));
                                GaiaEditorUtils.DrawKelvinLine(Mathf.InverseLerp(1000f, 20000f, m_profileValues.m_kelvinValue));
                                m_profileValues.m_kelvinValue = m_editorUtils.Slider("KelvinValue", m_profileValues.m_kelvinValue, 1000f, 20000f, helpEnabled);
                                EditorGUI.indentLevel--;
                            }

                            if (EditorGUI.EndChangeCheck())
                            {
                                m_profileValues.m_hDSunColor = GaiaUtils.ExecuteKelvinColor(m_profileValues.m_kelvinValue);
                            }
                        }
                        else
                        {
                            m_profileValues.m_hDSunColor = m_editorUtils.ColorField("SunColor", m_profileValues.m_hDSunColor, helpEnabled);
                        }

                        m_profileValues.m_hDSunIntensity = m_editorUtils.FloatField("SunIntensity", m_profileValues.m_hDSunIntensity, helpEnabled);
                        m_profileValues.m_hDSunVolumetricMultiplier = m_editorUtils.Slider("VolumetricLightMultiplier", m_profileValues.m_hDSunVolumetricMultiplier, 0f, 16f, helpEnabled);
                        EditorGUI.indentLevel--;
                        EditorGUILayout.Space();

                        m_editorUtils.Heading("VolumeSettings");
                        EditorGUI.indentLevel++;
#if HDPipeline
                        m_profileValues.PostProcessProfileHDRP = (VolumeProfile)m_editorUtils.ObjectField("PostProcessingProfile", m_profileValues.PostProcessProfileHDRP, typeof(VolumeProfile), false, helpEnabled);
                        m_profileValues.EnvironmentProfileHDRP = (VolumeProfile)m_editorUtils.ObjectField("EnvironmentProfile", m_profileValues.EnvironmentProfileHDRP, typeof(VolumeProfile), false, helpEnabled);
#endif
                        EditorGUI.indentLevel--;

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (!m_profile.m_lightingUpdateInRealtime)
                            {
                                if (m_profile.m_selectedLightingProfileValuesIndex != -99)
                                {
                                    GaiaUtils.GetRuntimeSceneObject();
                                    GaiaLighting.GetProfile(m_profile, m_gaiaSettings.m_pipelineProfile, m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, true);
                                }
                            }
                        }

                        if (m_editorUtils.Button("EditLightingSettigns"))
                        {
                            GaiaSceneManagement.SelectHDRPLighting();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }

            GUI.enabled = true;

            if (m_profile.m_lightingEditSettings)
            {
                if (m_editorUtils.Button("SaveToGaiaDefaultProfile"))
                {
                    if (m_sceneLighting != null)
                    {
#if GAIA_PRO_PRESENT
                        m_sceneLighting.SaveToGaiaDefault(m_profileValues, m_globalWeather);
#else
                        m_sceneLighting.SaveToGaiaDefault(m_profileValues);
#endif
                        EditorGUIUtility.ExitGUI();
                    }
                }

                if (m_editorUtils.Button("ConvertToPipeline"))
                {
                    if (m_sceneLighting != null)
                    {
                        switch (m_renderPipeline)
                        {
                            case GaiaConstants.EnvironmentRenderer.Universal:
                                SRPConversionUtils.ConvertProfileToURP(m_profile, m_profileValues);
                                break;
                            case GaiaConstants.EnvironmentRenderer.HighDefinition:
                                SRPConversionUtils.ConvertProfileToHDRP(m_profile, m_profileValues);
                                break;
                            case GaiaConstants.EnvironmentRenderer.BuiltIn:
                                Debug.Log("Conversion is not available for Built-In please change your render pipeline in the Gaia Manager Setup tab.");
                                break;
                        }
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (m_profileValues.m_userCustomProfile)
                {
#if GAIA_PRO_PRESENT
                    if (m_globalWeather != null)
                    {
                        if (m_profileValues != null)
                        {
                            m_profileValues.m_pwSkyAtmosphereData.Save(m_globalWeather);
                        }
                    }
#endif
                }
                else if (m_profile.m_lightingEditSettings)
                {
#if GAIA_PRO_PRESENT
                    if (m_globalWeather != null)
                    {
                        if (m_profileValues != null)
                        {
                            m_profileValues.m_pwSkyAtmosphereData.Save(m_globalWeather);
                        }
                    }
#endif
                }
            }
        }
        private void SaveAndLoad(bool helpEnabled)
        {
            bool canBeSaved = true;
            if (!m_profile.DefaultLightingSet)
            {
                EditorGUILayout.HelpBox("No lighting profile settings have been saved. Please use gaia default lighting profile to save them here.", MessageType.Warning);
                canBeSaved = false;
            }

            EditorGUILayout.BeginHorizontal();
            if (!canBeSaved)
            {
                GUI.enabled = false;
            }

            //string path = Application.dataPath + "/Assets";
            string path = "";
            if (m_editorUtils.Button("SaveToFile"))
            {
                string sceneName = EditorSceneManager.GetActiveScene().name;
                if (string.IsNullOrEmpty(sceneName))
                {
                    Debug.LogWarning("Unable to save file as scene has not been saved. Please save your scene then try again.");
                }
               
                path = EditorUtility.SaveFilePanelInProject("Save Location", sceneName + " Profile", "asset" , "Save new profile asset");
                GaiaSceneManagement.SaveFile(m_profile, path);
                EditorGUIUtility.ExitGUI();
            }

            GUI.enabled = true;

            if (m_editorUtils.Button("LoadFromFile"))
            {
                path = EditorUtility.OpenFilePanel("Load Profile", Application.dataPath + "/Assets", "asset");
                GaiaSceneManagement.LoadLighting(path);
                EditorGUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }
        private void ThirdPartyPanel(bool helpEnabled)
        {
            if (m_profile.m_thirdPartyLightObject != null)
            {
                EditorGUILayout.LabelField("Current Active Third Party System: " + m_profile.m_thirdPartyLightObject.name, EditorStyles.boldLabel);
            }

            EditorGUILayout.BeginHorizontal();
            m_profile.m_thirdPartyLightObject = (GameObject)m_editorUtils.ObjectField("ThirdPartySystem", m_profile.m_thirdPartyLightObject, typeof(GameObject), true);
            if (m_editorUtils.Button("EditThirdParty", GUILayout.MaxWidth(50f)))
            {
                if (m_profile.m_thirdPartyLightObject == null)
                {
                    Debug.LogWarning("No third party game object has been set");
                }
                else
                {
                    Selection.activeObject = m_profile.m_thirdPartyLightObject;
                }
            }
            EditorGUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("ThirdPartySystem", helpEnabled);
        }
        private void LightAndReflectionProbeSettings(bool helpEnabled)
        {
            probeSpawnCount = (m_profile.m_reflectionProbeData.reflectionProbesPerRow * m_profile.m_reflectionProbeData.reflectionProbesPerRow).ToString();
            lightProbeSpawnCount = (m_profile.m_reflectionProbeData.lightProbesPerRow * m_profile.m_reflectionProbeData.lightProbesPerRow).ToString();

            EditorGUI.BeginChangeCheck();

            m_editorUtils.Heading("ReflectionProbeSettings");
            EditorGUI.indentLevel++;
            m_profile.m_reflectionProbeData.reflectionProbeMode = (ReflectionProbeMode)m_editorUtils.EnumPopup("ReflectionProbeMode", m_profile.m_reflectionProbeData.reflectionProbeMode, helpEnabled);
            if (m_profile.m_reflectionProbeData.reflectionProbeMode != ReflectionProbeMode.Baked)
            {
                m_profile.m_reflectionProbeData.reflectionProbeRefresh = (GaiaConstants.ReflectionProbeRefreshModePW)m_editorUtils.EnumPopup("ReflectionProbeRefresh", m_profile.m_reflectionProbeData.reflectionProbeRefresh, helpEnabled);
            }

            m_editorUtils.LabelField("ProbePlacementSettings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            m_profile.m_reflectionProbeData.reflectionProbesPerRow = m_editorUtils.IntField("ReflectionProbesPerRow", m_profile.m_reflectionProbeData.reflectionProbesPerRow, helpEnabled);
            m_profile.m_reflectionProbeData.reflectionProbeOffset = m_editorUtils.FloatField("ReflectionProbeOffset", m_profile.m_reflectionProbeData.reflectionProbeOffset, helpEnabled);
            Terrain t = Terrain.activeTerrain;
            if (t != null)
            {
                int probeDist = 100;
                probeDist = (int)(t.terrainData.size.x / m_profile.m_reflectionProbeData.reflectionProbesPerRow);
                m_editorUtils.LabelField("ProbeDistanceLbl", new GUIContent(probeDist.ToString()));
                m_editorUtils.InlineHelp("ProbeDistanceLbl", helpEnabled);
            }
            m_editorUtils.LabelField("ProbesPerTerrainLbl", new GUIContent(probeSpawnCount));
            m_editorUtils.InlineHelp("ProbesPerTerrainLbl", helpEnabled);
            if (m_profile.m_reflectionProbeData.reflectionProbesPerRow < 2)
            {
                EditorGUILayout.HelpBox("Please set a value of 2 or higher in the Probes Per Row", MessageType.Warning);
            }
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space();

            m_editorUtils.LabelField("Probe Optimization Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            if (m_profile.m_reflectionProbeData.reflectionProbeRefresh == GaiaConstants.ReflectionProbeRefreshModePW.ViaScripting && m_profile.m_reflectionProbeData.reflectionProbeMode != ReflectionProbeMode.Baked)
            {
                m_profile.m_reflectionProbeData.reflectionProbeTimeSlicingMode = (ReflectionProbeTimeSlicingMode)m_editorUtils.EnumPopup("ReflectionProbeTimeSlicing", m_profile.m_reflectionProbeData.reflectionProbeTimeSlicingMode, helpEnabled);
            }
            if (m_renderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                m_profile.m_reflectionProbeData.reflectionProbeResolution = (GaiaConstants.ReflectionProbeResolution)m_editorUtils.EnumPopup("ReflectionProbeResolution", m_profile.m_reflectionProbeData.reflectionProbeResolution, helpEnabled);
                m_profile.m_reflectionProbeData.reflectionCubemapCompression = (ReflectionCubemapCompression)m_editorUtils.EnumPopup("ReflectionProbeCompression", m_profile.m_reflectionProbeData.reflectionCubemapCompression, helpEnabled);
            }
            m_profile.m_reflectionProbeData.reflectionProbeClipPlaneDistance = m_editorUtils.Slider("ReflectionProbeRenderDistance", m_profile.m_reflectionProbeData.reflectionProbeClipPlaneDistance, 0.1f, 10000f, helpEnabled);
            m_profile.m_reflectionProbeData.reflectionProbeShadowDistance = m_editorUtils.Slider("ReflectionProbeShadowDistance", m_profile.m_reflectionProbeData.reflectionProbeShadowDistance, 0.1f, 3000f, helpEnabled);
            m_profile.m_reflectionProbeData.reflectionprobeCullingMask = GaiaEditorUtils.LayerMaskField(new GUIContent(m_editorUtils.GetTextValue("ReflectionProbeCullingMask"), m_editorUtils.GetTooltip("ReflectionProbeCullingMask")), m_profile.m_reflectionProbeData.reflectionprobeCullingMask);
            m_editorUtils.InlineHelp("ReflectionProbeCullingMask", helpEnabled);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            if (m_editorUtils.Button("Generate Global Scene Reflection Probes"))
            {
                if (EditorUtility.DisplayDialog("Warning!", "You're about to generate reflection probes to cover your whole terrain. Depending on your terrain size and Probe Per Row count this could take some time. Would you like to proceed?", "Yes", "No"))
                {
                    ReflectionProbeEditorUtils.CreateAutomaticProbes(m_profile.m_reflectionProbeData, m_renderPipeline);
                }
            }
            if (m_editorUtils.Button("Clear Created Reflection Probes"))
            {
                if (EditorUtility.DisplayDialog("Warning!", "You are about to clear all your reflection probes you created. Are you sure you want to proceed?", "Yes", "No"))
                {
                    ReflectionProbeEditorUtils.ClearCreatedReflectionProbes();
                }
            }

            if (ReflectionProbeEditorUtils.m_currentProbeCount > 0 && m_renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                EditorGUILayout.HelpBox("To allow the sky to be affected in the reflection probe bake be sure to set the Volume Layer Mask to Transparent FX layer.", MessageType.Info);
            }
            EditorGUI.indentLevel--;

            m_editorUtils.Heading("LightProbeSettings");
            EditorGUI.indentLevel++;
            m_profile.m_reflectionProbeData.lightProbesPerRow = m_editorUtils.IntField("LightProbesPerRow", m_profile.m_reflectionProbeData.lightProbesPerRow, helpEnabled);
            if (m_profile.m_reflectionProbeData.lightProbesPerRow < 2)
            {
                EditorGUILayout.HelpBox("Please set a value of 2 or higher in the Light Probes Per Row", MessageType.Warning);
            }

            if (t != null)
            {
                int probeDist = 100;
                probeDist = (int)(t.terrainData.size.x / m_profile.m_reflectionProbeData.lightProbesPerRow);
                m_editorUtils.LabelField("ProbeDistanceLbl", new GUIContent(probeDist.ToString()));
                m_editorUtils.InlineHelp("ProbeDistanceLbl", helpEnabled);
            }

            m_editorUtils.LabelField("ProbesPerTerrainLbl", new GUIContent(lightProbeSpawnCount));
            m_editorUtils.InlineHelp("ProbesPerTerrainLbl", helpEnabled);

            if (m_editorUtils.Button("Generate Global Scene Light Probes"))
            {
                if (EditorUtility.DisplayDialog("Warning!", "You're about to generate light probes to cover your whole terrain. Depending on your terrain size and Probe Per Row count this could take some time. Would you like to proceed?", "Yes", "No"))
                {
                    LightProbeUtils.CreateAutomaticProbes(m_profile.m_reflectionProbeData);
                }
            }

            if (m_editorUtils.Button("Clear Created Light Probes"))
            {
                if (EditorUtility.DisplayDialog("Warning!", "You are about to clear all your light probes you created. Are you sure you want to proceed?", "Yes", "No"))
                {
                    LightProbeUtils.ClearCreatedLightProbes();
                }
            }

            if (LightProbeUtils.m_currentProbeCount > 0)
            {
                EditorGUILayout.HelpBox("Light Probes need to be baked. Go to the Light Bakin Tab in the Gaia Manager and press 'Full Lightmap Bake' to bake your lighting and Light Probes", MessageType.Info);
            }
            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_profile, "Made changes");
                EditorUtility.SetDirty(m_profile);
            }
        }
        private void EditorUpdate()
        {
            //Bake Probes
            if (ReflectionProbeEditorUtils.m_probeRenderActive)
            {
                if (ReflectionProbeEditorUtils.m_storedProbes.Count > 0)
                {
                    float progrss = (float)(ReflectionProbeEditorUtils.m_currentProbeCount - ReflectionProbeEditorUtils.m_storedProbes.Count) / ReflectionProbeEditorUtils.m_currentProbeCount;
                    EditorUtility.DisplayProgressBar("Baking Reflection Probes", "Probes remaining :" + ReflectionProbeEditorUtils.m_storedProbes.Count, progrss);
                    if(ReflectionProbeEditorUtils.m_storedProbes[0]!=null)
                        ReflectionProbeEditorUtils.m_storedProbes[0].enabled = true;
                    ReflectionProbeEditorUtils.m_storedProbes.RemoveAt(0);
                }
                else
                {
                    ReflectionProbeEditorUtils.m_probeRenderActive = false;
                    EditorUtility.ClearProgressBar();
                }
            }
        }
        /// <summary>
        /// Adds a new user profile
        /// </summary>
        private void AddNewCustomProfile()
        {
#if GAIA_PRO_PRESENT
            ProceduralWorldsGlobalWeather.SaveValuesToLightProfile(m_profileValues, m_globalWeather);
#endif
            GaiaLightingProfileValues selectdValues = m_profileValues;
            GaiaLightingProfileValues newProfile = new GaiaLightingProfileValues();
            GaiaUtils.CopyFields(selectdValues, newProfile);
            int count = m_profile.m_lightingProfiles.Count + 1;
            newProfile.m_typeOfLighting = "New User Profile " + count;
            newProfile.m_userCustomProfile = true;
#if UNITY_POST_PROCESSING_STACK_V2
            newProfile.PostProcessProfileBuiltIn = selectdValues.PostProcessProfileBuiltIn;
#endif
            GaiaSessionManager session = GaiaSessionManager.GetSessionManager();
#if UPPipeline
            VolumeProfile currentFXURPProfile = selectdValues.PostProcessProfileURP;
            if (session != null && currentFXURPProfile != null)
            {
                string path = GaiaDirectories.GetSceneProfilesFolderPath(session.m_session);
                if (!string.IsNullOrEmpty(path))
                {
                    path = path + "/" + "User Created " + currentFXURPProfile.name + " Profile " + count + ".asset";

                    if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) == null)
                    {
                        FileUtil.CopyFileOrDirectory(AssetDatabase.GetAssetPath(currentFXURPProfile), path);
                        AssetDatabase.ImportAsset(path);
                    }

                    newProfile.PostProcessProfileURP = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                }
            }
#endif
#if HDPipeline
            VolumeProfile currentFXProfile = selectdValues.PostProcessProfileHDRP;
            if (session != null && currentFXProfile != null)
            {
                string path = GaiaDirectories.GetSceneProfilesFolderPath(session.m_session);
                if (!string.IsNullOrEmpty(path))
                {
                    path = path + "/" + "User Created " + currentFXProfile.name + " Profile " + count + ".asset";

                    if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) == null)
                    {
                        FileUtil.CopyFileOrDirectory(AssetDatabase.GetAssetPath(currentFXProfile), path);
                        AssetDatabase.ImportAsset(path);
                    }

                    newProfile.PostProcessProfileHDRP = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                }
            }

            VolumeProfile currentProfile = selectdValues.EnvironmentProfileHDRP;
            if (session != null && currentProfile != null)
            {
                string path = GaiaDirectories.GetSceneProfilesFolderPath(session.m_session);
                if (!string.IsNullOrEmpty(path))
                {
                    path = path + "/" + "User Created " + currentProfile.name + " Profile " + count + ".asset";

                    if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) == null)
                    {
                        FileUtil.CopyFileOrDirectory(AssetDatabase.GetAssetPath(currentProfile), path);
                        AssetDatabase.ImportAsset(path);
                    }

                    newProfile.EnvironmentProfileHDRP = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                }
            }
#endif
            m_profile.m_lightingProfiles.Add(newProfile);
            m_profile.m_selectedLightingProfileValuesIndex = m_profile.m_lightingProfiles.Count - 1;
            m_profileValues = newProfile;
            //New PW Sky data
#if GAIA_PRO_PRESENT
                m_profileValues.m_pwSkyAtmosphereData = new PWSkyAtmosphere();
                GaiaUtils.CopyFields(selectdValues.m_pwSkyAtmosphereData, m_profileValues.m_pwSkyAtmosphereData);
                m_profileValues.m_pwSkyAtmosphereData.New(m_globalWeather);
                m_profileValues.m_pwSkyWindData = new PWSkyWind();
                GaiaUtils.CopyFields(selectdValues.m_pwSkyWindData, m_profileValues.m_pwSkyWindData);
                m_profileValues.m_pwSkyWindData.New(m_globalWeather);
                m_profileValues.m_pwSkyCloudData = new PWSkyCloud();
                GaiaUtils.CopyFields(selectdValues.m_pwSkyCloudData, m_profileValues.m_pwSkyCloudData);
                m_profileValues.m_pwSkyCloudData.New(m_globalWeather);
                m_profileValues.m_pwSkySeasonData = new PWSkySeason();
                GaiaUtils.CopyFields(selectdValues.m_pwSkySeasonData, m_profileValues.m_pwSkySeasonData);
                m_profileValues.m_pwSkySeasonData.New(m_globalWeather);
                m_profileValues.m_pwSkyWeatherData = new PWSkyWeather();
                GaiaUtils.CopyFields(selectdValues.m_pwSkyWeatherData, m_profileValues.m_pwSkyWeatherData);
                m_profileValues.m_pwSkyWeatherData.New(m_globalWeather, selectdValues.m_pwSkyWeatherData, m_profileValues.m_pwSkyWeatherData);
#endif

            if (m_profile.m_selectedLightingProfileValuesIndex != -99)
            {
                GaiaUtils.GetRuntimeSceneObject();
                GaiaLighting.GetProfile(m_profile, m_gaiaSettings.m_pipelineProfile, m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled);
            }

            EditorUtility.SetDirty(m_profile);
            AssetDatabase.SaveAssets();

        }

        #endregion
    }
}