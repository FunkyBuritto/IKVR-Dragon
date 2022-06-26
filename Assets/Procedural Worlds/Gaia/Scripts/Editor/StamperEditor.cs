using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor.Experimental.TerrainAPI;
using UnityEngine.Experimental.TerrainAPI;
using PWCommon4;
using Gaia.Internal;
using UnityEditorInternal;
using System.Linq;
using System.IO;
using ProceduralWorlds.WaterSystem;
//using PWCommon;

namespace Gaia
{
    [CustomEditor(typeof(Stamper))]
    public class StamperEditor : PWEditor, IPWEditor
    {
        GUIStyle m_boxStyle;
        GUIStyle m_wrapStyle;
        GUIStyle m_operationStyle;
        Stamper m_stamper;
        DateTime m_timeSinceLastUpdate = DateTime.Now;
        bool m_startedUpdates = false;
        private bool m_showTooltips = true;
        //private EditorUtilsOLD m_editorUtils = new EditorUtilsOLD();
        private EditorUtils m_editorUtils;

        private GaiaSettings m_gaiaSettings;

        private GUIStyle m_imageMaskHeader;
        private GUIStyle m_errorMaskHeader;
        private List<Texture2D> m_tempTextureList = new List<Texture2D>();
        private bool m_fitToWorldAllowed;

        #region Stamper Settings


        [SerializeField]
        private GaiaConstants.FeatureOperation operation = GaiaConstants.FeatureOperation.RaiseHeight;
        private GaiaResource resources;
        private int smoothIterations = 0;
        private bool m_ShowErosionControls;
        private bool m_ShowAdvancedUI;
        private bool m_ShowThermalUI;
        private bool m_ShowWaterUI;
        private bool m_ShowSedimentUI;
        private bool m_ShowRiverBankUI;
        private UnityEditorInternal.ReorderableList m_masksReorderable;
        private UnityEditorInternal.ReorderableList m_autoSpawnerReorderable;
#if GAIA_PRO_PRESENT
        private UnityEditorInternal.ReorderableList m_autoMaskExportReorderable;
#endif
        private int m_maskBakingResolution = 2048;
        private string m_maskBakingPath;
        private string m_SaveAndLoadMessage;
        private MessageType m_SaveAndLoadMessageType;
        private bool m_startedTerrainChanges;
        private bool m_absoluteHeightOPSwitch;

        private int m_imageMaskIndexBeingDrawn;
        private CollisionMask[] m_collisionMaskListBeingDrawn;
        private bool m_masksExpanded = true;

        private GaiaSessionManager m_sessionManager;
        private GaiaSessionManager SessionManager
        {
            get
            {
                if (m_sessionManager == null)
                {
                    m_sessionManager = GaiaSessionManager.GetSessionManager(false);
                }
                return m_sessionManager;
            }
        }
      
#endregion;

        private void OnDestroy()
        {
            if (m_editorUtils != null)
            {
                m_editorUtils.Dispose();
            }

            //check if we opened a stamp selection window from this stamper, and if yes, close it down
            var allWindows = Resources.FindObjectsOfTypeAll<GaiaStampSelectorEditorWindow>();
            for (int i = allWindows.Length-1; i>=0;i--)
            {
                foreach (ImageMask imageMask in m_stamper.m_settings.m_imageMasks)
                {
                    if (allWindows[i].m_editedImageMask == imageMask || allWindows[i].m_editedStamperSettings == m_stamper.m_settings)
                    {
                        allWindows[i].Close();
                    }
                }
            }


            for (int i = 0; i < m_tempTextureList.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(m_tempTextureList[i]);
            }

        }

        /// <summary>
        /// Called when object selected
        /// </summary>
        void OnEnable()
        {
            if (m_gaiaSettings == null)
            {
                m_gaiaSettings = Gaia.GaiaUtils.GetGaiaSettings();
            }

            if (m_imageMaskHeader == null || m_imageMaskHeader.normal.background == null)
            {
                m_imageMaskHeader = new GUIStyle();
                m_errorMaskHeader = new GUIStyle();
                // Setup colors for Unity Pro
                if (EditorGUIUtility.isProSkin)
                {
                    m_imageMaskHeader.normal.background = GaiaUtils.GetBGTexture(GaiaUtils.GetColorFromHTML("2d2d2dff"), m_tempTextureList);
                    m_errorMaskHeader.normal.background = GaiaUtils.GetBGTexture(GaiaUtils.GetColorFromHTML("804241ff"), m_tempTextureList);
                }
                else
                {
                    m_imageMaskHeader.normal.background = GaiaUtils.GetBGTexture(GaiaUtils.GetColorFromHTML("a2a2a2ff"), m_tempTextureList);
                    m_errorMaskHeader.normal.background = GaiaUtils.GetBGTexture(GaiaUtils.GetColorFromHTML("C46564ff"), m_tempTextureList);
                }
            }

            m_stamper = (Stamper)target;

            if (m_stamper.m_settings == null)
            {
                m_stamper.m_settings = ScriptableObject.CreateInstance<StamperSettings>();
                serializedObject.ApplyModifiedProperties();
            }

            m_stamper.UpdateTerrainLoader();
            CreateMaskList();
            CreateAutoSpawnerList();
            CreateAutoMaskExportList();

            if (m_gaiaSettings != null)
            {
                m_showTooltips = m_gaiaSettings.m_showTooltips;
            }

            //Init editor utils
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            if (GaiaWater.DoesWaterExist())
            {
                m_stamper.m_showSeaLevelPlane = false;
            }

            GaiaLighting.SetPostProcessingStatus(false);

#if GAIA_PRO_PRESENT
            m_stamper.TerrainLoader.m_isSelected = true;
#endif
            m_stamper.UpdateMinMaxHeight();
            ImageMask.RefreshSpawnRuleGUIDs();
            StartEditorUpdates();
            m_stamper.m_stampDirty = true;

            if (m_stamper.m_showAutoSpawnersOnEnable)
            {
                m_editorUtils.SetPanelStatus(DrawAdvanced, true, false);
                m_editorUtils.SetPanelStatus(DrawAutoTriggers, true, false);
                m_stamper.m_showAutoSpawnersOnEnable = false;
                EditorUtility.DisplayDialog("Setting up Spawners", "You will see the 'Auto Trigger' list inside the stamper next. Please select in there which spawners you want to trigger automatically whenever you stamp by ticking the boxes.", "OK");
            }
        }

#region AutoSpawnerList

        void CreateAutoSpawnerList()
        {
            m_autoSpawnerReorderable = new UnityEditorInternal.ReorderableList(m_stamper.m_autoSpawners, typeof(BiomeSpawnerListEntry), true, true, true, true);
            m_autoSpawnerReorderable.elementHeightCallback = OnElementHeightAutoSpawnerListEntry;
            m_autoSpawnerReorderable.drawElementCallback = DrawAutoSpawnerListElement; ;
            m_autoSpawnerReorderable.drawHeaderCallback = DrawAutoSpawnerListHeader;
            m_autoSpawnerReorderable.onAddCallback = OnAddAutoSpawnerListEntry;
            m_autoSpawnerReorderable.onRemoveCallback = OnRemoveAutosSpawnerListEntry;
            m_autoSpawnerReorderable.onReorderCallback = OnReorderAutoSpawnerList;
        }

        private void OnReorderAutoSpawnerList(ReorderableList list)
        {
            //Do nothing, changing the order does not immediately affect anything in the stamper
        }

        private void OnRemoveAutosSpawnerListEntry(ReorderableList list)
        {
            m_stamper.m_autoSpawners = StamperAutoSpawnerListEditor.OnRemoveListEntry(m_stamper.m_autoSpawners, m_autoSpawnerReorderable.index);
            list.list = m_stamper.m_autoSpawners;
        }

        private void OnAddAutoSpawnerListEntry(ReorderableList list)
        {
            m_stamper.m_autoSpawners = StamperAutoSpawnerListEditor.OnAddListEntry(m_stamper.m_autoSpawners);
            list.list = m_stamper.m_autoSpawners;
        }

        private void DrawAutoSpawnerListHeader(Rect rect)
        {
            m_stamper.m_autoSpawnersToggleAll = StamperAutoSpawnerListEditor.DrawListHeader(rect, true, m_stamper.m_autoSpawnersToggleAll, m_stamper.m_autoSpawners, m_editorUtils, ref m_stamper.m_settings.m_autoSpawnerArea);
        }

        private void DrawAutoSpawnerListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            bool changeBool = false;
            StamperAutoSpawnerListEditor.DrawListElement(rect, m_stamper.m_autoSpawners[index], ref changeBool);
        }

        private float OnElementHeightAutoSpawnerListEntry(int index)
        {
            return StamperAutoSpawnerListEditor.OnElementHeight();
        }

#endregion

        void CreateAutoMaskExportList()
        {
#if GAIA_PRO_PRESENT
            m_autoMaskExportReorderable = new UnityEditorInternal.ReorderableList(m_stamper.m_autoMaskExporter, typeof(MaskMapExport), true, true, true, true);
            m_autoMaskExportReorderable.elementHeightCallback = OnElementHeightAutoMaskExportListEntry;
            m_autoMaskExportReorderable.drawElementCallback = DrawAutoMaskExportListElement; ;
            m_autoMaskExportReorderable.drawHeaderCallback = DrawAutoMaskExportListHeader;
            m_autoMaskExportReorderable.onAddCallback = OnAddAutoMaskExportListEntry;
            m_autoMaskExportReorderable.onRemoveCallback = OnRemoveAutoMaskExportListEntry;
            m_autoMaskExportReorderable.onReorderCallback = OnReorderAutoMaskExportList;
#endif
        }
#if GAIA_PRO_PRESENT
        private void OnReorderAutoMaskExportList(ReorderableList list)
        {
            //Do nothing, changing the order does not immediately affect anything in the stamper
        }

        private void OnRemoveAutoMaskExportListEntry(ReorderableList list)
        {
            m_stamper.m_autoMaskExporter = StamperAutoMaskExportListEditor.OnRemoveListEntry(m_stamper.m_autoMaskExporter, m_autoMaskExportReorderable.index);
            list.list = m_stamper.m_autoMaskExporter;
        }

        private void OnAddAutoMaskExportListEntry(ReorderableList list)
        {
            m_stamper.m_autoMaskExporter = StamperAutoMaskExportListEditor.OnAddListEntry(m_stamper.m_autoMaskExporter);
            list.list = m_stamper.m_autoMaskExporter;
        }

        private void DrawAutoMaskExportListHeader(Rect rect)
        {
            m_stamper.m_autoMaskExportersToggleAll = StamperAutoMaskExportListEditor.DrawListHeader(rect, true, m_stamper.m_autoMaskExportersToggleAll, m_stamper.m_autoMaskExporter, m_editorUtils, ref m_stamper.m_settings.m_autoMaskExportArea);
        }

        private void DrawAutoMaskExportListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            AutoMaskExport autoMaskExporter = m_stamper.m_autoMaskExporter[index];
            StamperAutoMaskExportListEditor.DrawListElement(rect, ref autoMaskExporter, m_editorUtils);
            m_stamper.m_autoMaskExporter[index] = autoMaskExporter;
            m_autoMaskExportReorderable.list[index] = autoMaskExporter;
        }

        private float OnElementHeightAutoMaskExportListEntry(int index)
        {
            return StamperAutoMaskExportListEditor.OnElementHeight();
        }
#endif


        private void CreateMaskList()
        {
            m_masksReorderable = new UnityEditorInternal.ReorderableList(m_stamper.m_settings.m_imageMasks, typeof(ImageMask), true, true, true, true);
            m_masksReorderable.elementHeightCallback = OnElementHeightStamperMaskListEntry; 
            m_masksReorderable.drawElementCallback = DrawStamperMaskListElement; ;
            m_masksReorderable.drawHeaderCallback = DrawStamperMaskListHeader;
            m_masksReorderable.onAddCallback = OnAddStamperMaskListEntry;
            m_masksReorderable.onRemoveCallback = OnRemoveStamperMaskListEntry;
            m_masksReorderable.onReorderCallback = OnReorderStamperMaskList;

            foreach (ImageMask mask in m_stamper.m_settings.m_imageMasks)
            {
                mask.m_reorderableCollisionMaskList = CreateStamperCollisionMaskList(mask.m_reorderableCollisionMaskList, mask.m_collisionMasks);
            }
        }

        private float OnElementHeightStamperMaskListEntry(int index)
        {
            if (index >= 0 && index < m_stamper.m_settings.m_imageMasks.Length)
            {
                return ImageMaskListEditor.OnElementHeight(index, m_stamper.m_settings.m_imageMasks[index]);
            }
            else
            { 
                return EditorGUIUtility.singleLineHeight * 4;
            }
        }

        private void DrawStamperMaskListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            //bool isCopiedMask = m_stamper.m_settings.m_imageMasks[index] != null && m_stamper.m_settings.m_imageMasks[index] == m_copiedImageMask;

            ImageMask copiedImageMask = SessionManager.m_copiedImageMask;
            m_imageMaskIndexBeingDrawn = index;
            MaskListButtonCommand mlbc = ImageMaskListEditor.DrawMaskListElement(rect, index, m_stamper.m_settings.m_imageMasks, ref m_collisionMaskListBeingDrawn, m_editorUtils, Terrain.activeTerrain, GaiaUtils.IsStampOperation(m_stamper.m_settings.m_operation), copiedImageMask, m_imageMaskHeader.normal.background, m_errorMaskHeader.normal.background, m_gaiaSettings, SessionManager);
            switch (mlbc)
            {
                case MaskListButtonCommand.Delete:
                        m_masksReorderable.index = index;
                        OnRemoveStamperMaskListEntry(m_masksReorderable);
                    break;
                case MaskListButtonCommand.Duplicate:
                    ImageMask newImageMask = ImageMask.Clone(m_stamper.m_settings.m_imageMasks[index]);
                        m_stamper.m_settings.m_imageMasks = GaiaUtils.InsertElementInArray(m_stamper.m_settings.m_imageMasks, newImageMask, index + 1);
                        m_masksReorderable.list = m_stamper.m_settings.m_imageMasks;
                        m_stamper.m_settings.m_imageMasks[index + 1].m_reorderableCollisionMaskList = CreateStamperCollisionMaskList(m_stamper.m_settings.m_imageMasks[index + 1].m_reorderableCollisionMaskList, m_stamper.m_settings.m_imageMasks[index + 1].m_collisionMasks);
                        serializedObject.ApplyModifiedProperties();
 
                    break;
                case MaskListButtonCommand.Copy:
                    SessionManager.m_copiedImageMask = m_stamper.m_settings.m_imageMasks[index];
                    break;
                case MaskListButtonCommand.Paste:
                    m_stamper.m_settings.m_imageMasks[index] = ImageMask.Clone(copiedImageMask);
                    //Rebuild collsion mask list with new content from the cloning
                    m_stamper.m_settings.m_imageMasks[index].m_reorderableCollisionMaskList = CreateStamperCollisionMaskList(m_stamper.m_settings.m_imageMasks[index].m_reorderableCollisionMaskList, m_stamper.m_settings.m_imageMasks[index].m_collisionMasks);
                    SessionManager.m_copiedImageMask = null;
                    break;

            }
            if (m_stamper.m_settings.m_imageMasks.Length - 1 >= index)
            {
                m_stamper.m_settings.m_imageMasks[index].m_imageMaskLocation = ImageMaskLocation.Stamper;
            }
        }

        private void DrawStamperMaskListHeader(Rect rect)
        {
            m_masksExpanded = ImageMaskListEditor.DrawFilterListHeader(rect, m_masksExpanded, m_stamper.m_settings.m_imageMasks, m_editorUtils);
        }

        private void OnAddStamperMaskListEntry(ReorderableList list)
        {
            m_stamper.m_settings.m_imageMasks = ImageMaskListEditor.OnAddMaskListEntry(m_stamper.m_settings.m_imageMasks, m_stamper.m_maxCurrentTerrainHeight, m_stamper.m_minCurrentTerrainHeight, m_stamper.m_seaLevel);
            ImageMask lastElement = m_stamper.m_settings.m_imageMasks[m_stamper.m_settings.m_imageMasks.Length - 1];
            lastElement.m_reorderableCollisionMaskList = CreateStamperCollisionMaskList(lastElement.m_reorderableCollisionMaskList, lastElement.m_collisionMasks);
            list.list = m_stamper.m_settings.m_imageMasks;
        }

        private void OnRemoveStamperMaskListEntry(ReorderableList list)
        {
            m_stamper.m_settings.m_imageMasks = ImageMaskListEditor.OnRemoveMaskListEntry(m_stamper.m_settings.m_imageMasks, list.index);
            list.list = m_stamper.m_settings.m_imageMasks;
        }

        private void OnReorderStamperMaskList(ReorderableList list)
        {
                m_stamper.m_stampDirty = true;
                m_stamper.DrawStampPreview();
        }


        /// <summary>
        /// Creates the reorderable collision mask list for collision masks in the spawner itself.
        /// </summary>
        public ReorderableList CreateStamperCollisionMaskList(ReorderableList list, CollisionMask[] collisionMasks)
        {
            list = new ReorderableList(collisionMasks, typeof(CollisionMask), true, true, true, true);
            list.elementHeightCallback = OnElementHeightCollisionMaskList;
            list.drawElementCallback = DrawStamperCollisionMaskElement;
            list.drawHeaderCallback = DrawStamperCollisionMaskListHeader;
            list.onAddCallback = OnAddStamperCollisionMaskListEntry;
            list.onRemoveCallback = OnRemoveStamperCollisionMaskMaskListEntry;
            return list;
        }

        private void OnRemoveStamperCollisionMaskMaskListEntry(ReorderableList list)
        {
            //look up the collision mask in the spawner's mask list
            foreach (ImageMask imagemask in m_stamper.m_settings.m_imageMasks)
            {
                if (imagemask.m_reorderableCollisionMaskList == list)
                {
                    imagemask.m_collisionMasks = CollisionMaskListEditor.OnRemoveMaskListEntry(imagemask.m_collisionMasks, list.index);
                    list.list = imagemask.m_collisionMasks;
                    return;
                }
            }
        }

        private void OnAddStamperCollisionMaskListEntry(ReorderableList list)
        {
            //look up the collision mask in the spawner's mask list
            foreach (ImageMask imagemask in m_stamper.m_settings.m_imageMasks)
            {
                if (imagemask.m_reorderableCollisionMaskList == list)
                {
                    imagemask.m_collisionMasks = CollisionMaskListEditor.OnAddMaskListEntry(imagemask.m_collisionMasks);
                    list.list = imagemask.m_collisionMasks;
                    return;
                }
            }
        }

        private void DrawStamperCollisionMaskListHeader(Rect rect)
        {
            foreach (ImageMask imagemask in m_stamper.m_settings.m_imageMasks)
            {
                if (imagemask.m_collisionMasks == m_collisionMaskListBeingDrawn)
                {
                    imagemask.m_collisionMaskExpanded = CollisionMaskListEditor.DrawFilterListHeader(rect, imagemask.m_collisionMaskExpanded, imagemask.m_collisionMasks, m_editorUtils);
                }
            }
        }

        private void DrawStamperCollisionMaskElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (m_collisionMaskListBeingDrawn == null)
            {
                m_collisionMaskListBeingDrawn = m_stamper.m_settings.m_imageMasks[m_imageMaskIndexBeingDrawn].m_collisionMasks;
            }

            if (m_collisionMaskListBeingDrawn != null && m_collisionMaskListBeingDrawn.Length > index && m_collisionMaskListBeingDrawn[index] != null)
            {
                CollisionMaskListEditor.DrawMaskListElement(rect, index, m_collisionMaskListBeingDrawn[index], m_editorUtils, Terrain.activeTerrain, GaiaConstants.FeatureOperation.Contrast);
            }
        }

        private float OnElementHeightCollisionMaskList(int index)
        {
            return CollisionMaskListEditor.OnElementHeight(index, m_collisionMaskListBeingDrawn);
        }



        /// <summary>
        /// Called when object deselected
        /// </summary>
        void OnDisable()
        {
            m_stamper = (Stamper)target;
            if (m_stamper != null)
            {
                if (!m_stamper.m_alwaysShow)
                {
                    m_stamper.HidePreview();
                }
            }
#if GAIA_PRO_PRESENT
            m_stamper.TerrainLoader.m_isSelected = false;
            m_stamper.UpdateTerrainLoader();
#endif
            m_stamper.m_openedFromTerrainGenerator = false;

            if (m_stamper.m_worldMapStampToken != null)
            {
                m_stamper.m_worldMapStampToken.SyncLocationFromStamperSettings();
                m_stamper.m_worldMapStampToken.UpdateGizmoPos();
            }
            m_stamper.m_settings.ClearImageMaskTextures();
            
            GaiaLighting.SetPostProcessingStatus(true);
        }


        /// <summary>
        /// Start editor updates
        /// </summary>
        public void StartEditorUpdates()
        {
            if (!m_startedUpdates)
            {
                m_startedUpdates = true;
                EditorApplication.update += EditorUpdate;
            }

            if (!m_startedTerrainChanges)
            {
                m_startedTerrainChanges = true;
                TerrainCallbacks.heightmapChanged += OnHeightmapChanged;
            }
        }

        private void OnHeightmapChanged(Terrain terrain, RectInt heightRegion, bool synched)
        {
            if (m_stamper.m_autoSpawnRequested || m_stamper.m_heightUpdateRequested)
            {
                //delay preview re-activation as well
                if (m_stamper.m_activatePreviewRequested)
                {
                    m_stamper.m_activatePreviewTimeStamp = GaiaUtils.GetUnixTimestamp();
                }
                m_stamper.m_lastHeightmapUpdateTimeStamp = GaiaUtils.GetUnixTimestamp();
            }
        }

        /// <summary>
        /// Stop editor updates
        /// </summary>
        public void StopEditorUpdates()
        {
            if (m_startedUpdates)
            {
                m_startedUpdates = false;
                EditorApplication.update -= EditorUpdate;
            }

            if (m_startedTerrainChanges)
            {
                m_startedTerrainChanges = false;
                TerrainCallbacks.heightmapChanged -= OnHeightmapChanged;
            }
        }

        /// <summary>
        /// This is used just to force the editor to repaint itself
        /// </summary>
        void EditorUpdate()
        {
            if (m_stamper != null)
            {
                if (m_stamper.m_updateCoroutine != null)
                {
                    if ((DateTime.Now - m_timeSinceLastUpdate).TotalMilliseconds > 500)
                    {
                        m_timeSinceLastUpdate = DateTime.Now;
                        Repaint();
                    }
                }
                else
                {
                    if ((DateTime.Now - m_timeSinceLastUpdate).TotalSeconds > 5)
                    {
                        m_timeSinceLastUpdate = DateTime.Now;
                        Repaint();
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            //Init editor utils
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            m_editorUtils.Initialize();
            serializedObject.Update();

            //Get our stamper
            m_stamper = (Stamper)target;

            long currentTimeStamp = GaiaUtils.GetUnixTimestamp();

            //Disable Loading settings highlighting again after 2 seconds
            if (m_stamper.m_highlightLoadingSettings && m_stamper.m_highlightLoadingSettingsStartedTimeStamp + 2000 < currentTimeStamp)
            {
                m_stamper.m_highlightLoadingSettings = false;
            }


            //Handle terrain auto-loading. Do not run while an autospawn is running
            if (!m_stamper.m_autoSpawnRequested)
            {
                //There are edge cases where OnInspectorGUI can still be executed even though a different object has just been selected.
                if (Selection.activeObject == m_stamper.gameObject)
                {
#if GAIA_PRO_PRESENT
                    m_stamper.TerrainLoader.m_isSelected = true;
#endif
                    m_stamper.UpdateTerrainLoader();
                }
            }

            //synchronize position with world map token, if any
            if (m_stamper.m_worldMapStampToken != null)
            {
                m_stamper.m_worldMapStampToken.SyncLocationFromStamperSettings();
            }

            //Do we still have outstanding spawn ot height update requests?
            if (m_stamper.m_autoSpawnRequested || m_stamper.m_autoMaskExportRequested || m_stamper.m_heightUpdateRequested)
            {
                //push re-activation of the preview forward
                m_stamper.m_activatePreviewTimeStamp = currentTimeStamp;

                //we do have, lock GUI
                GUI.enabled = false;


                //do we want to process those requests yet?
                if ((m_stamper.m_lastHeightmapUpdateTimeStamp + m_gaiaSettings.m_autoTextureTreshold) < currentTimeStamp)
                {
                    //Clear the "waiting for unity terrain updates" progress bar.
                    EditorUtility.ClearProgressBar();

                    if (m_stamper.m_heightUpdateRequested)
                    {
                        //force recalculate for the terrain we just stamped, then update our min max values
                        foreach (Terrain t in m_stamper.m_lastAffectedTerrains)
                        {
                            SessionManager.ForceTerrainMinMaxCalculation(t);
                        }
                        m_stamper.m_lastAffectedTerrains.Clear();

                        m_stamper.UpdateMinMaxHeight();
                        m_stamper.m_heightUpdateRequested = false;
                        EditorUtility.ClearProgressBar();
                    }
                   
                    if (m_stamper.m_autoSpawnRequested)
                    {
                        
                        if (!m_stamper.m_autoSpawnStarted)
                        {
                            GaiaStopwatch.StartEvent("Stamper AutoSpawning");
                            bool worldSpawn = m_stamper.m_settings.m_autoSpawnerArea == GaiaConstants.AutoSpawnerArea.World;
                            float stamperRange = m_stamper.GetStamperRange(m_stamper.GetCurrentTerrain()) / 2f;
#if GAIA_PRO_PRESENT
                            m_stamper.TerrainLoader.LoadMode = LoadMode.Disabled;
                            m_stamper.TerrainLoader.UnloadTerrains();
#endif
                            Spawner.HandleAutoSpawnerStack(m_stamper.m_autoSpawners.FindAll(x => x.isActive == true), m_stamper.transform, stamperRange, worldSpawn);
                            m_stamper.m_autoSpawnStarted = true;
                        }
                        else
                        {
                            if (m_stamper.m_autoSpawners[0].spawner.m_spawnComplete)
                            {
                                m_stamper.m_autoSpawnRequested = false;
                                //unlock GUI
                                GUI.enabled = true;
#if GAIA_PRO_PRESENT
                                m_stamper.TerrainLoader.LoadMode = m_stamper.m_loadTerrainMode;
#endif
                                GaiaStopwatch.EndEvent("Stamper AutoSpawning");
                                GaiaStopwatch.EndEvent("Stamping");
                                GaiaStopwatch.Stop();
                            }
                        }
                    }
                    else
                    {
                        GaiaStopwatch.EndEvent("Stamping");
                        GaiaStopwatch.Stop();
                    }

                    if (!m_stamper.m_autoSpawnRequested && m_stamper.m_autoMaskExportRequested)
                    {
                        m_stamper.m_autoMaskExportRequested = false;
#if GAIA_PRO_PRESENT
                        int currentExporter=0;
                        foreach (AutoMaskExport exporter in m_stamper.m_autoMaskExporter.FindAll(x=>x.isActive==true && x.maskMapExport != null))
                        {
                            bool global = false;
                            if (m_stamper.m_settings.m_autoMaskExportArea == GaiaConstants.AutoSpawnerArea.World)
                            {
                                global = true;
                            }
                            if (EditorUtility.DisplayCancelableProgressBar(m_editorUtils.GetTextValue("MaskExportProgressTitle"), String.Format(m_editorUtils.GetTextValue("MaskExportProgressText"), currentExporter, m_stamper.m_autoMaskExporter.Count()), (float)currentExporter / (float)m_stamper.m_autoMaskExporter.Count()))
                            {
                                break;
                            }

                            exporter.maskMapExport.StartExport(global);

                        currentExporter++;
                        }
                        EditorUtility.ClearProgressBar();
#endif
                    }

                }
                else
                {
                    EditorUtility.DisplayProgressBar("Stamping", "Waiting for Unity Terrain Updates to complete...", 0);
                }
            }
            //Do not reactivate the preview while the autospawn is still running, can influence spawn results
            if (!m_stamper.m_autoSpawnRequested && m_stamper.m_activatePreviewRequested && (m_stamper.m_activatePreviewTimeStamp + m_gaiaSettings.m_stamperAutoHidePreviewMilliseconds < currentTimeStamp))
            {
                m_stamper.m_activatePreviewRequested = false;
                m_stamper.m_drawPreview = true;
            }

            //Set up the box style
            if (m_boxStyle == null)
            {
                m_boxStyle = new GUIStyle(GUI.skin.box);
                m_boxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                m_boxStyle.fontStyle = FontStyle.Bold;
                m_boxStyle.alignment = TextAnchor.UpperLeft;
            }

            //Setup the wrap style
            if (m_wrapStyle == null)
            {
                m_wrapStyle = new GUIStyle(GUI.skin.label);
                m_wrapStyle.wordWrap = true;
            }

            if(m_operationStyle == null || m_operationStyle.normal.background == null)
            {
                m_operationStyle = new GUIStyle();
                m_operationStyle.overflow = new RectOffset(2, 2, 2, 2);

                // Setup colors for Unity Pro
                if (EditorGUIUtility.isProSkin)
                {
                    m_operationStyle.normal.background = GaiaUtils.GetBGTexture(GaiaUtils.GetColorFromHTML("2d2d2dff"), m_tempTextureList);
                }
                // or Unity Personal
                else
                {
                    m_operationStyle.normal.background = GaiaUtils.GetBGTexture(GaiaUtils.GetColorFromHTML("a2a2a2ff"), m_tempTextureList);
                }
            }

            //Draw the intro
            //m_editorUtils.Panel("StamperInfo", DrawStamperInfo, true);
            //m_editorUtils.GUIHeader(null,"The stamper allows you to stamp features into your terrain. Click here to see a tutorial.",false,"", "http://www.procedural-worlds.com/gaia/tutorials/stamper-introduction/");

            //Disable if spawning
            if (m_stamper.m_stampComplete != true && !m_stamper.m_cancelStamp)
            {
                GUI.enabled = false;
            }
            EditorGUI.BeginChangeCheck();
            m_editorUtils.Panel("Operation", DrawOperation, false, true, true);
            if (m_stamper.m_highlightLoadingSettings)
            {
                m_editorUtils.SetPanelStatus(DrawAdvanced, true, false);
            }
            m_editorUtils.Panel("Advanced", DrawAdvanced, false);
            DrawStamperControls(false);
            //if (m_gaiaSettings.m_workflowTipsEnabled)
            //{
            //    m_editorUtils.Panel("Workflow", DrawWorkflow, true);
            //}

     
            //Check if sea level changed
            if (m_stamper.m_seaLevel != SessionManager.GetSeaLevel(m_stamper.m_settings.m_isWorldmapStamper))
            {
                m_stamper.m_seaLevel = SessionManager.GetSeaLevel(m_stamper.m_settings.m_isWorldmapStamper);
                m_stamper.m_stampDirty = true;
            }

            //Check for changes, make undo record, make changes and let editor know we are dirty
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_stamper, "Made changes");
                EditorUtility.SetDirty(m_stamper.m_settings);

                //MArk stamp / shader for re-calculation
                m_stamper.m_stampDirty = true;

                //Do we have a stamp operation selected?
                if (GaiaUtils.IsStampOperation(operation))
                {
                    //User switched to a different operation type? Set some default settings
                    if (m_stamper.m_settings.m_operation != operation)
                    {
                        if (operation == GaiaConstants.FeatureOperation.RaiseHeight || operation == GaiaConstants.FeatureOperation.AddHeight)
                        {
                            //Move the base level to 0 to make sure the stamp is not hidden right from the start
                            m_stamper.m_settings.m_baseLevel = 0;
                            m_stamper.m_settings.m_drawStampBase = true;

                            //Move the stamp to max height to make it likely it is visible right away in the preview.
                            if (!m_absoluteHeightOPSwitch)
                            {
                                m_stamper.transform.position = new Vector3(m_stamper.transform.position.x, m_stamper.m_maxCurrentTerrainHeight, m_stamper.transform.position.z);
                            }
                        }
                        if (operation == GaiaConstants.FeatureOperation.LowerHeight || operation == GaiaConstants.FeatureOperation.SubtractHeight)
                        {
                            //Move the base level to 1 to make sure the stamp is not hidden right from the start
                            m_stamper.m_settings.m_baseLevel = 1f;
                            m_stamper.m_settings.m_drawStampBase = true;

                            //Move the stamp close to the min height terrain to make it likely the subtractive operation is visible in the preview
                            if (!m_absoluteHeightOPSwitch)
                            {
                                m_stamper.transform.position = new Vector3(m_stamper.transform.position.x, m_stamper.m_minCurrentTerrainHeight, m_stamper.transform.position.z);
                            }
                        }

                    }
                }
                else
                {
                    //no stamp operation - empty the stamp image to a white texture
                    m_stamper.EmptyStampImage();
                }


                m_stamper.m_settings.m_operation = operation;

                m_stamper.m_MaskTexturesDirty = true;
            
                m_stamper.m_smoothIterations = smoothIterations;
                Vector3Double origin = TerrainLoaderManager.Instance.GetOrigin();
                //only update these values if there is no preview drawing - 
                //otherwise the stamperEditor code and the stamper code will both alter those values inbetween frames.
                if (!m_stamper.m_drawPreview)
                {
                    m_stamper.m_settings.m_x = m_stamper.transform.position.x + origin.x;
                    m_stamper.m_settings.m_y = m_stamper.transform.position.y + origin.y;
                    m_stamper.m_settings.m_z = m_stamper.transform.position.z + origin.z;
                    m_stamper.m_settings.m_width = m_stamper.transform.localScale.x;
                    m_stamper.m_settings.m_height = m_stamper.transform.localScale.y;
                    m_stamper.m_settings.m_rotation = m_stamper.transform.rotation.y;
                }
                EditorUtility.SetDirty(m_stamper);
            }

            //m_stamper.m_currentSettings.m_clipData = new StamperSettings.ClipData[m_clipData.arraySize];
            //for (int x = 0; x < m_clipData.arraySize; x++) {
            //    m_stamper.m_currentSettings.m_clipData[x] = new StamperSettings.ClipData()
            //    {
            //        m_clip = (AudioClip)m_clipData.GetArrayElementAtIndex(x).FindPropertyRelative("m_clip").objectReferenceValue,
            //        m_volume = m_clipData.GetArrayElementAtIndex(x).FindPropertyRelative("m_volume").floatValue
            //    };
            //}
                 
            serializedObject.ApplyModifiedProperties();
            //SerializedObject propObj = new SerializedObject(serializedObject.FindProperty("m_currentSettings").objectReferenceValue);
            //propObj.ApplyModifiedProperties();


        }

        //private void DrawWorkflow(bool helpEnabled)
        //{
        //    float buttonWidth = (EditorGUIUtility.currentViewWidth - 43f) / 2f;
        //    bool currentGUIState = GUI.enabled;


        //    if (m_stamper.m_linkedBiomeController == null || !m_stamper.m_hasStamped)
        //    {
        //        EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("WorkflowNotStampedYet"), MessageType.Info);
        //        GUI.enabled = false;
        //    }
        //    else
        //    {
        //        EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("WorkflowAfterStamping"), MessageType.Info);
        //    }
        //    GUILayout.BeginHorizontal();
        //    if (GUILayout.Button(GetLabel("Select Biome"), GUILayout.Width(buttonWidth)))
        //    {
        //        Selection.activeGameObject = m_stamper.m_linkedBiomeController.gameObject;
        //        EditorGUIUtility.PingObject(Selection.activeGameObject);
        //    }
        //    GUILayout.Space(3);
        //    if (GUILayout.Button(GetLabel("Spawn Biome"), GUILayout.Width(buttonWidth)))
        //    {
        //        Selection.activeGameObject = m_stamper.m_linkedBiomeController.gameObject;
        //        m_stamper.m_linkedBiomeController.m_autoSpawnRequested = true;
        //    }
        //    GUILayout.EndHorizontal();
        //    GUI.enabled = currentGUIState;
        //    if (m_editorUtils.Button("RemoveWorkflowTips"))
        //    {
        //        if (EditorUtility.DisplayDialog("Remove Workflow Tips", "Do you want to remove the workflow tips for all Gaia Tools? You can re-activate them anytime in the Gaia Settings.", "Remove Tips", "Cancel"))
        //        {
        //            m_gaiaSettings.m_workflowTipsEnabled = false;
        //        }
        //    }
        //}

        private void DrawAdvanced(bool showHelp)
        {
            if (m_stamper.m_highlightLoadingSettings)
            {
                m_editorUtils.SetPanelStatus(DrawAppearance, true, false);
            }
            m_editorUtils.Panel("Appearance", DrawAppearance, false);
            m_editorUtils.Panel("AutoTriggers", DrawAutoTriggers, false);
            m_editorUtils.Panel("SaveSettingsAndExport", DrawSaveSettingsAndExport, false);

        }

        private void DrawSaveSettingsAndExport(bool helpEnabled)
        {
            m_editorUtils.Heading("SaveAndLoadSettings");
            if(!String.IsNullOrEmpty(m_SaveAndLoadMessage))
                EditorGUILayout.HelpBox(m_SaveAndLoadMessage, m_SaveAndLoadMessageType, true);

            EditorGUILayout.BeginHorizontal();
            if (m_editorUtils.Button("LoadButton"))
            {
                string openFilePath = EditorUtility.OpenFilePanel("Load Stamper settings..", GaiaDirectories.GetUserSettingsDirectory(), "asset");
                

                bool loadConditionsMet = true;

                //Do we have a path to begin with?
                if (openFilePath==null || openFilePath =="")
                {
                    //Silently abort in this case, the user has pressed "Abort" in the File Open Dialog
                    loadConditionsMet = false;
                }


                //Look for the Assets Directory
                if (!openFilePath.Contains("Assets") && loadConditionsMet)
                {
                    m_SaveAndLoadMessage = m_editorUtils.GetContent("LoadNoAssetDirectory").text;
                    m_SaveAndLoadMessageType = MessageType.Error;
                    loadConditionsMet = false;
                }
                if (loadConditionsMet)
                {

                    openFilePath = GaiaDirectories.GetPathStartingAtAssetsFolder(openFilePath);
                    StamperSettings settingsToLoad = (StamperSettings)AssetDatabase.LoadAssetAtPath(openFilePath, typeof(StamperSettings));

                    if (settingsToLoad != null)
                    {
                        m_stamper.LoadSettings(settingsToLoad);
                        CreateMaskList();
                        //Update the internal editor position / scale values after loading
                        m_stamper.transform.position = new Vector3((float)m_stamper.m_settings.m_x, (float)m_stamper.m_settings.m_y, (float)m_stamper.m_settings.m_z);
                        m_stamper.transform.rotation = Quaternion.Euler(0, m_stamper.m_settings.m_rotation, 0);
                        m_stamper.transform.localScale = new Vector3(m_stamper.m_settings.m_width, m_stamper.m_settings.m_height, m_stamper.m_settings.m_width); 
                        //mark stamper as dirty so it will be redrawn
                        m_stamper.m_stampDirty = true;
                        m_SaveAndLoadMessage = m_editorUtils.GetContent("LoadSuccessful").text;
                        m_SaveAndLoadMessageType = MessageType.Info;
                    }
                    else
                    {
                        m_SaveAndLoadMessage = m_editorUtils.GetContent("LoadFailed").text;
                        m_SaveAndLoadMessageType = MessageType.Error;
                    }
                }

            }
            if (m_editorUtils.Button("SaveButton"))
            {
                string saveFilePath = EditorUtility.SaveFilePanel("Save Stamper settings as..", GaiaDirectories.GetUserSettingsDirectory(), "StamperSettings", "asset");

                bool saveConditionsMet = true;

                //Do we have a path to begin with?
                if (saveFilePath == null || saveFilePath == "")
                {
                    //Silently abort in this case, the user has pressed "Abort" in the File Open Dialog
                    saveConditionsMet = false;
                }

                //Look for the Assets Directory
                if (!saveFilePath.Contains("Assets") && saveConditionsMet)
                {
                    m_SaveAndLoadMessage = m_editorUtils.GetContent("SaveNoAssetDirectory").text;
                    m_SaveAndLoadMessageType = MessageType.Error;
                    saveConditionsMet = false;
                }

                if (saveConditionsMet)
                {
                    saveFilePath = GaiaDirectories.GetPathStartingAtAssetsFolder(saveFilePath);

                    // Check if there is already an asset in this path
                    StamperSettings settingsToLoad = (StamperSettings)AssetDatabase.LoadAssetAtPath(saveFilePath, typeof(StamperSettings));

                    if (settingsToLoad != null)
                    {
                        AssetDatabase.DeleteAsset(saveFilePath);
                    }
                    
                    AssetDatabase.CreateAsset(m_stamper.m_settings, saveFilePath);
                    EditorUtility.SetDirty(m_stamper.m_settings);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(saveFilePath);

                    //Check if save was successful
                    settingsToLoad = (StamperSettings)AssetDatabase.LoadAssetAtPath(saveFilePath, typeof(StamperSettings));
                    if (settingsToLoad != null)
                    {
                        m_SaveAndLoadMessage = m_editorUtils.GetContent("SaveSuccessful").text;
                        m_SaveAndLoadMessageType = MessageType.Info;
                        //dissociate the current stamper settings from the file we just saved, otherwise the user will continue editing the file afterwards

                        m_stamper.m_settings = ScriptableObject.CreateInstance<StamperSettings>();
                        m_stamper.LoadSettings(settingsToLoad);
                        CreateMaskList();
                        m_stamper.m_stampDirty = true;

                    }
                    else
                    {
                        m_SaveAndLoadMessage = m_editorUtils.GetContent("SaveFailed").text;
                        m_SaveAndLoadMessageType = MessageType.Error;
                    }
                }

            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);

            m_editorUtils.Panel("MaskBaking", DrawMaskBaking, false);

            
        }


        private void ExportTerrain(Terrain terrain, string path)
        {
            UnityHeightMap hm = new UnityHeightMap(terrain);
            hm.Normalise();
            hm.Flip();
            path += "/" + terrain.name;
            GaiaUtils.CompressToMultiChannelFileImage(path, hm, hm, hm, null, TextureFormat.RGBAFloat, GaiaConstants.ImageFileType.Exr);
            
            string textureFileName = path + ".exr";
            GaiaUtils.SetDefaultStampImportSettings(GaiaDirectories.GetPathStartingAtAssetsFolder(textureFileName));
        }



        private void DrawUndo(bool showHelp)
        {
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();

            m_stamper.m_recordUndo = m_editorUtils.Toggle(m_stamper.m_recordUndo, "UndoRecord");

            GUIContent content = m_editorUtils.GetContent("UndoSteps");
            content.text = string.Format(content.text, m_stamper.m_currentStamperUndoOperation, m_stamper.m_stamperUndoOperations.Count > 0 ? m_stamper.m_stamperUndoOperations.Count - 1 : 0);

            bool previousGUIState = GUI.enabled;

            //Check if physical terrain array resetted
            if (m_stamper.m_stamperUndoOperations == null)
            {
                m_stamper.m_currentStamperUndoOperation = 0;
                m_stamper.m_stamperUndoOperations = new List<GaiaWorldManager>();
            }

            if (m_stamper.m_currentStamperUndoOperation > m_stamper.m_stamperUndoOperations.Count - 1)
            {
                m_stamper.m_currentStamperUndoOperation = 0;
                m_stamper.m_stamperUndoOperations = new List<GaiaWorldManager>();
            }
            else if (m_stamper.m_stamperUndoOperations[m_stamper.m_currentStamperUndoOperation].PhysicalTerrainArray == null)
            {
                m_stamper.m_currentStamperUndoOperation = 0;
                m_stamper.m_stamperUndoOperations = new List<GaiaWorldManager>();
            }

            m_editorUtils.Label(content);

            if (m_stamper.m_stamperUndoOperations.Count > 0)
            {
                EditorGUI.indentLevel++;
                if (m_stamper.m_currentStamperUndoOperation == 0)
                {
                    GUI.enabled = false;
                    if (m_editorUtils.Button("UndoBackButton"))
                    {
                    }

                    GUI.enabled = previousGUIState;
                }
                else
                {
                    if (m_editorUtils.Button("UndoBackButton"))
                    {
                        if (m_stamper.m_stamperUndoOperations[m_stamper.m_currentStamperUndoOperation - 1].UpdatePhysicalTerrainArray())
                        {
                            GaiaSessionManager.StampUndo(Terrain.activeTerrains.Select(x=>x.name).ToList(), true, m_stamper);
                        }
                    }
                }

                if (m_stamper.m_currentStamperUndoOperation == m_stamper.m_stamperUndoOperations.Count - 1)
                {
                    GUI.enabled = false;
                    if (m_editorUtils.Button("UndoForwardButton"))
                    {
                    }

                    GUI.enabled = previousGUIState;
                }
                else
                {
                    if (m_editorUtils.Button("UndoForwardButton"))
                    {
                        if (m_stamper.m_stamperUndoOperations[m_stamper.m_currentStamperUndoOperation + 1].UpdatePhysicalTerrainArray())
                        {
                            GaiaSessionManager.StampRedo(Terrain.activeTerrains.Select(x => x.name).ToList(), true, m_stamper);
                        }

                    }
                }

                EditorGUI.indentLevel--;
            }

            GUILayout.EndHorizontal();

        }



        private void DrawStamperControls(bool showHelp)
        {
            //if the stamper is used to edit the sesssion, we only show a reduced set of controls together with a warning so that the user
            //does not accidentally mistake this stamper for a regular stamper
            if (m_stamper.m_sessionEditMode)
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("SessionEditWarning"), MessageType.Warning);
                GUILayout.BeginHorizontal();
                if (m_editorUtils.Button("SessionEditSaveButton"))
                {
                    SessionManager.EditStampOperation(m_stamper.m_sessionEditOperation, m_stamper.m_settings);
                }
                if (m_editorUtils.Button("SessionEditCancelButton"))
                {
                    DestroyImmediate(m_stamper.gameObject);
                    GUIUtility.ExitGUI();
                }
                GUILayout.EndHorizontal();
                return;
            }
            DrawUndo(showHelp);
            GUILayout.BeginHorizontal();
            Color currentBGColor = GUI.backgroundColor;
            //highlight the currently active preview.
            if (m_stamper.m_drawPreview)
            {
                GUI.backgroundColor = m_gaiaSettings.m_stamperPreviewButtonColor;
            }
            if (GUILayout.Button(GetLabel("Preview")))
            {
                //as soon as the user interacts with the button, the user is in control, no need for auto activate anymore
                m_stamper.m_activatePreviewRequested = false;
                m_stamper.TogglePreview();
            }
            
            GUILayout.Space(7);
            if (m_stamper.m_autoSpawnRequested || m_stamper.m_heightUpdateRequested)
            {
                GUI.backgroundColor = currentBGColor;
                //Regardless, re-enable the spawner controls to be able to cancel
                GUI.enabled = true;
                if (GUILayout.Button(GetLabel("Cancel")))
                {
                    foreach (AutoSpawner autoSpawner in m_stamper.m_autoSpawners)
                    {
                        autoSpawner.status = AutoSpawnerStatus.Done;
                        autoSpawner.spawner.CancelSpawn();
                    }
                }
            }
            else
            {
                GUI.backgroundColor = m_gaiaSettings.GetActionButtonColor(); 
                if (GUILayout.Button(GetLabel("Stamp")))
                {
                    //Check if there are any terrains in the scene that don't use "Draw Instanced"
                    if (Terrain.activeTerrains.Where(x => x.drawInstanced == false).Count()>0)
                    {
                        if (!EditorUtility.DisplayDialog("Draw Instanced Warning", "This scene contains terrains that have the setting 'Draw Instanced' turned off. This can lead to wrong stamp results when using certain masks. Please enable 'Draw Instanced' in the terrain inspector settings tab on all terrains. ", "Continue Anyways", "Cancel"))
                        {
                            GUI.enabled = true;
                            GUILayout.EndHorizontal();
                            return;
                        }
                    }

                    //Check if there are any terrains at all before stamping
                    if (Terrain.activeTerrains.Length > 0)
                    {
                        //Stamp via session manager so it is tracked in the session
                        GaiaSessionManager.Stamp(m_stamper.m_settings, true, m_stamper);
                        EditorGUIUtility.ExitGUI();
                    }
                    else
                    {
                        Debug.LogWarning("No active terrains in the scene! The stamper can't operate without an active terrain.");
                    }
                }
                GUI.backgroundColor = currentBGColor;


            }

            GUILayout.EndHorizontal();

            bool currentGUIState = GUI.enabled;

            if (m_stamper.m_linkedBiomeController == null || !m_stamper.m_hasStamped)
            {
                GUI.enabled = false;
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(GetLabel("Spawn Biome")))
            {
#if GAIA_PRO_PRESENT
                m_stamper.TerrainLoader.m_isSelected = false;
                m_stamper.UpdateTerrainLoader();
#endif
                Selection.activeGameObject = m_stamper.m_linkedBiomeController.gameObject;
                m_stamper.m_linkedBiomeController.m_autoSpawnRequested = true;
                GUIUtility.ExitGUI();
            }
            GUILayout.EndHorizontal();
            GUI.enabled = currentGUIState;
            
        }

        private void DrawAutoTriggers(bool showHelp)
        {
            if (m_autoSpawnerReorderable != null)
            {
                Rect listRect = EditorGUILayout.GetControlRect(true, m_autoSpawnerReorderable.GetHeight());
                m_autoSpawnerReorderable.DoList(listRect);
            }
#if GAIA_PRO_PRESENT
            if (m_autoMaskExportReorderable != null)
            {
                Rect listRect = EditorGUILayout.GetControlRect(true, m_autoMaskExportReorderable.GetHeight());
                m_autoMaskExportReorderable.DoList(listRect);
            }
#endif
        }

        //    private void DrawTerrainHelper(bool showHelp)
        //    {
        //        if (GUILayout.Button(GetLabel("Show Terrain Utilities")))
        //        {
        //            var export = EditorWindow.GetWindow<GaiaTerrainExplorerEditor>(false, "Terrain Utilities");
        //export.Show();
        //        }

        //            GUILayout.BeginHorizontal();

        //        if (GUILayout.Button(GetLabel("Smooth")))
        //        {
        //            if (EditorUtility.DisplayDialog("Smooth Terrain tiles ?", "Are you sure you want to smooth all terrain tiles - this can not be undone ?", "Yes", "No"))
        //            {
        //                m_stamper.SmoothTerrain();
        //            }
        //        }
        //        GUILayout.EndHorizontal();

        //        GUILayout.BeginHorizontal();

        //        if (GUILayout.Button(GetLabel("Clear Trees")))
        //        {
        //            if (EditorUtility.DisplayDialog("Clear Terrain trees ?", "Are you sure you want to clear all terrain trees - this can not be undone ?", "Yes", "No"))
        //            {
        //                m_stamper.ClearTrees();
        //            }
        //        }
        //        if (GUILayout.Button(GetLabel("Clear Details")))
        //        {
        //            if (EditorUtility.DisplayDialog("Clear Terrain details ?", "Are you sure you want to clear all terrain details - this can not be undone ?", "Yes", "No"))
        //            {
        //                m_stamper.ClearDetails();
        //            }
        //        }
        //        GUILayout.EndHorizontal();
        //    }


        private void DrawAppearance(bool showHelp)
        {
#if GAIA_PRO_PRESENT
            bool currentGUIState = GUI.enabled;
            if (!TerrainLoaderManager.Instance.TerrainSceneStorage.m_terrainLoadingEnabled)
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("AutoLoadTerrainsDisabled"),MessageType.Warning);
                GUI.enabled = false;
            }

            Color originalColor = GUI.backgroundColor;

            if (m_stamper.m_highlightLoadingSettings)
            {
                GUI.backgroundColor = GaiaUtils.GetColorFromHTML(GaiaConstants.TerrainLoadingSettingsHighlightColor); ;
            }
            m_stamper.m_loadTerrainMode = (LoadMode)m_editorUtils.EnumPopup("AutoLoadTerrains", m_stamper.m_loadTerrainMode, showHelp);
            m_stamper.m_impostorLoadingRange = m_editorUtils.IntField("ImpostorLoadingRange", m_stamper.m_impostorLoadingRange, showHelp);
            GUI.enabled = currentGUIState;
            GUI.backgroundColor = originalColor;
#endif
            m_stamper.m_showBoundingBox = m_editorUtils.Toggle("ShowBoundingBox", m_stamper.m_showBoundingBox, showHelp);
            EditorGUILayout.Space(10);
            //Only enable the base level settings for these operation types. The base level does not add value for the other op types.
            bool baseLevelEnabled = m_stamper.m_settings.m_operation == GaiaConstants.FeatureOperation.AddHeight ||
                                    m_stamper.m_settings.m_operation == GaiaConstants.FeatureOperation.SubtractHeight ||
                                    m_stamper.m_settings.m_operation == GaiaConstants.FeatureOperation.RaiseHeight ||
                                    m_stamper.m_settings.m_operation == GaiaConstants.FeatureOperation.LowerHeight;

            GUI.enabled = baseLevelEnabled;
            m_stamper.m_settings.m_baseLevel = m_editorUtils.Slider("BaseLevel", m_stamper.m_settings.m_baseLevel, 0f, 1f, showHelp);
            m_stamper.m_settings.m_drawStampBase = m_editorUtils.Toggle("StampBase", m_stamper.m_settings.m_drawStampBase, showHelp);
            m_stamper.m_settings.m_adaptiveBase = m_editorUtils.Toggle("AdaptiveBase", m_stamper.m_settings.m_adaptiveBase, showHelp);
            m_stamper.m_showBase = m_editorUtils.Toggle("ShowBase", m_stamper.m_showBase, showHelp);
#if GAIA_PRO_PRESENT
            GUI.enabled = currentGUIState;
#endif
            EditorGUILayout.Space(10);
            m_stamper.m_showSeaLevelPlane = m_editorUtils.Toggle("ShowSeaLevelPlane", m_stamper.m_showSeaLevelPlane, showHelp);
            m_stamper.m_showSeaLevelinStampPreview = m_editorUtils.Toggle("ShowSeaLevelStampPreview", m_stamper.m_showSeaLevelinStampPreview, showHelp);

            //Color gizmoColour = EditorGUILayout.ColorField(GetLabel("Gizmo Colour"), m_stamper.m_gizmoColour);
            //alwaysShow = m_editorUtils.Toggle("AlwaysShowStamper", m_stamper.m_alwaysShow, showHelp);

            //showRulers = m_stamper.m_showRulers = m_editorUtils.Toggle("ShowRulers", m_stamper.m_showRulers , showHelp);
            //bool showTerrainHelper = m_stamper.m_showTerrainHelper = EditorGUILayout.Toggle(GetLabel("Show Terrain Helper"), m_stamper.m_showTerrainHelper);
        }

        private void DrawMaskSettings(bool showHelp)
        {
            Rect maskRect;
            if (m_masksExpanded)
            {
                if (m_masksReorderable != null)
                {
                    maskRect = EditorGUILayout.GetControlRect(true, m_masksReorderable.GetHeight());
                    m_masksReorderable.DoList(maskRect);
                }
            }
            else
            {
                int oldIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 1;
                m_masksExpanded = EditorGUILayout.Foldout(m_masksExpanded, ImageMaskListEditor.PropertyCount("MaskSettings", m_stamper.m_settings.m_imageMasks, m_editorUtils), true);
                maskRect = GUILayoutUtility.GetLastRect();
                EditorGUI.indentLevel = oldIndent;
            }
            m_editorUtils.InlineHelp("MaskSettings", showHelp);
        }

        private void DrawMaskBaking(bool showHelp)
        {
            if (m_stamper.m_cachedMaskTexture != null)
            {
                Rect previewRect = EditorGUILayout.GetControlRect();
                float size = previewRect.width - EditorGUIUtility.labelWidth;
                previewRect.x = previewRect.x + EditorGUIUtility.labelWidth;
                previewRect.width = size;
                previewRect.height = size;
                EditorGUILayout.BeginVertical();
                EditorGUI.DrawPreviewTexture(previewRect, m_stamper.m_cachedMaskTexture);
                EditorGUILayout.EndVertical();
                GUILayout.Space(Mathf.Abs(previewRect.height) *1.3f);
            }
            m_maskBakingResolution = m_editorUtils.IntField("MaskBakingResolution", m_maskBakingResolution, showHelp);
            if (m_maskBakingPath == "")
            {
                m_maskBakingPath = GaiaDirectories.GetUserStampDirectory().Replace("Assets", Application.dataPath) + "BakedStamperMask.exr";
            }

            if (m_editorUtils.Button("ExportStampButton"))
            {
                string exportStampPath = EditorUtility.SaveFilePanel("Save Setup as Stamp Image...", GaiaDirectories.GetStamperExportsDirectory(), "MyStamp", "exr");
                Terrain currentTerrain = m_stamper.GetCurrentTerrain();
                float widthfactor = currentTerrain.terrainData.size.x / 100f;
                //Extension will be added when writing the render texture to disk
                exportStampPath.Replace(".exr", "");
                ImageProcessing.BakeMaskStack(m_stamper.m_settings.m_imageMasks, currentTerrain, m_stamper.transform, m_stamper.m_settings.m_width * widthfactor, m_maskBakingResolution, exportStampPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorGUILayout.Space(10);

            m_editorUtils.Heading("ExportTerrainsAsStamps");
            m_editorUtils.InlineHelp("TerrainExportDirectory", showHelp);
            EditorGUILayout.BeginHorizontal();
            if (m_editorUtils.Button("ExportTerrain"))
            {
                //Make sure the directory exists first, then suggest it to the user for exporting into there
                GaiaDirectories.GetStamperTerrainExportsDirectory();
                string exportTerrainsPath = EditorUtility.SaveFolderPanel("Save Terrains as Stamp Image...", GaiaDirectories.GetUserStampDirectory() , GaiaDirectories.STAMPER_TERRAIN_EXPORT_DIRECTORY.Replace("/",""));
                int terrainCount = 0;
                if (GaiaUtils.HasDynamicLoadedTerrains())
                {
                    Action<Terrain> act = (t) => ExportTerrain(t, exportTerrainsPath);
                    GaiaUtils.CallFunctionOnDynamicLoadedTerrains(act, false);
                    terrainCount = TerrainLoaderManager.TerrainScenes.Count;
                }
                else
                {

                    foreach (Terrain terrain in Terrain.activeTerrains)
                    {
                        EditorUtility.DisplayProgressBar(m_editorUtils.GetTextValue("ExportTerrainProgressBarTitle"), String.Format(m_editorUtils.GetTextValue("ExportTerrainProgressBarText"), terrainCount.ToString(), Terrain.activeTerrains.Length.ToString()), (float)terrainCount / (float)Terrain.activeTerrains.Length);
                        ExportTerrain(terrain, exportTerrainsPath);
                        terrainCount++;
                    }
                    EditorUtility.ClearProgressBar();

                }
                Debug.Log(String.Format(m_editorUtils.GetTextValue("ExportTerrainLog"), terrainCount.ToString(), exportTerrainsPath));
                UnityEngine.Object exportedPath = AssetDatabase.LoadAssetAtPath(GaiaDirectories.GetPathStartingAtAssetsFolder(exportTerrainsPath), typeof(UnityEngine.Object));
                if (exportedPath != null)
                {
                    EditorGUIUtility.PingObject(exportedPath);
                }
            }
            EditorGUILayout.EndHorizontal();
        }


        private void DrawStampSettings(bool showHelp)
        {
            if (m_stamper.m_settings.m_stamperInputImageMask.ImageMaskTexture != null && !GaiaConstants.Valid16BitFormats.Contains(m_stamper.m_settings.m_stamperInputImageMask.ImageMaskTexture.format))
            {
                EditorGUILayout.HelpBox("Supplied texture is not in 16-bit color format. For optimal quality use 16+ bit color images.", MessageType.Warning);
            }
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(m_editorUtils.GetContent("StampImage"), GUILayout.Width(EditorGUIUtility.labelWidth));
            m_stamper.m_settings.m_stamperInputImageMask.ImageMaskTexture = (Texture2D)EditorGUILayout.ObjectField(m_stamper.m_settings.m_stamperInputImageMask.ImageMaskTexture, typeof(Texture2D), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
           
            if (GUILayout.Button(m_editorUtils.GetContent("MaskImageOpenStampButton"),GUILayout.Width(70)))
            {
                ImageMaskListEditor.OpenStampBrowser(m_stamper.m_settings.m_stamperInputImageMask);
            }
            GUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("StampImage", showHelp);
            GUILayout.BeginHorizontal();
            m_stamper.m_settings.m_stamperInputImageMask.m_strengthTransformCurve = m_editorUtils.CurveField("MaskStrengthTransformCurve", m_stamper.m_settings.m_stamperInputImageMask.m_strengthTransformCurve);
            if (GUILayout.Button(m_editorUtils.GetContent("MaskInvert"),GUILayout.Width(70)))
            {
                GaiaUtils.InvertAnimationCurve(ref m_stamper.m_settings.m_stamperInputImageMask.m_strengthTransformCurve);
            }
            GUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("MaskStrengthTransformCurve", showHelp);

            if (operation == GaiaConstants.FeatureOperation.AddHeight || operation == GaiaConstants.FeatureOperation.SubtractHeight)
            {

                float maxHeight = 1000;
                Terrain terrain = m_stamper.GetCurrentTerrain();
                if (terrain != null)
                {
                    maxHeight = terrain.terrainData.size.y;
                }
                EditorGUI.BeginChangeCheck();
                m_stamper.m_settings.m_absoluteHeightValue = m_editorUtils.Slider("AbsoluteHeightMeter", m_stamper.m_settings.m_absoluteHeightValue, -maxHeight, maxHeight, showHelp);
                m_absoluteHeightOPSwitch = false;
                if (EditorGUI.EndChangeCheck())
                {
                    if (m_stamper.m_settings.m_absoluteHeightValue < 0 && operation != GaiaConstants.FeatureOperation.SubtractHeight)
                    {
                        operation =  GaiaConstants.FeatureOperation.SubtractHeight;
                        m_absoluteHeightOPSwitch = true;
                    }
                    if (m_stamper.m_settings.m_absoluteHeightValue > 0 && operation != GaiaConstants.FeatureOperation.AddHeight)
                    {
                        operation =  GaiaConstants.FeatureOperation.AddHeight;
                        m_absoluteHeightOPSwitch = true;
                    }
                    m_stamper.SetStampScaleByMeter(m_stamper.m_settings.m_absoluteHeightValue);
                }
                else
                {
                    m_stamper.m_settings.m_absoluteHeightValue = m_stamper.CurrentStampScaleToMeter();
                }
            }

            
            if (operation == GaiaConstants.FeatureOperation.BlendHeight)
            {
                //Value displayed in % on the UI
                m_stamper.m_blendStrength = EditorGUILayout.Slider(GetLabel("Blend Strength %"), m_stamper.m_blendStrength * 100f, 0f, 100f) /100f;
            }
            



        }

        //private void DrawLocation(bool showHelp)
        //{
        //    //GUILayout.Label("Operation:", EditorStyles.boldLabel);
        //    bool GUIwasEnabled = GUI.enabled;
        //    m_stamper.m_activacteLocationSliders = m_editorUtils.Toggle("ActivateLocationSliders", m_stamper.m_activacteLocationSliders);
        //    if(!m_stamper.m_activacteLocationSliders)
        //    {
        //        m_stamper.m_settings.m_x = m_stamper.transform.position.x;
        //        m_stamper.m_settings.m_y = m_stamper.transform.position.y;
        //        m_stamper.m_settings.m_z = m_stamper.transform.position.z;
        //        m_stamper.m_settings.m_rotation = m_stamper.transform.rotation.y;
        //        m_stamper.m_settings.m_width = Mathf.Max(m_stamper.transform.localScale.x, m_stamper.transform.localScale.z);
        //        m_stamper.m_settings.m_height = m_stamper.transform.localScale.y;
        //        GUI.enabled = false;
        //    }


        //        x = EditorGUILayout.Slider(GetLabel("Position X"), m_stamper.m_settings.m_x, m_minX, m_maxX);
        //        y = m_stamper.m_settings.m_y;
        //        y = EditorGUILayout.Slider(GetLabel("Position Y"), m_stamper.m_settings.m_y, m_minY, m_maxY);
        //        z = EditorGUILayout.Slider(GetLabel("Position Z"), m_stamper.m_settings.m_z, m_minZ, m_maxZ);
        //        rotation = EditorGUILayout.Slider(GetLabel("Rotation"), m_stamper.m_settings.m_rotation, -180f, 180f);
        //        BoundsDouble bounds = new BoundsDouble();
        //        Gaia.TerrainHelper.GetTerrainBounds(ref bounds);
        //        width = EditorGUILayout.Slider(GetLabel("Width"), m_stamper.m_settings.m_width, 0.1f, (float)Mathd.Max(bounds.size.x, bounds.size.z));
        //        height = EditorGUILayout.Slider(GetLabel("Height"), m_stamper.m_settings.m_height, 0.1f, 100f);

        //    GUI.enabled = GUIwasEnabled;

        //}

        private void DrawOperation(bool showHelp)
        {

            // m_editorUtils.LabelField("SeaLevel", new GUIContent(SessionManager.GetSeaLevel(m_stamper.m_settings.m_isWorldmapStamper).ToString() + " m"), showHelp);
            EditorGUILayout.BeginVertical(m_operationStyle);
            int selectedIndex = EditorGUILayout.Popup(GetLabel("Operation Type"), GaiaConstants.FeatureOperationNames.Select((x, i) => new { item = x, index = i }).First(x => x.item.Value == (int)m_stamper.m_settings.m_operation).index, GaiaConstants.FeatureOperationNames.Select(x => x.Key).ToArray());
            //The "FeatureOperationNames" are just an array of strings to get a multi-level popup going. To get the actual operation enum
            //we need to select the enum element at the same index as the name array.
            int selectedValue = -99;
            GaiaConstants.FeatureOperationNames.TryGetValue(GaiaConstants.FeatureOperationNames.Select(x => x.Key).ToArray()[selectedIndex], out selectedValue);
            operation = (GaiaConstants.FeatureOperation)selectedValue;



            //Drawing the "special" controls for the respective operation type.
            switch (m_stamper.m_settings.m_operation)
            {
                case GaiaConstants.FeatureOperation.RaiseHeight:
                    m_editorUtils.InlineHelp("RaiseHeightIntro", showHelp);
                    DrawStampSettings(showHelp);
                    break;
                case GaiaConstants.FeatureOperation.LowerHeight:
                    m_editorUtils.InlineHelp("LowerHeightIntro", showHelp);

                    DrawStampSettings(showHelp);
                    break;
                case GaiaConstants.FeatureOperation.BlendHeight:
                    m_editorUtils.InlineHelp("BlendHeightIntro", showHelp);
                    DrawStampSettings(showHelp);
                    break;
                case GaiaConstants.FeatureOperation.SetHeight:
                    m_editorUtils.InlineHelp("SetHeightIntro", showHelp);
                    DrawStampSettings(showHelp);
                    break;
                case GaiaConstants.FeatureOperation.AddHeight:
                    m_editorUtils.InlineHelp("AddHeightIntro", showHelp);
                    DrawStampSettings(showHelp);
                    break;
                case GaiaConstants.FeatureOperation.SubtractHeight:
                    m_editorUtils.InlineHelp("SubtractHeightIntro", showHelp);
                    DrawStampSettings(showHelp);
                    break;
                case GaiaConstants.FeatureOperation.HydraulicErosion:
                    m_editorUtils.InlineHelp("HydraulicErosionIntro", showHelp);
                    DrawHydraulicErosionControls(showHelp);
                    break;
                case GaiaConstants.FeatureOperation.Contrast:
                    m_editorUtils.InlineHelp("ContrastIntro", showHelp);
                    DrawContrastControls(showHelp);
                    break;
                case GaiaConstants.FeatureOperation.Terrace:
                    m_editorUtils.InlineHelp("TerraceIntro", showHelp);
                    DrawTerraceControls(showHelp);
                    break;
                case GaiaConstants.FeatureOperation.SharpenRidges:
                    m_editorUtils.InlineHelp("SharpenRidgesIntro", showHelp);
                    DrawSharpenRidgesControls(showHelp);
                    break;
                case GaiaConstants.FeatureOperation.HeightTransform:
                    m_editorUtils.InlineHelp("HeightTransformIntro", showHelp);
                    DrawHeightTransformControls(showHelp);
                    break;
                case GaiaConstants.FeatureOperation.PowerOf:
                    m_editorUtils.InlineHelp("PowerOfIntro", showHelp);
                    DrawPowerOfControls(showHelp);
                    break;
                case GaiaConstants.FeatureOperation.Smooth:
                    m_editorUtils.InlineHelp("SmoothIntro", showHelp);
                    DrawSmoothControls(showHelp);
                    break;
                case GaiaConstants.FeatureOperation.MixHeight:
                    m_editorUtils.InlineHelp("MixHeightIntro", showHelp);
                    DrawMixHeightControls(showHelp);
                    break;
                default:
                    DrawStampSettings(showHelp);
                    //m_editorUtils.Panel("StampSettings", DrawStampSettings, true);
                    break;
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            float maxSeaLevel = 2000f;
            if (m_stamper.GetCurrentTerrain() != null)
            {
                Terrain terrain = m_stamper.GetCurrentTerrain();
                maxSeaLevel = terrain.transform.position.y + terrain.terrainData.size.y;
            }
            else
            {
                maxSeaLevel = SessionManager.GetSeaLevel(m_stamper.m_settings.m_isWorldmapStamper) + 500f;
            }
            float newSeaLEvel = m_editorUtils.Slider("SeaLevel", SessionManager.GetSeaLevel(m_stamper.m_settings.m_isWorldmapStamper), 0, maxSeaLevel, showHelp);
            if (newSeaLEvel != SessionManager.GetSeaLevel(m_stamper.m_settings.m_isWorldmapStamper))
            {
                //Do we have a water instance? If yes, update it & it will update the sea level in the session as well
                if (PWS_WaterSystem.Instance != null)
                {
                    PWS_WaterSystem.Instance.SeaLevel = newSeaLEvel;
                }
                else
                {
                    //no water instance yet, just update the sea level in the session
                    SessionManager.SetSeaLevel(newSeaLEvel, m_stamper.m_settings.m_isWorldmapStamper);
                }
                m_stamper.UpdateMinMaxHeight();
            }
            GUILayout.Space(10);
            DrawMaskSettings(showHelp);
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            float buttonWidth = (EditorGUIUtility.currentViewWidth - 58f) / 2f;
            if (GUILayout.Button(GetLabel("Flatten"), GUILayout.Width(buttonWidth)))
            {
                if (EditorUtility.DisplayDialog("Flatten Terrain tiles ?", "Are you sure you want to flatten terrain tiles - this can not be undone ?", "Yes", "No"))
                {
                    List<String> terrainNames = new List<string>();

                    if (GaiaUtils.HasDynamicLoadedTerrains())
                    {
                        if (EditorUtility.DisplayDialog("Flatten Unloaded Terrains?", "The current scene uses dynamically loaded terrains - Do you want to flatten the entire world or only the currenlty loaded terrains?", "Entire World", "Only Current Terrains"))
                        {
                            foreach (TerrainScene ts in TerrainLoaderManager.TerrainScenes)
                            {
                                terrainNames.Add(ts.GetTerrainName());
                            }
                        }
                        else
                        {
                            terrainNames = Terrain.activeTerrains.Select(x => x.name).ToList();
                        }
                    }
                    else
                    {
                        terrainNames = Terrain.activeTerrains.Select(x => x.name).ToList();
                    }

                    GaiaSessionManager.FlattenTerrain(terrainNames, true);

                    //m_stamper.FlattenTerrain();
                    //m_stamper.m_stamperUndoOperations.Clear();
                    //m_stamper.m_currentStamperUndoOperation=0;
                }
            }
            GUILayout.Space(5);
            if (GUILayout.Button(GetLabel("Ground"), GUILayout.Width(buttonWidth)))
            {
                m_stamper.AlignToGround();
                m_stamper.UpdateStamp();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(GetLabel("Fit To Terrain"), GUILayout.Width(buttonWidth)))
            {
                m_stamper.FitToTerrain();
                m_stamper.UpdateStamp();
            }
            GUILayout.Space(5);

            bool currentGUIState = GUI.enabled;
            //Check if fit to world is possible with the current resolution etc. settings
            float requiredFitToWorldWidth = 100 * Mathf.Max(TerrainLoaderManager.Instance.TerrainSceneStorage.m_terrainTilesX, TerrainLoaderManager.Instance.TerrainSceneStorage.m_terrainTilesZ);
            if (requiredFitToWorldWidth > m_stamper.GetMaxStamperRange(m_stamper.GetCurrentTerrain()))
            {
                m_fitToWorldAllowed = false;
            }
            else
            {
                m_fitToWorldAllowed = true;
            }


            if (!m_fitToWorldAllowed)
            {
                GUI.enabled = false;
                GUILayout.Button(GetLabel("Fit To World (Inactive)"), GUILayout.Width(buttonWidth));
                GUI.enabled = currentGUIState;
            }
            else
            {
                if (GUILayout.Button(GetLabel("Fit To World"), GUILayout.Width(buttonWidth)))
                {
                    m_stamper.FitToAllTerrains();
                    m_stamper.UpdateStamp();
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawSmoothControls(bool showHelp)
        {
            m_stamper.m_settings.m_smoothVerticality = EditorGUILayout.Slider(m_editorUtils.GetContent("MaskSmoothVerticality"), m_stamper.m_settings.m_smoothVerticality, -1f, 1f);
            m_stamper.m_settings.m_smoothBlurRadius = EditorGUILayout.Slider(m_editorUtils.GetContent("MaskSmoothBlurRadius"), m_stamper.m_settings.m_smoothBlurRadius, 0f, 30f);
        }

        private void DrawMixHeightControls(bool showHelp)
        {
            if (m_stamper.m_settings.m_stamperInputImageMask.ImageMaskTexture != null && !GaiaConstants.Valid16BitFormats.Contains(m_stamper.m_settings.m_stamperInputImageMask.ImageMaskTexture.format))
            {
                EditorGUILayout.HelpBox("Supplied texture is not in 16-bit color format. For optimal quality use 16+ bit color images.", MessageType.Warning);
            }
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(m_editorUtils.GetContent("StampImage"), GUILayout.Width(EditorGUIUtility.labelWidth));
            m_stamper.m_settings.m_stamperInputImageMask.ImageMaskTexture = (Texture2D)EditorGUILayout.ObjectField(m_stamper.m_settings.m_stamperInputImageMask.ImageMaskTexture, typeof(Texture2D), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (GUILayout.Button(m_editorUtils.GetContent("MaskImageOpenStampButton"),GUILayout.Width(70)))
            {
                ImageMaskListEditor.OpenStampBrowser(m_stamper.m_settings.m_stamperInputImageMask);
            }
            GUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("StampImage", showHelp);
            GUILayout.BeginHorizontal();
            m_stamper.m_settings.m_stamperInputImageMask.m_strengthTransformCurve = m_editorUtils.CurveField("MaskStrengthTransformCurve", m_stamper.m_settings.m_stamperInputImageMask.m_strengthTransformCurve);
            if (GUILayout.Button(m_editorUtils.GetContent("MaskInvert"), GUILayout.Width(70)))
            {
                GaiaUtils.InvertAnimationCurve(ref m_stamper.m_settings.m_stamperInputImageMask.m_strengthTransformCurve);
            }
            GUILayout.EndHorizontal();
            if (operation == GaiaConstants.FeatureOperation.BlendHeight)
            {
                m_stamper.m_blendStrength = EditorGUILayout.Slider(GetLabel("Blend Strength %"), m_stamper.m_blendStrength, 0f, 1f);
            }
            m_stamper.m_settings.m_mixMidPoint = m_editorUtils.Slider("MixMidPoint", m_stamper.m_settings.m_mixMidPoint, 0f, 1f, showHelp);
            m_stamper.m_settings.m_mixHeightStrength = m_editorUtils.Slider("MixHeightStrength", m_stamper.m_settings.m_mixHeightStrength * 100f, 0f, 200f, showHelp) /100f;
        }

        private void DrawStamperInfo(bool showHelp)
        {
            string message = m_editorUtils.GetTextValue("StamperIntro"); ;

            EditorGUILayout.HelpBox(message, MessageType.Info, true);
        }

        private void DrawPowerOfControls(bool showHelp)
        {
            m_stamper.m_settings.m_powerOf = EditorGUILayout.Slider("PowerOf", m_stamper.m_settings.m_powerOf, 0.01f, 5.0f);
        }

        private void DrawContrastControls(bool showHelp)
        {
#if GAIA_PRO_PRESENT
            m_stamper.m_settings.m_contrastFeatureSize = EditorGUILayout.Slider("Feature Size",m_stamper.m_settings.m_contrastFeatureSize, 1.0f, 100.0f);
            m_stamper.m_settings.m_contrastStrength = EditorGUILayout.Slider("Strength", m_stamper.m_settings.m_contrastStrength, 0.01f, 10.0f);
#else
            EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("GaiaProOperationInfo"), MessageType.Warning);
            GUI.enabled = false;
#endif
        }

        private void DrawHeightTransformControls(bool showHelp)
        {
            m_stamper.m_settings.m_heightTransformCurve = m_editorUtils.CurveField("MaskHeightTransformCurve", m_stamper.m_settings.m_heightTransformCurve);
        }

        private void DrawTerraceControls(bool helpEnabled)
        {
#if GAIA_PRO_PRESENT
            m_stamper.m_settings.m_terraceCount = EditorGUILayout.Slider("Terrace Count", m_stamper.m_settings.m_terraceCount, 2.0f, 1000.0f);
            //m_stamper.m_terraceJitterCount = EditorGUILayout.Slider("Jitter Amount", m_stamper.m_terraceJitterCount, 0.0f, 1.0f);
            m_stamper.m_settings.m_terraceBevelAmountInterior = EditorGUILayout.Slider("Bevel Amount", m_stamper.m_settings.m_terraceBevelAmountInterior, 0.0f, 1.0f);
#else
            EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("GaiaProOperationInfo"), MessageType.Warning);
            GUI.enabled = false;
#endif
        }

        private void DrawSharpenRidgesControls(bool helpEnabled)
        {
#if GAIA_PRO_PRESENT
            m_stamper.m_settings.m_sharpenRidgesMixStrength = EditorGUILayout.Slider("Sharpness", m_stamper.m_settings.m_sharpenRidgesMixStrength, 0, 1);
            m_stamper.m_settings.m_sharpenRidgesIterations = EditorGUILayout.Slider("Iterations", m_stamper.m_settings.m_sharpenRidgesIterations, 0, 20);
#else
            EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("GaiaProOperationInfo"), MessageType.Warning);
            GUI.enabled = false;
#endif
        }

        private void DrawHydraulicErosionControls(bool helpEnabled)
        {
#if GAIA_PRO_PRESENT
            m_stamper.m_settings.m_erosionSimScale = m_editorUtils.Slider("ErosionSimScale", m_stamper.m_settings.m_erosionSimScale, 0.0f, 100f, helpEnabled);
            m_stamper.m_settings.m_erosionHydroTimeDelta = m_editorUtils.Slider("ErosionHydroTimeDelta", m_stamper.m_settings.m_erosionHydroTimeDelta, 0.0f, 0.1f, helpEnabled);
            m_stamper.m_settings.m_erosionHydroIterations = m_editorUtils.IntSlider("ErosionHydroIterations", m_stamper.m_settings.m_erosionHydroIterations, 1, 500, helpEnabled);
            EditorGUI.indentLevel++;
            m_ShowAdvancedUI = EditorGUILayout.Foldout(m_ShowAdvancedUI, "Advanced");

            if (m_ShowAdvancedUI)
            {
                //m_ErosionSettings.m_IterationBlendScalar.DrawInspectorGUI();
               //m_ErosionSettings.m_GravitationalConstant = EditorGUILayout.Slider(ErosionStyles.m_GravitationConstant, m_ErosionSettings.m_GravitationalConstant, 0.0f, -100.0f);

                EditorGUI.indentLevel++;
                m_ShowThermalUI = EditorGUILayout.Foldout(m_ShowThermalUI, "Thermal Smoothing");
                if (m_ShowThermalUI)
                {
                    //m_ErosionSettings.m_DoThermal = EditorGUILayout.Toggle(ErosionStyles.m_DoThermal, m_ErosionSettings.m_DoThermal);
                    m_stamper.m_settings.m_erosionThermalTimeDelta = m_editorUtils.Slider("ErosionThermalTimeDelta", m_stamper.m_settings.m_erosionThermalTimeDelta, 0, 0.01f, helpEnabled);
                    m_stamper.m_settings.m_erosionThermalIterations = m_editorUtils.IntSlider("ErosionThermalIterations", m_stamper.m_settings.m_erosionThermalIterations, 0, 100, helpEnabled);
                    m_stamper.m_settings.m_erosionThermalReposeAngle = m_editorUtils.IntSlider("ErosionThermalReposeAngle", m_stamper.m_settings.m_erosionThermalReposeAngle, 0, 90, helpEnabled);
                }

                m_ShowWaterUI = EditorGUILayout.Foldout(m_ShowWaterUI, "Water Transport");
                if (m_ShowWaterUI)
                {
                    //m_ErosionSettings.m_WaterLevelScale = EditorGUILayout.Slider(ErosionStyles.m_WaterLevelScale, m_ErosionSettings.m_WaterLevelScale, 0.0f, 100.0f);
                    m_stamper.m_settings.m_erosionPrecipRate = m_editorUtils.Slider("ErosionPrecipRate", m_stamper.m_settings.m_erosionPrecipRate, 0f, 1f, helpEnabled);
                    m_stamper.m_settings.m_erosionEvaporationRate = m_editorUtils.Slider("ErosionEvaporationRate", m_stamper.m_settings.m_erosionEvaporationRate, 0f, 1f, helpEnabled);
                    m_stamper.m_settings.m_erosionFlowRate = m_editorUtils.Slider("ErosionFlowRate", m_stamper.m_settings.m_erosionFlowRate, 0f, 1f, helpEnabled);
                }

                m_ShowSedimentUI = EditorGUILayout.Foldout(m_ShowSedimentUI, "Sediment Transport");
                if (m_ShowSedimentUI)
                {
                    //m_ErosionSettings.m_SedimentScale = EditorGUILayout.Slider(ErosionStyles.m_SedimentScale, m_ErosionSettings.m_SedimentScale, 0.0f, 10.0f);
                    m_stamper.m_settings.m_erosionSedimentCapacity = m_editorUtils.Slider("ErosionSedimentCapacity", m_stamper.m_settings.m_erosionSedimentCapacity, 0f, 1f, helpEnabled);
                    m_stamper.m_settings.m_erosionSedimentDepositRate = m_editorUtils.Slider("ErosionSedimentDepositRate", m_stamper.m_settings.m_erosionSedimentDepositRate, 0f, 1f, helpEnabled);
                    m_stamper.m_settings.m_erosionSedimentDissolveRate = m_editorUtils.Slider("ErosionSedimentDissolveRate", m_stamper.m_settings.m_erosionSedimentDissolveRate, 0f, 1f, helpEnabled);
                }

                m_ShowRiverBankUI = EditorGUILayout.Foldout(m_ShowRiverBankUI, "Riverbank");
                if (m_ShowRiverBankUI)
                {
                    m_stamper.m_settings.m_erosionRiverBankDepositRate = m_editorUtils.Slider("ErosionRiverBankDepositRate", m_stamper.m_settings.m_erosionRiverBankDepositRate, 0f, 10f, helpEnabled);
                    m_stamper.m_settings.m_erosionRiverBankDissolveRate = m_editorUtils.Slider("ErosionRiverBankDissolveRate", m_stamper.m_settings.m_erosionRiverBankDissolveRate, 0f, 10f, helpEnabled);
                    m_stamper.m_settings.m_erosionRiverBedDepositRate = m_editorUtils.Slider("ErosionRiverBedDepositRate", m_stamper.m_settings.m_erosionRiverBedDepositRate, 0f, 10f, helpEnabled);
                    m_stamper.m_settings.m_erosionRiverBedDissolveRate = m_editorUtils.Slider("ErosionRiverBedDissolveRate", m_stamper.m_settings.m_erosionRiverBedDissolveRate, 0f, 10f, helpEnabled);
                }
            }
#else
            EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("GaiaProOperationInfo"), MessageType.Warning);
            GUI.enabled = false;
#endif
        }


        private Vector2 ConvertPositonToTerrainUV(Terrain terrain, Vector2 worldSpacePosition)
        {
            float u = (worldSpacePosition.x - terrain.transform.position.x) / terrain.terrainData.size.x; 
            float v = (worldSpacePosition.y - terrain.transform.position.z) / terrain.terrainData.size.z;
            return new Vector2(u,v);
        }

        private void OnSceneGUI()
        {
            if (m_stamper.m_openedFromTerrainGenerator)
            {
                Handles.BeginGUI();

                if (GUI.Button(new Rect(Screen.width - 275, Screen.height - 75, 250, 25), "Return to Random Generator"))
                {
                    m_stamper.m_worldMapStampToken.SyncLocationFromStamperSettings();
                    m_stamper.m_worldMapStampToken.UpdateGizmoPos();
                    m_stamper.m_openedFromTerrainGenerator = false;
                    TerrainLoaderManager.Instance.SwitchToWorldMap();
                    GameObject worldmapObject = GaiaUtils.GetOrCreateWorldDesigner();
                    Selection.activeObject = worldmapObject;
                }
                Handles.EndGUI();
            }
            // dont render preview if this isnt a repaint. losing performance if we do
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

#if (!GAIA_PRO_PRESENT)
            if (operation == GaiaConstants.FeatureOperation.Contrast
            || operation == GaiaConstants.FeatureOperation.SharpenRidges
            || operation == GaiaConstants.FeatureOperation.Terrace
            || operation == GaiaConstants.FeatureOperation.HydraulicErosion)
            {
                return;
            }
#endif

            m_stamper.DrawStampPreview();

            if (m_stamper.m_worldMapStampToken != null)
            {
                if (!m_stamper.m_settings.m_isWorldmapStamper && m_stamper.m_worldMapStampToken.m_previewOnWorldMap)
                {
                    m_stamper.m_worldMapStampToken.DrawWorldMapPreview();
                }
            }
        }


        /// <summary>
        /// Display a progress bar
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

        /// <summary>
        /// Get a content label - look the tooltip up if possible
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        GUIContent GetLabel(string name)
        {
            string tooltip = "";
            if (m_showTooltips && m_tooltips.TryGetValue(name, out tooltip))
            {
                return new GUIContent(name, tooltip);
            }
            else
            {
                return new GUIContent(name);
            }
        }


        /// <summary>
        /// The tooltips
        /// </summary>
        static Dictionary<string, string> m_tooltips = new Dictionary<string,string>
        {
            { "Stamp Preview", "Preview texture for the feature being stamped. Drag and drop preview textures here." },
            { "Transform Height", "Pre-process and modify the stamp height. Can be used to further refine stamp shapes."},
            { "Invert Stamp", "Invert the stamp. Good for carving lakes and valleys."},
            { "Base Level", "Base level of the stamp."},
            { "Ground Base", "Sticks the stamp base level to the terrain base."},
            { "Stamp Base", "Applies stamp below base level, or not. Good for excluding low lying areas from the stamp."},
            { "Show Base", "Shows the base level as a yellow plane."},
            { "Show Bounding Box", "Show a box around the stamp, useful to better visualize the position of the stamp."},
            { "Normalise Stamp", "Modify stamp heights to use full height range. Essential for correct height settings when using Stencil Heights operation."},
            { "Operation Type", "The way this stamp will be applied to the terrain.\nRaise - Adds stamp to terrain if stamp height greater than terrain height.\nLower - Cuts stamp from terrain if stamp height lower than terrain height. \nBlend - Blend between terrain and stamp.\nDifference - Calculate height difference.\nStencil - Adjust by stencil height - normalise first."},
            { "Blend Strength %", "Blend between terrain and stamp. 0 % - all terrain - 100 % - all stamp."},
            { "Stencil Height", "Adjusted height in meters that a normalised stamp will be applied to the terrain."},
            { "Distance Mask", "Masks the effect of the stamp over distance from center. Left hand side of curve is centre of stamp, right hand side of curve is outer edge of stamp. Set right hand side to zero to blend edges of stamp into existing terrain."},
            { "Area Mask", "Masks the effect of the stamp using the strength of the texture or noise function provided. A value of 1 means apply full effect, a value of 0 means apply no effect. Visually this is much the same ways as a greyscale image mask. If using a terrain texture, then paint on the terrain with the selected texture, and the painted area will be used as the mask."},
            
            { "Noise Seed", "The seed value for the noise function - the same seed will always generate the same noise for a given set of parameters."},
            { "Octaves", "The amount of detail in the noise - more octaves mean more detail and longer calculation time."},
            { "Persistence", "The roughness of the noise. Controls how quickly amplitudes diminish for successive octaves. 0..1."},
            { "Frequency", "The frequency of the first octave."},
            { "Lacunarity", "The frequency multiplier between successive octaves. Experiment between 1.5 - 3.5."},
            { "Zoom", "The zoom level of the noise. Larger zooms display the noise over larger areas."},

            { "Image Mask", "The image to use as the area mask."},
            { "Invert Mask", "Invert the image used as the area mask before using it."},
            { "Smooth Mask", "Smooth the mask before applying it. This is a nice way to clean noise up in the mask, or to soften the edges of the mask."},
            { "Normalise Mask", "Normalise the mask before applying it. Ensures that the full dynamic range of the mask is used."},
            { "Flip Mask", "Flip the mask on its x and y axis mask before applying it. Useful sometimes to match the unity terrain as this is flipped internally."},
            { "Seed", "The unique seed for this spawner. This will cause all subseqent spawns to exactly match this spawn" },
            { "Smooth Stamp", "Smooth the stamp before applying it to the terrain. Good for cleaning up noisy stamps."},
            { "Preview Material", "The material used to display the Preview mesh. Has no effect other than to make the preview viewable."},
            { "Resources", "The terrains rsources file. Changing sea level will update that resource files sea level, and this will impact where the spawners spawn."},
            { "Texture Spawner", "The texure spawner. An optional feature that enables the spawn button. Enables you to do a texure spawn and saves you from having to select the texture spawner manually."},
            { "Position X", "X location of stamp centre." },
            { "Position Y", "Y location of stamp centre." },
            { "Position Z", "Z location of stamp centre." },
            { "Width", "Modify the width of the stamp." },
            { "Height", "Modify the height of this stamp." },
            { "Rotation", "Modify the rotation of this stamp." },
            { "Stick To Groud", "Stick the stamp to the base of the terrain." },
            { "Always Show Stamper", "Always show the stamper, even when something else is selected, otherwise hide it when something else is selected." },
            { "Gizmo Colour", "The colour of the gizmo that is drawn to show the size of the stamp, used to make positioning easier." },
            { "Sea Level", "The sea level in meters. Changes to this are applied back to the resources file, and then impact the spawners, so treat with care, and only change before spawning." },
            { "Show Sea Level", "Show sea level." },
            { "Show Rulers", "Show rulers." },
            { "Show Terrain Helper", "Show the terrain helper buttons - treat these with care!" },
            { "Flatten", "Flatten all terrains." },
            { "Smooth", "Smooth all terrains." },
            { "Clear Trees", "Clear trees on all terrains." },
            { "Clear Details", "Clear details on all terrains." },
            { "Ground", "Align the stamp to the base of the terrain." },
            { "Fit To Terrain", "Fit the stamp to the terrain." },
            { "Fit To World", "Fit the stamp across the world size." },
            { "Fit To World (Inactive)", "Your world size is too large to fit the stamper across it - the fit to world function is disabled." },
            { "Spawn Biome", "Spawn the biome that was selected upon world creation in the Gaia Manager. Terrain needs to be stamped first for this button to become active." },
            { "Select Biome", "Select the biome controller that was created during world creation from the Gaia Manager." },
            { "Stamp", "Apply this stamp to the terrain." },
            { "Preview", "Show or hide the stamp preview mesh." },
            { "Undo", "Undo the last stamp." },
            { "Redo", "Redo the last stamp." },
        };

    }
}