using System;
using ProceduralWorlds.WaterSystem;
using UnityEngine;
using UnityEngine.Rendering;
#if UPPipeline
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace Gaia
{
    [ExecuteAlways]
    public class GaiaPlanarReflections : MonoBehaviour
    {
        [Serializable]
        public class PlanarReflectionSettings
        {
            public bool m_disableSkyboxReflections = false;
            public bool m_enableReflections = true;
            public GaiaConstants.ResolutionMulltiplier m_ResolutionMultiplier = GaiaConstants.ResolutionMulltiplier.Half;
            public float m_ClipPlaneOffset = 0f;
            public LayerMask m_ReflectLayers = -1;
            public bool m_Shadows = false;
            public bool m_enableRenderDistance = false;
            public bool m_enableRenderDistances = false;
            public float m_customRenderDistance = 500f;
            public float[] m_customRenderDistances = new float[32];
            public int m_textureResolution = 512;
        }
        public class PlanarReflectionSettingData
        {
            private bool _fog;
            private int _maxLod;
            private float _lodBias;

            public void Get()
            {
                _fog = RenderSettings.fog;
                _maxLod = QualitySettings.maximumLODLevel;
                _lodBias = QualitySettings.lodBias;
            }

            public void Set()
            {
                GL.invertCulling = true;
                RenderSettings.fog = true; // disable fog for now as it's incorrect with projection
                QualitySettings.maximumLODLevel = 1;
                QualitySettings.lodBias = _lodBias * 0.5f;
            }

            public void Restore()
            {
                GL.invertCulling = false;
                RenderSettings.fog = _fog;
                QualitySettings.maximumLODLevel = _maxLod;
                QualitySettings.lodBias = _lodBias;
            }
        }

        public PlanarReflectionSettings m_settings = new PlanarReflectionSettings();
        public PlanarReflectionSettingData m_reflectionData = new PlanarReflectionSettingData();
        public static event Action<ScriptableRenderContext, Camera> BeginPlanarReflections;
        public Camera m_gameCamera;

        [SerializeField]
        private static Camera m_reflectionCamera;
        [SerializeField]
        private RenderTexture m_reflectionTexture = null;
        [SerializeField]
        private float[] m_distances = new float[32];

        #region Unity Functions

        /// <summary>
        /// Load on enable
        /// </summary>
        private void OnEnable()
        {
            StartReflectionSystem();
        }
        /// <summary>
        /// Execute on disable
        /// </summary>
        private void OnDisable()
        {
            Cleanup();
        }
        /// <summary>
        /// Execute when the object is destroyed
        /// </summary>
        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Planar Reflection Functions

        /// <summary>
        /// Calculates the matrix of the reflection plane view
        /// </summary>
        /// <param name="reflectionMat"></param>
        /// <param name="plane"></param>
        private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
        {
            reflectionMat.m00 = (1f - 2f * plane[0] * plane[0]);
            reflectionMat.m01 = (-2f * plane[0] * plane[1]);
            reflectionMat.m02 = (-2f * plane[0] * plane[2]);
            reflectionMat.m03 = (-2f * plane[3] * plane[0]);

            reflectionMat.m10 = (-2f * plane[1] * plane[0]);
            reflectionMat.m11 = (1f - 2f * plane[1] * plane[1]);
            reflectionMat.m12 = (-2f * plane[1] * plane[2]);
            reflectionMat.m13 = (-2f * plane[3] * plane[1]);

            reflectionMat.m20 = (-2f * plane[2] * plane[0]);
            reflectionMat.m21 = (-2f * plane[2] * plane[1]);
            reflectionMat.m22 = (1f - 2f * plane[2] * plane[2]);
            reflectionMat.m23 = (-2f * plane[3] * plane[2]);

            reflectionMat.m30 = 0f;
            reflectionMat.m31 = 0f;
            reflectionMat.m32 = 0f;
            reflectionMat.m33 = 1f;
        }
        /// <summary>
        /// Updates position
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static Vector3 ReflectPosition(Vector3 pos)
        {
            var newPos = new Vector3(pos.x, -pos.y, pos.z);
            return newPos;
        }
        /// <summary>
        /// Updates the camera planes based on it's current view
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="pos"></param>
        /// <param name="normal"></param>
        /// <param name="sideSign"></param>
        /// <returns></returns>
        private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
        {
            var offsetPos = pos + normal * m_settings.m_ClipPlaneOffset;
            var m = cam.worldToCameraMatrix;
            var cameraPosition = m.MultiplyPoint(offsetPos);
            var cameraNormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z, -Vector3.Dot(cameraPosition, cameraNormal));
        }
        /// <summary>
        /// Builds the reflection texture if null or new resolution has been set
        /// </summary>
        private void PlanarReflectionTexture()
        {
            if (m_reflectionTexture == null)
            {
#if UPPipeline
                var res = ReflectionResolution(m_settings.m_textureResolution, UniversalRenderPipeline.asset.renderScale);
                const RenderTextureFormat hdrFormat = RenderTextureFormat.DefaultHDR;
                m_reflectionTexture = RenderTexture.GetTemporary(res.x, res.y, 24, GraphicsFormatUtility.GetGraphicsFormat(hdrFormat, true));
#endif
            }
            else
            {
                if (m_reflectionTexture.width != m_settings.m_textureResolution)
                {
#if UPPipeline
                    //
                    var res = ReflectionResolution(m_settings.m_textureResolution, UniversalRenderPipeline.asset.renderScale);
                    const RenderTextureFormat hdrFormat = RenderTextureFormat.DefaultHDR;
                    m_reflectionTexture = RenderTexture.GetTemporary(res.x, res.y, 24, GraphicsFormatUtility.GetGraphicsFormat(hdrFormat, true));
#endif
                }
            }
            m_reflectionCamera.targetTexture = m_reflectionTexture;
        }
        /// <summary>
        /// Gets the reflection resolution
        /// </summary>
        /// <param name="textureSize"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        private Vector2Int ReflectionResolution(int textureSize, float scale)
        {
            var x = (int)(textureSize * scale * GetScaleValue());
            var y = (int)(textureSize * scale * GetScaleValue());
            return new Vector2Int(x, y);
        }
        /// <summary>
        /// Executes the planar reflections functions
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cam"></param>
        public void ExecutePlanarReflections(ScriptableRenderContext context, Camera cam)
        {
            if (m_settings.m_disableSkyboxReflections)
            {
                return;
            }

            if (cam.cameraType == CameraType.Reflection || cam.cameraType == CameraType.Preview)
            {
                return;
            }

            UpdateReflectionCamera(cam);
            PlanarReflectionTexture();

            if (m_reflectionData == null)
            {
                m_reflectionData = new PlanarReflectionSettingData();
            }

            m_reflectionData.Get();
            m_reflectionData.Set();
            BeginPlanarReflections?.Invoke(context, m_reflectionCamera);
#if UPPipeline
            UniversalRenderPipeline.RenderSingleCamera(context, m_reflectionCamera);
#endif
            PWS_WaterSystem.Instance.SetReflectionTexture(m_reflectionTexture);
            m_reflectionData.Restore();
        }

        #endregion

        #region Utils

        /// <summary>
        /// Cleans up planar reflections
        /// </summary>
        private void Cleanup()
        {
            RenderPipelineManager.beginCameraRendering -= ExecutePlanarReflections;

            if (m_reflectionCamera != null)
            {
                m_reflectionCamera.targetTexture = null;
                SafeDestroy(m_reflectionCamera.gameObject);
            }
            if (m_reflectionTexture != null)
            {
                RenderTexture.ReleaseTemporary(m_reflectionTexture);
            }
        }

        private void StartReflectionSystem()
        {
            RenderPipelineManager.beginCameraRendering += ExecutePlanarReflections;
            m_gameCamera = GaiaUtils.GetCamera();
        }

        public void SetReflectionState(bool state)
        {
            m_settings.m_enableReflections = state;
        }
        /// <summary>
        /// Creates the reflection camera
        /// </summary>
        /// <returns></returns>
        private Camera CreateMirrorObjects()
        {
            var go = new GameObject("Gaia Planar Reflections Camera",typeof(Camera));
#if UPPipeline
            var cameraData = go.AddComponent(typeof(UniversalAdditionalCameraData)) as UniversalAdditionalCameraData;

            cameraData.requiresColorOption = CameraOverrideOption.Off;
            cameraData.requiresDepthOption = CameraOverrideOption.Off;
            cameraData.SetRenderer(0);
#endif

            var t = transform;
            var reflectionCamera = go.GetComponent<Camera>();
            reflectionCamera.transform.SetPositionAndRotation(t.position, t.rotation);
            reflectionCamera.depth = -10;
            reflectionCamera.enabled = false;
            go.hideFlags = HideFlags.HideAndDontSave;

            return reflectionCamera;
        }
        /// <summary>
        /// Destory the object
        /// </summary>
        /// <param name="obj"></param>
        private static void SafeDestroy(GameObject obj)
        {
            if (Application.isEditor)
            {
                DestroyImmediate(obj);
            }
            else
            {
                Destroy(obj);
            }
        }
        /// <summary>
        /// Updates the reflection camera settings
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        private void UpdateCamera(Camera src, Camera dest)
        {
            if (dest == null) return;

            dest.CopyFrom(src);
            dest.useOcclusionCulling = true;
            SetCameraBackgroundType(dest, CameraClearFlags.SolidColor);
#if UPPipeline
            if (dest.gameObject.TryGetComponent(out UniversalAdditionalCameraData camData))
            {
                camData.SetRenderer(0);
                camData.renderShadows = m_settings.m_Shadows; // turn off shadows for the reflection camera
            }
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        private void SetCameraBackgroundType(Camera camera, CameraClearFlags backgroundRenderMode)
        {
            if (camera == null)
            {
                return;
            }

            camera.clearFlags = backgroundRenderMode;
            camera.backgroundColor = RenderSettings.fogColor;
        }
        /// <summary>
        /// Updates the reflection camera for rendering
        /// </summary>
        /// <param name="realCamera"></param>
        private void UpdateReflectionCamera(Camera realCamera)
        {
            if (m_reflectionCamera == null)
            {
                m_reflectionCamera = CreateMirrorObjects();
            }

            Vector3 pos = Vector3.zero;
            Vector3 normal = Vector3.up;
            UpdateCamera(realCamera, m_reflectionCamera);

            var d = Vector3.Dot(normal, pos) - m_settings.m_ClipPlaneOffset;
            var reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);
            var reflection = Matrix4x4.identity;
            reflection = Matrix4x4.Scale(new Vector3(1, 1, 1));
            CalculateReflectionMatrix(ref reflection, reflectionPlane);
            var oldPosition = realCamera.transform.position - new Vector3(0, pos.y, 0);
            var newPosition = ReflectPosition(oldPosition);
            m_reflectionCamera.transform.forward = Vector3.Scale(realCamera.transform.forward, new Vector3(1, 1, 1));
            m_reflectionCamera.worldToCameraMatrix = realCamera.worldToCameraMatrix * reflection;
            var clipPlane = CameraSpacePlane(m_reflectionCamera, pos - Vector3.up * 0.1f, normal, 1.0f);
            var projection = realCamera.CalculateObliqueMatrix(clipPlane);
            m_reflectionCamera.projectionMatrix = projection;
            if (m_settings.m_enableReflections)
            {
                m_reflectionCamera.cullingMask = ~(1 << 4) & m_settings.m_ReflectLayers;
            }
            else
            {
                m_reflectionCamera.cullingMask = 0;
            }

            if (m_distances.Length != 32)
            {
                m_distances = new float[32];
            }
            if (m_settings.m_enableRenderDistance)
            {
                if (m_settings.m_enableRenderDistances)
                {
                    m_reflectionCamera.layerCullDistances = m_settings.m_customRenderDistances;
                    m_reflectionCamera.layerCullSpherical = true;
                }
                else
                {
                    for (int idx = 0; idx < m_distances.Length; idx++)
                    {
                        m_distances[idx] = m_settings.m_customRenderDistance;
                    }
                    m_reflectionCamera.layerCullDistances = m_distances;
                    m_reflectionCamera.layerCullSpherical = true;
                }
            }
            else
            {
                for (int idx = 0; idx < m_distances.Length; idx++)
                {
                    m_distances[idx] = 0f;
                }
                m_reflectionCamera.layerCullDistances = m_distances;
                m_reflectionCamera.layerCullSpherical = true;
            }

            m_reflectionCamera.transform.position = newPosition;
        }
        /// <summary>
        /// Gets the multiplier scale
        /// </summary>
        /// <returns></returns>
        private float GetScaleValue()
        {
            switch(m_settings.m_ResolutionMultiplier)
            {
                case GaiaConstants.ResolutionMulltiplier.Full:
                    return 1f;
                case GaiaConstants.ResolutionMulltiplier.Half:
                    return 0.5f;
                case GaiaConstants.ResolutionMulltiplier.Third:
                    return 0.33f;
                case GaiaConstants.ResolutionMulltiplier.Quarter:
                    return 0.25f;
                default:
                    return 0.5f;
            }
        }

        #endregion
    }
}