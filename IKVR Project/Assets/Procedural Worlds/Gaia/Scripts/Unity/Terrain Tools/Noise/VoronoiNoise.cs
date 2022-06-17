#if UNITY_EDITOR
using System.Collections.Generic;

namespace Gaia
{
    /// <summary>
    /// A NoiseType implementation for Voronoi noise
    /// </summary>
    [System.Serializable]
    public class VoronoiNoise : NoiseType<VoronoiNoise>
    {
        private static NoiseTypeDescriptor desc = new NoiseTypeDescriptor()
        {
            name = "Voronoi",
            outputDir = "Assets/Procedural Worlds/Gaia/Shaders/Unity/Terrain Tools/NoiseLib/",
            sourcePath = "Assets/Procedural Worlds/Gaia/Shaders/Unity/Terrain Tools/NoiseLib/Implementation/VoronoiImpl.hlsl",
            supportedDimensions = NoiseDimensionFlags._1D | NoiseDimensionFlags._2D | NoiseDimensionFlags._3D,
            inputStructDefinition = null
        };

        public override NoiseTypeDescriptor GetDescription() => desc;
    }
}
#endif