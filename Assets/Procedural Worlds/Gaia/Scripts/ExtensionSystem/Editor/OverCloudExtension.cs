#if GAIA_2_PRESENT && UNITY_EDITOR
using Gaia.Pipeline;
using UnityEngine;
using UnityEditor;
#if OverCloud_Present
using OC;
#endif
using System.IO;

namespace Gaia.GX.FelixWestin
{
    [InitializeOnLoad]
    public static class OverCloudSetup
    {
        static OverCloudSetup()
        {
            string[] filePaths = Directory.GetDirectories(Application.dataPath);
            if (filePaths.Length > 0)
            {
                foreach (var filePath in filePaths)
                {
                    bool updateScripting = false;
                    var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                    if (filePath.Contains("OverCloud"))
                    {
                        if (!symbols.Contains("OverCloud_Present"))
                        {
                            updateScripting = true;
                            symbols += ";" + "OverCloud_Present";
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

    //<summary>
    //Overcloud setup for Gaia
    //</summary>
#if OverCloud_Present
    public class OverCloudExtension
    {
        private const string m_overCloudAbout = "OverCloud provides a complete, scalable solution for rendering a realistic, volumetric sky from any camera position and angle above the horizon. It is perfect for flight simulators, huge landscapes, or any developer looking for a simple-to-use, dynamic sky. ";
        private enum OverCloudQuality { High, Medium, Low }

        #region Generic Informational Methods

        /// <summary>
        /// Returns the publisher name if provided. 
        /// This will override the publisher name in the namespace ie Gaia.GX.PublisherName
        /// </summary>
        /// <returns>Publisher name</returns>
        public static string GetPublisherName()
        {
            return "Felix Westin";
        }

        /// <summary>
        /// Returns the package name if provided
        /// This will override the package name in the class name ie public class PackageName.
        /// </summary>
        /// <returns>Package name</returns>
        public static string GetPackageName()
        {
            return "OverCloud";
        }

        #endregion

        #region GX Functions

        //<summary>
        //Gives some info on OverCloud
        //</summary>
        public static void GX_About()
        {
            EditorUtility.DisplayDialog("About OverCloud", m_overCloudAbout, "OK");
        }
        public static void GX_AddOverCloud()
        {
            AddOverCloud();
        }
        public static void GX_SetHighQuality()
        {
            SetOverCloudQuality(OverCloudQuality.High);
        }
        public static void GX_SetMediumQuality()
        {
            SetOverCloudQuality(OverCloudQuality.Medium);
        }
        public static void GX_SetLowQuality()
        {
            SetOverCloudQuality(OverCloudQuality.Low);
        }
        public static void GX_RemoveOverCloud()
        {
            RemoveOverCloud();
        }

        #endregion

        #region Utils

        private static void AddOverCloud()
        {
            GameObject overCloudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GaiaUtils.GetAssetPath("OverCloud.prefab"));
            if (overCloudPrefab != null)
            {
                Light sunLight = GaiaUtils.GetMainDirectionalLight();
                if (sunLight != null)
                {
                    GameObject.DestroyImmediate(sunLight.gameObject);
                }

                PrefabUtility.InstantiatePrefab(overCloudPrefab, GetOrCreateParentObject(GaiaConstants.gaiaLightingObject, true).transform);

                Camera mainCamera = GaiaUtils.GetCamera();
                if (mainCamera != null)
                {
                    OverCloudCamera ocCamera = mainCamera.gameObject.GetComponent<OverCloudCamera>();
                    if (ocCamera == null)
                    {
                        ocCamera = mainCamera.gameObject.AddComponent<OverCloudCamera>();
                    }
                }

                GaiaUtils.SetCustomLightSystem(overCloudPrefab);
            }
            else
            {
                Debug.LogError("OverCloud Prefab was not found");
            }
        }
        private static void SetOverCloudQuality(OverCloudQuality quality)
        {
            Camera mainCamera = GaiaUtils.GetCamera();
            if (mainCamera != null)
            {
                OverCloudCamera ocCamera = mainCamera.GetComponent<OverCloudCamera>();
                if (ocCamera != null)
                {
                    switch (quality)
                    {
                        case OverCloudQuality.High:
                            ocCamera.downsample2DClouds = false;
                            ocCamera.downsampleFactor = DownSampleFactor.Full;
                            ocCamera.highQualityClouds = true;
                            ocCamera.renderAtmosphere = true;
                            ocCamera.renderScatteringMask = true;
                            ocCamera.renderVolumetricClouds = true;
                            ocCamera.scatteringMaskSamples = SampleCount.High;
                            ocCamera.lightSampleCount = SampleCount.High;
                            break;
                        case OverCloudQuality.Medium:
                            ocCamera.downsample2DClouds = true;
                            ocCamera.downsampleFactor = DownSampleFactor.Half;
                            ocCamera.highQualityClouds = true;
                            ocCamera.renderAtmosphere = true;
                            ocCamera.renderScatteringMask = true;
                            ocCamera.renderVolumetricClouds = true;
                            ocCamera.scatteringMaskSamples = SampleCount.Normal;
                            ocCamera.lightSampleCount = SampleCount.Normal;
                            break;
                        case OverCloudQuality.Low:
                            ocCamera.downsample2DClouds = true;
                            ocCamera.downsampleFactor = DownSampleFactor.Eight;
                            ocCamera.highQualityClouds = false;
                            ocCamera.renderAtmosphere = true;
                            ocCamera.renderScatteringMask = false;
                            ocCamera.renderVolumetricClouds = false;
                            ocCamera.scatteringMaskSamples = SampleCount.Low;
                            ocCamera.lightSampleCount = SampleCount.Low;
                            break;
                    }
                }
            }
        }
        private static void RemoveOverCloud()
        {
            GameObject overCloudPrefab = GameObject.Find("OverCloud");
            if (overCloudPrefab != null)
            {
                GameObject.DestroyImmediate(overCloudPrefab);
            }

            Camera mainCamera = GaiaUtils.GetCamera();
            if (mainCamera != null)
            {
                OverCloudCamera ocCamera = mainCamera.gameObject.GetComponent<OverCloudCamera>();
                if (ocCamera != null)
                {
                    GameObject.DestroyImmediate(ocCamera);
                }
            }

            if (GaiaUtils.CheckIfSceneProfileExists())
            {
                GaiaGlobal.Instance.SceneProfile.m_waterSystemMode = GaiaConstants.GlobalSystemMode.Gaia;
                GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
                if (gaiaSettings != null)
                {
                    UnityPipelineProfile pipelineProfile = gaiaSettings.m_pipelineProfile;
                    if (pipelineProfile != null)
                    {
                        GaiaLighting.GetProfile(GaiaGlobal.Instance.SceneProfile, pipelineProfile, GaiaUtils.GetActivePipeline());
                    }
                }
            }
        }

        /// <summary>
        /// Get or create a parent object
        /// </summary>
        /// <param name="parentGameObject"></param>
        /// <param name="parentToGaia"></param>
        /// <returns>Parent Object</returns>
        public static GameObject GetOrCreateParentObject(string parentGameObject, bool parentToGaia)
        {
            //Get the parent object
            GameObject theParentGo = GameObject.Find(parentGameObject);

            if (theParentGo == null)
            {
                theParentGo = GameObject.Find(GaiaConstants.gaiaLightingObject);

                if (theParentGo == null)
                {
                    theParentGo = new GameObject(GaiaConstants.gaiaLightingObject);
                }
            }

            if (theParentGo.GetComponent<GaiaSceneLighting>() == null)
            {
                theParentGo.AddComponent<GaiaSceneLighting>();
            }

            if (parentToGaia)
            {
                GameObject gaiaParent = GaiaUtils.GetRuntimeSceneObject();
                if (gaiaParent != null)
                {
                    theParentGo.transform.SetParent(gaiaParent.transform);
                }
            }

            return theParentGo;
        }

        #endregion
    }
#endif
}
#endif