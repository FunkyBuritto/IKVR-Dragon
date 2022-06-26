using Gaia.Internal;
using PWCommon4;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using Gaia.Pipeline.LWRP;
using Gaia.Pipeline.HDRP;
using Gaia.Pipeline.URP;
using Gaia.Pipeline;
using UnityStandardAssets.Characters.FirstPerson;
using UnityStandardAssets.Characters.ThirdPerson;
using UnityEngine.Scripting;
#if UNITY_2018_3_OR_NEWER
using UnityEngine.Networking;
#endif

namespace Gaia
{
    /// <summary>
    /// Handy helper for all things Gaia
    /// </summary>
    public class GaiaManagerEditor : EditorWindow, IPWEditor
    {
        #region Variables, Properties
        private GUIStyle m_boxStyle;
        private GUIStyle m_wrapStyle;
        private GUIStyle m_titleStyle;
        private GUIStyle m_headingStyle;
        private GUIStyle m_bodyStyle;
        private GUIStyle m_linkStyle;
        private static GaiaSettings m_settings;
        private UnityPipelineProfile m_gaiaPipelineSettings;
        private IEnumerator m_updateCoroutine;
        private static EditorUtils m_editorUtils;

        private TabSet m_mainTabs;
        private TabSet m_creationTabs;
        private TabSet m_extensionsTabs;

        //Extension manager
        bool m_needsScan = true;
        GaiaExtensionManager m_extensionMgr = new GaiaExtensionManager();
        //private bool m_foldoutSession = false;
        //private bool m_foldoutTerrain = false;
        //private bool m_foldoutSpawners = false;
        //private bool m_foldoutCharacters = false;
        //private bool m_foldoutUtils = false;
        private GaiaConstants.EnvironmentSize m_oldTargetSize;
        private GaiaConstants.EnvironmentTarget m_oldTargetEnv;
        private bool m_foldoutTerrainResolutionSettings = false;

        // Icon tests
        private Texture2D m_stdIcon;
        private Texture2D m_advIcon;
        private Texture2D m_gxIcon;
        private Texture2D m_moreIcon;

        //Bool system checks
        private bool m_shadersNotImported;
        public bool m_showSetupPanel;
        private bool m_enableGUI;
        private Color m_defaultPanelColor;

        //Water Profiles
        private string m_unityVersion;
        private List<string> m_profileList = new List<string>();
        private List<Material> m_allMaterials = new List<Material>();
        private int newProfileListIndex = 0;

        //Terrain resolution settings
        private GaiaConstants.HeightmapResolution m_heightmapResolution;
        private GaiaConstants.TerrainTextureResolution m_controlTextureResolution;
        private GaiaConstants.TerrainTextureResolution m_basemapResolution;
        private float m_spawnDensity = 1.0f;
        private int m_detailResolutionPerPatch;
        private int m_detailResolution;
        private int m_biomePresetSelection = int.MinValue;

        //Biomes and Spawners
        private List<BiomePresetDropdownEntry> m_allBiomePresets = new List<BiomePresetDropdownEntry>();
        private List<BiomeSpawnerListEntry> m_BiomeSpawnersToCreate = new List<BiomeSpawnerListEntry>();
        private List<BiomeSpawnerListEntry> m_advancedTabAllSpawners = new List<BiomeSpawnerListEntry>();
        private List<AdvancedTabBiomeListEntry> m_advancedTabAllBiomes = new List<AdvancedTabBiomeListEntry>();
        private UnityEditorInternal.ReorderableList m_biomeSpawnersList;
        private UnityEditorInternal.ReorderableList m_advancedTabBiomesList;
        private UnityEditorInternal.ReorderableList m_advancedTabSpawnersList;

        //Misc
        private bool m_foldoutSpawnerSettings;
        private bool m_foldOutWorldSizeSettings;
        private GUIStyle m_helpStyle;
        private bool m_foldoutExtrasSettings;
        private bool m_advancedTabFoldoutSpawners;
        private bool m_advancedTabFoldoutBiomes;


        private GaiaSessionManager m_sessionManager;
        private bool m_initResSettings;

        private bool m_statusCheckPerformed;
        private bool m_showAutoStreamSettingsBox;
        private bool m_terrainCreationRunning;
        private bool m_createStamper = true;
        private bool m_runtimeCreated;
        private bool m_useWorldDesigner = false;
        private bool m_renderPipelineDefaultStatus;
        private string m_setupWarningText;
        private UserFiles m_userFiles;
        private bool m_showInstallPipelineHelp;

        private URLParameters gaiaParameters = new URLParameters() { m_product = "Gaia" };
        private URLParameters gaiaProParameters = new URLParameters() { m_product = "Gaia Pro" };

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


        public bool PositionChecked { get; set; }
        #endregion

        #region Gaia Menu Items
        /// <summary>
        /// Show Gaia Manager editor window
        /// </summary>
        [MenuItem("Window/" + PWConst.COMMON_MENU + "/Gaia/Show Gaia Manager... %g", false, 40)]
        public static void ShowGaiaManager()
        {
            try
            {
                var manager = EditorWindow.GetWindow<Gaia.GaiaManagerEditor>(false, "Gaia Manager");
                //Manager can be null if the dependency package installation is started upon opening the manager window.
                if (manager != null)
                {
                    Vector2 initialSize = new Vector2(650f, 450f);
                    manager.position = new Rect(new Vector2(Screen.currentResolution.width / 2f - initialSize.x / 2f, Screen.currentResolution.height / 2f - initialSize.y / 2f), initialSize);
                    manager.Show();
                }
            }
            catch (Exception ex)
            {
                //not catching anything specific here, but the maintenance and shader installation tasks can trigger a null reference on that "GetWindow" above

                //get rid off the warning for unused "ex"
                if (ex.Message == "")
                { }
            };
        }

        ///// <summary>
        ///// Show the forum
        ///// </summary>
        //[MenuItem("Window/Gaia/Show Forum...", false, 60)]
        //public static void ShowForum()
        //{
        //    Application.OpenURL(
        //        "http://www.procedural-worlds.com/forum/gaia/");
        //}

        /// <summary>
        /// Show documentation
        /// </summary>
        [MenuItem("Window/" + PWConst.COMMON_MENU + "/Gaia/Show Extensions...", false, 65)]
        public static void ShowExtensions()
        {
            Application.OpenURL("https://www.procedural-worlds.com/products/indie/gaia/gaia-extensions/");
        }
        #endregion

        #region Constructors destructors and related delegates

        /// <summary>
        /// Setup on destroy
        /// </summary>
        private void OnDestroy()
        {
            EditorApplication.update -= ReflectionProbeBakeUpdate;
            m_editorUtils = null;
        }

        /// <summary>
        /// See if we can preload the manager with existing settings
        /// </summary>
        public void OnEnable()
        {
            //Get or create existing settings object
            if (m_settings == null)
            {
                m_settings = (GaiaSettings)PWCommon4.AssetUtils.GetAssetScriptableObject("GaiaSettings");
                if (m_settings == null)
                {
                    m_settings = CreateSettingsAsset();
                }
            }

            if (m_editorUtils == null)
            {
                // Get editor utils for this
#if GAIA_PRO_PRESENT
                m_editorUtils = PWApp.GetEditorUtils(this, null, GaiaConstants.GaiaProNewsURL, gaiaProParameters);
#else
                m_editorUtils = PWApp.GetEditorUtils(this, null, GaiaConstants.GaiaNewsURL, gaiaParameters);
#endif
            }

            GaiaManagerStatusCheck(true);

            m_defaultPanelColor = GUI.backgroundColor;

            if (EditorGUIUtility.isProSkin)
            {
                if (m_stdIcon == null)
                {
                    m_stdIcon = Resources.Load("gstdIco_p") as Texture2D;
                }
                if (m_advIcon == null)
                {
                    m_advIcon = Resources.Load("gadvIco_p") as Texture2D;
                }
                if (m_gxIcon == null)
                {
                    m_gxIcon = Resources.Load("ggxIco_p") as Texture2D;
                }
                if (m_moreIcon == null)
                {
                    m_moreIcon = Resources.Load("gmoreIco_p") as Texture2D;
                }
            }
            else
            {
                if (m_stdIcon == null)
                {
                    m_stdIcon = Resources.Load("gstdIco") as Texture2D;
                }
                if (m_advIcon == null)
                {
                    m_advIcon = Resources.Load("gadvIco") as Texture2D;
                }
                if (m_gxIcon == null)
                {
                    m_gxIcon = Resources.Load("ggxIco") as Texture2D;
                }
                if (m_moreIcon == null)
                {
                    m_moreIcon = Resources.Load("gmoreIco") as Texture2D;
                }
            }

            m_settings = GaiaUtils.GetGaiaSettings();
            if (m_settings == null)
            {
                Debug.Log("Gaia Settings are missing from our project, please make sure Gaia settings is in your project.");
                return;
            }
            //Make sure we have defaults
            if (m_settings.m_currentDefaults == null)
            {
                m_settings.m_currentDefaults = (GaiaDefaults)PWCommon4.AssetUtils.GetAssetScriptableObject("GaiaDefaults");
                EditorUtility.SetDirty(m_settings);
            }

            var mainTabs = new Tab[] {
                new Tab ("Standard", m_stdIcon, StandardTab),
                new Tab ("Advanced", m_advIcon, AdvancedTab),
                new Tab ("GX", m_gxIcon, ExtensionsTab),
                new Tab ("More...", m_moreIcon, TutorialsAndSupportTab),
            };
            var creationTabs = new Tab[] {
                //new Tab ("Legacy", LegacyTab),
                new Tab ("Setup", SetupTab),
                new Tab ("TerrainCreation", TerrainCreationTab),
                new Tab ("ToolCreation", ToolCreationTab),
                new Tab ("RuntimeCreation", RuntimeCreationTab),
                new Tab ("LightBaking", LightBakingTab),
            };

            var gxTabs = new Tab[] {
                new Tab ("PanelExtensions", InstalledExtensionsTab),
                new Tab ("Partners & Extensions", MoreOnProceduralWorldsTab),
            };

            m_mainTabs = new TabSet(m_editorUtils, mainTabs);
            m_creationTabs = new TabSet(m_editorUtils, creationTabs);
            m_extensionsTabs = new TabSet(m_editorUtils, gxTabs);

            //Signal we need a scan
            m_needsScan = true;

            m_gaiaPipelineSettings = m_settings.m_pipelineProfile;

            //Sets up the render to the correct pipeline
            if (GraphicsSettings.renderPipelineAsset == null)
            {
                m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.BuiltIn;
                m_settings.m_pipelineProfile.m_activePipelineInstalled = GaiaConstants.EnvironmentRenderer.BuiltIn;
            }
            else if (GraphicsSettings.renderPipelineAsset.GetType().ToString().Contains("HDRenderPipelineAsset"))
            {
                m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.HighDefinition;
            }
            else if (GraphicsSettings.renderPipelineAsset.GetType().ToString().Contains("UniversalRenderPipelineAsset"))
            {
                m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.Universal;
            }
            else
            {
                m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.Lightweight;
            }

            //Set water profile
            newProfileListIndex = m_settings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex;
            if (newProfileListIndex > m_settings.m_gaiaWaterProfile.m_waterProfiles.Count + 1)
            {
                newProfileListIndex = 0;
                m_settings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex = 0;
            }


            //Initialize editor resolution settings with defaults
            if (m_settings.m_currentDefaults != null)
            {
                m_oldTargetSize = (GaiaConstants.EnvironmentSize)m_settings.m_currentSize;
                m_oldTargetEnv = (GaiaConstants.EnvironmentTarget)m_settings.m_currentEnvironment;
                m_heightmapResolution = (GaiaConstants.HeightmapResolution)m_settings.m_currentDefaults.m_heightmapResolution;
                m_controlTextureResolution = (GaiaConstants.TerrainTextureResolution)m_settings.m_currentDefaults.m_controlTextureResolution;
                m_basemapResolution = (GaiaConstants.TerrainTextureResolution)m_settings.m_currentDefaults.m_baseMapSize;
                m_detailResolutionPerPatch = m_settings.m_currentDefaults.m_detailResolutionPerPatch;
                m_detailResolution = m_settings.m_currentDefaults.m_detailResolution;
                m_spawnDensity = m_settings.m_currentDefaults.m_spawnDensity;
            }

            //Find the correct default creation tab when opening the Gaia Manager.

            //Start with the Terrain creation tab
            int defaultTabIndex = 1;


            //Are there currently setup issues? If yes, we select the setup tab

            if (CheckForSetupIssues())
            {
                defaultTabIndex = 0;
            }

            //We are still at the Terrain Creation Tab? Check if there is already a terrain in the scene
            if (defaultTabIndex == 1)
            {
                if (Terrain.activeTerrains.Length > 0)
                {
                    //There is, select the tool tab instead
                    defaultTabIndex = 2;
                }
            }

            //If we select the tool tab, check if the tools have already been created
            if (defaultTabIndex == 2)
            {
                if (GameObject.FindObjectOfType<Stamper>() != null || GameObject.FindObjectOfType<BiomeController>() != null)
                {
                    //Tools are already in the scene, show the runtime tab instead
                    defaultTabIndex = 3;
                }
            }

            //Done with the default tab, set it up
            m_creationTabs.DefaultTabIndex = defaultTabIndex;
            m_creationTabs.ActiveTabIndex = defaultTabIndex;

            //Figure out if runtime was created already
            if (GameObject.Find(GaiaConstants.gaiaLightingObject) != null ||
               GameObject.Find(GaiaConstants.gaiaWaterObject) != null ||
               GameObject.Find(GaiaConstants.gaiaPlayerObject) != null)

            {
                m_runtimeCreated = true;
            }


            if (!Application.isPlaying)
            {
                StartEditorUpdates();
                m_updateCoroutine = GetNewsUpdate();
            }

            if (m_userFiles == null)
            {
                m_userFiles = GaiaUtils.GetOrCreateUserFiles();
            }

            for (int i = 0; i < m_userFiles.m_gaiaManagerBiomePresets.Count; i++)
            {
                BiomePreset sp = m_userFiles.m_gaiaManagerBiomePresets[i];
                if (sp != null)
                {
                    m_allBiomePresets.Add(new BiomePresetDropdownEntry { ID = i, name = sp.name, biomePreset = sp });
                }
            }
            m_allBiomePresets.Sort();
            //Add the artifical "Custom" option
            m_allBiomePresets.Add(new BiomePresetDropdownEntry { ID = -999, name = "Custom", biomePreset = null });

            if (m_allBiomePresets.Count > 0)
            {
                m_biomePresetSelection = m_allBiomePresets[0].ID;
            }

            if (m_biomePresetSelection != int.MinValue)
            {
                //Fill in initial content
                AddBiomeSpawnersForSelectedPreset();
                CreateBiomePresetList();
            }

            CreateAdvancedTabBiomesList();

            CreateAdvancedTabSpawnersList();

            EditorApplication.update -= ReflectionProbeBakeUpdate;
            EditorApplication.update += ReflectionProbeBakeUpdate;

            m_initResSettings = true;
        }

        public bool CheckForSetupIssues()
        {
            m_setupWarningText = "";

            //Is the selected & installed pipeline the same?
            if (m_settings.m_pipelineProfile.m_activePipelineInstalled != m_settings.m_currentRenderer)
            {
                m_renderPipelineDefaultStatus = true;
                return true;
            }

            //Is there an issue with the shaders for this pipeline?
            if (!AreShadersInstalledCorrectly(m_settings.m_pipelineProfile.m_activePipelineInstalled))
            {
                m_setupWarningText = "Some of the Gaia materials are missing shaders, please install the shaders for the current pipeline with the button below in the Render Pipeline Settings. It is expected to see this warning message if you just switched the rendering pipeline configuration, installing the shaders is part of the pipeline switching process.";
                m_renderPipelineDefaultStatus = true;
                return true;
            }

            //Is the color space correct?
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                m_renderPipelineDefaultStatus = true;
                return true;
            }

            //Check for deferred rendering in built-in pipeline
            GaiaConstants.EnvironmentRenderer renderer = GaiaUtils.GetActivePipeline();
            if (renderer == GaiaConstants.EnvironmentRenderer.BuiltIn)
            {
                if (m_settings.m_currentEnvironment == GaiaConstants.EnvironmentTarget.Desktop ||
                m_settings.m_currentEnvironment == GaiaConstants.EnvironmentTarget.PowerfulDesktop ||
                m_settings.m_currentEnvironment == GaiaConstants.EnvironmentTarget.Custom)
                {
                    var tier1 = EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier1);
                    if (tier1.renderingPath != RenderingPath.DeferredShading)
                    {
                        m_renderPipelineDefaultStatus = true;
                        return true;
                    }
                }
            }
            return false;
        }

        private void LightBakingTab()
        {
            m_editorUtils.Panel("LightBaking", DrawLightBaking, true);
        }

        private void RuntimeCreationTab()
        {
            m_editorUtils.Panel("RuntimeCreation", DrawRuntimeCreation, true);
        }

        private void ToolCreationTab()
        {
            m_editorUtils.Panel("ToolCreation", DrawToolCreation, true);
        }


        private void AddBiomeSpawnersForSelectedPreset()
        {
            m_BiomeSpawnersToCreate.Clear();

            BiomePresetDropdownEntry entry = m_allBiomePresets.Find(x => x.ID == m_biomePresetSelection);
            if (entry.biomePreset != null)
            {

                //Need to create a deep copy of the preset list, otherwise the users will overwrite it when they add custom spawners
                foreach (BiomeSpawnerListEntry spawnerListEntry in entry.biomePreset.m_spawnerPresetList)
                {
                    if (spawnerListEntry.m_spawnerSettings != null)
                    {
                        m_BiomeSpawnersToCreate.Add(spawnerListEntry);
                    }
                }
            }
        }

        private void CreateAdvancedTabBiomesList()
        {
            m_advancedTabAllBiomes.Clear();

            if (m_userFiles == null)
            {
                m_userFiles = GaiaUtils.GetOrCreateUserFiles();
            }

            for (int i = 0; i < m_userFiles.m_gaiaManagerBiomePresets.Count(); i++)
            {
                if (m_userFiles.m_gaiaManagerBiomePresets[i] != null)
                {
                    m_advancedTabAllBiomes.Add(new AdvancedTabBiomeListEntry { m_autoAssignPrototypes = true, m_biomePreset = m_userFiles.m_gaiaManagerBiomePresets[i] });
                }
            }
            m_advancedTabAllBiomes.Sort();

            m_advancedTabBiomesList = new UnityEditorInternal.ReorderableList(m_advancedTabAllBiomes, typeof(AdvancedTabBiomeListEntry), false, true, false, false);
            m_advancedTabBiomesList.drawElementCallback = DrawAdvancedTabBiomeListElement;
            m_advancedTabBiomesList.drawHeaderCallback = DrawAdvancedTabBiomeListHeader;
            m_advancedTabBiomesList.elementHeightCallback = OnElementHeightSpawnerPresetListEntry;
        }

        private void DrawAdvancedTabBiomeListHeader(Rect rect)
        {
            BiomeListEditor.DrawListHeader(rect, true, m_advancedTabAllBiomes, m_editorUtils, "AdvancedTabBiomeListHeader");
        }

        private void DrawAdvancedTabBiomeListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            BiomeListEditor.DrawListElement_AdvancedTab(rect, m_advancedTabAllBiomes[index], m_editorUtils);
        }

        private void CreateAdvancedTabSpawnersList()
        {
            m_advancedTabAllSpawners.Clear();

            if (m_userFiles == null)
            {
                m_userFiles = GaiaUtils.GetOrCreateUserFiles();
            }

            for (int i = 0; i < m_userFiles.m_gaiaManagerSpawnerSettings.Count; i++)
            {
                SpawnerSettings spawnerSettings = m_userFiles.m_gaiaManagerSpawnerSettings[i];
                if (spawnerSettings != null)
                {
                    m_advancedTabAllSpawners.Add(new BiomeSpawnerListEntry { m_autoAssignPrototypes = true, m_spawnerSettings = spawnerSettings });
                }
            }
            m_advancedTabAllSpawners.Sort();
            m_advancedTabSpawnersList = new UnityEditorInternal.ReorderableList(m_advancedTabAllSpawners, typeof(BiomeSpawnerListEntry), false, true, false, false);
            m_advancedTabSpawnersList.elementHeightCallback = OnElementHeightSpawnerPresetListEntry;
            m_advancedTabSpawnersList.drawElementCallback = DrawAdvancedTabSpawnerListElement;
            m_advancedTabSpawnersList.drawHeaderCallback = DrawAdvancedTabSpawnerPresetListHeader;
            m_advancedTabSpawnersList.onAddCallback = OnAddSpawnerPresetListEntry;
            m_advancedTabSpawnersList.onRemoveCallback = OnRemoveSpawnerPresetListEntry;
            m_advancedTabSpawnersList.onReorderCallback = OnReorderSpawnerPresetList;
        }

        /// <summary>
        /// Settings up settings on disable
        /// </summary>
        void OnDisable()
        {
            StopEditorUpdates();
        }

        #region Spawner Preset List

        void CreateBiomePresetList()
        {
            m_biomeSpawnersList = new UnityEditorInternal.ReorderableList(m_BiomeSpawnersToCreate, typeof(BiomeSpawnerListEntry), true, true, true, true);
            m_biomeSpawnersList.elementHeightCallback = OnElementHeightSpawnerPresetListEntry;
            m_biomeSpawnersList.drawElementCallback = DrawSpawnerPresetListElement;
            m_biomeSpawnersList.drawHeaderCallback = DrawSpawnerPresetListHeader;
            m_biomeSpawnersList.onAddCallback = OnAddSpawnerPresetListEntry;
            m_biomeSpawnersList.onRemoveCallback = OnRemoveSpawnerPresetListEntry;
            m_biomeSpawnersList.onReorderCallback = OnReorderSpawnerPresetList;
        }

        private void OnReorderSpawnerPresetList(ReorderableList list)
        {
            //Do nothing, changing the order does not immediately affect anything in this window
        }

        private void OnRemoveSpawnerPresetListEntry(ReorderableList list)
        {
            m_BiomeSpawnersToCreate = SpawnerPresetListEditor.OnRemoveListEntry(m_BiomeSpawnersToCreate, m_biomeSpawnersList.index);
            list.list = m_BiomeSpawnersToCreate;
            m_biomePresetSelection = -999;
        }

        private void OnAddSpawnerPresetListEntry(ReorderableList list)
        {
            m_BiomeSpawnersToCreate = SpawnerPresetListEditor.OnAddListEntry(m_BiomeSpawnersToCreate);
            list.list = m_BiomeSpawnersToCreate;
            m_biomePresetSelection = -999;
        }

        private void DrawSpawnerPresetListHeader(Rect rect)
        {
            SpawnerPresetListEditor.DrawListHeader(rect, true, m_BiomeSpawnersToCreate, m_editorUtils, "SpawnerAdded");
        }

        private void DrawSpawnerPresetListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SpawnerPresetListEditor.DrawListElement(rect, m_BiomeSpawnersToCreate[index], m_editorUtils, this);
        }

        private void DrawAdvancedTabSpawnerPresetListHeader(Rect rect)
        {
            SpawnerPresetListEditor.DrawListHeader(rect, true, m_BiomeSpawnersToCreate, m_editorUtils, "AdvancedTabSpawnerListHeader");
        }

        private void DrawAdvancedTabSpawnerListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SpawnerPresetListEditor.DrawListElement_AdvancedTab(rect, m_advancedTabAllSpawners[index], m_editorUtils);
        }

        private float OnElementHeightSpawnerPresetListEntry(int index)
        {
            return SpawnerPresetListEditor.OnElementHeight();
        }



        #endregion

        /// <summary>
        /// Creates a new Gaia settings asset
        /// </summary>
        /// <returns>New gaia settings asset</returns>
        public static GaiaSettings CreateSettingsAsset()
        {
            GaiaSettings settings = ScriptableObject.CreateInstance<Gaia.GaiaSettings>();
            AssetDatabase.CreateAsset(settings, GaiaDirectories.GetSettingsDirectory() + "/GaiaSettings.asset");
            AssetDatabase.SaveAssets();
            return settings;
        }

        #endregion

        #region Tabs
        /// <summary>
        /// Draw the brief editor
        /// </summary>
        void StandardTab()
        {
            m_editorUtils.Tabs(m_creationTabs);
        }


        private void TerrainCreationTab()
        {
            m_editorUtils.Panel("TerrainCreation", DrawTerrainCreation, true);
        }

        private void DrawTerrainCreation(bool helpEnabled)
        {

            #region WORLD SIZE
            GUILayout.BeginHorizontal();
            {
                GaiaConstants.EnvironmentSizePreset oldSizePreset = m_settings.m_targeSizePreset;
                m_oldTargetSize = m_settings.m_currentSize;
                m_settings.m_targeSizePreset = (GaiaConstants.EnvironmentSizePreset)m_editorUtils.EnumPopup("World Size", m_settings.m_targeSizePreset);

                if (oldSizePreset != GaiaConstants.EnvironmentSizePreset.Custom && m_settings.m_targeSizePreset == GaiaConstants.EnvironmentSizePreset.Custom)
                {
                    //Automatically fold out the advanced settings when the user switches to "Custom"
                    m_foldOutWorldSizeSettings = true;
                }
                if (!m_foldOutWorldSizeSettings)
                {
                    if (m_editorUtils.Button("PlusButtonWorldSize", GUILayout.Width(20), GUILayout.Height(18)))
                    {
                        m_foldOutWorldSizeSettings = true;
                    }
                }
                else
                {
                    if (m_editorUtils.Button("MinusButtonWorldSize", GUILayout.Width(20), GUILayout.Height(18)))
                    {
                        m_foldOutWorldSizeSettings = false;
                    }
                }

                switch (m_settings.m_targeSizePreset)
                {
                    case GaiaConstants.EnvironmentSizePreset.Tiny:
                        m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is256MetersSq;
                        m_settings.m_currentDefaults.m_terrainHeight = m_settings.m_currentDefaults.m_terrainSize;
                        break;
                    case GaiaConstants.EnvironmentSizePreset.Small:
                        m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is512MetersSq;
                        m_settings.m_currentDefaults.m_terrainHeight = m_settings.m_currentDefaults.m_terrainSize;
                        break;
                    case GaiaConstants.EnvironmentSizePreset.Medium:
                        m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is1024MetersSq;
                        m_settings.m_currentDefaults.m_terrainHeight = m_settings.m_currentDefaults.m_terrainSize;
                        break;
                    case GaiaConstants.EnvironmentSizePreset.Large:
                        m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is2048MetersSq;
                        m_settings.m_currentDefaults.m_terrainHeight = m_settings.m_currentDefaults.m_terrainSize;
                        break;
                        //case GaiaConstants.EnvironmentSizePreset.XLarge:
                        //    m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is4096MetersSq;
                        //    m_settings.m_currentDefaults.m_terrainHeight = m_settings.m_currentDefaults.m_terrainSize;
                        //    break;
                }

                m_settings.m_currentDefaults.m_terrainSize = GaiaUtils.EnvironmentSizeToInt(m_settings.m_currentSize);

                if (m_settings.m_targeSizePreset != GaiaConstants.EnvironmentSizePreset.Custom)
                {
                    m_settings.m_createTerrainScenes = false;
                    m_settings.m_unloadTerrainScenes = false;
                    m_settings.m_floatingPointFix = false;
                    m_showAutoStreamSettingsBox = false;
                    m_settings.m_tilesX = 1;
                    m_settings.m_tilesZ = 1;
                }


            }
            GUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("World Size", helpEnabled);

            float offSet = 16;

            #region ADVANCED WORLD SIZE SETTINGS

            if (m_foldOutWorldSizeSettings)
            {
                int oldTilesX = m_settings.m_tilesX;
                int oldTilesZ = m_settings.m_tilesZ;
                int tilesX = 0;
                int tilesZ = 0;
                m_editorUtils.LabelField("Empty", "AdvancedWorldSize", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                    EditorGUIUtility.labelWidth = 50;
                    tilesX = m_editorUtils.IntField("X Tiles", m_settings.m_tilesX);
                    GUILayout.Space(100);
                    tilesZ = m_editorUtils.IntField("Z Tiles", m_settings.m_tilesZ);
                    EditorGUIUtility.labelWidth = 0;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                    m_editorUtils.InlineHelp("X Tiles", helpEnabled);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                    m_settings.m_currentSize = (GaiaConstants.EnvironmentSize)m_editorUtils.EnumPopup("Terrain Size", m_settings.m_currentSize);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                    m_editorUtils.InlineHelp("Terrain Size", helpEnabled);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                    if (m_settings.m_currentSize == GaiaConstants.EnvironmentSize.Is4096MetersSq ||
                   m_settings.m_currentSize == GaiaConstants.EnvironmentSize.Is8192MetersSq ||
                   m_settings.m_currentSize == GaiaConstants.EnvironmentSize.Is16384MetersSq)
                    {
                        EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("LargeTerrainWarning"), MessageType.Error);
                    }
                }
                GUILayout.EndHorizontal();

                int oldHeight = m_settings.m_currentDefaults.m_terrainHeight;
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                    m_settings.m_currentDefaults.m_terrainHeight = m_editorUtils.IntField("Terrain Height", m_settings.m_currentDefaults.m_terrainHeight);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                    m_editorUtils.InlineHelp("Terrain Height", helpEnabled);
                }
                GUILayout.EndHorizontal();

                if (oldHeight != m_settings.m_currentDefaults.m_terrainHeight)
                {
                    m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Custom;
                    if (m_settings.m_currentDefaults.m_terrainHeight <= 0)
                    {
                        m_settings.m_currentDefaults.m_terrainHeight = 1;

                    }
                }

                bool currentGUIState = GUI.enabled;
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical();
                    {
#if !GAIA_PRO_PRESENT
                        GUI.enabled = false;
#else
                        //automatic activation of create terrain scenes / unload terrain scenes at a certain world size
                        if (oldTilesX < 3 && tilesX >= 3 && tilesZ < 3 || oldTilesZ < 3 && tilesZ >= 3 && tilesX < 3)
                        {
                            m_settings.m_createTerrainScenes = true;
                            m_settings.m_unloadTerrainScenes = true;
                            m_settings.m_floatingPointFix = true;
                            m_showAutoStreamSettingsBox = true;
                        }
#endif

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                            m_settings.m_createTerrainScenes = m_editorUtils.Toggle("CreateTerrainScenes", m_settings.m_createTerrainScenes);
#if !GAIA_PRO_PRESENT
                            m_editorUtils.LabelField("GaiaProOnly");
#endif
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                            m_editorUtils.InlineHelp("CreateTerrainScenes", helpEnabled);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                            m_settings.m_unloadTerrainScenes = m_editorUtils.Toggle("UnloadTerrainScenes", m_settings.m_unloadTerrainScenes);
#if !GAIA_PRO_PRESENT
                            m_editorUtils.LabelField("GaiaProOnly");
#endif
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                            m_editorUtils.InlineHelp("UnloadTerrainScenes", helpEnabled);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                            m_settings.m_floatingPointFix = m_editorUtils.Toggle("FloatingPointFix", m_settings.m_floatingPointFix);
#if !GAIA_PRO_PRESENT
                            m_editorUtils.LabelField("GaiaProOnly");
#endif
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                            m_editorUtils.InlineHelp("FloatingPointFix", helpEnabled);
                        }
                        GUILayout.EndHorizontal();

                    }
                    GUILayout.EndVertical();
                    if (m_showAutoStreamSettingsBox)
                    {
                        EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("AutoStreamSettings"), MessageType.Info);
                    }
                }
                GUILayout.EndHorizontal();


                if (m_settings.m_createTerrainScenes || m_settings.m_unloadTerrainScenes || m_settings.m_floatingPointFix)
                {
                    m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Custom;
                }

#if !GAIA_PRO_PRESENT
                GUI.enabled = currentGUIState;
#endif
                if (tilesX != m_settings.m_tilesX || tilesZ != m_settings.m_tilesZ || m_oldTargetSize != m_settings.m_currentSize)
                {

                    m_settings.m_tilesX = tilesX;
                    m_settings.m_tilesZ = tilesZ;

                    if (m_settings.m_tilesX > 1 ||
                        m_settings.m_tilesZ > 1 ||
                        m_settings.m_currentSize == GaiaConstants.EnvironmentSize.Is8192MetersSq ||
                        m_settings.m_currentSize == GaiaConstants.EnvironmentSize.Is16384MetersSq
                        )
                    {
                        m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Custom;
                    }
                    else
                    {
                        switch (m_settings.m_currentSize)
                        {
                            case GaiaConstants.EnvironmentSize.Is256MetersSq:
                                m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Tiny;
                                break;
                            case GaiaConstants.EnvironmentSize.Is512MetersSq:
                                m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Small;
                                break;
                            case GaiaConstants.EnvironmentSize.Is1024MetersSq:
                                m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Medium;
                                break;
                            case GaiaConstants.EnvironmentSize.Is2048MetersSq:
                                m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Large;
                                break;
                            default:
                                m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Custom;
                                break;
                                //case GaiaConstants.EnvironmentSize.Is4096MetersSq:
                                //    m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.XLarge;
                                //    break;
                        }

                    }

                    EditorUtility.SetDirty(m_settings);
                }
            }
            #endregion

            int world_xDimension = m_settings.m_tilesX * m_settings.m_currentDefaults.m_terrainSize;
            int world_zDimension = m_settings.m_tilesZ * m_settings.m_currentDefaults.m_terrainSize;
            int numberOfTerrains = m_settings.m_tilesX * m_settings.m_tilesZ;

            string worldXText = String.Format("{0:0} m", world_xDimension);
            string worldZText = String.Format("{0:0} m", world_zDimension);
            if (world_xDimension > 1000 || world_zDimension > 1000)
            {
                worldXText = String.Format("{0:0.00} km", world_xDimension / 1000f);
                worldZText = String.Format("{0:0.00} km", world_zDimension / 1000f);
            }

            GUIContent worldSizeInfo = new GUIContent(m_editorUtils.GetContent("TotalWorldSize").text + String.Format(": {0} x {1}, " + m_editorUtils.GetContent("Terrains").text + ": {2}", worldXText, worldZText, numberOfTerrains));

            GUILayout.BeginHorizontal();
            {
                if (m_foldOutWorldSizeSettings)
                {
                    GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                }
                else
                {
                    GUILayout.Space(EditorGUIUtility.labelWidth + 4);
                }
                m_editorUtils.Label(worldSizeInfo, EditorStyles.boldLabel);
            }
            GUILayout.EndHorizontal();

            #endregion

            GUILayout.Space(5);

            #region TARGET QUALITY
            GUILayout.BeginHorizontal();
            {
                m_oldTargetEnv = m_settings.m_currentEnvironment;
                m_settings.m_currentEnvironment = (GaiaConstants.EnvironmentTarget)m_editorUtils.EnumPopup("Quality Header", m_settings.m_currentEnvironment);
                if (!m_foldoutTerrainResolutionSettings)
                {
                    if (m_editorUtils.Button("PlusButtonQuality", GUILayout.Width(20), GUILayout.Height(18)))
                    {
                        m_foldoutTerrainResolutionSettings = true;
                    }
                }
                else
                {
                    if (m_editorUtils.Button("MinusButtonQuality", GUILayout.Width(20), GUILayout.Height(18)))
                    {
                        m_foldoutTerrainResolutionSettings = false;
                    }
                }
            }
            GUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("Quality Header", helpEnabled);
            if (m_settings.m_currentEnvironment == GaiaConstants.EnvironmentTarget.Custom && m_oldTargetEnv != GaiaConstants.EnvironmentTarget.Custom)
            {
                //User just switched to custom -> unfold the extra options
                m_foldoutTerrainResolutionSettings = true;
            }

            //Track the changes on the resolution settings in a change check
            bool resolutionSettingsChanged = false;

            if (m_foldoutTerrainResolutionSettings)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                    m_editorUtils.Label("AdvancedQuality", EditorStyles.boldLabel);
                }
                GUILayout.EndHorizontal();


                EditorGUI.BeginChangeCheck();
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                        m_spawnDensity = m_editorUtils.FloatField("Spawn Density", Mathf.Max(0.01f, m_spawnDensity));
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                        m_editorUtils.InlineHelp("Spawn Density", helpEnabled);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                        m_heightmapResolution = (GaiaConstants.HeightmapResolution)m_editorUtils.EnumPopup("Heightmap Resolution", m_heightmapResolution);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                        m_editorUtils.InlineHelp("Heightmap Resolution", helpEnabled);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                        m_controlTextureResolution = (GaiaConstants.TerrainTextureResolution)m_editorUtils.EnumPopup("Control Texture Resolution", m_controlTextureResolution);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                        m_editorUtils.InlineHelp("Control Texture Resolution", helpEnabled);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                        m_basemapResolution = (GaiaConstants.TerrainTextureResolution)m_editorUtils.EnumPopup("Basemap Resolution", m_basemapResolution);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                        m_editorUtils.InlineHelp("Basemap Resolution", helpEnabled);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                        m_detailResolutionPerPatch = m_editorUtils.IntField("Detail Resolution Per Patch", m_detailResolutionPerPatch);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                        m_editorUtils.InlineHelp("Detail Resolution Per Patch", helpEnabled);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                        m_detailResolution = m_editorUtils.IntField("Detail Resolution", m_detailResolution);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                        m_editorUtils.InlineHelp("Detail Resolution", helpEnabled);
                    }
                    GUILayout.EndHorizontal();
                }
                if (EditorGUI.EndChangeCheck())
                {
                    m_settings.m_currentEnvironment = GaiaConstants.EnvironmentTarget.Custom;
                    resolutionSettingsChanged = true;
                }
            }

            //Evaluate the resolution settings etc. according to what the user choses in the Manager
            //we only want to execute this on initially opening the window, or when settings have changed
            //This code needs to sit outside of the foldout!
            if (m_initResSettings || resolutionSettingsChanged || m_oldTargetEnv != m_settings.m_currentEnvironment || m_oldTargetSize != m_settings.m_currentSize)
            {
                if (m_oldTargetEnv != m_settings.m_currentEnvironment)
                {
                    switch (m_settings.m_currentEnvironment)
                    {
                        case GaiaConstants.EnvironmentTarget.UltraLight:
                            m_settings.m_currentDefaults = m_settings.m_ultraLightDefaults;
                            m_settings.m_currentWaterPrefabName = m_settings.m_waterMobilePrefabName;
                            break;
                        case GaiaConstants.EnvironmentTarget.MobileAndVR:
                            m_settings.m_currentDefaults = m_settings.m_mobileDefaults;
                            m_settings.m_currentWaterPrefabName = m_settings.m_waterMobilePrefabName;
                            break;
                        case GaiaConstants.EnvironmentTarget.Desktop:
                            m_settings.m_currentDefaults = m_settings.m_desktopDefaults;
                            m_settings.m_currentWaterPrefabName = m_settings.m_waterPrefabName;
                            break;
                        case GaiaConstants.EnvironmentTarget.PowerfulDesktop:
                            m_settings.m_currentDefaults = m_settings.m_powerDesktopDefaults;
                            m_settings.m_currentWaterPrefabName = m_settings.m_waterPrefabName;
                            break;
                    }
                }

                m_settings.m_currentDefaults.m_terrainSize = GaiaUtils.EnvironmentSizeToInt(m_settings.m_currentSize);

                GaiaUtils.SetSettingsForEnvironment(m_settings, m_settings.m_currentEnvironment);

                if (m_settings.m_currentEnvironment != GaiaConstants.EnvironmentTarget.Custom)
                {
                    m_spawnDensity = m_settings.m_currentDefaults.m_spawnDensity;
                    m_heightmapResolution = (GaiaConstants.HeightmapResolution)m_settings.m_currentDefaults.m_heightmapResolution;
                    m_controlTextureResolution = (GaiaConstants.TerrainTextureResolution)m_settings.m_currentDefaults.m_controlTextureResolution;
                    m_basemapResolution = (GaiaConstants.TerrainTextureResolution)m_settings.m_currentDefaults.m_baseMapSize;
                    m_detailResolutionPerPatch = m_settings.m_currentDefaults.m_detailResolutionPerPatch;
                    m_detailResolution = m_settings.m_currentDefaults.m_detailResolution;
                }
                m_settings.m_currentDefaults.m_spawnDensity = m_spawnDensity;
                m_settings.m_currentDefaults.m_heightmapResolution = (int)m_heightmapResolution;
                m_settings.m_currentDefaults.m_controlTextureResolution = (int)m_controlTextureResolution;
                m_settings.m_currentDefaults.m_baseMapSize = (int)m_basemapResolution;
                m_detailResolutionPerPatch = Mathf.RoundToInt(Mathf.Clamp(m_detailResolutionPerPatch, 8, 128));
                m_detailResolution = Mathf.RoundToInt(Mathf.Clamp(m_detailResolution, 0, 4096));
                m_settings.m_currentDefaults.m_detailResolutionPerPatch = m_detailResolutionPerPatch;
                m_settings.m_currentDefaults.m_detailResolution = m_detailResolution;
                m_initResSettings = false;
                EditorUtility.SetDirty(m_settings);
                EditorUtility.SetDirty(m_settings.m_currentDefaults);
            }

            #endregion

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = m_settings.GetActionButtonColor();
                if (m_useWorldDesigner)
                {
                    if (m_editorUtils.Button("CreateWorldDesigner"))
                    {
#if HDPipeline
                        GaiaHDRPPipelineUtils.SetDefaultHDRPLighting(m_settings.m_pipelineProfile);
#else
                        GaiaLighting.SetDefaultAmbientLight(m_settings.m_gaiaLightingProfile);
#endif
                        GaiaUtils.GetOrCreateWorldDesigner();
                        //Important: Subscribe to this event AFTER the call to GetOrCreateWorldDesigner for correct order of operation
                        GaiaSessionManager.OnWorldCreated -= AddDefaultWorldBiomeToWorldDesigner;
                        GaiaSessionManager.OnWorldCreated += AddDefaultWorldBiomeToWorldDesigner;
                        m_terrainCreationRunning = true;
                        m_creationTabs.ActiveTabIndex = 3;

#if GAIA_PRO_PRESENT
                        if (SessionManager != null)
                        {
                            WorldOriginEditor.m_sessionManagerExits = true;
                        }
#else
                if (SessionManager != null)
                {
                    Gaia2TopPanel.m_sessionManagerExits = true;
                }
#endif

                    }
                }
                else
                {
                    if (m_editorUtils.Button("StandardTabButtonCreateTerrain"))
                    {
                        int actualTerrainCount = Gaia.TerrainHelper.GetActiveTerrainCount();
                        if (actualTerrainCount != 0)
                        {
                            EditorUtility.DisplayDialog("Terrains already created", "You already have a terrain setup in this scene. Please remove all terrains or create a new scene before creating a new world.", "OK");
                        }
                        else
                        {

                            //Switch off pro only features if activated for some reason
#if !GAIA_PRO_PRESENT
                            m_settings.m_createTerrainScenes = false;
                            m_settings.m_unloadTerrainScenes = false;
                            m_settings.m_floatingPointFix = false;
#endif
                            //No terrain yet, create everything as usual
                            //Check lighting first
#if HDPipeline
                            GaiaHDRPPipelineUtils.SetDefaultHDRPLighting(m_settings.m_pipelineProfile);
                            GaiaLighting.EnableNewSceneQuickBake(true);
#else
                            GaiaLighting.SetDefaultAmbientLight(m_settings.m_gaiaLightingProfile);
#endif


                            bool cancel = false;

                            //Abort if exporting to scenes is active and the current scene has not been saved yet - we need a valid scene filename to create subfolders for the scene files, etc.
                            if (m_settings.m_createTerrainScenes && string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
                            {
                                bool scenesSaved = false;

                                if (EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("SceneNotSavedYetTitle"), m_editorUtils.GetTextValue("SceneNotSavedYetText"), m_editorUtils.GetTextValue("SaveNow"), m_editorUtils.GetTextValue("Cancel")))
                                {
                                    string suggestedPath = GaiaDirectories.GetSessionSubFolderPath(SessionManager.m_session, true);
                                    string sceneTargetPath = EditorUtility.SaveFilePanel("Save Scene As...", suggestedPath, "New Gaia Scene", "unity");
                                    if (!string.IsNullOrEmpty(sceneTargetPath))
                                    {
                                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), GaiaDirectories.GetPathStartingAtAssetsFolder(sceneTargetPath));
                                        scenesSaved = true;
                                    }
                                    else
                                    {
                                        scenesSaved = false;
                                    }
                                }
                                else
                                {
                                    //Canceled out
                                    cancel = true;
                                }
                                //Did the user actually save the scene after the prompt?
                                if (!scenesSaved)
                                {
                                    cancel = true;
                                }
                            }
                            if (!cancel)
                            {

                                float totalSteps = 3;
                                float currentStep = 0f;
                                EditorUtility.DisplayProgressBar("Creating Terrain", "Creating Terrain", ++currentStep / totalSteps);
                                GaiaSessionManager.OnWorldCreated -= CreateToolsAfterWorld;
                                GaiaSessionManager.OnWorldCreated += CreateToolsAfterWorld;
                                m_terrainCreationRunning = true;
                                CreateTerrain(false);
                                m_creationTabs.ActiveTabIndex = 2;
                                EditorGUIUtility.ExitGUI();
                            }
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = m_defaultPanelColor;
        }

        private void DrawToolCreation(bool helpEnabled)
        {
            bool currentGUIState = GUI.enabled;

            if (Terrain.activeTerrains.Length <= 0)
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("NoTerrainWarning"), MessageType.Warning);
                GUI.enabled = false;
            }

            m_createStamper = m_editorUtils.Toggle("ToolCreateStamper", m_createStamper, helpEnabled);

            int lastBiomePresetSelection = m_biomePresetSelection;
            if (m_biomePresetSelection == int.MinValue)
            {
                m_biomePresetSelection = 0;
            }
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(-1);
                m_editorUtils.Label("BiomePreset", GUILayout.Width(EditorGUIUtility.labelWidth - 1));
                m_biomePresetSelection = EditorGUILayout.IntPopup(m_biomePresetSelection, m_allBiomePresets.Select(x => x.name).ToArray(), m_allBiomePresets.Select(x => x.ID).ToArray());

                if (lastBiomePresetSelection != m_biomePresetSelection)
                {
                    AddBiomeSpawnersForSelectedPreset();
                    //re-create the reorderable list with the new contents
                    CreateBiomePresetList();

                }

                if (m_biomePresetSelection == -999 && lastBiomePresetSelection != -999)
                {
                    //user just switched to "Custom", foldout the extended options
                    m_foldoutSpawnerSettings = true;
                }

                if (!m_foldoutSpawnerSettings)
                {
                    if (m_editorUtils.Button("PlusButtonBiomePreset", GUILayout.Width(20), GUILayout.Height(18)))
                    {
                        m_foldoutSpawnerSettings = true;
                    }
                }
                else
                {
                    if (m_editorUtils.Button("MinusButtonBiomePreset", GUILayout.Width(20), GUILayout.Height(18)))
                    {
                        m_foldoutSpawnerSettings = false;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("BiomePreset", helpEnabled);
            if (m_foldoutSpawnerSettings)
            {
                float offSet = 16;
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                    m_editorUtils.Label("DefaultBiomeSettings", EditorStyles.boldLabel);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                    Rect listRect = EditorGUILayout.GetControlRect(true, m_biomeSpawnersList.GetHeight());
                    m_biomeSpawnersList.DoList(listRect);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(EditorGUIUtility.labelWidth + offSet);
                    m_editorUtils.InlineHelp("DefaultBiomeSettings", helpEnabled);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            {
                if (m_editorUtils.Button("BackToTerrains"))
                {
                    m_creationTabs.ActiveTabIndex = 1;
                }
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = m_settings.GetActionButtonColor();
                if (m_editorUtils.Button("StandardTabButtonCreateTools"))
                {
                    BiomePresetDropdownEntry selectedPresetEntry = m_allBiomePresets.Find(x => x.ID == m_biomePresetSelection);
                    try
                    {
                        ProgressBar.Show(ProgressBarPriority.CreateSceneTools, "Creating Tools", "Creating Biome", 0, 2);
                        List<Spawner> createdSpawners = CreateBiome(selectedPresetEntry);
                        if (m_createStamper)
                        {
                            ProgressBar.Show(ProgressBarPriority.CreateSceneTools, "Creating Tools", "Creating Stamper", 1, 2);
                            GameObject stamperObj = ShowStamper(createdSpawners, m_BiomeSpawnersToCreate);
                            //Stamper stamper = stamperObj.GetComponent<Stamper>();
                            //for (int i = 0; i < stamper.m_autoSpawners.Count(); i++)
                            //{
                            //    stamper.m_autoSpawners[i].isActive = m_BiomeSpawnersToCreate[i].m_isActiveInStamper;
                            //}
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Error while creating the tools in the scene, Exception: " + ex.Message + " Stack Trace: " + ex.StackTrace);
                    }
                    finally
                    {
#if HDPipeline
                        GaiaLighting.EnableNewSceneQuickBake(false);
#endif
                        ProgressBar.Clear(ProgressBarPriority.CreateSceneTools);
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                        m_creationTabs.ActiveTabIndex = 3;
                        EditorGUIUtility.ExitGUI();
                    }
                }
            }
            GUILayout.EndHorizontal();
            GUI.backgroundColor = m_defaultPanelColor;
            GUI.enabled = currentGUIState;
        }

        private void DrawLightBaking(bool helpEnabled)
        {
            bool currentGUIState = GUI.enabled;

            if (Terrain.activeTerrains.Length <= 0)
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("NoTerrainWarning"), MessageType.Warning);
                GUI.enabled = false;
            }

            m_editorUtils.Panel("ReflectionProbes", DrawReflectionProbes, false);
            m_editorUtils.Panel("LightProbes", DrawLightProbes, false);


            if (Lightmapping.isRunning)
            {
                if (m_editorUtils.Button("Cancel Bake"))
                {
                    GaiaLighting.CancelLightmapBaking();
                }
            }
            else
            {
                GUI.backgroundColor = m_settings.GetActionButtonColor();
                EditorGUILayout.BeginHorizontal();
                if (m_editorUtils.Button("QuickBakeLighting"))
                {
                    if (GaiaUtils.CheckIfSceneProfileExists())
                    {
                        if (GaiaUtils.CheckLightingProfileIndexRange())
                        {
                            GaiaLighting.QuickBakeLighting(GaiaGlobal.Instance.SceneProfile.m_lightingProfiles[GaiaGlobal.Instance.SceneProfile.m_selectedLightingProfileValuesIndex]);
                        }
                        else
                        {
                            GaiaLighting.QuickBakeLighting();
                        }
                    }
                }

                if (m_editorUtils.Button("Bake Lighting"))
                {
                    if (EditorUtility.DisplayDialog(
                        m_editorUtils.GetTextValue("BakingLightmaps!"),
                        m_editorUtils.GetTextValue("BakingLightmapsInfo"),
                        m_editorUtils.GetTextValue("Bake"), m_editorUtils.GetTextValue("Cancel")))
                    {
                        if (GaiaUtils.CheckIfSceneProfileExists())
                        {
                            if (GaiaUtils.CheckLightingProfileIndexRange())
                            {
                                GaiaLighting.BakeLighting(GaiaGlobal.Instance.SceneProfile.m_lightingBakeMode, GaiaGlobal.Instance.SceneProfile.m_lightingProfiles[GaiaGlobal.Instance.SceneProfile.m_selectedLightingProfileValuesIndex]);
                            }
                            else
                            {
                                GaiaLighting.BakeLighting(GaiaConstants.BakeMode.Realtime);
                            }
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = m_defaultPanelColor;
                GUILayout.Space(5);

            }

            GUILayout.BeginHorizontal();
            {
                if (m_editorUtils.Button("BackToRuntime"))
                {
                    m_creationTabs.ActiveTabIndex = 3;
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
            GUI.enabled = currentGUIState;
        }

        private void DrawLightProbes(bool helpEnabled)
        {
            //Adjusting label width, otherwise some labels won't fit
            EditorGUIUtility.labelWidth = 220;
            SceneProfile sceneProfile = GaiaGlobal.Instance.SceneProfile;
            string lightProbeSpawnCount = (sceneProfile.m_reflectionProbeData.lightProbesPerRow * sceneProfile.m_reflectionProbeData.lightProbesPerRow).ToString();
            EditorGUI.BeginChangeCheck();
            m_editorUtils.Heading("LightProbeSettings");
            EditorGUI.indentLevel++;
            sceneProfile.m_reflectionProbeData.lightProbesPerRow = m_editorUtils.IntField("LightProbesPerRow", sceneProfile.m_reflectionProbeData.lightProbesPerRow, helpEnabled);
            if (sceneProfile.m_reflectionProbeData.lightProbesPerRow < 2)
            {
                EditorGUILayout.HelpBox("Please set a value of 2 or higher in the Light Probes Per Row", MessageType.Warning);
            }
            Terrain t = Terrain.activeTerrain;
            if (t != null)
            {
                int probeDist = 100;
                probeDist = (int)(t.terrainData.size.x / sceneProfile.m_reflectionProbeData.lightProbesPerRow);
                m_editorUtils.LabelField("ProbeDistanceLbl", new GUIContent(probeDist.ToString()));
                m_editorUtils.InlineHelp("ProbeDistanceLbl", helpEnabled);
            }

            m_editorUtils.LabelField("ProbesPerTerrainLbl", new GUIContent(lightProbeSpawnCount));
            m_editorUtils.InlineHelp("ProbesPerTerrainLbl", helpEnabled);

            if (m_editorUtils.Button("Generate Global Scene Light Probes"))
            {
                if (EditorUtility.DisplayDialog("Warning!", "You're about to generate light probes to cover your whole terrain. Depending on your terrain size and Probe Per Row count this could take some time. Would you like to proceed?", "Yes", "No"))
                {
                    LightProbeUtils.CreateAutomaticProbes(sceneProfile.m_reflectionProbeData);
                }
            }

            if (m_editorUtils.Button("Clear Created Light Probes"))
            {
                if (EditorUtility.DisplayDialog("Warning!", "You are about to clear all your light probes you created. Are you sure you want to proceed?", "Yes", "No"))
                {
                    LightProbeUtils.ClearCreatedLightProbes();
                }
            }

            if (LightProbeUtils.m_currentProbeCount > 0)
            {
                EditorGUILayout.HelpBox("Light Probes need to be baked with the 'Full Lightmap Bake' button below to take effect in your scene.", MessageType.Info);
            }
            EditorGUI.indentLevel--;
            EditorGUIUtility.labelWidth = 0;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sceneProfile, "Made changes");
                EditorUtility.SetDirty(sceneProfile);
            }
        }

        private void DrawReflectionProbes(bool helpEnabled)
        {
            //Adjusting label width, otherwise some labels won't fit
            EditorGUIUtility.labelWidth = 220;
            SceneProfile sceneProfile = GaiaGlobal.Instance.SceneProfile;

            string probeSpawnCount = (sceneProfile.m_reflectionProbeData.reflectionProbesPerRow * sceneProfile.m_reflectionProbeData.reflectionProbesPerRow).ToString();
            GaiaConstants.EnvironmentRenderer renderPipeline = m_settings.m_pipelineProfile.m_activePipelineInstalled;

            EditorGUI.BeginChangeCheck();

            m_editorUtils.Heading("ReflectionProbeSettings");
            EditorGUI.indentLevel++;
            sceneProfile.m_reflectionProbeData.reflectionProbeMode = (ReflectionProbeMode)m_editorUtils.EnumPopup("ReflectionProbeMode", sceneProfile.m_reflectionProbeData.reflectionProbeMode, helpEnabled);
            if (sceneProfile.m_reflectionProbeData.reflectionProbeMode != ReflectionProbeMode.Baked)
            {
                sceneProfile.m_reflectionProbeData.reflectionProbeRefresh = (GaiaConstants.ReflectionProbeRefreshModePW)m_editorUtils.EnumPopup("ReflectionProbeRefresh", sceneProfile.m_reflectionProbeData.reflectionProbeRefresh, helpEnabled);
            }
            EditorGUI.indentLevel--;
            m_editorUtils.LabelField("ProbePlacementSettings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            sceneProfile.m_reflectionProbeData.reflectionProbesPerRow = m_editorUtils.IntField("ReflectionProbesPerRow", sceneProfile.m_reflectionProbeData.reflectionProbesPerRow, helpEnabled);
            sceneProfile.m_reflectionProbeData.reflectionProbeOffset = m_editorUtils.FloatField("ReflectionProbeOffset", sceneProfile.m_reflectionProbeData.reflectionProbeOffset, helpEnabled);
            Terrain t = Terrain.activeTerrain;
            if (t != null)
            {
                int probeDist = 100;
                probeDist = (int)(t.terrainData.size.x / sceneProfile.m_reflectionProbeData.reflectionProbesPerRow);
                m_editorUtils.LabelField("ProbeDistanceLbl", new GUIContent(probeDist.ToString()));
                m_editorUtils.InlineHelp("ProbeDistanceLbl", helpEnabled);
            }
            m_editorUtils.LabelField("ProbesPerTerrainLbl", new GUIContent(probeSpawnCount));
            m_editorUtils.InlineHelp("ProbesPerTerrainLbl", helpEnabled);
            if (sceneProfile.m_reflectionProbeData.reflectionProbesPerRow < 2)
            {
                EditorGUILayout.HelpBox("Please set a value of 2 or higher in the Probes Per Row", MessageType.Warning);
            }
            EditorGUI.indentLevel--;
            m_editorUtils.LabelField("Probe Optimization Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            if (sceneProfile.m_reflectionProbeData.reflectionProbeRefresh == GaiaConstants.ReflectionProbeRefreshModePW.ViaScripting && sceneProfile.m_reflectionProbeData.reflectionProbeMode != ReflectionProbeMode.Baked)
            {
                sceneProfile.m_reflectionProbeData.reflectionProbeTimeSlicingMode = (ReflectionProbeTimeSlicingMode)m_editorUtils.EnumPopup("ReflectionProbeTimeSlicing", sceneProfile.m_reflectionProbeData.reflectionProbeTimeSlicingMode, helpEnabled);
            }
            if (renderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                sceneProfile.m_reflectionProbeData.reflectionProbeResolution = (GaiaConstants.ReflectionProbeResolution)m_editorUtils.EnumPopup("ReflectionProbeResolution", sceneProfile.m_reflectionProbeData.reflectionProbeResolution, helpEnabled);
                sceneProfile.m_reflectionProbeData.reflectionCubemapCompression = (ReflectionCubemapCompression)m_editorUtils.EnumPopup("ReflectionProbeCompression", sceneProfile.m_reflectionProbeData.reflectionCubemapCompression, helpEnabled);
            }
            sceneProfile.m_reflectionProbeData.reflectionProbeClipPlaneDistance = m_editorUtils.Slider("ReflectionProbeRenderDistance", sceneProfile.m_reflectionProbeData.reflectionProbeClipPlaneDistance, 0.1f, 10000f, helpEnabled);
            sceneProfile.m_reflectionProbeData.reflectionProbeShadowDistance = m_editorUtils.Slider("ReflectionProbeShadowDistance", sceneProfile.m_reflectionProbeData.reflectionProbeShadowDistance, 0.1f, 3000f, helpEnabled);
            sceneProfile.m_reflectionProbeData.reflectionprobeCullingMask = GaiaEditorUtils.LayerMaskField(new GUIContent(m_editorUtils.GetTextValue("ReflectionProbeCullingMask"), m_editorUtils.GetTooltip("ReflectionProbeCullingMask")), sceneProfile.m_reflectionProbeData.reflectionprobeCullingMask);
            m_editorUtils.InlineHelp("ReflectionProbeCullingMask", helpEnabled);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            if (m_editorUtils.Button("Generate Global Scene Reflection Probes"))
            {
                if (EditorUtility.DisplayDialog("Warning!", "You're about to generate reflection probes to cover your whole terrain. Depending on your terrain size and Probe Per Row count this could take some time. Would you like to proceed?", "Yes", "No"))
                {
                    ReflectionProbeEditorUtils.CreateAutomaticProbes(sceneProfile.m_reflectionProbeData, renderPipeline);
                }
            }
            if (m_editorUtils.Button("Clear Created Reflection Probes"))
            {
                if (EditorUtility.DisplayDialog("Warning!", "You are about to clear all your reflection probes you created. Are you sure you want to proceed?", "Yes", "No"))
                {
                    ReflectionProbeEditorUtils.ClearCreatedReflectionProbes();
                }
            }

            if (ReflectionProbeEditorUtils.m_currentProbeCount > 0 && renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                EditorGUILayout.HelpBox("To allow the sky to be affected in the reflection probe bake be sure to set the Volume Layer Mask to Transparent FX layer.", MessageType.Info);
            }
            EditorGUI.indentLevel--;
            EditorGUIUtility.labelWidth = 0;


            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sceneProfile, "Made changes");
                EditorUtility.SetDirty(sceneProfile);
            }
        }

        private void DrawRuntimeCreation(bool helpEnabled)
        {
            bool currentGUIState = GUI.enabled;

            //if (Terrain.activeTerrains.Length <= 0)
            //{
            //    EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("NoTerrainWarning"), MessageType.Warning);
            //    GUI.enabled = false;
            //}
            EditorGUI.BeginChangeCheck();
            bool sceneProfileExists = false;
            if (GaiaUtils.CheckIfSceneProfileExists())
            {
                sceneProfileExists = true;
            }

            m_settings.m_currentController = (GaiaConstants.EnvironmentControllerType)m_editorUtils.EnumPopup("Controller", m_settings.m_currentController, helpEnabled);
            switch (m_settings.m_currentController)
            {
                case GaiaConstants.EnvironmentControllerType.FirstPerson:
                    m_settings.m_currentPlayerPrefabName = "FPSController";
                    break;
                case GaiaConstants.EnvironmentControllerType.ThirdPerson:
                    m_settings.m_currentPlayerPrefabName = "ThirdPersonController";
                    break;
                case GaiaConstants.EnvironmentControllerType.FlyingCamera:
                    m_settings.m_currentPlayerPrefabName = "FlyCam";
                    break;
                case GaiaConstants.EnvironmentControllerType.Car:
                    m_settings.m_currentPlayerPrefabName = GaiaConstants.m_carPlayerPrefabName;
                    break;
                case GaiaConstants.EnvironmentControllerType.Custom:
                    m_settings.m_customPlayerObject = (GameObject)m_editorUtils.ObjectField("CustomPlayer", m_settings.m_customPlayerObject, typeof(GameObject), true);
                    m_settings.m_customPlayerCamera = (Camera)m_editorUtils.ObjectField("CustomCamera", m_settings.m_customPlayerCamera, typeof(Camera), true);
                    break;
                case GaiaConstants.EnvironmentControllerType.XRController:
#if GAIA_XR
                    m_settings.m_currentPlayerPrefabName = "XRController";
#else
                    EditorUtility.DisplayDialog("XR Support not enabled", "The XR Controller is a default player for Virtual / Augmented Reality projects. Please open the Setup Panel in the Gaia Manager Standard Tab to enable XR Support in order to use the XR Player Controller. Please also make sure you have the Unity XR Interaction Toolkit package installed before doing so.", "OK");
                    m_settings.m_currentController = GaiaConstants.EnvironmentControllerType.FlyingCamera;
#endif
                    break;

            }
            GaiaLightingProfile lightingProfile = m_settings.m_gaiaLightingProfile;

            //Building up a value array of incrementing ints of the size of the lighting profile values array, this array will then match the displayed string selection in the popup
            int[] lightingProfileValuesIndices = Enumerable
                                .Repeat(0, (int)((lightingProfile.m_lightingProfiles.Count() - 1) / 1) + 1)
                                .Select((tr, ti) => tr + (1 * ti))
                                .ToArray();
            string[] profileNames = lightingProfile.m_lightingProfiles.Select(x => x.m_typeOfLighting).ToArray();

            //Injecting the "None" option
            lightingProfileValuesIndices = GaiaUtils.AddElementToArray(lightingProfileValuesIndices, -99);
            profileNames = GaiaUtils.AddElementToArray(profileNames, "None");
            GUILayout.BeginHorizontal();
            {
                m_editorUtils.Label("Skies", GUILayout.Width(EditorGUIUtility.labelWidth - 2));
                lightingProfile.m_selectedLightingProfileValuesIndex = EditorGUILayout.IntPopup(lightingProfile.m_selectedLightingProfileValuesIndex, profileNames, lightingProfileValuesIndices);
            }
            GUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("Skies", helpEnabled);


#if !GAIA_PRO_PRESENT
            if (lightingProfile.m_selectedLightingProfileValuesIndex > 0 && lightingProfile.m_selectedLightingProfileValuesIndex < lightingProfile.m_lightingProfiles.Count)
            {
                if (lightingProfile.m_lightingProfiles[lightingProfile.m_selectedLightingProfileValuesIndex].m_profileType == GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                {
                    EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("GaiaProLightingProfileInfo"), MessageType.Info);
                }
            }
#endif
            EditorGUI.indentLevel++;
            if (lightingProfile.m_selectedLightingProfileValuesIndex != -99)
            {
#if UNITY_POST_PROCESSING_STACK_V2
                m_settings.m_enablePostProcessing = m_editorUtils.Toggle("PostProcessing", m_settings.m_enablePostProcessing, helpEnabled);
#endif
            }
            EditorGUI.indentLevel--;

            //Water
            if (newProfileListIndex > m_settings.m_gaiaWaterProfile.m_waterProfiles.Count)
            {
                newProfileListIndex = 0;
            }
            m_profileList.Clear();
            if (m_settings.m_gaiaWaterProfile.m_waterProfiles.Count > 0)
            {
                foreach (GaiaWaterProfileValues profile in m_settings.m_gaiaWaterProfile.m_waterProfiles)
                {
                    m_profileList.Add(profile.m_typeOfWater);
                }
            }
            m_profileList.Add("None");
            newProfileListIndex = EditorGUILayout.Popup(m_editorUtils.GetContent("Water"), newProfileListIndex, m_profileList.ToArray());
            m_editorUtils.InlineHelp("Water", helpEnabled);

            if (m_settings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex != newProfileListIndex)
            {
                m_settings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex = newProfileListIndex;
            }
            if (m_profileList[newProfileListIndex] != "None")
            {
                EditorGUI.indentLevel++;
                m_settings.m_enableUnderwaterEffects = m_editorUtils.Toggle("UnderwaterEffects", m_settings.m_enableUnderwaterEffects, helpEnabled);
                EditorGUI.indentLevel--;
            }

            //Wind
            m_settings.m_createWind = m_editorUtils.Toggle("Wind", m_settings.m_createWind, helpEnabled);
            if (m_settings.m_createWind)
            {
                EditorGUI.indentLevel++;
                m_settings.m_windType = (GaiaConstants.GaiaGlobalWindType)m_editorUtils.EnumPopup("WindType", m_settings.m_windType, helpEnabled);
                EditorGUI.indentLevel--;
            }
            m_settings.m_enableAmbientAudio = m_editorUtils.Toggle("AmbientAudio", m_settings.m_enableAmbientAudio, helpEnabled);
            m_settings.m_createScreenShotter = m_editorUtils.Toggle("Screenshotter", m_settings.m_createScreenShotter, helpEnabled);
            m_settings.m_enableLocationManager = m_editorUtils.Toggle("LocationManager", m_settings.m_enableLocationManager, helpEnabled);
            if (GaiaUtils.HasDynamicLoadedTerrains())
            {
                m_settings.m_enableLoadingScreen = m_editorUtils.Toggle("LoadingScreen", m_settings.m_enableLoadingScreen, helpEnabled);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(m_settings.m_gaiaLightingProfile);
                m_settings.m_gaiaLightingProfile.m_enableAmbientAudio = m_settings.m_enableAmbientAudio;
                m_settings.m_gaiaLightingProfile.m_enablePostProcessing = m_settings.m_enablePostProcessing;

                EditorUtility.SetDirty(m_settings.m_gaiaWaterProfile);
                m_settings.m_gaiaWaterProfile.m_supportUnderwaterEffects = m_settings.m_enableUnderwaterEffects;
                if (sceneProfileExists)
                {
                    GaiaGlobal.Instance.SceneProfile.m_enableAmbientAudio = m_settings.m_enableAmbientAudio;
                    GaiaGlobal.Instance.SceneProfile.m_enablePostProcessing = m_settings.m_enablePostProcessing;
                    GaiaGlobal.Instance.SceneProfile.m_selectedWaterProfileValuesIndex = m_settings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex;
                    GaiaGlobal.Instance.SceneProfile.m_selectedLightingProfileValuesIndex = m_settings.m_gaiaLightingProfile.m_selectedLightingProfileValuesIndex;
                }
            }

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            {
                if (m_editorUtils.Button("BackToTools"))
                {
                    m_creationTabs.ActiveTabIndex = 2;
                }
                GUILayout.FlexibleSpace();

                string buttonText = "CreateRuntime";
                if (m_runtimeCreated)
                {
                    buttonText = "UpdateRuntime";
                }
                GUI.backgroundColor = m_settings.GetActionButtonColor();
                if (m_editorUtils.Button(buttonText))
                {
                    //Creates gaia water, lighting, player etc.
                    CreateGaiaExtras(m_settings, m_settings.m_gaiaLightingProfile, m_profileList, newProfileListIndex);
                    m_runtimeCreated = true;
                }
                GUI.backgroundColor = m_defaultPanelColor;
            }
            GUILayout.EndHorizontal();
            GUI.enabled = currentGUIState;
        }

        private void AddDefaultWorldBiomeToWorldDesigner()
        {
            try
            {
                TerrainLoaderManager.Instance.SwitchToWorldMap();
                GameObject worldMapObj = GameObject.Find(GaiaConstants.worldDesignerObject);
                if (worldMapObj != null)
                {
                    Selection.activeObject = worldMapObj;
                    worldMapObj.GetComponent<WorldMap>().LookAtWorldMap();

                    BiomePresetDropdownEntry selectedPresetEntry = m_allBiomePresets.Find(x => x.ID == m_biomePresetSelection);
                    if (selectedPresetEntry.name != "Custom")
                    {
                        Spawner spawner = worldMapObj.GetComponent<Spawner>();
                        SpawnRule rule = new SpawnRule();
                        rule.m_resourceType = GaiaConstants.SpawnerResourceType.WorldBiomeMask;
                        spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes = GaiaUtils.AddElementToArray(spawner.m_settings.m_resources.m_worldBiomeMaskPrototypes, new ResourceProtoWorldBiomeMask() { m_name = selectedPresetEntry.name, m_biomePreset = selectedPresetEntry.biomePreset });
                        rule.m_name = selectedPresetEntry.name;
                        rule.m_resourceIdx = 0;
                        //Fold out by default
                        rule.m_isFoldedOut = true;
                        spawner.m_settings.m_spawnerRules.Add(rule);
                    }
                }

                //Adjust the scene view so you can see the world designer
                if (SceneView.lastActiveSceneView != null)
                {
                    if (m_settings != null)
                    {
                        SceneView.lastActiveSceneView.LookAtDirect(new Vector3(0f, 300f, -1f * (m_settings.m_currentDefaults.m_terrainSize * m_settings.m_tilesX / 2f)), Quaternion.Euler(30f, 0f, 0f));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error while adding the default biome after world map creation. Message: " + e.Message + " Stack Trace: " + e.StackTrace);
            }
            finally
            {
                m_terrainCreationRunning = false;
                GaiaSessionManager.OnWorldCreated -= AddDefaultWorldBiomeToWorldDesigner;
            }
        }


        //This method is subscribed to the OnWorldCreatedEvent in the Session Manager and creates the tools after the world creation in a coroutine has been finished.
        void CreateToolsAfterWorld()
        {
            try
            {
                //If no player exists yet, add a basic Layer culling script to the main camera, and apply settings to the scene camera.
                if (GaiaUtils.GetPlayerObject(false) == null)
                {
                    Camera cam = Camera.main;

                    if (cam != null)
                    {
                        if (cam.gameObject.GetComponent<SimpleCameraLayerCulling>() == null)
                        {
                            SimpleCameraLayerCulling sclc = cam.gameObject.AddComponent<SimpleCameraLayerCulling>();
                            sclc.m_applyToGameCamera = true;
                            sclc.m_applyToSceneCamera = true;
                            sclc.Initialize();
                            foreach (var sceneCamera in SceneView.GetAllSceneCameras())
                            {
                                sceneCamera.layerCullDistances = sclc.m_profile.m_layerDistances;
                            }
                        }
                    }
                }

                //Create the spawners
                //Check if there already exist a fitting biome Game Object to group our spawners under
                //BiomePresetDropdownEntry selectedPresetEntry = m_allBiomePresets.Find(x => x.ID == m_biomePresetSelection);

                //ProgressBar.Show(ProgressBarPriority.CreateSceneTools, "Creating Tools", "Creating Biome", 0, 2);
                //List<Spawner> createdSpawners = CreateBiome(selectedPresetEntry);
                //ProgressBar.Show(ProgressBarPriority.CreateSceneTools, "Creating Tools", "Creating Stamper", 1, 2);
                //GameObject stamperObj = ShowStamper(createdSpawners);
                //Stamper stamper = stamperObj.GetComponent<Stamper>();
                //for (int i = 0; i < stamper.m_autoSpawners.Count(); i++)
                //{
                //    stamper.m_autoSpawners[i].isActive = m_BiomeSpawnersToCreate[i].m_isActiveInStamper;
                //}

                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            catch (Exception e)
            {
                Debug.LogError("Error while creating tools after terrain creation. Message: " + e.Message + " Stack Trace: " + e.StackTrace);
            }
            finally
            {
                ProgressBar.Clear(ProgressBarPriority.CreateSceneTools);
                m_terrainCreationRunning = false;
                GaiaSessionManager.OnWorldCreated -= CreateToolsAfterWorld;
            }
        }

        /// <summary>
        /// Draw the detailed editor
        /// </summary>
        void AdvancedTab()
        {
#if !UNITY_2019_3_OR_NEWER
            EditorGUILayout.HelpBox(Application.unityVersion + " is not supported in Gaia, please use 2019.3+.", MessageType.Error);
            GUI.enabled = false;
#endif

            EditorGUI.indentLevel++;

            GUI.enabled = m_enableGUI;

            GUILayout.Space(5f);
            m_editorUtils.Panel("SystemInfoSettings", SystemInfoSettingsEnabled, false);
            m_editorUtils.Panel("PanelTerrain", AdvancedPanelTerrain, false);
            m_editorUtils.Panel("PanelTools", AdvancedPanelTools, false);
            m_editorUtils.Panel("PanelRuntime", AdvancedPanelRuntime, false);

            EditorGUILayout.Space();
            m_editorUtils.Label("AdvancedTabIntro");

            EditorGUI.indentLevel++;
        }



        private void AdvancedPanelRuntime(bool helpEnabled)
        {
            if (m_editorUtils.ButtonAutoIndent("Add Character"))
            {

                Selection.activeGameObject = GaiaSceneManagement.CreatePlayer(m_settings);

                //#if GAIA_PRESENT
                //                    GameObject underwaterFX = GameObject.Find("Directional Light");
                //                    GaiaReflectionProbeUpdate theProbeUpdater = FindObjectOfType<GaiaReflectionProbeUpdate>();
                //                    GaiaUnderWaterEffects effectsSettings = underwaterFX.GetComponent<GaiaUnderWaterEffects>();
                //                    if (theProbeUpdater != null && effectsSettings != null)
                //                    {
                //#if UNITY_EDITOR
                //                        effectsSettings.player = effectsSettings.GetThePlayer();
                //#endif
                //                    }
                //#endif
            }
            if (m_editorUtils.ButtonAutoIndent("Add Screen Shotter"))
            {
                Selection.activeGameObject = CreateScreenShotter(m_settings);
            }
            if (m_editorUtils.ButtonAutoIndent("Add Wind Zone"))
            {
                Selection.activeGameObject = CreateWindZone(m_settings);
            }
            if (m_editorUtils.ButtonAutoIndent("Add Water"))
            {
                Selection.activeGameObject = CreateWater(m_settings);
            }
        }

        private void AdvancedPanelTools(bool helpEnabled)
        {
            m_editorUtils.Heading("BiomesAndSpawners");
            GUILayout.BeginHorizontal();
            {
                m_advancedTabFoldoutBiomes = EditorGUILayout.Foldout(m_advancedTabFoldoutBiomes, m_editorUtils.GetContent("AdvancedFoldoutAddBiomes"));
                GUILayout.FlexibleSpace();
                if (m_editorUtils.Button("CreateNewBiomeButton", GUILayout.Width(200)))
                {
                    BiomePreset newPreset = ScriptableObject.CreateInstance<BiomePreset>();
                    //The term "Biome" will automatically be added
                    newPreset.name = "Custom";
                    BiomeController biomeController = newPreset.CreateBiome(false);
                    Selection.activeObject = biomeController;
                    EditorGUIUtility.PingObject(Selection.activeObject);
                }
                //Some extra pixels to keep buttons aligned with the tool buttons below
                GUILayout.Space(5);
            }
            GUILayout.EndHorizontal();
            if (m_advancedTabFoldoutBiomes)
            {
                if (m_allBiomePresets.Exists(x => x.biomePreset == null && x.ID != -999))
                {
                    CreateAdvancedTabBiomesList();
                }

                Rect listRect = EditorGUILayout.GetControlRect(true, m_advancedTabBiomesList.GetHeight());
                m_advancedTabBiomesList.DoList(listRect);
            }
            GUILayout.BeginHorizontal();
            {
                m_advancedTabFoldoutSpawners = EditorGUILayout.Foldout(m_advancedTabFoldoutSpawners, m_editorUtils.GetContent("AdvancedFoldoutAddSpawners"));
                GUILayout.FlexibleSpace();
                if (m_editorUtils.Button("CreateNewSpawnerButton", GUILayout.Width(200)))
                {
                    GameObject spawnerObj = new GameObject("New Spawner");
                    Spawner spawner = spawnerObj.AddComponent<Spawner>();
                    spawner.m_createdFromGaiaManager = true;
                    spawner.FitToAllTerrains();
                    Selection.activeGameObject = spawnerObj;
                }
                //Some extra pixels to keep buttons aligned with the tool buttons below
                GUILayout.Space(5);
            }
            GUILayout.EndHorizontal();
            if (m_advancedTabFoldoutSpawners)
            {
                if (m_advancedTabAllSpawners.Exists(x => x.m_spawnerSettings == null))
                {
                    CreateAdvancedTabSpawnersList();
                }
                Rect listRect = EditorGUILayout.GetControlRect(true, m_advancedTabSpawnersList.GetHeight());
                m_advancedTabSpawnersList.DoList(listRect);
            }
            m_editorUtils.Heading("CreateShowTools");
            if (m_editorUtils.ButtonAutoIndent("Gaia 1 Stamp Converter"))
            {
                ShowGaiaStampConverter();
            }

            if (m_editorUtils.ButtonAutoIndent("Location Manager"))
            {
                LocationManagerEditor.ShowLocationManager();
            }

#if !GAIA_PRO_PRESENT
            bool currentGUIState = GUI.enabled;
            GUI.enabled = false;
#endif
            if (m_editorUtils.ButtonAutoIndent("Mask Map Exporter"))
            {
                ShowMaskMapExporter();
            }
#if !GAIA_PRO_PRESENT
            GUI.enabled = currentGUIState;
#endif

            if (m_editorUtils.ButtonAutoIndent("Resource Helper"))
            {
                GaiaEditorUtils.ShowResourceHelperWindow(GaiaResourceHelperOperation.CopyResources, position.position + new Vector2(50, 50));
            }

            if (m_editorUtils.ButtonAutoIndent("Scanner"))
            {
                Selection.activeGameObject = Scanner.CreateScanner();
                if (Selection.activeGameObject != null)
                {
                    SceneView.lastActiveSceneView.LookAt(Selection.activeGameObject.transform.position);
                }
            }

            if (m_editorUtils.ButtonAutoIndent("Session Manager"))
            {
                ShowSessionManager();
            }

            if (m_editorUtils.ButtonAutoIndent("Stamper"))
            {
                ShowStamper();
            }

            if (m_editorUtils.ButtonAutoIndent("Terrain Mesh Export"))
            {
                ShowTerrainObjExporter();
            }

        }

        private void AdvancedPanelTerrain(bool helpEnabled)
        {
            if (m_editorUtils.ButtonAutoIndent("Create Terrain"))
            {
                int actualTerrainCount = Gaia.TerrainHelper.GetActiveTerrainCount();
                if (actualTerrainCount != 0)
                {
                    EditorUtility.DisplayDialog("Terrains already created", "You already have a terrain setup in this scene. Please remove all terrains or create a new scene before creating a new world.", "OK");
                }
                else
                {
                    CreateTerrain(false);
                }
            }
            if (m_editorUtils.ButtonAutoIndent("Create World Map Editor"))
            {
                GameObject worldMapObj = GaiaUtils.GetOrCreateWorldDesigner();
                Selection.activeObject = worldMapObj;
                worldMapObj.GetComponent<WorldMap>().LookAtWorldMap();
                WorldMap.ShowWorldMapStampSpawner();
            }
        }


        /// <summary>
        /// Draw the extension editor
        /// </summary>
        void ExtensionsTab()
        {
#if !UNITY_2019_1_OR_NEWER

            EditorGUILayout.HelpBox(Application.unityVersion + " is not supported by Gaia, please use 2019.3+.", MessageType.Error);
            GUI.enabled = false;
#endif

            m_editorUtils.Tabs(m_extensionsTabs);

        }

        private void InstalledExtensionsTab()
        {
            GUILayout.Space(5f);

            //And scan if something has changed
            if (m_needsScan)
            {
                m_extensionMgr.ScanForExtensions();
                if (m_extensionMgr.GetInstalledExtensionCount() != 0)
                {
                    m_needsScan = false;
                }
            }

            int methodIdx = 0;
            string cmdName;
            string currFoldoutName = "";
            string prevFoldoutName = "";
            MethodInfo command;
            string[] cmdBreakOut = new string[0];
            List<GaiaCompatiblePackage> packages;
            List<GaiaCompatiblePublisher> publishers = m_extensionMgr.GetPublishers();

            foreach (GaiaCompatiblePublisher publisher in publishers)
            {
                if (publisher.InstalledPackages() > 0)
                {
                    if (publisher.m_installedFoldedOut = m_editorUtils.Foldout(publisher.m_installedFoldedOut, new GUIContent(publisher.m_publisherName)))
                    {
                        EditorGUI.indentLevel++;

                        packages = publisher.GetPackages();
                        foreach (GaiaCompatiblePackage package in packages)
                        {
                            if (package.m_isInstalled)
                            {
                                if (package.m_installedFoldedOut = m_editorUtils.Foldout(package.m_installedFoldedOut, new GUIContent(package.m_packageName)))
                                {
                                    EditorGUI.indentLevel++;
                                    methodIdx = 0;
                                    //Now loop thru and process
                                    while (methodIdx < package.m_methods.Count)
                                    {
                                        command = package.m_methods[methodIdx];
                                        cmdBreakOut = command.Name.Split('_');

                                        //Ignore if we are not a valid thing
                                        if ((cmdBreakOut.GetLength(0) != 2 && cmdBreakOut.GetLength(0) != 3) || cmdBreakOut[0] != "GX")
                                        {
                                            methodIdx++;
                                            continue;
                                        }

                                        //Get foldout and command name
                                        if (cmdBreakOut.GetLength(0) == 2)
                                        {
                                            currFoldoutName = "";
                                        }
                                        else
                                        {
                                            currFoldoutName = Regex.Replace(cmdBreakOut[1], "(\\B[A-Z])", " $1");
                                        }
                                        cmdName = Regex.Replace(cmdBreakOut[cmdBreakOut.GetLength(0) - 1], "(\\B[A-Z])", " $1");

                                        if (currFoldoutName == "")
                                        {
                                            methodIdx++;
                                            if (m_editorUtils.ButtonAutoIndent(new GUIContent(cmdName)))
                                            {
                                                command.Invoke(null, null);
                                            }
                                        }
                                        else
                                        {
                                            prevFoldoutName = currFoldoutName;

                                            //Make sure we have it in our dictionary
                                            if (!package.m_methodGroupFoldouts.ContainsKey(currFoldoutName))
                                            {
                                                package.m_methodGroupFoldouts.Add(currFoldoutName, false);
                                            }

                                            if (package.m_methodGroupFoldouts[currFoldoutName] = m_editorUtils.Foldout(package.m_methodGroupFoldouts[currFoldoutName], new GUIContent(currFoldoutName)))
                                            {
                                                EditorGUI.indentLevel++;

                                                while (methodIdx < package.m_methods.Count && currFoldoutName == prevFoldoutName)
                                                {
                                                    command = package.m_methods[methodIdx];
                                                    cmdBreakOut = command.Name.Split('_');

                                                    //Drop out if we are not a valid thing
                                                    if ((cmdBreakOut.GetLength(0) != 2 && cmdBreakOut.GetLength(0) != 3) || cmdBreakOut[0] != "GX")
                                                    {
                                                        methodIdx++;
                                                        continue;
                                                    }

                                                    //Get foldout and command name
                                                    if (cmdBreakOut.GetLength(0) == 2)
                                                    {
                                                        currFoldoutName = "";
                                                    }
                                                    else
                                                    {
                                                        currFoldoutName = Regex.Replace(cmdBreakOut[1], "(\\B[A-Z])", " $1");
                                                    }
                                                    cmdName = Regex.Replace(cmdBreakOut[cmdBreakOut.GetLength(0) - 1], "(\\B[A-Z])", " $1");

                                                    if (currFoldoutName != prevFoldoutName)
                                                    {
                                                        continue;
                                                    }

                                                    if (m_editorUtils.ButtonAutoIndent(new GUIContent(cmdName)))
                                                    {
                                                        command.Invoke(null, null);
                                                    }

                                                    methodIdx++;
                                                }

                                                EditorGUI.indentLevel--;
                                            }
                                            else
                                            {
                                                while (methodIdx < package.m_methods.Count && currFoldoutName == prevFoldoutName)
                                                {
                                                    command = package.m_methods[methodIdx];
                                                    cmdBreakOut = command.Name.Split('_');

                                                    //Drop out if we are not a valid thing
                                                    if ((cmdBreakOut.GetLength(0) != 2 && cmdBreakOut.GetLength(0) != 3) || cmdBreakOut[0] != "GX")
                                                    {
                                                        methodIdx++;
                                                        continue;
                                                    }

                                                    //Get foldout and command name
                                                    if (cmdBreakOut.GetLength(0) == 2)
                                                    {
                                                        currFoldoutName = "";
                                                    }
                                                    else
                                                    {
                                                        currFoldoutName = Regex.Replace(cmdBreakOut[1], "(\\B[A-Z])", " $1");
                                                    }
                                                    cmdName = Regex.Replace(cmdBreakOut[cmdBreakOut.GetLength(0) - 1], "(\\B[A-Z])", " $1");

                                                    if (currFoldoutName != prevFoldoutName)
                                                    {
                                                        continue;
                                                    }

                                                    methodIdx++;
                                                }
                                            }
                                        }
                                    }

                                    /*
                                    foreach (MethodInfo command in package.m_methods)
                                    {
                                        cmdBreakOut = command.Name.Split('_');

                                        if ((cmdBreakOut.GetLength(0) == 2 || cmdBreakOut.GetLength(0) == 3) && cmdBreakOut[0] == "GX")
                                        {
                                            if (cmdBreakOut.GetLength(0) == 2)
                                            {
                                                currFoldoutName = "";
                                            }
                                            else
                                            {
                                                currFoldoutName = cmdBreakOut[1];
                                                Debug.Log(currFoldoutName);
                                            }

                                            cmdName = Regex.Replace(cmdBreakOut[cmdBreakOut.GetLength(0) - 1], "(\\B[A-Z])", " $1");
                                            if (m_editorUtils.ButtonAutoIndent(new GUIContent(cmdName)))
                                            {
                                                command.Invoke(null, null);
                                            }
                                        }
                                    }
                                        */

                                    EditorGUI.indentLevel--;
                                }
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        void TutorialsAndSupportTab()
        {
            GUILayout.Space(5f);

            EditorGUI.indentLevel++;


            if (m_settings.m_hideHeroMessage)
            {
                if (m_editorUtils.ClickableHeadingNonLocalized(m_settings.m_latestNewsTitle))
                {
                    Application.OpenURL(m_settings.m_latestNewsUrl);
                }

                m_editorUtils.TextNonLocalized(m_settings.m_latestNewsBody);
                GUILayout.Space(5f);
            }

            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Tutorials"), m_linkStyle))
            {
                Application.OpenURL("https://www.procedural-worlds.com/support/tutorials");
            }
            m_editorUtils.Text("Tutorials Text");
            GUILayout.Space(5f);

            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Knowledge Base"), m_linkStyle))
            {
                Application.OpenURL("https://proceduralworlds.freshdesk.com/support/solutions");
            }
            m_editorUtils.Text("Knowledge Base Text");
            GUILayout.Space(5f);

            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Open Documentation Folder"), m_linkStyle))
            {
                UnityEngine.Object guide = (UnityEngine.Object)GaiaUtils.GetAsset("Quick Start.pdf", typeof(UnityEngine.Object));
                if (guide != null)
                {
                    EditorGUIUtility.PingObject(guide);
                }
            }
            m_editorUtils.Text("Quick Start Text");
            GUILayout.Space(5f);

            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Join Our Community"), m_linkStyle))
            {
                Application.OpenURL("https://discord.gg/TggjQNN");
            }
            m_editorUtils.Text("Whether you need an answer now or feel like a chat our friendly discord community is a great place to learn!");
            GUILayout.Space(5f);

            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Ticketed Support"), m_linkStyle))
            {
                Application.OpenURL("https://proceduralworlds.freshdesk.com/support/home");
            }
            m_editorUtils.Text("Don't let your question get lost in the noise. All ticketed requests are answered, and usually within 48 hours.");
            GUILayout.Space(5f);

            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Help us Grow - Rate & Review!"), m_linkStyle))
            {
                Application.OpenURL("https://assetstore.unity.com/publishers/15277");
            }
            m_editorUtils.Text("Quality products are a huge investment to create & support. Please take a moment to show your appreciation by leaving a rating & review.");
            GUILayout.Space(5f);

            if (m_settings.m_hideHeroMessage)
            {
                if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Show Hero Message"), m_linkStyle))
                {
                    m_settings.m_hideHeroMessage = false;
                    EditorUtility.SetDirty(m_settings);
                }
                m_editorUtils.Text("Show latest news and hero messages in Gaia.");
                GUILayout.Space(5f);
            }
            EditorGUI.indentLevel--;
        }

        void MoreOnProceduralWorldsTab()
        {
            GUILayout.Space(5f);

            EditorGUI.indentLevel++;
            m_editorUtils.Text("Super charge your development with our amazing partners & extensions.");
            GUILayout.Space(5f);

            if (m_settings.m_hideHeroMessage)
            {
                if (ClickableHeaderCustomStyle(m_editorUtils.GetContent(m_settings.m_latestNewsTitle), m_linkStyle))
                {
                    Application.OpenURL(m_settings.m_latestNewsUrl);
                }

                m_editorUtils.TextNonLocalized(m_settings.m_latestNewsBody);
                GUILayout.Space(5f);
            }

            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Our Partners"), m_linkStyle))
            {
                Application.OpenURL("https://www.procedural-worlds.com/about/our-partners/");
            }
            m_editorUtils.Text("The content included with Gaia is an awesome starting point for your game, but that's just the tip of the iceberg. Learn more about how these talented publishers can help you to create amazing environments in Unity.");
            GUILayout.Space(5f);

            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Gaia eXtensions (GX)"), m_linkStyle))
            {
                Application.OpenURL("https://proceduralworlds.freshdesk.com/support/solutions/articles/33000252356-creating-gaia-extensions-for-gaia-2-gaia-pro");
            }
            m_editorUtils.Text("Gaia eXtensions accelerate and simplify your development by automating asset setup in your scene. Check out the quality assets we have integrated for you!");
            GUILayout.Space(5f);

            if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Help Us to Grow - Spread The Word!"), m_linkStyle))
            {
                Application.OpenURL("https://www.facebook.com/proceduralworlds/");
            }
            m_editorUtils.Text("Get regular news updates and help us to grow by liking and sharing our Facebook page!");
            GUILayout.Space(5f);

            if (m_settings.m_hideHeroMessage)
            {
                if (ClickableHeaderCustomStyle(m_editorUtils.GetContent("Show Hero Message"), m_linkStyle))
                {
                    m_settings.m_hideHeroMessage = false;
                    EditorUtility.SetDirty(m_settings);
                }
                m_editorUtils.Text("Show latest news and hero messages in Gaia.");
                GUILayout.Space(5f);
            }
            EditorGUI.indentLevel--;
        }
        #endregion


        void OnInspectorUpdate()
        {
            if (!m_statusCheckPerformed)
            {
                GaiaManagerStatusCheck();
            }
        }


        #region On GUI
        void OnGUI()
        {
            m_editorUtils.Initialize(); // Do not remove this!

            //Set up the box style
            if (m_boxStyle == null)
            {
                m_boxStyle = new GUIStyle(GUI.skin.box);
                m_boxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                m_boxStyle.fontStyle = FontStyle.Bold;
                m_boxStyle.alignment = TextAnchor.UpperLeft;
            }

            //Setup the wrap style
            if (m_wrapStyle == null)
            {
                m_wrapStyle = new GUIStyle(GUI.skin.label);
                m_wrapStyle.fontStyle = FontStyle.Normal;
                m_wrapStyle.wordWrap = true;
            }

            if (m_bodyStyle == null)
            {
                m_bodyStyle = new GUIStyle(GUI.skin.label);
                m_bodyStyle.fontStyle = FontStyle.Normal;
                m_bodyStyle.wordWrap = true;
            }

            if (m_titleStyle == null)
            {
                m_titleStyle = new GUIStyle(m_bodyStyle);
                m_titleStyle.fontStyle = FontStyle.Bold;
                m_titleStyle.fontSize = 20;
            }

            if (m_headingStyle == null)
            {
                m_headingStyle = new GUIStyle(m_bodyStyle);
                m_headingStyle.fontStyle = FontStyle.Bold;
            }

            if (m_linkStyle == null)
            {
                m_linkStyle = new GUIStyle(m_bodyStyle);
                m_linkStyle.wordWrap = false;
                m_linkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);
                m_linkStyle.stretchWidth = false;
            }

            //Check if we are currently creating new terrains in a coroutine - need to lock the GUI then
            if (m_terrainCreationRunning)
            {
                GUI.enabled = false;
            }

            //Check for state of compiler
            if (EditorApplication.isCompiling)
            {
                m_needsScan = true;
            }


#if GAIA_PRO_PRESENT
            m_editorUtils.GUIHeader(true, "", "", "Gaia Pro");
            m_editorUtils.GUINewsHeader(true, new URLParameters() { m_product = "Gaia Pro" });
#else
            m_editorUtils.GUIHeader();
            m_editorUtils.GUINewsHeader(true, new URLParameters() { m_product = "Gaia"});
#endif

            GUILayout.Space(4);

            m_editorUtils.Tabs(m_mainTabs);

#if GAIA_PRO_PRESENT
            m_editorUtils.GUINewsFooter(false, new URLParameters() { m_product = "Gaia Pro" });
#else
            m_editorUtils.GUINewsFooter(false, new URLParameters() { m_product = "Gaia"});
#endif

            if (m_settings.m_pipelineProfile.m_pipelineSwitchUpdates)
            {
                EditorApplication.update -= EditorPipelineUpdate;
                EditorApplication.update += EditorPipelineUpdate;
            }
            else
            {
                EditorApplication.update -= EditorPipelineUpdate;
            }

        }

        //        private void NewWorldSettings(bool helpEnabled)
        //        {
        //            m_editorUtils.InlineHelp("World Size", helpEnabled);
        //            Rect rect = EditorGUILayout.GetControlRect();

        //            float lineHeight = EditorGUIUtility.singleLineHeight + 3;
        //            Rect labelRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, EditorGUIUtility.labelWidth, lineHeight);
        //            Rect fieldRect = new Rect(labelRect.x + labelRect.width, rect.y, (rect.width - EditorGUIUtility.labelWidth - labelRect.width), lineHeight);

        //            //World size settings

        //            EditorGUI.LabelField(new Rect(labelRect.x - EditorGUIUtility.labelWidth, labelRect.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("World Size").text, m_editorUtils.Styles.heading);
        //            m_oldTargetSize = m_settings.m_currentSize;
        //            GaiaConstants.EnvironmentSizePreset oldTargetSizePreset = m_settings.m_targeSizePreset;
        //            m_settings.m_targeSizePreset = (GaiaConstants.EnvironmentSizePreset)EditorGUI.EnumPopup(new Rect(labelRect.x, labelRect.y, labelRect.width + fieldRect.width - 23f, labelRect.height), m_settings.m_targeSizePreset);

        //            if (m_settings.m_targeSizePreset == GaiaConstants.EnvironmentSizePreset.Custom && oldTargetSizePreset != GaiaConstants.EnvironmentSizePreset.Custom)
        //            {
        //                //User just switched to custom -> unfold the extra options
        //                m_foldOutWorldSizeSettings = true;
        //            }

        //            GUIContent btnState = new GUIContent("+");
        //            if (m_foldOutWorldSizeSettings)
        //            {
        //                btnState = new GUIContent("-");
        //            }
        //            if (GUI.Button(new Rect(labelRect.x + labelRect.width + fieldRect.width - 20f, labelRect.y, 20f, labelRect.height - 3f), btnState))
        //            {
        //                m_foldOutWorldSizeSettings = !m_foldOutWorldSizeSettings;
        //            }

        //            switch (m_settings.m_targeSizePreset)
        //            {
        //                case GaiaConstants.EnvironmentSizePreset.Tiny:
        //                    m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is256MetersSq;
        //                    m_settings.m_currentDefaults.m_terrainSize = GaiaUtils.EnvironmentSizeToInt(m_settings.m_currentSize);
        //                    m_settings.m_currentDefaults.m_terrainHeight = m_settings.m_currentDefaults.m_terrainSize;
        //                    break;
        //                case GaiaConstants.EnvironmentSizePreset.Small:
        //                    m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is512MetersSq;
        //                    m_settings.m_currentDefaults.m_terrainHeight = m_settings.m_currentDefaults.m_terrainSize;
        //                    break;
        //                case GaiaConstants.EnvironmentSizePreset.Medium:
        //                    m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is1024MetersSq;
        //                    m_settings.m_currentDefaults.m_terrainHeight = m_settings.m_currentDefaults.m_terrainSize;
        //                    break;
        //                case GaiaConstants.EnvironmentSizePreset.Large:
        //                    m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is2048MetersSq;
        //                    m_settings.m_currentDefaults.m_terrainHeight = m_settings.m_currentDefaults.m_terrainSize;
        //                    break;
        //                //case GaiaConstants.EnvironmentSizePreset.XLarge:
        //                //    m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is4096MetersSq;
        //                //    m_settings.m_currentDefaults.m_terrainHeight = m_settings.m_currentDefaults.m_terrainSize;
        //                //    break;
        //            }

        //            if (m_settings.m_targeSizePreset != GaiaConstants.EnvironmentSizePreset.Custom)
        //            {
        //                m_settings.m_createTerrainScenes = false;
        //                m_settings.m_unloadTerrainScenes = false;
        //                m_settings.m_floatingPointFix = false;
        //                m_showAutoStreamSettingsBox = false;
        //                m_settings.m_tilesX = 1;
        //                m_settings.m_tilesZ = 1;
        //            }

        //            Rect foldOutWorldSizeRect = EditorGUILayout.GetControlRect();
        //            //m_foldOutWorldSizeSettings = EditorGUI.Foldout(new Rect(foldOutWorldSizeRect.x + EditorGUIUtility.labelWidth, foldOutWorldSizeRect.y, foldOutWorldSizeRect.width, foldOutWorldSizeRect.height), m_foldOutWorldSizeSettings, m_editorUtils.GetContent("AdvancedWorldSize"));
        //            if (m_foldOutWorldSizeSettings)
        //            {
        //                //Label
        //                EditorGUI.LabelField(new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y + lineHeight, rect.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("AdvancedWorldSize"), m_editorUtils.Styles.heading);

        //                EditorGUI.indentLevel++;

        //                //X Label
        //                Rect numFieldRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y + lineHeight * 2, (rect.width - EditorGUIUtility.labelWidth) * 0.2f, EditorGUIUtility.singleLineHeight);
        //                EditorGUI.LabelField(numFieldRect, m_editorUtils.GetContent("X Tiles"));
        //                // X Entry Field
        //                numFieldRect = new Rect(numFieldRect.x + numFieldRect.width, numFieldRect.y, numFieldRect.width, EditorGUIUtility.singleLineHeight);
        //                int oldTilesX = m_settings.m_tilesX;
        //                int tilesX = EditorGUI.IntField(numFieldRect, m_settings.m_tilesX);
        //                //Empty Label Field for Spacing
        //                numFieldRect = new Rect(numFieldRect.x + numFieldRect.width, numFieldRect.y, numFieldRect.width, EditorGUIUtility.singleLineHeight);
        //                EditorGUI.LabelField(numFieldRect, " ");
        //                //Z Label
        //                numFieldRect = new Rect(numFieldRect.x + numFieldRect.width, numFieldRect.y, numFieldRect.width, EditorGUIUtility.singleLineHeight);
        //                EditorGUI.LabelField(numFieldRect, m_editorUtils.GetContent("Z Tiles"));
        //                // Z Entry Field
        //                numFieldRect = new Rect(numFieldRect.x + numFieldRect.width, numFieldRect.y, numFieldRect.width, EditorGUIUtility.singleLineHeight);
        //                int oldTilesZ = m_settings.m_tilesZ;
        //                int tilesZ = EditorGUI.IntField(numFieldRect, m_settings.m_tilesZ);
        //                //Empty Label Field for Spacing

        //                labelRect.y = numFieldRect.y + lineHeight;
        //                fieldRect.y = labelRect.y;
        //                GUILayout.Space(lineHeight * 4);

        //                EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Terrain Size"));
        //                m_settings.m_currentSize = (GaiaConstants.EnvironmentSize)EditorGUI.EnumPopup(fieldRect, m_settings.m_currentSize);
        //                labelRect.y += lineHeight;
        //                fieldRect.y += lineHeight;
        //                int oldHeight = m_settings.m_currentDefaults.m_terrainHeight;
        //                EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Terrain Height"));
        //                m_settings.m_currentDefaults.m_terrainHeight = EditorGUI.IntField(fieldRect, m_settings.m_currentDefaults.m_terrainHeight);
        //                if (oldHeight != m_settings.m_currentDefaults.m_terrainHeight)
        //                {
        //                    m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Custom;
        //                    if (m_settings.m_currentDefaults.m_terrainHeight <= 0)
        //                    {
        //                        m_settings.m_currentDefaults.m_terrainHeight = 1;
        //                    }
        //                }

        //                labelRect.y += lineHeight;
        //                fieldRect.y += lineHeight;

        //#if !GAIA_PRO_PRESENT
        //                bool currentGUIState = GUI.enabled;
        //                GUI.enabled = false;
        //                Rect proOnlyRect = new Rect(fieldRect);
        //                proOnlyRect.x += 30;
        //                proOnlyRect.width = 100;

        //#endif


        //#if GAIA_PRO_PRESENT
        //                //automatic activation of create terrain scenes / unload terrain scenes at a certain world size
        //                if (oldTilesX < 3 && tilesX >= 3 && tilesZ < 3 || oldTilesZ < 3 && tilesZ >= 3 && tilesX < 3)
        //                {
        //                    m_settings.m_createTerrainScenes = true;
        //                    m_settings.m_unloadTerrainScenes = true;
        //                    m_settings.m_floatingPointFix = true;
        //                    m_showAutoStreamSettingsBox = true;
        //                }
        //#endif

        //                if (m_showAutoStreamSettingsBox)
        //                {
        //                    Rect helpBoxRect = new Rect(fieldRect);
        //                    helpBoxRect.x += 50;
        //                    helpBoxRect.y += 4;
        //                    helpBoxRect.width -= 50;
        //                    helpBoxRect.height = 40;
        //                    EditorGUI.HelpBox(helpBoxRect, m_editorUtils.GetTextValue("AutoStreamSettings"), MessageType.Info);
        //                }

        //                GUILayout.Space(lineHeight);
        //                EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("CreateTerrainScenes"));
        //                m_settings.m_createTerrainScenes = EditorGUI.Toggle(fieldRect, m_settings.m_createTerrainScenes);
        //#if !GAIA_PRO_PRESENT
        //                EditorGUI.LabelField(proOnlyRect, m_editorUtils.GetContent("GaiaProOnly"));
        //#endif

        //                labelRect.y += lineHeight;
        //                fieldRect.y += lineHeight;

        //                GUILayout.Space(lineHeight);

        //                EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("UnloadTerrainScenes"));
        //                m_settings.m_unloadTerrainScenes = EditorGUI.Toggle(fieldRect, m_settings.m_unloadTerrainScenes);
        //#if !GAIA_PRO_PRESENT
        //                proOnlyRect.y += lineHeight;
        //                EditorGUI.LabelField(proOnlyRect, m_editorUtils.GetContent("GaiaProOnly"));
        //#endif

        //                labelRect.y += lineHeight;
        //                fieldRect.y += lineHeight;
        //                GUILayout.Space(lineHeight);

        //                EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("FloatingPointFix"));
        //                m_settings.m_floatingPointFix = EditorGUI.Toggle(fieldRect, m_settings.m_floatingPointFix);
        //#if !GAIA_PRO_PRESENT
        //                proOnlyRect.y += lineHeight;
        //                EditorGUI.LabelField(proOnlyRect, m_editorUtils.GetContent("GaiaProOnly"));
        //#endif
        //                if (m_settings.m_createTerrainScenes || m_settings.m_unloadTerrainScenes || m_settings.m_floatingPointFix)
        //                {
        //                    m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Custom;
        //                }

        //#if !GAIA_PRO_PRESENT
        //                GUI.enabled = currentGUIState;
        //#endif

        //                labelRect.y += lineHeight;
        //                fieldRect.y += lineHeight;
        //                GUILayout.Space(lineHeight);

        //                int world_xDimension = m_settings.m_tilesX * m_settings.m_currentDefaults.m_terrainSize;
        //                int world_zDimension = m_settings.m_tilesZ * m_settings.m_currentDefaults.m_terrainSize;
        //                int numberOfTerrains = m_settings.m_tilesX * m_settings.m_tilesZ;

        //                string worldXText = String.Format("{0:0} m", world_xDimension);
        //                string worldZText = String.Format("{0:0} m", world_zDimension);
        //                if (world_xDimension > 1000 || world_zDimension > 1000)
        //                {
        //                    worldXText = String.Format("{0:0.00} km", world_xDimension / 1000f);
        //                    worldZText = String.Format("{0:0.00} km", world_zDimension / 1000f);
        //                }

        //                GUIContent worldSizeInfo = new GUIContent(m_editorUtils.GetContent("TotalWorldSize").text + String.Format(": {0} x {1}, " + m_editorUtils.GetContent("Terrains").text + ": {2}", worldXText, worldZText, numberOfTerrains));
        //                EditorGUI.LabelField(new Rect(labelRect.x, labelRect.y, labelRect.width + fieldRect.width, labelRect.height), worldSizeInfo, m_editorUtils.Styles.heading);

        //                if (tilesX != m_settings.m_tilesX || tilesZ != m_settings.m_tilesZ || m_oldTargetSize != m_settings.m_currentSize)
        //                {

        //                    m_settings.m_tilesX = tilesX;
        //                    m_settings.m_tilesZ = tilesZ;

        //                    if (m_settings.m_tilesX > 1 ||
        //                        m_settings.m_tilesZ > 1 ||
        //                        m_settings.m_currentSize == GaiaConstants.EnvironmentSize.Is8192MetersSq ||
        //                        m_settings.m_currentSize == GaiaConstants.EnvironmentSize.Is16384MetersSq
        //                        )
        //                    {
        //                        m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Custom;
        //                    }
        //                    else
        //                    {
        //                        switch (m_settings.m_currentSize)
        //                        {
        //                            case GaiaConstants.EnvironmentSize.Is256MetersSq:
        //                                m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Tiny;
        //                                break;
        //                            case GaiaConstants.EnvironmentSize.Is512MetersSq:
        //                                m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Small;
        //                                break;
        //                            case GaiaConstants.EnvironmentSize.Is1024MetersSq:
        //                                m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Medium;
        //                                break;
        //                            case GaiaConstants.EnvironmentSize.Is2048MetersSq:
        //                                m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.Large;
        //                                break;
        //                            //case GaiaConstants.EnvironmentSize.Is4096MetersSq:
        //                            //    m_settings.m_targeSizePreset = GaiaConstants.EnvironmentSizePreset.XLarge;
        //                            //    break;
        //                        }

        //                    }

        //                    EditorUtility.SetDirty(m_settings);
        //                }
        //                EditorGUI.indentLevel--;
        //            } //end of foldout

        //            labelRect.y += lineHeight * 1.3f;
        //            fieldRect.y += lineHeight * 1.3f;

        //            //Quality

        //            if (helpEnabled)
        //            {
        //                //GUILayout.Space(lineHeight * 0.5f);
        //                m_editorUtils.InlineHelp("Quality Header", helpEnabled);
        //                labelRect.y += GUILayoutUtility.GetLastRect().height;
        //                fieldRect.y += GUILayoutUtility.GetLastRect().height;
        //                GUILayout.Space(lineHeight * 1f);
        //                labelRect.y += lineHeight * 0.75f;
        //                fieldRect.y += lineHeight * 0.75f;
        //            }



        //            EditorGUI.LabelField(new Rect(labelRect.x - EditorGUIUtility.labelWidth, labelRect.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("Quality Header").text, m_editorUtils.Styles.heading);
        //            m_oldTargetEnv = m_settings.m_currentEnvironment;
        //            m_settings.m_currentEnvironment = (GaiaConstants.EnvironmentTarget)EditorGUI.EnumPopup(new Rect(labelRect.x, labelRect.y, labelRect.width + fieldRect.width - 23f, labelRect.height), m_settings.m_currentEnvironment);


        //            if (m_settings.m_currentEnvironment == GaiaConstants.EnvironmentTarget.Custom && m_oldTargetEnv != GaiaConstants.EnvironmentTarget.Custom)
        //            {
        //                //User just switched to custom -> unfold the extra options
        //                m_foldoutTerrainResolutionSettings = true;
        //            }


        //            btnState = new GUIContent("+");
        //            if (m_foldoutTerrainResolutionSettings)
        //            {
        //                btnState = new GUIContent("-");
        //                GUILayout.Space(lineHeight * 5f);
        //            }
        //            if (GUI.Button(new Rect(labelRect.x + labelRect.width + fieldRect.width - 20f, labelRect.y, 20f, labelRect.height - 3f), btnState))
        //            {
        //                m_foldoutTerrainResolutionSettings = !m_foldoutTerrainResolutionSettings;
        //            }


        //            labelRect.y += lineHeight;
        //            fieldRect.y += lineHeight;

        //            bool resSettingsChangeCheck = false;

        //            Rect resSettingsRect = EditorGUILayout.GetControlRect();
        //            resSettingsRect.y = labelRect.y;
        //            //m_foldoutTerrainResolutionSettings = EditorGUI.Foldout(new Rect(resSettingsRect.x + EditorGUIUtility.labelWidth, resSettingsRect.y, resSettingsRect.width, resSettingsRect.height), m_foldoutTerrainResolutionSettings, m_editorUtils.GetContent("AdvancedQuality"));
        //            if (m_foldoutTerrainResolutionSettings)
        //            {
        //                //Label
        //                EditorGUI.LabelField(new Rect(resSettingsRect.x + EditorGUIUtility.labelWidth, resSettingsRect.y, resSettingsRect.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("AdvancedQuality"), m_editorUtils.Styles.heading);

        //                EditorGUI.indentLevel++;
        //                resSettingsChangeCheck = TerrainResolutionSettingsEnabled(resSettingsRect, false);
        //                EditorGUI.indentLevel--;
        //                labelRect.y += EditorGUIUtility.singleLineHeight * 6;
        //                fieldRect.y += EditorGUIUtility.singleLineHeight * 6;
        //            }

        //            labelRect.y += lineHeight * 0.3f;
        //            fieldRect.y += lineHeight * 0.3f;

        //            //Default biome

        //            if (helpEnabled)
        //            {
        //                m_editorUtils.InlineHelp("BiomePreset", helpEnabled);
        //                labelRect.y += GUILayoutUtility.GetLastRect().height;
        //                fieldRect.y += GUILayoutUtility.GetLastRect().height;
        //                labelRect.y += lineHeight * 0.75f;
        //                fieldRect.y += lineHeight * 0.75f;
        //            }

        //            EditorGUI.LabelField(new Rect(labelRect.x - EditorGUIUtility.labelWidth, labelRect.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("BiomePreset").text, m_editorUtils.Styles.heading);
        //            int lastBiomePresetSelection = m_biomePresetSelection;
        //            if (m_biomePresetSelection == int.MinValue)
        //            {
        //                m_biomePresetSelection = 0;
        //            }

        //            m_biomePresetSelection = EditorGUI.IntPopup(new Rect(labelRect.x, labelRect.y, labelRect.width + fieldRect.width - 23f, labelRect.height), m_biomePresetSelection, m_allBiomePresets.Select(x => x.name).ToArray(), m_allBiomePresets.Select(x => x.ID).ToArray());

        //            if (lastBiomePresetSelection != m_biomePresetSelection)
        //            {
        //                AddBiomeSpawnersForSelectedPreset();
        //                //re-create the reorderable list with the new contents
        //                CreateBiomePresetList();
        //            }

        //            if (m_biomePresetSelection == -999 && lastBiomePresetSelection != -999)
        //            {
        //                //user just switched to "Custom", foldout the extended options
        //                m_foldoutSpawnerSettings = true;
        //            }

        //            btnState = new GUIContent("+");
        //            if (m_foldoutSpawnerSettings)
        //            {
        //                btnState = new GUIContent("-");
        //            }
        //            if (GUI.Button(new Rect(labelRect.x + labelRect.width + fieldRect.width - 20f, labelRect.y, 20f, labelRect.height - 3f), btnState))
        //            {
        //                m_foldoutSpawnerSettings = !m_foldoutSpawnerSettings;
        //            }

        //            labelRect.y += lineHeight;
        //            fieldRect.y += lineHeight;
        //            GUILayout.Space(lineHeight);

        //            Rect spawnerFoldOutRect = EditorGUILayout.GetControlRect();
        //            spawnerFoldOutRect.y = labelRect.y;

        //            //m_foldoutSpawnerSettings = EditorGUI.Foldout(new Rect(spawnerFoldOutRect.x + EditorGUIUtility.labelWidth, spawnerFoldOutRect.y, spawnerFoldOutRect.width, spawnerFoldOutRect.height), m_foldoutSpawnerSettings, m_editorUtils.GetContent("AdvancedSpawners"));

        //            if (m_foldoutSpawnerSettings)
        //            {
        //                //Label
        //                EditorGUI.LabelField(new Rect(spawnerFoldOutRect.x + EditorGUIUtility.labelWidth, spawnerFoldOutRect.y, spawnerFoldOutRect.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("DefaultBiomeSettings"), m_editorUtils.Styles.heading);

        //                //the hardcoded 15 are for some indent below the foldout label
        //                Rect listRect = new Rect(spawnerFoldOutRect.x + EditorGUIUtility.labelWidth + 15, spawnerFoldOutRect.y + EditorGUIUtility.singleLineHeight + (EditorGUIUtility.singleLineHeight * 0.25f), spawnerFoldOutRect.width - EditorGUIUtility.labelWidth - 15, m_biomeSpawnersList.GetHeight()); //EditorGUILayout.GetControlRect(true, m_spawnerPresetList.GetHeight());
        //                m_biomeSpawnersList.DoList(listRect);
        //                GUILayout.Space(m_biomeSpawnersList.GetHeight());
        //                labelRect.y += m_biomeSpawnersList.GetHeight();
        //                fieldRect.y += m_biomeSpawnersList.GetHeight();
        //            }

        //            //Scene settings
        //            if (helpEnabled)
        //            {
        //                GUILayout.Space(lineHeight * 0.5f);
        //                m_editorUtils.InlineHelp("Extras", helpEnabled);
        //                labelRect.y += GUILayoutUtility.GetLastRect().height;
        //                fieldRect.y += GUILayoutUtility.GetLastRect().height;
        //                labelRect.y += lineHeight * 1.5f;
        //                fieldRect.y += lineHeight * 1.5f;
        //                GUILayout.Space(GUILayoutUtility.GetLastRect().height);

        //            }
        //            else
        //            {
        //                labelRect.y += lineHeight * 0.5f;
        //                fieldRect.y += lineHeight * 0.5f;
        //                //GUILayout.Space(lineHeight * 2f);
        //            }

        //            EditorGUI.LabelField(new Rect(labelRect.x - EditorGUIUtility.labelWidth, labelRect.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), m_editorUtils.GetContent("Extras").text, m_editorUtils.Styles.heading);

        //            Rect extrasFoldOut = EditorGUILayout.GetControlRect();
        //            extrasFoldOut.y = labelRect.y;
        //            m_foldoutExtrasSettings = EditorGUI.Foldout(new Rect(extrasFoldOut.x + EditorGUIUtility.labelWidth, extrasFoldOut.y, extrasFoldOut.width, extrasFoldOut.height), m_foldoutExtrasSettings, m_editorUtils.GetContent("AdvancedExtras"));
        //            if (m_foldoutExtrasSettings)
        //            {
        //                EditorGUI.indentLevel++;
        //                ExtrasSettingsEnabled(extrasFoldOut, helpEnabled);
        //                EditorGUI.indentLevel--;
        //                labelRect.y += EditorGUIUtility.singleLineHeight * 4;
        //                fieldRect.y += EditorGUIUtility.singleLineHeight * 4;
        //                if (m_settings.m_pipelineProfile.m_activePipelineInstalled != m_settings.m_currentRenderer)
        //                {
        //                    //Need more space when the change pipeline button is drawn
        //                    GUILayout.Space(EditorGUIUtility.singleLineHeight * 14);
        //                }
        //                else
        //                {
        //                    if (helpEnabled)
        //                    {
        //                        if (m_settings.m_currentController == GaiaConstants.EnvironmentControllerType.Custom)
        //                        {
        //                            GUILayout.Space(EditorGUIUtility.singleLineHeight * 13);
        //                        }
        //                        else
        //                        {
        //                            GUILayout.Space(EditorGUIUtility.singleLineHeight * 13);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        if (m_settings.m_currentController == GaiaConstants.EnvironmentControllerType.Custom)
        //                        {
        //                            GUILayout.Space(EditorGUIUtility.singleLineHeight * 12);
        //                        }
        //                        else
        //                        {
        //                            GUILayout.Space(EditorGUIUtility.singleLineHeight * 10);
        //                        }
        //                    }
        //                }

        //                GaiaLightingProfile lightingProfile = m_settings.m_gaiaLightingProfile;
        //#if !GAIA_PRO_PRESENT
        //                //need extra space when the Gaia Pro info is displayed
        //                if (lightingProfile.m_selectedLightingProfileValuesIndex > 0 && lightingProfile.m_selectedLightingProfileValuesIndex < lightingProfile.m_lightingProfiles.Count)
        //                {
        //                    if (lightingProfile.m_lightingProfiles[lightingProfile.m_selectedLightingProfileValuesIndex].m_typeOfLighting == "Procedural Worlds Sky")
        //                    {
        //                        GUILayout.Space(EditorGUIUtility.singleLineHeight * 4.5f);
        //                    }
        //                }
        //#endif
        //            }

        //            //Evaluate the resolution settings etc. according to what the user choses in the Manager
        //            //we only want to execute this on initially opening the window, or when settings have changed
        //            if (m_initResSettings || resSettingsChangeCheck || m_oldTargetEnv != m_settings.m_currentEnvironment || m_oldTargetSize != m_settings.m_currentSize)
        //            {
        //                if (m_oldTargetEnv != m_settings.m_currentEnvironment)
        //                {
        //                    switch (m_settings.m_currentEnvironment)
        //                    {
        //                        case GaiaConstants.EnvironmentTarget.UltraLight:
        //                            m_settings.m_currentDefaults = m_settings.m_ultraLightDefaults;
        //                            //m_settings.m_currentResources = m_settings.m_ultraLightResources;
        //                            //m_settings.m_currentGameObjectResources = m_settings.m_ultraLightGameObjectResources;
        //                            m_settings.m_currentWaterPrefabName = m_settings.m_waterMobilePrefabName;
        //                            //m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is512MetersSq;
        //                            break;
        //                        case GaiaConstants.EnvironmentTarget.MobileAndVR:
        //                            m_settings.m_currentDefaults = m_settings.m_mobileDefaults;
        //                            //m_settings.m_currentResources = m_settings.m_mobileResources;
        //                            //m_settings.m_currentGameObjectResources = m_settings.m_mobileGameObjectResources;
        //                            m_settings.m_currentWaterPrefabName = m_settings.m_waterMobilePrefabName;
        //                            //m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is1024MetersSq;
        //                            break;
        //                        case GaiaConstants.EnvironmentTarget.Desktop:
        //                            m_settings.m_currentDefaults = m_settings.m_desktopDefaults;
        //                            //m_settings.m_currentResources = m_settings.m_desktopResources;
        //                            //m_settings.m_currentGameObjectResources = m_settings.m_desktopGameObjectResources;
        //                            m_settings.m_currentWaterPrefabName = m_settings.m_waterPrefabName;
        //                            //m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is2048MetersSq;
        //                            break;
        //                        case GaiaConstants.EnvironmentTarget.PowerfulDesktop:
        //                            m_settings.m_currentDefaults = m_settings.m_powerDesktopDefaults;
        //                            //m_settings.m_currentResources = m_settings.m_powerDesktopResources;
        //                            //m_settings.m_currentGameObjectResources = m_settings.m_powerDesktopGameObjectResources;
        //                            m_settings.m_currentWaterPrefabName = m_settings.m_waterPrefabName;
        //                            //m_settings.m_currentSize = GaiaConstants.EnvironmentSize.Is2048MetersSq;
        //                            break;
        //                    }
        //                }

        //                m_settings.m_currentDefaults.m_terrainSize = GaiaUtils.EnvironmentSizeToInt(m_settings.m_currentSize);

        //                GaiaUtils.SetSettingsForEnvironment(m_settings, m_settings.m_currentEnvironment);

        //                if (m_settings.m_currentEnvironment != GaiaConstants.EnvironmentTarget.Custom)
        //                {
        //                    m_heightmapResolution = (GaiaConstants.HeightmapResolution)m_settings.m_currentDefaults.m_heightmapResolution;
        //                    m_controlTextureResolution = (GaiaConstants.TerrainTextureResolution)m_settings.m_currentDefaults.m_controlTextureResolution;
        //                    m_basemapResolution = (GaiaConstants.TerrainTextureResolution)m_settings.m_currentDefaults.m_baseMapSize;
        //                    m_detailResolutionPerPatch = m_settings.m_currentDefaults.m_detailResolutionPerPatch;
        //                    m_detailResolution = m_settings.m_currentDefaults.m_detailResolution;
        //                }
        //                m_settings.m_currentDefaults.m_heightmapResolution = (int)m_heightmapResolution;
        //                m_settings.m_currentDefaults.m_controlTextureResolution = (int)m_controlTextureResolution;
        //                m_settings.m_currentDefaults.m_baseMapSize = (int)m_basemapResolution;
        //                m_detailResolutionPerPatch = Mathf.RoundToInt(Mathf.Clamp(m_detailResolutionPerPatch, 8, 128));
        //                m_detailResolution = Mathf.RoundToInt(Mathf.Clamp(m_detailResolution, 0, 4096));
        //                m_settings.m_currentDefaults.m_detailResolutionPerPatch = m_detailResolutionPerPatch;
        //                m_settings.m_currentDefaults.m_detailResolution = m_detailResolution;
        //                m_initResSettings = false;
        //                EditorUtility.SetDirty(m_settings);
        //                EditorUtility.SetDirty(m_settings.m_currentDefaults);
        //            }



        //        }

        internal void UpdateAllSpawnersList()
        {
            CreateAdvancedTabSpawnersList();
        }

        /// <summary>
        /// Terrain resolution settings foldout
        /// </summary>
        /// <param name="helpEnabled"></param>
        private bool TerrainResolutionSettingsEnabled(Rect rect, bool helpEnabled)
        {
            EditorGUI.BeginChangeCheck();



            Rect labelRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, EditorGUIUtility.labelWidth, rect.height);
            Rect fieldRect = new Rect(labelRect.x + EditorGUIUtility.labelWidth, rect.y, rect.width - labelRect.width - EditorGUIUtility.labelWidth, rect.height);

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            ////Display notice that these fields cannot be edited if the custom setting is not chosen
            //if (m_targetEnv != GaiaConstants.EnvironmentTarget.Custom)
            //{
            //    EditorGUI.LabelField(fieldRect, m_editorUtils.GetContent("QualityCustomNotice"));
            //    labelRect.y += EditorGUIUtility.singleLineHeight;
            //    fieldRect.y += EditorGUIUtility.singleLineHeight;
            //    GUI.enabled = false;
            //}

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Heightmap Resolution"));
            m_heightmapResolution = (GaiaConstants.HeightmapResolution)EditorGUI.EnumPopup(fieldRect, m_heightmapResolution);

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Control Texture Resolution"));
            m_controlTextureResolution = (GaiaConstants.TerrainTextureResolution)EditorGUI.EnumPopup(fieldRect, m_controlTextureResolution);

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Basemap Resolution"));
            m_basemapResolution = (GaiaConstants.TerrainTextureResolution)EditorGUI.EnumPopup(fieldRect, m_basemapResolution);

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Detail Resolution Per Patch"));
            m_detailResolutionPerPatch = EditorGUI.IntField(fieldRect, m_detailResolutionPerPatch);

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Detail Resolution"));
            m_detailResolution = EditorGUI.IntField(fieldRect, m_detailResolution);



            //m_heightmapResolution = (GaiaConstants.HeightmapResolution)m_editorUtils.EnumPopup("Heightmap Resolution", m_heightmapResolution, helpEnabled);
            //m_controlTextureResolution = (GaiaConstants.TerrainTextureResolution)m_editorUtils.EnumPopup("Control Texture Resolution", m_controlTextureResolution, helpEnabled);
            //m_basemapResolution = (GaiaConstants.TerrainTextureResolution)m_editorUtils.EnumPopup("Basemap Resolution", m_basemapResolution, helpEnabled);
            //m_detailResolutionPerPatch = m_editorUtils.IntField("Detail Resolution Per Patch", m_detailResolutionPerPatch, helpEnabled);
            //m_detailResolution = m_editorUtils.IntField("Detail Resolution", m_detailResolution, helpEnabled);

            bool changeCheckTriggered = false;

            if (EditorGUI.EndChangeCheck())
            {
                m_settings.m_currentEnvironment = GaiaConstants.EnvironmentTarget.Custom;
                changeCheckTriggered = true;
            }

            return changeCheckTriggered;


        }

        private bool ExtrasSettingsEnabled(Rect rect, bool helpEnabled)
        {
            EditorGUI.BeginChangeCheck();

            Rect labelRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, EditorGUIUtility.labelWidth, rect.height);
            Rect fieldRect = new Rect(labelRect.x + EditorGUIUtility.labelWidth, rect.y, rect.width - labelRect.width - EditorGUIUtility.labelWidth, rect.height);
            Rect buttonRect = labelRect;

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;
            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Controller"));
            m_settings.m_currentController = (GaiaConstants.EnvironmentControllerType)EditorGUI.EnumPopup(fieldRect, m_settings.m_currentController);
            switch (m_settings.m_currentController)
            {
                case GaiaConstants.EnvironmentControllerType.FirstPerson:
                    m_settings.m_currentPlayerPrefabName = "FPSController";
                    break;
                case GaiaConstants.EnvironmentControllerType.ThirdPerson:
                    m_settings.m_currentPlayerPrefabName = "ThirdPersonController";
                    break;
                case GaiaConstants.EnvironmentControllerType.FlyingCamera:
                    m_settings.m_currentPlayerPrefabName = "FlyCam";
                    break;
                case GaiaConstants.EnvironmentControllerType.Custom:
                    labelRect.x += 12;
                    labelRect.y += EditorGUIUtility.singleLineHeight;
                    fieldRect.y += EditorGUIUtility.singleLineHeight;
                    EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("CustomPlayer"));
                    m_settings.m_customPlayerObject = (GameObject)EditorGUI.ObjectField(fieldRect, m_settings.m_customPlayerObject, typeof(GameObject), true);
                    labelRect.y += EditorGUIUtility.singleLineHeight;
                    fieldRect.y += EditorGUIUtility.singleLineHeight;
                    EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("CustomCamera"));
                    m_settings.m_customPlayerCamera = (Camera)EditorGUI.ObjectField(fieldRect, m_settings.m_customPlayerCamera, typeof(Camera), true);
                    labelRect.x -= 12;

                    break;
                case GaiaConstants.EnvironmentControllerType.XRController:
#if GAIA_XR
                    m_settings.m_currentPlayerPrefabName = "XRController";
#else
                    EditorUtility.DisplayDialog("XR Support not enabled", "The XR Controller is a default player for Virtual / Augmented Reality projects. Please open the Setup Panel in the Gaia Manager Standard Tab to enable XR Support in order to use the XR Player Controller. Please also make sure you have the Unity XR Interaction Toolkit package installed before doing so.", "OK");
                    m_settings.m_currentController = GaiaConstants.EnvironmentControllerType.FlyingCamera;
#endif
                    break;

            }

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            //Skies

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Skies"));

            GaiaLightingProfile lightingProfile = m_settings.m_gaiaLightingProfile;

            //Building up a value array of incrementing ints of the size of the lighting profile values array, this array will then match the displayed string selection in the popup
            int[] lightingProfileValuesIndices = Enumerable
                                .Repeat(0, (int)((lightingProfile.m_lightingProfiles.Count() - 1) / 1) + 1)
                                .Select((tr, ti) => tr + (1 * ti))
                                .ToArray();
            string[] profileNames = lightingProfile.m_lightingProfiles.Select(x => x.m_typeOfLighting).ToArray();

            //Injecting the "None" option
            lightingProfileValuesIndices = GaiaUtils.AddElementToArray(lightingProfileValuesIndices, -99);
            profileNames = GaiaUtils.AddElementToArray(profileNames, "None");

            lightingProfile.m_selectedLightingProfileValuesIndex = EditorGUI.IntPopup(fieldRect, lightingProfile.m_selectedLightingProfileValuesIndex, profileNames, lightingProfileValuesIndices);
#if !GAIA_PRO_PRESENT
            if (lightingProfile.m_selectedLightingProfileValuesIndex > 0 && lightingProfile.m_selectedLightingProfileValuesIndex < lightingProfile.m_lightingProfiles.Count)
            {
                if (lightingProfile.m_lightingProfiles[lightingProfile.m_selectedLightingProfileValuesIndex].m_profileType == GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                {
                    labelRect.y += EditorGUIUtility.singleLineHeight * 1.5f;
                    fieldRect.y += EditorGUIUtility.singleLineHeight * 1.5f;
                    EditorGUI.HelpBox(new Rect(labelRect.position, new Vector2(labelRect.width + fieldRect.width, EditorGUIUtility.singleLineHeight * 3f)), m_editorUtils.GetTextValue("GaiaProLightingProfileInfo"), MessageType.Info);
                    labelRect.y += EditorGUIUtility.singleLineHeight * 2.5f;
                    fieldRect.y += EditorGUIUtility.singleLineHeight * 2.5f;
                }
            }
#endif

            if (lightingProfile.m_selectedLightingProfileValuesIndex != -99)
            {
#if UNITY_POST_PROCESSING_STACK_V2
                labelRect.y += EditorGUIUtility.singleLineHeight;
                fieldRect.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("PostProcessing"));
                m_settings.m_enablePostProcessing = EditorGUI.Toggle(fieldRect, m_settings.m_enablePostProcessing);
#endif
            }

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            //Water
            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Water"));
            if (newProfileListIndex > m_settings.m_gaiaWaterProfile.m_waterProfiles.Count)
            {
                newProfileListIndex = 0;
            }
            m_profileList.Clear();
            if (m_settings.m_gaiaWaterProfile.m_waterProfiles.Count > 0)
            {
                foreach (GaiaWaterProfileValues profile in m_settings.m_gaiaWaterProfile.m_waterProfiles)
                {
                    m_profileList.Add(profile.m_typeOfWater);
                }
            }
            m_profileList.Add("None");
            newProfileListIndex = EditorGUI.Popup(fieldRect, newProfileListIndex, m_profileList.ToArray());

            if (m_settings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex != newProfileListIndex)
            {
                m_settings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex = newProfileListIndex;
            }
            if (m_profileList[newProfileListIndex] != "None")
            {
                labelRect.y += EditorGUIUtility.singleLineHeight;
                fieldRect.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("UnderwaterEffects"));
                m_settings.m_enableUnderwaterEffects = EditorGUI.Toggle(fieldRect, m_settings.m_enableUnderwaterEffects);
            }

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            //Wind

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Wind"));
            m_settings.m_createWind = EditorGUI.Toggle(fieldRect, m_settings.m_createWind);
            if (m_settings.m_createWind)
            {
                labelRect.y += EditorGUIUtility.singleLineHeight;
                fieldRect.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("WindType"));
                m_settings.m_windType = (GaiaConstants.GaiaGlobalWindType)EditorGUI.EnumPopup(fieldRect, m_settings.m_windType);
            }

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("AmbientAudio"));
            m_settings.m_enableAmbientAudio = EditorGUI.Toggle(fieldRect, m_settings.m_enableAmbientAudio);

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("Screenshotter"));
            m_settings.m_createScreenShotter = EditorGUI.Toggle(fieldRect, m_settings.m_createScreenShotter);

            labelRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(labelRect, m_editorUtils.GetContent("LocationManager"));
            m_settings.m_enableLocationManager = EditorGUI.Toggle(fieldRect, m_settings.m_enableLocationManager);

            //m_heightmapResolution = (GaiaConstants.HeightmapResolution)m_editorUtils.EnumPopup("Heightmap Resolution", m_heightmapResolution, helpEnabled);
            //m_controlTextureResolution = (GaiaConstants.TerrainTextureResolution)m_editorUtils.EnumPopup("Control Texture Resolution", m_controlTextureResolution, helpEnabled);
            //m_basemapResolution = (GaiaConstants.TerrainTextureResolution)m_editorUtils.EnumPopup("Basemap Resolution", m_basemapResolution, helpEnabled);
            //m_detailResolutionPerPatch = m_editorUtils.IntField("Detail Resolution Per Patch", m_detailResolutionPerPatch, helpEnabled);
            //m_detailResolution = m_editorUtils.IntField("Detail Resolution", m_detailResolution, helpEnabled);

            bool changeCheckTriggered = false;

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(m_settings.m_gaiaLightingProfile);
                m_settings.m_gaiaLightingProfile.m_enableAmbientAudio = m_settings.m_enableAmbientAudio;
                m_settings.m_gaiaLightingProfile.m_enablePostProcessing = m_settings.m_enablePostProcessing;

                EditorUtility.SetDirty(m_settings.m_gaiaWaterProfile);
                m_settings.m_gaiaWaterProfile.m_supportUnderwaterEffects = m_settings.m_enableUnderwaterEffects;

                changeCheckTriggered = true;
            }

            return changeCheckTriggered;
        }

        /// <summary>
        /// Editor Update
        /// </summary>
        public void EditorPipelineUpdate()
        {
            if (m_settings == null)
            {
                m_settings = GaiaUtils.GetGaiaSettings();
            }
            if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.Lightweight)
            {
                GaiaLWRPPipelineUtils.StartLWRPSetup(m_settings.m_pipelineProfile).MoveNext();
            }
            else if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                GaiaHDRPPipelineUtils.StartHDRPSetup(m_settings.m_pipelineProfile).MoveNext();
            }
            else if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.Universal)
            {
                GaiaURPPipelineUtils.StartURPSetup(m_settings.m_pipelineProfile).MoveNext();
            }
        }

        #endregion

        #region Gaia Main Function Calls
        /// <summary>
        /// Create and returns a defaults asset
        /// </summary>
        /// <returns>New defaults asset</returns>
        public static GaiaDefaults CreateDefaultsAsset()
        {
            GaiaDefaults defaults = ScriptableObject.CreateInstance<Gaia.GaiaDefaults>();
            AssetDatabase.CreateAsset(defaults, string.Format(GaiaDirectories.GetSettingsDirectory() + "/GD-{0:yyyyMMdd-HHmmss}.asset", DateTime.Now));
            AssetDatabase.SaveAssets();
            return defaults;
        }

        /// <summary>
        /// Create and returns a resources asset
        /// </summary>
        /// <returns>New resources asset</returns>
        //public static GaiaResource CreateResourcesAsset()
        //{
        //    GaiaResource resources = ScriptableObject.CreateInstance<Gaia.GaiaResource>();
        //    AssetDatabase.CreateAsset(resources, string.Format(GaiaDirectories.GetDataDirectory() + "/GR-{0:yyyyMMdd-HHmmss}.asset", DateTime.Now));
        //    AssetDatabase.SaveAssets();
        //    return resources;
        //}

        /// <summary>
        /// Set up the Gaia Present defines
        /// </summary>
        public static void SetGaiaDefinesStatic()
        {
            string currBuildSettings = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            //Check for and inject GAIA_PRESENT
            if (!currBuildSettings.Contains("GAIA_PRESENT"))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, currBuildSettings + ";GAIA_PRESENT");
            }
        }

        /// <summary>
        /// Creates a biome from the selected biome preset entry.
        /// </summary>
        /// <param name="selectedPresetEntry">The selected biome preset entry from the Gaia Manager</param>
        /// <returns></returns>
        private List<Spawner> CreateBiome(BiomePresetDropdownEntry selectedPresetEntry)
        {
            int totalSteps = m_BiomeSpawnersToCreate.Where(x => x.m_spawnerSettings != null).Count();
            int currentStep = 0;
            List<Spawner> createdSpawners = new List<Spawner>();
            GameObject sessionManager = ShowSessionManager();
            Transform gaiaTransform = GaiaUtils.GetGaiaGameObject().transform;
            Transform target = gaiaTransform.Find(selectedPresetEntry.name);
            if (target == null)
            {
                GameObject newGO = new GameObject();
                newGO.name = selectedPresetEntry.name + " Biome";
                newGO.transform.parent = gaiaTransform;
                target = newGO.transform;
            }

            BiomeController biomeController = target.GetComponent<BiomeController>();
            if (biomeController == null)
            {
                biomeController = target.gameObject.AddComponent<BiomeController>();
            }

            if (selectedPresetEntry.biomePreset != null)
            {
                //apply culling profile
                if (GaiaUtils.CheckIfSceneProfileExists())
                {
                    GaiaGlobal.Instance.SceneProfile.CullingProfile = selectedPresetEntry.biomePreset.GaiaSceneCullingProfile;
                }

#if UNITY_POST_PROCESSING_STACK_V2
                biomeController.m_postProcessProfile = selectedPresetEntry.biomePreset.postProcessProfile;
#endif
            }


            //Track created spawners 

            foreach (BiomeSpawnerListEntry spawnerListEntry in m_BiomeSpawnersToCreate.Where(x => x.m_spawnerSettings != null))
            {
                Spawner spawner = spawnerListEntry.m_spawnerSettings.CreateSpawner(spawnerListEntry.m_autoAssignPrototypes, biomeController.transform);
                ProgressBar.Show(ProgressBarPriority.CreateBiomeTools, "Creating Biome", "Creating Biome " + selectedPresetEntry.name, ++currentStep, totalSteps);
                biomeController.m_autoSpawners.Add(new AutoSpawner() { isActive = spawnerListEntry.m_isActiveInBiome, status = AutoSpawnerStatus.Initial, spawner = spawner });
                createdSpawners.Add(spawner);
            }
            if (createdSpawners.Count > 0)
            {
                biomeController.m_settings.m_range = createdSpawners[0].m_settings.m_spawnRange;
            }



            ProgressBar.Clear(ProgressBarPriority.CreateBiomeTools);
            return createdSpawners;

        }

        /// <summary>
        /// Create the terrain
        /// </summary>
        void CreateTerrain(bool createSpawners)
        {

            //Collect the new world settings for world creation
            WorldCreationSettings worldCreationSettings = ScriptableObject.CreateInstance<WorldCreationSettings>();
            worldCreationSettings.m_xTiles = m_settings.m_tilesX;
            worldCreationSettings.m_zTiles = m_settings.m_tilesZ;
            worldCreationSettings.m_tileSize = m_settings.m_currentDefaults.m_terrainSize;
            //increase the possible height according to terrain size - when dealing with large world scenes, a much higher 
            //height space is required to allow for adequate height changes across the world.
            worldCreationSettings.m_tileHeight = m_settings.m_currentDefaults.m_terrainHeight;
#if GAIA_PRO_PRESENT
            worldCreationSettings.m_createInScene = m_settings.m_createTerrainScenes;
            worldCreationSettings.m_autoUnloadScenes = m_settings.m_unloadTerrainScenes;
            worldCreationSettings.m_applyFloatingPointFix = m_settings.m_floatingPointFix;
#else
            worldCreationSettings.m_createInScene = false;
            worldCreationSettings.m_autoUnloadScenes = false;
            worldCreationSettings.m_applyFloatingPointFix = false;
#endif

            //Check if we need to add resources from spawners as well
            if (createSpawners)
            {
                worldCreationSettings.m_spawnerPresetList = m_BiomeSpawnersToCreate;
            }

            GaiaSessionManager.CreateWorld(worldCreationSettings);
#if GAIA_PRO_PRESENT
            if (SessionManager != null)
            {
                WorldOriginEditor.m_sessionManagerExits = true;
            }
#else
            if (SessionManager != null)
            {
                Gaia2TopPanel.m_sessionManagerExits = true;
            }
#endif


            //Adjust the scene view so you can see the terrain
            if (SceneView.lastActiveSceneView != null)
            {
                if (m_settings != null)
                {
                    SceneView.lastActiveSceneView.LookAtDirect(new Vector3(0f, 300f, -1f * (m_settings.m_currentDefaults.m_terrainSize / 2f)), Quaternion.Euler(30f, 0f, 0f));
                }
            }

        }

        /// <summary>
        /// Create / show the session manager
        /// </summary>
        GameObject ShowSessionManager(bool pickupExistingTerrain = false)
        {
            GameObject mgrObj = GaiaSessionManager.GetSessionManager(pickupExistingTerrain).gameObject;
#if GAIA_PRO_PRESENT
            if (mgrObj != null)
            {
                WorldOriginEditor.m_sessionManagerExits = true;
            }
#else
            if (SessionManager != null)
            {
                Gaia2TopPanel.m_sessionManagerExits = true;
            }
#endif
            Selection.activeGameObject = mgrObj;
            return mgrObj;
        }


        GameObject ShowMaskMapExporter()
        {
#if GAIA_PRO_PRESENT
            GameObject maskMapExporterObj = GameObject.Find("Mask Map Exporter");
            if (maskMapExporterObj == null)
            {
                GameObject gaiaObj = GaiaUtils.GetGaiaGameObject();
                maskMapExporterObj = new GameObject("Mask Map Exporter");
                maskMapExporterObj.transform.parent = gaiaObj.transform;
                MaskMapExport export = maskMapExporterObj.AddComponent<MaskMapExport>();
                export.FitToAllTerrains();
            }
            Selection.activeGameObject = maskMapExporterObj;
            return maskMapExporterObj;
#else
            return null;
#endif
        }

        /// <summary>
        /// Select or create a stamper
        /// </summary>
        GameObject ShowStamper(List<Spawner> autoSpawnerCandidates = null, List<BiomeSpawnerListEntry> biomeSpawnersToCreate = null)
        {
            ////Only do this if we have 1 terrain
            //if (DisplayErrorIfNotMinimumTerrainCount(1))
            //{
            //    return;
            //}

            //Make sure we have a session manager
            //m_sessionManager = m_resources.CreateOrFindSessionManager().GetComponent<GaiaSessionManager>();

            //Make sure we have gaia object
            GameObject gaiaObj = GaiaUtils.GetGaiaGameObject();

            //Create or find the stamper
            GameObject stamperObj = GameObject.Find(GaiaConstants.StamperObject);
            Stamper stamper = null;
            if (stamperObj == null)
            {
                stamperObj = new GameObject(GaiaConstants.StamperObject);
                stamperObj.transform.parent = gaiaObj.transform;
                stamper = stamperObj.AddComponent<Stamper>();
                stamper.m_settings = ScriptableObject.CreateInstance<StamperSettings>();

                //Add an image mask as start configuration
                if (m_settings != null && m_settings.m_defaultStamp != null)
                {
                    //stamper.m_settings.m_imageMasks[0].m_imageMaskTexture = m_settings.m_defaultStamp;
                    stamper.m_settings.m_stamperInputImageMask.ImageMaskTexture = m_settings.m_defaultStamp;
                }
                //ImageMaskListEditor.OpenStampBrowser(null,stamper);
                stamper.UpdateStamp();
                stamperObj.transform.position = new Vector3Double(stamper.m_settings.m_x, stamper.m_settings.m_y, stamper.m_settings.m_z);
                stamper.UpdateMinMaxHeight();
                stamper.m_seaLevel = m_settings.m_currentDefaults.m_seaLevel;


#if GAIA_PRO_PRESENT
                if (GaiaUtils.HasDynamicLoadedTerrains())
                {
                    stamper.m_loadTerrainMode = LoadMode.EditorSelected;
                    //make sure the stamper does not load in the complete world right off the bat
                    stamper.FitToAllTerrains(true);
                }
                else
                {
#endif
                    stamper.FitToAllTerrains();
#if GAIA_PRO_PRESENT
                }
#endif
                ImageMaskListEditor.OpenStampBrowser(stamper.m_settings.m_stamperInputImageMask);
            }
            else
            {
                stamper = stamperObj.GetComponent<Stamper>();
                if (stamper != null)
                {
                    ImageMaskListEditor.OpenStampBrowser(stamper.m_settings.m_stamperInputImageMask);
                }
                else
                {
                    Debug.LogError("There is a Stamper Game Object in the scene, but it does not contain a stamper component, could not show the stamp browser.");
                }
            }

            //if spawners are supplied, set them up for automatic spawning
            if (autoSpawnerCandidates != null && autoSpawnerCandidates.Count > 0 && biomeSpawnersToCreate != null)
            {
                //if there are already auto spawners set up, ask first
                if (stamper.m_autoSpawners.Count == 0 || EditorUtility.DisplayDialog("Change Autospawners?", "Do you want the automatic spawners of the stamper to use the new biome, or do you want to keep them as they are?", "Switch to new Biome", "Keep"))
                {
                    stamper.m_autoSpawners.Clear();

                    for (int i = 0; i < autoSpawnerCandidates.Count; i++)
                    {
                        Spawner autoSpawnerCandidate = autoSpawnerCandidates[i];
                        AutoSpawner newAutoSpawner = new AutoSpawner()
                        {
                            isActive = biomeSpawnersToCreate[i].m_isActiveInStamper,
                            status = AutoSpawnerStatus.Initial,
                            spawner = autoSpawnerCandidate
                        };
                        stamper.m_autoSpawners.Add(newAutoSpawner);

                        //Use parent of spawner as linked biome controller for this spawner
                        if (autoSpawnerCandidate != null && stamper.m_linkedBiomeController == null)
                        {
                            BiomeController bc = autoSpawnerCandidate.transform.parent.GetComponent<BiomeController>();
                            if (bc != null)
                            {
                                stamper.m_linkedBiomeController = bc;
                            }
                        }
                    }
                }
            }

            Selection.activeGameObject = stamperObj;

            //activate Gizmos in scene view - too many users confused by missing stamper preview when Gizmos are turned off
            foreach (SceneView sv in SceneView.sceneViews)
            {
                sv.drawGizmos = true;
            }

            return stamperObj;
        }

        /// <summary>
        /// Create or select the existing visualiser
        /// </summary>
        /// <returns>New or exsiting visualiser - or null if no terrain</returns>
        //GameObject ShowVisualiser()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    GameObject gaiaObj = GaiaUtils.GetGaiaGameObject();
        //    GameObject visualiserObj = GameObject.Find("Visualiser");
        //    if (visualiserObj == null)
        //    {
        //        visualiserObj = new GameObject("Visualiser");
        //        visualiserObj.AddComponent<ResourceVisualiser>();
        //        visualiserObj.transform.parent = gaiaObj.transform;

        //        //Center it on the terrain
        //        visualiserObj.transform.position = Gaia.TerrainHelper.GetActiveTerrainCenter();
        //    }
        //    ResourceVisualiser visualiser = visualiserObj.GetComponent<ResourceVisualiser>();
        //    visualiser.m_resources = m_settings.m_currentResources;
        //    return visualiserObj;
        //}

        /// <summary>
        /// Show a normal exporter
        /// </summary>
        void ShowNormalMaskExporter()
        {
            //Only do this if we have 1 terrain
            if (DisplayErrorIfNotMinimumTerrainCount(1))
            {
                return;
            }

            var export = EditorWindow.GetWindow<GaiaNormalExporterEditor>(false, m_editorUtils.GetTextValue("Normalmap Exporter"));
            export.Show();
        }

        /// <summary>
        /// Show the terrain height adjuster
        /// </summary>
        void ShowTerrainHeightAdjuster()
        {
            var export = EditorWindow.GetWindow<GaiaTerrainHeightAdjuster>(false, m_editorUtils.GetTextValue("Height Adjuster"));
            export.Show();
        }

        /// <summary>
        /// Show the terrain explorer helper
        /// </summary>
        void ShowTerrainUtilties()
        {
            var export = EditorWindow.GetWindow<GaiaTerrainExplorerEditor>(false, m_editorUtils.GetTextValue("Terrain Utilities"));
            export.Show();
        }

        /// <summary>
        /// Show a texture mask exporter
        /// </summary>
        void ShowTexureMaskExporter()
        {
            //Only do this if we have 1 terrain
            if (DisplayErrorIfNotMinimumTerrainCount(1))
            {
                return;
            }

            var export = EditorWindow.GetWindow<GaiaMaskExporterEditor>(false, m_editorUtils.GetTextValue("Splatmap Exporter"));
            export.Show();
        }

        /// <summary>
        /// Show a grass mask exporter
        /// </summary>
        void ShowGrassMaskExporter()
        {
            //Only do this if we have 1 terrain
            if (DisplayErrorIfNotMinimumTerrainCount(1))
            {
                return;
            }

            var export = EditorWindow.GetWindow<GaiaGrassMaskExporterEditor>(false, m_editorUtils.GetTextValue("Grassmask Exporter"));
            export.Show();
        }

        /// <summary>
        /// Show flowmap exporter
        /// </summary>
        void ShowFlowMapMaskExporter()
        {
            //Only do this if we have 1 terrain
            if (DisplayErrorIfNotMinimumTerrainCount(1))
            {
                return;
            }

            var export = EditorWindow.GetWindow<GaiaWaterflowMapEditor>(false, m_editorUtils.GetTextValue("Flowmap Exporter"));
            export.Show();
        }

        /// <summary>
        /// Show a terrain obj exporter
        /// </summary>
        void ShowTerrainObjExporter()
        {
            //if (DisplayErrorIfNotMinimumTerrainCount(1))
            //{
            //    return;
            //}

            var export = EditorWindow.GetWindow<ExportTerrain>(false, m_editorUtils.GetTextValue("Export Terrain"));
            export.Show();
        }

        /// <summary>
        /// Export the world as a PNG heightmap
        /// </summary>
        void ExportWorldAsHeightmapPNG()
        {
            if (DisplayErrorIfNotMinimumTerrainCount(1))
            {
                return;
            }

            GaiaWorldManager mgr = new GaiaWorldManager(Terrain.activeTerrains);
            if (mgr.TileCount > 0)
            {
                string path = GaiaDirectories.GetExportDirectory();
                path = Path.Combine(path, PWCommon4.Utils.FixFileName(string.Format("Terrain-Heightmap-{0:yyyyMMdd-HHmmss}", DateTime.Now)));
                mgr.ExportWorldAsPng(path);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog(
                    m_editorUtils.GetTextValue("Export complete"),
                    m_editorUtils.GetTextValue(" Your heightmap has been saved to : ") + path,
                    m_editorUtils.GetTextValue("OK"));
            }
        }

        /// <summary>
        /// Export the shore mask as a png file
        /// </summary>
        void ExportShoremaskAsPNG()
        {
            //Only do this if we have 1 terrain
            if (DisplayErrorIfNotMinimumTerrainCount(1))
            {
                return;
            }

            var export = EditorWindow.GetWindow<ShorelineMaskerEditor>(false, m_editorUtils.GetTextValue("Export Shore"));
            export.m_seaLevel = GaiaSessionManager.GetSessionManager(false).GetSeaLevel();
            export.Show();
        }

        /// <summary>
        /// Show the Gaia Stamp converter
        /// </summary>
        void ShowGaiaStampConverter()
        {
            var convert = EditorWindow.GetWindow<ConvertStamps>(false, m_editorUtils.GetTextValue("Convert Gaia 1 Stamps"));
            convert.position = new Rect(position.position + new Vector2(50, 50), new Vector2(400, 220));
            convert.minSize = new Vector2(400, 220);
            convert.Show();
        }


        /// <summary>
        /// Show the extension exporter
        /// </summary>
        void ShowExtensionExporterEditor()
        {
            var export = EditorWindow.GetWindow<GaiaExtensionExporterEditor>(false, m_editorUtils.GetTextValue("Export GX"));
            export.Show();
        }

        /// <summary>
        /// Display an error if there is not exactly one terrain
        /// </summary>
        /// <param name="requiredTerrainCount">The amount required</param>
        /// <param name="feature">The feature name</param>
        /// <returns>True if an error, false otherwise</returns>
        private bool DisplayErrorIfInvalidTerrainCount(int requiredTerrainCount, string feature = "")
        {
            int actualTerrainCount = Gaia.TerrainHelper.GetActiveTerrainCount();
            if (actualTerrainCount != requiredTerrainCount)
            {
                if (string.IsNullOrEmpty(feature))
                {
                    if (actualTerrainCount < requiredTerrainCount)
                    {
                        EditorUtility.DisplayDialog(
                            m_editorUtils.GetTextValue("OOPS!"),
                            string.Format(m_editorUtils.GetTextValue("You currently have {0} active terrains in your scene, but to " +
                            "use this feature you need {1}. Please load in unloaded terrains or create a terrain!"), actualTerrainCount, requiredTerrainCount),
                            m_editorUtils.GetTextValue("OK"));
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(
                            m_editorUtils.GetTextValue("OOPS!"),
                            string.Format(m_editorUtils.GetTextValue("You currently have {0} active terrains in your scene, but to " +
                            "use this feature you need {1}. Please remove terrain!"), actualTerrainCount, requiredTerrainCount),
                            m_editorUtils.GetTextValue("OK"));
                    }
                }
                else
                {
                    if (actualTerrainCount < requiredTerrainCount)
                    {
                        EditorUtility.DisplayDialog(
                            m_editorUtils.GetTextValue("OOPS!"),
                            string.Format(m_editorUtils.GetTextValue("You currently have {0} active terrains in your scene, but to " +
                            "use {2} you need {1}. Please load in unloaded terrains or create a terrain!"), actualTerrainCount, requiredTerrainCount, feature),
                            m_editorUtils.GetTextValue("OK"));
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(
                            m_editorUtils.GetTextValue("OOPS!"),
                            string.Format(m_editorUtils.GetTextValue("You currently have {0} active terrains in your scene, but to " +
                            "use {2} you need {1}. Please remove terrain!"), actualTerrainCount, requiredTerrainCount, feature),
                            m_editorUtils.GetTextValue("OK"));
                    }
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Display an error if there is not exactly one terrain
        /// </summary>
        /// <param name="requiredTerrainCount">The amount required</param>
        /// <param name="feature">The feature name</param>
        /// <returns>True if an error, false otherwise</returns>
        private static bool DisplayErrorIfNotMinimumTerrainCount(int requiredTerrainCount, string feature = "")
        {
            int actualTerrainCount = Gaia.TerrainHelper.GetActiveTerrainCount();
            if (actualTerrainCount < requiredTerrainCount)
            {
                if (string.IsNullOrEmpty(feature))
                {
                    if (actualTerrainCount < requiredTerrainCount)
                    {
                        EditorUtility.DisplayDialog(
                            m_editorUtils.GetTextValue("OOPS!"),
                            string.Format(m_editorUtils.GetTextValue("You currently have {0} active terrains in your scene, but to " +
                            "use this feature you need at least {1}. Please load in unloaded terrains or create a terrain!"), actualTerrainCount, requiredTerrainCount),
                            m_editorUtils.GetTextValue("OK"));
                    }
                }
                else
                {
                    if (actualTerrainCount < requiredTerrainCount)
                    {
                        EditorUtility.DisplayDialog(
                            m_editorUtils.GetTextValue("OOPS!"),
                            string.Format(m_editorUtils.GetTextValue("You currently have {0} active terrains in your scene, but to " +
                            "use {2} you need at least {1}.Please load in unloaded terrains or create a terrain!"), actualTerrainCount, requiredTerrainCount, feature),
                            m_editorUtils.GetTextValue("OK"));
                    }
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Get the range from the terrain
        /// </summary>
        /// <returns></returns>
        private float GetRangeFromTerrain()
        {
            float range = (m_settings.m_currentDefaults.m_terrainSize / 2) * m_settings.m_tilesX;
            Terrain t = Gaia.TerrainHelper.GetActiveTerrain();
            if (t != null)
            {
                range = (Mathf.Max(t.terrainData.size.x, t.terrainData.size.z) / 2f) * m_settings.m_tilesX;
            }
            return range;
        }

        ///// <summary>
        ///// Create a texture spawner
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateTextureSpawner()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentResources.CreateCoverageTextureSpawner(GetRangeFromTerrain(), Mathf.Clamp(m_settings.m_currentDefaults.m_terrainSize / (float)m_settings.m_currentDefaults.m_controlTextureResolution, 0.2f, 100f));
        //}

        ///// <summary>
        ///// Create a detail spawner
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateDetailSpawner()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentResources.CreateCoverageDetailSpawner(GetRangeFromTerrain(), Mathf.Clamp(m_settings.m_currentDefaults.m_terrainSize / (float)m_settings.m_currentDefaults.m_detailResolution, 0.2f, 100f));
        //}

        ///// <summary>
        ///// Create a clustered detail spawner
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateClusteredDetailSpawner()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentResources.CreateClusteredDetailSpawner(GetRangeFromTerrain(), Mathf.Clamp(m_settings.m_currentDefaults.m_terrainSize / (float)m_settings.m_currentDefaults.m_detailResolution, 0.2f, 100f));
        //}

        ///// <summary>
        ///// Create a tree spawner
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateClusteredTreeSpawnerFromTerrainTrees()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentResources.CreateClusteredTreeSpawner(GetRangeFromTerrain());
        //}

        ///// <summary>
        ///// Create a tree spawner from game objecxts
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateClusteredTreeSpawnerFromGameObjects()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentGameObjectResources.CreateClusteredGameObjectSpawnerForTrees(GetRangeFromTerrain());
        //}

        ///// <summary>
        ///// Create a tree spawner
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateCoverageTreeSpawner()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentResources.CreateCoverageTreeSpawner(GetRangeFromTerrain());
        //}

        ///// <summary>
        ///// Create a tree spawner
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateCoverageTreeSpawnerFromGameObjects()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentGameObjectResources.CreateCoverageGameObjectSpawnerForTrees(GetRangeFromTerrain());
        //}

        ///// <summary>
        ///// Create a game object spawner
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateCoverageGameObjectSpawner()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentGameObjectResources.CreateCoverageGameObjectSpawner(GetRangeFromTerrain());
        //}

        ///// <summary>
        ///// Create a game object spawner
        ///// </summary>
        ///// <returns>Spawner</returns>
        //GameObject CreateClusteredGameObjectSpawner()
        //{
        //    //Only do this if we have 1 terrain
        //    if (DisplayErrorIfNotMinimumTerrainCount(1))
        //    {
        //        return null;
        //    }

        //    return m_settings.m_currentGameObjectResources.CreateClusteredGameObjectSpawner(GetRangeFromTerrain());
        //}
        #endregion

        #region Create Step 2 (Player, water, sky etc)

        /// <summary>
        /// Create a scene exporter object
        /// </summary>
        /*
        GameObject ShowSceneExporter()
        {
            GameObject exporterObj = GameObject.Find("Exporter");
            if (exporterObj == null)
            {
                exporterObj = new GameObject("Exporter");
                exporterObj.transform.position = Gaia.TerrainHelper.GetActiveTerrainCenter(false);
                GaiaExporter exporter = exporterObj.AddComponent<GaiaExporter>();
                GameObject gaiaObj = GameObject.Find("Gaia");
                if (gaiaObj != null)
                {
                    exporterObj.transform.parent = gaiaObj.transform;
                    exporter.m_rootObject = gaiaObj;
                }
                exporter.m_defaults = m_defaults;
                exporter.m_resources = m_resources;
                exporter.IngestGaiaSetup();
            }
            return exporterObj;
                     */

        /// <summary>
        /// Create a wind zone
        /// </summary>
        private static GameObject CreateWindZone(GaiaSettings gaiaSettings)
        {
            WindZone globalWind = FindObjectOfType<WindZone>();
            if (globalWind == null)
            {
                GameObject windZoneObj = new GameObject("PW Wind Zone");
                windZoneObj.transform.Rotate(new Vector3(25f, 0f, 0f));
                globalWind = windZoneObj.AddComponent<WindZone>();
                switch (gaiaSettings.m_windType)
                {
                    case GaiaConstants.GaiaGlobalWindType.Calm:
                        globalWind.windMain = 0.35f;
                        globalWind.windTurbulence = 0.35f;
                        globalWind.windPulseMagnitude = 0.2f;
                        globalWind.windPulseFrequency = 0.05f;
                        break;
                    case GaiaConstants.GaiaGlobalWindType.Moderate:
                        globalWind.windMain = 0.55f;
                        globalWind.windTurbulence = 0.45f;
                        globalWind.windPulseMagnitude = 0.2f;
                        globalWind.windPulseFrequency = 0.1f;
                        break;
                    case GaiaConstants.GaiaGlobalWindType.Strong:
                        globalWind.windMain = 0.75f;
                        globalWind.windTurbulence = 0.5f;
                        globalWind.windPulseMagnitude = 0.2f;
                        globalWind.windPulseFrequency = 0.25f;
                        break;
                    case GaiaConstants.GaiaGlobalWindType.None:
                        globalWind.windMain = 0f;
                        globalWind.windTurbulence = 0f;
                        globalWind.windPulseMagnitude = 0f;
                        globalWind.windPulseFrequency = 0f;
                        break;
                }

                GameObject gaiaObj = GaiaUtils.GetRuntimeSceneObject();
                windZoneObj.transform.SetParent(gaiaObj.transform);
            }
            else
            {
                switch (gaiaSettings.m_windType)
                {
                    case GaiaConstants.GaiaGlobalWindType.Calm:
                        globalWind.windMain = 0.35f;
                        globalWind.windTurbulence = 0.35f;
                        globalWind.windPulseMagnitude = 0.2f;
                        globalWind.windPulseFrequency = 0.05f;
                        break;
                    case GaiaConstants.GaiaGlobalWindType.Moderate:
                        globalWind.windMain = 0.55f;
                        globalWind.windTurbulence = 0.45f;
                        globalWind.windPulseMagnitude = 0.2f;
                        globalWind.windPulseFrequency = 0.1f;
                        break;
                    case GaiaConstants.GaiaGlobalWindType.Strong:
                        globalWind.windMain = 0.75f;
                        globalWind.windTurbulence = 0.5f;
                        globalWind.windPulseMagnitude = 0.2f;
                        globalWind.windPulseFrequency = 0.25f;
                        break;
                    case GaiaConstants.GaiaGlobalWindType.None:
                        globalWind.windMain = 0f;
                        globalWind.windTurbulence = 0f;
                        globalWind.windPulseMagnitude = 0f;
                        globalWind.windPulseFrequency = 0f;
                        break;
                }
            }

            GaiaLighting.SetupWind();

            GameObject returingObject = globalWind.gameObject;

            return returingObject;
        }

        public static void CreateGaiaExtras(GaiaSettings gaiaSettings, GaiaLightingProfile lightingProfile, List<string> waterList, int selectedWater)
        {
#if HDPipeline && !GAIA_EXPERIMENTAL
            if (m_settings.m_gaiaLightingProfile.m_lightingProfiles[m_settings.m_gaiaLightingProfile.m_selectedLightingProfileValuesIndex].m_profileType == GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
            {
                EditorUtility.DisplayDialog("Not Yet Supported", GaiaConstants.HDRPPWSkyExperimental + "\r\n\r\n Please choose a different sky type for the runtime creation.", "Ok");
                return;
            }
#endif


            bool restoreLoadingRange = false;
#if GAIA_PRO_PRESENT
            //Check if the center terrain is loaded in when we are in dynamic loaded terrain mode
            CenterSceneViewLoadingOn originalCenterOn = CenterSceneViewLoadingOn.WorldOrigin;
            double originalRange = 250;
            double originalImpostorRange = 500;

            if (GaiaUtils.HasDynamicLoadedTerrains() && (TerrainLoaderManager.Instance.CenterSceneViewLoadingOn == CenterSceneViewLoadingOn.SceneViewCamera || TerrainLoaderManager.Instance.GetLoadingRange() == 0))
            {
                //If center terrain is not loaded in / scene view camera is loading the terrains, we load in the center terrains while creating the player setup, then restore back to the original setup
                originalCenterOn = TerrainLoaderManager.Instance.CenterSceneViewLoadingOn;
                originalRange = TerrainLoaderManager.Instance.GetLoadingRange();
                originalImpostorRange = TerrainLoaderManager.Instance.GetImpostorLoadingRange();
                TerrainLoaderManager.Instance.CenterSceneViewLoadingOn = CenterSceneViewLoadingOn.WorldOrigin;
                TerrainLoaderManager.Instance.SetLoadingRange(10, 0);
                restoreLoadingRange = true;
            }
#endif

            try
            {

                if (m_editorUtils != null)
                {
                    if (GaiaUtils.CheckIfSceneProfileExists())
                    {
                        string textValue = "";
                        if (GaiaGlobal.Instance.SceneProfile.m_lightingProfiles.Count > 0)
                        {
                            textValue = m_editorUtils.GetTextValue("UpdateGaiaRuntime");
                        }
                        else
                        {
                            textValue = m_editorUtils.GetTextValue("AddGaiaRuntime");
                        }

                        if (lightingProfile.m_selectedLightingProfileValuesIndex != -99)
                        {
                            if (lightingProfile.m_lightingProfiles[lightingProfile.m_selectedLightingProfileValuesIndex].m_typeOfLighting != "None")
                            {
                                textValue += m_editorUtils.GetTextValue("AddGaiaLighting");
                            }
                        }

                        if (waterList[selectedWater] != "None")
                        {
                            textValue += m_editorUtils.GetTextValue("AddGaiaWater");
                        }

                        if (m_settings.m_enableAmbientAudio)
                        {
                            textValue += m_editorUtils.GetTextValue("AddGaiaAudio");
                        }

                        if (m_settings.m_currentController != GaiaConstants.EnvironmentControllerType.None)
                        {
                            textValue += m_editorUtils.GetTextValue("AddGaiaPlayer");
                        }

                        if (m_settings.m_createScreenShotter)
                        {
                            textValue += m_editorUtils.GetTextValue("AddGaiaScreenshot");
                        }

                        if (m_settings.m_enableLocationManager)
                        {
                            textValue += m_editorUtils.GetTextValue("AddGaiaLocationManager");
                        }

                        if (m_settings.m_enableLoadingScreen && GaiaUtils.HasDynamicLoadedTerrains())
                        {
                            textValue += m_editorUtils.GetTextValue("AddGaiaLoadingScreen");
                        }

                        if (GaiaGlobal.Instance.SceneProfile.m_lightingProfiles.Count > 0)
                        {
                            if (!EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("UpdateGaiaRuntimeTitle"), textValue, "Yes", "No"))
                            {
                                return;
                            }
                        }
                        else
                        {
                            if (!EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("AddGaiaRuntimeTitle"), textValue, "Yes", "No"))
                            {
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (!EditorUtility.DisplayDialog(m_editorUtils.GetTextValue("AddGaiaRuntimeTitle"), m_editorUtils.GetTextValue("AddGaiaRuntimeText"), "Yes", "No"))
                        {
                            return;
                        }
                    }
                }

                GameObject playerGO = null;
                if (gaiaSettings.m_currentController != GaiaConstants.EnvironmentControllerType.None)
                {

                    if (gaiaSettings.m_currentController == GaiaConstants.EnvironmentControllerType.Custom)
                    {
                        playerGO = GaiaSceneManagement.CreatePlayer(gaiaSettings, "Custom", false, gaiaSettings.m_customPlayerObject, gaiaSettings.m_customPlayerCamera);
                    }
                    else
                    {
                        playerGO = GaiaSceneManagement.CreatePlayer(gaiaSettings);
                    }

                }
                if (gaiaSettings.m_createWind)
                {
                    CreateWindZone(gaiaSettings);
                }
                else
                {
#if GAIA_PRO_PRESENT
                    ProceduralWorldsGlobalWeather.RemoveGlobalWindShader();
#endif
                }
                if (lightingProfile.m_selectedLightingProfileValuesIndex != -99)
                {
                    //m_settings.m_gaiaLightingProfile.m_lightingProfile = m_settings.m_currentSkies;
                    CreateSky(gaiaSettings);
                }
                else
                {
                    GaiaLighting.RemoveSystems();
                }
                if (gaiaSettings.m_currentWater != GaiaConstants.Water.None || gaiaSettings.m_currentWaterPro != GaiaConstants.GaiaWaterProfileType.None)
                {
                    CreateWater(gaiaSettings);
                }
                else
                {

                    GaiaUtils.RemoveWaterSystems();
                }
                if (gaiaSettings.m_createScreenShotter)
                {
                    CreateScreenShotter(gaiaSettings);
                }

                if (gaiaSettings.m_enableLocationManager)
                {
                    LocationManagerEditor.AddLocationSystem();
                }
                else
                {
                    LocationManagerEditor.RemoveLocationSystem();
                }

                SceneProfile sceneProfile = GaiaGlobal.Instance.SceneProfile;
                if (sceneProfile != null && playerGO != null)
                {
                    sceneProfile.m_controllerType = gaiaSettings.m_currentController;
#if GAIA_PRO_PRESENT
                    if (GaiaUtils.HasDynamicLoadedTerrains())
                    {
                        TerrainLoader tl = playerGO.GetComponentInChildren<TerrainLoader>();
                        //If the scene profile has no terrain loader data, copy it into the profile upon first creation of a player / loader
                        if (tl != null)
                        {
                            if (!sceneProfile.m_terrainLoaderDataInitialized)
                            {
                                sceneProfile.m_terrainLoaderLoadMode = tl.LoadMode;
                                sceneProfile.m_terrainLoaderMinRefreshDistance = tl.m_minRefreshDistance;
                                sceneProfile.m_terrainLoaderMaxRefreshDistance = tl.m_maxRefreshDistance;
                                sceneProfile.m_terrainLoaderMinRefreshMS = tl.m_minRefreshMS;
                                sceneProfile.m_terrainLoaderMaxRefreshMS = tl.m_maxRefreshMS;
                                sceneProfile.m_terrainLoaderFollowTransform = tl.m_followTransform;
                                sceneProfile.m_terrainLoaderLoadingBoundsRegular = tl.m_loadingBoundsRegular;
                                sceneProfile.m_terrainLoaderLoadingBoundsImpostor = tl.m_loadingBoundsImpostor;
                                sceneProfile.m_terrainLoaderLoadingBoundsCollider = tl.m_loadingBoundsCollider;
                                sceneProfile.m_terrainLoaderDataInitialized = true;
                            }
                            else
                            {
                                //Otherwise it is the other way round, and the existing data in the scene profile is the master.
                                sceneProfile.UpdateTerrainLoaderFromProfile(ref tl);
                            }
                        }
                    }
#endif
                }



                if (gaiaSettings.m_enableLoadingScreen && GaiaUtils.HasDynamicLoadedTerrains())
                {
#if GAIA_PRO_PRESENT

                    GameObject loadingScreenObject = GameObject.Find(GaiaConstants.loadingScreenName);
                    if (loadingScreenObject == null)
                    {
                        GameObject runtimeObj = GaiaUtils.GetRuntimeSceneObject(false);
                        foreach (Transform t in runtimeObj.transform)
                        {
                            if (t.name == GaiaConstants.loadingScreenName)
                            {
                                loadingScreenObject = t.gameObject;
                                break;
                            }
                        }
                    }

                    if (loadingScreenObject == null)
                    {
                        GameObject loadingScreenPrefab = PWCommon4.AssetUtils.GetAssetPrefab(GaiaConstants.loadingScreenName);
                        if (loadingScreenPrefab != null)
                        {
                            loadingScreenObject = GameObject.Instantiate(loadingScreenPrefab, Vector3.zero, Quaternion.identity) as GameObject;
                            if (loadingScreenObject != null)
                            {
                                loadingScreenObject.name = loadingScreenObject.name.Replace("(Clone)", "");
                                GameObject runtimeParent = GaiaUtils.GetRuntimeSceneObject();
                                if (runtimeParent != null)
                                {
                                    loadingScreenObject.transform.SetParent(runtimeParent.transform);
                                }
                                TerrainLoaderManager.Instance.m_loadingScreen = loadingScreenObject.GetComponent<GaiaLoadingScreen>();
                                loadingScreenObject.SetActive(false);
                            }
                        }
                    }
#endif
                }
                else
                {
                    GameObject loadingScreen = GameObject.Find(GaiaConstants.loadingScreenName);
                    if (loadingScreen != null)
                    {
                        DestroyImmediate(loadingScreen);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error while creating the Gaia Runtime setup: {ex.Message}, Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                if (restoreLoadingRange)
                {
#if GAIA_PRO_PRESENT

                    TerrainLoaderManager.Instance.CenterSceneViewLoadingOn = originalCenterOn;
                    TerrainLoaderManager.Instance.SetLoadingRange(originalRange, originalImpostorRange);
#endif
                }
            }
        }

        /// <summary>
        /// Create water
        /// </summary>
        private static GameObject CreateWater(GaiaSettings gaiaSettings)
        {
            //Only do this if we have 1 terrain
            //if (DisplayErrorIfNotMinimumTerrainCount(1))
            //{
            //    return null;
            //}
#if GAIA_2_PRESENT && !AMBIENT_WATER
            if (gaiaSettings.m_currentWaterPro == GaiaConstants.GaiaWaterProfileType.None)
            {
                GaiaUtils.RemoveWaterSystems();
            }
            else
            {
                Material waterMat = GaiaWater.GetGaiaOceanMaterial();
                GaiaUtils.GetRuntimeSceneObject();
                if (GaiaGlobal.Instance != null)
                {
                    GaiaSceneManagement.SaveToProfile(gaiaSettings.m_gaiaWaterProfile);
                    GaiaWater.GetProfile(gaiaSettings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex, waterMat, GaiaGlobal.Instance.SceneProfile, true, false);
                }
            }
#endif
            GameObject waterGO = GameObject.Find(GaiaConstants.waterSurfaceObject);
            return waterGO;
        }

        /// <summary>
        /// Create the sky
        /// </summary>
        private static void CreateSky(GaiaSettings gaiaSettings)
        {
#if GAIA_2_PRESENT
            GaiaUtils.GetRuntimeSceneObject();
            if (GaiaGlobal.Instance != null)
            {
                GaiaSceneManagement.SaveToProfile(gaiaSettings.m_gaiaLightingProfile);
                GaiaLighting.GetProfile(GaiaGlobal.Instance.SceneProfile, gaiaSettings.m_pipelineProfile, gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, true);
                //Commneted out since this creates problems when creating multiple terrains from the Gaia Manager initially
                //GaiaUtils.UpdateProbeDataDefaults(GaiaGlobal.Instance.SceneProfile);
            }
#endif
        }

        /// <summary>
        /// Create and return a screen shotter object
        /// </summary>
        /// <returns></returns>
        private static GameObject CreateScreenShotter(GaiaSettings gaiaSettings)
        {
            GameObject shotterObj = GameObject.Find(GaiaConstants.gaiaScreenshotter);
            if (shotterObj == null)
            {
                if (gaiaSettings == null)
                {
                    gaiaSettings = GaiaUtils.GetGaiaSettings();
                }
                shotterObj = new GameObject(GaiaConstants.gaiaScreenshotter);
                Gaia.ScreenShotter shotter = shotterObj.AddComponent<Gaia.ScreenShotter>();
                shotter.m_targetDirectory = gaiaSettings.m_screenshotsDirectory.Replace("Assets/", "");
                shotter.m_watermark = PWCommon4.AssetUtils.GetAsset("Made With Gaia Watermark.png", typeof(Texture2D)) as Texture2D;
                shotter.m_mainCamera = GaiaUtils.GetCamera();
                GameObject gaiaObj = GaiaUtils.GetRuntimeSceneObject();
                shotterObj.transform.parent = gaiaObj.transform;
                shotterObj.transform.position = Gaia.TerrainHelper.GetActiveTerrainCenter(false);
            }
            return shotterObj;
        }

        #endregion

        #region Setup Tab Functions

        /// <summary>
        /// Setup panel settings
        /// </summary>
        /// <param name="helpEnabled"></param>
        private void SetupTab()
        {
            m_editorUtils.Panel("Setup", DrawSetup, true);

        }

        private void DrawSetup(bool obj)
        {
            bool originalGUIState = GUI.enabled;

            if (EditorApplication.isCompiling)
            {
                GUI.enabled = false;
            }
            m_editorUtils.Panel("MaintenanceSettings", DrawMaintenanceSettings, false);
            m_editorUtils.Panel("NewsSettings", DrawNewsSettings, false);
            m_editorUtils.Panel("RenderSettings", DrawRenderSettings, m_renderPipelineDefaultStatus);
            m_editorUtils.Panel("XRSettings", DrawXRSettings, false);
            GUI.enabled = originalGUIState;
        }

        private void DrawNewsSettings(bool helpEnabled)
        {
            m_editorUtils.InlineHelp("NewsSystemHelp", helpEnabled);
            //m_editorUtils.Heading("NewsSystemHeading");
            m_editorUtils.Text("NewsSystemText");
            string prefString = "PWShowNews" + PWApp.CONF.NameSpace;
            GUILayout.BeginHorizontal();
            EditorPrefs.SetBool(prefString, m_editorUtils.Toggle("NewsSystemToggle", EditorPrefs.GetBool(prefString, true)));
            if (m_editorUtils.Button("NewsSystemUpdateNow"))
            {
                m_editorUtils.ForceNewsUpdate();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawMaintenanceSettings(bool helpEnabled)
        {
            EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("MaintenanceInfoText"), MessageType.Info);
            if (m_editorUtils.Button("StartMaintenanceButton"))
            {
                DoMaintenance(true);
            }

        }

        private void DrawXRSettings(bool helpEnabled)
        {
#if !GAIA_XR

            m_editorUtils.Text("InstallXRSupportHelp");
            GUILayout.Space(5);
            m_editorUtils.Link("XRSetupHelpLink");
            m_editorUtils.Link("XRInteractionToolkitLink");
            GUILayout.Space(5);
            if (m_editorUtils.Button("AddXRSupport"))
            {
                bool isChanged = false;
                string currBuildSettings = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                if (!currBuildSettings.Contains("GAIA_XR"))
                {
                    if (string.IsNullOrEmpty(currBuildSettings))
                    {
                        currBuildSettings = "GAIA_XR";
                    }
                    else
                    {
                        currBuildSettings += ";GAIA_XR";
                    }
                    isChanged = true;
                }
                if (isChanged)
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, currBuildSettings);
                    string packagePath = GaiaUtils.GetAssetPath(m_settings.m_XRControllerPackageName);
                    if (!string.IsNullOrEmpty(packagePath))
                    {
                        AssetDatabase.ImportPackage(packagePath, false);
                    }
                    Debug.Log("Gaia XR Support activated.");
                }
            }
#else
            m_editorUtils.Text("XRSupportInstalled");

            if (m_editorUtils.Button("RemoveXRSupport"))
            {
                bool isChanged = false;
                string currBuildSettings = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                if (currBuildSettings.Contains("GAIA_XR"))
                {
                    currBuildSettings = currBuildSettings.Replace("GAIA_XR;", "");
                    currBuildSettings = currBuildSettings.Replace("GAIA_XR", "");
                    isChanged = true;
                }
                if (isChanged)
                {
                    if (m_settings.m_currentController == GaiaConstants.EnvironmentControllerType.XRController)
                    {
                        m_settings.m_currentController = GaiaConstants.EnvironmentControllerType.FlyingCamera;
                    }
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, currBuildSettings);
                    Debug.Log("Gaia XR Support removed.");
                }
            }
#endif
        }

        private void DrawRenderSettings(bool helpEnabled)
        {
            bool currentGUIState = GUI.enabled;
            m_editorUtils.Heading("RenderSettingsStatus");
            m_editorUtils.InlineHelp("RenderSettingsStatusHelp", helpEnabled);
            bool statusAllOK = true;


#if !UNITY_POST_PROCESSING_STACK_V2
            if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.BuiltIn)
            {
                statusAllOK = false;
                m_editorUtils.InlineHelp("PostProcessingSetupHelp", helpEnabled);
                EditorGUILayout.HelpBox("Post Processing V2 is not installed in this project, please install Post Processing from the package manager. You can find out more about the post processing package for the built-in pipeline by following the link below.", MessageType.Warning);
                m_editorUtils.Link("PostProcessingLink");
                GUILayout.Space(5f);
            }
#endif

            Color regularBGColor = GUI.backgroundColor;
            Color buttonColor = GUI.backgroundColor;
            GaiaConstants.EnvironmentRenderer activePipeline = GaiaUtils.GetActivePipeline();
            if (m_settings.m_pipelineProfile.m_activePipelineInstalled != activePipeline)
            {
                string gaiaPipeline = "Unknown";
                string graphicsSettingsPipeline = "Unknown";

                switch (m_settings.m_pipelineProfile.m_activePipelineInstalled)
                {
                    case GaiaConstants.EnvironmentRenderer.BuiltIn:
                        gaiaPipeline = "Built-In Rendering";
                        break;
                    case GaiaConstants.EnvironmentRenderer.Lightweight:
                        gaiaPipeline = "LWRP";
                        break;
                    case GaiaConstants.EnvironmentRenderer.Universal:
                        gaiaPipeline = "URP";
                        break;
                    case GaiaConstants.EnvironmentRenderer.HighDefinition:
                        gaiaPipeline = "HDRP";
                        break;
                }

                switch (activePipeline)
                {
                    case GaiaConstants.EnvironmentRenderer.BuiltIn:
                        graphicsSettingsPipeline = "Built-In Rendering";
                        break;
                    case GaiaConstants.EnvironmentRenderer.Lightweight:
                        graphicsSettingsPipeline = "LWRP";
                        break;
                    case GaiaConstants.EnvironmentRenderer.Universal:
                        graphicsSettingsPipeline = "URP";
                        break;
                    case GaiaConstants.EnvironmentRenderer.HighDefinition:
                        graphicsSettingsPipeline = "HDRP";
                        break;
                }
                statusAllOK = false;

                string pipelineWarningText = "The render pipeline asset in the project graphics settings does not match the render pipeline configured for Gaia. \r\n\r\n You can use the controls below to update Gaia to the correct render pipeline (select pipeline, click 'Upgrade To...' button). Alternatively you can select a different render pipeline settings asset in the graphics settings of this project.\r\n\r\n" +
                "Render Pipeline according to Graphics Settings: " + graphicsSettingsPipeline + "\r\n" +
                "Render Pipeline configured for Gaia: " + gaiaPipeline;
                EditorGUILayout.HelpBox(pipelineWarningText, MessageType.Warning);
                buttonColor = Color.red;
            }


            if (!String.IsNullOrEmpty(m_setupWarningText))
            {
                EditorGUILayout.HelpBox(m_setupWarningText, MessageType.Warning);
                statusAllOK = false;
            }
            else
            {
                if (PlayerSettings.colorSpace != ColorSpace.Linear)
                {
                    statusAllOK = false;
                    EditorGUILayout.HelpBox("This project is not set to Linear Color Space, which is usually NOT recommended unless you are targeting specific devices that do not support linear color space. You can switch to linear color space with the button below. If you are unsure about this decision, please visit the Link below to read more about color spaces in the Unity Manual", MessageType.Warning);
                    m_editorUtils.Link("ColorSpaceLink");
                    if (m_editorUtils.Button("SetLinearColorSpace"))
                    {
                        var manager = GetWindow<GaiaManagerEditor>();

                        if (EditorUtility.DisplayDialog(
                        m_editorUtils.GetTextValue("SettingLinear"),
                        m_editorUtils.GetTextValue("SetLinear"),
                        m_editorUtils.GetTextValue("Yes"), m_editorUtils.GetTextValue("Cancel")))
                        {
                            manager.Close();
                            PlayerSettings.colorSpace = ColorSpace.Linear;
                            EditorGUIUtility.ExitGUI();
                        }
                    }
                }
                if (activePipeline == GaiaConstants.EnvironmentRenderer.BuiltIn)
                {
                    if (m_settings.m_currentEnvironment == GaiaConstants.EnvironmentTarget.Desktop ||
                       m_settings.m_currentEnvironment == GaiaConstants.EnvironmentTarget.PowerfulDesktop ||
                       m_settings.m_currentEnvironment == GaiaConstants.EnvironmentTarget.Custom)
                    {
                        var tier1 = EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier1);
                        var tier2 = EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier2);
                        var tier3 = EditorGraphicsSettings.GetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier3);
                        if (tier1.renderingPath != RenderingPath.DeferredShading)
                        {
                            statusAllOK = false;
                            EditorGUILayout.HelpBox("This project is not set to the deferred rendering path, which is recommended when targeting desktop devices (PC / MAC) in built-in rendering. You can switch to the deferred rendering path with the button below. If you are unsure about this decision, please visit the Link below to read more about forward / deferred rendering paths in the Unity Manual", MessageType.Warning);
                            m_editorUtils.Link("DeferredLink");
                            if (m_editorUtils.Button("SetDeferredRenderingPath"))
                            {
                                var manager = GetWindow<GaiaManagerEditor>();

                                if (EditorUtility.DisplayDialog(
                                m_editorUtils.GetTextValue("SettingDeferred"),
                                m_editorUtils.GetTextValue("SetDeferred"),
                                m_editorUtils.GetTextValue("Yes"), m_editorUtils.GetTextValue("Cancel")))
                                {
                                    tier1.renderingPath = RenderingPath.DeferredShading;
                                    EditorGraphicsSettings.SetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier1, tier1);

                                    tier2.renderingPath = RenderingPath.DeferredShading;
                                    EditorGraphicsSettings.SetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier2, tier2);

                                    tier3.renderingPath = RenderingPath.DeferredShading;
                                    EditorGraphicsSettings.SetTierSettings(EditorUserBuildSettings.selectedBuildTargetGroup, GraphicsTier.Tier3, tier3);
#if UNITY_2020_1_OR_NEWER
                                LightingSettings currentLightingSettings = new LightingSettings();
                                if (!Lightmapping.TryGetLightingSettings(out currentLightingSettings))
                                {
                                    GaiaLighting.CreateLightingSettingsAsset();
                                    Lightmapping.lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
                                    Lightmapping.lightingSettings.realtimeGI = true;
                                    Lightmapping.lightingSettings.bakedGI = true;
                                    Lightmapping.lightingSettings.indirectResolution = 2f;
                                    Lightmapping.lightingSettings.lightmapResolution = 40f;
                                    Lightmapping.lightingSettings.indirectScale = 2f;
                                    if (Lightmapping.lightingSettings.autoGenerate == true)
                                    {
                                        Lightmapping.lightingSettings.autoGenerate = false;
                                    }
                                }
#else
                                    LightmapEditorSettings.lightmapper = LightmapEditorSettings.Lightmapper.ProgressiveGPU;
                                    Lightmapping.realtimeGI = true;
                                    Lightmapping.bakedGI = true;
                                    LightmapEditorSettings.realtimeResolution = 2f;
                                    LightmapEditorSettings.bakeResolution = 40f;
                                    Lightmapping.indirectOutputScale = 2f;
                                    if (Lightmapping.giWorkflowMode == Lightmapping.GIWorkflowMode.Iterative)
                                    {
                                        Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
                                    }
#endif
                                    RenderSettings.defaultReflectionResolution = 256;
                                    if (QualitySettings.shadowDistance < 350f)
                                    {
                                        QualitySettings.shadowDistance = 350f;
                                    }

                                    if (GameObject.Find("Directional light") != null)
                                    {
                                        RenderSettings.sun = GameObject.Find("Directional light").GetComponent<Light>();
                                    }
                                    else if (GameObject.Find("Directional Light") != null)
                                    {
                                        RenderSettings.sun = GameObject.Find("Directional Light").GetComponent<Light>();
                                    }
                                }
                                CheckForSetupIssues();
                            }
                        }
                    }
                }
            }
            if (statusAllOK)
            {
                EditorGUILayout.HelpBox("Gaia Manager found no issues with the rendering settings in your project.", MessageType.Info);
            }

            bool enablePackageButton = false;

            EditorGUILayout.Space();
            m_editorUtils.Heading("RenderPipeline");
            m_editorUtils.InlineHelp("InstallPipelineHelp", helpEnabled);
            if (m_settings.m_pipelineProfile.m_activePipelineInstalled == GaiaConstants.EnvironmentRenderer.BuiltIn)
            {
                EditorGUILayout.BeginHorizontal();


                string[] displayedOptions;
                int[] optionValues;

#if UNITY_2019_3_OR_NEWER
                displayedOptions = new string[3] { "BuiltIn", "Universal", "HighDefinition" };
                optionValues = new int[3] { 0, 2, 3 };

#else
                displayedOptions = new string[3] { "BuiltIn", "Lightweight", "HighDefinition" };
                optionValues = new int[3] { 0, 1, 3 };
#endif


                m_editorUtils.Label("RenderPipelineSettings");


                int selectedRenderer = EditorGUILayout.IntPopup((int)m_settings.m_currentRenderer, displayedOptions, optionValues);
                m_settings.m_currentRenderer = (GaiaConstants.EnvironmentRenderer)selectedRenderer;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                //m_editorUtils.LabelField("RenderPipeline");
                EditorGUILayout.LabelField("Installed " + m_settings.m_pipelineProfile.m_activePipelineInstalled.ToString());
                EditorGUILayout.EndHorizontal();
            }

            m_editorUtils.InlineHelp("RenderPipelineSetupHelp", helpEnabled);

            GUI.backgroundColor = buttonColor;

            //Revert back to built-in renderer
            if (m_settings.m_pipelineProfile.m_activePipelineInstalled != m_settings.m_currentRenderer)
            {
                if (m_settings.m_pipelineProfile.m_activePipelineInstalled != GaiaConstants.EnvironmentRenderer.BuiltIn && m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.BuiltIn)
                {
                    if (m_editorUtils.Button("RevertGaiaBackToBuiltIn"))
                    {
                        if (EditorUtility.DisplayDialog("Upgrading Gaia Pipeline!", "You're about to revert gaia back to use Built-In render pipeline. Are you sure you want to proceed?", "Yes", "No"))
                        {
                            m_enableGUI = true;
                            if (m_settings.m_pipelineProfile.m_activePipelineInstalled == GaiaConstants.EnvironmentRenderer.Lightweight)
                            {
                                m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.BuiltIn;
                                GaiaLWRPPipelineUtils.CleanUpLWRP(m_settings.m_pipelineProfile, m_settings);
                            }
                            else if (m_settings.m_pipelineProfile.m_activePipelineInstalled == GaiaConstants.EnvironmentRenderer.HighDefinition)
                            {
                                m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.BuiltIn;
                                GaiaHDRPPipelineUtils.CleanUpHDRP(m_settings.m_pipelineProfile, m_settings);
                            }
                            else if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.Universal)
                            {
                                GaiaURPPipelineUtils.CleanUpURP(m_settings.m_pipelineProfile, m_settings);
                            }
                        }
                    }
                }
                else
                {
                    //Disable Install button
                    m_enableGUI = true;
                    enablePackageButton = true;
                    GaiaPackageVersion unityVersion = GetPackageVersion();
                    UnityVersionPipelineAsset mapping = null;
                    switch (m_settings.m_currentRenderer)
                    {
                        //HDRP Version Limit
                        //URP Version Limit
                        case GaiaConstants.EnvironmentRenderer.HighDefinition:
                            {
                                mapping = m_settings.m_pipelineProfile.m_highDefinitionPipelineProfiles.Find(x => x.m_unityVersion == unityVersion);
                                if (mapping != null)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    m_editorUtils.LabelField("MinHDRP");
                                    EditorGUILayout.LabelField(mapping.m_minHDRPVersion);
                                    EditorGUILayout.EndHorizontal();
                                    EditorGUILayout.BeginHorizontal();
                                    m_editorUtils.LabelField("MaxHDRP");
                                    EditorGUILayout.LabelField(mapping.m_maxHDRPVersion);
                                    EditorGUILayout.EndHorizontal();
                                }
                                break;
                            }
                        case GaiaConstants.EnvironmentRenderer.Universal:
                            {
                                mapping = m_settings.m_pipelineProfile.m_universalPipelineProfiles.Find(x => x.m_unityVersion == unityVersion);
                                if (mapping != null)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    m_editorUtils.LabelField("MinUPRP");
                                    EditorGUILayout.LabelField(mapping.m_minURPVersion);
                                    EditorGUILayout.EndHorizontal();
                                    EditorGUILayout.BeginHorizontal();
                                    m_editorUtils.LabelField("MaxUPRP");
                                    EditorGUILayout.LabelField(mapping.m_maxURPVersion);
                                    EditorGUILayout.EndHorizontal();
                                }
                                break;
                            }
                    }

                    //Upgrade to LWRP/HDRP
                    if (m_editorUtils.Button("UpgradeGaiaTo" + m_settings.m_currentRenderer.ToString()))
                    {
                        if (EditorUtility.DisplayDialog("Upgrading Gaia Pipeline", "You're about to change Gaia to use " + m_settings.m_currentRenderer.ToString() + " render pipeline. Are you sure you want to proceed?", "Yes", "No"))
                        {
                            m_enableGUI = true;
                            if (EditorUtility.DisplayDialog("Save Scene", "Would you like to save your scene before switching render pipeline?", "Yes", "No"))
                            {
                                if (EditorSceneManager.GetActiveScene().isDirty)
                                {
                                    EditorSceneManager.SaveOpenScenes();
                                }

                                AssetDatabase.SaveAssets();
                            }

                            if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.HighDefinition)
                            {
                                EditorUtility.DisplayProgressBar("Installing " + m_settings.m_currentRenderer.ToString(), "Preparing To Install " + m_settings.m_currentRenderer.ToString(), 0f);
                                GaiaHDRPPipelineUtils.m_waitTimer1 = 1f;
                                GaiaHDRPPipelineUtils.m_waitTimer2 = 3f;
                                GaiaHDRPPipelineUtils.SetPipelineAsset(m_settings.m_pipelineProfile);
                            }
                            else if (m_settings.m_currentRenderer == GaiaConstants.EnvironmentRenderer.Universal)
                            {
                                EditorUtility.DisplayProgressBar("Installing " + m_settings.m_currentRenderer.ToString(), "Preparing To Install " + m_settings.m_currentRenderer.ToString(), 0f);
                                GaiaURPPipelineUtils.m_waitTimer1 = 1f;
                                GaiaURPPipelineUtils.m_waitTimer2 = 3f;
                                GaiaURPPipelineUtils.SetPipelineAsset(m_settings.m_pipelineProfile);
                            }
                        }
                    }
                }
            }
            else
            {
                //Revert back to built-in renderer
                if (m_settings.m_pipelineProfile.m_activePipelineInstalled != GaiaConstants.EnvironmentRenderer.BuiltIn)
                {
                    if (m_editorUtils.Button("RevertGaiaBackToBuiltIn"))
                    {
                        if (EditorUtility.DisplayDialog("Upgrading Gaia Pipeline", "You're about to revert Gaia back to use Built-In render pipeline. Are you sure you want to proceed?", "Yes", "No"))
                        {
                            m_enableGUI = true;
                            if (EditorUtility.DisplayDialog("Save Scene", "Would you like to save your scene before switching render pipeline?", "Yes", "No"))
                            {
                                if (EditorSceneManager.GetActiveScene().isDirty)
                                {
                                    EditorSceneManager.SaveOpenScenes();
                                }

                                AssetDatabase.SaveAssets();
                            }


                            if (m_settings.m_pipelineProfile.m_activePipelineInstalled == GaiaConstants.EnvironmentRenderer.Lightweight)
                            {
                                m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.BuiltIn;
                                GaiaLWRPPipelineUtils.CleanUpLWRP(m_settings.m_pipelineProfile, m_settings);
                            }
                            else if (m_settings.m_pipelineProfile.m_activePipelineInstalled == GaiaConstants.EnvironmentRenderer.HighDefinition)
                            {
                                m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.BuiltIn;
                                GaiaHDRPPipelineUtils.CleanUpHDRP(m_settings.m_pipelineProfile, m_settings);
                            }
                            else if (m_settings.m_pipelineProfile.m_activePipelineInstalled == GaiaConstants.EnvironmentRenderer.Universal)
                            {
                                m_settings.m_currentRenderer = GaiaConstants.EnvironmentRenderer.BuiltIn;
                                GaiaURPPipelineUtils.CleanUpURP(m_settings.m_pipelineProfile, m_settings);
                            }
                        }
                    }
                }
            }

            GUI.backgroundColor = regularBGColor;

            if (!m_enableGUI)
            {
                ShaderMappingEntry[] materialLibrary = null;
                GaiaPackageVersion unityVersion = GetPackageVersion();
                GetPackages(unityVersion, out materialLibrary, out enablePackageButton);

                GUILayout.Space(5f);

                //m_editorUtils.Heading("PackagesThatWillBeInstalled");

                if (!enablePackageButton)
                {
                    EditorGUILayout.HelpBox("Shader Installation is not yet supported on this version of Unity.", MessageType.Info);
                    GUI.enabled = false;
                }

                GUI.backgroundColor = Color.red;

                if (EditorApplication.isCompiling)
                {
                    GUI.enabled = false;
                }

                if (m_editorUtils.Button("InstallPackages"))
                {
                    ProceduralWorlds.Gaia.PackageSystem.PackageInstallerUtils.m_installShaders = true;
                    ProceduralWorlds.Gaia.PackageSystem.PackageInstallerUtils.m_timer = 7f;
                    ProceduralWorlds.Gaia.PackageSystem.PackageInstallerUtils.StartInstallation(Application.unityVersion, m_settings.m_currentRenderer, materialLibrary, m_settings.m_pipelineProfile);
                }

                m_editorUtils.InlineHelp("PackageInstallSetupHelp", helpEnabled);

                GUI.enabled = currentGUIState;
            }

            GUI.backgroundColor = m_defaultPanelColor;
        }


        public static GaiaPackageVersion GetPackageVersion()
        {
            if (Application.unityVersion.Contains("2019.3"))
            {
                return GaiaPackageVersion.Unity2019_3;
            }
            else if (Application.unityVersion.Contains("2019.4"))
            {
                return GaiaPackageVersion.Unity2019_4;
            }
            else if (Application.unityVersion.Contains("2020.1"))
            {
                return GaiaPackageVersion.Unity2020_1;
            }
            else if (Application.unityVersion.Contains("2020.2"))
            {
                return GaiaPackageVersion.Unity2020_2;
            }
            else if (Application.unityVersion.Contains("2020.3"))
            {
                return GaiaPackageVersion.Unity2020_3;
            }
            else if (Application.unityVersion.Contains("2020.4"))
            {
                return GaiaPackageVersion.Unity2020_4;
            }
            else if (Application.unityVersion.Contains("2021.1"))
            {
                return GaiaPackageVersion.Unity2021_1;
            }
            else if (Application.unityVersion.Contains("2021.2"))
            {
                return GaiaPackageVersion.Unity2021_2;
            }
            else if (Application.unityVersion.Contains("2021.3"))
            {
                return GaiaPackageVersion.Unity2021_3;
            }
            else if (Application.unityVersion.Contains("2021.4"))
            {
                return GaiaPackageVersion.Unity2021_4;
            }
            else if (Application.unityVersion.Contains("2022.1"))
            {
                return GaiaPackageVersion.Unity2022_1;
            }
            else if (Application.unityVersion.Contains("2022.2"))
            {
                return GaiaPackageVersion.Unity2022_2;
            }
            else if (Application.unityVersion.Contains("2022.3"))
            {
                return GaiaPackageVersion.Unity2022_3;
            }
            else if (Application.unityVersion.Contains("2022.4"))
            {
                return GaiaPackageVersion.Unity2022_4;
            }
            else if (Application.unityVersion.Contains("2023.1"))
            {
                return GaiaPackageVersion.Unity2023_1;
            }

            return GaiaPackageVersion.Unity2023_1;
        }

        /// <summary>
        /// System info settings
        /// </summary>
        /// <param name="helpEnabled"></param>
        private bool m_copySettingsToClipboard = false;
        private void SystemInfoSettingsEnabled(bool helpEnabled)
        {
            StringBuilder clipStringBuilder = null;

            if (m_copySettingsToClipboard)
            {
                clipStringBuilder = new StringBuilder();
            }

            m_editorUtils.Heading("UnityInfo");
            EditorGUILayout.LabelField("Unity Version: " + Application.unityVersion);
            EditorGUILayout.LabelField("Company Name: " + Application.companyName);
            EditorGUILayout.LabelField("Product Name: " + Application.productName);
            EditorGUILayout.LabelField("Project Version: " + Application.version);
            EditorGUILayout.LabelField("Project Data Path: " + Application.dataPath);
            if (GraphicsSettings.renderPipelineAsset == null)
            {
                EditorGUILayout.LabelField("Render Pipeline: Builtin");
            }
            else
            {
                Type rpType = GraphicsSettings.renderPipelineAsset.GetType();
                if (rpType.FullName.Contains("HighDefinition"))
                {
                    EditorGUILayout.LabelField("Render Pipeline: High Definition");
                }
                else
                {
                    EditorGUILayout.LabelField("Render Pipeline: URP");
                }
            }

            if (m_copySettingsToClipboard)
            {
#if GAIA_PRO_PRESENT
                clipStringBuilder.AppendLine("Gaia Version: " + PWApp.CONF.Version + " PRO");
#else
                clipStringBuilder.AppendLine("Gaia Version: " + PWApp.CONF.Version);
#endif
                clipStringBuilder.AppendLine("");
                clipStringBuilder.AppendLine("Unity Info:");
                clipStringBuilder.AppendLine("Unity Version: " + Application.unityVersion);
                clipStringBuilder.AppendLine("Company Name: " + Application.companyName);
                clipStringBuilder.AppendLine("Product Name: " + Application.productName);
                clipStringBuilder.AppendLine("Project Version: " + Application.version);
                clipStringBuilder.AppendLine("Project Data Path: " + Application.dataPath);
                if (GraphicsSettings.renderPipelineAsset == null)
                {
                    clipStringBuilder.AppendLine("Render Pipeline: Builtin");
                }
                else
                {
                    Type rpType = GraphicsSettings.renderPipelineAsset.GetType();
                    if (rpType.FullName.Contains("HighDefinition"))
                    {
                        clipStringBuilder.AppendLine("Render Pipeline: High Definition");
                    }
                    else
                    {
                        clipStringBuilder.AppendLine("Render Pipeline: URP");
                    }
                }
            }

            EditorGUILayout.Space();
            m_editorUtils.Heading("SystemInfo");

            EditorGUILayout.LabelField("Operating System: " + SystemInfo.operatingSystem);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Supported Systems Info", EditorStyles.boldLabel);
            if (SystemInfo.supportsInstancing)
            {
                EditorGUILayout.LabelField("Supports GPU Instancing: Yes");
            }
            else
            {
                EditorGUILayout.LabelField("Supports GPU Instancing: No");
            }
            if (SystemInfo.supportsRayTracing)
            {
                EditorGUILayout.LabelField("Supports Ray Tracing: Yes");
            }
            else
            {
                EditorGUILayout.LabelField("Supports Ray Tracing: No");
            }
            if (SystemInfo.supportsAsyncCompute)
            {
                EditorGUILayout.LabelField("Supports Async Compute: Yes");
            }
            else
            {
                EditorGUILayout.LabelField("Supports Async Compute: No");
            }
            if (SystemInfo.supportsAsyncGPUReadback)
            {
                EditorGUILayout.LabelField("Supports GPU Async Compute: Yes");
            }
            else
            {
                EditorGUILayout.LabelField("Supports GPU Async Compute: No");
            }
            if (SystemInfo.supportsMipStreaming)
            {
                EditorGUILayout.LabelField("Supports Texture Streaming: Yes");
            }
            else
            {
                EditorGUILayout.LabelField("Supports Texture Streaming: No");
            }

            if (m_copySettingsToClipboard)
            {
                clipStringBuilder.AppendLine("");
                clipStringBuilder.AppendLine("System Info:");
                clipStringBuilder.AppendLine("Operating System: " + SystemInfo.operatingSystem);
                if (SystemInfo.supportsInstancing)
                {
                    clipStringBuilder.AppendLine("Supports GPU Instancing: Yes");
                }
                else
                {
                    clipStringBuilder.AppendLine("Supports GPU Instancing: No");
                }
                if (SystemInfo.supportsRayTracing)
                {
                    clipStringBuilder.AppendLine("Supports Ray Tracing: Yes");
                }
                else
                {
                    clipStringBuilder.AppendLine("Supports Ray Tracing: No");
                }
                if (SystemInfo.supportsAsyncCompute)
                {
                    clipStringBuilder.AppendLine("Supports Async Compute: Yes");
                }
                else
                {
                    clipStringBuilder.AppendLine("Supports Async Compute: No");
                }
                if (SystemInfo.supportsAsyncGPUReadback)
                {
                    clipStringBuilder.AppendLine("Supports GPU Async Compute: Yes");
                }
                else
                {
                    clipStringBuilder.AppendLine("Supports GPU Async Compute: No");
                }
                if (SystemInfo.supportsMipStreaming)
                {
                    clipStringBuilder.AppendLine("Supports Texture Streaming: Yes");
                }
                else
                {
                    clipStringBuilder.AppendLine("Supports Texture Streaming: No");
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Graphics Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Graphics Card Vendor: " + SystemInfo.graphicsDeviceVendor);
            EditorGUILayout.LabelField("Graphics Card Name: " + SystemInfo.graphicsDeviceName);
            EditorGUILayout.LabelField("Graphics Card Version: " + SystemInfo.graphicsDeviceVersion);
            EditorGUILayout.LabelField("Graphics Driver Version: " + SystemInfo.graphicsDeviceType);
            EditorGUILayout.LabelField("Graphics Card Memory: " + SystemInfo.graphicsMemorySize + " MB");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Processor Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Processor Name: " + SystemInfo.processorType);
            EditorGUILayout.LabelField("Processor Core Count: " + SystemInfo.processorCount);
            EditorGUILayout.LabelField("Processor Speed: " + SystemInfo.processorFrequency + " GHz");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Memory Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Memory (RAM): " + SystemInfo.systemMemorySize + " MB");

            if (m_copySettingsToClipboard)
            {
                clipStringBuilder.AppendLine("");
                clipStringBuilder.AppendLine("Graphics Info:");
                clipStringBuilder.AppendLine("Graphics Card Vendor: " + SystemInfo.graphicsDeviceVendor);
                clipStringBuilder.AppendLine("Graphics Card Name: " + SystemInfo.graphicsDeviceName);
                clipStringBuilder.AppendLine("Graphics Card Version: " + SystemInfo.graphicsDeviceVersion);
                clipStringBuilder.AppendLine("Graphics Driver Version: " + SystemInfo.graphicsDeviceType);
                clipStringBuilder.AppendLine("Graphics Card Memory: " + SystemInfo.graphicsMemorySize + " MB");
                clipStringBuilder.AppendLine("");
                clipStringBuilder.AppendLine("Processor Info:");
                clipStringBuilder.AppendLine("Processor Name: " + SystemInfo.processorType);
                clipStringBuilder.AppendLine("Processor Core Count: " + SystemInfo.processorCount);
                clipStringBuilder.AppendLine("Processor Speed: " + SystemInfo.processorFrequency + " GHz");
                clipStringBuilder.AppendLine("");
                clipStringBuilder.AppendLine("Memory Info:");
                clipStringBuilder.AppendLine("Memory (RAM): " + SystemInfo.systemMemorySize + " MB");
            }

            if (m_copySettingsToClipboard)
            {
                EditorGUIUtility.systemCopyBuffer = clipStringBuilder.ToString();
            }
            m_copySettingsToClipboard = false;
            if (m_editorUtils.ButtonRight("CopyToClipboardButton", null))
            {
                m_copySettingsToClipboard = true;
            }
        }

        /// <summary>
        /// Check if shaders need to be installed
        /// </summary>
        /// <returns></returns>
        private bool MissingShaders()
        {
            //Currently not needed, need to see if we want to re-enable this for a different directory

            //bool exist = false;
            //m_enableGUI = false;

            //string[] folders = Directory.GetDirectories(Application.dataPath, ".", SearchOption.AllDirectories);
            //foreach (string folder in folders)
            //{
            //    if (folder.Contains("PW Shader Library"))
            //    {
            //        m_enableGUI = true;
            //        exist = true;
            //    }
            //}

            //return exist;

            return true;
        }

        /// <summary>
        /// Check the project for files and check if needs to be installed
        /// </summary>
        /// <returns></returns>
        private bool AreShadersInstalledCorrectly(GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            //Collect all "migratable" shader names for this pipeline - If a material has a shader with this name it normally
            //should have been migrated to a different shader during the shader installation process. If we can find such a material
            //within the Gaia folder, chances are the Shader installation did not run correctly.
            List<string> migratableShaderNames = new List<string>();
            foreach (ShaderMappingEntry entry in m_settings.m_pipelineProfile.m_ShaderMappingLibrary)
            {
                switch (renderPipeline)
                {
                    case GaiaConstants.EnvironmentRenderer.BuiltIn:
                        migratableShaderNames.Add(entry.m_URPReplacementShaderName);
                        migratableShaderNames.Add(entry.m_HDRPReplacementShaderName);
                        break;
                    case GaiaConstants.EnvironmentRenderer.Lightweight:
                        //not supported anymore
                        break;
                    case GaiaConstants.EnvironmentRenderer.Universal:
                        migratableShaderNames.Add(entry.m_builtInShaderName);
                        migratableShaderNames.Add(entry.m_HDRPReplacementShaderName);
                        break;
                    case GaiaConstants.EnvironmentRenderer.HighDefinition:
                        migratableShaderNames.Add(entry.m_builtInShaderName);
                        migratableShaderNames.Add(entry.m_URPReplacementShaderName);
                        break;
                }
            }


            //Look for all materials inside the Gaia Directory
            string[] materialGUIDs = AssetDatabase.FindAssets("t:Material", new string[1] { GaiaDirectories.GetGaiaDirectory() });
            List<Material> collectedMaterials = new List<Material>();
            //Iterate through the guids, load the material, if it still uses a "migratable" shader then there is an issue with the shader installation
            foreach (string guid in materialGUIDs)
            {
                Material mat = (Material)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(Material));
                if (migratableShaderNames.Contains(mat.shader.name))
                {
                    return false;
                }
            }


            //if (m_settings != null)
            //{
            //    foreach (ShaderMappingEntry entry in m_settings.m_pipelineProfile.m_ShaderMappingLibrary)
            //    {
            //        string targetShaderName = entry.m_builtInShaderName;
            //        switch (renderPipeline)
            //        {
            //            case GaiaConstants.EnvironmentRenderer.BuiltIn:
            //                targetShaderName = entry.m_builtInShaderName;
            //                break;
            //            case GaiaConstants.EnvironmentRenderer.Lightweight:
            //                //not supported anymore                           
            //                break;
            //            case GaiaConstants.EnvironmentRenderer.Universal:
            //                targetShaderName = entry.m_URPReplacementShaderName;
            //                break;
            //            case GaiaConstants.EnvironmentRenderer.HighDefinition:
            //                targetShaderName = entry.m_HDRPReplacementShaderName;
            //                break;
            //        }

            //        //List<Material> entryMaterials = entry.m_materials;

            //        Shader targetShader = Shader.Find(targetShaderName);

            //        if (targetShader == null)
            //        {
            //            //Debug.LogError("Target shader for material library entry " + entry.m_name + " not found!");
            //            continue;
            //        }

            //        //foreach (Material material in entryMaterials)
            //        //{
            //        //    if (material != null)
            //        //    {
            //        //        string path = AssetDatabase.GetAssetPath(material);
            //        //        if (material.shader.name.StartsWith("Hidden/InternalErrorShader") || material.shader != targetShader)
            //        //        {
            //        //            return false;
            //        //        }
            //        //    }
            //        //}
            //    }
            //}
            return true;
        }

        /// <summary>
        /// Gets the unity packages shaders/setup
        /// </summary>
        /// <param name="unityVersion"></param>
        /// <param name="shaderLibrary"></param>
        private void GetPackages(GaiaPackageVersion unityVersion, out ShaderMappingEntry[] materialLibrary, out bool isSupported)
        {
            isSupported = false;

            materialLibrary = null;

            if (m_settings == null)
            {
                m_settings = GaiaUtils.GetGaiaSettings();
            }

            if (m_gaiaPipelineSettings == null)
            {
                m_gaiaPipelineSettings = m_settings.m_pipelineProfile;
            }

            if (m_gaiaPipelineSettings == null)
            {
                Debug.LogError("Gaia Pipeline Profile is empty, check the Gaia Settings to ensure the profile is defined.");
                return;
            }

            foreach (GaiaPackageSettings packageSettings in m_gaiaPipelineSettings.m_packageSetupSettings)
            {
                if (packageSettings.m_unityVersion == unityVersion)
                {
                    materialLibrary = m_gaiaPipelineSettings.m_ShaderMappingLibrary;

                    if (m_settings.m_pipelineProfile.m_activePipelineInstalled == GaiaConstants.EnvironmentRenderer.BuiltIn)
                    {
                        isSupported = packageSettings.m_isSupported;
                    }
                    else if (m_settings.m_pipelineProfile.m_activePipelineInstalled == GaiaConstants.EnvironmentRenderer.Lightweight)
                    {
                        isSupported = packageSettings.m_isSupported;
                    }
                    else
                    {
                        isSupported = packageSettings.m_isSupported;
                    }
                }
            }

        }

        /// <summary>
        /// Checks the gaia manager and updates the bool checks
        /// </summary>
        public void GaiaManagerStatusCheck(bool force = false)
        {
            if (!m_statusCheckPerformed || force)
            {
                //Do Maintenance Tasks (Delete unneccessary files, etc.)
                if (!Application.isPlaying)
                {
                    DoMaintenance();
                }

                //Check if shaders are missing
                m_shadersNotImported = MissingShaders();

                //disabled for now
                m_enableGUI = true;
                m_enableGUI = AreShadersInstalledCorrectly(m_settings.m_pipelineProfile.m_activePipelineInstalled);

                if (m_enableGUI)
                {
                    m_showSetupPanel = false;
                }
                else
                {
                    m_showSetupPanel = true;
                }
                m_statusCheckPerformed = true;
            }
        }

        /// <summary>
        /// Performs maintenance tasks such as deleting unwanted files from earlier Gaia installations
        /// </summary>
        /// <param name="forceMaintenanceRun">If the maintenance should be forced (will run without performing any checks) </param>
        private void DoMaintenance(bool forceMaintenanceRun = false)
        {
            //Only Perform maintenance if:
            //- not in dev environment
            //- token exists
            //Always execute if it is a forced Maintenance Run
            if ((!System.IO.Directory.Exists(GaiaUtils.GetAssetPath("Dev Utilities")) && File.Exists(GaiaDirectories.GetSettingsDirectory() + "\\" + GaiaConstants.maintenanceTokenFilename)) || forceMaintenanceRun)
            {
                //Do not ask this if it was forced & also do not ask if permission was already given / denied
                string prefString = "PWShowNews" + PWApp.CONF.NameSpace;
                if (!forceMaintenanceRun && !EditorPrefs.HasKey(prefString))
                {
                    if (EditorUtility.DisplayDialog("Install / Update detected", "It looks like you are opening the Gaia Manager the first time after an update or the initial installation. Do you allow Gaia to display news and special offers in the Gaia Manager? It will fetch the latest news once per day from the procedural worlds website. This feature can be turned on and off in the Setup tab of the Gaia Manager anytime.", "Yes, allow News", "No thanks"))
                    {
                        EditorPrefs.SetBool(prefString, true);
                    }
                }

                if (EditorPrefs.HasKey("VerifySavingAssets"))
                {
                    if (EditorPrefs.GetBool("VerifySavingAssets"))
                    {
                        if (EditorUtility.DisplayDialog("Verify Savings Assets detected", "It looks like you are using the setting 'Verify Saving Assets' in the Editor Preferences. This setting will prompt you on every (!) asset change to ask you if you want to save the asset or not. This makes it very hard to use Gaia as it creates a lot of assets during the Terrain Creation process. Do you want to deactivate this setting now? (Recommended)", "Yes, deactivate it", "No thanks"))
                        {
                            EditorPrefs.SetBool("VerifySavingAssets", false);
                        }
                    }
                }

                //Do not ask this if it was forced
                if (!forceMaintenanceRun)
                {
                    if (EditorUtility.DisplayDialog("Install / Update detected", "The Gaia Manager will now perform a few maintenance tasks to set up Gaia correctly. Do you want to visit a webpage with Gaia tutorials while this process is running?", "Yes, open website", "No thanks"))
                    {
                        Application.OpenURL("https://www.procedural-worlds.com/support/tutorials");
                    }
                }

                //Get Maintenance Profile
                GaiaMaintenanceProfile maintenanceProfile = (GaiaMaintenanceProfile)PWCommon4.AssetUtils.GetAssetScriptableObject("Gaia Maintenance Profile");

                if (maintenanceProfile == null)
                {
                    Debug.LogWarning("Could not find Gaia Maintenance Profile, maintenance tasks will be skipped.");
                    return;
                }

                List<string> paths = new List<string>();
                for (int i = 0; i < maintenanceProfile.meshColliderPrefabPaths.Length; i++)
                {
                    string path = GaiaDirectories.GetGaiaDirectory() + maintenanceProfile.meshColliderPrefabPaths[i];
                    if (Directory.Exists(path))
                    {
                        paths.Add(path);
                    }
                }

                string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab", paths.ToArray());
                List<string> allPrefabPaths = new List<string>();

                int currentGUID = 0;
                foreach (string prefabGUID in allPrefabGuids)
                {
                    EditorUtility.DisplayProgressBar(m_editorUtils.GetTextValue("MaintenanceProgressMeshTitle"), String.Format(m_editorUtils.GetTextValue("MaintenanceProgressMeshText"), currentGUID, allPrefabGuids.Length), (float)currentGUID / (float)allPrefabGuids.Length);
                    string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
                    allPrefabPaths.Add(prefabPath);
                }

                //Perform layer fix
                GaiaUtils.FixPrefabLayers(allPrefabPaths);

                //Read default biome presets
                GaiaUtils.ResetBiomePresets();

                List<string> directories = new List<string>(Directory.EnumerateDirectories(Application.dataPath, ".", SearchOption.AllDirectories));
                bool changesMade = false;
                foreach (string directory in directories)
                {
                    //Deletion Tasks
                    foreach (DeletionTask deletionTask in maintenanceProfile.m_deletionTasks)
                    {
                        if (PerformDeletionTask(directory, deletionTask))
                        {
                            changesMade = true;
                        }
                    }
                }

                //Rename Tasks
                foreach (RenameTask renameTask in maintenanceProfile.m_renameTasks)
                {
                    if (PerformRenameTask(renameTask))
                    {
                        changesMade = true;
                    }
                }

                if (changesMade)
                {
                    AssetDatabase.Refresh();
                }
            }

            //Maintenance done, remove token (if not in dev env)
            if (!System.IO.Directory.Exists(GaiaUtils.GetAssetPath("Dev Utilities")))
            {
                FileUtil.DeleteFileOrDirectory(GaiaDirectories.GetSettingsDirectory() + "\\" + GaiaConstants.maintenanceTokenFilename);
                FileUtil.DeleteFileOrDirectory(GaiaDirectories.GetSettingsDirectory() + "\\" + GaiaConstants.maintenanceTokenFilename + ".meta");
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Performs a single maintenance deletion task
        /// </summary>
        /// <param name="deletionTask"></param>
        private bool PerformDeletionTask(string directory, DeletionTask deletionTask)
        {
            if (deletionTask.m_pathContains == null || deletionTask.m_pathContains == "")
            {
                Debug.LogWarning("Empty Path for Gaia Maintenance Deletion Task is not supported.");
                return false;
            }

            if ((deletionTask.m_Name == null || deletionTask.m_Name == "") && (deletionTask.m_fileExtension == null || deletionTask.m_fileExtension == ""))
            {
                Debug.LogWarning("Deletion Task must at least contain either a name, or a file extension to check against.");
                return false;
            }

            bool changesMade = false;

            //filter out folders where the search string appears ONLY BEFORE the asset directory, 
            //e.g. because the user has their entire project placed in a folder containing the search string - we would not want to touch those
            if (directory.Contains(deletionTask.m_pathContains) && directory.LastIndexOf("Assets") < directory.LastIndexOf(deletionTask.m_pathContains))
            {
                //Check if we should perform this for subfolders as well, otherwise we will do the deletion only when the path ends with the search string
                if (deletionTask.m_includeSubDirectories || directory.EndsWith(deletionTask.m_pathContains))
                {
                    switch (deletionTask.m_taskType)
                    {
                        case DeletionTaskType.Directory:
                            if (CheckDeletionTaskCondition(deletionTask, directory, ""))
                            {
                                FileUtil.DeleteFileOrDirectory(directory);
                                changesMade = true;
                            }

                            break;
                        case DeletionTaskType.File:

                            DirectoryInfo dirInfo = new DirectoryInfo(directory);
                            var files = dirInfo.GetFiles();
                            foreach (FileInfo fileInfo in files)
                            {
                                if (CheckDeletionTaskCondition(deletionTask, fileInfo.Name.Replace(fileInfo.Extension, ""), fileInfo.Extension))
                                {
                                    FileUtil.DeleteFileOrDirectory(fileInfo.FullName);
                                    changesMade = true;
                                }
                            }

                            break;
                    }
                }
            }

            return changesMade;
        }

        /// <summary>
        /// Performs a single maintenance rename task
        /// </summary>
        /// <param name="renameTask"></param>
        private bool PerformRenameTask(RenameTask renameTask)
        {
            if (String.IsNullOrEmpty(renameTask.m_newPath) || String.IsNullOrEmpty(renameTask.m_oldPath))
            {
                Debug.LogWarning("Empty Paths for Gaia Maintenance Rename Task is not supported.");
                return false;
            }

            bool changesMade = false;

            string pathOld = GaiaDirectories.GetGaiaDirectory() + renameTask.m_oldPath;
            if (Directory.Exists(pathOld))
            {
                string pathNew = GaiaDirectories.GetGaiaDirectory() + renameTask.m_newPath;
                AssetDatabase.MoveAsset(pathOld, pathNew);
                changesMade = true;
            }

            return changesMade;
        }

        private bool CheckDeletionTaskCondition(DeletionTask deletionTask, string nameString, string extensionString)
        {
            switch (deletionTask.m_taskType)
            {
                case DeletionTaskType.Directory:
                    return (deletionTask.m_checkType == MaintenanceCheckType.Contains && nameString.Contains(deletionTask.m_Name)) ||
                               (deletionTask.m_checkType == MaintenanceCheckType.Equals && nameString.Equals(deletionTask.m_Name));
                case DeletionTaskType.File:
                    bool nameApplies = false;
                    bool extensionApplies = false;

                    //Breaking down the check into multiple if-statements, easier to read that way

                    if (deletionTask.m_Name == null || deletionTask.m_Name == "")
                    {
                        nameApplies = true;
                    }

                    if (deletionTask.m_checkType == MaintenanceCheckType.Contains && nameString.Contains(deletionTask.m_Name))
                    {
                        nameApplies = true;
                    }

                    if (deletionTask.m_checkType == MaintenanceCheckType.Equals && nameString.Equals(deletionTask.m_Name))
                    {
                        nameApplies = true;
                    }

                    if (deletionTask.m_fileExtension == null || deletionTask.m_fileExtension == "")
                    {
                        extensionApplies = true;
                    }

                    if (deletionTask.m_checkType == MaintenanceCheckType.Contains && extensionString.Contains(deletionTask.m_fileExtension))
                    {
                        extensionApplies = true;
                    }

                    if (deletionTask.m_checkType == MaintenanceCheckType.Equals && extensionString.Equals(deletionTask.m_fileExtension))
                    {
                        extensionApplies = true;
                    }

                    return nameApplies && extensionApplies;

            }

            return false;
        }

        /// <summary>
        /// Setup the material name list
        /// </summary>
        //private bool SetupMaterials(GaiaConstants.EnvironmentRenderer renderPipeline, GaiaSettings gaiaSettings, int profileIndex)
        //{
        //    bool successful = false;

        //    string[] folderPaths = Directory.GetDirectories(Application.dataPath + m_materialLocation, ".", SearchOption.AllDirectories);
        //    m_unityVersion = Application.unityVersion;
        //    m_unityVersion = m_unityVersion.Remove(m_unityVersion.LastIndexOf(".")).Replace(".", "_0");
        //    string keyWordToSearch = "";

        //    if (renderPipeline == GaiaConstants.EnvironmentRenderer.BuiltIn)
        //    {
        //        keyWordToSearch = PackageInstallerUtils.m_builtInKeyWord;
        //    }
        //    else if (renderPipeline == GaiaConstants.EnvironmentRenderer.Lightweight)
        //    {
        //        keyWordToSearch = PackageInstallerUtils.m_lightweightKeyWord;
        //    }
        //    else if (renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
        //    {
        //        keyWordToSearch = PackageInstallerUtils.m_universalKeyWord;
        //    }
        //    else
        //    {
        //        keyWordToSearch = PackageInstallerUtils.m_highDefinitionKeyWord;
        //    }

        //    string mainFolder = "";
        //    foreach (string folderPath in folderPaths)
        //    {
        //        string finalFolderName = folderPath.Substring(folderPath.LastIndexOf("\\"));

        //        if (finalFolderName.Contains(keyWordToSearch + " " + m_unityVersion))
        //        {
        //            mainFolder = finalFolderName;
        //            break;
        //        }
        //    }

        //    m_profileList.Clear();

        //    List<Material> allMaterials = GetMaterials(mainFolder);
        //    if (allMaterials != null)
        //    {
        //        foreach (Material mat in allMaterials)
        //        {
        //            m_profileList.Add(mat.name);
        //        }
        //    }
        //    //Always add the "None" option for water
        //    m_profileList.Add("None");

        //    if (allMaterials.Count > 0)
        //    {
        //        successful = true;
        //    }
        //    if (m_allMaterials[profileIndex] != null)
        //    {
        //        gaiaSettings.m_gaiaWaterProfile.m_activeWaterMaterial = m_allMaterials[profileIndex];
        //    }
        //    return successful;
        //}

        /// <summary>
        /// Removes Suffix in file formats required
        /// </summary>
        /// <param name="path"></param>
        private List<Material> GetMaterials(string path)
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

        #region System Helpers

        /// <summary>
        /// Display a button that takes editor indentation into account
        /// </summary>
        /// <param name="content">Text, image and tooltip for this button</param>
        /// <returns>True is clicked</returns>
        ///
        public bool ButtonLeftAligned(string key, params GUILayoutOption[] options)
        {
            TextAnchor oldalignment = GUI.skin.button.alignment;
            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            bool result = m_editorUtils.Button(key, options);
            GUI.skin.button.alignment = oldalignment;
            return result;
        }

        /// <summary>
        /// Display a button that takes editor indentation into account
        /// </summary>
        /// <param name="content">Text, image and tooltip for this button</param>
        /// <returns>True is clicked</returns>
        public bool ButtonLeftAligned(GUIContent content)
        {
            TextAnchor oldalignment = GUI.skin.button.alignment;
            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            bool result = m_editorUtils.Button(content);
            GUI.skin.button.alignment = oldalignment;
            return result;
        }


        /// <summary>
        /// Get a clamped size value
        /// </summary>
        /// <param name="newSize"></param>
        /// <returns></returns>
        float GetClampedSize(float newSize)
        {
            return Mathf.Clamp(newSize, 32f, m_settings.m_currentDefaults.m_size);
        }

        #region Helper methods

        /// <summary>
        /// Get the asset path of the first thing that matches the name
        /// </summary>
        /// <param name="name">Name to search for</param>
        /// <returns></returns>
        private static string GetAssetPath(string name)
        {
#if UNITY_EDITOR
            string[] assets = AssetDatabase.FindAssets(name, null);
            if (assets.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(assets[0]);
            }
#endif
            return null;
        }

        /// <summary>
        /// Get the currently active terrain - or any terrain
        /// </summary>
        /// <returns>A terrain if there is one</returns>
        public static Terrain GetActiveTerrain()
        {
            //Grab active terrain if we can
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null && terrain.isActiveAndEnabled)
            {
                return terrain;
            }

            //Then check rest of terrains
            for (int idx = 0; idx < Terrain.activeTerrains.Length; idx++)
            {
                terrain = Terrain.activeTerrains[idx];
                if (terrain != null && terrain.isActiveAndEnabled)
                {
                    return terrain;
                }
            }
            return null;
        }

        #endregion

        bool ClickableHeaderCustomStyle(GUIContent content, GUIStyle style, GUILayoutOption[] options = null)
        {
            var position = GUILayoutUtility.GetRect(content, style, options);
            Handles.BeginGUI();
            Color oldColor = Handles.color;
            Handles.color = style.normal.textColor;
            Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
            Handles.color = oldColor;
            Handles.EndGUI();
            EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);
            return GUI.Button(position, content, style);
        }

        /// <summary>
        /// Get the latest news from the web site at most once every 24 hours
        /// </summary>
        /// <returns></returns>
        IEnumerator GetNewsUpdate()
        {
            TimeSpan elapsed = new TimeSpan(DateTime.Now.Ticks - m_settings.m_lastWebUpdate);
            if (elapsed.TotalHours < 24.0)
            {
                StopEditorUpdates();
            }
            else
            {
                if (PWApp.CONF != null)
                {
#if UNITY_2018_3_OR_NEWER
                    using (UnityWebRequest www = new UnityWebRequest("http://www.procedural-worlds.com/gaiajson.php?gv=gaia-" + PWApp.CONF.Version))
                    {
                        while (!www.isDone)
                        {
                            yield return www;
                        }

                        if (!string.IsNullOrEmpty(www.error))
                        {
                            //Debug.Log(www.error);
                        }
                        else
                        {
                            try
                            {
                                string result = www.url;
                                int first = result.IndexOf("####");
                                if (first > 0)
                                {
                                    result = result.Substring(first + 10);
                                    first = result.IndexOf("####");
                                    if (first > 0)
                                    {
                                        result = result.Substring(0, first);
                                        result = result.Replace("<br />", "");
                                        result = result.Replace("&#8221;", "\"");
                                        result = result.Replace("&#8220;", "\"");
                                        var message = JsonUtility.FromJson<GaiaMessages>(result);
                                        m_settings.m_latestNewsTitle = message.title;
                                        m_settings.m_latestNewsBody = message.bodyContent;
                                        m_settings.m_latestNewsUrl = message.url;
                                        m_settings.m_lastWebUpdate = DateTime.Now.Ticks;
                                        EditorUtility.SetDirty(m_settings);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                //Debug.Log(e.Message);
                            }
                        }
                    }
#else
                    using (WWW www = new WWW("http://www.procedural-worlds.com/gaiajson.php?gv=gaia-" + PWApp.CONF.Version))
                    {
                        while (!www.isDone)
                        {
                            yield return www;
                        }

                        if (!string.IsNullOrEmpty(www.error))
                        {
                            //Debug.Log(www.error);
                        }
                        else
                        {
                            try
                            {
                                string result = www.text;
                                int first = result.IndexOf("####");
                                if (first > 0)
                                {
                                    result = result.Substring(first + 10);
                                    first = result.IndexOf("####");
                                    if (first > 0)
                                    {
                                        result = result.Substring(0, first);
                                        result = result.Replace("<br />", "");
                                        result = result.Replace("&#8221;", "\"");
                                        result = result.Replace("&#8220;", "\"");
                                        var message = JsonUtility.FromJson<GaiaMessages>(result);
                                        m_settings.m_latestNewsTitle = message.title;
                                        m_settings.m_latestNewsBody = message.bodyContent;
                                        m_settings.m_latestNewsUrl = message.url;
                                        m_settings.m_lastWebUpdate = DateTime.Now.Ticks;
                                        EditorUtility.SetDirty(m_settings);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                //Debug.Log(e.Message);
                            }
                        }
                    }
                
#endif
                }
            }
            StopEditorUpdates();
        }

        /// <summary>
        /// Import Package
        /// </summary>
        /// <param name="packageName"></param>
        public static void ImportPackage(string packageName)
        {
            string packageGaia = AssetUtils.GetAssetPath(packageName + ".unitypackage");
            Debug.Log(packageGaia);
            if (!string.IsNullOrEmpty(packageGaia))
            {
                AssetDatabase.ImportPackage(packageGaia, true);
            }
            else
                Debug.Log("Unable to find Gaia Dependencies.unitypackage");
        }

        /// <summary>
        /// Start editor updates
        /// </summary>
        public void StartEditorUpdates()
        {
            EditorApplication.update += EditorUpdate;
        }

        //Stop editor updates
        public void StopEditorUpdates()
        {
            EditorApplication.update -= EditorUpdate;
        }

        /// <summary>
        /// This is executed only in the editor - using it to simulate co-routine execution and update execution
        /// </summary>
        void EditorUpdate()
        {
            if (m_updateCoroutine == null)
            {
                StopEditorUpdates();
            }
            else
            {
                m_updateCoroutine.MoveNext();
            }
        }


        void ReflectionProbeBakeUpdate()
        {
            //Bake Probes
            if (ReflectionProbeEditorUtils.m_probeRenderActive)
            {
                if (ReflectionProbeEditorUtils.m_storedProbes.Count > 0)
                {
                    float progrss = (float)(ReflectionProbeEditorUtils.m_currentProbeCount - ReflectionProbeEditorUtils.m_storedProbes.Count) / ReflectionProbeEditorUtils.m_currentProbeCount;
                    EditorUtility.DisplayProgressBar("Baking Reflection Probes", "Probes remaining :" + ReflectionProbeEditorUtils.m_storedProbes.Count, progrss);
                    ReflectionProbeEditorUtils.m_storedProbes[0].enabled = true;
                    ReflectionProbeEditorUtils.m_storedProbes.RemoveAt(0);
                }
                else
                {
                    ReflectionProbeEditorUtils.m_probeRenderActive = false;
                    EditorUtility.ClearProgressBar();
                }
            }

        }

        #endregion

        #region GAIA eXtensions GX
        public static List<Type> GetTypesInNamespace(string nameSpace)
        {
            List<Type> gaiaTypes = new List<Type>();

            int assyIdx, typeIdx;
            System.Type[] types;
            System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            for (assyIdx = 0; assyIdx < assemblies.Length; assyIdx++)
            {
                if (assemblies[assyIdx].FullName.StartsWith("Assembly"))
                {
                    types = assemblies[assyIdx].GetTypes();
                    for (typeIdx = 0; typeIdx < types.Length; typeIdx++)
                    {
                        if (!string.IsNullOrEmpty(types[typeIdx].Namespace))
                        {
                            if (types[typeIdx].Namespace.StartsWith(nameSpace))
                            {
                                gaiaTypes.Add(types[typeIdx]);
                            }
                        }
                    }
                }
            }
            return gaiaTypes;
        }

        /// <summary>
        /// Return true if image FX have been included
        /// </summary>
        /// <returns></returns>
        public static bool GotImageFX()
        {
            List<Type> types = GetTypesInNamespace("UnityStandardAssets.ImageEffects");
            if (types.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Commented out tooltips
        ///// <summary>
        ///// The tooltips
        ///// </summary>
        //static Dictionary<string, string> m_tooltips = new Dictionary<string, string>
        //{
        //    { "Execution Mode", "The way this spawner runs. Design time : At design time only. Runtime Interval : At run time on a timed interval. Runtime Triggered Interval : At run time on a timed interval, and only when the tagged game object is closer than the trigger range from the center of the spawner." },
        //    { "Controller", "The type of control method that will be set up. " },
        //    { "Environment", "The type of environment that will be set up. This pre-configures your terrain settings to be better suited for the environment you are targeting. You can modify these setting by modifying the relevant terrain default settings." },
        //    { "Renderer", "The terrain renderer you are targeting. The 2018x renderers are only relevent when using Unity 2018 and above." },
        //    { "Terrain Size", "The size of the terrain you are setting up. Please be aware that larger terrain sizes are harder for Unity to render, and will result in slow frame rates. You also need to consider your target environment as well. A mobile or VR device will have problems with large terrains." },
        //    { "Terrain Defaults", "The default settings that will be used when creating new terrains." },
        //    { "Terrain Resources", "The texture, detail and tree resources that will be used when creating new terrains." },
        //    { "GameObject Resources", "The game object resources that will be passed to your GameObject spawners when creating new spawners." },
        //    { "1. Create Terrain & Show Stamper", "Creates your terrain based on the setting in the panel above. You use the stamper to terraform your terrain." },
        //    { "2. Create Spawners", "Creates the spawners based on your resources in the panel above. You use spawners to inject these resources into your scene." },
        //    { "3. Create Player, Water and Screenshotter", "Creates the things you most commonly need in your scene to make it playable." },
        //    { "3. Create Player, Wind, Water and Screenshotter", "Creates the things you most commonly need in your scene to make it playable." },
        //    { "Show Session Manager", "The session manager records stamping and spawning operations so that you can recreate your terrain later." },
        //    { "Create Terrain", "Creates a terrain based on your settings." },
        //    { "Create Coverage Texture Spawner", "Creates a texture spawner so you can paint your terrain." },
        //    { "Create Coverage Grass Spawner", "Creates a grass (terrain details) spawner so you can cover your terrain with grass." },
        //    { "Create Clustered Grass Spawner", "Creates a grass (terrain details) spawner so you can cover your terrain with patches with grass." },
        //    { "Create Coverage Terrain Tree Spawner", "Creates a terrain tree spawner so you can cover your terrain with trees." },
        //    { "Create Clustered Terrain Tree Spawner", "Creates a terrain tree spawner so you can cover your terrain with clusters with trees." },
        //    { "Create Coverage Prefab Tree Spawner", "Creates a tree spawner from prefabs so you can cover your terrain with trees." },
        //    { "Create Clustered Prefab Tree Spawner", "Creates a tree spawner from prefabs so you can cover your terrain with clusters with trees." },
        //    { "Create Coverage Prefab Spawner", "Creates a spawner from prefabs so you can cover your terrain with instantiations of those prefabs." },
        //    { "Create Clustered Prefab Spawner", "Creates a spawner from prefabs so you can cover your terrain with clusters of those prefabs." },
        //    { "Show Stamper", "Shows a stamper. Use the stamper to terraform your terrain." },
        //    { "Show Scanner", "Shows the scanner. Use the scanner to create new stamps from textures, world machine .r16 files, IBM 16 bit RAW file, MAC 16 bit RAW files, Terrains, and Meshes (with mesh colliders)." },
        //    { "Show Visualiser", "Shows the visualiser. Use the visualiser to visualise and configure fitness values for your resources." },
        //    { "Show Terrain Utilities", "Shows terrain utilities. These are a great way to add additional interest to your terrains." },
        //    { "Show Splatmap Exporter", "Shows splatmap exporter. Exports your texture splatmaps." },
        //    { "Show Grass Exporter", "Shows grass exporter. Exports your grass control maps." },
        //    { "Show Mesh Exporter", "Shows mesh exporter. Exports your terrain as a low poly mesh. Use in conjunction with Base Map Exporter and Normal Map Exporter in Terrain Utilties to create cool mesh features to use in the distance." },
        //    { "Show Shore Exporter", "Shows shore exporter. Exports a mask of your terrain shoreline." },
        //    { "Show Extension Exporter", "Shows extension exporter. Use extensions to save resource and spawner configurations for later use via the GX tab." },
        //};
        #endregion
    }
}
