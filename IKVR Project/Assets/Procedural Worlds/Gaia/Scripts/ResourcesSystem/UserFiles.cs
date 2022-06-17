using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gaia
{

    public class UserFiles : ScriptableObject
    {
        public bool m_autoAddNewFiles = true;
        public bool m_updateFilesWithGaiaUpdate = true;
        public List<BiomePreset> m_gaiaManagerBiomePresets = new List<BiomePreset>();
        public List<SpawnerSettings> m_gaiaManagerSpawnerSettings = new List<SpawnerSettings>();
        public List<ExportTerrainSettings> m_exportTerrainSettings = new List<ExportTerrainSettings>();

        public void PruneNonExisting()
        {
#if UNITY_EDITOR
            for (int i = m_gaiaManagerBiomePresets.Count - 1; i >= 0; i--)
            {
                if (String.IsNullOrEmpty(AssetDatabase.GetAssetPath(m_gaiaManagerBiomePresets[i])))
                {
                    m_gaiaManagerBiomePresets.RemoveAt(i);
                }
            }
            for (int i = m_gaiaManagerSpawnerSettings.Count - 1; i >= 0; i--)
            {
                if (String.IsNullOrEmpty(AssetDatabase.GetAssetPath(m_gaiaManagerSpawnerSettings[i])))
                {
                    m_gaiaManagerSpawnerSettings.RemoveAt(i);
                }
            }
            for (int i = m_exportTerrainSettings.Count - 1; i >= 0; i--)
            {
                if (String.IsNullOrEmpty(AssetDatabase.GetAssetPath(m_exportTerrainSettings[i])))
                {
                    m_exportTerrainSettings.RemoveAt(i);
                }
            }
#endif
        }
    }
}