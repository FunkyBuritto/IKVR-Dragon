using UnityEngine;

namespace Gaia
{
    public class PostProcessingConverter : MonoBehaviour
    {
        public GaiaConstants.EnvironmentRenderer RenderPipeline = GaiaConstants.EnvironmentRenderer.BuiltIn;
#if UNITY_POST_PROCESSING_STACK_V2
        public UnityEngine.Rendering.PostProcessing.PostProcessProfile ProcessProfile;
#endif
        public string SavePath = "Assets/";
    }
}