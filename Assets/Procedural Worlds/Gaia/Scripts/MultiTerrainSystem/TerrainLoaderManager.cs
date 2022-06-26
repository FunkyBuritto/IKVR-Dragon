using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gaia
{

    public enum CenterSceneViewLoadingOn { SceneViewCamera, WorldOrigin }
    public enum CacheSizePreset { _Off, _1GB, _2GB, _3GB, _4GB, _5GB, _6GB, _7GB, _8GB, Custom }

    public class TerrainLoaderManager : MonoBehaviour
    {
        /// <summary>
        /// Loading Bounds around the world origin - controlled directly in the scene view and displays the part of the world the user wants to edit.
        /// Use Get/SetLoadingRange and Get/SetOrigin to access
        /// </summary>
        [SerializeField]
        private BoundsDouble m_sceneViewOriginLoadingBounds = new BoundsDouble(Vector3Double.zero, new Vector3Double(500f, 500f, 500f));
        [SerializeField]
        private BoundsDouble m_sceneViewImpostorLoadingBounds = new BoundsDouble(Vector3Double.zero, new Vector3Double(500f, 500f, 500f));
        [SerializeField]
        private BoundsDouble m_sceneViewCameraLoadingBounds = new BoundsDouble(Vector3Double.zero, new Vector3Double(500f, 500f, 500f));
        [SerializeField]
        private CenterSceneViewLoadingOn m_centerSceneViewLoadingOn = CenterSceneViewLoadingOn.WorldOrigin;
#if GAIA_PRO_PRESENT
        public List<FloatingPointFixMember> m_allFloatingPointFixMembers = new List<FloatingPointFixMember>();
        public List<ParticleSystem> m_allWorldSpaceParticleSystems = new List<ParticleSystem>();
        public GaiaLoadingScreen m_loadingScreen;
#endif
        public int m_originTargetTileX;
        public int m_originTargetTileZ;
        public bool m_showOriginLoadingBounds;
        public bool m_showOriginTerrainBoxes;
        public CacheSizePreset m_cacheMemoryThresholdPreset = CacheSizePreset._4GB;
        public long m_cacheMemoryThreshold = 4294967296;
        public long m_cacheKeepAliveTime = 300000;
        public bool m_cacheInRuntime = true;
        public bool m_cacheInEditor = true;
        public int m_terrainLoadingTresholdMS = 100;
        public long m_lastTerrainLoadedTimeStamp = 0;
        public long m_trackLoadingProgressTimeOut = 10000;

        private List<TerrainSceneActionQueueEntry> m_terrainSceneActionQueue = new List<TerrainSceneActionQueueEntry>();

        private static TerrainLoaderManager instance = null;

        public static TerrainLoaderManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = GaiaUtils.GetTerrainLoaderManagerObject().GetComponent<TerrainLoaderManager>();
                }

                return instance;
            }
        }
        [SerializeField]
        private TerrainSceneStorage m_terrainSceneStorage;
        public TerrainSceneStorage TerrainSceneStorage
        {
            get
            {
                if (m_terrainSceneStorage == null)
                {
                    LoadStorageData();
                }
                return m_terrainSceneStorage;
            }
            set
            {
                if (m_terrainSceneStorage != value)
                {
                    //Remove all terrains from old storage file
                    UnloadAll(true);
                    m_terrainSceneStorage = value;
#if UNITY_EDITOR
                    m_lastUsedGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_terrainSceneStorage));
#endif
                    //load back in the terrains from the new file
                    RefreshSceneViewLoadingRange();
                }
            }
        }

        public CenterSceneViewLoadingOn CenterSceneViewLoadingOn
        {
            get
            {
                return m_centerSceneViewLoadingOn;
            }
            set
            {
                if (m_centerSceneViewLoadingOn != value)
                {
                    m_centerSceneViewLoadingOn = value;
                    //if both loading ranges are set to 0, we return to the default loading ranges
                    if (m_centerSceneViewLoadingOn == CenterSceneViewLoadingOn.SceneViewCamera)
                    {
                        m_showOriginLoadingBounds = false;
                    }

                    if (GetLoadingRange() == 0 && GetImpostorLoadingRange() == 0)
                    {
                        Double regularRange = TerrainLoaderManager.GetDefaultLoadingRangeForTilesize(TerrainSceneStorage.m_terrainTilesSize);
                        SetLoadingRange(regularRange, regularRange * 3f);
                    }
                    else
                    {
                        SetLoadingRange(m_sceneViewCameraLoadingBounds.extents.x, m_sceneViewImpostorLoadingBounds.extents.x);
                    }
                }
            }
        }


        public void ResetStorage()
        {
            m_terrainSceneStorage = null;
        }

        public void LoadStorageData()
        {
#if UNITY_EDITOR
            //Try to get the terrain scene storage file from the last used GUID first
            if (!String.IsNullOrEmpty(m_lastUsedGUID))
            {
                m_terrainSceneStorage = (TerrainSceneStorage)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_lastUsedGUID), typeof(TerrainSceneStorage));

            }

            //No guid / storage object? Then we need to create one in the current session directory
            if (m_terrainSceneStorage == null)
            {
                GaiaSessionManager gsm = GaiaSessionManager.GetSessionManager();
                if (gsm != null && gsm.m_session != null)
                {
                    string path = GaiaDirectories.GetScenePath(gsm.m_session) + "/TerrainScenes.asset";
                    if (File.Exists(path))
                    {
                        m_terrainSceneStorage = (TerrainSceneStorage)AssetDatabase.LoadAssetAtPath(path, typeof(TerrainSceneStorage));
                    }
                    else
                    {
                        m_terrainSceneStorage = ScriptableObject.CreateInstance<TerrainSceneStorage>();
                        if (TerrainHelper.GetWorldMapTerrain() != null)
                        {
                            m_terrainSceneStorage.m_hasWorldMap = true;
                        }
                        AssetDatabase.CreateAsset(m_terrainSceneStorage, path);
                        AssetDatabase.ImportAsset(path);
                    }
                }
                else
                {
                    m_terrainSceneStorage = ScriptableObject.CreateInstance<TerrainSceneStorage>();
                }
            }

            //Check if there are scene files existing already and if they are in the storage data - if not, we should pick them up accordingly
            string directory = GaiaDirectories.GetTerrainScenePathForStorageFile(m_terrainSceneStorage);
            var dirInfo = new DirectoryInfo(directory);

            bool madeChanges = false;

            if (dirInfo != null)
            {
                FileInfo[] allFiles = dirInfo.GetFiles();
                foreach (FileInfo fileInfo in allFiles)
                {
                    if (fileInfo.Extension == ".unity")
                    {
                        string path = GaiaDirectories.GetPathStartingAtAssetsFolder(fileInfo.FullName);

                        if (!m_terrainSceneStorage.m_terrainScenes.Exists(x => x.GetTerrainName() == x.GetTerrainName(path)))
                        {
                            string firstSegment = fileInfo.Name.Split('-')[0];
                            int xCoord = -99;
                            int zCoord = -99;
                            bool successX, successZ;
                            try
                            {
                                successX = Int32.TryParse(firstSegment.Substring(firstSegment.IndexOf('_') + 1, firstSegment.LastIndexOf('_') - (firstSegment.IndexOf('_') + 1)), out xCoord);
                                successZ = Int32.TryParse(firstSegment.Substring(firstSegment.LastIndexOf('_') + 1, firstSegment.Length - 1 - firstSegment.LastIndexOf('_')), out zCoord);
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message == "123")
                                {
                                }
                                successX = false;
                                successZ = false;
                            }


                            if (successX && successZ)
                            {

                                //double centerX = (xCoord - (m_terrainSceneStorage.m_terrainTilesX / 2f)) * m_terrainSceneStorage.m_terrainTilesSize + (m_terrainSceneStorage.m_terrainTilesSize /2f);
                                //double centerZ = (zCoord - (m_terrainSceneStorage.m_terrainTilesZ / 2f)) * m_terrainSceneStorage.m_terrainTilesSize + (m_terrainSceneStorage.m_terrainTilesSize / 2f);
                                Vector2 offset = new Vector2(-m_terrainSceneStorage.m_terrainTilesSize * m_terrainSceneStorage.m_terrainTilesX * 0.5f, -m_terrainSceneStorage.m_terrainTilesSize * m_terrainSceneStorage.m_terrainTilesZ * 0.5f);
                                Vector3Double position = new Vector3(m_terrainSceneStorage.m_terrainTilesSize * xCoord + offset.x, 0, m_terrainSceneStorage.m_terrainTilesSize * zCoord + offset.y);
                                Vector3Double center = new Vector3Double(position + new Vector3Double(m_terrainSceneStorage.m_terrainTilesSize / 2f, 0f, m_terrainSceneStorage.m_terrainTilesSize / 2f));
                                BoundsDouble bounds = new BoundsDouble(center, new Vector3Double(m_terrainSceneStorage.m_terrainTilesSize, m_terrainSceneStorage.m_terrainTilesSize * 4, m_terrainSceneStorage.m_terrainTilesSize));
                                //Use forward slashes in the path - The Unity scene management classes expect it that way
                                path = path.Replace("\\", "/");
                                TerrainScene terrainScene = new TerrainScene()
                                {
                                    m_scenePath = path,
                                    m_pos = position,
                                    m_bounds = bounds,
                                    m_useFloatingPointFix = m_terrainSceneStorage.m_useFloatingPointFix
                                };

                                if (File.Exists(path.Replace("Terrain", GaiaConstants.ImpostorTerrainName)))
                                {
                                    terrainScene.m_impostorScenePath = path.Replace("Terrain", GaiaConstants.ImpostorTerrainName);
                                }

                                if (File.Exists(path.Replace("Terrain", "Collider")))
                                {
                                    terrainScene.m_colliderScenePath = path.Replace("Terrain", "Collider");
                                }

                                if (File.Exists(path.Replace("Terrain", "Backup")))
                                {
                                    terrainScene.m_backupScenePath = path.Replace("Terrain", "Backup");
                                }

                                m_terrainSceneStorage.m_terrainScenes.Add(terrainScene);
                                madeChanges = true;
                            }
                        }
                    }
                }
                if (madeChanges)
                {
                    EditorUtility.SetDirty(m_terrainSceneStorage);
                    AssetDatabase.SaveAssets();
                }
            }

            m_lastUsedGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_terrainSceneStorage));


            RefreshTerrainsWithCurrentData();

            RefreshSceneViewLoadingRange();

            ////Go over the currently open scene and close the ones that do not seem to have a reference on them
            //for (int i = EditorSceneManager.loadedSceneCount-1; i >= 0; i--)
            //{
            //    Scene scene = EditorSceneManager.GetSceneAt(i);
            //    if (EditorSceneManager.GetActiveScene().Equals(scene))
            //    {
            //        continue;
            //    }
            //      TerrainScene terrainScene = m_terrainSceneStorage.m_terrainScenes.Find(x => x.m_scenePath == scene.path || x.m_impostorScenePath == scene.path || x.m_colliderScenePath == scene.path);
            //      if (terrainScene != null)
            //      {
            //            terrainScene.UpdateWithCurrentData();
            //      }
            //      else
            //      {
            //            EditorSceneManager.UnloadSceneAsync(scene);
            //      }
            //}
#endif
        }


        private Terrain m_worldMapTerrain;
        public Terrain WorldMapTerrain
        {
            get
            {
                if (m_worldMapTerrain == null)
                {
                    m_worldMapTerrain = TerrainHelper.GetWorldMapTerrain();
                }
                return m_worldMapTerrain;
            }
        }

        private GameObject m_terrainGO;
        public GameObject TerrainGO
        {
            get
            {
                if (m_terrainGO == null)
                {
                    m_terrainGO = GaiaUtils.GetTerrainObject();
                }
                return m_terrainGO;
            }
        }


        public static List<TerrainScene> TerrainScenes
        {
            get
            {
                return Instance.TerrainSceneStorage.m_terrainScenes;
            }
        }

        public static bool TerrainSceneStorageCreated
        {
            get
            {
                return Instance.m_terrainSceneStorage != null;
            }
        }


        private bool m_showWorldMapTerrain;
        public bool ShowWorldMapTerrain
        {
            get
            {
                return m_showWorldMapTerrain;
            }
            private set
            {
                m_showWorldMapTerrain = value;
                if (WorldMapTerrain != null)
                {
                    if (m_showWorldMapTerrain)
                    {
                        WorldMapTerrain.gameObject.SetActive(true);
                    }
                    else
                    {
                        WorldMapTerrain.gameObject.SetActive(false);
                    }
                }
            }
        }

        private bool m_showLocalTerrain = true;
        private bool m_runtimeInitialized;
        [SerializeField]
        private string m_lastUsedGUID;
        private bool m_progressTrackingRunning;
        private long m_lastLoadingProgressTimeStamp;
        private float m_lastTrackedProgressValue;


        public bool ShowLocalTerrain
        {
            get
            {
                return m_showLocalTerrain;
            }
            private set
            {
                if (value != m_showLocalTerrain)
                {
                    m_showLocalTerrain = value;
                    if (GaiaUtils.HasDynamicLoadedTerrains())
                    {
                        if (!m_showLocalTerrain)
                        {
                            TerrainLoaderManager.Instance.UnloadAll();
                        }
                    }
                    else
                    {

                        foreach (Transform child in TerrainGO.transform)
                        {
                            Terrain t = child.GetComponent<Terrain>();
                            if (t != null)
                            {
                                t.drawHeightmap = m_showLocalTerrain;
                                t.drawTreesAndFoliage = m_showLocalTerrain;
                                //Activate / deactivate all Childs below the terrain
                                foreach (Transform subTrans in t.transform)
                                {
                                    subTrans.gameObject.SetActive(m_showLocalTerrain);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static bool ColliderOnlyLoadingActive
        {
            get
            {
                return Instance.TerrainSceneStorage.m_colliderOnlyLoading;
            }
        }
#if GAIA_PRO_PRESENT
        /// <summary>
        /// Triggered when load progress tracking starts in the Terrain Loader Manager.
        /// </summary>
        public delegate void LoadProgressStartedCallback();
        public event LoadProgressStartedCallback OnLoadProgressStarted;

        /// <summary>
        /// Triggered when there is an update on the load progress during load progress tracking.
        /// </summary>
        /// <param name="progress">The load progress expressed as scalar value (0 to 1)</param>
        public delegate void LoadProgressUpdatedCallback(float progress);
        public event LoadProgressUpdatedCallback OnLoadProgressUpdated;

        /// <summary>
        /// Triggered when load progress tracking ends in the Terrain Loader Manager.
        /// </summary>
        public delegate void LoadProgressEndedCallback();
        public event LoadProgressEndedCallback OnLoadProgressEnded;

        /// <summary>
        /// Triggered when load progress tracking times out in the Terrain Loader Manager.
        /// </summary>
        /// <param name="missingTerrainScenes">List of terrain scenes which are referenced, but not loaded yet at the point of the timeout.</param>
        public delegate void LoadProgressTimeOutCallback(List<TerrainScene> missingTerrainScenes);
        public event LoadProgressTimeOutCallback OnLoadProgressTimeOut;
#endif

        public void Start()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                if (instance != this)
                {
                    Destroy(this);
                }
            }

            //Nothing to do if no terrain loading
            if (!GaiaUtils.HasDynamicLoadedTerrains())
            {
                return;
            }

            LookUpLoadingScreen();

            //Process the runtime deactivation if the "Collider Only" mode is active
            if (TerrainSceneStorage.m_colliderOnlyLoading)
            {
                DeactivateIfRequested(m_terrainSceneStorage.m_deactivateRuntimePlayer, GaiaConstants.gaiaPlayerObject);
                DeactivateIfRequested(m_terrainSceneStorage.m_deactivateRuntimeLighting, GaiaConstants.gaiaLightingObject);
                DeactivateIfRequested(m_terrainSceneStorage.m_deactivateRuntimeAudio, GaiaConstants.gaiaAudioObject);
                DeactivateIfRequested(m_terrainSceneStorage.m_deactivateRuntimeWeather, GaiaConstants.gaiaWeatherObject);
                DeactivateIfRequested(m_terrainSceneStorage.m_deactivateRuntimeWater, GaiaConstants.gaiaWaterObject);
                DeactivateIfRequested(m_terrainSceneStorage.m_deactivateRuntimeScreenShotter, GaiaConstants.gaiaScreenshotter);
            }
            UnloadAll();
            m_runtimeInitialized = true;

#if GAIA_PRO_PRESENT
            if (m_loadingScreen != null)
            {
                m_loadingScreen.gameObject.SetActive(true);
            }

             StartTrackingProgress();
#endif



        }

        private void Update()
        {
#if GAIA_PRO_PRESENT
            if (m_progressTrackingRunning)
            {
                float progress = 0f;
                int loadedScenesCount = TerrainSceneStorage.m_terrainScenes.FindAll(x => x.RegularReferences.Count > 0 && x.m_regularLoadState == LoadState.Loaded).Count;
                int referencedScenesCount = TerrainSceneStorage.m_terrainScenes.FindAll(x => x.RegularReferences.Count > 0).Count;
                int loadedScenesImpostorCount = TerrainSceneStorage.m_terrainScenes.FindAll(x => x.RegularReferences.Count == 0 && x.ImpostorReferences.Count > 0 && x.m_impostorLoadState == LoadState.Loaded).Count;
                int referencedImpostorScenesCount = TerrainSceneStorage.m_terrainScenes.FindAll(x => x.RegularReferences.Count == 0 && x.ImpostorReferences.Count > 0).Count;
                float loadedScenesTotal = loadedScenesCount + loadedScenesImpostorCount;
                float referencedScenesTotal = referencedScenesCount + referencedImpostorScenesCount;
                if (referencedScenesTotal > 0)
                {
                    progress = loadedScenesTotal / referencedScenesTotal;
                }
                else
                {
                    m_progressTrackingRunning = false;
                    if (OnLoadProgressEnded != null)
                    {
                        OnLoadProgressEnded();
                    }
                }

                if (OnLoadProgressUpdated !=null)
                {
                    OnLoadProgressUpdated(progress);
                }

                if (progress >= 1f)
                {
                    m_progressTrackingRunning = false;
                    if (OnLoadProgressEnded != null)
                    {
                        OnLoadProgressEnded();
                    }
                }
                else
                {
                    long currentTimeStamp = GaiaUtils.GetUnixTimestamp();

                    //Check if we made progress since the last update
                    if (progress != m_lastTrackedProgressValue)
                    {
                        //we made loading progress, update the progress and timestamp
                        m_lastTrackedProgressValue = progress;
                        m_lastLoadingProgressTimeStamp = currentTimeStamp;
                    }
                    else
                    {
                        //no load progress anymore? Time out the loading tracking eventually
                        if (m_lastLoadingProgressTimeStamp + m_trackLoadingProgressTimeOut < currentTimeStamp)
                        {
                            m_progressTrackingRunning = false;

                            if (OnLoadProgressTimeOut != null)
                            {
                                List<TerrainScene> missingScenes = m_terrainSceneStorage.m_terrainScenes.FindAll(x => (x.RegularReferences.Count > 0 && x.m_regularLoadState != LoadState.Loaded) || (x.ImpostorReferences.Count > 0 && x.m_impostorLoadState != LoadState.Loaded));
                                OnLoadProgressTimeOut(missingScenes);
                            }

                            if (OnLoadProgressEnded != null)
                            {
                                OnLoadProgressEnded();
                            }
                        }
                    }
                }
            }
#endif
        }

        public void StartTrackingProgress()
        {
#if GAIA_PRO_PRESENT
            //Update all runtime loaders to make sure the current references are set when we start tracking
            var allLoaders = Resources.FindObjectsOfTypeAll<TerrainLoader>();
            foreach (TerrainLoader terrainLoader in allLoaders.Where(x => x.LoadMode == LoadMode.RuntimeAlways))
            {
                terrainLoader.UpdateTerrains();
            }

            m_progressTrackingRunning = true;
            m_lastLoadingProgressTimeStamp = GaiaUtils.GetUnixTimestamp();

            if (OnLoadProgressStarted != null)
            {
                OnLoadProgressStarted();
            }
#endif
        }

        public void LookUpLoadingScreen()
        {
#if GAIA_PRO_PRESENT
            if (m_loadingScreen == null && GaiaUtils.HasDynamicLoadedTerrains())
            {
                var loadingScreens = Resources.FindObjectsOfTypeAll<GaiaLoadingScreen>();
                if (loadingScreens.Length > 0)
                {
                    m_loadingScreen = loadingScreens[0];
                }
            }
#endif
        }

        /// <summary>
        /// Deactivates the Game Object with the given name if requested & if it exists
        /// </summary>
        /// <param name="requested">If the deactivation is requested</param>
        /// <param name="gameObjectName">The name of the Game Object that will be deactivated</param>
        private void DeactivateIfRequested(bool requested, string gameObjectName)
        {
            if (requested)
            {
                GameObject gameObject = GameObject.Find(gameObjectName);
                if (gameObject != null)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public void Reset()
        {
#if GAIA_PRO_PRESENT
            m_allFloatingPointFixMembers.Clear();
            m_allWorldSpaceParticleSystems.Clear();
#endif
        }

        void OnApplicationQuit()
        {
            UnloadAll();
        }

        private void OnEnable()
        {

            if (instance == null)
            {
                instance = this;
            }
            else
            {
                if (instance != this)
                {
                    Destroy(this);
                }
            }
            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();

        }

        public void ResetTimestamps()
        {
            foreach (TerrainScene ts in m_terrainSceneStorage.m_terrainScenes)
            {
                ts.m_impostorCachedTimestamp = 0;
                ts.m_regularCachedTimestamp = 0;
                ts.m_nextUpdateTimestamp = 0;
            }
        }

        public Vector3Double GetOrigin()
        {
            return new Vector3Double(m_sceneViewOriginLoadingBounds.center);
        }

        public void SetOrigin(Vector3Double newOrigin)
        {
#if GAIA_PRO_PRESENT
            if (newOrigin != m_sceneViewOriginLoadingBounds.center)
            {
                //a origin shift has occured, 
                Vector3Double shiftDifference = newOrigin - m_sceneViewOriginLoadingBounds.center;

                //Don't shift on y-axis this will only lead to problems with sea level, height based rules, etc.
                //and should not be required under normal circumstances.
                shiftDifference.y = 0;

                //shift all tools such as stampers and spawners
                //Stamper[] allStampers = Resources.FindObjectsOfTypeAll<Stamper>();
                //foreach (Stamper stamper in allStampers)
                //{
                //    stamper.transform.position = (Vector3)((Vector3Double)stamper.transform.position + m_originLoadingBounds.center - shiftDifference);
                //}

                //if not in playmode, shift the player, if exists, very confusing otherwise
                if (!Application.isPlaying)
                {
                    GameObject playerObj = GameObject.Find(GaiaConstants.playerFlyCamName);

                    if (playerObj == null)
                    {
                        playerObj = GameObject.Find(GaiaConstants.playerFirstPersonName);
                    }

                    if (playerObj == null)
                    {
                        playerObj = GameObject.Find(GaiaConstants.playerThirdPersonName);
                    }

                    if (playerObj != null)
                    {
                        playerObj.transform.position = (Vector3)((Vector3Double)playerObj.transform.position - shiftDifference);
                    }

                    //Move spawners also only when not in playmode
                    Spawner[] allSpawners = Resources.FindObjectsOfTypeAll<Spawner>();
                    foreach (Spawner spawner in allSpawners)
                    {
                        spawner.transform.position = (Vector3)((Vector3Double)spawner.transform.position - shiftDifference);
                    }

                    //Move world map stamp tokens also only when not in playmode
                    WorldMapStampToken[] mapStampTokens = Resources.FindObjectsOfTypeAll<WorldMapStampToken>();
                    foreach (WorldMapStampToken token in mapStampTokens)
                    {
                        token.UpdateGizmoPos();
                    }

                    //When the application is not playing we can look for all floating point fix members, if it is playing we should
                    //rely on the list of members being filled correctly at the start of the scene
                    m_allFloatingPointFixMembers = Resources.FindObjectsOfTypeAll<FloatingPointFixMember>().ToList();
                }
                m_allFloatingPointFixMembers.RemoveAll(x => x == null);
                foreach (FloatingPointFixMember member in m_allFloatingPointFixMembers)
                {
                    member.transform.position = (Vector3)((Vector3Double)member.transform.position - shiftDifference);
                }


                //shift world space particles accordingly - only worth dealing with during playmode 
                if (Application.isPlaying)
                {
                    m_allWorldSpaceParticleSystems.RemoveAll(x => x == null);
                    foreach (ParticleSystem ps in m_allWorldSpaceParticleSystems)
                    {
                        bool wasPaused = ps.isPaused;
                        bool wasPlaying = ps.isPlaying;
                        ParticleSystem.Particle[] currentParticles = null;

                        if (!wasPaused)
                            ps.Pause();

                        if (currentParticles == null || currentParticles.Length < ps.main.maxParticles)
                        {
                            currentParticles = new ParticleSystem.Particle[ps.main.maxParticles];
                        }

                        int num = ps.GetParticles(currentParticles);

                        for (int i = 0; i < num; i++)
                        {
                            currentParticles[i].position -= (Vector3)shiftDifference;
                        }

                        ps.SetParticles(currentParticles, num);

                        if (wasPlaying)
                            ps.Play();
                    }
                }

                m_sceneViewOriginLoadingBounds.center = newOrigin;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif

                //if (WorldMapTerrain != null)
                //{
                //    WorldMapTerrain.transform.position = -m_originLoadingBounds.center - (new Vector3Double(WorldMapTerrain.terrainData.size.x / 2f, 0f, WorldMapTerrain.terrainData.size.z / 2f));
                //}

                //Update terrain loading state for all terrains since the session manager loads itself around the origin
                RefreshSceneViewLoadingRange();
            }
#endif
        }

        public void SetOriginByTargetTile(int tileX = -99, int tileZ = -99)
        {
            if (tileX == -99)
            {
                tileX = m_originTargetTileX;
            }

            if (tileZ == -99)
            {
                tileZ = m_originTargetTileZ;
            }

            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                //Get the terrain tile by X / Z tile in the scene path
                TerrainScene targetScene = TerrainLoaderManager.TerrainScenes.Find(x => x.m_scenePath.Contains("Terrain_" + tileX.ToString() + "_" + tileZ.ToString()));
                if (targetScene != null)
                {
                    SetOrigin(new Vector3Double(targetScene.m_pos.x + (m_terrainSceneStorage.m_terrainTilesSize / 2f), 0f, targetScene.m_pos.z + (m_terrainSceneStorage.m_terrainTilesSize / 2f)));
                    string terrainName = targetScene.GetTerrainName();
                    GameObject go = GameObject.Find(terrainName);
                    if (go != null)
                    {
#if UNITY_EDITOR
                        Selection.activeObject = go;
#endif
                    }
                }
                else
                {
                    Debug.LogWarning("Could not find a terrain with the tile coordinates " + tileX.ToString() + "-" + tileZ.ToString() + " in the available terrains. Please check if these coordinates are within the available bounds.");
                }
            }
            else
            {
                Terrain t = Terrain.activeTerrains.Where(x => x.name.Contains("Terrain_" + tileX.ToString() + "_" + tileZ.ToString())).First();
                if (t != null)
                {
                    SetOrigin(new Vector3Double(t.transform.position.x + (t.terrainData.size.x / 2f), 0f, t.transform.position.z + (t.terrainData.size.z / 2f)));
#if UNITY_EDITOR
                    Selection.activeObject = t.gameObject;
#endif
                }
                else
                {
                    Debug.LogWarning("Could not find a terrain with the tile coordinates " + tileX.ToString() + "-" + tileZ.ToString() + " in the scene.");
                }
            }


        }
        /// <summary>
        /// Updates the caching mechanism with the new settings - effectively this does only unload cached terrains which are not allowed to be cached anymore due to the new settings.
        /// </summary>
        public void UpdateCaching()
        {
            long currentTimeStamp = GaiaUtils.GetUnixTimestamp();
            foreach (TerrainScene terrainScene in m_terrainSceneStorage.m_terrainScenes)
            {
                if (terrainScene.m_impostorLoadState == LoadState.Cached && (!CachingAllowed() || terrainScene.m_impostorCachedTimestamp + m_cacheKeepAliveTime < currentTimeStamp))
                {
                    terrainScene.RemoveAllImpostorReferences(true);
                }
                if (terrainScene.m_regularLoadState == LoadState.Cached && (!CachingAllowed() || terrainScene.m_regularCachedTimestamp + m_cacheKeepAliveTime < currentTimeStamp))
                {
                    terrainScene.RemoveAllReferences(true);
                }
            }
        }


        /// <summary>
        /// Load and unload the terrain scenes stored in the current session for a certain object
        /// </summary>
        public void UpdateTerrainLoadState(BoundsDouble loadingBoundsRegular = null, BoundsDouble loadingBoundsImpostor = null, GameObject requestingObject = null, float minDistance = 0, float maxDistance = 0, float minThresholdMS = 0, float maxThresholdMS = 0)
        {
            //Do not accept changes to load state during runtime when there was no runtime init yet
            if (Application.isPlaying && !m_runtimeInitialized)
            {
                return;
            }

            //Do not perform any updates when terrain loading is disabled per default
#if GAIA_PRO_PRESENT
            if (TerrainLoaderManager.Instance.m_terrainSceneStorage == null || !TerrainLoaderManager.Instance.m_terrainSceneStorage.m_terrainLoadingEnabled)
            {
                return;
            }
#endif


            if (requestingObject == null)
            {
                requestingObject = gameObject;
            }

            long currentTimeStamp = GaiaUtils.GetUnixTimestamp();

            m_terrainSceneActionQueue.Clear();

            foreach (TerrainScene terrainScene in TerrainLoaderManager.TerrainScenes)
            {
                if (terrainScene.m_nextUpdateTimestamp > currentTimeStamp)
                {
                    continue;
                }
                float distance = Vector3.Distance(terrainScene.m_bounds.center - terrainScene.m_currentOriginOffset, requestingObject.transform.position);
                terrainScene.m_currentOriginOffset = m_sceneViewOriginLoadingBounds.center;
                bool wasChanged = false;
                //only evaluate load state if local terrain is supposed to be displayed
                if (m_showLocalTerrain)
                {
                    if (loadingBoundsImpostor != null && loadingBoundsImpostor.extents.magnitude > 0 && loadingBoundsImpostor.extents.magnitude > loadingBoundsRegular.extents.magnitude)
                    {
                        if (terrainScene.m_bounds.Intersects(loadingBoundsImpostor) && !TerrainSceneStorage.m_colliderOnlyLoading)
                        {
                            if (!terrainScene.HasImpostorReference(requestingObject) || terrainScene.m_impostorLoadState == LoadState.Unloaded)
                            {
                                //terrainScene.AddImpostorReference(requestingObject);
                                AddToTerrainSceneActionQueue(terrainScene, ReferenceChange.AddImpostorReference, distance);
                                terrainScene.m_useFloatingPointFix = m_terrainSceneStorage.m_useFloatingPointFix;
                                wasChanged = true;
                            }
                        }
                        else
                        {
                            if (terrainScene.HasImpostorReference(requestingObject) || terrainScene.m_impostorLoadState == LoadState.Loaded)
                            {
                                //terrainScene.RemoveImpostorReference(requestingObject, m_cacheMemoryThreshold);
                                AddToTerrainSceneActionQueue(terrainScene, ReferenceChange.RemoveImpostorReference, distance);
                                wasChanged = true;
                            }
                            if (terrainScene.m_impostorLoadState == LoadState.Cached && (!CachingAllowed() || terrainScene.m_impostorCachedTimestamp + m_cacheKeepAliveTime < currentTimeStamp))
                            {
                                //terrainScene.RemoveImpostorReference(requestingObject, m_cacheMemoryThreshold, true);
                                AddToTerrainSceneActionQueue(terrainScene, ReferenceChange.RemoveImpostorReference, distance, true);

                            }
                        }
                    }
                    else
                    {
                        if (terrainScene.HasImpostorReference(requestingObject))
                        {
                            //terrainScene.RemoveImpostorReference(requestingObject, m_cacheMemoryThreshold);
                            AddToTerrainSceneActionQueue(terrainScene, ReferenceChange.RemoveImpostorReference, distance);
                            wasChanged = true;
                        }
                    }

                    if (loadingBoundsRegular != null && loadingBoundsRegular.extents.magnitude > 0)
                    {
                        if (terrainScene.m_bounds.Intersects(loadingBoundsRegular))
                        {
                            if (!terrainScene.HasRegularReference(requestingObject) || terrainScene.m_regularLoadState == LoadState.Unloaded)
                            {
                                //terrainScene.AddRegularReference(requestingObject);
                                AddToTerrainSceneActionQueue(terrainScene, ReferenceChange.AddRegularReference, distance);
                                terrainScene.m_useFloatingPointFix = m_terrainSceneStorage.m_useFloatingPointFix;
                                wasChanged = true;
                            }
                        }
                        else
                        {
                            if (terrainScene.HasRegularReference(requestingObject) || terrainScene.m_regularLoadState == LoadState.Loaded)
                            {
                                //terrainScene.RemoveRegularReference(requestingObject, m_cacheMemoryThreshold);
                                AddToTerrainSceneActionQueue(terrainScene, ReferenceChange.RemoveRegularReference, distance);
                                wasChanged = true;
                            }
                            if (terrainScene.m_regularLoadState == LoadState.Cached && (!CachingAllowed() || terrainScene.m_regularCachedTimestamp + m_cacheKeepAliveTime < currentTimeStamp))
                            {
                                //terrainScene.RemoveRegularReference(requestingObject, m_cacheMemoryThreshold, true);
                                AddToTerrainSceneActionQueue(terrainScene, ReferenceChange.RemoveRegularReference, distance, true);
                            }
                        }
                    }
                }
                terrainScene.ShiftLoadedTerrain();

                if (Application.isPlaying && !wasChanged)
                {
                    long threshold = +(long)Mathf.Lerp(minThresholdMS, maxThresholdMS, Mathf.InverseLerp(minDistance, maxDistance, Vector3.Distance(loadingBoundsRegular.center, terrainScene.m_bounds.center))) + UnityEngine.Random.Range(10, 50);
                    terrainScene.m_nextUpdateTimestamp = currentTimeStamp + threshold;
                }
                else
                {
                    terrainScene.m_nextUpdateTimestamp = 0;
                }
            }
            for (int i = 0; i < m_terrainSceneActionQueue.Count; i++)
            {
                switch (m_terrainSceneActionQueue[i].m_referenceChange)
                {
                    case ReferenceChange.AddImpostorReference:
                        m_terrainSceneActionQueue[i].m_terrainScene.AddImpostorReference(requestingObject);
                        break;
                    case ReferenceChange.AddRegularReference:
                        m_terrainSceneActionQueue[i].m_terrainScene.AddRegularReference(requestingObject);
                        break;
                    case ReferenceChange.RemoveImpostorReference:
                        m_terrainSceneActionQueue[i].m_terrainScene.RemoveImpostorReference(requestingObject, m_cacheMemoryThreshold, m_terrainSceneActionQueue[i].m_forced);
                        break;
                    case ReferenceChange.RemoveRegularReference:
                        m_terrainSceneActionQueue[i].m_terrainScene.RemoveRegularReference(requestingObject, m_cacheMemoryThreshold, m_terrainSceneActionQueue[i].m_forced);
                        break;
                }
            }
        }

        private void AddToTerrainSceneActionQueue(TerrainScene terrainScene, ReferenceChange referenceChange, float distance, bool forced = false)
        {
            int index = m_terrainSceneActionQueue.FindIndex(x => x.m_distance > distance);
            if (index != -1)
            {
                //We found an entry with a larger distance -> insert it in before that
                m_terrainSceneActionQueue.Insert(index, new TerrainSceneActionQueueEntry { m_terrainScene = terrainScene, m_referenceChange = referenceChange, m_forced = forced, m_distance = distance });
            }
            else
            {
                //no larger distance found, this needs to go to the end of the queue
                m_terrainSceneActionQueue.Add(new TerrainSceneActionQueueEntry { m_terrainScene = terrainScene, m_referenceChange = referenceChange, m_forced = forced, m_distance = distance });
            }

        }

        public bool CachingAllowed()
        {
            return (Application.isPlaying && m_cacheInRuntime) || (!Application.isPlaying && m_cacheInEditor);
        }

        public double GetLoadingRange()
        {
            return m_sceneViewOriginLoadingBounds.extents.x;
        }

        public double GetImpostorLoadingRange()
        {
            return m_sceneViewImpostorLoadingBounds.extents.x;
        }

        public Vector3Double GetLoadingSize()
        {
            return new Vector3Double(m_sceneViewOriginLoadingBounds.size);
        }

        public Vector3Double GetImpostorLoadingSize()
        {
            return new Vector3Double(m_sceneViewImpostorLoadingBounds.size);
        }


        public Vector3Double GetLoadingCenter()
        {
            if (m_centerSceneViewLoadingOn == CenterSceneViewLoadingOn.SceneViewCamera)
            {
                return m_sceneViewCameraLoadingBounds.center;
            }
            else
            {
                return m_sceneViewOriginLoadingBounds.center;
            }
        }

        public void SwitchToWorldMap()
        {
            ShowWorldMapTerrain = true;
            ShowLocalTerrain = false;
            if (!Application.isPlaying)
            {
                UpdateTerrainLoadState();
            }
#if UNITY_EDITOR
            Selection.activeGameObject = GaiaUtils.GetOrCreateWorldDesigner();
#endif

        }

        public void SwitchToLocalMap(bool useInternalLoadingBounds = false)
        {
            ShowLocalTerrain = true;
            ShowWorldMapTerrain = false;
            if (!Application.isPlaying)
            {
                if (useInternalLoadingBounds)
                {
                    UpdateTerrainLoadState(m_sceneViewOriginLoadingBounds);
                }
                else
                {
                    UpdateTerrainLoadState();
                }
            }
        }


        public void SetLoadingRange(Double regularLoadingRange, Double impostorLoadingRange)
        {
#if UNITY_EDITOR
            if (regularLoadingRange > 0 || impostorLoadingRange > 0)
            {
                Vector3Double targetLoadingExtentsRegular = new Vector3Double(regularLoadingRange, regularLoadingRange, regularLoadingRange);
                Vector3Double targetLoadingExtentsImpostor = new Vector3Double(impostorLoadingRange, impostorLoadingRange, impostorLoadingRange);

                BoundsDouble loadingBounds = m_sceneViewOriginLoadingBounds;
                if (m_centerSceneViewLoadingOn == CenterSceneViewLoadingOn.SceneViewCamera)
                {

                    var allCameras = SceneView.GetAllSceneCameras();
                    if (allCameras.Length > 0)
                    {
                        if (m_sceneViewCameraLoadingBounds.center.Equals((Vector3Double)allCameras[0].transform.position) && m_sceneViewCameraLoadingBounds.extents.Equals(targetLoadingExtentsRegular) && m_sceneViewImpostorLoadingBounds.extents.Equals(targetLoadingExtentsImpostor))
                        {
                            //this exact setup is what is already currently loaded, skip the rest of processing
                            return;
                        }

                        m_sceneViewCameraLoadingBounds.center = (Vector3Double)allCameras[0].transform.position;
                    }
                    loadingBounds = m_sceneViewCameraLoadingBounds;
                }
                loadingBounds.extents = targetLoadingExtentsRegular;
                m_sceneViewOriginLoadingBounds.extents = loadingBounds.extents;
                m_sceneViewImpostorLoadingBounds.center = loadingBounds.center;
                m_sceneViewImpostorLoadingBounds.extents = targetLoadingExtentsImpostor;

                EditorUtility.SetDirty(this);
                UpdateTerrainLoadState(loadingBounds, m_sceneViewImpostorLoadingBounds, gameObject);
            }
            else
            {
                m_sceneViewOriginLoadingBounds.extents = Vector3.zero;
                m_sceneViewImpostorLoadingBounds.extents = Vector3.zero;
                foreach (TerrainScene ts in m_terrainSceneStorage.m_terrainScenes)
                {
                    ts.RemoveRegularReference(gameObject);
                    ts.RemoveImpostorReference(gameObject);
                }
            }
#endif
        }

        public void RefreshSceneViewLoadingRange()
        {
            if (m_centerSceneViewLoadingOn == CenterSceneViewLoadingOn.WorldOrigin)
            {
                UpdateTerrainLoadState(m_sceneViewOriginLoadingBounds, m_sceneViewImpostorLoadingBounds);
            }
            else
            {
                UpdateTerrainLoadState(m_sceneViewCameraLoadingBounds, m_sceneViewImpostorLoadingBounds);
            }
        }

        public void RefreshTerrainsWithCurrentData()
        {
            foreach (TerrainScene ts in TerrainSceneStorage.m_terrainScenes)
            {
                ts.UpdateWithCurrentData();
            }
        }

        public void UnloadAll(bool forceUnload = false)
        {
            if (m_terrainSceneStorage != null)
            {
                foreach (TerrainScene terrainScene in m_terrainSceneStorage.m_terrainScenes)
                {
                    terrainScene.RemoveAllReferences(forceUnload);
                    terrainScene.m_nextUpdateTimestamp = 0;
                }
            }
        }

        public void UnloadAllImpostors(bool forceUnload = false)
        {
            if (m_terrainSceneStorage != null)
            {
                foreach (TerrainScene terrainScene in m_terrainSceneStorage.m_terrainScenes)
                {
                    terrainScene.RemoveAllImpostorReferences(forceUnload);
                    terrainScene.m_nextUpdateTimestamp = 0;
                }
            }
        }

        public void EmptyCache()
        {
            if (m_terrainSceneStorage != null)
            {
                foreach (TerrainScene terrainScene in m_terrainSceneStorage.m_terrainScenes.Where(x => x.RegularReferences.Count() <= 0))
                {
                    terrainScene.RemoveAllReferences(true);
                }
            }
        }

        public void SaveStorageData()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(m_terrainSceneStorage);
            AssetDatabase.SaveAssets();
            LoadStorageData();
#endif
        }

        public TerrainScene GetTerrainSceneAtPosition(Vector3Double center)
        {
            return m_terrainSceneStorage.m_terrainScenes.Find(x => x.m_bounds.Contains(center));
        }

        /// <summary>
        /// Returns the default loading range according to the tile size of a single terrain tile of the scene.
        /// </summary>
        /// <param name="tileSize">The tile size to get the loading range for.</param>
        /// <returns>The default loading range.</returns>
        public static double GetDefaultLoadingRangeForTilesize(int tileSize)
        {
            return Mathf.Round((tileSize * 1.9f) / 2f);
        }
    }
}
