using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace Gaia
{
    [ExecuteInEditMode]
    public class GaiaPostProcessBiome : MonoBehaviour
    {
        [HideInInspector]
        public string PostProcessingFileName;
#if UNITY_POST_PROCESSING_STACK_V2
        [Header("Global Settings")]
        public bool m_removeThisAwake = true;
        public PostProcessProfile m_postProcessProfile;
        public PostProcessVolume m_postProcessVolume;
        public float m_blendDistance;
        public float m_priority;
        [Header("Trigger")]
        public BoxCollider m_triggerCollider;
        public Vector3 m_triggerSize;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                if (m_removeThisAwake)
                {
                    Destroy(this);
                }
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                if (m_postProcessVolume != null)
                {
                    m_postProcessVolume.blendDistance = m_blendDistance;
                    m_postProcessVolume.priority = m_priority;
                    if (m_postProcessProfile != null)
                    {
                        if (m_postProcessVolume.sharedProfile != m_postProcessProfile)
                        {
                            m_postProcessVolume.sharedProfile = m_postProcessProfile;
                        }
                    }
                }
                else
                {
                    m_postProcessVolume = GetComponent<PostProcessVolume>();
                }

                if (m_triggerCollider != null)
                {
                    m_triggerCollider.size = m_triggerSize;
                }
                else
                {
                    m_triggerCollider = GetComponent<BoxCollider>();
                }
            }
        }
#endif
    }
}