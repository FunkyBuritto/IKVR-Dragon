using System;
using UnityEngine;

namespace Gaia
{
    public enum ReflectionMaskerChannelSelection { R, G, B, A, RGBA }

    [Serializable]
    public class ReflectionMaskerData
    {
        public Texture2D m_maskingTexture;
        public ReflectionMaskerChannelSelection m_channelSelection = ReflectionMaskerChannelSelection.R;
        public float m_minValue = 0.15f;
        public float m_maxValue = 1f;

        public bool m_enableReflections;
        public bool m_allowMSAA;
        public bool m_enableHDR;
        public bool m_enableHeightFeatures;
        public float m_disableHeightValue;
        public bool m_enableCustomDistance;
        public bool m_enableMultiLayerDistances;
        public float m_reflectionDistance;
        public float[] m_reflectionDistances;
        public GaiaPlanarReflections m_planarReflections;

        public void Process(SceneProfile sceneProfile, bool state)
        {
            Get(sceneProfile);
            SetState(sceneProfile, state);
        }
        public void Get(SceneProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            m_enableReflections = profile.m_enableReflections;
            m_allowMSAA = profile.m_allowMSAA;
            m_enableHDR = profile.m_useHDR;
            m_enableHeightFeatures = profile.m_enableDisabeHeightFeature;
            m_disableHeightValue = profile.m_disableHeight;
            m_enableCustomDistance = profile.m_useCustomRenderDistance;
            m_enableMultiLayerDistances = profile.m_enableLayerDistances;
            m_reflectionDistance = profile.m_customRenderDistance;
            m_reflectionDistances = profile.m_customRenderDistances;
        }
        public void Set(SceneProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            profile.m_enableReflections = m_enableReflections;
            profile.m_allowMSAA = m_allowMSAA;
            profile.m_useHDR = m_enableHDR;
            profile.m_enableDisabeHeightFeature = m_enableHeightFeatures;
            profile.m_disableHeight = m_disableHeightValue;
            profile.m_useCustomRenderDistance = m_enableCustomDistance;
            profile.m_enableLayerDistances = m_enableMultiLayerDistances;
            profile.m_customRenderDistance = m_reflectionDistance;
            profile.m_customRenderDistances = m_reflectionDistances;
        }
        public void SetState(SceneProfile profile, bool state)
        {
            if (profile == null)
            {
                return;
            }

            profile.m_enableReflections = state;
            profile.m_enableDisabeHeightFeature = state;

#if UPPipeline

            if (m_planarReflections != null)
            {
                m_planarReflections.SetReflectionState(state);
            }
            else
            {
                m_planarReflections = GameObject.FindObjectOfType<GaiaPlanarReflections>();
                if (m_planarReflections != null)
                {
                    m_planarReflections.SetReflectionState(state);
                }
            }

#endif
        }
    }

    public class ReflectionMasker : MonoBehaviour
    {
        public Transform Player;
        public ReflectionMaskerData ReflectionMaskerData
        {
            get { return m_reflectionMaskerData; }
            set
            {
                if (m_reflectionMaskerData != value)
                {
                    m_reflectionMaskerData = value;
                    if (Application.isPlaying)
                    {
                        if (GaiaUtils.CheckIfSceneProfileExists())
                        {
                            Execute();
                        }
                    }
                }
            }
        }
        [SerializeField]
        private ReflectionMaskerData m_reflectionMaskerData = new ReflectionMaskerData();

        private bool m_needUpdating = false;
        [SerializeField]
        private Vector3 m_oldPosition;

        private void Start()
        {
            Initilize();
            Execute();
        }
        private void Update()
        {
            if (DoesNeedExecute())
            {
                Execute();
            }
        }

        public void Execute()
        {
            if (Player != null)
            {
                bool checkValue = CheckPoint(ReflectionMaskerData.m_maskingTexture, Player.position, ReflectionMaskerData.m_minValue, ReflectionMaskerData.m_maxValue);
                if (DoesNeedUpdating(checkValue))
                {
                    m_needUpdating = checkValue;
                    if (GaiaUtils.CheckIfSceneProfileExists())
                    {
                        ReflectionMaskerData.Process(GaiaGlobal.Instance.SceneProfile, checkValue);
                    }
                }
            }
        }
        private bool CheckPoint(Texture2D maskTexture, Vector3 position, float min, float max)
        {
            try
            {
                if (maskTexture == null)
                {
                    return false;
                }

                if (!maskTexture.isReadable)
                {
                    Debug.LogError("Texture is not marked as readable. Please set is readable on the texture import settings on texture " + maskTexture.name);
                }

                Terrain terrain = TerrainHelper.GetTerrain(position);
                if (terrain != null)
                {
                    float scalerX = (float)(position.x - terrain.transform.position.x) / (float)terrain.terrainData.size.x;
                    float scalerZ = (float)(position.z - terrain.transform.position.z) / (float)terrain.terrainData.size.z;

                    Color value = Color.white; 
                    value = maskTexture.GetPixel(Mathf.RoundToInt(scalerX * maskTexture.width), Mathf.RoundToInt(scalerZ * maskTexture.height));

                    switch (ReflectionMaskerData.m_channelSelection)
                    {
                        case ReflectionMaskerChannelSelection.R:
                            if (value.r >= min && value.r <= max)
                            {
                                return false;
                            }
                            break;
                        case ReflectionMaskerChannelSelection.G:
                            if (value.g >= min && value.g <= max)
                            {
                                return false;
                            }
                            break;
                        case ReflectionMaskerChannelSelection.B:
                            if (value.b >= min && value.b <= max)
                            {
                                return false;
                            }
                            break;
                        case ReflectionMaskerChannelSelection.A:
                            if (value.a >= min && value.a <= max)
                            {
                                return false;
                            }
                            break;
                        case ReflectionMaskerChannelSelection.RGBA:
                            if (value.r >= min && value.r <= max)
                            {
                                return false;
                            }
                            if (value.g >= min && value.g <= max)
                            {
                                return false;
                            }
                            if (value.b >= min && value.b <= max)
                            {
                                return false;
                            }
                            if (value.a >= min && value.a <= max)
                            {
                                return false;
                            }
                            break;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Issues happened when trying to check the point " + e.Message + " Happened here " + e.StackTrace);
                return true;
            }
        }
        private bool DoesNeedUpdating(bool newValue)
        {
            if (m_needUpdating != newValue)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private bool DoesNeedExecute()
        {
            if (Player != null)
            {
                if (Player.position != m_oldPosition)
                {
                    m_oldPosition = Player.position;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }
        private void Initilize()
        {
            if (GaiaUtils.CheckIfSceneProfileExists())
            {
                m_needUpdating = GaiaGlobal.Instance.SceneProfile.m_enableReflections;
            }

            if (Player == null)
            {
                Player = GetPlayer();
            }
        }
        public Transform GetPlayer()
        {
            Transform transform = null;
            if (GaiaUtils.CheckIfSceneProfileExists())
            {
                if (GaiaGlobal.Instance.m_mainCamera != null)
                {
                    transform = GaiaGlobal.Instance.m_mainCamera.transform;
                }
            }

            if (transform == null)
            {
                transform = GaiaUtils.GetPlayerTransform();
            }

            return transform;
        }
    }
}