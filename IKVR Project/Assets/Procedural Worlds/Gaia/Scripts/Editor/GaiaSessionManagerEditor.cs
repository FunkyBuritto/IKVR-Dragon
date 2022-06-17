using UnityEngine;
#if UNITY_EDITOR
using Gaia.Internal;
using PWCommon4;
using UnityEditor;
#endif
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using ProceduralWorlds.WaterSystem;

namespace Gaia
{
    /// <summary>
    /// Editor for reource manager
    /// </summary>
    [CustomEditor(typeof(GaiaSessionManager))]
    public class GaiaSessionManagerEditor : PWEditor
    {
        //GUIStyle m_boxStyle;
        //GUIStyle m_wrapStyle;
        //GUIStyle m_descWrapStyle;
        private GUIStyle m_operationCreateWorldStyle;
        private GUIStyle m_operationFlattenTerrainStyle;
        private GUIStyle m_operationClearSpawnsStyle;
        private GUIStyle m_operationStampStyle;
        private GUIStyle m_operationStampUndoRedoStyle;
        private GUIStyle m_operationSpawnStyle;
        private GUIStyle m_operationRemoveNonBiomeResourcesStyle;
        private GUIStyle m_operationMaskMapExportStyle;
        private GUIStyle m_operationCheckboxStyle;
        private GUIStyle m_operationFoldOutStyle;
        private List<Texture2D> m_tempTextureList = new List<Texture2D>();
        private Vector2 m_scrollPosition = Vector2.zero;
        GaiaSessionManager m_manager;
        private EditorUtils m_editorUtils;


        //private int m_lastSessionID = -1;
        //private string m_lastPreviewImgName = "";
        //private bool m_showTooltips = true;

        private void Awake()
        {
#if GAIA_PRO_PRESENT
            WorldOriginEditor.m_sessionManagerExits = true;
#else
           Gaia2TopPanel.m_sessionManagerExits = true;
#endif
        }

        void OnEnable()
        {
            m_manager = (GaiaSessionManager)target;

            //Init editor utils
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
            

            GaiaLighting.SetPostProcessingStatus(false);
        }

        private void OnDisable()
        {
            GaiaLighting.SetPostProcessingStatus(true);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < m_tempTextureList.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(m_tempTextureList[i]);
            }
        }

        public override void OnInspectorGUI()
        {
            m_editorUtils.Initialize(); // Do not remove this!
            m_manager = (GaiaSessionManager)target;
            serializedObject.Update();

            SetupOperationHeaderColor(ref m_operationCreateWorldStyle, "3FC1C9ff", "297e83ff", m_tempTextureList);
            SetupOperationHeaderColor(ref m_operationFlattenTerrainStyle, "C46564ff", "804241ff", m_tempTextureList);
            SetupOperationHeaderColor(ref m_operationClearSpawnsStyle, "F0E999ff", "9d9864ff", m_tempTextureList);
            SetupOperationHeaderColor(ref m_operationStampStyle, "B8C99Dff", "788367ff", m_tempTextureList);
            SetupOperationHeaderColor(ref m_operationStampUndoRedoStyle, "d1a6a3ff", "896c6bff", m_tempTextureList);
            SetupOperationHeaderColor(ref m_operationSpawnStyle, "EEB15Bff", "9c743bff", m_tempTextureList);
            SetupOperationHeaderColor(ref m_operationRemoveNonBiomeResourcesStyle, "ba7fcdff", "7a5386ff", m_tempTextureList);
            SetupOperationHeaderColor(ref m_operationMaskMapExportStyle, "9e955bff", "635D39ff", m_tempTextureList);

           if (m_operationCheckboxStyle == null)
           {
                m_operationCheckboxStyle = new GUIStyle(GUI.skin.toggle);
                m_operationCheckboxStyle.fixedWidth = 15;
                m_operationCheckboxStyle.margin = new RectOffset(5,0,0,0);
                m_operationCheckboxStyle.padding = new RectOffset(0, 0, 0, 5);
           }

           if (m_operationFoldOutStyle == null)
           { 
            m_operationFoldOutStyle = new GUIStyle(EditorStyles.foldout);
                m_operationFoldOutStyle.margin = new RectOffset(0, 0, 0, 0);
           }

            //m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);

            m_editorUtils.Panel("Summary", DrawSummary, true);

            m_editorUtils.Panel("Operations", DrawOperations, true);


            //End scroll
            //GUILayout.EndScrollView();

#region OLD CODE

            ////Set up the box style
            //if (m_boxStyle == null)
            //{
            //    m_boxStyle = new GUIStyle(GUI.skin.box);
            //    m_boxStyle.normal.textColor = GUI.skin.label.normal.textColor;
            //    m_boxStyle.fontStyle = FontStyle.Bold;
            //    m_boxStyle.alignment = TextAnchor.UpperLeft;
            //}

            ////Setup the wrap style
            //if (m_wrapStyle == null)
            //{
            //    m_wrapStyle = new GUIStyle(GUI.skin.label);
            //    m_wrapStyle.wordWrap = true;
            //}

            ////Set up the description wrap style
            //if (m_descWrapStyle == null)
            //{
            //    m_descWrapStyle = new GUIStyle(GUI.skin.textArea);
            //    m_descWrapStyle.wordWrap = true;
            //}

            ////Scroll

            ////Create a nice text intro
            //GUILayout.BeginVertical("Gaia Session Manager", m_boxStyle);
            //GUILayout.Space(20);
            //EditorGUILayout.LabelField("Track and control session creation and playback.", m_wrapStyle);
            //GUILayout.Space(4);
            //EditorGUILayout.BeginHorizontal();
            //m_manager.m_session = (GaiaSession)EditorGUILayout.ObjectField(GetLabel("Session"), m_manager.m_session, typeof(GaiaSession), false);

            //if (GUILayout.Button(GetLabel("New"), GUILayout.Width(45)))
            //{
            //    m_manager.CreateSession();
            //}
            //EditorGUILayout.EndHorizontal();
            //GUILayout.EndVertical();

            //if (m_manager.m_session == null)
            //{
            //    GUILayout.EndScrollView();
            //    return;
            //}

            ////Track changes
            ////EditorGUI.BeginChangeCheck();

            ////Make some space
            //GUILayout.Space(4);

            ////Wrap it up in a box

            ////Display the basic details
            //EditorGUILayout.LabelField("Name");
            //if (m_manager.IsLocked())
            //{
            //    GUI.enabled = false;
            //}
            //string name = EditorGUILayout.TextArea(m_manager.m_session.m_name, m_descWrapStyle, GUILayout.MinHeight(15));
            //GUI.enabled = true;

            //EditorGUILayout.LabelField("Description");
            //if (m_manager.IsLocked())
            //{
            //    GUI.enabled = false;
            //}
            //string description = EditorGUILayout.TextArea(m_manager.m_session.m_description, m_descWrapStyle, GUILayout.MinHeight(45));

            //Texture2D previewImage = m_manager.GetPreviewImage();
            //if (!m_manager.IsLocked())
            //{
            //    previewImage = (Texture2D)EditorGUILayout.ObjectField(GetLabel("Preview Image"), m_manager.m_session.m_previewImage, typeof(Texture2D), false, GUILayout.MaxHeight(15f));
            //}

            ////Detect change in session and handle changes to preview image
            //float width, height;
            //if (m_manager.m_session.GetInstanceID() != m_lastSessionID)
            //{
            //    m_lastPreviewImgName = "";
            //    m_lastSessionID = m_manager.m_session.GetInstanceID();
            //    if (m_manager.HasPreviewImage())
            //    {
            //        previewImage = m_manager.GetPreviewImage();
            //        m_lastPreviewImgName = previewImage.name;
            //    }
            //}
            //else //Process changes to preview image
            //{
            //    if (previewImage == null)
            //    {
            //        if (m_manager.IsLocked()) //Undo change if locked
            //        {
            //            if (m_manager.HasPreviewImage())
            //            {
            //                previewImage = m_manager.GetPreviewImage();
            //                m_lastPreviewImgName = previewImage.name;
            //                Debug.LogWarning("You can not change the image on a locked session");
            //            }
            //        }
            //        else
            //        {
            //            if (m_manager.HasPreviewImage())
            //            {
            //                m_manager.RemovePreviewImage();
            //                m_lastPreviewImgName = "";
            //            }
            //        }
            //    }
            //    else
            //    {
            //        //Handle changes to preview image
            //        if (previewImage.name != m_lastPreviewImgName)
            //        {
            //            if (m_manager.IsLocked()) //Revert
            //            {
            //                if (m_manager.HasPreviewImage())
            //                {
            //                    previewImage = m_manager.GetPreviewImage();
            //                    m_lastPreviewImgName = previewImage.name;
            //                    Debug.LogWarning("You can not change the image on a locked session");
            //                }
            //                else
            //                {
            //                    previewImage = null;
            //                    m_lastPreviewImgName = "";
            //                }
            //            }
            //            else
            //            {
            //                //Make it readable
            //                Gaia.GaiaUtils.MakeTextureReadable(previewImage);

            //                //Make a new texture from it
            //                Texture2D newTexture = new Texture2D(previewImage.width, previewImage.height, TextureFormat.ARGB32, false);
            //                newTexture.name = m_manager.m_session.m_name;
            //                newTexture.SetPixels(previewImage.GetPixels(0));
            //                newTexture.Apply();

            //                //Resize and scale it
            //                width = 320;
            //                height = previewImage.height * (width / previewImage.width);
            //                Gaia.ScaleTexture.Bilinear(newTexture, (int)width, (int)height);

            //                //Compress it
            //                //newTexture.Compress(true);

            //                //And store its content
            //                m_manager.AddPreviewImage(newTexture);

            //                //Assign back to the texture for the scene
            //                previewImage = newTexture;
            //                m_lastPreviewImgName = previewImage.name;
            //            }
            //        }
            //    }
            //}

            //GUI.enabled = true; //In response to locked above

            //if (previewImage != null)
            //{
            //    //Get aspect ratio and available space and display the image
            //    width = Screen.width - 43f;
            //    height = previewImage.height * (width / previewImage.width);
            //    GUILayout.Label(previewImage, GUILayout.MaxWidth(width), GUILayout.MaxHeight(height));
            //}

            //EditorGUILayout.LabelField("Created", m_manager.m_session.m_dateCreated);
            //EditorGUILayout.LabelField("Dimensions", string.Format("w{0} d{1} h{2}", m_manager.m_session.m_terrainWidth, m_manager.m_session.m_terrainDepth, m_manager.m_session.m_terrainHeight));

            //if (m_manager.IsLocked())
            //{
            //    GUI.enabled = false;
            //}
            //m_manager.m_session.m_seaLevel = EditorGUILayout.Slider(GetLabel("Sea Level"), m_manager.m_session.m_seaLevel, 0f, m_manager.m_session.m_terrainDepth);
            //GUI.enabled = true; //In response to locked above

            //bool locked = EditorGUILayout.Toggle(GetLabel("Locked"), m_manager.m_session.m_isLocked);
            //GUILayout.EndVertical();

            ////Iterate through the operations
            //GUILayout.BeginVertical("Operations:", m_boxStyle);
            //GUILayout.Space(20);

            //if (m_manager.m_session.m_operations.Count == 0)
            //{
            //    GUILayout.Space(5);
            //    GUILayout.Label("No operations yet...");
            //    GUILayout.Space(5);
            //}
            //else
            //{
            //    GaiaOperation op;
            //    EditorGUI.indentLevel++;
            //    for (int opIdx = 0; opIdx < m_manager.m_session.m_operations.Count; opIdx++)
            //    {
            //        op = m_manager.m_session.m_operations[opIdx];

            //        if (op.m_isActive)
            //        {
            //            op.m_isFoldedOut = EditorGUILayout.Foldout(op.m_isFoldedOut, op.m_description, true);
            //        }
            //        else
            //        {
            //            op.m_isFoldedOut = EditorGUILayout.Foldout(op.m_isFoldedOut, op.m_description + " [inactive]", true);
            //        }

            //        if (op.m_isFoldedOut)
            //        {
            //            EditorGUI.indentLevel++;

            //            EditorGUILayout.LabelField("Description", op.m_description, m_wrapStyle);
            //            EditorGUILayout.LabelField("Created", op.m_operationDateTime);
            //            if (m_manager.m_session.m_isLocked)
            //            {
            //                GUI.enabled = false;
            //            }
            //            op.m_isActive = EditorGUILayout.Toggle(GetLabel("Active"), op.m_isActive);
            //            GUI.enabled = true;

            //            int dataLength = 0;
            //            for (int idx = 0; idx < op.m_operationDataJson.GetLength(0); idx++)
            //            {
            //                dataLength += op.m_operationDataJson[idx].Length;
            //            }
            //            EditorGUILayout.LabelField("Data", dataLength.ToString() + " bytes");

            //            GUILayout.BeginHorizontal();
            //            GUILayout.FlexibleSpace();
            //            if (m_manager.m_session.m_isLocked)
            //            {
            //                GUI.enabled = false;
            //            }
            //            if (GUILayout.Button(GetLabel("Delete")))
            //            {
            //                if (EditorUtility.DisplayDialog("Delete Operation ?", "Are you sure you want to delete this operation ?", "Yes", "No"))
            //                {
            //                    m_manager.RemoveOperation(opIdx);
            //                }
            //            }
            //            GUI.enabled = true;
            //            if (GUILayout.Button(GetLabel("Apply")))
            //            {
            //                //m_manager.Apply(opIdx);
            //            }
            //            if (GUILayout.Button(GetLabel("Play")))
            //            {
            //                m_manager.PlayOperation(opIdx);
            //            }
            //            GUILayout.EndHorizontal();

            //            EditorGUI.indentLevel--;
            //        }
            //        //EditorGUILayout.Space();
            //    }
            //    EditorGUI.indentLevel--;
            //}
            //GUILayout.EndVertical();

            ////Create a nice text intro
            //if (!m_manager.m_session.m_isLocked)
            //{
            //    GUILayout.BeginVertical(m_boxStyle);
            //    m_manager.m_genShowRandomGenerator = EditorGUILayout.BeginToggleGroup(GetLabel(" Random Terrain Generator"), m_manager.m_genShowRandomGenerator);
            //    if (m_manager.m_genShowRandomGenerator)
            //    {
            //        m_manager.m_useRandomSeed = EditorGUILayout.Toggle("Use Random Seed", m_manager.m_useRandomSeed);
            //        if (!m_manager.m_useRandomSeed)
            //        {
            //            m_manager.m_randomSeed = EditorGUILayout.IntField(GetLabel("Random Seed"), m_manager.m_randomSeed);
            //        }
            //        m_manager.m_genGridSize = EditorGUILayout.IntSlider(GetLabel("Stamp Grid"), m_manager.m_genGridSize, 1, 5);
            //        if (m_manager.m_genGridSize == 1)
            //        {
            //            m_manager.m_genNumStampsToGenerate = 1;
            //        }
            //        else
            //        {
            //            m_manager.m_genNumStampsToGenerate = (m_manager.m_genGridSize * m_manager.m_genGridSize) + 1;
            //        }
            //        EditorGUILayout.LabelField(new GUIContent("Stamps Generated"), new GUIContent(m_manager.m_genNumStampsToGenerate.ToString()));
            //        //m_manager.m_genNumStampsToGenerate = EditorGUILayout.IntSlider(GetLabel("Stamps"), m_manager.m_genNumStampsToGenerate, 1, 26);
            //        m_manager.m_genScaleWidth = EditorGUILayout.Slider(GetLabel("Width Scale"), m_manager.m_genScaleWidth, 0.5f, 100f);
            //        m_manager.m_genScaleHeight = EditorGUILayout.Slider(GetLabel("Height Scale"), m_manager.m_genScaleHeight, 0.5f, 100f);
            //        m_manager.m_genBorderStyle = (Gaia.GaiaConstants.GeneratorBorderStyle)EditorGUILayout.EnumPopup(GetLabel("Border Style"), m_manager.m_genBorderStyle);
            //        m_manager.m_genChanceOfHills = EditorGUILayout.Slider(GetLabel("Hill Chance"), m_manager.m_genChanceOfHills, 0f, 1f);
            //        m_manager.m_genChanceOfIslands = EditorGUILayout.Slider(GetLabel("Island Chance"), m_manager.m_genChanceOfIslands, 0f, 1f);
            //        m_manager.m_genChanceOfLakes = EditorGUILayout.Slider(GetLabel("Lake Chance"), m_manager.m_genChanceOfLakes, 0f, 1f);
            //        m_manager.m_genChanceOfMesas = EditorGUILayout.Slider(GetLabel("Mesa Chance"), m_manager.m_genChanceOfMesas, 0f, 1f);
            //        m_manager.m_genChanceOfMountains = EditorGUILayout.Slider(GetLabel("Mountain Chance"), m_manager.m_genChanceOfMountains, 0f, 1f);
            //        m_manager.m_genChanceOfPlains = EditorGUILayout.Slider(GetLabel("Plains Chance"), m_manager.m_genChanceOfPlains, 0f, 1f);
            //        m_manager.m_genChanceOfRivers = EditorGUILayout.Slider(GetLabel("River Chance"), m_manager.m_genChanceOfRivers, 0f, 1f);
            //        m_manager.m_genChanceOfValleys = EditorGUILayout.Slider(GetLabel("Valley Chance"), m_manager.m_genChanceOfValleys, 0f, 1f);
            //        m_manager.m_genChanceOfVillages = EditorGUILayout.Slider(GetLabel("Village Chance"), m_manager.m_genChanceOfVillages, 0f, 1f);
            //        m_manager.m_genChanceOfWaterfalls = EditorGUILayout.Slider(GetLabel("Waterfall Chance"), m_manager.m_genChanceOfWaterfalls, 0f, 1f);

            //        GUILayout.BeginHorizontal();
            //        if (GUILayout.Button(GetLabel("Reset Session")))
            //        {
            //            if (EditorUtility.DisplayDialog("Reset Session ?", "Are you sure you want to reset your session - this can not be undone ?", "Yes", "No"))
            //            {
            //                m_manager.ResetSession();
            //            }
            //        }
            //        if (GUILayout.Button(GetLabel("Add Stamps")))
            //        {
            //            m_manager.RandomiseStamps();
            //        }
            //        GUILayout.EndHorizontal();
            //    }
            //    EditorGUILayout.EndToggleGroup();
            //    GUILayout.EndVertical();
            //}

            ////Create a nice text intro
            //GUILayout.BeginVertical(m_boxStyle);
            //m_manager.m_genShowTerrainHelper = EditorGUILayout.BeginToggleGroup(GetLabel(" Terrain Helper"), m_manager.m_genShowTerrainHelper);
            //if (m_manager.m_genShowTerrainHelper)
            //{
            //    GUILayout.BeginHorizontal();
            //    if (GUILayout.Button(GetLabel("Flatten Terrain")))
            //    {
            //        if (EditorUtility.DisplayDialog("Flatten Terrain?", "Are you sure you want to flatten your terrain - this can not be undone ?", "Yes", "No"))
            //        {
            //            GaiaWorldManager wm = new GaiaWorldManager(Terrain.activeTerrains);
            //            wm.FlattenWorld();
            //        }
            //    }
            //    if (GUILayout.Button(GetLabel("Smooth Terrain")))
            //    {
            //        if (EditorUtility.DisplayDialog("Smooth Terrain?", "Are you sure you want to smooth your terrain - this can not be undone ?", "Yes", "No"))
            //        {
            //            GaiaWorldManager wm = new GaiaWorldManager(Terrain.activeTerrains);
            //            wm.SmoothWorld();
            //        }
            //    }
            //    GUILayout.EndHorizontal();

            //    GUILayout.BeginHorizontal();
            //    if (GUILayout.Button(GetLabel("Clear Trees")))
            //    {
            //        if (EditorUtility.DisplayDialog("Clear Trees?", "Are you sure you want to clear your trees - this can not be undone ?", "Yes", "No"))
            //        {
            //            TerrainHelper.ClearTrees(ClearSpawnFor.AllTerrains,ClearSpawnFrom.AnySource);
            //        }
            //    }
            //    if (GUILayout.Button(GetLabel("Clear Details")))
            //    {
            //        if (EditorUtility.DisplayDialog("Clear Details?", "Are you sure you want to clear your details / grass - this can not be undone ?", "Yes", "No"))
            //        {
            //            TerrainHelper.ClearDetails(ClearSpawnFor.AllTerrains,ClearSpawnFrom.AnySource);
            //        }
            //    }
            //    GUILayout.EndHorizontal();
            //}
            //EditorGUILayout.EndToggleGroup();
            //GUILayout.EndVertical();




            //GUILayout.BeginVertical("Session Controller", m_boxStyle);
            //GUILayout.Space(20);
            //GUILayout.BeginHorizontal();
            //bool focusSceneView = EditorGUILayout.Toggle(GetLabel("Focus Scene View"), m_manager.m_focusSceneView);
            //GUILayout.EndHorizontal();
            //GUILayout.Space(5);
            //GUILayout.BeginHorizontal();
            //if (m_manager.m_updateSessionCoroutine == null && m_manager.m_updateOperationCoroutine == null)
            //{



            //    if (GUILayout.Button(GetLabel("Play Session")))
            //    {
            //        if (EditorUtility.DisplayDialog("Playback Session ?", "Are you sure you want to playback your session - this can not be undone ?", "Yes", "No"))
            //        {
            //            m_manager.PlaySession();
            //        }
            //    }
            //    if (GUILayout.Button(GetLabel("Export Resources")))
            //    {
            //        //m_manager.ExportSessionResources();
            //    }
            //}
            //else
            //{
            //    if (GUILayout.Button(GetLabel("Cancel")))
            //    {
            //        m_manager.CancelPlayback();
            //    }
            //}

            //GUILayout.EndHorizontal();
            //GUILayout.Space(5);


            ////Check for changes, make undo record, make changes and let editor know we are dirty
            //if (EditorGUI.EndChangeCheck())
            //{
            //    Undo.RecordObject(m_manager, "Made changes");
            //    m_manager.m_session.m_name = name;
            //    m_manager.m_session.m_description = description;
            //    m_manager.m_session.m_isLocked = locked;
            //    m_manager.m_session.m_previewImage = previewImage;
            //    m_manager.m_focusSceneView = focusSceneView;
            //    EditorUtility.SetDirty(m_manager.m_session);
            //    EditorUtility.SetDirty(m_manager);
            //}

            ////Debug.Log(m_manager.m_lastUpdateDateTime);

            ////Draw the various spawner progress bars
            //if (m_manager.m_currentStamper != null)
            //{
            //    if (m_manager.m_currentStamper.IsStamping())
            //    {
            //        ProgressBar(string.Format("{0}:{1} ({2:0.0}%)", m_manager.m_currentStamper.gameObject.name, m_manager.m_currentStamper.m_stampImage.name, m_manager.m_currentStamper.m_stampProgress * 100f), m_manager.m_currentStamper.m_stampProgress);
            //    }
            //}
            //if (m_manager.m_currentSpawner != null)
            //{
            //    if (m_manager.m_currentSpawner.IsSpawning())
            //    {
            //        ProgressBar(string.Format("{0} ({1:0.0}%)", m_manager.m_currentSpawner.gameObject.name, m_manager.m_currentSpawner.m_spawnProgress * 100f), m_manager.m_currentSpawner.m_spawnProgress);
            //    }
            //}

            //GUILayout.EndVertical();


            //GUILayout.EndVertical();

#endregion
        }

        public static void SetupOperationHeaderColor(ref GUIStyle style, string normalColor, string proColor, List<Texture2D> tempTextureList)
        {
            if (style == null || style.normal.background == null)
            {
                style = new GUIStyle();
                style.stretchWidth = true;
                style.margin = new RectOffset(5, 5, 0, 0);
                //m_operationHeaderStyle.overflow = new RectOffset(2, 2, 2, 2);

                // Setup colors for Unity Pro
                if (EditorGUIUtility.isProSkin)
                {
                    style.normal.background = GaiaUtils.GetBGTexture(GaiaUtils.GetColorFromHTML(proColor), tempTextureList);
                }
                // or Unity Personal
                else
                {
                    style.normal.background = GaiaUtils.GetBGTexture(GaiaUtils.GetColorFromHTML(normalColor), tempTextureList);
                }
            }
        }

        private void DrawSummary(bool helpEnabled)
        {
            m_manager.m_session = (GaiaSession)m_editorUtils.ObjectField("SessionData", m_manager.m_session, typeof(GaiaSession), helpEnabled);
            m_editorUtils.InlineHelp("SessionData", helpEnabled);
            if (m_manager.m_session == null)
            {
                if (m_editorUtils.Button("CreateSessionButton"))
                {
                    m_manager.CreateSession();
                }
            }
            if (m_manager.m_session == null)
            {
                return;
            }
            EditorGUI.BeginChangeCheck();
            m_manager.m_session.m_name = m_editorUtils.DelayedTextField("Name", m_manager.m_session.m_name, helpEnabled);
            if (EditorGUI.EndChangeCheck())
            {
                //Get the old path
                string oldSessionDataPath = GaiaDirectories.GetSessionSubFolderPath(m_manager.m_session);
                //Rename the session asset
                AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(m_manager.m_session), m_manager.m_session.m_name + ".asset");
                //rename the session data path as well
                string newSessionDataPath = GaiaDirectories.GetSessionSubFolderPath(m_manager.m_session, false);
                AssetDatabase.MoveAsset(oldSessionDataPath, newSessionDataPath);
                //if we have terrain scenes stored in the Terrain Loader, we need to update the paths in there as well
                foreach (TerrainScene terrainScene in TerrainLoaderManager.TerrainScenes)
                {
                    terrainScene.m_scenePath = terrainScene.m_scenePath.Replace(oldSessionDataPath, newSessionDataPath);
                    terrainScene.m_impostorScenePath = terrainScene.m_impostorScenePath.Replace(oldSessionDataPath, newSessionDataPath);
                    terrainScene.m_backupScenePath = terrainScene.m_backupScenePath.Replace(oldSessionDataPath, newSessionDataPath);
                    terrainScene.m_colliderScenePath = terrainScene.m_colliderScenePath.Replace(oldSessionDataPath, newSessionDataPath);
                }
                TerrainLoaderManager.Instance.SaveStorageData();

                AssetDatabase.DeleteAsset(oldSessionDataPath);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(m_editorUtils.GetContent("Description"), GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
            m_manager.m_session.m_description = EditorGUILayout.TextArea(m_manager.m_session.m_description, GUILayout.MinHeight(100));
            EditorGUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("Description", helpEnabled);
            m_manager.m_session.m_previewImage = (Texture2D)m_editorUtils.ObjectField("PreviewImage", m_manager.m_session.m_previewImage, typeof(Texture2D), helpEnabled);
            GUILayout.BeginHorizontal();
            Rect rect = EditorGUILayout.GetControlRect();
            GUILayout.Space(rect.width - 20);
            if (GUILayout.Button("Generate Image"))
            {
                string textureFileName = GaiaDirectories.GetSessionSubFolderPath(m_manager.m_session) + Path.DirectorySeparatorChar + m_manager.m_session + "_Preview";
                var originalLODBias = QualitySettings.lodBias;
                QualitySettings.lodBias = 100;
                OrthographicBake.BakeTerrain(Terrain.activeTerrain, 2048, 2048, Camera.main.cullingMask, textureFileName);
                OrthographicBake.RemoveOrthoCam();
                QualitySettings.lodBias = originalLODBias;
                textureFileName += ".png";
                AssetDatabase.ImportAsset(textureFileName);
                var importer = AssetImporter.GetAtPath(textureFileName) as TextureImporter;
                if (importer != null)
                {
                    importer.sRGBTexture = false;
                    importer.alphaIsTransparency = false;
                    importer.alphaSource = TextureImporterAlphaSource.None;
                    importer.mipmapEnabled = false;
                }
                AssetDatabase.ImportAsset(textureFileName);
                m_manager.m_session.m_previewImage = (Texture2D)AssetDatabase.LoadAssetAtPath(textureFileName, typeof(Texture2D));
            }
            GUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("PreviewImage", helpEnabled);
            m_editorUtils.LabelField("Created", new GUIContent(m_manager.m_session.m_dateCreated), helpEnabled);
            m_manager.m_session.m_isLocked = m_editorUtils.Toggle("Locked", m_manager.m_session.m_isLocked, helpEnabled);
            float maxSeaLevel = 2000f;
            if (Terrain.activeTerrain != null)
            {
                maxSeaLevel = Terrain.activeTerrain.terrainData.size.y + Terrain.activeTerrain.transform.position.y;
            }
            else
            {
                maxSeaLevel = m_manager.GetSeaLevel(false) + 500f;
            }

            float oldSeaLevel = m_manager.GetSeaLevel(false);
            float newSeaLEvel = oldSeaLevel;
            newSeaLEvel = m_editorUtils.Slider("SeaLevel", newSeaLEvel, 0, maxSeaLevel, helpEnabled);
            if (newSeaLEvel != oldSeaLevel)
            {
                //Do we have a water instance? If yes, update it & it will update the sea level in the session as well
                if (PWS_WaterSystem.Instance != null)
                {
                    PWS_WaterSystem.Instance.SeaLevel = newSeaLEvel;
                }
                else
                {
                    //no water instance yet, just update the sea level in the session
                    m_manager.SetSeaLevel(newSeaLEvel,false);
                    SceneView.RepaintAll();
                }
            }

            m_manager.m_session.m_spawnDensity = m_editorUtils.FloatField("SpawnDensity", Mathf.Max(0.01f, m_manager.m_session.m_spawnDensity), helpEnabled);
            GUILayout.BeginHorizontal();
            if (m_editorUtils.Button("DeleteAllOperations"))
            {
                if (EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("PopupDeleteAllTitle"), m_editorUtils.GetTextValue("PopupDeleteAllMessage"), m_editorUtils.GetTextValue("Continue"), m_editorUtils.GetTextValue("Cancel")))
                {
                    foreach (GaiaOperation op in m_manager.m_session.m_operations)
                    {
                        try
                        {
                            if (!String.IsNullOrEmpty(op.scriptableObjectAssetGUID))
                            {
                                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(op.scriptableObjectAssetGUID));
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("Error while deleting one of the operation data files: " + ex.Message + " Stack Trace:" + ex.StackTrace);
                        }
                    }

                    m_manager.m_session.m_operations.Clear();
                }
            }

            if (m_editorUtils.Button("PlaySession"))
            {
                GaiaLighting.SetDefaultAmbientLight(GaiaUtils.GetGaiaSettings().m_gaiaLightingProfile);
                GaiaSessionManager.PlaySession();
            }
            GUILayout.EndHorizontal();
        }


        private void DrawOperations(bool helpEnabled)
        {
            if (m_manager.m_session == null)
            {
                return;
            }
            if (m_manager.m_session.m_operations.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(8);
                bool oldSelectAll = m_manager.m_selectAllOperations;
                m_manager.m_selectAllOperations = m_editorUtils.Toggle(m_manager.m_selectAllOperations, m_editorUtils.GetContent("SelectAllToolTip"));

                if (m_manager.m_selectAllOperations != oldSelectAll)
                {
                    foreach (GaiaOperation op in m_manager.m_session.m_operations)
                    {
                        op.m_isActive = m_manager.m_selectAllOperations;
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(50);
                GUILayout.FlexibleSpace();
                m_editorUtils.Text("NoOperationsYet");
                GUILayout.FlexibleSpace();
                GUILayout.Space(50);
                GUILayout.EndHorizontal();
            }
            //Extra indent needed to draw the foldouts in the correct position
            EditorGUI.indentLevel++;
            bool currentGUIState = GUI.enabled;
            
            for (int i=0;i<m_manager.m_session.m_operations.Count;i++)
            {
                GaiaOperation op = m_manager.m_session.m_operations[i];
                GUIStyle headerStyle = m_operationCreateWorldStyle;

                switch (op.m_operationType)
                {
                    case GaiaOperation.OperationType.CreateWorld:
                        headerStyle = m_operationCreateWorldStyle;
                        break;
                    case GaiaOperation.OperationType.ClearSpawns:
                        headerStyle = m_operationClearSpawnsStyle;
                        break;
                    case GaiaOperation.OperationType.FlattenTerrain:
                        headerStyle = m_operationFlattenTerrainStyle;
                        break;
                    case GaiaOperation.OperationType.RemoveNonBiomeResources:
                        headerStyle = m_operationRemoveNonBiomeResourcesStyle;
                        break;
                    case GaiaOperation.OperationType.Spawn:
                        headerStyle = m_operationSpawnStyle;
                        break;
                    case GaiaOperation.OperationType.Stamp:
                        headerStyle = m_operationStampStyle;
                        break;
                    case GaiaOperation.OperationType.StampUndo:
                        headerStyle = m_operationStampUndoRedoStyle;
                        break;
                    case GaiaOperation.OperationType.StampRedo:
                        headerStyle = m_operationStampUndoRedoStyle;
                        break;
                    case GaiaOperation.OperationType.MaskMapExport:
                        headerStyle = m_operationMaskMapExportStyle;
                        break;
                }
                GUI.enabled = op.m_isActive; 
                GUILayout.BeginHorizontal(headerStyle);
                GUI.enabled = currentGUIState;
                op.m_isActive = GUILayout.Toggle(op.m_isActive, "", m_operationCheckboxStyle);
                GUI.enabled = op.m_isActive; 
                op.m_isFoldedOut = m_editorUtils.Foldout(op.m_isFoldedOut, new GUIContent((i+1).ToString() + " " + op.m_description.ToString()), true, m_operationFoldOutStyle);
                GUILayout.EndHorizontal();
                GUI.enabled = currentGUIState;

                if (op.m_isFoldedOut)
                {
                    DrawOperationFields(op, m_editorUtils, m_manager, helpEnabled, i);
                }
                GUILayout.Space(2);
            }
            EditorGUI.indentLevel--;
        }


        /// <summary>
        /// Draws the data fields for each operation 
        /// </summary>
        /// <param name="op"></param>
        public static void DrawOperationFields(GaiaOperation op, EditorUtils editorUtils, GaiaSessionManager sessionManager, bool helpEnabled, int currentIndex)
        {
            //shared default fields first
            //op.m_isActive = m_editorUtils.Toggle("Active", op.m_isActive, helpEnabled);
            bool currentGUIState = GUI.enabled;
            GUI.enabled = op.m_isActive;
            op.m_description = editorUtils.TextField("Description", op.m_description, helpEnabled);
            editorUtils.LabelField("DateTime", new GUIContent(op.m_operationDateTime), helpEnabled);
            EditorGUI.indentLevel++;
            op.m_terrainsFoldedOut = editorUtils.Foldout(op.m_terrainsFoldedOut, "AffectedTerrains", helpEnabled);

            if (op.m_terrainsFoldedOut)
            {
                foreach (string name in op.m_affectedTerrainNames)
                {
                    EditorGUILayout.LabelField(name);
                }
            }
            EditorGUI.indentLevel--;

            //type specific fields, switch by op type to draw additional fields suitable for the op type

            switch (op.m_operationType)
            {
                case GaiaOperation.OperationType.CreateWorld:
                    editorUtils.LabelField("xTiles", new GUIContent(op.WorldCreationSettings.m_xTiles.ToString()), helpEnabled);
                    editorUtils.LabelField("zTiles", new GUIContent(op.WorldCreationSettings.m_zTiles.ToString()), helpEnabled);
                    editorUtils.LabelField("TileSize", new GUIContent(op.WorldCreationSettings.m_tileSize.ToString()), helpEnabled);
                    break;
                case GaiaOperation.OperationType.Spawn:
                    editorUtils.LabelField("NumberOfSpawners", new GUIContent(op.SpawnOperationSettings.m_spawnerSettingsList.Count.ToString()), helpEnabled);
                    float size = (float)Mathd.Max(op.SpawnOperationSettings.m_spawnArea.size.x, op.SpawnOperationSettings.m_spawnArea.size.z);
                    editorUtils.LabelField("SpawnSize", new GUIContent(size.ToString()), helpEnabled);
                    break;
            }
            GUI.enabled = currentGUIState;
            //Button controls
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            if (editorUtils.Button("Delete"))
            {
                if (EditorUtility.DisplayDialog(editorUtils.GetTextValue("PopupDeleteTitle"), editorUtils.GetTextValue("PopupDeleteText"), editorUtils.GetTextValue("OK"), editorUtils.GetTextValue("Cancel")))
                {
                    try
                    {
                        if (!String.IsNullOrEmpty(op.scriptableObjectAssetGUID))
                        {
                            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(op.scriptableObjectAssetGUID));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Error while deleting one of the operation data files: " + ex.Message + " Stack Trace:" + ex.StackTrace);
                    }

                    sessionManager.RemoveOperation(currentIndex);
                    EditorGUIUtility.ExitGUI();
                }
            }
            GUI.enabled = op.m_isActive;
            if (editorUtils.Button("Play"))
            {
                if (EditorUtility.DisplayDialog(editorUtils.GetTextValue("PopupPlayTitle"), editorUtils.GetTextValue("PopupPlayText"), editorUtils.GetTextValue("OK"), editorUtils.GetTextValue("Cancel")))
                {
                    GaiaSessionManager.ExecuteOperation(op);
                    //Destroy all temporary tools used while executing
                    //not if it is a spawn operation since that is asynchronous
                    if (op.m_operationType != GaiaOperation.OperationType.Spawn)
                    {
                        GaiaSessionManager.DestroyTempSessionTools();
                    }
                }
            }
            GUI.enabled = currentGUIState;
            //EditorGUILayout.EndHorizontal();
            //EditorGUILayout.BeginHorizontal();
            //GUILayout.Space(20);
            if (editorUtils.Button("ViewData"))
            {
                switch (op.m_operationType)
                {
                    case GaiaOperation.OperationType.CreateWorld:
                        Selection.activeObject = op.WorldCreationSettings;
                        break;
                    case GaiaOperation.OperationType.Stamp:
                        Selection.activeObject = op.StamperSettings;
                        break;
                    case GaiaOperation.OperationType.Spawn:
                        Selection.activeObject = op.SpawnOperationSettings;
                        break;
                    case GaiaOperation.OperationType.FlattenTerrain:
                        Selection.activeObject = op.FlattenOperationSettings;
                        break;
                    case GaiaOperation.OperationType.StampUndo:
                        Selection.activeObject = op.UndoRedoOperationSettings;
                        break;
                    case GaiaOperation.OperationType.StampRedo:
                        Selection.activeObject = op.UndoRedoOperationSettings;
                        break;
                    case GaiaOperation.OperationType.ClearSpawns:
                        Selection.activeObject = op.ClearOperationSettings;
                        break;
                    case GaiaOperation.OperationType.RemoveNonBiomeResources:
                        Selection.activeObject = op.RemoveNonBiomeResourcesSettings;
                        break;
                    case GaiaOperation.OperationType.MaskMapExport:
                        Selection.activeObject = op.ExportMaskMapOperationSettings;
                        break;
                }

                EditorGUIUtility.PingObject(Selection.activeObject);
            }
            switch (op.m_operationType)
            {
                case GaiaOperation.OperationType.Stamp:
                    if (editorUtils.Button("PreviewInStamper"))
                    {
                        Stamper stamper = GaiaSessionManager.GetOrCreateSessionStamper();
                        stamper.LoadSettings(op.StamperSettings);
#if GAIA_PRO_PRESENT
                        if (GaiaUtils.HasDynamicLoadedTerrains())
                        {
                            //We got placeholders, activate terrain loading
                            stamper.m_loadTerrainMode = LoadMode.EditorSelected;
                        }
#endif
                        Selection.activeObject = stamper.gameObject;
                    }

                    break;
                case GaiaOperation.OperationType.Spawn:
                    if (editorUtils.Button("PreviewInSpawner"))
                    {
                        BiomeController bmc = null;
                        List<Spawner> spawnerList = null;
                        Selection.activeObject = GaiaSessionManager.GetOrCreateSessionSpawners(op.SpawnOperationSettings, ref bmc, ref spawnerList);
                    }

                    break;
                case GaiaOperation.OperationType.MaskMapExport:
#if GAIA_PRO_PRESENT
                    if (editorUtils.Button("PreviewInExport"))
                    {
                        MaskMapExport mme = null;
                        Selection.activeObject = GaiaSessionManager.GetOrCreateMaskMapExporter(op.ExportMaskMapOperationSettings.m_maskMapExportSettings, ref mme);
                    }
#endif
                    break;
            }
           
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw a progress bar
        /// </summary>
        /// <param name="label"></param>
        /// <param name="value"></param>
        void ProgressBar(string label, float value)
        {
            // Get a rect for the progress bar using the same margins as a textfield:
            Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
            EditorGUI.ProgressBar(rect, value, label);
            EditorGUILayout.Space();
        }

#region OLD LABEL CODE

        ///// <summary>
        ///// Get a content label - look the tooltip up if possible
        ///// </summary>
        ///// <param name="name"></param>
        ///// <returns></returns>
        //GUIContent GetLabel(string name)
        //{
        //    string tooltip = "";
        //    if (m_showTooltips && m_tooltips.TryGetValue(name, out tooltip))
        //    {
        //        return new GUIContent(name, tooltip);
        //    }
        //    else
        //    {
        //        return new GUIContent(name);
        //    }
        //}

        ///// <summary>
        ///// The tooltips
        ///// </summary>
        //static Dictionary<string, string> m_tooltips = new Dictionary<string, string>
        //{
        //    { "Sea Level", "The sea level the session will be rendered at. Changing this will also change the resource files when it is played." },
        //    { "Locked", "When activated then this stamp is locked and no further changes can be made." },
        //    { "Delete", "Delete the step." },
        //    { "Apply", "Apply the step to the relevant object, but don't execute it. Great for seeing how something was configured." },
        //    { "Play", "Apply the step and play it in the scene." },

        //    { "Flatten Terrain", "Flatten all terrains." },
        //    { "Smooth Terrain", "Smooth all terrains." },
        //    { "Clear Trees", "Clear trees on all terrains and reset all tree spawners." },
        //    { "Clear Details", "Clear details on all terrains." },

        //    { "Terrain Helper", "Show the terrain helper controls." },
        //    { "Focus Scene View", "Focus the scene view on the terrain during session Playback." },
        //    { "Play Session", "Play the session from end to end." },
        //    { "Export Resources", "Export the embedded session resources to the Assest\\Gaia Sessions\\SessionName directory." },
        //    { "Session", "The way this spawner runs. Design time : At design time only. Runtime Interval : At run time on a timed interval. Runtime Triggered Interval : At run time on a timed interval, and only when the tagged game object is closer than the trigger range from the center of the spawner." },
        //};
#endregion

    }
}