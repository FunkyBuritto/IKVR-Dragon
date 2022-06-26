#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
#if MEWLIST_MASSIVE_CLOUDS
using Mewlist;
#endif
using UnityEditor;
using UnityEngine;

namespace Gaia.GX.ProceduralWorlds
{
    public class MassiveCloudGX
    {
#if MEWLIST_MASSIVE_CLOUDS
        #region Generic informational methods

        /// <summary>
        /// Returns the publisher name if provided. 
        /// This will override the publisher name in the namespace ie Gaia.GX.PublisherName
        /// </summary>
        /// <returns>Publisher name</returns>
        public static string GetPublisherName()
        {
            return "Mewlist";
        }

        /// <summary>
        /// Returns the package name if provided
        /// This will override the package name in the class name ie public class PackageName.
        /// </summary>
        /// <returns>Package name</returns>
        public static string GetPackageName()
        {
            return "Massive Clouds - Screen Space Volumetric Clouds";
        }

        #endregion

        #region Methods exposed by Gaia as buttons must be prefixed with GX_

        /// <summary>
        /// Adds water system to the scene
        /// </summary>
        public static void GX_AddClouds()
        {
            AddClouds();
        }

        /// <summary>
        /// Removes water system from the scene
        /// </summary>
        public static void GX_RemoveClouds()
        {
            RemoveClouds();
        }

        #endregion

        #region Utils

        private static void AddClouds()
        {
            Camera mainCamera = GaiaUtils.GetCamera();
            if (mainCamera != null)
            {
                MassiveClouds clouds = mainCamera.GetComponent<MassiveClouds>();
                if (clouds == null)
                {
                    clouds = mainCamera.gameObject.AddComponent<MassiveClouds>();
                }

                List<MassiveCloudsProfile> profiles = new List<MassiveCloudsProfile>();
                MassiveCloudsProfile profile = AssetDatabase.LoadAssetAtPath<MassiveCloudsProfile>(GaiaUtils.GetAssetPath("Cloudy C.asset"));
                List<MassiveCloudsParameter> parameters = new List<MassiveCloudsParameter>();
                if (profile != null)
                {
                    profiles.Add(profile);
                    parameters.Add(profile.Parameter);
                    clouds.SetProfiles(profiles);
                    clouds.SetParameters(parameters);
                }

                MassiveCloudsCameraEffect cloudsEffect = mainCamera.GetComponent<MassiveCloudsCameraEffect>();
                if (cloudsEffect == null)
                {
                    cloudsEffect = mainCamera.gameObject.AddComponent<MassiveCloudsCameraEffect>();
                }
            }
        }

        private static void RemoveClouds()
        {
            Camera mainCamera = GaiaUtils.GetCamera();
            if (mainCamera != null)
            {
                MassiveCloudsCameraEffect cloudsEffect = mainCamera.GetComponent<MassiveCloudsCameraEffect>();
                if (cloudsEffect != null)
                {
                    GameObject.DestroyImmediate(cloudsEffect);
                }

                MassiveClouds clouds = mainCamera.GetComponent<MassiveClouds>();
                if (clouds != null)
                {
                    GameObject.DestroyImmediate(clouds);
                }
            }
        }

        #endregion

#endif
    }

    [InitializeOnLoad]
    public static class MewlistMassiveClouds
    {
        static MewlistMassiveClouds()
        {
            string folderName = "MassiveClouds";
            if (Directory.Exists(GaiaUtils.GetAssetPath(folderName)))
            {
                //Make sure we inject GAIA_2_PRESENT
                bool updateScripting = false;
                var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                if (!symbols.Contains("MEWLIST_MASSIVE_CLOUDS"))
                {
                    updateScripting = true;
                    if (symbols.Length < 1)
                    {
                        symbols += "MEWLIST_MASSIVE_CLOUDS";
                    }
                    else
                    {
                        symbols += ";MEWLIST_MASSIVE_CLOUDS";
                    }
                }

                if (updateScripting)
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols);
                }
            }
        }
    }
}

#endif