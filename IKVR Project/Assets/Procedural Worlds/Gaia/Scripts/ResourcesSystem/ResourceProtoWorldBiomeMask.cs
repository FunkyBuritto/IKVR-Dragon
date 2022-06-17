using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Gaia
{
    /// <summary>
    /// Prototype for a world biome mask
    /// </summary>
    [System.Serializable]
    public class ResourceProtoWorldBiomeMask
    {
        [Tooltip("World Biome Mask name.")]
        public string m_name;
        [Tooltip("Associated Biome preset.")]
        public BiomePreset m_biomePreset;
        
    }
}