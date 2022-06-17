using System;
using System.Collections;
using System.Collections.Generic;
using Gaia.Internal;
using PWCommon4;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
    public class LocationManagerEditor : EditorWindow, IPWEditor
    {
        private EditorUtils m_editorUtils;
        private LocationSystem m_profile;
        private Vector2 scrollPosition;
        private GUIStyle m_boxStyle;

        public bool PositionChecked { get; set; }

        /// <summary>
        /// Show Gaia Manager editor window
        /// </summary>
        public static void ShowLocationManager()
        {
            try
            {
                var manager = EditorWindow.GetWindow<Gaia.LocationManagerEditor>(false, "Location Manager");
                //Show manager if not null
                if (manager != null)
                {
                    Vector2 initialSize = new Vector2(600f, 400f);
                    manager.position = new Rect(new Vector2(Screen.currentResolution.width / 2f - initialSize.x / 2f, Screen.currentResolution.height / 2f - initialSize.y / 2f), initialSize);
                    manager.Show();
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "")
                { }
            }
        }
        private void OnEnable()
        {
            m_profile = GetLocationSystem();

            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            if (!Application.isPlaying)
            {
                if (GaiaUtils.CheckIfSceneProfileExists())
                {
                    m_profile.m_locationProfile.m_currentControllerType = GaiaGlobal.Instance.SceneProfile.m_controllerType;
                }
            }
        }
        /// <summary>
        /// Setup on destroy
        /// </summary>
        private void OnDestroy()
        {
            m_editorUtils?.Dispose();
        }

        public void OnGUI()
        {
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Width(position.width), GUILayout.Height(position.height));

            //Set up the box style
            if (m_boxStyle == null)
            {
                m_boxStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = {textColor = GUI.skin.label.normal.textColor},
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft
                };
            }

            if (m_profile == null)
            {
                m_profile = GetLocationSystem();
            }

            m_editorUtils.Panel("GlobalSettings", GlobalPanel, true);

            EditorGUILayout.EndScrollView();
        }
        private void GlobalPanel(bool helpEnabled)
        {
            if (Application.isPlaying)
            {
                if (GaiaUtils.CheckIfSceneProfileExists())
                {
                    if (m_profile.m_locationProfile.m_currentControllerType != GaiaGlobal.Instance.SceneProfile.m_controllerType)
                    {
                        EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("ControllerHasChanged"), MessageType.Warning);
                    }
                }
            }

            if (m_profile == null)
            {
                EditorGUILayout.HelpBox("Location manager allows you to track camera position at runtime and create bookmarks to load locations", MessageType.Info);
                if (m_editorUtils.Button("AddLocationManager"))
                {
                    m_profile = AddLocationSystem();
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();

                m_editorUtils.Panel("Setup", Setup);
                if (m_profile.m_locationProfile != null)
                {
                    GUI.enabled = true;
                }
                else
                {
                    GUI.enabled = false;
                }
                m_editorUtils.Panel("Controls", RuntimeControls);
                m_editorUtils.Panel("BookmarkSetup", BookmarkSetup, true);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(m_profile);
                    if (m_profile.m_locationProfile != null)
                    {
                        EditorUtility.SetDirty(m_profile.m_locationProfile);
                    }
                }
            }
        }
        private void RuntimeControls(bool helpEnabled)
        {
            m_editorUtils.Heading("Controls");
            if (m_profile.m_locationProfile != null)
            {
                m_profile.m_locationProfile.m_mainKey = (KeyCode)m_editorUtils.EnumPopup("MasterKey", m_profile.m_locationProfile.m_mainKey, helpEnabled);
                m_profile.m_locationProfile.m_addBookmarkKey = (KeyCode)m_editorUtils.EnumPopup("AddNewBookmark", m_profile.m_locationProfile.m_addBookmarkKey, helpEnabled);
                m_profile.m_locationProfile.m_prevBookmark = (KeyCode)m_editorUtils.EnumPopup("PreviousBookmark", m_profile.m_locationProfile.m_prevBookmark, helpEnabled);
                m_profile.m_locationProfile.m_nextBookmark = (KeyCode)m_editorUtils.EnumPopup("NextBookmark", m_profile.m_locationProfile.m_nextBookmark, helpEnabled);
            }
            else
            {
                EditorGUILayout.HelpBox("No location profile provided. Please add one.", MessageType.Info);
            }
        }
        private void Setup(bool helpEnabled)
        {
            m_profile.m_locationProfile = (LocationSystemScriptableObject)m_editorUtils.ObjectField("LocationProfile", m_profile.m_locationProfile, typeof(LocationSystemScriptableObject), false, helpEnabled);
            if (m_profile.m_locationProfile == null)
            {
                m_profile.m_locationProfile = AssetDatabase.LoadAssetAtPath<LocationSystemScriptableObject>(GaiaUtils.GetAssetPath("Location Profile.asset"));
            }

            if (m_profile.m_camera == null)
            {
                EditorGUILayout.BeginHorizontal();
                m_profile.m_camera = (Transform)m_editorUtils.ObjectField("Camera", m_profile.m_camera, typeof(Transform), true, helpEnabled);
                if (m_editorUtils.Button("FindCamera", GUILayout.MaxWidth(50f)))
                {
                    Camera locatedCam = GaiaUtils.GetCamera();
                    if (locatedCam != null)
                    {
                        m_profile.m_camera = locatedCam.transform;
                    }
                    else
                    {
                        Debug.LogWarning("No camera could be found in your scene please add one from the GameObject/Camera in the top toolbar.");
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("A camera transform must be provided for Location Manager to work. You can press 'Find' button to quickly locate the camera in the scene.", MessageType.Warning);
            }
            else
            {
                m_profile.m_camera = (Transform)m_editorUtils.ObjectField("Camera", m_profile.m_camera, typeof(Transform), true, helpEnabled);
            }

            m_profile.m_player = (Transform)m_editorUtils.ObjectField("Player", m_profile.m_player, typeof(Transform), true, helpEnabled);

            m_profile.m_trackPlayer = m_editorUtils.Toggle("TrackPlayer", m_profile.m_trackPlayer, helpEnabled);
        }
        private void BookmarkSetup(bool helpEnabled)
        {
            if (m_profile.m_locationProfile != null)
            {
                m_editorUtils.Heading("BookmarkSetup");
                m_profile.m_locationProfile.m_autoLoad = m_editorUtils.Toggle("AutoLoad", m_profile.m_locationProfile.m_autoLoad, helpEnabled);
                if (m_profile.m_locationProfile.m_bookmarkedLocationNames.Count > 0)
                {
                    int selected = m_profile.m_selectedBookmark;
                    for (int i = 0; i < m_profile.m_locationProfile.m_bookmarkedLocationNames.Count; i++)
                    {
                        if (m_profile.m_locationProfile.m_bookmarkedLocationNames[i] == m_profile.m_bookmarkName)
                        {
                            m_profile.m_bookmarkName = ObjectNames.GetUniqueName(m_profile.m_locationProfile.m_bookmarkedLocationNames.ToArray(), m_profile.m_bookmarkName);
                        }
                    }

                    EditorGUILayout.BeginHorizontal();
                    m_profile.m_bookmarkName = m_editorUtils.TextField("BookmarkName", m_profile.m_bookmarkName);
                    if (m_editorUtils.Button("AddBookmark", GUILayout.MaxWidth(60f)))
                    {
                        m_profile.m_locationProfile.AddNewBookmark(m_profile);
                        selected = m_profile.m_locationProfile.m_bookmarkedLocationNames.Count - 1;
                        m_profile.m_selectedBookmark = m_profile.m_locationProfile.m_bookmarkedLocationNames.Count - 1;
                    }
                    EditorGUILayout.EndHorizontal();
                    m_editorUtils.InlineHelp("BookmarkName", helpEnabled);

                    if (!m_profile.m_rename)
                    {
                        EditorGUILayout.BeginHorizontal();
                        selected = EditorGUILayout.Popup(new GUIContent(m_editorUtils.GetTextValue("SelectedBookmark"), m_editorUtils.GetTooltip("SelectedBookmark")), selected, m_profile.m_locationProfile.m_bookmarkedLocationNames.ToArray());
                        if (selected != m_profile.m_selectedBookmark)
                        {
                            m_profile.m_selectedBookmark = selected;
                            if (m_profile.m_locationProfile.m_autoLoad)
                            {
                                m_profile.m_locationProfile.LoadBookmark(m_profile);
                            }
                        }

                        if (m_editorUtils.Button("Rename", GUILayout.MaxWidth(60f)))
                        {
                            m_profile.m_savedName = m_profile.m_locationProfile.m_bookmarkedLocationNames[m_profile.m_selectedBookmark];
                            m_profile.m_rename = true;
                        }

                        if (m_editorUtils.Button("Override", GUILayout.MaxWidth(60f)))
                        {
                            m_profile.m_locationProfile.OverrideBookmark(m_profile);
                        }
                        if (!m_profile.m_locationProfile.m_autoLoad)
                        {
                            if (m_editorUtils.Button("LoadBookmark", GUILayout.MaxWidth(60f)))
                            {
                                m_profile.m_locationProfile.LoadBookmark(m_profile);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        m_editorUtils.InlineHelp("SelectedBookmark", helpEnabled);
                        if (m_profile.m_locationProfile.m_bookmarkedLocationNames.Count > 0)
                        {
                            EditorGUILayout.LabelField("Current Bookmark Controller Type Used: " + m_profile.m_locationProfile.m_bookmarkedSettings[selected].m_controllerUsed, EditorStyles.boldLabel);
                        }

                        EditorGUILayout.LabelField("Current Bookmark Was Created In Scene: " + m_profile.m_locationProfile.m_bookmarkedSettings[selected].m_sceneName, EditorStyles.boldLabel);
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        m_profile.m_savedName = m_editorUtils.TextField("BookmarkRename", m_profile.m_savedName);
                        if (m_editorUtils.Button("Save", GUILayout.MaxWidth(55f)))
                        {
                            m_profile.m_locationProfile.m_bookmarkedLocationNames[m_profile.m_selectedBookmark] = m_profile.m_savedName;
                            m_profile.m_rename = false;
                        }
                        EditorGUILayout.EndHorizontal();
                        m_editorUtils.InlineHelp("BookmarkRename", helpEnabled);
                    }

                    EditorGUILayout.BeginHorizontal();
                    if (m_editorUtils.Button("RemoveBookmark"))
                    {
                        if (EditorUtility.DisplayDialog("Removing Bookmark", "Are you sure you want to remove " + m_profile.m_locationProfile.m_bookmarkedLocationNames[selected] + " bookmark?", "Yes", "No"))
                        {
                            m_profile.m_locationProfile.RemoveBookmark(m_profile.m_selectedBookmark, m_profile);
                        }
                    }

                    if (m_editorUtils.Button("RemoveAllBookmark"))
                    {
                        if (EditorUtility.DisplayDialog("Removing All Bookmarks", "Are you sure you want to remove all the bookmarks?", "Yes", "No"))
                        {
                            m_profile.m_locationProfile.RemoveAllBookmarks();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    m_profile.m_selectedBookmark = 0;
                    EditorGUILayout.BeginHorizontal();
                    m_profile.m_bookmarkName = m_editorUtils.TextField("BookmarkName", m_profile.m_bookmarkName);
                    if (m_editorUtils.Button("AddBookmark", GUILayout.MaxWidth(55f)))
                    {
                        m_profile.m_locationProfile.AddNewBookmark(m_profile);
                        m_profile.m_selectedBookmark = m_profile.m_locationProfile.m_bookmarkedLocationNames.Count - 1;
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("No bookmarks added to use bookmark features please press 'Add Bookmark'", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No location profile provided. Please add one.", MessageType.Info);
            }
        }
        private static LocationSystem GetLocationSystem()
        {
            LocationSystem locationSystem = FindObjectOfType<LocationSystem>();
            return locationSystem;
        }
        /// <summary>
        /// Adds the system to the scene and sets it up
        /// </summary>
        /// <returns></returns>
        public static LocationSystem AddLocationSystem()
        {
            LocationSystem system = null;
            GameObject mainCam = GameObject.Find("Main Camera");
            if (mainCam != null)
            {
                system = mainCam.gameObject.GetComponent<LocationSystem>();
                if (system == null)
                {
                    system =  mainCam.gameObject.AddComponent<LocationSystem>();
                    system.m_camera = mainCam.transform;
                }
            }
            else
            {
                mainCam = GameObject.Find("Camera");
                if (mainCam != null)
                {
                    system = mainCam.gameObject.GetComponent<LocationSystem>();
                    if (system == null)
                    {
                        system = mainCam.gameObject.AddComponent<LocationSystem>();
                        system.m_camera = mainCam.transform;
                    }
                }
                else
                {
                    mainCam = GameObject.Find("FlyCam");
                    if (mainCam != null)
                    {
                        system = mainCam.gameObject.GetComponent<LocationSystem>();
                        if (system == null)
                        {
                            system = mainCam.gameObject.AddComponent<LocationSystem>();
                            system.m_camera = mainCam.transform;
                        }
                    }
                    else
                    {
                        mainCam = GameObject.Find("FirstPersonCharacter");
                        if (mainCam != null)
                        {
                            system = mainCam.gameObject.GetComponent<LocationSystem>();
                            if (system == null)
                            {
                                system = mainCam.gameObject.AddComponent<LocationSystem>();
                                system.m_camera = mainCam.transform;
                            }
                        }
                        else
                        {
                            mainCam = GameObject.FindGameObjectWithTag("MainCamera");
                            if (mainCam != null)
                            {
                                system = mainCam.gameObject.GetComponent<LocationSystem>();
                                if (system == null)
                                {
                                    system = mainCam.gameObject.AddComponent<LocationSystem>();
                                    system.m_camera = mainCam.transform;
                                }
                            }
                        }
                    }
                }
            }
            GameObject firstPersonController = GameObject.Find("FPSController");
            if (firstPersonController != null)
            {
                if (system != null)
                {
                    system.m_player = firstPersonController.transform;
                    return system;
                }
            }
            GameObject thirdPersonController = GameObject.Find("ThirdPersonController");
            if (thirdPersonController != null)
            {
                if (system != null)
                {
                    system.m_player = thirdPersonController.transform;
                    return system;
                }
            }
            GameObject fpsController = GameObject.Find("Player");
            if (fpsController != null)
            {
                if (system != null)
                {
                    system.m_player = fpsController.transform;
                    return system;
                }
            }

            return null;
        }
        /// <summary>
        /// Removes the location system
        /// </summary>
        public static void RemoveLocationSystem()
        {
            LocationSystem system = GameObject.FindObjectOfType<LocationSystem>();
            if (system != null)
            {
                GameObject.DestroyImmediate(system);
            }
        }
    }
}