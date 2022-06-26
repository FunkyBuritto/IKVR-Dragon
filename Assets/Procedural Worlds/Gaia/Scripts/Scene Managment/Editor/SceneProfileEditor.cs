using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Gaia
{
    [CustomEditor(typeof(SceneProfile))]
    public class SceneProfileEditor : Editor
    {
        private SceneProfile m_profile;
        private GUIStyle m_helpBoxStyle;
        private const string m_sceneProfileContext1 = "Welcome to the Scene Profile. This profile contains all of your scene settings from lighting to water profile settings and setup. This file was saved from Scene: ";
        private const string m_sceneProfileContext2 = " To load this profile just go to Gaia Runtime and at the botton to Save And Load then load this profile to apply all the settings from this profile to your scene. Note that this will override any settings you may already have setup in your scene.";

        public override void OnInspectorGUI()
        {
            if (m_profile == null)
            {
                m_profile = (SceneProfile) target;
            }

            m_helpBoxStyle = SetupHelpBoxStyle();
            EditorGUILayout.LabelField(m_sceneProfileContext1 + "<b>" + m_profile.m_savedFromScene + "</b>" + m_sceneProfileContext2, m_helpBoxStyle);
            if (GUILayout.Button(new GUIContent("Open Source Scene", "This will open the scene where this profile was created from. The scene where this was saved from is shown in Bold in the text above.")))
            {
                string sceneToOpen = GaiaUtils.GetAssetPath(m_profile.m_savedFromScene + ".unity");
                if (!string.IsNullOrEmpty(sceneToOpen))
                {
                   LoadSourceScene(sceneToOpen);
                }
            }
        }

        private GUIStyle SetupHelpBoxStyle()
        {
            GUIStyle style = GUI.skin.GetStyle("HelpBox");
            style.richText = true;
            style.fontSize = 13;
            return style;
        }

        private void LoadSourceScene(string sceneToLoad, OpenSceneMode sceneMode = OpenSceneMode.Single)
        {
            if (EditorSceneManager.GetActiveScene().name == m_profile.m_savedFromScene)
            {
                Debug.Log("This scene is already open");
            }
            else
            {
                EditorSceneManager.OpenScene(sceneToLoad, sceneMode);
            }
        }
    }
}