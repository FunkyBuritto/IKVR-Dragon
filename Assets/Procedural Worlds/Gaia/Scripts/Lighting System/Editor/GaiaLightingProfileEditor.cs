using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using Gaia.Internal;
using PWCommon4;
using System.Linq;
using System.Collections.Generic;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace Gaia
{
    [CustomEditor(typeof(GaiaLightingProfile))]
    public class GaiaLightingProfileEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private string m_version;
        private Color defaultBackground;
        private GaiaSettings m_gaiaSettings;
        private GaiaLightingProfile m_profile;
        private GaiaLightingProfileValues m_profileValues;
        private GaiaConstants.EnvironmentRenderer m_renderPipeline;
        private bool enableEditMode;

        public void OnEnable()
        {
            //Get Gaia Lighting Profile object
            m_profile = (GaiaLightingProfile)target;

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
            m_version = PWApp.CONF.Version;

            if (m_profile != null)
            {
                m_profileValues = m_profile.m_lightingProfiles[m_profile.m_selectedLightingProfileValuesIndex];
            }

            enableEditMode = System.IO.Directory.Exists(GaiaUtils.GetAssetPath("Dev Utilities"));
        }

        public override void OnInspectorGUI()
        {
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!

            //Monitor for changes
            EditorGUI.BeginChangeCheck();

            defaultBackground = GUI.backgroundColor;

            if (m_gaiaSettings == null)
            {
                m_gaiaSettings = GaiaUtils.GetGaiaSettings();
            }

            if (m_renderPipeline != m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled)
            {
                m_renderPipeline = m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled;
            }

            if (m_profile.m_selectedLightingProfileValuesIndex > m_profile.m_lightingProfiles.Count - 1)
            {
                m_profile.m_selectedLightingProfileValuesIndex = 0;
            }

            EditorGUILayout.LabelField("Profile Version: " + m_version);
            if (enableEditMode)
            {
                m_profile.m_editSettings = EditorGUILayout.ToggleLeft("Use Procedural Worlds Editor Settings", m_profile.m_editSettings);
            }
            else
            {
                m_profile.m_editSettings = false;
            }

            if (m_profile.m_editSettings)
            {
                m_editorUtils.Panel("UpdateSettings", RealtimeUpdateEnabled);
                m_editorUtils.Panel("GlobalSettings", GlobalSettingsEnabled);
                m_editorUtils.Panel("LightingProfileSettings", LightingProfileSettingsEnabled);

                DrawDefaultInspector();
            }

            //Check for changes, make undo record, make changes and let editor know we are dirty
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_profile, "Made changes");
                EditorUtility.SetDirty(m_profile);

                if (m_profile.m_updateInRealtime)
                {
                    if (m_profile.m_selectedLightingProfileValuesIndex != -99)
                    {
                        GaiaUtils.GetRuntimeSceneObject();

                        if (GaiaGlobal.Instance != null)
                        {
                            //GaiaLighting.GetProfile(m_profile, m_gaiaSettings.m_pipelineProfile, m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled);
                        }
                    }
                    EditorUtility.SetDirty(m_profile);
                }
            }
        }

        private void RealtimeUpdateEnabled(bool helpEnabled)
        {
            m_profile.m_updateInRealtime = m_editorUtils.ToggleLeft("UpdateChangesInRealtime", m_profile.m_updateInRealtime, helpEnabled);
            if (m_profile.m_updateInRealtime)
            {
                EditorGUILayout.HelpBox("Update In Realtime is enabled this will allow profiles to be endited inside the editor and automatically apply changes every frame. This feature can be expensive and should not be left enabled in testing and builds", MessageType.Warning);
            }
            else
            {
                if (m_editorUtils.Button("UpdateToScene"))
                {
                    if (m_profile.m_selectedLightingProfileValuesIndex != -99)
                    {
                        GaiaUtils.GetRuntimeSceneObject();

                        if (GaiaGlobal.Instance != null)
                        {
                            //GaiaLighting.GetProfile(m_profile, m_gaiaSettings.m_pipelineProfile, m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled);
                            EditorUtility.SetDirty(m_profile);
                        }
                    }
                }

                m_editorUtils.InlineHelp("UpdateToScene", helpEnabled);

                if (m_editorUtils.Button("UpdateDynamicGI"))
                {
                    GaiaLighting.UpdateAmbientEnvironment();
                }

                m_editorUtils.InlineHelp("UpdateDynamicGI", helpEnabled);
            }
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
            m_profile.m_multiSceneLightingSupport = m_editorUtils.Toggle("MultiSceneSupport", m_profile.m_multiSceneLightingSupport, helpEnabled);
            if (m_renderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                m_profile.m_masterSkyboxMaterial = (Material)m_editorUtils.ObjectField("MasterSkyboxMaterial", m_profile.m_masterSkyboxMaterial, typeof(Material), false, GUILayout.Height(16f));
            }
            EditorGUILayout.Space();

            m_editorUtils.Heading("LightmappingSettings");
            m_profile.m_lightingBakeMode = (GaiaConstants.BakeMode)m_editorUtils.EnumPopup("LightmappingBakeMode", m_profile.m_lightingBakeMode, helpEnabled);
#if UNITY_2020_1_OR_NEWER
            m_profile.m_lightmappingMode = (LightingSettings.Lightmapper)EditorGUILayout.EnumPopup("Lightmapping Mode", m_profile.m_lightmappingMode);
#else
            m_profile.m_lightmappingMode = (LightmapEditorSettings.Lightmapper)EditorGUILayout.EnumPopup("Lightmapping Mode", m_profile.m_lightmappingMode);
#endif
            EditorGUILayout.Space();

            m_editorUtils.Heading("PostProcessingSettings");
            m_profile.m_enablePostProcessing = m_editorUtils.ToggleLeft("EnablePostProcessing", m_profile.m_enablePostProcessing);
            if (m_profile.m_enablePostProcessing)
            {
                m_profile.m_hideProcessVolume = m_editorUtils.ToggleLeft("HidePostProcessingVolumesInScene", m_profile.m_hideProcessVolume);
                m_profile.m_antiAliasingMode = (GaiaConstants.GaiaProAntiAliasingMode)EditorGUILayout.EnumPopup("Anti-Aliasing Mode", m_profile.m_antiAliasingMode);
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
            EditorGUILayout.Space();

            m_editorUtils.Heading("CameraSettings");
            m_profile.m_enableAutoDOF = m_editorUtils.Toggle("UseAutoDOF", m_profile.m_enableAutoDOF, helpEnabled);
            if (m_profile.m_enableAutoDOF)
            {
                m_profile.m_dofLayerDetection = GaiaEditorUtils.LayerMaskField(new GUIContent(m_editorUtils.GetTextValue("DOFLayerDetection"), m_editorUtils.GetTooltip("DOFLayerDetection")), m_profile.m_dofLayerDetection);
            }
            EditorGUILayout.Space();

            m_profile.m_usePhysicalCamera = m_editorUtils.Toggle("UsePhysicalCamera", m_profile.m_usePhysicalCamera, helpEnabled);
            if (m_profile.m_usePhysicalCamera)
            {
                m_profile.m_cameraFocalLength = m_editorUtils.FloatField("CameraFocalLength", m_profile.m_cameraFocalLength, helpEnabled);
                m_profile.m_cameraSensorSize = m_editorUtils.Vector2Field("CameraSensorSize", m_profile.m_cameraSensorSize, helpEnabled);
            }
            EditorGUILayout.Space();

            m_editorUtils.Heading("MiscellaneousSettings");
            m_profile.m_globalReflectionProbe = m_editorUtils.ToggleLeft("GlobalReflectionProbe", m_profile.m_globalReflectionProbe);
            m_profile.m_parentObjects = m_editorUtils.ToggleLeft("ParentObjectsToGaia", m_profile.m_parentObjects);
            m_profile.m_enableAmbientAudio = m_editorUtils.ToggleLeft("EnableAmbientAudio", m_profile.m_enableAmbientAudio);

            if (m_renderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                m_profile.m_enableFog = m_editorUtils.ToggleLeft("EnableFog", m_profile.m_enableFog);
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
        }

        private void LightingProfileSettingsEnabled(bool helpEnabled)
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
            EditorGUILayout.BeginHorizontal();
            if (m_profile.m_renamingProfile)
            {
                m_profileValues.m_profileRename = m_editorUtils.TextField("NewProfileName", m_profileValues.m_profileRename);
            }
            else
            {
                m_profile.m_selectedLightingProfileValuesIndex = EditorGUILayout.IntPopup("Lighting Profile", m_profile.m_selectedLightingProfileValuesIndex, profileNames, lightingProfileValuesIndices);
            }

#if !GAIA_PRO_PRESENT
            bool currentGUIState = GUI.enabled;
            if (m_profileValues.m_profileType == GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
            {
                GUI.enabled = false;
            }
#endif
            if (m_profile.m_selectedLightingProfileValuesIndex != -99)
            {
                m_profileValues = m_profile.m_lightingProfiles[m_profile.m_selectedLightingProfileValuesIndex];
                m_profileValues.m_ambientMode = RenderSettings.ambientMode;
            }

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
                }
            }
            if (m_editorUtils.Button("MakeCopy", GUILayout.MaxWidth(30f)))
            {
                AddNewCustomProfile();
            }
            EditorGUILayout.EndHorizontal();

            if (m_profile.m_selectedLightingProfileValuesIndex == -99)
            {
                EditorGUILayout.HelpBox("No Profile selected. Select another profile that is not 'None' to view settings to edit", MessageType.Info);
            }
            else
            {
                if (!m_profileValues.m_userCustomProfile)
                {
                    m_profile.m_renamingProfile = false;
                    EditorGUILayout.HelpBox("This is one of the default Gaia lighting profiles. To create your own please press the '+' button to create a copy from the current selected profile.", MessageType.Info);
                    GUI.enabled = false;
                }

                if (m_profile.m_editSettings)
                {
                    GUI.enabled = true;
                }

                if (m_renderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    if (m_profile.m_enablePostProcessing)
                    {
                        m_editorUtils.Heading("PostProcessingSettings");
                        if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
                        {
#if UPPipeline
                            m_profileValues.PostProcessProfileURP = (VolumeProfile)m_editorUtils.ObjectField("PostProcessingProfile", m_profileValues.PostProcessProfileURP, typeof(VolumeProfile), false);
#endif
                        }
                        else
                        {
#if UNITY_POST_PROCESSING_STACK_V2
                            m_profileValues.PostProcessProfileBuiltIn = (PostProcessProfile)m_editorUtils.ObjectField("PostProcessingProfile", m_profileValues.PostProcessProfileBuiltIn, typeof(PostProcessProfile), false);
                            m_profileValues.m_directToCamera = m_editorUtils.Toggle("DirectToCamera", m_profileValues.m_directToCamera);
#else
                            EditorGUILayout.HelpBox("Post Processing is not installed. Install it from the package manager to use the post processing setup features.", MessageType.Info);
#endif
                        }
                        GUILayout.Space(20f);
                    }


                    if (m_profileValues.m_profileType == GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                    {
                        //m_profileValues.m_sunRotation = m_editorUtils.Slider("SunRotation", m_profileValues.m_sunRotation, 0f, 90f);
                    }

                    if (m_profileValues.m_profileType != GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                    {
                        m_editorUtils.Heading("SunSettings");
                        m_profileValues.m_sunRotation = m_editorUtils.Slider("SunRotation", m_profileValues.m_sunRotation, 0f, 360f);
                        m_profileValues.m_sunPitch = m_editorUtils.Slider("SunPitch", m_profileValues.m_sunPitch, 0f, 360f);
                        if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.BuiltIn)
                        {
                            m_profileValues.m_sunColor = m_editorUtils.ColorField("SunColor", m_profileValues.m_sunColor);
                            m_profileValues.m_sunIntensity = m_editorUtils.FloatField("SunIntensity", m_profileValues.m_sunIntensity);
                        }
                        else
                        {
                            m_profileValues.m_lWSunColor = m_editorUtils.ColorField("SunColor", m_profileValues.m_lWSunColor);
                            m_profileValues.m_lWSunIntensity = m_editorUtils.FloatField("SunIntensity", m_profileValues.m_lWSunIntensity);
                        }
                        GUILayout.Space(20f);
                    }

                    m_editorUtils.Heading("ShadowSettings");
                    m_profileValues.m_shadowCastingMode = (LightShadows)EditorGUILayout.EnumPopup("Sun Shadow Casting Mode", m_profileValues.m_shadowCastingMode);
                    if (m_profileValues.m_profileType != GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                    {
                        m_profileValues.m_shadowStrength = m_editorUtils.Slider("SunShadowStrength", m_profileValues.m_shadowStrength, 0f, 1f, helpEnabled);
                    }
                    m_profileValues.m_shadowDistance = m_editorUtils.Slider("ShadowDistance", m_profileValues.m_shadowDistance, 0f, 10000f, helpEnabled);
                    m_profileValues.m_sunShadowResolution = (LightShadowResolution)EditorGUILayout.EnumPopup("Sun Shadow Resolution", m_profileValues.m_sunShadowResolution);

                    GUILayout.Space(20f);
                    m_editorUtils.Heading("SkyboxSettings");

#if !GAIA_PRO_PRESENT
                    if (m_profileValues.m_profileType == GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky && m_profileValues.m_userCustomProfile)
                    {
                        GUI.enabled = true;
                    }
#endif
                    m_profileValues.m_profileType = (GaiaConstants.GaiaLightingProfileType)m_editorUtils.EnumPopup("SkyboxType", m_profileValues.m_profileType, helpEnabled);
#if !GAIA_PRO_PRESENT
                    if (m_profileValues.m_profileType == GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                    {
                        GUI.enabled = true;
                        EditorGUILayout.HelpBox("The 'Procedural Worlds Sky' profile sky type is available in Gaia Pro only, please choose a different profile type.", MessageType.Info);
                        GUI.enabled = false;
                    }
#endif

                    if (m_profileValues.m_profileType == GaiaConstants.GaiaLightingProfileType.Procedural)
                    {
                        m_profileValues.m_sunSize = m_editorUtils.Slider("SunSize", m_profileValues.m_sunSize, 0f, 1f);
                        m_profileValues.m_sunConvergence = m_editorUtils.Slider("SunConvergence", m_profileValues.m_sunConvergence, 0f, 10f);
                        m_profileValues.m_atmosphereThickness = m_editorUtils.Slider("AtmosphereThickness", m_profileValues.m_atmosphereThickness, 0f, 5f);
                        m_profileValues.m_skyboxExposure = m_editorUtils.Slider("SkyboxExposure", m_profileValues.m_skyboxExposure, 0f, 8f);
                        m_profileValues.m_skyboxTint = m_editorUtils.ColorField("SkyboxTint", m_profileValues.m_skyboxTint);
                        m_profileValues.m_groundColor = m_editorUtils.ColorField("GroundColor", m_profileValues.m_groundColor);
                    }
                    else if (m_profileValues.m_profileType == GaiaConstants.GaiaLightingProfileType.HDRI)
                    {
                        m_profileValues.m_skyboxHDRI = (Cubemap)m_editorUtils.ObjectField("HDRISkybox", m_profileValues.m_skyboxHDRI, typeof(Cubemap), false, GUILayout.Height(16f));
                        m_profileValues.m_skyboxTint = m_editorUtils.ColorField("SkyboxTint", m_profileValues.m_skyboxTint);
                        m_profileValues.m_skyboxExposure = m_editorUtils.Slider("SkyboxExposure", m_profileValues.m_skyboxExposure, 0f, 8f);
                    }
                    GUILayout.Space(20f);

                    //m_profileValues.m_ambientMode = (AmbientMode)EditorGUILayout.EnumPopup("Ambient Mode", m_profileValues.m_ambientMode);
                    if (m_profileValues.m_profileType != GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                    {
                        m_editorUtils.Heading("AmbientSettings");
                        m_profileValues.m_ambientMode = (AmbientMode)m_editorUtils.EnumPopup("AmbientMode", m_profileValues.m_ambientMode, helpEnabled);
                        if (RenderSettings.ambientMode == AmbientMode.Skybox)
                        {
                            m_profileValues.m_ambientIntensity = m_editorUtils.Slider("AmbientIntensity", m_profileValues.m_ambientIntensity, 0f, 10f);
                        }
                        else if (RenderSettings.ambientMode == AmbientMode.Flat)
                        {
                            m_profileValues.m_skyAmbient = m_editorUtils.ColorField("SkyAmbient", m_profileValues.m_skyAmbient);
                        }
                        else if (RenderSettings.ambientMode == AmbientMode.Trilight)
                        {
                            m_profileValues.m_skyAmbient = m_editorUtils.ColorField("SkyAmbient", m_profileValues.m_skyAmbient);
                            m_profileValues.m_equatorAmbient = m_editorUtils.ColorField("EquatorAmbient", m_profileValues.m_equatorAmbient);
                            m_profileValues.m_groundAmbient = m_editorUtils.ColorField("GroundAmbient", m_profileValues.m_groundAmbient);
                        }
                        GUILayout.Space(20f);
                    }

                    if (m_profile.m_enableFog)
                    {
                        m_editorUtils.Heading("FogSettings");
                        m_profileValues.m_fogMode = (FogMode)EditorGUILayout.EnumPopup("Fog Mode", m_profileValues.m_fogMode);
                        if (m_profileValues.m_profileType != GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                        {
                            if (m_profileValues.m_fogMode == FogMode.Linear)
                            {
                                m_profileValues.m_fogColor = m_editorUtils.ColorField("FogColor", m_profileValues.m_fogColor);
                                m_profileValues.m_fogStartDistance = m_editorUtils.FloatField("FogStartDistance", m_profileValues.m_fogStartDistance);
                                m_profileValues.m_fogEndDistance = m_editorUtils.FloatField("FogEndDistance", m_profileValues.m_fogEndDistance);
                            }
                            else
                            {
                                m_profileValues.m_fogColor = m_editorUtils.ColorField("FogColor", m_profileValues.m_fogColor);
                                m_profileValues.m_fogDensity = m_editorUtils.Slider("FogDensity", m_profileValues.m_fogDensity, 0f, 1f);
                            }
                        }
                    }
                }
                else
                {
                    m_profileValues.m_profileType = (GaiaConstants.GaiaLightingProfileType)m_editorUtils.EnumPopup("SkyboxType", m_profileValues.m_profileType, helpEnabled);
                    if (m_profile.m_enablePostProcessing)
                    {
                        m_editorUtils.Heading("PostProcessingSettings");
                        EditorGUI.indentLevel++;
#if HDPipeline
                        m_profileValues.PostProcessProfileHDRP = (VolumeProfile)m_editorUtils.ObjectField("PostProcessingProfile", m_profileValues.PostProcessProfileHDRP, typeof(VolumeProfile), false);
#endif
                        EditorGUI.indentLevel--;
                        GUILayout.Space(20f);
                    }

#if GAIA_PRO_PRESENT
                    if (m_profile.m_enableAmbientAudio)
                    {
                        m_editorUtils.Heading("AmbientAudioSettings");
                        EditorGUI.indentLevel++;
                        m_profileValues.m_ambientVolume = m_editorUtils.Slider("AmbientAudioVolume", m_profileValues.m_ambientVolume, 0f, 1f, helpEnabled);
                        m_profileValues.m_ambientAudio = (AudioClip)m_editorUtils.ObjectField("AmbientAudioClip", m_profileValues.m_ambientAudio, typeof(AudioClip), false, helpEnabled);
                        EditorGUI.indentLevel--;
                        GUILayout.Space(20f);
                    }
#endif

                    if (m_profileValues.m_profileType != GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                    {
                        m_editorUtils.Heading("SunSettings");
                        EditorGUI.indentLevel++;
                        m_profileValues.m_sunRotation = m_editorUtils.Slider("SunRotation", m_profileValues.m_sunRotation, 0f, 360f);
                        m_profileValues.m_sunPitch = m_editorUtils.Slider("SunPitch", m_profileValues.m_sunPitch, 0f, 360f);
                        m_profileValues.m_hDSunColor = m_editorUtils.ColorField("SunColor", m_profileValues.m_hDSunColor);
                        m_profileValues.m_hDSunIntensity = m_editorUtils.FloatField("SunIntensity", m_profileValues.m_hDSunIntensity);
                        EditorGUI.indentLevel--;
                        GUILayout.Space(20f);
                    }

                    m_editorUtils.Heading("ShadowSettings");
                    EditorGUI.indentLevel++;
                    m_profileValues.m_hDShadowDistance = m_editorUtils.Slider("ShadowDistance", m_profileValues.m_hDShadowDistance, 0f, 10000f);
                    m_profileValues.m_hDShadowResolution = (GaiaConstants.HDShadowResolution)m_editorUtils.EnumPopup("HDShadowResolution", m_profileValues.m_hDShadowResolution);
                    m_profileValues.m_hDContactShadows = m_editorUtils.Toggle("ContactShadows", m_profileValues.m_hDContactShadows);
                    if (m_profileValues.m_hDContactShadows)
                    {
                        EditorGUI.indentLevel++;
                        m_profileValues.m_hDContactShadowsDistance = m_editorUtils.Slider("ContactShadowsDistance", m_profileValues.m_hDContactShadowsDistance, 1f, 2000f);
                        m_profileValues.m_hDContactShadowQuality = (GaiaConstants.ContactShadowsQuality)m_editorUtils.EnumPopup("ContactShadowsQuality", m_profileValues.m_hDContactShadowQuality);
                        if (m_profileValues.m_hDContactShadowQuality == GaiaConstants.ContactShadowsQuality.Custom)
                        {
                            EditorGUI.indentLevel++;
                            m_profileValues.m_hDContactShadowCustomQuality = m_editorUtils.IntSlider("ContactShadowsSampleCount", m_profileValues.m_hDContactShadowCustomQuality, 4, 64);
                            EditorGUI.indentLevel--;
                        }
                        m_profileValues.m_hDContactShadowOpacity = m_editorUtils.Slider("ContactShadowOpacity", m_profileValues.m_hDContactShadowOpacity, 0f, 1f);
                        EditorGUI.indentLevel--;
                    }
                    m_profileValues.m_hDMicroShadows = m_editorUtils.Toggle("MicroShadows", m_profileValues.m_hDMicroShadows);
                    if (m_profileValues.m_hDMicroShadows)
                    {
                        EditorGUI.indentLevel++;
                        m_profileValues.m_hDMicroShadowOpacity = m_editorUtils.Slider("MicroShadowOpacity", m_profileValues.m_hDMicroShadowOpacity, 0f, 1f);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                    GUILayout.Space(20f);

                    if (m_profileValues.m_profileType != GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                    {
                        m_editorUtils.Heading("SkyboxSettings");
                        EditorGUI.indentLevel++;
                        m_profileValues.m_hDSkyType = (GaiaConstants.HDSkyType)EditorGUILayout.EnumPopup("Sky Type", m_profileValues.m_hDSkyType);
                        if (m_profileValues.m_hDSkyType == GaiaConstants.HDSkyType.HDRI)
                        {
                            m_profileValues.m_hDHDRISkybox = (Cubemap)m_editorUtils.ObjectField("HDRISkybox", m_profileValues.m_hDHDRISkybox, typeof(Cubemap), false, GUILayout.Height(16f));
                            m_profileValues.m_hDHDRIExposure = m_editorUtils.FloatField("SkyboxExposure", m_profileValues.m_hDHDRIExposure);
                            m_profileValues.m_hDHDRIMultiplier = m_editorUtils.FloatField("SkyboxMultiplier", m_profileValues.m_hDHDRIMultiplier);
                        }
                        else if (m_profileValues.m_hDSkyType == GaiaConstants.HDSkyType.Procedural)
                        {
                            EditorGUILayout.Space();
                            m_editorUtils.LabelField("GeometrySettings", EditorStyles.boldLabel);
                            m_profileValues.m_hDPBSPlanetaryRadius = m_editorUtils.FloatField("PBSPlanetaryRadius", m_profileValues.m_hDPBSPlanetaryRadius, helpEnabled);
                            m_profileValues.m_hDPBSPlantetCenterPosition = m_editorUtils.Vector3Field("PBSPlanetCenterPosition", m_profileValues.m_hDPBSPlantetCenterPosition, helpEnabled);
                            EditorGUILayout.Space();

                            m_editorUtils.LabelField("AirSettings", EditorStyles.boldLabel);
                            m_profileValues.m_hDPBSAirOpacity = m_editorUtils.ColorField("PBSAirOpacity", m_profileValues.m_hDPBSAirOpacity, helpEnabled);
                            m_profileValues.m_hDPBSAirAlbedo = m_editorUtils.ColorField("PBSAirAlbedo", m_profileValues.m_hDPBSAirAlbedo, helpEnabled);
                            m_profileValues.m_hDPBSAirMaximumAltitude = m_editorUtils.FloatField("PBSAirMaximumAltitude", m_profileValues.m_hDPBSAirMaximumAltitude, helpEnabled);
                            EditorGUILayout.Space();

                            m_editorUtils.LabelField("AerosolsSettings", EditorStyles.boldLabel);
                            m_profileValues.m_hDPBSAerosolDensity = m_editorUtils.Slider("PBSAerosolOpacity", m_profileValues.m_hDPBSAerosolDensity, -1f, 1f, helpEnabled);
                            m_profileValues.m_hDPBSAerosolAlbedo = m_editorUtils.ColorField("PBSAerosolAlbedo", m_profileValues.m_hDPBSAerosolAlbedo, helpEnabled);
                            m_profileValues.m_hDPBSAerosolMaximumAltitude = m_editorUtils.FloatField("PBSAerosolMaximumAltitude", m_profileValues.m_hDPBSAerosolMaximumAltitude, helpEnabled);
                            m_profileValues.m_hDPBSAerosolAnisotropy = m_editorUtils.Slider("PBSAerosolAnisotropy", m_profileValues.m_hDPBSAerosolAnisotropy, -1f, 1f, helpEnabled);
                            EditorGUILayout.Space();

                            m_editorUtils.LabelField("PlanetSettings", EditorStyles.boldLabel);
                            m_profileValues.m_hDPBSPlanetRotation = m_editorUtils.Vector3Field("PBSPlanetRotation", m_profileValues.m_hDPBSPlanetRotation, helpEnabled);
                            m_profileValues.m_hDPBSGroundTint = m_editorUtils.ColorField("PBSGroundColor", m_profileValues.m_hDPBSGroundTint);
                            m_profileValues.m_hDPBSGroundAlbedoTexture = (Cubemap)m_editorUtils.ObjectField("PBSGroundAlbedoTexture", m_profileValues.m_hDPBSGroundAlbedoTexture, typeof(Cubemap), false, helpEnabled, GUILayout.Height(16f));
                            m_profileValues.m_hDPBSGroundEmissionTexture = (Cubemap)m_editorUtils.ObjectField("PBSGroundEmissionTexture", m_profileValues.m_hDPBSGroundEmissionTexture, typeof(Cubemap), false, helpEnabled, GUILayout.Height(16f));
                            EditorGUILayout.Space();

                            m_editorUtils.LabelField("SpaceSettings", EditorStyles.boldLabel);
                            m_profileValues.m_hDPBSSpaceRotation = m_editorUtils.Vector3Field("PBSSpaceRotation", m_profileValues.m_hDPBSSpaceRotation, helpEnabled);
                            m_profileValues.m_hDPBSSpaceEmissionTexture = (Cubemap)m_editorUtils.ObjectField("PBSSpaceEmissionTexture", m_profileValues.m_hDPBSSpaceEmissionTexture, typeof(Cubemap), false, helpEnabled, GUILayout.Height(16f));
                            EditorGUILayout.Space();

                            m_editorUtils.LabelField("ArtisticOverrides", EditorStyles.boldLabel);
                            m_profileValues.m_hDPBSColorSaturation = m_editorUtils.Slider("PBSColorSaturation", m_profileValues.m_hDPBSColorSaturation, 0f, 1f, helpEnabled);
                            m_profileValues.m_hDPBSAlphaSaturation = m_editorUtils.Slider("PBSAlphaSaturation", m_profileValues.m_hDPBSAlphaSaturation, 0f, 1f, helpEnabled);
                            m_profileValues.m_hDPBSAlphaMultiplier = m_editorUtils.Slider("PBSAlphaMultiplier", m_profileValues.m_hDPBSAlphaMultiplier, 0f, 1f, helpEnabled);
                            m_profileValues.m_hDPBSHorizonTint = m_editorUtils.ColorField("PBSHorizonTint", m_profileValues.m_hDPBSHorizonTint, helpEnabled);
                            m_profileValues.m_hDPBSHorizonZenithShift = m_editorUtils.Slider("PBSHorizonZenithShift", m_profileValues.m_hDPBSHorizonZenithShift, -1f, 1f, helpEnabled);
                            m_profileValues.m_hDPBSZenithTint = m_editorUtils.ColorField("PBSZenithTint", m_profileValues.m_hDPBSZenithTint, helpEnabled);
                            EditorGUILayout.Space();

                            m_editorUtils.LabelField("MiscellaneousSettings", EditorStyles.boldLabel);
                            m_profileValues.m_hDPBSNumberOfBounces = m_editorUtils.IntSlider("PBSNumberOfBounces", m_profileValues.m_hDPBSNumberOfBounces, 1, 10, helpEnabled);
                            m_profileValues.m_hDPBSIntensityMode = (GaiaConstants.HDIntensityMode)m_editorUtils.EnumPopup("PBSIntensityMode", m_profileValues.m_hDPBSIntensityMode, helpEnabled);
                            if (m_profileValues.m_hDPBSIntensityMode == GaiaConstants.HDIntensityMode.Exposure)
                            {
                                m_profileValues.m_hDPBSExposure = m_editorUtils.FloatField("PBSExposure", m_profileValues.m_hDPBSExposure, helpEnabled);
                            }
                            else
                            {
                                m_profileValues.m_hDPBSMultiplier = m_editorUtils.FloatField("PBSMultiplier", m_profileValues.m_hDPBSMultiplier, helpEnabled);
                            }
                            m_profileValues.m_hDPBSIncludeSunInBaking = m_editorUtils.Toggle("PBSIncludeSunInBaking", m_profileValues.m_hDPBSIncludeSunInBaking, helpEnabled);
                        }
                        else
                        {
                            m_profileValues.m_hDGradientTopColor = m_editorUtils.ColorField("TopColor", m_profileValues.m_hDGradientTopColor);
                            m_profileValues.m_hDGradientMiddleColor = m_editorUtils.ColorField("MiddleColor", m_profileValues.m_hDGradientMiddleColor);
                            m_profileValues.m_hDGradientBottomColor = m_editorUtils.ColorField("BottomColor", m_profileValues.m_hDGradientBottomColor);
                            m_profileValues.m_hDGradientDiffusion = m_editorUtils.FloatField("Diffusion", m_profileValues.m_hDGradientDiffusion);
                            m_profileValues.m_hDGradientExposure = m_editorUtils.FloatField("Exposure", m_profileValues.m_hDGradientExposure);
                            m_profileValues.m_hDGradientMultiplier = m_editorUtils.FloatField("Multiplier", m_profileValues.m_hDGradientMultiplier);
                        }
                        EditorGUI.indentLevel--;
                        GUILayout.Space(20f);
                    }

                    m_editorUtils.Heading("AmbientSettings");
                    EditorGUI.indentLevel++;
                    m_profileValues.m_hDAmbientMode = (GaiaConstants.HDAmbientMode)EditorGUILayout.EnumPopup("Ambient Mode", m_profileValues.m_hDAmbientMode);
#if !UNITY_2019_3_OR_NEWER
                    m_profileValues.m_hDAmbientDiffuseIntensity = m_editorUtils.FloatField("DiffuseIntensity", m_profileValues.m_hDAmbientDiffuseIntensity);
                    m_profileValues.m_hDAmbientSpecularIntensity = m_editorUtils.FloatField("SpecularIntensity", m_profileValues.m_hDAmbientSpecularIntensity);
#else
                    if (m_profileValues.m_hDAmbientMode == GaiaConstants.HDAmbientMode.Static)
                    {
                        EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("HDRP2019_3AmbientMode"), MessageType.Info);
                    }
#endif
                    EditorGUI.indentLevel--;
                    GUILayout.Space(20f);

                    if (m_profileValues.m_profileType != GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                    {
                        m_editorUtils.Heading("FogSettings");
                        EditorGUI.indentLevel++;
                        m_profileValues.m_hDFogType2019_3 = (GaiaConstants.HDFogType2019_3)EditorGUILayout.EnumPopup("Fog Mode", m_profileValues.m_hDFogType2019_3);
                        if (m_profileValues.m_hDFogType2019_3 == GaiaConstants.HDFogType2019_3.None)
                        {
                            EditorGUILayout.HelpBox("No fog mode selected. If you want to use fog please select a different mode", MessageType.Info);
                        }
                        else
                        {
                            m_profileValues.m_hDVolumetricFogScatterColor = m_editorUtils.ColorField("ScatterColor", m_profileValues.m_hDVolumetricFogScatterColor);
                            m_profileValues.m_hDVolumetricFogDistance = m_editorUtils.FloatField("FogDistance", m_profileValues.m_hDVolumetricFogDistance);
                            if (m_profileValues.m_hDVolumetricFogDistance < 0f)
                            {
                                m_profileValues.m_hDVolumetricFogDistance = 0f;
                            }
                            m_profileValues.m_hDVolumetricFogBaseHeight = m_editorUtils.FloatField("FogBaseHeight", m_profileValues.m_hDVolumetricFogBaseHeight);
                            if (m_profileValues.m_hDVolumetricFogBaseHeight < 0f)
                            {
                                m_profileValues.m_hDVolumetricFogBaseHeight = 0f;
                            }
                            m_profileValues.m_hDVolumetricFogMeanHeight = m_editorUtils.FloatField("FogMeanHeight", m_profileValues.m_hDVolumetricFogMeanHeight);
                            if (m_profileValues.m_hDVolumetricFogMeanHeight < 0f)
                            {
                                m_profileValues.m_hDVolumetricFogMeanHeight = 0f;
                            }
                            m_profileValues.m_hDVolumetricFogAnisotropy = m_editorUtils.Slider("FogAnisotropy", m_profileValues.m_hDVolumetricFogAnisotropy, 0f, 1f);
                            m_profileValues.m_hDVolumetricFogProbeDimmer = m_editorUtils.Slider("FogProbeDimmer", m_profileValues.m_hDVolumetricFogProbeDimmer, 0f, 1f);
                            m_profileValues.m_hDVolumetricFogMaxDistance = m_editorUtils.FloatField("MaxFogDistance", m_profileValues.m_hDVolumetricFogMaxDistance);
                            if (m_profileValues.m_hDVolumetricFogMaxDistance < 0f)
                            {
                                m_profileValues.m_hDVolumetricFogMaxDistance = 0f;
                            }
                            m_profileValues.m_hDVolumetricFogDepthExtent = m_editorUtils.FloatField("FogDepthExtent", m_profileValues.m_hDVolumetricFogDepthExtent);
                            if (m_profileValues.m_hDVolumetricFogDepthExtent < 0f)
                            {
                                m_profileValues.m_hDVolumetricFogDepthExtent = 0f;
                            }
                            m_profileValues.m_hDVolumetricFogSliceDistribution = m_editorUtils.Slider("FogSliceDistribution", m_profileValues.m_hDVolumetricFogSliceDistribution, 0f, 1f);
                        }
                        EditorGUI.indentLevel--;
                    }
                }

                GUI.enabled = true;
            }

#if !GAIA_PRO_PRESENT
            GUI.enabled = currentGUIState;
#endif
        }

        /// <summary>
        /// Adds a new user profile
        /// </summary>
        private void AddNewCustomProfile()
        {
            //GaiaLightingProfileValues selectdValues = m_savedSettings.LoadSettings(m_profile, true);
            GaiaLightingProfileValues selectdValues = m_profileValues;
            GaiaLightingProfileValues newProfile = new GaiaLightingProfileValues();
            GaiaUtils.CopyFields(selectdValues, newProfile);
            int count = m_profile.m_lightingProfiles.Count + 1;
            newProfile.m_typeOfLighting = "New User Profile " + count;
            newProfile.m_userCustomProfile = true;
            m_profile.m_lightingProfiles.Add(newProfile);
            m_profile.m_selectedLightingProfileValuesIndex = m_profile.m_lightingProfiles.Count - 1;

            if (m_profile.m_selectedLightingProfileValuesIndex != -99)
            {
                GaiaUtils.GetRuntimeSceneObject();

                if (GaiaGlobal.Instance != null)
                {
                    GaiaLighting.GetProfile(m_profile, m_gaiaSettings.m_pipelineProfile, m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled);
                }
            }

            EditorUtility.SetDirty(m_profile);
        }
    }
}