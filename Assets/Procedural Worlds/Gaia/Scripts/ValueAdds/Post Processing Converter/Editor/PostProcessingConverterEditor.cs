using UnityEngine;
using UnityEditor;

namespace Gaia
{
    [CustomEditor(typeof(PostProcessingConverter))]
    public class PostProcessingConverterEditor : Editor
    {
        private PostProcessingConverter m_profile;

        private void OnEnable()
        {
            m_profile = (PostProcessingConverter)target;
        }

        public override void OnInspectorGUI()
        {
            m_profile.gameObject.name = "Post Processing Converter";

#if UNITY_POST_PROCESSING_STACK_V2
            EditorGUILayout.HelpBox("This only support from Built-In to URP/HDRP conversions only", MessageType.Info);

            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
            m_profile.RenderPipeline = (GaiaConstants.EnvironmentRenderer)EditorGUILayout.EnumPopup("Selected Pipeline", m_profile.RenderPipeline);
            m_profile.ProcessProfile = (UnityEngine.Rendering.PostProcessing.PostProcessProfile)EditorGUILayout.ObjectField("Post Processing Profile", m_profile.ProcessProfile, typeof(UnityEngine.Rendering.PostProcessing.PostProcessProfile), false);
            EditorGUILayout.BeginHorizontal();
            m_profile.SavePath = EditorGUILayout.TextField("Save Path", m_profile.SavePath);
            if (GUILayout.Button("Selected Folder", GUILayout.MaxWidth(110f)))
            {
                string newPath = EditorUtility.SaveFolderPanel("Save Path", Application.dataPath, "");
                m_profile.SavePath = newPath;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Converting from Built-In to " + m_profile.RenderPipeline);
            if (GUILayout.Button("Convert Profile"))
            {
#if HDPipeline || UPPipeline
                GaiaUtils.CreateURPOrHDRPPostProcessing(m_profile.RenderPipeline, m_profile.ProcessProfile, m_profile.SavePath);
#endif
            }
#else
            EditorGUILayout.HelpBox("Post Processing V2 is not installed. To use this feature you need to install post processing v2 fromt he package manager. This only support from Built-In to URP/HDRP conversions only", MessageType.Info);
#endif
        }
    }
}