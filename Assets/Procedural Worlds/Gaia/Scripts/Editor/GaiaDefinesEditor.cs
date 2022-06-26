using UnityEditor;
using UnityEngine.Rendering;

namespace Gaia
{
    /// <summary>
    /// Injects GAIA_PRESENT define into project
    /// </summary>
    [InitializeOnLoad]
    public class GaiaDefinesEditor : Editor
    {
        static GaiaDefinesEditor()
        {
            //Make sure we inject GAIA_2_PRESENT
            bool updateScripting = false;
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            if (symbols.Contains("GAIA_PRESENT"))
            {
                updateScripting = true;
                symbols = symbols.Replace("GAIA_PRESENT", "GAIA_2_PRESENT");
            }

            if (!symbols.Contains("GAIA_2_PRESENT"))
            {
                updateScripting = true;
                symbols += ";" + "GAIA_2_PRESENT";
            }

            if (GaiaProUtils.GaiaProDefineCheck())
            {
                if (!symbols.Contains("GAIA_PRO_PRESENT"))
                {
                    updateScripting = true;
                    symbols += ";GAIA_PRO_PRESENT";
                }
            }

            if (updateScripting)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols);
            }
        }

        /// <summary>
        /// Check if linear light intensity setup is needed
        /// </summary>
        private static void CheckLinearSetup()
        {
            if (PlayerSettings.colorSpace == UnityEngine.ColorSpace.Linear)
            {
                GraphicsSettings.lightsUseLinearIntensity = true;
            }
            else
            {
                GraphicsSettings.lightsUseLinearIntensity = false;
            }
        }
    }

    /// <summary>
    /// Pro utils to check for gaia pro
    /// </summary>
    public static class GaiaProUtils
    {
        #region Gaia Pro

        /// <summary>
        /// Checks if gaia pro exists
        /// </summary>
        /// <returns></returns>
        public static bool IsGaiaPro()
        {
            bool isPro = false;

            isPro = GaiaDirectories.GetGaiaProDirectory();

#if GAIA_PRO_PRESENT
            isPro = true;
#else
            isPro = false;
#endif

            return isPro;
        }

        /// <summary>
        /// Checks if gaia pro exists
        /// </summary>
        /// <returns></returns>
        public static bool GaiaProDefineCheck()
        {
            bool isPro = false;

            isPro = GaiaDirectories.GetGaiaProDirectory();

            return isPro;
        }

        #endregion
    }
}