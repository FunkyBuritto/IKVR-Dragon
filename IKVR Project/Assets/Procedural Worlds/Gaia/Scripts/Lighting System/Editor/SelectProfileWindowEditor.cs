using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Gaia
{
    public class SelectProfileWindowEditor : EditorWindow
    {
        private GaiaLightingProfile m_lightingProfile;
        private GaiaWaterProfile m_waterProfile;
        private GaiaSettings m_gaiaSettings;
        private GUIStyle m_boxStyle;

        private List<string> LightingList = new List<string>();
        private List<string> WaterList = new List<string>();

        /// <summary>
        /// Show Select Profile Manager editor window
        /// </summary>
        public static void ShowProfileManager(GaiaLightingProfile lightingProfile, GaiaWaterProfile waterProfile, GaiaSettings gaiaSettings)
        {
            var manager = EditorWindow.GetWindow<SelectProfileWindowEditor>(false, "Lighting And Water Profile Selection");
            if (manager != null)
            {
                manager.m_lightingProfile = lightingProfile;
                manager.m_waterProfile = waterProfile;
                manager.m_gaiaSettings = gaiaSettings;

                Vector2 initialSize = new Vector2(350f, 120f);
                manager.position = new Rect(new Vector2(Screen.currentResolution.width / 2f - initialSize.x / 2f, Screen.currentResolution.height / 2f - initialSize.y / 2f), initialSize);
                manager.Show();
            }
        }

        void OnGUI()
        {
            //Set up the box style
            if (m_boxStyle == null)
            {
                m_boxStyle = new GUIStyle(GUI.skin.box);
                m_boxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                m_boxStyle.fontStyle = FontStyle.Bold;
                m_boxStyle.alignment = TextAnchor.UpperLeft;
            }

            if (m_lightingProfile != null && m_waterProfile != null)
            {
                CreateArrayLists();
                MainTab();
            }
        }

        private void MainTab()
        {

            EditorGUILayout.BeginVertical(m_boxStyle);
            Lighting();
            Water();
            Weather();
            EditorGUILayout.EndVertical();
        }

        private void Lighting()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Lighting Preset", EditorStyles.boldLabel, GUILayout.Width(105f));
            m_lightingProfile.m_selectedLightingProfileValuesIndex = EditorGUILayout.Popup(m_lightingProfile.m_selectedLightingProfileValuesIndex, LightingList.ToArray());
            if (GUILayout.Button("Edit"))
            {
                GaiaLighting.FocusGaiaLightingProfile();
            }
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                if (GaiaGlobal.Instance != null)
                {
                    GaiaUtils.GetRuntimeSceneObject();
                    GaiaLighting.GetProfile(GaiaGlobal.Instance.SceneProfile, m_gaiaSettings.m_pipelineProfile, m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled);
                }
            }
        }

        private void Water()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Water Preset", EditorStyles.boldLabel, GUILayout.Width(105f));
            m_gaiaSettings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex = EditorGUILayout.Popup(m_gaiaSettings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex, WaterList.ToArray());
            if (GUILayout.Button("Edit"))
            {
                GaiaUtils.FocusWaterProfile();
            }
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                Material waterMat = GaiaWater.GetGaiaOceanMaterial();

                GaiaUtils.GetRuntimeSceneObject();
                if (GaiaGlobal.Instance != null)
                {
                    GaiaWater.GetProfile(m_gaiaSettings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex, waterMat, GaiaGlobal.Instance.SceneProfile, true, false);
                }
            }
        }

        private void Weather()
        {
#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                if (Application.isPlaying)
                {
                    GUI.enabled = true;
                }
                else
                {
                    if (ProceduralWorldsGlobalWeather.Instance.m_renderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
                    {
                        EditorGUILayout.HelpBox("Playing/Stoping weather is only avaliable in play mode", MessageType.Info);
                        GUI.enabled = false;
                    }
                }

                if (ProceduralWorldsGlobalWeather.Instance.m_renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    EditorGUILayout.HelpBox("Rain and Snow is not avaliable for HDRP, this feature will be available soon.", MessageType.Info);
                    GUI.enabled = false;
                }

                EditorGUILayout.BeginHorizontal();
                if (!ProceduralWorldsGlobalWeather.Instance.IsRaining)
                {
                    if (GUILayout.Button("Start Rain"))
                    {
                        ProceduralWorldsGlobalWeather.Instance.PlayRain();
                    }
                }
                else
                {
                    if (GUILayout.Button("Stop Rain"))
                    {
                        ProceduralWorldsGlobalWeather.Instance.StopRain();
                    }
                }
                if (!ProceduralWorldsGlobalWeather.Instance.IsSnowing)
                {
                    if (GUILayout.Button("Start Snow"))
                    {
                        if (ProceduralWorldsGlobalWeather.Instance.m_player.transform.position.y > ProceduralWorldsGlobalWeather.Instance.SnowHeight)
                        {
                            ProceduralWorldsGlobalWeather.Instance.PlaySnow();
                        }
                        else
                        {
                            Debug.Log("You are not at the right altitude for snow to happen. Your altitude is: " + ProceduralWorldsGlobalWeather.Instance.m_player.transform.position.y + " Snow can happen at altitude: " + ProceduralWorldsGlobalWeather.Instance.SnowHeight);
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button("Stop Snow"))
                    {
                        ProceduralWorldsGlobalWeather.Instance.StopSnow();
                    }
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }
#endif
        }

        private void CreateArrayLists()
        {
            LightingList.Clear();
            foreach(GaiaLightingProfileValues gaiaLighting in m_lightingProfile.m_lightingProfiles)
            {
                LightingList.Add(gaiaLighting.m_typeOfLighting);
            }

            WaterList.Clear();
            foreach(GaiaWaterProfileValues gaiaWater in m_waterProfile.m_waterProfiles)
            {
                WaterList.Add(gaiaWater.m_typeOfWater);
            }
        }
    }
}