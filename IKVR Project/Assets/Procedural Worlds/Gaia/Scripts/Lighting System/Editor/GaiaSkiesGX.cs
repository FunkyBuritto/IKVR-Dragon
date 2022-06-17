using UnityEditor;

namespace Gaia.GX.ProceduralWorlds
{
    public class GaiaSkiesGX
    {
        #region Generic informational methods

        /// <summary>
        /// Returns the publisher name if provided. 
        /// This will override the publisher name in the namespace ie Gaia.GX.PublisherName
        /// </summary>
        /// <returns>Publisher name</returns>
        public static string GetPublisherName()
        {
            return "Procedural Worlds";
        }

        /// <summary>
        /// Returns the package name if provided
        /// This will override the package name in the class name ie public class PackageName.
        /// </summary>
        /// <returns>Package name</returns>
        public static string GetPackageName()
        {
            return "Skies";
        }

        #endregion

        #region Methods exposed by Gaia as buttons must be prefixed with GX_

#if AMBIENT_SKIES

        public static void GX_OpenAmbientSkies()
        {
            //Ambient Skies Editor Window
            var mainWindow = EditorWindow.GetWindow<AmbientSkies.AmbientSkiesEditorWindow>(false, "Ambient Skies");
            //Show window
            mainWindow.Show();
        }

#endif

        /// <summary>
        /// Does a quick optimize to your scene/project settings
        /// </summary>
        public static void GX_Optimization_QuickOptimize()
        {
            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
            if (gaiaSettings != null)
            {
                GaiaLighting.QuickOptimize(gaiaSettings);
            }
        }

#if !AMBIENT_SKIES
        /// <summary>
        /// Sets the time of day to morning
        /// </summary>
        //public static void GX_TimeOfDay_Morning()
        //{
        //    GaiaLightingProfile lightingProfile = AssetDatabase.LoadAssetAtPath<GaiaLightingProfile>(GetAssetPath("Gaia Lighting System Profile"));
        //    GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();

        //    GaiaLighting.GetProfile(GaiaConstants.GaiaLightingProfileType.Morning, lightingProfile, gaiaSettings.m_pipelineProfile, gaiaSettings.m_currentRenderer);
        //}

        ///// <summary>
        ///// Sets the time of day to day
        ///// </summary>
        //public static void GX_TimeOfDay_Day()
        //{
        //    GaiaLightingProfile lightingProfile = AssetDatabase.LoadAssetAtPath<GaiaLightingProfile>(GetAssetPath("Gaia Lighting System Profile"));
        //    GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();

        //    GaiaLighting.GetProfile(GaiaConstants.GaiaLightingProfileType.Day, lightingProfile, gaiaSettings.m_pipelineProfile, gaiaSettings.m_currentRenderer);
        //}

        ///// <summary>
        ///// Sets the time of day to evening
        ///// </summary>
        //public static void GX_TimeOfDay_Evening()
        //{
        //    GaiaLightingProfile lightingProfile = AssetDatabase.LoadAssetAtPath<GaiaLightingProfile>(GetAssetPath("Gaia Lighting System Profile"));
        //    GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();

        //    GaiaLighting.GetProfile(GaiaConstants.GaiaLightingProfileType.Evening, lightingProfile, gaiaSettings.m_pipelineProfile, gaiaSettings.m_currentRenderer);
        //}

        ///// <summary>
        ///// Sets the time of day to night
        ///// </summary>
        //public static void GX_TimeOfDay_Night()
        //{
        //    GaiaLightingProfile lightingProfile = AssetDatabase.LoadAssetAtPath<GaiaLightingProfile>(GetAssetPath("Gaia Lighting System Profile"));
        //    GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();

        //    GaiaLighting.GetProfile(GaiaConstants.GaiaLightingProfileType.Night, lightingProfile, gaiaSettings.m_pipelineProfile, gaiaSettings.m_currentRenderer);
        //}

        ///// <summary>
        ///// Sets the time of day to default procedural
        ///// </summary>
        //public static void GX_TimeOfDay_DefaultProcedural()
        //{
        //    GaiaLightingProfile lightingProfile = AssetDatabase.LoadAssetAtPath<GaiaLightingProfile>(GetAssetPath("Gaia Lighting System Profile"));
        //    GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();

        //    GaiaLighting.GetProfile(GaiaConstants.GaiaLightingProfileType.Default, lightingProfile, gaiaSettings.m_pipelineProfile, gaiaSettings.m_currentRenderer);
        //}

        /// <summary>
        /// Removes gaia lighting from the scene
        /// </summary>
        /// 

        public static void GX_TimeOfDay_SelectLightingAndWaterProfile()
        {
            GaiaLighting.OpenProfileSelection();
        }

        public static void GX_TimeOfDay_SetupLightingProfiles()
        {
            if (EditorUtility.DisplayDialog("Opening Lighting Profile", "This will open the lighting profile settings. In there you will be able to adjust lighting settings for Gaia, and you can also edit the default lighting parameters for 'Day', 'Nighttime', etc.", "OK", "Cancel"))
            {
                GaiaUtils.FocusLightingProfile();
            }
        }

        public static void GX_TimeOfDay_RemoveGaiaLighting()
        {
            GaiaLighting.RemoveGaiaLighting();
        }

#endif

        /// <summary>
        /// Adds occlusion culling volume
        /// </summary>
        public static void GX_OcclusionCulling_AddOcclusionCullingVolume()
        {
            GaiaLighting.AddOcclusionCulling(true, false, false);
        }

        /// <summary>
        /// Removes occlusion volume
        /// </summary>
        public static void GX_OcclusionCulling_RemoveOcclusionCullingVolume()
        {
            GaiaLighting.RemoveOcclusionCulling(false, false, false);
        }

        /// <summary>
        /// Bakes and adds occlusion volume
        /// </summary>
        public static void GX_OcclusionCulling_BakeOcclusionCulling()
        {
            GaiaLighting.BakeOcclusionCulling(true, true, false);
        }

        /// <summary>
        /// Cancels occlusion bake
        /// </summary>
        public static void GX_OcclusionCulling_CancelOcclusionCulling()
        {
            GaiaLighting.CancelOcclusionCulling(true, false, false);
        }

        /// <summary>
        /// Clears occlusion data
        /// </summary>
        public static void GX_OcclusionCulling_ClearOcclusionCulling()
        {
            GaiaLighting.ClearOcclusionCulling(true, false, true);
        }

        #if !AMBIENT_SKIES
        /// <summary>
        /// Bakes lightmapping
        /// </summary>
        public static void GX_Lightmaps_BakeLighting()
        {
            GaiaSettings settings = GaiaUtils.GetGaiaSettings();
            if (settings != null)
            {
                if (GaiaUtils.CheckIfSceneProfileExists())
                {
                    GaiaLighting.BakeLighting(GaiaGlobal.Instance.SceneProfile.m_lightingBakeMode, GaiaGlobal.Instance.SceneProfile.m_lightingProfiles[GaiaGlobal.Instance.SceneProfile.m_selectedLightingProfileValuesIndex]);
                }
            }
        }

        /// <summary>
        /// Cancels lightmapping
        /// </summary>
        public static void GX_Lightmaps_CancelLightmapBaking()
        {
            GaiaLighting.CancelLightmapBaking();
        }

        /// <summary>
        /// Clear baked lightmapping
        /// </summary>
        public static void GX_Lightmaps_ClearBakedLightmaps()
        {
            GaiaLighting.ClearBakedLightmaps();
        }

        /// <summary>
        /// Clear lightmapping data on disk
        /// </summary>
        public static void GX_Lightmaps_ClearBakedLightmapDataOnDisk()
        {
            GaiaLighting.ClearLightmapDataOnDisk();
        }
#endif

        #endregion

        #region Utils

        /// <summary>
        /// Get the asset path of the first thing that matches the name
        /// </summary>
        /// <param name="name">Name to search for</param>
        /// <returns></returns>
        private static string GetAssetPath(string name)
        {
            string[] assets = AssetDatabase.FindAssets(name, null);
            if (assets.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(assets[0]);
            }
            return null;
        }

        #endregion
    }
}