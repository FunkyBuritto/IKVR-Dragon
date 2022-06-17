using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Gaia
{
    /// <summary>
    /// Stores noise settings for the noise image mask type for proper serialization.
    /// </summary>
    [System.Serializable]
    public class GaiaNoiseSettings
    {
        public Vector3 m_translation;
        public Vector3 m_rotation;
        public Vector3 m_scale = new Vector3(10f,1,10f);
        public string m_noiseTypeName = "Perlin";
        public string m_noiseTypeParams;
        public string m_fractalTypeName = "Fbm";
        public string m_fractalTypeParams;
        //public float m_octaves;
        //public float m_amplitude;
        //public float m_persistence;
        //public float m_frequency;
        //public float m_lacunarity;
        public bool m_warpEnabled;
        public float m_warpIterations;
        public float m_warpStrength;
        public Vector4 m_warpOffset;
    }
}