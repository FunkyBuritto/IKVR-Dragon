using UnityEngine;
using UnityEditor;
using Gaia.Internal;
using PWCommon4;
using ProceduralWorlds.HierachySystem;
using UnityEngine.Assertions.Must;

namespace Gaia
{
    [CustomEditor(typeof(GaiaHierarchyUtils))]
    public class GaiaHierarchyUtilsEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        private GaiaHierarchyUtils m_profile;
        private bool m_settingsChangedInfo;
        private Color m_guiBackground;
        private Color m_redGUI = new Color(0.905f, 0.415f, 0.396f, 1f);
        private Color m_greenGUI = new Color(0.696f, 0.905f, 0.397f, 1f);
        private Color m_orangeGUI = new Color(1.0f, 0.394f, 0.0f, 1f);

        private void OnEnable()
        {
            //Get GaiaHierarchyUtils Profile object
            m_profile = (GaiaHierarchyUtils)target;

            //Gets all the objects
           m_profile.UpdateParentObjects();

            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            m_guiBackground = GUI.backgroundColor;
            m_settingsChangedInfo = false;
        }

        public override void OnInspectorGUI()
        {
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!

            if (m_profile == null)
            {
                //Get GaiaHierarchyUtils Profile object
                m_profile = (GaiaHierarchyUtils)target;
            }

            //Monitor for changes
            EditorGUI.BeginChangeCheck();

            m_editorUtils.Panel("GlobalSettings", GlobalSettings, true);

            //Check for changes, make undo record, make changes and let editor know we are dirty
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_profile, "Made changes");
                EditorUtility.SetDirty(m_profile);

                m_profile.SetupHideInHierarchy();
                m_settingsChangedInfo = true;
            }
        }

        /// <summary>
        /// Applies the current settings by selecting the parent terrain, which forces an update of the scene view.
        /// </summary>
        public void ApplyNow()
        {
            Selection.activeObject = m_profile.transform.parent;
        }

        /// <summary>
        /// Global settings
        /// </summary>
        /// <param name="helpEnabled"></param>
        private void GlobalSettings(bool helpEnabled)
        {
            //m_profile.m_hideAllParentsInHierarchy = m_editorUtils.ToggleLeft("HideAllParentsInHierarchy", m_profile.m_hideAllParentsInHierarchy, helpEnabled);

            if (m_profile.m_visibilityEntries != null)
            {
                int count = m_profile.m_visibilityEntries.Count;
                if (count == 0)
                {
                    EditorGUILayout.HelpBox("No Objects found below this object to use this system please spawn some game objects to use the hide system.", MessageType.Info);
                }
                else
                {
                    GUI.backgroundColor = m_orangeGUI;
                    if (m_settingsChangedInfo)
                    {
                        if (m_editorUtils.Button("ApplyNow"))
                        {
                            ApplyNow();
                        }
                    }
                    GUI.backgroundColor = m_guiBackground;
                    GUIContent checkboxContent = m_editorUtils.GetContent("CheckboxTooltip");
                    foreach (GaiaHierarchyVisibility ghv in m_profile.m_visibilityEntries)
                    {
                        EditorGUILayout.BeginHorizontal();
                        ghv.m_isVisible = m_editorUtils.Toggle(ghv.m_isVisible, checkboxContent, GUILayout.Width(20));
                        GUILayout.Label(ghv.m_name);
                        EditorGUILayout.EndHorizontal();
                    }

                    GUI.backgroundColor = m_orangeGUI;
                    if (m_settingsChangedInfo)
                    {
                        if (m_editorUtils.Button("ApplyNow"))
                        {
                            ApplyNow();
                        }
                    }
                    GUI.backgroundColor = m_guiBackground;

                    GUI.backgroundColor = m_redGUI;
                    if (m_editorUtils.Button("HideAll"))
                    {
                        foreach (GaiaHierarchyVisibility ghv in m_profile.m_visibilityEntries)
                        {
                            ghv.m_isVisible = false;
                        }
                        m_profile.SetupHideInHierarchy();
                    }

                    GUI.backgroundColor = m_greenGUI;
                    if (m_editorUtils.Button("ShowAll"))
                    {
                        foreach (GaiaHierarchyVisibility ghv in m_profile.m_visibilityEntries)
                        {
                            ghv.m_isVisible = true;
                        }
                        m_profile.SetupHideInHierarchy();
                    }

                    //EditorGUILayout.LabelField("Object count in parent: " + count);
                    //if (m_profile.m_hideAllParentsInHierarchy)
                    //{
                    //    GUI.backgroundColor = m_redGUI;
                    //    if (m_editorUtils.Button("Show"))
                    //    {
                    //        m_profile.m_hideAllParentsInHierarchy = false;
                    //    }
                    //}
                    //else
                    //{

                    //}

                    GUI.backgroundColor = m_guiBackground;

                    //m_editorUtils.InlineHelp("HideAllParentsInHierarchy", helpEnabled);

                    if (m_editorUtils.Button("ConfigureAllInScene"))
                    {
                        m_profile.SetupAllHideInHierarchy();
                    }
                    
                }
            }
            else
            {
                EditorGUILayout.LabelField("Object count in parent: 0");
            }
        }
    }
}