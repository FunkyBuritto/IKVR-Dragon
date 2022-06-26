using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gaia
{

    public class WorldMapStampToken : MonoBehaviour
    {
        public bool m_previewOnWorldMap = true;
        public bool m_previewOnLocalMap = true;
        public string m_featureType;
        public StamperSettings m_connectedStamperSettings;
        public bool m_isSelected;
        public Stamper m_syncedLocalStamper;
        public Stamper m_syncedWorldMapStamper;
        public Color m_gizmoColor = Color.white;

        //store the position of the gizmo separatedly, needs still to be painted on the terrain when the stamp is located below
        private Vector3 m_gizmoPos;

        private GaiaSessionManager m_sessionManager;
        private GaiaSessionManager SessionManager
        {
            get
            {
                if (m_sessionManager == null)
                {
                    m_sessionManager = GaiaSessionManager.GetSessionManager(false);
                }
                return m_sessionManager;
            }
        }

        private Terrain m_worldMapTerrain;

        private Terrain WorldMapTerrain
        {
            get
            {
                if (m_worldMapTerrain == null)
                {
                    m_worldMapTerrain = TerrainHelper.GetWorldMapTerrain();
                }
                return m_worldMapTerrain;
            }
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnDestroy()
        {
            if (m_connectedStamperSettings != null)
            {
                m_connectedStamperSettings.ClearImageMaskTextures();
            }
            m_syncedLocalStamper.m_settings.ClearImageMaskTextures();
            m_syncedLocalStamper.m_settings.ClearImageMaskTextures();
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            //Only draw gizmos all over the world map when the random world generator or another stamp token is selected.
            if (Selection.activeGameObject!=null)
            {
                Spawner spawner = Selection.activeGameObject.GetComponent<Spawner>();
                WorldMapStampToken token = Selection.activeGameObject.GetComponent<WorldMapStampToken>();
                if (spawner != null || token != null)
                {
                    if (token != null || spawner.m_settings.m_isWorldmapSpawner)
                    {
                        if (!m_isSelected || !m_previewOnWorldMap)
                        {
                            if (Camera.current != null && Vector3.Distance(transform.position, Camera.current.transform.position) < 10000)
                            {
                                Gizmos.color = m_gizmoColor;
                                Gizmos.DrawSphere(m_gizmoPos, m_connectedStamperSettings.m_width /2f);
                            }
                        }
                    }
                }
            }
#endif
        }

        public void UpdateGizmoPos()
        {
            float scalarX = Mathf.InverseLerp(0,WorldMapTerrain.terrainData.size.x, transform.position.x - WorldMapTerrain.transform.position.x);
            float scalarZ = Mathf.InverseLerp(0, WorldMapTerrain.terrainData.size.z, transform.position.z - WorldMapTerrain.transform.position.z);
            float sampledYPos = WorldMapTerrain.terrainData.GetInterpolatedHeight(scalarX,scalarZ);
            if (sampledYPos > transform.position.y)
            {
                m_gizmoPos = new Vector3(transform.position.x, sampledYPos, transform.position.z);
            }
            else
            {
                m_gizmoPos = transform.position;
            }
        }

        public void SyncLocationToStamperSettings()
        {
            Vector3 newPos = GetLocalStamperPosition();

            m_connectedStamperSettings.m_x = newPos.x;
            m_connectedStamperSettings.m_z = newPos.z;
            m_connectedStamperSettings.m_y = newPos.y;



        }

        public void SyncLocationFromStamperSettings()
        {
            BoundsDouble b = new BoundsDouble();
            TerrainHelper.GetTerrainBounds(ref b);
            Transform worldmapTransform = transform.parent.parent;
            Terrain worldMapTerrain = worldmapTransform.GetComponent<Terrain>();
            //TODO: review if this needs to be in double precision

            Vector3Double origin = TerrainLoaderManager.Instance.GetOrigin();

            float relativeX = (float)(m_connectedStamperSettings.m_x - b.min.x + origin.x / TerrainLoaderManager.Instance.TerrainSceneStorage.m_worldMaprelativeSize) / (float)b.size.x;
            float relativeY = (float)m_connectedStamperSettings.m_y * TerrainLoaderManager.Instance.TerrainSceneStorage.m_worldMaprelativeSize;
            float relativeZ = (float)(m_connectedStamperSettings.m_z - b.min.z + origin.z / TerrainLoaderManager.Instance.TerrainSceneStorage.m_worldMaprelativeSize) / (float)b.size.z;

            //float relativeX = (float)(m_connectedStamperSettings.m_x - b.min.x) / (float)b.size.x;
            //float relativeY = (float) m_connectedStamperSettings.m_y * SessionManager.m_session.m_worldMaprelativeSize;
            //float relativeZ = (float)(m_connectedStamperSettings.m_z - b.min.z) / (float)b.size.z;

            float newX = worldmapTransform.position.x + worldMapTerrain.terrainData.size.x * relativeX;
            float newZ = worldmapTransform.position.z + worldMapTerrain.terrainData.size.z * relativeZ;

            transform.position = new Vector3(newX, relativeY, newZ);
        }

        public void ReloadLocalStamper()
        {
            if (m_syncedLocalStamper != null)
            {
        #if GAIA_PRO_PRESENT
                m_syncedLocalStamper.TerrainLoader.m_isSelected = true;
        #endif
                LoadStamperSettings(m_syncedLocalStamper, false);
            }
        }

        public void ReloadWorldStamper()
        {
            if (m_syncedWorldMapStamper != null)
            {
                LoadStamperSettings(m_syncedWorldMapStamper, true);
                //important - stamper must be marked as world map stamper to work with the world map terrain!
                m_syncedWorldMapStamper.m_settings.m_isWorldmapStamper = true;
                m_syncedWorldMapStamper.m_stampDirty = true;
            }
        }

        public void SyncLocalStamper(Stamper stamper)
        {
            stamper.transform.position = GetLocalStamperPosition();
        }

        public void SyncWorldMapStamper()
        {
            if (m_syncedWorldMapStamper != null)
            {
                m_syncedWorldMapStamper.transform.position = transform.position;
                m_syncedWorldMapStamper.m_settings.m_width = m_connectedStamperSettings.m_width / TerrainLoaderManager.Instance.TerrainSceneStorage.m_terrainTilesX; 
                m_syncedWorldMapStamper.m_settings.m_height = m_connectedStamperSettings.m_height;
                m_syncedWorldMapStamper.transform.localScale = new Vector3(m_syncedWorldMapStamper.m_settings.m_width, m_syncedWorldMapStamper.m_settings.m_height, m_syncedWorldMapStamper.m_settings.m_width);
            }
        }

        private Vector3 GetLocalStamperPosition()
        {
            BoundsDouble b = new BoundsDouble();
            TerrainHelper.GetTerrainBounds(ref b);
            Transform worldmapTransform = transform.parent.parent;
            Terrain worldMapTerrain = worldmapTransform.GetComponent<Terrain>();
            float relativeX = (transform.position.x - worldmapTransform.position.x) / worldMapTerrain.terrainData.size.x;
            float relativeZ = (transform.position.z - worldmapTransform.position.z) / worldMapTerrain.terrainData.size.z;
            float relativeY = transform.position.y / TerrainLoaderManager.Instance.TerrainSceneStorage.m_worldMaprelativeSize; 

            //TODO: Check if double precision required
            float newX = (float)b.min.x + (float)b.size.x * relativeX;
            float newZ = (float)b.min.z + (float)b.size.z * relativeZ;

            return new Vector3(newX, relativeY, newZ);
        }

        public static Stamper GetOrCreateSyncedStamper(string stamperName)
        {
            Stamper stamper = null;
            //No stamper passed in, does a session Stamper exist?
            if (stamper == null)
            {
                GameObject stamperObj = GameObject.Find(stamperName);
                if (stamperObj == null)
                {
                    GameObject wmeTempTools = GaiaUtils.GetOrCreateWorldMapTempTools();
                    stamperObj = new GameObject(stamperName);
                    stamperObj.transform.parent = wmeTempTools.transform;
                }
                if (stamperObj.GetComponent<Stamper>() == null)
                {
                    stamper = stamperObj.AddComponent<Stamper>();
#if GAIA_PRO_PRESENT
                    if (GaiaUtils.HasDynamicLoadedTerrains())
                    {
                        //We got placeholders, activate terrain loading
                        stamper.m_loadTerrainMode = LoadMode.EditorSelected;
                    }
#endif
                }
                stamper = stamperObj.GetComponent<Stamper>();
            }

            return stamper;
        }

        public void LoadStamperSettings(Stamper stamper, bool instantiateSettings)
        {
            stamper.LoadSettings(m_connectedStamperSettings, instantiateSettings);
            stamper.m_worldMapStampToken = this;
#if GAIA_PRO_PRESENT
            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                //We got placeholders, activate terrain loading
                stamper.m_loadTerrainMode = LoadMode.EditorSelected;
            }
#endif
        }

        public void DrawWorldMapPreview()
        {
            if (m_syncedWorldMapStamper != null)
            {
                SyncWorldMapStamper();
                m_syncedWorldMapStamper.DrawStampPreview();
            }
        }
    }
}
