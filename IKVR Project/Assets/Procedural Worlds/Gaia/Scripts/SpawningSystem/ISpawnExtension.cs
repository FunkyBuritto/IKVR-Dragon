using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gaia
{
    /// <summary>
    /// Use this interface to implement a Gaia Spawn extension that can be run by a spawner to execute your own code whenever the spawner finds a fit location.
    /// </summary>
    public interface ISpawnExtension
    {
        /// <summary>
        /// The name to identify the Extension
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Whether or not this extension impacts Terrain heights.
        /// If it does, Gaia will trigger the necessary terrain updates after running this extension so things like the terrain collider are updated according to your changes.
        /// </summary>
        bool AffectsHeights { get; }

        /// <summary>
        /// Whether or not this extension impacts Terrain textures.
        /// If it does, Gaia will trigger the necessary splatmap updates after running this extension 
        /// /// </summary>
        bool AffectsTextures { get; }

        /// <summary>
        /// Initialise the extension - To avoid some actions to happen at every instance of this 
        /// Extension spawning. All the spawner settings from the UI can be accessed via spawner.m_settings.
        /// Resource Information is in spawner.m_settings.m_resources
        /// </summary>
        /// <param name="spawner">The Gaia spawner that is running this extension.</param>
        void Init(Spawner spawner);

        /// <summary>
        /// This is the core function that will be called when spawning. 
        /// </summary>
        /// <param name="spawner">The Gaia spawner that is spawning this.  All the spawner settings from the UI can be accessed via spawner.m_settings.</param>
        /// <param name="target">The target transform for Game Object spawns</param>
        /// <param name="ruleIndex">The index of the spawn rule this extension is running in.</param>
        /// <param name="instanceIndex">The index of the instance inside the rule.</param>
        /// <param name="spawnInfo">Class containing details of the spawn.</param>
        void Spawn(Spawner spawner, Transform target, int ruleIndex, int instanceIndex, SpawnExtensionInfo spawnInfo);

        /// <summary>
        /// This is the function that the spawner calls when trying to remove the content produced by this spawn extension.
        /// </summary>
        void Delete();
    
        /// <summary>
        /// This will be called when the Spawn Extension is finished spawning. Do all cleanup operations in here.
        /// </summary>
        void Close();
    }

}
