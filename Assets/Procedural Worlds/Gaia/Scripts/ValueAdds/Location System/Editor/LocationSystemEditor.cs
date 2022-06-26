using UnityEditor;
using UnityEngine;

namespace Gaia
{
    [CustomEditor(typeof(LocationSystem))]
    public class LocationSystemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("The location manager allows you to create bookmark locations to allow you to quickly jump from bookmark to bookmark to help navigate around the scene.", MessageType.Info);

            if (GUILayout.Button(new GUIContent("Open Location Manager", "Opens the location manager window")))
            {
                LocationManagerEditor.ShowLocationManager();
            }
        }
    }
}