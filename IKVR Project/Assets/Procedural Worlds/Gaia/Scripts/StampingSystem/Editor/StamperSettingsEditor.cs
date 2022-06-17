// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using Gaia.Internal;
using PWCommon4;

/*
 * Custom Editor for Sequence assets
 */

namespace Gaia
{
    //[CustomPropertyDrawer(typeof(StamperSettings.ClipData))]
    //public class StamperSettingsClipDataEditor : PropertyDrawer
    //{
    //    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    //    {
    //        return EditorGUIUtility.singleLineHeight;
    //    }
    //    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //    {
    //        position.width -= 120f;
    //        SerializedProperty clip = property.FindPropertyRelative("m_clip");
    //        SerializedProperty volume = property.FindPropertyRelative("m_volume");
    //        EditorGUI.ObjectField(position, clip, GUIContent.none);
    //        position.xMin = position.xMax;
    //        position.xMax += 120f;
    //        EditorGUI.Slider(position, volume, 0f, 1f, GUIContent.none);
    //    }
    //}

    [CanEditMultipleObjects]
    [CustomEditor(typeof(StamperSettings))]
    public class StamperSettingsEditor : PWEditor, IPWEditor
    {
        /// <summary> Reference to EditorUtils for GUI functions </summary>
        private EditorUtils m_editorUtils;

        //Properties of Sequence asset
        SerializedProperty m_requirements;
        SerializedProperty m_clipData;
        SerializedProperty m_volume;
        SerializedProperty m_playbackSpeed;
        SerializedProperty m_randomizeOrder;
        SerializedProperty m_crossFade;
        SerializedProperty m_trackFadeTime;
        SerializedProperty m_volumeFadeTime;
        SerializedProperty m_playbackSpeedFadeTime;
        SerializedProperty m_delayChance;
        SerializedProperty m_delayFadeTime;
        SerializedProperty m_minMaxDelay;
        SerializedProperty m_valuesMix;
        SerializedProperty m_values;
        SerializedProperty m_eventsMix;
        SerializedProperty m_events;
        SerializedProperty m_randomVolume;
        SerializedProperty m_minMaxVolume;
        SerializedProperty m_randomPlaybackSpeed;
        SerializedProperty m_minMaxPlaybackSpeed;
        SerializedProperty m_modifiers;
        SerializedProperty m_outputType;
        SerializedProperty m_outputPrefab;
        SerializedProperty m_outputDirect;
        SerializedProperty m_outputDistance;
        SerializedProperty m_outputVerticalAngle;
        SerializedProperty m_outputHorizontalAngle;
        SerializedProperty m_syncGroup;
        SerializedProperty m_syncType;
        SerializedProperty m_eventsWhilePlaying;
        //SerializedProperty m_outputFollowPosition = null;
        //SerializedProperty m_outputFollowRotation = null;
        //SerializedProperty m_OnPlayClip = null;
        //SerializedProperty m_OnStopClip = null;

        /// <summary> Animated Boolean for if Output Distance should be shown </summary>
        AnimBool outputGroup;
        /// <summary> Animated Boolean for if Sliders should be displayed </summary>
        AnimBool slidersGroup;
        /// <summary> Animated Boolean for if Events should be displayed </summary>
        AnimBool eventsGroup;
        /// <summary> Animated Boolean for if OutputDirect is true </summary>
        AnimBool directGroup;
        /// <summary> Animated Boolean for if we are in a SyncGroup </summary>
        AnimBool syncGroup;
        UnityEditorInternal.ReorderableList m_eventsReorderable;
        UnityEditorInternal.ReorderableList m_valuesReorderable;
        bool m_clipsExpanded = true;
        UnityEditorInternal.ReorderableList m_clipsReorderable;

        UnityEditorInternal.ReorderableList m_modifiersReorderable;
        UnityEditorInternal.ReorderableList m_eventsWhilePlayingReorderable;

        /// <summary> Destructor to release references </summary>
        private void OnDestroy()
        {
            if (m_editorUtils != null)
            {
                m_editorUtils.Dispose();
            }
        }
        /// <summary> Constructor to set up references for editor </summary>
        void OnEnable()
        {
            m_clipData = serializedObject.FindProperty("m_clipData");
            m_volume = serializedObject.FindProperty("m_volume");
            m_playbackSpeed = serializedObject.FindProperty("m_playbackSpeed");
            m_randomizeOrder = serializedObject.FindProperty("m_randomizeOrder");
            m_crossFade = serializedObject.FindProperty("m_crossFade");
            m_trackFadeTime = serializedObject.FindProperty("m_trackFadeTime");
            m_volumeFadeTime = serializedObject.FindProperty("m_volumeFadeTime");
            m_playbackSpeedFadeTime = serializedObject.FindProperty("m_playbackSpeedFadeTime");
      
            m_clipsReorderable = new UnityEditorInternal.ReorderableList(serializedObject, m_clipData, true, false, true, true);
            m_clipsReorderable.elementHeight = EditorGUIUtility.singleLineHeight;
            m_clipsReorderable.drawElementCallback = DrawClipElement;
            m_clipsReorderable.drawHeaderCallback = DrawClipHeader;
            m_clipsReorderable.onAddCallback = OnAddClip;

         
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
        }

        void OnAddClip(UnityEditorInternal.ReorderableList list)
        {
            int idx = m_clipData.arraySize;
            m_clipData.InsertArrayElementAtIndex(idx);
            if (idx == 0)
                m_clipData.GetArrayElementAtIndex(idx).FindPropertyRelative("m_volume").floatValue = 1f;
        }
        void DrawClipElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(rect, m_clipData.GetArrayElementAtIndex(index), GUIContent.none);
            EditorGUI.indentLevel = oldIndent;
        }
        void DrawClipHeader(Rect rect)
        {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            rect.xMin += 8f;
            m_clipsExpanded = EditorGUI.Foldout(rect, m_clipsExpanded, PropertyCount("mClips", m_clipData), true);
            EditorGUI.indentLevel = oldIndent;
        }

        /// <summary> Main GUI Function </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            //    serializedObject.Update();

            //    m_editorUtils.Initialize(); // Do not remove this!
            //    m_editorUtils.GUIHeader();
            //    m_editorUtils.TitleNonLocalized(m_editorUtils.GetContent("SequenceDataHeader").text + serializedObject.targetObject.name);

            //    m_editorUtils.Panel("ClipsPanel", ClipsPanel, true);

            //    //m_editorUtils.GUIFooter();
            //    serializedObject.ApplyModifiedProperties();
            //}
        }
        /// <summary> Panel to show "Clip" options of Sequence </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        public void ClipsPanel(bool inlineHelp)
        {
            ++EditorGUI.indentLevel;
            EditorGUI.BeginChangeCheck();
            Rect clipsRect;
            if (m_clipsExpanded)
            {
                clipsRect = EditorGUILayout.GetControlRect(true, m_clipsReorderable.GetHeight());
                m_clipsReorderable.DoList(clipsRect);
            }
            else
            {
                int oldIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 1;
                m_clipsExpanded = EditorGUILayout.Foldout(m_clipsExpanded, PropertyCount("mClips", m_clipData), true);
                clipsRect = GUILayoutUtility.GetLastRect();
                EditorGUI.indentLevel = oldIndent;
            }
            if (Event.current.type == EventType.DragUpdated)
            {
                bool isValid = false;
                for (int i = 0; i < DragAndDrop.objectReferences.Length; ++i)
                    if (DragAndDrop.objectReferences[i] is AudioClip)
                    {
                        isValid = true;
                        break;
                    }
                if (isValid)
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                else
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
            else if (Event.current.type == EventType.DragPerform)
            {
                for (int i = 0; i < DragAndDrop.objectReferences.Length; ++i)
                {
                    if (!(DragAndDrop.objectReferences[i] is AudioClip))
                        continue;
                    int idx = m_clipData.arraySize;
                    m_clipData.InsertArrayElementAtIndex(idx);
                    SerializedProperty clipData = m_clipData.GetArrayElementAtIndex(idx);
                    clipData.FindPropertyRelative("m_volume").floatValue = 1f;
                    clipData.FindPropertyRelative("m_clip").objectReferenceValue = DragAndDrop.objectReferences[i];
                }
                Event.current.Use();
            }

            m_editorUtils.InlineHelp("mClips", inlineHelp);
            //PropertyCountField("mClips", true, m_clips, ((Sequence)target).m_clips.Length, inlineHelp);
            //int badClipCount = 0;
            //for (int x = 0; x < m_clipData.arraySize; ++x)
            //{
            //    AudioClip clip = m_clipData.GetArrayElementAtIndex(x).FindPropertyRelative("m_clip").objectReferenceValue as AudioClip;
            //    if (clip != null)
            //    {
            //        if (clip.loadType != AudioClipLoadType.DecompressOnLoad)
            //        {
            //            ++badClipCount;
            //        }
            //    }
            //}
            //if (badClipCount > 0)
            //{
            //    EditorGUILayout.HelpBox(m_editorUtils.GetContent("InvalidClipMessage").text, MessageType.Warning, true);
            //    if (m_editorUtils.ButtonRight("InvalidClipButton"))
            //    {
            //        GUIContent progressBarContent = m_editorUtils.GetContent("InvalidClipPopup");
            //        for (int x = 0; x < m_clipData.arraySize; ++x)
            //        {
            //            AudioClip clip = m_clipData.GetArrayElementAtIndex(x).FindPropertyRelative("m_clip").objectReferenceValue as AudioClip;
            //            EditorUtility.DisplayProgressBar(progressBarContent.text, progressBarContent.tooltip + clip.name, x / (float)badClipCount);
            //            if (clip != null)
            //            {
            //                if (clip.loadType != AudioClipLoadType.DecompressOnLoad)
            //                {
            //                    AudioImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as AudioImporter;
            //                    AudioImporterSampleSettings sampleSettings = importer.defaultSampleSettings;
            //                    sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
            //                    importer.defaultSampleSettings = sampleSettings;
            //                    if (importer.ContainsSampleSettingsOverride("Standalone"))
            //                    {
            //                        sampleSettings = importer.GetOverrideSampleSettings("Standalone");
            //                        sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
            //                        importer.SetOverrideSampleSettings("Standalone", sampleSettings);
            //                    }
            //                    importer.SaveAndReimport();
            //                }
            //            }
            //        }
            //        EditorUtility.ClearProgressBar();
            //    }
            //}
          
            //m_editorUtils.PropertyField("mTrackFadeTime", m_trackFadeTime, inlineHelp);
            //if (m_trackFadeTime.floatValue < 0f)
            //    m_trackFadeTime.floatValue = 0f;
            //m_editorUtils.PropertyField("mVolume", m_volume, inlineHelp);
            //m_editorUtils.PropertyField("mVolumeFadeTime", m_volumeFadeTime, inlineHelp);
            //if (m_volumeFadeTime.floatValue < 0f)
            //    m_volumeFadeTime.floatValue = 0f;
            ////EditorGUI.BeginDisabledGroup(syncGroup.target && ((SyncType)System.Enum.GetValues(typeof(SyncType)).GetValue(m_syncType.enumValueIndex) & SyncType.FIT) > 0);
            ////{
            ////    m_editorUtils.PropertyField("mPlaybackSpeed", m_playbackSpeed, inlineHelp);
            ////    if (m_playbackSpeed.floatValue < 0f)
            ////        m_playbackSpeed.floatValue = 0f;
            ////}
            ////EditorGUI.EndDisabledGroup();
            //EditorGUI.BeginDisabledGroup(syncGroup.target);
            //{
            //    m_editorUtils.PropertyField("mPlaybackSpeedFadeTime", m_playbackSpeedFadeTime, inlineHelp);
            //    if (m_playbackSpeedFadeTime.floatValue < 0f)
            //        m_playbackSpeedFadeTime.floatValue = 0f;
            //    m_editorUtils.PropertyField("mCrossFade", m_crossFade, inlineHelp);
            //    if (m_crossFade.floatValue < 0f)
            //        m_crossFade.floatValue = 0f;
            //}
            //EditorGUI.EndDisabledGroup();
            --EditorGUI.indentLevel;
        }
        /// <summary> Panel to display Output options </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
      

        private GUIContent PropertyCount(string key, SerializedProperty property)
        {
            GUIContent content = m_editorUtils.GetContent(key);
            content.text += " [" + property.arraySize + "]";
            return content;
        }
    }
}
