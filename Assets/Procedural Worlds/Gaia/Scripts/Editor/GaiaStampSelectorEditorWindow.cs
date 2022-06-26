using Gaia.Internal;
using PWCommon4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
    /// <summary>
    /// Some cool terrain modification utilities
    /// </summary>
    public class GaiaStampSelectorEditorWindow : EditorWindow, IPWEditor
    {
        private EditorUtils m_editorUtils;
        public StamperSettings m_editedStamperSettings;
        public ImageMask m_editedImageMask;
        private Texture2D m_previewTexture;
        private string m_originalTextureGUID;
        private List<string> m_categoryNames = new List<string>();
        private Stamper[] allStampers;
        private Spawner[] allSpawners;
        private BiomeController[] allBiomeController;
        private int m_selectedCategoryID = -99;
        private FileInfo[] m_currentStampDirFileInfo;
        private int m_currentStampIndex;
        private bool m_validStampInCategory = true;

        public bool PositionChecked { get; set; }

        public void Init(ImageMask imageMask = null)
        {

            allBiomeController = (BiomeController[])Resources.FindObjectsOfTypeAll(typeof(BiomeController));
            allStampers = (Stamper[])Resources.FindObjectsOfTypeAll(typeof(Stamper));
            allSpawners = (Spawner[])Resources.FindObjectsOfTypeAll(typeof(Spawner));

            m_categoryNames.Clear();
            var info = new DirectoryInfo(GaiaDirectories.GetStampDirectory());
            var fileInfo = info.GetDirectories();
            foreach (DirectoryInfo dir in fileInfo)
            {
                m_categoryNames.Add(dir.Name);
            }

            //Do the same with the user stamp folder
            info = new DirectoryInfo(GaiaDirectories.GetUserStampDirectory());
            fileInfo = info.GetDirectories();
            foreach (DirectoryInfo dir in fileInfo)
            {
                m_categoryNames.Add(dir.Name);
            }

            m_categoryNames.Sort();

            if (imageMask != null)
            {
                m_editedImageMask = imageMask;
            }
          
            //read & store the original texture guid if the user wants to cancel
            if ((m_editedImageMask != null && m_editedImageMask.ImageMaskTexture != null))
            {
                string pathToImage = "";
                if (m_editedImageMask != null)
                {
                    pathToImage = AssetDatabase.GetAssetPath(m_editedImageMask.ImageMaskTexture);
                }
                //else
                //{
                //    pathToImage = AssetDatabase.GetAssetPath(m_editedStamperSettings.m_stamperInputImage);
                //}
                m_originalTextureGUID = AssetDatabase.AssetPathToGUID(pathToImage);
                m_previewTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(pathToImage, typeof(Texture2D));

                //Try to locate the selected image in the stamps directory
                if (pathToImage.Contains(GaiaDirectories.STAMP_DIRECTORY))
                {
                    string lastFolderName = Path.GetFileName(Path.GetDirectoryName(pathToImage));
                    m_selectedCategoryID = m_categoryNames.FindIndex(x => x == lastFolderName);
                    LoadStampDirFileInfo();
                    int index = -1;
                    foreach (var fi in m_currentStampDirFileInfo)
                    {
                        index++;
                        if (pathToImage.Contains(fi.Name))
                        {
                            m_currentStampIndex = index;
                        }
                    }
                }
            }
            else
            {
                m_originalTextureGUID = "";
            }
        }


        void OnEnable()
        {
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
            titleContent = m_editorUtils.GetContent("WindowTitle");
        }

        void OnGUI()
        {
            m_editorUtils.Initialize();
            m_editorUtils.Panel("SelectStampPanel", SelectStampPanel, true);

        }

        private void SelectStampPanel(bool helpEnabled)
        {

            m_editorUtils.InlineHelp("Help", helpEnabled);


            if (m_editedImageMask == null && m_editedStamperSettings == null)
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("NoConnectedImageMask"), MessageType.Warning, true);
            }

            m_editorUtils.Label("StampPreview");
            EditorGUILayout.BeginVertical();
            if (m_previewTexture != null && m_validStampInCategory)
            {
                m_editorUtils.Image(m_previewTexture, position.width - 25, position.width - 25);
                m_editorUtils.Label(new GUIContent(m_previewTexture.name));
            }
            else
            {
                
                if (!m_validStampInCategory)
                {
                    GUILayout.Space(71);
                    EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("NoValidStampInCategory"), MessageType.Info, true);
                }
                else if (m_previewTexture == null)
                {
                    GUILayout.Space(67);
                    EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("NoTexture"), MessageType.Info, true);
                }
                GUILayout.Space(155);
            }
            GUILayout.Space(EditorGUIUtility.singleLineHeight);

            Rect rect = EditorGUILayout.GetControlRect();
            if (GUI.Button(new Rect(rect.x, rect.y, 50, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("StampBackwardButton")))
            {
                PickNextStamp(-1);
            }

            //Building up a value array of incrementing ints of the size of the directory names array, this array will then match the displayed string selection in the popup
            int[] categoryIDArray = Enumerable
                                .Repeat(0, (int)((m_categoryNames.ToArray().Length - 0) / 1) + 1)
                                .Select((tr, ti) => tr + (1 * ti))
                                .ToArray();

            int previousCategoryID = m_selectedCategoryID;
            m_selectedCategoryID = EditorGUI.IntPopup(new Rect(rect.x + 60, rect.y, rect.width - 120, EditorGUIUtility.singleLineHeight), m_selectedCategoryID, m_categoryNames.ToArray(), categoryIDArray);
            if (previousCategoryID != m_selectedCategoryID)
            {
                LoadStampDirFileInfo();
                m_currentStampIndex = -1;
                m_validStampInCategory = PickNextStamp(1);
            }

            if (GUI.Button(new Rect(rect.x + 60 + rect.width - 110, rect.y, 50, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("StampForwardButton")))
            {
                PickNextStamp(1);
            }

            rect.y += EditorGUIUtility.singleLineHeight * 2;

            if (GUI.Button(new Rect(rect.x + 20, rect.y, 100, EditorGUIUtility.singleLineHeight * 2), m_editorUtils.GetContent("ApplyButton")))
            {
                Close();
            }
            if (GUI.Button(new Rect(rect.x + rect.width - 100 - 20, rect.y, 100, EditorGUIUtility.singleLineHeight * 2), m_editorUtils.GetContent("CancelButton")))
            {
                //Load original stamp back in and close
                if (m_editedImageMask != null)
                {
                    m_editedImageMask.ImageMaskTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_originalTextureGUID), typeof(Texture2D));
                }
                //if (m_editedStamperSettings != null)
                //{
                //    m_editedStamperSettings.m_stamperInputImage = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_originalTextureGUID), typeof(Texture2D));
                //}
                UpdateToolsAndScene();
                Close();

            }


            EditorGUILayout.EndVertical();
        }

        private void LoadStampDirFileInfo()
        {
            if (m_selectedCategoryID != -99)
            {
                string directory = GaiaDirectories.GetStampDirectory() + "/" + m_categoryNames[m_selectedCategoryID];

                if (!Directory.Exists(directory))
                { 
                    directory = GaiaDirectories.GetUserStampDirectory() + "/" + m_categoryNames[m_selectedCategoryID];
                }

                if (!Directory.Exists(directory))
                {
                    Debug.LogWarning("Could not find the selected subdirectory '" + m_categoryNames[m_selectedCategoryID] + "' neither in the Gaia Stamps or in the User Stamps folder when trying to browse stamps.");
                    return;
                }

                var info = new DirectoryInfo(directory);

                if (info == null)
                {
                    Debug.LogWarning("Could not access directory " + directory + " when trying to browse stamps.");
                    return;
                }
                

                m_currentStampDirFileInfo = info.GetFiles();
            }
        }

        private void UpdateToolsAndScene()
        {
            foreach (Stamper stamper in allStampers)
            {
                stamper.m_stampDirty = true;
            }
            foreach (Spawner spawner in allSpawners)
            {
                spawner.m_spawnPreviewDirty = true;
                spawner.SetWorldBiomeMasksDirty();
            }
            foreach (BiomeController controller in allBiomeController)
            {
                controller.m_biomePreviewDirty = true;
            }
            EditorWindow view = EditorWindow.GetWindow<SceneView>();
            view.Repaint();
        }

        private bool PickNextStamp(int direction)
        {
            //try to load a stamp file from the next index
            int nextIndexCandidate = m_currentStampIndex + direction;

            if (m_currentStampDirFileInfo == null)
            {
                LoadStampDirFileInfo();
            }

            //still null? abort!
            if (m_currentStampDirFileInfo == null)
            {
                return false;
            }


            if (0 <= nextIndexCandidate && nextIndexCandidate < m_currentStampDirFileInfo.Length - 1)
            {
                try
                {
                    //Try to load the next texture2D into the preview texture, whatever file it is.
                    string assetPath = GaiaDirectories.GetStampDirectory() + "/" + m_categoryNames[m_selectedCategoryID] + "/" + m_currentStampDirFileInfo[nextIndexCandidate].Name;
                    m_previewTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D));
                    if (m_previewTexture == null)
                    {
                        //Not found? Try to load from the user stamps folder instead
                        assetPath = GaiaDirectories.GetUserStampDirectory() + "/" + m_categoryNames[m_selectedCategoryID] + "/" + m_currentStampDirFileInfo[nextIndexCandidate].Name;
                        m_previewTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D));
                    }

                    if (m_previewTexture == null)
                    {
                        //no exception, but no valid texture file either, let's try the next index then
                        m_currentStampIndex = nextIndexCandidate;
                        return PickNextStamp(direction);

                    }
                    else
                    {
                        m_currentStampIndex = nextIndexCandidate;
                        if (m_editedImageMask != null)
                        {
                            m_editedImageMask.ImageMaskTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D));
                        }
                        //else
                        //{
                        //    m_editedStamperSettings.m_stamperInputImage = (Texture2D)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D));
                        //}
                        UpdateToolsAndScene();
                        return true;
                    }
                }
                catch (Exception)
                {
                    //there might be more valid files, let's try the next index then
                    m_currentStampIndex = nextIndexCandidate;
                    return PickNextStamp(direction);
                }

            }
            else
            {
                return false;
            }

        }

    }
}