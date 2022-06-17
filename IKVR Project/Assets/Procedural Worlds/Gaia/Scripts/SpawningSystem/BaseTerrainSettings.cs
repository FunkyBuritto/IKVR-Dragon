// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using System.Collections.Generic;
using static Gaia.GaiaConstants;
using UnityEditor;

/*
 * Scriptable Object containing settings for a Spawner
 */
 public enum NoiseTypeName { Billow, Perlin, Ridge, Value}

namespace Gaia
{
    /// <summary> Contains settings for the initial creation of the Base Terrain during random terrain generation</summary>
    [System.Serializable]
    public class BaseTerrainSettings : ScriptableObject, ISerializationCallbackReceiver
    {

        #region Public Variables

        /// <summary>
        /// If the baseTerrain should be displayed as a preview in the random terrain generator or not.
        /// </summary>
        public bool m_drawPreview = true;

        public float m_heightScale = 8f;
        public float m_baseLevel = 0f;
        public float m_heightVariance = 0.5f;
        public GeneratorBorderStyle m_borderStyle;
        public NoiseTypeName m_shapeNoiseStyle = NoiseTypeName.Perlin;
        public float m_shapeSize = 8f;
        public float m_shapeStrength = 0.35f;
        public float m_shapeSteepness = 0.4f;
        public float m_shapeGranularity = 2f;


        public NoiseTypeName m_mountainNoiseStyle = NoiseTypeName.Billow;
        public float m_mountainSize = 6.5f;
        public float m_mountainStrength = 0.15f;
        public float m_mountainSteepness = 0.1f;
        public float m_mountainGranularity = 2f;
        

        public NoiseTypeName m_valleyNoiseStyle = NoiseTypeName.Perlin;
        public float m_valleySize = 5f;
        public float m_valleyStrength = 0.1f;
        public float m_valleySteepness = 0.2f;
        public float m_valleyGranularity = 1f;
        
        public Vector3 m_shapeOffset;
        public Vector3 m_mountainOffset;
        public Vector3 m_valleyOffset;
        public float m_smoothness = 1f;
        public bool m_shapeFoldedOut;
        public bool m_mountainsFoldedOut;
        public bool m_valleysFoldedOut;



        #endregion
        #region Serialization

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
        }

        #endregion


   

    }
}
