using PWCommon4;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Gaia
{
    //This class is not a full editor class by itself, but used to collect reusable methods
    //for editing Biomes in a reorderable list.
    public class BiomeListEditor : PWEditor, IPWEditor
    {

        public static float OnElementHeight()
        {
          return EditorGUIUtility.singleLineHeight;
        }

        public static void DrawListHeader(Rect rect, bool currentFoldOutState, List<AdvancedTabBiomeListEntry> biomesList, EditorUtils editorUtils, string headerKey)
        {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            //rect.xMin += 0f;
            EditorGUI.LabelField(rect, editorUtils.GetContent(headerKey));
            //bool newFoldOutState = EditorGUI.Foldout(rect, currentFoldOutState, PropertyCount("SpawnerAdded", spawnerList, editorUtils), true);
            EditorGUI.indentLevel = oldIndent;
            //return newFoldOutState;
        }

        public static void DrawListElement_AdvancedTab(Rect rect, AdvancedTabBiomeListEntry listEntry, EditorUtils m_editorUtils)
        {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;


            Rect labelRect = rect;
            //260 is the total width of the controls at the end of each row
            labelRect.width = rect.width -280;
            labelRect.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, listEntry.m_biomePreset.name);

            EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.Zoom);
            if (labelRect.Contains(Event.current.mousePosition) && Event.current.clickCount>0)
            {
                Selection.activeObject = listEntry.m_biomePreset;
                EditorGUIUtility.PingObject(Selection.activeObject);
            }

            labelRect.x = rect.width - 230;
            labelRect.width = 110f;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("SpawnerListAutoAssignPrototypes"));

            labelRect.x = rect.width - 130f;
            labelRect.width = 20f;

            listEntry.m_autoAssignPrototypes = EditorGUI.Toggle(labelRect, listEntry.m_autoAssignPrototypes);

            labelRect.x = rect.width - 80f;
            labelRect.width = 100f;
            if (GUI.Button(labelRect, m_editorUtils.GetContent("AdvancedTabAddBiome")))
            {
                BiomeController newBiome = listEntry.m_biomePreset.CreateBiome(listEntry.m_autoAssignPrototypes);
                Selection.activeGameObject = newBiome.gameObject;
            }


            EditorGUI.indentLevel = oldIndent;
        }

        public static void DrawListElement(Rect rect, BiomeSpawnerListEntry listEntry, EditorUtils m_editorUtils)
        {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            listEntry.m_spawnerSettings = (SpawnerSettings)EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), listEntry.m_spawnerSettings, typeof(SpawnerSettings), false);
            EditorGUI.indentLevel = oldIndent;
        }

        public static GUIContent PropertyCount(string key, List<BiomeSpawnerListEntry> list, EditorUtils editorUtils)
        {
            GUIContent content = editorUtils.GetContent(key);
            content.text += " [" + list.Count + "]";
            return content;
        }


    }
}