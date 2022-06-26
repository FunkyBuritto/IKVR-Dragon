using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Experimental.TerrainAPI;
using System.Linq;
using static Gaia.GaiaConstants;
using System.Text;
using UnityEngine.SceneManagement;
using ProceduralWorlds.WaterSystem;
#if CTS_PRESENT
using CTS;
#endif

#if UNITY_EDITOR
using UnityEditor.UIElements;
using UnityEditor.SceneManagement;
using UnityEditor;
#endif
using ProceduralWorlds.HierachySystem;

namespace Gaia
{
    [System.Serializable]
    public enum ClearSpawnFrom { AnySource, OnlyThisSpawner }
    public enum ClearSpawnFromBiomes { AnySource, OnlyThisBiome }
    public enum ClearSpawnFor { CurrentTerrainOnly, AllTerrains }

    /// <summary>
    /// simple data structure to store a protototype id and a terrain together
    /// used to log which prototype ids were already deleted from a terrain during world spawns
    /// </summary>
    public class TerrainPrototypeId
    {
        public Terrain terrain;
        public string m_prototypeAssetGUID;
    }

    /// <summary>
    /// Simple data structure to store spawn rules with missing prototypes per terrain.
    /// This is used during spawning to make sure prototypes are only placed on terrains where they are actually used.
    /// </summary>
    public class TerrainMissingSpawnRules
    {
        public Terrain terrain;
        public List<SpawnRule> spawnRulesWithMissingResources = new List<SpawnRule>();
    }

    /// <summary>
    /// Data structure to pass in terrain position information into the simulate compute shader
    /// </summary>
    struct TerrainPosition
    {
        public int terrainID;
        public Vector2Int min;
        public Vector2Int max;
        public int affected;
    };

    /// <summary>
    /// A generic spawning system.
    /// </summary>
    [ExecuteInEditMode]
    [System.Serializable]
    public class Spawner : MonoBehaviour
    {

        [SerializeField]
        private SpawnerSettings settings;
        /// <summary>
        /// The current spawner settings
        /// </summary>
        public SpawnerSettings m_settings
        {
            get
            {
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<SpawnerSettings>();
                    settings.m_resources = new GaiaResource();
                    settings.m_resources.m_name = "NewResources";
                }
                return settings;
            }
            set
            {
                settings = value;
            }
        }



        [SerializeField]
        private WorldCreationSettings worldCreationSettings;
        /// <summary>
        /// The settings for world creation if this is a random terrain generator spawner running on the world map
        /// </summary>
        public WorldCreationSettings m_worldCreationSettings
        {
            get
            {
                if (worldCreationSettings == null)
                {
                    worldCreationSettings = ScriptableObject.CreateInstance<WorldCreationSettings>();
                }
                return worldCreationSettings;
            }
            set
            {
                worldCreationSettings = value;
            }
        }



        [SerializeField]
        private BaseTerrainSettings baseTerrainsettings;
        /// <summary>
        /// The settings for base terrain creation in this is a random terrain generation spawner on a world map
        /// </summary>
        public BaseTerrainSettings m_baseTerrainSettings
        {
            get
            {
                if (baseTerrainsettings == null)
                {
                    baseTerrainsettings = ScriptableObject.CreateInstance<BaseTerrainSettings>();
                }
                return baseTerrainsettings;
            }
            set
            {
                baseTerrainsettings = value;
            }
        }

        //Holds all the generated stamps for random generation
        public List<StamperSettings> m_worldMapStamperSettings = new List<StamperSettings>();

#if GAIA_PRO_PRESENT
        private TerrainLoader m_terrainLoader;
        public TerrainLoader TerrainLoader
        {
            get
            {
                if (m_terrainLoader == null)
                {
                    if (this != null)
                    {
                        m_terrainLoader = gameObject.GetComponent<TerrainLoader>();

                        if (m_terrainLoader == null)
                        {
                            m_terrainLoader = gameObject.AddComponent<TerrainLoader>();
                            m_terrainLoader.hideFlags = HideFlags.HideInInspector;
                        }
                    }
                }
                return m_terrainLoader;
            }
        }
        public LoadMode m_loadTerrainMode = LoadMode.EditorSelected;
        public int m_impostorLoadingRange;
#endif

#if CTS_PRESENT

        [System.NonSerialized]
        private CTSProfile m_connectedCTSProfile = null;

        //This construct ensures we only serialize the GUID of the CTS profile, but not the profile itself
        //The GUID will "survive" when CTS is not installed in a project, while the CTS profile object would not
        public CTSProfile ConnectedCTSProfile
        {
            get
            {
                if (m_connectedCTSProfile == null && m_connectedCTSProfileGUID != null)
                {
#if UNITY_EDITOR
                    m_connectedCTSProfile = (CTSProfile)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_connectedCTSProfileGUID), typeof(CTSProfile));
#endif
                }
                return m_connectedCTSProfile;
            }
            set
            {
#if UNITY_EDITOR
                m_connectedCTSProfileGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
                m_connectedCTSProfile = value;
#endif
            }
        }

#endif

        //need this serialized to remember the GUID even when PP is not installed in the project
        [SerializeField]
        private string m_connectedCTSProfileGUID = "";

        public delegate void SpawnFinishedCallback();
#if UNITY_EDITOR
        public event SpawnFinishedCallback OnSpawnFinished;
#endif

        /// <summary>
        /// The spawner ID
        /// </summary>
        public string m_spawnerID = Guid.NewGuid().ToString();

        /// <summary>
        /// Operational mode of the spawner
        /// </summary>
        public Gaia.GaiaConstants.OperationMode m_mode = GaiaConstants.OperationMode.DesignTime;

        /// <summary>
        /// Source for the random number generator
        /// </summary>
        public int m_seed = DateTime.Now.Millisecond;


        /// <summary>
        /// The world map terrain
        /// </summary>
        public Terrain m_worldMapTerrain;

        /// <summary>
        /// The shape of the spawner
        /// </summary>
        public Gaia.GaiaConstants.SpawnerShape m_spawnerShape = GaiaConstants.SpawnerShape.Box;

        /// <summary>
        /// The rule selection approach
        /// </summary>
        public Gaia.GaiaConstants.SpawnerRuleSelector m_spawnRuleSelector = GaiaConstants.SpawnerRuleSelector.WeightedFittest;

        /// <summary>
        /// The type of spawner
        /// </summary>
        public Gaia.GaiaConstants.SpawnerLocation m_spawnLocationAlgorithm = GaiaConstants.SpawnerLocation.RandomLocation;

        /// <summary>
        /// The type of check performed at every location
        /// </summary>
        public Gaia.GaiaConstants.SpawnerLocationCheckType m_spawnLocationCheckType = GaiaConstants.SpawnerLocationCheckType.PointCheck;

        /// <summary>
        /// The step amount used when EveryLocation is selected
        /// </summary>
        public float m_locationIncrement = 1f;

        /// <summary>
        /// The maximum random offset on a jittered location
        /// </summary>
        public float m_maxJitteredLocationOffsetPct = 0.9f;

        /// <summary>
        /// Number of times a check is made for a new spawn location every interval 
        /// </summary>
        public int m_locationChecksPerInt = 1;

        /// <summary>
        /// In seeded mode, this will be the maximum number of individual spawns in a cluster before another locaiton is chosen
        /// </summary>
        public int m_maxRandomClusterSize = 50;

        //public GaiaResource m_resources;

        /// <summary>
        /// This will allow the user to filter the relative strength of items spawned by distance from the center
        /// </summary>
        public AnimationCurve m_spawnFitnessAttenuator = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));

        /// <summary>
        /// The image fitness filter mode to apply
        /// </summary>
        public Gaia.GaiaConstants.ImageFitnessFilterMode m_areaMaskMode = Gaia.GaiaConstants.ImageFitnessFilterMode.None;

        /// <summary>
        /// This will enable ot disable the collider cache at runtime - can be quite handy to keep them on some spawners
        /// </summary>
        public bool m_enableColliderCacheAtRuntime = false;

        /// <summary>
        /// This is used to filter the fitness based on the supplied texture, can be used in conjunction with th fitness attenuator
        /// </summary>
        public Texture2D m_imageMask;

        /// <summary>
        /// This is used to invert the fitness based on the supplied texture, can also be used in conjunction with the fitness attenuator
        /// </summary>
        public bool m_imageMaskInvert = false;

        /// <summary>
        /// This is used to normalise the fitness based on the supplied texture, can also be used in conjunction with the fitness attenuator
        /// </summary>
        public bool m_imageMaskNormalise = false;

        /// <summary>
        /// Flip the x, z of the image texture - sometimes required to match source with unity terrain
        /// </summary>
        public bool m_imageMaskFlip = false;

        /// <summary>
        /// This is used to smooth the supplied image mask texture
        /// </summary>
        public int m_imageMaskSmoothIterations = 3;

        /// <summary>
        /// The heightmap for the image filter
        /// </summary>
        [NonSerialized]
        public HeightMap m_imageMaskHM;

        /// <summary>
        /// Our noise generator
        /// </summary>
        private Gaia.FractalGenerator m_noiseGenerator;

        /// <summary>
        /// Seed for noise based fractal
        /// </summary>
        public float m_noiseMaskSeed = 0;

        /// <summary>
        /// The amount of detail in the fractal - more octaves mean more detail and longer calc time.
        /// </summary>
        public int m_noiseMaskOctaves = 8;

        /// <summary>
        /// The roughness of the fractal noise. Controls how quickly amplitudes diminish for successive octaves. 0..1.
        /// </summary>
        public float m_noiseMaskPersistence = 0.25f;

        /// <summary>
        /// The frequency of the first octave
        /// </summary>
        public float m_noiseMaskFrequency = 1f;

        /// <summary>
        /// The frequency multiplier between successive octaves. Experiment between 1.5 - 3.5.
        /// </summary>
        public float m_noiseMaskLacunarity = 1.5f;

        /// <summary>
        /// The zoom level of the noise
        /// </summary>
        public float m_noiseZoom = 10f;

        /// <summary>
        /// Invert the boise value
        /// </summary>
        public bool m_noiseInvert = false;

        /// <summary>
        /// How often the spawner should check to release new instances in seconds
        /// </summary>
        public float m_spawnInterval = 5f;

        /// <summary>
        /// The player to use for distance checks
        /// </summary>
        public string m_triggerTags = "Player";

        /// <summary>
        /// System will only iterate through spawn rules if the player / trigger object is closer than this distance
        /// </summary>
        public float m_triggerRange = 130f;

        /// <summary>
        /// Used to constrain which layers the spawner will attempt to get collisions on - used for virgin detection, terrain detection, tree detection and game object detection
        /// </summary>
        public LayerMask m_spawnCollisionLayers;

        /// <summary>
        /// Set to the terrain layer so that colliders are correctly setup
        /// </summary>
        public int m_spawnColliderLayer = 0;

        /// <summary>
        /// Whether or not to show gizmos
        /// </summary>
        public bool m_showGizmos = true;

        /// <summary>
        /// Whether or not to show debug messages
        /// </summary>
        public bool m_showDebug = false;


        /// <summary>
        /// Set to true once the base terrain has been stamped in a world generator spawner
        /// </summary>
        public bool m_baseTerrainStamped = false;

        /// <summary>
        /// Whether or not to show statistics
        /// </summary>
        //public bool m_showStatistics = true;

        /// <summary>
        /// Whether or not to show the terrain helper
        /// </summary>
        //public bool m_showTerrainHelper = true;

        /// <summary>
        /// Random number generator for this spawner - generates locations
        /// </summary>
        public Gaia.XorshiftPlus m_rndGenerator;

        /// <summary>
        /// Whether or not we are currently caching texures
        /// </summary>
        private bool m_cacheDetails = false;

        /// <summary>
        /// Detail map cache - used when doing area updates on details - indexed by the ID of the terrain it comes from
        /// </summary>
        private Dictionary<int, List<HeightMap>> m_detailMapCache = new Dictionary<int, List<HeightMap>>();

        /// <summary>
        /// Whether or not we are currently caching texures
        /// </summary>
        private bool m_cacheTextures = false;

        /// <summary>
        /// Set to true if the texture map is modified and needs to be written back to the terrain
        /// </summary>
        private bool m_textureMapsDirty = false;

        /// <summary>
        /// Texture map cache - used when doing area updates / reads on textures - indexed by the ID of the terrain it comes from
        /// </summary>
        private Dictionary<int, List<HeightMap>> m_textureMapCache = new Dictionary<int, List<HeightMap>>();

        /// <summary>
        /// Whether or not we are currently caching tags
        /// </summary>
        private bool m_cacheTags = false;

        /// <summary>
        /// Tagged game object cache
        /// </summary>
        private Dictionary<string, Quadtree<GameObject>> m_taggedGameObjectCache = new Dictionary<string, Quadtree<GameObject>>();

        /// <summary>
        /// Whether or not the trees are cached
        /// </summary>
        //private bool m_cacheTrees = false;

        /// <summary>
        /// Tree cache
        /// </summary>
        public TreeManager m_treeCache = new TreeManager();

        /// <summary>
        /// Whether or not we are currently caching height maps
        /// </summary>
        private bool m_cacheHeightMaps = false;

        /// <summary>
        /// Set to true if the height map is modified and needs to be written back to the terrain
        /// </summary>
        private bool m_heightMapDirty = false;

        /// <summary>
        /// Height map cache - used when doing area updates / reads on heightmaps - indexed by the ID of the terrain it comes from
        /// </summary>
        private Dictionary<int, UnityHeightMap> m_heightMapCache = new Dictionary<int, UnityHeightMap>();

        /// <summary>
        /// Whether or not we are currently caching height maps
        /// </summary>
        //private bool m_cacheStamps = false;

        /// <summary>
        /// Stamp cache - used to cache stamps when interacting with heightmaps - activated when heightmap cache is activated
        /// </summary>
        private Dictionary<string, HeightMap> m_stampCache = new Dictionary<string, HeightMap>();

        /// <summary>
        /// The sphere collider cache - used to test for area bounds
        /// </summary>
        [NonSerialized]
        public GameObject m_areaBoundsColliderCache;

        /// <summary>
        /// The game object collider cache - used to test for game object collisions
        /// </summary>
        [NonSerialized]
        public GameObject m_goColliderCache;

        /// <summary>
        /// The game object parent transform - used to make it easier to rehome spawned game objects
        /// </summary>
        [NonSerialized]
        public GameObject m_goParentGameObject;

        /// <summary>
        /// Set to true to cancel the spawn
        /// </summary>
        private static bool m_cancelSpawn = false;

        /// <summary>
        /// Handy counters for statistics
        /// </summary>
        //public int m_totalRuleCnt = 0;
        //public int m_activeRuleCnt = 0;
        //public int m_inactiveRuleCnt = 0;
        //public ulong m_maxInstanceCnt = 0;
        //public ulong m_activeInstanceCnt = 0;
        //public ulong m_inactiveInstanceCnt = 0;
        //public ulong m_totalInstanceCnt = 0;

        /// <summary>
        /// Handy check results - only one check at a time will ever be performed
        /// </summary>
        private float m_terrainHeight = 0f;
        private RaycastHit m_checkHitInfo = new RaycastHit();

        /// <summary>
        /// Use for co-routine simulation
        /// </summary>
        public IEnumerator m_updateCoroutine;
        /// <summary>


        public IEnumerator m_updateCoroutine2;
        /// Amount of time per allowed update
        /// </summary>
        public float m_updateTimeAllowed = 1f / 30f;

        /// <summary>
        /// Current status
        /// </summary>
        public float m_spawnProgress = 0f;

        /// <summary>
        /// Whether or not its completed processing
        /// </summary>
        public bool m_spawnComplete = true;

        /// <summary>
        /// The spawner bounds
        /// </summary>
        public Bounds m_spawnerBounds = new Bounds();

        /// <summary>
        /// Controls whether the spawn Preview needs to be redrawn
        /// </summary>
        public bool m_spawnPreviewDirty;

        /// <summary>
        /// The last active terrain this spawner was displayed for.
        /// </summary>
        public float m_lastActiveTerrainSize = 1024;

        /// <summary>
        /// The state of the "Toggle All" checkbox for regular spawn rules on top / end of the spawn rules list
        /// </summary>
        public bool m_spawnRuleRegularToggleAllState = true;

        /// <summary>
        /// The state of the "Toggle All" checkbox for stamp spawn rules on top / end of the spawn rules list
        /// </summary>
        public bool m_spawnRuleBiomeMasksToggleAllState = true;

        /// <summary>
        /// The state of the "Toggle All" checkbox for world biome mask spawn rules on top / end of the spawn rules list
        /// </summary>
        public bool m_spawnRuleStampsToggleAllState = true;


        /// <summary>
        /// Cached settings that are configired during the init call
        /// </summary>
        private bool m_isTextureSpawner = false;
        private bool m_isDetailSpawner = false;
        private bool m_isTreeSpawnwer = false;
        private bool m_isGameObjectSpawner = false;

        private RenderTexture m_cachedPreviewHeightmapRenderTexture;
        private RenderTexture[] m_cachedPreviewColorRenderTextures = new RenderTexture[GaiaConstants.maxPreviewedTextures];

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

        public bool m_drawPreview = false;
        public List<int> m_previewRuleIds = new List<int>();
        public float m_maxWorldHeight;
        public float m_minWorldHeight;
        public bool m_showSeaLevelinStampPreview = true;
        public bool m_rulePanelUnfolded;
        public bool m_createBaseTerrainUnfolded = true;
        public bool m_exportTerrainUnfolded;
        public bool m_worldBiomeMasksUnfolded;
        public bool m_createdfromBiomePreset;
        public bool m_createdFromGaiaManager;
        public bool m_showSeaLevelPlane = true;
        public bool m_showBoundingBox = true;
        public float m_seaLevel;

        //The Spawner Editor is a complex editor drawing settings for resources, spawner settings, reorderable mask lists, etc.
        //For all this to work it is sometimes required to store the current thing that is "Being Drawn" in a temporary variable so it becomes accessible elsewhere.
        public ImageMask[] m_maskListBeingDrawn;
        public CollisionMask[] m_collisionMaskListBeingDrawn;
        public int m_spawnRuleIndexBeingDrawn;
        public int m_spawnRuleMaskIndexBeingDrawn;
        public ResourceProtoTexture m_textureResourcePrototypeBeingDrawn;
        public ResourceProtoTree m_treeResourcePrototypeBeingDrawn;
        public ResourceProtoDetail m_terrainDetailPrototypeBeingDrawn;
        public ResourceProtoGameObject m_gameObjectResourcePrototypeBeingDrawn;
        public ResourceProtoSpawnExtension m_spawnExtensionPrototypeBeingDrawn;
        public ResourceProtoStampDistribution m_stampDistributionPrototypeBeingDrawn;
        public ResourceProtoWorldBiomeMask m_worldBiomeMaskPrototypeBeingDrawn;
        public ResourceProtoProbe m_probePrototypeBeingDrawn;


        //Lists for cleared prototypes when doing multiterrain world spawns
        //(Textures and terrain details are handled differently)
        private List<TerrainPrototypeId> m_clearedTreeProtos = new List<TerrainPrototypeId>();
        private List<TerrainPrototypeId> m_clearedDetailProtos = new List<TerrainPrototypeId>();
        private List<TerrainPrototypeId> m_clearedGameObjectProtos = new List<TerrainPrototypeId>();
        private List<TerrainPrototypeId> m_clearedSpawnExtensionProtos = new List<TerrainPrototypeId>();
        private List<TerrainPrototypeId> m_clearedStampDistributionProtos = new List<TerrainPrototypeId>();
        private List<TerrainPrototypeId> m_clearedProbeProtos = new List<TerrainPrototypeId>();



        private AnimationCurve m_strengthTransformCurve = ImageMask.NewAnimCurveStraightUpwards();
        private Texture2D m_strengthTransformCurveTexture;
        public bool m_useExistingTerrainForWorldMapExport;
        public bool m_stampOperationsFoldedOut;
        public bool m_worldSizeAdvancedUnfolded;
        public EnvironmentSize m_worldTileSize;


        /// <summary>
        /// Used to store the last world size that this (worldmap-)spawner exported to - this is required to check if the user changed the world size in the meantime.
        /// </summary>
        [SerializeField]
        private Vector3 m_lastStampSpawnWorldSize;

        /// <summary>
        /// Used to store the last heightmap resolution that this (worldmap-)spawner exported to - this is required to check if the user changed the heightmap resolution in the meantime.
        /// </summary>
        private int m_lastExportedHeightMapResolution;

        private Texture2D StrengthTransformCurveTexture
        {
            get
            {
                return ImageProcessing.CreateMaskCurveTexture(ref m_strengthTransformCurveTexture);
            }
        }

        private GaiaSessionManager m_sessionManager;
        public bool m_qualityPresetAdvancedUnfolded;
        public bool m_biomeMaskPanelUnfolded;
        public bool m_spawnStampsPanelUnfolded;
        public bool m_ExportRunning;
        public GaiaConstants.DroppableResourceType m_dropAreaResource;
        public bool m_highlightLoadingSettings;
        public long m_highlightLoadingSettingsStartedTimeStamp;


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

        /// <summary>
        /// Called by unity in editor when this is enabled - unity initialisation is quite opaque!
        /// </summary>
        void OnEnable()
        {
            //Check layer mask
            if (m_spawnCollisionLayers.value == 0)
            {
                m_spawnCollisionLayers = Gaia.TerrainHelper.GetActiveTerrainLayer();
            }

            m_spawnColliderLayer = Gaia.TerrainHelper.GetActiveTerrainLayerAsInt();

            //Create the random generator if we dont have one
            if (m_rndGenerator == null)
            {
                m_rndGenerator = new XorshiftPlus(m_seed);
            }

            //Get the min max height from the current terrain
            UpdateMinMaxHeight();

            if (m_connectedCTSProfileGUID == "1")
            {
                //This is just to get rid off the compilation warning when CTS is not installed in the project
            }


        }

        public void ControlSpawnRuleGUIDs()
        {
            Spawner[] allSpawner = Resources.FindObjectsOfTypeAll<Spawner>();
            foreach (SpawnRule rule in m_settings.m_spawnerRules)
            {
                //check if the spawn rule guid exists in this scene already - if yes, this rule must get a new ID then to avoid duplicate IDs
                if (allSpawner.Select(x => x.m_settings.m_spawnerRules).Where(x => x.Find(y => y.GUID == rule.GUID) != null).Count() > 1)
                {
                    rule.RegenerateGUID();
                }
            }
        }

        private void OnDestroy()
        {
            ImageMask.RefreshSpawnRuleGUIDs();
            m_settings.ClearImageMaskTextures();
        }

        void OnDisable()
        {
        }

        /// <summary>
        /// Start editor updates
        /// </summary>
        public void StartEditorUpdates()
        {
#if UNITY_EDITOR
            m_spawnComplete = false;
            EditorApplication.update += EditorUpdate;
#endif
        }

        //Stop editor updates
        public void StopEditorUpdates()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
            m_spawnComplete = true;
            if (OnSpawnFinished != null)
            {
                OnSpawnFinished();
            }
#endif
        }

        public void UpdateMinMaxHeight()
        {
            SessionManager.GetWorldMinMax(ref m_minWorldHeight, ref m_maxWorldHeight, m_settings.m_isWorldmapSpawner);
            float seaLevel = SessionManager.GetSeaLevel(m_settings.m_isWorldmapSpawner);
            //Iterate through all image masks and set up the current min max height
            //This is fairly important to display the height-dependent mask settings correctly
            //General spawner mask first
            foreach (ImageMask mask in m_settings.m_imageMasks)
            {
                mask.m_maxWorldHeight = m_maxWorldHeight;
                mask.m_minWorldHeight = m_minWorldHeight;
                mask.m_seaLevel = seaLevel;
                mask.CheckHeightMaskMigration();
            }

            ImageMask.CheckMaskStackForInvalidTextureRules("Spawner", this.name, m_settings.m_imageMasks);

            //Now the individual resource masks
            for (int i = 0; i < m_settings.m_spawnerRules.Count; i++)
            {
                ImageMask[] maskStack = m_settings.m_spawnerRules[i].m_imageMasks;
                if (maskStack != null && maskStack.Length > 0)
                {
                    foreach (ImageMask mask in maskStack)
                    {
                        mask.m_maxWorldHeight = m_maxWorldHeight;
                        mask.m_minWorldHeight = m_minWorldHeight;
                        mask.m_seaLevel = seaLevel;
                        mask.CheckHeightMaskMigration();
                    }
                    ImageMask.CheckMaskStackForInvalidTextureRules("Spawner", this.name + ", Spawn Rule: '" + m_settings.m_spawnerRules[i].m_name + "'", maskStack);
                }
            }
        }

        /// <summary>
        /// Store the last exported terrain size and heightmap resolution - used to determine if the user changed those on a world map spawner
        /// </summary>
        public void StoreWorldSize()
        {
            m_lastStampSpawnWorldSize = new Vector3(m_worldCreationSettings.m_xTiles * m_worldCreationSettings.m_tileSize, m_worldMapTerrain.terrainData.size.y, m_worldCreationSettings.m_zTiles * m_worldCreationSettings.m_tileSize);

        }

        //public void StoreHeightmapResolution()
        //{
        //    m_lastExportedHeightMapResolution = worldCreationSettings.m_gaiaDefaults.m_heightmapResolution;
        //}

        /// <summary>
        /// Returns true if the user changed the world size / heightmap resolution since the last world map export
        /// </summary>
        /// <returns></returns>
        public bool HasWorldSizeChanged()
        {
            return m_lastStampSpawnWorldSize != new Vector3(m_worldCreationSettings.m_xTiles * m_worldCreationSettings.m_tileSize, m_worldCreationSettings.m_tileHeight, m_worldCreationSettings.m_zTiles * m_worldCreationSettings.m_tileSize);
        }

        //public bool HasHeightmapResolutionChanged()
        //{
        //   return  m_lastExportedHeightMapResolution != worldCreationSettings.m_gaiaDefaults.m_heightmapResolution;
        //}


        /// <summary>
        /// This is executed only in the editor - using it to simulate co-routine execution and update execution
        /// </summary>
        void EditorUpdate()
        {
#if UNITY_EDITOR
            if (m_updateCoroutine == null)
            {
                StopEditorUpdates();
                if (SessionManager.m_session.m_operations.Exists(x => x.sessionPlaybackState == SessionPlaybackState.Queued))
                {
                    GaiaSessionManager.ContinueSessionPlayback();
                }
                else
                {
                    //No session to continue -> destroy the temporary tools, if any
                    GaiaSessionManager.AfterSessionPlaybackCleanup();
                }
                return;
            }
            else
            {
                if (EditorWindow.mouseOverWindow != null)
                {
                    m_updateTimeAllowed = 1 / 30f;
                }
                else
                {
                    m_updateTimeAllowed = 1 / 2f;
                }
                if (m_updateCoroutine2 != null)
                {
                    m_updateCoroutine2.MoveNext();
                }
                m_updateCoroutine.MoveNext();


            }
#endif
        }

        public void HighlightLoadingSettings()
        {
            m_highlightLoadingSettings = true;
            m_highlightLoadingSettingsStartedTimeStamp = GaiaUtils.GetUnixTimestamp();
        }

        /// <summary>
        /// Use this for initialization - this will kick the spawner off 
        /// </summary>
        void Start()
        {
            //Disable the colliders
            if (Application.isPlaying)
            {
                //Disable area bounds colliders
                Transform collTrans = this.transform.Find("Bounds_ColliderCache");
                if (collTrans != null)
                {
                    m_areaBoundsColliderCache = collTrans.gameObject;
                    m_areaBoundsColliderCache.SetActive(false);
                }

                if (!m_enableColliderCacheAtRuntime)
                {
                    collTrans = this.transform.Find("GameObject_ColliderCache");
                    if (collTrans != null)
                    {
                        m_goColliderCache = collTrans.gameObject;
                        m_goColliderCache.SetActive(false);
                    }
                }
            }

            if (m_mode == GaiaConstants.OperationMode.RuntimeInterval || m_mode == GaiaConstants.OperationMode.RuntimeTriggeredInterval)
            {
                //Initialise the spawner
                Initialise();

                //Start spawner checks in random period of time after game start, then every check interval
                InvokeRepeating("RunSpawnerIteration", 1f, m_spawnInterval);
            }
        }

        /// <summary>
        /// Build the spawner dictionary - allows for efficient updating of instances etc based on name
        /// </summary>
        public void Initialise()
        {
            if (m_showDebug)
            {
                Debug.Log("Initialising spawner");
            }

            //Set up layer for spawner collisions
            m_spawnColliderLayer = Gaia.TerrainHelper.GetActiveTerrainLayerAsInt();

            //Destroy any children
            List<Transform> transList = new List<Transform>();
            foreach (Transform child in transform)
            {
                transList.Add(child);
            }
            foreach (Transform child in transList)
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            //Set up the spawner type flags
            SetUpSpawnerTypeFlags();

            //Create the game object parent transform
            if (IsGameObjectSpawner())
            {
                m_goParentGameObject = new GameObject("Spawned_GameObjects");
                m_goParentGameObject.transform.parent = this.transform;
                m_areaBoundsColliderCache = new GameObject("Bounds_ColliderCache");
                m_areaBoundsColliderCache.transform.parent = this.transform;
                m_goColliderCache = new GameObject("GameObject_ColliderCache");
                m_goColliderCache.transform.parent = this.transform;
            }

            //Reset the random number generator
            ResetRandomGenertor();

            //Get terrain height - assume all terrains same height
            Terrain t = TerrainHelper.GetTerrain(transform.position);
            if (t != null)
            {
                m_terrainHeight = t.terrainData.size.y;
            }

            //Set the spawner bounds
            m_spawnerBounds = new Bounds(transform.position, new Vector3(m_settings.m_spawnRange * 2f, m_settings.m_spawnRange * 2f, m_settings.m_spawnRange * 2f));

            //Update the rule counters
            foreach (SpawnRule rule in m_settings.m_spawnerRules)
            {
                rule.m_currInstanceCnt = 0;
                rule.m_activeInstanceCnt = 0;
                rule.m_inactiveInstanceCnt = 0;
            }

            //Update the counters
            UpdateCounters();
        }

        /// <summary>
        /// Call this prior to doing a Spawn to do any setup required - particularly relevant for re-constituted spanwes
        /// </summary>
        private void PreSpawnInitialise()
        {
            //Update bounds
            m_spawnerBounds = new Bounds(transform.position, new Vector3(m_settings.m_spawnRange * 2f, m_settings.m_spawnRange * 2f, m_settings.m_spawnRange * 2f));

            //Make sure random number generator is online
            if (m_rndGenerator == null)
            {
                ResetRandomGenertor();
            }
            //Debug.Log(string.Format("RNG {0} Seed = {1} State A = {2} State B = {3}", gameObject.name, m_rndGenerator.m_seed, m_rndGenerator.m_stateA, m_rndGenerator.m_stateB));

            //Set up layer for spawner collisions
            m_spawnColliderLayer = Gaia.TerrainHelper.GetActiveTerrainLayerAsInt();

            //Set up the spawner type flags
            SetUpSpawnerTypeFlags();

            //Create the game object parent transform
            if (IsGameObjectSpawner())
            {
                if (transform.Find("Spawned_GameObjects") == null)
                {
                    m_goParentGameObject = new GameObject("Spawned_GameObjects");
                    m_goParentGameObject.transform.parent = this.transform;
                }
                if (transform.Find("Bounds_ColliderCache") == null)
                {
                    m_areaBoundsColliderCache = new GameObject("Bounds_ColliderCache");
                    m_areaBoundsColliderCache.transform.parent = this.transform;
                }
                if (transform.Find("GameObject_ColliderCache") == null)
                {
                    m_goColliderCache = new GameObject("GameObject_ColliderCache");
                    m_goColliderCache.transform.parent = this.transform;
                }
            }

            //Initialise spawner themselves
            foreach (SpawnRule rule in m_settings.m_spawnerRules)
            {
                rule.Initialise(this);
            }

            //Create and initialise the noise generator
            if (m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.PerlinNoise)
            {
                m_noiseGenerator = new FractalGenerator(m_noiseMaskFrequency, m_noiseMaskLacunarity, m_noiseMaskOctaves, m_noiseMaskPersistence, m_noiseMaskSeed, FractalGenerator.Fractals.Perlin);
            }
            else if (m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.BillowNoise)
            {
                m_noiseGenerator = new FractalGenerator(m_noiseMaskFrequency, m_noiseMaskLacunarity, m_noiseMaskOctaves, m_noiseMaskPersistence, m_noiseMaskSeed, FractalGenerator.Fractals.Billow);
            }
            else if (m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.RidgedNoise)
            {
                m_noiseGenerator = new FractalGenerator(m_noiseMaskFrequency, m_noiseMaskLacunarity, m_noiseMaskOctaves, m_noiseMaskPersistence, m_noiseMaskSeed, FractalGenerator.Fractals.RidgeMulti);
            }

            //Update the counters
            UpdateCounters();
        }

        /// <summary>
        /// Caching spawner type flags
        /// </summary>
        public void SetUpSpawnerTypeFlags()
        {
            m_isDetailSpawner = false;
            for (int ruleIdx = 0; ruleIdx < m_settings.m_spawnerRules.Count; ruleIdx++)
            {
                if (m_settings.m_spawnerRules[ruleIdx].m_resourceType == GaiaConstants.SpawnerResourceType.TerrainDetail)
                {
                    m_isDetailSpawner = true;
                    break;
                }
            }

            m_isTextureSpawner = false;
            for (int ruleIdx = 0; ruleIdx < m_settings.m_spawnerRules.Count; ruleIdx++)
            {
                if (m_settings.m_spawnerRules[ruleIdx].m_resourceType == GaiaConstants.SpawnerResourceType.TerrainTexture)
                {
                    m_isTextureSpawner = true;
                    break;
                }
            }

            for (int ruleIdx = 0; ruleIdx < m_settings.m_spawnerRules.Count; ruleIdx++)
            {
                if (m_settings.m_spawnerRules[ruleIdx].m_resourceType == GaiaConstants.SpawnerResourceType.TerrainTree)
                {
                    m_isTreeSpawnwer = true;
                    break;
                }
            }

            m_isGameObjectSpawner = false;
            for (int ruleIdx = 0; ruleIdx < m_settings.m_spawnerRules.Count; ruleIdx++)
            {
                if (m_settings.m_spawnerRules[ruleIdx].m_resourceType == GaiaConstants.SpawnerResourceType.GameObject)
                {
                    m_isGameObjectSpawner = true;
                    break;
                }
            }
        }


        /// <summary>
        /// Make sure the assets are set up properly in the resources file
        /// </summary>
        public void AssociateAssets()
        {
            if (m_settings.m_resources != null)
            {
                m_settings.m_resources.AssociateAssets();
            }
            else
            {
                Debug.LogWarning("Could not associated assets for " + name + " - resources file was missing");
            }
        }

        /// <summary>
        /// Get the index of any rules that are missing resources
        /// </summary>
        /// <returns>Array of the resources that are missing</returns>
        /// <param name="terrains">The terrains to be checked. If left null, the active terrain will be checked only.</param>
        public List<TerrainMissingSpawnRules> GetMissingResources(List<TerrainMissingSpawnRules> missingRes, Terrain[] terrains = null)
        {
            if (terrains == null)
            {
                terrains = new Terrain[1] { Terrain.activeTerrain };
            }

            if (missingRes == null)
            {
                missingRes = new List<TerrainMissingSpawnRules>();
            }

            //Loop over all terrains
            for (int terrainID = 0; terrainID < terrains.Length; terrainID++)
            {
                //skip if no terrain
                if (terrains[terrainID] == null)
                {
                    continue;
                }

                //Initialise spawner themselves
                for (int ruleIdx = 0; ruleIdx < m_settings.m_spawnerRules.Count; ruleIdx++)
                {
                    if (m_settings.m_spawnerRules[ruleIdx].m_isActive == true) //Only care about active resources
                    {
                        //check if there is actually a prototyp
                        if (m_settings.m_spawnerRules[ruleIdx].ResourceIsNull(m_settings))
                        {
                            Debug.Log("Spawn Rule " + m_settings.m_spawnerRules[ruleIdx].m_name + " is active but has missing resources (Texture, Tree Prefab, GameObject etc. are empty) maintained. This rule might not work properly in spawning.");
                        }
                        else if (!m_settings.m_spawnerRules[ruleIdx].ResourceIsLoadedInTerrain(this, terrains[terrainID]))
                        {
                            TerrainMissingSpawnRules terrainSpawnRulesId = missingRes.Find(x => x.terrain == terrains[terrainID]);

                            if (terrainSpawnRulesId != null)
                            {
                                if (!terrainSpawnRulesId.spawnRulesWithMissingResources.Contains(m_settings.m_spawnerRules[ruleIdx]))
                                {
                                    terrainSpawnRulesId.spawnRulesWithMissingResources.Add(m_settings.m_spawnerRules[ruleIdx]);
                                }
                            }
                            else
                            {
                                missingRes.Add(new TerrainMissingSpawnRules { terrain = terrains[terrainID], spawnRulesWithMissingResources = new List<SpawnRule>() { m_settings.m_spawnerRules[ruleIdx] } });
                            }
                        }
                    }
                }
            }

            return missingRes;
        }

        /// <summary>
        /// Add the resources related to the rules passed in into the terrain if they are not already there
        /// </summary>
        /// <param name="rules">Index of rules with resources that should be added to the terrain</param>
        public void AddResourcesToTerrain(int[] rules, Terrain[] terrains = null)
        {
            for (int terrainId = 0; terrainId < terrains.Length; terrainId++)
            {
                for (int ruleIdx = 0; ruleIdx < rules.GetLength(0); ruleIdx++)
                {
                    if (!m_settings.m_spawnerRules[rules[ruleIdx]].ResourceIsLoadedInTerrain(this, terrains[terrainId]))
                    {
                        m_settings.m_spawnerRules[rules[ruleIdx]].AddResourceToTerrain(this, new Terrain[1] { terrains[terrainId] });
                    }
                }
            }
        }

        /// <summary>
        /// Call this at the end of a spawn
        /// </summary>
        private void PostSpawn()
        {
            //Signal that everything has stopped
            m_spawnProgress = 0f;
            m_spawnComplete = true;
            m_updateCoroutine = null;

            //Update the counters
            UpdateCounters();
        }

        /// <summary>
        /// Return true if this spawner spawns textures
        /// </summary>
        /// <returns>True if we spawn textures</returns>
        public bool IsTextureSpawner()
        {
            return m_isTextureSpawner;
        }

        /// <summary>
        /// Return true if this spawner spawns details
        /// </summary>
        /// <returns>True if we spawn details</returns>
        public bool IsDetailSpawner()
        {
            return m_isDetailSpawner;
        }

        /// <summary>
        /// Return true if this spawner spawns trees
        /// </summary>
        /// <returns>True if we spawn trees</returns>
        public bool IsTreeSpawner()
        {
            return m_isTreeSpawnwer;
        }

        /// <summary>
        /// Return true if this spawner spawns game objects
        /// </summary>
        /// <returns>True if we spawn game objects</returns>
        public bool IsGameObjectSpawner()
        {
            return m_isGameObjectSpawner;
        }

        /// <summary>
        /// Reste the spawner and delete everything it points to
        /// </summary>
        public void ResetSpawner()
        {
            Initialise();
        }

        public void UpdateAutoLoadRange()
        {
            //world map spawner should not load terrains
            if (m_settings.m_isWorldmapSpawner)
            {
                return;
            }

#if GAIA_PRO_PRESENT
            if (m_loadTerrainMode != LoadMode.Disabled)
            {
                float width = m_settings.m_spawnRange * 2f;
                //reduce the loading width a bit => this is to prevent loading in terrains when the spawner bounds end exactly at the border of
                //surrounding terrains, this loads in a lot of extra terrains which are not required for the spawn 
                width -= 0.5f;
                Vector3 center = transform.position;
                TerrainLoader.m_loadingBoundsRegular.center = center;
                TerrainLoader.m_loadingBoundsRegular.size = new Vector3(width, width, width);
                TerrainLoader.m_loadingBoundsImpostor.center = center;
                if (m_impostorLoadingRange > 0)
                {
                    TerrainLoader.m_loadingBoundsImpostor.size = new Vector3(width + m_impostorLoadingRange, width + m_impostorLoadingRange, width + m_impostorLoadingRange);
                }
                else
                {
                    TerrainLoader.m_loadingBoundsImpostor.size = Vector3.zero;
                }
            }
            TerrainLoader.LoadMode = m_loadTerrainMode;
#endif
        }

        /// <summary>
        /// Cause any active spawn to cancel itself
        /// </summary>
        public void CancelSpawn()
        {
            m_cancelSpawn = true;
            m_spawnComplete = true;
            m_spawnProgress = 0f;
            ProgressBar.Clear(ProgressBarPriority.Spawning);
        }

        /// <summary>
        /// Returns true if we are currently in process of spawning
        /// </summary>
        /// <returns>True if spawning, false otherwise</returns>
        public bool IsSpawning()
        {
            return (m_spawnComplete != true);
        }

        /// <summary>
        /// Check to see if this spawner can spawn instances
        /// </summary>
        /// <returns>True if it can spawn instances, false otherwise</returns>
        private bool CanSpawnInstances()
        {
            SpawnRule rule;
            bool canSpawnInstances = false;
            for (int ruleIdx = 0; ruleIdx < m_settings.m_spawnerRules.Count; ruleIdx++)
            {
                rule = m_settings.m_spawnerRules[ruleIdx];
                if (rule.m_isActive)
                {
                    if (rule.m_ignoreMaxInstances)
                    {
                        return true;
                    }

                    if (rule.m_activeInstanceCnt < rule.m_maxInstances)
                    {
                        return true;
                    }
                }
            }
            return canSpawnInstances;
        }


        public void DrawSpawnerPreview()
        {
            if (m_drawPreview)
            {
                GaiaStopwatch.StartEvent("Drawing Spawner Preview");
                //early out if no preview rule is active 
                bool foundActive = false;
                for (int i = 0; i < m_previewRuleIds.Count; i++)
                {
                    if (m_previewRuleIds[i] < m_settings.m_spawnerRules.Count)
                    {
                        if (m_settings.m_spawnerRules[m_previewRuleIds[i]].m_isActive)
                        {
                            foundActive = true;
                        }
                    }
                }
                if (!foundActive)
                {
                    return;
                }
                //Set up a multi-terrain operation once, all rules can then draw from the data collected here
                Terrain currentTerrain = GetCurrentTerrain();
                if (currentTerrain == null)
                {
                    return;
                }

                GaiaMultiTerrainOperation operation = new GaiaMultiTerrainOperation(currentTerrain, transform, m_settings.m_spawnRange * 2f);
                operation.m_isWorldMapOperation = m_settings.m_isWorldmapSpawner;
                operation.GetHeightmap();

                //only re-generate all textures etc. if settings have changed and the preview is dirty, otherwise we can just use the cached textures
                if (m_spawnPreviewDirty == true)
                {
                    //To get a combined preview of different textures on a single mesh we need one color texture each per previewed 
                    // rule to determine the color areas on the heightmap mesh
                    // We need to iterate over the rules that are previewed, and build those color textures in this process

                    //Get additional op data (required for certain image masks)
                    operation.GetNormalmap();
                    operation.CollectTerrainBakedMasks();
                    //Preparing a simple add operation on the image mask shader for the combined heightmap texture
                    //Material filterMat = new Material(Shader.Find("Hidden/Gaia/FilterImageMask"));
                    //filterMat.SetFloat("_Strength", 1f);
                    //filterMat.SetInt("_Invert", 0);
                    //Store the currently active render texture here before we start manipulating
                    RenderTexture currentRT = RenderTexture.active;

                    //Clear texture cache first
                    ClearColorTextureCache();

                    //bool firstActiveRule = true;

                    for (int i = 0; i < m_previewRuleIds.Count; i++)
                    {

                        if (m_settings.m_spawnerRules[m_previewRuleIds[i]].m_isActive)
                        {
                            //Initialise our color texture cache at this index with this context
                            InitialiseColorTextureCache(i, operation.RTheightmap);
                            //Store result for this rule in our cache render texture array
                            Graphics.Blit(ApplyBrush(operation, MultiTerrainOperationType.Heightmap, m_previewRuleIds[i]), m_cachedPreviewColorRenderTextures[i]);
                            RenderTexture.active = currentRT;
                        }
                    }

                    //Everything processed, check if the preview is not dirty anymore
                    m_spawnPreviewDirty = false;
                }
                //Now draw the preview according to the cached textures
                Material material = GaiaMultiTerrainOperation.GetDefaultGaiaSpawnerPreviewMaterial();
                material.SetInt("_zTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);

                //assign all color textures in the material
                for (int i = 0; i < m_cachedPreviewColorRenderTextures.Length; i++)
                {
                    material.SetTexture("_colorTexture" + i, m_cachedPreviewColorRenderTextures[i]);
                }

                //iterate through spawn rules, and if it is a previewed texture set its color accordingly in the color slot
                int colorIndex = 0;
                for (int i = 0; i < m_settings.m_spawnerRules.Count; i++)
                {
                    if (m_previewRuleIds.Contains(i))
                    {
                        material.SetColor("_previewColor" + colorIndex.ToString(), m_settings.m_spawnerRules[m_previewRuleIds[colorIndex]].m_visualisationColor);
                        colorIndex++;
                    }
                }

                for (; colorIndex < GaiaConstants.maxPreviewedTextures; colorIndex++)
                {
                    Color transparentColor = Color.white;
                    transparentColor.a = 0f;
                    material.SetColor("_previewColor" + colorIndex.ToString(), transparentColor);
                }


                Color seaLevelColor = GaiaSettings.m_stamperSeaLevelTintColor;
                if (!m_showSeaLevelinStampPreview)
                {
                    seaLevelColor.a = 0f;
                }
                material.SetColor("_seaLevelTintColor", seaLevelColor);
                material.SetFloat("_seaLevel", SessionManager.GetSeaLevel(m_settings.m_isWorldmapSpawner));
                operation.Visualize(MultiTerrainOperationType.Heightmap, operation.RTheightmap, material, 1);

                //Clean up
                operation.CloseOperation();
                //Clean up temp textures
                GaiaUtils.ReleaseAllTempRenderTextures();
                GaiaStopwatch.EndEvent("Drawing Spawner Preview");
                GaiaStopwatch.Stop();
            }
        }

        private void ClearCachedTexture(RenderTexture cachedRT)
        {
            if (cachedRT != null)
            {
                cachedRT.Release();
                DestroyImmediate(cachedRT);
            }

            cachedRT = new RenderTexture(1, 1, 1);
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = cachedRT;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = currentRT;

        }

        private void ClearColorTextureCache()
        {
            for (int i = 0; i < m_cachedPreviewColorRenderTextures.Length; i++)
            {
                ClearCachedTexture(m_cachedPreviewColorRenderTextures[i]);
            }
        }

        /// <summary>
        /// Inizialises or "resets" a color texture in the cache
        /// </summary>
        /// <param name="index">The index at which to initialise.</param>
        /// <param name="rtToInitialiseFrom">A sample render texture with the correct resolution & format settings etc. to initialise from</param>
        private void InitialiseColorTextureCache(int index, RenderTexture rtToInitialiseFrom)
        {
            ClearCachedTexture(m_cachedPreviewColorRenderTextures[index]);
            m_cachedPreviewColorRenderTextures[index] = new RenderTexture(rtToInitialiseFrom);
        }

        private RenderTexture ApplyBrush(GaiaMultiTerrainOperation operation, MultiTerrainOperationType opType, int spawnRuleID = 0)
        {
            Terrain currentTerrain = GetCurrentTerrain();

            RenderTextureDescriptor rtDescriptor;

            switch (opType)
            {
                case MultiTerrainOperationType.Heightmap:
                    rtDescriptor = operation.RTheightmap.descriptor;
                    break;
                case MultiTerrainOperationType.Texture:
                    rtDescriptor = operation.RTtextureSplatmap.descriptor;
                    break;
                case MultiTerrainOperationType.TerrainDetail:
                    rtDescriptor = operation.RTdetailmap.descriptor;
                    break;
                case MultiTerrainOperationType.Tree:
                    rtDescriptor = operation.RTterrainTree.descriptor;
                    break;
                case MultiTerrainOperationType.GameObject:
                    rtDescriptor = operation.RTgameObject.descriptor;
                    break;
                default:
                    rtDescriptor = operation.RTheightmap.descriptor;
                    break;
            }
            //Random write needs to be enabled for certain mask types to function!
            rtDescriptor.enableRandomWrite = true;
            RenderTexture inputTexture1 = RenderTexture.GetTemporary(rtDescriptor);
            RenderTexture inputTexture2 = RenderTexture.GetTemporary(rtDescriptor);
            RenderTexture inputTexture3 = RenderTexture.GetTemporary(rtDescriptor);

            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = inputTexture1;
            GL.Clear(true, true, Color.white);
            RenderTexture.active = inputTexture2;
            GL.Clear(true, true, Color.white);
            RenderTexture.active = inputTexture3;
            GL.Clear(true, true, Color.white);
            RenderTexture.active = currentRT;

            //fetch the biome mask stack (if any)
            BiomeController biomeController = Resources.FindObjectsOfTypeAll<BiomeController>().FirstOrDefault(x => x.m_autoSpawners.Find(y => y.spawner == this) != null);
            ImageMask[] biomeControllerStack = new ImageMask[0];
            if (biomeController != null && biomeController.m_settings.m_imageMasks.Length > 0)
            {
                biomeControllerStack = biomeController.m_settings.m_imageMasks;
                biomeControllerStack[0].m_blendMode = ImageMaskBlendMode.Multiply;
                //Iterate through all image masks and set up the current paint context in case the shader uses heightmap data
                foreach (ImageMask mask in biomeControllerStack)
                {
                    mask.m_multiTerrainOperation = operation;
                    mask.m_seaLevel = SessionManager.GetSeaLevel(m_settings.m_isWorldmapSpawner);
                    mask.m_maxWorldHeight = m_maxWorldHeight;
                    mask.m_minWorldHeight = m_minWorldHeight;
                }
                ImageMask.CheckMaskStackForInvalidTextureRules("Biome Controller", biomeController.name, biomeControllerStack);

            }

            ImageMask[] spawnerStack = new ImageMask[0];
            //set up the spawner mask stack, only if it has masks or a biome controller exists with masks
            if (m_settings.m_imageMasks.Length > 0)
            {

                spawnerStack = m_settings.m_imageMasks;
                //We start from a white texture, so we need the first mask action in the stack to always be "Multiply", otherwise there will be no result.
                spawnerStack[0].m_blendMode = ImageMaskBlendMode.Multiply;

                //Iterate through all image masks and set up the current paint context in case the shader uses heightmap data
                foreach (ImageMask mask in spawnerStack)
                {
                    mask.m_multiTerrainOperation = operation;
                    mask.m_seaLevel = SessionManager.GetSeaLevel(m_settings.m_isWorldmapSpawner);
                    mask.m_maxWorldHeight = m_maxWorldHeight;
                    mask.m_minWorldHeight = m_minWorldHeight;
                }

                ImageMask.CheckMaskStackForInvalidTextureRules("Spawner", this.name, spawnerStack);

            }

            //set up the resource mask stack
            ImageMask[] maskStack = m_settings.m_spawnerRules[spawnRuleID].m_imageMasks;
            if (maskStack.Length > 0)
            {
                //We start from a white texture, so we need the first mask action in the stack to always be "Multiply", otherwise there will be no result.
                maskStack[0].m_blendMode = ImageMaskBlendMode.Multiply;

                //Iterate through all image masks and set up the current paint context in case the shader uses heightmap data
                foreach (ImageMask mask in maskStack)
                {
                    mask.m_multiTerrainOperation = operation;
                    mask.m_seaLevel = SessionManager.GetSeaLevel(m_settings.m_isWorldmapSpawner);
                    mask.m_maxWorldHeight = m_maxWorldHeight;
                    mask.m_minWorldHeight = m_minWorldHeight;
                }
                ImageMask.CheckMaskStackForInvalidTextureRules("Spawner", this.name + ", Spawn Rule: '" + m_settings.m_spawnerRules[spawnRuleID].m_name + "'", maskStack);
            }


            //Get the combined masks from the biomeController
            RenderTexture biomeOutputTexture = RenderTexture.GetTemporary(rtDescriptor);
            Graphics.Blit(ImageProcessing.ApplyMaskStack(inputTexture1, biomeOutputTexture, biomeControllerStack, ImageMaskInfluence.Local), biomeOutputTexture);

            //Get the combined masks from the spawner
            RenderTexture spawnerOutputTexture = RenderTexture.GetTemporary(rtDescriptor);
            Graphics.Blit(ImageProcessing.ApplyMaskStack(inputTexture2, spawnerOutputTexture, spawnerStack, ImageMaskInfluence.Local), spawnerOutputTexture);

            //Check if we have the special global output mask in the spawn rule - this mask routes the output from the spawner mask stack directly into the rule stack 
            //(instead of utilizing the multiply below)
            bool globalOutputMaskFound = false;
            foreach (ImageMask mask in maskStack)
            {
                if (mask.m_operation == ImageMaskOperation.GlobalSpawnerMaskStack && mask.m_active)
                {
                    globalOutputMaskFound = true;
                    mask.m_globalSpawnerMaskStackRT = spawnerOutputTexture;
                }
            }

            //Get the combined masks from the rule
            RenderTexture ruleOutputTexture = RenderTexture.GetTemporary(rtDescriptor);
            Graphics.Blit(ImageProcessing.ApplyMaskStack(inputTexture3, ruleOutputTexture, maskStack, ImageMaskInfluence.Local), ruleOutputTexture);

            //Run them through the image mask shader for a simple multiply
            Material filterMat = new Material(Shader.Find("Hidden/Gaia/FilterImageMask"));
            ImageProcessing.BakeCurveTexture(m_strengthTransformCurve, StrengthTransformCurveTexture);
            filterMat.SetTexture("_HeightTransformTex", StrengthTransformCurveTexture);


            RenderTexture combinedOutputTexture1 = RenderTexture.GetTemporary(rtDescriptor);

            //Only process the spawner stack if we are not useing the special global output mask
            if (!globalOutputMaskFound)
            {
                filterMat.SetTexture("_InputTex", biomeOutputTexture);
                filterMat.SetTexture("_ImageMaskTex", spawnerOutputTexture);
                Graphics.Blit(inputTexture1, combinedOutputTexture1, filterMat, 0);
            }
            else
            {
                //here we only blit the biome mask directly into the combined texture,
                //ignoring the spawner stack
                Graphics.Blit(biomeOutputTexture, combinedOutputTexture1);
            }
            filterMat.SetTexture("_InputTex", combinedOutputTexture1);
            filterMat.SetTexture("_ImageMaskTex", ruleOutputTexture);

            RenderTexture finalOutputTexture = RenderTexture.GetTemporary(rtDescriptor);
            Graphics.Blit(inputTexture1, finalOutputTexture, filterMat, 0);

            //clean up temporary textures
            ReleaseRenderTexture(inputTexture1);
            inputTexture1 = null;
            ReleaseRenderTexture(inputTexture2);
            inputTexture2 = null;
            ReleaseRenderTexture(inputTexture3);
            inputTexture3 = null;
            ReleaseRenderTexture(biomeOutputTexture);
            biomeOutputTexture = null;
            ReleaseRenderTexture(spawnerOutputTexture);
            spawnerOutputTexture = null;
            ReleaseRenderTexture(ruleOutputTexture);
            ruleOutputTexture = null;
            ReleaseRenderTexture(combinedOutputTexture1);
            combinedOutputTexture1 = null;

            //Release the texture references from the biome controller, if any
            if (biomeController != null)
            {
                biomeController.m_settings.ClearImageMaskTextures();
            }

            return finalOutputTexture;
        }



        public Terrain GetCurrentTerrain()
        {
            Terrain currentTerrain = Gaia.TerrainHelper.GetTerrain(transform.position, m_settings.m_isWorldmapSpawner);
            //Check if the stamper is over a terrain currently

            //if not, we check if there is any terrain within the bounds of the spawner
            if (currentTerrain == null)
            {
                float width = m_settings.m_spawnRange * 2f;
                Bounds spawnerBounds = new Bounds(transform.position, new Vector3(width, width, width));

                foreach (Terrain t in Terrain.activeTerrains)
                {
                    //only look at this terrain if it matches the selected world map mode
                    if (m_settings.m_isWorldmapSpawner == TerrainHelper.IsWorldMapTerrain(t))
                    {
                        Bounds worldSpaceBounds = t.terrainData.bounds;
                        worldSpaceBounds.center = new Vector3(worldSpaceBounds.center.x + t.transform.position.x, worldSpaceBounds.center.y + t.transform.position.y, worldSpaceBounds.center.z + t.transform.position.z);

                        if (worldSpaceBounds.Intersects(spawnerBounds))
                        {
                            currentTerrain = t;
                            break;
                        }
                    }
                }
            }

            //if we still not have any terrain, we will draw a preview based on the last active terrain
            //if that is null either we can't draw a stamp preview
            if (currentTerrain)
            {
                m_lastActiveTerrainSize = currentTerrain.terrainData.size.x;
                //Update last active terrain with current
            }

            return currentTerrain;
        }

        private void ReleaseRenderTexture(RenderTexture texture)
        {
            if (texture != null)
            {
                RenderTexture.ReleaseTemporary(texture);
                texture = null;
            }
        }


        //public ImageMask[] GetSpawnRuleImageMasksByIndex(int spawnRuleIndex)
        //{
        //    //Get the right mask list from the resources according to the resource type that is used
        //    switch (m_spawnerRules[spawnRuleIndex].m_resourceType)
        //    {
        //        case GaiaConstants.SpawnerResourceType.TerrainTexture:
        //            return m_resources.m_texturePrototypes[m_spawnerRules[spawnRuleIndex].m_resourceIdx].m_imageMasks;
        //        case GaiaConstants.SpawnerResourceType.TerrainDetail:
        //            return m_resources.m_detailPrototypes[m_spawnerRules[spawnRuleIndex].m_resourceIdx].m_imageMasks;
        //        case GaiaConstants.SpawnerResourceType.TerrainTree:
        //            return m_resources.m_treePrototypes[m_spawnerRules[spawnRuleIndex].m_resourceIdx].m_imageMasks;
        //        case GaiaConstants.SpawnerResourceType.GameObject:
        //            return m_resources.m_gameObjectPrototypes[m_spawnerRules[spawnRuleIndex].m_resourceIdx].m_imageMasks;
        //    }
        //    return null;
        //}


        //public CollisionMask[] GetSpawnRuleCollisionMasksByIndices(int spawnRuleIndex, int maskIndex)
        //{
        //    //Get the right collision mask list from the resources according to the resource type that is used
        //    return GetSpawnRuleImageMasksByIndex(spawnRuleIndex)[maskIndex].m_collisionMasks;
        //}

        //public void SetSpawnRuleImageMasksByIndex(int spawnRuleIndex, ImageMask[] imageMasks)
        //{
        //    //Get the right mask list from the resources according to the resource type that is used
        //    switch (m_spawnerRules[spawnRuleIndex].m_resourceType)
        //    {
        //        case GaiaConstants.SpawnerResourceType.TerrainTexture:
        //            m_resources.m_texturePrototypes[m_spawnerRules[spawnRuleIndex].m_resourceIdx].m_imageMasks = imageMasks;
        //            break;
        //        case GaiaConstants.SpawnerResourceType.TerrainDetail:
        //            m_resources.m_detailPrototypes[m_spawnerRules[spawnRuleIndex].m_resourceIdx].m_imageMasks = imageMasks;
        //            break;
        //        case GaiaConstants.SpawnerResourceType.TerrainTree:
        //            m_resources.m_treePrototypes[m_spawnerRules[spawnRuleIndex].m_resourceIdx].m_imageMasks = imageMasks;
        //            break;
        //        case GaiaConstants.SpawnerResourceType.GameObject:
        //            m_resources.m_gameObjectPrototypes[m_spawnerRules[spawnRuleIndex].m_resourceIdx].m_imageMasks = imageMasks;
        //            break;
        //    }
        //}

        //public void SetSpawnRuleCollisionMasksByIndices(int spawnRuleIndex, int maskIndex, CollisionMask[] collisionMasks)
        //{
        //    m_spawnerRules[spawnRuleIndex].m_imageMasks[maskIndex].m_collisionMasks = collisionMasks;
        //}

        //public Color GetVisualisationColorBySpawnRuleIndex(int spawnRuleIndex)
        //{
        //    switch (m_settings.m_spawnerRules[spawnRuleIndex].m_resourceType)
        //    {
        //        case GaiaConstants.SpawnerResourceType.TerrainTexture:
        //            ResourceProtoTexture protoTexture = (ResourceProtoTexture)GetResourceProtoBySpawnRuleIndex(spawnRuleIndex);
        //            if (protoTexture != null)
        //                return protoTexture.m_visualisationColor;
        //            else
        //                return Color.red;
        //        case GaiaConstants.SpawnerResourceType.TerrainTree:
        //            ResourceProtoTree protoTree = (ResourceProtoTree)GetResourceProtoBySpawnRuleIndex(spawnRuleIndex);
        //            if (protoTree != null)
        //                return protoTree.m_visualisationColor;
        //            else
        //                return Color.red;
        //        case GaiaConstants.SpawnerResourceType.TerrainDetail:
        //            ResourceProtoDetail protoDetail = (ResourceProtoDetail)GetResourceProtoBySpawnRuleIndex(spawnRuleIndex);
        //            if (protoDetail != null)
        //                return protoDetail.m_visualisationColor;
        //            else
        //                return Color.red;
        //        case GaiaConstants.SpawnerResourceType.GameObject:
        //            ResourceProtoGameObject protoGameObject = (ResourceProtoGameObject)GetResourceProtoBySpawnRuleIndex(spawnRuleIndex);
        //            if (protoGameObject != null)
        //                return protoGameObject.m_visualisationColor;
        //            else
        //                return Color.red;
        //    }
        //    return Color.red;
        //}

        //public void SetVisualisationColorBySpawnRuleIndex(Color color, int spawnRuleIndex)
        //{
        //    switch (m_settings.m_spawnerRules[spawnRuleIndex].m_resourceType)
        //    {
        //        case GaiaConstants.SpawnerResourceType.TerrainTexture:
        //            ResourceProtoTexture protoTexture = (ResourceProtoTexture)GetResourceProtoBySpawnRuleIndex(spawnRuleIndex);
        //            if (protoTexture != null)
        //            {
        //                protoTexture.m_visualisationColor = color;
        //            }
        //            break;
        //        case GaiaConstants.SpawnerResourceType.TerrainTree:
        //            ResourceProtoTree protoTree = (ResourceProtoTree)GetResourceProtoBySpawnRuleIndex(spawnRuleIndex);
        //            if (protoTree != null)
        //            {
        //                protoTree.m_visualisationColor = color;
        //            }
        //            break;
        //        case GaiaConstants.SpawnerResourceType.TerrainDetail:
        //            ResourceProtoDetail protoDetail = (ResourceProtoDetail)GetResourceProtoBySpawnRuleIndex(spawnRuleIndex);
        //            if (protoDetail != null)
        //            {
        //                protoDetail.m_visualisationColor = color;
        //            }
        //            break;
        //        case GaiaConstants.SpawnerResourceType.GameObject:
        //            ResourceProtoGameObject protoGameObject = (ResourceProtoGameObject)GetResourceProtoBySpawnRuleIndex(spawnRuleIndex);
        //            if (protoGameObject != null)
        //            {
        //                protoGameObject.m_visualisationColor = color;
        //            }
        //            break;
        //    }

        //}

        public object GetResourceProtoBySpawnRuleIndex(int spawnRuleIndex)
        {
            switch (m_settings.m_spawnerRules[spawnRuleIndex].m_resourceType)
            {
                case GaiaConstants.SpawnerResourceType.TerrainTexture:
                    if (m_settings.m_spawnerRules[spawnRuleIndex].m_resourceIdx < m_settings.m_resources.m_texturePrototypes.Length)
                        return m_settings.m_resources.m_texturePrototypes[m_settings.m_spawnerRules[spawnRuleIndex].m_resourceIdx];
                    else
                        return null;
                case GaiaConstants.SpawnerResourceType.TerrainTree:
                    if (m_settings.m_spawnerRules[spawnRuleIndex].m_resourceIdx < m_settings.m_resources.m_treePrototypes.Length)
                        return m_settings.m_resources.m_treePrototypes[m_settings.m_spawnerRules[spawnRuleIndex].m_resourceIdx];
                    else
                        return null;
                case GaiaConstants.SpawnerResourceType.TerrainDetail:
                    if (m_settings.m_spawnerRules[spawnRuleIndex].m_resourceIdx < m_settings.m_resources.m_detailPrototypes.Length)
                        return m_settings.m_resources.m_detailPrototypes[m_settings.m_spawnerRules[spawnRuleIndex].m_resourceIdx];
                    else
                        return null;
                case GaiaConstants.SpawnerResourceType.GameObject:
                    if (m_settings.m_spawnerRules[spawnRuleIndex].m_resourceIdx < m_settings.m_resources.m_gameObjectPrototypes.Length)
                        return m_settings.m_resources.m_gameObjectPrototypes[m_settings.m_spawnerRules[spawnRuleIndex].m_resourceIdx];
                    else
                        return null;
            }
            return null;
        }

        /// <summary>
        /// Toggle the preview mesh on and off
        /// </summary>
        public void TogglePreview()
        {
            m_drawPreview = !m_drawPreview;
            DrawSpawnerPreview();
        }


        /// <summary>
        /// Run a spawner iteration - called by timed invoke or manually
        /// </summary>
        //        public void RunSpawnerIteration()
        //        {
        //            //Reset status
        //            m_cancelSpawn = false;
        //            m_spawnComplete = false;

        //            //Perform a spawner iteration preinitialisation
        //            PreSpawnInitialise();

        //			//Check that there are rules that can be applied
        //			if (m_activeRuleCnt <= 0)
        //			{
        //				if (m_showDebug)
        //				{
        //					Debug.Log(string.Format("{0}: There are no active spawn rules. Can't spawn without rules.", gameObject.name));
        //				}
        //                m_spawnComplete = true;
        //				return;
        //			}

        //			//Check that we can actually add new instances
        //			if (!CanSpawnInstances())
        //			{
        //				if (m_showDebug)
        //				{
        //					Debug.Log(string.Format("{0}: Can't spawn or activate new instance - max instance count reached.", gameObject.name));
        //				}
        //                m_spawnComplete = true;
        //				return;
        //			}

        //            //Call out any issues with terrain height
        //            Terrain t = TerrainHelper.GetTerrain(transform.position);
        //            if (t != null)
        //            {
        //                m_terrainHeight = t.terrainData.size.y;
        //                if (m_resources != null && m_resources.m_terrainHeight != m_terrainHeight)
        //                {
        //                    Debug.LogWarning(string.Format("There is a mismatch between your resources Terrain Height {0} and your actual Terrain Height {1}. Your Spawn may not work as intended!", m_resources.m_terrainHeight, m_terrainHeight));
        //                }
        //            }

        //            //Look for any tagged objects that are acting as triggers and check if they were in range
        //            if (m_mode == GaiaConstants.OperationMode.RuntimeTriggeredInterval)
        //            {
        //                m_checkDistance = m_triggerRange + 1f;
        //                List<GameObject> triggerObjects = new List<GameObject>();
        //                string[] tags = new string[0];
        //                if (!string.IsNullOrEmpty(m_triggerTags))
        //                {
        //                    tags = m_triggerTags.Split(',');
        //                }
        //                else
        //                {
        //                    Debug.LogError("You have not supplied a trigger tag. Spawner will not spawn!");
        //                }
        //                int idx = 0;
        //                if (m_triggerTags.Length > 0 &&  tags.Length > 0)
        //                {
        //                    //Grab the tagged objects
        //                    for (idx = 0; idx < tags.Length; idx++)
        //                    {
        //                        triggerObjects.AddRange(GameObject.FindGameObjectsWithTag(tags[idx]));
        //                    }

        //                    //Now look for anything in range
        //                    for (idx = 0; idx < triggerObjects.Count; idx++)
        //                    {
        //                        m_checkDistance = Vector3.Distance(transform.position, triggerObjects[idx].transform.position);
        //                        if (m_checkDistance <= m_triggerRange)
        //                        {
        //                            break;
        //                        }
        //                    }

        //                    //And if its wasnt found then drop out
        //                    if (m_checkDistance > m_triggerRange)
        //                    {
        //                        if (m_showDebug)
        //                        {
        //                            Debug.Log(string.Format("{0}: No triggers were close enough", gameObject.name));
        //                        }
        //                        m_spawnComplete = true;
        //                        return; //Nothing to do - trigger is too far away
        //                    }
        //                }
        //                else
        //                {
        //                    //Nothing to see, drop out
        //                    if (m_showDebug)
        //                    {
        //                        Debug.Log(string.Format("{0}: No triggers found", gameObject.name));
        //                    }
        //                    m_spawnComplete = true;
        //                    return;
        //                }
        //            }

        //            //Update the session - but only of we are not playing
        //            if (!Application.isPlaying)
        //            {
        //                AddToSession(GaiaOperation.OperationType.Spawn, "Spawning " + transform.name);
        //            }

        //            //Run the spawner based on the location selection method chosen
        //            if (m_spawnLocationAlgorithm == GaiaConstants.SpawnerLocation.RandomLocation || m_spawnLocationAlgorithm == GaiaConstants.SpawnerLocation.RandomLocationClustered)
        //            {
        //                #if UNITY_EDITOR
        //                    if (!Application.isPlaying)
        //                    {
        //                        m_updateCoroutine = RunRandomSpawnerIteration();
        //                        StartEditorUpdates();
        //                    }
        //                    else
        //                    {
        //                        StartCoroutine(RunRandomSpawnerIteration());
        //                    }
        //#else
        //                    StartCoroutine(RunRandomSpawnerIteration(terrainID));
        //#endif
        //            }
        //            else
        //            {
        //                #if UNITY_EDITOR
        //                    if (!Application.isPlaying)
        //                    {
        //                        m_updateCoroutine = RunAreaSpawnerIteration();
        //                        StartEditorUpdates();
        //                    }
        //                    else
        //                    {
        //                        StartCoroutine(RunAreaSpawnerIteration());
        //                    }
        //#else
        //                    StartCoroutine(RunAreaSpawnerIteration(terrainID));
        //#endif
        //            }
        //        }

        public float GetMaxSpawnerRange()
        {
            Terrain currentTerrain = null;
            if (m_settings.m_isWorldmapSpawner)
            {
                currentTerrain = m_worldMapTerrain;
            }
            else
            {
                currentTerrain = GetCurrentTerrain();
            }
            if (currentTerrain != null)
            {
                return Mathf.Round((float)8192 / (float)currentTerrain.terrainData.heightmapResolution * currentTerrain.terrainData.size.x / 2f);
            }
            else
            {
                return 1000;
            }
        }



        /// <summary>
        /// Executes a List of spawners across an area in world space. The spawners are executed in steps defined by the world spawn range in the gaia settings.
        /// </summary>
        /// <param name="spawners">List of spawners to spawn across the area</param>
        /// <param name="area">The area in world space to spawn across</param>
        /// <param name="validTerrainNames">The terrain names that are valid to spawn on. If null, all terrains within operation range are assumed valid.</param>
        public IEnumerator AreaSpawn(List<Spawner> spawners, BoundsDouble area, List<string> validTerrainNames = null)
        {
            GaiaStopwatch.StartEvent("Area Spawn");
            m_cancelSpawn = false;
            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
            ClearPrototypeLists();
            ImageMask.RefreshSpawnRuleGUIDs();

            //remember the original world origin and loading range
            double originalLoadingRange = TerrainLoaderManager.Instance.GetLoadingRange();
            double originalLoadingRangeImpostor = TerrainLoaderManager.Instance.GetImpostorLoadingRange();
            CenterSceneViewLoadingOn originalCenter = TerrainLoaderManager.Instance.CenterSceneViewLoadingOn;
            Vector3Double originalOrigin = TerrainLoaderManager.Instance.GetOrigin();

            Vector3 startPosition;
            float spawnRange;

            //Track if we spawn GameObject -> If yes, the scenes affected must be dirtied for saving
            bool spawnedGameObjects = false;

            //Does the area exceed the range of the world spawn range? if yes,
            //this means we need to spawn in multiple locations. Otherwise a single location spawn
            //should do the trick.
            //World map spawns are always local across the entire world map - since the world map is just a single terrain this works fine
            //even if the world map spans 100s of km
            float locationIncrement = gaiaSettings.m_spawnerWorldSpawnRange * 2;
            if ((area.size.x > gaiaSettings.m_spawnerWorldSpawnRange * 2f || area.size.z > gaiaSettings.m_spawnerWorldSpawnRange * 2f) && !m_settings.m_isWorldmapSpawner)
            {
                //Multiple locations required
                spawnRange = (float)Mathd.Min(gaiaSettings.m_spawnerWorldSpawnRange, Mathd.Min(area.extents.x, area.extents.z));
                startPosition = new Vector3Double(area.min.x + spawnRange, 0, area.min.z + spawnRange);
            }
            else
            {
                //Single location spawn
                spawnRange = (float)Mathd.Max(area.extents.x, area.extents.z);
                startPosition = area.center;
                //Override the location increment to be so large we only go through the terrain iteration loops once.
                locationIncrement = float.MaxValue;
            }
            //Calculate the maximum amount of rules that need to be spawned in all iterations across the area for the progress bar
            int activeSpawnRules = 0;

            //Get all active rules
            foreach (Spawner spawner in spawners.Where(x => x != null && x.isActiveAndEnabled))
            {
                //Clear all prototype lists while we are at it
                spawner.ClearPrototypeLists();

                foreach (SpawnRule rule in spawner.settings.m_spawnerRules.Where(x => x.m_resourceType != SpawnerResourceType.WorldBiomeMask))
                {
                    if (rule.m_isActive)
                    {
                        activeSpawnRules++;
                    }
                    if (rule.m_resourceType == SpawnerResourceType.GameObject || rule.m_resourceType == SpawnerResourceType.SpawnExtension)
                    {
                        spawnedGameObjects = true;
                    }
                    if (spawner.m_settings.m_spawnMode == SpawnMode.Replace)
                    {
                        rule.m_spawnedInstances = 0;
                    }
                }
            }
            //multiply with all locations we need to spawn in
            int totalSpawns = activeSpawnRules * Spawner.GetAreaSpawnSteps(area, spawnRange);
            int totalSpawnsCompleted = 0;
            //Iterating across the area - X Axis
            for (Vector3 currentSpawnCenter = startPosition; currentSpawnCenter.x <= (area.max.x + spawnRange / 2f); currentSpawnCenter += new Vector3(locationIncrement, 0f, 0f))
            {
                if (!m_cancelSpawn)
                {
                    m_cancelSpawn = SpawnProgressBar.UpdateProgressBar("Preparing next Location...", totalSpawns, totalSpawnsCompleted, 0, 0);
                }
                if (m_cancelSpawn)
                {
                    break;
                }
                //Iterating across the area - Z Axis
                for (currentSpawnCenter = new Vector3(currentSpawnCenter.x, currentSpawnCenter.y, startPosition.z); currentSpawnCenter.z <= (area.max.z + spawnRange / 2f); currentSpawnCenter += new Vector3(0f, 0f, locationIncrement))
                {
                    if (!m_cancelSpawn)
                    {
                        m_cancelSpawn = SpawnProgressBar.UpdateProgressBar("Preparing next Location...", totalSpawns, totalSpawnsCompleted, 0, 0);
                    }
                    if (m_cancelSpawn)
                    {
                        break;
                    }

                    //Clear collision mask cache, since it needs to be built up fresh in this location anyways. The cache is then shared between all spawners
                    BakedMaskCache collisionMaskCache = SessionManager.m_bakedMaskCache;
                    collisionMaskCache.ClearCacheForSpawn();

                    GaiaMultiTerrainOperation operation = null;
                    List<TerrainMissingSpawnRules> terrainsMissingSpawnRules = new List<TerrainMissingSpawnRules>();

                    bool autoConnectNeighborsDisabled = false;

                    Terrain currentTerrain = null;

                    float boundsSize = spawnRange * 2f - 0.001f;
                    Bounds spawnerBounds = new Bounds(currentSpawnCenter, new Vector3(boundsSize, boundsSize, boundsSize));

                    //Iterate through all spawners that are spawning in "Replace" mode and clear out the previously spawned items before the spawning starts
                    //We need to remove everything first, otherwise it would give imprecise results when using collision masks
                    if (!m_settings.m_isWorldmapSpawner)
                    {
                        //We need to load terrains in before being able to remove anything
                        if (GaiaUtils.HasDynamicLoadedTerrains())
                        {
                            TerrainLoaderManager.Instance.SwitchToLocalMap();
                            TerrainLoaderManager.Instance.CenterSceneViewLoadingOn = CenterSceneViewLoadingOn.WorldOrigin;
                            TerrainLoaderManager.Instance.SetOrigin(currentSpawnCenter);
                            //Remove a tiny bit for the loading range - when the spawner directly aligns with terrain borders
                            //this will lead to a lot of terrains being loaded in unneccessarily, which takes loinger to process and
                            //creates issues during spawning.
                            TerrainLoaderManager.Instance.SetLoadingRange(spawnRange - 0.001f, spawnRange - 0.001f);
                            //if we compare against spawner bounds, that needs to be centered on world origin now because this is where we execute the spawn
                            spawnerBounds.center = Vector3.zero;
                        }
#if UNITY_EDITOR



                        foreach (Spawner spawner in spawners.FindAll(x => x.m_settings.m_spawnMode == SpawnMode.Replace))
                        {
                            foreach (SpawnRule sr in spawner.m_settings.m_spawnerRules.FindAll(x => x.m_resourceType != SpawnerResourceType.TerrainTexture))
                            {
                                //We only may remove / reset the Game Object spawner once per terrain- otherwise this might destroy earlier spawn results in world spawns!
                                foreach (Terrain t in Terrain.activeTerrains)
                                {
                                    Bounds terrainBoundsWorldSpace = new Bounds(t.transform.position + t.terrainData.size / 2f, t.terrainData.size);
                                    if (!terrainBoundsWorldSpace.Intersects(spawnerBounds))
                                    {
                                        continue;
                                    }

                                    switch (sr.m_resourceType)
                                    {
                                        case SpawnerResourceType.TerrainTexture:
                                            //should not happen, see restriction above
                                            break;
                                        case SpawnerResourceType.TerrainDetail:
                                            //We only may remove / reset this terrain detail rule once per terrain - otherwise this would destroy earlier detail spawn results in world spawns!
                                            string detailAssetGUID = "";
                                            ResourceProtoDetail detailPrototype = spawner.m_settings.m_resources.m_detailPrototypes[sr.m_resourceIdx];
                                            if (detailPrototype.m_renderMode == DetailRenderMode.VertexLit)
                                            {
                                                detailAssetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(detailPrototype.m_detailProtoype));
                                            }
                                            if (detailPrototype.m_renderMode == DetailRenderMode.Grass)
                                            {
                                                if (detailPrototype.m_detailProtoype != null)
                                                {
                                                    detailAssetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(detailPrototype.m_detailProtoype));
                                                }
                                                else
                                                {
                                                    detailAssetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(detailPrototype.m_detailTexture));
                                                }
                                            }
                                            if (detailPrototype.m_renderMode == DetailRenderMode.GrassBillboard)
                                            {
                                                detailAssetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(detailPrototype.m_detailTexture));
                                            }
                                            if (m_clearedDetailProtos.Find(x => x.terrain == t && x.m_prototypeAssetGUID == detailAssetGUID) == null)
                                            {
                                                int terrainDetailIndex = spawner.m_settings.m_resources.PrototypeIdxInTerrain(sr.m_resourceType, sr.m_resourceIdx, t);
                                                if (terrainDetailIndex != -1)
                                                {
                                                    t.terrainData.SetDetailLayer(0, 0, terrainDetailIndex, new int[t.terrainData.detailWidth, t.terrainData.detailHeight]);
                                                }
                                                m_clearedDetailProtos.Add(new TerrainPrototypeId() { terrain = t, m_prototypeAssetGUID = detailAssetGUID });
                                            }
                                            break;
                                        case SpawnerResourceType.TerrainTree:
                                            //We only may remove / reset this tree rule once per terrain - otherwise this would destroy earlier tree spawn results in world spawns!

                                            string treeAssetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(spawner.m_settings.m_resources.m_treePrototypes[sr.m_resourceIdx].m_desktopPrefab));

                                            if (m_clearedTreeProtos.Find(x => x.terrain == t && x.m_prototypeAssetGUID == treeAssetGUID) == null)
                                            {
                                                int treePrototypeIndex = spawner.m_settings.m_resources.PrototypeIdxInTerrain(sr.m_resourceType, sr.m_resourceIdx, t);
                                                TreeInstance[] newTrees = t.terrainData.treeInstances.Where(x => x.prototypeIndex != treePrototypeIndex).ToArray();
                                                t.terrainData.SetTreeInstances(newTrees, true);
                                                m_clearedTreeProtos.Add(new TerrainPrototypeId() { terrain = t, m_prototypeAssetGUID = treeAssetGUID });
                                            }
                                            break;
                                        case SpawnerResourceType.GameObject:
                                            //Game Object resources are difficult to uniquely identify, since the same prefab could be used in different spawn rules in a different context
                                            //we therefore use the name of the spawn rule as an unique identifier, since the container object for the spawned results is created by the name of the spawn rule anyways
                                            string gameObjectAssetGUID = sr.m_name;
                                            if (m_clearedGameObjectProtos.Find(x => x.terrain == t && x.m_prototypeAssetGUID == gameObjectAssetGUID) == null)
                                            {
                                                ClearGameObjectsForRule(spawner, sr, false, t);
                                                m_clearedGameObjectProtos.Add(new TerrainPrototypeId() { terrain = t, m_prototypeAssetGUID = gameObjectAssetGUID });
                                            }
                                            break;
                                        case SpawnerResourceType.SpawnExtension:
                                            //Same principle as for game objects
                                            string spawnExtensionAssetGUID = sr.m_name;
                                            if (m_clearedSpawnExtensionProtos.Find(x => x.terrain == t && x.m_prototypeAssetGUID == spawnExtensionAssetGUID) == null)
                                            {
                                                ClearSpawnExtensionsForRule(sr);
                                                m_clearedSpawnExtensionProtos.Add(new TerrainPrototypeId() { terrain = t, m_prototypeAssetGUID = spawnExtensionAssetGUID });
                                            }
                                            break;
                                        case SpawnerResourceType.Probe:
                                            //Same principle as for game objects
                                            string probeAssetGUID = sr.m_name;
                                            if (m_clearedProbeProtos.Find(x => x.terrain == t && x.m_prototypeAssetGUID == probeAssetGUID) == null)
                                            {
                                                //Deletion is handled by the Clear Game Objects function since the probes are essentially game objects
                                                ClearGameObjectsForRule(spawner, sr, false, t);
                                                m_clearedProbeProtos.Add(new TerrainPrototypeId() { terrain = t, m_prototypeAssetGUID = probeAssetGUID });
                                            }
                                            break;
                                        case SpawnerResourceType.StampDistribution:
                                            //Same principle as for game objects
                                            string stampDistributionAssetGUID = sr.m_name;
                                            if (m_clearedStampDistributionProtos.Find(x => x.terrain == t && x.m_prototypeAssetGUID == stampDistributionAssetGUID) == null)
                                            {
                                                ClearStampDistributionForRule(sr);
                                                m_clearedStampDistributionProtos.Add(new TerrainPrototypeId() { terrain = t, m_prototypeAssetGUID = stampDistributionAssetGUID });
                                            }
                                            break;
                                        case SpawnerResourceType.WorldBiomeMask:
                                            //not relevant
                                            break;
                                    }
                                }
                            }
                        }
#endif
                    }

                    //Iterating through all the spawners in this location for the actual spawn
                    foreach (Spawner spawner in spawners)
                    {
                        spawner.UpdateMinMaxHeight();
                        //Depending on wether we are in a dynamic loading scenario or not, we need to either control the dynamic loading to load terrains in below the spawner
                        //or move the spawner across the world. The world map spawner should not need to load terrains - just spawns to the world map.
                        if (GaiaUtils.HasDynamicLoadedTerrains() && !m_settings.m_isWorldmapSpawner)
                        {
                            TerrainLoaderManager.Instance.SwitchToLocalMap();
                            TerrainLoaderManager.Instance.CenterSceneViewLoadingOn = CenterSceneViewLoadingOn.WorldOrigin;
                            TerrainLoaderManager.Instance.SetOrigin(currentSpawnCenter);
                            //Remove a tiny bit for the loading range - when the spawner directly aligns with terrain borders
                            //this will lead to a lot of terrains being loaded in unneccessarily, which takes loinger to process and
                            //creates issues during spawning.
                            TerrainLoaderManager.Instance.SetLoadingRange(spawnRange - 0.001f, spawnRange - 0.001f);
                            spawner.transform.position = Vector3.zero;
                            spawner.m_settings.m_spawnRange = spawnRange;
                        }
                        else
                        {
                            if (m_settings.m_isWorldmapSpawner)
                            {
                                TerrainLoaderManager.Instance.SwitchToWorldMap();
                            }

                            spawner.transform.position = currentSpawnCenter;
                            spawner.m_settings.m_spawnRange = spawnRange;
                        }


                        currentTerrain = spawner.GetCurrentTerrain();
                        if (currentTerrain != null)
                        {
                            //the neighbor system can create issues with spawning if there is a spawn executed 
                            //while there is a gap between terrains, this can lead to faulty pixels on the edge of the normal map.
                            if (currentTerrain.allowAutoConnect)
                            {
                                currentTerrain.allowAutoConnect = false;
                                currentTerrain.SetNeighbors(null, null, null, null);
                                currentTerrain.terrainData.DirtyHeightmapRegion(new RectInt(0, 0, currentTerrain.terrainData.heightmapResolution, currentTerrain.terrainData.heightmapResolution), TerrainHeightmapSyncControl.HeightOnly);
                                currentTerrain.terrainData.SyncHeightmap();
                                autoConnectNeighborsDisabled = true;
                                yield return null;
                            }
                        }
                        else
                        {
                            continue;
                        }

                        try
                        {
                            //Check for missing resources in the currently loaded terrains.
                            //This information is passed into the operation which can then
                            //add the resources "on demand" while spawning.
                            if (!m_settings.m_isWorldmapSpawner)
                            {
                                terrainsMissingSpawnRules = spawner.GetMissingResources(terrainsMissingSpawnRules, Terrain.activeTerrains);
                            }


                            if (currentTerrain != null)
                            {
                                operation = new GaiaMultiTerrainOperation(currentTerrain, spawner.transform, spawnRange * 2f, true, validTerrainNames);
                                operation.m_isWorldMapOperation = m_settings.m_isWorldmapSpawner;
                                operation.m_terrainsMissingSpawnRules = terrainsMissingSpawnRules;
                                operation.GetHeightmap();
                                operation.GetNormalmap();
                                operation.CollectTerrainDetails();
                                operation.CollectTerrainTrees();
                                operation.CollectTerrainGameObjects();
                                operation.CollectTerrainBakedMasks();
                                spawner.ExecuteSpawn(operation, collisionMaskCache, totalSpawns, ref totalSpawnsCompleted);
                                if (spawnedGameObjects)
                                {
                                    foreach (Terrain t in operation.affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.GameObject).Select(x => x.Key.terrain))
                                    {
#if UNITY_EDITOR
                                        EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
                                        //apply the hierarchy hide settings
                                        GaiaHierarchyUtils ghu = t.transform.GetComponentInChildren<GaiaHierarchyUtils>();
                                        if (ghu != null)
                                        {
                                            ghu.SetupHideInHierarchy();
                                        }
#endif
                                    }
                                }
                                //Clean up between spawners
                                operation.CloseOperation();
                            }
                            else
                            {
                                Debug.LogError("Trying to spawn, but could not find any terrain for spawning!");
                            }

                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("Error during spawning, Error Message: " + ex.Message + " Stack Trace: " + ex.StackTrace);
                        }
                        finally
                        {
                            GaiaUtils.ReleaseAllTempRenderTextures();
                            spawner.m_spawnPreviewDirty = true;
                            spawner.SetWorldBiomeMasksDirty();
                            spawner.m_settings.ClearImageMaskTextures();
                        }
                        yield return null;
                    } //spawners

                    //if we disabled autoconnect during the spawn, we need to re-enable it
                    if (autoConnectNeighborsDisabled && currentTerrain != null)
                    {
                        currentTerrain.allowAutoConnect = true;
                    }

#if GAIA_PRO_PRESENT
                    //De-select the loaders only after all spawners ran in this location, otherwise terrains are being loaded / unloaded constantly on yield
                    foreach (Spawner spawner in spawners)
                    {
                        spawner.TerrainLoader.m_isSelected = false;
                    }
#endif
                    yield return null;

                }// for Z
            } // for X
            SpawnProgressBar.ClearProgressBar();
            m_updateCoroutine = null;

#if UNITY_EDITOR && GAIA_PRO_PRESENT
            //if the currently selected object is a spawner we switch back on the "selected" flag
            if (Selection.activeObject != null)
            {
                //try catch needed for the GameObject cast
                try
                {

                    Spawner selectedSpawner = ((GameObject)Selection.activeObject).GetComponent<Spawner>();
                    if (selectedSpawner != null)
                    {
                        selectedSpawner.TerrainLoader.m_isSelected = true;
                    }

                }
                catch (Exception ex)
                {
                    if (ex.Message == "123")
                    {
                        //Preventing compiler warning for unused "ex"
                    }
                }

            }

            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                TerrainLoaderManager.Instance.CenterSceneViewLoadingOn = originalCenter;
                TerrainLoaderManager.Instance.SetLoadingRange(originalLoadingRange, originalLoadingRangeImpostor);
                TerrainLoaderManager.Instance.SetOrigin(originalOrigin);
            }
#endif
            GaiaStopwatch.EndEvent("Area Spawn");
            GaiaStopwatch.Stop();
            yield return null;

        }

        public void ClearPrototypeLists()
        {
            m_clearedGameObjectProtos.Clear();
            m_clearedSpawnExtensionProtos.Clear();
            m_clearedTreeProtos.Clear();
            m_clearedDetailProtos.Clear();
            m_clearedStampDistributionProtos.Clear();
            m_clearedProbeProtos.Clear();
        }

        /// <summary>
        /// Flags the world biome masks maintained in this spawner as dirty
        /// </summary>
        public void SetWorldBiomeMasksDirty()
        {
            if (m_settings.m_isWorldmapSpawner)
            {
                for (int i = 0; i < m_settings.m_spawnerRules.Count; i++)
                {
                    if (m_settings.m_spawnerRules[i].m_resourceType == SpawnerResourceType.WorldBiomeMask)
                    {
                        SessionManager.m_bakedMaskCache.SetWorldBiomeMaskDirty(m_settings.m_spawnerRules[i].GUID);
                    }
                }
            }
        }

        private void ExecuteSpawn(GaiaMultiTerrainOperation operation, BakedMaskCache collisionMaskCache, int totalSpawns, ref int totalSpawsCompleted, bool allowStatic = true)
        {
            GaiaStopwatch.StartEvent("Execute Spawn");
            int maxSpawnerRules = m_settings.m_spawnerRules.Where(x => x.m_isActive == true && x.m_resourceType != SpawnerResourceType.WorldBiomeMask).Count();
            int completedSpawnerRules = 0;

            //Create a new random generator that will use the seed entered in the spawner ui to generate one seed each per spawn rule.
            //We can't simply pass down the seed in the rules, otherwise those will produce the same / too similar results
            XorshiftPlus xorshiftPlus = new XorshiftPlus(m_settings.m_randomSeed);
            xorshiftPlus.NextInt();

            //pre generate the seeds for each rule, regardless if the rule is active or not - this allows to deactivate a single rule but still getting the same result from the seed.
            int[] randomSeeds = new int[m_settings.m_spawnerRules.Count];
            for (int i = 0; i < m_settings.m_spawnerRules.Count; i++)
            {
                randomSeeds[i] = xorshiftPlus.NextInt();
            }

            if (m_settings.m_isWorldmapSpawner)
            {
                //clear the stamp operation list first, we will later rebuild it by iterating through all spawned / remaining tokens
                m_worldMapStamperSettings.Clear();
            }

            Terrain currentTerrain = GetCurrentTerrain();
            for (int i = 0; i < m_settings.m_spawnerRules.Count; i++)
            {
                //wrap in try-catch to close any progress bars on potential errors & possibly at least continue to spawn the other rules
                try
                {

                    if (!m_cancelSpawn)
                    {
                        m_cancelSpawn = SpawnProgressBar.UpdateProgressBar(this.name, totalSpawns, totalSpawsCompleted, maxSpawnerRules, completedSpawnerRules);
                    }

                    if (m_cancelSpawn)
                    {
                        break;
                    }

                    if (m_settings.m_spawnerRules[i].m_isActive)
                    {
                        switch (m_settings.m_spawnerRules[i].m_resourceType)
                        {
                            case GaiaConstants.SpawnerResourceType.TerrainTexture:

                                ResourceProtoTexture proto = m_settings.m_resources.m_texturePrototypes[m_settings.m_spawnerRules[i].m_resourceIdx];
                                //Look for the layer file associated with the resource in any of the currently active terrains
                                TerrainLayer targetLayer = TerrainHelper.GetLayerFromPrototype(proto);

                                operation.GetSplatmap(targetLayer);

                                RenderTexture tempTextureRT = SimulateRule(operation, i);

                                //Add missing texture / terrain layer - but only if required according to the simulation!
                                foreach (TerrainMissingSpawnRules tmsr in operation.m_terrainsMissingSpawnRules)
                                {
                                    if (operation.affectedTerrainPixels.Where(x => x.Key.terrain == tmsr.terrain && x.Key.operationType == MultiTerrainOperationType.Texture && x.Value.simulationPositive == true).Count() > 0)
                                    {
                                        operation.HandleMissingResources(this, m_settings.m_spawnerRules[i], tmsr.terrain);
                                    }
                                }

                                //Look for the target layer again - it might have been added now
                                if (targetLayer == null)
                                {
                                    targetLayer = TerrainHelper.GetLayerFromPrototype(proto);
                                }

                                if (targetLayer != null)
                                {
                                    //need to call Get Splatmap again before calling SetSplatmap since texture masks inside there can jeopardize the spawn result.
                                    operation.GetSplatmap(targetLayer);
                                    operation.SetSplatmap(tempTextureRT, this, m_settings.m_spawnerRules[i], false);
                                }

                                break;
                            case GaiaConstants.SpawnerResourceType.TerrainDetail:
                                RenderTexture tempTerrainDetailRT = SimulateRule(operation, i);
                                int affectedDetailTerrainsCount = operation.affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.TerrainDetail && x.Value.simulationPositive == true).Count();
                                if (m_settings.m_spawnMode == SpawnMode.Replace)
                                {
                                    SpawnMode originalMode = m_settings.m_spawnMode;

                                    m_settings.m_spawnMode = SpawnMode.Add;
                                    //foreach (Terrain t in operation.affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.TerrainDetail).Select(x => x.Key.terrain))
                                    //{
                                    //    ////We only may remove / reset this terrain detail rule once per terrain - otherwise this would destroy earlier detail spawn results in world spawns!
                                    //    //if (m_clearedDetailProtos.Find(x => x.terrain == t && x.prototypeId == m_settings.m_spawnerRules[i].m_resourceIdx) == null)
                                    //    //{
                                    //    //    //int terrainDetailIndex = m_settings.m_resources.PrototypeIdxInTerrain(m_settings.m_spawnerRules[i].m_resourceType, m_settings.m_spawnerRules[i].m_resourceIdx, t);
                                    //    //    //if (terrainDetailIndex != -1)
                                    //    //    //{
                                    //    //    //    t.terrainData.SetDetailLayer(0, 0, terrainDetailIndex, new int[t.terrainData.detailWidth, t.terrainData.detailHeight]);
                                    //    //    //}
                                    //    //    m_settings.spawnMode = SpawnMode.Replace;
                                    //    //    m_clearedDetailProtos.Add(new TerrainPrototypeId() { terrain = t, prototypeId = m_settings.m_spawnerRules[i].m_resourceIdx });
                                    //    //}
                                    //}
                                    if (affectedDetailTerrainsCount > 0)
                                    {
                                        operation.SetTerrainDetails(tempTerrainDetailRT, m_settings, this, m_settings.m_spawnerRules[i], randomSeeds[i], false);
                                    }

                                    m_settings.m_spawnMode = originalMode;
                                }
                                else
                                {
                                    if (affectedDetailTerrainsCount > 0)
                                    {
                                        operation.SetTerrainDetails(tempTerrainDetailRT, m_settings, this, m_settings.m_spawnerRules[i], randomSeeds[i], false);
                                    }
                                }

                                break;
                            case GaiaConstants.SpawnerResourceType.TerrainTree:
                                //foreach (Terrain t in operation.affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.Tree).Select(x => x.Key.terrain))
                                //{
                                //    //We only may remove / reset this tree rule once per terrain - otherwise this would destroy earlier tree spawn results in world spawns!
                                //    if (m_settings.spawnMode == SpawnMode.Replace && m_clearedTreeProtos.Find(x => x.terrain == t && x.prototypeId == m_settings.m_spawnerRules[i].m_resourceIdx) == null)
                                //    {
                                //        int treePrototypeIndex = m_settings.m_resources.PrototypeIdxInTerrain(m_settings.m_spawnerRules[i].m_resourceType, m_settings.m_spawnerRules[i].m_resourceIdx, t);
                                //        t.terrainData.SetTreeInstances(t.terrainData.treeInstances.Where(x => x.prototypeIndex != treePrototypeIndex).ToArray(), true);
                                //        m_settings.m_spawnerRules[i].m_spawnedInstances = 0;
                                //        m_clearedTreeProtos.Add(new TerrainPrototypeId() { terrain = t, prototypeId = m_settings.m_spawnerRules[i].m_resourceIdx });
                                //    }
                                //}

                                RenderTexture tempTreeRT = SimulateRule(operation, i);
                                int affectedTreeTerrainsCount = operation.affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.Tree && x.Value.simulationPositive == true).Count();

                                m_cancelSpawn = SpawnProgressBar.UpdateProgressBar(this.name, totalSpawns, totalSpawsCompleted, maxSpawnerRules, completedSpawnerRules);
                                if (affectedTreeTerrainsCount > 0)
                                {
                                    //Remember the scaling settings that were last used in a spawn - required to re-scale tree instances when doing a prototype refresh.
                                    m_settings.m_resources.m_treePrototypes[m_settings.m_spawnerRules[i].m_resourceIdx].StoreLastUsedScaleSettings();
                                    operation.SetTerrainTrees(tempTreeRT, m_settings, this, m_settings.m_spawnerRules[i], randomSeeds[i], false);
                                }

                                collisionMaskCache.SetTreeDirty(m_settings.m_spawnerRules[i].GUID, m_settings.m_resources.m_treePrototypes[m_settings.m_spawnerRules[i].m_resourceIdx].m_desktopPrefab.layer);

                                break;
                            case GaiaConstants.SpawnerResourceType.GameObject:
                                //int goPrototypeIndex = m_settings.m_resources.PrototypeIdxInTerrain(m_settings.m_spawnerRules[i].m_resourceType, m_settings.m_spawnerRules[i].m_resourceIdx);

                                //We only may remove / reset the Game Object spawner once per terrain- otherwise this would destroy earlier spawn results in world spawns!
                                //foreach (Terrain t in operation.affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.GameObject).Select(x => x.Key.terrain))
                                //{
                                //    if (m_settings.spawnMode == SpawnMode.Replace && m_clearedGameObjectProtos.Find(x => x.terrain == t && x.prototypeId == m_settings.m_spawnerRules[i].m_resourceIdx) == null)
                                //    {
                                //        ClearGameObjectsForRule(m_settings.m_spawnerRules[i], false, t);
                                //        m_clearedGameObjectProtos.Add(new TerrainPrototypeId() { terrain = t, prototypeId = m_settings.m_spawnerRules[i].m_resourceIdx });
                                //    }
                                //}

                                ResourceProtoGameObject protoGO = m_settings.m_resources.m_gameObjectPrototypes[m_settings.m_spawnerRules[i].m_resourceIdx];

                                if (protoGO == null)
                                {
                                    Debug.LogWarning("Could not find Game Object Prototype for Spawn Rule " + m_settings.m_spawnerRules[i].m_name);
                                    break;
                                }
                                m_cancelSpawn = SpawnProgressBar.UpdateProgressBar(this.name, totalSpawns, totalSpawsCompleted, maxSpawnerRules, completedSpawnerRules);

                                RenderTexture tempGameObjectRT = SimulateRule(operation, i);
                                int affectedGameObjectTerrainsCount = operation.affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.GameObject && x.Value.simulationPositive == true).Count();
                                if (affectedGameObjectTerrainsCount > 0)
                                {
                                    operation.SetTerrainGameObjects(tempGameObjectRT, protoGO, m_settings.m_spawnerRules[i], m_settings, randomSeeds[i], ref m_settings.m_spawnerRules[i].m_spawnedInstances, m_settings.m_spawnerRules[i].m_minRequiredFitness, false);
                                }

                                //Dirty affected collision masks - if we spawned Gameobject instances with tags that are used in other collision masks, we must dirty them so they will be re-baked upon request
                                foreach (ResourceProtoGameObjectInstance instance in protoGO.m_instances)
                                {
                                    collisionMaskCache.SetGameObjectDirty(instance.m_desktopPrefab);
                                }
                                break;
                            case GaiaConstants.SpawnerResourceType.SpawnExtension:
                                ResourceProtoSpawnExtension protoSpawnExtension = m_settings.m_resources.m_spawnExtensionPrototypes[m_settings.m_spawnerRules[i].m_resourceIdx];

                                //We only may remove / reset the Spawn Extension once per terrain - otherwise this would destroy earlier spawn results in world spawns!
                                //foreach (Terrain t in operation.affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.GameObject).Select(x => x.Key.terrain))
                                //{
                                //    if (m_settings.spawnMode == SpawnMode.Replace && m_clearedSpawnExtensionProtos.Find(x => x.terrain == t && x.prototypeId == m_settings.m_spawnerRules[i].m_resourceIdx) == null)
                                //    {
                                //        ClearSpawnExtensionsForRule(m_settings.m_spawnerRules[i]);
                                //        m_clearedSpawnExtensionProtos.Add(new TerrainPrototypeId() { terrain = t, prototypeId = m_settings.m_spawnerRules[i].m_resourceIdx });
                                //    }
                                //}
                                foreach (ResourceProtoSpawnExtensionInstance instance in protoSpawnExtension.m_instances)
                                {
                                    GameObject prefab = instance.m_spawnerPrefab;
                                    //Get ALL spawn extensions - could potentially be multiple on prefab
                                    var instanceSpawnExtensions = prefab.GetComponents<ISpawnExtension>();
                                    foreach (ISpawnExtension spawnExtension in instanceSpawnExtensions)
                                    {
                                        spawnExtension.Init(this);
                                    }
                                }
                                m_cancelSpawn = SpawnProgressBar.UpdateProgressBar(this.name, totalSpawns, totalSpawsCompleted, maxSpawnerRules, completedSpawnerRules);
                                m_cancelSpawn = operation.SetSpawnExtensions(ApplyBrush(operation, MultiTerrainOperationType.GameObject, i), this, protoSpawnExtension, m_settings, i, m_settings.m_spawnerRules[i], randomSeeds[i], ref m_settings.m_spawnerRules[i].m_spawnedInstances, m_settings.m_spawnerRules[i].m_minRequiredFitness, false);

                                foreach (ResourceProtoSpawnExtensionInstance instance in protoSpawnExtension.m_instances)
                                {
                                    GameObject prefab = instance.m_spawnerPrefab;
                                    //Get ALL spawn extensions - could potentially be multiple on prefab
                                    var instanceSpawnExtensions = prefab.GetComponents<ISpawnExtension>();
                                    foreach (ISpawnExtension spawnExtension in instanceSpawnExtensions)
                                    {
                                        spawnExtension.Close();
                                    }
                                }
                                break;

                            case GaiaConstants.SpawnerResourceType.StampDistribution:
                                ResourceProtoStampDistribution protoStampDistribution = m_settings.m_resources.m_stampDistributionPrototypes[m_settings.m_spawnerRules[i].m_resourceIdx];
                                //We only may remove / reset the Stamp Distribution once per terrain - otherwise this would destroy earlier spawn results in world spawns!
                                //foreach (Terrain t in operation.affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.GameObject).Select(x => x.Key.terrain))
                                //{
                                //    if (m_settings.spawnMode == SpawnMode.Replace && m_clearedStampDistributionProtos.Find(x => x.terrain == t && x.prototypeId == m_settings.m_spawnerRules[i].m_resourceIdx) == null)
                                //    {
                                //        ClearStampDistributionForRule(m_settings.m_spawnerRules[i]);
                                //        m_clearedStampDistributionProtos.Add(new TerrainPrototypeId() { terrain = t, prototypeId = m_settings.m_spawnerRules[i].m_resourceIdx });
                                //    }
                                //}
                                m_cancelSpawn = SpawnProgressBar.UpdateProgressBar(this.name, totalSpawns, totalSpawsCompleted, maxSpawnerRules, completedSpawnerRules);
                                //make sure we have defaults in there
                                if (m_worldCreationSettings.m_gaiaDefaults == null)
                                {
                                    m_worldCreationSettings.m_gaiaDefaults = Instantiate(GaiaSettings.m_currentDefaults);
                                }
                                operation.SetWorldMapStamps(ApplyBrush(operation, MultiTerrainOperationType.GameObject, i), this, protoStampDistribution, m_settings.m_spawnMode, i, m_settings.m_spawnerRules[i], m_worldCreationSettings, randomSeeds[i], ref m_settings.m_spawnerRules[i].m_spawnedInstances);
                                break;
                            case GaiaConstants.SpawnerResourceType.Probe:
                                ResourceProtoProbe protoProbe = m_settings.m_resources.m_probePrototypes[m_settings.m_spawnerRules[i].m_resourceIdx];
                                //We only may remove / reset the Stamp Distribution once per terrain - otherwise this would destroy earlier spawn results in world spawns!
                                //foreach (Terrain t in operation.affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.GameObject).Select(x => x.Key.terrain))
                                //{
                                //    if (m_settings.spawnMode == SpawnMode.Replace && m_clearedProbeProtos.Find(x => x.terrain == t && x.prototypeId == m_settings.m_spawnerRules[i].m_resourceIdx) == null)
                                //    {
                                //        //Deletion is handled by the Clear Game Objects function since the probes are essentially game objects
                                //        ClearGameObjectsForRule(m_settings.m_spawnerRules[i]);
                                //        m_clearedProbeProtos.Add(new TerrainPrototypeId() { terrain = t, prototypeId = m_settings.m_spawnerRules[i].m_resourceIdx });
                                //    }
                                //}
                                m_cancelSpawn = SpawnProgressBar.UpdateProgressBar(this.name, totalSpawns, totalSpawsCompleted, maxSpawnerRules, completedSpawnerRules);
                                float seaLevel = 0f;
                                bool seaLevelActive = false;

                                PWS_WaterSystem gaiawater = GameObject.FindObjectOfType<PWS_WaterSystem>();
                                if (gaiawater != null)
                                {
                                    seaLevel = gaiawater.SeaLevel;
                                    seaLevelActive = true;
                                }
                                operation.SetProbes(ApplyBrush(operation, MultiTerrainOperationType.GameObject, i), this, protoProbe, m_settings.m_spawnMode, i, m_settings.m_spawnerRules[i], randomSeeds[i], ref m_settings.m_spawnerRules[i].m_spawnedInstances, seaLevelActive, seaLevel);
                                break;

                        }
                        completedSpawnerRules++;
                        totalSpawsCompleted++;

                    }

                    if (m_cancelSpawn)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    SpawnProgressBar.ClearProgressBar();
                    Debug.LogError("Exception while spawning: " + ex.Message + " Stack Trace: " + ex.StackTrace);
                }
            }

            if (m_settings.m_isWorldmapSpawner)
            {
                //update the session with the terrain tile settings we just spawned - those are required to display the stamper tokens in the correct size on the world map

                TerrainLoaderManager.Instance.TerrainSceneStorage.m_terrainTilesX = m_worldCreationSettings.m_xTiles;
                TerrainLoaderManager.Instance.TerrainSceneStorage.m_terrainTilesZ = m_worldCreationSettings.m_zTiles;

                //Collect the actual created / remaining stamper settings from the tokens
                Transform target = m_worldMapTerrain.transform.Find(GaiaConstants.worldMapStampTokenSpawnTarget);
                if (target != null)
                {
                    foreach (Transform t in target)
                    {
                        WorldMapStampToken token = t.GetComponent<WorldMapStampToken>();
                        if (token != null)
                        {
                            m_worldMapStamperSettings.Add(token.m_connectedStamperSettings);
                        }
                    }
                }

            }
            GaiaStopwatch.EndEvent("Execute Spawn");
        }

        private RenderTexture SimulateRule(GaiaMultiTerrainOperation operation, int spawnRuleID)
        {
            SpawnRule spawnRule = m_settings.m_spawnerRules[spawnRuleID];
            if (m_gaiaSettings == null)
            {
                m_gaiaSettings = GaiaUtils.GetGaiaSettings();
            }

            ComputeShader shader = m_gaiaSettings.m_spawnSimulateComputeShader;
            int kernelHandle = shader.FindKernel("CSMain");

            MultiTerrainOperationType multiTerrainOperationType = MultiTerrainOperationType.GameObject;
            float fitnessThreshold = 0.5f;

            switch (spawnRule.m_resourceType)
            {
                case SpawnerResourceType.TerrainDetail:
                    multiTerrainOperationType = MultiTerrainOperationType.TerrainDetail;
                    fitnessThreshold = spawnRule.m_terrainDetailMinFitness;
                    break;
                case SpawnerResourceType.TerrainTexture:
                    multiTerrainOperationType = MultiTerrainOperationType.Texture;
                    fitnessThreshold = 0;
                    break;
                case SpawnerResourceType.TerrainTree:
                    multiTerrainOperationType = MultiTerrainOperationType.Tree;
                    fitnessThreshold = spawnRule.m_minRequiredFitness;
                    break;
                case SpawnerResourceType.GameObject:
                    multiTerrainOperationType = MultiTerrainOperationType.GameObject;
                    fitnessThreshold = spawnRule.m_minRequiredFitness;
                    break;
                case SpawnerResourceType.SpawnExtension:
                    multiTerrainOperationType = MultiTerrainOperationType.GameObject;
                    fitnessThreshold = 0;
                    break;
                case SpawnerResourceType.StampDistribution:
                    multiTerrainOperationType = MultiTerrainOperationType.GameObject;
                    fitnessThreshold = 0;
                    break;
                case SpawnerResourceType.WorldBiomeMask:
                    //this should never happen
                    break;
            }

            RenderTexture opRenderTexture = ApplyBrush(operation, multiTerrainOperationType, spawnRuleID);

            //Get the affected terrains according to operation type
            var affectedTerrains = operation.affectedTerrainPixels.Where(x => x.Key.operationType == multiTerrainOperationType).ToArray();

            //Build an input and output data buffer array to get info about the terrain positions in and out of the compute shader
            TerrainPosition[] inputTerrainPositions = new TerrainPosition[affectedTerrains.Count()];
            TerrainPosition[] outputTerrainPositions = new TerrainPosition[affectedTerrains.Count()];
            for (int i = 0; i < affectedTerrains.Count(); i++)
            {
                var entry = affectedTerrains[i];
                //assume first these terrain pixels will be affected, since this entry could still be set to false
                //form a previous spawn.
                affectedTerrains[i].Value.simulationPositive = true;
                inputTerrainPositions[i] = new TerrainPosition()
                {
                    terrainID = i,
                    min = entry.Value.affectedOperationPixels.min,
                    max = entry.Value.affectedOperationPixels.max,
                    affected = 0
                };
            }

            //Configure & run the compute shader
            ComputeBuffer buffer = new ComputeBuffer(affectedTerrains.Count(), 24);
            buffer.SetData(inputTerrainPositions);
            shader.SetTexture(kernelHandle, "Input", opRenderTexture);
            shader.SetFloat("fitnessThreshold", spawnRule.m_terrainDetailMinFitness);
            shader.SetInt("numberOfTerrains", affectedTerrains.Count());
            shader.SetBuffer(kernelHandle, "outputBuffer", buffer);
            shader.Dispatch(kernelHandle, opRenderTexture.width / 8, opRenderTexture.height / 8, 1);
            buffer.GetData(outputTerrainPositions);
            //We got the result, now take our initial array and check if those terrains listed in the OP are actually affected
            for (int i = 0; i < affectedTerrains.Count(); i++)
            {
                TerrainPosition terrainPosition = outputTerrainPositions[i];
                if (terrainPosition.affected <= 0)
                {
                    //kick out this entry from the operation if the simulation result says it will not be affected
                    // => no need to execute those later when spawning
                    affectedTerrains[i].Value.simulationPositive = false;
                }
            }

            buffer.Release();
            return opRenderTexture;

        }

        /// <summary>
        /// Returns the required spawner runs to cover the entire wolrd according to the current max spawner size when doing a spawn across a certain area
        /// </summary>
        /// <param name="area">The area we are iterating over</param>
        /// <param name="range">The spawner range used for the iterations</param>
        /// <returns>The number of steps required to iterate across the world</returns>
        public static int GetAreaSpawnSteps(BoundsDouble area, float range)
        {
            float spawnRange = (float)Mathd.Min(range, Mathd.Min(area.extents.x, area.extents.z));
            return Mathd.CeilToInt(area.size.x / (spawnRange * 2f)) * Mathd.CeilToInt(area.size.z / (spawnRange * 2f));
        }


        /// <summary>
        /// Decreases all resource indexes by 1 for a certain resource type - used when a resource is removed from the spawner so all indices need to be corrected by 1 above the old index of the deleted resource.
        /// </summary>
        /// <param name="terrainTree"></param>
        /// <param name="i"></param>
        public void CorrectIndicesAfteResourceDeletion(SpawnerResourceType resourceType, int oldIndex)
        {

            foreach (SpawnRule sr in m_settings.m_spawnerRules.Where(x => x.m_resourceType == resourceType))
            {
                if (sr.m_resourceIdx >= oldIndex)
                {
                    sr.m_resourceIdx--;
                }
            }
        }

        /// <summary>
        /// Run a random location based spawner iteration - the spawner is always trying to spawn something on the underlying terrain
        /// </summary>
        public IEnumerator RunRandomSpawnerIteration()
        {
            //if (m_showDebug)
            //{
            //    Debug.Log(string.Format("{0}: Running random iteration", gameObject.name));
            //}

            ////Start iterating
            //int ruleIdx;
            //float fitness, maxFitness, selectedFitness;
            //SpawnRule rule, fittestRule, selectedRule;
            //SpawnInfo spawnInfo = new SpawnInfo();
            //SpawnLocation spawnLocation;
            //List<SpawnLocation> spawnLocations = new List<SpawnLocation>();
            //int spawnLocationsIdx = 0;
            //int failedSpawns = 0;

            ////Set progress
            //m_spawnProgress = 0f;
            //m_spawnComplete = false;

            ////Time control for enumeration
            //float currentTime = Time.realtimeSinceStartup;
            //float accumulatedTime = 0.0f;

            ////Create spawn caches
            //CreateSpawnCaches();

            ////Load image filter
            //LoadImageMask();

            //for (int terrainID = 0; terrainID < Terrain.activeTerrains.Length; terrainID++)
            //{
            //    //Set up the texture layer array in spawn info
            //    spawnInfo.m_textureStrengths = new float[Terrain.activeTerrains[terrainID].terrainData.alphamapLayers];

            //    //Run the location checks
            //    for (int checks = 0; checks < m_locationChecksPerInt; checks++)
            //    {
            //        //Create the spawn location
            //        spawnLocation = new SpawnLocation();

            //        //Choose a random location around the spawner
            //        if (m_spawnLocationAlgorithm == GaiaConstants.SpawnerLocation.RandomLocation)
            //        {
            //            spawnLocation.m_location = GetRandomV3(m_settings.m_spawnRange);
            //            spawnLocation.m_location = transform.position + spawnLocation.m_location;
            //        }
            //        else
            //        {
            //            if (spawnLocations.Count == 0 || spawnLocations.Count > m_maxRandomClusterSize || failedSpawns > m_maxRandomClusterSize)
            //            {
            //                spawnLocation.m_location = GetRandomV3(m_settings.m_spawnRange);
            //                spawnLocation.m_location = transform.position + spawnLocation.m_location;
            //                failedSpawns = 0;
            //                spawnLocationsIdx = 0;
            //                spawnLocations.Clear();
            //            }
            //            else
            //            {
            //                if (spawnLocationsIdx >= spawnLocations.Count)
            //                {
            //                    spawnLocationsIdx = 0;
            //                }
            //                spawnLocation.m_location = GetRandomV3(spawnLocations[spawnLocationsIdx].m_seedDistance);
            //                spawnLocation.m_location = spawnLocations[spawnLocationsIdx++].m_location + spawnLocation.m_location;
            //            }
            //        }

            //        //Run a ray traced hit check to see what we have hit, use rules to determine fitness and select a rule to spawn
            //        if (CheckLocation(spawnLocation.m_location, ref spawnInfo))
            //        {
            //            //Now perform a rule check based on the selected algorithm

            //            //All rules
            //            if (m_spawnRuleSelector == GaiaConstants.SpawnerRuleSelector.All)
            //            {
            //                for (ruleIdx = 0; ruleIdx < m_settings.m_spawnerRules.Count; ruleIdx++)
            //                {
            //                    rule = m_settings.m_spawnerRules[ruleIdx];
            //                    spawnInfo.m_fitness = rule.GetFitness(ref spawnInfo);
            //                    if (TryExecuteRule(ref rule, ref spawnInfo) == true)
            //                    {
            //                        failedSpawns = 0;
            //                        //spawnLocation.m_seedDistance = rule.GetSeedThrowRange(ref spawnInfo);
            //                        spawnLocations.Add(spawnLocation);
            //                    }
            //                    else
            //                    {
            //                        failedSpawns++;
            //                    }
            //                }
            //            }

            //            //Random spawn rule
            //            else if (m_spawnRuleSelector == GaiaConstants.SpawnerRuleSelector.Random)
            //            {
            //                rule = m_settings.m_spawnerRules[GetRandomInt(0, m_settings.m_spawnerRules.Count - 1)];
            //                spawnInfo.m_fitness = rule.GetFitness(ref spawnInfo);
            //                if (TryExecuteRule(ref rule, ref spawnInfo) == true)
            //                {
            //                    failedSpawns = 0;
            //                    //spawnLocation.m_seedDistance = rule.GetSeedThrowRange(ref spawnInfo);
            //                    spawnLocations.Add(spawnLocation);
            //                }
            //                else
            //                {
            //                    failedSpawns++;
            //                }
            //            }

            //            //Fittest spawn rule
            //            else if (m_spawnRuleSelector == GaiaConstants.SpawnerRuleSelector.Fittest)
            //            {
            //                fittestRule = null;
            //                maxFitness = 0f;
            //                for (ruleIdx = 0; ruleIdx < m_settings.m_spawnerRules.Count; ruleIdx++)
            //                {
            //                    rule = m_settings.m_spawnerRules[ruleIdx];
            //                    fitness = rule.GetFitness(ref spawnInfo);
            //                    if (fitness > maxFitness)
            //                    {
            //                        maxFitness = fitness;
            //                        fittestRule = rule;
            //                    }
            //                    else
            //                    {
            //                        //If they are approx equal then give another rule a chance as well to add interest
            //                        if (Gaia.GaiaUtils.Math_ApproximatelyEqual(fitness, maxFitness, 0.005f))
            //                        {
            //                            if (GetRandomFloat(0f, 1f) > 0.5f)
            //                            {
            //                                maxFitness = fitness;
            //                                fittestRule = rule;
            //                            }
            //                        }
            //                    }
            //                }
            //                spawnInfo.m_fitness = maxFitness;
            //                if (TryExecuteRule(ref fittestRule, ref spawnInfo) == true)
            //                {
            //                    failedSpawns = 0;
            //                    spawnLocation.m_seedDistance = fittestRule.GetSeedThrowRange(ref spawnInfo);
            //                    spawnLocations.Add(spawnLocation);
            //                }
            //                else
            //                {
            //                    failedSpawns++;
            //                }
            //            }

            //            //Weighted fittest spawn rule - this implementation will favour fittest
            //            else
            //            {
            //                fittestRule = selectedRule = null;
            //                maxFitness = selectedFitness = 0f;
            //                for (ruleIdx = 0; ruleIdx < m_settings.m_spawnerRules.Count; ruleIdx++)
            //                {
            //                    rule = m_settings.m_spawnerRules[ruleIdx];
            //                    fitness = rule.GetFitness(ref spawnInfo);
            //                    if (GetRandomFloat(0f, 1f) < fitness)
            //                    {
            //                        selectedRule = rule;
            //                        selectedFitness = fitness;
            //                    }
            //                    if (fitness > maxFitness)
            //                    {
            //                        fittestRule = rule;
            //                        maxFitness = fitness;
            //                    }
            //                }
            //                //Check to see if we randomly bombed out - if so then choose fittest
            //                if (selectedRule == null)
            //                {
            //                    selectedRule = fittestRule;
            //                    selectedFitness = maxFitness;
            //                }
            //                //We could still bomb, check for this and avoid it
            //                if (selectedRule != null)
            //                {
            //                    spawnInfo.m_fitness = selectedFitness;
            //                    if (TryExecuteRule(ref selectedRule, ref spawnInfo) == true)
            //                    {
            //                        failedSpawns = 0;
            //                        spawnLocation.m_seedDistance = selectedRule.GetSeedThrowRange(ref spawnInfo);
            //                        spawnLocations.Add(spawnLocation);
            //                    }
            //                    else
            //                    {
            //                        failedSpawns++;
            //                    }
            //                }
            //            }
            //        }

            //        //Update progress and yield periodiocally
            //        m_spawnProgress = (float)checks / (float)m_locationChecksPerInt;
            //        float newTime = Time.realtimeSinceStartup;
            //        float stepTime = newTime - currentTime;
            //        currentTime = newTime;
            //        accumulatedTime += stepTime;
            //        if (accumulatedTime > m_updateTimeAllowed)
            //        {
            //            accumulatedTime = 0f;
            //            yield return null;
            //        }

            //        //Check the instance count, exit if necessary
            //        if (!CanSpawnInstances())
            //        {
            //            break;
            //        }

            //        //Check for cancellation
            //        if (m_cancelSpawn)
            //        {
            //            break;
            //        }
            //    }
            //}
            ////Delete spawn caches
            //DeleteSpawnCaches();

            ////Perform final operations
            //PostSpawn();
            yield return null;
        }

        /// <summary>
        /// Run an area spawner iteration
        /// </summary>
        public IEnumerator RunAreaSpawnerIteration()
        {
            if (m_showDebug)
            {
                Debug.Log(string.Format("{0}: Running area iteration", gameObject.name));
            }

            int ruleIdx;
            float fitness, maxFitness, selectedFitness;
            SpawnRule rule, fittestRule, selectedRule;
            SpawnInfo spawnInfo = new SpawnInfo();
            Vector3 location = new Vector3();
            long currChecks, totalChecks;
            float xWUMin, xWUMax, yMid, zWUMin, zWUMax, jitMin, jitMax;
            float xWU, zWU;

            //Set progress
            m_spawnProgress = 0f;
            m_spawnComplete = false;

            //Time control for enumeration
            float currentTime = Time.realtimeSinceStartup;
            float accumulatedTime = 0.0f;

            //Create spawn caches
            CreateSpawnCaches();

            //Load image filter
            LoadImageMask();

            //for (int terrainID = 0; terrainID < Terrain.activeTerrains.Length; terrainID++)
            //{


            //Determine check ranges
            xWUMin = transform.position.x - m_settings.m_spawnRange + (m_locationIncrement / 2f);
            xWUMax = xWUMin + (m_settings.m_spawnRange * 2f);
            yMid = transform.position.y;
            zWUMin = transform.position.z - m_settings.m_spawnRange + (m_locationIncrement / 2f);
            zWUMax = zWUMin + (m_settings.m_spawnRange * 2f);
            jitMin = (-1f * m_maxJitteredLocationOffsetPct) * m_locationIncrement;
            jitMax = (1f * m_maxJitteredLocationOffsetPct) * m_locationIncrement;

            //Update checks
            currChecks = 0;
            totalChecks = (long)(((xWUMax - xWUMin) / m_locationIncrement) * ((zWUMax - zWUMin) / m_locationIncrement));

            //Iterate across these ranges
            for (xWU = xWUMin; xWU < xWUMax; xWU += m_locationIncrement)
            {
                for (zWU = zWUMin; zWU < zWUMax; zWU += m_locationIncrement)
                {
                    currChecks++;

                    //Set the location we want to test
                    location.x = xWU;
                    location.y = yMid;
                    location.z = zWU;

                    //Jitter it
                    if (m_spawnLocationAlgorithm == GaiaConstants.SpawnerLocation.EveryLocationJittered)
                    {
                        location.x += GetRandomFloat(jitMin, jitMax);
                        location.z += GetRandomFloat(jitMin, jitMax);
                    }

                    //Run a ray traced hit check to see what we have hit, use rules to determine fitness and select a rule to spawn
                    if (CheckLocation(location, ref spawnInfo))
                    {


                        //Now perform a rule check based on the selected algorithm

                        //All rules
                        if (m_spawnRuleSelector == GaiaConstants.SpawnerRuleSelector.All)
                        {
                            for (ruleIdx = 0; ruleIdx < m_settings.m_spawnerRules.Count; ruleIdx++)
                            {
                                rule = m_settings.m_spawnerRules[ruleIdx];
                                spawnInfo.m_fitness = rule.GetFitness(ref spawnInfo);
                                TryExecuteRule(ref rule, ref spawnInfo);
                            }
                        }

                        //Random spawn rule
                        else if (m_spawnRuleSelector == GaiaConstants.SpawnerRuleSelector.Random)
                        {
                            ruleIdx = GetRandomInt(0, m_settings.m_spawnerRules.Count - 1);
                            rule = m_settings.m_spawnerRules[ruleIdx];
                            spawnInfo.m_fitness = rule.GetFitness(ref spawnInfo);
                            TryExecuteRule(ref rule, ref spawnInfo);
                        }

                        //Fittest spawn rule
                        else if (m_spawnRuleSelector == GaiaConstants.SpawnerRuleSelector.Fittest)
                        {
                            fittestRule = null;
                            maxFitness = 0f;
                            for (ruleIdx = 0; ruleIdx < m_settings.m_spawnerRules.Count; ruleIdx++)
                            {
                                rule = m_settings.m_spawnerRules[ruleIdx];
                                fitness = rule.GetFitness(ref spawnInfo);
                                if (fitness > maxFitness)
                                {
                                    maxFitness = fitness;
                                    fittestRule = rule;
                                }
                                else
                                {
                                    //If they are approx equal then give another rule a chance as well to add interest
                                    if (Gaia.GaiaUtils.Math_ApproximatelyEqual(fitness, maxFitness, 0.005f))
                                    {
                                        if (GetRandomFloat(0f, 1f) > 0.5f)
                                        {
                                            maxFitness = fitness;
                                            fittestRule = rule;
                                        }
                                    }
                                }
                            }
                            spawnInfo.m_fitness = maxFitness;
                            TryExecuteRule(ref fittestRule, ref spawnInfo);
                        }

                        //Weighted fittest spawn rule - this implementation will favour fittest
                        else
                        {
                            fittestRule = selectedRule = null;
                            maxFitness = selectedFitness = 0f;
                            for (ruleIdx = 0; ruleIdx < m_settings.m_spawnerRules.Count; ruleIdx++)
                            {
                                rule = m_settings.m_spawnerRules[ruleIdx];
                                fitness = rule.GetFitness(ref spawnInfo);
                                if (GetRandomFloat(0f, 1f) < fitness)
                                {
                                    selectedRule = rule;
                                    selectedFitness = fitness;
                                }
                                if (fitness > maxFitness)
                                {
                                    fittestRule = rule;
                                    maxFitness = fitness;
                                }
                            }
                            //Check to see if we randomly bombed out - if so then choose fittest
                            if (selectedRule == null)
                            {
                                selectedRule = fittestRule;
                                selectedFitness = maxFitness;
                            }
                            //We could still bomb, check for this and avoid it
                            if (selectedRule != null)
                            {
                                spawnInfo.m_fitness = selectedFitness;
                                TryExecuteRule(ref selectedRule, ref spawnInfo);
                            }
                        }

                        //If it caused textures to be updated then apply them
                        if (m_textureMapsDirty)
                        {
                            List<HeightMap> txtMaps = spawnInfo.m_spawner.GetTextureMaps(spawnInfo.m_hitTerrain.GetInstanceID());
                            if (txtMaps != null)
                            {
                                for (int idx = 0; idx < spawnInfo.m_textureStrengths.Length; idx++)
                                {
                                    //if ((int)spawnInfo.m_hitLocationWU.z == 1023)
                                    //{
                                    //    Debug.Log("Woopee");
                                    //}

                                    txtMaps[idx][spawnInfo.m_hitLocationNU.z, spawnInfo.m_hitLocationNU.x] = spawnInfo.m_textureStrengths[idx];
                                }
                            }
                        }

                    }

                    //Update progress and yield periodiocally
                    m_spawnProgress = (float)currChecks / (float)totalChecks;
                    float newTime = Time.realtimeSinceStartup;
                    float stepTime = newTime - currentTime;
                    currentTime = newTime;
                    accumulatedTime += stepTime;
                    if (accumulatedTime > m_updateTimeAllowed)
                    {
                        accumulatedTime = 0f;
                        yield return null;
                    }

                    //Check the instance count, exit if necessary
                    if (!CanSpawnInstances())
                    {
                        break;
                    }

                    //Check for cancelation
                    if (m_cancelSpawn == true)
                    {
                        break;
                    }
                }
            }
            //}
            //Determine whether or not we need to delete and apply spawn caches
            DeleteSpawnCaches(true);

            //Perform final operations
            PostSpawn();
        }

        /// <summary>
        /// Ground the spawner to the terrain
        /// </summary>
        public void GroundToTerrain()
        {
            Terrain t = Gaia.TerrainHelper.GetTerrain(transform.position);
            if (t == null)
            {
                t = Terrain.activeTerrain;
            }
            if (t == null)
            {
                Debug.LogError("Could not fit to terrain - no terrain present");
                return;
            }

            Bounds b = new Bounds();
            if (TerrainHelper.GetTerrainBounds(t, ref b))
            {
                transform.position = new Vector3(transform.position.x, t.transform.position.y, transform.position.z);
            }
        }

        /// <summary>
        /// Position and fit the spawner to the terrain
        /// </summary>
        public void FitToTerrain(Terrain t = null)
        {
            if (t == null)
            {
                t = Gaia.TerrainHelper.GetTerrain(transform.position, m_settings.m_isWorldmapSpawner);
                if (t == null)
                {
                    t = Terrain.activeTerrain;
                }
                if (t == null)
                {
                    Debug.LogWarning("Could not fit to terrain - no terrain present");
                    return;
                }
            }

            Bounds b = new Bounds();
            if (TerrainHelper.GetTerrainBounds(t, ref b))
            {
                transform.position = new Vector3(b.center.x, t.transform.position.y, b.center.z);
                m_settings.m_spawnRange = b.extents.x;
            }
        }


        /// <summary>
        /// Position and fit the spawner to the terrain
        /// </summary>
        public void FitToAllTerrains()
        {
            Terrain currentTerrain = GetCurrentTerrain();

            if (currentTerrain == null)
            {
                Debug.LogError("Could not fit to terrain - no active terrain present");
                return;
            }

            BoundsDouble b = new Bounds();
            if (TerrainHelper.GetTerrainBounds(ref b))
            {
                transform.position = b.center;
                m_settings.m_spawnRange = (float)b.extents.x;
            }
        }

        /// <summary>
        /// Check if the spawner has been fit to the terrain - ignoring height
        /// </summary>
        /// <returns>True if its a match</returns>
        public bool IsFitToTerrain()
        {
            Terrain t = Gaia.TerrainHelper.GetTerrain(transform.position);
            if (t == null)
            {
                t = Terrain.activeTerrain;
            }
            if (t == null)
            {
                Debug.LogError("Could not check if fit to terrain - no terrain present");
                return false;
            }

            Bounds b = new Bounds();
            if (TerrainHelper.GetTerrainBounds(t, ref b))
            {
                if (
                    b.center.x != transform.position.x ||
                    b.center.z != transform.position.z ||
                    b.extents.x != m_settings.m_spawnRange)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Load the image mask if one was specified
        /// </summary>
        public bool LoadImageMask()
        {
            //Kill old image height map
            m_imageMaskHM = null;

            //Check mode & exit 
            if (m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.None || m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.PerlinNoise)
            {
                return false;
            }

            //Load the supplied image
            if (m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.ImageRedChannel || m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.ImageGreenChannel ||
                m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.ImageBlueChannel || m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.ImageAlphaChannel ||
                m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.ImageGreyScale)
            {
                if (m_imageMask == null)
                {
                    Debug.LogError("You requested an image mask but did not supply one. Please select mask texture.");
                    return false;
                }

                //Check the image rw
                Gaia.GaiaUtils.MakeTextureReadable(m_imageMask);

                //Make it uncompressed
                Gaia.GaiaUtils.MakeTextureUncompressed(m_imageMask);

                //Load the image
                m_imageMaskHM = new HeightMap(m_imageMask.width, m_imageMask.height);
                for (int x = 0; x < m_imageMaskHM.Width(); x++)
                {
                    for (int z = 0; z < m_imageMaskHM.Depth(); z++)
                    {
                        switch (m_areaMaskMode)
                        {
                            case GaiaConstants.ImageFitnessFilterMode.ImageGreyScale:
                                m_imageMaskHM[x, z] = m_imageMask.GetPixel(x, z).grayscale;
                                break;
                            case GaiaConstants.ImageFitnessFilterMode.ImageRedChannel:
                                m_imageMaskHM[x, z] = m_imageMask.GetPixel(x, z).r;
                                break;
                            case GaiaConstants.ImageFitnessFilterMode.ImageGreenChannel:
                                m_imageMaskHM[x, z] = m_imageMask.GetPixel(x, z).g;
                                break;
                            case GaiaConstants.ImageFitnessFilterMode.ImageBlueChannel:
                                m_imageMaskHM[x, z] = m_imageMask.GetPixel(x, z).b;
                                break;
                            case GaiaConstants.ImageFitnessFilterMode.ImageAlphaChannel:
                                m_imageMaskHM[x, z] = m_imageMask.GetPixel(x, z).a;
                                break;
                        }
                    }
                }
            }
            else
            {
                //Or get a new one
                if (Terrain.activeTerrain == null)
                {
                    Debug.LogError("You requested an terrain texture mask but there is no active terrain.");
                    return false;
                }


                Terrain t = Terrain.activeTerrain;
                var splatPrototypes = GaiaSplatPrototype.GetGaiaSplatPrototypes(t);


                switch (m_areaMaskMode)
                {
                    case GaiaConstants.ImageFitnessFilterMode.TerrainTexture0:
                        if (splatPrototypes.Length < 1)
                        {
                            Debug.LogError("You requested an terrain texture mask 0 but there is no active texture in slot 0.");
                            return false;
                        }
                        m_imageMaskHM = new HeightMap(t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight), 0);
                        break;
                    case GaiaConstants.ImageFitnessFilterMode.TerrainTexture1:
                        if (splatPrototypes.Length < 2)
                        {
                            Debug.LogError("You requested an terrain texture mask 1 but there is no active texture in slot 1.");
                            return false;
                        }
                        m_imageMaskHM = new HeightMap(t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight), 1);
                        break;
                    case GaiaConstants.ImageFitnessFilterMode.TerrainTexture2:
                        if (splatPrototypes.Length < 3)
                        {
                            Debug.LogError("You requested an terrain texture mask 2 but there is no active texture in slot 2.");
                            return false;
                        }
                        m_imageMaskHM = new HeightMap(t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight), 2);
                        break;
                    case GaiaConstants.ImageFitnessFilterMode.TerrainTexture3:
                        if (splatPrototypes.Length < 4)
                        {
                            Debug.LogError("You requested an terrain texture mask 3 but there is no active texture in slot 3.");
                            return false;
                        }
                        m_imageMaskHM = new HeightMap(t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight), 3);
                        break;
                    case GaiaConstants.ImageFitnessFilterMode.TerrainTexture4:
                        if (splatPrototypes.Length < 5)
                        {
                            Debug.LogError("You requested an terrain texture mask 4 but there is no active texture in slot 4.");
                            return false;
                        }
                        m_imageMaskHM = new HeightMap(t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight), 4);
                        break;
                    case GaiaConstants.ImageFitnessFilterMode.TerrainTexture5:
                        if (splatPrototypes.Length < 6)
                        {
                            Debug.LogError("You requested an terrain texture mask 5 but there is no active texture in slot 5.");
                            return false;
                        }
                        m_imageMaskHM = new HeightMap(t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight), 5);
                        break;
                    case GaiaConstants.ImageFitnessFilterMode.TerrainTexture6:
                        if (splatPrototypes.Length < 7)
                        {
                            Debug.LogError("You requested an terrain texture mask 6 but there is no active texture in slot 6.");
                            return false;
                        }
                        m_imageMaskHM = new HeightMap(t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight), 6);
                        break;
                    case GaiaConstants.ImageFitnessFilterMode.TerrainTexture7:
                        if (splatPrototypes.Length < 8)
                        {
                            Debug.LogError("You requested an terrain texture mask 7 but there is no active texture in slot 7.");
                            return false;
                        }
                        m_imageMaskHM = new HeightMap(t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight), 7);
                        break;
                }

                //It came from terrain so flip it
                m_imageMaskHM.Flip();
            }

            //Because images are noisy, smooth it
            if (m_imageMaskSmoothIterations > 0)
            {
                m_imageMaskHM.Smooth(m_imageMaskSmoothIterations);
            }

            //Flip it
            if (m_imageMaskFlip == true)
            {
                m_imageMaskHM.Flip();
            }

            //Normalise it if necessary
            if (m_imageMaskNormalise == true)
            {
                m_imageMaskHM.Normalise();
            }

            //Invert it if necessessary
            if (m_imageMaskInvert == true)
            {
                m_imageMaskHM.Invert();
            }

            return true;
        }

        /// <summary>
        /// Startst the spawner, either in a local area or across all terrains ("world spawn")
        /// </summary>
        /// <param name="allTerrains">Whether the spawner should spawn across all terrains in the scene.</param>
        public void Spawn(bool allTerrains)
        {
            m_spawnComplete = false;
            BoundsDouble spawnArea = new BoundsDouble();

            if (allTerrains)
            {
                TerrainHelper.GetTerrainBounds(ref spawnArea);
            }
            else
            {
                spawnArea.center = transform.position;
                spawnArea.size = new Vector3(m_settings.m_spawnRange * 2f, m_settings.m_spawnRange * 2f, m_settings.m_spawnRange * 2f);
            }

            try
            {
                GenerateNewRandomSeed();
                SpawnOperationSettings soSettings = ScriptableObject.CreateInstance<SpawnOperationSettings>();
                soSettings.m_spawnerSettingsList = new List<SpawnerSettings>() { m_settings };
                soSettings.m_spawnArea = spawnArea;
                soSettings.m_isWorldMapSpawner = m_settings.m_isWorldmapSpawner;
                GaiaSessionManager.Spawn(soSettings, true, new List<Spawner>() { this });
                // m_updateCoroutine = AreaSpawn(new List<Spawner>() { this }, spawnArea);
                //StartEditorUpdates();
            }
            catch (Exception ex)
            {
                Debug.LogError("Spawner " + this.name + " failed with Exception: " + ex.Message + "\n\n" + "Stack trace: \n\n" + ex.StackTrace);
                ProgressBar.Clear(ProgressBarPriority.Spawning);
            }
            m_spawnComplete = true;
        }

        /// <summary>
        /// Generates a new random seed for the spawner (if generation of a new seed is activated)
        /// </summary>
        public void GenerateNewRandomSeed()
        {
            if (m_settings.m_generateRandomSeed)
            {
                m_settings.m_randomSeed = UnityEngine.Random.Range(0, int.MaxValue);
            }
        }

        /// <summary>
        /// Create spawn caches
        /// </summary>
        /// <param name="checkResources">Base on resources or base on rules, takes active state into account</param>
        public void CreateSpawnCaches()
        {
            //Determine whether or not we need to cache updates, in which case we needs to get the relevant caches
            int idx;
            m_cacheTextures = false;
            m_textureMapsDirty = false;
            for (idx = 0; idx < m_settings.m_spawnerRules.Count; idx++)
            {
                if (m_settings.m_spawnerRules[idx].CacheTextures(this))
                {
                    foreach (Terrain t in Terrain.activeTerrains)
                    {
                        CacheTextureMapsFromTerrain(t.GetInstanceID());
                    }
                    m_cacheTextures = true;
                    break;
                }
            }

            m_cacheDetails = false;
            for (idx = 0; idx < m_settings.m_spawnerRules.Count; idx++)
            {
                if (m_settings.m_spawnerRules[idx].CacheDetails(this))
                {
                    foreach (Terrain t in Terrain.activeTerrains)
                    {
                        CacheDetailMapsFromTerrain(t.GetInstanceID());
                    }
                    m_cacheDetails = true;
                    break;
                }
            }

            CacheTreesFromTerrain();

            m_cacheTags = false;
            List<string> tagList = new List<string>();
            for (idx = 0; idx < m_settings.m_spawnerRules.Count; idx++)
            {
                m_settings.m_spawnerRules[idx].AddProximityTags(this, ref tagList);
            }
            if (tagList.Count > 0)
            {
                CacheTaggedGameObjectsFromScene(tagList);
                m_cacheTags = true;
            }

            m_cacheHeightMaps = false;
            for (idx = 0; idx < m_settings.m_spawnerRules.Count; idx++)
            {
                if (m_settings.m_spawnerRules[idx].CacheHeightMaps(this))
                {
                    CacheHeightMapFromTerrain(Terrain.activeTerrain.GetInstanceID());
                    m_cacheHeightMaps = true;
                    break;
                }
            }

            /*
            m_cacheStamps = false;
            List<string> stampList = new List<string>();
            for (idx = 0; idx < m_spawnerRules.Count; idx++)
            {
                m_spawnerRules[idx].AddStamps(this, ref stampList);
            }
            if (stampList.Count > 0)
            {
                CacheStamps(stampList);
                m_cacheStamps = true;
            } */
        }

        /// <summary>
        /// Create spawn cache fore specific resources
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="resourceIdx"></param>
        public void CreateSpawnCaches(Gaia.GaiaConstants.SpawnerResourceType resourceType, int resourceIdx)
        {
            m_cacheTextures = false;
            m_textureMapsDirty = false;
            m_cacheDetails = false;
            m_cacheTags = false;

            switch (resourceType)
            {
                case GaiaConstants.SpawnerResourceType.TerrainTexture:
                    {
                        //Check indexes
                        if (resourceIdx >= m_settings.m_resources.m_texturePrototypes.Length)
                        {
                            break;
                        }

                        //If we are working with textures, then always cache the texture
                        foreach (Terrain t in Terrain.activeTerrains)
                        {
                            CacheTextureMapsFromTerrain(t.GetInstanceID());
                        }
                        m_cacheTextures = true;

                        //Check for proximity tags
                        List<string> tagList = new List<string>();
                        m_settings.m_resources.m_texturePrototypes[resourceIdx].AddTags(ref tagList);
                        if (tagList.Count > 0)
                        {
                            CacheTaggedGameObjectsFromScene(tagList);
                            m_cacheTags = true;
                        }

                        break;
                    }
                case GaiaConstants.SpawnerResourceType.TerrainDetail:
                    {
                        //Check indexes
                        if (resourceIdx >= m_settings.m_resources.m_detailPrototypes.Length)
                        {
                            break;
                        }

                        //If we are working with details, always cache details
                        foreach (Terrain t in Terrain.activeTerrains)
                        {
                            CacheDetailMapsFromTerrain(t.GetInstanceID());
                        }
                        m_cacheDetails = true;

                        //Check for textures
                        if (m_settings.m_resources.m_detailPrototypes[resourceIdx].ChecksTextures())
                        {
                            foreach (Terrain t in Terrain.activeTerrains)
                            {
                                CacheTextureMapsFromTerrain(t.GetInstanceID());
                            }
                            m_cacheTextures = true;
                        }

                        //Check for proximity tags
                        List<string> tagList = new List<string>();
                        m_settings.m_resources.m_detailPrototypes[resourceIdx].AddTags(ref tagList);
                        if (tagList.Count > 0)
                        {
                            CacheTaggedGameObjectsFromScene(tagList);
                            m_cacheTags = true;
                        }

                        break;
                    }
                case GaiaConstants.SpawnerResourceType.TerrainTree:
                    {
                        //Check indexes
                        if (resourceIdx >= m_settings.m_resources.m_treePrototypes.Length)
                        {
                            break;
                        }

                        //Cache textures
                        if (m_settings.m_resources.m_treePrototypes[resourceIdx].ChecksTextures())
                        {
                            foreach (Terrain t in Terrain.activeTerrains)
                            {
                                CacheTextureMapsFromTerrain(t.GetInstanceID());
                            }
                            m_cacheTextures = true;
                        }

                        //Cache trees
                        CacheTreesFromTerrain();

                        //Cache proximity tags
                        List<string> tagList = new List<string>();
                        m_settings.m_resources.m_treePrototypes[resourceIdx].AddTags(ref tagList);
                        if (tagList.Count > 0)
                        {
                            CacheTaggedGameObjectsFromScene(tagList);
                            m_cacheTags = true;
                        }

                        break;
                    }
                case GaiaConstants.SpawnerResourceType.GameObject:
                    {
                        //Check indexes
                        if (resourceIdx >= m_settings.m_resources.m_gameObjectPrototypes.Length)
                        {
                            break;
                        }

                        //Check for textures
                        if (m_settings.m_resources.m_gameObjectPrototypes[resourceIdx].ChecksTextures())
                        {
                            foreach (Terrain t in Terrain.activeTerrains)
                            {
                                CacheTextureMapsFromTerrain(t.GetInstanceID());
                            }
                            m_cacheTextures = true;
                        }

                        //Check for proximity tags
                        List<string> tagList = new List<string>();
                        m_settings.m_resources.m_gameObjectPrototypes[resourceIdx].AddTags(ref tagList);
                        if (tagList.Count > 0)
                        {
                            CacheTaggedGameObjectsFromScene(tagList);
                            m_cacheTags = true;
                        }

                        break;
                    }

                    /*
                default:
                    {
                        //Check indexes
                        if (resourceIdx >= m_resources.m_stampPrototypes.Length)
                        {
                            break;
                        }

                        //Check for textures
                        if (m_resources.m_stampPrototypes[resourceIdx].ChecksTextures())
                        {
                            CacheTextureMapsFromTerrain(Terrain.activeTerrain.GetInstanceID());
                            m_cacheTextures = true;
                        }

                        //Check for proximity tags
                        List<string> tagList = new List<string>();
                        m_resources.m_gameObjectPrototypes[resourceIdx].AddTags(ref tagList);
                        if (tagList.Count > 0)
                        {
                            CacheTaggedGameObjectsFromScene(tagList);
                            m_cacheTags = true;
                        }

                        //We are influencing terrain - so we always cache terrain
                        CacheHeightMapFromTerrain(Terrain.activeTerrain.GetInstanceID());
                        m_cacheHeightMaps = true;

                        break;
                    }
                     */
            }
        }


        /// <summary>
        /// Destroy spawn caches
        /// </summary>
        /// <param name="flush">Fluch changes back to the environment</param>
        public void DeleteSpawnCaches(bool flushDirty = false)
        {
            //Determine whether or not we need to apply cache updates
            if (m_cacheTextures)
            {
                if (flushDirty && m_textureMapsDirty && m_cancelSpawn != true)
                {
                    m_textureMapsDirty = false;
                    foreach (Terrain t in Terrain.activeTerrains)
                    {
                        SaveTextureMapsToTerrain(t.GetInstanceID());
                    }
                }
                DeleteTextureMapCache();
                m_cacheTextures = false;
            }

            if (m_cacheDetails)
            {
                if (m_cancelSpawn != true)
                {
                    foreach (Terrain t in Terrain.activeTerrains)
                    {
                        SaveDetailMapsToTerrain(t.GetInstanceID());
                    }
                }
                DeleteDetailMapCache();
                m_cacheDetails = false;
            }

            if (m_cacheTags)
            {
                DeleteTagCache();
                m_cacheTags = false;
            }

            if (m_cacheHeightMaps)
            {
                if (flushDirty && m_heightMapDirty && m_cancelSpawn != true)
                {
                    m_heightMapDirty = false;
                    SaveHeightMapToTerrain(Terrain.activeTerrain.GetInstanceID());
                }
                DeleteHeightMapCache();
                m_cacheHeightMaps = false;
            }
        }

        /// <summary>
        /// Attempt to execute a rule taking fitness, failure rate and instances into account
        /// </summary>
        /// <param name="rule">The rule to execute</param>
        /// <param name="spawnInfo">The related spawninfo</param>
        public bool TryExecuteRule(ref SpawnRule rule, ref SpawnInfo spawnInfo)
        {
            //Check null
            if (rule != null)
            {
                //Check instances
                if (rule.m_ignoreMaxInstances || (rule.m_activeInstanceCnt < rule.m_maxInstances))
                {
                    //Update fitness based on distance evaluation
                    spawnInfo.m_fitness *= m_spawnFitnessAttenuator.Evaluate(Mathf.Clamp01(spawnInfo.m_hitDistanceWU / m_settings.m_spawnRange));

                    //Udpate fitness based on area mask 
                    if (m_areaMaskMode != GaiaConstants.ImageFitnessFilterMode.None)
                    {
                        if (m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.PerlinNoise ||
                            m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.BillowNoise ||
                            m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.RidgedNoise)
                        {
                            if (!m_noiseInvert)
                            {
                                spawnInfo.m_fitness *= m_noiseGenerator.GetNormalisedValue(100000f + (spawnInfo.m_hitLocationWU.x * (1f / m_noiseZoom)), 100000f + (spawnInfo.m_hitLocationWU.z * (1f / m_noiseZoom)));
                            }
                            else
                            {
                                spawnInfo.m_fitness *= (1f - m_noiseGenerator.GetNormalisedValue(100000f + (spawnInfo.m_hitLocationWU.x * (1f / m_noiseZoom)), 100000f + (spawnInfo.m_hitLocationWU.z * (1f / m_noiseZoom))));
                            }
                        }
                        else
                        {
                            if (m_imageMaskHM.HasData())
                            {
                                float x = (spawnInfo.m_hitLocationWU.x - (transform.position.x - m_settings.m_spawnRange)) / (m_settings.m_spawnRange * 2f);
                                float z = (spawnInfo.m_hitLocationWU.z - (transform.position.z - m_settings.m_spawnRange)) / (m_settings.m_spawnRange * 2f);
                                spawnInfo.m_fitness *= m_imageMaskHM[x, z];
                            }
                        }
                    }

                    //Check fitness
                    if (spawnInfo.m_fitness > rule.m_minRequiredFitness)
                    {
                        //Only spawn if we pass a random failure check
                        if (GetRandomFloat(0f, 1f) > rule.m_failureRate)
                        {
                            rule.Spawn(ref spawnInfo);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// This is a fairly expensive raycast based location check that is capable of detecting things like tree collider hits on the terrain.
        /// It will return the name and height of the thing that was hit, plus some underlying terrain information. In the scenario of terrain tree
        /// hits you can comparing height of the rtaycast hit against the height of the terrain to detect this.
        /// It will return true plus details if something is hit, otherwise false.
        /// </summary>
        /// <param name="locationWU">The location we are checking in world units</param>
        /// <param name="spawnInfo">The information we gather about this location</param>
        /// <returns>True if we hit something, false otherwise</returns>
        public bool CheckLocation(Vector3 locationWU, ref SpawnInfo spawnInfo)
        {
            //Some initialisation
            spawnInfo.m_spawner = this;
            spawnInfo.m_outOfBounds = true;
            spawnInfo.m_wasVirginTerrain = false;
            spawnInfo.m_spawnRotationY = 0f;
            spawnInfo.m_hitDistanceWU = Vector3.Distance(transform.position, locationWU);
            spawnInfo.m_hitLocationWU = locationWU;
            spawnInfo.m_hitNormal = Vector3.zero;
            spawnInfo.m_hitObject = null;
            spawnInfo.m_hitTerrain = null;
            spawnInfo.m_terrainNormalWU = Vector3.one;
            spawnInfo.m_terrainHeightWU = 0f;
            spawnInfo.m_terrainSlopeWU = 0f;
            spawnInfo.m_areaHitSlopeWU = 0f;
            spawnInfo.m_areaMinSlopeWU = 0f;
            spawnInfo.m_areaAvgSlopeWU = 0f;
            spawnInfo.m_areaMaxSlopeWU = 0f;

            //Make sure we are above it
            locationWU.y = m_terrainHeight + 1000f;

            //Run a ray traced hit check to see what we have hit - if we dont get a hit then we are off terrain and will ignore
            if (Physics.Raycast(locationWU, Vector3.down, out m_checkHitInfo, Mathf.Infinity, m_spawnCollisionLayers))
            {
                //If its a grass spawner, and we got a sphere collider, try again so that we ignore the sphere collider
                if (spawnInfo.m_spawner.IsDetailSpawner())
                {
                    if ((m_checkHitInfo.collider is SphereCollider || m_checkHitInfo.collider is CapsuleCollider) && m_checkHitInfo.collider.name == "_GaiaCollider_Grass")
                    {
                        //Drop it slightly and run it again
                        locationWU.y = m_checkHitInfo.point.y - 0.01f;

                        //Run the raycast again - it should hit something
                        if (!Physics.Raycast(locationWU, Vector3.down, out m_checkHitInfo, Mathf.Infinity, m_spawnCollisionLayers))
                        {
                            return false;
                        }
                    }
                }


                //Update spawnInfo
                spawnInfo.m_hitLocationWU = m_checkHitInfo.point;
                spawnInfo.m_hitDistanceWU = Vector3.Distance(transform.position, spawnInfo.m_hitLocationWU);
                spawnInfo.m_hitNormal = m_checkHitInfo.normal;
                spawnInfo.m_hitObject = m_checkHitInfo.transform;

                //Check distance - bomb out if out of range
                if (m_spawnerShape == GaiaConstants.SpawnerShape.Box)
                {
                    if (!m_spawnerBounds.Contains(spawnInfo.m_hitLocationWU))
                    {
                        return false;
                    }
                }
                else
                {
                    if (spawnInfo.m_hitDistanceWU > m_settings.m_spawnRange)
                    {
                        return false;
                    }
                }
                spawnInfo.m_outOfBounds = false;

                //Gather some terrain info at this location
                Terrain terrain;
                if (m_checkHitInfo.collider is TerrainCollider)
                {
                    terrain = m_checkHitInfo.transform.GetComponent<Terrain>();
                    spawnInfo.m_wasVirginTerrain = true; //It might be virgin terrain
                }
                else
                {
                    terrain = Gaia.TerrainHelper.GetTerrain(m_checkHitInfo.point);
                }

                if (terrain != null)
                {
                    spawnInfo.m_hitTerrain = terrain;
                    spawnInfo.m_terrainHeightWU = terrain.SampleHeight(m_checkHitInfo.point);
                    Vector3 terrainLocalPos = terrain.transform.InverseTransformPoint(m_checkHitInfo.point);
                    Vector3 normalizedPos = new Vector3(Mathf.InverseLerp(0.0f, terrain.terrainData.size.x, terrainLocalPos.x),
                                                        Mathf.InverseLerp(0.0f, terrain.terrainData.size.y, terrainLocalPos.y),
                                                        Mathf.InverseLerp(0.0f, terrain.terrainData.size.z, terrainLocalPos.z));
                    spawnInfo.m_hitLocationNU = normalizedPos;
                    spawnInfo.m_terrainSlopeWU = terrain.terrainData.GetSteepness(normalizedPos.x, normalizedPos.z);
                    spawnInfo.m_areaHitSlopeWU = spawnInfo.m_areaMinSlopeWU = spawnInfo.m_areaAvgSlopeWU = spawnInfo.m_areaMaxSlopeWU = spawnInfo.m_terrainSlopeWU;
                    spawnInfo.m_terrainNormalWU = terrain.terrainData.GetInterpolatedNormal(normalizedPos.x, normalizedPos.z);

                    //Check for virgin terrain now that we know actual terrain height - difference will be tree colliders
                    if (spawnInfo.m_wasVirginTerrain == true)
                    {
                        //Use the tree manager to do hits on trees
                        if (spawnInfo.m_spawner.m_treeCache.Count(spawnInfo.m_hitLocationWU, 0.5f) > 0)
                        {
                            spawnInfo.m_wasVirginTerrain = false;
                        }
                    }

                    //Set up the texture layer array in spawn info
                    spawnInfo.m_textureStrengths = new float[spawnInfo.m_hitTerrain.terrainData.alphamapLayers];

                    //Grab the textures
                    if (m_textureMapCache != null && m_textureMapCache.Count > 0)
                    {
                        List<HeightMap> hms = m_textureMapCache[terrain.GetInstanceID()];
                        for (int i = 0; i < spawnInfo.m_textureStrengths.Length; i++)
                        {
                            spawnInfo.m_textureStrengths[i] = hms[i][normalizedPos.z, normalizedPos.x];
                        }
                    }
                    else
                    {
                        float[,,] hms = terrain.terrainData.GetAlphamaps((int)(normalizedPos.x * (float)(terrain.terrainData.alphamapWidth - 1)), (int)(normalizedPos.z * (float)(terrain.terrainData.alphamapHeight - 1)), 1, 1);
                        for (int i = 0; i < spawnInfo.m_textureStrengths.Length; i++)
                        {
                            spawnInfo.m_textureStrengths[i] = hms[0, 0, i];
                        }
                    }
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// This will do a bounded location check in order to calculate bounded slopes and checkd for bounded collisions
        /// </summary>
        /// <param name="spawnInfo"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public bool CheckLocationBounds(ref SpawnInfo spawnInfo, float distance)
        {
            //Initialise
            spawnInfo.m_areaHitSlopeWU = spawnInfo.m_areaMinSlopeWU = spawnInfo.m_areaAvgSlopeWU = spawnInfo.m_areaMaxSlopeWU = spawnInfo.m_terrainSlopeWU;
            if (spawnInfo.m_areaHitsWU == null)
            {
                spawnInfo.m_areaHitsWU = new Vector3[4];
            }
            spawnInfo.m_areaHitsWU[0] = new Vector3(spawnInfo.m_hitLocationWU.x + distance, spawnInfo.m_hitLocationWU.y + 3000f, spawnInfo.m_hitLocationWU.z);
            spawnInfo.m_areaHitsWU[1] = new Vector3(spawnInfo.m_hitLocationWU.x - distance, spawnInfo.m_hitLocationWU.y + 3000f, spawnInfo.m_hitLocationWU.z);
            spawnInfo.m_areaHitsWU[2] = new Vector3(spawnInfo.m_hitLocationWU.x, spawnInfo.m_hitLocationWU.y + 3000f, spawnInfo.m_hitLocationWU.z + distance);
            spawnInfo.m_areaHitsWU[3] = new Vector3(spawnInfo.m_hitLocationWU.x, spawnInfo.m_hitLocationWU.y + 3000f, spawnInfo.m_hitLocationWU.z - distance);

            //Run ray traced hits to check the lay of the land - if we dont get a hit then we are off terrain and will fail
            RaycastHit hit;

            //First check the main volume under the original position for non terrain related hits
            Vector3 extents = new Vector3(distance, 0.1f, distance);
            if (!Physics.BoxCast(new Vector3(spawnInfo.m_hitLocationWU.x, spawnInfo.m_hitLocationWU.y + 3000f, spawnInfo.m_hitLocationWU.z), extents, Vector3.down, out hit, Quaternion.identity, Mathf.Infinity, m_spawnCollisionLayers))
            //if (!Physics.SphereCast(new Vector3(spawnInfo.m_hitLocationWU.x, spawnInfo.m_hitLocationWU.y + 3000f, spawnInfo.m_hitLocationWU.z), distance, Vector3.down, out hit, Mathf.Infinity, m_spawnCollisionLayers))
            {
                return false;
            }

            //Test virginity
            if (spawnInfo.m_wasVirginTerrain == true)
            {
                if (hit.collider is TerrainCollider)
                {
                    //Use the tree manager to do hits on trees
                    if (spawnInfo.m_spawner.m_treeCache.Count(hit.point, 0.5f) > 0)
                    {
                        spawnInfo.m_wasVirginTerrain = false;
                    }
                }
                else
                {
                    spawnInfo.m_wasVirginTerrain = false;
                }
            }

            //Now test the first corner
            if (!Physics.Raycast(spawnInfo.m_areaHitsWU[0], Vector3.down, out hit, Mathf.Infinity, m_spawnCollisionLayers))
            {
                return false;
            }

            //Update hit location
            spawnInfo.m_areaHitsWU[0] = hit.point;

            //Update slope calculations
            Terrain terrain = hit.transform.GetComponent<Terrain>();
            if (terrain == null)
            {
                terrain = Gaia.TerrainHelper.GetTerrain(hit.point);
            }
            Vector3 localPos = Vector3.zero;
            Vector3 normPos = Vector3.zero;
            //float terrainHeight = 0f;
            float terrainSlope = 0f;

            if (terrain != null)
            {
                //terrainHeight = terrain.SampleHeight(hit.point);
                localPos = terrain.transform.InverseTransformPoint(hit.point);
                normPos = new Vector3(Mathf.InverseLerp(0.0f, terrain.terrainData.size.x, localPos.x),
                                                    Mathf.InverseLerp(0.0f, terrain.terrainData.size.y, localPos.y),
                                                    Mathf.InverseLerp(0.0f, terrain.terrainData.size.z, localPos.z));
                terrainSlope = terrain.terrainData.GetSteepness(normPos.x, normPos.z);
                spawnInfo.m_areaAvgSlopeWU += terrainSlope;
                if (terrainSlope > spawnInfo.m_areaMaxSlopeWU)
                {
                    spawnInfo.m_areaMaxSlopeWU = terrainSlope;
                }
                if (terrainSlope < spawnInfo.m_areaMinSlopeWU)
                {
                    spawnInfo.m_areaMinSlopeWU = terrainSlope;
                }

                //Check for virginity
                if (spawnInfo.m_wasVirginTerrain == true)
                {
                    if (hit.collider is TerrainCollider)
                    {
                        if (spawnInfo.m_spawner.m_treeCache.Count(hit.point, 0.5f) > 0)
                        {
                            spawnInfo.m_wasVirginTerrain = false;
                        }
                    }
                    else
                    {
                        spawnInfo.m_wasVirginTerrain = false;
                    }
                }
            }

            //Now test the next corner
            if (!Physics.Raycast(spawnInfo.m_areaHitsWU[1], Vector3.down, out hit, Mathf.Infinity, m_spawnCollisionLayers))
            {
                return false;
            }

            //Update hit location
            spawnInfo.m_areaHitsWU[1] = hit.point;

            //Update slope calculations
            terrain = hit.transform.GetComponent<Terrain>();
            if (terrain == null)
            {
                terrain = Gaia.TerrainHelper.GetTerrain(hit.point);
            }
            if (terrain != null)
            {
                //terrainHeight = terrain.SampleHeight(hit.point);
                localPos = terrain.transform.InverseTransformPoint(hit.point);
                normPos = new Vector3(Mathf.InverseLerp(0.0f, terrain.terrainData.size.x, localPos.x),
                                                    Mathf.InverseLerp(0.0f, terrain.terrainData.size.y, localPos.y),
                                                    Mathf.InverseLerp(0.0f, terrain.terrainData.size.z, localPos.z));
                terrainSlope = terrain.terrainData.GetSteepness(normPos.x, normPos.z);
                spawnInfo.m_areaAvgSlopeWU += terrainSlope;
                if (terrainSlope > spawnInfo.m_areaMaxSlopeWU)
                {
                    spawnInfo.m_areaMaxSlopeWU = terrainSlope;
                }
                if (terrainSlope < spawnInfo.m_areaMinSlopeWU)
                {
                    spawnInfo.m_areaMinSlopeWU = terrainSlope;
                }

                //Check for virginity
                if (spawnInfo.m_wasVirginTerrain == true)
                {
                    if (hit.collider is TerrainCollider)
                    {
                        if (spawnInfo.m_spawner.m_treeCache.Count(hit.point, 0.5f) > 0)
                        {
                            spawnInfo.m_wasVirginTerrain = false;
                        }
                    }
                    else
                    {
                        spawnInfo.m_wasVirginTerrain = false;
                    }
                }
            }

            //Now test the next corner
            if (!Physics.Raycast(spawnInfo.m_areaHitsWU[2], Vector3.down, out hit, Mathf.Infinity, m_spawnCollisionLayers))
            {
                return false;
            }

            //Update hit location
            spawnInfo.m_areaHitsWU[2] = hit.point;

            //Update slope calculations
            terrain = hit.transform.GetComponent<Terrain>();
            if (terrain == null)
            {
                terrain = Gaia.TerrainHelper.GetTerrain(hit.point);
            }
            if (terrain != null)
            {
                //terrainHeight = terrain.SampleHeight(hit.point);
                localPos = terrain.transform.InverseTransformPoint(hit.point);
                normPos = new Vector3(Mathf.InverseLerp(0.0f, terrain.terrainData.size.x, localPos.x),
                                                    Mathf.InverseLerp(0.0f, terrain.terrainData.size.y, localPos.y),
                                                    Mathf.InverseLerp(0.0f, terrain.terrainData.size.z, localPos.z));
                terrainSlope = terrain.terrainData.GetSteepness(normPos.x, normPos.z);
                spawnInfo.m_areaAvgSlopeWU += terrainSlope;
                if (terrainSlope > spawnInfo.m_areaMaxSlopeWU)
                {
                    spawnInfo.m_areaMaxSlopeWU = terrainSlope;
                }
                if (terrainSlope < spawnInfo.m_areaMinSlopeWU)
                {
                    spawnInfo.m_areaMinSlopeWU = terrainSlope;
                }

                //Check for virginity
                if (spawnInfo.m_wasVirginTerrain == true)
                {
                    if (hit.collider is TerrainCollider)
                    {
                        if (spawnInfo.m_spawner.m_treeCache.Count(hit.point, 0.5f) > 0)
                        {
                            spawnInfo.m_wasVirginTerrain = false;
                        }
                    }
                    else
                    {
                        spawnInfo.m_wasVirginTerrain = false;
                    }
                }
            }

            //Now test the next corner
            if (!Physics.Raycast(spawnInfo.m_areaHitsWU[3], Vector3.down, out hit, Mathf.Infinity, m_spawnCollisionLayers))
            {
                return false;
            }

            //Update hit location
            spawnInfo.m_areaHitsWU[3] = hit.point;

            //Update slope calculations
            terrain = hit.transform.GetComponent<Terrain>();
            if (terrain == null)
            {
                terrain = Gaia.TerrainHelper.GetTerrain(hit.point);
            }
            if (terrain != null)
            {
                //terrainHeight = terrain.SampleHeight(hit.point);
                localPos = terrain.transform.InverseTransformPoint(hit.point);
                normPos = new Vector3(Mathf.InverseLerp(0.0f, terrain.terrainData.size.x, localPos.x),
                                                    Mathf.InverseLerp(0.0f, terrain.terrainData.size.y, localPos.y),
                                                    Mathf.InverseLerp(0.0f, terrain.terrainData.size.z, localPos.z));
                terrainSlope = terrain.terrainData.GetSteepness(normPos.x, normPos.z);
                spawnInfo.m_areaAvgSlopeWU += terrainSlope;
                if (terrainSlope > spawnInfo.m_areaMaxSlopeWU)
                {
                    spawnInfo.m_areaMaxSlopeWU = terrainSlope;
                }
                if (terrainSlope < spawnInfo.m_areaMinSlopeWU)
                {
                    spawnInfo.m_areaMinSlopeWU = terrainSlope;
                }

                //Check for virginity
                if (spawnInfo.m_wasVirginTerrain == true)
                {
                    if (hit.collider is TerrainCollider)
                    {
                        if (spawnInfo.m_spawner.m_treeCache.Count(hit.point, 0.5f) > 0)
                        {
                            spawnInfo.m_wasVirginTerrain = false;
                        }
                    }
                    else
                    {
                        spawnInfo.m_wasVirginTerrain = false;
                    }
                }
            }

            //Now update the slopes and spawninfo
            spawnInfo.m_areaAvgSlopeWU = spawnInfo.m_areaAvgSlopeWU / 5f;
            float dx = spawnInfo.m_areaHitsWU[0].y - spawnInfo.m_areaHitsWU[1].y;
            float dz = spawnInfo.m_areaHitsWU[2].y - spawnInfo.m_areaHitsWU[3].y;
            spawnInfo.m_areaHitSlopeWU = Gaia.GaiaUtils.Math_Clamp(0f, 90f, (float)(Math.Sqrt((dx * dx) + (dz * dz))));

            return true;
        }


        //        public bool CheckForMissingResources(bool allTerrains, bool autoAssignResources = false)
        //        {
        //            //Get affected terrains first - we don't want to check for missing resources in a terrain which won't even be affected
        //            Terrain[] affectedTerrains;

        //            //Are we spawning across all terrains? Then all active are affected as well.
        //            if (allTerrains)
        //            {
        //                affectedTerrains = Terrain.activeTerrains;
        //            }
        //            else
        //            {
        //                //Local spawn only - simulate operation to get all terrains affected by the spawn range
        //                Terrain currentTerrain = GetCurrentTerrain();
        //                GaiaMultiTerrainOperation operation = new GaiaMultiTerrainOperation(currentTerrain, transform, m_settings.m_spawnRange * 2f);
        //                operation.m_isWorldMapOperation = m_settings.m_isWorldmapSpawner;
        //                operation.GetAffectedTerrainsOnly();
        //                affectedTerrains = operation.affectedTerrainPixels.Where(x => x.Key.operationType == MultiTerrainOperationType.AffectedTerrains).Select(y => y.Key.terrain).ToArray();
        //                operation.CloseOperation();
        //            }

        //            AssociateAssets();
        //            int[] missingResources = GetMissingResources(affectedTerrains);
        //            if (missingResources.GetLength(0) > 0)
        //            {
        //                SpawnRule missingRule;
        //                StringBuilder sb = new StringBuilder();
        //                for (int idx = 0; idx < missingResources.GetLength(0); idx++)
        //                {
        //                    missingRule = m_settings.m_spawnerRules[missingResources[idx]];
        //                    if (idx != 0)
        //                    {
        //                        sb.Append("\r\n");
        //                    }
        //                    sb.Append(missingRule.m_name);
        //                }
        //#if UNITY_EDITOR
        //                if (autoAssignResources || EditorUtility.DisplayDialog("WARNING!", "The following resources are missing from one or more terrains! \r\n\r\n" + sb.ToString() + "\r\n\r\n Do you want to add them now?", "Add Resources", "Cancel"))
        //                {
        //                    AddResourcesToTerrain(missingResources, affectedTerrains);
        //                }
        //                else
        //                {
        //                    return true;
        //                }
        //#endif
        //            }

        //            return false;

        //        }




        /// <summary>
        /// Update statistics counters
        /// </summary>
        public void UpdateCounters()
        {
            //m_totalRuleCnt = 0;
            //m_activeRuleCnt = 0;
            //m_inactiveRuleCnt = 0;
            //m_maxInstanceCnt = 0;
            //m_activeInstanceCnt = 0;
            //m_inactiveInstanceCnt = 0;
            //m_totalInstanceCnt = 0;

            //foreach (SpawnRule rule in m_settings.m_spawnerRules)
            //{
            //    m_totalRuleCnt++;
            //    if (rule.m_isActive)
            //    {
            //        m_activeRuleCnt++;
            //        m_maxInstanceCnt += rule.m_maxInstances;
            //        m_activeInstanceCnt += rule.m_activeInstanceCnt;
            //        m_inactiveInstanceCnt += rule.m_inactiveInstanceCnt;
            //        m_totalInstanceCnt += (rule.m_activeInstanceCnt + rule.m_inactiveInstanceCnt);
            //    }
            //    else
            //    {
            //        m_inactiveRuleCnt++;
            //    }
            //}
        }

        /// <summary>
        /// Draw gizmos
        /// </summary>
        void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            if (m_showGizmos && Selection.activeObject == gameObject)
            {
                if (m_showBoundingBox)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(transform.position, new Vector3(m_settings.m_spawnRange * 2f, m_settings.m_spawnRange * 2f, m_settings.m_spawnRange * 2f));
                }

                //Water
                if (m_settings.m_resources != null && m_showSeaLevelPlane)
                {
                    BoundsDouble bounds = new BoundsDouble();
                    if (m_settings.m_isWorldmapSpawner)
                    {
                        if (m_worldMapTerrain == null)
                        {
                            m_worldMapTerrain = TerrainHelper.GetWorldMapTerrain();
                        }

                        bounds.center = m_worldMapTerrain.terrainData.bounds.center;
                        bounds.extents = m_worldMapTerrain.terrainData.bounds.extents;
                        //bounds need to be in world space + use the shifted origin
                        bounds.center = transform.position;
                    }
                    else
                    {
                        TerrainHelper.GetTerrainBounds(ref bounds);
                    }
                    bounds.center = new Vector3Double(bounds.center.x, SessionManager.GetSeaLevel(m_settings.m_isWorldmapSpawner), bounds.center.z);
                    bounds.size = new Vector3Double(bounds.size.x, 0.05f, bounds.size.z);
                    Gizmos.color = new Color(Color.blue.r, Color.blue.g, Color.blue.b, Color.blue.a / 4f);
                    Gizmos.DrawCube(bounds.center, bounds.size);
                }


            }

            //Update the counters
            UpdateCounters();
#endif
        }

        #region Texture map management

        /// <summary>
        /// Cache the texture maps for the terrain object id supplied - this is very memory intensive so use with care!
        /// </summary>
        public void CacheTextureMapsFromTerrain(int terrainID)
        {
            //Construct them of we dont have them
            if (m_textureMapCache == null)
            {
                m_textureMapCache = new Dictionary<int, List<HeightMap>>();
            }

            //Now find the terrain and load them for the specified terrain
            Terrain terrain;
            for (int terrIdx = 0; terrIdx < Terrain.activeTerrains.Length; terrIdx++)
            {
                terrain = Terrain.activeTerrains[terrIdx];
                if (terrain.GetInstanceID() == terrainID)
                {
                    float[,,] splatMaps = terrain.terrainData.GetAlphamaps(0, 0, terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight);
                    List<HeightMap> textureMapList = new List<HeightMap>();
                    for (int txtIdx = 0; txtIdx < terrain.terrainData.alphamapLayers; txtIdx++)
                    {
                        HeightMap txtMap = new HeightMap(splatMaps, txtIdx);
                        textureMapList.Add(txtMap);
                    }
                    m_textureMapCache[terrainID] = textureMapList;
                    return;
                }
            }
            Debug.LogError("Attempted to get textures on terrain that does not exist!");
        }

        /// <summary>
        /// Get the detail map list for the terrain
        /// </summary>
        /// <param name="terrainID">Object id of the terrain</param>
        /// <returns>Detail map list or null</returns>
        public List<HeightMap> GetTextureMaps(int terrainID)
        {
            List<HeightMap> mapList;
            if (!m_textureMapCache.TryGetValue(terrainID, out mapList))
            {
                return null;
            }
            return mapList;
        }

        /// <summary>
        /// Save the texture maps back into the terrain
        /// </summary>
        /// <param name="terrainID">ID of the terrain to do this for</param>
        public void SaveTextureMapsToTerrain(int terrainID)
        {
            Terrain terrain;
            HeightMap txtMap;
            List<HeightMap> txtMapList;

            //Make sure we can find it
            if (!m_textureMapCache.TryGetValue(terrainID, out txtMapList))
            {
                Debug.LogError("Texture map list was not found for terrain ID : " + terrainID + " !");
                return;
            }

            //Abort if we dont have anything in the list
            if (txtMapList.Count <= 0)
            {
                Debug.LogError("Texture map list was empty for terrain ID : " + terrainID + " !");
                return;
            }

            //Locate the terrain
            for (int terrIdx = 0; terrIdx < Terrain.activeTerrains.Length; terrIdx++)
            {
                terrain = Terrain.activeTerrains[terrIdx];
                if (terrain.GetInstanceID() == terrainID)
                {
                    //Make sure that the number of prototypes matches up
                    if (txtMapList.Count != terrain.terrainData.alphamapLayers)
                    {
                        Debug.LogError("Texture map prototype list does not match terrain prototype list for terrain ID : " + terrainID + " !");
                        return;
                    }

                    float[,,] splatMaps = new float[terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight, terrain.terrainData.alphamapLayers];
                    for (int txtIdx = 0; txtIdx < terrain.terrainData.alphamapLayers; txtIdx++)
                    {
                        txtMap = txtMapList[txtIdx];
                        for (int x = 0; x < txtMap.Width(); x++)
                        {
                            for (int z = 0; z < txtMap.Depth(); z++)
                            {
                                splatMaps[x, z, txtIdx] = txtMap[x, z];
                            }
                        }
                    }
                    terrain.terrainData.SetAlphamaps(0, 0, splatMaps);
                    return;
                }
            }
            Debug.LogError("Attempted to locate a terrain that does not exist!");
        }

        /// <summary>
        /// Remove the texture maps from memory
        /// </summary>
        public void DeleteTextureMapCache()
        {
            m_textureMapCache = new Dictionary<int, List<HeightMap>>();
        }

        /// <summary>
        /// Set the texture maps dirty if we modified them
        /// </summary>
        public void SetTextureMapsDirty()
        {
            m_textureMapsDirty = true;
        }

        #endregion

        #region Detail map management

        /// <summary>
        /// Get the detail maps for the terrain object id supplied - this is very memory intensive so use with care!
        /// </summary>
        public void CacheDetailMapsFromTerrain(int terrainID)
        {
            //Construct them of we dont have them
            if (m_detailMapCache == null)
            {
                m_detailMapCache = new Dictionary<int, List<HeightMap>>();
            }

            //Now find the terrain and load them for the specified terrain
            Terrain terrain;
            for (int terrIdx = 0; terrIdx < Terrain.activeTerrains.Length; terrIdx++)
            {
                terrain = Terrain.activeTerrains[terrIdx];
                if (terrain.GetInstanceID() == terrainID)
                {
                    List<HeightMap> detailMapList = new List<HeightMap>();
                    for (int dtlIdx = 0; dtlIdx < terrain.terrainData.detailPrototypes.Length; dtlIdx++)
                    {
                        HeightMap dtlMap = new HeightMap(terrain.terrainData.GetDetailLayer(0, 0, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight, dtlIdx));
                        detailMapList.Add(dtlMap);
                    }
                    m_detailMapCache[terrainID] = detailMapList;
                    return;
                }
            }
            Debug.LogError("Attempted to get details on terrain that does not exist!");
        }

        /// <summary>
        /// Save the detail maps back into the terrain
        /// </summary>
        /// <param name="terrainID">ID of the terrain to do this for</param>
        public void SaveDetailMapsToTerrain(int terrainID)
        {
            Terrain terrain;
            HeightMap dtlMap;
            List<HeightMap> dtlMapList;

            //Make sure we can find it
            if (!m_detailMapCache.TryGetValue(terrainID, out dtlMapList))
            {
                Debug.LogWarning(gameObject.name + "Detail map list was not found for terrain ID : " + terrainID + " !");
                return;
            }

            //Abort if we dont have anything in the list
            if (dtlMapList.Count <= 0)
            {
                Debug.LogWarning(gameObject.name + ": Detail map list was empty for terrain ID : " + terrainID + " !");
                return;
            }

            //Locate the terrain
            for (int terrIdx = 0; terrIdx < Terrain.activeTerrains.Length; terrIdx++)
            {
                terrain = Terrain.activeTerrains[terrIdx];
                if (terrain.GetInstanceID() == terrainID)
                {
                    //Make sure that the number of prototypes matches up
                    if (dtlMapList.Count != terrain.terrainData.detailPrototypes.Length)
                    {
                        Debug.LogError("Detail map protoype list does not match terrain prototype list for terrain ID : " + terrainID + " !");
                        return;
                    }

                    //Mow iterate thru and apply back
                    int[,] dtlMapArray = new int[dtlMapList[0].Width(), dtlMapList[0].Depth()];
                    for (int dtlIdx = 0; dtlIdx < terrain.terrainData.detailPrototypes.Length; dtlIdx++)
                    {
                        dtlMap = dtlMapList[dtlIdx];
                        for (int x = 0; x < dtlMap.Width(); x++)
                        {
                            for (int z = 0; z < dtlMap.Depth(); z++)
                            {
                                dtlMapArray[x, z] = (int)dtlMap[x, z];
                            }
                        }
                        terrain.terrainData.SetDetailLayer(0, 0, dtlIdx, dtlMapArray);
                    }
                    terrain.Flush();
                    return;
                }
            }
            Debug.LogError("Attempted to locate a terrain that does not exist!");
        }

        /// <summary>
        /// Get the detail map list for the terrain
        /// </summary>
        /// <param name="terrainID">Object id of the terrain</param>
        /// <returns>Detail map list or null</returns>
        public List<HeightMap> GetDetailMaps(int terrainID)
        {
            List<HeightMap> mapList;
            if (!m_detailMapCache.TryGetValue(terrainID, out mapList))
            {
                return null;
            }
            return mapList;
        }

        /// <summary>
        /// Get the detail map for the specific detail
        /// </summary>
        /// <param name="terrainID">Terrain to query</param>
        /// <param name="detailIndex">Detail prototype index</param>
        /// <returns>Detail heightmap or null if not found</returns>
        public HeightMap GetDetailMap(int terrainID, int detailIndex)
        {
            List<HeightMap> dtlMapList;
            if (!m_detailMapCache.TryGetValue(terrainID, out dtlMapList))
            {
                return null;
            }
            if (detailIndex >= 0 && detailIndex < dtlMapList.Count)
            {
                return dtlMapList[detailIndex];
            }
            return null;
        }

        /// <summary>
        /// Remove the detail maps from memory
        /// </summary>
        public void DeleteDetailMapCache()
        {
            m_detailMapCache = new Dictionary<int, List<HeightMap>>();
        }

        #endregion

        #region Tree Management

        public void CacheTreesFromTerrain()
        {
            m_treeCache.LoadTreesFromTerrain();
        }

        public void DeleteTreeCache()
        {
            m_treeCache = new TreeManager();
        }

        #endregion

        #region Sessions and Serialisation

        /// <summary>
        /// Add the operationm to the session manager
        /// </summary>
        /// <param name="opType">The type of operation to add</param>
        public void AddToSession(GaiaOperation.OperationType opType, string opName)
        {
            //Update the session

            if (SessionManager != null && SessionManager.IsLocked() != true)
            {
                GaiaOperation op = new GaiaOperation();
                op.m_description = opName;
                //op.m_generatedByID = m_spawnerID;
                //op.m_generatedByName = transform.name;
                //op.m_generatedByType = this.GetType().ToString();
                op.m_isActive = true;
                op.m_operationDateTime = DateTime.Now.ToString();
                op.m_operationType = opType;
                //op.m_operationDataJson = new string[1];
                //op.m_operationDataJson[0] = this.SerialiseJson();
                SessionManager.AddOperation(op);
                SessionManager.AddResource(m_settings.m_resources);
            }
        }

        /// <summary>
        /// Serialise this as json
        /// </summary>
        /// <returns></returns>
        public string SerialiseJson()
        {
            //Grab the various paths
            //#if UNITY_EDITOR
            //            m_settings.m_resourcesPath = AssetDatabase.GetAssetPath(m_settings.m_resources);
            //#endif

            //            fsData data;
            //            fsSerializer serializer = new fsSerializer();
            //            serializer.TrySerialize(this, out data);

            //            //Debug.Log(fsJsonPrinter.PrettyJson(data));

            //           return fsJsonPrinter.CompressedJson(data);
            return "";
        }

        /// <summary>
        /// Deserialise the suplied json into this object
        /// </summary>
        /// <param name="json">Source json</param>
        public void DeSerialiseJson(string json)
        {
            //fsData data = fsJsonParser.Parse(json);
            //fsSerializer serializer = new fsSerializer();
            //var spawner = this;
            //serializer.TryDeserialize<Spawner>(data, ref spawner);
            //spawner.m_settings.m_resources = GaiaUtils.GetAsset(m_settings.m_resourcesPath, typeof(Gaia.GaiaResource)) as Gaia.GaiaResource;
        }

        #endregion

        #region Handy helpers

        /// <summary>
        /// Flatten all active terrains
        /// </summary>
        public void FlattenTerrain()
        {
            //Update the session
            AddToSession(GaiaOperation.OperationType.FlattenTerrain, "Flattening terrain");

            //Get an undo buffer
            GaiaWorldManager mgr = new GaiaWorldManager(Terrain.activeTerrains);
            mgr.FlattenWorld();
        }

        /// <summary>
        /// Smooth all active terrains
        /// </summary>
        public void SmoothTerrain()
        {
            //Update the session
            AddToSession(GaiaOperation.OperationType.SmoothTerrain, "Smoothing terrain");

            //Smooth the world
            GaiaWorldManager mgr = new GaiaWorldManager(Terrain.activeTerrains);
            mgr.SmoothWorld();
        }

        /// <summary>
        /// Clear trees
        /// </summary>
        public void ClearTrees(ClearSpawnFor clearSpawnFor, ClearSpawnFrom clearSpawnFrom, List<string> terrainNames = null)
        {
            TerrainHelper.ClearSpawns(SpawnerResourceType.TerrainTree, clearSpawnFor, clearSpawnFrom, terrainNames, this);
            //iterate through all spawners, reset counter for tree rules
            ResetAffectedSpawnerCounts(SpawnerResourceType.TerrainTree);
        }

        public void ActivateTreeStandIn(int m_spawnRuleIndexBeingDrawn)
        {
            //Before activation: Do other spawners currently use the stand-in? If yes, we need to turn them back before activating this one
            var allSpawnersWithStandIns = Resources.FindObjectsOfTypeAll<Spawner>().Where(x => x.m_settings.m_spawnerRules.Find(y => y.m_usesBoxStandIn == true) != null).ToArray();

            foreach (Spawner spawner in allSpawnersWithStandIns)
            {
                for (int i = 0; i < spawner.m_settings.m_spawnerRules.Count; i++)
                {
                    SpawnRule sr = (SpawnRule)spawner.m_settings.m_spawnerRules[i];
                    if (sr.m_usesBoxStandIn)
                    {
                        spawner.DeactivateTreeStandIn(i);
                    }
                }

            }
            ResourceProtoTree resourceProtoTree = m_settings.m_resources.m_treePrototypes[m_settings.m_spawnerRules[m_spawnRuleIndexBeingDrawn].m_resourceIdx];
            foreach (Terrain t in Terrain.activeTerrains)
            {
                int treePrototypeID = -1;
                int localTerrainIdx = 0;
                foreach (TreePrototype proto in t.terrainData.treePrototypes)
                {
                    if (PWCommon4.Utils.IsSameGameObject(resourceProtoTree.m_desktopPrefab, proto.prefab, false))
                    {
                        treePrototypeID = localTerrainIdx;
                        break;
                    }
                    localTerrainIdx++;
                }

                if (treePrototypeID != -1)
                {
                    //reference the exisiting prototypes, then assign them - otherwise the terrain trees won't update properly
                    TreePrototype[] exisitingPrototypes = t.terrainData.treePrototypes;
                    exisitingPrototypes[treePrototypeID].prefab = GaiaSettings.m_boxStandInPrefab;
                    t.terrainData.treePrototypes = exisitingPrototypes;
                }
                m_settings.m_spawnerRules[m_spawnRuleIndexBeingDrawn].m_usesBoxStandIn = true;
            }
        }

        public void DeactivateTreeStandIn(int m_spawnRuleIndexBeingDrawn)
        {
            ResourceProtoTree resourceProtoTree = m_settings.m_resources.m_treePrototypes[m_settings.m_spawnerRules[m_spawnRuleIndexBeingDrawn].m_resourceIdx];
            foreach (Terrain t in Terrain.activeTerrains)
            {
                int treePrototypeID = -1;
                int localTerrainIdx = 0;
                foreach (TreePrototype proto in t.terrainData.treePrototypes)
                {
                    if (PWCommon4.Utils.IsSameGameObject(m_gaiaSettings.m_boxStandInPrefab, proto.prefab, false))
                    {
                        treePrototypeID = localTerrainIdx;
                        break;
                    }
                    localTerrainIdx++;
                }

                if (treePrototypeID != -1)
                {
                    //reference the exisiting prototypes, then assign them - otherwise the terrain trees won't update properly
                    TreePrototype[] exisitingPrototypes = t.terrainData.treePrototypes;
                    exisitingPrototypes[treePrototypeID].prefab = resourceProtoTree.m_desktopPrefab;
                    t.terrainData.treePrototypes = exisitingPrototypes;
                }
            }
            m_settings.m_spawnerRules[m_spawnRuleIndexBeingDrawn].m_usesBoxStandIn = false;
        }

        private void ResetAffectedSpawnerCounts(SpawnerResourceType resourceType)
        {
            Spawner[] affectedSpawners;
            if (m_settings.m_clearSpawnsFrom == ClearSpawnFrom.AnySource)
            {
                affectedSpawners = Resources.FindObjectsOfTypeAll<Spawner>();
            }
            else
            {
                affectedSpawners = new Spawner[1] { this };
            }

            foreach (Spawner spawner in affectedSpawners)
            {
                foreach (SpawnRule spawnRule in spawner.m_settings.m_spawnerRules)
                {
                    if (spawnRule.m_resourceType == resourceType)
                    {
                        spawnRule.m_spawnedInstances = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Clear all the grass off all the terrains
        /// </summary>
        public void ClearDetails(ClearSpawnFor clearSpawnFor, ClearSpawnFrom clearSpawnFrom, List<string> terrainNames = null)
        {
            TerrainHelper.ClearSpawns(SpawnerResourceType.TerrainDetail, clearSpawnFor, clearSpawnFrom, terrainNames, this);
            ResetAffectedSpawnerCounts(SpawnerResourceType.TerrainDetail);

        }

        /// <summary>
        /// Clears all Game object spawn rules at once.
        /// </summary>
        public void ClearGameObjects(ClearSpawnFor clearSpawnFor, ClearSpawnFrom clearSpawnFrom, List<string> terrainNames = null)
        {
            TerrainHelper.ClearSpawns(SpawnerResourceType.GameObject, clearSpawnFor, clearSpawnFrom, terrainNames, this);
            ResetAffectedSpawnerCounts(SpawnerResourceType.GameObject);

            //Spawner[] allAffectedSpawners;

            //if (clearSpawnFrom == ClearSpawnFrom.OnlyThisSpawner)
            //{
            //    allAffectedSpawners = new Spawner[1] { this };
            //}
            //else
            //{
            //    allAffectedSpawners = Resources.FindObjectsOfTypeAll<Spawner>();
            //}

            //int completedSpawners = 1;

            //foreach (Spawner spawner in allAffectedSpawners)
            //{
            //    GaiaUtils.DisplayProgressBarNoEditor("Clearing Game Objects...", "Spawner " + completedSpawners.ToString() + " of " + allAffectedSpawners.Count().ToString(), (float)completedSpawners / (float)allAffectedSpawners.Count());
            //    foreach (SpawnRule spawnRule in spawner.m_settings.m_spawnerRules)
            //    {
            //        if (spawnRule.m_resourceType == SpawnerResourceType.GameObject)
            //        {
            //            spawner.ClearGameObjectsForRule(spawnRule, clearSpawnFor == ClearSpawnFor.AllTerrains);
            //        }
            //    }
            //    completedSpawners++;
            //}
            //GaiaUtils.ClearProgressBarNoEditor();
        }

        /// <summary>
        /// Clears all Game objects created by spawn extenisons at once.
        /// </summary>
        public void ClearAllSpawnExtensions(ClearSpawnFor clearSpawnFor, ClearSpawnFrom clearSpawnFrom, List<string> terrainNames = null)
        {
            TerrainHelper.ClearSpawns(SpawnerResourceType.SpawnExtension, clearSpawnFor, clearSpawnFrom, terrainNames, this);
            ResetAffectedSpawnerCounts(SpawnerResourceType.SpawnExtension);
            //Spawner[] allAffectedSpawners;

            //if (clearSpawnFrom == ClearSpawnFrom.OnlyThisSpawner)
            //{
            //    allAffectedSpawners = new Spawner[1] { this };
            //}
            //else
            //{
            //    allAffectedSpawners = Resources.FindObjectsOfTypeAll<Spawner>();
            //}

            //int completedSpawners = 1;

            //foreach (Spawner spawner in allAffectedSpawners)
            //{
            //    GaiaUtils.DisplayProgressBarNoEditor("Clearing Spawn Extensions...", "Spawner " + completedSpawners.ToString() + " of " + allAffectedSpawners.Count().ToString(), (float)completedSpawners / (float)allAffectedSpawners.Count());
            //    foreach (SpawnRule spawnRule in spawner.m_settings.m_spawnerRules)
            //    {
            //        if (spawnRule.m_resourceType == SpawnerResourceType.SpawnExtension)
            //        {
            //            spawner.ClearSpawnExtensionsForRule(spawnRule);
            //            spawner.ClearGameObjectsForRule(spawnRule, clearSpawnFor == ClearSpawnFor.AllTerrains);
            //        }
            //    }
            //    completedSpawners++;
            //}
            //GaiaUtils.ClearProgressBarNoEditor();
        }


        /// <summary>
        /// Calls the Delete function on all Spawn Extensions of a certain rule
        /// </summary>
        /// <param name="spawnRule"></param>
        public void ClearSpawnExtensionsForRule(SpawnRule spawnRule)
        {
            if (spawnRule.m_resourceIdx > m_settings.m_resources.m_spawnExtensionPrototypes.Length - 1)
            {
                return;
            }

            if (m_settings.m_resources.m_spawnExtensionPrototypes[spawnRule.m_resourceIdx] == null)
            {
                return;
            }

            ResourceProtoSpawnExtension protoSE = m_settings.m_resources.m_spawnExtensionPrototypes[spawnRule.m_resourceIdx];

            //iterate through all instances
            foreach (ResourceProtoSpawnExtensionInstance instance in protoSE.m_instances)
            {
                if (instance.m_spawnerPrefab == null)
                {
                    continue;
                }

                foreach (ISpawnExtension spawnExtension in instance.m_spawnerPrefab.GetComponents<ISpawnExtension>())
                {
                    spawnExtension.Delete();
                }

            }

        }

        /// <summary>
        /// Clears all StampDistributions
        /// </summary>
        public void ClearStampDistributions(ClearSpawnFor clearSpawnFor, ClearSpawnFrom clearSpawnFrom)
        {
            Spawner[] allAffectedSpawners;

            if (clearSpawnFrom == ClearSpawnFrom.OnlyThisSpawner)
            {
                allAffectedSpawners = new Spawner[1] { this };
            }
            else
            {
                allAffectedSpawners = Resources.FindObjectsOfTypeAll<Spawner>();
            }

            int completedSpawners = 1;

            foreach (Spawner spawner in allAffectedSpawners)
            {
                ProgressBar.Show(ProgressBarPriority.Spawning, "Clearing Stamps", "Clearing...", completedSpawners, allAffectedSpawners.Count(), true, false);
                foreach (SpawnRule spawnRule in spawner.m_settings.m_spawnerRules)
                {
                    if (spawnRule.m_resourceType == SpawnerResourceType.StampDistribution)
                    {
                        spawner.ClearStampDistributionForRule(spawnRule);
                        spawnRule.m_spawnedInstances = 0;
                    }
                }
                completedSpawners++;
            }
            ProgressBar.Clear(ProgressBarPriority.Spawning);
        }

        /// <summary>
        /// Clears all stamp tokens / stamper settings for the World Map Generation created by a certain rule
        /// </summary>
        /// <param name="spawnRule"></param>
        private void ClearStampDistributionForRule(SpawnRule spawnRule)
        {
            ResourceProtoStampDistribution protoSD = m_settings.m_resources.m_stampDistributionPrototypes[spawnRule.m_resourceIdx];


            //iterate through all Stamp Tokens and remove those belonging to the same feature type as spawned from this rule
            List<string> allFeatureTypes = protoSD.m_featureSettings.Select(x => x.m_featureType).ToList();

            if (m_worldMapTerrain == null)
            {
                m_worldMapTerrain = TerrainHelper.GetWorldMapTerrain();
            }

            Transform tokenContainer = m_worldMapTerrain.transform.Find(GaiaConstants.worldMapStampTokenSpawnTarget);
            if (tokenContainer != null)
            {
                var allStampTokens = tokenContainer.GetComponentsInChildren<WorldMapStampToken>();

                for (int i = allStampTokens.Length - 1; i >= 0; i--)
                {
                    if (allFeatureTypes.Contains(allStampTokens[i].m_featureType))
                    {
                        m_worldMapStamperSettings.Remove(allStampTokens[i].m_connectedStamperSettings);
                        DestroyImmediate(allStampTokens[i].gameObject);
                    }

                }
            }



        }


        /// <summary>
        /// Clear all the GameObjects created by this spawner off all the terrains
        /// </summary>
        public static void ClearGameObjectsForRule(Spawner spawner, SpawnRule spawnRule, bool allTerrains = true, Terrain terrainToDeleteFrom = null)
        {
            //Update the session
            string protoName = "";
            switch (spawnRule.m_resourceType)
            {
                case SpawnerResourceType.GameObject:
                    ResourceProtoGameObject protoGO = spawner.m_settings.m_resources.m_gameObjectPrototypes[spawnRule.m_resourceIdx];

                    if (protoGO == null)
                    {
                        Debug.LogError("Could not find prototype info trying to delete Game Objects from rule " + spawnRule.m_name);
                        return;
                    }
                    protoName = protoGO.m_name;
                    break;
                case SpawnerResourceType.SpawnExtension:
                    ResourceProtoSpawnExtension protoSE = spawner.m_settings.m_resources.m_spawnExtensionPrototypes[spawnRule.m_resourceIdx];

                    if (protoSE == null)
                    {
                        Debug.LogError("Could not find prototype info trying to delete Spawn Extensions Game Objects from rule " + spawnRule.m_name);
                        return;
                    }
                    protoName = protoSE.m_name;
                    break;
                case SpawnerResourceType.Probe:
                    if (spawnRule.m_resourceIdx >= spawner.m_settings.m_resources.m_probePrototypes.Length)
                    {
                        return;
                    }
                    ResourceProtoProbe protoProbe = spawner.m_settings.m_resources.m_probePrototypes[spawnRule.m_resourceIdx];

                    if (protoProbe == null)
                    {
                        Debug.LogError("Could not find prototype info trying to delete probes from rule " + spawnRule.m_name);
                        return;
                    }
                    protoName = protoProbe.m_name;
                    break;

            }

            Terrain[] relevantTerrains;

            if (allTerrains)
            {
                relevantTerrains = Terrain.activeTerrains;
            }
            else
            {
                if (terrainToDeleteFrom == null)
                {
                    relevantTerrains = new Terrain[1] { spawner.GetCurrentTerrain() };
                }
                else
                {
                    relevantTerrains = new Terrain[1] { terrainToDeleteFrom };
                }
            }



            foreach (Terrain t in relevantTerrains)
            {
                bool deletedSomething = false;
                Transform target = GaiaUtils.GetGOSpawnTarget(spawnRule, protoName, t);
                Scene sceneWeDeletedFrom = target.gameObject.scene;

                if (spawnRule.m_goSpawnTargetMode == SpawnerTargetMode.Terrain || allTerrains)
                {
                    //Terrain based target, or user choose to delete from all Terrains - this means deletion can be done fast and easy by removing the target object                     
                    if (target != null)
                    {
                        deletedSomething = true;
                        DestroyImmediate(target.gameObject);
                    }
                }
                else
                {
                    //There is a custom transform to spawn under and we want to delete on specific terrains only - which means we need to take a look at each Gameobject individually


                    float terrainMinX = t.transform.position.x;
                    float terrainMinZ = t.transform.position.z;
                    float terrainMaxX = t.transform.position.x + t.terrainData.size.x;
                    float terrainMaxZ = t.transform.position.z + t.terrainData.size.x;


                    for (int g = target.childCount - 1; g >= 0; g--)
                    {
                        GameObject GOtoDelete = target.GetChild(g).gameObject;

                        //is the gameobject placed on / above / below the terrain?
                        if (terrainMinX <= GOtoDelete.transform.position.x &&
                            terrainMinZ <= GOtoDelete.transform.position.z &&
                            terrainMaxX >= GOtoDelete.transform.position.x &&
                            terrainMaxZ >= GOtoDelete.transform.position.z)
                        {
                            DestroyImmediate(GOtoDelete);
                            deletedSomething = true;
                        }
                    }
                    //if the target is empty now, we can remove it as well to keep scene clean
                    if (target.childCount <= 0)
                    {
                        deletedSomething = true;
                        DestroyImmediate(target.gameObject);
                    }
                }
                //if we deleted something the scene we deleted from should be marked as dirty.
                if (deletedSomething)
                {
#if UNITY_EDITOR
                    EditorSceneManager.MarkSceneDirty(sceneWeDeletedFrom);
#endif
                }
            }
            spawnRule.m_spawnedInstances = 0;
        }

        /// <summary>
        /// Only serves the purpose of supressing the warning for the unused field when CTS is not installed.
        /// </summary>
        private void SupressCTSProfileWarning()
        {
#if CTS_PRESENT
            if (m_connectedCTSProfileGUID.Length > 1)
            {
            }
#endif
        }


        public static void HandleAutoSpawnerStack(List<AutoSpawner> autoSpawners, Transform transform, float range, bool allTerrains, BiomeControllerSettings biomeControllerSettings = null)
        {
            BoundsDouble spawnArea = new BoundsDouble();
            if (allTerrains)
            {
                TerrainHelper.GetTerrainBounds(ref spawnArea);
            }
            else
            {
                if (GaiaUtils.HasDynamicLoadedTerrains())
                {
                    spawnArea.center = new Vector3Double(transform.position) + TerrainLoaderManager.Instance.GetOrigin();
                }
                else
                {
                    spawnArea.center = transform.position;
                }
                spawnArea.size = new Vector3(range * 2f, range * 2f, range * 2f);
            }

            try
            {
                TerrainLoaderManager.Instance.SwitchToLocalMap();
                foreach (Spawner spawner in autoSpawners.Where(x => x.spawner != null && x.isActive == true).Select(x => x.spawner))
                {
                    spawner.GenerateNewRandomSeed();
                }
                SpawnOperationSettings soSettings = ScriptableObject.CreateInstance<SpawnOperationSettings>();
                soSettings.m_spawnerSettingsList = autoSpawners.Where(x => x.spawner != null && x.isActive == true).Select(x => x.spawner.m_settings).ToList();
                if (biomeControllerSettings != null)
                {
                    soSettings.m_biomeControllerSettings = biomeControllerSettings;
                }
                soSettings.m_spawnArea = spawnArea;
                soSettings.m_isWorldMapSpawner = autoSpawners.Find(x => x.spawner != null && x.spawner.m_settings.m_isWorldmapSpawner) != null;
                GaiaSessionManager.Spawn(soSettings, true, autoSpawners.Where(x => x.spawner != null && x.isActive == true).Select(x => x.spawner).ToList());
                //autoSpawners[0].spawner.m_updateCoroutine = autoSpawners[0].spawner.AreaSpawn(autoSpawners.Select(x => x.spawner).ToList(), spawnArea);
                //autoSpawners[0].spawner.StartEditorUpdates();
            }
            catch (Exception ex)
            {
                Debug.LogError("Autospawning failed with Exception: " + ex.Message + "\n\n" + "Stack trace: \n\n" + ex.StackTrace);
                ProgressBar.Clear(ProgressBarPriority.Spawning);
            }



            //AutoSpawner nextSpawner = autoSpawners.Find(x => x.status == AutoSpawnerStatus.Spawning);
            //if (nextSpawner != null)
            //{
            //    if (nextSpawner.spawner.IsSpawning())
            //    {
            //        return false;
            //        //Do Nothing, still spawning
            //    }
            //    else
            //    {
            //        //Auto Spawner is done, look for next spawner
            //        GaiaUtils.DisplayProgressBarNoEditor("Spawning", "Preparing next Spawner...",0);
            //        nextSpawner.status = AutoSpawnerStatus.Done;
            //        nextSpawner = autoSpawners.Find(x => x.status == AutoSpawnerStatus.Queued);

            //    }
            //}
            //else
            //{
            //    //No spawner spawning atm, let's pick the first queued one
            //    nextSpawner = autoSpawners.Find(x => x.status == AutoSpawnerStatus.Queued);
            //}

            //if (nextSpawner != null && !m_cancelSpawn)
            //{
            //    if (!nextSpawner.spawner.IsSpawning())
            //    {
            //        //nextSpawner.spawner.transform.position = new Vector3(m_stamper.transform.position.x, nextSpawner.spawner.transform.position.y, m_stamper.transform.position.z);
            //        //Terrain terrain = nextSpawner.spawner.GetCurrentTerrain();
            //        //nextSpawner.spawner.m_settings.m_spawnRange = terrain.terrainData.size.x * (m_stamper.m_settings.m_width / 100f);
            //        nextSpawner.spawner.UpdateMinMaxHeight();

            //        int totalSpawnRules = 0;
            //        int completedSpawnRules = 0;
            //        foreach (AutoSpawner autoSpawner in autoSpawners.Where(x => x.isActive))
            //        {
            //            foreach (SpawnRule rule in autoSpawner.spawner.settings.m_spawnerRules)
            //            {
            //                if (rule.m_isActive)
            //                {
            //                    totalSpawnRules++;
            //                    if (autoSpawner.status == AutoSpawnerStatus.Done)
            //                    {
            //                        completedSpawnRules++;
            //                    }
            //                }
            //            }
            //        }

            //        if (allTerrains)
            //        {
            //            ////int worldSpawnSteps = nextSpawner.spawner.GetWorldSpawnSteps();
            //            //totalSpawnRules *= worldSpawnSteps;
            //            //completedSpawnRules *= worldSpawnSteps;
            //        }

            //        //nextSpawner.spawner.Spawn(allTerrains, completedSpawnRules, totalSpawnRules);
            //        nextSpawner.status = AutoSpawnerStatus.Spawning;
            //    }
            //    return false;

            //}
            //else
            //{
            //    //no spawners left
            //   GaiaUtils.ClearProgressBarNoEditor();
            //   GaiaUtils.ReleaseAllTempRenderTextures();
            //    m_cancelSpawn = false;
            //    return true;

            //}
        }

        #endregion

        #region Height map management

        /// <summary>
        /// Cache the height map for the terrain object id supplied - this is very memory intensive so use with care!
        /// </summary>
        public void CacheHeightMapFromTerrain(int terrainID)
        {
            //Construct them of we dont have them
            if (m_heightMapCache == null)
            {
                m_heightMapCache = new Dictionary<int, UnityHeightMap>();
            }

            //Now find the terrain and load them for the specified terrain
            Terrain terrain;
            for (int terrIdx = 0; terrIdx < Terrain.activeTerrains.Length; terrIdx++)
            {
                terrain = Terrain.activeTerrains[terrIdx];
                if (terrain.GetInstanceID() == terrainID)
                {
                    m_heightMapCache[terrainID] = new UnityHeightMap(terrain);
                    return;
                }
            }
            Debug.LogError("Attempted to get height maps on a terrain that does not exist!");
        }

        /// <summary>
        /// Get the height map for the terrain
        /// </summary>
        /// <param name="terrainID">Object id of the terrain</param>
        /// <returns>Heightmap or null</returns>
        public UnityHeightMap GetHeightMap(int terrainID)
        {
            UnityHeightMap heightmap;
            if (!m_heightMapCache.TryGetValue(terrainID, out heightmap))
            {
                return null;
            }
            return heightmap;
        }

        /// <summary>
        /// Save the height map back into the terrain
        /// </summary>
        /// <param name="terrainID">ID of the terrain to do this for</param>
        public void SaveHeightMapToTerrain(int terrainID)
        {
            Terrain terrain;
            UnityHeightMap heightmap;

            //Make sure we can find it
            if (!m_heightMapCache.TryGetValue(terrainID, out heightmap))
            {
                Debug.LogError("Heightmap was not found for terrain ID : " + terrainID + " !");
                return;
            }

            //Locate the terrain and update it
            for (int terrIdx = 0; terrIdx < Terrain.activeTerrains.Length; terrIdx++)
            {
                terrain = Terrain.activeTerrains[terrIdx];
                if (terrain.GetInstanceID() == terrainID)
                {
                    heightmap.SaveToTerrain(terrain);
                    return;
                }
            }
            Debug.LogError("Attempted to locate a terrain that does not exist!");
        }

        /// <summary>
        /// Remove the texture maps from memory
        /// </summary>
        public void DeleteHeightMapCache()
        {
            m_heightMapCache = new Dictionary<int, UnityHeightMap>();
        }

        /// <summary>
        /// Set the height maps dirty if we modified them
        /// </summary>
        public void SetHeightMapsDirty()
        {
            m_heightMapDirty = true;
        }

        #endregion

        #region Stamp management

        public void CacheStamps(List<string> stampList)
        {
            //Construct them of we dont have them
            if (m_stampCache == null)
            {
                m_stampCache = new Dictionary<string, HeightMap>();
            }

            //Get the list of stamps for this spawner
            for (int idx = 0; idx < stampList.Count; idx++)
            {



            }
        }


        #endregion

        #region Tag management

        /// <summary>
        /// Load all the tags in the scene into the tag cache
        /// </summary>
        /// <param name="tagList"></param>
        private void CacheTaggedGameObjectsFromScene(List<string> tagList)
        {
            //Create a new cache (essentially releasing the old one)
            m_taggedGameObjectCache = new Dictionary<string, Quadtree<GameObject>>();

            //Now load all the tagged objects into the cache
            string tag;
            bool foundTag;
            Quadtree<GameObject> quadtree;
            Rect pos = new Rect(Terrain.activeTerrain.transform.position.x, Terrain.activeTerrain.transform.position.z,
                Terrain.activeTerrain.terrainData.size.x, Terrain.activeTerrain.terrainData.size.z);

            for (int tagIdx = 0; tagIdx < tagList.Count; tagIdx++)
            {
                //Check that unity knows about the tag

                tag = tagList[tagIdx].Trim();
                foundTag = false;
                if (!string.IsNullOrEmpty(tag))
                {
#if UNITY_EDITOR
                    for (int idx = 0; idx < UnityEditorInternal.InternalEditorUtility.tags.Length; idx++)
                    {
                        if (UnityEditorInternal.InternalEditorUtility.tags[idx].Contains(tag))
                        {
                            foundTag = true;
                            break;
                        }
                    }
#else
                    foundTag = true;
#endif
                }

                //If its good then cache it
                if (foundTag)
                {
                    quadtree = null;
                    if (!m_taggedGameObjectCache.TryGetValue(tag, out quadtree))
                    {
                        quadtree = new Quadtree<GameObject>(pos);
                        m_taggedGameObjectCache.Add(tag, quadtree);
                    }
                    GameObject go;
                    Vector2 go2DPos;
                    GameObject[] gos = GameObject.FindGameObjectsWithTag(tag);
                    for (int goIdx = 0; goIdx < gos.Length; goIdx++)
                    {
                        go = gos[goIdx];

                        //Only add it if within our bounds
                        go2DPos = new Vector2(go.transform.position.x, go.transform.position.z);
                        if (pos.Contains(go2DPos))
                        {
                            quadtree.Insert(go2DPos, go);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Delete the tag cache
        /// </summary>
        private void DeleteTagCache()
        {
            m_taggedGameObjectCache = null;
        }

        /// <summary>
        /// Get the objects that match the tag list within the defined area
        /// </summary>
        /// <param name="tagList">List of tags to search</param>
        /// <param name="area">Area to search</param>
        /// <returns></returns>
        public List<GameObject> GetNearbyObjects(List<string> tagList, Rect area)
        {
            string tag;
            List<GameObject> gameObjects = new List<GameObject>();
            Quadtree<GameObject> quadtree;
            for (int tagIdx = 0; tagIdx < tagList.Count; tagIdx++)
            {
                quadtree = null;
                tag = tagList[tagIdx];

                //Process each tag
                if (m_taggedGameObjectCache.TryGetValue(tag, out quadtree))
                {
                    IEnumerable<GameObject> gameObjs = quadtree.Find(area);
                    foreach (GameObject go in gameObjs)
                    {
                        gameObjects.Add(go);
                    }
                }
            }
            return gameObjects;
        }

        /// <summary>
        /// Get the closest gameobject to the centre of the area supplied that matches the tag list
        /// </summary>
        /// <param name="tagList">List of tags to search</param>
        /// <param name="area">The area to search</param>
        /// <returns></returns>
        public GameObject GetClosestObject(List<string> tagList, Rect area)
        {
            string tag;
            float distance;
            float closestDistance = float.MaxValue;
            GameObject closestGo = null;
            Quadtree<GameObject> quadtree;
            for (int tagIdx = 0; tagIdx < tagList.Count; tagIdx++)
            {
                quadtree = null;
                tag = tagList[tagIdx];

                //Process each tag
                if (m_taggedGameObjectCache.TryGetValue(tag, out quadtree))
                {
                    IEnumerable<GameObject> gameObjs = quadtree.Find(area);
                    foreach (GameObject go in gameObjs)
                    {
                        distance = Vector2.Distance(area.center, new Vector2(go.transform.position.x, go.transform.position.z));
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestGo = go;
                        }
                    }
                }
            }
            return closestGo;
        }

        /// <summary>
        /// Get the closest gameobject to the centre of the area supplied that matches the tag 
        /// </summary>
        /// <param name="tagList">Tag to search for</param>
        /// <param name="area">The area to search</param>
        /// <returns></returns>
        public GameObject GetClosestObject(string tag, Rect area)
        {
            float distance, closestDistance = float.MaxValue;
            GameObject closestGo = null;
            Quadtree<GameObject> quadtree = null;

            if (m_taggedGameObjectCache.TryGetValue(tag, out quadtree))
            {
                IEnumerable<GameObject> gameObjs = quadtree.Find(area);
                foreach (GameObject go in gameObjs)
                {
                    distance = Vector2.Distance(area.center, new Vector2(go.transform.position.x, go.transform.position.z));
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestGo = go;
                    }
                }
            }
            return closestGo;
        }


        #endregion

        #region Saving and Loading

        public void LoadSettings(SpawnerSettings settingsToLoad)
        {
            m_settings.ClearImageMaskTextures();
            //Set existing settings = null to force a new scriptable object
            m_settings = null;

            m_settings = Instantiate(settingsToLoad);
#if UNITY_EDITOR
            m_settings.m_lastGUIDSaved = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(settingsToLoad));
#endif
            //GaiaUtils.CopyFields(settingsToLoad, m_settings);

            Spawner[] allSpawner = Resources.FindObjectsOfTypeAll<Spawner>();

#if HDPipeline
            bool hdTerrainDetailMessageDisplayed = false;
#endif

            foreach (SpawnRule rule in m_settings.m_spawnerRules)
            {
                //close down all foldouts neatly when freshly loaded
                rule.m_isFoldedOut = false;
                rule.m_resourceSettingsFoldedOut = false;
                rule.m_spawnedInstances = 0;

                //check if the spawn rule guid exists in this scene already - if yes, this rule must get a new ID then to avoid duplicate IDs
                if (allSpawner.Select(x => x.m_settings.m_spawnerRules).Where(x => x.Find(y => y.GUID == rule.GUID) != null).Count() > 1)
                {
                    rule.RegenerateGUID();
                }

#if HDPipeline
                if(rule.m_resourceType == SpawnerResourceType.TerrainDetail)
                {
                    rule.m_isActive = false;
                    if (!hdTerrainDetailMessageDisplayed)
                    {
                        Debug.Log("Spawner '" + this.name + "' contains Terrain Detail Spawn Rules. These have been deactivated because HDRP does not support the terrain detail system. You can still activate these rules manually if you wish to spawn terrain details anyway.");
                        hdTerrainDetailMessageDisplayed = true;
                    }
                }
#endif

            }

            //Reset the stored terrain layer asset guids - need to start fresh
            foreach (ResourceProtoTexture resourceProtoTexture in m_settings.m_resources.m_texturePrototypes)
            {
                resourceProtoTexture.m_LayerGUID = "";
            }

            //Try to look up all collision layer masks by their name where possible - layer orders could be different from when the spawner was saved.
            foreach (ImageMask imageMask in m_settings.m_imageMasks.Where(x => x.m_operation == ImageMaskOperation.CollisionMask))
            {
                imageMask.TryRefreshCollisionMask();
            }
            foreach (SpawnRule sr in m_settings.m_spawnerRules)
            {
                foreach (ImageMask imageMask in sr.m_imageMasks.Where(x => x.m_operation == ImageMaskOperation.CollisionMask))
                {
                    imageMask.TryRefreshCollisionMask();
                }
            }



            //Refresh texture spawn ruled GUIDs for the texture masks since new ones could be added with these settings
            ImageMask.RefreshSpawnRuleGUIDs();

            m_rulePanelUnfolded = true;
            if (m_settings.m_isWorldmapSpawner)
            {
                if (Gaia.TerrainHelper.GetTerrain(transform.position, m_settings.m_isWorldmapSpawner) != null)
                {
                    FitToTerrain();
                }
                //Since this is a new world designer, assign these settings to be world biome mask settings
                SessionManager.m_session.m_worldBiomeMaskSettings = m_settings;
                SessionManager.SaveSession();
                TerrainLoaderManager.Instance.TerrainSceneStorage.m_hasWorldMap = true;
            }
            UpdateMinMaxHeight();

        }
        #endregion

        #region Random number utils

        /// <summary>
        /// Reset the random number generator
        /// </summary>
        public void ResetRandomGenertor()
        {
            m_rndGenerator = new XorshiftPlus(m_seed);
        }

        /// <summary>
        /// Get a random integer
        /// </summary>
        /// <param name="min">Minimum value inclusive</param>
        /// <param name="max">Maximum value inclusive</param>
        /// <returns>Random integer between minimum and maximum values</returns>
        public int GetRandomInt(int min, int max)
        {
            return m_rndGenerator.Next(min, max);
        }

        /// <summary>
        /// Get a random float
        /// </summary>
        /// <param name="min">Minimum value inclusive</param>
        /// <param name="max">Maximum value inclusive</param>
        /// <returns>Random float between minimum and maximum values</returns>
        public float GetRandomFloat(float min, float max)
        {
            return m_rndGenerator.Next(min, max);
        }

        /// <summary>
        /// Get a random vector 3
        /// </summary>
        /// <param name="range">Range of values to return</param>
        /// <returns>Vector 3 in the +- range supplied</returns>
        public Vector3 GetRandomV3(float range)
        {
            return m_rndGenerator.NextVector(-range, range);
        }

        #endregion
    }
}
