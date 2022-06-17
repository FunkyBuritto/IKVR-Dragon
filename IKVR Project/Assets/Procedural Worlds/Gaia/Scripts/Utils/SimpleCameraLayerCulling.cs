using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

namespace Gaia
{
    public class SimpleCameraLayerCulling : MonoBehaviour
    {
        [HideInInspector]
        public GaiaSceneCullingProfile m_profile;

        public bool m_applyToGameCamera;
        public Light m_directionalLight;
        public bool m_applyToSceneCamera;

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        public  void Initialize()
        {
            if (m_profile == null)
            {
                GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
                m_profile = ScriptableObject.CreateInstance<GaiaSceneCullingProfile>();
                m_profile.UpdateCulling(gaiaSettings);
                m_profile.UpdateShadow();
            }

            ApplyToGameCamera();
        }

        public void ApplyToGameCamera()
        {
            if (m_applyToGameCamera)
            {
                Camera cam = GetComponent<Camera>();
                if (cam != null)
                {
                    cam.layerCullDistances = m_profile.m_layerDistances;
                }
                ApplyToDirectionalLight();
            }
            else
            {
                float[] layerCulls = new float[32];
                for (int i = 0; i < layerCulls.Length; i++)
                {
                    layerCulls[i] = 0f;
                }
                Camera cam = GetComponent<Camera>();
                if (cam != null)
                {
                    cam.layerCullDistances = layerCulls;
                }
            }
        }

        public void ApplyToDirectionalLight()
        {
            if (m_directionalLight == null)
            {
                GameObject lightGO = GameObject.Find("Directional Light");
                if (lightGO != null)
                {
                    m_directionalLight = lightGO.GetComponent<Light>();
                }
            }

            if (m_directionalLight != null)
            {
                m_directionalLight.layerShadowCullDistances = m_profile.m_shadowLayerDistances;
            }
        }

        public void ResetDirectionalLight()
        {
            if (!m_applyToGameCamera && !m_applyToSceneCamera)
            {
                if (m_directionalLight == null)
                {
                    GameObject lightGO = GameObject.Find("Directional Light");
                    if (lightGO != null)
                    {
                        m_directionalLight = lightGO.GetComponent<Light>();
                    }

                    if (m_directionalLight != null)
                    {

                        float[] layers = new float[32];
                        for (int i = 0; i < layers.Length; i++)
                        {
                            layers[i] = 0f;
                        }
                        m_directionalLight.layerShadowCullDistances = layers;
                    }
                }
            }
        }
    }
}