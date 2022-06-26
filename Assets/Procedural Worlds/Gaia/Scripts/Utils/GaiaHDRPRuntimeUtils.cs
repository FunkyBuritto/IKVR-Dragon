using UnityEngine;
using Gaia.Pipeline;
using UnityEngine.Rendering;
#if HDPipeline
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Gaia.Pipeline.HDRP
{
    public static class GaiaHDRPRuntimeUtils
    {
        #region Utils

        /// <summary>
        /// Gets or creates HD camera data
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
#if HDPipeline
        public static HDAdditionalCameraData GetHDCameraData(Camera camera)
        {
            if (camera == null)
            {
                return null;
            }

            HDAdditionalCameraData cameraData = camera.gameObject.GetComponent<HDAdditionalCameraData>();
            if (cameraData == null)
            {
                cameraData = camera.gameObject.AddComponent<HDAdditionalCameraData>();
            }

            return cameraData;
        }
        /// <summary>
        /// Gets or creates HD Light data
        /// </summary>
        /// <param name="light"></param>
        /// <returns></returns>
        public static HDAdditionalLightData GetHDLightData(Light light)
        {
            if (light == null)
            {
                return null;
            }

            HDAdditionalLightData lightData = light.GetComponent<HDAdditionalLightData>();
            if (lightData == null)
            {
                lightData = light.gameObject.AddComponent<HDAdditionalLightData>();
            }

            return lightData;
        }
#endif
        /// <summary>
        /// Removes HDRP Post processing
        /// </summary>
        /// <param name="pipelineProfile"></param>
        public static void RemovePostProcesing(UnityPipelineProfile pipelineProfile)
        {
            GameObject postObject = GameObject.Find(pipelineProfile.m_HDPostVolumeObjectName);
            if (postObject != null)
            {
                Object.DestroyImmediate(postObject);
            }
        }
        /// <summary>
        /// Configures reflections to LWRP
        /// </summary>
        public static void ConfigureReflectionProbes()
        {
            ReflectionProbe[] reflectionProbes = Object.FindObjectsOfType<ReflectionProbe>();
            if (reflectionProbes != null)
            {
#if HDPipeline
                foreach (ReflectionProbe probe in reflectionProbes)
                {

                    if (probe.GetComponent<HDAdditionalReflectionData>() == null)
                    {
                        probe.gameObject.AddComponent<HDAdditionalReflectionData>();
                    }
                }
#endif
            }
        }
        /// <summary>
        /// Configures and setup the terrain
        /// </summary>
        /// <param name="profile"></param>
        public static void ConfigureTerrain(UnityPipelineProfile profile)
        {
            Terrain[] terrains = Terrain.activeTerrains;
            if (terrains != null)
            {
                foreach(Terrain terrain in terrains)
                {
                    terrain.materialTemplate = profile.m_highDefinitionTerrainMaterial;
                }
            }
        }

        /// <summary>
        /// Syncs user HDRP profiles settings to the source.
        /// </summary>
        public static void SyncUserHDRPEnvironmentProfile()
        {
            if (GaiaGlobal.Instance.SceneProfile == null)
            {
                return;
            }

            GameObject volumeEnvironment = GameObject.Find(GaiaConstants.HDRPEnvironmentObject);
            if (volumeEnvironment != null)
            {
                #if HDPipeline
                Volume volume = volumeEnvironment.GetComponent<Volume>();
                if (volume != null)
                {
                    VolumeProfile sceneVolumeProfile = volume.sharedProfile;
                    GaiaLightingProfileValues lightingProfileValues = GaiaGlobal.Instance.SceneProfile.m_lightingProfiles[GaiaGlobal.Instance.SceneProfile.m_selectedLightingProfileValuesIndex];
                    if (lightingProfileValues != null)
                    {
                        if (lightingProfileValues.m_userCustomProfile)
                        {
                            VolumeProfile gaialightVolumeProfile = lightingProfileValues.EnvironmentProfileHDRP;
                            if (gaialightVolumeProfile != null)
                            {
                                SyncHDRPVisualEnvironment(sceneVolumeProfile, gaialightVolumeProfile);
                                SyncHDRPGradientSky(sceneVolumeProfile, gaialightVolumeProfile);
                                SyncHDRPHDRISky(sceneVolumeProfile, gaialightVolumeProfile);
                                SyncHDRPPhysicallyBasedSky(sceneVolumeProfile, gaialightVolumeProfile);
                                SyncHDRPFog(sceneVolumeProfile, gaialightVolumeProfile);
                                SyncHDRPShadows(sceneVolumeProfile, gaialightVolumeProfile);
                                SyncHDRPContactShadows(sceneVolumeProfile, gaialightVolumeProfile);
                                SyncHDRPMicroShadows(sceneVolumeProfile, gaialightVolumeProfile);
                                SyncHDRPAmbientLight(sceneVolumeProfile, gaialightVolumeProfile);
                            }

                            #if UNITY_EDITOR

                            UnityEditor.EditorUtility.SetDirty(gaialightVolumeProfile);

                            #endif
                        }
                    }
                }
#endif
            }
        }
#if HDPipeline
        private static void SyncHDRPVisualEnvironment(VolumeProfile sceneProfile, VolumeProfile sourceProfile)
        {
            if (sceneProfile == null || sourceProfile == null)
            {
                return;
            }

            if (sceneProfile.TryGet(out VisualEnvironment visualEnvironmentScene))
            {
                if (sourceProfile.TryGet(out VisualEnvironment visualEnvironmentSource))
                {
                    visualEnvironmentSource.skyAmbientMode.value = visualEnvironmentScene.skyAmbientMode.value;
                    visualEnvironmentSource.skyType.value = visualEnvironmentScene.skyType.value;
                }
            }
        }
        private static void SyncHDRPGradientSky(VolumeProfile sceneProfile, VolumeProfile sourceProfile)
        {
            if (sceneProfile == null || sourceProfile == null)
            {
                return;
            }

            if (sceneProfile.TryGet(out GradientSky gradientSkyScene))
            {
                if (sourceProfile.TryGet(out GradientSky gradientSkySource))
                {
                    gradientSkySource.top.value = gradientSkyScene.top.value;
                    gradientSkySource.middle.value = gradientSkyScene.middle.value;
                    gradientSkySource.bottom.value = gradientSkyScene.bottom.value;
                    gradientSkySource.gradientDiffusion.value = gradientSkyScene.gradientDiffusion.value;
                    gradientSkySource.skyIntensityMode.value = gradientSkyScene.skyIntensityMode.value;
                    gradientSkySource.exposure.value = gradientSkyScene.exposure.value;
                    gradientSkySource.multiplier.value = gradientSkyScene.multiplier.value;
                    gradientSkySource.updateMode.value = gradientSkyScene.updateMode.value;
                }
            }
        }
        private static void SyncHDRPHDRISky(VolumeProfile sceneProfile, VolumeProfile sourceProfile)
        {
            if (sceneProfile == null || sourceProfile == null)
            {
                return;
            }

            if (sceneProfile.TryGet(out HDRISky hdriSkyScene))
            {
                if (sourceProfile.TryGet(out HDRISky hdriSkySource))
                {
                    hdriSkySource.hdriSky.value = hdriSkyScene.hdriSky.value;
                    hdriSkySource.skyIntensityMode.value = hdriSkyScene.skyIntensityMode.value;
                    hdriSkySource.exposure.value = hdriSkyScene.exposure.value;
                    hdriSkySource.multiplier.value = hdriSkyScene.multiplier.value;
                    hdriSkySource.rotation.value = hdriSkyScene.rotation.value;
                    hdriSkySource.updateMode.value = hdriSkyScene.updateMode.value;
                }
            }
        }
        private static void SyncHDRPPhysicallyBasedSky(VolumeProfile sceneProfile, VolumeProfile sourceProfile)
        {
            if (sceneProfile == null || sourceProfile == null)
            {
                return;
            }

            if (sceneProfile.TryGet(out PhysicallyBasedSky physicallyBasedSkyScene))
            {
                if (sourceProfile.TryGet(out PhysicallyBasedSky physicallyBasedSkySource))
                {
#if !UNITY_2020_2_OR_NEWER
                    physicallyBasedSkySource.earthPreset.value = physicallyBasedSkyScene.earthPreset.value;
#endif
                    physicallyBasedSkySource.planetaryRadius.value = physicallyBasedSkyScene.planetaryRadius.value;
                    physicallyBasedSkySource.sphericalMode.value = physicallyBasedSkyScene.sphericalMode.value;
                    physicallyBasedSkySource.planetCenterPosition.value = physicallyBasedSkyScene.planetCenterPosition.value;
                    physicallyBasedSkySource.planetRotation.value = physicallyBasedSkyScene.planetRotation.value;
                    physicallyBasedSkySource.groundColorTexture.value = physicallyBasedSkyScene.groundColorTexture.value;
                    physicallyBasedSkySource.groundTint.value = physicallyBasedSkyScene.groundTint.value;
                    physicallyBasedSkySource.groundEmissionTexture.value = physicallyBasedSkyScene.groundEmissionTexture.value;
                    physicallyBasedSkySource.groundEmissionMultiplier.value = physicallyBasedSkyScene.groundEmissionMultiplier.value;
                    physicallyBasedSkySource.spaceRotation.value = physicallyBasedSkyScene.spaceRotation.value;
                    physicallyBasedSkySource.spaceEmissionTexture.value = physicallyBasedSkyScene.spaceEmissionTexture.value;
                    physicallyBasedSkySource.spaceEmissionMultiplier.value = physicallyBasedSkyScene.spaceEmissionMultiplier.value;
                    physicallyBasedSkySource.airMaximumAltitude.value = physicallyBasedSkyScene.airMaximumAltitude.value;
                    physicallyBasedSkySource.airDensityR.value = physicallyBasedSkyScene.airDensityR.value;
                    physicallyBasedSkySource.airDensityG.value = physicallyBasedSkyScene.airDensityG.value;
                    physicallyBasedSkySource.airDensityB.value = physicallyBasedSkyScene.airDensityB.value;
                    physicallyBasedSkySource.airTint.value = physicallyBasedSkyScene.airTint.value;
                    physicallyBasedSkySource.aerosolMaximumAltitude.value = physicallyBasedSkyScene.aerosolMaximumAltitude.value;
                    physicallyBasedSkySource.aerosolDensity.value = physicallyBasedSkyScene.aerosolDensity.value;
                    physicallyBasedSkySource.aerosolTint.value = physicallyBasedSkyScene.aerosolTint.value;
                    physicallyBasedSkySource.aerosolAnisotropy.value = physicallyBasedSkyScene.aerosolAnisotropy.value;
                    physicallyBasedSkySource.colorSaturation.value = physicallyBasedSkyScene.colorSaturation.value;
                    physicallyBasedSkySource.alphaSaturation.value = physicallyBasedSkyScene.alphaSaturation.value;
                    physicallyBasedSkySource.alphaMultiplier.value = physicallyBasedSkyScene.alphaMultiplier.value;
                    physicallyBasedSkySource.horizonTint.value = physicallyBasedSkyScene.horizonTint.value;
                    physicallyBasedSkySource.horizonZenithShift.value = physicallyBasedSkyScene.horizonZenithShift.value;
                    physicallyBasedSkySource.zenithTint.value = physicallyBasedSkyScene.zenithTint.value;
                    physicallyBasedSkySource.numberOfBounces.value = physicallyBasedSkyScene.numberOfBounces.value;
                    physicallyBasedSkySource.skyIntensityMode.value = physicallyBasedSkyScene.skyIntensityMode.value;
                    physicallyBasedSkySource.exposure.value = physicallyBasedSkyScene.exposure.value;
                    physicallyBasedSkySource.multiplier.value = physicallyBasedSkyScene.multiplier.value;
                    physicallyBasedSkySource.updateMode.value = physicallyBasedSkyScene.updateMode.value;
                    physicallyBasedSkySource.includeSunInBaking.value = physicallyBasedSkyScene.includeSunInBaking.value;

                }
            }
        }
        private static void SyncHDRPFog(VolumeProfile sceneProfile, VolumeProfile sourceProfile)
        {
            if (sceneProfile == null || sourceProfile == null)
            {
                return;
            }

            if (sceneProfile.TryGet(out UnityEngine.Rendering.HighDefinition.Fog fogScene))
            {
                if (sourceProfile.TryGet(out UnityEngine.Rendering.HighDefinition.Fog fogSource))
                {
                    fogSource.enabled.value = fogScene.enabled.value;
                    fogSource.maxFogDistance.value = fogScene.maxFogDistance.value;
                    fogSource.baseHeight.value = fogScene.baseHeight.value;
                    fogSource.maximumHeight.value = fogScene.maximumHeight.value;
                    fogSource.maxFogDistance.value = fogScene.maxFogDistance.value;
                    fogSource.colorMode.value = fogScene.colorMode.value;
                    fogSource.tint.value = fogScene.tint.value;
                    fogSource.mipFogNear.value = fogScene.mipFogNear.value;
                    fogSource.mipFogFar.value = fogScene.mipFogFar.value;
                    fogSource.mipFogMaxMip.value = fogScene.mipFogMaxMip.value;
                    fogSource.enableVolumetricFog.value = fogScene.enableVolumetricFog.value;
                    fogSource.albedo.value = fogScene.albedo.value;
                    fogSource.anisotropy.value = fogScene.anisotropy.value;
                    fogSource.globalLightProbeDimmer.value = fogScene.globalLightProbeDimmer.value;
                    fogSource.depthExtent.value = fogScene.depthExtent.value;
                    fogSource.sliceDistributionUniformity.value = fogScene.sliceDistributionUniformity.value;
                    fogSource.filter.value = fogScene.filter.value;
                }
            }
        }
        private static void SyncHDRPShadows(VolumeProfile sceneProfile, VolumeProfile sourceProfile)
        {
            if (sceneProfile == null || sourceProfile == null)
            {
                return;
            }

            if (sceneProfile.TryGet(out HDShadowSettings hdShadowScene))
            {
                if (sourceProfile.TryGet(out HDShadowSettings hdShadowSource))
                {
                    hdShadowSource.maxShadowDistance.value = hdShadowScene.maxShadowDistance.value;
                    hdShadowSource.directionalTransmissionMultiplier.value = hdShadowScene.directionalTransmissionMultiplier.value;
                    hdShadowSource.cascadeShadowSplitCount.value = hdShadowScene.cascadeShadowSplitCount.value;
                    hdShadowSource.cascadeShadowSplit0.value = hdShadowScene.cascadeShadowSplit0.value;
                    hdShadowSource.cascadeShadowSplit1.value = hdShadowScene.cascadeShadowSplit1.value;
                    hdShadowSource.cascadeShadowSplit2.value = hdShadowScene.cascadeShadowSplit2.value;
                    hdShadowSource.cascadeShadowBorder0.value = hdShadowScene.cascadeShadowBorder0.value;
                    hdShadowSource.cascadeShadowBorder1.value = hdShadowScene.cascadeShadowBorder1.value;
                    hdShadowSource.cascadeShadowBorder2.value = hdShadowScene.cascadeShadowBorder2.value;
                    hdShadowSource.cascadeShadowBorder3.value = hdShadowScene.cascadeShadowBorder3.value;
                }
            }
        }
        private static void SyncHDRPContactShadows(VolumeProfile sceneProfile, VolumeProfile sourceProfile)
        {
            if (sceneProfile == null || sourceProfile == null)
            {
                return;
            }

            if (sceneProfile.TryGet(out ContactShadows contactShadowsScene))
            {
                if (sourceProfile.TryGet(out ContactShadows contactShadowsSource))
                {
                    contactShadowsSource.enable.value = contactShadowsScene.enable.value;
                    contactShadowsSource.length.value = contactShadowsScene.length.value;
                    contactShadowsSource.distanceScaleFactor.value = contactShadowsScene.distanceScaleFactor.value;
                    contactShadowsSource.maxDistance.value = contactShadowsScene.maxDistance.value;
#if UNITY_2020_1_OR_NEWER
                    contactShadowsSource.minDistance.value = contactShadowsScene.minDistance.value;
                    contactShadowsSource.fadeInDistance.value = contactShadowsScene.fadeInDistance.value;
#endif
                    contactShadowsSource.fadeDistance.value = contactShadowsScene.fadeDistance.value;
                    contactShadowsSource.opacity.value = contactShadowsScene.opacity.value;
                    contactShadowsSource.quality.value = contactShadowsScene.quality.value;
                    contactShadowsSource.sampleCount = contactShadowsScene.sampleCount;
                }
            }
        }
        private static void SyncHDRPMicroShadows(VolumeProfile sceneProfile, VolumeProfile sourceProfile)
        {
            if (sceneProfile == null || sourceProfile == null)
            {
                return;
            }

            if (sceneProfile.TryGet(out MicroShadowing microShadowingScene))
            {
                if (sourceProfile.TryGet(out MicroShadowing microShadowingSource))
                {
                    microShadowingSource.enable.value = microShadowingScene.enable.value;
                    microShadowingSource.opacity.value = microShadowingScene.opacity.value;
                }
            }
        }
        private static void SyncHDRPAmbientLight(VolumeProfile sceneProfile, VolumeProfile sourceProfile)
        {
            if (sceneProfile == null || sourceProfile == null)
            {
                return;
            }

#if UNITY_2020_1_OR_NEWER
            if (sceneProfile.TryGet(out IndirectLightingController visualEnvironmentScene))
            {
                if (sourceProfile.TryGet(out IndirectLightingController visualEnvironmentSource))
                {
#if !UNITY_2020_2_OR_NEWER
                    visualEnvironmentSource.indirectDiffuseIntensity.value = visualEnvironmentScene.indirectDiffuseIntensity.value;
                    visualEnvironmentSource.indirectSpecularIntensity.value = visualEnvironmentScene.indirectSpecularIntensity.value;
#endif
                    visualEnvironmentSource.indirectDiffuseLightingLayers.value = visualEnvironmentScene.indirectDiffuseLightingLayers.value;
                    visualEnvironmentSource.reflectionLightingMultiplier.value = visualEnvironmentScene.reflectionLightingMultiplier.value;
                    visualEnvironmentSource.reflectionLightingLayers.value = visualEnvironmentScene.reflectionLightingLayers.value;

                }
            }
#endif
        }
#endif

        #endregion
    }
}