using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Gaia.GX.ProceduralWorlds
{
    public class GaiaWaterGX
    {
        #region Private values

        private static string m_unityVersion;
        private static List<string> m_profileList = new List<string>();
        private static List<Material> m_allMaterials = new List<Material>();


        #endregion

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
            return "Water";
        }

        #endregion

        #region Methods exposed by Gaia as buttons must be prefixed with GX_

        /// <summary>
        /// Adds water system to the scene
        /// </summary>
        public static void GX_WaterSetup_AddWater()
        {
            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
            GaiaWaterProfile waterProfile = AssetDatabase.LoadAssetAtPath<GaiaWaterProfile>(GetAssetPath("Gaia Water System Profile"));
            Material material = GaiaWater.GetGaiaOceanMaterial();
            if (material != null)
            {
                GaiaUtils.GetRuntimeSceneObject();
                if (GaiaGlobal.Instance != null)
                {
                    GaiaWater.GetProfile(0, material, GaiaGlobal.Instance.SceneProfile, true, false);
                }
            }
            else
            {
                Debug.Log("Material could not be found");
            }
        }

        /// <summary>
        /// Removes water system from the scene
        /// </summary>
        public static void GX_WaterSetup_RemoveWater()
        {
            GaiaUtils.RemoveWaterSystems();
        }

        public static void GX_WaterSetup_SelectLightingAndWaterProfile()
        {
            GaiaLighting.OpenProfileSelection();
        }

        public static void GX_WaterSetup_SetupWaterProfiles()
        {
            if (EditorUtility.DisplayDialog("Opening Water Profile", "This will open the water profile settings. In there you will be able to adjust water settings for Gaia, and you can also edit the default water shader parameters for the different water types.", "OK", "Cancel"))
            {
                GaiaUtils.FocusWaterProfile();
            }
        }

        //public static void GX_WaterStyles_OceanTropical()
        //{
        //    GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
        //    GaiaWaterProfile waterProfile = AssetDatabase.LoadAssetAtPath<GaiaWaterProfile>(GetAssetPath("Gaia Water System Profile"));
        //    if (GaiaWater.SetupMaterials(gaiaSettings.m_currentRenderer, gaiaSettings, 10))
        //    {
        //        GaiaWater.GetProfile(GaiaConstants.GaiaWaterProfileType.StandardClearLake, waterProfile, gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, true);
        //    }
        //    else
        //    {
        //        Debug.Log("Materials could not be found");
        //    }
        //}




        /// <summary>
        /// Sets water style to deep blue
        /// </summary>
        //public static void GX_WaterStyles_OceanTropicalAngry()
        //{
        //    GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
        //    GaiaWaterProfile waterProfile = AssetDatabase.LoadAssetAtPath<GaiaWaterProfile>(GetAssetPath("Gaia Water System Profile"));

        //    if (GaiaWater.SetupMaterials(gaiaSettings.m_currentRenderer, gaiaSettings, 6))
        //    {
        //        GaiaWater.GetProfile(GaiaConstants.GaiaWaterProfileType.DeepBlueOcean, waterProfile, gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, true);
        //    }
        //    else
        //    {
        //        Debug.Log("Materials could not be found");
        //    }
        //}

        ///// <summary>
        ///// Sets water style to deep blue
        ///// </summary>
        //public static void GX_WaterStyles_OceanTropicalBlue()
        //{
        //    GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
        //    GaiaWaterProfile waterProfile = AssetDatabase.LoadAssetAtPath<GaiaWaterProfile>(GetAssetPath("Gaia Water System Profile"));

        //    if (GaiaWater.SetupMaterials(gaiaSettings.m_currentRenderer, gaiaSettings, 7))
        //    {
        //        GaiaWater.GetProfile(GaiaConstants.GaiaWaterProfileType.ClearBlueOcean, waterProfile, gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, true);
        //    }
        //    else
        //    {
        //        Debug.Log("Materials could not be found");
        //    }
        //}

        ///// <summary>
        ///// Sets water style to deep blue
        ///// </summary>
        //public static void GX_WaterStyles_OceanTropicalFlat()
        //{
        //    GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
        //    GaiaWaterProfile waterProfile = AssetDatabase.LoadAssetAtPath<GaiaWaterProfile>(GetAssetPath("Gaia Water System Profile"));
        //    if (GaiaWater.SetupMaterials(gaiaSettings.m_currentRenderer, gaiaSettings, 8))
        //    {
        //        GaiaWater.GetProfile(GaiaConstants.GaiaWaterProfileType.StandardLake, waterProfile, gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, true);
        //    }
        //    else
        //    {
        //        Debug.Log("Materials could not be found");
        //    }
        //}

        ///// <summary>
        ///// Sets water style to deep blue
        ///// </summary>
        //public static void GX_WaterStyles_OceanTropicalGreen()
        //{
        //    GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
        //    GaiaWaterProfile waterProfile = AssetDatabase.LoadAssetAtPath<GaiaWaterProfile>(GetAssetPath("Gaia Water System Profile"));
        //    if(GaiaWater.SetupMaterials(gaiaSettings.m_currentRenderer, gaiaSettings, 9))
        //    {
        //        GaiaWater.GetProfile(GaiaConstants.GaiaWaterProfileType.StandardClearLake, waterProfile, gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, true);
        //    }
        //    else
        //    {
        //        Debug.Log("Materials could not be found");
        //    }
        //}

        /// <summary>
        /// Enables water reflections
        /// </summary>
        public static void GX_WaterReflections_EnableReflections()
        {
            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
            GaiaWater.SetWaterReflectionsType(true, gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, gaiaSettings.m_gaiaWaterProfile, gaiaSettings.m_gaiaWaterProfile.m_waterProfiles[gaiaSettings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex]);
        }

        /// <summary>
        /// Disables water reflections
        /// </summary>
        public static void GX_WaterReflections_DisableReflections()
        {
            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
            GaiaWater.SetWaterReflectionsType(false, gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, gaiaSettings.m_gaiaWaterProfile, gaiaSettings.m_gaiaWaterProfile.m_waterProfiles[gaiaSettings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex]);
        }      

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

      

        /// <summary>
        /// Removes Suffix in file formats required
        /// </summary>
        /// <param name="path"></param>
        private static List<Material> GetMaterials(string path)
        {
            List<Material> materials = new List<Material>();

            DirectoryInfo dirInfo = new DirectoryInfo(path);
            var files = dirInfo.GetFiles();
            foreach (FileInfo file in files)
            {
                if (file.Extension.EndsWith("mat"))
                {
                    materials.Add(AssetDatabase.LoadAssetAtPath<Material>(GaiaUtils.GetAssetPath(file.Name)));
                }
            }

            m_allMaterials = materials;

            return materials;
        }

        #endregion
    }
}