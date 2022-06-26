using UnityEngine;

namespace Gaia
{
    [ExecuteAlways]
    public class LocationSystem : MonoBehaviour
    {
        [HideInInspector]
        public LocationSystemScriptableObject m_locationProfile;
        [HideInInspector]
        public Transform m_camera;
        [HideInInspector]
        public Transform m_player;
        [HideInInspector]
        public bool m_trackPlayer = false;
        [HideInInspector]
        public LocationBookmarkSettings m_bookmarkSettings;
        [HideInInspector]
        public int m_selectedBookmark;
        [HideInInspector]
        public string m_bookmarkName = "My New Location Name";
        [HideInInspector] 
        public bool m_rename = false;
        [HideInInspector] 
        public string m_savedName;

        private void OnEnable()
        {
            Initilize();
            if (m_locationProfile == null)
            {
                return;
            }

            if (m_locationProfile.HasBeenSaved())
            {
                if (m_trackPlayer)
                {
                    m_locationProfile.LoadLocation(m_camera, m_player);
                }
            }
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                if (m_camera == null || m_locationProfile == null)
                {
                    return;
                }

                if (m_trackPlayer)
                {
                    m_locationProfile.SaveLocation(m_camera, m_player);
                }

                if (Input.GetKey(m_locationProfile.m_mainKey) && Input.GetKeyDown(m_locationProfile.m_prevBookmark))
                {
                    m_selectedBookmark--;
                    if (m_selectedBookmark < 0)
                    {
                        m_selectedBookmark = m_locationProfile.m_bookmarkedLocationNames.Count - 1;
                    }

                    m_locationProfile.LoadBookmark(this);
                }

                if (Input.GetKey(m_locationProfile.m_mainKey) && Input.GetKeyDown(m_locationProfile.m_nextBookmark))
                {
                    m_selectedBookmark++;
                    if (m_selectedBookmark > m_locationProfile.m_bookmarkedLocationNames.Count - 1)
                    {
                        m_selectedBookmark = 0;
                    }

                    m_locationProfile.LoadBookmark(this);
                }

                if (Input.GetKey(m_locationProfile.m_mainKey) && Input.GetKeyDown(m_locationProfile.m_addBookmarkKey))
                {
                    LocationBookmarkSettings settings = LocationSystemScriptableObject.GetCurrentLocation(this);
                    string name = "Bookmark " + " Position(" + settings.m_savedCameraPosition.x + ", " +
                                  settings.m_savedCameraPosition.y + ", " +
                                  settings.m_savedCameraPosition.z + ")";

                    m_locationProfile.AddNewBookmark(this, name);
                    m_selectedBookmark = m_locationProfile.m_bookmarkedLocationNames.Count - 1;
                    Debug.Log("New Bookmark: " + name);
                }
            }
        }

        private void Initilize()
        {
            if (m_locationProfile == null)
            {
                #if UNITY_EDITOR
                m_locationProfile = UnityEditor.AssetDatabase.LoadAssetAtPath<LocationSystemScriptableObject>(GaiaUtils.GetAssetPath("Location Profile.asset"));
                #endif
            }
        }
    }
}