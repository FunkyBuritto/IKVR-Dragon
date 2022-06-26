using UnityEditor;

namespace Gaia
{
    [CustomEditor(typeof(AutoDepthOfField))]
    public class AutoDepthOfFieldEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GaiaUtils.GetActivePipeline() == GaiaConstants.EnvironmentRenderer.BuiltIn)
            {
                EditorGUILayout.HelpBox("This is the auto depth of field system for Gaia. To edit or remove please go to Gaia Player under Gaia Runtime", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Auto Depth Of Field is not yet supported for SRP this will be available in a near future update", MessageType.Info);
            }
        }
    }
}