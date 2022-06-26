using System;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace Gaia
{
    /// <summary>
    /// This object stores Gaia defaults. There should be only one of these in your project. Gaia will automatically pick these up and use them.
    /// </summary>
    public class GaiaDefaults : ScriptableObject
    {
        [Tooltip("Unique identifier for these defaults."), HideInInspector]
        public string m_defaultsID = Guid.NewGuid().ToString();

        [Tooltip("The absolute height of the sea or water table in meters. All spawn criteria heights are calculated relative to this. Used to populate initial sea level in new resources files.")]
        public float m_seaLevel = 50f;

        [Tooltip("The beach height in meters. Beaches are spawned at sea level and are extended for this height above sea level. This is used when creating default spawn rules in order to create a beach in the zone between water and land. Only used to populate initial beach height in new resources files.")]
        public float m_beachHeight = 5f;

        [Header("Base Terrain:")]
        [Space(5)]
        [Tooltip("The accuracy of the mapping between the terrain maps (heightmap, textures, etc) and the generated terrain; higher values indicate lower accuracy but lower rendering overhead.")]
        [Range(1, 200)]
        public int m_pixelError = 5;
        [Tooltip("The maximum distance at which terrain textures will be displayed at full resolution. Beyond this distance, a lower resolution composite image will be used for efficiency.")]
        [Range(0, 2000)]
        public int m_baseMapDist = 1024;
#if UNITY_2019_1_OR_NEWER
        [Tooltip("The shadow casting mode for the terrain.")]
        public UnityEngine.Rendering.ShadowCastingMode m_shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
#else
        [Tooltip("Whether or not the terrain casts shadows.")]
        public bool m_castShadows = true;
#endif
        [Tooltip("The material used to render the terrain. This should use a suitable shader, for example Nature/Terrain/Diffuse. The default terrain shader is used if no material is supplied.")]
        public Material m_material;
        [Tooltip("The Physic Material used for the terrain surface to specify its friction and bounce.")]
        public PhysicMaterial m_physicsMaterial;

        [Header("Tree & Detail Objects:")]
        [Space(5)]
        [Tooltip("Draw trees, grass & details.")]
        public bool m_draw = true;
        [Tooltip("The distance (from camera) beyond which details will be culled.")]
        [Range(0, 10000)]
        public int m_detailDistance = 120;
        [Tooltip("The number of detail/grass objects in a given unit of area. The value can be set lower to reduce rendering overhead.")]
        [Range(0f, 1f)]
        public float m_detailDensity = 1.0f;
        [Tooltip("The distance (from camera) beyond which trees will be culled.")]
        [Range(0, 2000)]
        public int m_treeDistance = 500;
        [Tooltip("The distance (from camera) at which 3D tree objects will be replaced by billboard images.")]
        [Range(5, 2000)]
        public int m_billboardStart = 50;
        [Tooltip("Distance over which trees will transition between 3D objects and billboards.There is often a rotation effect as this kicks in.")]
        [Range(0, 200)]
        public int m_fadeLength = 20;
        [Tooltip("The maximum number of visible trees that will be represented as solid 3D meshes. Beyond this limit, trees will be replaced with billboards.")]
        [Range(0, 10000)]
        public int m_maxMeshTrees = 50;

        [Header("Wind Settings:")]
        [Space(5)]
        [Tooltip("The speed of the wind as it blows grass.")]
        [Range(0f, 1f)]
        public float m_speed = 0.35f;
        [Tooltip("The size of the “ripples” on grassy areas as the wind blows over them.")]
        [Range(0f, 1f)]
        public float m_size = 0.12f;
        [Tooltip("The degree to which grass objects are bent over by the wind.")]
        [Range(0f, 1f)]
        public float m_bending = 0.1f;
        [Tooltip("Overall color tint applied to grass objects.")]
        public Color m_grassTint = new Color(180f / 255f, 180f / 255f, 180f / 255f, 1f);

        [Header("Resolution Settings:")]
        [Space(5)]
        [Tooltip("The size of terrain tile in X & Z axis (in world units).")]
        public int m_terrainSize = 2048;
        [Tooltip("The height of the terrain in world unit meters")]
        public int m_terrainHeight = 700;
        [Tooltip("This value increases or decreases the spawn density for all spawners in the scene (higher = more spawns)")]
        public float m_spawnDensity = 1;
        [Tooltip("Pixel resolution of the terrain’s heightmap (should be a power of two plus one e.g. 513 = 512 + 1). Higher resolutions allow for more detailed terrain features, at the cost of poorer performance.")]
        public int m_heightmapResolution = 1025;
        [Tooltip("Resolution of the map that determines the separate patches of details/grass. Higher resolution gives smaller and more detailed patches.")]
        public int m_detailResolution = 1024;
        [Tooltip("Length/width of the square of patches rendered with a single draw call.")]
        public int m_detailResolutionPerPatch = 8;
        [Tooltip("Resolution of the “splatmap” that controls the blending of the different terrain textures. Higher resolutions consumer more memory, but provide more accurate texturing.")]
        public int m_controlTextureResolution = 1024;
        [Tooltip("Resolution of the composite texture used on the terrain when viewed from a distance greater than the Basemap Distance (see above).")]
        public int m_baseMapSize = 1024;


        /// <summary>
        /// Create the terrain defined by these settings
        /// </summary>
        //public void CreateTerrain()
        //{
        //    Terrain[,] world;

        //    GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();

        //    //Update the session
        //    GaiaSessionManager sessionMgr = GaiaSessionManager.GetSessionManager();
        //    if (sessionMgr != null && sessionMgr.IsLocked() != true)
        //    {
        //        //Update terrain settings in session
        //        sessionMgr.m_session.m_terrainWidth = gaiaSettings.m_tilesX * m_terrainSize;
        //        sessionMgr.m_session.m_terrainDepth = gaiaSettings.m_tilesZ * m_terrainSize;
        //        sessionMgr.m_session.m_terrainHeight = m_terrainHeight;
        //        sessionMgr.AddDefaults(this);

        //        sessionMgr.SetSeaLevel(m_seaLevel);

        //        //Then add the operation
        //        GaiaOperation op = new GaiaOperation();
        //        op.m_description = "Creating terrain";
        //        op.m_isActive = true;
        //        op.m_operationDateTime = DateTime.Now.ToString();
        //        op.m_operationType = GaiaOperation.OperationType.CreateWorld;

        //        sessionMgr.AddOperation(op);
        //    }

        //    //Create the terrains array
        //    world = new Terrain[gaiaSettings.m_tilesX, gaiaSettings.m_tilesZ];

        //    //And iterate through and create each terrain
        //    for (int x = 0; x < gaiaSettings.m_tilesX; x++)
        //    {
        //        for (int z = 0; z < gaiaSettings.m_tilesZ; z++)
        //        {
        //            GameObject terrainGO = CreateTile(x, z, ref world, null, null, null);
        //            //Parent it to the scene -either below the terrain object, or if selected, as one scene each
        //            GameObject gaiaObj = GaiaUtils.GetTerrainObject();
        //            terrainGO.transform.SetParent(gaiaObj.transform);
        //        }
        //    }

        //    //Now join them together and remove their seams
        //    RemoveWorldSeams(ref world);
        //}

        /// <summary>
        /// Update the defaults if possible from the currently selected terrain
        /// </summary>
        public void UpdateFromTerrain()
        {
            Terrain terrain = GetActiveTerrain();
            if (terrain == null)
            {
                Debug.Log("Could not update from active terrain - no current active terrain");
                return;
            }

            m_baseMapDist = (int)terrain.basemapDistance;

#if UNITY_2019_1_OR_NEWER
            m_shadowCastingMode = terrain.shadowCastingMode;
#else
            m_castShadows = terrain.castShadows;
#endif
            m_detailDensity = terrain.detailObjectDensity;
            m_detailDistance = (int)terrain.detailObjectDistance;
            m_pixelError = (int)terrain.heightmapPixelError;
            m_billboardStart = (int)terrain.treeBillboardDistance;
            m_fadeLength = (int)terrain.treeCrossFadeLength;
            m_treeDistance = (int)terrain.treeDistance;
            m_maxMeshTrees = terrain.treeMaximumFullLODCount;
#if (!UNITY_2019_2_OR_NEWER)
            if (terrain.materialType == Terrain.MaterialType.Custom)
            {
#endif
            m_material = terrain.materialTemplate;
#if (!UNITY_2019_2_OR_NEWER)
            }
#endif

            TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
            if (collider != null)
            {
                m_physicsMaterial = collider.material;
            }

            TerrainData terrainData = terrain.terrainData;
            m_controlTextureResolution = terrainData.alphamapResolution;
            m_baseMapSize = terrainData.baseMapResolution;
            m_detailResolution = terrainData.detailResolution;
            //m_detailResolutionPerPatch = terrainData.
            m_heightmapResolution = terrainData.heightmapResolution;
            m_bending = terrainData.wavingGrassAmount;
            m_size = terrainData.wavingGrassSpeed;
            m_speed = terrainData.wavingGrassStrength;
            m_grassTint = terrainData.wavingGrassTint;
            m_terrainSize = (int)terrainData.size.x;
            m_terrainHeight = (int)terrainData.size.y;
        }


        /// <summary>
        /// Takes a spawner preset list and assembles arrays with the new prototypes in them. If a reference Terrain is passed in, the arrays will contain the existing prototypes on the terrain + only the additional stuff from the preset list, no duplicates.
        /// </summary>
        /// <param name="spawnerPresetList"></param>
        /// <param name="terrainLayers"></param>
        /// <param name="terrainDetails"></param>
        /// <param name="terrainTrees"></param>
        /// <param name="referenceTerrain"></param>
        public static void GetPrototypes(List<BiomeSpawnerListEntry> spawnerPresetList, ref TerrainLayer[] terrainLayers, ref DetailPrototype[] terrainDetails, ref TreePrototype[] terrainTrees, Terrain referenceTerrain = null)
        {
            //Early out when no spawner preset list
            if (spawnerPresetList == null || spawnerPresetList.Count == 0)
            {
                return;
            }

            //collect the splat prototypes in a GaiaSplatPrototype List first, then build a terrain layer array from that later
            List<GaiaSplatPrototype> presetSplatPrototypes = new List<GaiaSplatPrototype>();
            List<DetailPrototype> presetTerrainDetails = new List<DetailPrototype>();
            List<TreePrototype> presetTerrainTrees = new List<TreePrototype>();


            //if there is a reference terrain, start by pre-filling those lists with the prototypes from the terrain
            if (referenceTerrain != null)
            {
                foreach (TerrainLayer layer in referenceTerrain.terrainData.terrainLayers)
                {
                    presetSplatPrototypes.Add(new GaiaSplatPrototype(layer));
                }
                foreach (DetailPrototype detailPrototype in referenceTerrain.terrainData.detailPrototypes)
                {
                    presetTerrainDetails.Add(detailPrototype);
                }
                foreach (TreePrototype treePrototype in referenceTerrain.terrainData.treePrototypes)
                {
                    presetTerrainTrees.Add(treePrototype);
                }

            }

            foreach (BiomeSpawnerListEntry preset in spawnerPresetList.Where(x => x.m_autoAssignPrototypes == true))
            {
                if (preset == null)
                {
                    //Skip entries missing information
                    continue;
                }

                if (preset.m_spawnerSettings == null)
                {
                    continue;
                }

                if (preset.m_spawnerSettings.m_resources == null)
                {
                    continue;
                }

                GaiaResource resources = preset.m_spawnerSettings.m_resources;
                foreach (SpawnRule sr in preset.m_spawnerSettings.m_spawnerRules)
                {
                    //skip if resource is not maintained properly (=null)
                    if (sr.ResourceIsNull(preset.m_spawnerSettings))
                    {
                        Debug.Log("Spawn Rule " + sr.m_name + " in " + preset.m_spawnerSettings.name + " has missing resources maintained. This rule might not work properly when spawning later.");
                        continue;
                    }

                    switch (sr.m_resourceType)
                    {
                        case GaiaConstants.SpawnerResourceType.TerrainTexture:
                            ResourceProtoTexture protoTexture = resources.m_texturePrototypes[sr.m_resourceIdx];
                            if (protoTexture != null)
                            {
                                GaiaSplatPrototype newSplat = new GaiaSplatPrototype();
                                newSplat.normalMap = protoTexture.m_normal;
                                newSplat.normalScale = protoTexture.m_normalScale;
                                newSplat.smoothness = protoTexture.m_smoothness;
                                newSplat.metallic = protoTexture.m_metallic;
                                newSplat.diffuseRemapMin = protoTexture.m_diffuseRemapMin;
                                newSplat.diffuseRemapMax = protoTexture.m_diffuseRemapMax;
                                newSplat.maskMapRemapMin = protoTexture.m_maskMapRemapMin;
                                newSplat.maskMapRemapMax = protoTexture.m_maskMapRemapMax;
                                newSplat.specularColor = protoTexture.m_specularColor;
                                newSplat.tileOffset = new Vector2(protoTexture.m_offsetX, protoTexture.m_offsetY);
                                newSplat.tileSize = new Vector2(protoTexture.m_sizeX, protoTexture.m_sizeY);
                                newSplat.texture = protoTexture.m_texture;
                                newSplat.maskMap = protoTexture.m_maskmap;
                                //Only add as a new prototype if not a fully equal prototype already exists in the list
                                if (!presetSplatPrototypes.Exists(x => x.texture == newSplat.texture &&
                                                            x.normalMap == newSplat.normalMap &&
                                                            x.tileOffset == newSplat.tileOffset &&
                                                            x.tileSize == newSplat.tileSize
                                                            ))
                                {
                                    presetSplatPrototypes.Add(newSplat);
                                }
                            }
                            break;
                        case GaiaConstants.SpawnerResourceType.TerrainDetail:
                            ResourceProtoDetail protoDetail = resources.m_detailPrototypes[sr.m_resourceIdx];
                            if (protoDetail != null)
                            {
                                DetailPrototype newTerrainDetail = new DetailPrototype();
                                newTerrainDetail.renderMode = protoDetail.m_renderMode;
                                newTerrainDetail.prototypeTexture = protoDetail.m_detailTexture;
                                newTerrainDetail.prototype = protoDetail.m_detailProtoype;
                                newTerrainDetail.dryColor = protoDetail.m_dryColour;
                                newTerrainDetail.healthyColor = protoDetail.m_healthyColour;
                                newTerrainDetail.maxHeight = protoDetail.m_maxHeight;
                                newTerrainDetail.maxWidth = protoDetail.m_maxWidth;
                                newTerrainDetail.minHeight = protoDetail.m_minHeight;
                                newTerrainDetail.minWidth = protoDetail.m_minWidth;
                                newTerrainDetail.noiseSpread = protoDetail.m_noiseSpread;

                                if ((protoDetail.m_renderMode == DetailRenderMode.Grass && protoDetail.m_detailProtoype == null) || protoDetail.m_renderMode == DetailRenderMode.GrassBillboard)
                                {
                                    if (!presetTerrainDetails.Exists(x => x.prototypeTexture == newTerrainDetail.prototypeTexture &&
                                                                    x.prototype == newTerrainDetail.prototype &&
                                                                    x.dryColor == newTerrainDetail.dryColor &&
                                                                    x.healthyColor == newTerrainDetail.healthyColor &&
                                                                    x.maxHeight == newTerrainDetail.maxHeight &&
                                                                    x.maxWidth == newTerrainDetail.maxWidth &&
                                                                    x.minHeight == newTerrainDetail.minHeight &&
                                                                    x.minWidth == newTerrainDetail.minWidth &&
                                                                    x.noiseSpread == newTerrainDetail.noiseSpread
                                                               ))
                                    {
                                        presetTerrainDetails.Add(newTerrainDetail);
                                    }
                                }
                                else
                                {
                                    if (!presetTerrainDetails.Exists(x => x.prototype == newTerrainDetail.prototype &&
                                                                    x.prototype == newTerrainDetail.prototype &&
                                                                    x.dryColor == newTerrainDetail.dryColor &&
                                                                    x.healthyColor == newTerrainDetail.healthyColor &&
                                                                    x.maxHeight == newTerrainDetail.maxHeight &&
                                                                    x.maxWidth == newTerrainDetail.maxWidth &&
                                                                    x.minHeight == newTerrainDetail.minHeight &&
                                                                    x.minWidth == newTerrainDetail.minWidth &&
                                                                    x.noiseSpread == newTerrainDetail.noiseSpread
                                                               ))
                                    {
                                        presetTerrainDetails.Add(newTerrainDetail);
                                    }

                                }

                            }
                            break;
                        case GaiaConstants.SpawnerResourceType.TerrainTree:
                            ResourceProtoTree protoTree = resources.m_treePrototypes[sr.m_resourceIdx];
                            if (protoTree != null)
                            {
                                TreePrototype newTree = new TreePrototype();
                                newTree.bendFactor = protoTree.m_bendFactor;
                                newTree.prefab = protoTree.m_desktopPrefab;
                                if (!presetTerrainTrees.Exists(x => x.bendFactor == newTree.bendFactor &&
                                                                 x.prefab == newTree.prefab))
                                {
                                    presetTerrainTrees.Add(newTree);
                                }
                            }
                            break;
                    }
                }
            }

            //now look at the existing terrain layers on the terrain again, and add only the additional new Splat Prototypes as new layers - all the other ones must exist on the terrains already
            List<GaiaSplatPrototype> additionalSplatPrototypes = new List<GaiaSplatPrototype>();
            List<DetailPrototype> additionalTerrainDetails = new List<DetailPrototype>();
            List<TreePrototype> additionalTerrainTrees = new List<TreePrototype>();
            //Do we have a refernece terrain? The only the splat prototypes with a higher index then what is currently present on the terrain already are relevant
            if (referenceTerrain != null)
            {
                for (int i = referenceTerrain.terrainData.terrainLayers.Length; i <= presetSplatPrototypes.Count - 1; i++)
                {
                    additionalSplatPrototypes.Add(presetSplatPrototypes[i]);
                }
                for (int i = referenceTerrain.terrainData.detailPrototypes.Length; i <= presetTerrainDetails.Count - 1; i++)
                {
                    additionalTerrainDetails.Add(presetTerrainDetails[i]);
                }
                for (int i = referenceTerrain.terrainData.treePrototypes.Length; i <= presetTerrainTrees.Count - 1; i++)
                {
                    additionalTerrainTrees.Add(presetTerrainTrees[i]);
                }
            }
            else
            {
                //no reference terrain - we take preset prototypes collected before
                additionalSplatPrototypes = presetSplatPrototypes;
                additionalTerrainDetails = presetTerrainDetails;
                additionalTerrainTrees = presetTerrainTrees;
            }

            //convert Gaia Splat Prototypes into actual terrain layer files
            string layername = string.Format("Gaia_-{0:yyyyMMdd-HHmmss}", DateTime.Now);
            TerrainLayer[] additionalTerrainLayers = GaiaSplatPrototype.CreateTerrainLayers(layername, additionalSplatPrototypes.ToArray());

            if (referenceTerrain != null)
            {
                //got a reference terrain? return the existing prototypes so they are not altered and attach the truly new ones
                terrainLayers = referenceTerrain.terrainData.terrainLayers;
                terrainLayers = terrainLayers.Concat(additionalTerrainLayers).ToArray();

                terrainDetails = referenceTerrain.terrainData.detailPrototypes;
                terrainDetails = terrainDetails.Concat(additionalTerrainDetails.ToArray()).ToArray();

                terrainTrees = referenceTerrain.terrainData.treePrototypes;
                terrainTrees = terrainTrees.Concat(additionalTerrainTrees.ToArray()).ToArray();
            }
            else
            {
                //no reference terrain, just return all the new stuff directly
                terrainLayers = additionalTerrainLayers;
                terrainDetails = additionalTerrainDetails.ToArray();
                terrainTrees = additionalTerrainTrees.ToArray();
            }

        }

        public GaiaOperation GetTerrainCreationOperation(GaiaResource resources)
        {
            //            //Then add the operation
            //            GaiaOperation op = new GaiaOperation();
            //            op.m_description = "Creating terrain";
            //            op.m_generatedByID = m_defaultsID;
            //            op.m_generatedByName = this.name;
            //            op.m_generatedByType = this.GetType().ToString();
            //            op.m_isActive = true;
            //            op.m_operationDateTime = DateTime.Now.ToString();
            //            op.m_operationType = GaiaOperation.OperationType.CreateTerrain;
            //#if UNITY_EDITOR
            //            op.m_operationDataJson = new string[2];
            //            string ap = AssetDatabase.GetAssetPath(this);
            //            if (string.IsNullOrEmpty(ap))
            //            {
            //                op.m_operationDataJson[0] = "GaiaDefaults.asset";
            //            }
            //            else
            //            {
            //                op.m_operationDataJson[0] = AssetDatabase.GetAssetPath(this);
            //            }
            //            ap = AssetDatabase.GetAssetPath(resources);
            //            if (string.IsNullOrEmpty(ap))
            //            {
            //                op.m_operationDataJson[1] = "GaiaResources.asset";
            //            }
            //            else
            //            {
            //                op.m_operationDataJson[1] = AssetDatabase.GetAssetPath(resources);
            //            }
            //#endif

            //return op;
            return null;
        }

        /// <summary>
        /// Get the currently active terrain - or any terrain
        /// </summary>
        /// <returns>A terrain if there is one</returns>
        public static Terrain GetActiveTerrain()
        {
            //Grab active terrain if we can
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null && terrain.isActiveAndEnabled)
            {
                return terrain;
            }

            //Then check rest of terrains
            for (int idx = 0; idx < Terrain.activeTerrains.Length; idx++)
            {
                terrain = Terrain.activeTerrains[idx];
                if (terrain != null && terrain.isActiveAndEnabled)
                {
                    return terrain;
                }
            }

            if (terrain == null)
            {
                Terrain findTerrainComponent = FindObjectOfType<Terrain>();
                if (findTerrainComponent != null && findTerrainComponent.isActiveAndEnabled)
                {
                    terrain = findTerrainComponent;
                    return terrain;
                }
            }

            return null;
        }



        /// <summary>
        /// Set these assets into the terrain provided
        /// </summary>
        /// <param name="terrain"></param>
        public static void ApplyPrototypesToTerrain(Terrain terrain, TerrainLayer[] terrainLayers, DetailPrototype[] terrainDetails, TreePrototype[] terrainTrees)
        {
            // Do a terrain check
            if (terrain == null)
            {
                Debug.LogWarning("Can not apply assets to terrain no terrain has been supplied.");
                return;
            }
            if (terrainLayers != null && terrainLayers.Length > 0)
                terrain.terrainData.terrainLayers = terrainLayers;
            if (terrainDetails != null && terrainLayers.Length > 0)
                terrain.terrainData.detailPrototypes = terrainDetails;
            if (terrainTrees != null && terrainLayers.Length > 0)
                terrain.terrainData.treePrototypes = terrainTrees;

            terrain.Flush();
        }

        /// <summary>
        /// Update the terrain tiles to relate properly to each other
        /// </summary>
        /// <param name="tiles"></param>
        private void RemoveWorldSeams(ref Terrain[,] world)
        {
            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();

            //Set the neighbours of terrain to remove seams.
            for (int x = 0; x < gaiaSettings.m_tilesX; x++)
            {
                for (int z = 0; z < gaiaSettings.m_tilesZ; z++)
                {
                    Terrain right = null;
                    Terrain left = null;
                    Terrain bottom = null;
                    Terrain top = null;

                    if (x > 0) left = world[(x - 1), z];
                    if (x < gaiaSettings.m_tilesX - 1) right = world[(x + 1), z];

                    if (z > 0) bottom = world[x, (z - 1)];
                    if (z < gaiaSettings.m_tilesZ - 1) top = world[x, (z + 1)];

                    world[x, z].SetNeighbors(left, top, right, bottom);
                }
            }
        }




        ///// <summary>
        ///// Serialise this as json
        ///// </summary>
        ///// <returns></returns>
        //public string SerialiseJson()
        //{
        //    fsData data;
        //    fsSerializer serializer = new fsSerializer();
        //    serializer.TrySerialize(this, out data);
        //    return fsJsonPrinter.CompressedJson(data);
        //}

        ///// <summary>
        ///// Deserialise the suplied json into this object
        ///// </summary>
        ///// <param name="json">Source json</param>
        //public void DeSerialiseJson(string json)
        //{
        //    fsData data = fsJsonParser.Parse(json);
        //    fsSerializer serializer = new fsSerializer();
        //    var defaults = this;
        //    serializer.TryDeserialize<GaiaDefaults>(data, ref defaults);
        //}
    }
}