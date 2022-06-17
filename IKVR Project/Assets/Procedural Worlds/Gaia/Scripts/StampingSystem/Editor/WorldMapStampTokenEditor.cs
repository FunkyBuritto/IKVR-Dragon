using Gaia.Internal;
using PWCommon4;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Gaia
{

    /// <summary>
    /// Editor for Biome Preset settings, only offers a text & a button to create the spawner in the scene
    /// If the user wants to edit or create new spawner settings, they can do so by saving a spawner settings file from a spawner directly.
    /// </summary>
    [CustomEditor(typeof(WorldMapStampToken))]
    public class WorldMapStampTokenEditor : PWEditor
    {
        private EditorUtils m_editorUtils;
        WorldMapStampToken m_stampToken;

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

        public EditorUtils EditorUtils { get => m_editorUtils; set => m_editorUtils = value; }

        private void OnEnable()
        {
            m_stampToken = (WorldMapStampToken)target;
            //Init editor utils
            if (EditorUtils == null)
            {
                // Get editor utils for this
                EditorUtils = PWApp.GetEditorUtils(this);
            }

            m_stampToken.ReloadLocalStamper();
            m_stampToken.ReloadWorldStamper();

            m_stampToken.m_isSelected = true;
            m_stampToken.UpdateGizmoPos();
        }
        private void OnDisable()
        {
            if (m_stampToken.m_syncedLocalStamper != null)
            {
#if GAIA_PRO_PRESENT
                m_stampToken.m_syncedLocalStamper.TerrainLoader.m_isSelected = false;
#endif
            }
            m_stampToken.m_isSelected = false;

            if (m_stampToken.m_connectedStamperSettings != null)
            {
                m_stampToken.m_connectedStamperSettings.ClearImageMaskTextures();
            }
            m_stampToken.m_syncedLocalStamper.m_settings.ClearImageMaskTextures();
            m_stampToken.m_syncedLocalStamper.m_settings.ClearImageMaskTextures();
        }

            private void OnSceneGUI()
            {
            if (m_stampToken.m_syncedLocalStamper != null)
            {
                Handles.BeginGUI();
                bool currentGUIState = GUI.enabled;
                if (!GaiaUtils.HasTerrains())
                {
                    GUI.enabled = false;
                    if (GUI.Button(new Rect(Screen.width - 275, Screen.height - 110, 250, 25), new GUIContent("Edit Stamp (Terrain not created yet!)","This Button allows you to edit the stamp on your actual terrain - it only becomes available after you export the terrain from the world designer.")))
                    {

                    }
                }
                else
                {
                    if (GUI.Button(new Rect(Screen.width - 275, Screen.height - 110, 250, 25), new GUIContent("Edit Stamp on Terrain", "This Button will close the world map view to edit the stamp on your actual terrain.")))
                    {
                        EditStamp();
                    }
                }

                GUI.enabled = currentGUIState;

                if (GUI.Button(new Rect(Screen.width - 275, Screen.height - 75, 250, 25), new GUIContent("Return to World Designer","Closes the stamp preview to return to the World Designer.")))
                {
                    TerrainLoaderManager.Instance.SwitchToWorldMap();
                    GameObject worldmapObject = GaiaUtils.GetOrCreateWorldDesigner();
                    Selection.activeObject = worldmapObject;
                }
                Handles.EndGUI();
            }

            // dont render preview if this isnt a repaint. losing performance if we do
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            //Prefer to show the world map stamper
            if (TerrainLoaderManager.Instance.ShowWorldMapTerrain)
            {
                if (m_stampToken.m_syncedWorldMapStamper != null)
                {
                    m_stampToken.SyncWorldMapStamper();
                    m_stampToken.m_syncedWorldMapStamper.DrawStampPreview();
                }
            }
            else
            {
                if (TerrainLoaderManager.Instance.ShowLocalTerrain)
                {
                    if (m_stampToken.m_syncedLocalStamper != null)
                    {
                        m_stampToken.SyncLocalStamper(m_stampToken.m_syncedLocalStamper);
                        m_stampToken.m_syncedLocalStamper.UpdateTerrainLoader();
                        m_stampToken.m_syncedLocalStamper.DrawStampPreview();
                    }
                }
            }
           
        }

        private void EditStamp()
        {
            TerrainLoaderManager.Instance.SwitchToLocalMap();
            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                Vector3Double origin = TerrainLoaderManager.Instance.GetOrigin();
                TerrainLoaderManager.Instance.SetOrigin(new Vector3Double(((m_stampToken.transform.position.x + origin.x) / TerrainLoaderManager.Instance.TerrainSceneStorage.m_worldMaprelativeSize) , 0, ((m_stampToken.transform.position.z+ origin.z) / TerrainLoaderManager.Instance.TerrainSceneStorage.m_worldMaprelativeSize)));
                //position the stamper at 0,Y,0 - we just shifted the origin to there, so we can set the stamper to origin.
                m_stampToken.m_syncedLocalStamper.transform.position = new Vector3(0f, m_stampToken.m_syncedLocalStamper.transform.position.y, 0f);
                m_stampToken.m_syncedLocalStamper.m_openedFromTerrainGenerator = true;
            }
            Selection.activeGameObject = m_stampToken.m_syncedLocalStamper.gameObject;
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.Initialize(); // Do not remove this!
            m_stampToken = (WorldMapStampToken)target;
            serializedObject.Update();
            m_stampToken.SyncLocationToStamperSettings();
            m_stampToken.UpdateGizmoPos();
            m_editorUtils.Panel("StamperControls", DrawStamperControls, true);
        }

        private void DrawStamperControls(bool helpEnabled)
        {
            //m_stampToken.m_previewOnWorldMap = m_editorUtils.Toggle("PreviewOnWorldMap", m_stampToken.m_previewOnWorldMap, helpEnabled);
            //m_stampToken.m_previewOnLocalMap = m_editorUtils.Toggle("PreviewOnLocalMap", m_stampToken.m_previewOnLocalMap, helpEnabled);
            bool currentGUIState = GUI.enabled;

            if (!GaiaUtils.HasTerrains())
            {
                GUI.enabled = false;
                m_editorUtils.ButtonAutoIndent("EditStampDisabled");
            }
            else
            {
                if (m_editorUtils.ButtonAutoIndent("EditStamp"))
                {
                    if (m_stampToken.m_syncedLocalStamper != null)
                    {
                        EditStamp();
                    }
                }
            }

            GUI.enabled = currentGUIState;

            if (m_editorUtils.ButtonAutoIndent("ReturnToGenerator"))
            {
                WorldMap.ShowWorldMapStampSpawner();
            }

            if (m_stampToken.m_previewOnLocalMap && m_stampToken.m_syncedLocalStamper == null)
            {
                Stamper stamper = WorldMapStampToken.GetOrCreateSyncedStamper(GaiaConstants.worldMapLocalStamper);
                //load settings and don't instantiate - the stamper should be able to modify thses settings on the local map if the user wishes to!
                m_stampToken.LoadStamperSettings(stamper, false);
                m_stampToken.m_syncedLocalStamper = stamper;
                stamper.UpdateMinMaxHeight();
                stamper.m_stampDirty = true;
            }

            if (m_stampToken.m_previewOnWorldMap && m_stampToken.m_syncedWorldMapStamper == null)
            {
                Stamper stamper = WorldMapStampToken.GetOrCreateSyncedStamper(GaiaConstants.worldMapWorldStamper);

                //load settings and instantiate - the stamper should not modify the settings as it is just a scaled preview on the world map
                m_stampToken.LoadStamperSettings(stamper, true);
                //important - stamper must be marked as world map stamper to work with the world map terrain!
                stamper.m_settings.m_isWorldmapStamper = true;
                stamper.UpdateMinMaxHeight();
                stamper.m_stampDirty = true;
                m_stampToken.m_syncedWorldMapStamper = stamper;
            }
        }

      
    }
}
