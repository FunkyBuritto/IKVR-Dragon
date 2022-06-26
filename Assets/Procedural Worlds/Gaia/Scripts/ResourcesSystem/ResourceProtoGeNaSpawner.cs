using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gaia
{
    [System.Serializable]
    public class ResourceProtoSpawnExtensionInstance
    {
        public string m_name;
        public GameObject m_spawnerPrefab;
        //[HideInInspector]
        public string m_spawnerPrefabFileName; // Used for re-association
        ////[Tooltip("Mobile prefab - future proofing here - not currently used.")]
        ////public GameObject m_mobilePrefab;
        ////[HideInInspector]
        //public string m_mobilePrefabFileName; // Used for re-association

        //V1.5 from here

        public int m_minSpawnerRuns = 1;
        public int m_maxSpawnerRuns = 1;

        public float m_failureRate = 0f;

        public float m_minSpawnOffsetX = 0f;
        public float m_maxSpawnOffsetX = 0f;

        //[Tooltip("Minimum Y offset from terrain in meters to intantiate at. Can use this to move embed or raise objects from the terrain.")]
        public float m_minSpawnOffsetY = -0.3f;
        //[Tooltip("Maximum Y offset from terrain in meters to intantiate at. Can use this to move embed or raise objects from the terrain.")]
        public float m_maxSpawnOffsetY = -0.1f;

        public float m_minSpawnOffsetZ = 0f;
        public float m_maxSpawnOffsetZ = 0f;

        //[Tooltip("Rotate the object to the terrain normal. Allows natural slope following. Great for things like trees to give them a little more variation in your scene.")]
        //public bool m_rotateToSlope = false;

        //[Tooltip("Minimum X rotation from spawned rotation to intantiate at. Can use this to rotate objects relative to spawn point rotation.")]
        [Range(-180f, 180f)]
        public float m_minRotationOffsetX = 0f;
        //[Tooltip("Maximum X rotation from spawned rotation to intantiate at. Can use this to rotate objects relative to spawn point rotation."), Range(-180f, 180f)]
        public float m_maxRotationOffsetX = 0f;

        //[Tooltip("Minimum Y rotation from spawned rotation to intantiate at. Can use this to rotate objects relative to spawn point rotation."), Range(-180f, 180f)]
        public float m_minRotationOffsetY = -180f;
        //[Tooltip("Maximum Y rotation from spawned rotation to intantiate at. Can use this to rotate objects relative to spawn point rotation."), Range(-180f, 180f)]
        public float m_maxRotationOffsetY = 180f;

        //[Tooltip("Minimum Z rotation from spawned rotation to intantiate at. Can use this to rotate objects relative to spawn point rotation."), Range(-180f, 180f)]
        public float m_minRotationOffsetZ = 0f;
        //[Tooltip("Maximum Z rotation from spawned rotation to intantiate at. Can use this to rotate objects relative to spawn point rotation."), Range(-180f, 180f)]
        public float m_maxRotationOffsetZ = 0f;

        //[Tooltip("Get object scale from parent scale.")]
        //public bool m_useParentScale = true;

        //[Tooltip("Minimum scale."), Range(0f, 20f)]
        public float m_minScale = 1f;
        //[Tooltip("Maximum scale."), Range(0f, 20)]
        public float m_maxScale = 1f;
        //[Tooltip("Influence scale between min and max scale based on distance from spawn point centre.")]
        public AnimationCurve m_scaleByDistance = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));

        //[Tooltip("Local bounds radius of this instance.")]
        //public float m_localBounds = 5f;

        //[Tooltip("Will only spawn on virgin terrain.")]
        //public bool m_virginTerrain = true;

        //[Tooltip("Custom parameter to be interpreted by an extension if there is one.")]
        //public string m_extParam = "";

        public SpawnScale m_spawnScale = SpawnScale.Fixed;

        public bool m_commonScale = true;
        public Vector3 m_minXYZScale = new Vector3(0.75f, 0.75f, 0.75f);
        public Vector3 m_maxXYZScale = new Vector3(1.5f, 1.5f, 1.5f);
        public float m_scaleRandomPercentage = 0.5f;
        public Vector3 m_XYZScaleRandomPercentage = new Vector3(0.5f, 0.5f, 0.5f);

        //Just a control flag to store if this instance is folded out
        [HideInInspector]
        public bool m_foldedOut;
        public bool m_invalidPrefabSupplied;
    }

    [System.Serializable]
    public class ResourceProtoSpawnExtension
    {
        public string m_name;
        public ResourceProtoSpawnExtensionInstance[] m_instances = new ResourceProtoSpawnExtensionInstance[0];
        public ResourceProtoDNA m_dna = new ResourceProtoDNA();
        
        //[Tooltip("SPAWN MASKS - This list of masks can be used to determine where the terrain detail will appear on the terrain.")]
        //public ImageMask[] m_imageMasks = new ImageMask[0];

        //just a control flag to store if the instances list is folded out or not
        [HideInInspector]
        public bool m_instancesFoldOut;
        public bool m_dnaFoldedOut;
        



        /// <summary>
        /// Set up the asset associations, return true if something changes. Can only be run when the editor is present.
        /// </summary>
        /// <returns>True if something changes</returns>
        public bool SetAssetAssociations()
        {
            bool isModified = false;

#if UNITY_EDITOR
            ResourceProtoSpawnExtensionInstance goInstance;
            for (int idx = 0; idx < m_instances.GetLength(0); idx++)
            {
                goInstance = m_instances[idx];

                if (goInstance.m_spawnerPrefab != null)
                {
                    string fileName = Path.GetFileName(AssetDatabase.GetAssetPath(goInstance.m_spawnerPrefab));
                    if (fileName != goInstance.m_spawnerPrefabFileName)
                    {
                        goInstance.m_spawnerPrefabFileName = fileName;
                        isModified = true;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(goInstance.m_spawnerPrefabFileName))
                    {
                        goInstance.m_spawnerPrefabFileName = "";
                        isModified = true;
                    }
                }

                //if (goInstance.m_mobilePrefab != null)
                //{
                //    string fileName = Path.GetFileName(AssetDatabase.GetAssetPath(goInstance.m_mobilePrefab));
                //    if (fileName != goInstance.m_mobilePrefabFileName)
                //    {
                //        goInstance.m_mobilePrefabFileName = fileName;
                //        isModified = true;
                //    }
                //}
                //else
                //{
                //    if (!string.IsNullOrEmpty(goInstance.m_mobilePrefabFileName))
                //    {
                //        goInstance.m_mobilePrefabFileName = "";
                //        isModified = true;
                //    }
                //}
            }
            #endif

            return isModified;
        }


        /// <summary>
        /// Associate any unallocated assets to this resource. Return true if something changes.
        /// </summary>
        /// <returns>True if the prototype was in some way modified</returns>
        public bool AssociateAssets()
        {
            bool isModified = false;

#if UNITY_EDITOR
            ResourceProtoSpawnExtensionInstance goInstance;
            for (int idx = 0; idx < m_instances.GetLength(0); idx++)
            {
                goInstance = m_instances[idx];

                if (goInstance.m_spawnerPrefab == null)
                {
                    if (!string.IsNullOrEmpty(goInstance.m_spawnerPrefabFileName))
                    {
                        goInstance.m_spawnerPrefab = GaiaUtils.GetAsset(goInstance.m_spawnerPrefabFileName, typeof(UnityEngine.GameObject)) as GameObject;
                        if (goInstance.m_spawnerPrefab != null)
                        {
                            isModified = true;
                        }
                    }
                }

                //if (goInstance.m_mobilePrefab == null)
                //{
                //    if (!string.IsNullOrEmpty(goInstance.m_mobilePrefabFileName))
                //    {
                //        goInstance.m_mobilePrefab = GaiaUtils.GetAsset(goInstance.m_mobilePrefabFileName, typeof(UnityEngine.GameObject)) as GameObject;
                //        if (goInstance.m_mobilePrefab != null)
                //        {
                //            isModified = true;
                //        }
                //    }
                //}
            }
            #endif

            return isModified;
        }

        /// <summary>
        /// Determine whether this has active criteria that checks textures
        /// </summary>
        /// <returns>True if has active criteria that checks textures</returns>
        //public bool ChecksTextures()
        //{
        //    for (int idx = 0; idx < m_spawnCriteria.Length; idx++)
        //    {
        //        if (m_spawnCriteria[idx].m_isActive && m_spawnCriteria[idx].m_checkTexture)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        /// <summary>
        /// Determine whether this has active criteria that checks proximity
        /// </summary>
        /// <returns>True if has active criteria that checks proximity</returns>
        //public bool ChecksProximity()
        //{
        //    for (int idx = 0; idx < m_spawnCriteria.Length; idx++)
        //    {
        //        if (m_spawnCriteria[idx].m_isActive && m_spawnCriteria[idx].m_checkProximity)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        /// <summary>
        /// Add tags to the list if they are not already there
        /// </summary>
        ///// <param name="tagList">The list to add the tags to</param>
        //public void AddTags(ref List<string> tagList)
        //{
        //    for (int idx = 0; idx < m_spawnCriteria.Length; idx++)
        //    {
        //        if (m_spawnCriteria[idx].m_isActive && m_spawnCriteria[idx].m_checkProximity)
        //        {
        //            if (!tagList.Contains(m_spawnCriteria[idx].m_proximityTag))
        //            {
        //                tagList.Add(m_spawnCriteria[idx].m_proximityTag);
        //            }
        //        }
        //    }
        //}

    }
}