using System;
using System.Collections.Generic;
using UnityEngine;
using Gaia;
#if MEWLIST_MASSIVE_CLOUDS
using Mewlist;
#endif
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
#if HDPipeline
using UnityEngine.Rendering.HighDefinition;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProceduralWorlds.WaterSystem
{
    [Serializable]
    public class SceneConditionsData
    {
        //Sun
        public Color m_sunColor;
        public float m_sunIntensity;
        public Vector3 m_sunDirection;
        //Time of day
        public int m_hour;
        public float m_minute;
    }

    /// <summary>
    /// Generates Water Reflection/Underwater Control and Mesh Generation.
    /// </summary>
    [ExecuteAlways]
    public class PWS_WaterSystem : MonoBehaviour
    {
        public static PWS_WaterSystem Instance
        {
            get 
            {
                if (m_instance == null)
                {
                    m_instance = FindObjectOfType<PWS_WaterSystem>();
                }
                return m_instance; 
            }

        }
        [SerializeField]
        private static PWS_WaterSystem m_instance;

        #region Variables

        public bool InfiniteMode
        {
            get { return m_infiniteMode; }
            set
            {
                if (m_infiniteMode != value)
                {
                    m_infiniteMode = value;
                    SetInfiniteMode(m_infiniteMode);
                }
            }
        }
        private bool m_infiniteMode = true;
        public GaiaConstants.WaterAutoUpdateMode m_autoUpdateMode = GaiaConstants.WaterAutoUpdateMode.Interval;
        public SceneConditionsData SceneConditionsData = new SceneConditionsData();

        #region Water Reflections/Surface

        #region Public Variables

        public bool m_disableAllReflections = false;
        public bool m_autoRefresh = true;
        public bool m_ignoreSceneConditions = true;
        public float m_refreshRate = 0.5f;
        public bool m_layersHasBeenSet = false;
        public GaiaConstants.EnvironmentRenderer RenderPipeline;
        public SceneProfile m_waterProfile;
        public GaiaWaterProfileValues m_waterProfileValues;
        public Transform m_player;
        public Camera m_RenderCamera;
        public Camera m_gameCamera;
        public RenderingPath m_RenderPath;
        public List<Material> m_waterMaterialInstances = new List<Material>();
        public Gradient m_currentGradient;
        public Texture2D m_waterTexture;
        public Texture2D m_currentWaterTexture;
        public bool m_requestDepthTextureGeneration = false;
        public Light SunLight;
        public float m_minSurfaceLight = 0.5f;
        public float m_maxSurfaceLight = 2f;

        //Sea level setup
        public bool m_updatingSeaLevel = false;
        public float SeaLevel
        {
            get { return m_seaLevel; }
            set
            {
                if (m_seaLevel != value)
                {
                    m_seaLevel = value;
                    m_updatingSeaLevel = true;
                    UpdateSeaLevel(m_seaLevel, true);
                }
            }
        }
        [SerializeField]
        private float m_seaLevel = 25f;

        public GaiaPlanarReflections m_planarReflections;
#if HDPipeline
        public PlanarReflectionProbe m_HDPlanarReflections;
#endif

        #endregion

        #region Private Variables

        public bool m_weatherSystemPresent = false;
        public bool m_ableToRender = true;
        private float m_currentRefreshRate;
        private bool m_hdr;
        private bool m_rebuild;
        private int m_oldRenderTextureSize;
        private Vector3 m_worldPosition;
        private Vector3 m_normal;
        private Vector3 m_oldPosition;
        private Vector3 m_newPosition;
        private Vector3 m_euler;
        private Vector4 m_clipPlane;
        private Vector3 m_currentPosition;
        private Vector3 m_currentRotation;
        private Matrix4x4 m_projection;
        private Matrix4x4 m_reflection;
        private int m_frameCheck;
        [SerializeField] 
        private bool m_sceneProfileExists = false;
        [SerializeField]
        private RenderTexture m_reflectionTexture;
        public Camera m_reflectionCamera;
#if UNITY_EDITOR
        [SerializeField]
        private GaiaSessionManager m_gaiaSession;
#endif
        [SerializeField]
        private GaiaUnderwaterEffects m_underwaterFX;
        [SerializeField]
        private Material m_waterMaterial;
        [SerializeField]
        private bool m_rendererPresent;
#if GAIA_PRO_PRESENT
        [SerializeField]
        private ProceduralWorldsGlobalWeather WeatherSystem;
#endif
        private bool m_forceGenerateDepthTexture = true;
        // [SerializeField] 
        // private bool m_setupFog = false;

        private const string m_reflectionName = "__MirrorReflection";
        private const string m_reflectionIDName = "Mirror Refl Camera id";
        private const string m_forText = " for ";

        #endregion

        #endregion

        #region Underwater Sync Variables

        #region Public Variables

        public GaiaConstants.PW_RENDER_SIZE renderSize = GaiaConstants.PW_RENDER_SIZE.HALF;
        public bool refractionEnabled = true;
        public float directionAngle	= 0;

        #endregion

        #endregion

        #region Mesh Generation Variables

        #region Public Variables

        public int m_height = 0;
        public float m_gizmoSize = 0.1f;
        public Vector3 m_Size = new Vector3(200, 30, 200);
        public Vector2 m_meshDensity = new Vector2(50, 50);
        public GaiaConstants.MeshType m_MeshType = GaiaConstants.MeshType.Plane;

        #endregion

        #region Private Variables

        private Vector2 m_uvScale;
        private Vector3 m_sizeScale;
        private List<int> m_triangles = new List<int>();
        private List<Vector3> m_vertices = new List<Vector3>();
        private List<Vector2> m_uvs = new List<Vector2>();
        private Mesh m_generateMesh;
        private Vector2Int m_numberOfPoints;

        #endregion

        #endregion

        #endregion

        #region Unity Methods

        /// <summary>
        /// On enable rebuild the required data
        /// </summary>
        private void OnEnable()
        {
            StartAndOnEnabled();
        }
        /// <summary>
        /// Update is called every frame
        /// </summary>
        private void Update()
        {
            UpdateMode();
        }
        /// <summary>
        /// OnRenderObject is called after camera has rendered the Scene
        /// </summary>
        public void OnWillRenderObject()
        {
            if (RenderPipeline == GaiaConstants.EnvironmentRenderer.BuiltIn)
            {
                OnRenderObjectUpdate();
            }
        }
        /// <summary>
        /// OnDisable ClearData
        /// </summary>
        private void OnDisable()
        {
            m_rebuild = true;
            ClearAllData();
        }
        /// <summary>
        /// OnDestroy ClearData
        /// </summary>
        private void OnDestroy()
        {
            ClearAllData();
        }

        #endregion

        #region Unity Method Functions

        /// <summary>
        /// Function used to initilize on enable or start depending on bool status
        /// </summary>
        /// <param name="onEnable"></param>
        public void StartAndOnEnabled()
        {
            if (m_waterProfile == null && m_waterProfileValues == null)
            {
                return;
            }

            m_forceGenerateDepthTexture = true;
            if (m_underwaterFX == null)
            {
                m_underwaterFX = GaiaUnderwaterEffects.Instance;
            }

            m_sceneProfileExists = GaiaUtils.CheckIfSceneProfileExists();
            m_frameCheck = Random.Range(5, 15);
            LoadResources();
            m_currentRefreshRate = m_refreshRate;
            m_waterMaterialInstances = GetWaterMaterialInstances();
            RenderPipeline = GaiaUtils.GetActivePipeline();
            m_rendererPresent = CheckRendererComponent();
            ClearAllData();
            ProcessOnEnableReflections();
            RefractionInit();
            if (m_waterMaterialInstances.Count == 2)
            {
                if (RenderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    SendNormalMaps();
                    SendWaterEdge();
                    SendWaveData();
                }
            }

            m_rebuild = true;
            Generate(m_waterProfile);
            m_rebuild = false;
#if GAIA_PRO_PRESENT
            m_weatherSystemPresent = ProceduralWorldsGlobalWeather.Instance;
            WeatherSystem = ProceduralWorldsGlobalWeather.Instance;
#endif
            SyncAutoRefresh();

            //Updates aura if it's present
            UpdateAura();

            SetInfiniteMode(InfiniteMode);
        }
        /// <summary>
        /// Used for the update/late update functions
        /// </summary>
        private void UpdateMode()
        {
            if (m_waterProfile == null)
            {
                return;
            }
            if (m_waterProfile.m_selectedWaterProfileValuesIndex <= m_waterProfile.m_waterProfiles.Count - 1)
            {
                m_waterProfileValues = m_waterProfile.m_waterProfiles[m_waterProfile.m_selectedWaterProfileValuesIndex];
            }

            if (!m_updatingSeaLevel)
            {
                if (gameObject.transform.position.y != SeaLevel)
                {
                    SeaLevel = transform.position.y;
                }
            }

#if UNITY_EDITOR
            m_waterProfile.UpdateTextureResolution();
#endif
            BuildWaterColorDepth();
            UpdateShaderValues(m_waterProfileValues);
            float distanceCheck = 500f;
            if (m_RenderCamera != null)
            {
                distanceCheck = m_RenderCamera.farClipPlane;
            }
            if (m_waterMaterialInstances.Count == 2)
            {
                if (RenderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    SendWaveData();
                    SendWindDirection();
#if UNITY_EDITOR
                    SendNormalMaps();
                    SendWaterEdge();
#endif
                }
            }

            //CalculateSmoothnessRay(m_waterProfile, m_waterProfileValues.m_smoothness, distanceCheck);
        }
        /// <summary>
        /// Used on will render object and the alternative for SRP
        /// </summary>
        /// <param name="SRP"></param>
        private void OnRenderObjectUpdate(Camera overrideCam = null)
        {
            if (m_waterProfile == null && m_waterProfileValues == null)
            {
                return;
            }

            if (m_disableAllReflections)
            {
                ClearData(true);
                return;
            }

            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                if (SceneView.lastActiveSceneView == null)
                {
                    return;
                }

                m_RenderCamera = SceneView.lastActiveSceneView.camera;
                if (overrideCam != null)
                {
                    m_RenderCamera = overrideCam;
                }
                if (m_RenderCamera != null)
                {
                    if (CheckPositionAndRotation())
                    {
                        GenerateCamera(m_waterProfile);
                        ResyncCameraSettings(m_waterProfile);
                        UpdateCameraModes();
                        if (m_reflectionTexture != null && m_reflectionTexture.width > 0)
                        {
                            BuildReflection(m_waterProfile);
                        }
                    }
                }
#endif
            }
            else
            {
                m_RenderCamera = m_gameCamera;
                if (overrideCam != null)
                {
                    m_RenderCamera = overrideCam;
                }
                if (m_RenderCamera != null)
                {
                    if (CheckPositionAndRotation())
                    {
                        if (m_reflectionTexture != null && m_reflectionTexture.width > 0)
                        {
                            BuildReflection(m_waterProfile);
                        }
                    }
                    else
                    {
                        if (AutoRefresh())
                        {
                            m_frameCheck = Random.Range(5, 15);
                            if (m_reflectionTexture != null && m_reflectionTexture.width > 0)
                            {
                                BuildReflection(m_waterProfile);
                            }
                        }

                        m_frameCheck--;
                    }
                }
                else
                {
                    if (m_RenderCamera == null)
                    {
                        m_RenderCamera = GaiaUtils.GetCamera();
                    }
                }
            }
        }
        /// <summary>
        /// Render SRP camera
        /// </summary>
        /// <param name="src"></param>
        /// <param name="cam"></param>
        private void OnRenderSRP(ScriptableRenderContext src, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview)
            {
                return;
            }

            var roll = cam.transform.localEulerAngles.z;
            Shader.SetGlobalFloat(GaiaShaderID.CameraRoll, roll);
            Shader.SetGlobalMatrix(GaiaShaderID.InvViewProjection, (GL.GetGPUProjectionMatrix(cam.projectionMatrix, false) * cam.worldToCameraMatrix).inverse);

            if (Instance != null)
            {
                OnRenderObjectUpdate(cam);
            }
        }
        /// <summary>
        /// Checks the player position and rotation
        /// </summary>
        /// <returns></returns>
        private bool CheckPositionAndRotation()
        {
            try
            {
                if (m_RenderCamera == null)
                {
                    return false;
                }

                if (m_RenderCamera.transform.position != m_currentPosition || m_RenderCamera.transform.rotation.eulerAngles != m_currentRotation)
                {
                    m_currentPosition = m_RenderCamera.transform.position;
                    m_currentRotation = m_RenderCamera.transform.rotation.eulerAngles;
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// Checks if auto refresh can be set
        /// </summary>
        /// <returns></returns>
        private bool AutoRefresh()
        {
            if (m_autoRefresh)
            {
                if (m_autoUpdateMode == GaiaConstants.WaterAutoUpdateMode.SceneConditions)
                {
                    if (m_frameCheck <= 0)
                    {
                        if (CheckSceneConditions())
                        {
                            SetSceneConditionsData();
                            return true;
                        }
                    }
                }
                else
                {
                    m_currentRefreshRate -= Time.deltaTime;
                    if (m_currentRefreshRate < 0)
                    {
                        m_currentRefreshRate = m_refreshRate;
                        if (m_ignoreSceneConditions)
                        {
                            return true;
                        }
                        else
                        {
                            if (CheckSceneConditions())
                            {
                                SetSceneConditionsData();
                                return true;
                            }
                        }
                    }
                }
            }
            
            return false;
        }

        private bool CheckSceneConditions()
        {
            if (SunLight != null)
            {
                if (SunLight.color != SceneConditionsData.m_sunColor)
                {
                    return true;
                }
                if (SunLight.intensity != SceneConditionsData.m_sunIntensity)
                {
                    return true;
                }
                if (SunLight.transform.eulerAngles != SceneConditionsData.m_sunDirection)
                {
                    return true;
                }
            }
            if (m_sceneProfileExists)
            {
                if (GaiaGlobal.Instance.GaiaTimeOfDayValue.m_todHour != SceneConditionsData.m_hour)
                {
                    return true;
                }
                if (GaiaGlobal.Instance.GaiaTimeOfDayValue.m_todMinutes != SceneConditionsData.m_minute)
                {
                    return true;
                }
            }

            return false;
        }

        private void SyncAutoRefresh()
        {
            if (GaiaUtils.CheckIfSceneProfileExists() && m_waterProfile != null)
            {
                m_autoRefresh = m_waterProfile.m_autoRefresh;
                m_ignoreSceneConditions = m_waterProfile.m_ignoreSceneConditions;
                m_autoUpdateMode = m_waterProfile.m_autoUpdateMode;
                m_refreshRate = m_waterProfile.m_refreshRate;
            }
        }
        /// <summary>
        /// Updates the scene conditions
        /// </summary>
        private void SetSceneConditionsData()
        {
            if (SunLight != null)
            {
                SceneConditionsData.m_sunColor = SunLight.color;
                SceneConditionsData.m_sunIntensity = SunLight.intensity;
                SceneConditionsData.m_sunDirection = SunLight.transform.eulerAngles;
            }
            if (m_sceneProfileExists)
            {
                SceneConditionsData.m_hour = GaiaGlobal.Instance.GaiaTimeOfDayValue.m_todHour;
                SceneConditionsData.m_minute = GaiaGlobal.Instance.GaiaTimeOfDayValue.m_todMinutes;
            }
        }

        #endregion

        #region Water Reflections/Surface

        #region Public Functions

        /// <summary>
        /// Used to generate the reflections
        /// </summary>
        /// <param name="profile"></param>
        public void Generate(SceneProfile profile)
        {
            if (m_disableAllReflections)
            {
                return;
            }

            if (m_RenderCamera == null)
            {
                m_RenderCamera = GaiaUtils.GetCamera();
            }

            if (profile == null)
            {
                return;
            }

            if (m_rendererPresent)
            {
                if (RenderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    if (m_waterMaterialInstances == null || m_waterMaterialInstances.Count < 2)
                    {
                        m_waterMaterialInstances = GetWaterMaterialInstances();
                    }
                }

                if (m_waterMaterial != null)
                {
                    SetWaterShader(m_waterMaterial, RenderPipeline);
                }

                GenerateCamera(profile);
                CreateMirrorObjects(profile);
                if (m_reflectionTexture != null && m_reflectionTexture.width > 0)
                {
                    BuildReflection(profile);
                }
            }
            else
            {
                Debug.Log("unable to create reflections, renderer component missing");
            }
        }
        /// <summary>
        /// Creates water material instances
        /// </summary>
        public void CreateWaterMaterialInstances(List<Material> materials)
        {
            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                m_waterMaterialInstances.Clear();
                for (int i = 0; i < materials.Count; i++)
                {
                    Material material = new Material(Shader.Find(materials[i].shader.name));
                    material.CopyPropertiesFromMaterial(materials[i]);
                    m_waterMaterialInstances.Add(material);
                }

                meshRenderer.sharedMaterials = m_waterMaterialInstances.ToArray();
                if (m_waterMaterialInstances.Count == 2)
                {
                    foreach (var material in m_waterMaterialInstances)
                    {
                        if (!material.name.Contains("Under"))
                        {
                            m_waterMaterial = m_waterMaterialInstances[0];
                            break;
                        }
                    }
                }

                for (int i = 0; i < m_waterMaterialInstances.Count; i++)
                {
#if UNITY_EDITOR
                    EditorUtility.SetDirty(m_waterMaterialInstances[i]);
#endif
                }

                GenerateColorDepth();
            }
        }
        /// <summary>
        /// Updates the water shader
        /// </summary>
        /// <param name="masterMaterial"></param>
        /// <param name="renderPipeline"></param>
        public void SetWaterShader(Material masterMaterial, GaiaConstants.EnvironmentRenderer renderPipeline)
        {
            if (masterMaterial == null)
            {
                return;
            }

            if (renderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                masterMaterial.shader = Shader.Find(GaiaShaderID.BuiltInWaterShader);
            }
            else
            {
                masterMaterial.shader = Shader.Find(GaiaShaderID.HDRPWaterShader);
            }
        }
        /// <summary>
        /// Generates color depth
        /// </summary>
        public void GenerateColorDepth()
        {
            if (m_waterTexture == null || m_waterTexture.wrapMode != TextureWrapMode.Clamp)
            {
                m_waterTexture = new Texture2D(m_waterProfileValues.m_gradientTextureResolution,
                    m_waterProfileValues.m_gradientTextureResolution) {wrapMode = TextureWrapMode.Clamp};
            }
            else if (m_waterProfileValues.m_gradientTextureResolution != m_waterTexture.width)
            {
                m_waterTexture = new Texture2D(m_waterProfileValues.m_gradientTextureResolution,
                    m_waterProfileValues.m_gradientTextureResolution) {wrapMode = TextureWrapMode.Clamp};
            }
            else if (!m_waterTexture.isReadable)
            {
                m_waterTexture = new Texture2D(m_waterProfileValues.m_gradientTextureResolution,
                    m_waterProfileValues.m_gradientTextureResolution) {wrapMode = TextureWrapMode.Clamp};
            }

            if (m_waterProfileValues.m_waterGradient == null)
            {
                return;
            }

            if (m_requestDepthTextureGeneration || GaiaUtils.CheckGradientColorKeys(m_waterProfileValues.m_waterGradient.colorKeys, m_currentGradient.colorKeys))
            {
                for (int x = 0; x < m_waterProfileValues.m_gradientTextureResolution; x++)
                {
                    for (int y = 0; y < m_waterProfileValues.m_gradientTextureResolution; y++)
                    {
                        Color color = m_waterProfileValues.m_waterGradient.Evaluate((float)x / (float)m_waterProfileValues.m_gradientTextureResolution);
                        m_waterTexture.SetPixel(x, y, color);
                    }
                }
                m_waterTexture.Apply();

                if (m_waterMaterial != null)
                {
                    m_waterMaterial.SetTexture(GaiaShaderID.m_waterDepthRamp, m_waterTexture);
                }

                m_requestDepthTextureGeneration = false;
                m_currentGradient = m_waterProfileValues.m_waterGradient;
            }
        }
        /// <summary>
        /// Updates the sea level
        /// </summary>
        /// <param name="seaLevel"></param>
        public void UpdateSeaLevel(float seaLevel, bool regenerateWaterReflections = false)
        {
            gameObject.transform.position = new Vector3(0f, seaLevel, 0f);
            if (m_underwaterFX == null)
            {
                m_underwaterFX = GaiaUnderwaterEffects.Instance;
            }

            if (m_underwaterFX != null)
            {
                if (m_underwaterFX.m_underwaterPostFX != null)
                {
                    if (m_underwaterFX.m_underwaterPostFX != null)
                    {
                        m_underwaterFX.m_underwaterPostFX.transform.position = new Vector3(0f, -3500f + seaLevel, 0f);
                    }

                    if (m_underwaterFX.m_underwaterTransitionPostFX != null)
                    {
                        m_underwaterFX.m_underwaterTransitionPostFX.transform.position = new Vector3(0f, seaLevel, 0f);
                    }
                }

                m_underwaterFX.m_seaLevel = seaLevel;
            }

            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                if (m_gaiaSession == null)
                {
                    m_gaiaSession = GaiaSessionManager.GetSessionManager(false, false);
                }

                if (m_gaiaSession != null)
                {
                    m_gaiaSession.SetSeaLevel(seaLevel);
                }

                EditorUtility.SetDirty(this);
#endif
            }

            if (regenerateWaterReflections)
            {
                Generate(m_waterProfile);
            }

            m_updatingSeaLevel = false;
        }

        #endregion

        #region Private Functions

        /// <summary>
        /// Gets the current material instances on the mesh renderer
        /// </summary>
        /// <returns></returns>
        private List<Material> GetWaterMaterialInstances()
        {
            List<Material> materials = new List<Material>();
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                materials.AddRange(meshRenderer.sharedMaterials);
            }

            if (m_waterMaterialInstances.Count > 0)
            {
                m_waterMaterialInstances.Clear();
                m_waterMaterialInstances = materials;
                foreach (var material in m_waterMaterialInstances)
                {
                    if (material != null)
                    {
                        if (!material.name.Contains("Under"))
                        {
                            m_waterMaterial = m_waterMaterialInstances[0];
                            break;
                        }
                    }
                }
            }

            return materials;
        }
        /// <summary>
        /// Builds the and setup the color depth texture or sets baked texture
        /// </summary>
        private void BuildWaterColorDepth()
        {
            if (m_waterProfile != null)
            {
#if UNITY_EDITOR
                if (m_waterProfileValues == null)
                {
                    m_waterProfileValues = m_waterProfile.m_waterProfiles[m_waterProfile.m_selectedWaterProfileValuesIndex];
                }
#endif
            }

            if (m_waterProfileValues == null)
            {
                Debug.Log("Unable To Build. Profile values from current water profile could not be set");
                return;
            }

            if (m_waterProfileValues.m_colorDepthRamp == null)
            {
                if (m_waterProfileValues.m_waterGradient != null)
                {
                    GenerateColorDepth();
                }
            }
            else
            {
                m_waterTexture = m_waterProfileValues.m_colorDepthRamp;
            }

            if (ValidateDepthRamp(m_forceGenerateDepthTexture))
            {
                m_waterMaterial.SetTexture(GaiaShaderID.m_waterDepthRamp, m_waterTexture);
                m_currentWaterTexture = m_waterTexture;
                m_forceGenerateDepthTexture = false;
            }
        }
        /// <summary>
        /// Checks if the depth ramp texture needs to be reset
        /// </summary>
        /// <returns></returns>
        private bool ValidateDepthRamp(bool forceUpdate = false)
        {
            if (m_waterMaterial == null)
            {
                return false;
            }

            if (forceUpdate)
            {
                return true;
            }

            if (m_waterTexture != null)
            {
                if (m_currentWaterTexture == null)
                {
                    return true;
                }
                else
                {
                    if (m_waterTexture != m_currentWaterTexture)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        /// <summary>
        /// Enables or disables the smoothness based on ray setup
        /// </summary>
        private void CalculateSmoothnessRay(SceneProfile profile, float smoothness, float range)
        {
            if (profile == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                if (m_RenderCamera != null)
                {
                    Ray ray = m_RenderCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
#if GAIA_PRO_PRESENT
                    if (m_weatherSystemPresent)
                    {
                        if (WeatherSystem.CheckIsNight())
                        {
                            if (Physics.Raycast(ray.origin, -WeatherSystem.m_moonLight.transform.forward, range, profile.m_reflectedLayers))
                            {
                                m_waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, 1f);
                            }
                            else
                            {
                                m_waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, smoothness);
                            }
                        }
                        else
                        {
                            if (Physics.Raycast(ray.origin, -WeatherSystem.m_sunLight.transform.forward, range, profile.m_reflectedLayers))
                            {
                                m_waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, 1f);
                            }
                            else
                            {
                                m_waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, smoothness);
                            }
                        }

                    }
                    else
                    {
                        if (SunLight != null)
                        {
                            if (Physics.Raycast(ray.origin, -SunLight.transform.forward, range, profile.m_reflectedLayers))
                            {
                                m_waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, 1f);
                            }
                            else
                            {
                                m_waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, smoothness);
                            }
                        }
                        else
                        {
                            SunLight = GaiaUtils.GetMainDirectionalLight();
                        }
                    }
#else
                    if (SunLight != null)
                    {
                        if (Physics.Raycast(ray.origin, -SunLight.transform.forward, range, profile.m_reflectedLayers))
                        {
                            m_waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, 1f);
                        }
                        else
                        {
                            m_waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, smoothness);
                        }
                    }
                    else
                    {
                        SunLight = GaiaUtils.GetMainDirectionalLight();
                    }
#endif
                }
            }
            else
            {
#if UNITY_EDITOR

#if GAIA_PRO_PRESENT
                if (m_weatherSystemPresent)
                {
                    if (WeatherSystem.CheckIsNight())
                    {
                        if (SceneView.lastActiveSceneView != null)
                        {
                            Ray ray = SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                            if (Physics.Raycast(ray.origin, -WeatherSystem.m_moonLight.transform.forward, range, profile.m_reflectedLayers))
                            {
                                m_waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, 1f);
                            }
                            else
                            {
                                m_waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, smoothness);
                            }
                        }
                    }
                    else
                    {
                        if (SceneView.lastActiveSceneView != null)
                        {
                            Ray ray = SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                            if (WeatherSystem.m_sunLight != null)
                            {
                                if (Physics.Raycast(ray.origin, -WeatherSystem.m_sunLight.transform.forward, range, profile.m_reflectedLayers))
                                {
                                    m_waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, 1f);
                                }
                                else
                                {
                                    m_waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, smoothness);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (SceneView.lastActiveSceneView != null)
                    {
                        if (SunLight != null)
                        {
                            Ray ray = SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                            if (Physics.Raycast(ray.origin, -SunLight.transform.forward, range, profile.m_reflectedLayers))
                            {
                                m_waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, 1f);
                            }
                            else
                            {
                                m_waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, smoothness);
                            }
                        }
                        else
                        {
                            SunLight = GaiaUtils.GetMainDirectionalLight();
                        }
                    }
                }
#else
                if (UnityEditor.SceneView.lastActiveSceneView != null)
                {
                    if (SunLight != null)
                    {
                        Ray ray = UnityEditor.SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                        if (Physics.Raycast(ray.origin, -SunLight.transform.forward, range, profile.m_reflectedLayers))
                        {
                            m_waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, 1f);
                        }
                        else
                        {
                            m_waterMaterial.SetFloat(GaiaShaderID.m_waterSmoothness, smoothness);
                        }
                    }
                    else
                    {
                        SunLight = GaiaUtils.GetMainDirectionalLight();
                    }
                }
#endif

#endif
            }
        }
        /// <summary>
        /// Clears all the data/buffers
        /// </summary>
        private void ClearAllData()
        {
            ClearData();
            //Stops prerenders and clears data
            switch (RenderPipeline)
            {
                case GaiaConstants.EnvironmentRenderer.Universal:
                    RenderPipelineManager.beginCameraRendering += OnRenderSRP;
                    break;
                case GaiaConstants.EnvironmentRenderer.HighDefinition:
                    //RenderPipelineManager.beginCameraRendering += OnRenderSRP;
                    break;
            }
        }
        /// <summary>
        /// Checks to see if the active gameobject has a mesh renderer
        /// </summary>
        /// <returns></returns>
        private bool CheckRendererComponent()
        {
            if (GetComponent<Renderer>())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Loads the missing resources
        /// </summary>
        private void LoadResources()
        {
            if (m_instance == null)
            {
                m_instance = this;
            }

            if (m_gameCamera == null)
            {
                m_gameCamera = GaiaUtils.GetCamera();
            }
            if (Application.isPlaying)
            {
                m_RenderCamera = m_gameCamera;
            }
            else
            {
#if UNITY_EDITOR
                if (SceneView.lastActiveSceneView == null)
                {
                    return;
                }
                m_RenderCamera = SceneView.lastActiveSceneView.camera;
#endif
            }

            if (m_player == null)
            {
                m_player = GaiaUtils.GetPlayerTransform();
            }
            if (m_player == null)
            {
                if (m_RenderCamera)
                {
                    m_player = m_RenderCamera.transform;
                }
            }
#if UNITY_EDITOR
            if (m_gaiaSession == null)
            {
                m_gaiaSession = GaiaSessionManager.GetSessionManager(false, false);
            }
#endif
            if (SunLight == null)
            {
                SunLight = GaiaUtils.GetMainDirectionalLight();
            }

            if (m_currentGradient == null)
            {
                m_currentGradient = new Gradient();
            }
        }
        /// <summary>
        /// Update Aura
        /// </summary>
        private void UpdateAura()
        {
#if AURA_IN_PROJECT
            Aura2API.AuraCamera.CommonDataManager.UpdateData();
#endif
        }

        /// <summary>
        /// Updates the infinate ocean mode
        /// </summary>
        private void UpdateInfiniteMode()
        {
            Transform camera = m_player;
            if (camera != null)
            {
                gameObject.transform.position = new Vector3(camera.position.x, SeaLevel, camera.position.z);
            }
        }
        /// <summary>
        /// Sets the infinate mode
        /// </summary>
        /// <param name="active"></param>
        private void SetInfiniteMode(bool active, float refreshRate = 0.1f)
        {
            if (active)
            {
                if (IsInvoking("UpdateInfiniteMode"))
                {
                    CancelInvoke("UpdateInfiniteMode");
                }
                InvokeRepeating("UpdateInfiniteMode", 0.05f, refreshRate);
            }
            else
            {
                if (IsInvoking("UpdateInfiniteMode"))
                {
                    CancelInvoke("UpdateInfiniteMode");
                }
            }
        }

        #endregion

        #region Build/Clear Reflections

        /// <summary>
        /// Processes SRP reflections baased on the active pipeline
        /// </summary>
        private void ProcessOnEnableReflections()
        {
            switch (RenderPipeline)
            {
                case GaiaConstants.EnvironmentRenderer.BuiltIn:
                    RemoveURPPlanarReflections();
                    RemoveHDPRPlanarReflections();
                    break;
                case GaiaConstants.EnvironmentRenderer.Universal:
                    UpdateURPPlanarReflections();
                    RemoveBuiltInReflectionCamera(m_reflectionCamera);
                    RemoveHDPRPlanarReflections();
                    break;
                case GaiaConstants.EnvironmentRenderer.HighDefinition:
                    UpdateHDPPlanarReflections();
                    RemoveBuiltInReflectionCamera(m_reflectionCamera);
                    RemoveURPPlanarReflections();
                    break;
            }
        }
        /// <summary>
        /// Used to clear Camera and Render texture
        /// </summary>
        public void ClearData(bool dataOnly = false)
        {
            if (dataOnly)
            {
                if (m_reflectionTexture)
                {
                    m_reflectionTexture.Release();
                    DestroyImmediate(m_reflectionTexture);
                    m_reflectionTexture = null;
                }

                if (RenderPipeline == GaiaConstants.EnvironmentRenderer.Universal || RenderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    RenderPipelineManager.beginCameraRendering -= OnRenderSRP;
                }
            }
            else
            {
                if (m_reflectionTexture)
                {
                    m_reflectionTexture.Release();
                    DestroyImmediate(m_reflectionTexture);
                    m_reflectionTexture = null;
                }

                if (m_reflectionCamera)
                {
                    DestroyImmediate(m_reflectionCamera.gameObject);
                }

                if (RenderPipeline == GaiaConstants.EnvironmentRenderer.Universal || RenderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    RenderPipelineManager.beginCameraRendering -= OnRenderSRP;
                }
            }
        }
        /// <summary>
        /// Builds the necessary steps to produce a reflection
        /// </summary>
        private void BuildReflection(SceneProfile profile)
        {
            if (m_disableAllReflections)
            {
                return;
            }

            if (m_RenderCamera == null)
            {
                m_RenderCamera = GaiaUtils.GetCamera();
            }

            if (m_underwaterFX != null)
            {
                if (m_underwaterFX.IsUnderwater)
                {
                    return;
                }
            }

            bool reflectionsEnabled = profile.m_enableReflections;
            if (Application.isPlaying)
            {
                if (profile.m_enableDisabeHeightFeature)
                {
                    if (m_player!=null && m_player.position.y > profile.m_disableHeight)
                    {
                        reflectionsEnabled = true;
                    }
                }
            }

            if (m_RenderCamera != null)
            {
                if (m_ableToRender)
                {
                    m_ableToRender = false;
                    m_worldPosition = transform.position;
                    m_normal = transform.up;
                    GenerateReflection(profile, reflectionsEnabled);
                    CreateMirrorObjects(profile);
                    m_ableToRender = true;
                }
            }
            else
            {
                Debug.Log("no rendering camera found");
            }
        }
        /// <summary>
        /// Can be used to keep the cameras settings in check
        /// </summary>
        private void ResyncCameraSettings(SceneProfile profile)
        {
            if (m_RenderCamera != null)
            {
                if (m_reflectionCamera == null)
                {
                    return;
                }

                m_reflectionCamera.orthographic = m_RenderCamera.orthographic;
                m_reflectionCamera.fieldOfView = m_RenderCamera.fieldOfView;
                m_reflectionCamera.aspect = m_RenderCamera.aspect;
                m_reflectionCamera.orthographicSize = m_RenderCamera.orthographicSize;
                m_reflectionCamera.renderingPath = m_RenderCamera.actualRenderingPath;

                if (profile == null)
                {
                    return;
                }

                m_reflectionCamera.allowHDR = profile.m_useHDR;
                m_reflectionCamera.allowMSAA = profile.m_allowMSAA;

#if MEWLIST_MASSIVE_CLOUDS
                SetMassiveCloudSystem(m_gameCamera, m_reflectionCamera);
#endif
            }
        }

#if MEWLIST_MASSIVE_CLOUDS
        private void SetMassiveCloudSystem(Camera gameCamera, Camera reflectionCamera, bool cleanup = false)
        {
            if (cleanup)
            {
                MassiveCloudsCameraEffect[] massiveCloudComponentsCameras = FindObjectsOfType<MassiveCloudsCameraEffect>();
                if (massiveCloudComponentsCameras.Length > 0)
                {
                    for (int i = 0; i < massiveCloudComponentsCameras.Length; i++)
                    {
                        DestroyImmediate(massiveCloudComponentsCameras[i]);
                    }
                }

                MassiveClouds[] massiveCloudComponentsMains = FindObjectsOfType<MassiveClouds>();
                if (massiveCloudComponentsMains.Length > 0)
                {
                    for (int i = 0; i < massiveCloudComponentsMains.Length; i++)
                    {
                        DestroyImmediate(massiveCloudComponentsMains[i]);
                    }
                }
            }

            MassiveCloudsCameraEffect massiveCloudComponentsCamera = gameCamera.GetComponent<MassiveCloudsCameraEffect>();
            MassiveCloudsCameraEffect reflectionMassiveCloudComponentsCamera = reflectionCamera.GetComponent<MassiveCloudsCameraEffect>();
            if (massiveCloudComponentsCamera)
            {
                if (reflectionMassiveCloudComponentsCamera == null)
                {
                    reflectionMassiveCloudComponentsCamera = reflectionCamera.gameObject.AddComponent<MassiveCloudsCameraEffect>();
                }
            }
            else
            {
                if (reflectionMassiveCloudComponentsCamera != null)
                {
                    DestroyImmediate(reflectionMassiveCloudComponentsCamera);
                }
            }

            MassiveClouds massiveCloudComponentsMain = gameCamera.GetComponent<MassiveClouds>();
            MassiveClouds reflectionMassiveCloudComponentsMain = reflectionCamera.GetComponent<MassiveClouds>();
            if (massiveCloudComponentsMain)
            {
                if (reflectionMassiveCloudComponentsMain == null)
                {
                    reflectionMassiveCloudComponentsMain = reflectionCamera.gameObject.AddComponent<MassiveClouds>();
                }

                reflectionMassiveCloudComponentsMain.SetProfiles(massiveCloudComponentsMain.Profiles);
                reflectionMassiveCloudComponentsMain.SetParameters(massiveCloudComponentsMain.Parameters);
            }
            else
            {
                if (reflectionMassiveCloudComponentsMain != null)
                {
                    DestroyImmediate(reflectionMassiveCloudComponentsMain);
                }
            }
        }
#endif
        /// <summary>
        /// Updates Cameras flags, background color & when present the sky
        /// </summary>
        private void UpdateCameraModes()
        {
            if (m_reflectionCamera == null)
            {
                return;
            }
            m_reflectionCamera.clearFlags = m_RenderCamera.clearFlags;
            m_reflectionCamera.backgroundColor = m_RenderCamera.backgroundColor;
        }
        /// <summary>
        /// Create a mirror texture
        /// </summary>
        private void CreateMirrorObjects(SceneProfile profile)
        {
            if (m_oldRenderTextureSize != profile.m_textureResolution || m_rebuild || m_reflectionTexture == null || profile.m_useHDR != m_hdr)
            {
                CreateTexture(profile);
                m_reflectionTexture.hideFlags = HideFlags.HideAndDontSave;
                m_oldRenderTextureSize = profile.m_textureResolution;
                m_hdr = profile.m_useHDR;
            }
        }
        /// <summary>
        /// Creates the render texture
        /// </summary>
        private void CreateTexture(SceneProfile profile)
        {
            if (profile.m_textureResolution < 1)
            {
                return;
            }

            if (profile.m_useHDR)
            {
                m_reflectionTexture = new RenderTexture(profile.m_textureResolution, profile.m_textureResolution, 24, DefaultFormat.HDR)
                {
                    name = m_reflectionName + GetInstanceID(),
                    isPowerOfTwo = true
                };
            }
            else
            {
                m_reflectionTexture = new RenderTexture(profile.m_textureResolution, profile.m_textureResolution, 24)
                {
                    name = m_reflectionName + GetInstanceID(),
                    isPowerOfTwo = true
                };
            }
        }
        /// <summary>
        /// Generates the camera used for the mirror
        /// </summary>
        private void GenerateCamera(SceneProfile profile)
        {
            if (m_RenderCamera == null)
            {
                m_RenderCamera = GaiaUtils.GetCamera();
            }

            switch (RenderPipeline)
            {
                case GaiaConstants.EnvironmentRenderer.BuiltIn:
                {
                    if (m_reflectionCamera == null && m_RenderCamera != null)
                    {
                        GameObject MirrorGameObject = new GameObject(m_reflectionIDName + GetInstanceID() + m_forText + m_RenderCamera.GetInstanceID(), typeof(Camera));
                        m_reflectionCamera = MirrorGameObject.GetComponent<Camera>();
                        m_reflectionCamera.enabled = false;
                        m_reflectionCamera.transform.position = transform.position;
                        m_reflectionCamera.transform.rotation = transform.rotation;
                        MirrorGameObject.hideFlags = HideFlags.HideAndDontSave;
                    }
                    else if (m_reflectionCamera != null)
                    {
                        m_reflectionCamera.enabled = false;
                        m_reflectionCamera.transform.position = transform.position;
                        m_reflectionCamera.transform.rotation = transform.rotation;
                        m_reflectionCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                        m_reflectionCamera.transform.SetParent(gameObject.transform);
                    }

                    if (m_RenderCamera != null)
                    {
                        ResyncCameraSettings(profile);
                        UpdateCameraModes();
                    }

                    break;
                }
                case GaiaConstants.EnvironmentRenderer.Universal:
                    UpdateURPPlanarReflections();
                    break;
            }
        }
        /// <summary>
        /// Generates the reflection based on the main camera
        /// </summary>
        private void GenerateReflection(SceneProfile profile, bool reflectionsEnabled)
        {
            //
            switch (RenderPipeline)
            {
                case GaiaConstants.EnvironmentRenderer.BuiltIn:
                    UpdateBuiltInPlanarReflections(profile, reflectionsEnabled);
                    break;
                case GaiaConstants.EnvironmentRenderer.Universal:
                    UpdateURPPlanarReflections();
                    break;
                case GaiaConstants.EnvironmentRenderer.HighDefinition:
                    UpdateHDPPlanarReflections();
                    break;
            }
        }
        /// <summary>
        /// Given position/normal of the plane, calculates plane in camera space.
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="pos"></param>
        /// <param name="normal"></param>
        /// <param name="sideSign"></param>
        /// <returns></returns>
        private Vector4 CameraSpacePlane(SceneProfile profile, Camera cam, Vector3 pos, Vector3 normal, float sideSign)
        {
            Vector3 offsetPos = pos + normal * profile.m_clipPlaneOffset;
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3 cpos = m.MultiplyPoint(offsetPos);
            Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }
        /// <summary>
        /// Calculates reflection matrix around the given plane
        /// </summary>
        /// <param name="reflectionMat"></param>
        /// <param name="plane"></param>
        private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
        {
            reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
            reflectionMat.m01 = (-2F * plane[0] * plane[1]);
            reflectionMat.m02 = (-2F * plane[0] * plane[2]);
            reflectionMat.m03 = (-2F * plane[3] * plane[0]);

            reflectionMat.m10 = (-2F * plane[1] * plane[0]);
            reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
            reflectionMat.m12 = (-2F * plane[1] * plane[2]);
            reflectionMat.m13 = (-2F * plane[3] * plane[1]);

            reflectionMat.m20 = (-2F * plane[2] * plane[0]);
            reflectionMat.m21 = (-2F * plane[2] * plane[1]);
            reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
            reflectionMat.m23 = (-2F * plane[3] * plane[2]);

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;
        }
        /// <summary>
        /// Updates the built in planar reflections
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="reflectionsEnabled"></param>
        private void UpdateBuiltInPlanarReflections(SceneProfile profile, bool reflectionsEnabled)
        {
            //if (!m_setupFog)
            //{
            //    GaiaSceneLighting.AddReflectionFog(m_reflectionCamera, true);
            //    m_setupFog = true;
            //}

            float DotProduct = -Vector3.Dot(m_normal, m_worldPosition) - profile.m_clipPlaneOffset;
            Vector4 ReflectionPlane = new Vector4(m_normal.x, m_normal.y, m_normal.z, DotProduct);
            m_reflection = Matrix4x4.zero;
            CalculateReflectionMatrix(ref m_reflection, ReflectionPlane);
            m_oldPosition = m_RenderCamera.transform.localPosition;
            m_newPosition = m_reflection.MultiplyPoint(m_oldPosition);
            m_reflectionCamera.worldToCameraMatrix = m_RenderCamera.worldToCameraMatrix * m_reflection;
            m_clipPlane = CameraSpacePlane(profile, m_reflectionCamera, m_worldPosition, m_normal, 1.0f);
            m_projection = m_RenderCamera.CalculateObliqueMatrix(m_clipPlane);
            m_reflectionCamera.projectionMatrix = m_projection;

            if (!reflectionsEnabled)
            {
                m_reflectionCamera.cullingMask = 0;
            }
            else
            {
                m_reflectionCamera.cullingMask = ~(1 << 1) & ~(1 << 2) & ~(1 << 4) & ~(2 << 4) & profile.m_reflectedLayers.value;
                profile.m_reflectedLayers.value = m_reflectionCamera.cullingMask;
                if (profile.m_useCustomRenderDistance)
                {
                    if (profile.m_enableLayerDistances)
                    {
                        m_reflectionCamera.layerCullDistances = profile.m_customRenderDistances;
                        m_reflectionCamera.layerCullSpherical = true;
                    }
                    else
                    {
                        float[] distances = new float[32];
                        for (int idx = 0; idx < distances.Length; idx++)
                        {
                            distances[idx] = profile.m_customRenderDistance;
                        }
                        m_reflectionCamera.layerCullDistances = distances;
                        m_reflectionCamera.layerCullSpherical = true;
                    }
                }
                else
                {
                    m_reflectionCamera.layerCullDistances = m_RenderCamera.layerCullDistances;
                    m_reflectionCamera.layerCullSpherical = true;
                }
            }

            m_reflectionCamera.targetTexture = m_reflectionTexture;
            GL.invertCulling = true;
            m_reflectionCamera.transform.position = m_newPosition;
            m_euler = m_RenderCamera.transform.eulerAngles;
#if GAIA_XR
            //m_reflectionCamera.transform.eulerAngles = new Vector3(m_euler.x, m_euler.y, m_euler.z);
            m_reflectionCamera.transform.eulerAngles = Vector3.zero;
#else
            m_reflectionCamera.transform.eulerAngles = new Vector3(0, m_euler.y, m_euler.z);
#endif
            if (profile.m_useHDR)
            {
                m_reflectionCamera.allowHDR = true;
            }

            m_reflectionCamera.nearClipPlane = 0.001f;
            m_reflectionCamera.Render();
            m_reflectionCamera.transform.position = m_oldPosition;
            GL.invertCulling = false;

            SetReflectionTexture();
        }

        public void SetReflectionTexture(Texture overrideTex = null)
        {
            if (m_waterMaterial != null)
            {
                if (overrideTex != null)
                {
                    m_waterMaterial.SetTexture(GaiaShaderID.m_globalReflectionTexture, overrideTex);
                }
                else
                {
                    m_waterMaterial.SetTexture(GaiaShaderID.m_globalReflectionTexture, m_reflectionTexture);
                }
            }
        }

        /// <summary>
        /// Removes built-in reflection camera
        /// </summary>
        /// <param name="cam"></param>
        private void RemoveBuiltInReflectionCamera(Camera cam)
        {
            if (cam != null)
            {
                DestroyImmediate(cam.gameObject);
            }
        }

        #region URP

        /// <summary>
        /// Creates the planar reflections for URP and resyncs the settings
        /// </summary>
        private void UpdateURPPlanarReflections()
        {
            if (m_planarReflections == null)
            {
                m_planarReflections = CreatePlanarReflections(false);
            }

            ResyncPlanarReflectionSettings();
        }
        /// <summary>
        /// Creates a gaia planar reflections
        /// </summary>
        /// <param name="hideObject"></param>
        /// <returns></returns>
        private GaiaPlanarReflections CreatePlanarReflections(bool hideObject = true)
        {
            GaiaPlanarReflections planarReflections = FindObjectOfType<GaiaPlanarReflections>();
            if (planarReflections == null)
            {
                GameObject planarObject = new GameObject(GaiaConstants.gaiaPlanarReflections);
                planarReflections = planarObject.AddComponent<GaiaPlanarReflections>();

                planarObject.transform.SetParent(gameObject.transform);
                if (hideObject)
                {
                    planarObject.hideFlags = HideFlags.HideInHierarchy;
                }
                else
                {
                    planarObject.hideFlags = HideFlags.None;
                }
            }

            ResyncPlanarReflectionSettings();
            return planarReflections;
        }
        /// <summary>
        /// Resyncs the planar reflection settings
        /// </summary>
        private void ResyncPlanarReflectionSettings()
        {
            if (m_planarReflections != null && m_waterProfile != null)
            {
                m_waterProfile.m_reflectionSettingsData.m_ReflectLayers = m_waterProfile.m_reflectedLayers;
                m_waterProfile.m_reflectionSettingsData.m_textureResolution = m_waterProfile.m_textureResolution;
                m_planarReflections.m_settings = m_waterProfile.m_reflectionSettingsData;
            }
        }
        /// <summary>
        /// Removes URP planar reflections
        /// </summary>
        private void RemoveURPPlanarReflections()
        {
            if (m_planarReflections != null)
            {
                DestroyImmediate(m_planarReflections.gameObject);
            }

            GameObject planarReflectionsCamera = GameObject.Find(GaiaConstants.gaiaPlanarReflectionsCamera);
            if (planarReflectionsCamera != null)
            {
                DestroyImmediate(planarReflectionsCamera);
            }
        }

        #endregion

        #region HDRP

        /// <summary>
        /// Creates the planar reflections for HDRP and resyncs the settings
        /// </summary>
        private void UpdateHDPPlanarReflections()
        {
#if HDPipeline
            if (m_HDPlanarReflections == null)
            {
                m_HDPlanarReflections = CreateHDRPPlanarReflections(false);
            }

            ResyncHDRPPlanarReflectionSettings();
#endif
        }
        /// <summary>
        /// Creates a HDRP planar reflections
        /// </summary>
        /// <param name="hideObject"></param>
        /// <returns></returns>
#if HDPipeline
        private PlanarReflectionProbe CreateHDRPPlanarReflections(bool hideObject = true)
        {
            GameObject planarObject = GameObject.Find(GaiaConstants.gaiaHDRPPlanarReflections);
            if (planarObject == null)
            {
                planarObject = new GameObject(GaiaConstants.gaiaHDRPPlanarReflections);
                planarObject.transform.SetParent(gameObject.transform);
                if (hideObject)
                {
                    planarObject.hideFlags = HideFlags.HideInHierarchy;
                }
                else
                {
                    planarObject.hideFlags = HideFlags.None;
                }
            }

            PlanarReflectionProbe planarReflections = planarObject.GetComponent<PlanarReflectionProbe>();
            if (planarReflections == null)
            {
                planarReflections = planarObject.AddComponent<PlanarReflectionProbe>();
            }

            ProbeSettings settings = planarReflections.settings;
            settings.influence.boxSize = new Vector3(10000f, 5f, 10000f);
            settings.realtimeMode = ProbeSettings.RealtimeMode.EveryFrame;

            ResyncHDRPPlanarReflectionSettings();
            return planarReflections;
        }
#endif
        /// <summary>
        /// Resyncs the HDRP planar reflection settings
        /// </summary>
        private void ResyncHDRPPlanarReflectionSettings()
        {
#if HDPipeline
            if (m_HDPlanarReflections != null && m_waterProfile != null)
            {
                ProbeSettings settings = m_HDPlanarReflections.settings;
                settings.influence.boxSize = new Vector3(10000f, 5f, 10000f);
                settings.realtimeMode = ProbeSettings.RealtimeMode.EveryFrame;
                settings.cameraSettings.culling.cullingMask = ~(1 << 4) & m_waterProfile.m_reflectedLayers.value;
                settings.cameraSettings.culling.useOcclusionCulling = false;

                m_HDPlanarReflections.transform.position = new Vector3(0f, SeaLevel, 0f);
            }
#endif
        }
        /// <summary>
        /// Removes URP planar reflections
        /// </summary>
        private void RemoveHDPRPlanarReflections()
        {
            GameObject hdrpReflections = GameObject.Find(GaiaConstants.gaiaHDRPPlanarReflections);
            if (hdrpReflections != null)
            {
                DestroyImmediate(hdrpReflections);
            }
        }

        #endregion

        #endregion

        #region Shader Functions

        /// <summary>
        /// Updates the water shader values
        /// </summary>
        public void UpdateShaderValues(GaiaWaterProfileValues profileValues)
        {
#if GAIA_PRO_PRESENT
            if (!Application.isPlaying)
            {
                m_weatherSystemPresent = ProceduralWorldsGlobalWeather.Instance;
                WeatherSystem = ProceduralWorldsGlobalWeather.Instance;
            }
#endif
            if (m_waterMaterial == null)
            {
                foreach (var material in m_waterMaterialInstances)
                {
                    if (!material.name.Contains("Under"))
                    {
                        m_waterMaterial = m_waterMaterialInstances[0];
                        break;
                    }
                }
            }
            else
            {
                if (GaiaUtils.ValidateShaderProperty(m_waterMaterial, GaiaShaderID.m_waterGrabPass))
                {
                    m_waterMaterial.SetShaderPassEnabled(GaiaShaderID.m_waterGrabPass, true);
                }
                if (RenderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    if (GaiaUtils.ValidateShaderProperty(m_waterMaterial, GaiaShaderID.m_hdrpZTest))
                    {
                        m_waterMaterial.SetInt(GaiaShaderID.m_hdrpZTest, (int)GaiaConstants.HDRPDepthTest.LessEqual);
                    }
                }
                if (GaiaUtils.ValidateShaderProperty(m_waterMaterial, GaiaShaderID.m_globalAmbientColor))
                {
                    m_waterMaterial.SetColor(GaiaShaderID.m_globalAmbientColor, RenderSettings.ambientSkyColor);
                }
#if GAIA_PRO_PRESENT
                if (m_weatherSystemPresent)
                {
                    if (WeatherSystem.CheckIsNight())
                    {
                        if (WeatherSystem.m_moonLight != null)
                        {
                            SunLight = WeatherSystem.m_moonLight;
                            if (GaiaUtils.ValidateShaderProperty(m_waterMaterial, GaiaShaderID.m_globalLightDirection))
                            {
                                m_waterMaterial.SetVector(GaiaShaderID.m_globalLightDirection, -WeatherSystem.m_moonLight.transform.forward);
                            }
                            if (GaiaUtils.ValidateShaderProperty(m_waterMaterial, GaiaShaderID.m_globalLightColor))
                            {
                                m_waterMaterial.SetColor(GaiaShaderID.m_globalLightColor, WeatherSystem.m_moonLight.color);
                            }
                            if (GaiaUtils.ValidateShaderProperty(m_waterMaterial, GaiaShaderID.m_globalLightIntensity))
                            {
                                m_waterMaterial.SetFloat(GaiaShaderID.m_globalLightIntensity, WeatherSystem.m_moonLight.intensity);
                            }
                        }
                    }
                    else
                    {
                        if (WeatherSystem.m_sunLight != null)
                        {
                            SunLight = WeatherSystem.m_sunLight;
                            if (GaiaUtils.ValidateShaderProperty(m_waterMaterial, GaiaShaderID.m_globalLightDirection))
                            {
                                m_waterMaterial.SetVector(GaiaShaderID.m_globalLightDirection, -WeatherSystem.m_sunLight.transform.forward);
                            }
                            if (GaiaUtils.ValidateShaderProperty(m_waterMaterial, GaiaShaderID.m_globalLightColor))
                            {
                                m_waterMaterial.SetColor(GaiaShaderID.m_globalLightColor, WeatherSystem.m_sunLight.color);
                            }
                            if (GaiaUtils.ValidateShaderProperty(m_waterMaterial, GaiaShaderID.m_globalLightIntensity))
                            {
                                m_waterMaterial.SetFloat(GaiaShaderID.m_globalLightIntensity, WeatherSystem.m_sunLight.intensity);
                            }
                        }
                    }
                }
                else
                {
                    if (SunLight != null)
                    {
                        if (GaiaUtils.ValidateShaderProperty(m_waterMaterial, GaiaShaderID.m_globalLightDirection))
                        {
                            m_waterMaterial.SetVector(GaiaShaderID.m_globalLightDirection, -SunLight.transform.forward);
                        }
                        if (GaiaUtils.ValidateShaderProperty(m_waterMaterial, GaiaShaderID.m_globalLightColor))
                        {
                            m_waterMaterial.SetColor(GaiaShaderID.m_globalLightColor, SunLight.color);
                        }
                        if (GaiaUtils.ValidateShaderProperty(m_waterMaterial, GaiaShaderID.m_globalLightIntensity))
                        {
                            m_waterMaterial.SetFloat(GaiaShaderID.m_globalLightIntensity, SunLight.intensity);
                        }
                    }
                }
#else
                if (SunLight != null)
                {
                    if (GaiaUtils.ValidateShaderProperty(m_waterMaterial, GaiaShaderID.m_globalLightDirection))
                    {
                        m_waterMaterial.SetVector(GaiaShaderID.m_globalLightDirection, -SunLight.transform.forward);
                    }
                    if (GaiaUtils.ValidateShaderProperty(m_waterMaterial, GaiaShaderID.m_globalLightColor))
                    {
                        m_waterMaterial.SetColor(GaiaShaderID.m_globalLightColor, SunLight.color);
                    }
                    if (GaiaUtils.ValidateShaderProperty(m_waterMaterial, GaiaShaderID.m_globalLightIntensity))
                    {
                        m_waterMaterial.SetFloat(GaiaShaderID.m_globalLightIntensity, SunLight.intensity);
                    }
                }
#endif
            }
        }

        /// <summary>
        /// Sets the sun light on the water system
        /// </summary>
        /// <param name="light"></param>
        public static void SetSunLight(Light light)
        {
            if (light == null)
            {
                return;
            }

            if (Instance == null)
            {
                return;
            }

            Instance.SunLight = light;
        }

        #endregion

        #endregion

        #region Underwater/Refraction Setup

        //Public
	    public void RefractionInit()
	    {
		    if (refractionEnabled) 
		    {
			    Shader.EnableKeyword(GaiaShaderID.m_refractionOn);
                Shader.SetGlobalInt(GaiaShaderID.m_blendSRC, (int)UnityEngine.Rendering.BlendMode.One);
			    Shader.SetGlobalInt(GaiaShaderID.m_blendDST, (int)UnityEngine.Rendering.BlendMode.Zero);
		    }
		    else
		    {
			    Shader.DisableKeyword(GaiaShaderID.m_refractionOn);
                Shader.SetGlobalInt(GaiaShaderID.m_blendSRC, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			    Shader.SetGlobalInt(GaiaShaderID.m_blendDST, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		    }
        }
        //Private
        private void SendWindDirection()
	    {
		    Vector2 dir = (Vector2) ( Quaternion.Euler ( 0, 0, directionAngle ) * Vector2.right );

		    Vector4 vec4Dir = Vector4.zero;

		    vec4Dir.x = dir.x;
		    vec4Dir.z = dir.y;

		    m_waterMaterial.SetVector (GaiaShaderID._waveDirection_ID, vec4Dir );
            m_waterMaterial.SetVector (GaiaShaderID._waveDirection_ID, vec4Dir );
	    }
        private void SendWaterEdge()
	    {
            m_waterMaterial.SetVector (GaiaShaderID._edgeWaterColor_ID, m_waterMaterial.GetVector (GaiaShaderID._edgeWaterColor_ID ) );
            m_waterMaterial.SetFloat (GaiaShaderID._edgeWaterDist_ID, m_waterMaterial.GetFloat (GaiaShaderID._edgeWaterDist_ID ));
	    }
        private void SendNormalMaps()
	    {
            m_waterMaterial.SetTexture (GaiaShaderID._normalLayer0_ID, m_waterMaterial.GetTexture (GaiaShaderID._normalLayer0_ID ) );
            m_waterMaterial.SetTexture (GaiaShaderID._normalLayer1_ID, m_waterMaterial.GetTexture (GaiaShaderID._normalLayer1_ID ) );
	    }
        private void SendWaveData()
	    {
            m_waterMaterial.SetFloat (GaiaShaderID._normalLayer0_ID, m_waterMaterial.GetFloat (GaiaShaderID._normalLayer0Scale_ID ) );
            m_waterMaterial.SetFloat (GaiaShaderID._normalLayer1_ID, m_waterMaterial.GetFloat (GaiaShaderID._normalLayer1Scale_ID ) );

            m_waterMaterial.SetFloat (GaiaShaderID._normalTile_ID, m_waterMaterial.GetFloat (GaiaShaderID._normalTile_ID ) );

            m_waterMaterial.SetFloat (GaiaShaderID._waveShoreClamp_ID, m_waterMaterial.GetFloat (GaiaShaderID._waveShoreClamp_ID ) );
            m_waterMaterial.SetFloat (GaiaShaderID._waveLength_ID, m_waterMaterial.GetFloat (GaiaShaderID._waveLength_ID ) );
            m_waterMaterial.SetFloat (GaiaShaderID._waveSteepness_ID, m_waterMaterial.GetFloat (GaiaShaderID._waveSteepness_ID ) );
            m_waterMaterial.SetFloat (GaiaShaderID._waveSpeed_ID, m_waterMaterial.GetFloat (GaiaShaderID._waveSpeed_ID ) );
            m_waterMaterial.SetVector (GaiaShaderID._waveDirection_ID, m_waterMaterial.GetVector (GaiaShaderID._waveDirection_ID ) );
	    }

        #endregion

        #region Mesh Generation

#region WaterGeneration

        /// <summary>
        /// Creates a procedural mesh with a vertex count and size.
        /// </summary>
        public void ProceduralMeshGeneration()
        {
            SizeGeneration();
            if (m_MeshType == GaiaConstants.MeshType.Plane)
            {
                if (m_uvScale.y <= 0 || m_uvScale.x <= 0 || m_sizeScale.x <= 0 || m_sizeScale.y <= 0)
                {
                    Debug.Log("Size was 0, unable to generate mesh");
                    return;
                }
            }
            if (CalculatePolysRequired() == 0)
            {
                Debug.LogWarning("The selected water size is too small for the selected water mesh quality. Please increase water size and / or water mesh quality.");
                return;
            }

            if (!GetComponent<MeshRenderer>())
            {
                Debug.Log("Warning Mesh Render is missing for the water generator");
            }
            if (GetComponent<MeshFilter>())
            {
                MeshFilter meshFilter = GetComponent<MeshFilter>();
                m_generateMesh = new Mesh
                {
                    name = "Procedural Grid"
                };
                VerticesGeneration();
                TriangleGeneration();
                UVGeneration();
                NormalGeneration();
                BoundsGeneration();
                meshFilter.mesh = m_generateMesh;
                ClearMeshData();
            }
            else
            {
                Debug.Log("unable to find MeshFilter, unable to generate mesh");
            }
        }

#endregion

#region MeshGeneration

        /// <summary>
        /// Size Generation for a given object
        /// </summary>
        private void SizeGeneration()
        {
            if (m_MeshType == GaiaConstants.MeshType.Plane)
            {
                m_numberOfPoints = new Vector2Int(Mathf.RoundToInt(m_meshDensity.x / 40 * m_Size.x) +1, Mathf.RoundToInt(m_meshDensity.y / 40 * m_Size.z)+1);
                m_uvScale = new Vector2(1f / (m_numberOfPoints.x), 1f / (m_numberOfPoints.y));
                m_sizeScale = new Vector3(m_Size.x / (m_numberOfPoints.x -1), m_Size.y, m_Size.z / (m_numberOfPoints.y -1));
            }
            else
            {
                
                float half = m_Size.x / 2f;
                m_numberOfPoints = new Vector2Int(Mathf.RoundToInt(m_meshDensity.x / 40 * half), Mathf.RoundToInt(m_meshDensity.y / 40 * half));
                m_uvScale = new Vector2(1f / (m_meshDensity.x), 1f / (m_meshDensity.x));
                m_sizeScale = new Vector3(half / (m_numberOfPoints.x), m_Size.y, half / (m_numberOfPoints.y));
                //m_sizeScale = new Vector3(half / (m_meshDensity.x), m_Size.y, half / (m_meshDensity.x));
                
            }
        }
        /// <summary>
        /// Generates the vertices
        /// </summary>
        private void VerticesGeneration()
        {
            if (m_MeshType == GaiaConstants.MeshType.Plane)
            {
                var MiddlePoint = new Vector3((float)(m_numberOfPoints.x -1) / 2, 0, (float)(m_numberOfPoints.y -1) / 2);
                for (int y = 0; y < (int)m_numberOfPoints.y ; y++)
                {
                    for (int x = 0; x < (int)m_numberOfPoints.x; x++)
                    {
                        Vector3 vertex = new Vector3(MiddlePoint.x - x, m_height, MiddlePoint.z - y);
                        m_vertices.Add(Vector3.Scale(m_sizeScale, vertex));
                    }
                }

                //Determine number of vertices for mesh index format
                if (m_vertices.Count < 64000)
                {
                    m_generateMesh.indexFormat = IndexFormat.UInt16;
                }
                else
                {
                    m_generateMesh.indexFormat = IndexFormat.UInt32;
                }

                m_generateMesh.vertices = m_vertices.ToArray();
                
            }
            else
            {
                m_vertices.Add(Vector3.Scale(m_sizeScale, Vector3.zero));
                for (int CirclePoint = 0; CirclePoint < m_numberOfPoints.x; CirclePoint++)
                {
                    //angle step is the next position in the circle to iterate over.
                    //Mathf.PI one side, *2 both sides
                    float angleStep = (Mathf.PI * 2f) / ((CirclePoint + 1) * 6);
                    for (int point = 0; point < (CirclePoint + 1) * 6; point++)
                    {
                        Vector3 vertex = new Vector3(Mathf.Cos(angleStep * point), 0, Mathf.Sin(-angleStep * point));
                        vertex = vertex * 1 * (CirclePoint + 1);
                        m_vertices.Add(Vector3.Scale(m_sizeScale, vertex));

                    }
                }

                //Determine number of vertices for mesh index format
                if (m_vertices.Count < 64000)
                {
                    m_generateMesh.indexFormat = IndexFormat.UInt16;
                }
                else
                {
                    m_generateMesh.indexFormat = IndexFormat.UInt32;
                }

                m_generateMesh.vertices = m_vertices.ToArray();
            }
        }
        /// <summary>
        /// Generates the Triangles
        /// </summary>
        private void TriangleGeneration()
        {
            if (m_MeshType == GaiaConstants.MeshType.Plane)
            {
                int x, y;
                for (y = 0; y < (int)m_numberOfPoints.y - 1; y++)
                {
                    for (x = 0; x < (int)m_numberOfPoints.x - 1; x++)
                    {
                        // For each grid cell output two triangles
                        m_triangles.Add((y * (int)m_numberOfPoints.x) + x);
                        m_triangles.Add(((y + 1) * (int)m_numberOfPoints.x) + x);
                        m_triangles.Add((y * (int)m_numberOfPoints.x) + x + 1);
                        m_triangles.Add(((y + 1) * (int)m_numberOfPoints.x) + x);
                        m_triangles.Add(((y + 1) * (int)m_numberOfPoints.x) + x + 1);
                        m_triangles.Add((y * (int)m_numberOfPoints.x) + x + 1);
                    }
                }
                m_generateMesh.triangles = m_triangles.ToArray();
            }
            else
            {
                m_triangles.Clear();
                for (int circ = 0; circ < (int)m_numberOfPoints.x; circ++)
                {
                    for (int point = 0, other = 0; point < (circ + 1) * 6; point++)
                    {
                        if (point % (circ + 1) != 0)
                        {
                            // Creates 2 triangles (square generation)
                            m_triangles.Add(GetPoint(circ - 1, other + 1));
                            m_triangles.Add(GetPoint(circ - 1, other));
                            m_triangles.Add(GetPoint(circ, point));
                            //second triangle
                            m_triangles.Add(GetPoint(circ, point));
                            m_triangles.Add(GetPoint(circ, point + 1));
                            m_triangles.Add(GetPoint(circ - 1, other + 1));
                            ++other;
                        }
                        else
                        {
                            // Creates the triangles for sections in the core and 4 break up points.
                            m_triangles.Add(GetPoint(circ, point));
                            m_triangles.Add(GetPoint(circ, point + 1));
                            m_triangles.Add(GetPoint(circ - 1, other));
                            // Do not move to the next point in the smaller circles
                        }
                    }
                }
                m_generateMesh.triangles = m_triangles.ToArray();
            }
        }
        /// <summary>
        /// Calculates the normal of the mesh
        /// </summary>
        private void NormalGeneration()
        {
            m_generateMesh.RecalculateNormals();
        }
        /// <summary>
        /// Generates the UV map for the mesh
        /// </summary>
        private void UVGeneration()
        {
            if (m_MeshType == GaiaConstants.MeshType.Plane)
            {
                Vector2 MiddlePoint = new Vector2(m_numberOfPoints.x / 2, m_numberOfPoints.y / 2);
                for (int y = 0; y < (int)m_numberOfPoints.y; y++)
                {
                    for (int x = 0; x < (int)m_numberOfPoints.x; x++)
                    {
                        m_uvs.Add(Vector2.Scale(new Vector2(MiddlePoint.x - x, MiddlePoint.y- y), m_uvScale));
                    }
                }
                m_generateMesh.uv = m_uvs.ToArray();
            }
            else
            {
                m_uvs.Add(Vector2.Scale(m_uvScale, Vector2.zero));
                for (int CirclePoint = 0; CirclePoint < m_numberOfPoints.x; CirclePoint++)
                {
                    float angleStep = (Mathf.PI * 2f) / ((CirclePoint + 1) * 6);
                    for (int point = 0; point < (CirclePoint + 1) * 6; point++)
                    {
                        Vector2 vertex = new Vector2(Mathf.Cos(angleStep * point), Mathf.Sin(-angleStep * point));
                        vertex = vertex * 1 * (CirclePoint + 1);
                        m_uvs.Add(Vector2.Scale(vertex, m_uvScale));
                    }
                }
                m_generateMesh.uv = m_uvs.ToArray();
            }
        }
        /// <summary>
        /// Generates the bounds of the mesh
        /// </summary>
        private void BoundsGeneration()
        {
            m_generateMesh.RecalculateBounds();
        }

#endregion

#region ClearMemory 

        /// <summary>
        /// Clears memory of lists and arrays
        /// </summary>
        public void ClearMeshData()
        {
            m_vertices.Clear();
            m_uvs.Clear();
            m_triangles.Clear();
        }

#endregion

#region Point Calculuation

        /// <summary>
        /// Gets the point on a circle
        /// </summary>
        /// <param name="Center"></param>
        /// <param name="Extent"></param>
        /// <returns></returns>
        private static int GetPoint(int Center, int Extent)
        {
            // In case of center point no calculation needed
            if (Center < 0)
            {
                return 0;
            }
            Extent = Extent % ((Center + 1) * 6);
            // Make the point index circular
            // Explanation: index = number of points in previous circles + central point + x
            // hence: (0+1+2+...+c)*6+x+1 = ((c/2)*(c+1))*6+x+1 = 3*c*(c+1)+x+1
            return (3 * Center * (Center + 1) + Extent + 1);
        }

#endregion

#region DensityCalculation

        /// <summary>
        /// Calculate the real amount of triangles.
        /// dependent on MeshType selected.
        /// </summary>
        /// <returns>total triangle count</returns>
        public int CalculatePolysRequired()
        {
            SizeGeneration();
            if (m_MeshType == GaiaConstants.MeshType.Plane)
            {
                return (int)(m_numberOfPoints.y-1) * (int)(m_numberOfPoints.x-1) *2;
            }
            else
            {
                return (int)m_numberOfPoints.x * ((int)m_numberOfPoints.x) * 6;
            }
        }

#endregion

#endregion
    }
}