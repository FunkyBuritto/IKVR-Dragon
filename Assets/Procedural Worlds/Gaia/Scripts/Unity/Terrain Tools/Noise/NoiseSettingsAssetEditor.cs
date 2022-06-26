#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Gaia
{
    [CustomEditor(typeof(NoiseSettings))]
    public class NoiseSettingsEditor : Editor
    {
        NoiseSettingsGUI gui = new NoiseSettingsGUI();

        void OnEnable()
        {
            gui.Init(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            gui.OnGUI();
        }
    }
}
#endif