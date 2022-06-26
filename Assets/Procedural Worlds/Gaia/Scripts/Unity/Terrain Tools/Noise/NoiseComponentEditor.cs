#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.TerrainAPI;

namespace Gaia
{
    [CustomEditor(typeof(NoiseComponent))]
    public class NoiseComponentEditor : Editor
    {

    }
}
#endif