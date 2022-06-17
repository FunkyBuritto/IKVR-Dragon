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
    /// Prototype for textures and their fitness
    /// </summary>
    [System.Serializable]
    public class ResourceProtoTexture
    {
        [Tooltip("Resource name.")]
        public string m_name;
        [Tooltip("Resource texture.")]
        public Texture2D m_texture;
#if SUBSTANCE_PLUGIN_ENABLED
        public int substanceSourceIndex;
        public Substance.Game.Substance m_substanceMaterial;
#endif
        [HideInInspector]
        public string m_textureFileName; //Used for re-association
        [Tooltip("Resource normal.")]
        public Texture2D m_normal;
        public Texture2D m_maskmap;

        public Texture2D m_CTSSmoothnessMap;
        public Texture2D m_CTSRoughnessMap;
        public Texture2D m_CTSHeightMap;
        public Texture2D m_CTSAmbientOcclusionMap;

        [HideInInspector]
        public string m_normalFileName; //Used for re-association
        [Tooltip("The width over which the image will stretch on the terrain’s surface.")]
        public float m_sizeX = 10;
        [Tooltip("The height over which the image will stretch on the terrain’s surface.")]
        public float m_sizeY = 10;
        [Tooltip("How far from the terrain’s anchor point the tiling will start.")]
        public float m_offsetX = 0;
        [Tooltip("How far from the terrain’s anchor point the tiling will start.")]
        public float m_offsetY = 0;
        [Tooltip("Controls the overall metalness of the surface."), Range(0f, 1f)]
        public float m_metallic = 0f;
        [Tooltip("Controls the overall smoothness of the surface."), Range(0f, 1f)]
        public float m_smoothness = 0f;
        [Tooltip("SPAWN CRITERIA - Spawn criteria are run against the terrain to assess its fitness in a range of 0..1 for use by this resource. If you add multiple criteria then the fittest one will be selected.")]
        public SpawnCritera[] m_spawnCriteria = new SpawnCritera[0];
        private string m_maskmapFileName;
        /// <summary>
        /// The GUID of a terrain layer that was created for this texture resource.
        /// </summary>
        public string m_LayerGUID;
        public float m_normalScale = 1f;
        public Vector4 m_diffuseRemapMax = new Vector4(1f,1f,1f,1f);
        public Vector4 m_diffuseRemapMin = new Vector4(0f,0f,0f,0f);
        public Vector4 m_maskMapRemapMax = new Vector4(1f,1f,1f,1f);
        public Vector4 m_maskMapRemapMin = new Vector4(0f,0f,0f,0f);
        public Color m_specularColor = new Color();
        public bool m_channelRemapFoldedOut;


        //[Tooltip("SPAWN MASKS - This list of masks can be used to determine where the texture will appear on the terrain.")]
        //public ImageMask[] m_imageMasks = new ImageMask[0];
        //[Tooltip("SPAWN EXTENSIONS - Spawn extensions allow fitness, spawning and post spawning extensions to be made to the spawning system.")]
        //public SpawnRuleExtension[] m_spawnExtensions = new SpawnRuleExtension[0];

        /// <summary>
        /// Used to help locate the physical textures
        /// </summary>

        /// <summary>
        /// Initialise the texture
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
            if (m_texture != null)
            {
                string fileName = Path.GetFileName(AssetDatabase.GetAssetPath(m_texture));
                if (fileName != m_textureFileName)
                {
                    m_textureFileName = fileName;
                    isModified = true;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(m_textureFileName))
                {
                    m_textureFileName = "";
                    isModified = true;
                }
            }

            if (m_normal != null)
            {
                string fileName = Path.GetFileName(AssetDatabase.GetAssetPath(m_normal));
                if (fileName != m_normalFileName)
                {
                    m_normalFileName = fileName;
                    isModified = true;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(m_normalFileName))
                {
                    m_normalFileName = "";
                    isModified = true;
                }
            }

            if (m_maskmap != null)
            {
                string fileName = Path.GetFileName(AssetDatabase.GetAssetPath(m_maskmap));
                if (fileName != m_maskmapFileName)
                {
                    m_maskmapFileName = fileName;
                    isModified = true;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(m_maskmapFileName))
                {
                    m_maskmapFileName = "";
                    isModified = true;
                }
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
            if (m_texture == null)
            {
                if (!string.IsNullOrEmpty(m_textureFileName))
                {
                    m_texture = GaiaUtils.GetAsset(m_textureFileName, typeof(UnityEngine.Texture2D)) as Texture2D;
                    if (m_texture != null)
                    {
                        isModified = true;
                    }
                }
            }

            if (m_normal == null)
            {
                if (!string.IsNullOrEmpty(m_normalFileName))
                {
                    m_normal = GaiaUtils.GetAsset(m_normalFileName, typeof(UnityEngine.Texture2D)) as Texture2D;
                    if (m_normal != null)
                    {
                        isModified = true;
                    }
                }
            }

            if (m_maskmap == null)
            {
                if (!string.IsNullOrEmpty(m_maskmapFileName))
                {
                    m_maskmap = GaiaUtils.GetAsset(m_maskmapFileName, typeof(UnityEngine.Texture2D)) as Texture2D;
                    if (m_maskmap != null)
                    {
                        isModified = true;
                    }
                }
            }


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
    }
}