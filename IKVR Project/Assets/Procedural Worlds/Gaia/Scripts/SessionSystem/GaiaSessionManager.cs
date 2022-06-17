using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Experimental.TerrainAPI;
using System.Text;
using System.Linq;
using static Gaia.GaiaConstants;
using ProceduralWorlds.WaterSystem;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.Scripting;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace Gaia
{

    /// <summary>
    /// Data structure to read min max info from a terrain in a compute shader
    /// </summary>
    struct TerrainMinMax
    {
        public float minHeight;
        public float maxHeight;
    };

    /// <summary>
    /// Gaia scene tecording and playback system
    /// </summary>
    [ExecuteInEditMode]
    public class GaiaSessionManager : MonoBehaviour
    {
        /// <summary>
        /// Use for co-routine simulation
        /// </summary>
        public IEnumerator m_updateSessionCoroutine;

        /// <summary>
        /// Use for co-routine simulation
        /// </summary>
        public IEnumerator m_updateOperationCoroutine;

        /// <summary>
        /// Used to signal a cancelled playback
        /// </summary>
        private bool m_cancelPlayback = false;

        /// <summary>
        /// The session we are managing
        /// </summary>
        public GaiaSession m_session;

        /// <summary>
        /// Controls if the scene view will be focused on the terrain when playing back a session.
        /// </summary>
        public bool m_focusSceneView = true;


        public bool m_useRandomSeed = true;

        public int m_randomSeed;

        /// <summary>
        /// Public variables used by the terrain generator
        /// </summary>
        public bool m_genShowRandomGenerator = false;
        public bool m_genShowTerrainHelper = false;
        public Gaia.GaiaConstants.GeneratorBorderStyle m_genBorderStyle = Gaia.GaiaConstants.GeneratorBorderStyle.Water;
        public int m_genGridSize = 3;
        public int m_genNumStampsToGenerate = 10;
        public float m_genScaleWidth = 60f;
        public float m_genScaleHeight = 15f;
        public float m_genChanceOfHills = 0.7f;
        public float m_genChanceOfIslands = 0f;
        public float m_genChanceOfLakes = 0f;
        public float m_genChanceOfMesas = 0.1f;
        public float m_genChanceOfMountains = 0.1f;
        public float m_genChanceOfPlains = 0f;
        public float m_genChanceOfRivers = 0.1f; //
        public float m_genChanceOfValleys = 0f;
        public float m_genChanceOfVillages = 0f; //
        public float m_genChanceOfWaterfalls = 0f; //

        [NonSerialized]
        public Stamper m_currentStamper = null;

        [NonSerialized]
        public Spawner m_currentSpawner = null;

        [NonSerialized]
        public DateTime m_lastUpdateDateTime = DateTime.Now;

        [NonSerialized]
        public ulong m_progress = 0;

        public BakedMaskCache m_bakedMaskCache = new BakedMaskCache();

        /// <summary>
        /// The settings which were used last when exporting terrains to meshes.
        /// </summary>
        [SerializeField]
        public ExportTerrainSettings m_lastUsedTerrainExportSettings;

        /// <summary>
        /// Private vairables used by the terrain generator
        /// </summary>
        private List<string> m_genHillStamps = new List<string>();
        private List<string> m_genIslandStamps = new List<string>();
        private List<string> m_genLakeStamps = new List<string>();
        private List<string> m_genMesaStamps = new List<string>();
        private List<string> m_genMountainStamps = new List<string>();
        private List<string> m_genPlainsStamps = new List<string>();
        private List<string> m_genRiverStamps = new List<string>();
        private List<string> m_genValleyStamps = new List<string>();
        private List<string> m_genVillageStamps = new List<string>();
        private List<string> m_genWaterfallStamps = new List<string>();

        /// <summary>
        /// bool to make sure we are subscribed to the scene saved event only once
        /// </summary>
        private bool savingSubscribed;

        private GaiaSettings m_gaiaSettings;

        private GaiaSettings GaiaSettings
        {
            get
            {
                if (m_gaiaSettings == null)
                {
                    m_gaiaSettings = GaiaUtils.GetGaiaSettings();
                }
                return m_gaiaSettings;
            }
        }

        private bool m_heightmapChangeSubscribed;
        public ImageMask m_copiedImageMask;
        public SpawnRule m_copiedSpawnRule;
        public Spawner m_copiedSpawnRuleSpawner;
        public bool m_waitForSessionContinueAfterStamping;
        public List<StamperSettings> m_massStamperSettingsList = new List<StamperSettings>();
        public int m_massStamperSettingsIndex = int.MaxValue;

        public bool m_showSceneViewPanel;
        public bool m_showTerrainOpsPanel;



        /// <summary>
        /// Contains the terrain names that need to be regenerated due to change / removal of a session operation
        /// </summary>
        public List<string> m_terrainNamesFlaggedForRegeneration = new List<string>();
        /// <summary>
        /// Contains the terrain names that need to be regenerated due to deactivation of a session operation
        /// As soon as the user re-activates this operation, those would be removed from the list again which is why we need to keep 2 lists.
        /// </summary>
        public List<string> m_terrainNamesFlaggedForRegenerationDeactivation = new List<string>();


        private static bool m_regenerateRun;
        private Vector3Double m_originBackup;
        private double m_loadRangeBackup;
        private double m_loadRangeBackupImpostor;
        private int m_editorUpdateCount = 100;
        public bool m_selectAllOperations = true;

        public delegate void WorldCreatedCallback();
        public static event WorldCreatedCallback OnWorldCreated;

        public delegate void MassStampingFinishedCallback();
        public static event MassStampingFinishedCallback OnMassStampingFinished;

        public delegate void WorldMapExportedCallback();
        public static event WorldMapExportedCallback OnWorldMapExported;


#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            GameObject mgrObj = GameObject.Find("Session Manager");
            if (mgrObj != null)
            {
                EditorSceneManager.sceneSaved += GaiaSessionManager.GetSessionManager().OnSceneSaved;
            }
        }
#endif

        public void OnEnable()
        {
#if UNITY_EDITOR
            if (!savingSubscribed)
            {
                EditorSceneManager.sceneSaved += OnSceneSaved;
                savingSubscribed = true;
            }

            //not thread save, but more reliable 
            TerrainCallbacks.heightmapChanged -= OnHeightmapChanged;
            TerrainCallbacks.heightmapChanged += OnHeightmapChanged;
            m_heightmapChangeSubscribed = true;

#endif
        }

        /// <summary>
        /// Performs a regenerate run on the terrains that were flagged for regeneration due to changes in the session. This will first flatten the flagged terrains and then play back the session from the last world creation.
        /// </summary>
        public void RegenerateFlaggedTerrains()
        {
            //First flatten the terrains - we do not want this to be tracked in the session so we execute right away & bypass the session logging.
            FlattenTerrainsByList(m_terrainNamesFlaggedForRegeneration.Concat(m_terrainNamesFlaggedForRegenerationDeactivation).ToList());
            //Remove everything that is spawned on the terrain so far
            TerrainHelper.ClearSpawns(SpawnerResourceType.TerrainDetail, ClearSpawnFor.CurrentTerrainOnly, ClearSpawnFrom.AnySource, m_terrainNamesFlaggedForRegeneration, null);
            TerrainHelper.ClearSpawns(SpawnerResourceType.TerrainTree, ClearSpawnFor.CurrentTerrainOnly, ClearSpawnFrom.AnySource, m_terrainNamesFlaggedForRegeneration, null);
            TerrainHelper.ClearSpawns(SpawnerResourceType.GameObject, ClearSpawnFor.CurrentTerrainOnly, ClearSpawnFrom.AnySource, m_terrainNamesFlaggedForRegeneration, null);
            TerrainHelper.ClearSpawns(SpawnerResourceType.SpawnExtension, ClearSpawnFor.CurrentTerrainOnly, ClearSpawnFrom.AnySource, m_terrainNamesFlaggedForRegeneration, null);
            //Do we have a world map? If yes, we need to init the regen terrains with the world map heights first
            if (TerrainLoaderManager.Instance.WorldMapTerrain != null)
            {
                GameObject wmeGO = GaiaUtils.GetOrCreateWorldDesigner();
                wmeGO.GetComponent<WorldMap>().SyncWorldMapToLocalMap(m_terrainNamesFlaggedForRegeneration.Concat(m_terrainNamesFlaggedForRegenerationDeactivation).ToList());
            }
            //now all the regeneration terrains should be "reset" properly to re-run the session on them
            PlaySession(null, true);
        }

        private void OnSceneSaved(Scene scene)
        {
            if (GaiaUtils.GetGaiaSettings().m_saveCollisionCacheWhenSaving)
            {
                m_bakedMaskCache.WriteCacheToDisk();
            }
        }

        public void OnDestroy()
        {
#if UNITY_EDITOR

            if (savingSubscribed)
            {
                EditorSceneManager.sceneSaved -= OnSceneSaved;
                savingSubscribed = false;
            }

            if (m_heightmapChangeSubscribed)
            {
                TerrainCallbacks.heightmapChanged -= OnHeightmapChanged;
                m_heightmapChangeSubscribed = false;
            }
#endif
        }


        private void OnHeightmapChanged(Terrain terrain, RectInt heightRegion, bool synched)
        {
            if (m_session == null)
            {
                return;
            }
#if UNITY_EDITOR
            //as soon as the heightmap changes for whatever reason, we need to flag this terrain for min max height recalculation
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(terrain.terrainData));
            TerrainMinMaxHeight tmmh = m_session.m_terrainMinMaxCache.Find(x => x.guid == guid);
            if (tmmh != null)
            {
                tmmh.recalculate = true;
                if (TerrainHelper.IsWorldMapTerrain(terrain))
                {
                    tmmh.isWorldmap = true;
                }
            }
            else
            {
                //Create a new cache entry
                tmmh = new TerrainMinMaxHeight();
                if (TerrainHelper.IsWorldMapTerrain(terrain))
                    tmmh.isWorldmap = true;
                tmmh.recalculate = true;
                tmmh.min = 0;
                tmmh.max = 0;
                tmmh.guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(terrain.terrainData));
                m_session.m_terrainMinMaxCache.Add(tmmh);
            }

            //if (m_waitForSessionContinueAfterStamping)
            //{
            //    m_waitForSessionContinueAfterStamping = false;
            //    if (m_session.m_operations.Exists(x => x.sessionPlaybackState == SessionPlaybackState.Queued))
            //    {
            //        GaiaSessionManager.ContinueSessionPlayback();
            //    }
            //    else
            //    {
            //        //No session to continue -> destroy the temporary tools, if any
            //        GaiaSessionManager.DestroyTempSessionTools();
            //    }
            //    return;
            //}

#endif
        }


        /// <summary>
        /// Get the session manager
        /// </summary>
        public static GaiaSessionManager GetSessionManager(bool pickupExistingTerrain = false, bool createSession = true)
        {
            GaiaSessionManager sessionMgr = null;
            GameObject mgrObj = GameObject.Find("Session Manager");
            if (mgrObj == null)
            {
                if (createSession)
                {
                    //Find or create gaia
                    GameObject gaiaObj = GaiaUtils.GetGaiaGameObject();
                    mgrObj = new GameObject("Session Manager");
                    sessionMgr = mgrObj.AddComponent<GaiaSessionManager>();
                    sessionMgr.CreateSession(pickupExistingTerrain);
                    mgrObj.transform.parent = gaiaObj.transform;
                    mgrObj.transform.position = Gaia.TerrainHelper.GetActiveTerrainCenter();
                }
            }
            else
            {
                sessionMgr = mgrObj.GetComponent<GaiaSessionManager>();
            }
            return sessionMgr;
        }



        /// <summary>
        /// Gets the minimum and maximum height of the entire world.
        /// </summary>
        /// <param name="minWorldHeight">ref value for the minimum world height </param>
        /// <param name="maxWorldHeight">ref value for the maximum world height</param>
        /// <param name="isWorldmap">Whether this check should be performed for the regular game world, or for the world map only. (Which might be different in scale!)</param>
        public void GetWorldMinMax(ref float minWorldHeight, ref float maxWorldHeight, bool isWorldmap = false)
        {
#if UNITY_EDITOR
            bool dynamicLoaded = GaiaUtils.HasDynamicLoadedTerrains();

            //early out when no terrains
            if (!dynamicLoaded && Terrain.activeTerrains.Length <= 0)
            {
                minWorldHeight = 0;
                maxWorldHeight = 0;
                return;
            }


            if (!dynamicLoaded)
            {
                //Are there any new terrains that are not part of the cache yet?
                foreach (Terrain t in Terrain.activeTerrains)
                {
                    if (!TerrainHelper.IsWorldMapTerrain(t))
                    {
                        if (m_session.m_terrainMinMaxCache.Find(x => x.guid == AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(t.terrainData))) == null)
                        {
                            float newMin = float.MaxValue;
                            float newMax = float.MinValue;
                            GetTerrainMinMax(t.terrainData, ref newMin, ref newMax);
                        }
                    }
                }
            }


            //are there terrains left that need to be re-eavlauted?
            var remainingCacheEntries = m_session.m_terrainMinMaxCache.Where(x => x.isWorldmap == isWorldmap && x.recalculate == true).ToList();
            if (remainingCacheEntries.Count() > 0)
            {
                float newMin = float.MaxValue;
                float newMax = float.MinValue;
                if (isWorldmap)
                {
                    GetTerrainMinMax(TerrainLoaderManager.Instance.WorldMapTerrain.terrainData, ref newMin, ref newMax);
                }
                else
                {
                    if (dynamicLoaded)
                    {
                        //dynamic loaded terrains, assemble a list of terrain names to load terrains in one after another and do the min-max evaluation on them
                        List<string> terrainNames = new List<string>();
                        foreach (TerrainMinMaxHeight tmmh in remainingCacheEntries)
                        {
                            TerrainData terrainData = (TerrainData)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(tmmh.guid), typeof(TerrainData));
                            GetTerrainMinMax(terrainData, ref newMin, ref newMax);
                        }
                    }
                    else
                    {
                        //non-dynamic loaded terrains, iterate over all active terrains in the scene to see if one of those needs to be re-evaluated in the cache
                        foreach (Terrain t in Terrain.activeTerrains)
                        {
                            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(t.terrainData));
                            if (remainingCacheEntries.Where(x => x.guid == guid).Count() > 0)
                            {
                                GetTerrainMinMax(t.terrainData, ref newMin, ref newMax);
                            }
                        }
                    }
                }
            }
            if (m_session.m_terrainMinMaxCache.Where(x => x.isWorldmap == isWorldmap).Any())
            {
                minWorldHeight = m_session.m_terrainMinMaxCache.Where(x => x.isWorldmap == isWorldmap).Select(x => x.min).Min();
                maxWorldHeight = m_session.m_terrainMinMaxCache.Where(x => x.isWorldmap == isWorldmap).Select(x => x.max).Max();
            }
            else
            {
                minWorldHeight = 0;
                maxWorldHeight = 0;
            }
            ProgressBar.Clear(ProgressBarPriority.MinMaxHeightCalculation);
#endif
        }


        /// <summary>
        /// Current lock status of the session
        /// </summary>
        /// <returns>True if the session is locked false otherwise</returns>
        public bool IsLocked()
        {
            if (m_session == null)
            {
                CreateSession();
            }
            return m_session.m_isLocked;
        }

        /// <summary>
        /// Lock the session, return previous lock state
        /// </summary>
        /// <returns>Previous lock state</returns>
        public bool LockSession()
        {
            if (m_session == null)
            {
                CreateSession();
            }

            bool prevLockState = m_session.m_isLocked;
            m_session.m_isLocked = true;
            if (prevLockState == false)
            {
                SaveSession();
            }
            return prevLockState;
        }

        /// <summary>
        /// Un lock the session, return previous lock state
        /// </summary>
        /// <returns>Previous lock state</returns>
        public bool UnLockSession()
        {
            if (m_session == null)
            {
                CreateSession();
            }

            bool prevLockState = m_session.m_isLocked;
            m_session.m_isLocked = false;
            if (prevLockState == true)
            {
                SaveSession();
            }
            return prevLockState;
        }

        /// <summary>
        /// Add an operation to the session
        /// </summary>
        /// <param name="operation"></param>
        public void AddOperation(GaiaOperation operation)
        {
            if (IsLocked())
            {
                Debug.Log("Cant add operation on locked session");
                return;
            }
            m_session.m_operations.Add(operation);

            //write out the scriptable object that holds the actual data

            SaveSession();
        }

        /// <summary>
        /// Get the operation with the supplied index
        /// </summary>
        /// <param name="operationIdx">Operation index</param>
        /// <returns>Operation or null if index out of brounds</returns>
        public GaiaOperation GetOperation(int operationIdx)
        {
            if (m_session == null)
            {
                CreateSession();
            }
            if (operationIdx < 0 || operationIdx >= m_session.m_operations.Count)
            {
                return null;
            }
            return m_session.m_operations[operationIdx];
        }

        /// <summary>
        /// Remove the operation at the supplied index - ignores if undex out of bounds
        /// </summary>
        /// <param name="operationIdx">Operation index</param>
        public void RemoveOperation(int operationIdx)
        {
            if (!ChangeAllowed())
            {
                return;
            }
            if (operationIdx < 0 || operationIdx >= m_session.m_operations.Count)
            {
                return;
            }

            GaiaOperation op = m_session.m_operations[operationIdx];
            switch (op.m_operationType)
            {
                case GaiaOperation.OperationType.CreateWorld:
                    if (op.WorldCreationSettings != null)
                    {
                        DestroyImmediate(op.WorldCreationSettings, true);
                    }
                    break;
                case GaiaOperation.OperationType.FlattenTerrain:
                    if (op.FlattenOperationSettings != null)
                    {
                        DestroyImmediate(op.FlattenOperationSettings, true);
                    }
                    break;
                case GaiaOperation.OperationType.ClearSpawns:
                    if (op.ClearOperationSettings != null)
                    {
                        DestroyImmediate(op.ClearOperationSettings, true);
                    }
                    break;
                case GaiaOperation.OperationType.Stamp:
                    if (op.StamperSettings != null)
                    {
                        DestroyImmediate(op.StamperSettings, true);
                    }
                    break;
                case GaiaOperation.OperationType.StampUndo:
                    if (op.UndoRedoOperationSettings != null)
                    {
                        DestroyImmediate(op.UndoRedoOperationSettings, true);
                    }
                    break;
                case GaiaOperation.OperationType.StampRedo:
                    if (op.UndoRedoOperationSettings != null)
                    {
                        DestroyImmediate(op.UndoRedoOperationSettings, true);
                    }
                    break;
                case GaiaOperation.OperationType.Spawn:
                    if (op.SpawnOperationSettings != null)
                    {
                        DestroyImmediate(op.SpawnOperationSettings, true);
                    }
                    break;
                case GaiaOperation.OperationType.RemoveNonBiomeResources:
                    if (op.RemoveNonBiomeResourcesSettings != null)
                    {
                        DestroyImmediate(op.RemoveNonBiomeResourcesSettings, true);
                    }
                    break;
                case GaiaOperation.OperationType.MaskMapExport:
                    if (op.ExportMaskMapOperationSettings != null)
                    {
                        DestroyImmediate(op.ExportMaskMapOperationSettings, true);
                    }
                    break;
                case GaiaOperation.OperationType.ClearWorld:
                    //nothing to do here, since this op has no settings
                    break;
            }

            FlagAffectedTerrainsForRegen(op);


            m_session.m_operations.RemoveAt(operationIdx);
            SaveSession();
        }

        private void FlagAffectedTerrainsForRegen(GaiaOperation op)
        {
            //Flag all the affected terrains for regeneration
            foreach (string terrainName in op.m_affectedTerrainNames)
            {
                if (!m_terrainNamesFlaggedForRegeneration.Contains(terrainName))
                {
                    m_terrainNamesFlaggedForRegeneration.Add(terrainName);
                }
            }
        }

        /// <summary>
        /// Add a resources file if its not already there
        /// </summary>
        /// <param name="resource">Resource to be added</param>
        public void AddResource(GaiaResource resource)
        {
            //if (IsLocked())
            //{
            //    Debug.Log("Cant add resource on locked session");
            //    return;
            //}
            //if (resource != null)
            //{
            //    if (!m_session.m_resources.ContainsKey(resource.m_resourcesID + resource.name))
            //    {
            //        //Get the raw resource and add it into the dictionary
            //        #if UNITY_EDITOR
            //        ScriptableObjectWrapper so = new ScriptableObjectWrapper();
            //        so.m_name = resource.m_name;
            //        so.m_fileName = AssetDatabase.GetAssetPath(resource);
            //        so.m_content = PWCommon1.Utils.ReadAllBytes(so.m_fileName);
            //        if (so.m_content != null && so.m_content.GetLength(0) > 0)
            //        {
            //            m_session.m_resources.Add(resource.m_resourcesID + resource.name, so);
            //            SaveSession();
            //        }
            //        #endif
            //    }
            //}
        }

        /// <summary>
        /// Add a defaults file if its not already there
        /// </summary>
        /// <param name="defaults">Resource to be added</param>
        public void AddDefaults(GaiaDefaults defaults)
        {
            //            if (IsLocked())
            //            {
            //                Debug.Log("Cant add defaults on locked session");
            //                return;
            //            }

            //            if (defaults != null)
            //            {
            //                //Get the raw resource and add it into the dictionary
            //#if UNITY_EDITOR
            //                m_session.m_defaults = new ScriptableObjectWrapper();
            //                m_session.m_defaults.m_name = "Defaults";
            //                m_session.m_defaults.m_fileName = AssetDatabase.GetAssetPath(defaults);
            //                m_session.m_defaults.m_content = PWCommon4.Utils.ReadAllBytes(m_session.m_defaults.m_fileName);
            //                SaveSession();
            //#endif
            //            }
        }

        /// <summary>
        /// Add the preview image
        /// </summary>
        /// <param name="image">The image to add</param>
        public void AddPreviewImage(Texture2D image)
        {
            if (IsLocked())
            {
                Debug.Log("Cant add preview on locked session");
                return;
            }
            m_session.m_previewImageWidth = image.width;
            m_session.m_previewImageHeight = image.height;
            m_session.m_previewImageBytes = image.GetRawTextureData();
            SaveSession();
        }

        /// <summary>
        /// Whether or not the session has a preview image
        /// </summary>
        /// <returns>Returns true if the session has a preview image</returns>
        public bool HasPreviewImage()
        {
            if (m_session.m_previewImageWidth > 0 && m_session.m_previewImageHeight > 0 && m_session.m_previewImageBytes.GetLength(0) > 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove the preview image
        /// </summary>
        public void RemovePreviewImage()
        {
            if (IsLocked())
            {
                Debug.Log("Cant remove preview on locked session");
                return;
            }
            m_session.m_previewImageWidth = 0;
            m_session.m_previewImageHeight = 0;
            m_session.m_previewImageBytes = new byte[0];
            SaveSession();
        }

        /// <summary>
        /// Get the embedded preview image or null
        /// </summary>
        /// <returns>Embedded preview image or null</returns>
        public Texture2D GetPreviewImage()
        {
            if (m_session.m_previewImageBytes.GetLength(0) == 0)
            {
                return null;
            }

            Texture2D image = new Texture2D(m_session.m_previewImageWidth, m_session.m_previewImageHeight, TextureFormat.ARGB32, false);
            image.LoadRawTextureData(m_session.m_previewImageBytes);
            image.Apply();

            //Do a manual colour mod if in linear colour space
#if UNITY_EDITOR
            if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                Color[] pixels = image.GetPixels();
                for (int idx = 0; idx < pixels.GetLength(0); idx++)
                {
                    pixels[idx] = pixels[idx].gamma;
                }
                image.SetPixels(pixels);
                image.Apply();
            }
#endif
            image.name = m_session.m_name;
            return image;
        }

        /// <summary>
        /// Force unity to save the session
        /// </summary>
        public void SaveSession()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(m_session);
            AssetDatabase.SaveAssets();
#endif
        }

        /// <summary>
        /// Start editor updates
        /// </summary>
        public void StartEditorUpdates()
        {
#if UNITY_EDITOR
            EditorApplication.update += EditorUpdate;
#endif
        }

        /// <summary>
        /// Stop editor updates
        /// </summary>
        public void StopEditorUpdates()
        {
            //For editor update purposes
            m_currentSpawner = null;
            m_currentStamper = null;
            m_updateOperationCoroutine = null;
            m_updateSessionCoroutine = null;

#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
#endif
        }

        /// <summary>
        /// This is executed only in the editor - using it to simulate co-routine execution and update execution
        /// </summary>
        void EditorUpdate()
        {
            if (m_cancelPlayback)
            {
                if (m_currentSpawner != null)
                {
                    m_currentSpawner.CancelSpawn();
                }
                if (m_currentStamper != null)
                {
                    m_currentStamper.CancelStamp();
                }
                StopEditorUpdates();
            }
            else
            {
                if (m_updateSessionCoroutine == null && m_updateOperationCoroutine == null)
                {
                    StopEditorUpdates();
                }
                else
                {
                    if (m_updateOperationCoroutine != null)
                    {
                        if (m_editorUpdateCount > 100)
                        {
                            m_updateOperationCoroutine.MoveNext();
                        }
                        else
                        {
                            m_editorUpdateCount++;
                        }
                    }
                    else
                    {
                        m_updateSessionCoroutine.MoveNext();
                    }
                }
            }
        }


        /// <summary>
        /// Will create a session - and if in the editor, also save it to disk
        /// </summary>
        /// <returns></returns>
        public GaiaSession CreateSession(bool pickupExistingTerrain = false)
        {
            m_session = ScriptableObject.CreateInstance<Gaia.GaiaSession>();
            GaiaSettings settings = Gaia.GaiaUtils.GetGaiaSettings();

            m_session.m_description = "Rocking out at Creativity Central!\n\nIf you like Gaia please consider rating it :)";

            //Do we have a water surface in the scene already? If yes, we should take the sea level from there.
            if (PWS_WaterSystem.Instance != null)
            {
                m_session.m_seaLevel = PWS_WaterSystem.Instance.SeaLevel;
            }
            else
            {
                //No sea level in scene? Grab the sea level from the default resources file
                if (settings != null)
                {
                    if (settings.m_currentDefaults != null)
                    {
                        m_session.m_seaLevel = settings.m_currentDefaults.m_seaLevel;
                    }
                }
            }

            //Setting up the global spawn density: Do we have any spawners in the scene already? If yes, take it from there
            Spawner spawner = FindObjectOfType<Spawner>();
            if (spawner != null)
            {
                m_session.m_spawnDensity = spawner.m_settings.m_spawnDensity;
            }
            else
            {
                //no spawner? take the value from the current defaults then
                if (settings != null && settings.m_currentDefaults != null)
                {
                    m_session.m_spawnDensity = settings.m_currentDefaults.m_spawnDensity;
                }
            }



            //Lets see if we can pick up some defaults from the extisting terrain if there is one
            Terrain t = Gaia.TerrainHelper.GetActiveTerrain();
            if (t != null)
            {
                m_session.m_terrainWidth = (int)t.terrainData.size.x;
                m_session.m_terrainDepth = (int)t.terrainData.size.z;
                m_session.m_terrainHeight = (int)t.terrainData.size.y;

                //Pick up existing terrain
                //if (pickupExistingTerrain)
                //{
                //    GaiaDefaults defaults = ScriptableObject.CreateInstance<Gaia.GaiaDefaults>();
                //    defaults.UpdateFromTerrain();

                //    GaiaResource resources = new Gaia.GaiaResource();
                //    resources.UpdatePrototypesFromTerrain();
                //    resources.ChangeSeaLevel(m_session.m_seaLevel);

                //    AddDefaults(defaults);
                //    AddResource(resources);
                //    AddOperation(defaults.GetTerrainCreationOperation(resources));
                //}
            }
            else
            {
                if (settings != null && settings.m_currentDefaults != null)
                {
                    m_session.m_terrainWidth = settings.m_currentDefaults.m_terrainSize;
                    m_session.m_terrainDepth = settings.m_currentDefaults.m_terrainHeight;
                    m_session.m_terrainHeight = settings.m_currentDefaults.m_terrainSize;
                }
            }

            //Update the width and height scales based on terrain size
            float wScale = 60f / 2048f;
            float hScale = 15f / 2048f;
            float sScale = 50f / 2048f;

            float tw = m_session.m_terrainWidth;
            float th = m_session.m_terrainHeight;

            m_genScaleWidth = Mathf.Clamp(tw * wScale, 0.1f, 100f);
            m_genScaleHeight = Mathf.Clamp(th * hScale, 0.1f, 16f);
            if (PWS_WaterSystem.Instance == null)
            {
                m_session.m_seaLevel = Mathf.Clamp(th * sScale, 0f, 150f);
            }

            //set up the min max cache with the existing terrains in the scene / in the terrain scene data object
            float newMin = float.MaxValue;
            float newMax = float.MinValue;
            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                Action<Terrain> act = (terrain) => GetTerrainMinMax(terrain.terrainData, ref newMin, ref newMax);
                GaiaUtils.CallFunctionOnDynamicLoadedTerrains(act, false, null, "Evaluating World Height...");
            }
            else
            {
                foreach (Terrain terrain in Terrain.activeTerrains)
                {
                    GetTerrainMinMax(terrain.terrainData, ref newMin, ref newMax);
                }
            }
            ProgressBar.Clear(ProgressBarPriority.MinMaxHeightCalculation);
#if UNITY_EDITOR
            string path = GaiaDirectories.GetSessionDirectory();
            AssetDatabase.CreateAsset(m_session, path + "/" + GetNewSessionName() + ".asset");
            AssetDatabase.SaveAssets();
#endif
            return m_session;
        }

        /// <summary>
        /// Returns a new name for the session file
        /// </summary>
        /// <returns></returns>
        public static string GetNewSessionName()
        {
            return string.Format("GS-{0:yyyyMMdd - HHmmss}", DateTime.Now);
        }

        /// <summary>
        /// Set the session sea level - this will influence the spawners and the resources they use
        /// </summary>
        /// <param name="seaLevel"></param>
        public void SetSeaLevel(float seaLevel, bool setFromWorldMap = false)
        {
            if (!setFromWorldMap)
            {
                m_session.m_seaLevel = seaLevel;
            }
            else
            {
                m_session.m_seaLevel = seaLevel / TerrainLoaderManager.Instance.TerrainSceneStorage.m_worldMaprelativeSize;
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(m_session);
#endif
        }

        /// <summary>
        /// Get the session sea level
        /// </summary>
        /// <param name="getForWorldMap">Get the sea level adjusted for the world map scale</param>
        /// <returns></returns>
        public float GetSeaLevel(bool getForWorldMap = false)
        {
            float seaLevel = 50f;

            if (m_session != null)
            {
                seaLevel = m_session.m_seaLevel;
            }
            else
            {
                //Fallback in case session is missing for some reason
                if (GaiaSettings != null)
                {
                    seaLevel = GaiaSettings.m_currentDefaults.m_seaLevel;
                }
            }


            if (getForWorldMap)
            {
                seaLevel *= TerrainLoaderManager.Instance.TerrainSceneStorage.m_worldMaprelativeSize;
            }

            return seaLevel;
        }

        /// <summary>
        /// Get the session sea level
        /// </summary>
        /// <param name="getForWorldMap">Get the sea level adjusted for the world map scale</param>
        /// <returns></returns>
        public float GetSeaLevel(float currentSeaLevel, bool getForWorldMap = false)
        {
            float seaLevel = currentSeaLevel;

            if (m_session != null)
            {
                seaLevel = m_session.m_seaLevel;
            }
            else
            {
                return seaLevel;
            }


            if (getForWorldMap)
            {
                seaLevel *= TerrainLoaderManager.Instance.TerrainSceneStorage.m_worldMaprelativeSize;
            }

            return seaLevel;
        }

        /// <summary>
        /// Reset the session
        /// </summary>
        public void ResetSession()
        {
            //Check we have a session
            if (m_session == null)
            {
                Debug.LogError("Can not erase the session as there is no existing session!");
                return;
            }

            //Check session not locked
            if (!ChangeAllowed())
            {
                return;
            }

            if (m_session.m_operations.Count > 1)
            {
                //Keep the create terrain operation
                GaiaOperation firstOp = m_session.m_operations[0];
                m_session.m_operations.Clear();
                if (firstOp.m_operationType == GaiaOperation.OperationType.CreateWorld)
                {
                    AddOperation(firstOp);
                }
            }
        }

        /// <summary>
        /// Create randomise the stamps in a session
        /// </summary>
        public void RandomiseStamps()
        {
            //            //Set Seed
            //            if (!m_useRandomSeed)
            //            {
            //                UnityEngine.Random.InitState(m_randomSeed);
            //            }

            //            //Check we have a session
            //            if (m_session == null)
            //            {
            //                Debug.LogError("Can not randomise stamps as there is no existing session!");
            //                return;
            //            }

            //            //Check session not locked
            //            if (m_session.m_isLocked == true)
            //            {
            //                Debug.LogError("Can not randomise stamps as the existing session is locked!");
            //                return;
            //            }

            //            //Check we have an active terrain (really should be able to create one)
            //            Terrain terrain = TerrainHelper.GetActiveTerrain();
            //            if (terrain == null)
            //            {
            //                //Pick up current settings
            //                GaiaSettings settings = (GaiaSettings) GaiaUtils.GetAssetScriptableObject("GaiaSettings");
            //                if (settings == null)
            //                {
            //                    Debug.LogError("Can not randomise stamps as we are missing the terrain and settings!");
            //                    return;
            //                }

            //                //Grab defaults n settings
            //                GaiaDefaults defaults = settings.m_currentDefaults;
            //                GaiaResource resources = settings.m_currentResources;

            //                if (defaults == null || resources == null)
            //                {
            //                    Debug.LogError("Can not randomise stamps as we are missing the terrain defaults or resources!");
            //                    return;
            //                }

            //                #if UNITY_EDITOR
            //                //Disable automatic light baking - this kills perf on most systems
            //                Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
            //                #endif

            //                //Create the terrain
            //                defaults.CreateTerrain();

            //                //Grab it
            //                terrain = TerrainHelper.GetActiveTerrain();
            //            }

            //            //Get its bounds
            //            Bounds terrainBounds = new Bounds();
            //            TerrainHelper.GetTerrainBounds(terrain, ref terrainBounds);

            //            //Create stamper
            //            GameObject gaiaObj = GameObject.Find("Gaia");
            //            if (gaiaObj == null)
            //            {
            //                gaiaObj = new GameObject("Gaia");
            //            }

            //            Stamper stamper = null;
            //            GameObject stamperObj = GameObject.Find("Stamper");
            //            if (stamperObj == null)
            //            {
            //                stamperObj = new GameObject("Stamper");
            //                stamperObj.transform.parent = gaiaObj.transform;
            //                stamper = stamperObj.AddComponent<Stamper>();
            //            }
            //            else
            //            {
            //                stamper = stamperObj.GetComponent<Stamper>();
            //            }

            //            //Ok now randomly get some stamps and assemble them into a scene - we need a number that goes into a square root 
            //            float sqrtStamps = 1f;
            //            if (m_genNumStampsToGenerate > 1)
            //            {
            //                sqrtStamps = Mathf.Sqrt((float)m_genNumStampsToGenerate - 1f);

            //                if (!GaiaUtils.Math_ApproximatelyEqual(0f, sqrtStamps % 1f))
            //                {
            //                    m_genNumStampsToGenerate = Mathf.CeilToInt(sqrtStamps);
            //                    m_genNumStampsToGenerate = m_genNumStampsToGenerate * m_genNumStampsToGenerate + 1;
            //                }

            //                sqrtStamps = Mathf.Sqrt((float)m_genNumStampsToGenerate - 1f);

            //                //Debug.LogFormat("Sqrt {0}", sqrtStamps);
            //            }
            //            #if UNITY_EDITOR
            //            EditorUtility.ClearProgressBar();
            //            #endif
            //            for (int stampIdx = 0; stampIdx < m_genNumStampsToGenerate; stampIdx++)
            //            {
            //                string stampPath = "";
            //                GaiaConstants.FeatureType featureType = GaiaConstants.FeatureType.Hills;

            //                #if UNITY_EDITOR
            //                EditorUtility.DisplayProgressBar("Adding Stamps", "Adding Stamp " + (stampIdx + 1).ToString() + " of " + m_genNumStampsToGenerate.ToString(), Mathf.InverseLerp(0, m_genNumStampsToGenerate, stampIdx));

            //                if (m_session.m_operations.Count <= 1)
            //                {
            //                    if (m_genBorderStyle != GaiaConstants.GeneratorBorderStyle.Mountains)
            //                    {
            //                        featureType = GetWeightedRandomFeatureType();
            //                        stampPath = GetRandomStampPath(featureType);
            //                    }
            //                    else
            //                    {
            //                        featureType = GaiaConstants.FeatureType.Mountains;
            //                        stampPath = GetRandomMountainFieldPath();
            //                    }
            //                }
            //                else
            //                {
            //                    featureType = GetWeightedRandomFeatureType();
            //                    stampPath = GetRandomStampPath(featureType);
            //                }

            //                //Check to see if we got something useful - if not then drop out
            //                if (string.IsNullOrEmpty(stampPath))
            //                {
            //                    continue;
            //                }
            //                stampPath = stampPath.Replace('\\', '/');
            //                stampPath = stampPath.Replace(Application.dataPath + "/", "Assets/");
            //#endif

            //                //Do the basic load and initialise
            //                //stamper.LoadStamp(stampPath);
            //                //storing the guid of the stamp should be enough, no need to load up the stamp
            //                //stamper.m_stampImageGUID = AssetDatabase.AssetPathToGUID(stampPath);
            //                string stampName = Path.GetFileNameWithoutExtension(stampPath);
            //                stamper.HidePreview();

            //                //Then customise
            //                if (m_session.m_operations.Count <= 1)
            //                {
            //                    stamper.FitToTerrain();
            //                    float fullWidth = stamper.m_settings.m_width;
            //                    PositionStamp(terrainBounds, stamper, featureType);
            //                    stamper.m_settings.m_rotation = 0f;
            //                    stamper.m_settings.m_baseLevel = 0f;
            //                    stamper.m_settings.m_x = 0f;
            //                    stamper.m_settings.m_z = 0f;
            //                    stamper.m_settings.m_width = fullWidth;
            //                    if (m_genBorderStyle == GaiaConstants.GeneratorBorderStyle.Mountains)
            //                    {
            //                        stamper.m_distanceMask = new AnimationCurve(new Keyframe(1f, 1f), new Keyframe(1f, 1f));
            //                        stamper.m_areaMaskMode = GaiaConstants.ImageFitnessFilterMode.ImageGreyScale;
            //                        stamper.m_imageMask = GaiaUtils.GetAsset("Island Mask 1.exr", typeof(Texture2D)) as Texture2D;
            //                        stamper.m_imageMaskGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(stamper.m_imageMask));
            //                        stamper.m_imageMaskNormalise = true;
            //                        stamper.m_imageMaskInvert = true;
            //                    }
            //                    else
            //                    {
            //                        stamper.m_distanceMask = new AnimationCurve(new Keyframe(1f, 1f), new Keyframe(1f, 1f));
            //                        stamper.m_areaMaskMode = GaiaConstants.ImageFitnessFilterMode.ImageGreyScale;
            //                        stamper.m_imageMask = GaiaUtils.GetAsset("Island Mask 1.exr", typeof(Texture2D)) as Texture2D;
            //                        stamper.m_imageMaskGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(stamper.m_imageMask));
            //                        stamper.m_imageMaskNormalise = true;
            //                        stamper.m_imageMaskInvert = false;
            //                    }
            //                    stamper.m_distanceMaskInfluence = GaiaConstants.MaskInfluence.TotalSpawnResult;
            //                    stamper.m_areaMaskInfluence = GaiaConstants.MaskInfluence.TotalSpawnResult;
            //                }
            //                else
            //                {
            //                    int suggestX1 = (stampIdx - 1) % (int)sqrtStamps;
            //                    int suggestZ1 = (stampIdx - 1) / (int)sqrtStamps;

            //                    float invSqrt = 1f / sqrtStamps;
            //                    float offset = invSqrt / 2f;
            //                    float suggestZ = offset + (invSqrt * suggestX1);
            //                    float suggestX = offset + (invSqrt * suggestZ1);

            //                    PositionStampV2(terrainBounds, stamper, featureType, suggestX, suggestZ, offset);

            //                    //Randomly make to an inverted stamp and subtract it
            //                    float featureSelector = UnityEngine.Random.Range(0f, 1f);

            //                    if (featureSelector < 0.1f)
            //                    {
            //                        stamper.m_settings.m_operation = GaiaConstants.FeatureOperation.LowerHeight;
            //                        stamper.m_invertStamp = true;
            //                        stamper.m_settings.m_baseLevel = 1f;
            //                    }
            //                    else if (featureSelector < 0.35f)
            //                    {
            //                        stamper.m_settings.m_operation = GaiaConstants.FeatureOperation.SetHeight;
            //                        stamper.m_normaliseStamp = true;
            //                        stamper.m_settings.m_baseLevel = 0f;

            //                        if (featureType == GaiaConstants.FeatureType.Rivers || featureType == GaiaConstants.FeatureType.Lakes)
            //                        {
            //                            stamper.m_invertStamp = true;
            //                            stamper.m_stencilHeight = UnityEngine.Random.Range(-80f, -5f);
            //                        }
            //                        else
            //                        {
            //                            if (UnityEngine.Random.Range(0f, 1f) < 0.5f)
            //                            {
            //                                stamper.m_invertStamp = true;
            //                                stamper.m_stencilHeight = UnityEngine.Random.Range(-80f, -5f);
            //                            }
            //                            else
            //                            {
            //                                stamper.m_invertStamp = false;
            //                                stamper.m_stencilHeight = UnityEngine.Random.Range(5f, 80f);
            //                            }
            //                        }
            //                    }
            //                    else
            //                    {
            //                        stamper.m_settings.m_operation = GaiaConstants.FeatureOperation.RaiseHeight;
            //                        stamper.m_settings.m_baseLevel = 0f;
            //                        stamper.m_invertStamp = false;
            //                    }

            //                    //Remove any leftover image mask
            //                    stamper.m_areaMaskMode = GaiaConstants.ImageFitnessFilterMode.None;
            //                    stamper.m_imageMask = null;
            //                    stamper.EmptyImageMask();
            //                    stamper.m_MaskTexturesDirty = true;

            //                    //always set the stamp result to "Total" to prevent sharp cuts in the terrain
            //                    stamper.m_areaMaskInfluence = GaiaConstants.MaskInfluence.TotalSpawnResult;
            //                    stamper.m_distanceMaskInfluence = GaiaConstants.MaskInfluence.TotalSpawnResult;

            //                    //Also explore stenciling rivers
            //                    //- normalise - invert - negative height
            //                }

            //                //And finally update and add to session
            //                stamper.UpdateStamp();
            //                stamper.m_MaskTexturesDirty = true;
            //                stamper.AddToSession(GaiaOperation.OperationType.Stamp, "Stamping " + stampName);
            //            }

            //            #if UNITY_EDITOR
            //            EditorUtility.ClearProgressBar();
            //            #endif

            //            //Reset random Seed
            //            UnityEngine.Random.InitState(System.Environment.TickCount);
        }

        public void ForceTerrainMinMaxCalculation(Terrain terrain)
        {
            float fake1 = 0, fake2 = 0;
            GetTerrainMinMax(terrain.terrainData, ref fake1, ref fake2, true);
            ProgressBar.Clear(ProgressBarPriority.MinMaxHeightCalculation);
        }

        /// <summary>
        /// Position the stamp somewhere on terrain
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="stamper"></param>
        /// <param name="stampType"></param>
        //private void PositionStamp(Bounds bounds, Stamper stamper, GaiaConstants.FeatureType stampType)
        //{
        //    float stampBaseLevel = 0f;
        //    float stampMinHeight = 0f;
        //    float stampMaxHeight = 0f;
        //    float fullHeight = stamper.m_settings.m_height * 4f;

        //    float terrainWaterLevel = 0f;
        //    if (m_session.m_terrainHeight > 0f)
        //    {
        //        terrainWaterLevel = m_session.m_seaLevel / (float)m_session.m_terrainHeight;
        //    }

        //    //Get some basic info about the stamp and then make semi intelligent decisions about the size and placement
        //    if (stamper.GetHeightRange(ref stampBaseLevel, ref stampMinHeight, ref stampMaxHeight))
        //    {
        //        //stampRange = stampMaxHeight - stampMinHeight;
        //        //Debug.Log(string.Format("Base {0:0.000} Min {1:0.000} Max {2:0.000} Range {3:0.000} Water {4:0.000}", stampBaseLevel, stampMinHeight, stampMaxHeight, stampRange, terrainWaterLevel));

        //        //By default we are raising height
        //        stamper.m_settings.m_operation = GaiaConstants.FeatureOperation.RaiseHeight;

        //        //And not inverting or any other weirdness
        //        stamper.m_invertStamp = false;
        //        stamper.m_normaliseStamp = false;

        //        //Set stamp width, height and rotation
        //        stamper.m_settings.m_rotation = UnityEngine.Random.Range(-179, 179f);
        //        stamper.m_settings.m_width = UnityEngine.Random.Range(0.7f, 1.3f) * m_genScaleWidth;
        //        stamper.m_settings.m_height = UnityEngine.Random.Range(0.7f, 1.3f) * m_genScaleHeight; // *stampRange;

        //        //Set stamp offset accounting for water level
        //        //float relativeHeight = (stamper.m_height / fullHeight) * m_session.m_terrainHeight;
        //        //float relativeZero = relativeHeight / 2f;
        //        //float waterTrue = terrainWaterLevel * m_session.m_terrainHeight;
        //        //stamper.m_stickBaseToGround = false;
        //        //stamper.m_y = relativeZero + waterTrue - (stampBaseLevel * relativeHeight);

        //        if (m_genBorderStyle == GaiaConstants.GeneratorBorderStyle.Mountains)
        //            stamper.m_settings.m_y = UnityEngine.Random.Range(m_session.m_seaLevel + 25f, m_session.m_seaLevel + 75f);
        //        else
        //            stamper.m_settings.m_y = stamper.m_settings.m_y = UnityEngine.Random.Range(m_session.m_seaLevel +10f, m_session.m_seaLevel + 20f);

        //        //Set stamp position
        //        float offsetRange = 1f;
        //        if (m_genBorderStyle == GaiaConstants.GeneratorBorderStyle.None)
        //        {
        //            stamper.m_settings.m_x = UnityEngine.Random.Range(-bounds.extents.x, bounds.extents.x);
        //            stamper.m_settings.m_z = UnityEngine.Random.Range(-bounds.extents.z, bounds.extents.z);
        //        }
        //        else if (m_genBorderStyle == GaiaConstants.GeneratorBorderStyle.Mountains)
        //        {
        //            offsetRange = 0.85f;
        //            stamper.m_settings.m_x = UnityEngine.Random.Range(-(bounds.extents.x * offsetRange), bounds.extents.x * offsetRange);
        //            stamper.m_settings.m_z = UnityEngine.Random.Range(-(bounds.extents.z * offsetRange), bounds.extents.z * offsetRange);
        //        }
        //        else
        //        {
        //            offsetRange = 0.75f;
        //            stamper.m_settings.m_x = UnityEngine.Random.Range(-(bounds.extents.x * offsetRange), bounds.extents.x * offsetRange);
        //            stamper.m_settings.m_z = UnityEngine.Random.Range(-(bounds.extents.z * offsetRange), bounds.extents.z * offsetRange);
        //        }

        //        //Debug.LogFormat("{0:0.0000} {1:0.0000}",stamper.m_x, stamper.m_z);

        //        //Set the mask
        //        stamper.m_distanceMask = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        //        stamper.m_areaMaskMode = GaiaConstants.ImageFitnessFilterMode.None;
        //        stamper.m_imageMask = null;
        //    }
        //}


        /// <summary>
        /// Adds an entire list of stamps to the session. If executed, the stamps will be stamped with a slight delay to allow for proper processing on the terrain.
        /// </summary>
        /// <param name="stamperSettings">List of stamper settings to add</param>
        /// <param name="execute">If the stamper settings should be executed (=stamped) right away</param>
        public static void MassStamp(List<StamperSettings> stamperSettings, bool execute)
        {
            if (!execute)
            {
                for (int i = 0; i < stamperSettings.Count; i++)
                {
                    GaiaSessionManager.Stamp(stamperSettings[i], execute, null, true);
                }
            }
            else
            {
                GaiaSessionManager gsm = GaiaSessionManager.GetSessionManager();
                if (!gsm.ChangeAllowed())
                {
                    return;
                }
                gsm.StartMassStamp(stamperSettings);
            }
        }

        private void StartMassStamp(List<StamperSettings> stamperSettings)
        {
            TerrainLoaderManager.Instance.SwitchToLocalMap();
            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                TerrainLoaderManager.Instance.UnloadAll();
            }
            m_massStamperSettingsList = stamperSettings;
            m_massStamperSettingsIndex = 0;
            m_updateOperationCoroutine = ContinueMassStamp();
            StartEditorUpdates();
        }

        public IEnumerator ContinueMassStamp()
        {
            for (int i = 0; i < m_massStamperSettingsList.Count; i++)
            {
                //Allow for a little pause after every 10 stamps, to allow allocations to render textures to be actually freed up
                //otherwise those will just be kept around until stamping finishes!
                if (i % 9 == 0)
                {
                    m_editorUpdateCount = 0;
                }
                string stampName = "Stamp";
                if (m_massStamperSettingsList[i].m_stamperInputImageMask != null && m_massStamperSettingsList[i].m_stamperInputImageMask.ImageMaskTexture != null)
                {
                    stampName = m_massStamperSettingsList[i].m_stamperInputImageMask.ImageMaskTexture.name;
                }
                if (ProgressBar.Show(ProgressBarPriority.WorldCreation, "Stamping", "Stamping " + stampName, i, m_massStamperSettingsList.Count, true, true))
                {
                    m_massStamperSettingsList = null;
                    m_massStamperSettingsIndex = int.MaxValue;
                    if (OnMassStampingFinished != null)
                    {
                        OnMassStampingFinished();
                    }
                    StopEditorUpdates();
                    yield return null;
                }
                GaiaSessionManager.Stamp(m_massStamperSettingsList[i], true, null, true);
                yield return null;
            }
            m_massStamperSettingsList = null;
            m_massStamperSettingsIndex = int.MaxValue;
            if (OnMassStampingFinished != null)
            {
                OnMassStampingFinished();
            }
            StopEditorUpdates();
            yield return null;
        }

        /// <summary>
        /// Overwrites an existing stamp operation in the session and marks the terrains affected by this edit for regeneration
        /// </summary>
        /// <param name="op"></param>
        /// <param name="stamperSettings"></param>
        public void EditStampOperation(GaiaOperation op, StamperSettings stamperSettings)
        {
            //flag the originally affected terrains for regen first
            FlagAffectedTerrainsForRegen(op);
            //Remove the old stamper settings data
            if (op.StamperSettings != null)
            {
                DestroyImmediate(op.StamperSettings, true);
            }
            //save the new stamper settings data
            StamperSettings newSettings = Instantiate(stamperSettings);
            SaveOperationData(op, newSettings);
#if UNITY_EDITOR
            EditorUtility.SetDirty(newSettings);
#endif
            //the stamp might have been moved - evaluate which terrains it is hitting now and also flag those for regen
            double range = op.StamperSettings.m_width / 100 * TerrainLoaderManager.Instance.TerrainSceneStorage.m_terrainTilesSize;
            BoundsDouble operationBounds = new BoundsDouble(new Vector3Double(op.StamperSettings.m_x, op.StamperSettings.m_y, op.StamperSettings.m_z), new Vector3Double(range, range, range));
            //add the origin offset for the check (if any)
            operationBounds.center += TerrainLoaderManager.Instance.GetOrigin();
            op.m_affectedTerrainNames = TerrainHelper.GetTerrainsIntersectingBounds(operationBounds);
            //Flag those terrains for regen again - might be different than before!
            FlagAffectedTerrainsForRegen(op);
            SaveSession();
        }



        /// <summary>
        /// Position the stamp somewhere on terrain
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="stamper"></param>
        /// <param name="stampType"></param>
        private void PositionStampV2(Bounds bounds, Stamper stamper, GaiaConstants.FeatureType stampType, float suggestX, float suggestZ, float suggestJitter)
        {
            //Debug.LogFormat(" Suggested: {0:0.0000} {1:0.0000}", suggestX, suggestZ);

            //float stampBaseLevel = 0f;
            //float stampMinHeight = 0f;
            //float stampMaxHeight = 0f;
            float fullHeight = stamper.m_settings.m_height * 4f;

            float terrainWaterLevel = 0f;
            if (m_session.m_terrainHeight > 0f)
            {
                terrainWaterLevel = m_session.m_seaLevel / (float)m_session.m_terrainHeight;
            }

            //Get some basic info about the stamp and then make semi intelligent decisions about the size and placement
            //if (stamper.GetHeightRange(ref stampBaseLevel, ref stampMinHeight, ref stampMaxHeight))
            //{
            //stampRange = stampMaxHeight - stampMinHeight;
            //Debug.Log(string.Format("Base {0:0.000} Min {1:0.000} Max {2:0.000} Range {3:0.000} Water {4:0.000}", stampBaseLevel, stampMinHeight, stampMaxHeight, stampRange, terrainWaterLevel));

            //By default we are raising height
            stamper.m_settings.m_operation = GaiaConstants.FeatureOperation.RaiseHeight;

            //And not inverting or any other weirdness
            stamper.m_invertStamp = false;
            stamper.m_normaliseStamp = false;

            //Set stamp width, height and rotation
            stamper.m_settings.m_rotation = UnityEngine.Random.Range(-179, 179f);
            stamper.m_settings.m_width = UnityEngine.Random.Range(0.7f, 1.3f) * m_genScaleWidth;
            stamper.m_settings.m_height = UnityEngine.Random.Range(0.3f, 1.5f) * m_genScaleHeight; // *stampRange;

            //Set stamp offset accounting for water level
            //float relativeHeight = (stamper.m_height / fullHeight) * m_session.m_terrainHeight;
            //float relativeZero = relativeHeight / 2f;
            //float waterTrue = terrainWaterLevel * m_session.m_terrainHeight;
            //stamper.m_stickBaseToGround = false;
            //stamper.m_y = relativeZero + waterTrue - (stampBaseLevel * relativeHeight);
            if (m_genBorderStyle == GaiaConstants.GeneratorBorderStyle.Mountains)
                stamper.m_settings.m_y = UnityEngine.Random.Range(m_session.m_seaLevel + 50f, m_session.m_seaLevel + 150f);
            else
                stamper.m_settings.m_y = stamper.m_settings.m_y = UnityEngine.Random.Range(m_session.m_seaLevel + 15f, m_session.m_seaLevel + 75f);

            //Move stamps closer to centre if water
            if (m_genBorderStyle == GaiaConstants.GeneratorBorderStyle.Water)
            {
                Vector2 source = new Vector2(suggestX, suggestZ);
                Vector2 centre = new Vector2(0.5f, 0.5f);
                float distance = Vector2.Distance(source, centre) * 2f;
                Vector2 dest = Vector2.Lerp(source, centre, distance / 2f);

                suggestX = dest.x;
                suggestZ = dest.y;
            }

            float widthX = bounds.size.x;
            float depthZ = bounds.size.z;
            float jitterX = widthX * suggestJitter;
            float jitterZ = depthZ * suggestJitter;
            stamper.m_settings.m_x = bounds.min.x + (suggestX * widthX) + UnityEngine.Random.Range(-jitterX, jitterX);
            stamper.m_settings.m_z = bounds.min.z + (suggestZ * depthZ) + UnityEngine.Random.Range(-jitterZ, jitterZ);

            //Debug.LogFormat("{0:0.0000} {1:0.0000}", stamper.m_x, stamper.m_z);

            //Set the mask
            stamper.m_distanceMask = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            stamper.m_areaMaskMode = GaiaConstants.ImageFitnessFilterMode.None;
            stamper.m_imageMask = null;
            //}
        }

        public string GetRandomMountainFieldPath()
        {
            if (m_genMountainStamps.Count == 0)
            {
                m_genMountainStamps = Gaia.GaiaUtils.GetGaiaStampsList(GaiaConstants.FeatureType.Mountains);
            }
            if (m_genMountainStamps.Count == 0)
            {
                return "";
            }

            string stampPath;
            int idx = 0;
            int fields = 0;

            //Count fields
            for (idx = 0; idx < m_genMountainStamps.Count; idx++)
            {
                stampPath = m_genMountainStamps[idx];
                if (stampPath.Contains("Field"))
                {
                    fields++;
                }
            }

            //Now choose one
            int hits = 0;
            int luckyNumber = UnityEngine.Random.Range(0, fields - 1);
            for (idx = 0; idx < m_genMountainStamps.Count; idx++)
            {
                stampPath = m_genMountainStamps[idx];
                if (stampPath.Contains("Field"))
                {
                    if (hits == luckyNumber)
                    {
                        return stampPath;
                    }
                    hits++;
                }
            }
            return "";
        }
        /// <summary>
        /// Backs up the current origin and load range
        /// </summary>
        private void BackupOriginAndLoadRange()
        {
            m_originBackup = TerrainLoaderManager.Instance.GetOrigin();
            m_loadRangeBackup = TerrainLoaderManager.Instance.GetLoadingRange();
            m_loadRangeBackupImpostor = TerrainLoaderManager.Instance.GetImpostorLoadingRange();
        }

        private void RestoreOriginAndLoadRange()
        {
            TerrainLoaderManager.Instance.SetOrigin(m_originBackup);
            TerrainLoaderManager.Instance.SetLoadingRange(m_loadRangeBackup, m_loadRangeBackupImpostor);
        }

        /// <summary>
        /// Plays back the given Session, or the current one if session parameter is left empty
        /// <param name="session">The session to play back.</param>
        /// <paramref name="regenerateOnly"/>Whether the session should be played back in "Regeneration Mode" - In this case only terrains that are flagged for outstanding changes in the session will be affected by the session playback.
        /// </summary>
        public static void PlaySession(GaiaSession session = null, bool regenerateOnly = false)
        {
            GaiaSessionManager sessionManager = GaiaSessionManager.GetSessionManager(false, false);
            if (session != null)
            {
                sessionManager.m_session = session;
            }

            sessionManager.BackupOriginAndLoadRange();

            if (regenerateOnly)
            {
                m_regenerateRun = true;

                //Regenerate run - start the session playback only from the last world creation entry onwards
                //For regeneration we are only interested in the operations which were executed after the terrain tiles were created
                int lastCreationIndex = sessionManager.m_session.m_operations.FindLastIndex(x => x.m_operationType == GaiaOperation.OperationType.CreateWorld && !x.WorldCreationSettings.m_isWorldMap);
                for (int i = lastCreationIndex + 1; i < sessionManager.m_session.m_operations.Count; i++)
                {
                    GaiaOperation op = sessionManager.m_session.m_operations[i];
                    //Skip all world map operations - not interested in those
                    if (op.m_operationType == GaiaOperation.OperationType.CreateWorld)
                    {
                        continue;
                    }
                    if (op.m_operationType == GaiaOperation.OperationType.Stamp && op.StamperSettings.m_isWorldmapStamper == true)
                    {
                        continue;
                    }
                    if (op.m_operationType == GaiaOperation.OperationType.Spawn && op.SpawnOperationSettings.m_isWorldMapSpawner == true)
                    {
                        continue;
                    }
                    sessionManager.m_session.m_operations[i].sessionPlaybackState = SessionPlaybackState.Queued;
                }
            }
            else
            {
                m_regenerateRun = false;
                foreach (GaiaOperation op in sessionManager.m_session.m_operations)
                {
                    op.sessionPlaybackState = SessionPlaybackState.Queued;
                }
#if UNITY_EDITOR
                Selection.activeGameObject = sessionManager.gameObject;
#endif
            }

            ContinueSessionPlayback();
        }

        public static void ContinueSessionPlayback()
        {
            GaiaSessionManager sessionManager = GaiaSessionManager.GetSessionManager();

            foreach (GaiaOperation op in sessionManager.m_session.m_operations)
            {
                if (op.m_isActive && op.sessionPlaybackState == SessionPlaybackState.Queued)
                {
                    bool success = GaiaSessionManager.ExecuteOperation(op);
                    op.sessionPlaybackState = SessionPlaybackState.Started;

                    if (op.m_operationType == GaiaOperation.OperationType.CreateWorld && !success)
                    {
                        //abort if the world creation was not successful - all other ops will fail as well
                        Debug.LogError("World Creation failed - Aborting Session Playback.");
                        break;
                    }

                    if (op.m_operationType == GaiaOperation.OperationType.CreateWorld)
                    {
                        //World creation is asynchronous - stop the loop here and wait for the session manager to call continue again after the terrain(s) were created
                        return;
                    }

                    if (op.m_operationType == GaiaOperation.OperationType.Spawn)
                    {
                        //Spawning is asynchronous - stop the loop here and wait for the spawner to call continue again to process the rest of the operations
                        return;
                    }

                    if (op.m_operationType == GaiaOperation.OperationType.Stamp)
                    {
                        //We need to wait for heightmap updates after stamping - stop the loop here and the OnHeightmapChanged event will pick it up after
                        return;
                    }

                }
                else
                {
                    op.sessionPlaybackState = SessionPlaybackState.Started;
                }
            }
            AfterSessionPlaybackCleanup();
#if UNITY_EDITOR
            Selection.activeGameObject = sessionManager.gameObject;
#endif
        }


        /// <summary>
        /// Cleans up the scene and data after session playback, also restores the world origin / terrain load state to as it was before playback
        /// </summary>
        public static void AfterSessionPlaybackCleanup()
        {
            GaiaSessionManager sessionManager = GetSessionManager();

            //empty the lists for the regen terrains - these should be handled by now, regardless if this was a regen run or a full session playback
            sessionManager.m_terrainNamesFlaggedForRegeneration.Clear();
            sessionManager.m_terrainNamesFlaggedForRegenerationDeactivation.Clear();

            m_regenerateRun = false;

            //Remove all temporary tools after session playback is complete
            DestroyTempSessionTools();
            Resources.UnloadUnusedAssets();
            sessionManager.RestoreOriginAndLoadRange();

        }


        /// <summary>
        /// Destroys temporary session tools used during session / operation playback
        /// </summary>
        public static void DestroyTempSessionTools()
        {
            GameObject tempToolsGo = GaiaUtils.GetTempSessionToolsObject();
            DestroyImmediate(tempToolsGo);
        }

        /// <summary>
        /// Loads the given session into the manager in the scene
        /// </summary>
        /// <param name="session">The session to load.</param>
        public static void LoadSession(GaiaSession session = null)
        {
            GaiaSessionManager sessionManager = GaiaSessionManager.GetSessionManager();
            if (session != null)
            {
                sessionManager.m_session = session;
            }
#if UNITY_EDITOR
            Selection.activeGameObject = sessionManager.gameObject;
#endif
        }

        /// <summary>
        /// Adds a stamping operation to the session and optionally executes it right away.
        /// </summary>
        /// <param name="stamperSettings">The Stamper settings representing the stamping operation that should be added to the session</param>
        /// <param name="executeNow">Whether the stamping operation should be executed right away or not</param>
        /// <param name="stamper">A stamper which should perform the stamping if the operation is executed right away.</param>
        /// <param name="massStamp">Whether this is a "mass stamping" for world generation - turns off Undo recording in this case.</param>
        public static void Stamp(StamperSettings stamperSettings, bool executeNow = true, Stamper stamper = null, bool massStamp = false)
        {
            GaiaStopwatch.StartEvent("Stamping Session Manager");
            if (stamperSettings == null)
            {
                Debug.LogError("Trying to stamp but stamper settings are null!");
                return;
            }

            //Get the current session manager instance from the scene
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();

            //We need a session to do anything
            if (sessionMgr == null)
            {
                Debug.LogError("Could not find or create a Session Manager in the scene. A session manager is required for terrain creation.");
                return;
            }

            if (!sessionMgr.ChangeAllowed())
            {
                return;
            }
            //Copy the stamper settings in the operation for serialization
            stamperSettings.ClearImageMaskTextures();
            StamperSettings newSettings = Instantiate(stamperSettings);

            GaiaOperation op = new GaiaOperation();
            op.m_operationType = GaiaOperation.OperationType.Stamp;
            //create the saved version of the scriptable asset
            string scriptableObjectPath = sessionMgr.SaveOperationData(op, newSettings);
#if UNITY_EDITOR
            EditorUtility.SetDirty(newSettings);
#endif
            sessionMgr.AddOperation(op);
            //Play the operation
            if (executeNow)
            {
                sessionMgr.BackupOriginAndLoadRange();
                m_regenerateRun = false;
                //if we are about to stamp, make sure terrain loading is enabled in the session manager
                //GaiaTerrainLoaderManager.Instance.SwitchToLocalMap();

                if (stamper != null)
                {
                    ExecuteStamp(op, stamper.gameObject, massStamp);
                }
                else
                {
                    ExecuteStamp(op, null, massStamp);
                }
            }
            GaiaStopwatch.EndEvent("Stamping Session Manager");

        }

        private bool ChangeAllowed()
        {
            //Is this session locked? If yes, we can't change it
            if (IsLocked() == true)
            {
#if UNITY_EDITOR
                if (EditorUtility.DisplayDialog("Session Locked", "The current session is locked, which prevents changes from being made in the scene. Do you want to unlock the current session for changes to continue the operation?", "Unlock Session", "Abort"))
                {
                    m_session.m_isLocked = false;
                }
#endif
            }
            return !m_session.m_isLocked;
        }

        /// <summary>
        /// Exports the world map height map to the local map chunks
        /// </summary>
        /// <param name="wcs"></param>
        /// <param name="v"></param>
        public static void ExportWorldMapToLocalMap(WorldCreationSettings worldCreationSettings, bool executeNow)
        {

            //Get the current session manager instance from the scene
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();

            if (!sessionMgr.ChangeAllowed())
            {
                return;
            }

            WorldCreationSettings newWorldCreationSettings = Instantiate(worldCreationSettings);

            GaiaOperation op = new GaiaOperation();
            op.m_operationType = GaiaOperation.OperationType.ExportWorldMapToLocalMap;
            //create the saved version of the scriptable asset
            string scriptableObjectPath = sessionMgr.SaveOperationData(op, newWorldCreationSettings);
#if UNITY_EDITOR
            EditorUtility.SetDirty(newWorldCreationSettings);
#endif
            sessionMgr.AddOperation(op);

            //Play the operation
            if (executeNow)
            {
                ExecuteExportWorldMapToLocalMap(newWorldCreationSettings);
            }
        }


#if GAIA_PRO_PRESENT
        /// <summary>
        /// Creates a mask map export operation in the setting which can be executed right away.
        /// </summary>
        /// <param name="exportMaskMapOperationSettings">An export settings object for the mask map export.</param>
        /// <param name="executeNow">If the export should be executed directly as well.</param>
        /// <param name="maskMapExport">A mask map exporter to execute this operation with.</param>
        public static void ExportMaskMap(ExportMaskMapOperationSettings exportMaskMapOperationSettings, bool executeNow, MaskMapExport maskMapExport)
        {
            //Get the current session manager instance from the scene
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();

            //We need a session to do anything
            if (sessionMgr == null)
            {
                Debug.LogError("Could not find or create a Session Manager in the scene. A session manager is required for this operation.");
                return;
            }

            //Is this session locked? If yes, we can't change it
            if (sessionMgr.IsLocked() == true)
            {
                Debug.LogError("The current session is locked for changes, could not export the mask map in this session");
                return;
            }

            ExportMaskMapOperationSettings exportSettings = Instantiate(exportMaskMapOperationSettings);

            GaiaOperation op = new GaiaOperation();
            op.m_operationType = GaiaOperation.OperationType.MaskMapExport;
            //create the saved version of the scriptable asset
            string scriptableObjectPath = sessionMgr.SaveOperationData(op, exportSettings);

            if (exportSettings.m_maskMapExportSettings != null)
            {
                exportSettings.m_maskMapExportSettings = Instantiate(exportSettings.m_maskMapExportSettings);
                exportSettings.m_maskMapExportSettings.name = exportSettings.m_maskMapExportSettings.name.Replace("(Clone)", "");
#if UNITY_EDITOR
                AssetDatabase.AddObjectToAsset(exportSettings.m_maskMapExportSettings, scriptableObjectPath);
#endif
            }
#if UNITY_EDITOR
            EditorUtility.SetDirty(exportSettings);
#endif

            sessionMgr.AddOperation(op);

            if (executeNow)
            {
                sessionMgr.BackupOriginAndLoadRange();
                m_regenerateRun = false;
                ExecuteMaskMapExport(op, maskMapExport);
            }

        }
#endif
        /// <summary>
        /// Creates a flatten terrain operation in the session, can optionally be executed right away
        /// </summary>
        /// <param name="terrainNames">A list of terrain names that the flattening should be applied to. Leave null for "All terrains".</param>
        /// <param name="executeNow"></param>
        public static void FlattenTerrain(List<string> terrainNames, bool executeNow)
        {
            //Get the current session manager instance from the scene
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();

            //We need a session to do anything
            if (sessionMgr == null)
            {
                Debug.LogError("Could not find or create a Session Manager in the scene. A session manager is required for terrain creation.");
                return;
            }

            //Is this session locked? If yes, we can't change it
            if (!sessionMgr.ChangeAllowed())
            {
                return;
            }

            FlattenOperationSettings flattenSettings = ScriptableObject.CreateInstance<FlattenOperationSettings>();

            flattenSettings.m_TerrainsList = terrainNames;

            GaiaOperation op = new GaiaOperation();
            op.m_operationType = GaiaOperation.OperationType.FlattenTerrain;
            //create the saved version of the scriptable asset
            string scriptableObjectPath = sessionMgr.SaveOperationData(op, flattenSettings);
#if UNITY_EDITOR
            EditorUtility.SetDirty(flattenSettings);
#endif

            sessionMgr.AddOperation(op);

            if (executeNow)
            {
                sessionMgr.BackupOriginAndLoadRange();
                m_regenerateRun = false;
                ExecuteFlatten(op);
            }
        }


        /// <summary>
        /// Creates an operation for the removal of "non-biome" resources - Removing trees etc. in the biome area that are not part of the biome itself.
        /// </summary>
        ///<param name="removeNonBiomeResourcesSettings">The settings for the removal operation</param>
        ///<param name="executeNow">The settings for the removal operation</param>
        ///<param name="biomeController">The settings for the removal operation</param>
        public static void RemoveNonBiomeResources(RemoveNonBiomeResourcesSettings removeNonBiomeResourcesSettings, bool executeNow, BiomeController biomeController = null)
        {
            //Get the current session manager instance from the scene
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();

            //We need a session to do anything
            if (sessionMgr == null)
            {
                Debug.LogError("Could not find or create a Session Manager in the scene. A session manager is required for terrain creation.");
                return;
            }

            //Is this session locked? If yes, we can't change it
            if (!sessionMgr.ChangeAllowed())
            {
                return;
            }

            RemoveNonBiomeResourcesSettings newRemoveSettings = Instantiate(removeNonBiomeResourcesSettings);

            GaiaOperation op = new GaiaOperation();
            op.m_operationType = GaiaOperation.OperationType.RemoveNonBiomeResources;
            //create the saved version of the scriptable asset
            string scriptableObjectPath = sessionMgr.SaveOperationData(op, newRemoveSettings);

            if (newRemoveSettings.m_biomeControllerSettings != null)
            {
                newRemoveSettings.m_biomeControllerSettings = Instantiate(newRemoveSettings.m_biomeControllerSettings);
                newRemoveSettings.m_biomeControllerSettings.name = newRemoveSettings.m_biomeControllerSettings.name.Replace("(Clone)", "");
#if UNITY_EDITOR
                AssetDatabase.AddObjectToAsset(newRemoveSettings.m_biomeControllerSettings, scriptableObjectPath);
#endif
            }

            for (int i = 0; i < newRemoveSettings.m_spawnerSettingsList.Count; i++)
            {
                newRemoveSettings.m_spawnerSettingsList[i] = Instantiate(newRemoveSettings.m_spawnerSettingsList[i]);
                newRemoveSettings.m_spawnerSettingsList[i].name = newRemoveSettings.m_spawnerSettingsList[i].name.Replace("(Clone)", "");
#if UNITY_EDITOR
                AssetDatabase.AddObjectToAsset(newRemoveSettings.m_spawnerSettingsList[i], scriptableObjectPath);
#endif
            }
#if UNITY_EDITOR
            EditorUtility.SetDirty(newRemoveSettings);
#endif

            sessionMgr.AddOperation(op);

            if (executeNow)
            {
                sessionMgr.BackupOriginAndLoadRange();
                m_regenerateRun = false;
                ExecuteRemoveNonBiomeResources(op, biomeController);
            }
        }

        /// <summary>
        /// Clears all the actual terrains from the world. The world map will remain intact
        /// </summary>
        /// <param name="executeNow">If this operation should be executed right after being added to the session.</param>
        public static void ClearWorld(bool executeNow)
        {
            //Get the current session manager instance from the scene
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();

            //We need a session to do anything
            if (sessionMgr == null)
            {
                Debug.LogError("Could not find or create a Session Manager in the scene. A session manager is required for terrain creation.");
                return;
            }

            //Is this session locked? If yes, we can't change it
            if (!sessionMgr.ChangeAllowed())
            {
                return;
            }

            GaiaOperation op = new GaiaOperation();
            op.m_operationType = GaiaOperation.OperationType.ClearWorld;
            op.m_isActive = true;
            op.m_operationDateTime = DateTime.Now.ToString();
            op.m_description = "Clear World";

            sessionMgr.AddOperation(op);

            if (executeNow)
            {
                ExecuteClearWorld();
            }

        }


        /// <summary>
        /// Creates a clear spawns operation in the session and optionally executes it right away
        /// </summary>
        /// <param name="clearOperationSettings">The settings that define what and where will be cleared.</param>
        /// <param name="spawnerSettings">Optional spawner settings, required only if the clearing should only delete the resources contained within these spawner settings.</param>
        /// <param name="executeNow">If this operation should be executed right after being added to the session.</param>
        /// <param name="spawner">A reference to a spawner to execute the deletion from.</param>
        public static void ClearSpawns(ClearOperationSettings clearOperationSettings, SpawnerSettings spawnerSettings = null, bool executeNow = true, Spawner spawner = null)
        {
            //Get the current session manager instance from the scene
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();

            //We need a session to do anything
            if (sessionMgr == null)
            {
                Debug.LogError("Could not find or create a Session Manager in the scene. A session manager is required for terrain creation.");
                return;
            }

            //Is this session locked? If yes, we can't change it
            if (!sessionMgr.ChangeAllowed())
            {
                return;
            }

            ClearOperationSettings newClearOperationSetting = Instantiate(clearOperationSettings);

            GaiaOperation op = new GaiaOperation();
            op.m_operationType = GaiaOperation.OperationType.ClearSpawns;
            //create the saved version of the scriptable asset
            string scriptableObjectPath = sessionMgr.SaveOperationData(op, newClearOperationSetting);

            if (spawnerSettings != null)
            {
                SpawnerSettings newSpawnerSettings = Instantiate(spawnerSettings);
                newClearOperationSetting.m_spawnerSettings = newSpawnerSettings;
#if UNITY_EDITOR
                AssetDatabase.AddObjectToAsset(newSpawnerSettings, scriptableObjectPath);
#endif
            }
#if UNITY_EDITOR
            EditorUtility.SetDirty(newClearOperationSetting);
#endif

            sessionMgr.AddOperation(op);

            if (executeNow)
            {
                sessionMgr.BackupOriginAndLoadRange();
                m_regenerateRun = false;
                ExecuteClearSpawns(op);
            }

        }


        /// <summary>
        /// Creates an Undo operation in the session, can optionally be executed right away
        /// </summary>
        /// <param name="terrainNames">A list of terrain names that need to be loaded for the undo being performed correctly.</param>
        /// <param name="executeNow"></param>
        public static void StampUndo(List<string> terrainNames, bool executeNow = true, Stamper stamper = null)
        {
            StampUndoRedo(false, terrainNames, executeNow, stamper);
        }


        /// <summary>
        /// Creates an Redo operation in the session, can optionally be executed right away
        /// </summary>
        /// <param name="terrainNames">A list of terrain names that need to be loaded for the redo being performed correctly.</param>
        /// <param name="executeNow"></param>
        public static void StampRedo(List<string> terrainNames, bool executeNow = true, Stamper stamper = null)
        {
            StampUndoRedo(true, terrainNames, executeNow, stamper);

        }


        private static void StampUndoRedo(bool isRedo, List<string> terrainNames, bool executeNow = true, Stamper stamper = null)
        {
            //Get the current session manager instance from the scene
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();

            //We need a session to do anything
            if (sessionMgr == null)
            {
                Debug.LogError("Could not find or create a Session Manager in the scene. A session manager is required for terrain creation.");
                return;
            }

            //Is this session locked? If yes, we can't change it
            if (!sessionMgr.ChangeAllowed())
            {
                return;
            }

            UndoRedoOperationSettings undoRedoSettings = ScriptableObject.CreateInstance<UndoRedoOperationSettings>();

            undoRedoSettings.m_TerrainsList = terrainNames;

            GaiaOperation op = new GaiaOperation();
            if (isRedo)
            {
                op.m_operationType = GaiaOperation.OperationType.StampRedo;
            }
            else
            {
                op.m_operationType = GaiaOperation.OperationType.StampUndo;
            }
            //create the saved version of the scriptable asset
            string scriptableObjectPath = sessionMgr.SaveOperationData(op, undoRedoSettings);
#if UNITY_EDITOR
            EditorUtility.SetDirty(undoRedoSettings);
#endif

            sessionMgr.AddOperation(op);

            if (executeNow)
            {
                m_regenerateRun = false;
                ExecuteStampUndoRedo(isRedo, op, stamper);
            }
        }


        /// <summary>
        /// Logs an entry from an external application in the session.
        /// </summary>
        /// <param name="scriptableObject"></param>
        /// <param name="actionToExecute"></param>
        /// <param name="executeNow"></param>
        public static void ExternalSessionEntry(ScriptableObject scriptableObject, Action<ScriptableObject> actionToExecute, bool executeNow = true)
        {
            //Get the current session manager instance from the scene
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();

            //Copy the operation settings in the operation for serialization
            ScriptableObject newSOSettings = Instantiate(scriptableObject);

            GaiaOperation op = new GaiaOperation();
            op.m_operationType = GaiaOperation.OperationType.External;
            //create the saved version of the scriptable asset
            string scriptableObjectPath = sessionMgr.SaveOperationData(op, newSOSettings);
#if UNITY_EDITOR
            EditorUtility.SetDirty(newSOSettings);
#endif
            op.m_serializedExternalAction = SerializeExternalAction(actionToExecute);

            sessionMgr.AddOperation(op);
            //Play the operation
            if (executeNow)
            {
                sessionMgr.BackupOriginAndLoadRange();
                m_regenerateRun = false;
                actionToExecute(newSOSettings);
            }
        }

        /// <summary>
        /// Creates a spawning operation in the session with a list of spawners in the given bounds area, can optionally be executed right away
        /// </summary>
        /// <param name="spawnOperationSettings">The settings for this spawning operation</param>
        /// <param name="executeNow">Whether this operation should be executed right after storing it in the session</param>
        /// <param name="spawnerList">Optional list of spawners that should execute the spawning, those need to match the spanwer settings provided in the SpawnOperationSettings.</param>
        public static void Spawn(SpawnOperationSettings spawnOperationSettings, bool executeNow = true, List<Spawner> spawnerList = null)
        {
            GaiaStopwatch.StartEvent("Session Manager Spawn");
            if (spawnOperationSettings == null)
            {
                Debug.Log("Trying to spawn without a spawner settings object!");
                return;
            }

            if (spawnOperationSettings.m_spawnerSettingsList == null)
            {
                Debug.LogError("Trying to spawn without a spawner list!");
                return;
            }

            if (spawnOperationSettings.m_spawnArea == null)
            {
                Debug.LogError("Trying to spawn without a spawn Area being set!");
                return;
            }

            //Get the current session manager instance from the scene
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();

            //We need a session to do anything
            if (sessionMgr == null)
            {
                Debug.LogError("Could not find or create a Session Manager in the scene. A session manager is required for terrain creation.");
                return;
            }

            //Is this session locked? If yes, we can't change it
            if (!sessionMgr.ChangeAllowed())
            {
                return;
            }

            //Make sure the global spawn density is set up according to the session for all spawners.
            for (int i = 0; i < spawnOperationSettings.m_spawnerSettingsList.Count; i++)
            {
                spawnOperationSettings.m_spawnerSettingsList[i].m_spawnDensity = sessionMgr.m_session.m_spawnDensity;
            }

            //Copy the spawner operation settings in the operation for serialization
            SpawnOperationSettings newSOSettings = Instantiate(spawnOperationSettings);
            //newSOSettings.m_spawnArea = new BoundsDouble(spawnOperationSettings.m_spawnArea.center, spawnOperationSettings.m_spawnArea.size);

            //foreach (SpawnerSettings spawnerSettings in spawnOperationSettings.m_spawnerSettingsList)
            //{
            //    SpawnerSettings newSettings

            //    //Image masks are their own objects and need to be cloned into the new settings file
            //    //otherwise subsequent spawns can  overwrite the image mask session data of previous stampings.
            //    newSettings.m_imageMasks = new ImageMask[stamperSettings.m_imageMasks.Length];
            //    for (int i = 0; i < newSettings.m_imageMasks.Length; i++)
            //    {
            //        newSettings.m_imageMasks[i] = ImageMask.Clone(stamperSettings.m_imageMasks[i]);
            //    }
            //}

            GaiaOperation op = new GaiaOperation();
            op.m_operationType = GaiaOperation.OperationType.Spawn;
            //create the saved version of the scriptable asset
            string scriptableObjectPath = sessionMgr.SaveOperationData(op, newSOSettings);

            //Check the spawner settings contained in list - we need to create a deep copy of each and
            //serialize those spawner settings under the session entry as an individual object, not as a reference to the user made
            //spawner in the asset hierarchy
            if (newSOSettings.m_spawnerSettingsList != null)
            {
                for (int i = 0; i < newSOSettings.m_spawnerSettingsList.Count; i++)
                {
                    newSOSettings.m_spawnerSettingsList[i] = Instantiate(newSOSettings.m_spawnerSettingsList[i]);
                    newSOSettings.m_spawnerSettingsList[i].name = newSOSettings.m_spawnerSettingsList[i].name.Replace("(Clone)", "");
#if UNITY_EDITOR
                    AssetDatabase.AddObjectToAsset(newSOSettings.m_spawnerSettingsList[i], scriptableObjectPath);
#endif
                }
            }

            if (newSOSettings.m_biomeControllerSettings != null)
            {
                newSOSettings.m_biomeControllerSettings = Instantiate(newSOSettings.m_biomeControllerSettings);
                newSOSettings.m_biomeControllerSettings.name = newSOSettings.m_biomeControllerSettings.name.Replace("(Clone)", "");
#if UNITY_EDITOR
                AssetDatabase.AddObjectToAsset(newSOSettings.m_biomeControllerSettings, scriptableObjectPath);
#endif
            }
#if UNITY_EDITOR
            EditorUtility.SetDirty(newSOSettings);
#endif

            sessionMgr.AddOperation(op);

            //Play the operation
            if (executeNow)
            {
                sessionMgr.BackupOriginAndLoadRange();
                m_regenerateRun = false;
                if (spawnerList != null)
                {
                    ExecuteSpawn(op, spawnerList);
                }
                else
                {
                    ExecuteSpawn(op, null);
                }
            }
            GaiaStopwatch.EndEvent("Session Manager Spawn");
        }

        /// <summary>
        /// Playback a session as a co-routine
        /// </summary>
        public IEnumerator PlaySessionCoRoutine()
        {
            //Debug.Log("Playing session " + m_session.m_name);

            m_progress = 0;

            if (Application.isPlaying)
            {
                for (int idx = 0; idx < m_session.m_operations.Count; idx++)
                {
                    if (!m_cancelPlayback)
                    {
                        if (m_session.m_operations[idx].m_isActive)
                        {
                            yield return StartCoroutine(PlayOperationCoRoutine(idx, false));
                        }
                    }
                }
            }
            else
            {
                for (int idx = 0; idx < m_session.m_operations.Count; idx++)
                {
                    if (!m_cancelPlayback)
                    {
                        if (m_session.m_operations[idx].m_isActive)
                        {
                            m_updateOperationCoroutine = PlayOperationCoRoutine(idx, false);
                            yield return new WaitForSeconds(0.001f);
                        }
                    }
                }
            }
            if (m_currentStamper != null)
            {
                m_currentStamper.SyncHeightmaps();
            }

#if UNITY_EDITOR
            SceneView.RepaintAll();
#endif

            Debug.Log("Finished playing session " + m_session.m_name);

            m_updateSessionCoroutine = null;
        }


        /// <summary>
        /// Playback an operation - kicks off the coroutine
        /// </summary>
        /// <param name="opIdx"></param>
        public void PlayOperation(int opIdx)
        {
            m_cancelPlayback = false;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                m_updateOperationCoroutine = PlayOperationCoRoutine(opIdx, true);
                StartEditorUpdates();
            }
            else
            {
                StartCoroutine(PlayOperationCoRoutine(opIdx, true));
            }
#else
            StartCoroutine(PlayOperationCoRoutine(opIdx, true));
#endif
        }

        /// <summary>
        /// Plays back an operation as a co-routine
        /// </summary>
        /// <param name="operationIdx"></param>
        /// <returns></returns>
        public IEnumerator PlayOperationCoRoutine(int operationIdx, bool syncHeightmapsWhenStamping)
        {
            ////Check operation index
            //if (operationIdx < 0 || operationIdx >= m_session.m_operations.Count)
            //{
            //    Debug.LogWarning(string.Format("Operation index {0} is out of bounds.", operationIdx));
            //    m_updateOperationCoroutine = null;
            //    yield break;
            //}

            ////Check if active
            //if (!m_session.m_operations[operationIdx].m_isActive)
            //{
            //    Debug.LogWarning(string.Format("Operation '{0}' is not active. Ignoring.", m_session.m_operations[operationIdx].m_description));
            //    m_updateOperationCoroutine = null;
            //    yield break;
            //}


            ////Stop this operation from adding more to the session
            //bool lockState = m_session.m_isLocked;
            //m_session.m_isLocked = true;

            ////Grab operation and let world know about it
            //GaiaOperation operation = m_session.m_operations[operationIdx];
            ////Debug.Log("Playing: " + operation.m_description);

            ////Get or create the operation game object, and apply the operation to it
            //GameObject go = Apply(operationIdx);

            ////Now invoke the necessary code to play it
            //Stamper stamper = null;
            //Spawner spawner = null;
            //if (go != null)
            //{
            //    stamper = go.GetComponent<Stamper>();
            //    spawner = go.GetComponent<Spawner>();
            //}

            //switch (operation.m_operationType)
            //{
            //    case GaiaOperation.OperationType.CreateTerrain:
            //        if (Gaia.TerrainHelper.GetActiveTerrainCount() == 0)
            //        {
            //            if (m_session.m_defaults != null && m_session.m_defaults.m_content.GetLength(0) > 0)
            //            {
            //                #if UNITY_EDITOR
            //                GaiaDefaults defaults = GaiaUtils.GetAsset(
            //                    Gaia.ScriptableObjectWrapper.GetSessionedFileName(m_session.m_name, m_session.m_defaults.m_fileName), typeof(Gaia.GaiaDefaults)) 
            //                    as Gaia.GaiaDefaults;
            //                if (defaults == null)
            //                {
            //                    ExportSessionDefaults();
            //                    defaults = GaiaUtils.GetAsset(
            //                        Gaia.ScriptableObjectWrapper.GetSessionedFileName(m_session.m_name, m_session.m_defaults.m_fileName), typeof(Gaia.GaiaDefaults)) 
            //                        as Gaia.GaiaDefaults;
            //                }
            //                if (defaults == null)
            //                {
            //                    Debug.LogWarning("Could not create terrain - unable to locate exported defaults");
            //                }
            //                else
            //                {
            //                    //Now try and locate the resources and pass the into the terrain creation
            //                    if (operation.m_operationDataJson != null && operation.m_operationDataJson.GetLength(0) == 2)
            //                    {
            //                        GaiaResource resources = GaiaUtils.GetAsset(
            //                            Gaia.ScriptableObjectWrapper.GetSessionedFileName(m_session.m_name, operation.m_operationDataJson[1]), typeof(Gaia.GaiaResource)) 
            //                            as Gaia.GaiaResource;
            //                        if (resources == null)
            //                        {
            //                            ExportSessionResource(operation.m_operationDataJson[1]);
            //                            resources = GaiaUtils.GetAsset(
            //                                m_session.GetSessionFileName() + "_" + Path.GetFileName(operation.m_operationDataJson[1]), typeof(Gaia.GaiaResource)) 
            //                                as Gaia.GaiaResource;
            //                        }

            //                        defaults.CreateTerrain();

            //                    }
            //                    else
            //                    {
            //                        defaults.CreateTerrain();
            //                    }
            //                }
            //                #endif
            //            }
            //        }
            //        break;
            //    case GaiaOperation.OperationType.FlattenTerrain:
            //        if (stamper != null)
            //        {
            //            stamper.FlattenTerrain();
            //        }
            //        break;
            //    case GaiaOperation.OperationType.SmoothTerrain:
            //        if (stamper != null)
            //        {
            //            stamper.SmoothTerrain();
            //        }
            //        break;
            //    case GaiaOperation.OperationType.ClearDetails:
            //        if (stamper != null)
            //        {
            //            stamper.ClearDetails();
            //        }
            //        break;
            //    case GaiaOperation.OperationType.ClearTrees:
            //        if (stamper != null)
            //        {
            //            stamper.ClearTrees();
            //        }
            //        break;
            //    case GaiaOperation.OperationType.Stamp:
            //        if (stamper != null)
            //        {
            //            m_currentStamper = stamper;
            //            m_currentSpawner = null;
            //            if (!Application.isPlaying)
            //            {
            //                stamper.HidePreview();
            //                // we will sync heightmaps after all stamps have been applied for performance.
            //                stamper.m_syncHeightmaps = syncHeightmapsWhenStamping;
            //                //make sure the mask textures are rebuilt before stamping
            //                stamper.m_MaskTexturesDirty = true;
            //                stamper.Stamp();
            //                while (stamper.IsStamping())
            //                {
            //                    if ((DateTime.Now - m_lastUpdateDateTime).Milliseconds > 5)
            //                    {
            //                        m_lastUpdateDateTime = DateTime.Now; //Forces an editor refresh
            //                        m_progress++;
            //                    }
            //                    yield return new WaitForSeconds(0.005f);
            //                }
            //            }
            //            else
            //            {
            //               // yield return StartCoroutine(stamper.ApplyStamp());
            //            }
            //        }
            //        break;
            //    case GaiaOperation.OperationType.StampUndo:
            //        if (stamper != null)
            //        {
            //            stamper.Undo();
            //        }
            //        break;
            //    case GaiaOperation.OperationType.StampRedo:
            //        if (stamper != null)
            //        {
            //            stamper.Redo();
            //        }
            //        break;
            //    case GaiaOperation.OperationType.Spawn:
            //        if (spawner!= null)
            //        {
            //            m_currentStamper = null;
            //            m_currentSpawner = spawner;

            //            if (!Application.isPlaying)
            //            {
            //                spawner.RunSpawnerIteration(true);
            //                while (spawner.IsSpawning())
            //                {
            //                    if ((DateTime.Now - m_lastUpdateDateTime).Milliseconds > 250)
            //                    {
            //                        m_lastUpdateDateTime = DateTime.Now; //Forces an editor refresh
            //                        m_progress++;
            //                    }
            //                    yield return new WaitForSeconds(0.2f);
            //                }
            //            }
            //            else
            //            {
            //                //yield return StartCoroutine(spawner.R);
            //            }
            //        }
            //        break;
            //    case GaiaOperation.OperationType.SpawnReset:
            //        break;
            //    default:
            //        break;

            //}

            //Return session lock state to what it was before
            //m_session.m_isLocked = lockState;

            //Signal an end
            //m_updateOperationCoroutine = null;
            yield return null;
        }

        /// <summary>
        /// Cancel the playback
        /// </summary>
        public void CancelPlayback()
        {
            m_cancelPlayback = true;
            if (m_currentStamper != null)
            {
                m_currentStamper.CancelStamp();
            }
            if (m_currentSpawner != null)
            {
                m_currentSpawner.CancelSpawn();
            }
        }


        public static byte[] SerializeExternalAction(Action<ScriptableObject> action)
        {
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            formatter.Serialize(stream, action);
            stream.Position = 0;
            return stream.GetBuffer();
        }

        public static bool ExecuteSerializedExternalAction(ScriptableObject scriptableObject, byte[] serializedAction)
        {
            bool exceptionsCaught = false;
            try
            {
                var formatter = new BinaryFormatter();
                var stream = new MemoryStream(serializedAction);
                var action = (Action<ScriptableObject>)formatter.Deserialize(stream);
                action(scriptableObject);
            }
            catch (Exception ex)
            {
                exceptionsCaught = true;
                Debug.LogError("Error while trying to execute a serialized external action from the session. Message: " + ex.Message + " Stack: " + ex.StackTrace);
            }
            if (exceptionsCaught)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Draw gizmos
        /// </summary>
        void OnDrawGizmosSelected()
        {
            if (m_session != null)
            {
                BoundsDouble bounds = new BoundsDouble();
                if (TerrainHelper.GetTerrainBounds(ref bounds) == true)
                {
                    //Terrain dimensions
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);

                    //Water dimensions
                    if (PWS_WaterSystem.Instance == null)
                    {
                        bounds.center = new Vector3Double(bounds.center.x, m_session.m_seaLevel, bounds.center.z);
                        bounds.size = new Vector3Double(bounds.size.x, 0.05f, bounds.size.z);
                        Gizmos.color = new Color(Color.blue.r, Color.blue.g, Color.blue.b, Color.blue.a / 4f);
                        Gizmos.DrawCube(bounds.center, bounds.size);
                    }
                }
            }
        }


        #region Object location and creation scripts

        /// <summary>
        /// Find or create the object that created this operation
        /// </summary>
        /// <param name="operation">The operation</param>
        /// <returns>The object if possible or null</returns>
        GameObject FindOrCreateObject(GaiaOperation operation)
        {
            //if (operation.m_generatedByType == "Gaia.Stamper")
            //{
            //    //See if we can locate it in the existing stamps
            //    Stamper[] stampers = GameObject.FindObjectsOfType<Stamper>();
            //    for (int stampIdx = 0; stampIdx < stampers.GetLength(0); stampIdx++)
            //    {
            //        if (stampers[stampIdx].m_stampID == operation.m_generatedByID && stampers[stampIdx].name == operation.m_generatedByName)
            //        {
            //            return stampers[stampIdx].gameObject;
            //        }
            //    }
            //    //If we couldnt find this - then add it
            //    return ShowStamper(operation.m_generatedByName, operation.m_generatedByID);
            //}
            //else if (operation.m_generatedByType == "Gaia.Spawner")
            //{
            //    //See if we can locate it in the existing stamps
            //    Spawner[] spawners = GameObject.FindObjectsOfType<Spawner>();
            //    for (int spawnerIdx = 0; spawnerIdx < spawners.GetLength(0); spawnerIdx++)
            //    {
            //        if (spawners[spawnerIdx].m_spawnerID == operation.m_generatedByID && spawners[spawnerIdx].name == operation.m_generatedByName)
            //        {
            //            return spawners[spawnerIdx].gameObject;
            //        }
            //    }
            //    //If we couldnt find this - the add it
            //    return CreateSpawner(operation.m_generatedByName, operation.m_generatedByID);
            //}
            return null;
        }

        /// <summary>
        /// Select or create a stamper
        /// </summary>
        GameObject ShowStamper(string name, string id)
        {
            GameObject gaiaObj = GaiaUtils.GetGaiaGameObject();
            GameObject stamperObj = GameObject.Find(name);
            if (stamperObj == null)
            {
                stamperObj = new GameObject(name);
                stamperObj.transform.parent = gaiaObj.transform;
                Stamper stamper = stamperObj.AddComponent<Stamper>();
                stamper.m_stampID = id;
                stamper.HidePreview();
                stamper.m_seaLevel = m_session.m_seaLevel;
            }
            return stamperObj;
        }

        /// <summary>
        /// Create and show spawner
        /// </summary>
        GameObject CreateSpawner(string name, string id)
        {
            GameObject gaiaObj = GaiaUtils.GetGaiaGameObject();
            GameObject spawnerObj = new GameObject(name);
            spawnerObj.transform.parent = gaiaObj.transform;
            Spawner spawner = spawnerObj.AddComponent<Spawner>();
            spawner.m_spawnerID = id;
            return spawnerObj;
        }

        #endregion

        #region Terrain Min Max Cache
        private void GetTerrainMinMax(TerrainData terrainData, ref float min, ref float max, bool forceRecalculate = false)
        {
#if UNITY_EDITOR
            //Early exit on no terrain
            if (terrainData == null)
                return;

            //Early exit on no session
            if (m_session == null)
                return;

            //Get Terrain Asset Guid
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(terrainData));

            //if min max cache is null we got to create one
            if (m_session.m_terrainMinMaxCache == null)
            {
                m_session.m_terrainMinMaxCache = new List<TerrainMinMaxHeight>();
            }

            TerrainMinMaxHeight tmmh = m_session.m_terrainMinMaxCache.Find(x => x.guid == guid);
            //Did we find a valid entry, does it have the recalculate flag on itself or in the function parameter?
            if (tmmh != null && !tmmh.recalculate && !forceRecalculate)
            {
                //ok, set the ref values and exit
                min = tmmh.min;
                max = tmmh.max;
                return;
            }
            else
            {
                //either no entry or needs to be recalculated
                float newMin = 0f;
                float newMax = 0f;
                ProgressBar.Show(ProgressBarPriority.MinMaxHeightCalculation, "Calculating heigths", "Calculating the min and max height of the game world...", 0, 0, false, false);
                GetMinMaxTerrainHeight(terrainData, ref newMin, ref newMax);
                if (tmmh != null)
                {
                    //Update existing entry in cache
                    tmmh.min = newMin;
                    tmmh.max = newMax;
                    tmmh.recalculate = false;
                }
                else
                {
                    //Create a new cache entry
                    tmmh = new TerrainMinMaxHeight();
                    if (TerrainHelper.IsWorldMapTerrain(terrainData))
                        tmmh.isWorldmap = true;
                    tmmh.min = newMin;
                    tmmh.max = newMax;
                    tmmh.guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(terrainData));
                    m_session.m_terrainMinMaxCache.Add(tmmh);
                }

                min = newMin;
                max = newMax;
                return;
            }
#endif
        }

        private float GetMinMaxTerrainHeight(TerrainData terrainData, ref float min, ref float max)
        {
            if (terrainData == null)
            {
                return 0f;
            }

            if (terrainData == null)
            {
                return 0f;
            }

            //reset the values, we want a "fresh" measurement from the terrain
            min = float.MaxValue;
            max = float.MinValue;

            if (GaiaSettings.m_terrainHeightsComputeShader == null)
            {
                Debug.LogError("Compute shader for terrain min max evaluation missing in Gaia Settings!");
                return 0f;
            }


            ComputeShader shader = GaiaSettings.m_terrainHeightsComputeShader;
            int kernelHandle = shader.FindKernel("CSMain");
            ComputeBuffer buffer = new ComputeBuffer(1, 8);
            buffer.SetData(new TerrainMinMax[1] { new TerrainMinMax() { minHeight = 0.5f, maxHeight = 0 } });
            shader.SetBuffer(kernelHandle, "outputBuffer", buffer);
            shader.SetTexture(kernelHandle, "Input", terrainData.heightmapTexture);
            shader.Dispatch(kernelHandle, terrainData.heightmapTexture.width / 8, terrainData.heightmapTexture.height / 8, 1);

            TerrainMinMax[] tmm = new TerrainMinMax[1];
            buffer.GetData(tmm);

            //Terrain height is expressed from 0 (lowest possible point) to 0.5f (maximum possible height) in the shader
            //we need to map this to proper unity units

            min = Mathf.Lerp(0, terrainData.size.y, Mathf.InverseLerp(0, 0.5f, tmm[0].minHeight));
            max = Mathf.Lerp(0, terrainData.size.y, Mathf.InverseLerp(0, 0.5f, tmm[0].maxHeight));

            buffer.Release();
            return max;
        }



        /// <summary>
        /// Create a terrain tile based on these settings
        /// </summary>
        /// <param name="tx">X location</param>
        /// <param name="tz">Z location</param>
        /// <param name="world">The array managing it</param>
        private static GameObject CreateTile(int tx, int tz, WorldCreationSettings worldCreationSettings, ref Terrain[,] world, TerrainLayer[] terrainLayers, DetailPrototype[] terrainDetails, TreePrototype[] terrainTrees)
        {
            GaiaStopwatch.StartEvent("Terrain Tile Creation");
            if (tx < 0 || tx >= worldCreationSettings.m_xTiles)
            {
                Debug.LogError("X value out of bounds");
                return null;
            }

            if (tz < 0 || tz >= worldCreationSettings.m_zTiles)
            {
                Debug.LogError("Z value out of bounds");
                return null;
            }

            if (worldCreationSettings.m_gaiaDefaults == null)
            {
                Debug.LogError("Could not find or create valid Gaia Defaults for world creation. Please check the current defaults set up in the Gaia Settings.");
                return null;
            }

            GaiaDefaults gaiaDefaults = worldCreationSettings.m_gaiaDefaults;

            Vector2 m_offset = Vector2.zero;


            //if (worldCreationSettings.m_createInScene)
            //{
            //    //When creating multiple terrains in external scene, positioning does not matter anymore, it is up to the terrain loader to position the terrain correctly according to the current world location then
            //    m_offset = new Vector2(-worldCreationSettings.m_tileSize * 0.5f, -worldCreationSettings.m_tileSize * 0.5f);
            //}
            //else
            //{
            //this will center terrain / world  at origin
            m_offset = new Vector2(-worldCreationSettings.m_tileSize * worldCreationSettings.m_xTiles * 0.5f, -worldCreationSettings.m_tileSize * worldCreationSettings.m_zTiles * 0.5f);
            //}



            //apply additional offset, if any
            m_offset += worldCreationSettings.m_centerOffset;


            //create the terrains if they dont already exist
            if (world.Length < worldCreationSettings.m_xTiles)
            {
                world = new Terrain[worldCreationSettings.m_xTiles, worldCreationSettings.m_zTiles];
            }

            //Create the terrain
            Terrain terrain;
            TerrainData terrainData = new TerrainData();
            if (worldCreationSettings.m_isWorldMap)
            {
                terrainData.name = string.Format(GaiaConstants.worldMapTerrainPrefix + "_{0}_{1}-{2}", tx, tz, worldCreationSettings.m_dateTimeString);
            }
            else
            {
                terrainData.name = string.Format("Terrain_{0}_{1}-{2}", tx, tz, worldCreationSettings.m_dateTimeString);
            }
            terrainData.alphamapResolution = gaiaDefaults.m_controlTextureResolution;
            terrainData.baseMapResolution = gaiaDefaults.m_baseMapSize;
            terrainData.SetDetailResolution(gaiaDefaults.m_detailResolution, gaiaDefaults.m_detailResolutionPerPatch);
            terrainData.heightmapResolution = gaiaDefaults.m_heightmapResolution;
            terrainData.wavingGrassAmount = gaiaDefaults.m_bending;
            terrainData.wavingGrassSpeed = gaiaDefaults.m_size;
            terrainData.wavingGrassStrength = gaiaDefaults.m_speed;
            terrainData.wavingGrassTint = gaiaDefaults.m_grassTint;
            terrainData.size = new Vector3(worldCreationSettings.m_tileSize, worldCreationSettings.m_tileHeight, worldCreationSettings.m_tileSize);

#if UNITY_EDITOR
            AssetDatabase.CreateAsset(terrainData, GaiaDirectories.GetTerrainDataScenePath() + "/" + terrainData.name + ".asset");
            AssetDatabase.ImportAsset(GaiaDirectories.GetTerrainDataScenePath() + "/" + terrainData.name + ".asset");
#endif

            terrain = Terrain.CreateTerrainGameObject(terrainData).GetComponent<Terrain>();
            terrain.name = terrainData.name;
            terrain.transform.position =
            new Vector3(worldCreationSettings.m_tileSize * tx + m_offset.x, 0, worldCreationSettings.m_tileSize * tz + m_offset.y);
            terrain.basemapDistance = gaiaDefaults.m_baseMapDist;
#if UNITY_2019_1_OR_NEWER
            terrain.shadowCastingMode = gaiaDefaults.m_shadowCastingMode;
#else
            terrain.castShadows = m_castShadows;
#endif
            terrain.detailObjectDensity = gaiaDefaults.m_detailDensity;

            TerrainDetailOverwrite detailOverwrite = terrain.GetComponent<TerrainDetailOverwrite>();
            if (detailOverwrite == null)
            {
                detailOverwrite = terrain.gameObject.AddComponent<TerrainDetailOverwrite>();
                detailOverwrite.m_detailDistance = gaiaDefaults.m_detailDistance;
                detailOverwrite.m_detailDensity = gaiaDefaults.m_detailDensity;

                if (gaiaDefaults.m_detailResolutionPerPatch == 2)
                {
                    detailOverwrite.m_detailQuality = GaiaConstants.TerrainDetailQuality.Ultra2;
                }
                else if (gaiaDefaults.m_detailResolutionPerPatch == 4)
                {
                    detailOverwrite.m_detailQuality = GaiaConstants.TerrainDetailQuality.VeryHigh4;
                }
                else if (gaiaDefaults.m_detailResolutionPerPatch == 8)
                {
                    detailOverwrite.m_detailQuality = GaiaConstants.TerrainDetailQuality.High8;
                }
                else if (gaiaDefaults.m_detailResolutionPerPatch == 16)
                {
                    detailOverwrite.m_detailQuality = GaiaConstants.TerrainDetailQuality.Medium16;
                }
                else if (gaiaDefaults.m_detailResolutionPerPatch == 32)
                {
                    detailOverwrite.m_detailQuality = GaiaConstants.TerrainDetailQuality.Low32;
                }
                else
                {
                    detailOverwrite.m_detailQuality = GaiaConstants.TerrainDetailQuality.VeryLow64;
                }
            }
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            terrain.detailObjectDistance = gaiaDefaults.m_detailDistance;
            terrain.heightmapPixelError = gaiaDefaults.m_pixelError;
            terrain.preserveTreePrototypeLayers = true;
            terrain.treeBillboardDistance = gaiaDefaults.m_billboardStart;
            terrain.treeCrossFadeLength = gaiaDefaults.m_fadeLength;
            terrain.treeDistance = gaiaDefaults.m_treeDistance;
            terrain.treeMaximumFullLODCount = gaiaDefaults.m_maxMeshTrees;
#if UNITY_EDITOR
            GameObjectUtility.SetStaticEditorFlags(terrain.gameObject,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic |
                StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic |
#if (UNITY_2019_2_OR_NEWER)
                StaticEditorFlags.OffMeshLinkGeneration | StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.ContributeGI
#else
                StaticEditorFlags.OffMeshLinkGeneration | StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.LightmapStatic
#endif
                );
            terrain.bakeLightProbesForTrees = false;
#if UNITY_2018_3_OR_NEWER
            terrain.drawInstanced = true;
#endif
#endif
            if (gaiaDefaults.m_material != null)
            {
                terrain.materialTemplate = gaiaDefaults.m_material;
            }

            if (gaiaDefaults.m_physicsMaterial != null)
            {
                TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
                if (collider != null)
                {
                    collider.material = gaiaDefaults.m_physicsMaterial;
                }
                else
                {
                    Debug.LogWarning("Unable to assign physics material to terrain!");
                }
            }

            //Assign prototypes
            GaiaDefaults.ApplyPrototypesToTerrain(terrain, terrainLayers, terrainDetails, terrainTrees);

            //Save the new tile
            world[tx, tz] = terrain;

            GaiaStopwatch.EndEvent("Terrain Tile Creation");
            return terrain.gameObject;
        }

        #endregion

        #region Static API Calls
        /// <summary>
        /// Creates a session entry for world / terrain generation in the session and (optionally) executes it right away. 
        /// </summary>
        /// <param name="newWorldCreationSettings">The world creation settings for the new world.</param>
        /// <param name="executeNow">Controls if the creation should be excuted right away as well.</param>
        public static void CreateWorld(WorldCreationSettings worldCreationSettings, bool executeNow = true)
        {
            GaiaStopwatch.StartEvent("Session Manager Create World");

            //Get the current session manager instance from the scene
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();


            if (!CheckIfSceneIsSaved(worldCreationSettings))
            {
                return;
            }
#if UNITY_EDITOR
            //Add project settings for terrain streaming
            if (worldCreationSettings.m_createInScene)
            {
                if (!GarbageCollector.isIncremental)
                {
                    if (GaiaUtils.DisplayDialogNoEditor("Activate Incremental Garbage Collection?", "Incremental Garbage collection is currently turned off in your project. Do you want Gaia to activate this setting now? (Recommended) Incremental Garbage Collection helps to reduce loading performance spikes when loading terrain scenes during runtime.", "Yes", "No", false))
                    {
                        PlayerSettings.gcIncremental = true;
                    }
                }

                if (!QualitySettings.streamingMipmapsActive)
                {
                    if (GaiaUtils.DisplayDialogNoEditor("Activate Texture Streaming?", "Texture Streaming is currently turned off in your project. Do you want Gaia to activate this setting now? (Recommended) Texture streaming helps to reduce overall GPU memory load and also helps to reduce loading spikes when loading terrain scenes during runtime.", "Yes", "No", false))
                    {
                        QualitySettings.streamingMipmapsActive = true;
                    }
                }
            }
#endif

            //We need a session to do anything
            if (sessionMgr == null)
            {
                Debug.LogError("Could not find or create a Session Manager in the scene. A session manager is required for terrain creation.");
                return;
            }

            //Is this session locked? If yes, we can't change it
            if (!sessionMgr.ChangeAllowed())
            {
                return;
            }
            //Check / Get the defaults - if no defaults were supplied we need to get them from the gaia settings so they will be serialized with the session entry as well
            if (worldCreationSettings.m_gaiaDefaults == null)
            {
                GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
                worldCreationSettings.m_gaiaDefaults = gaiaSettings.m_currentDefaults;
            }

            //Copy the world creation Settings so we have a separate object for serialization
            WorldCreationSettings newWorldCreationSettings = Instantiate(worldCreationSettings);

            //Defaults need to be re-instantiated as well as new object
            newWorldCreationSettings.m_gaiaDefaults = Instantiate(worldCreationSettings.m_gaiaDefaults);
            newWorldCreationSettings.m_gaiaDefaults.name = "Gaia Defaults";
            if (newWorldCreationSettings.m_gaiaDefaults == null)
            {
                Debug.LogError("Could not find or create valid Gaia Defaults for world creation. Please check the current defaults set up in the Gaia Settings.");
                return;
            }

            newWorldCreationSettings.m_dateTimeString = String.Format("{0:yyyyMMdd - HHmmss}", DateTime.Now);

            //Fix world creation / default settings
            string warnings = newWorldCreationSettings.CheckSettings(worldCreationSettings.m_isWorldMap);
            if (warnings != "")
            {
                Debug.LogWarning(warnings);
            }

            //Check the spawners contained in the biome spawner list - if there are spawners provided, we need to create a deep copy 
            //to serialize those spawner settings with the session entry as an individual object, not as a reference
            //We also need to build up a new list in the process as well - keeping the original list is dangerous as it could overwrite the
            //original list entries in the biome itself!
            List<BiomeSpawnerListEntry> clonedList = new List<BiomeSpawnerListEntry>();


            if (newWorldCreationSettings.m_spawnerPresetList != null)
            {
                foreach (BiomeSpawnerListEntry entry in newWorldCreationSettings.m_spawnerPresetList)
                {
                    if (entry.m_spawnerSettings != null)
                    {
                        SpawnerSettings spawnerSettings = Instantiate(entry.m_spawnerSettings);
                        spawnerSettings.name = entry.m_spawnerSettings.name;
                        clonedList.Add(new BiomeSpawnerListEntry() { m_isActiveInStamper = entry.m_isActiveInStamper, m_isActiveInBiome = entry.m_isActiveInBiome, m_autoAssignPrototypes = entry.m_autoAssignPrototypes, m_spawnerSettings = spawnerSettings });
                    }
                }
            }
            //work with the cloned list from now on.
            newWorldCreationSettings.m_spawnerPresetList = clonedList;

            //Add the world creation operation to the session
            GaiaOperation op = new GaiaOperation();
            op.m_operationType = GaiaOperation.OperationType.CreateWorld;

            //create the saved version of the scriptable asset
            string scriptableObjectPath = sessionMgr.SaveOperationData(op, newWorldCreationSettings);


            //Add the defaults as sub-object to the session entry
#if UNITY_EDITOR
            AssetDatabase.AddObjectToAsset(newWorldCreationSettings.m_gaiaDefaults, scriptableObjectPath);
#endif
            //Add each spawner settings as sub-object to the session entry
            if (newWorldCreationSettings.m_spawnerPresetList != null)
            {
                foreach (BiomeSpawnerListEntry entry in newWorldCreationSettings.m_spawnerPresetList)
                {
                    if (entry.m_spawnerSettings != null)
                    {
#if UNITY_EDITOR
                        AssetDatabase.AddObjectToAsset(entry.m_spawnerSettings, scriptableObjectPath);
#endif
                    }
                }
            }
#if UNITY_EDITOR
            EditorUtility.SetDirty(newWorldCreationSettings);
            AssetDatabase.SaveAssets();
#endif
            sessionMgr.AddOperation(op);

            //Play the operation
            if (executeNow)
            {
                ExecuteOperation(op);
            }
            GaiaStopwatch.EndEvent("Session Manager Create World");
        }

        public static bool CheckIfSceneIsSaved(WorldCreationSettings worldCreationSettings)
        {
            bool isSaved = true;
#if UNITY_EDITOR
            //Check if scene has been saved yet
            if (worldCreationSettings.m_createInScene && string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
            {
                bool scenesSaved = false;

                if (GaiaUtils.DisplayDialogNoEditor("Scene not saved yet", "You are creating a multi-terrain setup with individual terrain scenes. This requires you to save the scene before proceeding.\r\n\r\n Please Consider using the suggested session directory - Gaia will create all terrain data files, scenes etc. in this session directory as well.", "Save Now", "Cancel", false, "Could not setup a multi-terrain world with individual terrain scenes without the scene being saved first!"))
                {
                    string suggestedPath = GaiaDirectories.GetSessionSubFolderPath(GetSessionManager().m_session, true);
                    string sceneTargetPath = EditorUtility.SaveFilePanel("Save Scene As...", suggestedPath, "New Gaia Scene", "unity");
                    if (!string.IsNullOrEmpty(sceneTargetPath))
                    {
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), GaiaDirectories.GetPathStartingAtAssetsFolder(sceneTargetPath));
                        scenesSaved = true;
                    }
                    else
                    {
                        scenesSaved = false;
                    }

                }
                else
                {
                    //Canceled out
                    isSaved = false;
                }

                //Did the user actually save the scene after the prompt?
                if (!scenesSaved)
                {
                    isSaved = false;
                }
            }

            //If the scene is saved, make sure it is in build settings as well
            if (isSaved)
            {
                List<EditorBuildSettingsScene> sceneSettings = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                if (!sceneSettings.Exists(x => x.path == EditorSceneManager.GetActiveScene().path))
                {
                    sceneSettings.Insert(0, new EditorBuildSettingsScene(EditorSceneManager.GetActiveScene().path, true));
                }
                EditorBuildSettings.scenes = sceneSettings.ToArray();
            }

#endif
            return isSaved;
        }

        private string SaveOperationData(GaiaOperation op, ScriptableObject dataObject)
        {
            string operationName = "Session Entry";
            switch (op.m_operationType)
            {

                case GaiaOperation.OperationType.CreateWorld:
                    WorldCreationSettings worldCreationSettings = (WorldCreationSettings)dataObject;
                    op.m_description = String.Format("Creating World: {0} x {1} tiles, tile size: {2}", worldCreationSettings.m_xTiles.ToString(), worldCreationSettings.m_zTiles.ToString(), worldCreationSettings.m_tileSize.ToString());
                    operationName = String.Format("Creating World", worldCreationSettings.m_xTiles.ToString(), worldCreationSettings.m_zTiles.ToString());
                    break;
                case GaiaOperation.OperationType.Stamp:
                    StamperSettings stamperSettings = (StamperSettings)dataObject;
                    //Check if we have an image mask in first slot - we want to log that as this is the usual way how the stamper is used
                    //Otherwise we just log the first mask operation type
                    string imageMaskInfo = "No Masks";
                    if (stamperSettings.m_imageMasks.Length > 0)
                    {
                        switch (stamperSettings.m_imageMasks[0].m_operation)
                        {
                            case ImageMaskOperation.ImageMask:
                                imageMaskInfo = stamperSettings.m_imageMasks[0].ImageMaskTexture.name;
                                break;
                            default:
                                imageMaskInfo = stamperSettings.m_imageMasks[0].m_operation.ToString();
                                break;
                        }
                    }

                    op.m_description = String.Format("{0} - {1}", stamperSettings.m_operation.ToString(), imageMaskInfo);
                    operationName = String.Format("{0} - {1}", stamperSettings.m_operation.ToString(), imageMaskInfo); ;
                    break;
                case GaiaOperation.OperationType.Spawn:
                    SpawnOperationSettings soSettings = (SpawnOperationSettings)dataObject;
                    //Check if we have a single spawner only, if yes, we will take spawner name for the description, otherwise we put "Multiple Spawners"
                    //Otherwise we just log the first mask operation type
                    string spawnerInfo = "Unknown";
                    if (soSettings.m_spawnerSettingsList.Count > 0)
                    {
                        if (soSettings.m_spawnerSettingsList.Count == 1)
                        {
                            spawnerInfo = soSettings.m_spawnerSettingsList[0].name;
                        }
                        else
                        {
                            spawnerInfo = "Multiple Spawners (" + soSettings.m_spawnerSettingsList.Count.ToString() + ")";
                        }
                    }

                    op.m_description = String.Format("Spawning - {0}", spawnerInfo);
                    operationName = String.Format("Spawning - {0}", spawnerInfo);
                    break;

                case GaiaOperation.OperationType.FlattenTerrain:
                    FlattenOperationSettings flattenSettings = (FlattenOperationSettings)dataObject;
                    //Check if we have no terrains -> that means the entire world was flattened
                    //else we display the number of affected terrains
                    string flattenInfo = "Unknown";
                    if (flattenSettings.m_TerrainsList.Count > 0)
                    {
                        flattenInfo = flattenSettings.m_TerrainsList.Count().ToString() + " Terrains";
                    }
                    else
                    {
                        flattenInfo = "All Terrains";
                    }

                    op.m_description = String.Format("Flatten - {0}", flattenInfo);
                    operationName = String.Format("Flatten - {0}", flattenInfo);
                    break;
                case GaiaOperation.OperationType.StampUndo:
                    UndoRedoOperationSettings undoSettings = (UndoRedoOperationSettings)dataObject;
                    //justdisplay the number of affected terrains
                    string undoInfo = undoSettings.m_TerrainsList.Count().ToString() + " Terrains";
                    op.m_description = String.Format("Undo - {0}", undoInfo);
                    operationName = String.Format("Undo - {0}", undoInfo);
                    break;
                case GaiaOperation.OperationType.StampRedo:
                    UndoRedoOperationSettings redoSettings = (UndoRedoOperationSettings)dataObject;
                    //justdisplay the number of affected terrains
                    string redoInfo = redoSettings.m_TerrainsList.Count().ToString() + " Terrains";
                    op.m_description = String.Format("Redo - {0}", redoInfo);
                    operationName = String.Format("Redo - {0}", redoInfo);
                    break;
                case GaiaOperation.OperationType.ClearSpawns:
                    ClearOperationSettings clearOperationSettings = (ClearOperationSettings)dataObject;
                    //Display what & where is being cleared
                    string clearInfo = "";
                    if (clearOperationSettings.m_clearTrees)
                    {
                        clearInfo += " Trees";
                    }
                    if (clearOperationSettings.m_clearTerrainDetails)
                    {
                        clearInfo += " Details";
                    }
                    if (clearOperationSettings.m_clearGameObjects)
                    {
                        clearInfo += " GOs";
                    }
                    if (clearOperationSettings.m_clearSpawnExtensions)
                    {
                        clearInfo += " SpawnExt";
                    }
                    if (clearOperationSettings.m_clearProbes)
                    {
                        clearInfo += " Probes";
                    }
                    clearInfo += ", " + clearOperationSettings.m_clearSpawnFor.ToString();
                    clearInfo += ", " + clearOperationSettings.m_clearSpawnFrom.ToString();
                    op.m_description = String.Format("Clearing -{0}", clearInfo);
                    operationName = String.Format("Clearing -{0}", clearInfo);
                    break;
                case GaiaOperation.OperationType.RemoveNonBiomeResources:
                    RemoveNonBiomeResourcesSettings removeSettings = (RemoveNonBiomeResourcesSettings)dataObject;
                    //just display the name of the biome controller
                    string removeInfo = removeSettings.m_biomeControllerSettings.name;
                    op.m_description = String.Format("Remove Non Biome Resources - {0}", removeInfo);
                    operationName = String.Format("Remove Non Biome Resources - {0}", removeInfo);
                    break;
                case GaiaOperation.OperationType.MaskMapExport:
                    ExportMaskMapOperationSettings exportSettings = (ExportMaskMapOperationSettings)dataObject;
                    //just display the name of the export settings object
                    string exportMaskMapInfo = exportSettings.m_maskMapExportSettings.name;
                    op.m_description = String.Format("Export Mask Map - {0}", exportMaskMapInfo);
                    operationName = String.Format("Export Mask Map - {0}", exportMaskMapInfo);
                    break;
                case GaiaOperation.OperationType.ExportWorldMapToLocalMap:
                    //just display that the world map is being exported - not much more to log for that one
                    operationName = "Export World Map to Local Map";
                    op.m_description = "Export World Map to Local Map";
                    break;
                case GaiaOperation.OperationType.External:
                    //just display that the world map is being exported - not much more to log for that one
                    operationName = "External Operation";
                    op.m_description = dataObject.name;
                    break;
            }
            string scriptableObjectPath = GaiaDirectories.GetSessionOperationPath(m_session);
            scriptableObjectPath += Path.DirectorySeparatorChar + string.Format("{0:yyyyMMdd-HHmmss}", DateTime.Now) + " " + operationName + ".asset";
#if UNITY_EDITOR
            AssetDatabase.CreateAsset(dataObject, scriptableObjectPath);
            op.scriptableObjectAssetGUID = AssetDatabase.AssetPathToGUID(scriptableObjectPath);
#endif
            op.m_isActive = true;
            op.m_operationDateTime = DateTime.Now.ToString();
            return scriptableObjectPath;
        }

        public static bool ExecuteOperation(GaiaOperation op)
        {
            if (op == null)
            {
                Debug.LogError("Trying to execute a session operation, but the operation is null!");
                return false;
            }

            switch (op.m_operationType)
            {
                case GaiaOperation.OperationType.CreateWorld:
                    GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();
                    if (sessionMgr == null)
                    {
                        Debug.LogError("Could not find or create a Session Manager in the scene. A session manager is required for world creation.");
                        return false;
                    }
                    sessionMgr.StartCreateWorld(op.WorldCreationSettings);
                    return true;
                case GaiaOperation.OperationType.Stamp:
                    return ExecuteStamp(op, null, true);
                case GaiaOperation.OperationType.Spawn:
                    return ExecuteSpawn(op, null);
                case GaiaOperation.OperationType.FlattenTerrain:
                    return ExecuteFlatten(op);
                case GaiaOperation.OperationType.StampUndo:
                    return ExecuteStampUndoRedo(false, op);
                case GaiaOperation.OperationType.StampRedo:
                    return ExecuteStampUndoRedo(true, op);
                case GaiaOperation.OperationType.ClearSpawns:
                    return ExecuteClearSpawns(op);
                case GaiaOperation.OperationType.RemoveNonBiomeResources:
                    return ExecuteRemoveNonBiomeResources(op, null);
                case GaiaOperation.OperationType.ExportWorldMapToLocalMap:
                    return ExecuteExportWorldMapToLocalMap(op.WorldCreationSettings);
                case GaiaOperation.OperationType.External:
                    return ExecuteSerializedExternalAction(op.ExternalOperationScriptableObject, op.m_serializedExternalAction);
#if GAIA_PRO_PRESENT
                case GaiaOperation.OperationType.MaskMapExport:
                    return ExecuteMaskMapExport(op);
#endif
            }
            return true;
        }

        private bool StartCreateWorld(WorldCreationSettings worldCreationSettings)
        {
            m_updateOperationCoroutine = ExecuteCreateWorld(worldCreationSettings);
            StartEditorUpdates();
            return true;
        }

        #region OPERATION EXECUTION
        private IEnumerator ExecuteCreateWorld(WorldCreationSettings worldCreationSettings)
        {
#if UNITY_EDITOR
            GaiaStopwatch.StartEvent("Session Manager Create World Execution");
            m_regenerateRun = false;
            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();

            if (worldCreationSettings == null)
            {
                Debug.LogError("Trying to create a world without world creation settings, world creation will be skipped.");
                yield return null;
            }

            if (!CheckIfSceneIsSaved(worldCreationSettings))
            {
                Debug.LogError("Trying to create a world with multiple terrain scenes in an unsaved scene. This is not possible, please save your scene when prompted to.");
                yield return null;
            }

            //Create the terrains array
            Terrain[,] world = new Terrain[worldCreationSettings.m_xTiles, worldCreationSettings.m_zTiles];

            //Display a progress bar
            int totalTerrains = worldCreationSettings.m_xTiles * worldCreationSettings.m_zTiles;
            int currentTerrain = 1;

            //prepare resource prototype arrays once, so the same prototypes can be added to all the tiles.
            TerrainLayer[] terrainLayers = new TerrainLayer[0];
            DetailPrototype[] terrainDetails = new DetailPrototype[0];
            TreePrototype[] terrainTrees = new TreePrototype[0];

            if (worldCreationSettings.m_spawnerPresetList != null)
            {
                GaiaDefaults.GetPrototypes(worldCreationSettings.m_spawnerPresetList, ref terrainLayers, ref terrainDetails, ref terrainTrees, null);
            }

            Scene originalScene = EditorSceneManager.GetActiveScene();


            //we need to remember if this setup had a world map before, otherwise we might overwrite that info.
            bool hasWorldMap = TerrainLoaderManager.Instance.TerrainSceneStorage.m_hasWorldMap;

            string terrainSceneStoragePath = "";
            bool createNewTSStorage = false;
            //only remove the existing terrain scene storage if it is NOT a worldmap we are creating, or if there are no terrains stored in it anyways.
            //We would not want to overwrite the existing storage if a world map is being created later in the scene creation process.
            TerrainSceneStorage terrainSceneStorage = TerrainLoaderManager.Instance.TerrainSceneStorage;
            if (!worldCreationSettings.m_isWorldMap || TerrainLoaderManager.Instance.TerrainSceneStorage.m_terrainScenes.Count == 0)
            {
                //Has the original scene been saved before? If yes, delete old potential terrain scene storage
                terrainSceneStoragePath = GaiaDirectories.GetScenePath(m_session) + "/TerrainScenes.asset";

                if (File.Exists(terrainSceneStoragePath))
                {
                    File.Delete(terrainSceneStoragePath);
                }

                TerrainLoaderManager.Instance.ResetStorage();

                terrainSceneStorage = ScriptableObject.CreateInstance<TerrainSceneStorage>();
                terrainSceneStorage.m_hasWorldMap = hasWorldMap;
                createNewTSStorage = true;
            }

            bool cancel = false;
            //And iterate through and create each terrain
            for (int x = 0; x < worldCreationSettings.m_xTiles; x++)
            {
                for (int z = 0; z < worldCreationSettings.m_zTiles; z++)
                {

                    //GaiaUtils.DisplayProgressBarNoEditor("Creating Terrains", "Creating Terrain " + currentTerrain.ToString() + " of " + totalTerrains.ToString(), (float)currentTerrain / (float)totalTerrains);
                    cancel = ProgressBar.Show(ProgressBarPriority.WorldCreation, "Creating World", "Creating Terrains", currentTerrain, totalTerrains, true, true);

                    if (cancel)
                    {
                        break;
                    }

                    GameObject terrainGO = CreateTile(x, z, worldCreationSettings, ref world, terrainLayers, terrainDetails, terrainTrees);

                    if (worldCreationSettings.m_createInScene && !worldCreationSettings.m_isWorldMap)
                    {
#if GAIA_PRO_PRESENT
                        terrainSceneStorage.m_terrainScenes.Add(TerrainSceneCreator.CreateTerrainScene(originalScene, terrainSceneStorage, m_session, terrainGO, worldCreationSettings));
#endif
                    }
                    else
                    {
                        if (worldCreationSettings.m_isWorldMap)
                        {
                            GameObject wme = GaiaUtils.GetOrCreateWorldDesigner(true, false);
                            terrainGO.transform.SetParent(wme.transform);
                            terrainGO.GetComponent<Terrain>().terrainData.terrainLayers = new TerrainLayer[1] { gaiaSettings.m_worldmapLayer };
                            terrainGO.GetComponent<Terrain>().basemapDistance = 10000;
                        }
                        else
                        {
                            GameObject gaiaObj = GaiaUtils.GetTerrainObject();
                            terrainGO.transform.SetParent(gaiaObj.transform);
                        }
#if GAIA_PRO_PRESENT
                        //we need to attach a floating point fix member component to these terrain tiles if the user wants the floating point fix
                        //- but not when it is a scenario with dynamic loaded terrains, those will shift automatically with the origin
                        if (worldCreationSettings.m_applyFloatingPointFix && !worldCreationSettings.m_createInScene)
                        {
                            terrainGO.AddComponent<FloatingPointFixMember>();
                            terrainGO.isStatic = false;
                        }
#endif
                    }
                    currentTerrain++;
                    yield return null;
                }
                if (cancel)
                {
                    break;
                }
            }

            if (cancel)
            {
                ProgressBar.Clear(ProgressBarPriority.WorldCreation);
                StopEditorUpdates();
                if (OnWorldCreated != null)
                {
                    OnWorldCreated();
                }
                yield return null;
            }

            //Is this still the default sea level? If yes, adjust it according to terrain tile height
            if (GetSeaLevel() == worldCreationSettings.m_gaiaDefaults.m_seaLevel)
            {
                SetSeaLevel(worldCreationSettings.m_tileHeight / 2048 * 50);
            }

            if (!worldCreationSettings.m_isWorldMap)
            {
                terrainSceneStorage.m_terrainTilesX = worldCreationSettings.m_xTiles;
                terrainSceneStorage.m_terrainTilesZ = worldCreationSettings.m_zTiles;
                terrainSceneStorage.m_terrainTilesSize = worldCreationSettings.m_tileSize;
                if (worldCreationSettings.m_applyFloatingPointFix)
                {
                    terrainSceneStorage.m_useFloatingPointFix = true;
                }
                else
                {
                    terrainSceneStorage.m_useFloatingPointFix = false;
                }
            }

            TerrainLoaderManager.Instance.m_cacheMemoryThreshold = gaiaSettings.m_terrainUnloadMemoryTreshold;


            if (createNewTSStorage)
            {
                //permanently save the multi-scene data in a TerrainScenes storage file
                AssetDatabase.CreateAsset(terrainSceneStorage, terrainSceneStoragePath);
                AssetDatabase.ImportAsset(terrainSceneStoragePath);
                TerrainLoaderManager.Instance.LoadStorageData();
#if GAIA_PRO_PRESENT
                if (worldCreationSettings.m_createInScene)
                {
                    Double regularRange = TerrainLoaderManager.GetDefaultLoadingRangeForTilesize(worldCreationSettings.m_tileSize);
                    TerrainLoaderManager.Instance.SetLoadingRange(regularRange, regularRange * 3f);
                    TerrainLoaderManager.Instance.UpdateTerrainLoadState();
                    AddTerrainScenesToBuildSettings(TerrainLoaderManager.TerrainScenes);
                }
#endif
            }

            if (!worldCreationSettings.m_isWorldMap)
            {

                TerrainLoaderManager.Instance.SwitchToLocalMap();
            }
            else
            {
                TerrainLoaderManager.Instance.SwitchToWorldMap();
            }

            //Save the session (in case we added terrainScenes)
            SaveSession();



            ProgressBar.Clear(ProgressBarPriority.WorldCreation);
            StopEditorUpdates();

            //If this utilizes terrain scenes, we should save the main scene to save the terrain loading setup
            //otherwise if this scene is closed and re-opened without saving, it will miss the Terrain Loader etc. and will appear broken
            if (worldCreationSettings.m_createInScene)
            {
                EditorSceneManager.SaveScene(originalScene);
            }
#endif

            if (OnWorldCreated != null)
            {
                OnWorldCreated();
            }
            //Are there still session entries queued for execution? If yes, we must continue playback after the terrain creation
            if (m_session.m_operations.Exists(x => x.sessionPlaybackState == SessionPlaybackState.Queued))
            {
                ContinueSessionPlayback();
            }
            GaiaStopwatch.EndEvent("Session Manager Create World Execution");
            GaiaStopwatch.Stop();

            yield return null;
        }
        private static bool ExecuteFlatten(GaiaOperation op)
        {
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();

            //No flatten settings? Abort!
            if (op.FlattenOperationSettings == null)
            {
                Debug.LogError("Trying to flatten without flatten settings, flattening will be skipped!");
                return false;
            }

            if (m_regenerateRun)
            {
                List<string> allAffectedTerrainNames = sessionMgr.m_terrainNamesFlaggedForRegeneration.Concat(sessionMgr.m_terrainNamesFlaggedForRegenerationDeactivation).ToList();
                FlattenTerrainsByList(allAffectedTerrainNames);
            }
            else
            {
                FlattenTerrainsByList(op.FlattenOperationSettings.m_TerrainsList);
            }

            if (!m_regenerateRun)
            {
                op.m_affectedTerrainNames = op.FlattenOperationSettings.m_TerrainsList.ToArray();
                sessionMgr.SaveSession();
            }

            return true;
        }
        private static bool ExecuteRemoveNonBiomeResources(GaiaOperation op, BiomeController biomeController)
        {
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();
            //No settings? Abort!
            if (op.RemoveNonBiomeResourcesSettings == null)
            {
                Debug.LogError("Trying to remove non-biomes without settings, removal will be skipped!");
                return false;
            }


            if (biomeController == null)
            {
                //No biome controller was passed in - we need to create one with the specific settings
                GameObject tempSessionToolObject = GaiaUtils.GetTempSessionToolsObject();
                string biomeName = "Biome";
                if (op.RemoveNonBiomeResourcesSettings.m_biomeControllerSettings != null)
                {
                    biomeName = op.RemoveNonBiomeResourcesSettings.m_biomeControllerSettings.name;
                }
                GameObject spawnerObj = new GameObject("Session - " + biomeName);
                biomeController = spawnerObj.AddComponent<BiomeController>();
                biomeController.gameObject.transform.parent = tempSessionToolObject.transform;
                if (op.RemoveNonBiomeResourcesSettings.m_biomeControllerSettings != null)
                {
                    biomeController.LoadSettings(op.RemoveNonBiomeResourcesSettings.m_biomeControllerSettings);
                }
            }
#if GAIA_PRO_PRESENT
            LoadMode currentLoadMode = biomeController.TerrainLoader.LoadMode;
            bool terrainLoaderWasSelected = biomeController.TerrainLoader.m_isSelected;
            biomeController.UpdateAutoLoadRange();
            biomeController.TerrainLoader.m_isSelected = true;
            biomeController.TerrainLoader.LoadMode = LoadMode.EditorSelected;
            biomeController.TerrainLoader.UpdateTerrains();
#endif

            List<string> allAffectedTerrainNames = null;
            if (m_regenerateRun)
            {
                allAffectedTerrainNames = sessionMgr.m_terrainNamesFlaggedForRegeneration.Concat(sessionMgr.m_terrainNamesFlaggedForRegenerationDeactivation).ToList();
            }


            if (op.RemoveNonBiomeResourcesSettings.m_biomeControllerSettings.m_removeForeignTrees)
            {
                biomeController.RemoveForeignTrees(op.RemoveNonBiomeResourcesSettings.m_spawnerSettingsList, allAffectedTerrainNames);
            }
            if (op.RemoveNonBiomeResourcesSettings.m_biomeControllerSettings.m_removeForeignTerrainDetails)
            {
                biomeController.RemoveForeignTerrainDetails(op.RemoveNonBiomeResourcesSettings.m_spawnerSettingsList, allAffectedTerrainNames);
            }
            if (op.RemoveNonBiomeResourcesSettings.m_biomeControllerSettings.m_removeForeignGameObjects)
            {
                biomeController.RemoveForeignGameObjects(op.RemoveNonBiomeResourcesSettings.m_spawnerSettingsList, allAffectedTerrainNames);
            }
#if GAIA_PRO_PRESENT
            //switch back to whatever load mode there was before the stamp
            biomeController.TerrainLoader.LoadMode = currentLoadMode;
            biomeController.TerrainLoader.m_isSelected = terrainLoaderWasSelected;
            biomeController.TerrainLoader.UpdateTerrains();
#endif

            if (!m_regenerateRun)
            {
                //Log the affected terrains in the operation
                double range = op.RemoveNonBiomeResourcesSettings.m_biomeControllerSettings.m_range;
                BoundsDouble operationBounds = new BoundsDouble(new Vector3Double(biomeController.m_settings.m_x, biomeController.m_settings.m_y, biomeController.m_settings.m_z), new Vector3Double(range, range, range));

                //add the origin offset for the check (if any)
                operationBounds.center += TerrainLoaderManager.Instance.GetOrigin();

                op.m_affectedTerrainNames = TerrainHelper.GetTerrainsIntersectingBounds(operationBounds);

                sessionMgr.SaveSession();
            }

            return true;
        }
        private static bool ExecuteStamp(GaiaOperation op, GameObject stamperGO, bool skipUndo = false)
        {
            GaiaStopwatch.StartEvent("Execute Stamp Session Manager");
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();

            //No stamper settings? Abort!
            if (op.StamperSettings == null)
            {
                Debug.LogError("Trying to stamp without stamper settings, stamping will be skipped!");
                return false;
            }

            //Get or create Stamper
            Stamper stamper = GaiaSessionManager.GetOrCreateSessionStamper(stamperGO);

            //still no stamper? Abort
            if (stamper == null)
            {
                Debug.LogError("Could not find or create stamper for stamping in this scene!");
                return false;
            }


            //Selection.activeObject = stamper.gameObject;
            if (stamperGO == null)
            {
                stamper.LoadSettings(op.StamperSettings);
            }
            stamper.UpdateStamp();

            bool originalUndo = stamper.m_recordUndo;
            if (skipUndo)
            {
                stamper.m_recordUndo = false;
            }

            //make sure min max info is up to date
            //needs to be done BEFORE regular terrain loading else it can disturb stamping process
            stamper.UpdateMinMaxHeight();

            //Fake the selection during the stamping so that the terrains will get loaded in any case for stamping
#if GAIA_PRO_PRESENT
            LoadMode currentLoadMode = stamper.TerrainLoader.LoadMode;
            bool terrainLoaderWasSelected = stamper.TerrainLoader.m_isSelected;
            stamper.TerrainLoader.m_isSelected = true;
            stamper.TerrainLoader.LoadMode = LoadMode.EditorSelected;
            stamper.m_loadTerrainMode = LoadMode.EditorSelected;
            stamper.UpdateTerrainLoader();
            stamper.TerrainLoader.UpdateTerrains();
#endif



            if (stamper.m_recordUndo)
            {
                GaiaWorldManager mgr = new GaiaWorldManager(Terrain.activeTerrains, op.StamperSettings.m_isWorldmapStamper);
                if (mgr.TileCount > 0)
                {
                    //Store initial state of terrain
                    if (stamper.m_stamperUndoOperations.Count == 0)
                    {
                        GaiaWorldManager undo = new GaiaWorldManager(Terrain.activeTerrains, op.StamperSettings.m_isWorldmapStamper);
                        undo.LoadFromWorld();
                        stamper.m_stamperUndoOperations.Add(undo);
                    }
                }
            }


            stamper.m_syncHeightmaps = true;
            ProgressBar.Show(ProgressBarPriority.Stamping, "Stamping", "Stamping " + op.StamperSettings.m_operation.ToString());

            if (m_regenerateRun)
            {
                List<string> allAffectedTerrainNames = sessionMgr.m_terrainNamesFlaggedForRegeneration.Concat(sessionMgr.m_terrainNamesFlaggedForRegenerationDeactivation).ToList();
                stamper.Stamp(allAffectedTerrainNames);
            }
            else
            {
                stamper.Stamp();
            }
            stamper.m_settings.ClearImageMaskTextures();
            GaiaUtils.ReleaseAllTempRenderTextures();

            if (stamper.m_recordUndo)
            {
                GaiaWorldManager stmpMgr = new GaiaWorldManager(Terrain.activeTerrains, op.StamperSettings.m_isWorldmapStamper);
                stmpMgr.LoadFromWorld();

                //Are we on the last operation in the list -> add a new one at the end
                if (stamper.m_currentStamperUndoOperation >= stamper.m_stamperUndoOperations.Count - 1)
                {
                    stamper.m_stamperUndoOperations.Add(stmpMgr);
                    stamper.m_currentStamperUndoOperation++;
                }
                else //overwrite the following op and delete the rest to start a new "branch"
                {
                    stamper.m_currentStamperUndoOperation++;
                    stamper.m_stamperUndoOperations[stamper.m_currentStamperUndoOperation] = stmpMgr;
                    for (int i = stamper.m_stamperUndoOperations.Count - 1; i > stamper.m_currentStamperUndoOperation; i--)
                    {
                        stamper.m_stamperUndoOperations.RemoveAt(i);
                    }
                }
            }

            stamper.m_recordUndo = originalUndo;

            //always deactivate the preview during autospawn, even if the delay is set to 0 ms
            //preview being drawn during autospawning can influence the spawn ruesults
            stamper.m_drawPreview = false;
            stamper.m_activatePreviewRequested = true;
            stamper.m_activatePreviewTimeStamp = GaiaUtils.GetUnixTimestamp();
            stamper.m_lastHeightmapUpdateTimeStamp = GaiaUtils.GetUnixTimestamp();

            //always request a height update
            stamper.m_heightUpdateRequested = true;

            //if a stamper was passed in for the operation, switch back to whatever load mode there was before the stamp
            if (stamperGO != null)
            {
#if GAIA_PRO_PRESENT
                stamper.TerrainLoader.LoadMode = currentLoadMode;
                stamper.m_loadTerrainMode = currentLoadMode;
                stamper.TerrainLoader.m_isSelected = terrainLoaderWasSelected;
                stamper.TerrainLoader.UpdateTerrains();
#endif
            }

            if (stamper.m_autoSpawners.Exists(x => x.isActive == true && x.spawner != null))
            {
                //Do not execute spawns right away as this can lead to errors in texture spawning, set a flag to do it after all "OnTerrainChanged" callbacks
                stamper.m_autoSpawnRequested = true;
                stamper.m_autoSpawnStarted = false;
                //Notify the collision mask cache that we are about to do an autospawn - this will prevent excessive cache clearing by the spawners

                foreach (AutoSpawner autoSpawner in stamper.m_autoSpawners.FindAll(x => x.isActive == true && x.spawner != null))
                {
                    autoSpawner.status = AutoSpawnerStatus.Queued;
                }

            }
            else
            {
                ProgressBar.Clear(ProgressBarPriority.Stamping);
            }


#if GAIA_PRO_PRESENT
            if (stamper.m_autoMaskExporter.FindAll(x => x.isActive == true && x.maskMapExport != null).Count > 0)
            {
                stamper.m_autoMaskExportRequested = true;
            }
#endif

            if (!m_regenerateRun)
            {
                //Log the affected terrains in the operation
                double range = op.StamperSettings.m_width / 100 * TerrainLoaderManager.Instance.TerrainSceneStorage.m_terrainTilesSize;
                BoundsDouble operationBounds = new BoundsDouble(new Vector3Double(op.StamperSettings.m_x, op.StamperSettings.m_y, op.StamperSettings.m_z), new Vector3Double(range, range, range));
                //add the origin offset for the check (if any)
                operationBounds.center += TerrainLoaderManager.Instance.GetOrigin();
                op.m_affectedTerrainNames = TerrainHelper.GetTerrainsIntersectingBounds(operationBounds);
                sessionMgr.SaveSession();
            }
            GaiaStopwatch.EndEvent("Execute Stamp Session Manager");
            return true;
        }
        private static bool ExecuteClearWorld()
        {
#if UNITY_EDITOR
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();

            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                TerrainLoaderManager.Instance.UnloadAll(true);

                //Remove all terrain data objects but keep the world map!
                DirectoryInfo directoryInfo = new DirectoryInfo(GaiaDirectories.GetTerrainDataScenePath(sessionMgr.m_session));

                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    if (!file.Name.Contains("World Map"))
                    {
                        FileUtil.DeleteFileOrDirectory(file.FullName);
                    }
                }

                //Remove the entire scene directory 
                FileUtil.DeleteFileOrDirectory(GaiaDirectories.GetTerrainScenePathForSession(sessionMgr.m_session));
                //Remove the scene directory from the terrain scene storage (if different)
                FileUtil.DeleteFileOrDirectory(GaiaDirectories.GetTerrainScenePathForStorageFile(TerrainLoaderManager.Instance.TerrainSceneStorage));
                //empty the terrainScene list in the session
                TerrainLoaderManager.TerrainScenes.Clear();
                //Clear the min max cache (except world map)
                sessionMgr.m_session.m_terrainMinMaxCache.RemoveAll(x => !x.isWorldmap);
            }

            foreach (Terrain t in Resources.FindObjectsOfTypeAll<Terrain>())
            {
                if (!TerrainHelper.IsWorldMapTerrain(t))
                {
                    DestroyImmediate(t.terrainData, true);
                    DestroyImmediate(t.gameObject, true);
                }
            }
#endif
            return true;
        }
        private static bool ExecuteClearSpawns(GaiaOperation op, Spawner spawner = null)
        {
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();

            //No clear settings? Abort!
            if (op.ClearOperationSettings == null)
            {
                Debug.LogError("Trying to clear spawns without clear settings, clearing will be skipped!");
                return false;
            }
            if (spawner == null)
            {
                //No list was passed in- need to create new temporary session spawner, then start the clearing
                GameObject tempSessionToolObject = GaiaUtils.GetTempSessionToolsObject();
                string spawnerName = "Spawner";
                if (op.ClearOperationSettings.m_spawnerSettings != null)
                {
                    spawnerName = op.ClearOperationSettings.m_spawnerSettings.name;
                }
                GameObject spawnerObj = new GameObject("Session - " + spawnerName);
                spawner = spawnerObj.AddComponent<Spawner>();
                spawner.gameObject.transform.parent = tempSessionToolObject.transform;
                if (op.ClearOperationSettings.m_spawnerSettings != null)
                {
                    spawner.LoadSettings(op.ClearOperationSettings.m_spawnerSettings);
                }
            }

            List<string> affectedTerrainNames = new List<string>();

            if (m_regenerateRun)
            {
                affectedTerrainNames = op.m_affectedTerrainNames.ToList();
            }
            else
            {
                if (op.ClearOperationSettings.m_clearSpawnFor == ClearSpawnFor.CurrentTerrainOnly)
                {
                    Terrain currentTerrain = spawner.GetCurrentTerrain();
                    if (currentTerrain != null)
                    {
                        affectedTerrainNames = new List<string>() { currentTerrain.name };
                    }
                }
                else
                {
                    if (GaiaUtils.HasDynamicLoadedTerrains())
                    {
                        affectedTerrainNames = TerrainLoaderManager.TerrainScenes.Select(x => x.GetTerrainName()).ToList();
                    }
                    else
                    {
                        affectedTerrainNames = Terrain.activeTerrains.Select(x => x.name).ToList();
                    }

                }
            }


            if (op.ClearOperationSettings.m_clearTrees)
            {
                spawner.ClearTrees(op.ClearOperationSettings.m_clearSpawnFor, op.ClearOperationSettings.m_clearSpawnFrom, affectedTerrainNames);
            }
            if (op.ClearOperationSettings.m_clearTerrainDetails)
            {
                spawner.ClearDetails(op.ClearOperationSettings.m_clearSpawnFor, op.ClearOperationSettings.m_clearSpawnFrom, affectedTerrainNames);
            }
            if (op.ClearOperationSettings.m_clearGameObjects)
            {
                spawner.ClearGameObjects(op.ClearOperationSettings.m_clearSpawnFor, op.ClearOperationSettings.m_clearSpawnFrom, affectedTerrainNames);
            }
            if (op.ClearOperationSettings.m_clearSpawnExtensions)
            {
                spawner.ClearAllSpawnExtensions(op.ClearOperationSettings.m_clearSpawnFor, op.ClearOperationSettings.m_clearSpawnFrom, affectedTerrainNames);
            }
            if (op.ClearOperationSettings.m_clearStamps)
            {
                spawner.ClearStampDistributions(op.ClearOperationSettings.m_clearSpawnFor, op.ClearOperationSettings.m_clearSpawnFrom);
            }
            if (op.ClearOperationSettings.m_clearProbes)
            {
                spawner.ClearGameObjects(op.ClearOperationSettings.m_clearSpawnFor, op.ClearOperationSettings.m_clearSpawnFrom);
            }

            if (!m_regenerateRun)
            {
                op.m_affectedTerrainNames = affectedTerrainNames.ToArray();
                sessionMgr.SaveSession();
            }

            return true;
        }
#if GAIA_PRO_PRESENT
        private static bool ExecuteMaskMapExport(GaiaOperation op, MaskMapExport maskMapExport = null)
        {
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();
            //No Operation Settings? Abort!
            if (op.ExportMaskMapOperationSettings == null)
            {
                Debug.LogError("Trying to export a mask map without operation settings, export will be skipped!");
                return false;
            }

            List<string> affectedTerrainNames = new List<string>();

            if (m_regenerateRun)
            {
                affectedTerrainNames = op.m_affectedTerrainNames.ToList();
            }
            else
            {
                affectedTerrainNames = op.ExportMaskMapOperationSettings.m_terrainNames;
            }

            GetOrCreateMaskMapExporter(op.ExportMaskMapOperationSettings.m_maskMapExportSettings, ref maskMapExport);
            maskMapExport.ExecuteExport(op.ExportMaskMapOperationSettings.isGlobalExport, affectedTerrainNames);
            maskMapExport.m_settings.ClearImageMaskTextures();
            if (!m_regenerateRun)
            {
                op.m_affectedTerrainNames = op.ExportMaskMapOperationSettings.m_terrainNames.ToArray();
                sessionMgr.SaveSession();
            }

            return true;
        }
#endif
        private static bool ExecuteSpawn(GaiaOperation op, List<Spawner> spawnerList = null, BiomeController biomeController = null)
        {
            GaiaStopwatch.StartEvent("Session Manager Execute Spawn");
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();
            //No spawnOperationSettings? Abort!
            if (op.SpawnOperationSettings == null)
            {
                Debug.LogError("Trying to spawn without Spawn Operation settings, spawning will be skipped!");
                return false;
            }

            GetOrCreateSessionSpawners(op.SpawnOperationSettings, ref biomeController, ref spawnerList);

            ////Look for missing resources and auto-assign them if required
            //foreach (Spawner spawner in spawnerList)
            //{
            //    spawner.CheckForMissingResources(true, true);
            //}

            if (spawnerList.Count > 0)
            {
                if (m_regenerateRun)
                {
                    spawnerList[0].m_updateCoroutine = spawnerList[0].AreaSpawn(spawnerList, op.SpawnOperationSettings.m_spawnArea, op.m_affectedTerrainNames.ToList());
                }
                else
                {
                    spawnerList[0].m_updateCoroutine = spawnerList[0].AreaSpawn(spawnerList, op.SpawnOperationSettings.m_spawnArea);
                }
                spawnerList[0].StartEditorUpdates();
            }

            if (!m_regenerateRun)
            {
                op.m_affectedTerrainNames = TerrainHelper.GetTerrainsIntersectingBounds(op.SpawnOperationSettings.m_spawnArea);
                sessionMgr.SaveSession();
            }
            GaiaStopwatch.EndEvent("Session Manager Execute Spawn");
            return true;
        }
        private static bool ExecuteStampUndoRedo(bool isRedo, GaiaOperation op, Stamper stamper = null)
        {
            GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();
            //No stamper settings? Abort!
            if (op.UndoRedoOperationSettings == null)
            {
                Debug.LogError("Trying to undo / redo without settings, undo / redo will be skipped!");
                return false;
            }

            //Get Stamper. Do not create one as we need to rely on a stamper being present that has the recorded undo data stored - otherwise undo is not possible
            GameObject stamperObj = GameObject.Find("Session Stamper");
            if (stamper != null || stamperObj != null)
            {
                if (stamper == null)
                {
                    stamper = stamperObj.GetComponent<Stamper>();
                }

                List<string> affectedTerrainNames = null;
                if (m_regenerateRun)
                {
                    affectedTerrainNames = op.m_affectedTerrainNames.ToList();
                }
                else
                {
                    affectedTerrainNames = op.UndoRedoOperationSettings.m_TerrainsList;
                }



                if (stamper != null)
                {
                    //Make sure the exact terrains from the UndoOperationSettings are loaded
                    if (GaiaUtils.HasDynamicLoadedTerrains())
                    {
                        sessionMgr.BackupOriginAndLoadRange();
                        foreach (TerrainScene terrainScene in TerrainLoaderManager.TerrainScenes)
                        {
                            if (affectedTerrainNames.Contains(terrainScene.GetTerrainName()))
                            {
                                terrainScene.AddRegularReference(sessionMgr.gameObject);
                            }
                            else
                            {
                                terrainScene.RemoveAllReferences();
                            }
                        }
                    }
                    else
                    {
                        foreach (Terrain t in Resources.FindObjectsOfTypeAll<Terrain>())
                        {
                            if (affectedTerrainNames.Contains(t.name))
                            {
                                t.gameObject.SetActive(true);
                            }
                            else
                            {
                                t.gameObject.SetActive(false);
                            }
                        }
                    }

                    if (isRedo)
                    {
                        if (stamper.m_stamperUndoOperations[stamper.m_currentStamperUndoOperation + 1].SaveToWorld(true))
                        {
                            stamper.m_currentStamperUndoOperation++;
                        }
                    }
                    else
                    {
                        if (stamper.m_stamperUndoOperations[stamper.m_currentStamperUndoOperation - 1].SaveToWorld(true))
                        {
                            stamper.m_currentStamperUndoOperation--;
                        }
                    }
                    if (GaiaUtils.HasDynamicLoadedTerrains())
                    {
                        TerrainLoaderManager.Instance.UnloadAll();
                        sessionMgr.RestoreOriginAndLoadRange();
                    }
                }


            }
            if (!m_regenerateRun)
            {
                op.m_affectedTerrainNames = op.UndoRedoOperationSettings.m_TerrainsList.ToArray();
                sessionMgr.SaveSession();
            }

            return true;

        }

        private static bool ExecuteExportWorldMapToLocalMap(WorldCreationSettings newWorldCreationSettings)
        {
            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
            Terrain worldmapTerrain = TerrainHelper.GetWorldMapTerrain();
            if (worldmapTerrain == null)
            {
                Debug.LogError("Can't execute export from world map to local map: World map terrain not found in the scene!");
                return false;
            }
            bool exception = false;
            try
            {
                float worldsizeX = newWorldCreationSettings.m_xTiles * newWorldCreationSettings.m_tileSize;
                //Calculate relative size and heightmap res values for the session

                TerrainLoaderManager.Instance.TerrainSceneStorage.m_worldMaprelativeSize = worldmapTerrain.terrainData.size.x / worldsizeX;
                GaiaDefaults defaults = (newWorldCreationSettings.m_gaiaDefaults == null) ? gaiaSettings.m_currentDefaults : newWorldCreationSettings.m_gaiaDefaults;
                TerrainLoaderManager.Instance.TerrainSceneStorage.m_worldMapRelativeHeightmapPixels = ((worldsizeX / newWorldCreationSettings.m_tileSize) * defaults.m_heightmapResolution / defaults.m_heightmapResolution);
                TerrainLoaderManager.Instance.SwitchToLocalMap();
                TerrainLoaderManager.Instance.UnloadAll();
                ////m_spawner.StoreHeightmapResolution();
                ProgressBar.Show(ProgressBarPriority.WorldCreation, "Export To World", "Syncing World Map to World...");
                GameObject wmeGO = GaiaUtils.GetOrCreateWorldDesigner();
                wmeGO.GetComponent<WorldMap>().SyncWorldMapToLocalMap();
                if (OnWorldMapExported != null)
                {
                    OnWorldMapExported();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception while exporting world map to local map, Message: " + ex.Message + " Stack Trace: " + ex.StackTrace);
                exception = true;
            }
            finally
            {
                ProgressBar.Clear(ProgressBarPriority.WorldCreation);
            }
            return exception;
        }

        #endregion


        /// <summary>
        /// Flattens all terrains that are contained within a list of terrain names
        /// </summary>
        /// <param name="terrainsList">The list of terrain names</param>
        private static void FlattenTerrainsByList(List<string> terrainsList)
        {
            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                GaiaUtils.CallFunctionOnDynamicLoadedTerrains(Flatten, false, terrainsList);
            }

            foreach (Terrain t in Terrain.activeTerrains)
            {
                if (terrainsList.Contains(t.name))
                {
                    Flatten(t);
                }
            }
        }

        /// <summary>
        /// Flattens a single Terrain
        /// </summary>
        /// <param name="terrain">The terrain to flatten.</param>
        private static void Flatten(Terrain terrain)
        {
            UnityHeightMap hm = new UnityHeightMap(terrain);
            if (hm != null)
            {
                hm.SetHeight(0f);
                hm.SaveToTerrain(terrain);
            }
        }

        public static GameObject GetOrCreateSessionSpawners(SpawnOperationSettings spawnOperationSettings, ref BiomeController biomeController, ref List<Spawner> spawnerList)
        {

            if (spawnOperationSettings.m_biomeControllerSettings != null && biomeController == null)
            {
                //This spawning operation was using a biome controller, but no biome controller was passed in => need to create one
                GameObject tempSessionToolObject = GaiaUtils.GetTempSessionToolsObject();
                string biomeName = "Biome";
                if (spawnOperationSettings.m_biomeControllerSettings != null)
                {
                    biomeName = spawnOperationSettings.m_biomeControllerSettings.name;
                }
                GameObject spawnerObj = new GameObject("Session - " + biomeName);
                biomeController = spawnerObj.AddComponent<BiomeController>();
                biomeController.gameObject.transform.parent = tempSessionToolObject.transform;
                if (spawnOperationSettings.m_biomeControllerSettings != null)
                {
                    biomeController.LoadSettings(spawnOperationSettings.m_biomeControllerSettings);
                }

            }


            if (spawnerList == null)
            {
                //No spawner list was passed in- need to create new temporary session spawners per spawner settings entry, then start the spawning
                spawnerList = new List<Spawner>();
                foreach (SpawnerSettings spawnerSettings in spawnOperationSettings.m_spawnerSettingsList)
                {
                    GameObject spawnerObj = new GameObject("Session - " + spawnerSettings.name);
                    Spawner spawner = spawnerObj.AddComponent<Spawner>();
                    spawner.LoadSettings(spawnerSettings);
                    spawnerList.Add(spawner);

                    //Decide the parent for the spawners - if it is a biome spawner they must be placed below it for the mask stack of the biome controller to function properly! 
                    if (biomeController != null && spawnOperationSettings.m_biomeControllerSettings != null)
                    {
                        spawner.gameObject.transform.parent = biomeController.transform;
                        biomeController.m_autoSpawners.Add(new AutoSpawner() { isActive = true, spawner = spawner });
                    }
                    else
                    {
                        GameObject tempSessionToolObject = GaiaUtils.GetTempSessionToolsObject();
                        spawner.gameObject.transform.parent = tempSessionToolObject.transform;
                    }
                }

            }

            if (biomeController != null)
            {
                return biomeController.gameObject;
            }
            else
            {
                return spawnerList[0].gameObject;
            }
        }
#if GAIA_PRO_PRESENT
        public static GameObject GetOrCreateMaskMapExporter(MaskMapExportSettings maskMapExportSettings, ref MaskMapExport maskMapExport)
        {
            if (maskMapExport == null)
            {
                GameObject sessionTempObj = GaiaUtils.GetTempSessionToolsObject();
                GameObject maskMapExporterObj = new GameObject("Mask Map Exporter");
                maskMapExporterObj.transform.parent = sessionTempObj.transform;
                maskMapExport = maskMapExporterObj.AddComponent<MaskMapExport>();
            }
            maskMapExport.LoadSettings(maskMapExportSettings);
            return maskMapExport.gameObject;
        }
#endif
        public static Stamper GetOrCreateSessionStamper(GameObject stamperGO = null)
        {
            Stamper stamper = null;

            if (stamperGO != null)
            {
                stamper = stamperGO.GetComponent<Stamper>();
            }

            //No stamper passed in, does a session Stamper exist?
            if (stamper == null)
            {
                GameObject stamperObj = GameObject.Find("Session Stamper");
                if (stamperObj == null)
                {
                    GameObject tempSessionToolObject = GaiaUtils.GetTempSessionToolsObject();
                    stamperObj = new GameObject("Session Stamper");
                    stamperObj.transform.parent = tempSessionToolObject.transform;
                }
                if (stamperObj.GetComponent<Stamper>() == null)
                {
                    stamper = stamperObj.AddComponent<Stamper>();
#if GAIA_PRO_PRESENT
                    if (GaiaUtils.HasDynamicLoadedTerrains())
                    {
                        //We got placeholders, activate terrain loading
                        stamper.m_loadTerrainMode = LoadMode.EditorSelected;
                    }
#endif
                }
                stamper = stamperObj.GetComponent<Stamper>();
            }

            return stamper;
        }



        /// <summary>
        /// Adds only the collider scenes to build settings, will remove the regular / impostor scenes
        /// </summary>
        public static void AddOnlyColliderScenesToBuildSettings(List<TerrainScene> allTerrainScenes)
        {
#if GAIA_PRO_PRESENT
#if UNITY_EDITOR
            List<EditorBuildSettingsScene> sceneSettings = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            foreach (TerrainScene terrainScene in allTerrainScenes)
            {
                if (String.IsNullOrEmpty(terrainScene.m_colliderScenePath))
                {
                    continue;
                }
                EditorBuildSettingsScene colliderSceneSettings = new EditorBuildSettingsScene(terrainScene.m_colliderScenePath, true);
                bool sceneExists = false;
                foreach (EditorBuildSettingsScene oldScene in sceneSettings)
                {
                    if (oldScene.path == terrainScene.m_colliderScenePath)
                    {
                        sceneExists = true;
                        oldScene.enabled = true;
                    }
                    //deactivate both regular & impostor scenes in build settings
                    if (oldScene.path == terrainScene.m_scenePath || oldScene.path == terrainScene.m_impostorScenePath)
                    {
                        oldScene.enabled = false;
                    }
                }
                if (!sceneExists)
                {
                    sceneSettings.Add(colliderSceneSettings);
                }
            }
            // Save off the accumulated build settings
            EditorBuildSettings.scenes = sceneSettings.ToArray();
#endif
#endif
        }


        /// <summary>
        /// Adds all Placeholders to build settings
        /// </summary>
        public static void AddTerrainScenesToBuildSettings(List<TerrainScene> allTerrainScenes)
        {
#if GAIA_PRO_PRESENT
#if UNITY_EDITOR
            List<EditorBuildSettingsScene> sceneSettings = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            Scene mainScene = TerrainLoaderManager.Instance.gameObject.scene;
            bool mainSceneExists = false;
            bool mainSceneFirstInLoadOrder = false;
            
            int mainSceneIndex = -99;

            //Look for the presence / position of main scene first
            int index = 0;
            foreach (EditorBuildSettingsScene oldScene in sceneSettings)
            {
                if (oldScene.path == mainScene.path)
                {
                    mainSceneExists = true;
                    mainSceneIndex = index;
                    if (index == 0)
                    {
                        mainSceneFirstInLoadOrder = true;
                    }
                    break;
                }
                index++;
            }

            foreach (TerrainScene terrainScene in allTerrainScenes)
            {
                EditorBuildSettingsScene regularSceneSettings = new EditorBuildSettingsScene(terrainScene.m_scenePath, true);
                EditorBuildSettingsScene impostorSceneSettings = null;
                bool hasImpostor = !String.IsNullOrEmpty(terrainScene.m_impostorScenePath);
                if (hasImpostor)
                {
                    impostorSceneSettings = new EditorBuildSettingsScene(terrainScene.m_impostorScenePath, true);
                }
                bool sceneExists = false;
                bool impostorSceneExists = false;

                foreach (EditorBuildSettingsScene oldScene in sceneSettings)
                {
                    
                    if (oldScene.path == terrainScene.m_scenePath)
                    {
                        sceneExists = true;
                        oldScene.enabled = true;
                    }
                    if (hasImpostor)
                    {
                        if (oldScene.path == terrainScene.m_impostorScenePath)
                        {
                            impostorSceneExists = true;
                            oldScene.enabled = true;
                        }
                    }
                    if (sceneExists && (impostorSceneExists || !hasImpostor))
                    {
                        break;
                    }

                }
                if (!sceneExists)
                {
                    sceneSettings.Add(regularSceneSettings);
                }
                if (!impostorSceneExists && hasImpostor)
                {
                    sceneSettings.Add(impostorSceneSettings);
                }
            }

            if (!mainSceneExists)
            {
                if(GaiaUtils.DisplayDialogNoEditor("Main Scene not found in Build Settings", "Gaia could not find the scene of terrain loader (" + mainScene.name + ") in the Build Settings. Unless you load up this scene with other means (e.g. from a main menu) you would want the Terrain Loader Scene to be the first scene in the build settings so the terrains start loading on start of the build. Do you want Gaia to put the Main Scene on the top of the included scenes in the build settings?", "Yes, Add the scene", "No thanks"))
                {
                    sceneSettings.Insert(0, new EditorBuildSettingsScene(mainScene.path, true));
                }
            }
            else
            {
                if (!mainSceneFirstInLoadOrder)
                {
                    if (GaiaUtils.DisplayDialogNoEditor("Main Scene not at first position in Build Settings", "Gaia did find the scene of terrain loader (" + mainScene.name + ") in the Build Settings, but it is not in the first position in the scene list. Unless you load up this scene with other means (e.g. from a main menu) you would want the Terrain Loader Scene to be the first scene in the build settings so the terrains start loading on start of the build. Do you want Gaia to put the Main Scene on the top of the included scenes in the build settings?", "Yes, move to top", "No thanks"))
                    {
                        //Remove & Insert to keep the other scenes in the same order.
                        EditorBuildSettingsScene temp = sceneSettings[mainSceneIndex];
                        sceneSettings.Remove(temp);
                        sceneSettings.Insert(0, temp);
                    }
                }
            }


            // Save off the accumulated build settings
            EditorBuildSettings.scenes = sceneSettings.ToArray();
#endif
#endif
        }



        #endregion

    }
}
