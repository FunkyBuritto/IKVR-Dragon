using UnityEngine;
using UnityEditor;
#if HDPipeline
using UnityEngine.Rendering.HighDefinition;
#endif
using System.Collections.Generic;
using ProceduralWorlds.WaterSystem;
using System;
using UnityEngine.Rendering;

namespace Gaia
{
    public static class ReflectionProbeEditorUtils
    {
        #region Variables

        //Stored list of reflection probes
        public static List<ReflectionProbe> m_storedProbes = new List<ReflectionProbe>();
        //Current created count
        public static int m_currentProbeCount;
        //If the probe system is active to begin baking the probes
        public static bool m_probeRenderActive = false;

        #endregion

        #region Reflection Probe Creation Button Functions

        /// <summary>
        /// Setup for the automatically probe spawning
        /// </summary>
        /// <param name="reflectionProbeData"></param>
        public static void CreateAutomaticProbes(ReflectionProbeData reflectionProbeData, GaiaConstants.EnvironmentRenderer renderPipelineSettings)
        {
            if (reflectionProbeData == null)
            {
                return;
            }

            if (reflectionProbeData.reflectionProbeRefresh == GaiaConstants.ReflectionProbeRefreshModePW.ProbeManager && reflectionProbeData.reflectionProbeMode == ReflectionProbeMode.Realtime)
            {
#if GAIA_PRO_PRESENT
                ReflectionProbeManager manager = ReflectionProbeManager.GetOrCreateProbeManager();
                if (manager != null)
                {
                    manager.ProbeLayerMask = reflectionProbeData.reflectionprobeCullingMask;
                }
#endif
            }
            else
            {
#if GAIA_PRO_PRESENT
                ReflectionProbeManager.RemoveReflectionProbeManager();
#endif
            }

            ClearCreatedReflectionProbes();

            GameObject oldReflectionProbe = GameObject.Find("Global Reflection Probe");
            if (oldReflectionProbe != null)
            {
                UnityEngine.Object.DestroyImmediate(oldReflectionProbe);
                Debug.Log("Old Reflection Probe Destroyed");
            }

            if (reflectionProbeData.reflectionProbesPerRow < 2)
            {
                Debug.LogError("Please set Probes Per Row to a value of 2 or higher");
            }
            else
            {
                m_currentProbeCount = 0;

                float seaLevel = 0f;
                bool seaLevelActive = false;

                PWS_WaterSystem gaiawater = GameObject.FindObjectOfType<PWS_WaterSystem>();
                if (gaiawater != null)
                {
                    seaLevel = gaiawater.SeaLevel;
                    seaLevelActive = true;
                }

                if (GaiaUtils.HasDynamicLoadedTerrains())
                {
                    Action<Terrain> terrAction = (t) => GenerateProbesOnTerrain(t, reflectionProbeData, seaLevelActive, seaLevel, renderPipelineSettings); 
                    GaiaUtils.CallFunctionOnDynamicLoadedTerrains(terrAction, true, null, "Generating Reflection Probes");
                }
                else
                {
                    if (Terrain.activeTerrains.Length < 1)
                    {
                        Debug.LogError("No terrains we're found, unable to generate reflection probes.");
                    }
                    else
                    {
                        foreach (var activeTerrain in Terrain.activeTerrains)
                        {
                            GenerateProbesOnTerrain(activeTerrain, reflectionProbeData, seaLevelActive, seaLevel, renderPipelineSettings);
                        }
                    }
                }
                m_probeRenderActive = true;
            }
        }



        #endregion

        #region Reflection Probe Utils

        private static void GenerateProbesOnTerrain(Terrain terrain, ReflectionProbeData reflectionProbeData, bool seaLevelActive, float seaLevel, GaiaConstants.EnvironmentRenderer renderPipelineSettings)
        {
            GameObject reflectionParentObject = ReflectionProbeParenting(terrain);
            Vector3 terrainSize = terrain.terrainData.size;
#if GAIA_PRO_PRESENT
            ReflectionProbeManager rpManager = ReflectionProbeManager.Instance;

            for (int row = 0; row < reflectionProbeData.reflectionProbesPerRow; ++row)
            {
                for (int columns = 0; columns < reflectionProbeData.reflectionProbesPerRow; ++columns)
                {
                    Vector3 newPosition = new Vector3
                    {
                        x = ((columns + 1) * terrainSize.x / reflectionProbeData.reflectionProbesPerRow) - terrainSize.x / reflectionProbeData.reflectionProbesPerRow / 2f + terrain.transform.position.x,
                        z = ((row + 1) * terrainSize.z / reflectionProbeData.reflectionProbesPerRow) - terrainSize.z / reflectionProbeData.reflectionProbesPerRow / 2f + terrain.transform.position.z
                    };
                    Vector3 size = new Vector3(terrain.terrainData.size.x / reflectionProbeData.reflectionProbesPerRow, terrain.terrainData.size.y, terrain.terrainData.size.z / reflectionProbeData.reflectionProbesPerRow);
                    if (rpManager != null)
                    {
                        ReflectionProbe newProbe = ReflectionProbeUtils.CreateReflectionProbe(newPosition, size, terrain, reflectionProbeData, seaLevelActive, seaLevel, reflectionParentObject.transform, renderPipelineSettings == GaiaConstants.EnvironmentRenderer.HighDefinition, rpManager.UseReflectionProbeCuller);
                        m_storedProbes.Add(newProbe);
                    }
                    else
                    {
                        ReflectionProbe newProbe = ReflectionProbeUtils.CreateReflectionProbe(newPosition, size, terrain, reflectionProbeData, seaLevelActive, seaLevel, reflectionParentObject.transform, renderPipelineSettings == GaiaConstants.EnvironmentRenderer.HighDefinition);
                        m_storedProbes.Add(newProbe);
                    }
                    m_currentProbeCount++;
                }
            }
#else
            for (int row = 0; row < reflectionProbeData.reflectionProbesPerRow; ++row)
            {
                for (int columns = 0; columns < reflectionProbeData.reflectionProbesPerRow; ++columns)
                {
                    Vector3 newPosition = new Vector3
                    {
                        x = ((columns + 1) * terrainSize.x / reflectionProbeData.reflectionProbesPerRow) - terrainSize.x / reflectionProbeData.reflectionProbesPerRow / 2f + terrain.transform.position.x,
                        z = ((row + 1) * terrainSize.z / reflectionProbeData.reflectionProbesPerRow) - terrainSize.z / reflectionProbeData.reflectionProbesPerRow / 2f + terrain.transform.position.z
                    };
                    Vector3 size = new Vector3(terrain.terrainData.size.x / reflectionProbeData.reflectionProbesPerRow, terrain.terrainData.size.y, terrain.terrainData.size.z / reflectionProbeData.reflectionProbesPerRow);
                    ReflectionProbe newProbe = ReflectionProbeUtils.CreateReflectionProbe(newPosition, size, terrain, reflectionProbeData, seaLevelActive, seaLevel, reflectionParentObject.transform, renderPipelineSettings == GaiaConstants.EnvironmentRenderer.HighDefinition);
                    m_storedProbes.Add(newProbe);
                    m_currentProbeCount++;
                }
            }
#endif

            EditorGUIUtility.PingObject(reflectionParentObject);
        }

        /// <summary>
        /// Creates the reflection prrobe parent object
        /// </summary>
        /// <param name="parentWithAmbientSkies"></param>
        /// <returns></returns>
        private static GameObject ReflectionProbeParenting(Terrain parentTerrain)
        {
            if (parentTerrain == null)
            {
                Debug.LogWarning("Terrain provided in ReflectionProbeParenting() is null");
                return null;
            }
            GameObject reflectionProbeParent = GameObject.Find(parentTerrain.name + " Reflection Probes");
            if (reflectionProbeParent == null)
            {
                reflectionProbeParent = new GameObject(parentTerrain.name + " Reflection Probes");
            }

            reflectionProbeParent.transform.SetParent(parentTerrain.transform);

            return reflectionProbeParent;
        }

        /// <summary>
        /// Clears reflection probes created
        /// </summary>
        public static void ClearCreatedReflectionProbes()
        {
            GameObject oldReflectionProbe = GameObject.Find("Global Reflection Probe");
            if (oldReflectionProbe != null)
            {
                UnityEngine.Object.DestroyImmediate(oldReflectionProbe);
                Debug.Log("Old Reflection Probe Destroyed");
            }

            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                Action<Terrain> terrAction = (t) => DeleteProbeGroup(t); 
                GaiaUtils.CallFunctionOnDynamicLoadedTerrains(terrAction, true, null, "Clearning Reflection Probes");
            }
            else
            {
                if (Terrain.activeTerrains.Length < 1)
                {
                    Debug.LogError("No terrains we're found, unable to generate reflection probes.");
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
            GameObject reflectionProbeParent = GameObject.Find(terrain.name + " Reflection Probes");
            if (reflectionProbeParent != null)
            {
                GameObject.DestroyImmediate(reflectionProbeParent);
                m_currentProbeCount = 0;
            }
        }

        #endregion
    }
}