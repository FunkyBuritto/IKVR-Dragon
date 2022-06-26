using Gaia.Internal;
using PWCommon4;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
   /// <summary>
   /// Utility to convert stamps from the old data format into .exr images
   /// </summary>

    class ConvertStamps : EditorWindow, IPWEditor
    {
        private EditorUtils m_editorUtils;

        //The folder path we will process
        string folderPath = "";

        //Are subfolders included?
        bool includeSubFolders = true;

        //Remove the source files after conversion?
        bool deleteSourceFiles = true;

        //The absolute number of stamp preview jpgs we are going to process
        int jpgCount = 0;

        //The current jpg we are at for the progress bar
        int jpgIndex = 0;

        public bool PositionChecked { get => true; set => PositionChecked = value; }

        private void OnEnable()
        {
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
        }

        void OnGUI()
        {
            m_editorUtils.Initialize();

            m_editorUtils.Panel("ConvertGaia1Stamps", DrawConvertStamps, true);

          
        }

        private void DrawConvertStamps(bool helpEnabled)
        {
            EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("Info"), MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            {
                folderPath = m_editorUtils.TextField("Folder", folderPath);
                if (m_editorUtils.Button("Select Folder"))
                {
                    folderPath = EditorUtility.OpenFolderPanel("Select Stamp Folder", "", "");
                }
            }
            EditorGUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("Folder", helpEnabled);
            GUILayout.Space(15f);
            includeSubFolders = m_editorUtils.Toggle("Include Subfolders", includeSubFolders, helpEnabled);
            deleteSourceFiles = m_editorUtils.Toggle("Remove Old Stamp Data", deleteSourceFiles, helpEnabled);
            GUILayout.Space(25f);

            if (m_editorUtils.Button("Start Conversion"))
            {
                string popupText = "This conversion process will look for old Gaia Stamps in the folder\n\n" + folderPath +
                                    "\n\n and convert them into the new format. Running this process is only required if you " +
                                    "still have stamps that consist of a preview picture and the actual data in a 'Data' Folder.";

                if (deleteSourceFiles)
                {
                    popupText += "\n\nWARNING: You selected to delete the source files. This process will delete all .jpg files and all 'Data' folders in the given directory after the conversion.";
                }
                popupText += "\n\nContinue?";


                if (EditorUtility.DisplayDialog("Starting Conversion", popupText, "OK", "Cancel"))
                {
                    StartConversion(folderPath);
                }
            }
        }

        private void StartConversion(string folderPath)
        {
            EditorUtility.ClearProgressBar();
            //Get the count of jpg files for the progress bar
            jpgIndex = 1;
            jpgCount = Directory.GetFiles(folderPath, "*.jpg", SearchOption.AllDirectories).Length;

            //Begin with root folder, the subfolders will be handled automatically with recursion
            ConvertFolder(folderPath);

            jpgIndex = 1;

            if (deleteSourceFiles)
            {
                RemoveSourceData(folderPath);
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        private void RemoveSourceData(string folderPath)
        {
            //Handle Subfolders recursively
            if (includeSubFolders)
            {
                string[] subFolders = Directory.GetDirectories(folderPath);
                foreach (string s in subFolders)
                {
                    //Did we find a data folder? Delete it
                    if (s.EndsWith("\\Data"))
                    {
                        FileUtil.DeleteFileOrDirectory(s);
                    }
                    else //no data folder -> search inside
                    {
                        RemoveSourceData(s);
                    }
                }
            }

            //Delete all stamp preview files
            var allJPGs = Directory.GetFiles(folderPath, "*.jpg", SearchOption.TopDirectoryOnly);
            foreach (string filename in allJPGs)
            {
                EditorUtility.DisplayProgressBar("Deleting Source Files...", "Deleting File " + jpgIndex.ToString() + " of " + jpgCount.ToString(), Mathf.InverseLerp(0, jpgCount, jpgIndex));
                FileUtil.DeleteFileOrDirectory(filename);
            }

        }

        private void ConvertFolder(string folderPath)
        {
            //Handle Subfolders recursively
            if (includeSubFolders)
            {
                string[] subFolders = Directory.GetDirectories(folderPath);
                foreach (string s in subFolders)
                {
                    ConvertFolder(s);
                }
            }

            //Do the actual file conversion in the current folder
            string[] files = Directory.GetFiles(folderPath);

            foreach (string file in files)
            {
                if (file.EndsWith(".jpg"))
                {
                    EditorUtility.DisplayProgressBar("Converting Stamps", "Processing Stamp " + jpgIndex.ToString() + " of " + jpgCount.ToString(), Mathf.InverseLerp(0, jpgCount, jpgIndex));
                    //Get path relative to project folder else the function to load the asset will create a warning in the console
                    string relativePath = file.Substring(file.IndexOf("Assets", 0));
                    var tex = (Texture2D)GaiaUtils.GetAsset(relativePath, typeof(Texture2D));
                    if (GaiaUtils.CheckValidGaiaStampPath(tex))
                    {
                        var heightMap = new UnityHeightMap(GaiaUtils.GetGaiaStampPath(tex));
                        GaiaUtils.CompressToMultiChannelFileImage(file.Replace(".jpg", ""), heightMap, heightMap, heightMap, null, TextureFormat.RGBAFloat, GaiaConstants.ImageFileType.Exr);
                        jpgIndex++;
                    }
                }
            }

        }

    }
}