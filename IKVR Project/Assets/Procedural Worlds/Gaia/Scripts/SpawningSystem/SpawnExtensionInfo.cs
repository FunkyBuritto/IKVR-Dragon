using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gaia
{
    public class SpawnExtensionInfo
    {
        /// <summary>
        /// The location where the Spawn Extension is triggered in world space.
        /// </summary>
        public Vector3 m_position;
        /// <summary>
        /// The (suggested) rotation for this location - it is up to the extension to implement or ignore this
        /// </summary>
        public Quaternion m_rotation;
        /// <summary>
        /// The (suggested) scale for this location - it is up to the extension to implement or ignore this
        /// </summary>
        public Vector3 m_scale;
        /// <summary>
        /// The scalar fitness value for this location. 0 = no fitness, 1 = full fitness.
        /// </summary>
        public float m_fitness;
        /// <summary>
        /// The current terrain the Spawn Extension was triggered on. Use this together with the location to query information from the terrain, e.g. slope, etc.
        /// </summary>
        public Terrain m_currentTerrain;
    }
}