#define GAIA_PRESENT

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Gaia
{
    public static class GaiaConstants
    {
        /// <summary>
        /// Where scanned assets will be loaded from and saved to
        /// </summary>
        //public static readonly string AssetDir = "Gaia/Stamps";
        //public static readonly string AssetDirFromAssetDB = "Assets/Gaia/Stamps";

        public const string builtInKeyWord = "SP";
        public const string lightweightKeyWord = "LW";
        public const string universalKeyWord = "UP";
        public const string highDefinitionKeyWord = "HD";

        /// <summary>
        /// Screenshot resolutions
        /// </summary>
        public enum ScreenshotResolution { Resolution640X480, Resolution800X600, Resolution1280X720, Resolution1366X768, Resolution1600X900, Resolution1920X1080, Resolution2560X1440, Resolution3840X2160, Resolution7680X4320, Custom }

        /// <summary>
        /// Defines the type of lighting is suppose to be
        /// </summary>
        public enum GaiaLightingProfileType { Procedural, HDRI, ProceduralWorldsSky }

        /// <summary>
        /// The supported Anti aliasing modes
        /// </summary>
        public enum GaiaProAntiAliasingMode { None, FXAA, MSAA, SMAA, TAA }

        /// <summary>
        /// The water mesh quality
        /// </summary>
        public enum WaterMeshQuality { VeryLow, Low, Medium, High, VeryHigh, Ultra, Cinematic, Custom }

        /// <summary>
        /// Used for detail patch resolution in the terrain detail overwrite system
        /// </summary>
        public enum TerrainDetailQuality { VeryLow64, Low32, Medium16, High8, VeryHigh4, Ultra2 }

        public enum ContactShadowsQuality { Low, Medium, High, Ultra, Custom }

        public enum HDSkyType { Gradient, HDRI, Procedural }

        public enum HDAmbientMode { Static, Dynamic }

        public enum HDFogType { None, Exponential, Linear, Volumetric }

        public enum HDFogType2019_3 { None, Volumetric }

        public enum HDSkyUpdateMode { OnChanged, OnDemand, Realtime }

        public enum HDIntensityMode { Exposure, Multiplier }

        public enum HDShadowResolution { Resolution256, Resolution512, Resolution1024, Resolution2048, Resolution4096, Resolution8192 }

        /// <summary>
        /// Sets the supported water resolution
        /// </summary>
        public enum GaiaProWaterReflectionsQuality { Resolution8, Resolution16, Resolution32, Resolution64, Resolution128, Resolution256, Resolution512, Resolution1024, Resolution2048, Resolution4096, Resolution8192 }

        /// <summary>
        /// Defines what type of water is suppose to be
        /// </summary>
        public enum GaiaWaterProfileType { DeepBlueOcean, ClearBlueOcean, StandardLake, StandardClearLake, None }

        /// <summary>
        /// Sets the wind type on how strong it is rendered in the scene
        /// </summary>
        public enum GaiaWindType { Calm, Moderate, Strong, None }
        /// <summary>
        /// Sets the thunder storm type and how it behaves
        /// </summary>
        public enum ThunderStormType { Light, Moderate, Continuous }

        public enum GaiaGlobalWindType { Calm, Moderate, Strong, None, Custom }

        public enum WindSetupType { VertexAnimated, VertexPainted }

        public enum MeshType { Plane, Circle };

        public enum PositionType { Absolute, Relative }

        /// <summary>
        /// Byte orders the scanner uses to load raw files.
        /// </summary>
        public enum RawByteOrder { IBM, Macintosh }

        /// <summary>
        /// Bit depths the scanner uses to load raw files.
        /// </summary>
        public enum RawBitDepth { Sixteen, Eight }

        /// <summary>
        /// The type of thing we are targeting
        /// </summary>
        public enum EnvironmentTarget { UltraLight, MobileAndVR, Desktop, PowerfulDesktop, Custom }

        /// <summary>
        /// The type of thing we are targeting
        /// </summary>
        public enum EnvironmentRenderer { BuiltIn, Lightweight, Universal, HighDefinition }


        /// <summary>
        /// Preset Environment Sizes
        /// </summary>
        public enum EnvironmentSizePreset { Tiny, Small, Medium, Large, Custom }

        /// <summary>
        /// The size of the environment we are targeting
        /// </summary>
        public enum EnvironmentSize { Is256MetersSq, Is512MetersSq, Is1024MetersSq, Is2048MetersSq, Is4096MetersSq, Is8192MetersSq, Is16384MetersSq }

        /// <summary>
        /// The heightmap resolution per terrain chunk 
        /// </summary>
        public enum HeightmapResolution { _33 = 33, _65 = 65, _129 = 129, _257 = 257, _513 = 513, _1025 = 1025, _2049 = 2049, _4097 = 4097 }

        /// <summary>
        /// The texture resolution per terrain chunk (This enum is used both for control and base texture since they allow the same values)
        /// </summary>
        public enum TerrainTextureResolution { _16 = 16, _32 = 32, _64 = 64, _128 = 128, _256 = 256, _512 = 512, _1024 = 1024, _2048 = 2048 }

        /// <summary>
        /// The different Ambient Skies samples
        /// </summary>
        public enum Skies { Day, Morning, Evening, Night, None }

        /// <summary>
        /// Sets the baked lighting mode to be realtime or baked
        /// </summary>
        public enum BakeMode { Baked, Realtime, Both }

        /// <summary>
        /// The different Ambient Water samples
        /// </summary>
        public enum Water { DeepBlue, ClearBlue, ToxicGreen, Cyan, None }

        public enum PW_RENDER_SIZE
        {
            FULL 	= -1,
            HALF 	= -2,
            QUARTER = -3
        };

        /// <summary>
        /// The different modes for Creating Post Processing Volumes when spawning - does the user want to create a new one for each spawn
        // or do they want the existing one replaced
        /// </summary>
        public enum BiomePostProcessingVolumeSpawnMode { Add, Replace }

        /// <summary>
        /// The different Post FX settings
        /// </summary>
        public enum PostFX { Day, Morning, Evening, Night, None }

        /// <summary>
        /// Used to set the starting time of day mode
        /// </summary>
        public enum TimeOfDayStartingMode { Morning, Day, Evening, Night }

        ///// <summary>
        ///// The Material pass selected for the stamping operation
        ///// </summary>
        //public enum MaterialPass
        //{
        //    RaiseHeight = 0,
        //    LowerHeight = 1,
        //    BlendHeight = 2,
        //    StencilHeight = 3,
        //    DifferenceHeight = 4
        //}

        /// <summary>
        /// Control type - from std assets
        /// </summary>
        public enum EnvironmentControllerType { FirstPerson, FlyingCamera, ThirdPerson, Car, XRController, Custom, None }

        /// <summary>
        /// Operational mode of the manager
        /// </summary>
        public enum ManagerEditorMode { Standard, Advanced, Extensions, ShowMore }

        /// <summary>
        /// Operational mode of the manager
        /// </summary>
        public enum ManagerEditorNewsMode { MoreOnGaia, MoreOnProceduralWorlds }

        /// <summary>
        /// Operational mode of the spawner component
        /// </summary>
        public enum OperationMode { DesignTime, RuntimeInterval, RuntimeTriggeredInterval }

        public enum DOFTrackingType { FixedOffset, LeftMouseClick, RightMouseClick, FollowScreen, FollowTarget }

        /// <summary>
        /// Type of terrain operation
        /// </summary>
        public enum TerrainOperationType { AddToTerrain, ApplyMaskToSplatmap, ContrastFilter, GrowFeaturesFilter, DeNoiseFilter, HydraulicFilter, MultiplyTerrain, PowerOfFilter, QuantizeFilter, QuantizeCurvesFilter, SetTerrainToHeight, ShrinkFeaturesFilter, SubtractFromTerrain, ThermalFilter, ExportAspectMap, ExportBaseMap, ExportCurvatureMap, ExportFlowMap, ExportHeightMap, ExportNoiseMap, ExportNormalMap, ExportMasks, ExportSlopeMap }

        /// <summary>
        /// Type of mask merge operation
        /// </summary>
        public enum MaskMergeType { AssignMask2IfGreaterThan, AssignMask2IfLessThan, AddMask2, MultiplyByMask2, SubtractMask2 }

        /// <summary>
        /// Type of rain map
        /// </summary>
        public enum ErosionRainType { Constant, ErodePeaks, ErodeValleys, ErodeSlopes }

        /// <summary>
        /// Type of curvature to calculate
        /// </summary>
        public enum CurvatureType { Average, Horizontal, Vertical }

        /// <summary>
        /// Type of aspect to calculate
        /// </summary>
        public enum AspectType { Aspect, Northerness, Easterness }

        /// <summary>
        /// Type of noise being generated
        /// </summary>
        public enum NoiseType { None, Perlin, Billow, Ridged }

        /// <summary>
        /// The fitness filter mode to apply when a texture is used to filter on fitness
        /// </summary>
        public enum ImageFitnessFilterMode { None, ImageGreyScale, ImageRedChannel, ImageGreenChannel, ImageBlueChannel, ImageAlphaChannel, TerrainTexture0, TerrainTexture1, TerrainTexture2, TerrainTexture3, TerrainTexture4, TerrainTexture5, TerrainTexture6, TerrainTexture7, PerlinNoise, BillowNoise, RidgedNoise }

        /// <summary>
        /// Classification of feature types - also used to load and save features
        /// </summary>
        public enum FeatureType { Hills, Islands, Mesas, Mountains, Plains, Rivers, Valleys};

        /// <summary>
        /// Types of borders to put on generated terrain
        /// </summary>
        public enum GeneratorBorderStyle { None, Mountains, Water }

        /// <summary>
        /// Different operation modes for the spawner - depending on what the spawner is doing, it will create different paint contexts for textures, heightmaps, etc.
        /// </summary>
        public enum SpawnerApplyBrushMode { Preview, Texture, TerrainDetail,
            TerrainTree,
            JustCacheContext
        }

        /// <summary>
        /// Obscure preset color, used as a flag to detect that the user has never set a custom color for themselves
        /// </summary>
        public static readonly Color spawnerInitColor = new Color(1f, 0.9849656f, 0.7688679f, 0f);

        public static readonly string HDRPPWSkyExperimental = "Procedural Worlds Sky is not yet available in HDRP, this will be available soon. If you would like to try it out then you can enable this feature by adding GAIA_EXPERIMENTAL to the scripting defines in Project Settings/Player settings.";
        /// <summary>
        /// 
        /// </summary>
        public static readonly string defaultSceneCullingProfile = "Gaia Default Scene Culling Profile.asset";

        public static readonly string HDRPEnvironmentObject = "HD Environment Volume";

        /// <summary>
        /// Determines whether the spawns should replace the exisiting instances, added to existing, or remove existing instances.
        /// </summary>
        public enum SpawnMode { Replace, Add, Remove };

        public enum HDRPDepthTest { Disabled, Never, Less, Equal, LessEqual, Greater, NotEqual, GreaterEqual, Always }

        /// <summary>
        /// The type of feature operation that will be used on the terrain by the stamper
        /// AddHeight - Will add height is stamped object exceeds height at that location
        /// RemoveHeight - Will remove height if the terrain is higher than stamp at that location
        /// Blend Height - Will blend the height difference from the existing terrain towards the stamp
        /// Set Height - Will set the terrain height at the given position 1:1 as it is in the stamp, no ifs and buts
        /// Add Height - Will add the stamp height on top of the existing terrain features
        /// Subtract Height - Will remove the stamp height from the existing terrain features
        /// Contrast - Will bring scale up height differences in the terrain to bring out details
        /// Hydraulic Erosion - Will simulate removing sediment from the terrain caused by rainfall over time
        /// </summary>
        public enum FeatureOperation { RaiseHeight=0, LowerHeight=1, BlendHeight=2, SetHeight=3, AddHeight=4, SubtractHeight=5, HeightTransform = 10, PowerOf = 11, Smooth = 12, Contrast =6, SharpenRidges=7, Terrace=8, HydraulicErosion=9, MixHeight=13 };

        /// <summary>
        /// The official UI names for the different Feature Operations, and their enum value in the dropdown menu. This dictionary can be used to display a multilevel popup in the UI and stil get the enum operation value regardless of order in the UI
        /// </summary>
        public static Dictionary<string, int> FeatureOperationNames = new Dictionary<string, int>()
         {
            {"Stamping/Raise Height", 0 },
            {"Stamping/Lower Height", 1 },
            {"Stamping/Blend Height", 2 },
            {"Stamping/Mix Height", 13 },
            {"Stamping/Set Height", 3 },
            {"Stamping/Add Height", 4 },
            {"Stamping/Subtract Height", 5 },
            {"Effects/HeightTransform", 10},
            {"Effects/PowerOf", 11 },
            {"Effects/Smooth", 12 },
            {"Effects/PRO Contrast", 6 },
            {"Effects/PRO Sharpen Ridges", 7 },
            {"Effects/PRO Terraces", 8 },
            {"Erosion/PRO HydraulicErosion", 9 }
        };

        /// <summary>
        /// A subset of the full Stamper Feature operations. Used in the Terrain Generator to allow the user only to select stamping operations
        /// </summary>
        public enum TerrainGeneratorFeatureOperation {  RaiseHeight = FeatureOperation.RaiseHeight,
                                                        LowerHeight = FeatureOperation.LowerHeight,
                                                        BlendHeight = FeatureOperation.BlendHeight,
                                                        MixHeight = FeatureOperation.MixHeight,
                                                        SetHeight = FeatureOperation.SetHeight,
                                                        AddHeight = FeatureOperation.AddHeight,
                                                        SubtractHeight = FeatureOperation.SubtractHeight
                                                    }
        /// <summary>
        /// The official UI names for the different Feature Operations, and their position in the dropdown menu. The order of this List must match with the order
        /// of the enum "FeatureOperation"
        /// </summary>
        public static string[] TerrainGeneratorFeatureOperationNames = {    "Stamping/Raise Height",
                                                                            "Stamping/Lower Height",
                                                                            "Stamping/Blend Height",
                                                                            "Stamping/Mix Height",
                                                                            "Stamping/Set Height",
                                                                            "Stamping/Add Height",
                                                                            "Stamping/Subtract Height" };


        /// <summary>
        /// Defines which water visualisation we want to display - the real, actual water (if available), the simple blue plane or none.
        /// </summary>
        public enum PreferredWaterVisualisation { ActualWater, Plane, None}


        /// <summary>
        /// Defines what the distance and area mask in the stamper can influence : Either the mask is applied to the stamp result as a whole,
        /// or the mask is applied only to the stamp itself. 
        /// </summary>
        public enum MaskInfluence { OnlyStampItself, TotalSpawnResult }

        
        public enum ImageMaskFilterMode  {Brightness, PROColorSelection, RedColorChannel, GreenColorChannel, BlueColorChannel, AlphaChannel }


        /// <summary>
        /// The different output types for the erosion image mask
        /// </summary>
        public enum ErosionMaskOutput { Sediment, WaterVelocity, WaterFlux }

        /// <summary>
        /// The area of effect for autospawners - local = only in tool range, world = perform worldspawn
        /// </summary>
        public enum AutoSpawnerArea { Local, World }

        /// <summary>
        /// The shape of the spawner
        /// </summary>
        public enum SpawnerShape { Box, Sphere }

        /// <summary>
        /// Enum for render update on water
        /// </summary>
        public enum RenderUpdateMode
        {
            OnEnable, Update, Interval, OnRender
        };

        public enum GlobalSystemMode { Gaia, ThirdParty, None }
        public enum ReflectionProbeResolution { Resolution16, Resolution32, Resolution64, Resolution128, Resolution256, Resolution512, Resolution1024, Resolution2048 }
        public enum ReflectionProbeRefreshModePW { OnAwake, EveryFrame, ViaScripting, ProbeManager }
        public enum ResolutionMulltiplier
        {
            Full,
            Half,
            Third,
            Quarter
        }

        /// <summary>
        /// The algorithm used to choose spawn locations
        /// RandomLocation - A random location will be chosen
        /// RandomLocationSeeded - A random start location will be chose, the next location will be based in seed throw radius
        /// EveryLocation - Every location in the spawner area will be checked
        /// EveryLocationJittered - Every location in the spawner area will be checked, and jitter added to break up the lines
        /// </summary>
        public enum SpawnerLocation { RandomLocation, RandomLocationClustered, EveryLocation, EveryLocationJittered }

        /// <summary>
        /// The algorithm used at spawn locations to determine suitability
        /// PointCheck - Just that location is checked
        /// BoundedAreaCheck - The entire area is checked - slower but good for large object placement
        /// </summary>
        public enum SpawnerLocationCheckType { PointCheck, BoundedAreaCheck }

        /// <summary>
        /// The algorithm which defines which rule will be selected in spawn location. Regardless of whether
        ///   selected or not, the rule will still only run if it exceeds its minimum fitness settings, and 
        ///   didnt randomly fail (failure rate).
        /// All - All rules will be run at this location
        /// Fittest - Only the fittest rule will be run at this location
        /// WeightedFittest - The fittest rule will most likely run, then the next fittest, distributed by relative fitness
        /// Random - A random rule will be chosen and evaluated to run
        /// </summary>
        public enum SpawnerRuleSelector { All, Fittest, WeightedFittest, Random }

        /// <summary>
        /// The type of resource that the spawner will use in a spawn rule. Make sure to keep IDs consistent when adding new resource types
        /// </summary>
        public enum SpawnerResourceType { TerrainTexture=0, TerrainDetail=1, TerrainTree=2, GameObject=3, SpawnExtension=4, Probe=7, 
            StampDistribution=5, WorldBiomeMask=6
        }

        /// <summary>
        /// The type of resource that is available for the user to select in a regular spawner
        /// </summary>
        public enum RegularSpawnerResourceType
        {
            TerrainTexture=0, TerrainDetail=1, TerrainTree=2, GameObject=3, SpawnExtension=4, Probe=7
        }

        /// <summary>
        /// A subset of the spawner resource types that can be imported from the terrain
        /// </summary>
        public enum ImportableResourceType { TerrainTexture=0,TerrainDetail=1,TerrainTree=2 }

        /// <summary>
        /// A subset of the spawner resource types that can be dropped on the spawner to create new rules
        /// </summary>
        public enum DroppableResourceType { TerrainTexture = 0, TerrainDetail = 1, TerrainTree = 2, GameObject = 3 }

        /// <summary>
        /// Defines whether resources from the terrain should be added as new spawn rules, or should replace the existing rules.
        /// </summary>
        public enum ImportableResourceMode { AddRules, ReplaceRules}

        /// <summary>
        /// Used by system determine what constitutes a valid virgin terrain height check threshold
        /// </summary>
        public static float VirginTerrainCheckThreshold = 0.01f;

        /// <summary>
        /// Determines how many textures can be previewed at the same time in the spawner
        /// </summary>
        public static int maxPreviewedTextures = 5;

        /// <summary>
        /// Controls if the spawner should place spawned game objects parented to each terrain, or to a single transform in the scene only.
        /// </summary>
        public enum SpawnerTargetMode
        {
            Terrain, SingleTransform
        }

        /// <summary>
        /// Name of the standard GameObject spawn transform under which new game objects will be created from the spawner
        /// </summary>
        public static string defaultGOSpawnTarget = "Gaia Game Object Spawns";

        /// <summary>
        /// Name of the Game Object that holds all designe time tools
        /// </summary>
        public static string gaiaToolsObject = "Gaia Tools";

        /// <summary>
        /// Name of the Game Object that holds runtime objects such as water and lighting etc.
        /// </summary>
        public static string gaiaRuntimeObject = "Gaia Runtime";

        /// <summary>
        /// Name of the Game Object that holds gaia stopwatch data
        /// </summary>
        public static string gaiaStopWatchDataObject = "Gaia Stopwatch";

        /// <summary>
        /// Name of the Game Object that holds the gaia terrains if created in a single scene.
        /// </summary>
        public static string gaiaTerrainObjects = "Gaia Terrains";

        /// <summary>
        /// Name of the Game Object that holds the gaia terrain exports from the Terrain Exporter
        /// </summary>
        public static string gaiaTerrainExportObjects = "Gaia Terrain Exports";

        /// <summary>
        /// Name of the Game Object that controls the loading of terrains on demand.
        /// </summary>
        public static string gaiaTerrainLoaderManagerObjects = "Terrain Loader Manager";

        /// <summary>
        /// Name of the Game Object that holds all player related stuff.
        /// </summary>
        public static string gaiaPlayerObject = "Gaia Player";

        /// <summary>
        /// Name of the Game Object that holds temporary sessio.
        /// </summary>
        public static string gaiaTempSessionToolsObject = "Temp Session Tools";

        /// <summary>
        /// Name of the Game Object that holds the Gaia Water.
        /// </summary>
        public static string gaiaWaterObject = "Gaia Water";

        /// <summary>
        /// Name of the Game Object that holds the Gaia Water.
        /// </summary>
        public static string gaiaLightingObject = "Gaia Lighting";

        /// <summary>
        /// Name of the Game Object that holds the Gaia Audio.
        /// </summary>
        public static string gaiaAudioObject = "Gaia Audio";

        /// <summary>
        /// Name of the World Map Game Object
        /// </summary>
        public static string worldDesignerObject = "World Designer";

        /// <summary>
        /// Name of the World Map Random Generator Object
        /// </summary>
        public static string worldGenerator = "World Generator";

        /// <summary>
        /// Name of the World Biome Masks Object
        /// </summary>
        public static string worldBiomeMasks = "World Biome Masks";

        /// <summary>
        /// Name of the World Map Stamper Object
        /// </summary>
        public static string worldMapStamper = "World Map Stamper";

        /// <summary>
        /// Name of the World Map Temporary Tools Object
        /// </summary>
        public static string worldTempTools = "World Map Temp Tools";

        /// <summary>
        /// Name of the Stamper that will display the preview on the local map
        /// </summary>
        public static string worldMapLocalStamper = "Local Map Stamp Preview";


        /// <summary>
        /// Name of the Stamper that will display the preview on the world map
        /// </summary>
        public static string worldMapWorldStamper = "World Map Stamp Preview";

        /// <summary>
        /// Prefix for the World Map Terrain Object
        /// </summary>
        public static string worldMapTerrainPrefix = "World Map";

        /// <summary>
        /// Name of the Container object that contains the stamp tokens on the world map.
        /// </summary>
        public static string worldMapStampTokenSpawnTarget = "Stamp Token";

        /// <summary>
        /// Name of the Water Surface Game Object for the Gaia Water.
        /// </summary>
        public static string waterSurfaceObject = "Water Surface";
        public static string gaiaUnderwaterMaterial = "Gaia Ocean Underwater";
        public static string GaiaThunderObjectName = "Gaia Thunder";
        public static string GaiaThunderPrefabName = "Gaia Thunder.prefab";

        /// <summary>
        /// Name of the Stamper Game Object.
        /// </summary>
        public static string StamperObject = "Stamper";

        /// <summary>
        /// Name of the Gaia Weather Game Object.
        /// </summary>
        public static string gaiaWeatherObject = "Gaia Weather";

        public static string gaiaPlanarReflections = "Gaia Planar Reflections";
        public static string gaiaHDRPPlanarReflections = "Gaia HDRP Planar Reflections";

        public static string gaiaPlanarReflectionsCamera = "Gaia Planar Reflections Camera";

        public static string gaiaFileFormatAsset = ".asset";

        /// <summary>
        /// Name of the Maintenance Token object that triggers a maintenance run if present in the Data directory
        /// </summary>
        public static string maintenanceTokenFilename = "MaintenanceToken.dat";

        /// <summary>
        /// Name of the FlyCam Player
        /// </summary>
        public static string playerFlyCamName = "FlyCam";

        /// <summary>
        /// Name of the First Person Player
        /// </summary>
        public static string playerFirstPersonName = "FPSController";

        /// <summary>
        /// Name of the XRPlayer
        /// </summary>
        public static string playerXRName = "XRController";

        /// <summary>
        /// Name of the car controller
        /// </summary>
        public static string m_carPlayerPrefabName = "Gaia Car";


        /// <summary>
        /// Name of the Third Person Player
        /// </summary>
        public static string playerThirdPersonName = "ThirdPersonController";

        public static string underwaterTransitionObjectName = "Underwater Transition Post Processing";

        public static string underwaterPostProcessingName = "Underwater Post Processing";

        public static string underwaterHorizonName = "Underwater Horizon";

        public static string underwaterEffectsName = "Underwater Effects";

        public static string lightingSettingsName = "Gaia Lighting Settings.asset";

        public static string newSpawnRuleName = "New Spawn Rule";

        public static string SourceTerrainBackupObject = "Source Terrain Backup";

        /// <summary>
        /// Name of the Label used to identify all the spawners that should appear in the Gaia Manager
        /// </summary>
        public static string gaiaManagerSpawnerLabel = "GaiaManagerSpawner";

        /// <summary>
        /// Name of the loading screen prefab object
        /// </summary>
        public static string loadingScreenName = "Gaia Loading Screen";

        /// <summary>
        /// The minimum possible location increment in Spawn Rules
        /// </summary>
        public static float minlocationIncrement = 0.01f;

        /// <summary>
        /// File type to use when saving images
        /// </summary>
        public enum ImageFileType {  Exr, Jpg, Png, Tga }

        public enum ProceduralSkySunTypes { None, Simple, HighQuality }
        public enum CloudRenderQueue { Background1000, Geometry2000, AlphaTest2450, Transparent3000 }
        public enum WaterAutoUpdateMode { Interval, SceneConditions }

        /// <summary>
        /// Image formatting defaults
        /// </summary>
        public const TextureFormat defaultTextureFormat = TextureFormat.RGBA32;
        public const TextureFormat fmtHmTextureFormat = TextureFormat.RGBA32;
        public const TextureFormat fmtRGBA32 = TextureFormat.RGBA32;

        /// <summary>
        /// Storage formats
        /// </summary>
        public enum StorageFormat { PNG, JPG }
        public const StorageFormat defaultImageStorageFormat = StorageFormat.PNG;

        /// <summary>
        /// Image channels
        /// </summary>
        public enum ImageChannel { R, G, B, A }
        public const ImageChannel defaultImageStorageChannel = ImageChannel.R;

        public static TextureFormat[] Valid16BitFormats = new TextureFormat[10] {TextureFormat.ARGB4444, TextureFormat.R16, TextureFormat.RFloat, TextureFormat.RGB565, TextureFormat.RGBA4444, TextureFormat.RGBAFloat, TextureFormat.RGBAHalf, TextureFormat.RGFloat, TextureFormat.RGHalf, TextureFormat.RHalf };

        /// <summary>
        /// URL for Gaia News messages
        /// </summary>
        public static string GaiaNewsURL = "https://www.procedural-worlds.com/unity-asset-news/gaia/";

        /// <summary>
        /// URL for Gaia Pro News messages
        /// </summary>
        public static string GaiaProNewsURL = "https://www.procedural-worlds.com/unity-asset-news/gaia-pro/";

        /// <summary>
        /// Prefix for LOD'ed Mesh Terrains created by the terrain exporter
        /// </summary>
        public static string MeshTerrainLODGroupPrefix = "LODMesh";


        /// <summary>
        /// Official Name for a Mesh terrain - used on the Game Object, scene paths, etc.
        /// </summary>
        public static string MeshTerrainName = "Mesh Terrains";

        /// <summary>
        /// Official Name for the impostor terrain - used on the Game Object, scene paths, etc.
        /// </summary>
        public static string ImpostorTerrainName = "Impostor";

        /// <summary>
        /// Name for the directional light game object used in orthographic bakes
        /// </summary>
        public static string BakeDirectionalLight = "OrthoBakeLight";

        /// <summary>
        /// Name for the Gaia Screenshotter object
        /// </summary>
        public static string gaiaScreenshotter = "Screen Shotter";

        /// <summary>
        /// HTML Color string for the highlighting of loader settings in the Gaia Tools. Used when selecting a loader from the Terrain Loader Manager
        /// to highlight where the loading specific settings are on the tool in question.
        /// </summary>
        public static string TerrainLoadingSettingsHighlightColor = "ffa100cc";
    }
}

