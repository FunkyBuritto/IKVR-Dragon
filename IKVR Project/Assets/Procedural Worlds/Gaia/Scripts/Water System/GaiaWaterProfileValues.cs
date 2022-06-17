using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace Gaia
{
    [System.Serializable]
    public class GaiaWaterProfileValues
    {
        [Header("Global Settings")]
        public string m_typeOfWater = "Deep Blue Ocean";
        public string m_profileRename;
        public bool m_userCustomProfile = false;

        [Header("Post Processing Settings")]
#if UNITY_POST_PROCESSING_STACK_V2
        [SerializeField]
        private PostProcessProfile m_postProcessProfileBuiltIn = null;
        public PostProcessProfile PostProcessProfileBuiltIn 
        {
            get 
            {
                if (m_postProcessProfileBuiltIn == null && m_postProcessingProfileGUIDBuiltIn !=null)
                {
#if UNITY_EDITOR
                    m_postProcessProfileBuiltIn = (PostProcessProfile)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_postProcessingProfileGUIDBuiltIn),typeof(PostProcessProfile));
#endif
                }
                return m_postProcessProfileBuiltIn;
            }
            set 
            {
#if UNITY_EDITOR
                m_postProcessingProfileGUIDBuiltIn = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
                m_postProcessProfileBuiltIn = value;
#endif
            }
        }
#endif
        //need this serialized to remember the GUID even when PP is not installed in the project
        public string m_postProcessingProfileGUIDBuiltIn = "";
        public bool m_directToCamera = true;
#if UPPipeline
        [Header("URP Post Processign Settings")]
        [SerializeField]
        private VolumeProfile m_postProcessProfileURP = null;
        public VolumeProfile PostProcessProfileURP
        {
            get 
            {
                if (m_postProcessProfileURP == null && m_postProcessingProfileGUIDURP !=null)
                {
#if UNITY_EDITOR
                    m_postProcessProfileURP = (VolumeProfile)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_postProcessingProfileGUIDURP),typeof(VolumeProfile));
#endif
                }
                return m_postProcessProfileURP;
            }
            set 
            {
#if UNITY_EDITOR
                m_postProcessingProfileGUIDURP = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
                m_postProcessProfileURP = value;
#endif
            }
        }
#endif
        //need this serialized to remember the GUID even when PP is not installed in the project
        public string m_postProcessingProfileGUIDURP = "";

#if HDPipeline
        [Header("HDRP Post Processing Settings")]
        [SerializeField]
        private VolumeProfile m_postProcessProfileHDRP = null;
        public VolumeProfile PostProcessProfileHDRP
        {
            get 
            {
                if (m_postProcessProfileHDRP == null && m_postProcessingProfileGUIDHDRP !=null)
                {
#if UNITY_EDITOR
                    m_postProcessProfileHDRP = (VolumeProfile)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_postProcessingProfileGUIDHDRP),typeof(VolumeProfile));
#endif
                }
                return m_postProcessProfileHDRP;
            }
            set 
            {
#if UNITY_EDITOR
                m_postProcessingProfileGUIDHDRP = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
                m_postProcessProfileHDRP = value;
#endif
            }
        }
#endif
        //need this serialized to remember the GUID even when PP is not installed in the project
        [SerializeField]
        public string m_postProcessingProfileGUIDHDRP = "";

        [Header("Underwater Effects")]
        public Gradient m_underwaterFogGradient;
        public Color m_underwaterFogColor = Color.cyan;
        public float m_underwaterFogDepth = 100f;
        public float m_underwaterFogDistance = 45f;
        public float m_underwaterNearFogDistance = -4f;
        public float m_underwaterFogDensity = 0.045f;
        public float m_constUnderwaterPostExposure = 0f;
        public AnimationCurve m_gradientUnderwaterPostExposure = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f));
        public Color m_constUnderwaterColorFilter = Color.white;
        public Gradient m_gradientUnderwaterColorFilter = new Gradient();

        [Header("Texture Settings")]
        public Texture2D m_colorDepthRamp;
        public Texture2D m_normalLayer0;
        public Texture2D m_normalLayer1;
        public Texture2D m_fadeNormal;
        public Texture2D m_foamTexture;
        public Texture2D m_foamAlphaRamp;
        public Texture2D m_renderTexture;
        public Texture2D m_foamBubbleTexture;

        [Header("Water Setup")]
        public GaiaConstants.PW_RENDER_SIZE m_refractionRenderResolution = GaiaConstants.PW_RENDER_SIZE.HALF;
        public Gradient m_waterGradient;
        public int m_gradientTextureResolution = 128;
        public int m_foamTiling = 128;
        public int m_waterTiling = 256;
        public float m_shorelineMovement = 0.1f;
        public float m_waveCount = 0.2f;
        public float m_waveSpeed = 3f;
        public float m_waveSize = 0.26f;
        public float m_transparentDistance = 10f;
        public float m_foamDistance = 8f;
        public float m_reflectionDistortion = 0.7f;
        public float m_reflectionStrength = 0.5f;
        public Color m_specularColor = Color.white;
        public float m_metallic = 0.25f;
        public float m_smoothness = 0.9f;
        public float m_normalStrength0 = 0.4f;
        public float m_normalStrength1 = 0.8f;
        public float m_fadeNormalStrength = 1f;
        public float m_fadeStart = 32f;
        public float m_fadeDistance = 128f;
        public float m_foamBubbleScale = 0.2f;
        public int m_foamBubbleTiling = 1024;
        public float m_foamStrength = 4f;
        public float m_foamBubblesStrength = 1f;
        public float m_foamEdge = 1f;
        public float m_foamMoveSpeed = 0.3f;

        private void RemoveWarnings()
        {
            if (m_postProcessingProfileGUIDBuiltIn.Length == 0)
            {
                m_postProcessingProfileGUIDBuiltIn = "";
            }

            if (m_postProcessingProfileGUIDURP.Length == 0)
            {
                m_postProcessingProfileGUIDURP = "";
            }

            if (m_postProcessingProfileGUIDHDRP.Length == 0)
            {
                m_postProcessingProfileGUIDHDRP = "";
            }
        }
    }
}