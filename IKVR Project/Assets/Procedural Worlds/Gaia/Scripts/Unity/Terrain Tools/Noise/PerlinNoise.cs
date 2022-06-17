#if UNITY_EDITOR
using System.Collections.Generic;

namespace Gaia
{
    /// <summary>
    /// A NoiseType implementation for Perlin noise
    /// </summary>
    [System.Serializable]
    public class PerlinNoise : NoiseType<PerlinNoise>
    {
        private static NoiseTypeDescriptor desc = new NoiseTypeDescriptor()
        {
            name = "Perlin",
            outputDir = "Assets/Procedural Worlds/Gaia/Shaders/Unity/Terrain Tools/NoiseLib/",
            sourcePath = "Assets/Procedural Worlds/Gaia/Shaders/Unity/Terrain Tools/NoiseLib/Implementation/PerlinImpl.hlsl",
            supportedDimensions = NoiseDimensionFlags._1D | NoiseDimensionFlags._2D | NoiseDimensionFlags._3D,
            inputStructDefinition = null
        };

        public override NoiseTypeDescriptor GetDescription() => desc;
    }
}
#endif