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
    //for editing Spawner Settings in a reorderable list.
    public class SpawnerPresetListEditor : PWEditor, IPWEditor
    {

        public static float OnElementHeight()
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public static List<BiomeSpawnerListEntry> OnRemoveListEntry(List<BiomeSpawnerListEntry> oldList, int index)
        {
            //if (index < 0 || index >= oldList.Length)
            //    return null;
            //SpawnerPresetListEntry toRemove = oldList[index];
            //SpawnerPresetListEntry[] newList = new SpawnerPresetListEntry[oldList.Length - 1];
            //for (int i = 0; i < newList.Length; ++i)
            //{
            //    if (i < index)
            //    {
            //        newList[i] = oldList[i];
            //    }
            //    else if (i >= index)
            //    {
            //        newList[i] = oldList[i + 1];
            //    }
            //}
            oldList.RemoveAt(index);
            return oldList;
        }

        public static List<BiomeSpawnerListEntry> OnAddListEntry(List<BiomeSpawnerListEntry> oldList)
        {
            oldList.Add(new BiomeSpawnerListEntry() { m_autoAssignPrototypes = true });
            return oldList;
        }

        public static void DrawListHeader(Rect rect, bool currentFoldOutState, List<BiomeSpawnerListEntry> spawnerList, EditorUtils editorUtils, string headerKey)
        {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            //rect.xMin += 0f;
            EditorGUI.LabelField(rect, editorUtils.GetContent(headerKey));
            //bool newFoldOutState = EditorGUI.Foldout(rect, currentFoldOutState, PropertyCount("SpawnerAdded", spawnerList, editorUtils), true);
            EditorGUI.indentLevel = oldIndent;
            //return newFoldOutState;
        }

        public static void DrawList(ReorderableList list, EditorUtils editorUtils)
        {
            Rect maskRect;
            //if (listExpanded)
            //{
            maskRect = EditorGUILayout.GetControlRect(true, list.GetHeight());
            list.DoList(maskRect);
            //}
            //else
            //{
            //    int oldIndent = EditorGUI.indentLevel;
            //    EditorGUI.indentLevel = 1;
            //    listExpanded = EditorGUILayout.Foldout(listExpanded, PropertyCount("SpawnerAdded", (List<SpawnerPresetListEntry>)list.list, editorUtils), true);
            //    maskRect = GUILayoutUtility.GetLastRect();
            //    EditorGUI.indentLevel = oldIndent;
            //}

            //editorUtils.Panel("MaskBaking", DrawMaskBaking, false);
        }

        //public static void DrawListElement_GaiaManager(Rect rect, BiomeSpawnerListEntry listEntry, EditorUtils m_editorUtils, EditorWindow window = null)
        //{
        //    int oldIndent = EditorGUI.indentLevel;
        //    Rect controlRect = new Rect(rect);
        //    controlRect.width = 40;
        //    controlRect.height = EditorGUIUtility.singleLineHeight;
        //    EditorGUI.indentLevel = 0;
        //    EditorGUI.LabelField(controlRect, m_editorUtils.GetContent("SpawnerListActive"));
        //    controlRect.x = rect.x + 40;
        //    controlRect.width = 10;
        //    listEntry.m_isActiveInStamper = EditorGUI.Toggle(controlRect, listEntry.m_isActiveInStamper);
        //    controlRect.width = 40;
        //    EditorGUI.LabelField(controlRect, m_editorUtils.GetContent("BiomeActive"));
        //    controlRect.x = rect.x + 40;
        //    controlRect.width = 10;
        //    listEntry.m_isActiveInBiome = EditorGUI.Toggle(controlRect, listEntry.m_isActiveInBiome);
        //    controlRect.x = rect.x + 60;
        //    controlRect.width = 70;
        //    controlRect.height = EditorGUIUtility.singleLineHeight;
        //    EditorGUI.LabelField(controlRect, m_editorUtils.GetContent("SpawnerListAutoAssignPrototypes"));
        //    controlRect.x = rect.x + 125;
        //    controlRect.width = 10;
        //    listEntry.m_autoAssignPrototypes = EditorGUI.Toggle(controlRect, listEntry.m_autoAssignPrototypes);
        //    controlRect.width = 26;
        //    controlRect.x = rect.x + rect.width - 30;
        //    int controlID = EditorGUIUtility.GetControlID(FocusType.Passive);
        //    if (GUI.Button(controlRect, " "))
        //    {
        //        EditorGUIUtility.ShowObjectPicker<SpawnerSettings>(listEntry.m_spawnerSettings, false, "l:GaiaManagerSpawner", controlID);
        //    }
        //    controlRect.width = rect.width - 160;
        //    controlRect.x = rect.x + 150;
        //    listEntry.m_spawnerSettings = (SpawnerSettings)EditorGUI.ObjectField(controlRect, listEntry.m_spawnerSettings, typeof(SpawnerSettings), false);
          
        //    string commandName = Event.current.commandName;
        //    if (commandName == "ObjectSelectorUpdated")
        //    {
        //        if (controlID == EditorGUIUtility.GetObjectPickerControlID())
        //        {
        //            listEntry.m_spawnerSettings = (SpawnerSettings)EditorGUIUtility.GetObjectPickerObject();
        //            if (window != null)
        //            {
        //                window.Repaint();
        //            }
        //        }
        //    }
        //    else if (commandName == "ObjectSelectorClosed")
        //    {
        //        if (controlID == EditorGUIUtility.GetObjectPickerControlID())
        //        {
        //            listEntry.m_spawnerSettings = (SpawnerSettings)EditorGUIUtility.GetObjectPickerObject();
        //        }
        //    }

        //    EditorGUI.indentLevel = oldIndent;
        //}

        public static void DrawListElement(Rect rect, BiomeSpawnerListEntry listEntry, EditorUtils m_editorUtils, EditorWindow window = null)
        {
            int oldIndent = EditorGUI.indentLevel;
            Rect controlRect = new Rect(rect);
            EditorGUI.indentLevel = 0;
            controlRect.height = EditorGUIUtility.singleLineHeight;
            controlRect.width = 12;
            EditorGUI.LabelField(controlRect, m_editorUtils.GetContent("StamperActive"));
            controlRect.x = rect.x + 12;
            controlRect.width = 20;
            listEntry.m_isActiveInStamper = EditorGUI.Toggle(controlRect, listEntry.m_isActiveInStamper);
            controlRect.x = controlRect.x + 20;
            controlRect.width = 12;
            EditorGUI.LabelField(controlRect, m_editorUtils.GetContent("BiomeActive"));
            controlRect.x = controlRect.x + 12;
            controlRect.width = 15;
            listEntry.m_isActiveInBiome = EditorGUI.Toggle(controlRect, listEntry.m_isActiveInBiome);
            controlRect.x = controlRect.x + 20;
            controlRect.width = 12;
            EditorGUI.LabelField(controlRect, m_editorUtils.GetContent("AutoAssignPrototypes"));
            controlRect.x = controlRect.x + 12;
            controlRect.width = 15;
            listEntry.m_autoAssignPrototypes = EditorGUI.Toggle(controlRect, listEntry.m_autoAssignPrototypes);
            controlRect.x = rect.x + 10;
            controlRect.width = 20;
            controlRect.x = rect.x + rect.width - 20;
            int controlID = EditorGUIUtility.GetControlID(FocusType.Passive);
            if (GUI.Button(controlRect, " "))
            {
                EditorGUIUtility.ShowObjectPicker<SpawnerSettings>(listEntry.m_spawnerSettings, false, "l:" + GaiaConstants.gaiaManagerSpawnerLabel, controlID);
            }
            controlRect.width = rect.width - 100;
            controlRect.x = rect.x + 100;

            EditorGUI.BeginChangeCheck();          
            listEntry.m_spawnerSettings = (SpawnerSettings)EditorGUI.ObjectField(controlRect, listEntry.m_spawnerSettings, typeof(SpawnerSettings), false);

            string commandName = Event.current.commandName;
            if (commandName == "ObjectSelectorUpdated")
            {
                if (controlID == EditorGUIUtility.GetObjectPickerControlID())
                {
                    //Triggers when the custom picker object was updated
                    listEntry.m_spawnerSettings = (SpawnerSettings)EditorGUIUtility.GetObjectPickerObject();
                    listEntry.m_spawnerSettings.RefreshGUID();
                    if (window != null)
                    {
                        window.Repaint();
                    }
                }
            }
            else if (commandName == "ObjectSelectorClosed")
            {
                if (controlID == EditorGUIUtility.GetObjectPickerControlID())
                {
                    //Triggers when the custom picker object was closed
                    listEntry.m_spawnerSettings = (SpawnerSettings)EditorGUIUtility.GetObjectPickerObject();
                    listEntry.m_spawnerSettings.RefreshGUID();
                }
            }
            if(EditorGUI.EndChangeCheck())
            {
                //Triggers when the user drags and drops a spawner settings file into the slot
                listEntry.m_spawnerSettings.RefreshGUID();
            }

            EditorGUI.indentLevel = oldIndent;
        }

        public static void DrawListElement_AdvancedTab(Rect rect, BiomeSpawnerListEntry listEntry, EditorUtils m_editorUtils)
        {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect labelRect = rect;
            //260 is the total width of the controls at the end of each row
            labelRect.width = rect.width - 280;
            labelRect.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, listEntry.m_spawnerSettings.name);

            EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.Zoom);
            if (labelRect.Contains(Event.current.mousePosition) && Event.current.clickCount > 0)
            {
                Selection.activeObject = listEntry.m_spawnerSettings;
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
            if (GUI.Button(labelRect, m_editorUtils.GetContent("AdvancedTabAddSpawner")))
            {
                Spawner newSpawner = listEntry.m_spawnerSettings.CreateSpawner(listEntry.m_autoAssignPrototypes);
                Selection.activeGameObject = newSpawner.gameObject;
            }


            EditorGUI.indentLevel = oldIndent;
        }

        public static void DrawListElementInStamper(Rect rect, BiomeSpawnerListEntry listEntry, EditorUtils m_editorUtils)
        {
            int oldIndent = EditorGUI.indentLevel;
            Rect controlRect = new Rect(rect);
            controlRect.width = 40;
            controlRect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.indentLevel = 0;
            EditorGUI.LabelField(controlRect, "Active");
            controlRect.x = rect.x + 40;
            controlRect.width = 10;
            listEntry.m_isActiveInStamper = EditorGUI.Toggle(controlRect, listEntry.m_isActiveInStamper);
            controlRect.width = rect.width - 70;
            controlRect.x = rect.x + 70;
            listEntry.m_spawnerSettings = (SpawnerSettings)EditorGUI.ObjectField(controlRect, listEntry.m_spawnerSettings, typeof(SpawnerSettings), false);
            EditorGUI.indentLevel = oldIndent;
        }

        public static void DrawListElementInBiome(Rect rect, BiomeSpawnerListEntry listEntry, EditorUtils m_editorUtils)
        {
            int oldIndent = EditorGUI.indentLevel;
            Rect controlRect = new Rect(rect);
            controlRect.width = 40;
            controlRect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.indentLevel = 0;
            EditorGUI.LabelField(controlRect, "Active");
            controlRect.x = rect.x + 40;
            controlRect.width = 10;
            listEntry.m_isActiveInBiome = EditorGUI.Toggle(controlRect, listEntry.m_isActiveInBiome);
            controlRect.width = rect.width - 70;
            controlRect.x = rect.x + 70;
            listEntry.m_spawnerSettings = (SpawnerSettings)EditorGUI.ObjectField(controlRect, listEntry.m_spawnerSettings, typeof(SpawnerSettings), false);
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
