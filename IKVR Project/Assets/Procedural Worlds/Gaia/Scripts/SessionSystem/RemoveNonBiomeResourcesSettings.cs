
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace Gaia
{
    [System.Serializable]
    public class RemoveNonBiomeResourcesSettings : ScriptableObject
    {
        #region public input variables
        public BiomeControllerSettings m_biomeControllerSettings;
        public List<SpawnerSettings> m_spawnerSettingsList; 
        #endregion
    }
}