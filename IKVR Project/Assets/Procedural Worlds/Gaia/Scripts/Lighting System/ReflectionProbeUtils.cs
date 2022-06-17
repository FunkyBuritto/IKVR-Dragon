using UnityEngine;
using UnityEditor;
#if HDPipeline
using UnityEngine.Rendering.HighDefinition;
#endif
using System.Collections.Generic;
using ProceduralWorlds.WaterSystem;
using System;
using UnityEngine.Rendering;

namespace Gaia
{
    public static class ReflectionProbeUtils
    {
        /// <summary>
        /// Creates a single Reflection Probe in the given position
        /// </summary>
        /// <param name="position"></param>
        /// <param name="terrain"></param>
        /// <param name="reflectionProbeData"></param>
        /// <param name="seaLevelActive"></param>
        /// <param name="seaLevel"></param>
        /// <param name="parentTransform"></param>
        /// <param name="isHDRP"></param>
        /// <returns></returns>
        public static ReflectionProbe CreateReflectionProbe(Vector3 position, Vector3 size, Terrain terrain, ReflectionProbeData reflectionProbeData, bool seaLevelActive, float seaLevel, Transform parentTransform, bool isHDRP, bool probeCulling = false)
        {
            GameObject probeObject = new GameObject("Global Generated Reflection Probe");

            float sampledHeight = terrain.SampleHeight(position);

            ReflectionProbe reflectionProbe = probeObject.AddComponent<ReflectionProbe>();
            reflectionProbe.enabled = false;
            reflectionProbe.blendDistance = 0f;
            reflectionProbe.cullingMask = reflectionProbeData.reflectionprobeCullingMask;
            reflectionProbe.farClipPlane = reflectionProbeData.reflectionProbeClipPlaneDistance;
            reflectionProbe.mode = reflectionProbeData.reflectionProbeMode;
            switch (reflectionProbeData.reflectionProbeRefresh)
            {
                case GaiaConstants.ReflectionProbeRefreshModePW.OnAwake:
                    reflectionProbe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                    break;
                case GaiaConstants.ReflectionProbeRefreshModePW.EveryFrame:
                    reflectionProbe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
                    break;
                case GaiaConstants.ReflectionProbeRefreshModePW.ViaScripting:
                    reflectionProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                    break;
                case GaiaConstants.ReflectionProbeRefreshModePW.ProbeManager:
                    reflectionProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                    break;
            }

            if (!seaLevelActive)
            {
                position.y = 500f + seaLevel + 0.2f;
            }
            else
            {
                position.y = sampledHeight + reflectionProbeData.reflectionProbeOffset;
                reflectionProbe.center = new Vector3(0f, 0f - reflectionProbeData.reflectionProbeOffset - sampledHeight, 0f);
            }

            if (position.y < seaLevel)
            {
                position.y = seaLevel + reflectionProbeData.reflectionProbeOffset;
            }
            probeObject.transform.position = position;
            probeObject.transform.SetParent(parentTransform);

            switch (reflectionProbeData.reflectionProbeResolution)
            {
                case GaiaConstants.ReflectionProbeResolution.Resolution16:
                    reflectionProbe.resolution = 16;
                    break;
                case GaiaConstants.ReflectionProbeResolution.Resolution32:
                    reflectionProbe.resolution = 32;
                    break;
                case GaiaConstants.ReflectionProbeResolution.Resolution64:
                    reflectionProbe.resolution = 64;
                    break;
                case GaiaConstants.ReflectionProbeResolution.Resolution128:
                    reflectionProbe.resolution = 128;
                    break;
                case GaiaConstants.ReflectionProbeResolution.Resolution256:
                    reflectionProbe.resolution = 256;
                    break;
                case GaiaConstants.ReflectionProbeResolution.Resolution512:
                    reflectionProbe.resolution = 512;
                    break;
                case GaiaConstants.ReflectionProbeResolution.Resolution1024:
                    reflectionProbe.resolution = 1024;
                    break;
                case GaiaConstants.ReflectionProbeResolution.Resolution2048:
                    reflectionProbe.resolution = 2048;
                    break;
            }

            reflectionProbe.shadowDistance = 80f;
            reflectionProbe.size = size;
            reflectionProbe.timeSlicingMode = reflectionProbeData.reflectionProbeTimeSlicingMode;
            reflectionProbe.hdr = true;
            reflectionProbe.shadowDistance = reflectionProbeData.reflectionProbeShadowDistance;

            //If HDRP
            if (isHDRP)
            {
#if HDPipeline
                HDAdditionalReflectionData reflectionData = probeObject.GetComponent<HDAdditionalReflectionData>();
                if (reflectionData == null)
                {
                    reflectionData = probeObject.AddComponent<HDAdditionalReflectionData>();
                }

                reflectionData.multiplier = 1f;
                reflectionData.realtimeMode = ProbeSettings.RealtimeMode.OnEnable;
#endif
            }

#if GAIA_PRO_PRESENT
            ReflectionProbeCuller culler = reflectionProbe.GetComponent<ReflectionProbeCuller>();
            if (probeCulling)
            {
                if (culler == null)
                {
                    culler = reflectionProbe.gameObject.AddComponent<ReflectionProbeCuller>();
                    culler.m_probe = reflectionProbe;
                    culler.Initialize();
                }
            }
            else
            {
                if (culler != null)
                {
                    GameObject.DestroyImmediate(culler);
                }
            }
#endif

            return reflectionProbe;
        }

    }
}
