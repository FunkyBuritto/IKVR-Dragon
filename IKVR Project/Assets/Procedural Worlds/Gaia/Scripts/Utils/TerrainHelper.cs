using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using static Gaia.GaiaConstants;
using System.Security.AccessControl;

namespace Gaia
{
    /// <summary>
    /// Terrain utility functions
    /// </summary>
    public class TerrainHelper : MonoBehaviour
    {
        [Range(1, 5), Tooltip("Number of smoothing interations to run. Can be run multiple times.")]
        public int m_smoothIterations = 1;



        //Knock ourselves out if we happen to be left there in play mode
        void Awake()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Flatten all the active terrains
        /// </summary>
        public static void Flatten()
        {
            FlattenTerrain(Terrain.activeTerrains);
        }

        /// <summary>
        /// Flatten the terrain passed in
        /// </summary>
        /// <param name="terrain">Terrain to be flattened</param>
        public static void FlattenTerrain(Terrain terrain)
        {
            float[,] heights = new float[terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution];
            terrain.terrainData.SetHeights(0, 0, heights);
        }

        /// <summary>
        /// Flatten all the terrains passed in
        /// </summary>
        /// <param name="terrains">Terrains to be flattened</param>
        public static void FlattenTerrain(Terrain[] terrains)
        {
            foreach (Terrain terrain in terrains)
            {
                float[,] heights = new float[terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution];
                terrain.terrainData.SetHeights(0, 0, heights);
            }
        }

        /// <summary>
        /// Stitch the terrains together with unity set neighbors calls
        /// </summary>
        public static void Stitch()
        {
            StitchTerrains(Terrain.activeTerrains);
        }

        /// <summary>
        /// Stitch the terrains together - wont align them although should update this to support that as well.
        /// </summary>
        /// <param name="terrains">Array of terrains to organise as neighbors</param>
        public static void StitchTerrains(Terrain[] terrains)
        {
            Terrain right = null;
            Terrain left = null;
            Terrain bottom = null;
            Terrain top = null;

            foreach (Terrain terrain in terrains)
            {
                right = null;
                left = null;
                bottom = null;
                top = null;

                foreach (Terrain neighbor in terrains)
                {
                    //Check to see if neighbor is above or below
                    if (neighbor.transform.position.x == terrain.transform.position.x)
                    {
                        if ((neighbor.transform.position.z + neighbor.terrainData.size.z) == terrain.transform.position.z)
                        {
                            top = neighbor;
                        }
                        else if ((terrain.transform.position.z + terrain.terrainData.size.z) == neighbor.transform.position.z)
                        {
                            bottom = neighbor;
                        }
                    }
                    else if (neighbor.transform.position.z == terrain.transform.position.z)
                    {
                        if ((neighbor.transform.position.x + neighbor.terrainData.size.z) == terrain.transform.position.z)
                        {
                            left = neighbor;
                        }
                        else if ((terrain.transform.position.x + terrain.terrainData.size.x) == neighbor.transform.position.x)
                        {
                            right = neighbor;
                        }
                    }
                }

                terrain.SetNeighbors(left, top, right, bottom);
            }
        }

        /// <summary>
        /// Smooth the active terrain - needs to be extended to all and to handle edges
        /// </summary>
        /// <param name="iterations">Number of smoothing iterations</param>
        public void Smooth()
        {
            Smooth(m_smoothIterations);
        }

        /// <summary>
        /// Smooth the active terrain - needs to be extended to all and to handle edges
        /// </summary>
        /// <param name="iterations">Number of smoothing iterations</param>
        public static void Smooth(int iterations)
        {
            UnityHeightMap hm = new UnityHeightMap(Terrain.activeTerrain);
            hm.Smooth(iterations);
            hm.SaveToTerrain(Terrain.activeTerrain);
        }

        /// <summary>
        /// Get the vector of the centre of the active terrain, and flush to ground level if asked to
        /// </summary>
        /// <param name="flushToGround">If true set it flush to the ground</param>
        /// <returns>Vector3.zero if no terrain, otherwise the centre of it</returns>
        public static Vector3 GetActiveTerrainCenter(bool flushToGround = true)
        {
            Bounds b = new Bounds();
            Terrain t = GetActiveTerrain();
            if (GetTerrainBounds(t, ref b))
            {
                if (flushToGround == true)
                {
                    return new Vector3(b.center.x, t.SampleHeight(b.center), b.center.z);
                }
                else
                {
                    return b.center;
                }
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Gets the world map terrain from the scene
        /// </summary>
        /// <returns>The world map terrain</returns>
        public static Terrain GetWorldMapTerrain()
        {
            foreach (Terrain t in Terrain.activeTerrains)
            {
                if (TerrainHelper.IsWorldMapTerrain(t))
                {
                    return t;
                }
            }

            //still no world map terrain? might be a deactivated GameObject, check those as well
            GameObject worldMapGO = GaiaUtils.FindObjectDeactivated(GaiaConstants.worldMapTerrainPrefix + "_", false);
            if (worldMapGO != null)
            {
                Terrain t = worldMapGO.GetComponent<Terrain>();
                if (t != null)
                {
                    return t;
                }
            }
            return null;
        }

        /// <summary>
        /// Get any active terrain - pref active terrain
        /// </summary>
        /// <returns>Any active terrain or null</returns>
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
            return null;
        }

        /// <summary>
        /// Get the layer mask of the active terrain, or default if there isnt one
        /// </summary>
        /// <returns>Layermask of activer terrain or default if there isnt one</returns>
        public static LayerMask GetActiveTerrainLayer()
        {
            LayerMask layer = new LayerMask();
            Terrain terrain = GetActiveTerrain();
            if (terrain != null)
            {
                layer.value = 1 << terrain.gameObject.layer;
                return layer;
            }
            layer.value = 1 << LayerMask.NameToLayer("Default");
            return layer;
        }

        /// <summary>
        /// Get the layer mask of the active terrain, or default if there isnt one
        /// </summary>
        /// <returns>Layermask of activer terrain or default if there isnt one</returns>
        public static LayerMask GetActiveTerrainLayerAsInt()
        {
            LayerMask layerValue = GetActiveTerrainLayer().value;
            for (int layerIdx = 0; layerIdx < 32; layerIdx++)
            {
                if (layerValue == (1 << layerIdx))
                {
                    return layerIdx;
                }
            }
            return LayerMask.NameToLayer("Default");
        }

        /// <summary>
        /// Get the number of active terrain tiles in this scene
        /// </summary>
        /// <returns>Number of terrains in the scene</returns>
        public static int GetActiveTerrainCount()
        {
            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                //with terrain loading, we can simply count the loaded scenes, those should be the active terrains
                return TerrainLoaderManager.TerrainScenes.Where(x => x.m_regularLoadState == LoadState.Loaded).Count() + TerrainLoaderManager.TerrainScenes.Where(x => x.m_impostorLoadState == LoadState.Loaded).Count();

            }
            else
            {
                //For non-terrain loading we need to take a look at what we can find in the scene
                //Regular terrains
                Terrain terrain;
                int terrainCount = 0;
                for (int idx = 0; idx < Terrain.activeTerrains.Length; idx++)
                {
                    terrain = Terrain.activeTerrains[idx];
                    if (terrain != null && terrain.isActiveAndEnabled)
                    {
                        terrainCount++;
                    }
                }

                //Mesh Terrains from a terrain export
                GameObject exportContainer = GaiaUtils.GetTerrainExportObject(false);
                if (exportContainer != null)
                {
                    //Iterate through the objects in here, if it is active and the name checks out we can assume it is a mesh terrain.
                    foreach (Transform t in exportContainer.transform)
                    {
                        if (t.gameObject.GetComponent<MeshRenderer>() != null)
                        {
                            if (t.gameObject.activeInHierarchy && (t.name.StartsWith(GaiaConstants.MeshTerrainName) || t.name.StartsWith(GaiaConstants.MeshTerrainLODGroupPrefix)))
                            {
                                terrainCount++;
                            }
                        }
                    }
                }
                return terrainCount;
            }
        }
        #if GAIA_PRO_PRESENT
        /// <summary>
        /// Get the terrain scene that matches this location, otherwise return null
        /// </summary>
        /// <param name="locationWU">Location to check in world units</param>
        /// <returns>Terrain here or null</returns>
        public static TerrainScene GetDynamicLoadedTerrain(Vector3 locationWU, GaiaSessionManager gsm = null)
        {
            if (gsm == null)
            {
                gsm = GaiaSessionManager.GetSessionManager(false);
            }

            foreach (TerrainScene terrainScene in TerrainLoaderManager.TerrainScenes)
            {
                if (terrainScene.m_bounds.min.x <= locationWU.x && terrainScene.m_bounds.min.z <= locationWU.z && terrainScene.m_bounds.max.x >= locationWU.x && terrainScene.m_bounds.max.z >= locationWU.z)
                {
                    return terrainScene;
                }
            }

            return null;
        }
#endif
        /// <summary>
        /// Returns the Quaternion rotation from the active terrain normal
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="playerObj"></param>
        /// <returns></returns>
        public static Vector3 GetRotationFromTerrainNormal(Terrain terrain, GameObject playerObj)
        {
            if (terrain != null && playerObj != null)
            {
                float scalarX = (playerObj.transform.position.x - terrain.transform.position.x) / (float)terrain.terrainData.size.x;
                float scalarZ = (playerObj.transform.position.z - terrain.transform.position.z) / (float)terrain.terrainData.size.z;
                Vector3 interpolatedNormal = terrain.terrainData.GetInterpolatedNormal(scalarX, scalarZ);
                Quaternion quaternion = Quaternion.FromToRotation(Vector3.up, interpolatedNormal) * playerObj.transform.rotation;
                return quaternion.eulerAngles;
            }
            else
            {
                return Vector3.zero;
            }
        }
        /// <summary>
        /// Get the terrain that matches this location, otherwise return null
        /// </summary>
        /// <param name="locationWU">Location to check in world units</param>
        /// <returns>Terrain here or null</returns>
        public static Terrain GetTerrain(Vector3 locationWU, bool selectWorldMapTerrains = false)
        {
            Terrain terrain;
            Vector3 terrainMin = new Vector3();
            Vector3 terrainMax = new Vector3();

            //First check active terrain - most likely already selected
            terrain = Terrain.activeTerrain;
            if (terrain != null && terrain.terrainData !=null &&(selectWorldMapTerrains == TerrainHelper.IsWorldMapTerrain(terrain)))
            {
                terrainMin = terrain.GetPosition();
                terrainMax = terrainMin + terrain.terrainData.size;
                if (locationWU.x >= terrainMin.x && locationWU.x <= terrainMax.x)
                {
                    if (locationWU.z >= terrainMin.z && locationWU.z <= terrainMax.z)
                    {
                        return terrain;
                    }
                }
            }

            //Then check rest of terrains
            Terrain closestTerrain = null;
            float closestDistance = float.MaxValue;
            for (int idx = 0; idx < Terrain.activeTerrains.Length; idx++)
            {
                terrain = Terrain.activeTerrains[idx];
                if (terrain.terrainData==null || (selectWorldMapTerrains != TerrainHelper.IsWorldMapTerrain(terrain)))
                {
                    continue;
                }
                terrainMin = terrain.GetPosition();
                terrainMax = terrainMin + terrain.terrainData.size;

                if (locationWU.x >= terrainMin.x && locationWU.x <= terrainMax.x)
                {
                    if (locationWU.z >= terrainMin.z && locationWU.z <= terrainMax.z)
                    {
                        return terrain;
                    }
                }

                if (closestTerrain == null || Vector3.Distance(terrain.transform.position, locationWU) < closestDistance)
                {
                    closestTerrain = terrain;
                }
            }
            return closestTerrain;
        }

        /// <summary>
        /// Get the bounds of the space encapsulated by the supplied terrain
        /// </summary>
        /// <param name="terrain">Terrain to get bounds for</param>
        /// <param name="bounds">Bounds to update</param>
        /// <returns>True if we got some terrain bounds</returns>
        public static bool GetTerrainBounds(Terrain terrain, ref Bounds bounds)
        {
            if (terrain == null)
            {
                return false;
            }
            bounds.center = terrain.transform.position;
            bounds.size = terrain.terrainData.size;
            bounds.center += bounds.extents;
            return true;
        }


        /// <summary>
        /// Get the bounds of the terrain at this location or fail with a null
        /// </summary>
        /// <param name="locationWU">Location to check and get terrain for</param>
        /// <returns>Bounds of selected terrain or null if invalid for some reason</returns>
        public static bool GetTerrainBounds(ref BoundsDouble bounds, bool activeTerrainsOnly = false)
        {
            //Terrain terrain = GetTerrain(locationWU);
            //if (terrain == null)
            //{
            //    return false;
            //}
            //bounds.center = terrain.transform.position;
            //bounds.size = terrain.terrainData.size;
            //bounds.center += bounds.extents;

            Vector3Double accumulatedCenter = new Vector3Double();

            //Do we use dynamic loaded terrains in the scene?
            if (GaiaUtils.HasDynamicLoadedTerrains() && !activeTerrainsOnly)
            {
#if GAIA_PRO_PRESENT
                //we do have dynamic terrains -> calculate the bounds according to the terrain scene data in the session
                GaiaSessionManager gsm = GaiaSessionManager.GetSessionManager(false);

                foreach (TerrainScene t in TerrainLoaderManager.TerrainScenes)
                {
                    accumulatedCenter += t.m_bounds.center;
                }

                bounds.center = accumulatedCenter / TerrainLoaderManager.TerrainScenes.Count;

                foreach (TerrainScene t in TerrainLoaderManager.TerrainScenes)
                {
                    bounds.Encapsulate(t.m_bounds);
                }
#endif
            }
            else
            {
                //no placeholder -> calculate bounds according to the active terrains in the scene
                if (Terrain.activeTerrains.Length > 0)
                {
                    foreach (Terrain t in Terrain.activeTerrains)
                    {
                        if (!TerrainHelper.IsWorldMapTerrain(t))
                        {
                            if (t.terrainData != null)
                            {
                                accumulatedCenter += new Vector3Double(t.transform.position) + new Vector3Double(t.terrainData.bounds.extents);
                            }
                            else
                            {
                                Debug.LogWarning("Terrain " + t.name + " in the scene is missing the terrain data object!");
                            }
                        }
                    }
                    bounds.center = accumulatedCenter / Terrain.activeTerrains.Length;
             
                    foreach (Terrain t in Terrain.activeTerrains)
                    {
                        if (!TerrainHelper.IsWorldMapTerrain(t))
                        {
                            if (t.terrainData != null)
                            {
                                Bounds newBounds = new Bounds();
                                newBounds.center = t.transform.position;
                                newBounds.size = t.terrainData.size;
                                newBounds.center += t.terrainData.bounds.extents;
                                bounds.Encapsulate(newBounds);
                            }
                        }
                    }
                }
                else
                {
                    bounds = new BoundsDouble(Vector3Double.zero, Vector3Double.zero);
                    //No active terrains? There might be mesh terrains we can use then
                    GameObject meshTerrainExportObject = GaiaUtils.GetTerrainExportObject(false);
                    if (meshTerrainExportObject != null)
                    {
                        foreach (Transform t in meshTerrainExportObject.transform)
                        {
                            MeshRenderer mr = t.GetComponent<MeshRenderer>();
                            if (mr != null)
                            {
                                bounds.Encapsulate(mr.bounds);
                            }
                        }
                    }
                }
            }


            return true;
        }

        /// <summary>
        /// Get a random location on the terrain supplied
        /// </summary>
        /// <param name="terrain">Terrain to check</param>
        /// <param name="start">Start locaton</param>
        /// <param name="radius">Radius to hunt in</param>
        /// <returns></returns>
        public static Vector3 GetRandomPositionOnTerrain(Terrain terrain, Vector3 start, float radius)
        {
            Vector3 newLocation;
            Vector3 terrainMin = terrain.GetPosition();
            Vector3 terrainMax = terrainMin + terrain.terrainData.size;
            while (true)
            {
                //Get a new location
                newLocation = UnityEngine.Random.insideUnitSphere * radius;
                newLocation = start + newLocation;
                //Make sure the new location is within the terrain bounds
                if (newLocation.x >= terrainMin.x && newLocation.x <= terrainMax.x)
                {
                    if (newLocation.z >= terrainMin.z && newLocation.z <= terrainMax.z)
                    {
                        //Update it to be on the terrain surface
                        newLocation.y = terrain.SampleHeight(newLocation);
                        return newLocation;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the bounds of a terrain in world space (The bounds in the terrainData object is in local space of the terrain itself)
        /// </summary>
        /// <param name="t">The terrain to get the bounds in world space for.</param>
        /// <returns></returns>
        public static Bounds GetWorldSpaceBounds(Terrain t)
        {
            Bounds worldSpaceBounds = t.terrainData.bounds;
            worldSpaceBounds.center = new Vector3(worldSpaceBounds.center.x + t.transform.position.x, worldSpaceBounds.center.y + t.transform.position.y, worldSpaceBounds.center.z + t.transform.position.z);
            return worldSpaceBounds;
        }

        /// <summary>
        /// Clear all the trees on all the terrains
        /// </summary>
        public static void ClearSpawns(SpawnerResourceType resourceType, ClearSpawnFor clearSpawnFor, ClearSpawnFrom clearSpawnFrom, List<string> terrainNames = null, Spawner spawner = null)
        {
            if (terrainNames == null)
            {
                if (clearSpawnFor == ClearSpawnFor.AllTerrains)
                {
                    if (GaiaUtils.HasDynamicLoadedTerrains())
                    {
                        GaiaSessionManager sessionManager = GaiaSessionManager.GetSessionManager();
                        terrainNames = TerrainLoaderManager.TerrainScenes.Select(x => x.GetTerrainName()).ToList();
                    }
                    else
                    {
                        terrainNames = Terrain.activeTerrains.Select(x => x.name).ToList();
                    }
                }
                else
                {
                    terrainNames = new List<string> { spawner.GetCurrentTerrain().name };
                }
            }

            string progressBarTitle = "Clearing...";

            Action<Terrain> act = null;
            switch (resourceType)
            {
                case SpawnerResourceType.TerrainTexture:
                    progressBarTitle = "Clearing Textures";
                    //Not supported, should not be required
                    throw new NotSupportedException("Clearing of Textures is currently not supported via the terrain helper");
                case SpawnerResourceType.TerrainDetail:
                    progressBarTitle = "Clearing Terrain Details";
                    act = (t) => ClearDetailsOnSingleTerrain(t, spawner, clearSpawnFrom);
                    break;
                case SpawnerResourceType.TerrainTree:
                    progressBarTitle = "Clearing Trees";
                    act = (t) => ClearTreesOnSingleTerrain(t, spawner, clearSpawnFrom);
                    break;
                case SpawnerResourceType.GameObject:
                    progressBarTitle = "Clearing Game Objects";
                    act = (t) => ClearGameObjectsOnSingleTerrain(t, spawner, clearSpawnFrom);
                    break;
                case SpawnerResourceType.Probe:
                    progressBarTitle = "Clearing Probes";
                    act = (t) => ClearGameObjectsOnSingleTerrain(t, spawner, clearSpawnFrom);
                    break;
                case SpawnerResourceType.SpawnExtension:
                    progressBarTitle = "Clearing Spawn Extensions";
                    act = (t) => ClearSpawnExtensionsOnSingleTerrain(t, spawner, clearSpawnFrom);
                    break;
                case SpawnerResourceType.StampDistribution:
                    //Not supported, should not be required
                    throw new NotSupportedException("Clearing of Stamps is currently not supported via the terrain helper");
            }

            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                GaiaUtils.CallFunctionOnDynamicLoadedTerrains(act, true, terrainNames);
            }
            else
            {
                for (int idx = 0; idx < terrainNames.Count; idx++)
                {
                    ProgressBar.Show(ProgressBarPriority.Spawning ,progressBarTitle, progressBarTitle,  idx + 1, terrainNames.Count(), true);

                    GameObject go = GameObject.Find(terrainNames[idx]);
                    if (go != null)
                    {
                        Terrain terrain = go.GetComponent<Terrain>();
                        act(terrain);
                    }
                    ProgressBar.Clear(ProgressBarPriority.Spawning);

                }
            }
        }

        private static void ClearTreesOnSingleTerrain(Terrain terrain, Spawner spawner, ClearSpawnFrom clearSpawnFrom)
        {
            if (spawner == null || clearSpawnFrom == ClearSpawnFrom.AnySource)
            {
                //No tree prototypes passed in => we delete any tree from any source
                terrain.terrainData.treeInstances = new TreeInstance[0];
            }
            else
            {
                //We need to get the correct prototype Ids for this terrain only
                //Prototype Ids might be different from terrain to terrain, depending on when / how the prototype was added
                List<int> treePrototypeIds = new List<int>();
                foreach (SpawnRule sr in spawner.m_settings.m_spawnerRules)
                {
                    if (sr.m_resourceType == GaiaConstants.SpawnerResourceType.TerrainTree)
                    {
                        int treePrototypeIndex = spawner.m_settings.m_resources.PrototypeIdxInTerrain(sr.m_resourceType, sr.m_resourceIdx, terrain);
                        if (treePrototypeIndex != -1)
                        {
                            treePrototypeIds.Add(treePrototypeIndex);
                        }
                    }
                }
                //Reapply the tree instances on this terrain, but leave all "to be deleted" ids out via the where clause
                terrain.terrainData.SetTreeInstances(terrain.terrainData.treeInstances.Where(x => !treePrototypeIds.Contains(x.prototypeIndex)).ToArray(), true);

            }
            terrain.Flush();
        }

        /// <summary>
        /// Clear all the details (grass) on all the terrains
        /// </summary>
        private static void ClearDetailsOnSingleTerrain(Terrain terrain, Spawner spawner, ClearSpawnFrom clearSpawnFrom)
        {
            int[,] details = new int[terrain.terrainData.detailWidth, terrain.terrainData.detailHeight];

            if (spawner == null || clearSpawnFrom == ClearSpawnFrom.AnySource)
            {

                for (int dtlIdx = 0; dtlIdx < terrain.terrainData.detailPrototypes.Length; dtlIdx++)
                {
                    terrain.terrainData.SetDetailLayer(0, 0, dtlIdx, details);
                }
            }
            else
            {
                //We need to get the correct prototype Ids for this terrain only
                //Prototype Ids might be different from terrain to terrain, depending on when / how the prototype was added
                List<int> detailPrototypeIds = new List<int>();
                foreach (SpawnRule sr in spawner.m_settings.m_spawnerRules)
                {
                    if (sr.m_resourceType == GaiaConstants.SpawnerResourceType.TerrainDetail)
                    {
                        int detailPrototypeIndex = spawner.m_settings.m_resources.PrototypeIdxInTerrain(sr.m_resourceType, sr.m_resourceIdx, terrain);
                        if (detailPrototypeIndex != -1)
                        {
                            detailPrototypeIds.Add(detailPrototypeIndex);
                        }
                    }
                }

                for (int dtlIdx = 0; dtlIdx < detailPrototypeIds.Count; dtlIdx++)
                {
                    terrain.terrainData.SetDetailLayer(0, 0, detailPrototypeIds[dtlIdx], details);
                }
            }
            terrain.Flush();
        }


        private static void ClearGameObjectsOnSingleTerrain(Terrain terrain, Spawner spawner, ClearSpawnFrom clearSpawnFrom)
        {
            Spawner[] allAffectedSpawners;

            if (clearSpawnFrom == ClearSpawnFrom.OnlyThisSpawner)
            {
                allAffectedSpawners = new Spawner[1] { spawner };
            }
            else
            {
                allAffectedSpawners = Resources.FindObjectsOfTypeAll<Spawner>();
            }

            foreach (Spawner sp in allAffectedSpawners)
            {
                foreach (SpawnRule sr in sp.m_settings.m_spawnerRules.Where(x=>x.m_resourceType == SpawnerResourceType.GameObject || x.m_resourceType == SpawnerResourceType.SpawnExtension || x.m_resourceType == SpawnerResourceType.Probe))
                    Spawner.ClearGameObjectsForRule(sp, sr, false, terrain);
            }
        }

        private static void ClearSpawnExtensionsOnSingleTerrain(Terrain terrain, Spawner spawner, ClearSpawnFrom clearSpawnFrom)
        {
            Spawner[] allAffectedSpawners;

            if (clearSpawnFrom == ClearSpawnFrom.OnlyThisSpawner)
            {
                allAffectedSpawners = new Spawner[1] { spawner };
            }
            else
            {
                allAffectedSpawners = Resources.FindObjectsOfTypeAll<Spawner>();
            }

            foreach (Spawner sp in allAffectedSpawners)
            {
                foreach (SpawnRule sr in sp.m_settings.m_spawnerRules)
                {
                    sp.ClearSpawnExtensionsForRule(sr);
                    Spawner.ClearGameObjectsForRule(sp, sr, false, terrain);
                }
            }
        }

        /// <summary>
        /// Get the range from the terrain
        /// </summary>
        /// <returns></returns>
        public static float GetRangeFromTerrain()
        {
            Terrain t = Gaia.TerrainHelper.GetActiveTerrain();
            if (t != null)
            {
                return Mathf.Max(t.terrainData.size.x, t.terrainData.size.z) / 2f;
            }
            return 0f;
        }

        /// <summary>
        /// Returns true if this terrain is a world map terrain.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsWorldMapTerrain(Terrain t)
        {
            return t.name.StartsWith(GaiaConstants.worldMapTerrainPrefix);
        }

        public static bool IsWorldMapTerrain(TerrainData td)
        {
            return td.name.StartsWith(GaiaConstants.worldMapTerrainPrefix);
        }

        /// <summary>
        /// Returns terrain names of terrains that intersect with the given bounds object
        /// </summary>
        /// <param name="bounds">A bounds object to check against the terrains. Needs to be in absolute world space position, mind the current origin offset!</param>
        /// <returns></returns>
        public static string[] GetTerrainsIntersectingBounds(BoundsDouble bounds)
        {
            //Reduce the bounds size a bit to prevent selecting terrains that are perfectly aligned with the bounds border
            //-this leads to too many terrains being logged as affected by an operation otherwise.
            Bounds intersectingBounds = new BoundsDouble();
            intersectingBounds.center = bounds.center;
            intersectingBounds.size = bounds.size - new Vector3Double(0.001f, 0.001f, 0.001f);

            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                GaiaSessionManager sessionManager = GaiaSessionManager.GetSessionManager();
                if (sessionManager == null)
                {
                    Debug.LogError("Trying to get terrains that intersect with bounds, but there is no session manager in scene.");
                    return null;
                }
                return TerrainLoaderManager.TerrainScenes.Where(x => x.m_bounds.Intersects(intersectingBounds)).Select(x => x.GetTerrainName()).ToArray();
            }
            else
            {
                List<string> affectedTerrainNames = new List<string>();
                foreach (Terrain t in Terrain.activeTerrains)
                {
                    if (intersectingBounds.Intersects(TerrainHelper.GetWorldSpaceBounds(t)))
                    {
                        affectedTerrainNames.Add(t.name);
                    }
                }
                return affectedTerrainNames.ToArray();
            }
        }

        public static TerrainLayer GetLayerFromPrototype(ResourceProtoTexture proto)
        {
            foreach (Terrain t in Terrain.activeTerrains)
            {
                foreach (TerrainLayer layer in t.terrainData.terrainLayers)
                {
                    if (proto != null && layer != null)
                    {
                        if (proto.m_texture != null && layer.diffuseTexture != null)
                        {
                            if (PWCommon4.Utils.IsSameTexture(proto.m_texture, layer.diffuseTexture, false) == true)
                            {
                                return layer;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public static int GetTreePrototypeIDFromSpawnRule(SpawnRule sr, Terrain terrain)
        {
            Spawner spawner = CollisionMask.m_allTreeSpawners.FirstOrDefault(x => x.m_settings.m_spawnerRules.Contains(sr));
            if (spawner != null)
            {
                GameObject treePrefabInRule = spawner.m_settings.m_resources.m_treePrototypes[sr.m_resourceIdx].m_desktopPrefab;
                for (int i = 0; i < terrain.terrainData.treePrototypes.Length; i++)
                {
                    TreePrototype tp = terrain.terrainData.treePrototypes[i];
                    if (tp.prefab == treePrefabInRule)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public static Vector3 GetWorldCenter(bool sampleHeight = false)
        {
            BoundsDouble bounds = new BoundsDouble();
            GetTerrainBounds(ref bounds);

            if (sampleHeight)
            {
                Terrain t = GetTerrain(bounds.center);
                if (t != null)
                {
                    Vector3 centerOnTerrain = t.transform.position + new Vector3(t.terrainData.size.x / 2f, 0f, t.terrainData.size.z / 2f);
                    float height = t.SampleHeight(centerOnTerrain);
                    return new Vector3(centerOnTerrain.x, height, centerOnTerrain.z);
                }
                else
                {
                    //No terrain? The user might be using mesh terrains then. Send out a raycast at the center to determine height
                    RaycastHit raycastHit = new RaycastHit();
                    if (Physics.Raycast(new Vector3Double(bounds.center.x, 1000000f, bounds.center.z), Vector3.down, out raycastHit))
                    {
                        return raycastHit.point;
                    }
                    else
                    {
                        return bounds.center;
                    }
                }
            }
            else
            {
                return new Vector3((float)bounds.center.x, (float)bounds.center.y, (float)bounds.center.z);
            }
        }
    }
}