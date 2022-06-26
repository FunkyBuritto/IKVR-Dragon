using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif


namespace Gaia
{
    [System.Serializable]
    public class BiomeSpawnerListEntry : IComparable
    {
        public bool m_autoAssignPrototypes;
        public SpawnerSettings m_spawnerSettings;
        public bool m_isActiveInStamper;
        public bool m_isActiveInBiome;

        public int CompareTo(object other)
        {
            if (other.GetType() != typeof(BiomeSpawnerListEntry))
                throw new NotImplementedException();

            return (m_spawnerSettings.name.CompareTo(((BiomeSpawnerListEntry)other).m_spawnerSettings.name));
        }
    }

    [System.Serializable]
    public class AdvancedTabBiomeListEntry : IComparable
    {
        public bool m_autoAssignPrototypes;
        public BiomePreset m_biomePreset;

        public int CompareTo(object other)
        {
            if (other.GetType() != typeof(AdvancedTabBiomeListEntry))
                throw new NotImplementedException();

            return (m_biomePreset.m_orderNumber.CompareTo(((AdvancedTabBiomeListEntry)other).m_biomePreset.m_orderNumber));
        }
    }


    /// <summary>
    /// Just a simple association from Spawner Preset to an incremental int ID - used in the the Gaia Manager Window to have a simple dropdown of possible presets + an empty "Custom" selection 
    /// </summary>
    public struct BiomePresetDropdownEntry : IComparable
    {
        public BiomePreset biomePreset;
        public string name;
        public int ID;

        public int CompareTo(object other)
        {
            if (other.GetType() != typeof(BiomePresetDropdownEntry))
                throw new NotImplementedException();

            return biomePreset.m_orderNumber.CompareTo(((BiomePresetDropdownEntry)other).biomePreset.m_orderNumber);
        }
    }

    /// <summary> Contains a list of spawner settings that are designed to be created together with a new terrain.</summary>
    [CreateAssetMenu(menuName = "Procedural Worlds/Gaia/Biome Preset")]
    [System.Serializable]
    public class BiomePreset : ScriptableObject
    {
        public int m_orderNumber = 0;
        public List<BiomeSpawnerListEntry> m_spawnerPresetList = new List<BiomeSpawnerListEntry>();

#if UNITY_POST_PROCESSING_STACK_V2
        [System.NonSerialized]
        private PostProcessProfile m_postProcessProfile = null;

        //This construct ensures we only serialize the GUID of the PP profile, but not the profile itself
        //The GUID will "survive" when PP is not installed in a project, while the PP profile object would not
        public PostProcessProfile postProcessProfile
        {

            get
            {
                if (m_postProcessProfile == null && m_postProcessingProfileGUID != null)
                {
#if UNITY_EDITOR
                    m_postProcessProfile = (PostProcessProfile)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_postProcessingProfileGUID), typeof(PostProcessProfile));
#endif
                }
                return m_postProcessProfile;
            }
            set
            {
#if UNITY_EDITOR
                m_postProcessingProfileGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
                m_postProcessProfile = value;
#endif

            }
        }

#endif
        //need this serialized to remember the GUID even when PP is not installed in the project
        [SerializeField]
        private string m_postProcessingProfileGUID = "";
        [SerializeField]
        private GaiaSceneCullingProfile m_gaiaSceneCullingProfile;
        public GaiaSceneCullingProfile GaiaSceneCullingProfile
        {
            get
            {
                if (m_gaiaSceneCullingProfile == null)
                {
                    m_gaiaSceneCullingProfile = (GaiaSceneCullingProfile)GaiaUtils.GetAsset(GaiaConstants.defaultSceneCullingProfile, typeof(GaiaSceneCullingProfile));
                }
                return m_gaiaSceneCullingProfile;
            }
            set
            {
                m_gaiaSceneCullingProfile = value;
            }
        }

        public void RefreshSpawnerListEntries()
        {
#if UNITY_EDITOR
            //walk through the references spawner settings files, and if they have a "last saved path" reload them again in this preset to make sure the reference in there is correct.
            foreach (BiomeSpawnerListEntry bsle in m_spawnerPresetList)
            {
                if (bsle.m_spawnerSettings != null)
                {
                    if (!string.IsNullOrEmpty(bsle.m_spawnerSettings.m_lastGUIDSaved))
                    {
                        SpawnerSettings spawnerSettings = (SpawnerSettings)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(bsle.m_spawnerSettings.m_lastGUIDSaved), typeof(SpawnerSettings));
                        if (spawnerSettings != null)
                        {
                            bsle.m_spawnerSettings = spawnerSettings;
                        }
                    }
                }
            }
#endif
        }

        public BiomeController CreateBiome(bool autoAssignPrototypes)
        {
            int totalSteps = m_spawnerPresetList.Count;
            int currentStep = 0;

#if !UNITY_POST_PROCESSING_STACK_V2
             //Workaround to disable warnings for "unused" field m_postProcessingProfileGUID
            //This is just a "harmless" piece of code to make the compiler think the field is in use
            if (m_postProcessingProfileGUID == "")
            {
                currentStep = 0;
            }
#endif

            GaiaSessionManager sessionManager = GaiaSessionManager.GetSessionManager();
            Transform gaiaTransform = GaiaUtils.GetGaiaGameObject().transform;
            GameObject newGO = new GameObject();
            newGO.name = this.name + " Biome";
            newGO.transform.parent = gaiaTransform;

            BiomeController biomeController = newGO.AddComponent<BiomeController>();

            RefreshSpawnerListEntries();

            //Track created spawners 
            List<Spawner> createdSpawners = new List<Spawner>();
            foreach (BiomeSpawnerListEntry spawnerListEntry in m_spawnerPresetList)
            {
                if (spawnerListEntry != null)
                {
                    if (spawnerListEntry.m_spawnerSettings != null)
                    {
                        createdSpawners.Add(spawnerListEntry.m_spawnerSettings.CreateSpawner(false, biomeController.transform));
                        //GaiaUtils.DisplayProgressBarNoEditor("Creating Tools", "Creating Biome " + this.name, ++currentStep / totalSteps);
                        if (ProgressBar.Show(ProgressBarPriority.CreateBiomeTools, "Creating Biome", "Creating Tools", ++currentStep, totalSteps, false, true))
                        {
                            break;
                        }
                    }
                }

            }
            if (createdSpawners.Count > 0)
            {
                biomeController.m_settings.m_range = createdSpawners[0].m_settings.m_spawnRange;
            }

            for (int i = 0; i < createdSpawners.Count; i++)
            {
                Spawner spawner = createdSpawners[i];
                biomeController.m_autoSpawners.Add(new AutoSpawner() { isActive = m_spawnerPresetList[i].m_isActiveInBiome, status = AutoSpawnerStatus.Initial, spawner = spawner });
            }
            if (autoAssignPrototypes)
            {
                //prepare resource prototype arrays once, so the same prototypes can be added to all the tiles.
                TerrainLayer[] terrainLayers = new TerrainLayer[0];
                DetailPrototype[] terrainDetails = new DetailPrototype[0];
                TreePrototype[] terrainTrees = new TreePrototype[0];
                GaiaDefaults.GetPrototypes(m_spawnerPresetList.Where(x => x.m_autoAssignPrototypes == true).ToList(), ref terrainLayers, ref terrainDetails, ref terrainTrees, Terrain.activeTerrain);

                foreach (Terrain t in Terrain.activeTerrains)
                {
                    GaiaDefaults.ApplyPrototypesToTerrain(t, terrainLayers, terrainDetails, terrainTrees);
                }
            }

            if (GaiaUtils.CheckIfSceneProfileExists())
            {
                GaiaGlobal.Instance.SceneProfile.CullingProfile = GaiaSceneCullingProfile;
            }


#if UNITY_POST_PROCESSING_STACK_V2
            biomeController.m_postProcessProfile = postProcessProfile;
#endif
            ProgressBar.Clear(ProgressBarPriority.CreateBiomeTools);

            return biomeController;
        }
    }
}