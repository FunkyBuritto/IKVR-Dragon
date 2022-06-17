#if UNITY_EDITOR
using System.Collections.Generic;

namespace Gaia
{
    /// <summary>
    /// A NoiseType implementation for Value noise
    /// </summary>
    [System.Serializable]
    public class ValueNoise : NoiseType<ValueNoise>
    {
        private static NoiseTypeDescriptor desc = new NoiseTypeDescriptor()
        {
            name = "Value",
            outputDir = "Assets/Procedural Worlds/Gaia/Shaders/Unity/Terrain Tools/NoiseLib/",
            sourcePath = "Assets/Procedural Worlds/Gaia/Shaders/Unity/Terrain Tools/NoiseLib/Implementation/ValueImpl.hlsl",
            supportedDimensions = NoiseDimensionFlags._1D | NoiseDimensionFlags._2D | NoiseDimensionFlags._3D,
            inputStructDefinition = null
        };

        public override NoiseTypeDescriptor GetDescription() => desc;
    }
}
#endif