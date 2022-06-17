
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace Gaia
{
    [System.Serializable]
    public class SpawnOperationSettings : ScriptableObject
    {
        #region public input variables
        /// <summary>
        /// The area where the spawn should be executed
        /// </summary>
        public BoundsDouble m_spawnArea;

        /// <summary>
        /// The biome controller settings - if the spawn was performed from a biome controller.
        /// </summary>
        public BiomeControllerSettings m_biomeControllerSettings;

        /// <summary>
        /// Whether this spawn was performed on the world map or not.
        /// </summary>
        public bool m_isWorldMapSpawner;

        /// <summary>
        /// List of all the spawner settings that should be spawned
        /// </summary>
        public List<SpawnerSettings> m_spawnerSettingsList;
        #endregion


       


    }
}