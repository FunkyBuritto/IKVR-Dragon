using UnityEngine;

namespace Gaia
{
    [System.Serializable]
    public class CameraControllerData
    {
        public float targetHeight = 1.5f; // Vertical offset adjustment 
        public float distance = 6.0f; // Default Distance 
        public float offsetFromWall = 0.1f; // Bring camera away from any colliding objects 
        public float maxDistance = 20f; // Maximum zoom Distance 
        public float minDistance = 0.6f; // Minimum zoom Distance 
        public float xSpeed = 200.0f; // Orbit speed (Left/Right) 
        public float ySpeed = 200.0f; // Orbit speed (Up/Down) 
        public float yMinLimit = -80f; // Looking up limit 
        public float yMaxLimit = 80f; // Looking down limit 
        public float zoomRate = 40f; // Zoom Speed 
        public float rotationDampening = 0.5f; // Auto Rotation speed (higher = faster) 
        public float zoomDampening = 5.0f; // Auto Zoom speed (Higher = faster) 
        public LayerMask collisionLayers = 3841; // What the camera will collide with 
        public bool lockToRearOfTarget = false; // Lock camera to rear of target 
        public bool allowMouseInputX = true; // Allow player to control camera angle on the X axis (Left/Right) 
        public bool allowMouseInputY = true; // Allow player to control camera angle on the Y axis (Up/Down) 
    }
    public class CarControllerSetup : MonoBehaviour
    {
        public GameObject m_carFocusPoint;
        public Camera m_camera;
        public CameraControllerData m_cameraControllerData = new CameraControllerData();

        private void Start()
        {
            if (m_camera == null)
            {
                m_camera = GaiaUtils.GetCamera();
            }

            if (!VerifyCameraController())
            {
                Debug.LogError("Car Controller could not be setup correctly.");
            }
        }
        public bool VerifyCameraController()
        {
            if (m_camera == null)
            {
                m_camera = GaiaUtils.GetCamera();
                if (m_camera == null)
                {
                    return false;
                }
            }

            CameraController controller = m_camera.GetComponent<CameraController>();
            if (controller == null)
            {
                controller = m_camera.gameObject.AddComponent<CameraController>();
            }

            controller.target = m_carFocusPoint;
            controller.targetHeight = m_cameraControllerData.targetHeight;
            controller.distance = m_cameraControllerData.distance;
            controller.offsetFromWall = m_cameraControllerData.offsetFromWall;
            controller.maxDistance = m_cameraControllerData.maxDistance;
            controller.minDistance = m_cameraControllerData.minDistance;
            controller.xSpeed = m_cameraControllerData.xSpeed;
            controller.ySpeed = m_cameraControllerData.ySpeed;
            controller.yMinLimit = m_cameraControllerData.yMinLimit;
            controller.yMaxLimit = m_cameraControllerData.yMaxLimit;
            controller.zoomRate = m_cameraControllerData.zoomRate;
            controller.rotationDampening = m_cameraControllerData.rotationDampening;
            controller.zoomDampening = m_cameraControllerData.zoomDampening;
            controller.collisionLayers = m_cameraControllerData.collisionLayers;
            controller.lockToRearOfTarget = m_cameraControllerData.lockToRearOfTarget;
            controller.allowMouseInputX = m_cameraControllerData.allowMouseInputX;
            controller.allowMouseInputY = m_cameraControllerData.allowMouseInputY;

            return true;
        }
    }
}