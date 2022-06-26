using UnityEngine;
using UnityEditor;
using Gaia.Internal;
using PWCommon4;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace Gaia
{
    public class LoadUpSceneSettings
    {
        public static void UpdateSceneSettingsFromProfile()
        {
            GaiaSettings GaiaSettings = GaiaUtils.GetGaiaSettings();
            if (GaiaSettings != null)
            {
                LoadWaterAndLighting(GaiaSettings);
            }
        }

        private static void LoadWaterAndLighting(GaiaSettings settings, bool showDebug = false)
        {
            if (settings == null)
            {
                Debug.LogError("Gaia settings was not found.");
                return;
            }
            if (settings.m_gaiaWaterProfile == null)
            {
                Debug.LogError("Water Profile was not found!");
                return;
            }
            else
            {
                Material waterMat = GaiaWater.GetGaiaOceanMaterial();
                if (waterMat != null)
                {
                    GameObject waterObject = GameObject.Find(GaiaConstants.waterSurfaceObject);
                    if (waterObject != null)
                    {

                        GaiaUtils.GetRuntimeSceneObject();
                        if (GaiaGlobal.Instance != null)
                        {
                            GaiaWater.GetProfile(GaiaGlobal.Instance.SceneProfile.m_selectedWaterProfileValuesIndex, waterMat, GaiaGlobal.Instance.SceneProfile, true, false);
                        }
                    }
                }
            }
            if (settings.m_gaiaLightingProfile == null)
            {
                Debug.LogError("Lighting Profile was not found!");
                return;
            }
            else
            {
                GameObject lightObject = GameObject.Find(GaiaConstants.gaiaLightingObject);
                if (lightObject != null)
                {
                    if (GaiaGlobal.Instance != null)
                    {
                        GaiaUtils.GetRuntimeSceneObject();
                        GaiaLighting.GetProfile(GaiaGlobal.Instance.SceneProfile, settings.m_pipelineProfile, settings.m_pipelineProfile.m_activePipelineInstalled);
                    }
                }
            }

            if (showDebug)
            {
                Debug.Log("Loading up profile settings successfully");
            }
        }
    }

    [CustomEditor(typeof(GaiaGlobal))]
    public class GaiaGlobalEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private GaiaSettings m_gaiaSettings;
        private GaiaGlobal m_profile;
        private GUIStyle dropdownGUIStyle;

        private void OnEnable()
        {
            //Get Gaia Lighting Profile object
            m_profile = (GaiaGlobal)target;

            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            if (m_gaiaSettings == null)
            {
                m_gaiaSettings = GaiaUtils.GetGaiaSettings();
            }

            if (m_profile.SceneProfile == null)
            {
                m_profile.SceneProfile = ScriptableObject.CreateInstance<SceneProfile>();
            }

            if (m_profile.SceneProfile.m_selectedLightingProfileValuesIndex > m_profile.SceneProfile.m_lightingProfiles.Count - 1)
            {
                m_profile.SceneProfile.m_selectedLightingProfileValuesIndex = 1;
            }

            if (m_profile.SceneProfile.m_selectedWaterProfileValuesIndex > m_profile.SceneProfile.m_waterProfiles.Count - 1)
            {
                m_profile.SceneProfile.m_selectedWaterProfileValuesIndex = 1;
            }

            m_profile.SceneProfile.ProfileVersion = PWApp.CONF.Version;

#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                LoadSettings();
            }
#endif
            if (dropdownGUIStyle == null)
            {
                dropdownGUIStyle = new GUIStyle(EditorStyles.popup)
                {
                    fixedHeight = 16f, margin = new RectOffset(0, 0, 4, 0)
                };
            }

            if (m_profile.SceneProfile.m_lightingProfiles.Count > 0)
            {
                if (m_profile.SceneProfile.m_selectedLightingProfileValuesIndex != -99)
                {
                    GaiaLightingProfileValues profileValues = m_profile.SceneProfile.m_lightingProfiles[m_profile.SceneProfile.m_selectedLightingProfileValuesIndex];
                    GaiaSceneManagement.FetchSceneSettigns(m_profile.SceneProfile, profileValues);
                }
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
            if (dropdownGUIStyle == null)
            {
                dropdownGUIStyle = new GUIStyle(EditorStyles.popup)
                {
                    fixedHeight = 16f, margin = new RectOffset(0, 0, 4, 0)
                };
            }

            if (m_profile != null)
            {
                Transform transform = m_profile.gameObject.transform;
                transform.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
                if (m_profile.m_mainCamera == null)
                {
                    m_profile.m_mainCamera = GaiaUtils.GetCamera();
                }
            }

            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!
            if (m_gaiaSettings == null)
            {
                m_gaiaSettings = GaiaUtils.GetGaiaSettings();
            }

            m_editorUtils.Panel("GlobalSettings", GlobalSettingsPanel, false, true, true);
        }

        private void GlobalSettingsPanel(bool helpEnabled)
        {
            if (Application.isPlaying)
            {
                LoadFromApplicationPlaying();
            }

            m_editorUtils.Text("GaiaRuntimeInfo");
            EditorGUILayout.Space();

            m_profile.m_mainCamera = (Camera)m_editorUtils.ObjectField("MainCamera", m_profile.m_mainCamera, typeof(Camera), true, helpEnabled);
            if (m_profile.SceneProfile != null)
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.BeginHorizontal();

                m_profile.SceneProfile.m_lightSystemMode = (GaiaConstants.GlobalSystemMode)m_editorUtils.EnumPopup("LightingSystemMode", m_profile.SceneProfile.m_lightSystemMode);
                if (m_profile.SceneProfile.m_lightSystemMode == GaiaConstants.GlobalSystemMode.Gaia)
                {
                    if (m_profile.SceneProfile.m_lightingProfiles.Count > 0)
                    {
                        if (m_editorUtils.Button("AdvancedLightingSettings", GUILayout.MaxWidth(170f)))
                        {
                            GaiaLighting.FocusGaiaLightingProfile();
                        }
                    }
                }
                else if (m_profile.SceneProfile.m_lightSystemMode == GaiaConstants.GlobalSystemMode.ThirdParty)
                {
                    if (m_editorUtils.Button("AdvancedLightingSettings", GUILayout.MaxWidth(170f)))
                    {
                        GaiaLighting.FocusGaiaLightingProfile();
                    }
                    /*if (m_profile.SceneProfile.m_thirdPartyLightObject != null)
                    {
                        if (m_editorUtils.Button("EditThirdParty", GUILayout.MaxWidth(170f)))
                        {
                            Selection.activeObject = m_profile.SceneProfile.m_thirdPartyLightObject;
                        }
                    }
                    else
                    {
                        if (m_editorUtils.Button("AdvancedLightingSettings", GUILayout.MaxWidth(170f)))
                        {
                            GaiaLighting.FocusGaiaLightingProfile();
                        }
                    }*/
                }
                EditorGUILayout.EndHorizontal();

                m_editorUtils.InlineHelp("LightingSystemMode", helpEnabled);

                if (m_profile.SceneProfile.m_lightSystemMode == GaiaConstants.GlobalSystemMode.Gaia)
                {
                    if (m_profile.SceneProfile.m_lightingProfiles.Count < 1)
                    {
                        EditorGUILayout.HelpBox("No lighting data has been set you can go to Gaia Manager and click step 2 to create skies and water or you can go to Save and Load and click Revert To Defaults to load the skies and water data.", MessageType.Info);
                    }
                }
                else if (m_profile.SceneProfile.m_lightSystemMode == GaiaConstants.GlobalSystemMode.ThirdParty)
                {
                    if (m_profile.SceneProfile.m_thirdPartyLightObject == null)
                    {
                        EditorGUILayout.HelpBox("No Lighting System has been set, open 'Advanced Lighting Settings' to setup", MessageType.Info);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Lighting System Mode is set to 'None' to use light set the mode to 'Gaia or ThirdParty'", MessageType.Info);
                }

                EditorGUILayout.BeginHorizontal();

                m_profile.SceneProfile.m_waterSystemMode = (GaiaConstants.GlobalSystemMode)m_editorUtils.EnumPopup("WaterSystemMode", m_profile.SceneProfile.m_waterSystemMode);
                if (m_profile.SceneProfile.m_waterSystemMode == GaiaConstants.GlobalSystemMode.Gaia)
                {
                    if (m_profile.SceneProfile.m_waterProfiles.Count > 0)
                    {
                        if (m_editorUtils.Button("AdvancedWaterSettings", GUILayout.MaxWidth(170f)))
                        {
                            GaiaUtils.FocusWaterProfile();
                        }
                    }
                }
                else if (m_profile.SceneProfile.m_waterSystemMode == GaiaConstants.GlobalSystemMode.ThirdParty)
                {
                    if (m_editorUtils.Button("AdvancedWaterSettings", GUILayout.MaxWidth(170f)))
                    {
                        GaiaUtils.FocusWaterProfile();
                    }
                    /*if (m_profile.SceneProfile.m_thirdPartyWaterObject != null)
                    {
                        if (m_editorUtils.Button("EditThirdParty", GUILayout.MaxWidth(170f)))
                        {
                            Selection.activeObject = m_profile.SceneProfile.m_thirdPartyLightObject;
                        }
                    }
                    else
                    {
                        if (m_editorUtils.Button("AdvancedWaterSettings", GUILayout.MaxWidth(170f)))
                        {
                            GaiaUtils.FocusWaterProfile();
                        }
                    }*/
                }
                EditorGUILayout.EndHorizontal();
                m_editorUtils.InlineHelp("WaterSystemMode", helpEnabled);

                if (m_profile.SceneProfile.m_waterSystemMode == GaiaConstants.GlobalSystemMode.Gaia)
                {
                    if (m_profile.SceneProfile.m_lightingProfiles.Count < 1)
                    {
                        EditorGUILayout.HelpBox("No lighting data has been set you can go to Gaia Manager and click step 2 to create skies and water or you can go to Save and Load and click Revert To Defaults to load the skies and water data.", MessageType.Info);
                    }
                }
                else if (m_profile.SceneProfile.m_waterSystemMode == GaiaConstants.GlobalSystemMode.ThirdParty)
                {
                    if (m_profile.SceneProfile.m_thirdPartyWaterObject == null)
                    {
                        EditorGUILayout.HelpBox("No Water System has been set, open 'Advanced Water Settings' to setup", MessageType.Info);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Water System Mode is set to 'None' to use light set the mode to 'Gaia or ThirdParty'", MessageType.Info);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(m_profile);
                    Undo.RecordObject(m_profile, "Changes Made");

                    m_profile.UpdateGaiaTimeOfDay(false);
                    m_profile.UpdateGaiaWeather();
                }

                m_editorUtils.Panel("SaveAndLoad", SaveAndLoad);
            }
        }

        private void SaveAndLoad(bool helpEnabled)
        {
            EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("SaveAndLoadHelpText"), MessageType.Info);

            if (m_profile.SceneProfile == null)
            {
                m_profile.SceneProfile = ScriptableObject.CreateInstance<SceneProfile>();
            }

            bool canBeSaved = true;
            if (!m_profile.SceneProfile.DefaultLightingSet)
            {
                EditorGUILayout.HelpBox("No lighting profile settings have been saved. Please use gaia default lighting profile to save them here.", MessageType.Warning);
                canBeSaved = false;
            }
            else
            {
                //EditorGUILayout.HelpBox("Lighting profile has been saved", MessageType.Info);
            }

            if (!m_profile.SceneProfile.DefaultWaterSet)
            {
                EditorGUILayout.HelpBox("No water profile settings have been saved. Please use gaia default water profile to save them here.", MessageType.Warning);
                canBeSaved = false;
            }
            else
            {
                //EditorGUILayout.HelpBox("Water profile has been saved", MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            if (!canBeSaved)
            {
                GUI.enabled = false;
            }

            //string path = Application.dataPath + "/Assets";
            string path = "";
            if (m_editorUtils.Button("Save"))
            {
                string sceneName = EditorSceneManager.GetActiveScene().name;
                if (string.IsNullOrEmpty(sceneName))
                {
                    Debug.LogWarning("Unable to save file as scene has not been saved. Please save your scene then try again.");
                }
               
                path = EditorUtility.SaveFilePanelInProject("Save Location", sceneName + " Profile", "asset" , "Save new profile asset");
                GaiaSceneManagement.SaveFile(m_profile.SceneProfile, path);
                EditorGUIUtility.ExitGUI();
            }

            GUI.enabled = true;

            if (m_editorUtils.Button("Load"))
            {
                path = EditorUtility.OpenFilePanel("Load Profile", Application.dataPath + "/Assets", "asset");
                GaiaSceneManagement.LoadFile(path);
                EditorGUIUtility.ExitGUI();
            }

            if (m_editorUtils.Button("Revert"))
            {
                GaiaSceneManagement.Revert(m_profile.SceneProfile);
                EditorGUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void LoadSettings()
        {
#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                //Weather
                m_profile.SceneProfile.m_gaiaWeather.m_season = ProceduralWorldsGlobalWeather.Instance.Season;
                m_profile.SceneProfile.m_gaiaWeather.m_windDirection = ProceduralWorldsGlobalWeather.Instance.WindDirection;
            }
#endif
        }

        private void LoadFromApplicationPlaying()
        {
#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                //Weather
                m_profile.SceneProfile.m_gaiaWeather.m_season = ProceduralWorldsGlobalWeather.Instance.Season;
                m_profile.SceneProfile.m_gaiaWeather.m_windDirection = ProceduralWorldsGlobalWeather.Instance.WindDirection;
            }
#endif
        }

        private void UpdateLighting(bool process, bool applyProfile = false)
        {
            if (process)
            {
                if (GaiaGlobal.Instance != null)
                {
                    GaiaUtils.GetRuntimeSceneObject();
                    GaiaLighting.GetProfile(GaiaGlobal.Instance.SceneProfile, m_gaiaSettings.m_pipelineProfile, m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, applyProfile);
                }

                EditorUtility.SetDirty(m_profile.SceneProfile);
            }
        }

        private void UpdateWater(bool process)
        {
            if (process)
            {
                if (m_profile.WaterMaterial == null)
                {
                    m_profile.WaterMaterial = GaiaWater.GetGaiaOceanMaterial();
                }

                GaiaUtils.GetRuntimeSceneObject();
                if (GaiaGlobal.Instance != null)
                {
                    GaiaWater.GetProfile(m_profile.SceneProfile.m_selectedWaterProfileValuesIndex, m_profile.WaterMaterial, GaiaGlobal.Instance.SceneProfile, true, false);
                }
                EditorUtility.SetDirty(m_profile.SceneProfile);
            }
        }
    }
}