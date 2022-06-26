using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gaia
{
    [ExecuteAlways]
    public class GaiaScenePlayer : MonoBehaviour
    {
        private Camera m_camera;
        private float m_cameraFOV;
        private Vector3 m_FOVCenter;
        private Bounds m_worldSpaceBounds = new Bounds();
        private Plane[] m_planes = new Plane[6];
        private Terrain[] m_allTerrains = new Terrain[0];
        private MeshRenderer[] m_allTerrainMeshRenderers = new MeshRenderer[0];

        private void Start()
        {
            if (!GaiaUtils.CheckIfSceneProfileExists())
            {
                return;
            }

            m_camera = GaiaGlobal.Instance.m_mainCamera;
            m_allTerrains = Terrain.activeTerrains;

            //Collect all Mesh Terrains that are present at startup
            List<MeshRenderer> tempMeshRenderers = new List<MeshRenderer>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                foreach (GameObject go in scene.GetRootGameObjects())
                {
                    AddTerrainMeshRenderer(go, tempMeshRenderers);
                }
            }
            m_allTerrainMeshRenderers = tempMeshRenderers.ToArray();


            if (GaiaGlobal.Instance.SceneProfile.m_terrainCullingEnabled)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.sceneUnloaded -= OnSceneUnLoaded;
                SceneManager.sceneUnloaded += OnSceneUnLoaded;
            }
            else
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneUnloaded -= OnSceneUnLoaded;
            }

            UpdateCullingDistances();
        }

        private void AddTerrainMeshRenderer(GameObject go, List<MeshRenderer> meshRenderers)
        {
            if (IsSingleMeshTerrain(go))
            {
                MeshRenderer mr = go.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    if (!meshRenderers.Contains(mr) && !m_allTerrainMeshRenderers.Contains(mr))
                    {
                        meshRenderers.Add(mr);
                    }
                }
                return;
            }

            if (IsMeshLODTerrain(go))
            {
                LODGroup lg = go.GetComponent<LODGroup>();
                foreach (LOD lod in lg.GetLODs())
                {
                    foreach (Renderer renderer in lod.renderers)
                    {
                        if (renderer != null && renderer.GetType() == typeof(MeshRenderer))
                        {
                            MeshRenderer mr = (MeshRenderer)renderer;

                            if (!meshRenderers.Contains(mr))
                            {
                                meshRenderers.Add((MeshRenderer)mr);
                            }
                        }
                    }
                }
            }
        }

        private bool IsSingleMeshTerrain(GameObject go)
        {
            //Two possible things to find here: terrains that have been converted to a single mesh terrain or impostor terrains WITHOUT a LOD Group.
            string searchString = GaiaConstants.MeshTerrainName;
            string searchString2 = GaiaConstants.ImpostorTerrainName;
            if (go.name.StartsWith(searchString) || (go.name.StartsWith(searchString2) && go.GetComponent<LODGroup>() == null))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsMeshLODTerrain(GameObject go)
        {
            //Two possible things to find here: terrains that have been converted to a mesh terrain with a LOD Group, or impostor terrains with a LOD Group.
            string searchString1 = GaiaConstants.MeshTerrainLODGroupPrefix;
            string searchString2 = GaiaConstants.ImpostorTerrainName;
            if (go.name.StartsWith(searchString1) || (go.name.StartsWith(searchString2) && go.GetComponent<LODGroup>()!=null))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (!GaiaUtils.CheckIfSceneProfileExists())
            {
                return;
            }

            if (!GaiaGlobal.Instance.SceneProfile.m_terrainCullingEnabled || m_camera == null)
            {
                return;
            }


            Vector3 cameraForward = m_camera.transform.forward;
            float cameraViewDistance = m_camera.farClipPlane;
            m_cameraFOV = m_camera.fieldOfView;

            m_FOVCenter = new Vector3(cameraForward.x, cameraForward.z).normalized * cameraViewDistance;
            GeometryUtility.CalculateFrustumPlanes(m_camera, m_planes);
            if (GaiaGlobal.Instance.SceneProfile.m_terrainCullingEnabled)
            {
                for (int i = 0; i < m_allTerrains.Length; i++)
                {
                    Terrain terrain = m_allTerrains[i];
                    if (terrain == null)
                    {
                        continue;
                    }
                    //Check needs to performed in world space, terrain bounds are in local space of the terrain
                    m_worldSpaceBounds = terrain.terrainData.bounds;
                    m_worldSpaceBounds.center = new Vector3(m_worldSpaceBounds.center.x + terrain.transform.position.x, m_worldSpaceBounds.center.y + terrain.transform.position.y, m_worldSpaceBounds.center.z + terrain.transform.position.z);

                    if (GeometryUtility.TestPlanesAABB(m_planes, m_worldSpaceBounds))
                    {
                        terrain.drawHeightmap = true;
                        terrain.drawTreesAndFoliage = true;

                        //Deactivate terrain GO entirely
                        //terrain.gameObject.SetActive(true);

                        //Activate object spawns
                        //Transform spawnsTransform = terrain.gameObject.transform.Find(GaiaConstants.defaultGOSpawnTarget);
                        //spawnsTransform.gameObject.SetActive(true);
                    }
                    else
                    {
                        terrain.drawHeightmap = false;
                        terrain.drawTreesAndFoliage = false;

                        //Deactivate terrain GO entirely
                        //terrain.gameObject.SetActive(false);

                        //Deactivate object spawns
                        //Transform spawnsTransform = terrain.gameObject.transform.Find(GaiaConstants.defaultGOSpawnTarget);
                        //spawnsTransform.gameObject.SetActive(false);
                    }
                }
            }

            if (GaiaGlobal.Instance.SceneProfile.m_terrainCullingEnabled)
            {
                for (int i = 0; i < m_allTerrainMeshRenderers.Length; i++)
                {
                    MeshRenderer mr = m_allTerrainMeshRenderers[i];
                    if (mr != null)
                    {
                        if (GeometryUtility.TestPlanesAABB(m_planes, mr.bounds))
                        {
                            mr.enabled = true;
                        }
                        else
                        {
                            mr.enabled = false;
                        }
                    }
                }
            }
             
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnLoaded;
        }

        private void OnEnable()
        {
            if (!GaiaUtils.CheckIfSceneProfileExists())
            {
                return;
            }
            if (Application.isPlaying)
            {
                UpdateCullingDistances();
            }
            else
            {
                if (GaiaGlobal.Instance.SceneProfile.CullingProfile != null)
                {
                    ApplySceneSetup(GaiaGlobal.Instance.SceneProfile.CullingProfile.m_applyToEditorCamera);
                }
            }
        }

        //Terrain Culling
        private void OnSceneUnLoaded(Scene scene)
        {
            Invoke("UpdateTerrains", 0.5f);

            m_allTerrainMeshRenderers = m_allTerrainMeshRenderers.Where(x => x != null).ToArray();
            


        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode arg1)
        {
            Invoke("UpdateTerrains", 0.5f);
            List<MeshRenderer> tempMeshRenderers = new List<MeshRenderer>();
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                AddTerrainMeshRenderer(go, tempMeshRenderers);
            }
            m_allTerrainMeshRenderers = m_allTerrainMeshRenderers.Concat(tempMeshRenderers).ToArray();
        }
        private void UpdateTerrains()
        {
            m_allTerrains = Terrain.activeTerrains;
        }

        //Camera Culling
        public static void UpdateCullingDistances()
        {
            if (!GaiaUtils.CheckIfSceneProfileExists())
            {
                return;
            }

            if (GaiaGlobal.Instance.SceneProfile.CullingProfile == null)
            {
                return;
            }

#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                if (ProceduralWorldsGlobalWeather.Instance.CheckIsNight())
                {
                    GaiaGlobal.Instance.SceneProfile.m_sunLight = ProceduralWorldsGlobalWeather.Instance.m_moonLight;
                }
                else
                {
                    GaiaGlobal.Instance.SceneProfile.m_sunLight = ProceduralWorldsGlobalWeather.Instance.m_sunLight;
                }
            }
            else
            {
                if (GaiaGlobal.Instance.SceneProfile.m_sunLight == null)
                {
                    GaiaGlobal.Instance.SceneProfile.m_sunLight = GaiaUtils.GetMainDirectionalLight();
                }
            }
#else
            if (GaiaGlobal.Instance.SceneProfile.m_sunLight == null)
            {
                GaiaGlobal.Instance.SceneProfile.m_sunLight = GaiaUtils.GetMainDirectionalLight();
            }
#endif

            //Make sure we have distances
            if (GaiaGlobal.Instance.SceneProfile.CullingProfile.m_layerDistances == null || GaiaGlobal.Instance.SceneProfile.CullingProfile.m_layerDistances.Length != 32)
            {
                return;
            }

            if (GaiaGlobal.Instance.SceneProfile.m_enableLayerCulling)
            {
                //Apply to main camera
                GaiaGlobal.Instance.m_mainCamera.layerCullDistances = GaiaGlobal.Instance.SceneProfile.CullingProfile.m_layerDistances;

                if (GaiaGlobal.Instance.SceneProfile.m_sunLight != null)
                {
                    GaiaGlobal.Instance.SceneProfile.m_sunLight.layerShadowCullDistances = GaiaGlobal.Instance.SceneProfile.CullingProfile.m_shadowLayerDistances;
                }
            }
            else
            {
                float[] layerCulls = new float[32];
                for (int i = 0; i < layerCulls.Length; i++)
                {
                    layerCulls[i] = 0f;
                }

                //Apply to main camera
                GaiaGlobal.Instance.m_mainCamera.layerCullDistances = layerCulls;

                if (GaiaGlobal.Instance.SceneProfile.m_sunLight != null)
                {
                    GaiaGlobal.Instance.SceneProfile.m_sunLight.layerShadowCullDistances = layerCulls;
                }
            }
        }
        public static void ApplySceneSetup(bool active)
        {
            //Apply to editor camera
#if UNITY_EDITOR
            if (GaiaGlobal.Instance.SceneProfile.m_enableLayerCulling)
            {
                if (active)
                {
                    foreach (var sceneCamera in SceneView.GetAllSceneCameras())
                    {
                        sceneCamera.layerCullDistances = GaiaGlobal.Instance.SceneProfile.CullingProfile.m_layerDistances;
                    }

                    if (GaiaGlobal.Instance.SceneProfile.m_sunLight != null)
                    {
                        GaiaGlobal.Instance.SceneProfile.m_sunLight.layerShadowCullDistances = GaiaGlobal.Instance.SceneProfile.CullingProfile.m_shadowLayerDistances;
                    }
                }
                else
                {
                    foreach (var sceneCamera in SceneView.GetAllSceneCameras())
                    {
                        float[] layers = new float[32];
                        for (int i = 0; i < layers.Length; i++)
                        {
                            layers[i] = 0f;
                        }

                        sceneCamera.layerCullDistances = layers;
                    }

                    if (GaiaGlobal.Instance.SceneProfile.m_sunLight != null)
                    {
                        float[] layers = new float[32];
                        for (int i = 0; i < layers.Length; i++)
                        {
                            layers[i] = 0f;
                        }
                        GaiaGlobal.Instance.SceneProfile.m_sunLight.layerShadowCullDistances = layers;
                    }
                }
            }
            else
            {
                foreach (var sceneCamera in SceneView.GetAllSceneCameras())
                {
                    float[] layers = new float[32];
                    for (int i = 0; i < layers.Length; i++)
                    {
                        layers[i] = 0f;
                    }

                    sceneCamera.layerCullDistances = layers;
                }

                if (GaiaGlobal.Instance.SceneProfile.m_sunLight != null)
                {
                    float[] layers = new float[32];
                    for (int i = 0; i < layers.Length; i++)
                    {
                        layers[i] = 0f;
                    }
                    GaiaGlobal.Instance.SceneProfile.m_sunLight.layerShadowCullDistances = layers;
                }
            }
#endif
        }

        //Controller Setup
        /// <summary>
        /// Sets the current controller type
        /// </summary>
        /// <param name="type"></param>
        public static void SetCurrentControllerType(GaiaConstants.EnvironmentControllerType type)
        {
            LocationSystem system = FindObjectOfType<LocationSystem>();
            if (system != null)
            {
                if (system.m_locationProfile != null)
                {
                    system.m_locationProfile.m_currentControllerType = type;
                }
            }
        }
    }
}