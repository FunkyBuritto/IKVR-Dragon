#if UNITY_EDITOR
using UnityEngine;

namespace Gaia
{
    internal class NoiseBlitShaderGenerator : NoiseShaderGenerator<NoiseBlitShaderGenerator>
    {
        private static ShaderGeneratorDescriptor m_desc = new ShaderGeneratorDescriptor()
        {
            name = "NoiseBlit",
            shaderCategory = "Hidden/TerrainTools/Noise/NoiseBlit",
            outputDir = "Assets/Procedural Worlds/Gaia/Shaders/Unity/Terrain Tools/Generated/",
            templatePath = "Assets/Procedural Worlds/Gaia/Shaders/Unity/Terrain Tools/NoiseLib/Templates/Blit.noisehlsltemplate"
        };

        public override ShaderGeneratorDescriptor GetDescription() => m_desc;
    }
}
#endif