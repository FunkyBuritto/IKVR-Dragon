// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using System.Collections.Generic;
using static Gaia.GaiaConstants;
using UnityEditor;
using System.Linq;
using System;
using System.IO;

/*
 * Scriptable Object containing settings for a Spawner
 */

namespace Gaia
{
    /// <summary> Contains information about a Sequence of clips to play and how </summary>
    [CreateAssetMenu(menuName = "Procedural Worlds/Gaia/Spawner Settings")]
    [System.Serializable]
    public class SpawnerSettings : ScriptableObject, ISerializationCallbackReceiver
    {

        #region Public Variables

        /// <summary>
        /// The resources associated with the spawner
        /// </summary>
        public GaiaResource m_resources = new GaiaResource();

        ///// <summary>
        ///// Spanwer x location - done this way to expose in the editor as a simple slider
        ///// </summary>
        public float m_x = 0f;

        ///// <summary>
        ///// Spawner y location - done this way to expose in the editor as a simple slider
        ///// </summary>
        public float m_y = 50f;

        ///// <summary>
        ///// Spawner z location - done this way to expose in the editor as a simple slider
        ///// </summary>
        public float m_z = 0f;

        ///// <summary>
        ///// Spawner width - this is the horizontal scaling factor - applied to both x & z
        ///// </summary>
        public float m_width = 10f;

        ///// <summary>
        ///// Spawner height - this is the vertical scaling factor
        ///// </summary>
        public float m_height = 10f;

        ///// <summary>
        ///// Spawner rotation
        ///// </summary>
        public float m_rotation = 0f;


        /// <summary>
        /// Is this spawner intended to be used on world map terrains?
        /// </summary>
        public bool m_isWorldmapSpawner = false;

        /// <summary>
        /// Range of the spawn area
        /// </summary>
        public float m_spawnRange = 500f;

        /// <summary>
        /// Should the random seed be generated anew for each spawn?
        /// </summary>
        public bool m_generateRandomSeed = true;

        /// <summary>
        /// Seed value for the random number generator
        /// </summary>
        public int m_randomSeed = 0;

        /// <summary>
        /// The spawn Density which controls the global object density for all spawners. This value is managed in the session
        /// for the scene, but also needs to be stored in the spawner settings for correct session playback.
        /// </summary>
        public float m_spawnDensity = 1f;

        /// <summary>
        /// The path this resources file came from
        /// </summary>
        public string m_resourcesPath;


        /// <summary>
        /// The GUID of the last used resources file; Used while saving and loading to save / load the resource file reference
        /// </summary>
        //public string m_resourcesGUID;


        /// <summary>
        /// The prefabs that can be spawned and their settings
        /// </summary>
        public List<SpawnRule> m_spawnerRules = new List<SpawnRule>();

        /// <summary>
        /// Whether or not to show gizmos
        /// </summary>
        public bool m_showGizmos = true;

        /// <summary>
        /// Whether or not to show debug messages
        /// </summary>
        public bool m_showDebug = false;

        /// <summary>
        /// Whether or not to show the terrain helper
        /// </summary>
        public bool m_showTerrainHelper = true;


        public SpawnMode m_spawnMode = SpawnMode.Replace;

        [SerializeField]
        private ImageMask[] imageMasks = new ImageMask[0];

        /// <summary>
        /// Toggle for trees in the spawn clear controls
        /// </summary>
        public bool m_clearSpawnsToggleTrees = false;

        /// <summary>
        /// Toggle for terrain details in the spawn clear controls
        /// </summary>
        public bool m_clearSpawnsToggleDetails = false;

        /// <summary>
        /// Toggle for game objects in the spawn clear controls
        /// </summary>
        public bool m_clearSpawnsToggleGOs = false;

        /// <summary>
        /// Toggle for spawn extensions in the spawn clear controls
        /// </summary>
        public bool m_clearSpawnsToggleSpawnExtensions = false;

        /// <summary>
        /// Toggle for probes in the spawn clear controls
        /// </summary>
        public bool m_clearSpawnsToggleProbes = false;

        /// <summary>
        /// Setting to determine which terrains should be affected by a clearing action
        /// </summary>
        public ClearSpawnFor m_clearSpawnsFor = ClearSpawnFor.AllTerrains;

        /// <summary>
        /// Setting to determine which prototypes should be deleted in a clearing action
        /// </summary>
        public ClearSpawnFrom m_clearSpawnsFrom = ClearSpawnFrom.AnySource;

        /// <summary>
        /// The last GUID of the settings file used to save these settings.
        /// </summary>
        public string m_lastGUIDSaved = "";



        //Using a property to make sure the image mask list is always initialized
        //<summary>All image filters that are being applied in this spawning process</summary>

        public ImageMask[] m_imageMasks
        {
            get
            {
                if (imageMasks == null)
                {
                    imageMasks = new ImageMask[0];
                }
                return imageMasks;
            }
            set
            {
                imageMasks = value;
            }
        }

        //public  float m_powerOf;



        #endregion
        #region Unity Events
        private void Awake()
        {
        }

        #endregion
        #region Serialization

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
        }

        #endregion

        /// <summary>
        /// Removes References to Texture2Ds in Image masks. The image mask will still remember the GUID of that texture to load it when needed.
        /// Call this when you are "done" with the spawner settings to free up memory caused by these references.
        /// </summary>
        public void ClearImageMaskTextures()
        {
            foreach (ImageMask im in m_imageMasks)
            {
                im.FreeTextureReferences();
            }
            foreach (SpawnRule sr in m_spawnerRules)
            {
                foreach (ImageMask im in sr.m_imageMasks)
                {
                    im.FreeTextureReferences();
                }
            }
            Resources.UnloadUnusedAssets();
        }

        public void RefreshGUID()
        {
#if UNITY_EDITOR
            string currentGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
            if (!m_lastGUIDSaved.Equals(currentGUID))
            {
                m_lastGUIDSaved = currentGUID;
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
#endif
        }

        public Spawner CreateSpawner(bool autoAddResources = false, Transform targetTransform = null)
        {
            //Find or create gaia
            GameObject gaiaObj = GaiaUtils.GetGaiaGameObject();
            GameObject spawnerObj = new GameObject(this.name);
            spawnerObj.AddComponent<Spawner>();
            if (targetTransform != null)
            {
                spawnerObj.transform.parent = targetTransform;
            }
            else
            {
                spawnerObj.transform.parent = gaiaObj.transform;
            }

            Spawner spawner = spawnerObj.GetComponent<Spawner>();
            spawner.LoadSettings(this);
            //spawner.m_settings.m_resources = (GaiaResource)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(this.m_resourcesGUID), typeof(GaiaResource));
            if (autoAddResources)
            {
                TerrainLayer[] terrainLayers = new TerrainLayer[0];
                DetailPrototype[] terrainDetails = new DetailPrototype[0];
                TreePrototype[] terrainTrees = new TreePrototype[0];
                GaiaDefaults.GetPrototypes(new List<BiomeSpawnerListEntry>() { new BiomeSpawnerListEntry() { m_spawnerSettings = this, m_autoAssignPrototypes = true } }, ref terrainLayers, ref terrainDetails, ref terrainTrees, Terrain.activeTerrain);

                foreach (Terrain t in Terrain.activeTerrains)
                {
                    GaiaDefaults.ApplyPrototypesToTerrain(t, terrainLayers, terrainDetails, terrainTrees);
                }
            }

            //We need to check the texture prototypes in this spawner against the already created terrain layers for this session
            //- otherwise the spawner will not know about those in subsequent spawns and might create unneccessary additional layers

            //Get a list of all exisiting Terrain Layers for this session
            string path = GaiaDirectories.GetTerrainLayerPath();
#if UNITY_EDITOR
            AssetDatabase.ImportAsset(path);
            if (Directory.Exists(path))
            {
                string[] allLayerGuids = AssetDatabase.FindAssets("t:TerrainLayer", new string[1] { path });
                List<TerrainLayer> existingTerrainLayers = new List<TerrainLayer>();
                foreach (string guid in allLayerGuids)
                {
                    try
                    {
                        TerrainLayer layer = (TerrainLayer)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(TerrainLayer));
                        if (layer != null)
                        {
                            existingTerrainLayers.Add(layer);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message == "")
                        { }
                    }

                }
                foreach (SpawnRule sr in spawner.m_settings.m_spawnerRules)
                {
                    if (sr.m_resourceType == SpawnerResourceType.TerrainTexture)
                    {
                        ResourceProtoTexture protoTexture = spawner.m_settings.m_resources.m_texturePrototypes[sr.m_resourceIdx];
                        //if a terrainLayer with these properties exist we can assume it fits to the given spawn rule
                        TerrainLayer terrainLayer = existingTerrainLayers.FirstOrDefault(x => x.diffuseTexture == protoTexture.m_texture &&
                                                            x.normalMapTexture == protoTexture.m_normal &&
                                                            x.tileOffset == new Vector2(protoTexture.m_offsetX, protoTexture.m_offsetY) &&
                                                            x.tileSize == new Vector2(protoTexture.m_sizeX, protoTexture.m_sizeY)
                                                            );
                        if (terrainLayer != null)
                        {
                            protoTexture.m_LayerGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(terrainLayer));
                        }
                    }
                }
            }
#endif


            foreach (SpawnRule rule in spawner.m_settings.m_spawnerRules)
            {
                rule.m_spawnedInstances = 0;
            }

            if (Terrain.activeTerrains.Where(x => !TerrainHelper.IsWorldMapTerrain(x)).Count() > 0)
            {
                spawner.FitToAllTerrains();
            }
            //else
            //{
            //    spawner.FitToTerrain();
            //}
            return spawner;
        }

    }
}
