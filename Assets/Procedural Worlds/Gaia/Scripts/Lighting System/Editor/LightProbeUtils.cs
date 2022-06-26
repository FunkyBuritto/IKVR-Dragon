using System;
using System.Collections.Generic;
using ProceduralWorlds.WaterSystem;
using UnityEngine;
using UnityEditor;

namespace Gaia
{
    public static class LightProbeUtils
    {
        #region Variables

        //Probe locaions 
        private static Quadtree<LightProbeGroup> m_probeLocations = new Quadtree<LightProbeGroup>(new Rect(0, 0, 10f, 10f));
        //Stored list of reflection probes
        public static int m_storedProbes;
        //Current created count
        public static int m_currentProbeCount;

        #endregion

        #region Light Probe Creation Button Functions

        /// <summary>
        /// Setup for the automatically probe spawning
        /// </summary>
        /// <param name="profile"></param>
        public static void CreateAutomaticProbes(ReflectionProbeData profile)
        {
            int numberTerrains = Terrain.activeTerrains.Length;

            if (numberTerrains == 0)
            {
                Debug.LogError("Unable to initiate probe spawning systen. No terrain found");
            }
            else
            {
                if (profile.lightProbesPerRow < 2)
                {
                    Debug.LogError("Please set Light Probes Per Row to a value of 2 or higher");
                }
                else
                {
                    LoadProbesFromScene();

                    m_probeLocations = null;
                    ClearCreatedLightProbes();

                    float seaLevel = 0f;

                    PWS_WaterSystem gaiawater = GameObject.FindObjectOfType<PWS_WaterSystem>();
                    if (gaiawater != null)
                    {
                        seaLevel = gaiawater.SeaLevel;
                    }

                    if (GaiaUtils.HasDynamicLoadedTerrains())
                    {
                        Action<Terrain> terrAction = (t) => GenerateProbesOnTerrain(t, profile, seaLevel); 
                        GaiaUtils.CallFunctionOnDynamicLoadedTerrains(terrAction, true, null, "Generating Light Probes");
                    }
                    else
                    {
                        foreach (var activeTerrain in Terrain.activeTerrains)
                        {
                            GenerateProbesOnTerrain(activeTerrain, profile, seaLevel);
                        }
                    }
                }
            }
        }

        #endregion

        #region Light Probe Utils

        private static void GenerateProbesOnTerrain(Terrain terrain, ReflectionProbeData profile, float seaLevel)
        {
            GameObject lightProbeObject = GameObject.Find(terrain.name + " Light Probes Group Data");
            if (lightProbeObject == null)
            {
                lightProbeObject = new GameObject(terrain.name + " Light Probes Group Data");
            }

            LightProbeGroup lightProbeData = lightProbeObject.GetComponent<LightProbeGroup>();
            if (lightProbeData == null)
            {
                lightProbeData = lightProbeObject.AddComponent<LightProbeGroup>();
                lightProbeData.probePositions = new Vector3[0];
            }
            GameObject lightParentObject = LightProbeParenting(terrain);
            Vector3 terrainSize = terrain.terrainData.size;

            m_storedProbes = profile.lightProbesPerRow * profile.lightProbesPerRow;

            for (int row = 0; row < profile.lightProbesPerRow; ++row)
            {
                for (int columns = 0; columns < profile.lightProbesPerRow; ++columns)
                {
                    Vector3 newPosition = lightProbeObject.transform.position - lightProbeData.transform.position;
                    newPosition.x = ((columns + 1) * terrainSize.x / profile.lightProbesPerRow) - terrainSize.x / profile.lightProbesPerRow / 2f + terrain.transform.position.x;
                    newPosition.z = ((row + 1) * terrainSize.z / profile.lightProbesPerRow) - terrainSize.z / profile.lightProbesPerRow / 2f + terrain.transform.position.z;
                    float sampledHeight = terrain.SampleHeight(newPosition);
                    newPosition.y = sampledHeight + 2.5f;

                    List<Vector3> probePositions = new List<Vector3>(lightProbeData.probePositions);

                    if (sampledHeight > seaLevel)
                    {
                        probePositions.Add(newPosition);
                        newPosition += new Vector3(0f, 2.5f, 0f);
                        probePositions.Add(newPosition);
                        newPosition += new Vector3(0f, 10f, 0f);
                        probePositions.Add(newPosition);
                        lightProbeData.probePositions = probePositions.ToArray();
                        m_currentProbeCount++;
                    }
                }
            }

            lightProbeObject.transform.SetParent(lightParentObject.transform);
            EditorGUIUtility.PingObject(lightParentObject);
        }

        /// <summary>
        /// Creates the reflection prrobe parent object
        /// </summary>
        /// <param name="parentWithGaia"></param>
        /// <returns></returns>
        private static GameObject LightProbeParenting(Terrain parentTerrain)
        {
            if (parentTerrain == null)
            {
                Debug.LogWarning("Terrain provided in ReflectionProbeParenting() is null");
                return null;
            }
            GameObject reflectionProbeParent = GameObject.Find(parentTerrain.name + " Light Probes");
            if (reflectionProbeParent == null)
            {
                reflectionProbeParent = new GameObject(parentTerrain.name + " Light Probes");
            }

            reflectionProbeParent.transform.SetParent(parentTerrain.transform);

            return reflectionProbeParent;
        }

        /// <summary>
        /// Load the probes in from the scene
        /// </summary>
        private static void LoadProbesFromScene()
        {
            //Start time
            //DateTime startTime = DateTime.Now;

            //Destroy previous contents
            m_probeLocations = null;

            //Work out the bounds of the environment
            float minY = float.NaN;
            float minX = float.NaN;
            float maxX = float.NaN;
            float minZ = float.NaN;
            float maxZ = float.NaN;
            Terrain sampleTerrain = null;
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                if (float.IsNaN(minY))
                {
                    sampleTerrain = terrain;
                    minY = terrain.transform.position.y;
                    minX = terrain.transform.position.x;
                    minZ = terrain.transform.position.z;
                    maxX = minX + terrain.terrainData.size.x;
                    maxZ = minZ + terrain.terrainData.size.z;
                }
                else
                {
                    if (terrain.transform.position.x < minX)
                    {
                        minX = terrain.transform.position.x;
                    }
                    if (terrain.transform.position.z < minZ)
                    {
                        minZ = terrain.transform.position.z;
                    }
                    if ((terrain.transform.position.x + terrain.terrainData.size.x) > maxX)
                    {
                        maxX = terrain.transform.position.x + terrain.terrainData.size.x;
                    }
                    if ((terrain.transform.position.z + terrain.terrainData.size.z) > maxZ)
                    {
                        maxZ = terrain.transform.position.z + terrain.terrainData.size.z;
                    }
                }
            }

            if (sampleTerrain != null)
            {
                Rect terrainBounds = new Rect(minX, minZ, maxX - minX, maxZ - minZ);
                m_probeLocations = new Quadtree<LightProbeGroup>(terrainBounds);
            }
            else
            {
                Rect bigSpace = new Rect(-10000f, -10000f, 20000f, 20000f);
                m_probeLocations = new Quadtree<LightProbeGroup>(bigSpace);
            }

            //Now grab all the light probes in the scene
            LightProbeGroup probeGroup;
            LightProbeGroup[] probeGroups = UnityEngine.Object.FindObjectsOfType<LightProbeGroup>();

            for (int probeGroupIdx = 0; probeGroupIdx < probeGroups.Length; probeGroupIdx++)
            {
                probeGroup = probeGroups[probeGroupIdx];
                for (int probePosition = 0; probePosition < probeGroup.probePositions.Length; probePosition++)
                {
                    m_probeLocations.Insert(probeGroup.transform.position.x + probeGroup.probePositions[probePosition].x, probeGroup.transform.position.z + probeGroup.probePositions[probePosition].z, probeGroup);
                }
            }
        }

        /// <summary>
        /// Add a probe instance into storage - must be called after the initial load call
        /// </summary>
        /// <param name="position">Position it is being located at</param>
        /// <param name="probeGroup">Probe group being managed</param>
        public static void AddProbe(Vector3 position, LightProbeGroup probeGroup)
        {
            if (m_probeLocations == null)
            {
                return;
            }
            m_probeLocations.Insert(position.x, position.z, probeGroup);
        }

        /// <summary>
        /// Add a probe instance into storage - must be called after the initial load call
        /// </summary>
        /// <param name="position">Position it is being located at</param>
        /// <param name="probeGroup">Probe group being managed</param>
        public static void RemoveProbe(Vector3 position, LightProbeGroup probeGroup)
        {
            if (m_probeLocations == null)
            {
                return;
            }
            m_probeLocations.Remove(position.x, position.z, probeGroup);
        }

        /// <summary>
        /// Clears reflection probes created
        /// </summary>
        public static void ClearCreatedLightProbes()
        {
            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                Action<Terrain> terrAction = (t) => DeleteProbeGroup(t); 
                GaiaUtils.CallFunctionOnDynamicLoadedTerrains(terrAction, true, null, "Clearing Light Probes");
            }
            else
            {
                if (Terrain.activeTerrains.Length < 1)
                {
                    Debug.LogError("No terrains we're found, unable to clear light probes.");
                }
                else
                {
                    foreach (var activeTerrain in Terrain.activeTerrains)
                    {
                        DeleteProbeGroup(activeTerrain);
                    }
                }
            }
        }

        private static void DeleteProbeGroup(Terrain terrain)
        {
            GameObject lightProbeParent = GameObject.Find(terrain.name + " Light Probes");
            if (lightProbeParent != null)
            {
                GameObject.DestroyImmediate(lightProbeParent);
                m_currentProbeCount = 0;
            }
        }

        #endregion
    }
}