using UnityEngine;
using UnityEditor;
using System.IO;
using Gaia.Internal;
using PWCommon4;
using UnityEngine.UI;

namespace Gaia
{
    [CustomEditor(typeof(Scanner))]
    public class ScannerEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
		[SerializeField] private GaiaConstants.RawByteOrder m_rawByteOrder;
        [SerializeField] private GaiaConstants.RawBitDepth m_rawBitDepth = GaiaConstants.RawBitDepth.Sixteen;
		private Scanner m_scanner;
        private GUIStyle m_boxStyle;
        private GUIStyle m_dropBoxStyle;
        private GUIStyle m_wrapStyle;
        private Color m_redGUI = new Color(0.500f, 0.0f, 0.0f, 1f);
        private Color m_greenGUI = new Color(0.696f, 0.905f, 0.397f, 1f);
        private string m_rawFilePath;
		private int m_assumedRawRes = 0;
        private string m_infoMessage = "";
        private string m_scanMessage = "No object scanned yet - Drag and drop a raw file, texture or terrain on the box above to begin!";


        // The below are used so GUI update doesn't happen at wrong times causing layout mismatches
        private bool m_showRawInfo = false;
		private bool m_showBitDepthWarning = false;
        private GaiaSettings m_settings;
        private readonly GUIContent BYTE_ORDER_LABEL = new GUIContent("Raw File Byte Order", "The byte order used when creating from RAW files. Try changing this if your stamp comes out flat.");
		private readonly GUIContent BIT_DEPTH_LABEL = new GUIContent("Raw File Bit Depth", "The bit depth used when creating from RAW files. Try changing this if the processing resolution is double, or half of what you expected.\n\n" + "NOTE: 8-bit RAW files have very poor precision and result in terraced stamps.");
		private readonly GUIContent[] BIT_DEPTHS_LABELS = new GUIContent[] 
        {
			new GUIContent("16-bit (Recommended)", "16-bit RAW files can contain higher precision data."),
			new GUIContent("8-bit", "8-bit RAW files have low precision and result in terraced stamps/terrains."),
		};

        private void OnEnable()
        {
            m_scanner = (Scanner)target;

            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
            UpdateMaterial();
            m_infoMessage = "";
        }

        public override void OnInspectorGUI()
        {
            m_scanner = (Scanner)target;

            m_editorUtils.Initialize(); // Do not remove this!

            //Set up the box style
            if (m_boxStyle == null)
            {
                m_boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    normal = { textColor = GUI.skin.label.normal.textColor },
                    fontStyle = FontStyle.Bold,
                    fontSize = GUI.skin.label.fontSize,
                    alignment = TextAnchor.UpperLeft
                };
            }

            if (m_dropBoxStyle == null)
            {
                m_dropBoxStyle = new GUIStyle(GUI.skin.box)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }

            //Setup the wrap style
            if (m_wrapStyle == null)
            {
                m_wrapStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Normal, wordWrap = true
                };
            }

            m_editorUtils.Panel("GlobalSettings", GlobalPanel, true);
        }

        private void GlobalPanel(bool helpEnabled)
        {
            //Text intro
            GUILayout.BeginVertical("Gaia Scanner", m_boxStyle);
            GUILayout.Space(20);
            EditorGUILayout.LabelField("The Gaia Scanner allows you to create new stamps from Windows R16, Windows 16 bit RAW, Mac 16 bit RAW, Terrains, Textures or Meshes. Just drag and drop the file or object onto the area below to scan it.", m_wrapStyle);
            GUILayout.EndVertical();

            DropAreaGUI();

            bool originalGUIState = GUI.enabled;

            if (!m_scanner.m_objectScanned)
            {
                GUI.enabled = false;
            }

            //DrawDefaultInspector();
            GUILayout.BeginVertical("Setup", m_boxStyle);
            GUILayout.Space(20);
            m_editorUtils.Heading("GlobalSettings");
            m_scanner.m_previewMaterial = (Material)m_editorUtils.ObjectField("PreviewMaterial", m_scanner.m_previewMaterial, typeof(Material), false, helpEnabled);
            EditorGUILayout.BeginHorizontal();
            m_scanner.m_exportFolder = m_editorUtils.TextField("ExportPath", m_scanner.m_exportFolder);
            
            if (m_editorUtils.Button("ExportDirectoryOpen", GUILayout.Width(80)))
            {
                string path = EditorUtility.SaveFolderPanel(m_editorUtils.GetTextValue("ExportDirectoryWindowTitle"), GaiaDirectories.GetUserStampDirectory(), GaiaDirectories.SCANNER_EXPORT_DIRECTORY.Replace("/",""));
                if (path.Contains(Application.dataPath))
                {
                    m_scanner.m_exportFolder = GaiaDirectories.GetPathStartingAtAssetsFolder(path);
                }
                else
                {
                    EditorUtility.DisplayDialog("Path outside the Assets folder", "The selected path needs to be inside the Assets folder of the project. Please select an appropiate path.", "OK");
                    m_scanner.m_exportFolder = GaiaDirectories.GetScannerExportDirectory();
                }
            }
            EditorGUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("ExportPath", helpEnabled);
            m_scanner.m_exportFileName = m_editorUtils.TextField("ExportFilename", m_scanner.m_exportFileName, helpEnabled);

            float oldBaseLevel = m_scanner.m_baseLevel;
            m_scanner.m_baseLevel = m_editorUtils.Slider("BaseLevel", m_scanner.m_baseLevel, 0f, 1f, helpEnabled);
            if (oldBaseLevel != m_scanner.m_baseLevel)
            {
                SceneView.lastActiveSceneView.Repaint();
            }
            EditorGUILayout.Space(10f);
            m_editorUtils.Heading("ObjectTypeSettings");
            if (m_scanner.m_scannerObjectType == ScannerObjectType.Mesh)
            {
                m_scanner.m_scanResolution = m_editorUtils.Slider("ScanResolution", m_scanner.m_scanResolution, 0.0001f, 1f, helpEnabled);
                if (m_scanner.m_lastScanResolution != m_scanner.m_scanResolution)
                {
                    if(m_editorUtils.Button("RefreshScan"))
                    {
                        m_scanner.LoadGameObject(m_scanner.m_lastScannedMesh);
                    }
                }

            }
            if (m_scanner.m_scannerObjectType == ScannerObjectType.Raw)
            {
                //Drop Options section
                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PrefixLabel(BYTE_ORDER_LABEL);
                    EditorGUI.BeginChangeCheck();
                    {
                        m_rawByteOrder = (GaiaConstants.RawByteOrder)GUILayout.Toolbar((int)m_rawByteOrder, new string[] { "IBM PC", "Macintosh" });
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        ReloadRawFile();
                    }
                }
                GUILayout.EndHorizontal();
                m_editorUtils.InlineHelp("RawFileByteOrder", helpEnabled);

                EditorGUI.BeginChangeCheck();
                {
                    m_rawBitDepth = (GaiaConstants.RawBitDepth)EditorGUILayout.Popup(BIT_DEPTH_LABEL, (int)m_rawBitDepth, BIT_DEPTHS_LABELS);
                    m_editorUtils.InlineHelp("RawFileBitDepth", helpEnabled);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    ReloadRawFile();
                }
                GUILayout.BeginVertical();
                if (m_showRawInfo)
                {
                    EditorGUILayout.HelpBox("Assumed " + (m_rawBitDepth == GaiaConstants.RawBitDepth.Sixteen ? "16-bit" : "8-bit") + " RAW " + m_assumedRawRes + " x " + m_assumedRawRes, MessageType.Info);
                }
                if (m_showBitDepthWarning)
                {
                    EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("8BitWarning"), MessageType.Warning);
                }
                GUILayout.EndVertical();
            }
            EditorGUILayout.Space(10f);

          

            m_editorUtils.Heading("ExportSettings");
            //m_scanner.m_textureExportResolution = (GaiaConstants.GaiaProWaterReflectionsQuality) EditorGUILayout.EnumPopup(new GUIContent("Export Resolution", "Sets the export resolution of the texture generated"), m_scanner.m_textureExportResolution);
            m_scanner.m_normalize = m_editorUtils.Toggle("Normalize", m_scanner.m_normalize, helpEnabled);
            m_scanner.m_exportTextureAlso = m_editorUtils.Toggle("ExportPNG", m_scanner.m_exportTextureAlso, helpEnabled);
            //m_scanner.m_exportBytesData = m_editorUtils.Toggle("ExportBytesData", m_scanner.m_exportBytesData, helpEnabled);

            GUILayout.EndVertical();

            //Terraform section
            GUILayout.BeginVertical("Scanner Controller", m_boxStyle);
            GUILayout.Space(20);
            if (!string.IsNullOrEmpty(m_infoMessage))
            {
                EditorGUILayout.HelpBox(m_infoMessage, MessageType.Info);
            }
            if (m_scanner.m_scanMap == null)
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("NoScanDataInfo"), MessageType.Info);
                GUI.enabled = false;
            }
            GUILayout.BeginHorizontal();
            Color normalBGColor = GUI.backgroundColor;
            if (m_settings == null)
            {
                m_settings = GaiaUtils.GetGaiaSettings();
            }

            GUI.backgroundColor = m_settings.GetActionButtonColor(); 
            if (m_editorUtils.Button("SaveScan"))
            {
                string path = m_scanner.SaveScan();
                AssetDatabase.Refresh();
                path += ".exr";
                Object exportedTexture = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
                if (exportedTexture != null)
                {
                    m_infoMessage = "Scan exported to: " + path;
                    Debug.Log(m_infoMessage);
                    EditorGUIUtility.PingObject(exportedTexture);
                }

            }
            GUI.backgroundColor = normalBGColor;
            GUI.enabled = true;
            if (m_editorUtils.Button("Clear"))
            {
                m_scanner.Clear();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.EndVertical();
            GUILayout.Space(5f);

            m_showRawInfo = m_assumedRawRes > 0;
			m_showBitDepthWarning = m_rawBitDepth == GaiaConstants.RawBitDepth.Eight;

            GUI.enabled = originalGUIState;
        }

        private void DropAreaGUI()
        {
            //Ok - set up for drag and drop
            Event evt = Event.current;
            Rect drop_area = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(drop_area, ">> Drop Objects Here To Scan <<", m_dropBoxStyle);
            EditorGUILayout.HelpBox(m_scanMessage, MessageType.Info);

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    {
                        if (!drop_area.Contains(evt.mousePosition))
                        {
                            break;
                        }

                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
							m_rawFilePath = null;
							m_assumedRawRes = 0;

							//Is it a saved file - only raw files are processed this way
							if (DragAndDrop.paths.Length > 0)
                            {
                                string filePath = DragAndDrop.paths[0];

                                //Update in case unity has messed with it 
                                if (filePath.StartsWith("Assets"))
                                {
                                    filePath = Path.Combine(Application.dataPath, filePath.Substring(7)).Replace('\\', '/');
                                }

                                //Check file type and process as we can
                                string fileType = Path.GetExtension(filePath).ToLower();

                                //Handle raw files
                                if (fileType == ".r16" || fileType == ".raw")
                                {
                                    m_scanMessage = "Dropped Object recognized as a raw file";
                                    m_scanner.m_scannerObjectType = ScannerObjectType.Raw;
                                    m_scanner.m_objectScanned = true;
                                    m_rawFilePath = filePath;
									m_scanner.LoadRawFile(filePath, m_rawByteOrder, ref m_rawBitDepth, ref m_assumedRawRes);
                                    return;
                                }
                            }

                            //Is it something that unity knows about - may or may not have been saved
                            bool finished = false;
                            if (DragAndDrop.objectReferences.Length > 0)
                            {
                                switch (DragAndDrop.objectReferences[0])
                                {
                                    //Check for textures
                                    case Texture2D _:
                                    {
                                        EditorUtility.DisplayProgressBar("Processing Texture", "Processing texture to mask texture", 0.5f);
                                            GaiaUtils.MakeTextureReadable(DragAndDrop.objectReferences[0] as Texture2D);
                                            GaiaUtils.MakeTextureUncompressed(DragAndDrop.objectReferences[0] as Texture2D);
                                        m_scanner.LoadTextureFile(DragAndDrop.objectReferences[0] as Texture2D);
                                        m_scanMessage = "Dropped Object recognized as a texture";
                                        m_scanner.m_scannerObjectType = ScannerObjectType.Texture;
                                        finished = true;
                                        EditorUtility.ClearProgressBar();
                                        break;
                                    }
                                    //Check for terrains
                                    case GameObject _:
                                    {
                                        GameObject go = DragAndDrop.objectReferences[0] as GameObject;
                                        Terrain t = go.GetComponentInChildren<Terrain>();
                                        //Handle a terrain
                                        if (t != null)
                                        {
                                            EditorUtility.DisplayProgressBar("Processing Terrain", "Processing terrain to mask texture", 0.5f);
                                            m_scanner.LoadTerain(t);
                                            m_scanMessage = "Dropped Object recognized as terrain";
                                            m_scanner.m_scannerObjectType = ScannerObjectType.Terrain;
                                            finished = true;
                                            EditorUtility.ClearProgressBar();
                                        }

                                        //Check for a mesh - this means we can scan it
                                        if (!finished)
                                        {
                                            MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>();
                                            for (int idx = 0; idx < filters.Length; idx++)
                                            {
                                                if (filters[idx].sharedMesh != null)
                                                {
                                                    m_scanMessage = "Dropped Object recognized as mesh";
                                                    m_scanner.m_scannerObjectType = ScannerObjectType.Mesh;
                                                    m_scanner.m_lastScannedMesh = go;
                                                    m_scanner.LoadGameObject(go);
                                                    finished = true;
                                                }
                                            }
                                        }
                                        break;
                                    }
                                }
                            }

                            //If we got to here then we couldnt process it
                            if (!finished)
                            {
                                Debug.LogWarning("Object type not supported by scanner. Ignored");
                                m_scanMessage = "Object type not supported by scanner.";
                                m_scanner.m_scannerObjectType = ScannerObjectType.Unkown;
                                m_scanner.m_objectScanned = false;
                            }
                            else
                            {
                                m_scanner.m_objectScanned = true;
                            }
                        }
                        break;
                    }
            }
        }

        private void ReloadRawFile() 
        {
			if (string.IsNullOrEmpty(m_rawFilePath)) 
            {
				return;
			}
			m_assumedRawRes = 0;
			m_scanner.LoadRawFile(m_rawFilePath, m_rawByteOrder, ref m_rawBitDepth, ref m_assumedRawRes);
		}

        private void UpdateMaterial()
        {
            if (m_scanner != null)
            {
                if (m_scanner.m_previewMaterial != null)
                {
                    GaiaConstants.EnvironmentRenderer renderPipeline = GaiaUtils.GetActivePipeline();
                    Texture2D texture = m_scanner.m_previewMaterial.GetTexture("_MainTex") as Texture2D;
                    if (texture == null)
                    {
                        texture = m_scanner.m_previewMaterial.GetTexture("_BaseMap") as Texture2D;
                    }

                    switch (renderPipeline)
                    {
                        case GaiaConstants.EnvironmentRenderer.BuiltIn:
                            m_scanner.m_previewMaterial.shader = Shader.Find("Standard");
                            m_scanner.m_previewMaterial.SetTexture("_MainTex", texture);
                            break;
                        case GaiaConstants.EnvironmentRenderer.Universal:
                            m_scanner.m_previewMaterial.shader = Shader.Find("Universal Render Pipeline/Lit");
                            m_scanner.m_previewMaterial.SetTexture("_BaseMap", texture);
                            break;
                        case GaiaConstants.EnvironmentRenderer.HighDefinition:
                            m_scanner.m_previewMaterial.shader = Shader.Find("HDRP/Lit");
                            m_scanner.m_previewMaterial.SetTexture("_BaseMap", texture);
                            break;
                    }
                }
            }
        }
    }
}
