using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if HDPipeline
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Gaia
{
    /// <summary>
    /// Utility class to provide the functionality of performing an "orthographic bake" from anywhere and return a render texture as result. 
    /// In an orthographic bake an orthographic camera is placed above the terrain pointing straight downwards to render the current view to a render texture.
    /// </summary>
    public class OrthographicBake
    {
        static Camera m_orthoCamera;
        public static RenderTexture m_tmpRenderTexture;
        private static List<Light> m_deactivatedLights = new List<Light>();
        private static GameObject m_bakeDirectionalLight;
        internal static int m_HDLODBiasOverride = 1;

        public static Camera CreateOrthoCam(Vector3 position, float nearClipping, float farClipping, float size, LayerMask cullingMask)
        {
            //existing ortho cam? Try to recycle
            GameObject gameObject = GameObject.Find("OrthoCaptureCam");

            if (gameObject == null)
            {
                gameObject = new GameObject("OrthoCaptureCam");
            }
            gameObject.transform.position = position;
            //facing straight downwards
            gameObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);


            //existing Camera? Try to recycle
            Camera cam = gameObject.GetComponent<Camera>();

            if (cam == null)
            {
                cam = gameObject.AddComponent<Camera>();
            }

            //setup camera the way we need it for the ortho bake - adjust everything to default to make sure there is no interference
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = cullingMask;
            cam.orthographic = true;
            cam.orthographicSize = size;
            cam.nearClipPlane = nearClipping;
            cam.farClipPlane = farClipping;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.depth = 0f;
            cam.renderingPath = RenderingPath.Forward; //Forward rendering required for orthographic
            cam.useOcclusionCulling = true;

#if HDPipeline
            HDAdditionalCameraData hdData = gameObject.GetComponent<HDAdditionalCameraData>();
            if (hdData == null)
            {
                hdData = cam.gameObject.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
            }
            hdData.volumeLayerMask = 0;
            hdData.backgroundColorHDR = Color.black;
            hdData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;

            FrameSettings frameSettings = new FrameSettings();
            frameSettings.lodBiasMode = LODBiasMode.OverrideQualitySettings;
            frameSettings.lodBias = m_HDLODBiasOverride;
            hdData.customRenderingSettings = true;
            hdData.renderingPathCustomFrameSettings = frameSettings;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[0] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.LODBiasMode] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.LODBias] = true;
#endif

            m_orthoCamera = cam;
            return cam;

        }

        public static void RemoveOrthoCam()
        {
            if (m_orthoCamera == null)
            {
                return;
            }

            if (m_orthoCamera.targetTexture != null)
            {
                RenderTexture.ReleaseTemporary(m_orthoCamera.targetTexture);
                m_orthoCamera.targetTexture = null;
            }

            GameObject.DestroyImmediate(m_orthoCamera.gameObject);
        }

        public static void BakeTerrain(Terrain terrain, int Xresolution, int Yresolution, LayerMask cullingMask, string path = null)
        {
            CreateOrthoCam(terrain.GetPosition() + new Vector3(terrain.terrainData.size.x / 2f, 0f, terrain.terrainData.size.z / 2f), -(terrain.terrainData.size.y + 200f), 1f, terrain.terrainData.size.x / 2f, cullingMask);
            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor();
            rtDesc.autoGenerateMips = true;
            rtDesc.bindMS = false;
            rtDesc.colorFormat = RenderTextureFormat.ARGB32;
            rtDesc.depthBufferBits = 24;
            rtDesc.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            rtDesc.enableRandomWrite = false;
            //rtDesc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8_SRGB;
            rtDesc.height = Yresolution;
            rtDesc.memoryless = RenderTextureMemoryless.None;
            rtDesc.msaaSamples = 1;
            rtDesc.sRGB = true;
            rtDesc.shadowSamplingMode = UnityEngine.Rendering.ShadowSamplingMode.None;
            rtDesc.useDynamicScale = false;
            rtDesc.useMipMap = false;
            rtDesc.volumeDepth = 1;
            rtDesc.vrUsage = VRTextureUsage.None;
            rtDesc.width = Xresolution;
            m_tmpRenderTexture = RenderTexture.GetTemporary(rtDesc);
            if (path != null)
            {
                RenderToPng(path);
            }
            else
            {
                RenderToTemporary();
            }
        }

        private static void RenderToTemporary()
        {
            if (m_orthoCamera == null)
            {
                Debug.LogError("Orthographic Bake: Camera does not exist!");
                return;
            }

            m_orthoCamera.targetTexture = m_tmpRenderTexture;
            m_orthoCamera.Render();
            //In the SRPs we get a flipped image when rendering from a camera to a render textures, need to flip it on the Y-axis for or purposes
#if HDPipeline || UPPipeline
            Material flipMat = new Material(Shader.Find("Hidden/Gaia/FlipY"));
            flipMat.SetTexture("_InputTex", m_tmpRenderTexture);
            RenderTexture buffer = RenderTexture.GetTemporary(m_tmpRenderTexture.descriptor);
            Graphics.Blit(m_tmpRenderTexture, buffer, flipMat);
            Graphics.Blit(buffer, m_tmpRenderTexture);
            RenderTexture.ReleaseTemporary(buffer);
#endif
            RenderTexture.active = m_tmpRenderTexture;
        }

        private static void RenderToPng(string path)
        {
            RenderToTemporary();
            ImageProcessing.WriteRenderTexture(path, m_tmpRenderTexture, GaiaConstants.ImageFileType.Png, TextureFormat.RGBA32);
            CleanUpRenderTexture();
        }

        /// <summary>
        /// switches off all active lights in the scene and stores the lights in a list to turn them back on later with LightsOn()
        /// </summary>
        public static void LightsOff()
        {
            m_deactivatedLights.Clear();
            var allLights = Resources.FindObjectsOfTypeAll<Light>();
            foreach (Light light in allLights)
            {
                if (light.isActiveAndEnabled)
                {
                    light.enabled = false;
                    m_deactivatedLights.Add(light);
                }
            }
        }

        /// <summary>
        /// turns all the lights on again that were disabled with LightsOff before
        /// </summary>
        public static void LightsOn()
        {
            foreach (Light light in m_deactivatedLights)
            {
                light.enabled = true;
            }
        }

        public static void CleanUpRenderTexture()
        {
            m_orthoCamera.targetTexture = null;
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(m_tmpRenderTexture);
        }

        /// <summary>
        /// Creates a directional light pointing straight downwards on the y-axis with the given intensity & color. Use this together with LightsOn & LightsOff to better control lighting during the scene.
        /// </summary>
        /// <param name="intensity">Intensity for the directional light.</param>
        /// <param name="color">Color for the directional light.</param>
        public static void CreateBakeDirectionalLight(float intensity, Color color)
        {
            GameObject lightGO = GameObject.Find(GaiaConstants.BakeDirectionalLight);
            if (lightGO == null)
            {
                lightGO = new GameObject(GaiaConstants.BakeDirectionalLight);
            }
            m_bakeDirectionalLight = lightGO;
            Light light = lightGO.GetComponent<Light>();
            if (light == null)
            {
                light = lightGO.AddComponent<Light>();
            }
            light.shadows = LightShadows.None;
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(90, 0, 0);
            light.intensity = intensity;
            light.color = color;

        }

        /// <summary>
        /// Removes the bake directional light (Created with CreateBakeDirectionalLight()) from the scene again.
        /// </summary>
        public static void RemoveBakeDirectionalLight()
        {
            if (m_bakeDirectionalLight != null)
            {
                GameObject.DestroyImmediate(m_bakeDirectionalLight);
            }
        }
    }

}