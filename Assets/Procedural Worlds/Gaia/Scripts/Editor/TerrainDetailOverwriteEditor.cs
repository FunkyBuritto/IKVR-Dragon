using UnityEngine;
using PWCommon4;
using UnityEditor.SceneManagement;
using UnityEditor;
using Gaia.Internal;

namespace Gaia
{
    [CustomEditor(typeof(TerrainDetailOverwrite))]
    public class TerrainDetailOverwriteEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private string m_version;
        private Color defaultBackground;
        private TerrainDetailOverwrite m_profile;

        private void OnEnable()
        {
            //Get Gaia Lighting Profile object
            m_profile = (TerrainDetailOverwrite)target;

            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            m_version = PWApp.CONF.Version;

            if (m_profile.m_terrain == null)
            {
                m_profile.m_terrain = m_profile.GetComponent<Terrain>();
            }

            m_profile.m_detailDistance = (int)m_profile.m_terrain.detailObjectDistance;
            m_profile.m_detailDensity = m_profile.m_terrain.detailObjectDensity;

            if (m_profile.m_terrain.terrainData.detailResolutionPerPatch == 2)
            {
                m_profile.m_detailQuality = GaiaConstants.TerrainDetailQuality.Ultra2;
            }
            else if (m_profile.m_terrain.terrainData.detailResolutionPerPatch == 4)
            {
                m_profile.m_detailQuality = GaiaConstants.TerrainDetailQuality.VeryHigh4;
            }
            else if (m_profile.m_terrain.terrainData.detailResolutionPerPatch == 8)
            {
                m_profile.m_detailQuality = GaiaConstants.TerrainDetailQuality.High8;
            }
            else if (m_profile.m_terrain.terrainData.detailResolutionPerPatch == 16)
            {
                m_profile.m_detailQuality = GaiaConstants.TerrainDetailQuality.Medium16;
            }
            else if (m_profile.m_terrain.terrainData.detailResolutionPerPatch == 32)
            {
                m_profile.m_detailQuality = GaiaConstants.TerrainDetailQuality.Low32;
            }
            else
            {
                m_profile.m_detailQuality = GaiaConstants.TerrainDetailQuality.VeryLow64;
            }
        }

        public override void OnInspectorGUI()
        {
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!

            m_profile.GetResolutionPatches();

            EditorGUILayout.LabelField("Version: " + m_version);

            m_editorUtils.Panel("DetailDistanceSettings", DetailTerrainDistance, true);
        }

        private void DetailTerrainDistance(bool helpEnabled)
        {
            EditorGUI.BeginChangeCheck();

            m_editorUtils.Text("DetailInfo");
            EditorGUILayout.Space();

            //switch (m_profile.m_detailQuality)
            //{
            //    case GaiaConstants.TerrainDetailQuality.Ultra2:
            //        EditorGUILayout.LabelField(m_editorUtils.GetTextValue("DetailPatchResolution") + "Ultra");
            //        break;
            //    case GaiaConstants.TerrainDetailQuality.VeryHigh4:
            //        EditorGUILayout.LabelField(m_editorUtils.GetTextValue("DetailPatchResolution") + "Very High");
            //        break;
            //    case GaiaConstants.TerrainDetailQuality.High8:
            //        EditorGUILayout.LabelField(m_editorUtils.GetTextValue("DetailPatchResolution") + "High");
            //        break;
            //    case GaiaConstants.TerrainDetailQuality.Medium16:
            //        EditorGUILayout.LabelField(m_editorUtils.GetTextValue("DetailPatchResolution") + "Medium");
            //        break;
            //    case GaiaConstants.TerrainDetailQuality.Low32:
            //        EditorGUILayout.LabelField(m_editorUtils.GetTextValue("DetailPatchResolution") + "Low");
            //        break;
            //    case GaiaConstants.TerrainDetailQuality.VeryLow64:
            //        EditorGUILayout.LabelField(m_editorUtils.GetTextValue("DetailPatchResolution") + "Very Low");
            //        break;
            //}
            //EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("DetailPatchResolutionHelp"), MessageType.Info);

            m_profile.m_detailDistance = m_editorUtils.IntField("DetailDistance", m_profile.m_detailDistance, helpEnabled);
            if (m_profile.m_detailDistance < 0)
            {
                m_profile.m_detailDistance = 0;
            }
            if (m_profile.m_detailDistance > 250)
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("DetailDistanceHelp"), MessageType.Info);
            }

            m_profile.m_detailDensity = m_editorUtils.Slider("DetailDensity", m_profile.m_detailDensity, 0f, 1f, helpEnabled);
            if (m_profile.m_detailDistance > 250 && m_profile.m_detailDensity > 0.5f)
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("DetailDensityHelp"), MessageType.Info);
            }

            if (m_editorUtils.Button("ApplyToAll"))
            {
                m_profile.ApplySettings(true);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_profile, "Made changes");
                EditorUtility.SetDirty(m_profile);
                m_profile.ApplySettings(false);

                if (!Application.isPlaying)
                {
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
            }
        }
    }
}