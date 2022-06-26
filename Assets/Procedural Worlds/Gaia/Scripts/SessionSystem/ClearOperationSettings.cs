
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace Gaia
{
    [System.Serializable]
    public class ClearOperationSettings : ScriptableObject
    {
        #region public input variables
        public bool m_clearTrees;
        public bool m_clearTerrainDetails;
        public bool m_clearGameObjects;
        public bool m_clearSpawnExtensions;
        public bool m_clearProbes;
        public bool m_clearStamps;
        public ClearSpawnFrom m_clearSpawnFrom;
        public ClearSpawnFor m_clearSpawnFor;
        public SpawnerSettings m_spawnerSettings; 
        #endregion
    }
}