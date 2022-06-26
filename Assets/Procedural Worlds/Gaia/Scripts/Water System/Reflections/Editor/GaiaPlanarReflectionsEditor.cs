using UnityEditor;

namespace Gaia
{
    [CustomEditor(typeof(GaiaPlanarReflections))]
    public class GaiaPlanarReflectionsEditor : Editor
    {
        private const string m_helpText = "Gaia Planar Reflections is the reflection system for SRP this system only work in URP/HDRP. This system uses the SRP camera commands to render reflections in realtime. This system also supports shadows, custom render distances and unity layers.";
        private GaiaPlanarReflections m_reflections;

        public override void OnInspectorGUI()
        {
            if (m_reflections == null)
            {
                m_reflections = (GaiaPlanarReflections) target;
            }
            //Initialization
            EditorGUILayout.HelpBox(m_helpText, MessageType.Info);
        }
    }
}