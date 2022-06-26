using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gaia
{
    public enum PW_RENDER_SIZE
    {
        FULL = -1,
        HALF = -2,
        QUARTER = -3
    };
    public static class CommandBufferManagerShaderID
    {
        public static readonly string _cbufName = "Echo_Refaction";
        public static readonly int _grabID	= 0;
        public static readonly int m_cameraOpaqueTexture;

        static CommandBufferManagerShaderID()
        {
            m_cameraOpaqueTexture = Shader.PropertyToID("_CameraOpaqueTexture");
        }
    }

    [ExecuteAlways]
    public class CommandBufferManager : MonoBehaviour
    {
        #region Variables

        #region Public

        public bool m_allowHDR = true;
        public PW_RENDER_SIZE m_renderSize = PW_RENDER_SIZE.HALF;
        public CameraEvent m_cameraEventRefraction = CameraEvent.AfterImageEffectsOpaque;

        #endregion
        #region Private

        [SerializeField] private GaiaConstants.EnvironmentRenderer RenderPipeline = GaiaConstants.EnvironmentRenderer.BuiltIn;
        private const string m_bufferManagerObjectName = "Command Buffer Manager";
        #region Events

        private Dictionary<Camera, CommandBuffer> m_camerasRefraction = new Dictionary<Camera, CommandBuffer>();

        #endregion

        #endregion

        #endregion
        #region Unity Functions

        /// <summary>
        /// Start on enable
        /// </summary>
        private void OnEnable()
        {
            RebuildBuffers();
        }
        /// <summary>
        /// OnDisable ClearData
        /// </summary>
        private void OnDisable()
        {
            RebuildBuffers();
        }
        /// <summary>
        /// OnDestroy ClearData
        /// </summary>
        private void OnDestroy()
        {
            ClearBuffers();
        }

        #endregion
        #region Methods

        /// <summary>
        /// Function used to rebuild buffers
        /// </summary>
        public void RebuildBuffers(bool clear = true)
        {
            RenderPipeline = GaiaUtils.GetActivePipeline();
            if (clear)
            {
                ClearBuffers();
            }

            StartBuffers();
        }
        /// <summary>
        /// Clears all the buffers
        /// </summary>
        public void ClearBuffers()
        {
            switch (RenderPipeline)
            {
                case GaiaConstants.EnvironmentRenderer.BuiltIn:
                    Camera.onPreRender -= PreRender;
                    break;
                default:
                    RenderPipelineManager.beginCameraRendering -= PreRender;
                    break;
            }

            //Refraction
            foreach (var cam in m_camerasRefraction)
            {
                if (cam.Key)
                {
                    cam.Key.RemoveCommandBuffer(m_cameraEventRefraction, cam.Value);
                }
            }
            m_camerasRefraction.Clear();
        }
        /// <summary>
        /// Starts the buffer render process
        /// </summary>
        private void StartBuffers()
        {
            switch (RenderPipeline)
            {
                case GaiaConstants.EnvironmentRenderer.BuiltIn:
                    Camera.onPreRender += PreRender;
                    break;
                default:
                    RenderPipelineManager.beginCameraRendering += PreRender;
                    break;
            }
        }
        /// <summary>
        /// Process function to generate the buffers
        /// </summary>
        /// <param name="i_cam"></param>
        private void PreRender(Camera i_cam)
        {
            if (gameObject.activeInHierarchy)
            {
                RenderBuffers(i_cam);
            }
            else
            {
                ClearBuffers();
            }
        }
        /// <summary>
        /// Process function to generate the buffers
        /// </summary>
        /// <param name="i_cam"></param>
        private void PreRender(ScriptableRenderContext src, Camera i_cam)
        {
            if (gameObject.activeInHierarchy)
            {
                RenderBuffers(i_cam);
            }
            else
            {
                ClearBuffers();
            }
        }
        /// <summary>
        /// Function used to build the buffer data
        /// </summary>
        /// <param name="i_cam"></param>
        private void RenderBuffers(Camera i_cam)
        {
            if (i_cam != null)
            {
                if (i_cam.name.Contains("Reflection Probe"))
                {
                    return;
                }
            }

            //Refraction
            if (m_camerasRefraction.ContainsKey(i_cam))
            {
                return;
            }

            CommandBuffer cameraOpaqueBuffer = new CommandBuffer {name = CommandBufferManagerShaderID._cbufName};
            m_camerasRefraction[i_cam] = cameraOpaqueBuffer;
            if (m_allowHDR)
            {
                cameraOpaqueBuffer.GetTemporaryRT(CommandBufferManagerShaderID._grabID, (int) m_renderSize, (int) m_renderSize, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
            }
            else
            {
                cameraOpaqueBuffer.GetTemporaryRT(CommandBufferManagerShaderID._grabID, (int) m_renderSize, (int) m_renderSize, 0, FilterMode.Bilinear);
            }

            cameraOpaqueBuffer.Blit(BuiltinRenderTextureType.CurrentActive, CommandBufferManagerShaderID._grabID);
            cameraOpaqueBuffer.SetGlobalTexture(CommandBufferManagerShaderID.m_cameraOpaqueTexture, CommandBufferManagerShaderID._grabID);
            cameraOpaqueBuffer.ReleaseTemporaryRT(CommandBufferManagerShaderID._grabID);
            i_cam.AddCommandBuffer(m_cameraEventRefraction, cameraOpaqueBuffer);
        }

        #endregion
        #region Static Methods

        /// <summary>
        /// Creates the buffer manager in the scene
        /// </summary>
        public static void CreateBufferManager()
        {
            //Create Manager
            CommandBufferManager manager = FindObjectOfType<CommandBufferManager>();
            if (manager == null)
            {
                GameObject managerGameObject = new GameObject(m_bufferManagerObjectName);
                manager = managerGameObject.AddComponent<CommandBufferManager>();
                manager.RebuildBuffers();

                //Parent
                manager.transform.SetParent(GaiaUtils.GetRuntimeSceneObject().transform);
            }
        }

        #endregion
    }
}