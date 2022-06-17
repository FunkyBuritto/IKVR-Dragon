#if UNITY_EDITOR
using System.Collections.Generic;

namespace Gaia
{
    /// <summary>
    /// A FractalType implementation for a fractal that does nothing. This will give you raw
    /// noise values from the "first" fractal (from Fractal Brownian Motion, for instance) when used
    /// </summary>
    public class NoneFractalType : FractalType<NoneFractalType>
    {
        public override FractalTypeDescriptor GetDescription() => new FractalTypeDescriptor()
        {
            name = "None",
            templatePath = "Assets/Procedural Worlds/Gaia/Shaders/Unity/Terrain Tools/NoiseLib/Templates/FractalNone.noisehlsltemplate",
            supportedDimensions = NoiseDimensionFlags._1D | NoiseDimensionFlags._2D | NoiseDimensionFlags._3D,
            inputStructDefinition = null,
            additionalIncludePaths = new List<string>()
            {
                "Assets/Procedural Worlds/Gaia/Shaders/Unity/Terrain Tools/NoiseLib/NoiseCommon.hlsl"
            }
        };
    }
}
#endif