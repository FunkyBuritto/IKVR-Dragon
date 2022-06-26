using ProceduralWorlds.WaterSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
using static Gaia.GaiaConstants;
namespace Gaia
{
    /// <summary>
    /// A generic spawning system.
    /// </summary>
    [System.Serializable]
    public class BiomeController : MonoBehaviour
    {

        [SerializeField]
        private BiomeControllerSettings settings;
        /// <summary>
        /// The current biome controller settings
        /// </summary>
        public BiomeControllerSettings m_settings
        {
            get
            {
                if (settings == null && gameObject!=null)
                {
                    settings = ScriptableObject.CreateInstance<BiomeControllerSettings>();
                    settings.name = gameObject.name;
                }
                return settings;
            }
            set
            {
                settings = value;
            }

        }
        public bool m_autoSpawnersToggleAll = false;
        public List<AutoSpawner> m_autoSpawners = new List<AutoSpawner>();
#if UNITY_POST_PROCESSING_STACK_V2
        public PostProcessProfile m_postProcessProfile;
        public BiomePostProcessingVolumeSpawnMode m_ppVSpawnMode = BiomePostProcessingVolumeSpawnMode.Add;
#endif
        public bool m_autoSpawnRequested;
        public bool m_drawPreview;
        public bool m_biomePreviewDirty;
        private Terrain m_lastActiveTerrain;
        private float m_minWorldHeight;
        private float m_maxWorldHeight;
        private RenderTexture m_cachedPreviewRT;

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

        public AutoSpawnerArea m_autoSpawnerArea = AutoSpawnerArea.Local;
        public bool m_showSeaLevelPlane = true;
        public bool m_showSeaLevelinPreview = true;
        public bool m_showBoundingBox = true;
        public string m_oldName;
        public bool m_changesMadeSinceLastSave;
        public bool m_biomeWasSpawned;

        public bool m_highlightLoadingSettings;
        public long m_highlightLoadingSettingsStartedTimeStamp;

#if GAIA_PRO_PRESENT
        private TerrainLoader m_terrainLoader;
        public LoadMode m_loadTerrainMode = LoadMode.Disabled;
        public int m_impostorLoadingRange;
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

        /// <summary>
        /// Adds child game objects that contain a spawner component to the list of spawners
        /// </summary>
        public void UpdateSpawnerList()
        {
            for (int i =0; i<transform.childCount; i++)
            {
                Transform t = transform.GetChild(i);
                Spawner spawner = t.GetComponent<Spawner>();
                if (spawner != null)
                {
                    if (m_autoSpawners.Find(x => x.spawner == spawner) == null)
                    {
                        m_autoSpawners.Insert(i, new AutoSpawner() { isActive = true, spawner = spawner });
                    }
                }
            }
        }

        public void HighlightLoadingSettings()
        {
            m_highlightLoadingSettings = true;
            m_highlightLoadingSettingsStartedTimeStamp = GaiaUtils.GetUnixTimestamp();
        }


        public void UpdateAutoLoadRange()
        {
#if GAIA_PRO_PRESENT
            if (m_loadTerrainMode != LoadMode.Disabled)
            {
                float width = m_settings.m_range * 2f;
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

        //void OnEnable()
        //{
        //    if (m_gaiaSettings == null)
        //    {
        //        m_gaiaSettings = GaiaUtils.GetGaiaSettings();
        //    }
        //}

        /// <summary>
        /// Position and fit the spawner to the terrain
        /// </summary>
        public void FitToTerrain(Terrain t = null)
        {
            if (t == null)
            {
                t = Gaia.TerrainHelper.GetTerrain(transform.position,false);
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
                m_settings.m_range = b.extents.x;
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
                m_settings.m_range = (float)b.extents.x;
            }
        }


        private void OnDestroy()
        {
            m_settings.ClearImageMaskTextures();
        }

        void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            if (Selection.activeObject == gameObject)
            {
                if (m_showBoundingBox)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(transform.position, new Vector3(m_settings.m_range * 2f, m_settings.m_range * 2f, m_settings.m_range * 2f));
                }

                //Water
                if (m_showSeaLevelPlane && PWS_WaterSystem.Instance == null)
                {
                    BoundsDouble bounds = new BoundsDouble();
                    if (TerrainHelper.GetTerrainBounds(ref bounds) == true)
                    {
                        bounds.center = new Vector3Double(bounds.center.x, SessionManager.GetSeaLevel(), bounds.center.z);
                        bounds.size = new Vector3Double(bounds.size.x, 0.05f, bounds.size.z);
                        Gizmos.color = new Color(Color.blue.r, Color.blue.g, Color.blue.b, Color.blue.a / 4f);
                        Gizmos.DrawCube(bounds.center, bounds.size);
                    }
                }
            }
#endif
        }


        public void LoadSettings(BiomeControllerSettings settingsToLoad)
        {
            m_settings.ClearImageMaskTextures();
            //set position according to the stored settings
            transform.position = new Vector3(settingsToLoad.m_x, settingsToLoad.m_y, settingsToLoad.m_z);

            //Set existing settings = null to force a new scriptable object
            m_settings = null;
            m_settings = Instantiate(settingsToLoad);

            //Try to look up all collision layer masks by their name where possible - layer orders could be different from when the biome controller was saved.
            foreach (ImageMask imageMask in m_settings.m_imageMasks.Where(x => x.m_operation == ImageMaskOperation.CollisionMask))
            {
                imageMask.TryRefreshCollisionMask();
            }
        }

        public void LoadFromPreset(BiomePreset presetToLoad)
        {
            try
            {
                presetToLoad.RefreshSpawnerListEntries();
                m_autoSpawners.Clear();
                //Remove all child spawners
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(transform.GetChild(i).gameObject);
                }
                
                //Track created spawners 
                List<Spawner> createdSpawners = new List<Spawner>();
                int totalCount = presetToLoad.m_spawnerPresetList.Count();
                int currentCount = 0;
                foreach (BiomeSpawnerListEntry spawnerListEntry in presetToLoad.m_spawnerPresetList)
                {
                    createdSpawners.Add(spawnerListEntry.m_spawnerSettings.CreateSpawner(false, transform));
                    //GaiaUtils.DisplayProgressBarNoEditor("Creating Tools", "Creating Biome " + this.name, ++currentStep / totalSteps);
                    if (ProgressBar.Show(ProgressBarPriority.CreateBiomeTools, "Loading Biome Preset", "Creating Tools", ++currentCount, totalCount, false, true))
                    {
                        break;
                    }
                }
                if (createdSpawners.Count > 0)
                {
                    m_settings.m_range = createdSpawners[0].m_settings.m_spawnRange;
                }

                foreach (Spawner spawner in createdSpawners)
                {
                    m_autoSpawners.Add(new AutoSpawner() { isActive = true, status = AutoSpawnerStatus.Initial, spawner = spawner });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error while loading biome preset settings, Message: " + ex.Message + ", Stack Trace: " + ex.StackTrace);
            }
            finally
            {
                ProgressBar.Clear(ProgressBarPriority.CreateBiomeTools);
            }
        }

        public Terrain GetCurrentTerrain()
        {
            Terrain currentTerrain = Gaia.TerrainHelper.GetTerrain(transform.position, false);
            //Check if the stamper is over a terrain currently
            //if not, we will draw a preview based on the last active terrain we were over
            //if that is null either we can't draw a stamp preview
            if (currentTerrain)
            {
                //Update last active terrain with current
                if (m_lastActiveTerrain != currentTerrain)
                {
                    //if the current terrain is a new terrain, we should refresh the min max values in case this terrain has never been calculated before
                    SessionManager.GetWorldMinMax(ref m_minWorldHeight, ref m_maxWorldHeight);
                }
                m_lastActiveTerrain = currentTerrain;
            }
            //if not, we check if there is any terrain within the bounds of the biome spawner
            if (currentTerrain == null)
            {
                float width = m_settings.m_range * 2f;
                Bounds stamperBounds = new Bounds(transform.position, new Vector3(width, width, width));

                foreach (Terrain t in Terrain.activeTerrains)
                {
                    //only look at this terrain if it matches the selected world map mode
                    if (!TerrainHelper.IsWorldMapTerrain(t) && t.terrainData!=null)
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

        public void UpdateMinMaxHeight()
        {
            SessionManager.GetWorldMinMax(ref m_minWorldHeight, ref m_maxWorldHeight, false);
            float seaLevel = SessionManager.GetSeaLevel(false);
            //Iterate through all image masks and set up the current min max height
            //This is fairly important to display the height-dependent mask settings correctly
            foreach (ImageMask mask in m_settings.m_imageMasks)
            {
                mask.m_maxWorldHeight = m_maxWorldHeight;
                mask.m_minWorldHeight = m_minWorldHeight;
                mask.m_seaLevel = seaLevel;
            }
            ImageMask.CheckMaskStackForInvalidTextureRules("Biome Controller", this.name, m_settings.m_imageMasks);
        }

            public void DrawBiomePreview()
        {
            if (m_drawPreview)
            {

                //Set up a multi-terrain operation once, all rules can then draw from the data collected here
                Terrain currentTerrain = GetCurrentTerrain();
                if (currentTerrain == null)
                {
                    return;
                }

                GaiaMultiTerrainOperation operation = new GaiaMultiTerrainOperation(currentTerrain, transform, m_settings.m_range * 2f);
                operation.GetHeightmap();


                //only re-generate all textures etc. if settings have changed and the preview is dirty, otherwise we can just use the cached textures
                if (m_biomePreviewDirty == true)
                {
                    //Get additional op data (required for certain image masks)
                    operation.GetNormalmap();
                    operation.CollectTerrainBakedMasks();

                    //Clear texture cache first
                    if (m_cachedPreviewRT != null)
                    {
                        m_cachedPreviewRT.Release();
                        DestroyImmediate(m_cachedPreviewRT);
                    }

                    m_cachedPreviewRT = new RenderTexture(operation.RTheightmap);
                    RenderTexture currentRT = RenderTexture.active;
                    RenderTexture.active = m_cachedPreviewRT;
                    GL.Clear(true, true, Color.black);
                    RenderTexture.active = currentRT;

                    Graphics.Blit(ApplyBrush(operation), m_cachedPreviewRT);
                    RenderTexture.active = currentRT;
                    //Everything processed, preview not dirty anymore
                    m_biomePreviewDirty = false;
                }

                //Now draw the preview according to the cached textures
                Material material = GaiaMultiTerrainOperation.GetDefaultGaiaSpawnerPreviewMaterial();
                material.SetInt("_zTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);

                //assign the first color texture in the material
                material.SetTexture("_colorTexture0", m_cachedPreviewRT);

                //remove all other potential color textures, there can be caching issues if other visualisers were used in the meantime

                for (int colorIndex = 1; colorIndex < GaiaConstants.maxPreviewedTextures; colorIndex++)
                {
                    material.SetTexture("_colorTexture" + colorIndex, null);
                }



                //set the color
                material.SetColor("_previewColor0", m_settings.m_visualisationColor);

                Color seaLevelColor = GaiaSettings.m_stamperSeaLevelTintColor;
                if (!m_showSeaLevelinPreview)
                {
                    seaLevelColor.a = 0f;
                }
                material.SetColor("_seaLevelTintColor", seaLevelColor);
                material.SetFloat("_seaLevel", SessionManager.m_session.m_seaLevel);
                operation.Visualize(MultiTerrainOperationType.Heightmap, operation.RTheightmap, material, 1);

                //Clean up
                operation.CloseOperation();
                //Clean up temp textures
                GaiaUtils.ReleaseAllTempRenderTextures();
            }
        }



        private RenderTexture ApplyBrush(GaiaMultiTerrainOperation operation, MultiTerrainOperationType opType = MultiTerrainOperationType.Heightmap)
        {
            Terrain currentTerrain = GetCurrentTerrain();

            RenderTextureDescriptor rtDescriptor = operation.RTheightmap.descriptor;

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

            RenderTexture inputTexture = RenderTexture.GetTemporary(rtDescriptor);

            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = inputTexture;
            GL.Clear(true, true, Color.white);
            RenderTexture.active = currentRT;

            //Iterate through all image masks and set up the current paint context in case the shader uses heightmap data
            foreach (ImageMask mask in m_settings.m_imageMasks)
            {
                mask.m_multiTerrainOperation = operation;
                mask.m_seaLevel = SessionManager.GetSeaLevel();
                mask.m_maxWorldHeight = m_maxWorldHeight;
                mask.m_minWorldHeight = m_minWorldHeight;
            }

            //Get the combined masks for the biome 
            RenderTexture biomeOutputTexture = RenderTexture.GetTemporary(rtDescriptor);
            Graphics.Blit(ImageProcessing.ApplyMaskStack(inputTexture, biomeOutputTexture, m_settings.m_imageMasks, ImageMaskInfluence.Local), biomeOutputTexture);
            ReleaseRenderTexture(inputTexture);
            inputTexture = null;
            return biomeOutputTexture;
        }


        private void ReleaseRenderTexture(RenderTexture texture)
        {
            if (texture != null)
            {
                RenderTexture.ReleaseTemporary(texture);
                texture = null;
            }
        }

        public void RemoveForeignTrees(List<SpawnerSettings> biomeSpawnerSettings, List<string> validTerrainNames = null)
        {
            ProgressBar.Show(ProgressBarPriority.BiomeRemoval, "Removing Foreign Trees", "Removing...");
            //Collect the "allowed" tree prefabs in a list, any tree that is not in this list will be affected by the removal operation
            List<GameObject> domesticTreePrefabs = new List<GameObject>();
            //List<TreePrototype> treeProtosToRemove = new List<TreePrototype>();
            Terrain currentTerrain = GetCurrentTerrain();
            foreach (SpawnerSettings spawnerSettings in biomeSpawnerSettings)
            {
                foreach (SpawnRule sr in spawnerSettings.m_spawnerRules)
                {
                    if (sr.m_resourceType == GaiaConstants.SpawnerResourceType.TerrainTree)
                    {
                        domesticTreePrefabs.Add(spawnerSettings.m_resources.m_treePrototypes[sr.m_resourceIdx].m_desktopPrefab);
                    }
                }
            }

            GaiaMultiTerrainOperation operation = new GaiaMultiTerrainOperation(currentTerrain, transform, m_settings.m_range * 2f, false, validTerrainNames);
            operation.GetHeightmap();
            operation.GetNormalmap();
            operation.CollectTerrainDetails();
            operation.CollectTerrainTrees();
            operation.CollectTerrainGameObjects();
            operation.CollectTerrainBakedMasks();

            operation.RemoveForeignTrees(ApplyBrush(operation,MultiTerrainOperationType.Tree), domesticTreePrefabs, m_settings.m_removeForeignTreesStrength);
            ProgressBar.Clear(ProgressBarPriority.BiomeRemoval);
           operation.CloseOperation();
        }

        public void RemoveForeignGameObjects(List<SpawnerSettings> biomeSpawnerSettings, List<string> validTerrainNames=null)
        {
            ProgressBar.Show(ProgressBarPriority.BiomeRemoval, "Removing Foreign GameObjects", "Removing...");
            List<ResourceProtoGameObjectInstance> knownProtoInstances = new List<ResourceProtoGameObjectInstance>();
            List<ResourceProtoGameObjectInstance> GoProtoInstancesToRemove = new List<ResourceProtoGameObjectInstance>();
            Terrain currentTerrain = GetCurrentTerrain();
            foreach (SpawnerSettings spawnerSettings in biomeSpawnerSettings)
            {
                foreach (SpawnRule sr in spawnerSettings.m_spawnerRules)
                {
                    if (sr.m_resourceType == GaiaConstants.SpawnerResourceType.GameObject)
                    {
                        foreach (ResourceProtoGameObjectInstance instance in spawnerSettings.m_resources.m_gameObjectPrototypes[sr.m_resourceIdx].m_instances)
                        {
                            knownProtoInstances.Add(instance);
                        }
                    }
                }
            }

            GaiaMultiTerrainOperation operation = new GaiaMultiTerrainOperation(currentTerrain, transform, m_settings.m_range * 2f, false, validTerrainNames);
            operation.GetHeightmap();
            operation.GetNormalmap();
            operation.CollectTerrainDetails();
            operation.CollectTerrainTrees();
            operation.CollectTerrainGameObjects();
            operation.CollectTerrainBakedMasks();

            int protoIndex = 0;
            var allSpawners = Resources.FindObjectsOfTypeAll<Spawner>();
            foreach (Spawner spawner in allSpawners)
            {
                //During the removal we have to put the spawn settings in remove mode
                SpawnMode originalMode = spawner.m_settings.m_spawnMode;
                spawner.m_settings.m_spawnMode = SpawnMode.Remove;

                ProgressBar.Show(ProgressBarPriority.BiomeRemoval, "Removing Foreign GameObjects", "Removing Game Objects...", protoIndex , allSpawners.Length);
                foreach (SpawnRule sr in spawner.m_settings.m_spawnerRules)
                {
                    

                    if (sr.m_resourceType == GaiaConstants.SpawnerResourceType.GameObject)
                    {
                        ResourceProtoGameObject protoGO = spawner.m_settings.m_resources.m_gameObjectPrototypes[sr.m_resourceIdx];
                        foreach (ResourceProtoGameObjectInstance instance in protoGO.m_instances)
                        {
                            if (!knownProtoInstances.Contains(instance))
                            {
                                operation.SetTerrainGameObjects(ApplyBrush(operation, MultiTerrainOperationType.GameObject), protoGO, sr, spawner.m_settings, 0, ref sr.m_spawnedInstances, m_settings.m_removeForeignGameObjectStrength,false);
                                //no need to look at other instances if this one triggered the removal already
                                break;
                            }
                        }
                    }
                }

                spawner.m_settings.m_spawnMode = originalMode;
                protoIndex++;
            }
            operation.CloseOperation();

#if UNITY_EDITOR
            //need to dirty the scene when we remove game objects
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
            ProgressBar.Clear(ProgressBarPriority.BiomeRemoval);
        }

        public void RemoveForeignTerrainDetails(List<SpawnerSettings> biomeSpawnerSettings, List<string> validTerrainNames = null)
        {
            ProgressBar.Show(ProgressBarPriority.BiomeRemoval, "Removing Foreign Terrain Details", "Removing...");
            List<ResourceProtoDetail> knownTerrainDetails = new List<ResourceProtoDetail>();
            Terrain currentTerrain = GetCurrentTerrain();
            foreach (SpawnerSettings spawnerSettings in biomeSpawnerSettings)
            {
                foreach (SpawnRule sr in spawnerSettings.m_spawnerRules)
                {
                    if (sr.m_resourceType == GaiaConstants.SpawnerResourceType.TerrainDetail)
                    {
                        knownTerrainDetails.Add(spawnerSettings.m_resources.m_detailPrototypes[sr.m_resourceIdx]);
                    }
                }
            }

            GaiaMultiTerrainOperation operation = new GaiaMultiTerrainOperation(currentTerrain, transform, m_settings.m_range * 2f, false, validTerrainNames);
            operation.GetHeightmap();
            operation.GetNormalmap();
            operation.CollectTerrainDetails();
            operation.CollectTerrainTrees();
            operation.CollectTerrainGameObjects();
            operation.CollectTerrainBakedMasks();

            ProgressBar.Show(ProgressBarPriority.BiomeRemoval, "Removing Foreign Terrain Details", "Removing Terrain Details...");
            operation.RemoveForeignTerrainDetails(ApplyBrush(operation, MultiTerrainOperationType.TerrainDetail), knownTerrainDetails, m_settings.m_removeForeignTerrainDetailsStrength);
            
            operation.CloseOperation();
            ProgressBar.Clear(ProgressBarPriority.BiomeRemoval);
        }


    }


}
