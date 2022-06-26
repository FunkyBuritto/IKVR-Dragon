#if CTS_PRESENT

using CTS;
using System;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gaia.GX.ProceduralWorlds
{
    public class CTSGX
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
            return "CTS";
        }

#endregion

#region Methods exposed by Gaia as buttons must be prefixed with GX_

#if GAIA_PRO_PRESENT

        public static void GX_AddAlpineMeadowProfile()
        {
            AddCTSProfile("CTS_Alpine_Meadow_Profile");
        }
        public static void GX_AddConiferousForestProfile()
        {
            AddCTSProfile("CTS_Coniferous_Profile");
        }
        public static void GX_AddGiantForestProfile()
        {
            AddCTSProfile("CTS_Giant_Profile");
        }
#endif

        public static void GX_AddGaiaProSampleProfile()
        {
            AddCTSProfile("CTS_Gaia_Sample_Profile");
        }
        public static void GX_AddSyntyStudiosProfile()
        {
            AddCTSProfile("CTS_Synty_Studios_Profile");
        }

        public static void GX_BakeTerrainNormals()
        {
            if (EditorUtility.DisplayDialog("Bake Terrain Normals", "This will bake the normal maps for all Gaia Terrains in the scene (currently loaded or not). This can take a while depending on the number of your terrains.", "Bake Normals Now", "Cancel"))
            {
                if (GaiaUtils.HasDynamicLoadedTerrains())
                {
                    GaiaUtils.CallFunctionOnDynamicLoadedTerrains(BakeCTSNormalsForTerrain, true);
                }
                else
                {
                    foreach (Terrain t in Terrain.activeTerrains)
                    {
                        BakeCTSNormalsForTerrain(t);
                    }
                }
            }
        }


        public static void GX_BakeColorMaps()
        {
            if (EditorUtility.DisplayDialog("Bake Terrain Color Maps", "This will bake the color maps for all Gaia Terrains in the scene (currently loaded or not). This can take a while depending on the number of your terrains.", "Bake Color Maps Now", "Cancel"))
            {
                bool bakeGrass = false;

                if (EditorUtility.DisplayDialog("Bake Grass?", "Do you want to bake the terrain details / grass in the color map?", "Yes", "No"))
                {
                    bakeGrass = true;
                }

                if (GaiaUtils.HasDynamicLoadedTerrains())
                {
                    Action<Terrain> act = (t) => BakeCTSColorMapForTerrain(t, bakeGrass);
                    GaiaUtils.CallFunctionOnDynamicLoadedTerrains(BakeCTSNormalsForTerrain, true);
                }
                else
                {
                    foreach (Terrain t in Terrain.activeTerrains)
                    {
                        BakeCTSColorMapForTerrain(t, bakeGrass);
                    }
                }
            }
        }


        public static void GX_RemoveCTS()
        {
            if (EditorUtility.DisplayDialog("Remove CTS", "Are you sure you want to completely remove CTS from all the terrains in your scene? This will also remove CTS from terrains which are currently not loaded in this scene.", "Remove CTS", "Cancel"))
            {
                if (GaiaUtils.HasDynamicLoadedTerrains())
                {
                    GaiaUtils.CallFunctionOnDynamicLoadedTerrains(RemoveCTSFromTerrain, true);
                }
                else
                {
                    foreach (Terrain t in Terrain.activeTerrains)
                    {
                        RemoveCTSFromTerrain(t);
                    }
                }
                GameObject wm = GameObject.Find("CTS Weather Manager");
                if(wm!=null)
                {
                    UnityEngine.Object.DestroyImmediate(wm);
                }
            }
        }



#region Utils

        /// <summary>
        /// Removes the CTS components from the terrain
        /// </summary>
        /// <param name="terrain">The terrain to remove the CTS components from</param>
        private static void RemoveCTSFromTerrain(Terrain terrain)
        {
            CompleteTerrainShader cts = terrain.GetComponent<CompleteTerrainShader>();
            if (cts != null)
            {
                cts.Profile = null;
                cts.ApplyMaterialAndUpdateShader();
                UnityEngine.Object.DestroyImmediate(cts);
            }
            CTSWeatherController wc = terrain.GetComponent<CTSWeatherController>();
            if (wc != null)
            {
                UnityEngine.Object.DestroyImmediate(wc);
            }
        }
        /// <summary>
        /// Bakes the normal map on a terrain with a CTS component
        /// </summary>
        /// <param name="t"></param>
        private static void BakeCTSNormalsForTerrain(Terrain t)
        {
            CompleteTerrainShader cts = t.GetComponent<CompleteTerrainShader>();
            if (cts != null)
            {
                cts.BakeTerrainNormals();
            }
        }

        /// <summary>
        /// Bakes the color map on a terrain with a CTS component
        /// </summary>
        /// <param name="t"></param>
        private static void BakeCTSColorMapForTerrain(Terrain t, bool bakeGrass)
        {
            CompleteTerrainShader cts = t.GetComponent<CompleteTerrainShader>();
            if (cts != null)
            {
                
                if (bakeGrass)
                {
                    cts.AutoBakeGrassIntoColorMap = true;
                    cts.BakeTerrainBaseMapWithGrass();
                }
                else
                {
                    cts.AutoBakeGrassIntoColorMap = false;
                    cts.BakeTerrainBaseMap();
                }
            }
        }

        /// <summary>
        /// Adds a CTS profile with a certain name to all terrains (loaded or not) and then bakes the textures for that terrain
        /// </summary>
        /// <param name="profileName"></param>
        public static void AddCTSProfile(string profileName)
        {
            bool bakeTerrainNormals = false;
            if (EditorUtility.DisplayDialog("Bake Terrain Normals?", "Do you want CTS to bake terrain normal maps while applying the profile as well?", "Yes", "No"))
            {
                bakeTerrainNormals = true;
            }


            CTSProfile ctsProfile = (CTSProfile)AssetDatabase.LoadAssetAtPath(GetCTSProfilePath(profileName), typeof(CTSProfile));
            if (ctsProfile != null)
            {
                if (GaiaUtils.HasDynamicLoadedTerrains())
                {
                    Action<Terrain> act = (t) => ApplyCTSProfile(t, ctsProfile, bakeTerrainNormals);
                    GaiaUtils.CallFunctionOnDynamicLoadedTerrains(act, true);
                }
                else
                {
                    foreach (Terrain t in Terrain.activeTerrains)
                    {
                        ApplyCTSProfile(t, ctsProfile, bakeTerrainNormals);
                    }
                }
                if (ctsProfile.m_needsAlbedosArrayUpdate || ctsProfile.m_needsNormalsArrayUpdate)
                {
                    CTSTerrainManager.Instance.BroadcastShaderSetup(ctsProfile);
                }
            }   
        }

        /// <summary>
        /// Get the asset path of the first cts profile that matches the exact name
        /// </summary>
        /// <param name="name">Name to search for</param>
        /// <returns></returns>
        private static string GetCTSProfilePath(string name)
        {
            string[] assets = AssetDatabase.FindAssets(name, null);
            if (assets != null && assets.Length > 0)
            {
                foreach (string assetGUID in assets)
                {
                    if(AssetDatabase.GUIDToAssetPath(assetGUID).EndsWith(name  + ".asset"))
                    {
                        return AssetDatabase.GUIDToAssetPath(assetGUID);
                    }
                }
                
            }
            else
            {
                Debug.LogError("Could not find CTS profile " + name + "!");
            }
            return null;
        }

        private static void ApplyCTSProfile(Terrain terrain, CTSProfile ctsProfile, bool bakeTerrainNormals)
        {
            CompleteTerrainShader cts = terrain.gameObject.GetComponent<CompleteTerrainShader>();
            if (cts==null)
            {
                cts = terrain.gameObject.AddComponent<CompleteTerrainShader>();
            }
            cts.Profile = ctsProfile;
            if (bakeTerrainNormals)
            {
                cts.BakeTerrainNormals();
            }

        }

#endregion

#endregion
    }
}
#endif