using Gaia;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace ProceduralWorlds.HierachySystem
{
    [System.Serializable]
    public class GaiaHierarchyVisibility 
    {
        public string m_name;
        public bool m_isVisible;
    }

    [ExecuteAlways]
    public class GaiaHierarchyUtils : MonoBehaviour
    {
        //All objects in the parent transforms
        [HideInInspector]
        public List<GaiaHierarchyVisibility> m_visibilityEntries = new List<GaiaHierarchyVisibility>();



        /// <summary>
        /// Iniatilize on enable
        /// </summary>
        private void OnEnable()
        {
            SetupHideInHierarchy();
        }

        /// <summary>
        /// Configures the objects showed in Hierarchy to be shown or hidden
        /// </summary>
        public void SetupHideInHierarchy()
        {
            //Get all objects
            UpdateParentObjects();

            //Proceed if objects exist
            if (m_visibilityEntries != null)
            {
                foreach (Transform t in transform)
                {
                    GaiaHierarchyVisibility ghv = m_visibilityEntries.Find(x => x.m_name == t.name);
                    if (ghv == null || ghv.m_isVisible)
                    {
                        foreach (Transform child in t)
                        {
                            child.gameObject.hideFlags = HideFlags.None;
                        }
                    }
                    else
                    {
                        foreach (Transform child in t)
                        {
                            child.gameObject.hideFlags = HideFlags.HideInHierarchy;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Configures all GaiaHierarchyUtils components and in each of the objects showed in Hierarchy to be shown or hidden
        /// </summary>
        public void SetupAllHideInHierarchy()
        {
            bool localOnly = false;

            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                #if UNITY_EDITOR
                if (EditorUtility.DisplayDialog("Apply to all terrains?", "Do you want to apply these settings to ALL terrains (inlcuding the ones which are currently not loaded), or the loaded ones only?", "All terrains", "Only loaded terrains"))
                {
                    GaiaUtils.CallFunctionOnDynamicLoadedTerrains(SetupAllHideOnTerrain, true, null, "Applying hide settings to terrain scenes...");
                }
                else
                {
                    localOnly = true;
                }
                #endif
            }
            else
            {
                localOnly = true;
            }
            if (localOnly)
            {
                //Get all GaiaHierarchyUtils
                GaiaHierarchyUtils[] hierarchyUtils = FindObjectsOfType<GaiaHierarchyUtils>();

                //Proceed if objects exist
                if (hierarchyUtils != null)
                {
                    foreach (GaiaHierarchyUtils utils in hierarchyUtils)
                    {
                        utils.m_visibilityEntries = m_visibilityEntries;
                        utils.SetupHideInHierarchy();
                    }
                }
            }
            
        }

        public void SetupAllHideOnTerrain(Terrain t)
        {
            GaiaHierarchyUtils hierarchyUtils = t.GetComponentInChildren<GaiaHierarchyUtils>();
            if (hierarchyUtils != null)
            {
                hierarchyUtils.m_visibilityEntries = m_visibilityEntries;
                hierarchyUtils.SetupHideInHierarchy();
            }
        }


        /// <summary>
        /// Update the visibility state for all parent objects that hold game objects 
        /// </summary>
        /// <returns></returns>
        public void UpdateParentObjects()
        {
            //Build up a new list that keeps the existing settings, this will take care of sorting if the hierarchy has changed as well.
            List<GaiaHierarchyVisibility> newList = new List<GaiaHierarchyVisibility>();

            foreach (Transform t in transform)
            {

                GaiaHierarchyVisibility ghv = m_visibilityEntries.Find(x => x.m_name == t.name);
                if (ghv == null || ghv.m_isVisible)
                {
                    newList.Add(new GaiaHierarchyVisibility() { m_name = t.name, m_isVisible = true });
                }
                else
                {
                    newList.Add(new GaiaHierarchyVisibility() { m_name = t.name, m_isVisible = false });
                }
            }
            //Add all remaining entries to carry over settings for objects which are currently not spawned in the hierarchy
            foreach (GaiaHierarchyVisibility ghv in m_visibilityEntries.Where(x => !newList.Exists(y=>y.m_name==x.m_name)))
            {
                newList.Add(ghv);                            
            }

            m_visibilityEntries = newList;

        }

        /// <summary>
        /// Removes old entries from the visibility settings list that do not have a corresponding game object anymore.
        /// </summary>
        public void RemoveOldEntries()
        {
            for (int i=m_visibilityEntries.Count-1;i>=0;i--)
            {
                if (transform.Find(m_visibilityEntries[i].m_name) == null)
                {
                    m_visibilityEntries.RemoveAt(i);
                }
            }
        }
    }
}