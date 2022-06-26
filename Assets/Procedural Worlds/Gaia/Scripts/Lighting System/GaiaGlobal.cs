#if UNITY_EDITOR
using UnityEditor;
#endif
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
using UnityEngine;
using UnityEngine.Rendering;
#if HDPipeline
using UnityEngine.Rendering.HighDefinition;
#endif
using Gaia.Pipeline.HDRP;
using ProceduralWorlds.WaterSystem;

namespace Gaia
{
    [System.Serializable]
    public class GaiaTimeOfDay
    {
        public GaiaConstants.TimeOfDayStartingMode m_todStartingType;
        public bool m_todEnabled;
        public float m_todDayTimeScale;
        public int m_todHour;
        public float m_todMinutes;
    }

    [System.Serializable]
    public class GaiaWeather
    {
        public float m_season;
        public float m_windDirection;
    }

    [ExecuteAlways]
    public class GaiaGlobal : MonoBehaviour
    {
        public static GaiaGlobal Instance
        {
            get { return m_instance; }
            set
            {
                if (m_instance != value)
                {
                    m_instance = value;
                }
            }
        }
        [SerializeField]
        private static GaiaGlobal m_instance;

        public SceneProfile SceneProfile
        {
            get
            {
                if (m_sceneProfile == null)
                {
                    m_sceneProfile = ScriptableObject.CreateInstance<SceneProfile>();
                }

                return m_sceneProfile;
            }
            set { m_sceneProfile = value; }
        }
        [SerializeField]
        private SceneProfile m_sceneProfile;
        public Camera m_mainCamera;
        public Material WaterMaterial;
        public bool m_currentIsUserProfile = false;

        #region Variables Setting Saving

        //public bool m_enableSettingSaving = true;
        //public GaiaLightingProfileValues m_lightingSavedProfileValues;
        //public GaiaWaterProfileValues m_waterSavedProfileValues;

        [Header("Global Settings")]
        public string m_typeOfLighting = "Morning";
        //public GaiaConstants.GaiaLightingProfileType m_profileType = GaiaConstants.GaiaLightingProfileType.Morning;
        public int m_lightingProfileIndex = 0;
        public int m_waterProfileIndex = 0;
        [Header("Post Processing Settings")]
        public string m_postProcessingProfile = "Ambient Sample Default Evening Post Processing";
        public bool m_directToCamera = true;
        [Header("HDRP Post Processing Settings")]
        public string m_hDPostProcessingProfile = "Ambient Sample Default Evening Post Processing";
        [Header("Ambient Audio Settings")]
        [HideInInspector]
        public AudioClip m_ambientAudio;
        [Range(0f, 1f)]
        public float m_ambientVolume = 0.55f;
        [Header("Sun Settings")]
        [Range(0f, 360f)]
        public float m_sunRotation = 0f;
        [Range(0f, 360f)]
        public float m_sunPitch = 65f;
        public Color m_sunColor = Color.white;
        public float m_sunIntensity = 1f;
        [Header("LWRP Sun Settings")]
        public Color m_lWSunColor = Color.white;
        public float m_lWSunIntensity = 1f;
        [Header("HDRP Sun Settings")]
        public Color m_hDSunColor = Color.white;
        public float m_hDSunIntensity = 1f;
        [Header("Sun Shadow Settings")]
        public LightShadows m_shadowCastingMode = LightShadows.Soft;
        [Range(0f, 1f)]
        public float m_shadowStrength = 1f;
        public LightShadowResolution m_sunShadowResolution = LightShadowResolution.FromQualitySettings;
        [Header("HDRP Shadow Settings")]
        public float m_hDShadowDistance = 700f;
        public GaiaConstants.HDShadowResolution m_hDShadowResolution = GaiaConstants.HDShadowResolution.Resolution1024;
        public bool m_hDContactShadows = true;
        public GaiaConstants.ContactShadowsQuality m_hDContactShadowQuality = GaiaConstants.ContactShadowsQuality.Medium;
        public int m_hDContactShadowCustomQuality = 10;
        public float m_hDContactShadowsDistance = 150f;
        [Range(0f, 1f)]
        public float m_hDContactShadowOpacity = 1f;
        public bool m_hDMicroShadows = true;
        [Range(0f, 1f)]
        public float m_hDMicroShadowOpacity = 1f;
        [Header("Skybox Settings")]
        [HideInInspector]
        public Cubemap m_skyboxHDRI;
        public Color m_skyboxTint = new Color(0.5f, 0.5f, 0.5f, 1f);
        [Range(0f, 8f)]
        public float m_skyboxExposure = 1.6f;
        [Space(15)]
        [Range(0f, 1f)]
        public float m_sunSize = 0.04f;
        [Range(0.01f, 10f)]
        public float m_sunConvergence = 10f;
        [Range(0f, 5f)]
        public float m_atmosphereThickness = 1f;
        public Color m_groundColor = Color.gray;
        [Header("HDRP Skybox Settings")]
        public GaiaConstants.HDSkyType m_hDSkyType = GaiaConstants.HDSkyType.HDRI;
        public GaiaConstants.HDSkyUpdateMode m_hDSkyUpdateMode = GaiaConstants.HDSkyUpdateMode.OnChanged;
        [Space(10)]
        //HDRI
        [HideInInspector]
        public Cubemap m_hDHDRISkybox;
        public float m_hDHDRIExposure = 0.75f;
        public float m_hDHDRIMultiplier = 1f;
        [Space(10)]
        //Gradient
        public Color m_hDGradientTopColor = Color.blue;
        public Color m_hDGradientMiddleColor = Color.cyan;
        public Color m_hDGradientBottomColor = Color.white;
        public float m_hDGradientDiffusion = 1f;
        public float m_hDGradientExposure = 0f;
        public float m_hDGradientMultiplier = 1f;
        [Space(10)]
        //Procedural
        public bool m_hDProceduralEnableSunDisk = true;
        public bool m_hDProceduralIncludeSunInBaking = true;
        public float m_hDProceduralSunSize = 0.015f;
        public float m_hDProceduralSunSizeConvergence = 9.5f;
        public float m_hDProceduralAtmosphereThickness = 1f;
        public Color32 m_hDProceduralSkyTint = new Color32(128, 128, 128, 128);
        public Color32 m_hDProceduralGroundColor = new Color32(148, 161, 176, 255);
        public float m_hDProceduralExposure = 1f;
        public float m_hDProceduralMultiplier = 2.5f;
        //Physically Based Sky
        //Planet
        public bool m_hDPBSEarthPreset = true;
        public float m_hDPBSPlanetaryRadius = 6378.759f;
        public bool m_hDPBSSphericalMode = true;
        public float m_hDPBSSeaLevel = 50f;
        public Vector3 m_hDPBSPlantetCenterPosition = new Vector3(0f, -6378.759f, 0f);
        public Vector3 m_hDPBSPlanetRotation = new Vector3(0f, 0f, 0f);
        [HideInInspector]
        public Cubemap m_hDPBSGroundAlbedoTexture;
        public Color m_hDPBSGroundTint = new Color(0.5803922f, 0.6313726f, 0.6901961f);
        [HideInInspector]
        public Cubemap m_hDPBSGroundEmissionTexture;
        public float m_hDPBSGroundEmissionMultiplier = 1f;
        //Space
        public Vector3 m_hDPBSSpaceRotation = new Vector3(0f, 0f, 0f);
        [HideInInspector]
        public Cubemap m_hDPBSSpaceEmissionTexture;
        public float m_hDPBSSpaceEmissionMultiplier = 1f;
        //Air
        public float m_hDPBSAirMaximumAltitude = 70f;
        public Color m_hDPBSAirOpacity = Color.white;
        public Color m_hDPBSAirAlbedo = Color.white;
        public float m_hDPBSAirDensityBlue = 0.232f;
        public Color m_hDPBSAirTint = new Color(0.172f, 0.074f, 0.030f);
        //Aerosols
        public float m_hDPBSAerosolMaximumAltitude = 8.3f;
        public float m_hDPBSAerosolDensity = 0.5f;
        public Color m_hDPBSAerosolAlbedo = Color.white;
        public float m_hDPBSAerosolAnisotropy = 0f;
        //Artistic Overrides
        public float m_hDPBSColorSaturation = 1f;
        public float m_hDPBSAlphaSaturation = 1f;
        public float m_hDPBSAlphaMultiplier = 1f;
        public Color m_hDPBSHorizonTint = Color.white;
        public float m_hDPBSHorizonZenithShift = 0f;
        public Color m_hDPBSZenithTint = Color.white;
        //Miscellaneous
        public int m_hDPBSNumberOfBounces = 8;
        public GaiaConstants.HDIntensityMode m_hDPBSIntensityMode = GaiaConstants.HDIntensityMode.Exposure;
        public float m_hDPBSMultiplier = 1f;
        public float m_hDPBSExposure = 1f;
        public bool m_hDPBSIncludeSunInBaking = true;

        [Header("Ambient Light Settings")]
        public AmbientMode m_ambientMode = AmbientMode.Trilight;
        [Range(0f, 10f)]
        public float m_ambientIntensity = 1f;
        public Color m_skyAmbient = Color.white;
        public Color m_equatorAmbient = Color.gray;
        public Color m_groundAmbient = Color.gray;
        [Header("HDRP Ambient Light Settings")]
        public GaiaConstants.HDAmbientMode m_hDAmbientMode = GaiaConstants.HDAmbientMode.Static;
        public float m_hDAmbientDiffuseIntensity = 1f;
        public float m_hDAmbientSpecularIntensity = 1f;
        [Header("Fog Settings")]
        public FogMode m_fogMode = FogMode.Linear;
        public Color m_fogColor = Color.white;
        [Range(0f, 1f)]
        public float m_fogDensity = 0.01f;
        public float m_fogStartDistance = 15f;
        public float m_fogEndDistance = 800f;
        [Header("HDRP Fog Settings")]
        public GaiaConstants.HDFogType m_hDFogType = GaiaConstants.HDFogType.Volumetric;
        public GaiaConstants.HDFogType2019_3 m_hDFogType2019_3 = GaiaConstants.HDFogType2019_3.Volumetric;
        [Space(10)]
        //Exponential
        [Range(0f, 1f)]
        public float m_hDExponentialFogDensity = 1f;
        public float m_hDExponentialFogDistance = 200f;
        public float m_hDExponentialFogBaseHeight = 0f;
        [Range(0f, 1f)]
        public float m_hDExponentialFogHeightAttenuation = 0.2f;
        public float m_hDExponentialFogMaxDistance = 5000f;
        [Space(10)]
        //Linear
        [Range(0f, 1f)]
        public float m_hDLinearFogDensity = 1f;
        public float m_hDLinearFogStart = 5f;
        public float m_hDLinearFogEnd = 1200f;
        public float m_hDLinearFogHeightStart = 100f;
        public float m_hDLinearFogHeightEnd = 800f;
        public float m_hDLinearFogMaxDistance = 5000f;
        [Space(10)]
        //Volumetric
        public Color m_hDVolumetricFogScatterColor = Color.white;
        public float m_hDVolumetricFogDistance = 1000f;
        public float m_hDVolumetricFogBaseHeight = 100f;
        public float m_hDVolumetricFogMeanHeight = 200f;
        [Range(0f, 1f)]
        public float m_hDVolumetricFogAnisotropy = 0.75f;
        [Range(0f, 1f)]
        public float m_hDVolumetricFogProbeDimmer = 0.8f;
        public float m_hDVolumetricFogMaxDistance = 5000f;
        public float m_hDVolumetricFogDepthExtent = 50f;
        [Range(0f, 1f)]
        public float m_hDVolumetricFogSliceDistribution = 0f;
        [Header("Lightmapping Settings")]
#if UNITY_EDITOR
#if UNITY_2020_1_OR_NEWER
        public LightingSettings.Lightmapper m_lightmappingMode = LightingSettings.Lightmapper.ProgressiveGPU;
#else
        public LightmapEditorSettings.Lightmapper m_lightmappingMode = LightmapEditorSettings.Lightmapper.ProgressiveGPU;
#endif
#endif
        //Main Global
        public bool m_lightingHasBeenSaved = false;
        [HideInInspector]
        public GaiaLightingProfile m_lightingProfile;
        [HideInInspector]
        public GaiaWaterProfile m_waterProfile;
        public bool m_waterHasBeenSaved = false;
        private GaiaSettings m_gaiaSettings;
        [HideInInspector]
        public Material m_masterSkyboxMaterial;
        [HideInInspector]
        public bool m_parentObjects = true;
        [HideInInspector]
        public bool m_hideProcessVolume = true;
        [HideInInspector]
        public bool m_enablePostProcessing = true;
        [HideInInspector]
        public bool m_enableAmbientAudio = true;
        [HideInInspector]
        public bool m_enableFog = true;
        [HideInInspector]
        public GaiaConstants.GaiaProAntiAliasingMode m_antiAliasingMode = GaiaConstants.GaiaProAntiAliasingMode.TAA;
        [HideInInspector]
        public float m_AAJitterSpread = 0.55f;
        [HideInInspector]
        public float m_AAStationaryBlending = 0.95f;
        [HideInInspector]
        public float m_AAMotionBlending = 0.9f;
        [HideInInspector]
        public float m_AASharpness = 0.25f;
        [HideInInspector]
        public float m_antiAliasingTAAStrength = 0.7f;
        [HideInInspector]
        public bool m_cameraDithering = true;
        [HideInInspector]
        public float m_cameraAperture = 16f;
        [HideInInspector]
        public bool m_usePhysicalCamera = false;
        [HideInInspector]
        public Vector2 m_cameraSensorSize = new Vector2(70.41f, 52.63f);
        [HideInInspector]
        public bool m_globalReflectionProbe = true;

        #endregion

        #region Variables Time Of Day

        public GaiaTimeOfDay GaiaTimeOfDayValue
        {
            get { return m_gaiaTimeOfDay; }
            set
            {
                m_gaiaTimeOfDay = value;
                UpdateGaiaTimeOfDay(false);
            }
        }
        [SerializeField]
        private GaiaTimeOfDay m_gaiaTimeOfDay = new GaiaTimeOfDay();

        #endregion

        #region Variables Weather

        public GaiaWeather GaiaWeather
        {
            get { return m_gaiaWeather; }
            set
            {
                m_gaiaWeather = value;
                UpdateGaiaWeather();
            }
        }
        [SerializeField]
        private GaiaWeather m_gaiaWeather = new GaiaWeather();

        #endregion

        #region Private Stored Values

        [SerializeField]
        private Light m_sunLight;

        [SerializeField]
        private Light m_moonLight;

        [SerializeField]
        public bool WeatherPresent = false;
#if GAIA_PRO_PRESENT
        [SerializeField]
        private ProceduralWorldsGlobalWeather WeatherSystem;
#endif

#if HDPipeline
        [SerializeField]
        private HDAdditionalLightData SunHDLightData;

        [SerializeField]
        private HDAdditionalLightData MoonHDLightData;
#endif

        #endregion

        #region Public Stored Values

        public const string m_shaderLightDirection = "_PW_MainLightDir";
        public const string m_shaderLightColor = "_PW_MainLightColor";
        public const string m_shaderSpecLightColor = "_PW_MainLightSpecular";
        public const string m_shaderWind = "_WaveDirection";
        public const string m_shaderReflectionTexture = "_ReflectionTex";


        #endregion

        [SerializeField]
        private bool m_sunLightExists;
#if GAIA_PRO_PRESENT
        [SerializeField]
        private bool m_moonLightExists;
#endif

        #region Unity Functions

        private void Awake()
        {
            if (m_mainCamera == null)
            {
                m_mainCamera = GaiaUtils.GetCamera();
            }
        }

        private void Start()
        {
            m_instance = this;
            WeatherPresent = CheckWeatherPresent();
            UpdateGaiaTimeOfDay(false);

            if (m_sunLight == null)
            {
                m_sunLight = GaiaUtils.GetMainDirectionalLight();
            }
            if (m_sunLight != null)
            {
                m_sunLightExists = true;
            }

            if (m_moonLight == null)
            {
                GameObject moonObject = GameObject.Find("Moon Light");
                if (moonObject != null)
                {
                    m_moonLight = moonObject.GetComponent<Light>();
                }
            }
#if GAIA_PRO_PRESENT
            if (m_moonLight != null)
            {
                m_moonLightExists = true;
            }
#endif

        }

        public void OnEnable()
        {
            m_instance = this;
            WeatherPresent = CheckWeatherPresent();

            if (!Application.isPlaying)
            {
                if (m_lightingProfile == null)
                {
                    return;
                }

                if (m_waterProfile == null)
                {
                    return;
                }

                if (m_gaiaSettings == null)
                {
                    m_gaiaSettings = GaiaUtils.GetGaiaSettings();
                    if (m_gaiaSettings == null)
                    {
                        return;
                    }
                }
            }

            if (m_mainCamera == null)
            {
                m_mainCamera = GaiaUtils.GetCamera();
            }

            if (m_sunLight == null)
            {
                m_sunLight = GaiaUtils.GetMainDirectionalLight();
            }

            if (m_moonLight == null)
            {
                GameObject moonObject = GameObject.Find("Moon Light");
                if (moonObject != null)
                {
                    m_moonLight = moonObject.GetComponent<Light>();
                }
            }

            UpdateGaiaTimeOfDay(false);
            CheckPostProcessingFog(true);

        }

        private void Update()
        {
#if GAIA_PRO_PRESENT
            if (!Application.isPlaying)
            {
                WeatherPresent = CheckWeatherPresent();
                WeatherSystem = ProceduralWorldsGlobalWeather.Instance;
            }

            if (WeatherPresent)
            {
                if (WeatherSystem.CheckIsNight())
                {
                    Shader.SetGlobalVector(GaiaShaderID.m_globalLightDirection, -WeatherSystem.m_moonLight.transform.forward);
                    Shader.SetGlobalColor(GaiaShaderID.m_globalLightColor, new Vector4(WeatherSystem.m_moonLight.color.r * WeatherSystem.m_moonLight.intensity, WeatherSystem.m_moonLight.color.g * WeatherSystem.m_moonLight.intensity, WeatherSystem.m_moonLight.color.b * WeatherSystem.m_moonLight.intensity, WeatherSystem.m_moonLight.color.a * WeatherSystem.m_moonLight.intensity));
                }
                else
                {
                    Shader.SetGlobalVector(GaiaShaderID.m_globalLightDirection, -WeatherSystem.m_sunLight.transform.forward);
                    Shader.SetGlobalColor(GaiaShaderID.m_globalLightColor, new Vector4(WeatherSystem.m_sunLight.color.r * WeatherSystem.m_sunLight.intensity, WeatherSystem.m_sunLight.color.g * WeatherSystem.m_sunLight.intensity, WeatherSystem.m_sunLight.color.b * WeatherSystem.m_sunLight.intensity, WeatherSystem.m_sunLight.color.a * WeatherSystem.m_sunLight.intensity));
                }
            }
            else
            {
                if (m_sunLightExists)
                {
                    if (m_sunLight != null)
                    {
                        Shader.SetGlobalVector(GaiaShaderID.m_globalLightDirection, -m_sunLight.transform.forward);
                        Shader.SetGlobalColor(GaiaShaderID.m_globalLightColor, m_sunLight.color * m_sunLight.intensity);
                    }
                }
            }
#else
            if (m_sunLightExists)
            {
                Shader.SetGlobalVector(GaiaShaderID.m_globalLightDirection, -m_sunLight.transform.forward);
                Shader.SetGlobalVector(GaiaShaderID.m_globalLightColor, m_sunLight.color * m_sunLight.intensity);
            }
#endif

            if (WeatherPresent)
            {
                if (Application.isPlaying)
                {
                    if (GaiaTimeOfDayValue.m_todEnabled)
                    {
                        GaiaTimeOfDayValue.m_todMinutes += Time.deltaTime * GaiaTimeOfDayValue.m_todDayTimeScale;
                    }
                }
                else
                {
#if GAIA_PRO_PRESENT
                    if (WeatherSystem.RunInEditor)
                    {
                        GaiaTimeOfDayValue.m_todMinutes += Time.deltaTime * GaiaTimeOfDayValue.m_todDayTimeScale;
                    }
#endif
                }

                if (GaiaTimeOfDayValue.m_todMinutes > 59.1f)
                {
                    GaiaTimeOfDayValue.m_todMinutes = 0f;
                    GaiaTimeOfDayValue.m_todHour++;
                }

                if (GaiaTimeOfDayValue.m_todHour > 23)
                {
                    GaiaTimeOfDayValue.m_todHour = 0;
                }

                UpdateGaiaTimeOfDay(false);
            }
        }

        #endregion

        #region Setting Saving Functions

        /// <summary>
        /// Loads the profile type only
        /// </summary>
        /// <param name="mainProfile"></param>
        public void LoadProfileType(GaiaLightingProfile mainProfile)
        {
            mainProfile.m_selectedLightingProfileValuesIndex = m_lightingProfileIndex;
        }

        /// <summary>
        /// Loads the profile type only
        /// </summary>
        /// <param name="mainProfile"></param>
        public void LoadProfileType(GaiaWaterProfile mainProfile)
        {
            mainProfile.m_selectedWaterProfileValuesIndex = m_waterProfileIndex;
        }

        #endregion

        #region Gaia Time Of Day Functions

        public void UpdateGaiaTimeOfDay(bool revertDefault)
        {
#if GAIA_PRO_PRESENT
            if (WeatherPresent)
            {
                if (WeatherSystem == null)
                {
                    WeatherSystem = ProceduralWorldsGlobalWeather.Instance;
                    return;
                }
                bool applicationUpdate = !WeatherSystem.IsRaining;
                if (WeatherSystem.IsSnowing)
                {
                    applicationUpdate = false;
                }

                if (SceneProfile.m_gaiaTimeOfDay.m_todHour != m_gaiaTimeOfDay.m_todHour || SceneProfile.m_gaiaTimeOfDay.m_todMinutes != m_gaiaTimeOfDay.m_todMinutes)
                {
                    if (Application.isPlaying && applicationUpdate)
                    {
                        PW_VFX_Atmosphere.Instance.UpdateSystem();
                    }
                }

                UpdateNightMode();
            }

            m_gaiaTimeOfDay = SceneProfile.m_gaiaTimeOfDay;
#endif
        }

        /// <summary>
        /// Sets the starting time of day mode
        /// </summary>
        /// <param name="mode"></param>
        public void UpdateTimeOfDayMode(GaiaConstants.TimeOfDayStartingMode mode, bool revertDefault)
        {
            switch (mode)
            {
                case GaiaConstants.TimeOfDayStartingMode.Morning:
                    SceneProfile.m_gaiaTimeOfDay.m_todHour = 6;
                    m_gaiaTimeOfDay.m_todHour = 6;
                    SceneProfile.m_gaiaTimeOfDay.m_todMinutes = 30f;
                    m_gaiaTimeOfDay.m_todMinutes = 30;
                    break;
                case GaiaConstants.TimeOfDayStartingMode.Day:
                    SceneProfile.m_gaiaTimeOfDay.m_todHour = 15;
                    m_gaiaTimeOfDay.m_todHour = 15;
                    SceneProfile.m_gaiaTimeOfDay.m_todMinutes = 0f;
                    m_gaiaTimeOfDay.m_todMinutes = 0;
                    break;
                case GaiaConstants.TimeOfDayStartingMode.Evening:
                    SceneProfile.m_gaiaTimeOfDay.m_todHour = 17;
                    m_gaiaTimeOfDay.m_todHour = 17;
                    SceneProfile.m_gaiaTimeOfDay.m_todMinutes = 30f;
                    m_gaiaTimeOfDay.m_todMinutes = 30;
                    break;
                case GaiaConstants.TimeOfDayStartingMode.Night:
                    SceneProfile.m_gaiaTimeOfDay.m_todHour = 1;
                    m_gaiaTimeOfDay.m_todHour = 1;
                    SceneProfile.m_gaiaTimeOfDay.m_todMinutes = 0f;
                    m_gaiaTimeOfDay.m_todMinutes = 0;
                    break;
            }

            UpdateGaiaTimeOfDay(revertDefault);
        }

        /// <summary>
        /// Update the night mode stuff
        /// </summary>
        public void UpdateNightMode()
        {
#if GAIA_PRO_PRESENT
            if (WeatherPresent)
            {
                if (m_sunLight == null)
                {
                    m_sunLight = GaiaUtils.GetMainDirectionalLight();
                    if (m_sunLight != null)
                    {
                        m_sunLightExists = true;
                    }
                }
                else
                {
                    m_sunLightExists = true;
                }

                if (m_moonLight == null)
                {
                    GameObject moonObject = GameObject.Find("Moon Light");
                    if (moonObject != null)
                    {
                        m_moonLight = moonObject.GetComponent<Light>();
                    }

                    if (m_moonLight != null)
                    {
                        m_moonLightExists = true;
                    }
                }
                else
                {
                    m_moonLightExists = true;
                }

                if (WeatherSystem.CheckIsNight())
                {
                    if (m_moonLightExists)
                    {
                        RenderSettings.sun = m_moonLight;
                    }

                    if (m_sunLightExists)
                    {
#if HDPipeline
                        if (SunHDLightData == null)
                        {
                            SunHDLightData = GaiaHDRPRuntimeUtils.GetHDLightData(m_sunLight);
                        }

                        SunHDLightData.intensity = 0;
                        SunHDLightData.lightUnit = LightUnit.Lux;
#endif
                        m_sunLight.intensity = 0f;
                    }
                }
                else
                {
                    if (m_moonLightExists)
                    {
#if HDPipeline
                        if (MoonHDLightData == null)
                        {
                            MoonHDLightData = GaiaHDRPRuntimeUtils.GetHDLightData(m_moonLight);
                        }

                        MoonHDLightData.intensity = 0;
                        MoonHDLightData.lightUnit = LightUnit.Lux;
#endif
                        m_moonLight.intensity = 0f;
                    }

                    if (m_sunLightExists)
                    {
                        RenderSettings.sun = m_sunLight;
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Checks if the weather system is present in the scene
        /// </summary>
        /// <returns></returns>
        public static bool CheckWeatherPresent()
        {
#if GAIA_PRO_PRESENT
            Instance.WeatherSystem = ProceduralWorldsGlobalWeather.Instance;
            return ProceduralWorldsGlobalWeather.Instance != null;
#else
            return false;
#endif
        }

        /// <summary>
        /// Checks if the weather system is present in the scene
        /// </summary>
        /// <returns></returns>
        public static void CheckWeatherPresent(bool setStatus)
        {
#if GAIA_PRO_PRESENT
            if (Instance != null)
            {
                if (setStatus)
                {
                    Instance.WeatherSystem = ProceduralWorldsGlobalWeather.Instance;
                    Instance.WeatherPresent = ProceduralWorldsGlobalWeather.Instance != null;
                }
            }
#endif
        }

        #endregion

        #region Gaia Weather Functions

        public void UpdateGaiaWeather()
        {
#if GAIA_PRO_PRESENT
            if (WeatherPresent)
            {
                WeatherSystem.Season = SceneProfile.m_gaiaWeather.m_season;
                WeatherSystem.WindDirection = SceneProfile.m_gaiaWeather.m_windDirection;
            }
#endif
        }

        #endregion

        #region Public Static Functions

        /// <summary>
        /// Checks to see if the fog state needs to be enabled
        /// </summary>
        /// <param name="enabled"></param>
        public static void CheckPostProcessingFog(bool enabled)
        {
            #if UNITY_EDITOR

            GameObject selection = Selection.activeGameObject;
            if (selection != null)
            {
                if (!selection.GetComponent<BiomeController>() && !selection.GetComponent<Spawner>() && !selection.GetComponent<Stamper>() && !selection.GetComponent<GaiaSessionManager>())
                {
                    #if UNITY_POST_PROCESSING_STACK_V2

                    PostProcessLayer layer = GameObject.FindObjectOfType<PostProcessLayer>();
                    if (layer != null)
                    {
                        layer.fog.enabled = enabled;
                    }

                    #endif
                }
            }

            #endif
        }
        /// <summary>
        /// This function is used to return the current time of day value from 0-1.
        /// This is used for evaluation animation cruves and color gradients in the weather or any other systems required.
        /// </summary>
        /// <param name="gaiaGlobal"></param>
        /// <returns></returns>
        public static float GetTimeOfDayMainValue()
        {
            if (Instance == null)
            {
                return 0f;
            }
            float value = 0;
            value = ((Instance.GaiaTimeOfDayValue.m_todHour * 60f) + Instance.GaiaTimeOfDayValue.m_todMinutes) / 1440f;
            return value;
        }
        /// <summary>
        /// Gets the current Hour
        /// </summary>
        /// <returns></returns>
        public static int GetTimeOfDayHour()
        {
            if (Instance == null)
            {
                return 0;
            }
            int value = 0;
            value = Instance.GaiaTimeOfDayValue.m_todHour;
            return value;
        }
        /// <summary>
        /// Gets the current Minute
        /// </summary>
        /// <returns></returns>
        public static float GetTimeOfDayMinute()
        {
            if (Instance == null)
            {
                return 0f;
            }
            float value = 0;
            value = Instance.GaiaTimeOfDayValue.m_todMinutes;
            return value;
        }

        //Network Get
        public static void GaiaGlobalNetworkSyncGetAll(out int timeHour, out float timeMinute, out bool isRaining, out bool isSnowing, out bool isTODEnabled, out float timeScale)
        {
            timeHour = 15;
            timeMinute = 0f;
            isRaining = false;
            isSnowing = false;
            isTODEnabled = false;
            timeScale = 0;

            if (Instance != null)
            {
                //Set Time
                timeHour = Instance.GaiaTimeOfDayValue.m_todHour;
                timeMinute = Instance.GaiaTimeOfDayValue.m_todMinutes;
                isTODEnabled = Instance.GaiaTimeOfDayValue.m_todEnabled;
                timeScale = Instance.GaiaTimeOfDayValue.m_todDayTimeScale;
            }

#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                //Set Weather
                isRaining = ProceduralWorldsGlobalWeather.Instance.IsRaining;
                isSnowing = ProceduralWorldsGlobalWeather.Instance.IsSnowing;
            }
#endif
        }
        public static void GaiaGlobalNetworkSyncGetTimeAndWeather(out int timeHour, out float timeMinute, out bool isRaining, out bool isSnowing)
        {
            timeHour = 15;
            timeMinute = 0f;
            isRaining = false;
            isSnowing = false;

            if (Instance != null)
            {
                //Set Time
                timeHour = Instance.GaiaTimeOfDayValue.m_todHour;
                timeMinute = Instance.GaiaTimeOfDayValue.m_todMinutes;
            }

#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                //Set Weather
                isRaining = ProceduralWorldsGlobalWeather.Instance.IsRaining;
                isSnowing = ProceduralWorldsGlobalWeather.Instance.IsSnowing;
            }
#endif
        }
        public static void GaiaGlobalNetworkSyncGetTime(out int timeHour, out float timeMinute)
        {
            timeHour = 15;
            timeMinute = 0f;

            if (Instance != null)
            {
                //Set Time
                timeHour = Instance.GaiaTimeOfDayValue.m_todHour;
                timeMinute = Instance.GaiaTimeOfDayValue.m_todMinutes;
            }
        }
        public static void GaiaGlobalNetworkSyncGetWeather(out bool isRaining, out bool isSnowing)
        {
            isRaining = false;
            isSnowing = false;

#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                //Set Weather
                isRaining = ProceduralWorldsGlobalWeather.Instance.IsRaining;
                isSnowing = ProceduralWorldsGlobalWeather.Instance.IsSnowing;
            }
#endif
        }
        public static void GaiaGlobalNetworkSyncGetTimeStatus(out bool isTODEnabled, out float timeScale)
        {
            isTODEnabled = false;
            timeScale = 0;

#if GAIA_PRO_PRESENT
            if (Instance != null)
            {
                isTODEnabled = Instance.GaiaTimeOfDayValue.m_todEnabled;
                timeScale = Instance.GaiaTimeOfDayValue.m_todDayTimeScale;
            }
#endif
        }
        //Network Set
        public static void GaiaGlobalNetworkSyncSetAll(int timeHour, float timeMinute, bool isRaining, bool isSnowing, bool isTODEnabled, float timeScale)
        {
            if (Instance != null)
            {
                //Set Time
                Instance.GaiaTimeOfDayValue.m_todHour = timeHour;
                Instance.GaiaTimeOfDayValue.m_todMinutes = timeMinute;
                Instance.GaiaTimeOfDayValue.m_todEnabled = isTODEnabled;
                Instance.GaiaTimeOfDayValue.m_todDayTimeScale = timeScale;
                Instance.UpdateGaiaTimeOfDay(false);
            }

#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                //Set Weather
                if (!ProceduralWorldsGlobalWeather.Instance.IsRaining && isRaining)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsRaining = isRaining;
                    ProceduralWorldsGlobalWeather.Instance.PlayRain();
                }
                else if (ProceduralWorldsGlobalWeather.Instance.IsRaining && !isRaining)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsRaining = isRaining;
                    ProceduralWorldsGlobalWeather.Instance.StopRain();
                }
                if (!ProceduralWorldsGlobalWeather.Instance.IsSnowing && isSnowing)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsSnowing = isSnowing;
                    ProceduralWorldsGlobalWeather.Instance.PlaySnow();
                }
                else if (ProceduralWorldsGlobalWeather.Instance.IsSnowing && !isSnowing)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsSnowing = isSnowing;
                    ProceduralWorldsGlobalWeather.Instance.StopSnow();
                }
                ProceduralWorldsGlobalWeather.Instance.UpdateAllSystems(false);
                ProceduralWorldsGlobalWeather.Instance.ForceUpdateSkyShaders();
            }
#endif
        }
        public static void GaiaGlobalNetworkSyncSetTimeAndWeather(int timeHour, float timeMinute, bool isRaining, bool isSnowing)
        {
            if (Instance != null)
            {
                //Set Time
                Instance.GaiaTimeOfDayValue.m_todHour = timeHour;
                Instance.GaiaTimeOfDayValue.m_todMinutes = timeMinute;
                Instance.UpdateGaiaTimeOfDay(false);
            }

#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                //Set Weather
                if (!ProceduralWorldsGlobalWeather.Instance.IsRaining && isRaining)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsRaining = isRaining;
                    ProceduralWorldsGlobalWeather.Instance.PlayRain();
                }
                else if (ProceduralWorldsGlobalWeather.Instance.IsRaining && !isRaining)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsRaining = isRaining;
                    ProceduralWorldsGlobalWeather.Instance.StopRain();
                }
                if (!ProceduralWorldsGlobalWeather.Instance.IsSnowing && isSnowing)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsSnowing = isSnowing;
                    ProceduralWorldsGlobalWeather.Instance.PlaySnow();
                }
                else if (ProceduralWorldsGlobalWeather.Instance.IsSnowing && !isSnowing)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsSnowing = isSnowing;
                    ProceduralWorldsGlobalWeather.Instance.StopSnow();
                }
                ProceduralWorldsGlobalWeather.Instance.UpdateAllSystems(false);
                ProceduralWorldsGlobalWeather.Instance.ForceUpdateSkyShaders();
            }
#endif
        }
        public static void GaiaGlobalNetworkSyncSetTime(int timeHour, float timeMinute)
        {
            if (Instance != null)
            {
                //Set Time
                Instance.GaiaTimeOfDayValue.m_todHour = timeHour;
                Instance.GaiaTimeOfDayValue.m_todMinutes = timeMinute;
                Instance.UpdateGaiaTimeOfDay(false);
            }
        }
        public static void GaiaGlobalNetworkSyncSetWeather(bool isRaining, bool isSnowing)
        {     
#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                //Set Weather
                if (!ProceduralWorldsGlobalWeather.Instance.IsRaining && isRaining)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsRaining = isRaining;
                    ProceduralWorldsGlobalWeather.Instance.PlayRain();
                }
                else if (ProceduralWorldsGlobalWeather.Instance.IsRaining && !isRaining)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsRaining = isRaining;
                    ProceduralWorldsGlobalWeather.Instance.StopRain();
                }
                if (!ProceduralWorldsGlobalWeather.Instance.IsSnowing && isSnowing)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsSnowing = isSnowing;
                    ProceduralWorldsGlobalWeather.Instance.PlaySnow();
                }
                else if (ProceduralWorldsGlobalWeather.Instance.IsSnowing && !isSnowing)
                {
                    ProceduralWorldsGlobalWeather.Instance.IsSnowing = isSnowing;
                    ProceduralWorldsGlobalWeather.Instance.StopSnow();
                }
                ProceduralWorldsGlobalWeather.Instance.UpdateAllSystems(false);
                ProceduralWorldsGlobalWeather.Instance.ForceUpdateSkyShaders();
            }
#endif
        }
        public static void GaiaGlobalNetworkSyncSetTimeStatus(bool isTODEnabled, float timeScale)
        {
#if GAIA_PRO_PRESENT
            if (Instance != null)
            {
                Instance.GaiaTimeOfDayValue.m_todEnabled = isTODEnabled;
                Instance.GaiaTimeOfDayValue.m_todDayTimeScale = timeScale;
            }
#endif
        }

        /// <summary>
        /// Used to set the player transform for weather VFX to follow
        /// </summary>
        /// <param name="player"></param>
        public static void SetNetworkedPlayerTransform(Transform player)
        {
#if GAIA_PRO_PRESENT
            if (player != null)
            {
                if (ProceduralWorldsGlobalWeather.Instance != null)
                {
                    ProceduralWorldsGlobalWeather.Instance.m_player = player;
                }
            }
            else
            {
                Debug.LogError("No player transform that was set is valid. You sure the value isn't null?");
            }
#endif
        }
        public static void SetNetworkedPlayerCharacterController(GameObject player)
        {
#if GAIA_PRO_PRESENT
            if (GaiaAudioManager.Instance != null)
            {
                GaiaAudioManager.Instance.m_player = player;
            }
#endif
        }
        /// <summary>
        /// Sets up the player and camera to add the required components to allow gaia runtime systems to run correctly.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="camera"></param>
        public static void SetupNetworkPlayerAndCamera(GameObject player, Camera camera)
        {
            if (player != null)
            {
                FinalizePlayerObjectRuntime(player);
            }
            else
            {
                Debug.LogError("The player gameobject was null please make sure you are passing in the player object after it has been spawned and not before.");
            }

            if (camera != null)
            {
                FinalizeCameraObjectRuntime(camera);
            }
            else
            {
                Debug.LogError("The camera object component was null please make sure you are passing in the camera object component after it has been spawned and not before.");
            }
        }
        public static void FinalizePlayerObjectRuntime(GameObject playerObj)
        {
            GaiaSessionManager session = GaiaSessionManager.GetSessionManager();
            if (session != null)
            {
                if (session.m_session != null)
                {
                    if (playerObj.transform.position.y < session.m_session.m_seaLevel)
                    {
                        playerObj.transform.position = new Vector3(playerObj.transform.position.x, session.m_session.m_seaLevel + 5f, playerObj.transform.position.z);
                    }
                }
            }

#if GAIA_PRO_PRESENT
            //Add the simple terrain culling script, useful in any case
            if (GaiaUtils.CheckIfSceneProfileExists())
            {
                GaiaGlobal.Instance.SceneProfile.m_terrainCullingEnabled = true;
            }
#endif

            bool dynamicLoadedTerrains = GaiaUtils.HasDynamicLoadedTerrains();
            if (dynamicLoadedTerrains)
            {
#if GAIA_PRO_PRESENT
                Terrain terrain = TerrainHelper.GetActiveTerrain();
                TerrainLoader loader = playerObj.GetComponent<TerrainLoader>();
                if (loader == null)
                {
                    loader = playerObj.AddComponent<TerrainLoader>();
                }
                loader.LoadMode = LoadMode.RuntimeAlways;
                float size = terrain.terrainData.size.x * 1.25f * 2f;
                loader.m_loadingBoundsRegular = new BoundsDouble(playerObj.transform.position, new Vector3(size, size, size));
                loader.m_loadingBoundsImpostor = new BoundsDouble(playerObj.transform.position, new Vector3(size * 3f, size * 3f, size * 3f));
#endif
            }
        }
        public static void FinalizePlayerObjectEditor(GameObject playerObj, GaiaSettings gaiaSettings)
        {
            if (playerObj != null)
            {
                playerObj.transform.SetParent(GaiaUtils.GetPlayerObject().transform);
                #if UNITY_EDITOR
                //Adjust the scene view to see the camera
                if (SceneView.lastActiveSceneView != null)
                {
                    if (gaiaSettings.m_focusPlayerOnSetup)
                    {
                        SceneView.lastActiveSceneView.LookAtDirect(playerObj.transform.position, playerObj.transform.rotation);
                    }
                }
                #endif
            }

            GaiaSessionManager session = GaiaSessionManager.GetSessionManager();
            if (session != null)
            {
                if (session.m_session != null)
                {
                    if (playerObj.transform.position.y < session.m_session.m_seaLevel)
                    {
                        playerObj.transform.position = new Vector3(playerObj.transform.position.x, session.m_session.m_seaLevel + 5f, playerObj.transform.position.z);
                    }
                }
            }

#if GAIA_PRO_PRESENT
            //Add the simple terrain culling script, useful in any case
            if (GaiaUtils.CheckIfSceneProfileExists())
            {
                GaiaGlobal.Instance.SceneProfile.m_terrainCullingEnabled = true;
            }
#endif

            bool dynamicLoadedTerrains = GaiaUtils.HasDynamicLoadedTerrains();
            if (dynamicLoadedTerrains)
            {
#if GAIA_PRO_PRESENT

                TerrainLoader loader = playerObj.GetComponent<TerrainLoader>();
                if (loader == null)
                {
                    loader = playerObj.AddComponent<TerrainLoader>();
                }
                loader.LoadMode = LoadMode.RuntimeAlways;
                float tileSize = 512;
                if (TerrainLoaderManager.Instance.TerrainSceneStorage.m_terrainTilesSize > 0)
                {
                    tileSize = TerrainLoaderManager.Instance.TerrainSceneStorage.m_terrainTilesSize;
                }
                float size = tileSize * 1.25f * 2f;
                loader.m_loadingBoundsRegular = new BoundsDouble(playerObj.transform.position, new Vector3(size, size, size));
                loader.m_loadingBoundsImpostor = new BoundsDouble(playerObj.transform.position, new Vector3(size * 3f, size * 3f, size * 3f));
                loader.m_loadingBoundsCollider = new BoundsDouble(playerObj.transform.position, new Vector3(size, size, size)); 
#endif
            }
        }
        public static void FinalizeCameraObjectRuntime(Camera cameraObject)
        {
            if (cameraObject == null)
            {
                return;
            }

            CharacterController controller = GameObject.FindObjectOfType<CharacterController>();
            GameObject xr = GameObject.Find(GaiaConstants.playerXRName);
#if GAIA_PRO_PRESENT
            if (PW_VFX_Clouds.Instance != null)
            {
                PW_VFX_Clouds.Instance.GameCam = cameraObject;
            }

            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                ProceduralWorldsGlobalWeather.Instance.m_player = cameraObject.transform;
            }

            if (GaiaAudioManager.Instance != null)
            {
                if (controller == null)
                {
                    if (xr != null)
                    {
                        GaiaAudioManager.Instance.m_player = xr;
                    }
                }
                else
                {
                    GaiaAudioManager.Instance.m_player = controller.gameObject;
                }
            }
#endif
            if (PWS_WaterSystem.Instance != null)
            {
                PWS_WaterSystem.Instance.m_RenderCamera = cameraObject;
                PWS_WaterSystem.Instance.m_gameCamera = cameraObject;
                if (controller == null)
                {
                    if (xr != null)
                    {
                        PWS_WaterSystem.Instance.m_player = xr.transform;
                    }
                }
                else
                {
                    PWS_WaterSystem.Instance.m_player = controller.transform;
                }
            }

            if (GaiaUnderwaterEffects.Instance != null)
            {
                GaiaUnderwaterEffects.Instance.m_playerCamera = cameraObject.transform;
            }

            if (Instance != null)
            {
                Instance.m_mainCamera = cameraObject;
            }

            ScreenShotter screenShotter = GameObject.FindObjectOfType<ScreenShotter>();
            if (screenShotter != null)
            {
                screenShotter.m_mainCamera = cameraObject;
            }

            FollowPlayerSystem[] followPlayerSystems = GameObject.FindObjectsOfType<FollowPlayerSystem>();
            foreach (var system in followPlayerSystems)
            {
                system.m_player = cameraObject.transform;
            }
        }

        #endregion
    }
}