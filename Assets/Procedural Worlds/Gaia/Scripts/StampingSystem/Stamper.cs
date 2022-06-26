using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using ProceduralWorlds.WaterSystem;
using UnityEngine.SocialPlatforms;
#if UNITY_EDITOR
using UnityEngine.Experimental.TerrainAPI;
#endif
using System.Linq;

namespace Gaia
{

    public enum AutoSpawnerStatus { Initial,Queued,Spawning,Done}

    /// <summary>
    /// Class to hold data about spawners being automatically triggered by a stamper
    /// </summary>
    [System.Serializable]
    public class AutoSpawner
    {
        public bool isActive = true;
        public AutoSpawnerStatus status = AutoSpawnerStatus.Initial;
        public float lastSpawnTimeStamp = float.MinValue;
        public Spawner spawner;
    }

    /// <summary>
    /// Class to hold data about mask exports being automatically triggered by a stamper
    /// </summary>
    [System.Serializable]
    public class AutoMaskExport
    {
        public bool isActive = true;
#if GAIA_PRO_PRESENT
        public MaskMapExport maskMapExport;
#endif
    }

    /// <summary>
    /// Class to apply stamps to the terrain
    /// </summary>
    [ExecuteInEditMode]
    [System.Serializable]
    public class Stamper : MonoBehaviour
    {
#region Basic stamp control

        /// <summary>
        /// The stamp ID
        /// </summary>
        public string m_stampID = Guid.NewGuid().ToString();

        /// <summary>
        /// A high quality texture from the stamp raw data - used for the stamper preview
        /// </summary>
        public Texture2D m_stampImage;

        /// <summary>
        /// The stamp image GUID, used for (re-) loading the stamp when required
        /// </summary>
        public string m_stampImageGUID = "";

        public bool m_autoSpawnersToggleAll = false;
        public bool m_autoMaskExportersToggleAll = false;

        public List<AutoSpawner> m_autoSpawners = new List<AutoSpawner>();
        public List<AutoMaskExport> m_autoMaskExporter = new List<AutoMaskExport>();

        public List<Terrain> m_lastAffectedTerrains;

        public float m_lastActiveTerrainSize;


        /// <summary>
        /// Of the resoucces are missing then base sea level off this instead
        /// </summary>
        [NonSerialized]
        public float m_seaLevel = 0f;

       
                

        public StamperSettings m_settings;

        //public StamperSettings.ClipData[] m_clipData = new StamperSettings.ClipData[0];

#endregion


#region Stamp variables

        /// <summary>
        /// Toggling this value will toggle the inversion status on the stamp - preset to have the stamp inverted when it is loaded
        /// </summary>
        public bool m_invertStamp = false;

        /// <summary>
        /// Toggling this value will toggle the normalisation status of the stamp - preset to have the stamp normalised when loaded
        /// </summary>
        public bool m_normaliseStamp = false;

        /// <summary>
        /// Stamp operation type - determines how the stamp will be applied
        /// </summary>
        //public GaiaConstants.FeatureOperation m_stampOperation = GaiaConstants.FeatureOperation.RaiseHeight;

        /// <summary>
        /// Stamp smooth iterations - the number of smoothing iterations to be applied to a stamp before stamping - use to clean up noisy stamps
        /// without affecting rest of the terrain.
        /// </summary>
        public int m_smoothIterations = 0;

        /// <summary>
        /// The blend strength to use 0.. original terrain... 1.. new stamp - used only for Constants.FeatureOperation.BlendHeight
        /// </summary>
        public float m_blendStrength = 0.5f; //The strength of the stamp if blending

        /// <summary>
        /// The physical stencil height in meters - adds or subtracts to that height based on the stamp height 0.. no impact... 1.. full impact.
        /// Most normalise stamp first for this to be accurate. Only used for Constants.FeatureOperation.StencilHeight
        /// </summary>
        public float m_stencilHeight = 1f;

        /// <summary>
        /// A curve that influences adjusts the height of the stamp
        /// </summary>
        public AnimationCurve m_heightModifier = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        /// <summary>
        /// A curve that influences the strength of the stamp over distance from centre. LHS is centre of stamp, RHS is outer edge of stamp. 
        /// </summary>
        public AnimationCurve m_distanceMask = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));

        /// <summary>
        /// Determines whether the distance mask will be applied to the total spawning result, or only to the stamp itself
        /// </summary>
        public GaiaConstants.MaskInfluence m_distanceMaskInfluence = GaiaConstants.MaskInfluence.OnlyStampItself;

        /// <summary>
        /// A flag to determine if the distance Mask was changed so that the shader input must be reprocessed
        /// </summary>
        public bool m_MaskTexturesDirty;

        /// <summary>
        /// The area mask to apply
        /// </summary>
        public GaiaConstants.ImageFitnessFilterMode m_areaMaskMode = GaiaConstants.ImageFitnessFilterMode.None;

        /// <summary>
        /// The source texture used for area based filters, can be used in conjunction with the distance mask. Values range in 0..1.
        /// </summary>
        public Texture2D m_imageMask;

        /// <summary>
        /// The GUID to the image mask. Used for (re-) loading the image mask via the asset database if required.
        /// </summary>
        public string m_imageMaskGUID;

        /// <summary>
        /// Determines whether the area mask will be applied to the total spawning result, or only to the stamp itself
        /// </summary>
        public GaiaConstants.MaskInfluence m_areaMaskInfluence = GaiaConstants.MaskInfluence.OnlyStampItself;


        /// <summary>
        /// This is used to invert the strengh supplied image mask texture
        /// </summary>
        public bool m_imageMaskInvert = false;

        /// <summary>
        /// This is used to invert the strengh supplied image mask texture
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
        /// Used to force the stamp to always show itself when you select something else in the editor
        /// </summary>
        public bool m_alwaysShow = false;


        /// <summary>
        /// Set this to true if we want to show a box around the stamp to better visualize the position & extents
        /// </summary>
        public bool m_showBoundingBox = true;


        /// <summary>
        /// Set this to true if we want to show the base
        /// </summary>
        public bool m_showBase = true;

        /// <summary>
        /// Used to get the stamp to draw the sea level
        /// </summary>
        public bool m_showSeaLevelPlane = true;

        /// <summary>
        /// Used to show the stamp rulers - some people might find this useful
        /// </summary>
        public bool m_showRulers = false;

        /// <summary>
        /// Shows the terrain helper - handy helper feature
        /// </summary>
        //public bool m_showTerrainHelper = false;

        /// <summary>
        /// If the stamper will sync the heightmaps after stamping. Only set to false if you intend to call SyncHeightmaps manually after the stamping!
        /// </summary>
        public bool m_syncHeightmaps = true;


        /// <summary>
        /// flag to determine whether the stamp preview needs to be re-calculated
        /// </summary>
        public bool m_stampDirty = true;
        private bool m_lastScaleChangeWasX;

        /// <summary>
        /// The associated stamp token, if any. If there is a token associated with this stamper, it will try to synchronize its position on the world map.
        /// </summary>
        public WorldMapStampToken m_worldMapStampToken;

#region Effect Properties

    

        /// <summary>
        /// Whether or not the stamper operation will be recorded in the undo stack.
        /// </summary>
        public bool m_recordUndo = true;

        /// <summary>
        /// The index of the UndoOperation we are currently looking at in the scene view.
        /// </summary>
        public int m_currentStamperUndoOperation = 0;

        /// <summary>
        /// The recorded undo operations for this stamper.
        /// </summary>
        public List<GaiaWorldManager> m_stamperUndoOperations = new List<GaiaWorldManager>(); 
#endregion

        /// <summary>
        /// Gizmo colour
        /// </summary>
        [NonSerialized]
        public Color m_gizmoColour = new Color(1f, .6f, 0f, 1f);

        /// <summary>
        /// Use for co-routine simulation
        /// </summary>
        [NonSerialized]
        public IEnumerator m_updateCoroutine;

        /// <summary>
        /// Amount of time per allowed update
        /// </summary>
        [NonSerialized]
        public float m_updateTimeAllowed = 1f / 30f;

        /// <summary>
        /// Current progress on updating the stamp
        /// </summary>
        [NonSerialized]
        public float m_stampProgress = 0f;

        /// <summary>
        /// Whether or not its completed processing
        /// </summary>
        [NonSerialized]
        public bool m_stampComplete = true;

        /// <summary>
        /// Whether or not to cancel it
        /// </summary>
        [NonSerialized]
        public bool m_cancelStamp = false;

        /// <summary>
        /// The material used to preview the stamp
        /// </summary>
        [NonSerialized]
        public Material m_previewMaterial;


        /// <summary>
        /// The biome controller created initially on terrain / world creation with Gaia.
        /// If set, the user can select / spawn the biome directly from buttons in the stamper.
        /// </summary>
        public BiomeController m_linkedBiomeController;


#endregion

#region Private variables

        private Texture2D m_heightTransformCurveTexture;
        private Texture2D heightTransformCurveTexture
        {
            get
            {
                return ImageProcessing.CreateMaskCurveTexture(ref m_heightTransformCurveTexture);
            }
        }

        /// <summary>
        /// Current feature ID - used to do feature change detection
        /// </summary>
#pragma warning disable 414
        private int m_featureID;
#pragma warning restore 414

        /// <summary>
        /// Internal variables to control how the stamp stamps
        /// </summary>
        private int m_scanWidth = 0;
        private int m_scanDepth = 0;
        private int m_scanHeight = 0;
        private float m_scanResolution = 0.1f; //Every 10 cm
        private Bounds m_scanBounds;
        private UnityHeightMap m_stampHM;
        private GaiaWorldManager m_undoMgr;
        private GaiaWorldManager m_redoMgr;
        private MeshFilter m_previewFilter;
        private MeshRenderer m_previewRenderer = null;

        private GaiaSettings m_gaiaSettings;



        //Holds the last valid terrain the stamper was over
        //If the stamper was moved out of the bounds of that terrain and no
        //other terrain is there to take over, stamp preview will be drawn based on this terrain.
        //private Terrain m_lastActiveTerrain;

        

        private Texture2D m_distanceMaskCurveTexture;

        private Texture2D distanceMaskCurveTexture
        {
            get
            {
                if (m_distanceMaskCurveTexture == null)
                {
                    TextureFormat format = TextureFormat.RGB24;
                    if (SystemInfo.SupportsTextureFormat(TextureFormat.RFloat))
                        format = TextureFormat.RFloat;
                    else if (SystemInfo.SupportsTextureFormat(TextureFormat.RHalf))
                        format = TextureFormat.RHalf;

                    m_distanceMaskCurveTexture = new Texture2D(256, 1, format, false, true)
                    {
                        name = "Distance mask curve texture",
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear,
                        anisoLevel = 0,
                        hideFlags = HideFlags.DontSave
                    };
                }

                return m_distanceMaskCurveTexture;
            }
        }

        private Texture2D m_transformHeightCurveTexture;

        private Texture2D transformHeightCurveTexture
        {
            get
            {
                if (m_transformHeightCurveTexture == null)
                {
                    TextureFormat format = TextureFormat.RGB24;
                    if (SystemInfo.SupportsTextureFormat(TextureFormat.RFloat))
                        format = TextureFormat.RFloat;
                    else if (SystemInfo.SupportsTextureFormat(TextureFormat.RHalf))
                        format = TextureFormat.RHalf;

                    m_transformHeightCurveTexture = new Texture2D(256, 1, format, false, true)
                    {
                        name = "Distance mask curve texture",
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear,
                        anisoLevel = 0,
                        hideFlags = HideFlags.DontSave
                    };
                }

                return m_transformHeightCurveTexture;
            }
        }

        //Eroder class reference for the erosion feature
#if UNITY_EDITOR && GAIA_PRO_PRESENT
        private HydraulicEroder m_Eroder = null;
#endif

        //holds the material for the current FX used in the stamper
        private Material m_currentFXMaterial;

        //cached Render Texture for storing the result of stamp processing
        //this version is used to display the stamp preview until stamper values change
        private RenderTexture m_cachedRenderTexture;

        //This is a cached version of the current mask used in the stamper, used for the mask preview
        public RenderTexture m_cachedMaskTexture;

        //Is the stamp preview enabled or not?
        public bool m_drawPreview = true;

        //stores the last terrain the stamper was positioned over
        //public Terrain m_cachedTerrain;

        //should the sea level be shown in the stamp preview?
        public bool m_showSeaLevelinStampPreview = true;
        public float m_maxCurrentTerrainHeight;
        public float m_minCurrentTerrainHeight;
        public bool m_activacteLocationSliders = true;


        private GaiaSessionManager m_sessionManager;
        public bool m_activatePreviewRequested;
        public long m_activatePreviewTimeStamp;
        public bool m_heightUpdateRequested;
        public bool m_autoSpawnRequested;
        public bool m_autoMaskExportRequested;
        public bool m_autoSpawnStarted;
        public long m_lastHeightmapUpdateTimeStamp;
        public bool m_openedFromTerrainGenerator;
        public bool m_hasStamped;
        public bool m_showAutoSpawnersOnEnable;

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

        public bool m_sessionEditMode;
        public GaiaOperation m_sessionEditOperation;

        public int m_impostorLoadingRange;
        public bool m_highlightLoadingSettings;
        public long m_highlightLoadingSettingsStartedTimeStamp;


#if GAIA_PRO_PRESENT
        public LoadMode m_loadTerrainMode;
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
#endif

#endregion

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            //mark shader textures as dirty after compiling so they are being rebuilt
            //otherwise the stamper has display issues after compilation
            foreach (var obj in GameObject.FindObjectsOfType<Stamper>())
            {
                obj.m_MaskTexturesDirty = true;
            }
        }
#endif


#region Public API Methods

        /// <summary>
        /// Load the currently selected stamp
        /// </summary>
        public void LoadStamp()
        {
            m_featureID = -1;
            m_scanBounds = new Bounds(transform.position, Vector3.one * 10f);

            string stampImageName = "";

            //See if we have something to load
            if (m_stampImage == null)
            {
                Debug.LogWarning("Can't load feature - texture not set");
                return;
            }
            else
            {
#if UNITY_EDITOR
                m_stampImageGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_stampImage));
#endif
                stampImageName = m_stampImage.name;
            }
            //            //Get path
            //            m_featureID = m_stampPreviewImage.GetInstanceID();
            //            if (!GaiaUtils.CheckValidGaiaStampPath(m_stampPreviewImage))
            //            {
            //#if UNITY_EDITOR
            //                EditorUtility.DisplayDialog("OOPS!", "The image you have selected is not a valid Stamp preview. You can find your Stamps and their Stamp previews in one of the directories underneath your Gaia\\Stamps directory. \n\nIf you want to turn this image into a Stamp that can be used by the Stamper then please use the Scanner. You can access the Scanner via the utilities section of the Gaia Manager window. You can open Gaia Manager by pressing Ctrl G, or selecting Window -> Gaia -> Gaia Manager.", "OK");
            //#else
            //                Debug.LogError("The file provided is not a valid stamp. You need to drag the stamp preview from one of the directories underneath your Gaia Stamps directory.");
            //#endif
            //                m_featureID = -1;
            //                m_stampPreviewImage = null;
            //                return;
            //            }
            //string path = GaiaUtils.GetGaiaStampPath(m_stampImage);

            //Load stamp
            m_stampHM = new UnityHeightMap(m_stampImage);
            if (!m_stampHM.HasData())
            {
                m_featureID = -1;
                m_stampImage = null;
                Debug.LogError("Was unable to load " + m_stampImage.name);
                return;
            }

            //Get metadata
            //float[] metaData = new float[5];
            //Buffer.BlockCopy(m_stampHM.GetMetaData(), 0, metaData, 0, metaData.Length * 4);
            //m_scanWidth = (int)metaData[0];
            //m_scanDepth = (int)metaData[1];
            //m_scanHeight = (int)metaData[2];
            //m_scanResolution = metaData[3];
            //m_baseLevel = metaData[4];
            //m_scanBounds = new Bounds(transform.position, new Vector3(
            //   (float)m_scanWidth * m_scanResolution * m_width,
            //   (float)m_scanHeight * m_scanResolution * m_height,
            //   (float)m_scanDepth * m_scanResolution * m_width));

            //Invert
            //Note that stamp inversion is handled by inverting the brush strength before passing it into the 
            //preview / stamp shader - faster this way than converting the heightmap

            //if (m_invertStamp)
            //{
            //    m_stampHM.Invert();
            //}

            //Normalise
            if (m_normaliseStamp)
            {
                m_stampHM.Normalise();
            }

            //Smooth
            if (m_smoothIterations > 0)
            {
                SmoothStamp();
            }

            m_stampImage = m_stampHM.ToTexture();
            m_stampImage.name = stampImageName;
            m_MaskTexturesDirty = true;
            //Generate the feature mesh
            //GeneratePreviewMesh();
        }



        /// <summary>
        /// Load the stamp at the image preview path provided
        /// </summary>
        /// <param name="imagePreviewPath">Path to the image preview</param>
        public void LoadStamp(string imagePreviewPath)
        {
#if UNITY_EDITOR
            m_stampImage = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePreviewPath);
            m_stampImage.name = Path.GetFileNameWithoutExtension(imagePreviewPath);
            m_stampImageGUID = m_stampImage.name;
#endif
            LoadStamp();
        }

        /// <summary>
        /// Bypass the image preview stuff and load the stamp at runtime - the stamp must be located in the resources directory
        /// </summary>
        /// <param name="stampPath">Path eg Assets/Resources/Stamps/Mountain1.bytes</param>
        public bool LoadRuntimeStamp(TextAsset stamp)
        {
            //Load stamp
            m_stampHM = new UnityHeightMap(stamp);
            if (!m_stampHM.HasData())
            {
                m_featureID = -1;
                m_stampImage = null;
                Debug.LogError("Was unable to load textasset stamp");
                return false;
            }

            //Get metadata
            float[] metaData = new float[5];
            Buffer.BlockCopy(m_stampHM.GetMetaData(), 0, metaData, 0, metaData.Length * 4);
            m_scanWidth = (int)metaData[0];
            m_scanDepth = (int)metaData[1];
            m_scanHeight = (int)metaData[2];
            m_scanResolution = metaData[3];
            m_settings.m_baseLevel = metaData[4];
            m_scanBounds = new Bounds(transform.position, new Vector3(
               (float)m_scanWidth * m_scanResolution * m_settings.m_width,
               (float)m_scanHeight * m_scanResolution * m_settings.m_height,
               (float)m_scanDepth * m_scanResolution * m_settings.m_width));

            //Invert
            if (m_invertStamp)
            {
                m_stampHM.Invert();
            }

            //Normalise
            if (m_normaliseStamp)
            {
                m_stampHM.Normalise();
            }

            //We are good
            return true;
        }

        /// <summary>
        /// Puts the contents of the heightmap into the stamp image
        /// </summary>
        public void UpdateStampImageFromHeightmap()
        {
            m_stampImage = m_stampHM.ToTexture();
        }

        /// <summary>
        /// Invert the stamp
        /// </summary>
        public void InvertStamp()
        {
            m_stampHM.Invert();
            //GeneratePreviewMesh();
        }

        /// <summary>
        /// Normalise the stamp - makes stamp use full dynamic range - particularly usefule for stencil
        /// </summary>
        public void NormaliseStamp()
        {
            m_stampHM.Normalise();
            //GeneratePreviewMesh();
        }

        /// <summary>
        /// Executes the smoothing algorithm with the number of times stored in m_smoothIterations
        /// </summary>
        public void SmoothStamp()
        {
            m_stampHM.Smooth(m_smoothIterations);
        }


        public Terrain GetCurrentTerrain()
        {
            Terrain currentTerrain = Gaia.TerrainHelper.GetTerrain(transform.position, m_settings.m_isWorldmapStamper);
            //Check if the stamper is over a terrain currently
            //if not, we will stamp based on the last active terrain we were over
            //if that is null either we can't stamp at all
            if (currentTerrain)
            {
                //Update last active terrain with current
                //m_lastActiveTerrain = currentTerrain;
                m_lastActiveTerrainSize = currentTerrain.terrainData.size.x;
            }
            //if not, we check if there is any terrain within the bounds of the stamper
            if (currentTerrain == null)
            {
                float width = m_settings.m_width * 2f;
                Bounds stamperBounds = new Bounds(transform.position, new Vector3(width, width, width));

                foreach (Terrain t in Terrain.activeTerrains)
                {
                    //only look at this terrain if it matches the selected world map mode
                    if (m_settings.m_isWorldmapStamper == TerrainHelper.IsWorldMapTerrain(t))
                    {
                        Bounds worldSpaceBounds = t.terrainData.bounds;
                        worldSpaceBounds.center = new Vector3(worldSpaceBounds.center.x + t.transform.position.x, worldSpaceBounds.center.y + t.transform.position.y, worldSpaceBounds.center.z + t.transform.position.z);

                        if (worldSpaceBounds.Intersects(stamperBounds))
                        {
                            currentTerrain = t;
                            break;
                        }
                    }
                }
            }

            return currentTerrain;
        }

        /// <summary>
        /// Stamp the stamp - will kick the co-routine off
        /// </summary>
        /// <param name="validTerrainNames">The terrain names valid for applying this stamp. If no list is supplied, all terrains in the stamp area are automatically valid.</param>
        public void Stamp(List<string> validTerrainNames = null)
        {
            GaiaStopwatch.StartEvent("Stamping");
            Terrain currentTerrain = GetCurrentTerrain();
            
            if (currentTerrain == null)
            {
                Debug.LogWarning("The stamper could not find a terrain for stamping! Please move the stamper over an active terrain to stamp!" +
                           "+");
                return;
            }

            //CreateUndo();

            GaiaMultiTerrainOperation operation = new GaiaMultiTerrainOperation(currentTerrain, transform, GetStamperRange(currentTerrain), true, validTerrainNames);
            operation.m_isWorldMapOperation = m_settings.m_isWorldmapStamper;
            RenderTexture result = ApplyBrush(operation);
            operation.SetHeightmap(result);

            m_lastAffectedTerrains = operation.affectedHeightmapData;
            //Clean up
            operation.CloseOperation();
            operation = null;
            //Force stamp refresh, since the terrain has changed now
            m_stampDirty = true;
            m_hasStamped = true;
            StartEditorUpdates();
            GaiaStopwatch.EndEvent("Stamping");
        }

        public void HighlightLoadingSettings()
        {
            m_highlightLoadingSettings = true;
            m_highlightLoadingSettingsStartedTimeStamp = GaiaUtils.GetUnixTimestamp();
        }

        public float GetStamperRange(Terrain currentTerrain)
        {

            if (currentTerrain == null)
            {
                return 100;
            }

            //limit the width to not overstep the max internal resolution value of the stamper preview
            float range = Mathf.RoundToInt(Mathf.Clamp(m_settings.m_width, 1, GetMaxStamperRange(currentTerrain)));
            float widthfactor = currentTerrain.terrainData.size.x / 100f;
            
            return range * widthfactor;
        }

        public float GetMaxStamperRange(Terrain currentTerrain)
        {
            if (currentTerrain == null)
            {
                return 100;
            }
            return (float)11574 / (float)currentTerrain.terrainData.heightmapResolution * 100;
        }

        public void SyncHeightmaps()
        {
            foreach (Terrain t in Terrain.activeTerrains)
            {
                t.terrainData.SyncHeightmap();
            }
        }


        /// <summary>
        /// Cause any active stamp to cancel itself - the tidy up will happen in the enumerator
        /// </summary>
        public void CancelStamp()
        {
            m_cancelStamp = true;
        }

        /// <summary>
        /// Returns true if we are currently in process of stamping
        /// </summary>
        /// <returns>True if stamping, false otherwise</returns>
        public bool IsStamping()
        {
            return (m_stampComplete != true);
        }


        /// <summary>
        /// Resets the stamper back to default values
        /// </summary>
        public void Reset()
        {
            if (m_settings == null)
            {
                m_settings = ScriptableObject.CreateInstance<StamperSettings>();
            }


            m_stampID = Guid.NewGuid().ToString();
            m_stampImage = null;
            m_stampImageGUID = "";
            m_settings.m_x = 0f;
            m_settings.m_y = 50f;
            m_settings.m_z = 0f;
            m_settings.m_width = 100f;
            m_settings.m_height = 10f;
            m_settings.m_rotation = 0f;
            m_invertStamp = false;
            m_normaliseStamp = false;
            m_settings.m_baseLevel = 0f;
            m_settings.m_drawStampBase = true;
            m_settings.m_adaptiveBase = false;
            m_settings.m_operation = GaiaConstants.FeatureOperation.RaiseHeight;
            m_smoothIterations = 0;
            m_blendStrength = 0.5f; //The strength of the stamp if blending
            m_stencilHeight = 1f;
            m_heightModifier = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            m_distanceMask = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));
            m_distanceMaskInfluence = GaiaConstants.MaskInfluence.OnlyStampItself;
            m_MaskTexturesDirty = true;
            m_areaMaskMode = GaiaConstants.ImageFitnessFilterMode.None;
            m_imageMask = null;
            m_imageMaskGUID = "";
            m_areaMaskInfluence = GaiaConstants.MaskInfluence.OnlyStampItself;
            m_imageMaskInvert = false;
            m_imageMaskNormalise = false;
            m_imageMaskFlip = false;
            m_imageMaskSmoothIterations = 0;
            m_noiseMaskSeed = 0;
            m_noiseMaskOctaves = 8;
            m_noiseMaskPersistence = 0.25f;
            m_noiseMaskFrequency = 1f;
            m_noiseMaskLacunarity = 1.5f;
            m_noiseZoom = 10f;
            m_syncHeightmaps = true;
            m_stampDirty = true;
            m_settings.m_contrastFeatureSize = 1;
            m_settings.m_contrastStrength = 1;
            m_settings.m_terraceCount = 10f;
            m_settings.m_terraceJitterCount = 0.5f;
            m_settings.m_terraceBevelAmountInterior = 0.5f;
            m_settings.m_sharpenRidgesMixStrength = 0.5f;
            m_settings.m_sharpenRidgesIterations = 16f;

        }

        /// <summary>
        /// Update the stamp incase of movement etc
        /// </summary>
        /// <param name="newPosition">New location</param>
        public void UpdateStamp()
        {

            //Update location

            //Vector3Double origin = SessionManager.GetOrigin();
            ////Stamper settings are stored as absolute value in world space, so we need to deduct the current origin shift
            //transform.position = new Vector3Double(m_settings.m_x - origin.x, m_settings.m_y, m_settings.m_z - origin.z);

            ////Update scales and rotation
            transform.localScale = new Vector3(m_settings.m_width, m_settings.m_height, m_settings.m_width);
            transform.localRotation = Quaternion.AngleAxis(m_settings.m_rotation, Vector3.up);

            ////Update bounds
            //m_scanBounds.center = transform.position;
            //m_scanBounds.size = new Vector3(
            //   (float)m_scanWidth * m_scanResolution * m_settings.m_width,
            //   (float)m_scanHeight * m_scanResolution * m_settings.m_height,
            //   (float)m_scanDepth * m_scanResolution * m_settings.m_width);

            //if (m_stampHM != null)
            //{
            //    m_stampHM.SetBoundsWU(m_scanBounds);
            //}

            //Mark Erosion textures as dirty, since the stamp has been moved
            //they need to be re-evaluated
            //m_stampDirty = true;


            //Set transform changed to false to stop looped updates in OnDrawGizmos
            transform.hasChanged = false;
        }

        /// <summary>
        /// Align the stamp to ground setting - will need to be followed by an UpdateStamp call
        /// </summary>
        public void AlignToGround()
        {
            Terrain currentTerrain = GetCurrentTerrain();
            if (currentTerrain != null)
            {
                transform.position = new Vector3(transform.position.x, currentTerrain.transform.position.y, transform.position.z);
            }
            else
            {
                //No terrain, default to 0
                transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
            }
        }

        /// <summary>
        /// Loads Settings into the stamper.
        /// </summary>
        /// <param name="settingsToLoad">The settings to load</param>
        /// <param name="instantiateNewSettings">Whether the settings should be instantiated as new object on load - if not, this stamper will modify the original scriptable object that was loaded in!</param>
        public void LoadSettings(StamperSettings settingsToLoad, bool instantiateNewSettings = true)
        {
            m_settings.ClearImageMaskTextures();
            transform.position = new Vector3Double(settingsToLoad.m_x, settingsToLoad.m_y, settingsToLoad.m_z);
            transform.rotation = Quaternion.Euler(0f, settingsToLoad.m_rotation, 0f);
            transform.localScale = new Vector3(settingsToLoad.m_width, settingsToLoad.m_height, settingsToLoad.m_width);
            if (instantiateNewSettings)
            {
                m_settings = Instantiate(settingsToLoad);
            }
            else
            {
                m_settings = settingsToLoad;
                
            }

            //Try to look up all collision layer masks by their name where possible - layer orders could be different from when the stamper was saved.
            foreach (ImageMask imageMask in m_settings.m_imageMasks.Where(x => x.m_operation == ImageMaskOperation.CollisionMask))
            {
                imageMask.TryRefreshCollisionMask();
            }
        }

        /// <summary>
        /// Gte the height range for this stamp
        /// </summary>
        /// <param name="minHeight">Base level for this stamp</param>
        /// <param name="minHeight">Minimum height</param>
        /// <param name="maxHeight">Maximum height</param>
        /// <returns>True if stamp had data, false otherwise</returns>
        public bool GetHeightRange(ref float baseLevel, ref float minHeight, ref float maxHeight)
        {
            if (m_stampHM == null || !m_stampHM.HasData())
            {
                return false;
            }

            baseLevel = m_settings.m_baseLevel;
            m_stampHM.GetHeightRange(ref minHeight, ref maxHeight);
            return true;
        }

        /// <summary>
        /// Position and fit the stamp perfectly to the terrain - will need to be followed by an UpdateStamp call
        /// </summary>
        public void FitToTerrain(Terrain t =null)
        {
            if (t == null)
            {
                t = Gaia.TerrainHelper.GetTerrain(transform.position, m_settings.m_isWorldmapStamper);
            }
            if (t == null)
            {
                t = Gaia.TerrainHelper.GetActiveTerrain();
            }
            if (t == null)
            {
                return;
            }
            Bounds b = new Bounds();
            if (Gaia.TerrainHelper.GetTerrainBounds(t, ref b))
            {
                //m_height = (b.size.y / 100f) * 2f;
                //if (m_stampHM != null && m_stampHM.HasData() != false)
                //{
                //    m_width = (b.size.x / (float)m_stampHM.Width()) * 10f;
                //}
                //else
                //{
                //    m_width = m_height;
                //}
                //
                m_settings.m_width = b.size.x / t.terrainData.size.x * 100f;
                m_settings.m_height = 5f;//t.terrainData.size.y / 400f;

                transform.localScale = new Vector3(m_settings.m_width, m_settings.m_height, m_settings.m_width);
                //
                m_settings.m_x = b.center.x;
                m_settings.m_y = t.transform.position.y;
                m_settings.m_z = b.center.z;

                transform.position = new Vector3Double(m_settings.m_x, m_settings.m_y, m_settings.m_z);
                transform.rotation = new Quaternion();

                m_stampDirty = true;
                DrawStampPreview();


            }
        }

        /// <summary>
        /// Check if the stamp has been fit to the terrain - ignoring height
        /// </summary>
        /// <returns>True if its a match</returns>
        public bool IsFitToTerrain()
        {
            Terrain t = Gaia.TerrainHelper.GetTerrain(transform.position);
            if (t == null)
            {
                t = Terrain.activeTerrain;
            }
            if (t == null || m_stampHM == null || m_stampHM.HasData() == false)
            {
                Debug.LogError("Could not check if fit to terrain - no terrain present");
                return false;
            }

            Bounds b = new Bounds();
            if (TerrainHelper.GetTerrainBounds(t, ref b))
            {
                float width = (b.size.x / (float)m_stampHM.Width()) * 10f;
                float x = b.center.x;
                float z = b.center.z;
                float rotation = 0f;

                if (
                    width != m_settings.m_width ||
                    x != m_settings.m_x ||
                    z != m_settings.m_z ||
                    rotation != m_settings.m_rotation)
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
        /// Set up this stamper with a default set of masks for base terrain stamping 
        /// </summary>
        public void SetBaseTerrainStandardMasks()
        {
            m_settings.m_imageMasks = new ImageMask[5]
            {
                new ImageMask() { m_operation = ImageMaskOperation.NoiseMask},
                new ImageMask() { m_operation = ImageMaskOperation.NoiseMask},
                new ImageMask() { m_operation = ImageMaskOperation.NoiseMask},
                new ImageMask() { m_operation = ImageMaskOperation.DistanceMask},
                new ImageMask() { m_operation = ImageMaskOperation.Smooth}
            };
        }

        /// <summary>
        /// Serialise this as json
        /// </summary>
        /// <returns></returns>
        //        public string SerialiseJson()
        //        {
        //            //Grab the various paths
        //#if UNITY_EDITOR
        //            //m_resourcesPath = AssetDatabase.GetAssetPath(m_resources);
        //#endif

        //            fsData data;
        //            fsSerializer serializer = new fsSerializer();
        //            serializer.TrySerialize(this, out data);
        //            return fsJsonPrinter.CompressedJson(data);
        //        }

        //        /// <summary>
        //        /// Deserialise the suplied json into this object
        //        /// </summary>
        //        /// <param name="json">Source json</param>
        //        public void DeSerialiseJson(string json)
        //        {
        //            fsData data = fsJsonParser.Parse(json);
        //            fsSerializer serializer = new fsSerializer();
        //            var stamper = this;
        //            serializer.TryDeserialize<Stamper>(data, ref stamper);
        //#if UNITY_EDITOR
        //            //stamper.m_resources = GaiaUtils.GetAsset(m_resourcesPath, typeof(Gaia.GaiaResource)) as Gaia.GaiaResource;
        //            if (m_imageMask != null)
        //            {
        //                if (m_imageMask.width == 0 && m_imageMask.height == 0)
        //                {
        //                    m_imageMask = null;
        //                }
        //            }
        //#endif

        //    stamper.LoadStampByGUID(false);
        //    stamper.LoadImageMaskByGUID(false);
        //    stamper.UpdateStamp();
        //}

        //public void LoadStampByGUID(bool loadHeightmap)
        //{
        //    if (m_stampImageGUID != null && m_stampImageGUID != "")
        //    {
        //        m_stampImage = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_stampImageGUID), typeof(Texture2D)) as Texture2D;
        //    }
        //    if (loadHeightmap)
        //    {
        //        LoadStamp();
        //    }
        //}

        //public void LoadImageMaskByGUID(bool loadHeightmap)
        //{
        //    if (m_imageMaskGUID != null && m_imageMaskGUID != "")
        //    {
        //        m_imageMask = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_imageMaskGUID), typeof(Texture2D)) as Texture2D;
        //    }
        //    if (loadHeightmap)
        //    {
        //        LoadImageMask();
        //    }
        //}
        /// <summary>
        /// Flatten all active terrains
        /// </summary>
        public void FlattenTerrain()
        {
          
            //Get an undo buffer
            m_undoMgr = new GaiaWorldManager(Terrain.activeTerrains, m_settings.m_isWorldmapStamper);
            m_undoMgr.LoadFromWorld();

            //Flatten the world
            m_redoMgr = new GaiaWorldManager(Terrain.activeTerrains, m_settings.m_isWorldmapStamper);
            m_redoMgr.FlattenWorld();
            m_redoMgr = null;
        }

        /// <summary>
        /// Smooth all active terrains
        /// </summary>
        public void SmoothTerrain()
        {

            //Get an undo buffer
            m_undoMgr = new GaiaWorldManager(Terrain.activeTerrains, m_settings.m_isWorldmapStamper);
            m_undoMgr.LoadFromWorld();

            //Flatten the world
            m_redoMgr = new GaiaWorldManager(Terrain.activeTerrains, m_settings.m_isWorldmapStamper);
            m_redoMgr.SmoothWorld();
            m_redoMgr = null;
        }

        ///// <summary>
        ///// Clear trees off all actiove terrains
        ///// </summary>
        //public void ClearTrees()
        //{
        //    //Update the session
        //    AddToSession(GaiaOperation.OperationType.ClearTrees, "Clearing terrain trees");
        //    TerrainHelper.ClearTrees();
        //}

        ///// <summary>
        ///// Clear all the grass off all the terrains
        ///// </summary>
        //public void ClearDetails()
        //{
        //    //Update the session
        //    AddToSession(GaiaOperation.OperationType.ClearDetails, "Clearing terrain details");
        //    TerrainHelper.ClearDetails();
        //}

#endregion

#region Preview methods

        /// <summary>
        /// Return true if we have a preview we can use
        /// </summary>
        /// <returns>True if we can preview</returns>
        public bool CanPreview()
        {
            return (m_stampImage != null);
        }

        /// <summary>
        /// Get current preview state
        /// </summary>
        /// <returns>Current preview state</returns>
        public bool CurrentPreviewState()
        {
            if (m_previewRenderer != null)
            {
                return m_previewRenderer.enabled;
            }
            return false;
        }

        /// <summary>
        /// Show the preview if possible
        /// </summary>
        public void ShowPreview()
        {
            if (m_previewRenderer != null)
            {
                m_previewRenderer.enabled = true;
            }
        }

        /// <summary>
        /// Hide the preview if possible
        /// </summary>
        public void HidePreview()
        {
            if (m_previewRenderer != null)
            {
                m_previewRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Toggle the preview mesh on and off
        /// </summary>
        public void TogglePreview()
        {
            m_drawPreview = !m_drawPreview;
        }

#endregion

#region Undo / Redo methods

        /// <summary>
        /// Whether or not we can undo an operation. Due to memory constraints only one level of undo is supported.
        /// </summary>
        ///// <returns>True if we can undo an operation</returns>
        //public bool CanUndo()
        //{
        //    if (m_undoMgr == null)
        //    {
        //        return false;
        //    }
        //    return true;
        //}

        ///// <summary>
        ///// Create an undo - creating an undo always destroys the redo if one existed
        ///// </summary>
        //public void CreateUndo()
        //{
        //    //Create new undo manager
        //    m_undoMgr = new GaiaWorldManager(Terrain.activeTerrains);
        //    m_undoMgr.LoadFromWorld();

        //    //And destroy the redo manager
        //    m_redoMgr = null;
        //}

        ///// <summary>
        ///// Undo a previous operation if possible - create redo so we can redo the undo
        ///// </summary>
        //public void Undo()
        //{
        //    if (m_undoMgr != null)
        //    {
        //        //Update the session
        //        AddToSession(GaiaOperation.OperationType.StampUndo, "Undoing stamp");

        //        m_redoMgr = new GaiaWorldManager(Terrain.activeTerrains);
        //        m_redoMgr.LoadFromWorld();
        //        m_undoMgr.SaveToWorld(true);
        //    }
        //}

        ///// <summary>
        ///// True if the previous undo can be redone
        ///// </summary>
        ///// <returns>True if a redo is possible</returns>
        //public bool CanRedo()
        //{
        //    if (m_redoMgr == null)
        //    {
        //        return false;
        //    }
        //    return true;
        //}

        ///// <summary>
        ///// Redo a previous operation if possible
        ///// </summary>
        //public void Redo()
        //{
        //    if (m_redoMgr != null)
        //    {
        //        //Update the session
        //        AddToSession(GaiaOperation.OperationType.StampRedo, "Redoing stamp");

        //        m_redoMgr.SaveToWorld(true);
        //        m_redoMgr = null;
        //    }
        //}

#endregion

#region Unity Related Methods

        /// <summary>
        /// Called when the stamp is enabled, loads stamp if necessary
        /// </summary>
        void OnEnable()
        {
            //Check for changed feature and load if necessary
            if (m_stampImage != null)
            {
                LoadStamp();
            }

            if (m_gaiaSettings == null)
            {
                m_gaiaSettings = Gaia.GaiaUtils.GetGaiaSettings();
            }

            m_stampDirty = true;

#if UNITY_EDITOR && GAIA_PRO_PRESENT
            m_Eroder = new HydraulicEroder();
            m_Eroder.OnEnable();
#endif

        }

        public void UpdateTerrainLoader()
        {
#if GAIA_PRO_PRESENT
            if (m_loadTerrainMode != LoadMode.Disabled)
            {
                Terrain currentTerrain = GetCurrentTerrain();
                float sizeFactor = 1;
                if (currentTerrain != null)
                {
                    sizeFactor = currentTerrain.terrainData.size.x / 100f;
                }
                else
                {
                    if (m_lastActiveTerrainSize > 0)
                    {
                        //we have a width from the last active terrain, take this then
                        sizeFactor = m_lastActiveTerrainSize / 100f;
                    }
                    else
                    {
                        //no terrain? try placeholders then
                        TerrainScene terrainScene = Gaia.TerrainHelper.GetDynamicLoadedTerrain(transform.position,SessionManager);
                        if (terrainScene!=null)
                        {
                            sizeFactor = (float)terrainScene.m_bounds.size.x / 100f;
                        }
                        else
                        {
                            //last resort: default to 2048
                            sizeFactor = 2048 / 100f;
                        }
                    }

                }

                float rad = (transform.rotation.eulerAngles.y + 45) * Mathf.Deg2Rad;
                float width = transform.localScale.x * sizeFactor * Mathf.Sqrt(2) * Mathf.Max(Mathf.Abs(Mathf.Cos(rad)), Mathf.Abs(Mathf.Sin(rad)));

                //Overextend the height for the terrain loading a bit on top & bottom
                //If the stamper only loads to the exact height it can be irritating 
                //when you adjust the stamper 1mm above the max terrain height & all terrains unload immediately 
                float height = transform.localScale.y * sizeFactor * 6f;
    
                Vector3 center = transform.position;

                TerrainLoader.m_loadingBoundsRegular.center = center;
                TerrainLoader.m_loadingBoundsRegular.size = new Vector3(width, height, width);
                TerrainLoader.m_loadingBoundsImpostor.center = center;
                if (m_impostorLoadingRange > 0)
                {
                    TerrainLoader.m_loadingBoundsImpostor.size= new Vector3(width + m_impostorLoadingRange, height + m_impostorLoadingRange, width + m_impostorLoadingRange);
                }
                else
                {
                    TerrainLoader.m_loadingBoundsImpostor.size= Vector3.zero;
                }
            }
            //Always set the load mode anew, setting it triggers re-evaluation of the loaded terrains
            TerrainLoader.LoadMode = m_loadTerrainMode;
#endif
        }

        public void UpdateMinMaxHeight()
        {
            SessionManager.GetWorldMinMax(ref m_minCurrentTerrainHeight, ref m_maxCurrentTerrainHeight, m_settings.m_isWorldmapStamper);

            //Update the image masks with the newest terrain values after stamping, so that height masks etc. are updated instantly
            if (m_settings.m_imageMasks.Length > 0)
            {
                foreach (ImageMask mask in m_settings.m_imageMasks)
                {
                    mask.m_seaLevel = m_seaLevel;
                    mask.m_maxWorldHeight = m_maxCurrentTerrainHeight;
                    mask.m_minWorldHeight = m_minCurrentTerrainHeight;
                }

                ImageMask.CheckMaskStackForInvalidTextureRules("Stamper", this.name, m_settings.m_imageMasks);
            }

            //Look for all spawners in the scene and update their heightmaps in case they are over the affected terrain
            Spawner[] spawners = FindObjectsOfType<Spawner>();
            int length = spawners.Length;
            for (int i = 0; i < length; i++)
            {
                spawners[i].UpdateMinMaxHeight();
            }
        }

        public void FitToAllTerrains(bool loadedOnly=false) 
        {
            BoundsDouble b = new BoundsDouble();
            BoundsDouble totalBounds = new BoundsDouble();
            TerrainHelper.GetTerrainBounds(ref totalBounds);
            if (TerrainHelper.GetTerrainBounds(ref b, loadedOnly))
            {
                Terrain terrain = GetCurrentTerrain();
                
                m_settings.m_x = (float)b.center.x;
                if (terrain != null)
                {
                    m_settings.m_y = terrain.transform.position.y;
                    m_settings.m_width = (float)b.size.x / terrain.terrainData.size.x * 100f;
                    m_settings.m_width = Mathf.RoundToInt(Mathf.Clamp(m_settings.m_width, 1, GetMaxStamperRange(terrain)));
                }
                else
                {
#if GAIA_PRO_PRESENT
                    TerrainScene terrainScene = Gaia.TerrainHelper.GetDynamicLoadedTerrain(transform.position, SessionManager);
                    //no terrain? assume placeholders then
                    if (terrainScene != null)
                    {
                        m_settings.m_y = 0;
                        m_settings.m_width = (float)b.size.x / (float)terrainScene.m_bounds.size.x * 100f;
                        m_settings.m_width = Mathf.RoundToInt(Mathf.Clamp(m_settings.m_width, 1, GetMaxStamperRange(terrain)));
                    }
                    else
                    {
#endif
                        //no terrain, no placeholder? Default to arbitary values then
                        m_settings.m_width = 100;
                        m_settings.m_y = 0;
#if GAIA_PRO_PRESENT
                    }
#endif
                }
                m_settings.m_z = (float)b.center.z;
                transform.position = new Vector3Double(m_settings.m_x, m_settings.m_y, m_settings.m_z);
                m_settings.m_height = 5 * (float)b.size.x / (float)totalBounds.size.x;

                transform.localScale = new Vector3(m_settings.m_width, m_settings.m_height, m_settings.m_width);
                transform.rotation = new Quaternion();
                m_stampDirty = true;
                DrawStampPreview();
            }
        }

        private void OnDisable()
        {
            DestroyBakedTextures();
            ClearRTCache();
        }

        private void OnDestroy()
        {
            DestroyBakedTextures();
            ClearRTCache();
#if UNITY_EDITOR && GAIA_PRO_PRESENT
            if(m_Eroder!=null)
            {
                m_Eroder.ReleaseRenderTextures();
                m_Eroder = null;
            }
#endif
            if (m_settings != null)
            {
                m_settings.ClearImageMaskTextures();
            }

        }

        private void ClearRTCache()
        {
            if (m_cachedRenderTexture != null)
            {
                m_cachedRenderTexture.Release();
                m_cachedRenderTexture = null;
            }
        }

        private void DestroyBakedTextures()
        {
            //Clean up baked Texture2Ds
            if (m_distanceMaskCurveTexture != null)
                DestroyImmediate(m_distanceMaskCurveTexture);
            m_distanceMaskCurveTexture = null;
            if (m_transformHeightCurveTexture != null)
                DestroyImmediate(m_transformHeightCurveTexture);
            m_transformHeightCurveTexture = null;
        }


        /// <summary>
        /// Called when app starts
        /// </summary>
        void Start()
        {
            //Hide stamp preview mesh at runtime
            if (Application.isPlaying)
            {
                HidePreview();
            }
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

        //Stop editor updates
        public void StopEditorUpdates()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
#endif
        }

        /// <summary>
        /// This is executed only in the editor - using it to simulate co-routine execution and update execution
        /// </summary>
        void EditorUpdate()
        {
#if UNITY_EDITOR

            if (m_lastHeightmapUpdateTimeStamp + 10 < GaiaUtils.GetUnixTimestamp())
            {
                if (SessionManager.m_session.m_operations.Exists(x => x.sessionPlaybackState == SessionPlaybackState.Queued))
                {
                    GaiaSessionManager.ContinueSessionPlayback();
                }
                else
                {
                    //No session & mass stamp to continue -> destroy the temporary tools, if any
                    if (SessionManager.m_massStamperSettingsList == null || SessionManager.m_massStamperSettingsIndex >= SessionManager.m_massStamperSettingsList.Count - 1)
                    {
                        GaiaSessionManager.AfterSessionPlaybackCleanup();
                    }
                }
                //if (SessionManager.m_massStamperSettingsList != null)
                //{
                //    if (SessionManager.m_massStamperSettingsIndex < SessionManager.m_massStamperSettingsList.Count - 1)
                //    {
                //        SessionManager.ContinueMassStamp();
                //    }
                //    else
                //    {
                //        SessionManager.m_massStamperSettingsIndex = int.MaxValue;
                //        SessionManager.m_massStamperSettingsList = null;
                //    }
                //}
                StopEditorUpdates();
            }
             

            //if (m_updateCoroutine == null)
            //{
            //    StopEditorUpdates();
            //    return;
            //}
            //else
            //{
            //    if (EditorWindow.mouseOverWindow != null)
            //    {
            //        m_updateTimeAllowed = 1 / 30f;
            //    }
            //    else
            //    {
            //        m_updateTimeAllowed = 1 / 2f;
            //    }
            //    m_updateCoroutine.MoveNext();
            //}
#endif
        }

        public void DrawStampPreview()
        {
            if (!m_drawPreview)
            {
                return;
            }

            if (transform.position.x != m_settings.m_x)
            {
                m_settings.m_x = transform.position.x;
                m_stampDirty = true;
            }
            if (transform.position.y != m_settings.m_y)
            {
                m_settings.m_y = transform.position.y;
                m_stampDirty = true;
            }
            if (transform.position.z != m_settings.m_z)
            {
                m_settings.m_z = transform.position.z;
                m_stampDirty = true;
            }

            if (m_lastScaleChangeWasX)
            {
                if (transform.localScale.x != m_settings.m_width)
                {
                    m_settings.m_width = transform.localScale.x;
                    m_stampDirty = true;
                    m_lastScaleChangeWasX = true;
                }
                else
                {
                    if (transform.localScale.z != m_settings.m_width)
                    {
                        m_settings.m_width = transform.localScale.z;
                        m_stampDirty = true;
                        m_lastScaleChangeWasX = false;
                    }
                }
            }
            else
            {
                if (transform.localScale.z != m_settings.m_width)
                {
                    m_settings.m_width = transform.localScale.z;
                    m_stampDirty = true;
                    m_lastScaleChangeWasX = false;
                }
                else
                {
                    if (transform.localScale.x != m_settings.m_width)
                    {
                        m_settings.m_width = transform.localScale.x;
                        m_stampDirty = true;
                        m_lastScaleChangeWasX = true;
                    }
                }
            }


            if (transform.rotation.eulerAngles.y != m_settings.m_rotation)
            {
                m_settings.m_rotation = transform.rotation.eulerAngles.y;
                m_stampDirty = true;
            }
            if (transform.localScale.y != m_settings.m_height)
            {
                if (transform.localScale.y > 0f)
                {
                    m_settings.m_height = transform.localScale.y;
                    m_stampDirty = true;
                }
            }
            UpdateStamp();
#if UNITY_EDITOR
                //SceneView.RepaintAll();
#endif
            //}

            Terrain currentTerrain = GetCurrentTerrain(); ;
            if (currentTerrain == null)
            {
                return;
            }

            //Check if the stamper is over a terrain currently
            //if not, we will draw a preview based on the last active terrain we were over
            //if that is null either we can't draw a stamp preview
            //if (currentTerrain)
            //{
            //    //Update last active terrain with current
            //    m_lastActiveTerrain = currentTerrain;
            //}
            //else
            //{
            //    if (m_lastActiveTerrain)
            //        currentTerrain = m_lastActiveTerrain;
            //    else
            //        return;
            //}

            Material material = GaiaMultiTerrainOperation.GetDefaultGaiaStamperPreviewMaterial();


            //Switch zTest mode according to operation mode
            switch (m_settings.m_operation)
            {
                case GaiaConstants.FeatureOperation.LowerHeight:
                    material.SetInt("_zTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                    break;
                case GaiaConstants.FeatureOperation.SetHeight:
                    material.SetInt("_zTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                    break;
                case GaiaConstants.FeatureOperation.BlendHeight:
                    material.SetInt("_zTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                    break;
                case GaiaConstants.FeatureOperation.SubtractHeight:
                    material.SetInt("_zTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                    break;
                case GaiaConstants.FeatureOperation.HydraulicErosion:
                    material.SetInt("_zTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                    break;
                case GaiaConstants.FeatureOperation.Contrast:
                    material.SetInt("_zTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                    break;
                case GaiaConstants.FeatureOperation.Terrace:
                    material.SetInt("_zTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                    break;
                case GaiaConstants.FeatureOperation.SharpenRidges:
                    material.SetInt("_zTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                    break;
                case GaiaConstants.FeatureOperation.HeightTransform:
                    material.SetInt("_zTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                    break;
                case GaiaConstants.FeatureOperation.PowerOf:
                    material.SetInt("_zTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                    break;
                case GaiaConstants.FeatureOperation.Smooth:
                    material.SetInt("_zTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                    break;
                case GaiaConstants.FeatureOperation.MixHeight:
                    material.SetInt("_zTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                    break;
                default:
                    material.SetInt("_zTestMode", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
                    break;
            }

            //TerrainPaintUtilityEditor.DrawBrushPreview(ctx, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, m_stamper.m_stampHighQualityImage, brushXform, material, 0);

            // draw result preview
            {
                GaiaMultiTerrainOperation operation = new GaiaMultiTerrainOperation(currentTerrain, transform, GetStamperRange(currentTerrain));
                operation.m_isWorldMapOperation = m_settings.m_isWorldmapStamper;
                RenderTexture preview = ApplyBrush(operation);

                //restore old render target
                //RenderTexture.active = ctx.oldRenderTexture;

                material.SetTexture("_HeightmapOrig", operation.RTheightmap);
                
                material.SetColor("_positiveHeightColor", m_gaiaSettings.m_stamperPositiveHeightChangeColor);
                material.SetColor("_negativeHeightColor", m_gaiaSettings.m_stamperNegativeHeightChangeColor);

                //Make the sea level color fully transparent if sea level is deactivated
                Color seaLevelColor = m_gaiaSettings.m_stamperSeaLevelTintColor;
                if (!m_showSeaLevelinStampPreview)
                {
                    seaLevelColor.a = 0f;
                }
                material.SetColor("_seaLevelTintColor", seaLevelColor);
                material.SetFloat("_normalMapColorPower", m_gaiaSettings.m_stamperNormalMapColorPower);
                //float relativeSeaLevel = Mathf.Lerp(-0.5f, 0.5f, Mathf.InverseLerp(currentTerrain.transform.position.y - currentTerrain.terrainData.size.y, currentTerrain.transform.position.y + currentTerrain.terrainData.size.y, m_seaLevel));
                material.SetFloat("_seaLevel", m_seaLevel);
                operation.Visualize(MultiTerrainOperationType.Heightmap, preview, material, 1);

                //Clean up
                operation.CloseOperation();
                //GaiaUtils.ReleaseAllTempRenderTextures();

            }

           

           

        }

        public void SetStampScaleByMeter(float absoluteHeightValue)
        {
            Terrain currentTerrain = GetCurrentTerrain();
            if (currentTerrain == null)
            {
                return;
            }
            transform.position = new Vector3(transform.position.x, currentTerrain.transform.position.y, transform.position.z);
            transform.localScale = new Vector3(transform.localScale.x, m_settings.GetStampScaleByMeter(currentTerrain, absoluteHeightValue), transform.localScale.z) ; 
        }

        public float CurrentStampScaleToMeter()
        {
            Terrain currentTerrain = GetCurrentTerrain();
            if (currentTerrain == null)
            {
                return 0;
            }
            float heightDifference = 0f;
            if (m_settings.m_operation == GaiaConstants.FeatureOperation.SubtractHeight)
            {
                heightDifference = currentTerrain.transform.position.y - transform.position.y;
                return Mathf.Lerp(0, -currentTerrain.terrainData.size.y, Mathf.InverseLerp(0, 50f, transform.localScale.y) + Mathf.InverseLerp(0, currentTerrain.terrainData.size.y, heightDifference));
            }
            else
            {
                heightDifference = transform.position.y - currentTerrain.transform.position.y;
                return Mathf.Lerp(0, currentTerrain.terrainData.size.y, Mathf.InverseLerp(0, 50f, transform.localScale.y) + Mathf.InverseLerp(0, currentTerrain.terrainData.size.y, heightDifference));
            }
            
        }

        /// <summary>
        /// Draw gizmos when selected
        /// </summary>
        void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            if (Selection.activeObject == this.gameObject)
            {
                DrawGizmos(true);
            }
#endif
        }

        /// <summary>
        /// Draw gizmos when not selected
        /// </summary>
        void OnDrawGizmos()
        {
            //DrawGizmos(false);
        }

        /// <summary>
        /// Draw the gizmos
        /// </summary>
        /// <param name="isSelected"></param>
        void DrawGizmos(bool isSelected)
        {

            Terrain activeTerrain = TerrainHelper.GetTerrain(transform.position,m_settings.m_isWorldmapStamper);

            if (activeTerrain == null)
            {
                return;
            }

            //Determine whether to drop out
            if (!isSelected && !m_alwaysShow)
            {
                return;
            }

            //Now draw the gizmos

            //Stamp Bounding Box
            if (m_showBoundingBox)
            {
                Bounds bounds = new Bounds();
                Gizmos.color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, Color.cyan.a / 2f);
                
                float yPos = activeTerrain.transform.position.y + (activeTerrain.terrainData.size.y / 2f);

                //The final gizmo will be drawn in LOCAL SPACE of the stamp to inherit its rotation
                //we therefore need to offset the scale and ypos of the stamp itself

                //offset ypos of the stamper
                yPos -= transform.position.y - activeTerrain.transform.position.y;

                //offset stamper height scale
                yPos /= transform.localScale.y;

                bounds.center = new Vector3(0f, yPos, 0f);
                //size will be scaled automatically due to stamp scaling
                bounds.size = new Vector3(activeTerrain.terrainData.size.x / 100f, activeTerrain.terrainData.size.y / transform.localScale.y, activeTerrain.terrainData.size.x / 100f);
                Matrix4x4 currMatrixBoundingBox = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
                Gizmos.matrix = currMatrixBoundingBox;
            }


            //Base Level - only draw when enabled & larger than 0
            if (m_showBase && OperationSupportsBaseSettings())
            {
                //Displaying the base level can be irrelevant when it is all at the bottom for a raise height operation, and vice versa for lowering height.
                //In this case we can just hide it, as it just would add visual clutter.
                bool relevant = true;
                if (m_settings.m_operation == GaiaConstants.FeatureOperation.RaiseHeight || m_settings.m_operation == GaiaConstants.FeatureOperation.AddHeight)
                {
                    if (m_settings.m_baseLevel == 0)
                    {
                        relevant = false;
                    }
                }
                if (m_settings.m_operation == GaiaConstants.FeatureOperation.LowerHeight || m_settings.m_operation == GaiaConstants.FeatureOperation.SubtractHeight)
                {
                    if (m_settings.m_baseLevel == 1)
                    {
                        relevant = false;
                    }
                }

                if (relevant)
                {

                    Bounds bounds = new Bounds();
                    Gizmos.color = new Color(Color.red.r, Color.red.g, Color.red.b, Color.red.a / 2f);
                    //yPos in world space:
                    float yPos = Mathf.Lerp(activeTerrain.transform.position.y, activeTerrain.transform.position.y + activeTerrain.terrainData.size.y, m_settings.m_baseLevel);

                    //The final gizmo will be drawn in LOCAL SPACE of the stamp to inherit its rotation
                    //we therefore need to offset the scale and ypos of the stamp itself

                    //offset ypos of the stamper
                    yPos -= transform.position.y - activeTerrain.transform.position.y;

                    //offset stamper height scale
                    yPos /= m_settings.m_height;



                    bounds.center = new Vector3(0, yPos, 0);
                    //size will be scaled automatically due to stamp scaling
                    bounds.size = new Vector3(activeTerrain.terrainData.size.x / 90f, 0.05f, activeTerrain.terrainData.size.x / 90f);
                    Matrix4x4 currMatrixBaseLevel = Gizmos.matrix;
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(bounds.center, bounds.size);
                    Gizmos.matrix = currMatrixBaseLevel;

                    //if (TerrainHelper.GetTerrainBounds(ref bounds) == true)
                    //{
                    //    //bounds.center = new Vector3(bounds.center.x, m_scanBounds.min.y + (m_scanBounds.size.y * m_baseLevel), bounds.center.z);
                    //    //bounds.size = new Vector3(bounds.size.x, 0.05f, bounds.size.z);
                    //    //Gizmos.color = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, Color.yellow.a / 2f);
                    //    //Gizmos.DrawWireCube(bounds.center, bounds.size);


                    //}
                }
            }

            //Water


            //Check if sea level changed
            if (m_seaLevel != SessionManager.GetSeaLevel(m_settings.m_isWorldmapStamper))
            {
                m_seaLevel = SessionManager.GetSeaLevel(m_settings.m_isWorldmapStamper);
                m_stampDirty = true;
            }
            if (m_showSeaLevelPlane && PWS_WaterSystem.Instance == null)
            {
                BoundsDouble bounds = new BoundsDouble();
                if (m_settings.m_isWorldmapStamper)
                {
                    bounds.center = activeTerrain.terrainData.bounds.center;
                    bounds.extents = activeTerrain.terrainData.bounds.extents;
                    //bounds need to be in world space + use the shifted origin
                    bounds.center = transform.position;
                }
                else
                {
                    TerrainHelper.GetTerrainBounds(ref bounds);
                    bounds.center = new Vector3Double(bounds.center.x, m_seaLevel, bounds.center.z);
                    bounds.size = new Vector3Double(bounds.size.x, 0.05f, bounds.size.z);
                }
                if (isSelected)
                {
                    Gizmos.color = new Color(Color.blue.r, Color.blue.g, Color.blue.b, Color.blue.a / 4f);
                    Gizmos.DrawCube(bounds.center, bounds.size);
                }
                else
                {
                    Gizmos.color = new Color(Color.blue.r, Color.blue.g, Color.blue.b, Color.blue.a / 4f);
                    Gizmos.DrawCube(bounds.center, bounds.size);
                }
            
            }

            //Stamp box
            //Gizmos.color = Color.magenta;
            //Gizmos.DrawWireCube(new Vector3(m_x, 0f, m_z), new Vector3(m_width, m_height, m_width));


            //Rulers
            if (m_showRulers)
            {
                DrawRulers();
            }

            //Rotation n size
            Matrix4x4 currMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3 origSize = new Vector3(
                (float)m_scanWidth * m_scanResolution,
                (float)m_scanHeight * m_scanResolution,
                (float)m_scanDepth * m_scanResolution);
            Gizmos.color = new Color(m_gizmoColour.r, m_gizmoColour.g, m_gizmoColour.b, m_gizmoColour.a / 2f);
            Gizmos.DrawWireCube(Vector3.zero, origSize);
            Gizmos.matrix = currMatrix;

            //Terrain bounds
            if (activeTerrain != null)
            {
                Gizmos.color = Color.white;
                Bounds b = new Bounds();
                Gaia.TerrainHelper.GetTerrainBounds(activeTerrain, ref b);
                Gizmos.DrawWireCube(b.center, b.size);
            }

        }

        


        //private List<string> GetLocalMapNames()
        //{
        //    if (GaiaUtils.HasDynamicLoadedTerrains())
        //    {
        //        return Resources.FindObjectsOfTypeAll<GaiaTerrainPlaceHolder>().Select(x => x.name.Replace("Placeholder ", "")).ToList();
        //    }
        //    else
        //    {
        //        List<string> returnList = new List<string>();
        //        foreach (Terrain t in Terrain.activeTerrains)
        //        {
        //            if (t != m_worldMap)
        //            {
        //                returnList.Add(t.name);
        //            }
        //        }
        //        return returnList;
        //    }
        //}


        //private void SyncTerrainHeightmaps(List<string> sourceTerrains, List<string> targetTerrains)
        //{
        //    if (sourceTerrains == null || targetTerrains == null)
        //    {
        //        return;
        //    }

        //    int sourcePixels = GetTotalHeightmapResolutionPixels(sourceTerrains);
        //    int targetPixels = GetTotalHeightmapResolutionPixels(targetTerrains);
        //}

        //private int GetTotalHeightmapResolutionPixels(List<string> terrainNames)
        //{
        //    if (terrainNames == null)
        //    {
        //        return 0;
        //    }
        //    Bounds bounds = new Bounds();

        //    bool hasPlaceholders = GaiaUtils.HasDynamicLoadedTerrains();

        //    foreach (string name in terrainNames)
        //    {
        //        foreach (Terrain t in Terrain.activeTerrains)
        //        {
        //            if (t.name == name)
        //            {
        //                bounds.Encapsulate(t.terrainData.bounds);
        //                continue;
        //            }
        //        }

        //        //not found in active terrains? need to try to find the placeholder, load the terrain in and encapsulate
        //        if (hasPlaceholders)
        //        {

        //        }
        //    }


        //}


        /// <summary>
        /// Returns true if the current operation supports base settings (for base stamping)
        /// </summary>
        /// <returns></returns>
        private bool OperationSupportsBaseSettings()
        {
            return m_settings.m_operation == GaiaConstants.FeatureOperation.AddHeight ||
                m_settings.m_operation == GaiaConstants.FeatureOperation.SubtractHeight ||
                m_settings.m_operation == GaiaConstants.FeatureOperation.RaiseHeight ||
                m_settings.m_operation == GaiaConstants.FeatureOperation.LowerHeight;
        }

        /// <summary>
        /// Draw the rulers
        /// </summary>
        void DrawRulers()
        {
#if UNITY_EDITOR
            if (m_showRulers)
            {
                Gizmos.color = Color.green;

                //Ruler gizmos
                int ticks;
                float tickOffset;
                float tickInterval = 100f;
                float vertRulerSize = m_scanBounds.max.y - m_scanBounds.min.y;
                float horizRulerSize = m_scanBounds.max.x - m_scanBounds.min.x;
                Vector3 startPosition;
                Vector3 endPosition;
                Vector3 labelPosition;

                //Vertical ruler
                startPosition = m_scanBounds.center;
                startPosition.y = m_scanBounds.min.y;
                endPosition = m_scanBounds.center;
                endPosition.y = m_scanBounds.max.y;
                labelPosition = startPosition;
                labelPosition.x += 5f;
                labelPosition.y += 2f;
                Gizmos.DrawLine(startPosition, endPosition);

                ticks = Mathf.RoundToInt(vertRulerSize / tickInterval);
                tickOffset = vertRulerSize / (float)ticks;
                for (int i = 0; i <= ticks; i++)
                {
                    Handles.Label(labelPosition, string.Format("{0:0m}", labelPosition.y));
                    labelPosition.y += tickOffset;
                }

                //Horizontal ruler - x axis
                startPosition = m_scanBounds.center;
                startPosition.x = m_scanBounds.min.x;
                endPosition = m_scanBounds.center;
                endPosition.x = m_scanBounds.max.x;
                labelPosition = startPosition;
                labelPosition.x += 5f;
                labelPosition.y += 2f;
                Gizmos.DrawLine(startPosition, endPosition);

                ticks = Mathf.RoundToInt(horizRulerSize / tickInterval);
                tickOffset = horizRulerSize / (float)ticks;
                for (int i = 0; i <= ticks; i++)
                {
                    Handles.Label(labelPosition, string.Format("{0:0m}", labelPosition.x));
                    labelPosition.x += tickOffset;
                }
            }
#endif
        }

#endregion

#region Private worker methods


        
        /// <summary>
        /// Applies the current image masks and creates a render texture containing the result
        /// </summary>
        /// <returns>A rendertexture with the mask results/returns>
        private RenderTexture ApplyBrush(GaiaMultiTerrainOperation operation)
        {
            //Try to get the current terrain the stamper is over, if there is no terrain below, let's take the last cached one instead for our calculations.
            Terrain currentTerrain = Gaia.TerrainHelper.GetTerrain(transform.position, m_settings.m_isWorldmapStamper);
            //if (currentTerrain == null)
            //{
            //    currentTerrain = m_cachedTerrain;
            //}
            //else
            //{
            //    m_cachedTerrain = currentTerrain;
            //}

            //Still no terrain? -> Abort            
            if (currentTerrain == null)
            {
                Debug.LogWarning("The Gaia Stamper could find no terrain to work on - Please position the stamper above an active terrain!");
                return null;
            }
            operation.GetHeightmap();
            if (m_stampDirty)
            {
                operation.GetNormalmap();
                operation.CollectTerrainBakedMasks();
                RenderTexture returnRT = ApplyBrushInternal(operation);

                if (m_worldMapStampToken != null)
                {
                    if (!m_settings.m_isWorldmapStamper && m_worldMapStampToken.m_previewOnWorldMap)
                    {
                        m_worldMapStampToken.ReloadWorldStamper();
                    }
                }

                return returnRT;
            }
            else
            {
                return m_cachedRenderTexture;
            }
        }

        public RenderTexture ApplyBrushInternal(GaiaMultiTerrainOperation operation)
        {
            RenderTexture currentRT = RenderTexture.active;
            if (m_stampDirty || m_cachedRenderTexture == null)
            {


                //Process the filters so that we receive our final brush texture
                //Create tow separate input textures for global and local
                RenderTextureDescriptor rtDescriptor = operation.RTheightmap.descriptor;
                //Random write needs to be enabled for certain mask types to function!
                rtDescriptor.enableRandomWrite = true;
                rtDescriptor.colorFormat = RenderTextureFormat.RFloat;

                RenderTexture localinputTexture = RenderTexture.GetTemporary(rtDescriptor);
                RenderTexture globalInputTexture = RenderTexture.GetTemporary(rtDescriptor);
                RenderTexture outputTexture = RenderTexture.GetTemporary(rtDescriptor); ;


                if (GaiaUtils.IsStampOperation(m_settings.m_operation) && m_settings.m_stamperInputImageMask.ImageMaskTexture !=null)
                {
                    RenderTexture tempTexture = RenderTexture.GetTemporary(localinputTexture.descriptor);
                    RenderTexture.active = tempTexture;
                    GL.Clear(true, true, Color.white);
                    m_settings.m_stamperInputImageMask.Apply(tempTexture, localinputTexture);
                    tempTexture.DiscardContents();
                    RenderTexture.ReleaseTemporary(tempTexture);
                    tempTexture = null;
                }
                else
                {
                    RenderTexture.active = localinputTexture;
                    GL.Clear(true, true, Color.white);
                }

                RenderTexture.active = globalInputTexture;
                GL.Clear(true, true, Color.white);
                RenderTexture.active = currentRT;

                //always populate the internal input image mask
                m_settings.m_stamperInputImageMask.m_multiTerrainOperation = operation;
                m_settings.m_stamperInputImageMask.m_seaLevel = m_seaLevel;
                m_settings.m_stamperInputImageMask.m_maxWorldHeight = m_maxCurrentTerrainHeight;
                m_settings.m_stamperInputImageMask.m_minWorldHeight = m_minCurrentTerrainHeight;

                if (m_settings.m_imageMasks.Length > 0)
                {
                    //We start from a white texture, so we need the first mask action in the stack to always be "Multiply", otherwise there will be no result.
                    m_settings.m_imageMasks[0].m_blendMode = ImageMaskBlendMode.Multiply;

                    //Iterate through all image masks and set up the required data that masks might need to function properly
                    foreach (ImageMask mask in m_settings.m_imageMasks)
                    {
                        //mask.m_heightmapContext = heightmapContext;
                        //mask.m_normalmapContext = normalmapContext;
                        //mask.m_collisionContext = collisionContext;
                        mask.m_multiTerrainOperation = operation;
                        mask.m_seaLevel = m_seaLevel;
                        mask.m_maxWorldHeight = m_maxCurrentTerrainHeight;
                        mask.m_minWorldHeight = m_minCurrentTerrainHeight;
                    }
                    ImageMask.CheckMaskStackForInvalidTextureRules("Stamper", this.name, m_settings.m_imageMasks);
                }

                RenderTexture localOutputTexture = RenderTexture.GetTemporary(rtDescriptor);
                RenderTexture globalOutputTexture = RenderTexture.GetTemporary(rtDescriptor);
                localOutputTexture = ImageProcessing.ApplyMaskStack(localinputTexture, localOutputTexture, m_settings.m_imageMasks, ImageMaskInfluence.Local);
                globalOutputTexture = ImageProcessing.ApplyMaskStack(globalInputTexture, globalOutputTexture, m_settings.m_imageMasks, ImageMaskInfluence.Global);
                //Run the 2 output textures through the image mask shader for a simple multiply for the mask preview
                Material multiplyMat = new Material(Shader.Find("Hidden/Gaia/FilterImageMask"));
                multiplyMat.SetTexture("_InputTex", localOutputTexture);
                multiplyMat.SetFloat("_Strength", 1f);
                multiplyMat.SetInt("_Invert", 0);
                multiplyMat.SetTexture("_ImageMaskTex", globalOutputTexture);

                if (m_cachedMaskTexture != null)
                {
                    m_cachedMaskTexture.Release();
                    DestroyImmediate(m_cachedMaskTexture);
                }

                m_cachedMaskTexture = new RenderTexture(rtDescriptor);
                Graphics.Blit(globalInputTexture, m_cachedMaskTexture, multiplyMat, 0);
                multiplyMat.SetTexture("_InputTex", null);
                multiplyMat.SetTexture("_ImageMaskTex", null);
                DestroyImmediate(multiplyMat);

                switch (m_settings.m_operation)
                {
                    case GaiaConstants.FeatureOperation.HydraulicErosion:
#if UNITY_EDITOR && GAIA_PRO_PRESENT
                        operation.RTheightmap.filterMode = FilterMode.Bilinear;
                        Material erosionMat = GetCurrentFXMaterial();
                        m_Eroder.inputTextures["Height"] = operation.RTheightmap;

                        Vector2 texelSize = new Vector2(operation.m_originTerrain.terrainData.size.x / operation.m_originTerrain.terrainData.heightmapResolution,
                                                        operation.m_originTerrain.terrainData.size.z / operation.m_originTerrain.terrainData.heightmapResolution);

                        //apply Erosion settings
                        m_Eroder.m_ErosionSettings.m_SimScale.value = m_settings.m_erosionSimScale;
                        m_Eroder.m_ErosionSettings.m_HydroTimeDelta.value = m_settings.m_erosionHydroTimeDelta;
                        m_Eroder.m_ErosionSettings.m_HydroIterations.value = m_settings.m_erosionHydroIterations;
                        m_Eroder.m_ErosionSettings.m_ThermalTimeDelta = m_settings.m_erosionThermalTimeDelta;
                        m_Eroder.m_ErosionSettings.m_ThermalIterations = m_settings.m_erosionThermalIterations;
                        m_Eroder.m_ErosionSettings.m_ThermalReposeAngle = m_settings.m_erosionThermalReposeAngle;
                        m_Eroder.m_ErosionSettings.m_PrecipRate.value = m_settings.m_erosionPrecipRate;
                        m_Eroder.m_ErosionSettings.m_EvaporationRate.value = m_settings.m_erosionEvaporationRate;
                        m_Eroder.m_ErosionSettings.m_FlowRate.value = m_settings.m_erosionFlowRate;
                        m_Eroder.m_ErosionSettings.m_SedimentCapacity.value = m_settings.m_erosionSedimentCapacity;
                        m_Eroder.m_ErosionSettings.m_SedimentDepositRate.value = m_settings.m_erosionSedimentDepositRate;
                        m_Eroder.m_ErosionSettings.m_SedimentDissolveRate.value = m_settings.m_erosionSedimentDissolveRate;
                        m_Eroder.m_ErosionSettings.m_RiverBankDepositRate.value = m_settings.m_erosionRiverBankDepositRate;
                        m_Eroder.m_ErosionSettings.m_RiverBankDissolveRate.value = m_settings.m_erosionRiverBankDissolveRate;
                        m_Eroder.m_ErosionSettings.m_RiverBedDepositRate.value = m_settings.m_erosionRiverBedDepositRate;
                        m_Eroder.m_ErosionSettings.m_RiverBedDissolveRate.value = m_settings.m_erosionRiverBedDissolveRate;

                        //and erode
                        m_Eroder.ErodeHeightmap(operation.m_originTerrain.terrainData.size, operation.m_terrainDetailBrushTransform.GetBrushXYBounds(), texelSize, Event.current.control);

                        Vector4 erosionBrushParams = new Vector4(1f, 0.0f, 0.0f, 0.0f);
                        erosionMat.SetTexture("_BrushTex", localOutputTexture);
                        erosionMat.SetTexture("_NewHeightTex", m_Eroder.outputTextures["Height"]);
                        erosionMat.SetVector("_BrushParams", erosionBrushParams);
                        operation.SetupMaterialProperties(erosionMat, MultiTerrainOperationType.Heightmap);
                        Graphics.Blit(operation.RTheightmap, outputTexture, erosionMat, 0);
                        m_Eroder.ReleaseRenderTextures();
#endif
                        break;
                    case GaiaConstants.FeatureOperation.Contrast:
                        operation.RTheightmap.filterMode = FilterMode.Bilinear;
                        Material contrastMat = GetCurrentFXMaterial();

                        Vector4 contrastBrushParams = new Vector4(m_settings.m_contrastStrength, 0.0f, m_settings.m_contrastFeatureSize, 0);
                        //if (activeLocalFilters)
                            contrastMat.SetTexture("_BrushTex", localOutputTexture);
                        //else
                        //    contrastMat.SetTexture("_BrushTex", localinputTexture);
                        contrastMat.SetVector("_BrushParams", contrastBrushParams);

                        operation.SetupMaterialProperties(contrastMat, MultiTerrainOperationType.Heightmap);
                        Graphics.Blit(operation.RTheightmap, outputTexture, contrastMat, 0);
                        break;
                    case GaiaConstants.FeatureOperation.Terrace:
                        Material terraceMat = GetCurrentFXMaterial();
                        float delta = m_settings.m_terraceJitterCount * 500.0f;
                        //float jitteredFeatureSize = m_terraceCount + UnityEngine.Random.Range(m_terraceCount - delta, m_terraceCount + delta);
                        Vector4 terraceBrushParams = new Vector4(1f, m_settings.m_terraceCount, m_settings.m_terraceBevelAmountInterior, 0.0f);

                        //if (activeLocalFilters)
                            terraceMat.SetTexture("_BrushTex", localOutputTexture);
                        //else
                        //    terraceMat.SetTexture("_BrushTex", localinputTexture);
                        terraceMat.SetVector("_BrushParams", terraceBrushParams);

                        operation.SetupMaterialProperties(terraceMat, MultiTerrainOperationType.Heightmap);
                        
                        Graphics.Blit(operation.RTheightmap, outputTexture, terraceMat, 0);
                        break;
                    case GaiaConstants.FeatureOperation.SharpenRidges:
                        Material sharpenRidgesMat = GetCurrentFXMaterial();

                        // apply brush
                        Vector4 sharpenRidgesBrushParams = new Vector4(
                            1f,
                            16f,
                            m_settings.m_sharpenRidgesMixStrength,
                            0.0f);

                        //if (activeLocalFilters)
                            sharpenRidgesMat.SetTexture("_BrushTex", localOutputTexture);
                        //else
                        //    sharpenRidgesMat.SetTexture("_BrushTex", localinputTexture);
                        sharpenRidgesMat.SetVector("_BrushParams", sharpenRidgesBrushParams);

                        operation.SetupMaterialProperties(sharpenRidgesMat, MultiTerrainOperationType.Heightmap);
                        RenderTexture sharpenRidgesResultTex = new RenderTexture(operation.RTheightmap);
                        Graphics.Blit(operation.RTheightmap, sharpenRidgesResultTex, sharpenRidgesMat, 0);
                        //Perform Iterations
                        RenderTexture sharpenRidgesIterationTex = new RenderTexture(sharpenRidgesResultTex);
                        for (int i = 1; i <= m_settings.m_sharpenRidgesIterations; i++)
                        {
                            Graphics.Blit(sharpenRidgesResultTex, sharpenRidgesIterationTex, sharpenRidgesMat, 0);
                            Graphics.Blit(sharpenRidgesIterationTex, sharpenRidgesResultTex);
                        }

                        Graphics.Blit(sharpenRidgesResultTex, outputTexture, sharpenRidgesMat, 0);

                        break;
                    case GaiaConstants.FeatureOperation.HeightTransform:
                        Material heightTransformMat = GetCurrentFXMaterial();
                        heightTransformMat.SetTexture("_InputTex", operation.RTheightmap);
                        heightTransformMat.SetTexture("_BrushTex", localOutputTexture);
                        Terrain currentTerrain = GetCurrentTerrain();
                        float scalarMaxHeight = Mathf.InverseLerp(0, currentTerrain.terrainData.size.y, m_maxCurrentTerrainHeight);
                        float scalarMinHeight = Mathf.InverseLerp(0, currentTerrain.terrainData.size.y, m_minCurrentTerrainHeight);
                        //transfer the scalar 0..1 value to -0.5..0.5 as this is how it is used in the shader
                        scalarMaxHeight = Mathf.Lerp(0, 0.5f, scalarMaxHeight);
                        scalarMinHeight = Mathf.Lerp(0, 0.5f, scalarMinHeight);
                        heightTransformMat.SetFloat("_MaxWorldHeight", scalarMaxHeight);
                        heightTransformMat.SetFloat("_MinWorldHeight", scalarMinHeight);
                        operation.SetupMaterialProperties(heightTransformMat, MultiTerrainOperationType.Heightmap);
                        ImageProcessing.BakeCurveTexture(m_settings.m_heightTransformCurve, heightTransformCurveTexture);
                        heightTransformMat.SetTexture("_HeightTransformTex", heightTransformCurveTexture);
                        Graphics.Blit(operation.RTheightmap, outputTexture, heightTransformMat, 0);
                        break;
                    case GaiaConstants.FeatureOperation.PowerOf:
                        Material powerOfMat = GetCurrentFXMaterial();
                        powerOfMat.SetTexture("_InputTex", operation.RTheightmap);
                        powerOfMat.SetTexture("_BrushTex", localOutputTexture);
                        powerOfMat.SetFloat("_Power", m_settings.m_powerOf);
                        operation.SetupMaterialProperties(powerOfMat, MultiTerrainOperationType.Heightmap);
                        Graphics.Blit(operation.RTheightmap, outputTexture, powerOfMat, 0);
                        break;
                    case GaiaConstants.FeatureOperation.Smooth:
                        Vector4 smoothBrushParams = new Vector4(1f, 0.0f, 0.0f, 0.0f);
                        Material smoothMat = GetCurrentFXMaterial();
                        m_heightModifier = ImageMask.NewAnimCurveStraightUpwards();
                        ImageProcessing.BakeCurveTexture(m_heightModifier, transformHeightCurveTexture);
                        smoothMat.SetTexture("_MainTex", operation.RTheightmap);
                        smoothMat.SetTexture("_BrushTex", localinputTexture);
                        smoothMat.SetTexture("_HeightTransformTex", transformHeightCurveTexture);
                        smoothMat.SetVector("_BrushParams", smoothBrushParams);
                        Vector4 smoothWeights = new Vector4(
                            Mathf.Clamp01(1.0f - Mathf.Abs(m_settings.m_smoothVerticality)),   // centered
                            Mathf.Clamp01(-m_settings.m_smoothVerticality),                    // min
                            Mathf.Clamp01(m_settings.m_smoothVerticality),                     // max
                            m_settings.m_smoothBlurRadius);                                  // kernel size
                        smoothMat.SetVector("_SmoothWeights", smoothWeights);

                        operation.SetupMaterialProperties(smoothMat, MultiTerrainOperationType.Heightmap);

                        // Two pass blur (first horizontal, then vertical)
                        RenderTexture workaround1 = RenderTexture.GetTemporary(operation.RTheightmap.descriptor);
                        RenderTexture tmpsmoothRT = RenderTexture.GetTemporary(operation.RTheightmap.descriptor);
                        Graphics.Blit(operation.RTheightmap, tmpsmoothRT, smoothMat, 0);
                        Graphics.Blit(tmpsmoothRT, outputTexture, smoothMat, 1);

                        RenderTexture.ReleaseTemporary(tmpsmoothRT);
                        RenderTexture.ReleaseTemporary(workaround1);
                        break;
                    case GaiaConstants.FeatureOperation.MixHeight:
                        Material mixHeightMat = GetCurrentFXMaterial();
                        mixHeightMat.SetTexture("_InputTex", operation.RTheightmap);
                        mixHeightMat.SetTexture("_BrushTex", localOutputTexture);
                        mixHeightMat.SetTexture("_StampTex", m_settings.m_stamperInputImageMask.ImageMaskTexture);
                        mixHeightMat.SetFloat("_MixMidPoint", m_settings.m_mixMidPoint);
                        mixHeightMat.SetFloat("_Strength", m_settings.m_mixHeightStrength);
                        if (operation.m_originTerrain != null)
                        {
                            mixHeightMat.SetFloat("_WorldHeightMin", Mathf.InverseLerp(0, operation.m_originTerrain.terrainData.size.y, m_minCurrentTerrainHeight));
                            mixHeightMat.SetFloat("_WorldHeightMax", Mathf.InverseLerp(0, operation.m_originTerrain.terrainData.size.y, m_maxCurrentTerrainHeight));
                        }
                        operation.SetupMaterialProperties(mixHeightMat, MultiTerrainOperationType.Heightmap);
                        Graphics.Blit(operation.RTheightmap, outputTexture, mixHeightMat, 0);
                        break;
                    default:
                        Material mat = new Material(Shader.Find("Hidden/Gaia/BaseOperation"));
                        Vector4 brushParams = new Vector4(0.01f * m_settings.m_height, 0.0f, 0.0f, 0.0f);
                        mat.SetTexture("_BrushTex", localOutputTexture);
                        mat.SetTexture("_GlobalMaskTex", globalOutputTexture);
                        mat.SetFloat("_BaseLevel", Mathf.Lerp(0f, 0.5f, m_settings.m_baseLevel));
                        mat.SetFloat("_BlendStrength", m_blendStrength);
                        //Get relative y position according to terrain height where 
                        //-1 = terrain y-position - max terrain height 
                        //+1 = terrain y-position + max terrain height
                        //this covers the complete y range where a stamp could potentially still influence the terrain.
                        float relativeYPos = Mathf.Lerp(-0.5f, 0.5f, Mathf.InverseLerp(operation.m_originTerrain.transform.position.y - operation.m_originTerrain.terrainData.size.y, operation.m_originTerrain.transform.position.y + operation.m_originTerrain.terrainData.size.y, (float)m_settings.m_y));
                        
                        mat.SetFloat("_yPos", relativeYPos);
                        if (m_settings.m_drawStampBase)
                            mat.SetFloat("_StampBase", 1f);
                        else
                            mat.SetFloat("_StampBase", 0f);
                        if (m_settings.m_adaptiveBase)
                            mat.SetFloat("_AdaptiveBase", 1f);
                        else
                            mat.SetFloat("_AdaptiveBase", 0f);
                        mat.SetVector("_BrushParams", brushParams);

                        operation.SetupMaterialProperties(mat, MultiTerrainOperationType.Heightmap);

                        Graphics.Blit(operation.RTheightmap, outputTexture, mat, (int)m_settings.m_operation);

                        mat.SetTexture("_BrushTex", null);
                        mat.SetTexture("_GlobalMaskTex", null);
                        DestroyImmediate(mat);

                        break;
                }



                if (m_cachedRenderTexture != null)
                {
                    m_cachedRenderTexture.Release();
                    DestroyImmediate(m_cachedRenderTexture);
                }

                //save result in cache
                m_cachedRenderTexture = new RenderTexture(rtDescriptor);
                m_cachedRenderTexture.name = "Stamper Cached Render Texture";
                Graphics.Blit(outputTexture, m_cachedRenderTexture);
                RenderTexture.active = currentRT;

                //Clean up render textures
                if (localinputTexture != null)
                {
                    localinputTexture.DiscardContents();
                    RenderTexture.ReleaseTemporary(localinputTexture);
                    localinputTexture = null;
                }
                if (globalInputTexture != null)
                {
                    globalInputTexture.DiscardContents();
                    RenderTexture.ReleaseTemporary(globalInputTexture);
                    globalInputTexture = null;
                }
                if (localOutputTexture != null)
                {
                    localOutputTexture.DiscardContents();
                    RenderTexture.ReleaseTemporary(localOutputTexture);
                    localOutputTexture = null;
                }
                if (globalOutputTexture != null)
                {
                    globalOutputTexture.DiscardContents();
                    RenderTexture.ReleaseTemporary(globalOutputTexture);
                    globalOutputTexture = null;
                }
                if (outputTexture != null)
                {
                    outputTexture.DiscardContents();
                    RenderTexture.ReleaseTemporary(outputTexture);
                    outputTexture = null;
                }
                m_stampDirty = false;
                GaiaUtils.ReleaseAllTempRenderTextures();

            }
            
            
            return m_cachedRenderTexture;
        
            //else
            //{
            //    //no re-calculation necessary, just return last result from cache
            //    return m_cachedRenderTexture;
            //}
            
        }

        private Material GetCurrentFXMaterial()
        {
            string shaderName = "";

            switch (m_settings.m_operation)
            {
                case GaiaConstants.FeatureOperation.Contrast:
                    shaderName = "Hidden/GaiaPro/Contrast";
                    break;
                case GaiaConstants.FeatureOperation.Terrace:
                    shaderName = "Hidden/GaiaPro/Terrace";
                    break;
                case GaiaConstants.FeatureOperation.SharpenRidges:
                    shaderName = "Hidden/GaiaPro/SharpenRidges";
                    break;
                case GaiaConstants.FeatureOperation.HydraulicErosion:
                    shaderName = "Hidden/GaiaPro/SimpleHeightBlend";
                    break;
                case GaiaConstants.FeatureOperation.HeightTransform:
                    shaderName = "Hidden/GaiaPro/HeightTransform";
                    break;
                case GaiaConstants.FeatureOperation.PowerOf:
                    shaderName = "Hidden/GaiaPro/PowerOf";
                    break;
                case GaiaConstants.FeatureOperation.Smooth:
                    shaderName = "Hidden/Gaia/SmoothHeight";
                    break;
                case GaiaConstants.FeatureOperation.MixHeight:
                    shaderName = "Hidden/Gaia/MixHeight";
                    break;
                default:
                    break;

            }

            if (shaderName == "")
            {
                return null;
            }

            if (m_currentFXMaterial == null || m_currentFXMaterial.shader.name != shaderName)
                m_currentFXMaterial = new Material(Shader.Find(shaderName));
            return m_currentFXMaterial;
        }

        private void BakeCurveTextures()
        {
            if (m_MaskTexturesDirty)
            {
                //if (m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.PerlinNoise)
                //{
                //    m_imageMask = new Texture2D(m_stampImage.width, m_stampImage.height, TextureFormat.RGBAFloat, true, false)
                //    {
                //        name = "Image Mask Noise Texture",
                //        hideFlags = HideFlags.DontSave
                //    };

                //    FractalGenerator fractGen = new FractalGenerator(m_noiseMaskFrequency, m_noiseMaskLacunarity, m_noiseMaskOctaves, m_noiseMaskPersistence, m_noiseMaskSeed, FractalGenerator.Fractals.Perlin);


                //    for (int x = 0; x < m_stampImage.width; x++)
                //    {
                //        for (int z = 0; z < m_stampImage.height; z++)
                //        {
                //            //float colorValue = Mathf.Clamp01(fractGen.GetNormalisedValue(100000f + (x * (1f / m_noiseZoom)), 100000f + (z * (1f / m_noiseZoom))));
                //            float colorValue = Mathf.Clamp01(Mathf.PerlinNoise(x/100f, z/100f));
                //            m_imageMask.SetPixel(x, z, new Color(colorValue, colorValue, colorValue));
                //        }
                //    }
                //    m_imageMask.Apply();
                //    //UnityHeightMap hm = new UnityHeightMap(m_imageMask);
                //    //hm.Smooth(m_smoothIterations);
                //    //m_imageMask = hm.ToTexture();

                //    byte[] exrBytes = ImageConversion.EncodeToEXR(m_imageMask, Texture2D.EXRFlags.CompressZIP);
                //    PWCommon1.Utils.WriteAllBytes("D:\\NoiseMAsk.exr", exrBytes);
                //}

                float range = 1f;

                if (m_distanceMask != null && m_distanceMask.length > 0)
                {
                    range = m_distanceMask[m_distanceMask.length - 1].time;

                    for (float i = 0f; i <= 1f; i += 1f / 255f)
                    {
                        float c = m_distanceMask.Evaluate(i * range);
                        distanceMaskCurveTexture.SetPixel(Mathf.FloorToInt(i * 255f), 0, new Color(c, c, c));
                    }

                    distanceMaskCurveTexture.Apply();
                }

                range = 1f;
                if (m_heightModifier != null && m_heightModifier.length > 0)
                {
                    range = m_heightModifier[m_heightModifier.length - 1].time;

                    for (float i = 0f; i <= 1f; i += 1f / 255f)
                    {
                        float c = m_heightModifier.Evaluate(i * range);
                        transformHeightCurveTexture.SetPixel(Mathf.FloorToInt(i * 255f), 0, new Color(c, c, c));
                    }

                    transformHeightCurveTexture.Apply();
                }

                m_MaskTexturesDirty = false;
            }
        }


        /// <summary>
        /// Generate a small white texture for the image mask when no image mask is selected to always have full mask influence in the shader
        /// </summary>
        public void EmptyImageMask()
        {
            m_imageMask = Texture2D.whiteTexture;
            m_imageMaskGUID = "";
            m_imageMaskInvert = false;
            m_imageMaskNormalise = false;
        }

        /// <summary>
        /// Generate a small white texture for the stamp image when no stamp image is selected to always have full stamp influence in the shader
        /// </summary>
        public void EmptyStampImage()
        {
            m_stampImage = Texture2D.whiteTexture;
            m_stampImageGUID = "";
            m_invertStamp = false;
            m_normaliseStamp = false;
        }

        /// <summary>
        /// Generate the preview mesh
        /// </summary>
        //        private void GeneratePreviewMesh()
        //        {
        //            if (m_previewMaterial == null)
        //            {
        //#if UNITY_EDITOR
        //                string matPath = GaiaUtils.GetAssetPath("GaiaStamperMaterial.mat");
        //                if (!string.IsNullOrEmpty(matPath))
        //                {
        //                    m_previewMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        //                }
        //                else
        //                {
        //                    m_previewMaterial = new Material(Shader.Find("Diffuse"));
        //                    m_previewMaterial.color = Color.white;
        //                    if (Terrain.activeTerrain != null)
        //                    {
        //                        var splatPrototypes = GaiaSplatPrototype.GetGaiaSplatPrototypes(Terrain.activeTerrain);
        //                        if (splatPrototypes.Length > 0)
        //                        {
        //                            Texture2D oldTex;
        //                            if (splatPrototypes.Length == 4) //Defaults to cliff
        //                            {
        //                                oldTex = splatPrototypes[3].texture;
        //                            }
        //                            else
        //                            {
        //                                oldTex = splatPrototypes[0].texture;
        //                            }
        //                            GaiaUtils.MakeTextureReadable(oldTex);
        //                            Texture2D newTex = new Texture2D(oldTex.width, oldTex.height, TextureFormat.ARGB32, true);
        //                            newTex.SetPixels32(oldTex.GetPixels32());
        //                            newTex.wrapMode = TextureWrapMode.Repeat;
        //                            newTex.Apply();
        //                            m_previewMaterial.mainTexture = newTex;
        //                            m_previewMaterial.mainTextureScale = new Vector2(30f, 30f);
        //                        }
        //                    }
        //                    m_previewMaterial.hideFlags = HideFlags.HideInInspector;
        //                    m_previewMaterial.name = "StamperMaterial";
        //                }
        //#else
        //                m_previewMaterial = new Material(Shader.Find("Diffuse"));
        //                m_previewMaterial.color = Color.white;
        //                if (Terrain.activeTerrain != null)
        //                {
        //                    var splatPrototypes = GaiaSplatPrototype.GetGaiaSplatPrototypes(Terrain.activeTerrain);
        //                    if (splatPrototypes.Length > 0)
        //                    {
        //                        Texture2D oldTex;
        //                        if (splatPrototypes.Length == 4) //Defaults to cliff
        //                        {
        //                            oldTex = splatPrototypes[3].texture;
        //                        }
        //                        else
        //                        {
        //                            oldTex = splatPrototypes[0].texture;
        //                        }
        //                        GaiaUtils.MakeTextureReadable(oldTex);
        //                        Texture2D newTex = new Texture2D(oldTex.width, oldTex.height, TextureFormat.ARGB32, true);
        //                        newTex.SetPixels32(oldTex.GetPixels32());
        //                        newTex.wrapMode = TextureWrapMode.Repeat;
        //                        newTex.Apply();
        //                        m_previewMaterial.mainTexture = newTex;
        //                        m_previewMaterial.mainTextureScale = new Vector2(30f, 30f);
        //                    }
        //                }
        //                m_previewMaterial.hideFlags = HideFlags.HideInInspector;
        //                m_previewMaterial.name = "StamperMaterial";
        //#endif
        //                    }

        //            m_previewFilter = GetComponent<MeshFilter>();
        //            if (m_previewFilter == null)
        //            {
        //                this.gameObject.AddComponent<MeshFilter>();
        //                m_previewFilter = GetComponent<MeshFilter>();
        //                m_previewFilter.hideFlags = HideFlags.HideInInspector;
        //            }

        //            m_previewRenderer = GetComponent<MeshRenderer>();
        //            if (m_previewRenderer == null)
        //            {
        //                this.gameObject.AddComponent<MeshRenderer>();
        //                m_previewRenderer = GetComponent<MeshRenderer>();
        //                m_previewRenderer.hideFlags = HideFlags.HideInInspector;
        //            }

        //            m_previewRenderer.sharedMaterial = m_previewMaterial;
        //            Vector3 meshSize = new Vector3((float)m_scanWidth * m_scanResolution, (float)m_scanHeight * m_scanResolution, (float)m_scanDepth * m_scanResolution);
        //            m_previewFilter.mesh = GaiaUtils.CreateMesh(m_stampHM.Heights(), meshSize);
        //        }

        /// <summary>
        /// Load the image mask if one was specified, or calculate it from noise
        /// </summary>
        //public bool LoadImageMask()
        //{
        //    //Kill old image height map
        //    m_imageMaskHM = null;

        //    //Check mode & exit 
        //    if (m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.None)
        //    {
        //        return false;
        //    }

        //    //Load the supplied image
        //    if (m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.ImageRedChannel || m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.ImageGreenChannel ||
        //        m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.ImageBlueChannel || m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.ImageAlphaChannel ||
        //        m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.ImageGreyScale)
        //    {
        //        if (m_imageMask == null)
        //        {
        //            Debug.LogError("You requested an image mask but did not supply one. Please select mask texture.");
        //            return false;
        //        }
        //        else
        //        {
        //            m_imageMaskGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_imageMask));
        //        }


        //        //Check the image rw
        //        GaiaUtils.MakeTextureReadable(m_imageMask);

        //        //Make it uncompressed
        //        GaiaUtils.MakeTextureUncompressed(m_imageMask);

        //        //Load the image
        //        m_imageMaskHM = new HeightMap(m_imageMask.width, m_imageMask.height);
        //        for (int x = 0; x < m_imageMaskHM.Width(); x++)
        //        {
        //            for (int z = 0; z < m_imageMaskHM.Depth(); z++)
        //            {
        //                switch (m_areaMaskMode)
        //                {
        //                    case GaiaConstants.ImageFitnessFilterMode.ImageGreyScale:
        //                        m_imageMaskHM[x, z] = m_imageMask.GetPixel(x, z).grayscale;
        //                        break;
        //                    case GaiaConstants.ImageFitnessFilterMode.ImageRedChannel:
        //                        m_imageMaskHM[x, z] = m_imageMask.GetPixel(x, z).r;
        //                        break;
        //                    case GaiaConstants.ImageFitnessFilterMode.ImageGreenChannel:
        //                        m_imageMaskHM[x, z] = m_imageMask.GetPixel(x, z).g;
        //                        break;
        //                    case GaiaConstants.ImageFitnessFilterMode.ImageBlueChannel:
        //                        m_imageMaskHM[x, z] = m_imageMask.GetPixel(x, z).b;
        //                        break;
        //                    case GaiaConstants.ImageFitnessFilterMode.ImageAlphaChannel:
        //                        m_imageMaskHM[x, z] = m_imageMask.GetPixel(x, z).a;
        //                        break;
        //                }
        //            }
        //        }
        //    }
        //    else if (m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.PerlinNoise || m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.RidgedNoise ||
        //        m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.BillowNoise)
        //    {
        //        int width = 2048;
        //        int depth = 2048;

        //        Terrain t = Gaia.TerrainHelper.GetTerrain(transform.position);
        //        if (t == null)
        //        {
        //            t = Terrain.activeTerrain;
        //        }
        //        if (t != null)
        //        {
        //            width = t.terrainData.heightmapWidth;
        //            depth = t.terrainData.heightmapWidth;
        //        }

        //        m_imageMaskHM = new HeightMap(width, depth);

        //        //Create the noise generator
        //        Gaia.FractalGenerator noiseGenerator = new FractalGenerator();
        //        noiseGenerator.Seed = m_noiseMaskSeed;
        //        noiseGenerator.Octaves = m_noiseMaskOctaves;
        //        noiseGenerator.Persistence = m_noiseMaskPersistence;
        //        noiseGenerator.Frequency = m_noiseMaskFrequency;
        //        noiseGenerator.Lacunarity = m_noiseMaskLacunarity;
        //        if (m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.PerlinNoise)
        //        {
        //            noiseGenerator.FractalType = FractalGenerator.Fractals.Perlin;
        //        }
        //        else if (m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.RidgedNoise)
        //        {
        //            noiseGenerator.FractalType = FractalGenerator.Fractals.RidgeMulti;
        //        }
        //        else if (m_areaMaskMode == GaiaConstants.ImageFitnessFilterMode.BillowNoise)
        //        {
        //            noiseGenerator.FractalType = FractalGenerator.Fractals.Billow;
        //        }

        //        float zoom = 1f / m_noiseZoom;

        //        //Now fill it with the selected noise
        //        for (int x = 0; x < width; x++)
        //        {
        //            for (int z = 0; z < depth; z++)
        //            {
        //                m_imageMaskHM[x, z] = noiseGenerator.GetValue((float)(x * zoom), (float)(z * zoom));
        //            }
        //        }
        //    }
        //    else
        //    {
        //        //Or get a new one

        //        //Grab the terrain 
        //        Terrain t = Gaia.TerrainHelper.GetTerrain(transform.position);
        //        if (t == null)
        //        {
        //            t = Terrain.activeTerrain;
        //        }
        //        if (t == null)
        //        {
        //            Debug.LogError("You requested an terrain texture mask but there is no terrain.");
        //            return false;
        //        }

        //        var splatPrototypes = GaiaSplatPrototype.GetGaiaSplatPrototypes(t);

        //        switch (m_areaMaskMode)
        //        {
        //            case GaiaConstants.ImageFitnessFilterMode.TerrainTexture0:
        //                if (splatPrototypes.Length < 1)
        //                {
        //                    Debug.LogError("You requested an terrain texture mask 0 but there is no active texture in slot 0.");
        //                    return false;
        //                }
        //                m_imageMaskHM = new HeightMap(t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight), 0);
        //                break;
        //            case GaiaConstants.ImageFitnessFilterMode.TerrainTexture1:
        //                if (splatPrototypes.Length < 2)
        //                {
        //                    Debug.LogError("You requested an terrain texture mask 1 but there is no active texture in slot 1.");
        //                    return false;
        //                }
        //                m_imageMaskHM = new HeightMap(t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight), 1);
        //                break;
        //            case GaiaConstants.ImageFitnessFilterMode.TerrainTexture2:
        //                if (splatPrototypes.Length < 3)
        //                {
        //                    Debug.LogError("You requested an terrain texture mask 2 but there is no active texture in slot 2.");
        //                    return false;
        //                }
        //                m_imageMaskHM = new HeightMap(t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight), 2);
        //                break;
        //            case GaiaConstants.ImageFitnessFilterMode.TerrainTexture3:
        //                if (splatPrototypes.Length < 4)
        //                {
        //                    Debug.LogError("You requested an terrain texture mask 3 but there is no active texture in slot 3.");
        //                    return false;
        //                }
        //                m_imageMaskHM = new HeightMap(t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight), 3);
        //                break;
        //            case GaiaConstants.ImageFitnessFilterMode.TerrainTexture4:
        //                if (splatPrototypes.Length < 5)
        //                {
        //                    Debug.LogError("You requested an terrain texture mask 4 but there is no active texture in slot 4.");
        //                    return false;
        //                }
        //                m_imageMaskHM = new HeightMap(t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight), 4);
        //                break;
        //            case GaiaConstants.ImageFitnessFilterMode.TerrainTexture5:
        //                if (splatPrototypes.Length < 6)
        //                {
        //                    Debug.LogError("You requested an terrain texture mask 5 but there is no active texture in slot 5.");
        //                    return false;
        //                }
        //                m_imageMaskHM = new HeightMap(t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight), 5);
        //                break;
        //            case GaiaConstants.ImageFitnessFilterMode.TerrainTexture6:
        //                if (splatPrototypes.Length < 7)
        //                {
        //                    Debug.LogError("You requested an terrain texture mask 6 but there is no active texture in slot 6.");
        //                    return false;
        //                }
        //                m_imageMaskHM = new HeightMap(t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight), 6);
        //                break;
        //            case GaiaConstants.ImageFitnessFilterMode.TerrainTexture7:
        //                if (splatPrototypes.Length < 8)
        //                {
        //                    Debug.LogError("You requested an terrain texture mask 7 but there is no active texture in slot 7.");
        //                    return false;
        //                }
        //                m_imageMaskHM = new HeightMap(t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight), 7);
        //                break;
        //        }

        //        //It came from terrain so flip it
        //        m_imageMaskHM.Flip();
        //    }

        //    //Because images are noisy, smooth it
        //    if (m_imageMaskSmoothIterations > 0)
        //    {
        //        m_imageMaskHM.Smooth(m_imageMaskSmoothIterations);
        //    }

        //    //Flip it
        //    if (m_imageMaskFlip == true)
        //    {
        //        m_imageMaskHM.Flip();
        //    }

        //    //Normalise it if necessary
        //    if (m_imageMaskNormalise == true)
        //    {
        //        m_imageMaskHM.Normalise();
        //    }

        //    //Invert it if necessessary
        //    //Inversion is done directly in the shader now
        //    //if (m_imageMaskInvert == true)
        //    //{
        //    //    m_imageMaskHM.Invert();
        //    //}

        //    m_imageMask = m_imageMaskHM.ToTexture();

        //    return true;
        //}

        /// <summary>
        /// Calculate the height to apply to the location supplied
        /// </summary>
        /// <param name="terrainHeight">The terrain height at this location</param>
        /// <param name="smHeightRaw">The raw unadsjusted source map height at this location</param>
        /// <param name="smHeightAdj">The adjusted source map height at this location</param>
        /// <param name="stencilHeightNU">The stencil height in normal units</param>
        /// <param name="strength">The strength of the effect 0 - no effect - 1 - full effect</param>
        /// <returns>New height</returns>
        private float CalculateHeight(float terrainHeight, float smHeightRaw, float smHeightAdj, float stencilHeightNU, float strength)
        {
            float tmpHeight = 0f;
            float heightDiff = 0f;

            //Check for the base
            if (m_settings.m_drawStampBase != true)
            {
                if (smHeightRaw < m_settings.m_baseLevel)
                {
                    return terrainHeight;
                }
            }

            switch (m_settings.m_operation)
            {
                case GaiaConstants.FeatureOperation.RaiseHeight:
                    {
                        if (smHeightAdj > terrainHeight)
                        {
                            heightDiff = (smHeightAdj - terrainHeight) * strength;
                            terrainHeight += heightDiff;
                        }
                    }
                    break;
                case GaiaConstants.FeatureOperation.BlendHeight:
                    {
                        tmpHeight = (m_blendStrength * smHeightAdj) + ((1f - m_blendStrength) * terrainHeight);
                        heightDiff = (tmpHeight - terrainHeight) * strength;
                        terrainHeight += heightDiff;
                    }
                    break;
                //case GaiaConstants.FeatureOperation.DifferenceHeight:
                //    {
                //        tmpHeight = Mathf.Abs(smHeightAdj - terrainHeight);
                //        heightDiff = (tmpHeight - terrainHeight) * strength;
                //        terrainHeight += heightDiff;
                //    }
                //    break;
                /*
                case Constants.FeatureOperation.OverlayHeight:
                    {
                        if (terrainHeight < 0.5f)
                        {
                            tmpHeight = 2f * terrainHeight * smHeight;
                        }
                        else
                        {
                            tmpHeight = 1f - (2f * (1f - terrainHeight) * (1f - smHeight));
                        }
                        heightDiff = (tmpHeight - terrainHeight) * strength;
                        terrainHeight += heightDiff;
                    }
                    break;
                case Constants.FeatureOperation.ScreenHeight:
                    {
                        tmpHeight = 1f - ((1f - terrainHeight) * (1f - smHeight));
                        heightDiff = (tmpHeight - terrainHeight) * strength;
                        terrainHeight += heightDiff;
                    }
                    break;
                 */
                case GaiaConstants.FeatureOperation.SetHeight:
                    {
                        tmpHeight = terrainHeight + (smHeightAdj * stencilHeightNU);
                        heightDiff = (tmpHeight - terrainHeight) * strength;
                        terrainHeight += heightDiff;
                    }
                    break;
                case GaiaConstants.FeatureOperation.LowerHeight:
                    {
                        if (smHeightAdj < terrainHeight)
                        {
                            heightDiff = (terrainHeight - smHeightAdj) * strength;
                            terrainHeight -= heightDiff;
                        }
                    }
                    break;
            }
            return terrainHeight;
        }

        /// <summary>
        /// Rotate the point around the pivot - used to handle rotation
        /// </summary>
        /// <param name="point">Point to move</param>
        /// <param name="pivot">Pivot</param>
        /// <param name="angle">Angle to pivot</param>
        /// <returns></returns>
        private Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angle)
        {
            Vector3 dir = point - pivot;
            dir = Quaternion.Euler(angle) * dir;
            point = dir + pivot;
            return point;
        }

#endregion

    }

    class RotationProducts
    {
        public double sinTheta = 0;
        public double cosTheta = 0;
    }
}