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
    //for editing Stamper Auto Mask Exporters in a reorderable list.
    public class StamperAutoMaskExportListEditor : PWEditor, IPWEditor
    {

        public static float OnElementHeight()
        {
          return EditorGUIUtility.singleLineHeight;
        }

        public static List<AutoMaskExport> OnRemoveListEntry(List<AutoMaskExport> oldList, int index)
        {
            oldList.RemoveAt(index);
            return oldList;
        }

        public static List<AutoMaskExport> OnAddListEntry(List<AutoMaskExport> oldList)
        {
            oldList.Add(new AutoMaskExport { isActive=true });
            return oldList;
        }

        public static bool DrawListHeader(Rect rect, bool currentFoldOutState, bool currentToggleAllState, List<AutoMaskExport> autoMaskList, EditorUtils editorUtils, ref GaiaConstants.AutoSpawnerArea autoSpawnerArea)
        {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            //rect.xMin += 0f;


            Rect dropdownRect = new Rect(rect);
            dropdownRect.width = 50;
            dropdownRect.height = 15;
            dropdownRect.y += 2;
            dropdownRect.x = rect.x + rect.width - 2 - dropdownRect.width;

            GUIStyle dropdownStyle = new GUIStyle(GUI.skin.button);
            dropdownStyle.fixedHeight = 15;

            Rect label2Rect = new Rect(rect);
            label2Rect.width = 30;
            label2Rect.x = dropdownRect.x - 5 - label2Rect.width;


            Rect label1Rect = new Rect(rect);
            label1Rect.x += 35;
            label1Rect.width = rect.width - label2Rect.width - 35 - dropdownRect.width - 5 - 10;


            Rect toggleAllRect = new Rect(rect);
            toggleAllRect.x += 15;
            toggleAllRect.width = 12;

            bool oldToggleOldState = currentToggleAllState;
            currentToggleAllState = EditorGUI.Toggle(toggleAllRect, currentToggleAllState);
            EditorGUI.LabelField(toggleAllRect, editorUtils.GetContent("AutoToggleAllTooltip"));

            if (currentToggleAllState != oldToggleOldState)
            {
                foreach (AutoMaskExport entry in autoMaskList)
                {
                    entry.isActive = currentToggleAllState;
                }
            }

            EditorGUI.LabelField(label1Rect, editorUtils.GetContent("AutoMaskMapExportHeader"));
            EditorGUI.LabelField(label2Rect, editorUtils.GetContent("AutoMaskMapExportAreaLabel"));

            autoSpawnerArea = (GaiaConstants.AutoSpawnerArea)EditorGUI.EnumPopup(dropdownRect, autoSpawnerArea, dropdownStyle);
            EditorGUI.LabelField(dropdownRect, editorUtils.GetContent("AutoMaskMapExportAreaTooltip"));

            //bool newFoldOutState = EditorGUI.Foldout(rect, currentFoldOutState, PropertyCount("SpawnerAdded", spawnerList, editorUtils), true);
            EditorGUI.indentLevel = oldIndent;
            //return newFoldOutState;


            return currentToggleAllState;
        }

        public static void DrawList(ReorderableList list, EditorUtils editorUtils)
        {
            Rect maskRect;
            maskRect = EditorGUILayout.GetControlRect(true, list.GetHeight());
            list.DoList(maskRect);
        }

        public static void DrawListElement(Rect rect, ref AutoMaskExport listEntry, EditorUtils m_editorUtils)
        {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            //EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width * 0.1f, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("AutoSpawnerActive"));
            listEntry.isActive = EditorGUI.Toggle(new Rect(rect.x, rect.y, 20, EditorGUIUtility.singleLineHeight), listEntry.isActive);
            //switch (listEntry.status)
            //{
            //    case AutoSpawnerStatus.Spawning:
            //        EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.2f, rect.y, rect.width * 0.2f, EditorGUIUtility.singleLineHeight), String.Format("{0:f0}", listEntry.spawner.m_spawnProgress * 100));
            //        break;
            //    default:
            //        EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.2f, rect.y, rect.width * 0.2f, EditorGUIUtility.singleLineHeight), listEntry.status.ToString());
            //        break;
            //}
            bool currentGUIState = GUI.enabled;
            GUI.enabled = listEntry.isActive;
#if GAIA_PRO_PRESENT
            listEntry.maskMapExport = (MaskMapExport)EditorGUI.ObjectField(new Rect(rect.x + 20, rect.y, rect.width - 20, EditorGUIUtility.singleLineHeight), listEntry.maskMapExport, typeof(MaskMapExport), true);
#endif
            GUI.enabled = currentGUIState;
            EditorGUI.indentLevel = oldIndent;
        }

      

        public static GUIContent PropertyCount(string key, List<AutoMaskExport> list, EditorUtils editorUtils)
        {
            GUIContent content = editorUtils.GetContent(key);
            content.text += " [" + list.Count + "]";
            return content;
        }


    }
}