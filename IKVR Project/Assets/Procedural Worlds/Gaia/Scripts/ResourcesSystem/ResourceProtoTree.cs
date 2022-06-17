using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gaia
{
    /// <summary>
    /// Used to serialise the tree prototypes
    /// </summary>
    [System.Serializable]
    public class ResourceProtoTree
    {
        [Tooltip("Resource name.")]
        public string m_name;
        [Tooltip("Desktop prefab.")]
        public GameObject m_desktopPrefab;
        [HideInInspector]
        public string m_desktopPrefabFileName; // Used for re-association
        //[Tooltip("Mobile prefab - future proofing here - not currently used.")]
        //public GameObject m_mobilePrefab;
        [HideInInspector]
        public string m_mobilePrefabFileName; // Used for re-association
        [Tooltip("How much the tree bends in the wind - only used by unity tree creator trees, ignored by SpeedTree trees.")]
        public float m_bendFactor;
        [Tooltip("The colour of healthy trees - only used by unity tree creator trees, ignored by SpeedTree trees.")]
        public Color m_healthyColour = Color.white;
        [Tooltip("The colour of dry trees - only used by unity tree creator trees, ignored by SpeedTree trees.")]
        public Color m_dryColour = Color.white;
        [Tooltip("The spawn scale used for this tree")]
        public SpawnScale m_spawnScale = SpawnScale.Fitness;
        [Tooltip("Minimum width in world units.")]
        public float m_minWidth = 0.75f;
        [Tooltip("Maximum width in world units.")]
        public float m_maxWidth = 1.5f;
        [Tooltip("Minimum height in world units.")]
        public float m_minHeight = 0.75f;         //Height in world units
        [Tooltip("Maximum height in world units.")]
        public float m_maxHeight = 1.5f;         //Height in world units

        public float m_widthRandomPercentage = 0.5f;
        public float m_heightRandomPercentage = 0.5f;


        /// The "last used fields" are filled when the tree is being spawned and can then be used to re-scle the tree when the user refreshes the prototype
        public SpawnScale m_lastUsedSpawnScale = SpawnScale.Fitness;
        public float m_lastUsedMinWidth = 0.75f;
        public float m_lastUsedMaxWidth = 1.5f;
        public float m_lastUsedMinHeight = 0.75f;         //Height in world units
        public float m_lastUsedMaxHeight = 1.5f;         //Height in world units
        public float m_lastUsedWidthRandomPercentage = 0.5f;
        public float m_lastUsedHeightRandomPercentage = 0.5f;



        [Tooltip("SPAWN CRITERIA - Spawn criteria are run against the terrain to assess its fitness in a range of 0..1 for use by this resource. If you add multiple criteria then the fittest one will be selected.")]
        public SpawnCritera[] m_spawnCriteria = new SpawnCritera[0];
        [Tooltip("SPAWN EXTENSIONS - Spawn extensions allow fitness, spawning and post spawning extensions to be made to the spawning system.")]
        public SpawnRuleExtension[] m_spawnExtensions = new SpawnRuleExtension[0];
        //[Tooltip("SPAWN MASKS - This list of masks can be used to determine where the terrain detail will appear on the terrain.")]
        //public ImageMask[] m_imageMasks = new ImageMask[0];
        public bool m_instancesFoldOut;
        public bool m_dnaFoldedOut;




        /// <summary>
        /// Initialise the tree
        /// </summary>
        /// <param name="spawner">The spawner it belongs to</param>
        public void Initialise(Spawner spawner)
        {
            foreach (SpawnCritera criteria in m_spawnCriteria)
            {
                criteria.Initialise(spawner);
            }
        }

        /// <summary>
        /// Determine whether this has active criteria
        /// </summary>
        /// <returns>True if has actrive criteria</returns>
        public bool HasActiveCriteria()
        {
            for (int idx = 0; idx < m_spawnCriteria.Length; idx++)
            {
                if (m_spawnCriteria[idx].m_isActive)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Set up the asset associations, return true if something changes. Can only be run when the editor is present.
        /// </summary>
        /// <returns>True if something changes</returns>
        public bool SetAssetAssociations()
        {
            bool isModified = false;

            #if UNITY_EDITOR
            if (m_desktopPrefab != null)
            {
                string fileName = Path.GetFileName(AssetDatabase.GetAssetPath(m_desktopPrefab));
                if (fileName != m_desktopPrefabFileName)
                {
                    m_desktopPrefabFileName = fileName;
                    isModified = true;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(m_desktopPrefabFileName))
                {
                    m_desktopPrefabFileName = "";
                    isModified = true;
                }
            }

            //if (m_mobilePrefab != null)
            //{
            //    string fileName = Path.GetFileName(AssetDatabase.GetAssetPath(m_mobilePrefab));
            //    if (fileName != m_mobilePrefabFileName)
            //    {
            //        m_mobilePrefabFileName = fileName;
            //        isModified = true;
            //    }
            //}
            //else
            //{
            //    if (!string.IsNullOrEmpty(m_mobilePrefabFileName))
            //    {
            //        m_mobilePrefabFileName = "";
            //        isModified = true;
            //    }
            //}

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
            if (m_desktopPrefab == null)
            {
                if (!string.IsNullOrEmpty(m_desktopPrefabFileName))
                {
                    m_desktopPrefab = GaiaUtils.GetAsset(m_desktopPrefabFileName, typeof(UnityEngine.GameObject)) as GameObject;
                    if (m_desktopPrefab != null)
                    {
                        isModified = true;
                    }
                }
            }

            //if (m_mobilePrefab == null)
            //{
            //    if (!string.IsNullOrEmpty(m_mobilePrefabFileName))
            //    {
            //        m_mobilePrefab = GaiaUtils.GetAsset(m_mobilePrefabFileName, typeof(UnityEngine.GameObject)) as GameObject;
            //        if (m_mobilePrefab != null)
            //        {
            //            isModified = true;
            //        }
            //    }
            //}
            #endif
            return isModified;
        }


        /// <summary>
        /// Determine whether this has active criteria that checks textures
        /// </summary>
        /// <returns>True if has active criteria that checks textures</returns>
        public bool ChecksTextures()
        {
            for (int idx = 0; idx < m_spawnCriteria.Length; idx++)
            {
                if (m_spawnCriteria[idx].m_isActive && m_spawnCriteria[idx].m_checkTexture)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determine whether this has active criteria that checks proximity
        /// </summary>
        /// <returns>True if has active criteria that checks proximity</returns>
        public bool ChecksProximity()
        {
            for (int idx = 0; idx < m_spawnCriteria.Length; idx++)
            {
                if (m_spawnCriteria[idx].m_isActive && m_spawnCriteria[idx].m_checkProximity)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Add tags to the list if they are not already there
        /// </summary>
        /// <param name="tagList">The list to add the tags to</param>
        public void AddTags(ref List<string> tagList)
        {
            for (int idx = 0; idx < m_spawnCriteria.Length; idx++)
            {
                if (m_spawnCriteria[idx].m_isActive && m_spawnCriteria[idx].m_checkProximity)
                {
                    if (!tagList.Contains(m_spawnCriteria[idx].m_proximityTag))
                    {
                        tagList.Add(m_spawnCriteria[idx].m_proximityTag);
                    }
                }
            }
        }

        /// <summary>
        /// Stores the current spawn scale settings in the "last used" fields
        /// </summary>
        public void StoreLastUsedScaleSettings()
        {
            m_lastUsedSpawnScale = m_spawnScale;
            m_lastUsedMinWidth = m_minWidth;
            m_lastUsedMaxWidth = m_maxWidth;
            m_lastUsedMinHeight = m_minHeight;
            m_lastUsedMaxHeight = m_maxHeight;
            m_lastUsedWidthRandomPercentage = m_widthRandomPercentage;
            m_lastUsedHeightRandomPercentage = m_heightRandomPercentage;
        }

        /// <summary>
        /// Compares the current spawn scale settings agains the "last used" settings to determine whether the scaling was changed
        /// after the tree was spawned and the tree needs to be rescaled
        /// </summary>
        /// <returns></returns>
        public bool NeedsRescale()
        {
            return m_lastUsedSpawnScale != m_spawnScale ||
            m_lastUsedMinWidth != m_minWidth ||
            m_lastUsedMaxWidth != m_maxWidth ||
            m_lastUsedMinHeight != m_minHeight ||
            m_lastUsedMaxHeight != m_maxHeight ||
            m_lastUsedWidthRandomPercentage != m_widthRandomPercentage ||
            m_lastUsedHeightRandomPercentage != m_heightRandomPercentage;
        }
    }
}