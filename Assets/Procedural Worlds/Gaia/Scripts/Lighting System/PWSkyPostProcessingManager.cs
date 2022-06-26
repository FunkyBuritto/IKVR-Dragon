using UnityEngine;

namespace Gaia
{
    public class PWSkyPostProcessingManager : MonoBehaviour
    {
        public void DisableWeatherPostFX()
        {
#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                ProceduralWorldsGlobalWeather.Instance.m_modifyPostProcessing = false;
            }
#endif
        }
        public void EnableWeatherPostFX()
        {
#if GAIA_PRO_PRESENT
            if (ProceduralWorldsGlobalWeather.Instance != null)
            {
                ProceduralWorldsGlobalWeather.Instance.m_modifyPostProcessing = true;
            }
#endif
        }
    }
}