#if UNITY_EDITOR
using System.Collections.Generic;

namespace Gaia
{
    /// <summary>
    /// A NoiseType implementation for Ridge noise
    /// </summary>
    [System.Serializable]
    public class RidgeNoise : NoiseType<RidgeNoise>
    {
        private static NoiseTypeDescriptor desc = new NoiseTypeDescriptor()
        {
            name = "Ridge",
            outputDir = "Assets/Procedural Worlds/Gaia/Shaders/Unity/Terrain Tools/NoiseLib/",
            sourcePath = "Assets/Procedural Worlds/Gaia/Shaders/Unity/Terrain Tools/NoiseLib/Implementation/RidgeImpl.hlsl",
            supportedDimensions = NoiseDimensionFlags._1D | NoiseDimensionFlags._2D | NoiseDimensionFlags._3D,
            inputStructDefinition = null
        };

        public override NoiseTypeDescriptor GetDescription() => desc;
    }
}
#endif