using UnityEngine;

namespace Gaia
{
    [RequireComponent(typeof(WindZone))]
    public class WindManager : MonoBehaviour
    {
        #region Variables

        #region Public Variables

        public bool m_useWindAudio = true;
        public float windGlobalMaxDist = 1000f;
        public Vector4 windGlobals;
        public Vector4 windGlobalsB;
        public AudioClip m_windAudioClip;
        public float m_windTransitionTime = 5f;
        
        #endregion
        #region Private Variables

        [SerializeField]
        private WindZone m_windZone;
        private AudioSource m_windAudioSource;
        private int m_shaderPropertyIDWindGlobals;
        private int m_shaderPropertyIDWindGlobalsB;
        private Vector3 m_windDirection;
        private Vector4 m_targetWindGlobals;
        private float m_windTime = 0;

        private bool m_canProcessWind = false;
        private float m_currentWindSpeed = 0f;
        private float m_currentWindVolume = 0f;

        #endregion

        #endregion
        #region Unity Function

        private void Start()
        {
            Setup();
        }
        private void Update()
        {
            UpdateWind();

            if (m_useWindAudio)
            {
                CheckWindVolume();
                if (m_canProcessWind)
                {
                    ProcessWindAudio();
                }
            }
        }

        #endregion
        #region Public Functions

        /// <summary>
        /// Applies the settings isntantly
        /// </summary>
        public void InstantWindApply()
        {
            Setup();
            m_windDirection = m_windZone.transform.forward;
            m_targetWindGlobals.x = m_windDirection.x;
            m_targetWindGlobals.y = -Mathf.Max(m_windDirection.y, Vector2.Distance(new Vector2(m_windDirection.x, m_windDirection.z), Vector2.zero) * 0.5f); // force some data into y to give branches some bounce
            m_targetWindGlobals.z = m_windDirection.z;
            m_targetWindGlobals.w = Mathf.Clamp(m_windZone.windMain, 0.0f, 1.2f);
            windGlobals = m_targetWindGlobals;
            Shader.SetGlobalVector(m_shaderPropertyIDWindGlobals, windGlobals);

            windGlobalsB.x = windGlobalMaxDist;
            windGlobalsB.y = Mathf.Pow(windGlobals.w * 0.5f + 0.5f, 3.0f) * 0.1f;
            Shader.SetGlobalVector(m_shaderPropertyIDWindGlobalsB, windGlobalsB);
        }
        /// <summary>
        /// Processes and updates the wind
        /// </summary>
        public void UpdateWind()
        {
            m_windDirection = m_windZone.transform.forward;
            m_targetWindGlobals.x = m_windDirection.x;
            m_targetWindGlobals.y = -Mathf.Max(m_windDirection.y, Vector2.Distance(new Vector2(m_windDirection.x, m_windDirection.z), Vector2.zero) * 0.5f); // force some data into y to give branches some bounce
            m_targetWindGlobals.z = m_windDirection.z;
            m_targetWindGlobals.w = Mathf.Clamp(m_windZone.windMain, 0.0f, 1.2f);

            windGlobals = Vector4.Lerp(windGlobals, m_targetWindGlobals, Time.deltaTime * 0.25f);
            Shader.SetGlobalVector(m_shaderPropertyIDWindGlobals, windGlobals);
            m_windTime += Time.deltaTime * Mathf.Pow(windGlobals.w * 0.5f + 0.5f, 3.0f) * 0.1f;
            if (m_windTime > 100.0f)
            {
                m_windTime -= 100.0f;
            }

            windGlobalsB.x = windGlobalMaxDist;
            windGlobalsB.y = m_windTime;

            Shader.SetGlobalVector(m_shaderPropertyIDWindGlobalsB, windGlobalsB);
        }

        #endregion
        #region Private Functions

        /// <summary>
        /// Sets up the wind manager
        /// </summary>
        private void Setup()
        {
            m_shaderPropertyIDWindGlobals = Shader.PropertyToID("_PW_WindGlobals");
            m_shaderPropertyIDWindGlobalsB = Shader.PropertyToID("_PW_WindGlobalsB");
            if (m_windZone == null)
            {
                m_windZone = GetWindZone();
            }

            if (m_windZone == null)
            {
                m_windDirection = m_windZone.transform.forward;
                windGlobals.x = m_windDirection.x;
                windGlobals.y = m_windDirection.y;
                windGlobals.z = m_windDirection.z;
                windGlobals.w = m_windZone.windMain;
                m_currentWindSpeed = m_windZone.windMain;
                m_currentWindVolume = Mathf.Clamp01(m_windZone.windMain);
            }

            if (Application.isPlaying)
            {
                m_windAudioSource = GetWindAudioSource();
                if (m_windAudioSource != null)
                {
                    m_windAudioSource.clip = m_windAudioClip;
                    m_windAudioSource.volume = m_currentWindVolume;
                    if (!m_windAudioSource.isPlaying)
                    {
                        m_windAudioSource.Play();
                    }
                }
            }
        }
        /// <summary>
        /// Checks if the volume needs to be processed
        /// </summary>
        private void CheckWindVolume()
        {
            if (m_windZone.windMain != m_currentWindSpeed)
            {
                if (m_windAudioSource != null)
                {
                    m_currentWindVolume = m_windAudioSource.volume;
                    m_canProcessWind = true;
                }
            }
        }
        /// <summary>
        /// Processes the volume for the wind to be updated
        /// </summary>
        private void ProcessWindAudio()
        {
            if (m_windAudioClip == null || m_windAudioSource == null)
            {
                return;
            }

            float clampedWindSpeed = Mathf.Clamp01(m_windZone.windMain);
            m_windAudioSource.clip = m_windAudioClip;
            if (!m_windAudioSource.isPlaying)
            {
                m_windAudioSource.Play();
            }
            m_windAudioSource.volume = Mathf.Lerp(m_currentWindVolume, clampedWindSpeed, Time.deltaTime / m_windTransitionTime);
            if (m_currentWindVolume >= clampedWindSpeed)
            {
                if (m_windAudioSource.volume <= clampedWindSpeed - 0.05f)
                {
                    m_windAudioSource.volume = clampedWindSpeed;
                    m_canProcessWind = false;
                }
            }
            else
            {
                if (m_windAudioSource.volume >= clampedWindSpeed - 0.05f)
                {
                    m_windAudioSource.volume = clampedWindSpeed;
                    m_canProcessWind = false;
                }
            }
        }
        /// <summary>
        /// Gets the wind audio source from the object
        /// </summary>
        /// <returns></returns>
        private AudioSource GetWindAudioSource()
        {
            if (m_windAudioSource != null)
            {
                return m_windAudioSource;
            }

            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();

            }

            audioSource.loop = true;
            audioSource.playOnAwake = true;
            audioSource.maxDistance = 100000f;

            return audioSource;
        }
        /// <summary>
        /// Gets the wind zone
        /// </summary>
        /// <returns></returns>
        private WindZone GetWindZone()
        {
            WindZone windZone = gameObject.GetComponent<WindZone>();
            if (windZone == null)
            {
                windZone = gameObject.AddComponent<WindZone>();
            }

            return windZone;
        }

        #endregion
    }
}