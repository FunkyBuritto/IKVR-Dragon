using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gaia
{
    /// <summary>
    /// Simple Spawn Extension for demo / debug purposes. Just writes some info to the console when being executed.
    /// </summary>
    public class DebugSpawnExtension : MonoBehaviour, ISpawnExtension
    {
        public string Name { get { return "DebugSpawnExtension"; } }

        public bool AffectsHeights => false;

        public bool AffectsTextures => false;

        public GameObject testPrefab;

        public void Close()
        {
            Debug.Log("Spawn Extension is closing down.");
        }

        public void Init(Spawner spawner)
        {
            Debug.Log("Spawn Extension starting up.");
        }

        public void Spawn(Spawner spawner, Transform target, int ruleIndex, int instanceIndex, SpawnExtensionInfo spawnExtensionInfo)
        {
            Debug.Log("Spawn Extension spawning.");
            if (testPrefab != null)
            {
                GameObject newGO = GameObject.Instantiate(testPrefab, spawnExtensionInfo.m_position, spawnExtensionInfo.m_rotation);
                newGO.transform.localScale = spawnExtensionInfo.m_scale;
                newGO.transform.parent = target;
            }

        }

        public void Delete()
        {
            Debug.Log("Spawn Extenison deleting.");
        }
    }
}
