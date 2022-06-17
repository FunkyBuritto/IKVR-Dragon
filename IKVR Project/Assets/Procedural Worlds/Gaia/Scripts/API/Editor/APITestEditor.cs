using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(APITest))]
public class APITestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        APITest apiTest = (APITest)target;
        DrawDefaultInspector();
        if (GUILayout.Button("Create Terrain"))
        {
            apiTest.CreateTerrainButton();
        }
        if (GUILayout.Button("Stamp"))
        {
            apiTest.StampButton();
        }
    }
}
