using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
    /// <summary>
    /// This class only exists to change the displayed name from "Spawner" to "World Biome Masks" on the component.
    /// </summary>
    [CustomEditor(typeof(WorldBiomeMasks))]
    public class WorldBiomeMasksEditor : SpawnerEditor
    {
    }
}
